#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
MidiBill3 - a MIDI felvevo SMF-iro logikajanak ellenorzese
===========================================================

MIT ELLENORIZ EZ A SZKRIPT?

A `Modellek/MidiFelvevo.vb` osztaly Standard MIDI File (.mid) iro
logikajanak HU PYTHON-PORTJAT futtatja le egy szimulalt felvetelen, majd
a keletkezett fajlt egy FUGGETLEN MIDI-konyvtarral (mido) olvassa vissza,
es allitasokkal ellenorzi, hogy minden ugy jott-e vissza, ahogy kell.

Igy derul ki, hogy a valasztott fajlformatum-megoldasok helyesek-e:
  - MThd fejlec, format 1, 2 track (conductor + felvett anyag)
  - 480 tick / negyed hangjegy felbontas, 120 BPM (500 000 us/negyed)
  - variable-length quantity (VLQ) delta-time kodolas, tobb-bajtos esetben is
  - a Program Change / Channel Pressure EGY adatbajtja a tobbi uzenet
    ket adatbajtjaval szemben
  - az ezredmasodperces idobelyegek -> tick atvaltasa kerekitesi
    hibafelhalmozas nelkul (eloszor abszolut tick, csak utana delta)

MIT NEM ELLENORIZ?

  1. Magat a VB kodot: ez egy PORT, nem a VB futtatasa. Ha a
     `MidiFelvevo.vb` SMF-iro resze valtozik, EZT A SZKRIPTET IS
     FRISSITENI KELL, kulonben mar nem ugyanazt a logikat vizsgalja.
  2. A VB oldali allapotgepet (Alap / Felfegyverezve / Rogzit), a
     "logo" hangok lezarasat, az overdub- es metronom-szurest, valamint
     a rendszer-uzenetek (>= 0xF0) kiszureset - ezek tisztan VB-oldali
     logikak, itt csak a mar leszurt esemenylista feldolgozasa fut.

HASZNALAT

    pip install mido
    python3 smf_iro_ellenorzes.py

Sikeres futas eseten a vegen "MINDEN ELLENORZES RENDBEN" felirat all, es
a kilepesi kod 0. Barmely elteres eseten AssertionError szall el.

Keszult: 2026-09-04, a MIDI felvetel 2. fazisanak ellenorzesehez.
Ellenorzo konyvtar: mido 1.3.3
"""

import os
import sys
import tempfile

# ---------------------------------------------------------------------
# A MidiFelvevo.vb konstansai es SMF-iro logikaja, hu portban
# ---------------------------------------------------------------------

TICK_PER_NEGYED = 480
MIKROSZEC_PER_NEGYED = 500000                                     # 500 000 us = 120 BPM
TICK_PER_MS = TICK_PER_NEGYED * 1000.0 / MIKROSZEC_PER_NEGYED     # = 0,96

UTEM_SZAMLALO = 4
UTEM_NEVEZO_KITEVO = 2          # 2^2 = 4 -> 4/4
METRONOM_UTES_TICK = 24
HARMINCKETTED_NEGYEDENKENT = 8


def be_int_bajtok(ertek, hossz):
    """Egesz szam big-endian bajtsorozatta (a MIDI fajlformatum vegig big-endian)."""
    return bytes(((ertek >> (8 * (hossz - 1 - i))) & 0xFF) for i in range(hossz))


def vlq(ertek):
    """Variable-length quantity: 7 bites csoportok, az utolso KIVETELEVEL 0x80-nal jelolve."""
    e = 0 if ertek < 0 else ertek
    csoportok = [e & 0x7F]
    maradek = e >> 7
    while maradek > 0:
        csoportok.append((maradek & 0x7F) | 0x80)
        maradek >>= 7
    return bytes(reversed(csoportok))


def adatbajtok_szama(statusz):
    """Program Change (0xC0) es Channel Pressure (0xD0) egybajtos, minden mas ketbajtos."""
    tipus = statusz & 0xF0
    return 1 if tipus in (0xC0, 0xD0) else 2


def adatbajt(ertek):
    """MIDI adatbajt 0-127 tartomanyra szoritva (a felso bit a szabvany szerint mindig 0)."""
    return max(0, min(127, ertek))


def vege_jel():
    """Minden MTrk chunk kotelezo zaro meta-esemenye: delta 0 + FF 2F 00."""
    return bytes([0, 0xFF, 0x2F, 0])


def chunk_epitese(azonosito, adat_resz):
    return azonosito.encode("ascii") + be_int_bajtok(len(adat_resz), 4) + adat_resz


def conductor_track_epitese():
    """A 0. track: tempo + utemjelzes + a kotelezo End of Track."""
    adat = bytearray([0, 0xFF, 0x51, 3])
    adat += be_int_bajtok(MIKROSZEC_PER_NEGYED, 3)
    adat += bytes([0, 0xFF, 0x58, 4,
                   UTEM_SZAMLALO, UTEM_NEVEZO_KITEVO,
                   METRONOM_UTES_TICK, HARMINCKETTED_NEGYEDENKENT])
    adat += vege_jel()
    return bytes(adat)


def kezdoallapot_blokk(kezdo_uzenetek, esemenyek):
    """
    A csatorna-allapot uzenetei tick 0-n, a felvett anyag ELE - de csak
    azokra a csatornakra, amelyeken tenylegesen van felvett anyag
    (ugyanaz a vedelmi elv, mint a MidiFajlSzerkeszto-ben).
    """
    hasznalt = {st & 0x0F for (st, _d1, _d2, _ido) in esemenyek}
    adat = bytearray()
    for (st, d1, d2, adatbajt_szam) in kezdo_uzenetek:
        if (st & 0x0F) not in hasznalt:
            continue
        adat.append(0)                    # delta-time = 0
        adat.append(st & 0xFF)
        adat.append(adatbajt(d1))
        if adatbajt_szam == 2:
            adat.append(adatbajt(d2))
    return bytes(adat)


def adat_track_epitese(esemenyek, elso_idobelyeg_ms, kezdo_uzenetek=()):
    """
    Az 1. track: a felvett uzenetek delta-time + esemeny parokkent.

    Az ezredmasodperces idobelyegek ABSZOLUT tick-erteke szamolodik ki
    eloszor, es csak UTANA vonodnak ki egymasbol - ha delta-nkent
    kerekitenenk, a hibak egy hosszabb felvetelen osszeadodnanak.
    """
    adat = bytearray()
    adat += kezdoallapot_blokk(kezdo_uzenetek, esemenyek)
    elozo_tick = 0

    for (statusz, d1, d2, idobelyeg_ms) in esemenyek:
        relativ_ms = idobelyeg_ms - elso_idobelyeg_ms
        abszolut_tick = int(round(relativ_ms * TICK_PER_MS))
        if abszolut_tick < elozo_tick:
            abszolut_tick = elozo_tick

        adat += vlq(abszolut_tick - elozo_tick)
        elozo_tick = abszolut_tick

        adat.append(statusz & 0xFF)
        adat.append(adatbajt(d1))
        if adatbajtok_szama(statusz) == 2:
            adat.append(adatbajt(d2))

    adat += vege_jel()
    return bytes(adat)


def smf_fajl_epitese(esemenyek, kezdo_uzenetek=()):
    """A teljes .mid fajl: MThd + conductor track + adat track."""
    elso = esemenyek[0][3]
    fajl = bytearray()
    fajl += "MThd".encode("ascii")
    fajl += be_int_bajtok(6, 4)                   # a fejlec adatreszenek hossza mindig 6
    fajl += be_int_bajtok(1, 2)                   # format 1
    fajl += be_int_bajtok(2, 2)                   # 2 track
    fajl += be_int_bajtok(TICK_PER_NEGYED, 2)     # felbontas
    fajl += chunk_epitese("MTrk", conductor_track_epitese())
    fajl += chunk_epitese("MTrk", adat_track_epitese(esemenyek, elso, kezdo_uzenetek))
    return bytes(fajl)


# ---------------------------------------------------------------------
# Szimulalt felvetel
# ---------------------------------------------------------------------
# Formatum: (statuszbajt, adat1, adat2, abszolut idobelyeg ms-ban).
# Az idobelyegek SZANDEKOSAN nem 0-tol indulnak: a valosagban is a
# MidiEngine folyamatosan futo Stopwatch-anak tetszoleges pillanatatol
# kezdodik a felvetel, es a felvevo az elso esemenyhez kepest szamol.

SZIMULALT_FELVETEL = [
    (0xC0, 40, 0,   10000),   # Program Change - EGY adatbajt!
    (0xB0, 7, 100,  10000),   # CC7 (Main Volume)
    (0x90, 60, 100, 10000),   # C4 be, 1. csatorna
    (0x91, 72, 100, 10000),   # C5 be, 2. csatorna (fan-out: azonos idobelyeg)
    (0x90, 60, 0,   10500),   # C4 ki, 0 velocity-s Note On formaban
    (0x91, 72, 0,   10500),
    (0x90, 62, 90,  10500),
    (0x80, 62, 0,   13456),   # valodi Note Off, "csunya" idopont
    (0x90, 64, 90,  70000),   # nagy szunet -> tobb-bajtos VLQ delta-time
    (0x80, 64, 0,   70250),
]

# A felvetel INDULASAKOR keszult csatorna-allapot pillanatkep.
# Formatum: (statuszbajt, adat1, adat2, adatbajtok szama).
# A 3. csatorna (index 2) SZANDEKOSAN szerepel itt, de a felvetelben NEM
# jatszik egy hangot sem - ellenorizzuk, hogy a szuro kihagyja.
SZIMULALT_KEZDOALLAPOT = []
for _cs, _program, _hangero in ((0, 40, 100), (1, 48, 90), (2, 33, 80)):
    SZIMULALT_KEZDOALLAPOT += [
        (0xB0 | _cs, 0, 0, 2),          # Bank Select MSB
        (0xB0 | _cs, 32, 0, 2),         # Bank Select LSB
        (0xC0 | _cs, _program, 0, 1),   # Program Change
        (0xB0 | _cs, 7, _hangero, 2),   # Main Volume
        (0xB0 | _cs, 10, 64, 2),        # Pan
        (0xB0 | _cs, 1, 0, 2),          # Mod Wheel (vibrato)
        (0xB0 | _cs, 93, 0, 2),         # Chorus
        (0xB0 | _cs, 91, 25, 2),        # Reverb
    ]

# A kezdoallapotbol EZEKET varjuk vissza: csak az 1. es 2. csatorna (8-8
# uzenet), a 3. csatornae kimarad, mert azon nincs felvett anyag.
VART_KEZDOALLAPOT = [(st, d1, d2) for (st, d1, d2, _n) in SZIMULALT_KEZDOALLAPOT
                     if (st & 0x0F) in (0, 1)]

# Amit a visszaolvasaskor varunk: (masodperc, tipus, csatorna, hang/kontroller)
VART_UZENETEK = [
    (0.000, "program_change", 0, 40),
    (0.000, "control_change", 0, 7),
    (0.000, "note_on",        0, 60),
    (0.000, "note_on",        1, 72),
    (0.500, "note_on",        0, 60),   # velocity 0 = hangkikapcsolas
    (0.500, "note_on",        1, 72),
    (0.500, "note_on",        0, 62),
    (3.456, "note_off",       0, 62),
    (60.000, "note_on",       0, 64),
    (60.250, "note_off",      0, 64),
]

IDO_TURES_MP = 0.002   # 2 ms: a 0,96 tick/ms atvaltas kerekitesi hataran belul


def main():
    try:
        import mido
    except ImportError:
        print("HIANYZO FUGGOSEG: pip install mido", file=sys.stderr)
        return 1

    adat = smf_fajl_epitese(SZIMULALT_FELVETEL, SZIMULALT_KEZDOALLAPOT)

    utvonal = os.path.join(tempfile.mkdtemp(), "midibill_teszt.mid")
    with open(utvonal, "wb") as f:
        f.write(adat)

    print("Generalt fajl:", utvonal)
    print("Fajlmeret:", len(adat), "bajt")
    print("MThd fejlec:", adat[:14].hex(" "))
    print()

    mf = mido.MidiFile(utvonal)

    # --- Fejlec-szintu ellenorzesek ---
    assert mf.type == 1, f"format 1-et vartunk, ez format {mf.type}"
    assert len(mf.tracks) == 2, f"2 tracket vartunk, ez {len(mf.tracks)}"
    assert mf.ticks_per_beat == TICK_PER_NEGYED, \
        f"{TICK_PER_NEGYED} tick/negyedet vartunk, ez {mf.ticks_per_beat}"
    print(f"OK  format {mf.type}, {len(mf.tracks)} track, {mf.ticks_per_beat} tick/negyed")

    # --- Tempo a conductor trackben ---
    tempok = [m for m in mf.tracks[0] if m.type == "set_tempo"]
    assert len(tempok) == 1, f"pontosan 1 tempo-esemenyt vartunk, ez {len(tempok)}"
    assert tempok[0].tempo == MIKROSZEC_PER_NEGYED, \
        f"{MIKROSZEC_PER_NEGYED} us/negyedet vartunk, ez {tempok[0].tempo}"
    print(f"OK  tempo: {tempok[0].tempo} us/negyed "
          f"({round(60_000_000 / tempok[0].tempo)} BPM)")

    # --- Utemjelzes a conductor trackben ---
    utemek = [m for m in mf.tracks[0] if m.type == "time_signature"]
    assert len(utemek) == 1, f"pontosan 1 utemjelzest vartunk, ez {len(utemek)}"
    assert utemek[0].numerator == UTEM_SZAMLALO, \
        f"{UTEM_SZAMLALO}-es szamlalot vartunk, ez {utemek[0].numerator}"
    assert utemek[0].denominator == 2 ** UTEM_NEVEZO_KITEVO, \
        f"{2 ** UTEM_NEVEZO_KITEVO}-es nevezot vartunk, ez {utemek[0].denominator}"
    print(f"OK  utemjelzes: {utemek[0].numerator}/{utemek[0].denominator}")

    # --- Minden MTrk vegen ott a kotelezo End of Track ---
    for i, track in enumerate(mf.tracks):
        assert any(m.type == "end_of_track" for m in track), \
            f"a(z) {i}. track vegerol hianyzik az End of Track"
    print("OK  mindket track vegen ott az End of Track")

    # --- Uzenetek es idozites ---
    idozitett = []
    ido = 0.0
    for msg in mf:
        ido += msg.time
        if not msg.is_meta:
            idozitett.append((ido, msg))

    # A blokk elso len(VART_KEZDOALLAPOT) uzenete a csatorna-allapot kell legyen,
    # mind tick 0-n, MEG a felvett anyag elott.
    assert len(idozitett) == len(VART_KEZDOALLAPOT) + len(VART_UZENETEK), \
        (f"{len(VART_KEZDOALLAPOT)} kezdoallapot + {len(VART_UZENETEK)} felvett "
         f"uzenetet vartunk, ez osszesen {len(idozitett)}")

    print()
    print("Kezdoallapot-blokk (a felvett anyag elott, tick 0-n):")
    for (ido, msg), (vart_st, vart_d1, vart_d2) in zip(idozitett, VART_KEZDOALLAPOT):
        assert ido == 0.0, f"a kezdoallapot tick 0-n kell legyen, ez {ido}s"
        assert msg.channel == (vart_st & 0x0F), \
            f"kezdoallapot: {vart_st & 0x0F}. csatornat vartunk, ez {msg.channel}"
        if msg.type == "program_change":
            assert msg.program == vart_d1, f"program {vart_d1}-t vartunk, ez {msg.program}"
        else:
            assert msg.control == vart_d1 and msg.value == vart_d2, \
                f"CC{vart_d1}={vart_d2}-t vartunk, ez CC{msg.control}={msg.value}"
        print(f"  {ido:8.3f}s  {msg}")

    csatornak = sorted({m.channel for _i, m in idozitett[:len(VART_KEZDOALLAPOT)]})
    assert csatornak == [0, 1], \
        f"a kezdoallapotban csak a 0. es 1. csatornat vartuk (a 3. nem jatszik), ez {csatornak}"
    print(f"OK  {len(VART_KEZDOALLAPOT)} kezdoallapot-uzenet, csak a jatszo csatornakra "
          f"(a nem jatszo 3. csatorna helyesen kimaradt)")

    idozitett = idozitett[len(VART_KEZDOALLAPOT):]

    print()
    print("Visszaolvasott uzenetek:")
    for (tenyleges_ido, msg), (vart_ido, vart_tipus, vart_csatorna, vart_ertek) \
            in zip(idozitett, VART_UZENETEK):

        assert msg.type == vart_tipus, f"{vart_tipus}-t vartunk, ez {msg.type}"
        assert msg.channel == vart_csatorna, \
            f"{vart_tipus}: {vart_csatorna}. csatornat vartunk, ez {msg.channel}"

        tenyleges_ertek = msg.note if hasattr(msg, "note") else \
            (msg.control if hasattr(msg, "control") else msg.program)
        assert tenyleges_ertek == vart_ertek, \
            f"{vart_tipus}: {vart_ertek} erteket vartunk, ez {tenyleges_ertek}"

        elteres = abs(tenyleges_ido - vart_ido)
        assert elteres <= IDO_TURES_MP, \
            f"{vart_tipus} @ {vart_ido}s: {elteres:.4f}s elteres (max {IDO_TURES_MP}s)"

        print(f"  {tenyleges_ido:8.3f}s  {msg}")

    print()
    print(f"OK  mind a {len(VART_UZENETEK)} uzenet a helyen, "
          f"idozites hibahatáron belul (max {IDO_TURES_MP * 1000:.0f} ms)")

    # --- A teljes hossz is stimmel ---
    vart_hossz = (SZIMULALT_FELVETEL[-1][3] - SZIMULALT_FELVETEL[0][3]) / 1000.0
    assert abs(mf.length - vart_hossz) <= IDO_TURES_MP, \
        f"{vart_hossz}s hosszt vartunk, ez {mf.length}s"
    print(f"OK  teljes hossz: {mf.length}s")

    print()
    print("=" * 52)
    print("MINDEN ELLENORZES RENDBEN")
    print("=" * 52)
    return 0


if __name__ == "__main__":
    sys.exit(main())

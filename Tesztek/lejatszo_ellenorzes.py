#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
MidiBill3 - a lejatszo esemenylistajanak ellenorzese
====================================================

MIT ELLENORIZ EZ A SZKRIPT?

A `Modellek/MidiFajlOlvaso.vb` EsemenyekBajtokbol() fuggvenyenek (a
lejatszashoz keszulo esemenylista, a felvetel 5. fazisa) HU PYTHON-PORTJAT
futtatja kezzel osszerakott MIDI fajlokon, es az eredmenyt a FUGGETLEN mido
konyvtar kiolvasasaval veti ossze:

  - minden csatorna-uzenet benne van, es semmi mas (meta, SysEx kimarad)
  - az idozites ezredmasodpercre stimmel, tempovaltasokkal egyutt (a
    tempovaltas a 0. trackben van, a hangok mas trackekben)
  - AZONOS idopontnal a track sorrendje, azon belul a fajlbeli sorrend dont
    (pl. a Bank Select a Program Change ele kerul)
  - running status, egy adatbajtos uzenetek (Program Change, Channel
    Pressure), Pitch Bend, 0 sebessegu Note On
  - format 0 es format 1 fajl, ismeretlen chunk atugrasa
  - a fajl hossza (a leghosszabb track vege)
  - hibas fajlok: SMPTE-idozites, hianyzo MTrk, hianyzo MThd -> hiba;
    csonka track -> az addigi resz ervenyes

A szkript a vegen SZANDEKOSAN ELRONTOTT valtozatokat is lefuttat (mutacios
teszt) - mindegyiknek MEG KELL BUKNIA.

Az `--export <mappa>` kapcsoloval a tesztfajlokat es a port szoveges
kimenetet kiirja - a Linuxon forditott VALODI VB kod ugyanezeket a fajlokat
olvassa be, es a kimenete betura egyezik a portéval.

HASZNALAT

    pip install mido
    python3 lejatszo_ellenorzes.py

Keszult: 2026-09-26, a MIDI felvetel 5. fazisahoz (lejatszas).
Ellenorzo konyvtar: mido 1.3.3
"""

import os
import sys
import tempfile

HIBA = {}


# ---------------------------------------------------------------------
# A MidiFajlOlvaso.EsemenyekBajtokbol hu portja
# ---------------------------------------------------------------------

class ErvenytelenFajl(Exception):
    pass


def be_int(adat, pos, hossz):
    if pos + hossz > len(adat):
        return -1 if hossz == 4 else 0
    ertek = 0
    for i in range(hossz):
        ertek = (ertek << 8) | adat[pos + i]
    return ertek


def vlq(adat, pos, hatar):
    v, db = 0, 0
    while pos < hatar:
        b = adat[pos]
        pos += 1
        db += 1
        v = (v << 7) | (b & 0x7F)
        if not (b & 0x80):
            break
        if db >= 4:
            break
    return pos, v


def track_esemenyei(adat, kezdet, vege, nyers, tempok):
    pos, tick, elozo = kezdet, 0, 0
    while pos < vege:
        pos, delta = vlq(adat, pos, vege)
        if pos >= vege:
            break
        tick += delta
        elso = adat[pos]
        if elso >= 0x80:
            statusz = elso
            pos += 1
        else:
            if HIBA.get("nincs_running_status"):
                break
            statusz = elozo
            if statusz == 0:
                break
        if statusz == 0xFF:
            if pos >= vege:
                break
            tipus = adat[pos]
            pos += 1
            pos, hossz = vlq(adat, pos, vege)
            if hossz < 0 or pos + hossz > vege:
                break
            if tipus == 0x51 and hossz >= 3:
                us = (adat[pos] << 16) | (adat[pos + 1] << 8) | adat[pos + 2]
                if us > 0:
                    tempok.append((tick, us))
            pos += hossz
            elozo = 0
        elif statusz in (0xF0, 0xF7):
            pos, hossz = vlq(adat, pos, vege)
            if hossz < 0 or pos + hossz > vege:
                break
            pos += hossz
            elozo = 0
        elif 0x80 <= statusz < 0xF0:
            elozo = statusz
            tipus = statusz & 0xF0
            n = 1 if tipus in (0xC0, 0xD0) and not HIBA.get("program_change_ket_bajt") else 2
            if pos + n > vege:
                break
            d1 = adat[pos] & 0x7F
            d2 = (adat[pos + 1] & 0x7F) if n == 2 else 0
            pos += n
            nyers.append((tick, statusz, d1, d2))
        else:
            break
    return tick


class TickAtvalto:
    def __init__(self, terkep, tpq):
        self.tick = [t for t, _ in terkep]
        self.ms_per_tick = [us / 1000.0 / tpq for _, us in terkep]
        self.ms = [0.0]
        for i in range(1, len(terkep)):
            self.ms.append(self.ms[i - 1] + (self.tick[i] - self.tick[i - 1]) * self.ms_per_tick[i - 1])

    def ezredmasodperc(self, tick):
        if tick <= 0:
            return 0.0
        if HIBA.get("csak_kezdo_tempo"):
            return tick * self.ms_per_tick[0]
        also = 0
        for i in range(len(self.tick)):
            if self.tick[i] <= tick:
                also = i
        return self.ms[also] + (tick - self.tick[also]) * self.ms_per_tick[also]


def tempoterkep(tempok):
    terkep = sorted(tempok, key=lambda t: t[0])
    if not terkep or terkep[0][0] > 0:
        terkep.insert(0, (0, 500000))
    return terkep


def esemenyek_bajtokbol(adat):
    if len(adat) < 14 or adat[0:4] != b"MThd":
        raise ErvenytelenFajl("nincs MThd")
    fejlec = be_int(adat, 4, 4)
    if fejlec < 6 or len(adat) < 8 + fejlec:
        raise ErvenytelenFajl("csonka fejlec")
    division = be_int(adat, 12, 2)
    if division & 0x8000:
        raise ErvenytelenFajl("SMPTE")
    if division <= 0:
        raise ErvenytelenFajl("0 felbontas")

    nyers, tempok, leghosszabb = [], [], 0
    offset, track_db = 8 + fejlec, 0
    while offset + 8 <= len(adat):
        hossz = be_int(adat, offset + 4, 4)
        if hossz < 0:
            break
        if adat[offset:offset + 4] != b"MTrk":
            offset += 8 + hossz
            continue
        vege = min(offset + 8 + hossz, len(adat))
        track_nyers = []
        vege_tick = track_esemenyei(adat, offset + 8, vege, track_nyers, tempok)
        if HIBA.get("track_forditott_sorrend"):
            nyers[0:0] = track_nyers
        else:
            nyers.extend(track_nyers)
        leghosszabb = max(leghosszabb, vege_tick)
        track_db += 1
        offset = vege
    if track_db == 0:
        raise ErvenytelenFajl("nincs MTrk")

    if HIBA.get("nem_stabil_rendezes"):
        rendezett = sorted(nyers, key=lambda e: (e[0], e[1], e[2]))
    else:
        rendezett = sorted(nyers, key=lambda e: e[0])
    atvalto = TickAtvalto(tempoterkep(tempok), division)
    esemenyek = [(atvalto.ezredmasodperc(t), s, d1, d2) for t, s, d1, d2 in rendezett]
    return esemenyek, atvalto.ezredmasodperc(leghosszabb)


def szoveges(esemenyek, hossz):
    """A VB tesztfuttatoval azonos szoveges alak."""
    sorok = ["hossz %.3f" % hossz]
    for ido, s, d1, d2 in esemenyek:
        sorok.append("%.3f %d %d %d" % (ido, s, d1, d2))
    return "\n".join(sorok) + "\n"


# ---------------------------------------------------------------------
# Tesztfajlok - kezzel, bajtonkent, hogy a running status es a tobbi
# szelso eset biztosan benne legyen
# ---------------------------------------------------------------------

def vlq_ir(ertek):
    csop = [ertek & 0x7F]
    ertek >>= 7
    while ertek:
        csop.append((ertek & 0x7F) | 0x80)
        ertek >>= 7
    return bytes(reversed(csop))


def mthd(formatum, trackek, tpq):
    return b"MThd" + (6).to_bytes(4, "big") + formatum.to_bytes(2, "big") + \
        trackek.to_bytes(2, "big") + tpq.to_bytes(2, "big")


def mtrk(esemenyek):
    """esemenyek: (delta, nyers bajtok) - a bajtokban a running status kezzel."""
    adat = b"".join(vlq_ir(d) + bytes(b) for d, b in esemenyek) + b"\x00\xFF\x2F\x00"
    return b"MTrk" + len(adat).to_bytes(4, "big") + adat


def tempo(us):
    return [0xFF, 0x51, 0x03, (us >> 16) & 0xFF, (us >> 8) & 0xFF, us & 0xFF]


# Egy ismeretlen chunk, amit a szabvany szerint at kell ugrani. (A mido ezt
# nem tudja - neki az ellenorzeshez nelkule adjuk at a fajlt.)
ISMERETLEN_CHUNK = b"XTRA" + (3).to_bytes(4, "big") + b"abc"


def tesztfajlok():
    f = {}
    # 1) format 1: tempovaltasok a conductorban, ket csatorna-track azonos
    #    idopontokkal, running status, SysEx, ismeretlen chunk.
    conductor = mtrk([(0, [0xFF, 0x03, 4] + list(b"Teszt")[:4]), (0, tempo(500000)),
                      (960, tempo(250000)), (960, tempo(1000000))])
    t1 = mtrk([(0, [0xB0, 0, 8]), (0, [0xB0, 32, 1]), (0, [0xC0, 24]),
               (0, [0x90, 60, 100]), (240, [62, 90]),            # running status
               (240, [60, 0]), (480, [0x80, 62, 0]),
               (960, [0x90, 64, 80]), (960, [0x80, 64, 64]),
               (100, [0xE0, 0, 72]), (50, [0xD0, 40])])
    t2 = mtrk([(0, [0xF0, 5, 0x7E, 0x7F, 0x09, 0x01, 0xF7]),   # GM On SysEx
               (0, [0xC1, 40]), (0, [0x91, 67, 100]),
               (960, [0xFF, 0x01, 2] + list(b"ab")),            # meta a hang kozott
               (0, [0x81, 67, 0]), (0, [0xB1, 64, 127]),
               (1920, [0xB1, 64, 0])])
    f["format1_tempo.mid"] = mthd(1, 3, 480) + conductor + ISMERETLEN_CHUNK + t1 + t2
    # 2) format 0, egyetlen track, tempovaltas KOZBEN, tobb csatorna.
    t0 = mtrk([(0, [0xC9, 0]), (0, [0x99, 36, 110]), (0, [0x90, 48, 70]),
               (96, [0x99, 36, 0]), (0, [42, 90]), (96, tempo(400000)),
               (0, [0x80, 48, 0]), (192, [0x89, 42, 0]), (10, [0xA0, 48, 20])])
    f["format0.mid"] = mthd(0, 1, 96) + t0
    # 3) csonka: a masodik track a kozepen megszakad.
    ep = mtrk([(0, [0x90, 70, 100]), (500, [0x80, 70, 0])])
    csonka = mtrk([(0, [0x90, 72, 100]), (100, [0x80, 72, 0]), (100, [0x90, 74, 100])])
    f["csonka.mid"] = mthd(1, 2, 480) + ep + csonka[:len(csonka) - 7]
    return f


HIBAS_FAJLOK = {
    "smpte.mid": mthd(1, 1, 0xE728) + mtrk([(0, [0x90, 60, 100])]),
    "mtrk_nelkul.mid": mthd(1, 0, 480) + b"XTRA" + (0).to_bytes(4, "big"),
    "nem_midi.mid": b"RIFF0000WAVEfmt ",
}


# ---------------------------------------------------------------------
# Ellenorzes a mido-val
# ---------------------------------------------------------------------

TURES_MS = 0.001


def mido_esemenyek(mido, utvonal):
    mf = mido.MidiFile(utvonal, clip=True)
    ido, ki = 0.0, []
    for m in mf:
        ido += m.time
        if m.is_meta or m.type == "sysex":
            continue
        b = m.bytes()
        s = b[0]
        d1 = b[1] if len(b) > 1 else 0
        d2 = b[2] if len(b) > 2 else 0
        ki.append((ido * 1000.0, s, d1, d2))
    return ki, mf.length * 1000.0


def ellenorzes(mido, konyvtar, reszletes=False):
    def kiir(*a):
        if reszletes:
            print(*a)

    for nev, adat in tesztfajlok().items():
        utvonal = os.path.join(konyvtar, nev)
        with open(utvonal, "wb") as f:
            f.write(adat)
        sajat, hossz = esemenyek_bajtokbol(adat)

        if nev == "csonka.mid":
            # A mido a csonka tracket egeszen eldobja - itt a vart eredmeny kezzel:
            # a teljes 1. track, es a 2. track ep resze (a 74-es hang nelkul).
            hangok = [(round(t, 3), s, d1) for t, s, d1, _ in sajat]
            vart = [(0.0, 0x90, 70), (0.0, 0x90, 72), (104.167, 0x80, 72), (520.833, 0x80, 70)]
            assert hangok == vart, f"csonka fajl: {hangok} != {vart}"
            kiir(f"OK  {nev}: a csonka track ep resze megmaradt")
            continue

        mido_utvonal = os.path.join(konyvtar, "mido_" + nev)
        with open(mido_utvonal, "wb") as f:
            f.write(adat.replace(ISMERETLEN_CHUNK, b""))
        vart, vart_hossz = mido_esemenyek(mido, mido_utvonal)
        assert len(sajat) == len(vart), f"{nev}: {len(sajat)} esemeny, a mido szerint {len(vart)}"
        for i, (a, b) in enumerate(zip(sajat, vart)):
            assert a[1:] == b[1:], f"{nev} #{i}: {a[1:]} != mido {b[1:]} (sorrend vagy tartalom)"
            assert abs(a[0] - b[0]) <= TURES_MS, f"{nev} #{i}: {a[0]:.3f} ms != mido {b[0]:.3f} ms"
        assert abs(hossz - vart_hossz) <= TURES_MS, f"{nev}: hossz {hossz:.3f} != mido {vart_hossz:.3f}"
        kiir(f"OK  {nev}: {len(sajat)} esemeny, idozites es sorrend = mido, hossz {hossz:.1f} ms")

    for nev, adat in HIBAS_FAJLOK.items():
        try:
            esemenyek_bajtokbol(adat)
        except ErvenytelenFajl:
            kiir(f"OK  {nev}: hibakent elutasitva")
        else:
            raise AssertionError(f"{nev}: el kellett volna utasitani")


MUTACIOK = [
    ("nincs_running_status", "a running status nincs kezelve"),
    ("program_change_ket_bajt", "a Program Change / Channel Pressure ket adatbajtot olvas"),
    ("csak_kezdo_tempo", "a tempovaltasok figyelmen kivul maradnak"),
    ("nem_stabil_rendezes", "azonos idopontnal nem a track- es fajlbeli sorrend dont"),
    ("track_forditott_sorrend", "azonos idopontnal a kesobbi track kerul elore"),
]


def mutacios_teszt(mido, konyvtar):
    print()
    print("Mutacios teszt - minden elrontott valtozatnak MEG KELL BUKNIA:")
    atment = []
    for kulcs, leiras in MUTACIOK:
        HIBA.clear()
        HIBA[kulcs] = True
        try:
            ellenorzes(mido, konyvtar)
        except Exception as ex:
            print(f"  OK   elbukott: {leiras}")
            print(f"       ({type(ex).__name__}: {str(ex).splitlines()[0][:70]})")
        else:
            print(f"  HIBA ATMENT: {leiras}")
            atment.append(leiras)
    HIBA.clear()
    assert not atment, "a teszt NEM veszi eszre: " + "; ".join(atment)
    print(f"OK  mind a {len(MUTACIOK)} elrontott valtozat fennakadt")


def main():
    try:
        import mido
    except ImportError:
        print("HIANYZO FUGGOSEG: pip install mido", file=sys.stderr)
        return 1

    if len(sys.argv) == 3 and sys.argv[1] == "--export":
        mappa = sys.argv[2]
        os.makedirs(mappa, exist_ok=True)
        for nev, adat in list(tesztfajlok().items()) + list(HIBAS_FAJLOK.items()):
            with open(os.path.join(mappa, nev), "wb") as f:
                f.write(adat)
            with open(os.path.join(mappa, nev + ".txt"), "w", encoding="utf-8", newline="\n") as f:
                try:
                    f.write(szoveges(*esemenyek_bajtokbol(adat)))
                except ErvenytelenFajl:
                    f.write("HIBA\n")
        print("exportalva:", mappa)
        return 0

    konyvtar = tempfile.mkdtemp()
    HIBA.clear()
    ellenorzes(mido, konyvtar, reszletes=True)
    mutacios_teszt(mido, konyvtar)
    print()
    print("=" * 52)
    print("MINDEN ELLENORZES RENDBEN")
    print("=" * 52)
    return 0


if __name__ == "__main__":
    sys.exit(main())

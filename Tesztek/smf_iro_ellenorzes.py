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
  - MThd fejlec, format 1, 1 + N track (conductor + csatornankent egy)
  - 480 tick / negyed hangjegy felbontas, 120 BPM (500 000 us/negyed)
  - a 0. track: a darab cime, a tempo es az utemjelzes; a hangnem
    (FF 59) viszont CSAK akkor, ha a felhasznalo meg is adta - alapbol
    nem kotelezo (a metronom-korben allithato lesz)
  - csatorna-trackek: csatorna-eloteg (FF 20), tracknev "NN - Hangszernev"
    (NN = az 1-tol indulo CSATORNASZAM ket szamjegyen, NEM a track
    sorszama), hangszernev (FF 04), majd a csatorna indulo beallitasai
    tick 0-n es vegul a csatorna felvett uzenetei
  - a hangszernev feloldasa: bank+program a listabol, es ha az nincs
    benne, visszaeses a puszta programszamra; a 10. csatornan a
    dobkeszlet-listabol
  - a magyar "o" es "u" hosszu ekezetes valtozatanak cserejе a Latin-1
    kodolashoz (a szabvany szerinti szoveg-kodolas nem ismeri oket)
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

MUTACIOS TESZT

A szkript vegen SZANDEKOSAN ELRONTOTT valtozatok futnak le. Mindegyiknek
MEG KELL BUKNIA - ha egy elrontott valtozat is atmegy, akkor a teszt nem
kepes eszrevenni azt a hibafajtat, es bovitendo. Ez bizonyitja, hogy a
fenti allitasok tenylegesen fognak is valamit.

HASZNALAT

    pip install mido
    python3 smf_iro_ellenorzes.py

Sikeres futas eseten a vegen "MINDEN ELLENORZES RENDBEN" felirat all, es
a kilepesi kod 0. Barmely elteres eseten AssertionError szall el.

Keszult: 2026-09-04, a MIDI felvetel 2. fazisanak ellenorzesehez.
Bovitve: 2026-09-06, a csatornankenti trackekhez es a nevekhez.
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

# Hangnem (key signature). None = NINCS megadva, ilyenkor FF 59 sem kerul a
# fajlba - ez az ALAPERTELMEZES (a hangnem megadasa szandekosan nem kotelezo).
# Beallitva: (elojegyzesek szama -7..+7, moll?) - a VB oldalon a felvevo
# HangnemMegadva / HangnemElojegyzes / HangnemMoll property-jei.
HANGNEM = None
VART_HANGNEM = None
MAX_ELOJEGYZES = 7

# A ket vizsgalt eset: hangnem nelkul (ez az alapertelmezes) es egy
# SZANDEKOSAN b-s, MOLL hangnemmel - utobbi azert, mert csak a negativ
# elojegyzes mutatja meg, ha valaki elrontja az elojeles bajt kodolasat.
HANGNEM_ESETEK = [
    (None, None, "hangnem nelkul (alapertelmezes)"),
    ((-2, True), "Gm", "g-moll (2 b, moll)"),
]

DARAB_CIME = "MidiBill3 felvétel"
MAX_NEV_HOSSZ = 120
DOB_CSATORNA_INDEX = 9

META_TRACKNEV = 0x03
META_HANGSZERNEV = 0x04
META_CSATORNA_ELOTAG = 0x20

BANK_SELECT_MSB = 0
BANK_SELECT_LSB = 32

# A szandekosan elrontott valtozatokat ezek a kapcsolok vezerlik. Alaphelyzetben
# MIND hamis - a mutacios teszt egyenkent kapcsolja be oket.
HIBA = {}


def be_int_bajtok(ertek, hossz):
    """Egesz szam big-endian bajtsorozatta (a MIDI fajlformatum vegig big-endian)."""
    return bytes(((ertek >> (8 * (hossz - 1 - i))) & 0xFF) for i in range(hossz))


def vlq(ertek):
    """Variable-length quantity: 7 bites csoportok, az utolso KIVETELEVEL 0x80-nal jelolve."""
    e = 0 if ertek < 0 else ertek
    csoportok = [e & 0x7F]
    maradek = e >> 7
    while maradek > 0:
        jeloles = 0x00 if HIBA.get("vlq_jeloles_nelkul") else 0x80
        csoportok.append((maradek & 0x7F) | jeloles)
        maradek >>= 7
    return bytes(reversed(csoportok))


def adatbajtok_szama(statusz):
    """Program Change (0xC0) es Channel Pressure (0xD0) egybajtos, minden mas ketbajtos."""
    if HIBA.get("program_change_ket_bajt"):
        return 2
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


def szoveges_meta(meta_tipus, szoveg):
    """
    Szoveges meta-esemeny delta 0-val. A Standard MIDI File a szovegeket
    Latin-1 karakterekkent tarolja, amiben NINCS "o" es "u" hosszu ekezetes
    valtozata - ezeket a rovid ekezetesre csereljuk, hogy olvashato
    maradjon (enelkul kerdojel kerulne a helyukre).
    """
    tisztitott = szoveg
    if not HIBA.get("hosszu_ekezet_csere_nelkul"):
        for regi, uj in (("ő", "ö"), ("Ő", "Ö"), ("ű", "ü"), ("Ű", "Ü")):
            tisztitott = tisztitott.replace(regi, uj)
    tisztitott = tisztitott[:MAX_NEV_HOSSZ]

    bajtok = tisztitott.encode("latin-1", errors="replace")
    return bytes([0, 0xFF, meta_tipus]) + vlq(len(bajtok)) + bajtok


# ---------------------------------------------------------------------
# Hangszernev-feloldas (a VB oldalon a HangszerLista modul adja)
# ---------------------------------------------------------------------

HANGSZERLISTA = {
    40: "Hegedű",                  # SZANDEKOSAN hosszu ekezetes: "Hegedü"-ve kell valnia
    48: "String Ensemble 1",
    33: "Electric Bass (finger)",
    80: "Lead 1 (square)",
}

DOBLISTA = {
    0: "Standard Kit",
    8: "Room Kit",
}


def hangszer_nev(kombinalt_kulcs, dob_csatorna):
    lista = DOBLISTA if dob_csatorna else HANGSZERLISTA
    if HIBA.get("dob_lista_helyett_hangszerlista"):
        lista = HANGSZERLISTA
    return lista.get(kombinalt_kulcs, "")


def csatorna_hangszer_neve(csatorna, kezdo_uzenetek, esemenyek):
    """
    A csatorna elso Program Change uzenetebol kiolvasott hangszernev.
    Eloszor a felvetel indulasakor keszult pillanatkepben keresunk, es csak
    ha ott nincs, akkor a felvett anyagban (ez akkor szamit, ha a csatorna
    csak menet kozben valt aktivva).
    """
    def kereses(forras):
        bank_msb = bank_lsb = 0
        for (st, d1, d2) in forras:
            if (st & 0x0F) != csatorna:
                continue
            tipus = st & 0xF0
            if tipus == 0xB0:
                if d1 == BANK_SELECT_MSB:
                    bank_msb = d2
                elif d1 == BANK_SELECT_LSB:
                    bank_lsb = d2
            elif tipus == 0xC0:
                return (bank_lsb if bank_lsb != 0 else bank_msb), d1
        return None, None

    bank, program = kereses([(st, d1, d2) for (st, d1, d2, _n) in kezdo_uzenetek])
    if program is None:
        bank, program = kereses([(st, d1, d2) for (st, d1, d2, _t) in esemenyek])
    if program is None:
        return ""

    dob = (csatorna == DOB_CSATORNA_INDEX)
    nev = hangszer_nev(bank * 128 + program, dob)
    if nev:
        return nev
    if HIBA.get("bank_visszaeses_nelkul"):
        return ""
    return hangszer_nev(program, dob)


def tracknev_szoveg(csatorna, track_sorszam, hangszer_nev_szoveg):
    """A track neve: KETJEGYU CSATORNASZAM (1-tol) - HANGSZERNEV."""
    szam = track_sorszam if HIBA.get("tracknev_track_sorszammal") else (csatorna + 1)
    szoveg = "%02d" % szam
    if not hangszer_nev_szoveg:
        return szoveg
    return szoveg + " - " + hangszer_nev_szoveg


# ---------------------------------------------------------------------
# Track- es fajlepites
# ---------------------------------------------------------------------

def szereplo_csatornak(esemenyek):
    return sorted({st & 0x0F for (st, _d1, _d2, _ido) in esemenyek})


def conductor_track_epitese():
    """A 0. track: a darab cime + tempo + utemjelzes + a kotelezo End of Track."""
    adat = bytearray()
    adat += szoveges_meta(META_TRACKNEV, DARAB_CIME)
    adat += bytes([0, 0xFF, 0x51, 3])
    adat += be_int_bajtok(MIKROSZEC_PER_NEGYED, 3)
    adat += bytes([0, 0xFF, 0x58, 4,
                   UTEM_SZAMLALO, UTEM_NEVEZO_KITEVO,
                   METRONOM_UTES_TICK, HARMINCKETTED_NEGYEDENKENT])
    if HANGNEM is not None or HIBA.get("hangnem_mindig"):
        # Key Signature: az elojegyzes bajtja ELOJELES, a maszkolas a negativ
        # erteket is helyes kettes komplemensu bajtta alakitja.
        elojegyzes, moll = HANGNEM if HANGNEM is not None else (0, False)
        elojegyzes = max(-MAX_ELOJEGYZES, min(MAX_ELOJEGYZES, elojegyzes))
        if HIBA.get("hangnem_elojel_nelkul"):
            elojegyzes = abs(elojegyzes)
        adat += bytes([0, 0xFF, 0x59, 2,
                       elojegyzes & 0xFF, 1 if moll else 0])
    adat += vege_jel()
    return bytes(adat)


def csatorna_track_epitese(csatorna, track_sorszam, esemenyek, elso_idobelyeg_ms, kezdo_uzenetek):
    """
    EGY csatorna sajat trackje: csatorna-eloteg, nevek, a csatorna indulo
    beallitasai tick 0-n, vegul a csatorna felvett uzenetei.
    """
    nev = csatorna_hangszer_neve(csatorna, kezdo_uzenetek, esemenyek)

    adat = bytearray()
    if not HIBA.get("csatorna_eloteg_nelkul"):
        adat += bytes([0, 0xFF, META_CSATORNA_ELOTAG, 1, csatorna])
    adat += szoveges_meta(META_TRACKNEV, tracknev_szoveg(csatorna, track_sorszam, nev))
    if nev:
        adat += szoveges_meta(META_HANGSZERNEV, nev)

    # A csatorna indulo beallitasai a SAJAT trackje elejere, tick 0-ra.
    for (st, d1, d2, adatbajt_szam) in kezdo_uzenetek:
        cel = 0 if HIBA.get("kezdoallapot_mind_az_elso_trackbe") else csatorna
        if (st & 0x0F) != cel:
            continue
        adat.append(0)                    # delta-time = 0
        adat.append(st & 0xFF)
        adat.append(adatbajt(d1))
        if adatbajt_szam == 2:
            adat.append(adatbajt(d2))

    elozo_tick = 0
    elozo_ms = elso_idobelyeg_ms
    for (statusz, d1, d2, idobelyeg_ms) in esemenyek:
        if (statusz & 0x0F) != csatorna:
            continue

        if HIBA.get("deltankenti_kerekites"):
            # Elrontott valtozat: az EGYMAS UTANI esemenyek KULONBSEGET
            # kerekiti tickre, igy minden lepesnel keletkezik egy kis hiba,
            # ami egy suru futamon osszeadodik.
            abszolut_tick = elozo_tick + int(round((idobelyeg_ms - elozo_ms) * TICK_PER_MS))
        else:
            abszolut_tick = int(round((idobelyeg_ms - elso_idobelyeg_ms) * TICK_PER_MS))
        elozo_ms = idobelyeg_ms
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
    """A teljes .mid fajl: MThd + conductor track + csatornankent egy track."""
    elso = esemenyek[0][3]
    csatornak = szereplo_csatornak(esemenyek)

    track_szam = 2 if HIBA.get("regi_ket_trackes_fejlec") else 1 + len(csatornak)

    fajl = bytearray()
    fajl += "MThd".encode("ascii")
    fajl += be_int_bajtok(6, 4)                   # a fejlec adatreszenek hossza mindig 6
    fajl += be_int_bajtok(1, 2)                   # format 1
    fajl += be_int_bajtok(track_szam, 2)
    fajl += be_int_bajtok(TICK_PER_NEGYED, 2)     # felbontas
    fajl += chunk_epitese("MTrk", conductor_track_epitese())
    for sorszam, csatorna in enumerate(csatornak, start=1):
        fajl += chunk_epitese("MTrk",
                              csatorna_track_epitese(csatorna, sorszam, esemenyek, elso, kezdo_uzenetek))
    return bytes(fajl)


# ---------------------------------------------------------------------
# Szimulalt felvetel
# ---------------------------------------------------------------------
# Formatum: (statuszbajt, adat1, adat2, abszolut idobelyeg ms-ban).
# Az idobelyegek SZANDEKOSAN nem 0-tol indulnak: a valosagban is a
# MidiEngine folyamatosan futo Stopwatch-anak tetszoleges pillanatatol
# kezdodik a felvetel, es a felvevo az elso esemenyhez kepest szamol.
#
# A csatornak SZANDEKOSAN negy KULONBOZO esetet fednek le:
#   0. csatorna - a pillanatkepben van, HOSSZU EKEZETES hangszernevvel
#   1. csatorna - a pillanatkepben van, sima nevvel
#   3. csatorna - SEHOL nincs Program Change -> csak szam lesz a tracknev
#   5. csatorna - csak MENET KOZBEN valik aktivva, NEM NULLA bankkal, ami
#                 nincs a listaban -> visszaeses a puszta programszamra
#   9. csatorna - DOB: a nevnek a dobkeszlet-listabol kell jonnie

SZIMULALT_FELVETEL = [
    (0xC0, 40, 0,   10000),   # Program Change - EGY adatbajt!
    (0xB0, 7, 100,  10000),   # CC7 (Main Volume)
    (0x90, 60, 100, 10000),   # C4 be, 1. csatorna
    (0x91, 72, 100, 10000),   # C5 be, 2. csatorna (fan-out: azonos idobelyeg)
    (0x93, 48, 80,  10000),   # 4. csatorna: hang Program Change NELKUL
    (0x99, 36, 110, 10000),   # 10. csatorna (dob): labdob
    (0x90, 60, 0,   10500),   # C4 ki, 0 velocity-s Note On formaban
    (0x91, 72, 0,   10500),
    (0x93, 48, 0,   10500),
    (0x99, 36, 0,   10500),
    (0x90, 62, 90,  10500),
    (0xB5, 32, 8,   12000),   # 6. csatorna: Bank Select LSB = 8 (menet kozbeni aktivalas)
    (0xC5, 80, 0,   12000),   # 6. csatorna: Program Change 80
    (0x95, 55, 95,  12000),
    (0x80, 62, 0,   13456),   # valodi Note Off, "csunya" idopont
    (0x85, 55, 0,   13456),
    # SURU FUTAM a 2. csatornan, 15 ms-onkent. SZANDEKOSAN ilyen a
    # lepeskoz: 15 ms * 0,96 = 14,4 tick, ami minden lepesnel LEFELE
    # kerekedik - ha valaki delta-nkent kerekitene, a hiba lepesenkent
    # 0,4 tickkel nőne, es a futam vegere kilepne a turesbol. Ez a resz
    # bizonyitja, hogy az "eloszor abszolut tick, csak utana delta"
    # sorrend tenylegesen szamit.
    (0x91, 67, 90,  20000),
    (0x81, 67, 0,   20015),
    (0x91, 69, 90,  20030),
    (0x81, 69, 0,   20045),
    (0x91, 71, 90,  20060),
    (0x81, 71, 0,   20075),
    (0x91, 72, 90,  20090),
    (0x81, 72, 0,   20105),
    (0x91, 74, 90,  20120),
    (0x81, 74, 0,   20135),
    (0x91, 76, 90,  20150),
    (0x81, 76, 0,   20165),
    (0x90, 64, 90,  70000),   # nagy szunet -> tobb-bajtos VLQ delta-time
    (0x80, 64, 0,   70250),
]

# A felvetel INDULASAKOR keszult csatorna-allapot pillanatkep.
# Formatum: (statuszbajt, adat1, adat2, adatbajtok szama).
# A 3. csatorna (index 2) SZANDEKOSAN szerepel itt, de a felvetelben NEM
# jatszik egy hangot sem - ellenorizzuk, hogy nem kap tracket.
SZIMULALT_KEZDOALLAPOT = []
for _cs, _program, _hangero in ((0, 40, 100), (1, 48, 90), (2, 33, 80), (9, 0, 110)):
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

# Amit a trackektol varunk: csatorna -> tracknev.
VART_TRACKNEVEK = {
    0: "01 - Hegedü",              # a hosszu "u" rovidre valt a Latin-1 miatt
    1: "02 - String Ensemble 1",
    3: "04",                       # nincs Program Change -> csak a szam
    5: "06 - Lead 1 (square)",     # bank 8 nincs a listaban -> a 80. program neve
    9: "10 - Standard Kit",        # DOB: a dobkeszlet-listabol
}

# Amit a visszaolvasaskor varunk csatornankent: (masodperc, tipus, hang/kontroller).
# A kezdoallapot-uzenetek (tick 0) NEM szerepelnek itt, azokat kulon nezzuk.
VART_UZENETEK = {
    0: [(0.000, "program_change", 40), (0.000, "control_change", 7),
        (0.000, "note_on", 60), (0.500, "note_on", 60), (0.500, "note_on", 62),
        (3.456, "note_off", 62), (60.000, "note_on", 64), (60.250, "note_off", 64)],
    1: [(0.000, "note_on", 72), (0.500, "note_on", 72),
        (10.000, "note_on", 67), (10.015, "note_off", 67),
        (10.030, "note_on", 69), (10.045, "note_off", 69),
        (10.060, "note_on", 71), (10.075, "note_off", 71),
        (10.090, "note_on", 72), (10.105, "note_off", 72),
        (10.120, "note_on", 74), (10.135, "note_off", 74),
        (10.150, "note_on", 76), (10.165, "note_off", 76)],
    3: [(0.000, "note_on", 48), (0.500, "note_on", 48)],
    5: [(2.000, "control_change", 32), (2.000, "program_change", 80),
        (2.000, "note_on", 55), (3.456, "note_off", 55)],
    9: [(0.000, "note_on", 36), (0.500, "note_on", 36)],
}

IDO_TURES_MP = 0.002   # 2 ms: a 0,96 tick/ms atvaltas kerekitesi hataran belul


# ---------------------------------------------------------------------
# Ellenorzes
# ---------------------------------------------------------------------

def hangnem_beallitasa(ertek, vart):
    global HANGNEM, VART_HANGNEM
    HANGNEM, VART_HANGNEM = ertek, vart


def fajl_irasa(mido):
    adat = smf_fajl_epitese(SZIMULALT_FELVETEL, SZIMULALT_KEZDOALLAPOT)
    utvonal = os.path.join(tempfile.mkdtemp(), "midibill_teszt.mid")
    with open(utvonal, "wb") as f:
        f.write(adat)
    return utvonal, adat


def ellenorzes(mido, utvonal, reszletes=False):
    """Minden allitas egy helyen - a mutacios teszt is ezt futtatja."""
    def kiir(*a):
        if reszletes:
            print(*a)

    mf = mido.MidiFile(utvonal)
    csatornak = szereplo_csatornak(SZIMULALT_FELVETEL)

    # --- Fejlec-szintu ellenorzesek ---
    assert mf.type == 1, f"format 1-et vartunk, ez format {mf.type}"
    assert len(mf.tracks) == 1 + len(csatornak), \
        f"{1 + len(csatornak)} tracket vartunk (conductor + {len(csatornak)} csatorna), ez {len(mf.tracks)}"
    assert mf.ticks_per_beat == TICK_PER_NEGYED, \
        f"{TICK_PER_NEGYED} tick/negyedet vartunk, ez {mf.ticks_per_beat}"
    kiir(f"OK  format {mf.type}, {len(mf.tracks)} track, {mf.ticks_per_beat} tick/negyed")

    # --- A 0. (conductor) track ---
    nevek = [m for m in mf.tracks[0] if m.type == "track_name"]
    assert len(nevek) == 1 and nevek[0].name == DARAB_CIME, \
        f'a 0. track neve "{DARAB_CIME}" kell legyen, ez {[n.name for n in nevek]}'

    tempok = [m for m in mf.tracks[0] if m.type == "set_tempo"]
    assert len(tempok) == 1, f"pontosan 1 tempo-esemenyt vartunk, ez {len(tempok)}"
    assert tempok[0].tempo == MIKROSZEC_PER_NEGYED, \
        f"{MIKROSZEC_PER_NEGYED} us/negyedet vartunk, ez {tempok[0].tempo}"

    utemek = [m for m in mf.tracks[0] if m.type == "time_signature"]
    assert len(utemek) == 1, f"pontosan 1 utemjelzest vartunk, ez {len(utemek)}"
    assert utemek[0].numerator == UTEM_SZAMLALO, \
        f"{UTEM_SZAMLALO}-es szamlalot vartunk, ez {utemek[0].numerator}"
    assert utemek[0].denominator == 2 ** UTEM_NEVEZO_KITEVO, \
        f"{2 ** UTEM_NEVEZO_KITEVO}-es nevezot vartunk, ez {utemek[0].denominator}"

    hangnemek = [m for m in mf.tracks[0] if m.type == "key_signature"]
    if HANGNEM is None:
        assert len(hangnemek) == 0, \
            (f"hangnem NINCS megadva, ezert FF 59 sem kerulhet a fajlba - "
             f"megis {[h.key for h in hangnemek]}")
    else:
        assert len(hangnemek) == 1, f"pontosan 1 hangnemet vartunk, ez {len(hangnemek)}"
        assert hangnemek[0].key == VART_HANGNEM, \
            f"{VART_HANGNEM} hangnemet vartunk, ez {hangnemek[0].key}"

    assert not any(hasattr(m, "channel") and not m.is_meta for m in mf.tracks[0]), \
        "a 0. track SZANDEKOSAN csak meta-esemenyeket tartalmaz, csatorna-uzenetet nem"
    kiir(f'OK  0. track: "{DARAB_CIME}", {tempok[0].tempo} us/negyed '
         f"({round(60_000_000 / tempok[0].tempo)} BPM), {utemek[0].numerator}/{utemek[0].denominator}, "
         f"hangnem: {hangnemek[0].key if hangnemek else 'nincs megadva'}")

    # --- Minden MTrk vegen ott a kotelezo End of Track ---
    for i, track in enumerate(mf.tracks):
        assert any(m.type == "end_of_track" for m in track), \
            f"a(z) {i}. track vegerol hianyzik az End of Track"
    kiir(f"OK  mind a {len(mf.tracks)} track vegen ott az End of Track")

    # --- Csatorna-trackek: sorrend, eloteg, nevek ---
    kiir()
    kiir("Csatorna-trackek:")
    for index, csatorna in enumerate(csatornak, start=1):
        track = mf.tracks[index]

        elotegek = [m for m in track if m.type == "channel_prefix"]
        assert len(elotegek) == 1, \
            f"a(z) {index}. trackben pontosan 1 csatorna-eloteget vartunk, ez {len(elotegek)}"
        assert elotegek[0].channel == csatorna, \
            f"a(z) {index}. track eloteget {csatorna}-nak vartuk, ez {elotegek[0].channel}"

        nevek = [m for m in track if m.type == "track_name"]
        assert len(nevek) == 1, f"a(z) {index}. trackben pontosan 1 tracknevet vartunk, ez {len(nevek)}"
        assert nevek[0].name == VART_TRACKNEVEK[csatorna], \
            f'a(z) {index}. track neve "{VART_TRACKNEVEK[csatorna]}" kell legyen, ez "{nevek[0].name}"'

        # A hangszernev (FF 04) csak akkor van kiirva, ha van is nev.
        hangszernevek = [m for m in track if m.type == "instrument_name"]
        vart_hangszernev = VART_TRACKNEVEK[csatorna].split(" - ", 1)
        if len(vart_hangszernev) == 2:
            assert len(hangszernevek) == 1 and hangszernevek[0].name == vart_hangszernev[1], \
                f'a(z) {index}. track hangszerneve "{vart_hangszernev[1]}" kell legyen, ' \
                f"ez {[h.name for h in hangszernevek]}"
        else:
            assert len(hangszernevek) == 0, \
                f"a(z) {index}. trackben nem vartunk hangszernevet, ez {[h.name for h in hangszernevek]}"

        # A trackben CSAK a sajat csatornajanak uzenetei lehetnek.
        idegen = {m.channel for m in track if hasattr(m, "channel") and not m.is_meta} - {csatorna}
        assert not idegen, f"a(z) {index}. trackben idegen csatornak is vannak: {sorted(idegen)}"

        kiir(f'  track {index}: {csatorna}. csatorna, "{nevek[0].name}"')

    # --- A kezdoallapot a SAJAT trackje elejen, tick 0-n ---
    kiir()
    kiir("Kezdoallapot csatornankent (a sajat trackje elejen, tick 0-n):")
    for index, csatorna in enumerate(csatornak, start=1):
        vart = [(st, d1, d2) for (st, d1, d2, _n) in SZIMULALT_KEZDOALLAPOT
                if (st & 0x0F) == csatorna]

        # A track ELSO len(vart) csatorna-uzenete a kezdoallapot - ezek
        # mind tick 0-n allnak. Ami utanuk jon, az mar a felvett anyag (az
        # is kezdodhet tick 0-n, ezert nem az idopont valasztja szet oket).
        tick = 0
        kapott = []
        for m in mf.tracks[index]:
            tick += m.time
            if m.is_meta:
                continue
            if len(kapott) >= len(vart):
                break
            assert tick == 0, \
                f"a {csatorna}. csatorna kezdoallapota tick 0-n kell legyen, ez tick {tick}"
            kapott.append(m)

        assert len(kapott) == len(vart), \
            f"a {csatorna}. csatornan {len(vart)} kezdoallapot-uzenetet vartunk tick 0-n, ez {len(kapott)}"

        for m, (vart_st, vart_d1, vart_d2) in zip(kapott, vart):
            assert m.channel == csatorna, \
                f"kezdoallapot: {csatorna}. csatornat vartunk, ez {m.channel}"
            if m.type == "program_change":
                assert m.program == vart_d1, f"program {vart_d1}-t vartunk, ez {m.program}"
            else:
                assert m.control == vart_d1 and m.value == vart_d2, \
                    f"CC{vart_d1}={vart_d2}-t vartunk, ez CC{m.control}={m.value}"

        kiir(f"  {csatorna}. csatorna: {len(kapott)} uzenet")

    # A NEM jatszo 3. csatorna (index 2) nem kaphatott tracket.
    assert 2 not in csatornak, "a nem jatszo 3. csatornara nem szabad tracknek keszulnie"
    kiir("OK  a nem jatszo 3. csatorna helyesen kimaradt")

    # --- Uzenetek es idozites, csatornankent ---
    idozitett = []
    ido = 0.0
    for msg in mf:
        ido += msg.time
        if not msg.is_meta:
            idozitett.append((ido, msg))

    kiir()
    kiir("Visszaolvasott uzenetek csatornankent:")
    for csatorna in csatornak:
        kezdo_db = len([1 for (st, _d1, _d2, _n) in SZIMULALT_KEZDOALLAPOT if (st & 0x0F) == csatorna])
        sajat = [(i, m) for (i, m) in idozitett if m.channel == csatorna][kezdo_db:]
        vart = VART_UZENETEK[csatorna]

        assert len(sajat) == len(vart), \
            f"a {csatorna}. csatornan {len(vart)} felvett uzenetet vartunk, ez {len(sajat)}"

        for (tenyleges_ido, msg), (vart_ido, vart_tipus, vart_ertek) in zip(sajat, vart):
            assert msg.type == vart_tipus, \
                f"{csatorna}. csatorna: {vart_tipus}-t vartunk, ez {msg.type}"

            tenyleges_ertek = msg.note if hasattr(msg, "note") else \
                (msg.control if hasattr(msg, "control") else msg.program)
            assert tenyleges_ertek == vart_ertek, \
                f"{csatorna}. csatorna / {vart_tipus}: {vart_ertek} erteket vartunk, ez {tenyleges_ertek}"

            elteres = abs(tenyleges_ido - vart_ido)
            assert elteres <= IDO_TURES_MP, \
                f"{csatorna}. csatorna / {vart_tipus} @ {vart_ido}s: " \
                f"{elteres:.4f}s elteres (max {IDO_TURES_MP}s)"

        kiir(f"  {csatorna}. csatorna: {len(sajat)} uzenet, idozites rendben")

    # --- A teljes hossz is stimmel ---
    vart_hossz = (SZIMULALT_FELVETEL[-1][3] - SZIMULALT_FELVETEL[0][3]) / 1000.0
    assert abs(mf.length - vart_hossz) <= IDO_TURES_MP, \
        f"{vart_hossz}s hosszt vartunk, ez {mf.length}s"
    kiir(f"OK  teljes hossz: {mf.length}s")


# ---------------------------------------------------------------------
# Mutacios teszt: minden szandekosan elrontott valtozatnak MEG KELL BUKNIA
# ---------------------------------------------------------------------

MUTACIOK = [
    ("vlq_jeloles_nelkul",
     "a VLQ tobb-bajtos delta-time-nal nem jeloli a folytatast (0x80)"),
    ("program_change_ket_bajt",
     "a Program Change ket adatbajtot kap egy helyett"),
    ("regi_ket_trackes_fejlec",
     "a fejlec a regi, fix 2-es trackszamot irja"),
    ("kezdoallapot_mind_az_elso_trackbe",
     "minden csatorna kezdoallapota az elso csatorna-trackbe kerul"),
    ("tracknev_track_sorszammal",
     "a tracknev a TRACK sorszamat hasznalja a csatornaszam helyett"),
    ("bank_visszaeses_nelkul",
     "a listaban nem szereplo bank nem esik vissza a programszamra"),
    ("dob_lista_helyett_hangszerlista",
     "a 10. csatorna neve a dallamhangszer-listabol jon"),
    ("csatorna_eloteg_nelkul",
     "a csatorna-eloteg (FF 20) kimarad a trackbol"),
    ("hosszu_ekezet_csere_nelkul",
     'a hosszu ekezetes betuk ("o", "u") nem cserelodnek Latin-1-re'),
    ("deltankenti_kerekites",
     "a tick-atvaltas delta-nkent kerekit, igy a hibak osszeadodnak"),
    ("hangnem_mindig",
     "a hangnem (FF 59) akkor is bekerul, ha nincs megadva"),
    ("hangnem_elojel_nelkul",
     "a b-s (negativ) elojegyzes elojel nelkul kerul a fajlba"),
]


def minden_eset(mido, reszletes=False):
    """Mindket hangnem-esetre legyartja es ellenorzi a fajlt."""
    for ertek, vart, leiras in HANGNEM_ESETEK:
        hangnem_beallitasa(ertek, vart)
        if reszletes:
            print()
            print("=" * 52)
            print("ESET:", leiras)
            print("=" * 52)
        utvonal, adat = fajl_irasa(mido)
        if reszletes:
            print("Generalt fajl:", utvonal, f"({len(adat)} bajt)")
        ellenorzes(mido, utvonal, reszletes=reszletes)
    hangnem_beallitasa(None, None)


def mutacios_teszt(mido):
    print()
    print("Mutacios teszt - minden elrontott valtozatnak MEG KELL BUKNIA:")
    elszivargott = []

    for kulcs, leiras in MUTACIOK:
        HIBA.clear()
        HIBA[kulcs] = True
        try:
            minden_eset(mido)
        except Exception as ex:
            rovid = str(ex).split("\n")[0][:70]
            print(f"  OK   elbukott: {leiras}")
            print(f"       ({type(ex).__name__}: {rovid})")
        else:
            print(f"  HIBA ATMENT: {leiras}")
            elszivargott.append(leiras)

    HIBA.clear()

    assert not elszivargott, \
        "a teszt NEM veszi eszre ezeket a hibakat, bovitendo:\n  - " + "\n  - ".join(elszivargott)

    print(f"OK  mind a {len(MUTACIOK)} szandekosan elrontott valtozat fennakadt")


def main():
    try:
        import mido
    except ImportError:
        print("HIANYZO FUGGOSEG: pip install mido", file=sys.stderr)
        return 1

    HIBA.clear()
    minden_eset(mido, reszletes=True)
    mutacios_teszt(mido)

    print()
    print("=" * 52)
    print("MINDEN ELLENORZES RENDBEN")
    print("=" * 52)
    return 0


if __name__ == "__main__":
    sys.exit(main())

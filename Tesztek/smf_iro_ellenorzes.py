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
  - a hangszernev feloldasa: a teljes MSB + LSB + program kulcs a listabol
    (lasd Modellek/Hangszerkulcs.vb), es ha az nincs benne, ket lepcsos
    visszaeses (MSB nelkul, majd a puszta programszam); a 10. csatornan a
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
  2. A felvevo ablakot (MidiFelvevoForm) es a MidiEngine-t - a felvevonek
     itt kozvetlenul adjuk at a "kikuldott" uzeneteket.

AZ ALLAPOTGEP (2026-09-23-tol)

A szkript kulon reszben a felvevo ALLAPOTGEPET is portolja es ellenorzi
(Alap / Felfegyverezve / Rogzit): hogy felfegyverzes utan CSAK az elso
valodi leutes inditja el a felvetelt, hogy a metronom, a visszajatszas
es a rendszer-uzenetek szurese az inditas ELOTT fut, hogy a kezdoallapot-
pillanatkep az INDULAS pillanataban keszul, hogy a felfegyverzes alatt
kikuldott eloadoi vezerlok (modulacio, kitarto pedal) utolso erteke a
pillanatkep UTAN tick 0-ra kerul, es hogy a "logo" hangok lezarodnak.
A program BELSO karbantarto uzenetei (a kimeneti eszkoz valtasakor
kikuldott elnemito soepres es a beallitasok ujrakuldese) nem indithatnak
es nem kerulhetnek a fajlba; felvetel kozben csak az altaluk elnemitott
hangok kapnak Note Off-ot, abban a pillanatban.

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
Bovitve: 2026-09-23, az allapotgeppel (inditas az elso leutesre), es a
         hangszernev-feloldas atallitasa a 2026-09-07-es MSB/LSB kulcsra -
         a port addig meg a regi "amelyik nem nulla, az a bank" szabalyt
         futtatta, es csak azert ment at, mert a tesztadatban nem volt
         MSB-s hangszer.
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

def kulcs(msb, lsb, program):
    """Kombinalt hangszerkulcs - a Modellek/Hangszerkulcs.vb Kulcs() fuggvenye."""
    return msb * 16384 + lsb * 128 + program


HANGSZERLISTA = {
    40: "Hegedű",                  # SZANDEKOSAN hosszu ekezetes: "Hegedü"-ve kell valnia
    41: "Viola",
    48: "String Ensemble 1",
    33: "Electric Bass (finger)",
    80: "Lead 1 (square)",
    24: "Acoustic Guitar (nylon)",
    # MSB-s (SoundFont-bankos) bejegyzes: a regi szabaly ezt nem talalna
    # meg, hanem a 24-es nylon gitarra esne vissza - pontosan az a hiba,
    # amit a felhasznalo 2026-09-07-en hallott.
    kulcs(8, 0, 24): "Ukulele",
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
                return bank_msb, bank_lsb, d1
        return None, None, None

    msb, lsb, program = kereses([(st, d1, d2) for (st, d1, d2, _n) in kezdo_uzenetek])
    if program is None:
        msb, lsb, program = kereses([(st, d1, d2) for (st, d1, d2, _t) in esemenyek])
    if program is None:
        return ""

    dob = (csatorna == DOB_CSATORNA_INDEX)

    if HIBA.get("regi_bank_szabaly"):
        # Elrontott valtozat: a 2026-09-07 elotti "amelyik nem nulla, az a
        # bank" szabaly, a regi bank * 128 + program kulccsal.
        bank = lsb if lsb != 0 else msb
        return hangszer_nev(bank * 128 + program, dob) or hangszer_nev(program, dob)

    # Ugyanaz a KET lepcsos visszaeses, mint a MidiFelvevo.CsatornaHangszerNeve-ben.
    nev = hangszer_nev(kulcs(msb, lsb, program), dob)
    if nev:
        return nev
    if HIBA.get("bank_visszaeses_nelkul"):
        return ""
    if msb != 0 and lsb != 0:
        nev = hangszer_nev(kulcs(0, lsb, program), dob)
        if nev:
            return nev
    return hangszer_nev(kulcs(0, 0, program), dob)


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
# Ez a lista a felvevo altal MAR RÖGZITETT esemenyeket tartalmazza. A
# felvetelt az elso valodi leutes inditja (2026-09-23-tol), ezert az ELSO
# elem mindig Note On - a felvevo ennel korabbi uzenetet nem is rogzit.
#
# A csatornak SZANDEKOSAN a kovetkezo eseteket fedik le:
#   0. csatorna - a pillanatkepben van, HOSSZU EKEZETES hangszernevvel
#   1. csatorna - a pillanatkepben van, sima nevvel
#   3. csatorna - SEHOL nincs Program Change -> csak szam lesz a tracknev
#   5. csatorna - csak MENET KOZBEN valik aktivva, LSB-s bankkal, ami
#                 nincs a listaban -> visszaeses a puszta programszamra
#   6. csatorna - menet kozben, MSB-s (SoundFont-) bankkal, ami BENNE van a
#                 listaban -> a teljes kulcs szerinti nev (Ukulele)
#   9. csatorna - DOB: a nevnek a dobkeszlet-listabol kell jonnie

SZIMULALT_FELVETEL = [
    (0x90, 60, 100, 10000),   # C4 be, 1. csatorna - az ELSO esemeny mindig leutes
    (0xC0, 40, 0,   10000),   # Program Change - EGY adatbajt!
    (0xB0, 7, 100,  10000),   # CC7 (Main Volume)
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
    (0xB6, 0, 8,    12000),   # 7. csatorna: Bank Select MSB = 8 (SoundFont-bank)
    (0xB6, 32, 0,   12000),   #              Bank Select LSB = 0
    (0xC6, 24, 0,   12000),   #              Program Change 24 -> kulcs(8, 0, 24)
    (0x96, 67, 90,  12000),
    (0x80, 62, 0,   13456),   # valodi Note Off, "csunya" idopont
    (0x85, 55, 0,   13456),
    (0x86, 67, 0,   13456),
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
    6: "07 - Ukulele",             # MSB 8: a teljes kulcs benne van a listaban
    9: "10 - Standard Kit",        # DOB: a dobkeszlet-listabol
}

# Amit a visszaolvasaskor varunk csatornankent: (masodperc, tipus, hang/kontroller).
# A kezdoallapot-uzenetek (tick 0) NEM szerepelnek itt, azokat kulon nezzuk.
VART_UZENETEK = {
    0: [(0.000, "note_on", 60), (0.000, "program_change", 40),
        (0.000, "control_change", 7), (0.500, "note_on", 60), (0.500, "note_on", 62),
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
    6: [(2.000, "control_change", 0), (2.000, "control_change", 32),
        (2.000, "program_change", 24), (2.000, "note_on", 67), (3.456, "note_off", 67)],
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
# A felvevo ALLAPOTGEPE (MidiFelvevo.UzenetRogzitese es tarsai), hu portban
# ---------------------------------------------------------------------

ALAP, FELFEGYVEREZVE, ROGZIT = 0, 1, 2

MOD_WHEEL = 1
SUSTAIN_PEDAL = 64

# A MidiFelvevo.eloadoiVezerlok: ezek felfegyverzes alatti utolso erteke a
# pillanatkep UTAN tick 0-ra kerul.
ELOADOI_VEZERLOK = (MOD_WHEEL, SUSTAIN_PEDAL)


def valodi_leutes(statusz, d2):
    """Note On, nullanal NAGYOBB sebesseggel - a MidiFelvevo.ValodiLeutesE."""
    if HIBA.get("indit_nulla_sebessegu_note_on"):
        return (statusz & 0xF0) == 0x90
    return (statusz & 0xF0) == 0x90 and d2 > 0


class Felvevo:
    """A MidiFelvevo rogzito resze: allapotgep, szurok, pillanatkep, lezaras."""

    def __init__(self):
        self.lejatszott_hangok_rogzitese = False
        self.alaphelyzet()

    def alaphelyzet(self):
        self.esemenyek = []
        self.kezdo_uzenetek = []
        self.lekerdezo = None
        self.korai_pillanatkep = []
        self.hang_szol = set()
        self.allapot = ALAP
        self.elso_ms = 0
        self.utolso_ms = 0
        self.vezerlo_ertekek = {}          # (csatorna, vezerlo) -> utolso ertek

    def felfegyverzes(self, lekerdezo=None):
        self.alaphelyzet()
        self.lekerdezo = lekerdezo
        self.allapot = FELFEGYVEREZVE
        if HIBA.get("pillanatkep_felfegyverzeskor") and lekerdezo is not None:
            # Elrontott valtozat: a pillanatkep mar a felfegyverzeskor elkeszul,
            # igy a kozben atallitott hangszin elveszik.
            self.korai_pillanatkep = lekerdezo()

    def _kiszurendo(self, statusz, lejatszasbol, metronombol):
        if metronombol:
            return True
        if lejatszasbol and not self.lejatszott_hangok_rogzitese:
            return True
        return statusz < 0x80 or statusz >= 0xF0

    def uzenet_rogzitese(self, statusz, d1, d2, ido_ms, lejatszasbol=False, metronombol=False, belso=False):
        if self.allapot == ALAP:
            return

        szurok_elol = not HIBA.get("szurok_az_inditas_utan")
        if szurok_elol and self._kiszurendo(statusz, lejatszasbol, metronombol):
            return

        # Belso karbantarto uzenet: se nem indit, se nem rogzul - felvetel
        # kozben csak az altala elnemitott hangokat zarja le.
        if belso and not HIBA.get("belso_uzenet_rogzul"):
            if self.allapot == ROGZIT and not HIBA.get("belso_elnemitas_nem_zar"):
                self._belso_elnemitas(statusz, d1, ido_ms)
            return

        if self.allapot == FELFEGYVEREZVE:
            if not HIBA.get("indit_barmilyen_uzenetre") and not valodi_leutes(statusz, d2):
                self._vezerlo_megjegyzese(statusz, d1, d2)
                return

            self.allapot = ROGZIT
            self.elso_ms = ido_ms

            if HIBA.get("pillanatkep_felfegyverzeskor"):
                pillanatkep = list(self.korai_pillanatkep)
            else:
                pillanatkep = list(self.lekerdezo()) if self.lekerdezo is not None else []
            vezerlok = [] if HIBA.get("eloadoi_vezerlok_kimaradnak") else self._megjegyzett_vezerlok()
            if HIBA.get("vezerlok_a_pillanatkep_ele"):
                self.kezdo_uzenetek += vezerlok + pillanatkep
            else:
                self.kezdo_uzenetek += pillanatkep + vezerlok

        if not szurok_elol and self._kiszurendo(statusz, lejatszasbol, metronombol):
            return

        self.utolso_ms = ido_ms
        self.esemenyek.append((statusz, d1, d2, ido_ms))
        self._hang_nyilvantartas(statusz, d1, d2)

    def _belso_elnemitas(self, statusz, d1, ido_ms):
        """A MidiFelvevo.BelsoElnemitasKezelese: All Sound/Notes Off -> Note Off a szolo hangokra."""
        if (statusz & 0xF0) != 0xB0 or d1 not in (120, 123):
            return
        csatorna = statusz & 0x0F
        for hang in range(128):
            if (csatorna, hang) not in self.hang_szol:
                continue
            self.esemenyek.append((0x80 | csatorna, hang, 0, ido_ms))
            self.hang_szol.discard((csatorna, hang))
            self.utolso_ms = ido_ms

    def _vezerlo_megjegyzese(self, statusz, d1, d2):
        if (statusz & 0xF0) != 0xB0:
            return
        if d1 not in ELOADOI_VEZERLOK and not HIBA.get("minden_vezerlo_megjegyezve"):
            return
        self.vezerlo_ertekek[(statusz & 0x0F, d1)] = d2

    def _megjegyzett_vezerlok(self):
        """Csatornankent, az ELOADOI_VEZERLOK sorrendjeben - a VB oldal sorrendje."""
        ki = []
        for csatorna in range(16):
            if HIBA.get("minden_vezerlo_megjegyezve"):
                sorrend = sorted(v for (c, v) in self.vezerlo_ertekek if c == csatorna)
            else:
                sorrend = ELOADOI_VEZERLOK
            for vezerlo in sorrend:
                if (csatorna, vezerlo) in self.vezerlo_ertekek:
                    ki.append((0xB0 | csatorna, vezerlo, self.vezerlo_ertekek[(csatorna, vezerlo)], 2))
        return ki

    def _hang_nyilvantartas(self, statusz, d1, d2):
        tipus = statusz & 0xF0
        if tipus not in (0x80, 0x90) or not 0 <= d1 <= 127:
            return
        if tipus == 0x90 and d2 > 0:
            self.hang_szol.add((statusz & 0x0F, d1))
        else:
            self.hang_szol.discard((statusz & 0x0F, d1))

    def lezaras(self):
        """A LogoHangokLezarasa + a gyujtott anyag visszaadasa."""
        if self.esemenyek and not HIBA.get("logo_hang_nem_zarodik"):
            for (csatorna, hang) in sorted(self.hang_szol):
                self.esemenyek.append((0x80 | csatorna, hang, 0, self.utolso_ms))
        self.hang_szol.clear()
        return self.esemenyek, self.kezdo_uzenetek


def panel_pillanatkep(panel, alap_hangero=100, reverb_szazalek=20):
    """A CsatornaUzenetek.AktivCsatornakAllapotUzenetei portja (8 uzenet csatornankent)."""
    ki = []
    for csatorna in sorted(panel):
        b = panel[csatorna]
        hangero = int(b["hangero"] * (alap_hangero / 100.0))
        reverb = reverb_szazalek * 127 // 100
        ki += [
            (0xB0 | csatorna, BANK_SELECT_MSB, 0, 2),
            (0xB0 | csatorna, BANK_SELECT_LSB, 0, 2),
            (0xC0 | csatorna, b["program"], 0, 1),
            (0xB0 | csatorna, 7, hangero, 2),
            (0xB0 | csatorna, 10, 64, 2),
            (0xB0 | csatorna, MOD_WHEEL, 0, 2),
            (0xB0 | csatorna, 93, 0, 2),
            (0xB0 | csatorna, 91, reverb, 2),
        ]
    return ki


# A felfegyverzes forgatokonyve: (statusz, adat1, adat2, ido_ms, lejatszasbol,
# metronombol, belso), illetve ("PANEL", csatorna, uj_program) a felulet
# valtozasara. Az 1. es 2. csatorna aktiv; minden, ami az elso leutes ELOTT
# jon, SZANDEKOSAN olyan, ami NEM indithatja el a felvetelt.
N, I = False, True
FELFEGYVERZES_FORGATOKONYV = [
    (0xB0, 7, 90, 1000, N, N, N),     # hangero-csuszka: nem indit, nem jegyzett
    (0xB0, 64, 127, 1200, N, N, N),   # pedal le (1. cs.)
    (0xB1, 64, 127, 1200, N, N, N),   # pedal le (2. cs., fan-out)
    (0xB0, 1, 50, 1300, N, N, N),     # modulacios kerek = 50
    (0xB1, 1, 50, 1300, N, N, N),
    (0xB0, 123, 0, 1400, N, N, I),    # kimenetvaltas, BELSO soepres: All Notes Off
    (0xB0, 120, 0, 1400, N, N, I),    #                               All Sound Off
    (0xB0, 1, 0, 1450, N, N, I),      # BELSO ujrakuldes: a kerek 50-et NEM irhatja felul
    (0x99, 37, 100, 1500, N, I, N),   # metronom-kattanas
    (0x90, 50, 100, 1600, I, N, N),   # visszajatszott hang, overdub KI
    (0x90, 60, 0, 1700, N, N, N),     # 0 sebessegu Note On
    (0xF8, 0, 0, 1750, N, N, N),      # MIDI Clock
    (0xB1, 64, 0, 1800, N, N, N),     # pedal FEL (2. cs.) - az utolso ertek szamit
    ("PANEL", 0, 41),                 # hangszinvaltas a feluleten, meg felfegyverezve
    (0x90, 60, 100, 2000, N, N, N),   # ELSO LEUTES -> indit
    (0x91, 60, 100, 2000, N, N, N),
    (0xB0, 64, 0, 2500, N, N, N),     # pedal fel felvetel kozben -> rogzul
    # Kimenetvaltas FELVETEL KOZBEN: a belso soepres itt zarja a szolo hangokat,
    # az ujrakuldes pedig egy INAKTIV csatornara sem hozhat letre tracket.
    (0xB0, 120, 0, 2800, N, N, I),
    (0xB0, 123, 0, 2800, N, N, I),
    (0xB1, 120, 0, 2800, N, N, I),
    (0xB1, 123, 0, 2800, N, N, I),
    (0xB5, 7, 100, 2800, N, N, I),
    # A jatek folytatodik az uj eszkozon.
    (0x90, 67, 100, 2900, N, N, N),
    (0x91, 67, 100, 2900, N, N, N),
    (0x80, 67, 0, 3000, N, N, N),
    (0x81, 67, 0, 3000, N, N, N),
    (0x90, 64, 100, 3000, N, N, N),   # LOGVA marad -> a felvetel vegen zarodik
]


def felfegyverzes_ellenorzese(mido, reszletes=False):
    """A forgatokonyv lefuttatasa az allapotgep portjan, majd a fajl ellenorzese mido-val."""
    def kiir(*a):
        if reszletes:
            print(*a)

    panel = {0: {"program": 40, "hangero": 100}, 1: {"program": 48, "hangero": 127}}
    felvevo = Felvevo()
    felvevo.felfegyverzes(lambda: panel_pillanatkep(panel))

    for lepes in FELFEGYVERZES_FORGATOKONYV:
        if lepes[0] == "PANEL":
            panel[lepes[1]]["program"] = lepes[2]
            # Ezen a ponton meg MINDEN elotte levo uzenet mellett is felfegyverzett.
            assert felvevo.allapot == FELFEGYVEREZVE and not felvevo.esemenyek, \
                ("az elso leutes ELOTT semmi nem indithatja el a felvetelt - "
                 f"allapot: {felvevo.allapot}, rogzitett: {len(felvevo.esemenyek)}")
            continue
        statusz, d1, d2, ido, lejatszasbol, metronombol, belso = lepes
        felvevo.uzenet_rogzitese(statusz, d1, d2, ido, lejatszasbol, metronombol, belso)

    esemenyek, kezdo = felvevo.lezaras()
    adat = smf_fajl_epitese(esemenyek, kezdo)
    utvonal = os.path.join(tempfile.mkdtemp(), "felfegyverzes_teszt.mid")
    with open(utvonal, "wb") as f:
        f.write(adat)

    mf = mido.MidiFile(utvonal)
    assert len(mf.tracks) == 3, \
        (f"conductor + 2 csatorna-track kell (a belso ujrakuldes egy inaktiv "
         f"csatornara sem hozhat letre tracket), ez {len(mf.tracks)}")

    def tick0_blokk(track):
        tick, ki = 0, []
        for m in track:
            tick += m.time
            if tick == 0 and m.type in ("control_change", "program_change"):
                ki.append(m)
        return ki

    def cc(blokk, szam):
        return [m.value for m in blokk if m.type == "control_change" and m.control == szam]

    nev0 = [m.name for m in mf.tracks[1] if m.type == "track_name"][0]
    assert nev0 == "01 - Viola", \
        f'a pillanatkepnek az INDULASKORI hangszint (41, Viola) kell mutatnia, ez "{nev0}"'

    blokk0, blokk1 = tick0_blokk(mf.tracks[1]), tick0_blokk(mf.tracks[2])
    assert len(blokk0) == 8 + 2, \
        f"az 1. csatornan 8 pillanatkep- + 2 eloadoi-vezerlo-uzenet kell tick 0-n, ez {len(blokk0)}"
    assert cc(blokk0, MOD_WHEEL) == [0, 50], \
        f"elobb a panel modulacioja (0), UTANA a kereke (50) kell: {cc(blokk0, MOD_WHEEL)}"
    assert cc(blokk0, SUSTAIN_PEDAL) == [127], \
        f"a felfegyverzes alatt lenyomott pedalnak tick 0-n kell allnia: {cc(blokk0, SUSTAIN_PEDAL)}"
    assert cc(blokk1, SUSTAIN_PEDAL) == [0], \
        f"a lenyomott, majd felengedett pedal felengedettkent kell: {cc(blokk1, SUSTAIN_PEDAL)}"
    kiir(f"OK  tick 0: pillanatkep + eloadoi vezerlok, 1. cs.: {len(blokk0)} uzenet, "
         f"modulacio {cc(blokk0, MOD_WHEEL)}, pedal {cc(blokk0, SUSTAIN_PEDAL)} / {cc(blokk1, SUSTAIN_PEDAL)}")

    osszes = [m for tr in mf.tracks for m in tr if not m.is_meta]
    assert not [m for m in osszes if m.type == "control_change" and m.control in (120, 123)], \
        "a belso (eszkozvaltasi) soepres nem kerulhet a fajlba"
    assert not [m for m in osszes if m.type == "control_change" and m.control == 7 and m.value == 90], \
        "a felfegyverzes alatti csuszkamozgatas nem kerulhet a fajlba"
    assert not [m for m in osszes if getattr(m, "note", None) in (37, 50)], \
        "a metronom es a visszajatszott hang nem kerulhet a fajlba"

    ido, idozitett = 0.0, []
    for m in mf:
        ido += m.time
        if not m.is_meta:
            idozitett.append((ido, m))
    elso_leutes = [t for t, m in idozitett if m.type == "note_on" and m.velocity > 0][0]
    assert abs(elso_leutes) <= IDO_TURES_MP, \
        f"a felvetelnek az elso leutessel kell indulnia, de az a {elso_leutes:.3f}. masodpercben van"
    pedal_fel = [t for t, m in idozitett
                 if m.type == "control_change" and m.control == SUSTAIN_PEDAL and m.channel == 0 and t > 0]
    assert pedal_fel and abs(pedal_fel[0] - 0.5) <= IDO_TURES_MP, \
        f"a felvetel kozbeni pedalfelengedes 0,5 mp-nel kell: {pedal_fel}"

    for csatorna in (0, 1):
        zaras = [t for t, m in idozitett if m.channel == csatorna and getattr(m, "note", None) == 60
                 and (m.type == "note_off" or (m.type == "note_on" and m.velocity == 0))]
        assert zaras and abs(zaras[0] - 0.8) <= IDO_TURES_MP, \
            (f"a {csatorna + 1}. csatorna hangjanak a belso elnemitas pillanataban (0,8 mp) "
             f"kell veget ernie: {zaras}")

    szol = set()
    for _t, m in idozitett:
        if m.type == "note_on" and m.velocity > 0:
            szol.add((m.channel, m.note))
        elif m.type == "note_off" or (m.type == "note_on" and m.velocity == 0):
            szol.discard((m.channel, m.note))
    assert not szol, f"logo hang maradt a fajlban: {sorted(szol)}"
    kiir("OK  a felvetel az elso leutessel indult; soepres, csuszka, metronom, "
         "visszajatszas kimaradt; logo hang nincs")
    return adat


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
    ("regi_bank_szabaly",
     'a hangszernev a regi "amelyik nem nulla, az a bank" szaballyal oldodik fel'),
    ("indit_barmilyen_uzenetre",
     "a felfegyverzett felvevot barmilyen kikuldott uzenet elinditja"),
    ("indit_nulla_sebessegu_note_on",
     "a 0 sebessegu Note On (valojaban hangkikapcsolas) is elinditja"),
    ("szurok_az_inditas_utan",
     "a metronom/visszajatszas szurese az inditas UTAN fut, igy azok is inditanak"),
    ("pillanatkep_felfegyverzeskor",
     "a kezdoallapot-pillanatkep a felfegyverzeskor keszul, nem az indulaskor"),
    ("eloadoi_vezerlok_kimaradnak",
     "a felfegyverzes alatt lenyomott pedal / modulacio kimarad a fajl elejerol"),
    ("vezerlok_a_pillanatkep_ele",
     "az eloadoi vezerlok a pillanatkep ELE kerulnek, igy a panel erteke felulirja oket"),
    ("minden_vezerlo_megjegyezve",
     "a felfegyverzes alatt MINDEN vezerlo megjegyzodik (soepres, csuszka is)"),
    ("logo_hang_nem_zarodik",
     "a felvetel vegen lenyomva maradt hang nem kap Note Off-ot"),
    ("belso_uzenet_rogzul",
     "a belso karbantarto uzenetek (eszkozvaltas) normal uzenetkent rogzulnek"),
    ("belso_elnemitas_nem_zar",
     "a felvetel kozbeni belso elnemitas nem zarja le a szolo hangokat"),
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

    if reszletes:
        print()
        print("=" * 52)
        print("ALLAPOTGEP: inditas az elso leutesre")
        print("=" * 52)
    felfegyverzes_ellenorzese(mido, reszletes=reszletes)


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

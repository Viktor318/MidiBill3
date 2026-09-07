#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
MidiBill3 - a kozos MIDI-beolvaso (MidiFajlOlvaso.vb) ellenorzese
==================================================================

MIT ELLENORIZ EZ A SZKRIPT?

A `Modellek/MidiFajlOlvaso.vb` SMF-ertelmezo logikajanak HU PYTHON-PORTJAT
futtatja le olyan teszt-MIDI-fajlokon, amelyeket egy FUGGETLEN
MIDI-konyvtar (mido) allit elo, majd osszeveti a ket eredmenyt:
amit a portunk kiolvas, egyeznie kell azzal, amit a mido lat.

Igy derul ki, hogy helyes-e a bajt-szintu ertelmezes:
  - format 0 (egy kozos track) ES format 1 (tobb track) fajlok
  - running status (elhagyott statuszbajt ismetlodo uzeneteknel)
  - meta-esemenyek (tempo, utemjelzes, hangnem) es SysEx atugrasa,
    valamint hogy ezek ERVENYTELENITIK a running statust
  - Bank Select (MSB/LSB) + Program Change osszeparositasa, mindket
    konvencioval - a variacio a GS-nel az MSB-ben, az XG-nel es a GM2-nel
    az LSB-ben van, ezert MINDKET bajtot meg kell orizni
  - a vezerlok ELSO ertekenek megorzese
  - a hangok szamlalasa (a 0 sebessegu Note On NEM megszolalo hang)
  - a teljes hossz szamitasa tempovaltasokkal egyutt

MIT NEM ELLENORIZ?

Magat a VB kodot: ez egy PORT, nem a VB futtatasa. Ha a
`MidiFajlOlvaso.vb` ertelmezo resze valtozik, EZT A SZKRIPTET IS
FRISSITENI KELL, kulonben mar nem ugyanazt a logikat vizsgalja.

HASZNALAT

    pip install mido
    python3 midi_olvaso_ellenorzes.py

Sikeres futas eseten a vegen "MINDEN ELLENORZES RENDBEN" felirat all, es
a kilepesi kod 0. Barmely elteres eseten AssertionError szall el.

Keszult: 2026-09-05, a ket uj segedprogram (Preset keszitese MIDI fajlbol,
MIDI fajl beallitasainak mentese) beolvaso reszehez.
"""

import os
import sys
import tempfile

BANK_SELECT_MSB = 0
BANK_SELECT_LSB = 32

# A hangszerkulcs osszefuzese - a Modellek/Hangszerkulcs.vb portja.
# Reszletesen a Tesztek/hangszerkulcs_ellenorzes.py vizsgalja.
LSB_SULY = 128
MSB_SULY = 16384


def hangszerkulcs(bank_msb, bank_lsb, program):
    return bank_msb * MSB_SULY + bank_lsb * LSB_SULY + program
ALAP_TEMPO_MIKROSZEK = 500000

META_TEMPO = 0x51
META_UTEMJELZES = 0x58
META_HANGNEM = 0x59

META_SZOVEG = 0x01
META_SZERZOI_JOG = 0x02
META_TRACKNEV = 0x03
META_HANGSZERNEV = 0x04
META_JELOLO = 0x06
META_PROGRAMNEV = 0x08
META_ESZKOZNEV = 0x09
META_CSATORNA_ELOTAG = 0x20

MAX_SZOVEG_HOSSZ = 300


# =====================================================================
# A MidiFajlOlvaso.vb ertelmezo logikaja, hu portban
# =====================================================================

class CsatornaAdat:
    def __init__(self):
        self.van_adat = False
        self.hangok_szama = 0
        self.legmelyebb_hang = -1
        self.legmagasabb_hang = -1
        self.hangszervaltasok = []
        self.kezdo_vezerlok = {}
        self.fajlbeli_nev = ""
        self.fajlbeli_nev_forrasa = ""


class FajlAdat:
    def __init__(self):
        self.formatum = 0
        self.track_szam = 0
        self.tick_per_negyed = 0
        self.smpte = False
        self.kezdo_tempo_mikroszek = 0
        self.tempo_valtasok_szama = 0
        self.van_utemjelzes = False
        self.utem_szamlalo = 4
        self.utem_nevezo = 4
        self.utemjelzes_valtasok_szama = 0
        self.van_hangnem = False
        self.hangnem_elojegyzes = 0
        self.hangnem_moll = False
        self.teljes_hossz_masodperc = 0.0
        self.cim = ""
        self.szerzoi_jog = ""
        self.eszkoz_nev = ""
        self.altalanos_szoveg = ""
        self.szabvany = ""
        self.jelolok = []          # (ido_masodperc, szoveg)
        self.csatornak = [CsatornaAdat() for _ in range(16)]


class TrackAllapot:
    def __init__(self):
        self.tracknev = ""
        self.hangszernev = ""
        self.programnev = ""
        self.hasznalt_csatornak = set()
        self.csatorna_elotag = -1
        self.vege_tick = 0


def olvas_be_int32(adat, offset):
    if offset + 4 > len(adat):
        return -1
    return (adat[offset] << 24) | (adat[offset + 1] << 16) | (adat[offset + 2] << 8) | adat[offset + 3]


def olvas_be_int16(adat, offset):
    if offset + 2 > len(adat):
        return 0
    return (adat[offset] << 8) | adat[offset + 1]


def chunk_azonosito_egyezik(adat, offset, azonosito):
    if offset + 4 > len(adat):
        return False
    return adat[offset:offset + 4] == azonosito.encode("ascii")


def vlq_olvasas(adat, pos, hatar):
    """Visszaadja: (ertek, uj_pozicio)."""
    p = pos
    v = 0
    bajtok = 0
    while p < hatar:
        b = adat[p]
        p += 1
        bajtok += 1
        v = (v << 7) | (b & 0x7F)
        if (b & 0x80) == 0:
            break
        if bajtok >= 4:
            break
    return v, p


def szoveg_olvasas(adat, pos, hossz):
    """A VB SzovegOlvasas hu portja: Latin-1, vezerlokarakterek szurve, levagva."""
    if hossz <= 0:
        return ""
    nyers = adat[pos:pos + hossz].decode("latin-1")
    ki = []
    for c in nyers:
        if ord(c) < 32 or ord(c) == 127:
            if ki and ki[-1] != " ":
                ki.append(" ")
        else:
            ki.append(c)
    eredmeny = "".join(ki).strip()
    if len(eredmeny) > MAX_SZOVEG_HOSSZ:
        eredmeny = eredmeny[:MAX_SZOVEG_HOSSZ] + "..."
    return eredmeny


def szabvany_felismerese(adat, pos, hossz):
    """A VB SzabvanyFelismerese hu portja."""
    if hossz < 4:
        return ""
    veg = pos + hossz
    elso = adat[pos]

    if elso == 0x7E:
        for i in range(pos, veg - 1):
            if adat[i] == 0x09:
                if adat[i + 1] == 0x01:
                    return "General MIDI"
                if adat[i + 1] == 0x03:
                    return "General MIDI 2"

    elif elso == 0x41:
        for i in range(pos, veg - 4):
            if (adat[i] == 0x42 and adat[i + 1] == 0x12 and adat[i + 2] == 0x40
                    and adat[i + 3] == 0x00 and adat[i + 4] == 0x7F):
                return "Roland GS"
        return "Roland (egyedi SysEx)"

    elif elso == 0x43:
        for i in range(pos, veg - 3):
            if (adat[i] == 0x4C and adat[i + 1] == 0x00
                    and adat[i + 2] == 0x00 and adat[i + 3] == 0x7E):
                return "Yamaha XG"
        return "Yamaha (egyedi SysEx)"

    return ""


def tracknevek_tarsitasa(eredmeny, ta, track_index):
    """A VB TracknevekTarsitasa hu portja."""
    cim_track = (track_index == 0)
    if cim_track and not eredmeny.cim:
        eredmeny.cim = ta.tracknev

    if not ta.hasznalt_csatornak:
        return

    cel = -1
    if ta.csatorna_elotag >= 0:
        cel = ta.csatorna_elotag
    elif len(ta.hasznalt_csatornak) == 1:
        cel = next(iter(ta.hasznalt_csatornak))

    if cel < 0:
        return

    cs = eredmeny.csatornak[cel]
    if cs.fajlbeli_nev:
        return

    if ta.hangszernev:
        cs.fajlbeli_nev, cs.fajlbeli_nev_forrasa = ta.hangszernev, "hangszernev"
    elif ta.programnev:
        cs.fajlbeli_nev, cs.fajlbeli_nev_forrasa = ta.programnev, "programnev"
    elif ta.tracknev and not cim_track:
        cs.fajlbeli_nev, cs.fajlbeli_nev_forrasa = ta.tracknev, "tracknev"


def meta_esemeny_feldolgozasa(adat, pos, hossz, meta_tipus, abszolut_tick, eredmeny, tempo_valtasok,
                              jelolok=None, ta=None):
    if meta_tipus == META_TEMPO:
        if hossz < 3:
            return
        mikroszek = (adat[pos] << 16) | (adat[pos + 1] << 8) | adat[pos + 2]
        if mikroszek <= 0:
            return
        tempo_valtasok.append((abszolut_tick, mikroszek))

    elif meta_tipus == META_UTEMJELZES:
        if hossz < 2:
            return
        eredmeny.utemjelzes_valtasok_szama += 1
        if eredmeny.van_utemjelzes:
            return
        eredmeny.van_utemjelzes = True
        eredmeny.utem_szamlalo = adat[pos]
        kitevo = adat[pos + 1]
        if kitevo > 10:
            kitevo = 10
        eredmeny.utem_nevezo = 2 ** kitevo

    elif meta_tipus == META_HANGNEM:
        if hossz < 2 or eredmeny.van_hangnem:
            return
        eredmeny.van_hangnem = True
        elojegyzes = adat[pos]
        if elojegyzes > 127:
            elojegyzes -= 256
        eredmeny.hangnem_elojegyzes = elojegyzes
        eredmeny.hangnem_moll = (adat[pos + 1] != 0)

    elif meta_tipus == META_TRACKNEV:
        if ta is not None and not ta.tracknev:
            ta.tracknev = szoveg_olvasas(adat, pos, hossz)

    elif meta_tipus == META_HANGSZERNEV:
        if ta is not None and not ta.hangszernev:
            ta.hangszernev = szoveg_olvasas(adat, pos, hossz)

    elif meta_tipus == META_PROGRAMNEV:
        if ta is not None and not ta.programnev:
            ta.programnev = szoveg_olvasas(adat, pos, hossz)

    elif meta_tipus == META_SZERZOI_JOG:
        if not eredmeny.szerzoi_jog:
            eredmeny.szerzoi_jog = szoveg_olvasas(adat, pos, hossz)

    elif meta_tipus == META_ESZKOZNEV:
        if not eredmeny.eszkoz_nev:
            eredmeny.eszkoz_nev = szoveg_olvasas(adat, pos, hossz)

    elif meta_tipus == META_SZOVEG:
        if not eredmeny.altalanos_szoveg:
            eredmeny.altalanos_szoveg = szoveg_olvasas(adat, pos, hossz)

    elif meta_tipus == META_JELOLO:
        szoveg = szoveg_olvasas(adat, pos, hossz)
        if szoveg and jelolok is not None:
            jelolok.append((abszolut_tick, szoveg))

    elif meta_tipus == META_CSATORNA_ELOTAG:
        if hossz >= 1 and ta is not None and 0 <= adat[pos] <= 15:
            ta.csatorna_elotag = adat[pos]


def csatorna_uzenet_feldolgozasa(cs, csatorna, uzenet_tipus, adat1, adat2, bank_msb, bank_lsb):
    cs.van_adat = True

    if uzenet_tipus == 0x90:
        if adat2 > 0:
            cs.hangok_szama += 1
            if cs.legmelyebb_hang < 0 or adat1 < cs.legmelyebb_hang:
                cs.legmelyebb_hang = adat1
            if adat1 > cs.legmagasabb_hang:
                cs.legmagasabb_hang = adat1

    elif uzenet_tipus == 0xB0:
        if adat1 == BANK_SELECT_MSB:
            bank_msb[csatorna] = adat2
        elif adat1 == BANK_SELECT_LSB:
            bank_lsb[csatorna] = adat2
        if adat1 not in cs.kezdo_vezerlok:
            cs.kezdo_vezerlok[adat1] = adat2

    elif uzenet_tipus == 0xC0:
        # MINDKET bank-bajt megorzodik, ahogy a fajlban all. Korabban
        # "amelyik nem nulla, az a bank" szabaly volt - az egy GS fajlnal
        # (MSB = variacio, LSB = hangkeszlet-terkep) rossz hangot adott.
        cs.hangszervaltasok.append(
            hangszerkulcs(bank_msb[csatorna], bank_lsb[csatorna], adat1))


def track_feldolgozasa(adat, kezdet, vege, eredmeny, tempo_valtasok, jelolok=None, ta=None):
    pos = kezdet
    abszolut_tick = 0
    elozo_statusz = 0
    bank_msb = [0] * 16
    bank_lsb = [0] * 16

    while pos < vege:
        delta, pos = vlq_olvasas(adat, pos, vege)
        if pos >= vege:
            break
        abszolut_tick += delta

        elso_bajt = adat[pos]
        if elso_bajt >= 0x80:
            statusz = elso_bajt
            pos += 1
        else:
            statusz = elozo_statusz
            if statusz == 0:
                break

        if statusz == 0xFF:
            if pos >= vege:
                break
            meta_tipus = adat[pos]
            pos += 1
            hossz, pos = vlq_olvasas(adat, pos, vege)
            if hossz < 0 or pos + hossz > vege:
                break
            meta_esemeny_feldolgozasa(adat, pos, hossz, meta_tipus, abszolut_tick, eredmeny,
                                      tempo_valtasok, jelolok, ta)
            pos += hossz
            elozo_statusz = 0

        elif statusz in (0xF0, 0xF7):
            hossz, pos = vlq_olvasas(adat, pos, vege)
            if hossz < 0 or pos + hossz > vege:
                break
            if not eredmeny.szabvany:
                eredmeny.szabvany = szabvany_felismerese(adat, pos, hossz)
            pos += hossz
            elozo_statusz = 0

        elif 0x80 <= statusz < 0xF0:
            elozo_statusz = statusz
            csatorna = statusz & 0x0F
            uzenet_tipus = statusz & 0xF0
            adatbajtok = 1 if uzenet_tipus in (0xC0, 0xD0) else 2
            if pos + adatbajtok > vege:
                break
            adat1 = adat[pos]
            adat2 = adat[pos + 1] if adatbajtok == 2 else 0
            pos += adatbajtok
            if ta is not None:
                ta.hasznalt_csatornak.add(csatorna)
            csatorna_uzenet_feldolgozasa(eredmeny.csatornak[csatorna], csatorna,
                                         uzenet_tipus, adat1, adat2, bank_msb, bank_lsb)
        else:
            break

    return abszolut_tick


def hossz_szamitasa(vege_tick, tempo_valtasok, eredmeny):
    if vege_tick <= 0 or eredmeny.smpte or eredmeny.tick_per_negyed <= 0:
        return 0.0

    terkep = sorted(tempo_valtasok, key=lambda tv: tv[0])
    if not terkep or terkep[0][0] > 0:
        terkep.insert(0, (0, ALAP_TEMPO_MIKROSZEK))

    osszes_mikroszek = 0.0
    for i, (szakasz_kezdet, mikroszek) in enumerate(terkep):
        if szakasz_kezdet >= vege_tick:
            break
        szakasz_vege = vege_tick
        if i < len(terkep) - 1 and terkep[i + 1][0] < vege_tick:
            szakasz_vege = terkep[i + 1][0]
        tickek = szakasz_vege - szakasz_kezdet
        osszes_mikroszek += tickek * (mikroszek / float(eredmeny.tick_per_negyed))

    return osszes_mikroszek / 1000000.0


def beolvas(fajl_utvonal):
    with open(fajl_utvonal, "rb") as f:
        adat = f.read()

    if len(adat) < 14 or not chunk_azonosito_egyezik(adat, 0, "MThd"):
        raise ValueError("Nem ervenyes Standard MIDI File")

    eredmeny = FajlAdat()
    header_hossz = olvas_be_int32(adat, 4)
    eredmeny.formatum = olvas_be_int16(adat, 8)
    eredmeny.track_szam = olvas_be_int16(adat, 10)

    division = olvas_be_int16(adat, 12)
    if (division & 0x8000) == 0:
        eredmeny.tick_per_negyed = division
    else:
        eredmeny.smpte = True

    if header_hossz < 6 or len(adat) < 8 + header_hossz:
        raise ValueError("Hibas fejlec")

    tempo_valtasok = []
    jelolok = []
    leghosszabb_tick = 0
    offset = 8 + header_hossz
    feldolgozott = 0

    while offset + 8 <= len(adat):
        if not chunk_azonosito_egyezik(adat, offset, "MTrk"):
            ismeretlen = olvas_be_int32(adat, offset + 4)
            if ismeretlen < 0:
                break
            offset += 8 + ismeretlen
            continue

        track_hossz = olvas_be_int32(adat, offset + 4)
        if track_hossz < 0:
            break
        track_vege = min(offset + 8 + track_hossz, len(adat))

        ta = TrackAllapot()
        vege_tick = track_feldolgozasa(adat, offset + 8, track_vege, eredmeny, tempo_valtasok, jelolok, ta)
        tracknevek_tarsitasa(eredmeny, ta, feldolgozott)
        leghosszabb_tick = max(leghosszabb_tick, vege_tick)

        feldolgozott += 1
        offset = track_vege

    if feldolgozott == 0:
        raise ValueError("Nincs ervenyes track")

    eredmeny.tempo_valtasok_szama = len(tempo_valtasok)
    if tempo_valtasok:
        eredmeny.kezdo_tempo_mikroszek = min(tempo_valtasok, key=lambda tv: tv[0])[1]

    eredmeny.teljes_hossz_masodperc = hossz_szamitasa(leghosszabb_tick, tempo_valtasok, eredmeny)

    for tick, szoveg in sorted(jelolok, key=lambda j: j[0]):
        eredmeny.jelolok.append((hossz_szamitasa(tick, tempo_valtasok, eredmeny), szoveg))

    return eredmeny


# =====================================================================
# Teszt-fajlok eloallitasa a FUGGETLEN mido konyvtarral
# =====================================================================

def teszt_fajl_format1(utvonal):
    """Format 1: kulon tempo-track + 2 hangszeres csatorna + dob, tempovaltassal."""
    import mido
    mf = mido.MidiFile(type=1, ticks_per_beat=480)

    meta = mido.MidiTrack()
    meta.append(mido.MetaMessage("track_name", name="Teszt darab cime", time=0))
    meta.append(mido.MetaMessage("copyright", text="(c) 2026 Teszt Szerzo", time=0))
    meta.append(mido.MetaMessage("device_name", name="Roland SC-88", time=0))
    meta.append(mido.MetaMessage("text", text="Ez egy megjegyzes a fajlban", time=0))
    # GS Reset SysEx: ebbol kell felismerni, hogy GS fajlrol van szo
    meta.append(mido.Message("sysex", data=[0x41, 0x10, 0x42, 0x12, 0x40, 0x00, 0x7F, 0x00, 0x41], time=0))
    meta.append(mido.MetaMessage("marker", text="Intro", time=0))
    # A szovegben SZANDEKOSAN van vezerlokarakter (soremeles) - a beolvasonak
    # ki kell szurnie, kulonben elcsuszna tole a jelentes elrendezese.
    meta.append(mido.MetaMessage("marker", text="Refren\nmasodik sor", time=960))
    meta.append(mido.MetaMessage("set_tempo", tempo=500000, time=0))          # 120 BPM
    meta.append(mido.MetaMessage("time_signature", numerator=3, denominator=4, time=0))
    meta.append(mido.MetaMessage("key_signature", key="F", time=0))           # 1 b, dur
    meta.append(mido.MetaMessage("set_tempo", tempo=400000, time=960))        # 150 BPM a fajl KOZEPETOL
    # (szandekosan a kozepen: ha a vegen lenne, a hossz-szamitas hibaja nem latszana)
    mf.tracks.append(meta)

    # 1. csatorna: XG-stilusu bank (a variacio az LSB-ben: MSB=0, LSB=2) + program 48
    t1 = mido.MidiTrack()
    t1.append(mido.MetaMessage("track_name", name="Vonosok", time=0))
    t1.append(mido.Message("control_change", channel=0, control=0, value=0, time=0))
    t1.append(mido.Message("control_change", channel=0, control=32, value=2, time=0))
    t1.append(mido.Message("program_change", channel=0, program=48, time=0))
    t1.append(mido.Message("control_change", channel=0, control=7, value=100, time=0))
    t1.append(mido.Message("control_change", channel=0, control=10, value=32, time=0))
    t1.append(mido.Message("control_change", channel=0, control=91, value=40, time=0))
    t1.append(mido.Message("control_change", channel=0, control=64, value=127, time=0))  # sustain -> "egyeb"
    t1.append(mido.Message("note_on", channel=0, note=60, velocity=90, time=0))
    t1.append(mido.Message("note_on", channel=0, note=60, velocity=0, time=480))   # 0 velocity = NEM uj hang
    t1.append(mido.Message("note_on", channel=0, note=72, velocity=90, time=0))
    t1.append(mido.Message("note_off", channel=0, note=72, velocity=0, time=480))
    t1.append(mido.Message("control_change", channel=0, control=7, value=60, time=0))  # kesobbi CC7 - NEM ez szamit
    t1.append(mido.Message("program_change", channel=0, program=49, time=0))           # 2. hangszervaltas
    t1.append(mido.Message("note_on", channel=0, note=48, velocity=80, time=0))
    t1.append(mido.Message("note_off", channel=0, note=48, velocity=0, time=960))
    mf.tracks.append(t1)

    # 2. csatorna: GS-stilusu bank (a variacio az MSB-ben: MSB=8, LSB=0)
    t2 = mido.MidiTrack()
    t2.append(mido.MetaMessage("track_name", name="2. sav", time=0))
    t2.append(mido.MetaMessage("instrument_name", name="Tenor Sax", time=0))
    t2.append(mido.Message("control_change", channel=1, control=0, value=8, time=0))
    t2.append(mido.Message("program_change", channel=1, program=25, time=0))
    t2.append(mido.Message("control_change", channel=1, control=91, value=90, time=0))  # a LEGNAGYOBB reverb
    t2.append(mido.Message("note_on", channel=1, note=64, velocity=70, time=0))
    t2.append(mido.Message("note_off", channel=1, note=64, velocity=0, time=1920))
    mf.tracks.append(t2)

    # 5. csatorna: MINDKET bank-bajt nem nulla (MSB=1, LSB=3). Csak ez a
    # helyzet kulonbozteti meg a ket konvenciot - a sajat szabalyunk szerint
    # ilyenkor az LSB nyer (a program is igy ir).
    t5 = mido.MidiTrack()
    t5.append(mido.Message("control_change", channel=4, control=0, value=1, time=0))
    t5.append(mido.Message("control_change", channel=4, control=32, value=3, time=0))
    t5.append(mido.Message("program_change", channel=4, program=10, time=0))
    t5.append(mido.Message("note_on", channel=4, note=55, velocity=64, time=0))
    t5.append(mido.Message("note_off", channel=4, note=55, velocity=0, time=240))
    mf.tracks.append(t5)

    # TOBB csatornat hasznalo, NEVESITETT track: a nev ilyenkor ketertelmu
    # lenne, ezert egyik csatornahoz sem szabad tarsitani.
    t67 = mido.MidiTrack()
    t67.append(mido.MetaMessage("track_name", name="Ketertelmu sav", time=0))
    t67.append(mido.Message("note_on", channel=6, note=50, velocity=64, time=0))
    t67.append(mido.Message("note_off", channel=6, note=50, velocity=0, time=120))
    t67.append(mido.Message("note_on", channel=7, note=52, velocity=64, time=0))
    t67.append(mido.Message("note_off", channel=7, note=52, velocity=0, time=120))
    mf.tracks.append(t67)

    # FF 20 (MIDI Channel Prefix): a track KET csatornat hasznal, tehat a
    # darabszam-alapu szabaly nem dontene - a csatorna-elotag viszont
    # kifejezetten megmondja, melyik csatornarol szol a nev.
    t_elotag = mido.MidiTrack()
    t_elotag.append(mido.MetaMessage("channel_prefix", channel=8, time=0))
    t_elotag.append(mido.MetaMessage("track_name", name="Elotaggal jelolt", time=0))
    t_elotag.append(mido.Message("note_on", channel=8, note=55, velocity=64, time=0))
    t_elotag.append(mido.Message("note_off", channel=8, note=55, velocity=0, time=120))
    t_elotag.append(mido.Message("note_on", channel=11, note=57, velocity=64, time=0))
    t_elotag.append(mido.Message("note_off", channel=11, note=57, velocity=0, time=120))
    mf.tracks.append(t_elotag)

    # 10. csatorna (index 9): csak vezerlo, EGYETLEN hang sem
    t3 = mido.MidiTrack()
    t3.append(mido.Message("control_change", channel=9, control=7, value=110, time=0))
    mf.tracks.append(t3)

    mf.save(utvonal)
    return mf


def teszt_fajl_format0(utvonal):
    """Format 0: minden egyetlen trackben, SysEx-szel es running statusszal."""
    import mido
    mf = mido.MidiFile(type=0, ticks_per_beat=96)
    tr = mido.MidiTrack()
    # Format 0-nal az EGYETLEN track neve a darab cime - es mivel ugyanez a
    # track tartalmazza a csatorna-adatokat is, itt derul ki, ha a cim tevesen
    # csatornanevkent is megjelenne.
    tr.append(mido.MetaMessage("track_name", name="Format 0 darab cime", time=0))
    tr.append(mido.MetaMessage("set_tempo", tempo=600000, time=0))            # 100 BPM
    tr.append(mido.Message("sysex", data=[0x41, 0x10, 0x42], time=0))          # SysEx: running statust torol
    tr.append(mido.Message("program_change", channel=3, program=11, time=0))
    tr.append(mido.Message("control_change", channel=3, control=1, value=20, time=0))
    # Ismetlodo azonos tipusu uzenetek -> a mido running statust hasznal a mentesnel
    for i in range(8):
        tr.append(mido.Message("note_on", channel=3, note=60 + i, velocity=64, time=0 if i == 0 else 48))
        tr.append(mido.Message("note_on", channel=3, note=60 + i, velocity=0, time=48))
    mf.tracks.append(tr)
    mf.save(utvonal)
    return mf


# =====================================================================
# Ellenorzes
# =====================================================================

def mido_referencia(mf):
    """Amit a FUGGETLEN konyvtar lat - ehhez hasonlitjuk a sajat portunkat."""
    hangok = {}
    programok = {}
    for tr in mf.tracks:
        for msg in tr:
            if msg.type == "note_on" and msg.velocity > 0:
                hangok[msg.channel] = hangok.get(msg.channel, 0) + 1
            elif msg.type == "program_change":
                programok.setdefault(msg.channel, []).append(msg.program)
    return hangok, programok


def ellenoriz_format1(utvonal, mf):
    import mido
    e = beolvas(utvonal)
    ref_hangok, ref_programok = mido_referencia(mf)

    print("  format 1 fajl:")
    assert e.formatum == 1, f"format 1-et vartunk, ez {e.formatum}"
    assert e.track_szam == len(mf.tracks), f"{len(mf.tracks)} tracket vartunk, ez {e.track_szam}"
    assert e.tick_per_negyed == 480, f"480 tick/negyedet vartunk, ez {e.tick_per_negyed}"
    print(f"    OK  format {e.formatum}, {e.track_szam} track, {e.tick_per_negyed} tick/negyed")

    assert e.kezdo_tempo_mikroszek == 500000, f"500000-t vartunk, ez {e.kezdo_tempo_mikroszek}"
    assert e.tempo_valtasok_szama == 2, f"2 tempovaltast vartunk, ez {e.tempo_valtasok_szama}"
    print(f"    OK  kezdo tempo {e.kezdo_tempo_mikroszek} us/negyed, {e.tempo_valtasok_szama} tempovaltas")

    assert e.van_utemjelzes and (e.utem_szamlalo, e.utem_nevezo) == (3, 4), \
        f"3/4-et vartunk, ez {e.utem_szamlalo}/{e.utem_nevezo}"
    assert e.van_hangnem and e.hangnem_elojegyzes == -1 and not e.hangnem_moll, \
        f"F-dur (-1, dur) vartunk, ez ({e.hangnem_elojegyzes}, moll={e.hangnem_moll})"
    print(f"    OK  utemjelzes {e.utem_szamlalo}/{e.utem_nevezo}, hangnem elojegyzes {e.hangnem_elojegyzes}")

    # 1. csatorna - XG-stilusu bank (LSB=2) + program 48/49
    cs1 = e.csatornak[0]
    vart1 = [hangszerkulcs(0, 2, 48), hangszerkulcs(0, 2, 49)]
    assert cs1.hangszervaltasok == vart1, \
        f"{vart1}-t vartunk, ez {cs1.hangszervaltasok}"
    assert cs1.kezdo_vezerlok[7] == 100, f"CC7 kezdoerteke 100 kell legyen, ez {cs1.kezdo_vezerlok[7]}"
    assert cs1.kezdo_vezerlok[10] == 32
    assert cs1.kezdo_vezerlok[91] == 40
    assert cs1.kezdo_vezerlok[64] == 127
    assert cs1.hangok_szama == ref_hangok[0], \
        f"a mido {ref_hangok[0]} hangot lat az 1. csatornan, mi {cs1.hangok_szama}-t"
    assert (cs1.legmelyebb_hang, cs1.legmagasabb_hang) == (48, 72)
    print(f"    OK  1. csatorna: XG-stilusu bank (LSB) -> hangszinek {cs1.hangszervaltasok}, "
          f"CC7 kezdoerteke {cs1.kezdo_vezerlok[7]} (a kesobbi 60 helyesen figyelmen kivul), "
          f"{cs1.hangok_szama} hang, {cs1.legmelyebb_hang}-{cs1.legmagasabb_hang}")

    # 2. csatorna - GS-stilusu bank (MSB=8) + program 25. A REGI szabaly
    # szerint ez 8*128+25 = 1049 lett volna, vagyis UGYANAZ a kulcs, mint
    # egy XG-fajl "LSB=8" bankja - pedig a ketto MAS hangszer.
    cs2 = e.csatornak[1]
    vart2 = [hangszerkulcs(8, 0, 25)]
    assert cs2.hangszervaltasok == vart2, f"{vart2}-t vartunk, ez {cs2.hangszervaltasok}"
    assert cs2.hangszervaltasok != [8 * 128 + 25], \
        ("az MSB-s bank NEM eshet egybe az LSB-s bankkal - ha igen, a "
         "beolvaso megint osszevonja a ket konvenciot")
    assert cs2.kezdo_vezerlok[91] == 90
    print(f"    OK  2. csatorna: GS-stilusu bank (MSB) -> hangszin {cs2.hangszervaltasok}, reverb {cs2.kezdo_vezerlok[91]}")

    # --- A fajlba irt szoveges informaciok ---
    assert e.cim == "Teszt darab cime", f"a cimet vartuk, ez {e.cim!r}"
    assert e.szerzoi_jog == "(c) 2026 Teszt Szerzo", f"szerzoi jog: {e.szerzoi_jog!r}"
    assert e.eszkoz_nev == "Roland SC-88", f"eszkoznev: {e.eszkoz_nev!r}"
    assert e.altalanos_szoveg == "Ez egy megjegyzes a fajlban", f"szoveg: {e.altalanos_szoveg!r}"
    print(f"    OK  cim / szerzoi jog / eszkoznev / megjegyzes mind kiolvasva")

    # --- Szabvany-felismeres a SysEx-bol ---
    assert e.szabvany == "Roland GS", f"'Roland GS'-t vartunk a GS Reset alapjan, ez {e.szabvany!r}"
    print(f"    OK  szabvany a SysEx-bol: {e.szabvany}")

    # --- Jelolok, idorendben, helyes idoponttal ---
    assert len(e.jelolok) == 2, f"2 jelolot vartunk, ez {len(e.jelolok)}"
    assert e.jelolok[0][1] == "Intro" and abs(e.jelolok[0][0]) < 0.01, f"1. jelolo: {e.jelolok[0]}"
    # A 2. jelolo a 960. ticken van; 480 tick/negyed es 120 BPM mellett ez 1,0 s
    assert e.jelolok[1][1] == "Refren masodik sor", \
        (f"a vezerlokaraktert (soremeles) szokozre kellett volna cserelni: {e.jelolok[1][1]!r}")
    assert abs(e.jelolok[1][0] - 1.0) < 0.01, f"2. jelolo ideje: {e.jelolok[1]}"
    print(f"    OK  {len(e.jelolok)} jelolo idorendben: "
          f"{e.jelolok[0][1]} @ {e.jelolok[0][0]:.2f}s, {e.jelolok[1][1]} @ {e.jelolok[1][0]:.2f}s")

    # --- Tracknev -> csatorna tarsitas ---
    assert cs1.fajlbeli_nev == "Vonosok" and cs1.fajlbeli_nev_forrasa == "tracknev", \
        f"1. csatorna neve 'Vonosok' (tracknev) kellene, ez {cs1.fajlbeli_nev!r} ({cs1.fajlbeli_nev_forrasa})"
    # A 2. csatorna trackjeben tracknev ES hangszernev is van - a hangszernevnek kell nyernie
    assert cs2.fajlbeli_nev == "Tenor Sax" and cs2.fajlbeli_nev_forrasa == "hangszernev", \
        (f"2. csatorna: a hangszernevnek ('Tenor Sax') kell nyernie a tracknev ('2. sav') felett, "
         f"ez {cs2.fajlbeli_nev!r} ({cs2.fajlbeli_nev_forrasa})")
    print(f"    OK  nevek: 1. csatorna {cs1.fajlbeli_nev!r} ({cs1.fajlbeli_nev_forrasa}), "
          f"2. csatorna {cs2.fajlbeli_nev!r} ({cs2.fajlbeli_nev_forrasa}) - a hangszernev nyert")

    # A tobbcsatornas track neve NEM tarsithato egyertelmuen
    for cs_index in (6, 7):
        assert e.csatornak[cs_index].fajlbeli_nev == "", \
            (f"a {cs_index + 1}. csatorna nevet kapott egy TOBB csatornat hasznalo tracktol "
             f"({e.csatornak[cs_index].fajlbeli_nev!r}) - ez ketertelmu lenne")
    print("    OK  a tobbcsatornas track neve helyesen NEM tarsitodott egyik csatornahoz sem")

    # FF 20: a csatorna-elotag donti el a nevet, nem a csatornak szama
    assert e.csatornak[8].fajlbeli_nev == "Elotaggal jelolt", \
        (f"a 9. csatornanak (index 8) kellett volna megkapnia a nevet az FF 20 elotag alapjan, "
         f"ez {e.csatornak[8].fajlbeli_nev!r}")
    assert e.csatornak[11].fajlbeli_nev == "", \
        f"a 12. csatorna nem kaphatta volna meg a nevet, ez {e.csatornak[11].fajlbeli_nev!r}"
    print("    OK  az FF 20 csatorna-elotag helyesen dontotte el, melyik csatornahoz tartozik a nev")

    # A cim NEM szivarog at csatornanevkent
    for cs_index in range(16):
        assert e.csatornak[cs_index].fajlbeli_nev != "Teszt darab cime", \
            f"a darab cime tevesen a(z) {cs_index + 1}. csatorna nevekent jelent meg"
    print("    OK  a darab cime nem szivargott at csatornanevkent")

    # 5. csatorna - MINDKET bank-bajt nem nulla: MINDKETTO megorzodik.
    # Ez a legelesebb eset: a regi, osszevono szabaly itt dobta el az egyik
    # bajtot, es epp ez a Roland GS tipikus alakja (MSB = variacio,
    # LSB = hangkeszlet-terkep).
    cs5 = e.csatornak[4]
    vart5 = [hangszerkulcs(1, 3, 10)]
    assert cs5.hangszervaltasok == vart5, \
        (f"MSB=1 es LSB=3 mellett MINDKETTONEK meg kell orzodnie -> {vart5}, "
         f"ez {cs5.hangszervaltasok}")
    print(f"    OK  5. csatorna: MSB=1 ES LSB=3 -> mindketto megorizve, hangszin {cs5.hangszervaltasok}")

    # 10. csatorna - van adat, de NINCS hang
    cs10 = e.csatornak[9]
    assert cs10.van_adat and cs10.hangok_szama == 0, \
        f"a 10. csatornan adatot, de hangot NEM vartunk (van_adat={cs10.van_adat}, hang={cs10.hangok_szama})"
    print(f"    OK  10. csatorna: van adat, de 0 hang (a preset szurojenek ki kell hagynia)")

    # Minden csatorna hangszamlaloja egyezik a mido-eval
    for cs_index in range(16):
        varhato = ref_hangok.get(cs_index, 0)
        assert e.csatornak[cs_index].hangok_szama == varhato, \
            f"{cs_index + 1}. csatorna: a mido {varhato} hangot lat, mi {e.csatornak[cs_index].hangok_szama}-t"
    print("    OK  a hangszamlalok mind a 16 csatornan egyeznek a mido-eval")

    # Teljes hossz: a mido sajat szamitasaval osszevetve
    assert abs(e.teljes_hossz_masodperc - mf.length) < 0.01, \
        f"a mido {mf.length:.3f}s hosszt lat, mi {e.teljes_hossz_masodperc:.3f}s-t"
    print(f"    OK  teljes hossz {e.teljes_hossz_masodperc:.3f}s (mido: {mf.length:.3f}s, tempovaltassal egyutt)")


def ellenoriz_format0(utvonal, mf):
    e = beolvas(utvonal)
    ref_hangok, ref_programok = mido_referencia(mf)

    print("  format 0 fajl (SysEx + running status):")
    assert e.formatum == 0, f"format 0-t vartunk, ez {e.formatum}"
    assert e.track_szam == 1
    assert e.tick_per_negyed == 96
    assert e.kezdo_tempo_mikroszek == 600000
    print(f"    OK  format {e.formatum}, {e.track_szam} track, {e.tick_per_negyed} tick/negyed, "
          f"tempo {e.kezdo_tempo_mikroszek} us/negyed")

    cs4 = e.csatornak[3]
    assert cs4.hangszervaltasok == [11], f"[11]-et vartunk (bank 0), ez {cs4.hangszervaltasok}"
    assert cs4.kezdo_vezerlok[1] == 20
    assert cs4.hangok_szama == ref_hangok[3] == 8, \
        f"8 hangot vartunk, mi {cs4.hangok_szama}-t, a mido {ref_hangok.get(3)}-t lat"
    assert (cs4.legmelyebb_hang, cs4.legmagasabb_hang) == (60, 67)
    print(f"    OK  4. csatorna: hangszin {cs4.hangszervaltasok}, {cs4.hangok_szama} hang "
          f"({cs4.legmelyebb_hang}-{cs4.legmagasabb_hang}) - a SysEx es a running status helyesen kezelve")

    assert e.cim == "Format 0 darab cime", f"a format 0 track neve a cim kell legyen, ez {e.cim!r}"
    assert cs4.fajlbeli_nev == "", \
        (f"format 0-nal a track neve a darab CIME, nem a csatornae - megis "
         f"{cs4.fajlbeli_nev!r} lett a 4. csatorna neve")
    print(f"    OK  a track neve a cim lett ({e.cim!r}), es NEM szivargott at csatornanevkent")

    assert abs(e.teljes_hossz_masodperc - mf.length) < 0.01, \
        f"a mido {mf.length:.3f}s hosszt lat, mi {e.teljes_hossz_masodperc:.3f}s-t"
    print(f"    OK  teljes hossz {e.teljes_hossz_masodperc:.3f}s (mido: {mf.length:.3f}s)")


def teszt_running_status_meta_utan():
    """
    KEZZEL osszerakott, SZANDEKOSAN szabalytalan fajl: egy meta-esemeny UTAN
    running status-szal (statuszbajt nelkul) folytatodik egy hang.

    Az SMF szabvany szerint a meta- es SysEx-esemenyek ERVENYTELENITIK a
    running statust, tehat ez a fajl hibas. A helyes viselkedes: a meta ELOTTI
    hangot meg beolvassuk, utana viszont nem talalgatunk, hanem abbahagyjuk a
    track feldolgozasat - NEM talalunk ki nem letezo hangokat.

    Ezt mido-val nem lehet eloallitani (az mindig szabalyos fajlt ir), ezert
    kell kezzel osszerakni a bajtokat.
    """
    track = bytearray()
    track += bytes([0x00, 0x90, 60, 100])          # delta 0: Note On, explicit statusszal
    track += bytes([0x60, 0xFF, 0x01, 0x03]) + b"ABC"   # delta 96: szoveges meta-esemeny
    track += bytes([0x00, 62, 100])                # delta 0: "running status" - SZABALYTALAN
    track += bytes([0x00, 0xFF, 0x2F, 0x00])       # End of Track

    fajl = bytearray()
    fajl += b"MThd" + bytes([0, 0, 0, 6, 0, 0, 0, 1, 0x01, 0xE0])   # format 0, 1 track, 480 tpq
    fajl += b"MTrk" + len(track).to_bytes(4, "big") + track

    utvonal = os.path.join(tempfile.mkdtemp(), "running_status_meta.mid")
    with open(utvonal, "wb") as f:
        f.write(bytes(fajl))

    print("  Kezzel osszerakott fajl (running status meta-esemeny utan):")
    e = beolvas(utvonal)
    cs = e.csatornak[0]

    assert cs.hangok_szama == 1, \
        (f"pontosan 1 hangot vartunk (a meta ELOTTIT); ha 2 jon ki, akkor a meta "
         f"utan tevesen tovabbvittuk a running statust - ez {cs.hangok_szama}")
    assert cs.legmelyebb_hang == 60 and cs.legmagasabb_hang == 60, \
        f"csak a 60-as hangot vartuk, ez {cs.legmelyebb_hang}-{cs.legmagasabb_hang}"
    print(f"    OK  {cs.hangok_szama} hang - a meta-esemeny helyesen ervenytelenitette "
          f"a running statust, es a szabalytalan folytatas nem lett kitalalt hang")


def teszt_csonka_fajl(ep_utvonal):
    """
    CSONKA fajl: a track fejlecben jelzett hossz tulnyulik a fajl vegen (pl.
    felbeszakadt masolas, serult letoltes). A helyes viselkedes: NEM szabad
    kivetellel elszallni - ameddig van adat, addig olvassuk be, a tobbit
    elhagyjuk. Ez azert fontos, mert a segedprogramok kotegben dolgoznak: egy
    serult fajl miatt nem allhat le a tobbi feldolgozasa.
    """
    with open(ep_utvonal, "rb") as f:
        ep = f.read()

    csonka_utvonal = os.path.join(tempfile.mkdtemp(), "csonka.mid")
    with open(csonka_utvonal, "wb") as f:
        f.write(ep[:-40])            # az utolso 40 bajt hianyzik, a hossz-mezo valtozatlan

    print("  Csonka (serult) fajl:")
    e = beolvas(csonka_utvonal)      # ha ez kivetelt dob, a teszt elszall
    cs = e.csatornak[3]
    assert cs.hangok_szama > 0, "a fajl elejerol beolvasott hangokat vartunk"
    print(f"    OK  nem szallt el, {cs.hangok_szama} hangot olvasott be a megmaradt reszbol")


def main():
    try:
        import mido  # noqa: F401
    except ImportError:
        print("HIANYZO FUGGOSEG: pip install mido", file=sys.stderr)
        return 1

    mappa = tempfile.mkdtemp()
    u1 = os.path.join(mappa, "teszt_format1.mid")
    u0 = os.path.join(mappa, "teszt_format0.mid")

    mf1 = teszt_fajl_format1(u1)
    mf0 = teszt_fajl_format0(u0)

    print(f"Teszt-fajlok: {mappa}")
    print()

    ellenoriz_format1(u1, mf1)
    print()
    ellenoriz_format0(u0, mf0)

    print()
    teszt_running_status_meta_utan()

    print()
    teszt_csonka_fajl(u0)

    print()
    print("=" * 52)
    print("MINDEN ELLENORZES RENDBEN")
    print("=" * 52)
    return 0


if __name__ == "__main__":
    sys.exit(main())

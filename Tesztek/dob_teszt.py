#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
DIAGNOSZTIKA: hogyan lehet dobkeszletet valtani a 10. csatornan?

MIERT KELL? A MidiBill3 a dobcsatornan (10.) a dobkeszlet-lista (.drm)
kulcsat kuldi ki, ami SZANDEKOSAN bank nelkuli programszam - vagyis a
program ezt a harmast kuldi:

    CC0  (Bank Select MSB) = 0
    CC32 (Bank Select LSB) = 0
    Program Change         = <kit szam>

Ha a lejatszoban emellett NEM valtozik a dobkeszlet, akkor vagy a
Program Change onmagaban nem eleg, vagy epp a kikuldott CC0 = 0 allitja
rossz bankra a csatornat. Ez a szkript egy olyan .mid fajlt general,
amivel EGYETLEN meghallgatasbol eldontheto, melyik eset all fenn.

A fajl kilenc szakaszbol all, mindegyik elott mas bank/program
kombinacioval, de UGYANAZZAL a dobmintaval. Minden szakasz elejen egy
MIDI jelolo (FF 06) is all a szakasz nevevel - igy a MidiBill3 sajat
"MIDI fajl beallitasainak mentese olvashato formaban" segedprogramja is
kilistazza oket idobelyeggel egyutt.

HASZNALAT

    python3 dob_teszt.py

Ezutan a keletkezett dob_teszt.mid fajlt le kell jatszani azzal a
lejatszoval / SoundFonttal, amit a MidiBill3 is hasznal, es megjegyezni,
MELYIK SZAKASZOKBAN valtozott a hangzas.

ERTELMEZES

  - Ha a 2. es 3. szakasz mar mas kitet szolaltat meg, mint az 1., de a
    4. es 5. (ahol CC0 = 0 is megy) visszater az alapra:
        -> a kikuldott CC0 = 0 a hibas; a dobcsatornan nem szabad
           Bank Select-et kuldeni.
  - Ha sem a 2-3., sem a 4-5. nem valtozik, de a 6. (CC0 = 127) vagy a
    7. (CC0 = 120) igen:
        -> a SoundFont dobkeszletei abban a bankban vannak, es a
           .drm kulcsba bele kell tenni a bankot (nem szabad levagni).
  - Ha a 8. (CC32 = 25) valtozik:
        -> a lejatszo a dobcsatornan az LSB-t nezi.
  - Ha EGYIK szakasz sem valtozik:
        -> a dobkeszletek nem erhetok el Program Change-dzsel ebben a
           SoundFontban / lejatszo-beallitasban.

Keszult: 2026-09-15, a "dobkeszlet-valtas nem hallatszik" bejelentesre.
"""

import os

TICK_PER_NEGYED = 480
MIKROSZEC_PER_NEGYED = 500000          # 120 BPM
DOB_CSATORNA = 9                       # 0-tol szamitva = 10. csatorna

CC_BANK_MSB = 0
CC_BANK_LSB = 32

# A dobminta hangjai (GM dobkiosztas)
KICK = 36
SNARE = 38
HIHAT = 42
CRASH = 49

# (nev, bank_msb vagy None, bank_lsb vagy None, program)
# A None azt jelenti: azt a Bank Select uzenetet EL SEM kuldjuk.
SZAKASZOK = [
    ("1. PC=0 - nincs bank (alap allapot)",                None, None, 0),
    ("2. PC=25 - nincs bank (TR-808?)",                    None, None, 25),
    ("3. PC=48 - nincs bank (Orchestral?)",                None, None, 48),
    ("4. CC0=0 CC32=0 PC=25 - EZT KULDI A MIDIBILL",          0,    0, 25),
    ("5. CC0=0 CC32=0 PC=48 - EZT KULDI A MIDIBILL",          0,    0, 48),
    ("6. CC0=127 CC32=0 PC=25",                             127,    0, 25),
    ("7. CC0=120 CC32=0 PC=25",                             120,    0, 25),
    ("8. CC0=0 CC32=25 PC=0 - LSB-s valtozat",                0,   25, 0),
    ("9. CC0=0 CC32=0 PC=0 - vissza az alapra",               0,    0, 0),
]


def vlq(ertek):
    """Variable-length quantity kodolas (a MIDI delta-time formatuma)."""
    if ertek < 0:
        ertek = 0
    csoportok = [ertek & 0x7F]
    ertek >>= 7
    while ertek > 0:
        csoportok.append((ertek & 0x7F) | 0x80)
        ertek >>= 7
    return bytes(reversed(csoportok))


def be_int(ertek, hossz):
    """Big-endian egesz szam (a MIDI fajlformatum vegig big-endian)."""
    return bytes((ertek >> (8 * (hossz - 1 - i))) & 0xFF for i in range(hossz))


def chunk(azonosito, adat):
    return azonosito.encode("ascii") + be_int(len(adat), 4) + adat


def szoveges_meta(delta, tipus, szoveg):
    # A szabvany Latin-1 karaktereket var; a szkript szandekosan
    # ekezet nelkuli szakasznevekkel dolgozik.
    bajtok = szoveg.encode("latin-1", errors="replace")
    return vlq(delta) + bytes([0xFF, tipus]) + vlq(len(bajtok)) + bajtok


def conductor_track():
    adat = b""
    adat += szoveges_meta(0, 0x03, "MidiBill3 - dobkeszlet-valtas teszt")
    # Set Tempo
    adat += vlq(0) + bytes([0xFF, 0x51, 3]) + be_int(MIKROSZEC_PER_NEGYED, 3)
    # Time Signature 4/4
    adat += vlq(0) + bytes([0xFF, 0x58, 4]) + bytes([4, 2, 24, 8])
    adat += vlq(0) + bytes([0xFF, 0x2F, 0])
    return adat


def dob_track():
    """A dobcsatorna trackje: szakaszonkent jelolo + bank/program + egy utem minta."""
    adat = b""
    adat += szoveges_meta(0, 0x03, "%02d - Dobteszt" % (DOB_CSATORNA + 1))
    adat += vlq(0) + bytes([0xFF, 0x20, 1, DOB_CSATORNA])

    statusz_cc = 0xB0 | DOB_CSATORNA
    statusz_pc = 0xC0 | DOB_CSATORNA
    statusz_on = 0x90 | DOB_CSATORNA

    negyed = TICK_PER_NEGYED
    # Egy szakasz: 1 utem minta + 1 utem csend
    minta = [
        (0 * negyed, [KICK, CRASH]),
        (1 * negyed, [SNARE]),
        (2 * negyed, [KICK]),
        (3 * negyed, [SNARE, HIHAT]),
    ]
    hang_hossz = negyed // 4
    szakasz_hossz = 8 * negyed

    # (abszolut_tick, esemeny_bajtok) parok, a vegen rendezve delta-va alakitva
    esemenyek = []

    for i, (nev, msb, lsb, program) in enumerate(SZAKASZOK):
        alap = i * szakasz_hossz

        # Jelolo (FF 06) - a MidiBill3 riportja kilistazza
        bajtok = nev.encode("latin-1", errors="replace")
        esemenyek.append((alap, bytes([0xFF, 0x06]) + vlq(len(bajtok)) + bajtok))

        # Bank Select - csak ha a szakasz keri
        if msb is not None:
            esemenyek.append((alap, bytes([statusz_cc, CC_BANK_MSB, msb])))
        if lsb is not None:
            esemenyek.append((alap, bytes([statusz_cc, CC_BANK_LSB, lsb])))

        # Program Change
        esemenyek.append((alap, bytes([statusz_pc, program])))

        # A dobminta
        for eltolas, hangok in minta:
            for hang in hangok:
                esemenyek.append((alap + eltolas, bytes([statusz_on, hang, 100])))
                esemenyek.append((alap + eltolas + hang_hossz, bytes([statusz_on, hang, 0])))

    # Stabil rendezes tick szerint (az azonos tickuek megtartjak a sorrendjuket,
    # tehat a bank/program mindig a hangok ELE kerul)
    esemenyek.sort(key=lambda e: e[0])

    elozo = 0
    for tick, bajtok in esemenyek:
        adat += vlq(tick - elozo) + bajtok
        elozo = tick

    adat += vlq(negyed) + bytes([0xFF, 0x2F, 0])
    return adat


def main():
    fajl = b"MThd" + be_int(6, 4) + be_int(1, 2) + be_int(2, 2) + be_int(TICK_PER_NEGYED, 2)
    fajl += chunk("MTrk", conductor_track())
    fajl += chunk("MTrk", dob_track())

    kimenet = os.path.join(os.path.dirname(os.path.abspath(__file__)), "dob_teszt.mid")
    with open(kimenet, "wb") as f:
        f.write(fajl)

    print("Elkeszult: %s (%d bajt)" % (kimenet, len(fajl)))
    print()
    print("Szakaszok (egyenkent 4 masodperc 120 BPM mellett):")
    for i, (nev, msb, lsb, program) in enumerate(SZAKASZOK):
        print("  %5.1f mp  %s" % (i * 4.0, nev))


if __name__ == "__main__":
    main()

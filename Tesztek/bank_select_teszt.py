#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
MidiBill3 - Bank Select diagnosztikai MIDI fajl eloallitasa
============================================================

MIRE VALO?

Eldonteni, hogy a te lejatszod (VirtualMIDISynth / BASSMIDI, FluidSynth,
hardveres modul...) a Bank Select melyik bajtjat veszi figyelembe:
  - a CC0-t (MSB)  -> Roland GS stilus
  - a CC32-t (LSB) -> Yamaha XG stilus; a MidiBill3 JELENLEG ezt kuldi
  - egyiket sem

A fajl NEGY szakaszt jatszik le, ugyanazzal a programszammal es UGYANAZT
a rovid dallamot, csak a bank-beallitas kulonbozik. Egyszeri meghallgatas
utan tudod a valaszt:

  1. szakasz  bank nelkul (CC0=0, CC32=0)   <- referencia
  2. szakasz  CC0 = <BANK>, CC32 = 0        <- GS stilus
  3. szakasz  CC0 = 0, CC32 = <BANK>        <- amit a MidiBill3 kuld
  4. szakasz  bank nelkul, ujra             <- kontroll: visszaallt-e

ERTELMEZES

  - Csak a 2. szakasz szol maskent -> a lejatszo a CC0-t hasznalja (GS).
    A MidiBill3 mostani uzenete NEM jut at; a lejatszo bank-modjat kell
    atallitani, vagy meg kell varni a kulon MSB/LSB kezelest.
  - Csak a 3. szakasz szol maskent -> a lejatszo a CC32-t hasznalja (XG).
    A MidiBill3 mostani uzenete helyesen mukodik.
  - MINDKETTO maskent szol -> a lejatszo mindket bajtot figyeli
    (pl. bank = CC0*128 + CC32).
  - Egyik sem ter el -> vagy nem jut at a bank, VAGY az adott bank+program
    paros egyszeruen nincs a SoundFontban (lasd a figyelmeztetest lent).
  - Nemasag vagy teljesen oda nem illo hang a 3. szakaszban -> a lejatszo
    a CC32-t NEM bankkent, hanem valami masra (pl. SoundFont-valasztasra)
    hasznalja.

FONTOS - A BANKOT ES A PROGRAMOT ALLITSD A SAJAT SOUNDFONTODHOZ!

A teszt csak akkor mond valamit, ha a valasztott bank+program paros
TENYLEGESEN LETEZIK a betoltott SoundFontban. Ha nincs benne, mind a negy
szakasz egyforman fog szolni, es abbol nem lehet kovetkeztetni semmire.

Hol nezd meg? A MidiBill3 "SoundFont adatok importalasa" menupontja a
SoundFontbol keszit egy .lst fajlt, amiben a nem nulla bankok
"bank.program=Nev" alakban szerepelnek (pl. "8.24=Ukulele"). Vegy onnan
egy sort, es ird be ide a BANK es a PROGRAM erteket.

Az alapertelmezes (bank 8, program 24) a Roland SC-55 egyik legismertebb
variacioja: a nylon gitar helyett ukulele szol - ezt nem lehet elteveszteni.

HASZNALAT

    pip install mido
    python3 bank_select_teszt.py [kimeneti_fajl.mid]
"""

import sys

# ---------------------------------------------------------------------
# EZT A KETTOT ALLITSD A SAJAT SOUNDFONTODHOZ (lasd a fenti magyarazatot)
# ---------------------------------------------------------------------
BANK = 8         # a vizsgalt bank szama (1-127)
PROGRAM = 24     # a hangszer programszama (0-127); 24 = Acoustic Guitar (nylon)

# ---------------------------------------------------------------------

CSATORNA = 0             # 1. MIDI csatorna
HANGERO = 100            # CC7
TICK_PER_NEGYED = 480
TEMPO_MIKROSZEK = 500000  # 120 BPM -> 1 negyed = 0,5 s

NEGYED = TICK_PER_NEGYED
FEL = TICK_PER_NEGYED * 2

DALLAM = (60, 64, 67)    # C4 - E4 - G4
AKKORD = (60, 64, 67)


def szakasz(track, cim, bank_msb, bank_lsb, elso):
    """Egy szakasz: jelolo + bank + program, majd a dallam es egy akkord."""
    import mido

    # A szakaszok kozott egy fel utemnyi csend, hogy a fulnek legyen ideje.
    varakozas = 0 if elso else FEL

    track.append(mido.MetaMessage("marker", text=cim, time=varakozas))
    track.append(mido.Message("control_change", channel=CSATORNA, control=0,
                              value=bank_msb, time=0))
    track.append(mido.Message("control_change", channel=CSATORNA, control=32,
                              value=bank_lsb, time=0))
    track.append(mido.Message("program_change", channel=CSATORNA, program=PROGRAM, time=0))
    track.append(mido.Message("control_change", channel=CSATORNA, control=7,
                              value=HANGERO, time=0))

    # Harom kulon hang, egyenkent egy negyed
    for i, hang in enumerate(DALLAM):
        track.append(mido.Message("note_on", channel=CSATORNA, note=hang,
                                  velocity=100, time=0))
        track.append(mido.Message("note_off", channel=CSATORNA, note=hang,
                                  velocity=0, time=NEGYED))

    # Majd egy kitartott akkord - itt hallatszik a legjobban a hangszin
    for hang in AKKORD:
        track.append(mido.Message("note_on", channel=CSATORNA, note=hang,
                                  velocity=100, time=0))
    track.append(mido.Message("note_off", channel=CSATORNA, note=AKKORD[0],
                              velocity=0, time=FEL))
    for hang in AKKORD[1:]:
        track.append(mido.Message("note_off", channel=CSATORNA, note=hang,
                                  velocity=0, time=0))


def keszit(utvonal):
    import mido

    mf = mido.MidiFile(type=1, ticks_per_beat=TICK_PER_NEGYED)

    fej = mido.MidiTrack()
    fej.append(mido.MetaMessage("track_name", name="Bank Select teszt", time=0))
    fej.append(mido.MetaMessage(
        "text", text=f"MidiBill3 diagnosztika - bank {BANK}, program {PROGRAM}", time=0))
    fej.append(mido.MetaMessage("set_tempo", tempo=TEMPO_MIKROSZEK, time=0))
    fej.append(mido.MetaMessage("time_signature", numerator=4, denominator=4, time=0))
    mf.tracks.append(fej)

    zene = mido.MidiTrack()
    zene.append(mido.MetaMessage("channel_prefix", channel=CSATORNA, time=0))
    zene.append(mido.MetaMessage("track_name", name="01 - Bank Select teszt", time=0))

    szakasz(zene, "1 - bank nelkul (referencia)", 0, 0, elso=True)
    szakasz(zene, f"2 - CC0={BANK} (GS stilus)", BANK, 0, elso=False)
    szakasz(zene, f"3 - CC32={BANK} (XG stilus, ezt kuldi a MidiBill3)", 0, BANK, elso=False)
    szakasz(zene, "4 - bank nelkul, ujra (kontroll)", 0, 0, elso=False)

    mf.tracks.append(zene)
    mf.save(utvonal)
    return mf


def main():
    try:
        import mido  # noqa: F401
    except ImportError:
        print("HIANYZO FUGGOSEG: pip install mido", file=sys.stderr)
        return 1

    utvonal = sys.argv[1] if len(sys.argv) > 1 else "bank_select_teszt.mid"
    mf = keszit(utvonal)

    print(f"Elkeszult: {utvonal}")
    print(f"  bank {BANK}, program {PROGRAM}, {len(mf.tracks)} track, "
          f"hossz {mf.length:.1f} s")
    print()
    print("Szakaszok:")
    print("  1. bank nelkul (referencia)")
    print(f"  2. CC0={BANK}, CC32=0      - Roland GS stilus")
    print(f"  3. CC0=0, CC32={BANK}      - Yamaha XG stilus (a MidiBill3 ezt kuldi)")
    print("  4. bank nelkul, ujra (kontroll)")
    return 0


if __name__ == "__main__":
    sys.exit(main())

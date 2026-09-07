#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
A GM2.lst es a GM2.drm eloallitasa a General MIDI Level 2 hangkeszletbol.

FORRAS: https://en.wikipedia.org/wiki/General_MIDI_Level_2 (a felhasznalo
adta meg). A dobkeszlet-szamokat kulon ellenoriztuk a pgmusic.com GM
osszefoglalojaval.

CIMZES a GM2 szabvany szerint:
  - dallamhangszer: Bank Select MSB = 121, LSB = variacio, Program Change
  - dobkeszlet:     Bank Select MSB = 120, LSB = 0,        Program Change

A .lst sorok ezert "121.<lsb>.<program>=Nev", illetve "120.0.<program>=Nev"
alakuak - a haromtagu formatumot a 13. pont 2. fazisa vezette be.

A programszamok a fajlban 0-tol indulnak (mint a GM.lst-ben), a lenti
tablazat viszont a szabvany szerinti 1-tol indulo szamokat hasznalja -
az atszamitas itt tortenik, egy helyen.
"""

import io

# (programszam 1-tol, Bank Select LSB, nev)
GM2 = [
    (1, 0, "Acoustic Grand Piano"), (1, 1, "Wide Acoustic Grand Piano"),
    (1, 2, "Dark Acoustic Grand Piano"),
    (2, 0, "Bright Acoustic Piano"), (2, 1, "Wide Bright Acoustic Piano"),
    (3, 0, "Electric Grand Piano"), (3, 1, "Wide Electric Grand Piano"),
    (4, 0, "Honky-Tonk Piano"), (4, 1, "Wide Honky-Tonk Piano"),
    (5, 0, "Rhodes Electric Piano"), (5, 1, "Detuned Electric Piano 1"),
    (5, 2, "Variation Electric Piano 1"), (5, 3, "60's Electric Piano"),
    (6, 0, "Chorused Electric Piano"), (6, 1, "Detuned Electric Piano 2"),
    (6, 2, "Variation Electric Piano 2"), (6, 3, "Electric Piano Legend"),
    (6, 4, "Phaser Electric Piano"),
    (7, 0, "Harpsichord"), (7, 1, "Coupled Harpsichord"),
    (7, 2, "Wide Harpsichord"), (7, 3, "Open Harpsichord"),
    (8, 0, "Clavinet"), (8, 1, "Pulsed Clavinet"),
    (9, 0, "Celesta"),
    (10, 0, "Glockenspiel"),
    (11, 0, "Music Box"),
    (12, 0, "Vibraphone"), (12, 1, "Wet Vibraphone"),
    (13, 0, "Marimba"), (13, 1, "Wide Marimba"),
    (14, 0, "Xylophone"),
    (15, 0, "Tubular Bells"), (15, 1, "Church Bells"), (15, 2, "Carillon Bells"),
    (16, 0, "Dulcimer/Santur"),
    (17, 0, "Drawbar Organ 1"), (17, 1, "Detuned Drawbar Organ"),
    (17, 2, "60's Drawbar Organ"), (17, 3, "Drawbar Organ 2"),
    (18, 0, "Percussive B3 Organ 1"), (18, 1, "Detuned Percussive B3 Organ"),
    (18, 2, "Percussive B3 Organ 2"),
    (19, 0, "Rock Organ"),
    (20, 0, "Church Organ 1"), (20, 1, "Church Organ 2"), (20, 2, "Church Organ 3"),
    (21, 0, "Reeds Organ"), (21, 1, "Puffs Organ"),
    (22, 0, "French Accordion"), (22, 1, "Italian Accordion"),
    (23, 0, "Harmonica"),
    (24, 0, "Tango Accordion"),
    (25, 0, "Nylon-Strings Guitar 1"), (25, 1, "Ukulele"),
    (25, 2, "Opened Nylon-Strings Guitar"), (25, 3, "Nylon-Strings Guitar 2"),
    (26, 0, "Steel-Strings Guitar"), (26, 1, "12-Strings Steel Guitar"),
    (26, 2, "Mandolin"), (26, 3, "Steel-Strings Guitar + Body Tapped Sounds"),
    (27, 0, "Jazz Guitar"), (27, 1, "Hawaiian Guitar"),
    (28, 0, "Clean Electric Guitar"), (28, 1, "Chorus Guitar"), (28, 2, "Mid Tone Guitar"),
    (29, 0, "Muted Electric Guitar"), (29, 1, "Funky Guitar 1"),
    (29, 2, "Funky Guitar 2"), (29, 3, "Jazz Man"),
    (30, 0, "Overdriven Guitar"), (30, 1, "Guitar Pinch"),
    (31, 0, "Distortion Guitar"), (31, 1, "Feedback Guitar"),
    (31, 2, "Distortion Rhythm Guitar"),
    (32, 0, "Guitar Harmonics"), (32, 1, "Guitar Feedback"),
    (33, 0, "Acoustic Bass"),
    (34, 0, "Fingered Bass"), (34, 1, "Fingered Slap Bass"),
    (35, 0, "Picked Bass"),
    (36, 0, "Fretless Bass"),
    (37, 0, "Slap Bass 1"),
    (38, 0, "Slap Bass 2"),
    (39, 0, "Synth Bass 1"), (39, 1, "Synth Bass 101"), (39, 2, "Synth Bass 3"),
    (39, 3, "Clavi Bass"), (39, 4, "Hammered Bass"),
    (40, 0, "Synth Bass 2"), (40, 1, "Synth Bass 4"), (40, 2, "Rubber Bass"),
    (40, 3, "Attack Pulsed"),
    (41, 0, "Violin"), (41, 1, "Slow Violin"),
    (42, 0, "Viola"),
    (43, 0, "Cello"),
    (44, 0, "Contrabass"),
    (45, 0, "Tremolo Strings"),
    (46, 0, "Pizzicato Strings"),
    (47, 0, "Harp"), (47, 1, "Yangqin"),
    (48, 0, "Timpani"),
    (49, 0, "Strings Ensemble"), (49, 1, "Orchestral Strings"), (49, 2, "60's Strings"),
    (50, 0, "Slow Strings Ensemble"),
    (51, 0, "Synth Strings 1"), (51, 1, "Synth Strings 3"),
    (52, 0, "Synth Strings 2"),
    (53, 0, "Choir Aahs 1"), (53, 1, "Choir Aahs 2"),
    (54, 0, "Voice Oohs"), (54, 1, "Humming"),
    (55, 0, "Synth Voice"), (55, 1, "Analog Voice"),
    (56, 0, "Orchestra Hit"), (56, 1, "Bass Hit"), (56, 2, "6th Hit"), (56, 3, "Euro Hit"),
    (57, 0, "Trumpet"), (57, 1, "Dark Trumpet Soft"),
    (58, 0, "Trombone 1"), (58, 1, "Trombone 2"), (58, 2, "Bright Trombone"),
    (59, 0, "Tuba"),
    (60, 0, "Muted Trumpet 1"), (60, 1, "Muted Trumpet 2"),
    (61, 0, "French Horns 1"), (61, 1, "French Horns 2"),
    (62, 0, "Brass Section 1"), (62, 1, "Brass Section 2"),
    (63, 0, "Synth Brass 1"), (63, 1, "Synth Brass 3"), (63, 2, "Analog Brass 1"),
    (63, 3, "Jump Brass"),
    (64, 0, "Synth Brass 2"), (64, 1, "Synth Brass 4"), (64, 2, "Analog Brass 2"),
    (65, 0, "Soprano Sax"),
    (66, 0, "Alto Sax"),
    (67, 0, "Tenor Sax"),
    (68, 0, "Baritone Sax"),
    (69, 0, "Oboe"),
    (70, 0, "English Horn"),
    (71, 0, "Bassoon"),
    (72, 0, "Clarinet"),
    (73, 0, "Piccolo"),
    (74, 0, "Flute"),
    (75, 0, "Recorder"),
    (76, 0, "Pan Flute"),
    (77, 0, "Bottle Blow"),
    (78, 0, "Shakuhachi"),
    (79, 0, "Whistle"),
    (80, 0, "Ocarina"),
    (81, 0, "Square Lead"), (81, 1, "Square Wave"), (81, 2, "Sine Wave"),
    (82, 0, "Saw Lead"), (82, 1, "Saw Wave"), (82, 2, "Doctor Solo"),
    (82, 3, "Natural Lead"), (82, 4, "Sequenced Saw"),
    (83, 0, "Synth Calliope"),
    (84, 0, "Chiffer Lead"),
    (85, 0, "Charang"), (85, 1, "Wire Lead"),
    (86, 0, "Solo Synth Vox"),
    (87, 0, "5th Saw Wave"),
    (88, 0, "Bass & Lead"), (88, 1, "Delayed Lead"),
    (89, 0, "Fantasia Pad"),
    (90, 0, "Warm Pad"), (90, 1, "Sine Pad"),
    (91, 0, "Polysynth Pad"),
    (92, 0, "Space Voice Pad"), (92, 1, "Itopia"),
    (93, 0, "Bowed Glass Pad"),
    (94, 0, "Metal Pad"),
    (95, 0, "Halo Pad"),
    (96, 0, "Sweep Pad"),
    (97, 0, "Ice Rain"),
    (98, 0, "Soundtrack"),
    (99, 0, "Crystal"), (99, 1, "Synth Mallet"),
    (100, 0, "Atmosphere"),
    (101, 0, "Brightness"),
    (102, 0, "Goblin"),
    (103, 0, "Echo Drops"), (103, 1, "Echo Bell"), (103, 2, "Echo Pan"),
    (104, 0, "Star Theme"),
    (105, 0, "Sitar 1"), (105, 1, "Sitar 2"),
    (106, 0, "Banjo"),
    (107, 0, "Shamisen"),
    (108, 0, "Koto"), (108, 1, "Taisho Koto"),
    (109, 0, "Kalimba"),
    (110, 0, "Bagpipe"),
    (111, 0, "Fiddle"),
    (112, 0, "Shanai"),
    (113, 0, "Tinkle Bells"),
    (114, 0, "Agogo"),
    (115, 0, "Steel Drums"),
    (116, 0, "Woodblock"), (116, 1, "Castanets"),
    (117, 0, "Taiko Drums"), (117, 1, "Concert Bass Drums"),
    (118, 0, "Melodic Tom 1"), (118, 1, "Melodic Tom 2"),
    (119, 0, "Synth Drums"), (119, 1, "808 Toms"), (119, 2, "Electric Percussion"),
    (120, 0, "Reversed Cymbals"),
    (121, 0, "Guitar Fret Noise"), (121, 1, "Guitar Cut Noise"), (121, 2, "String Slap"),
    (122, 0, "Breath Noise"), (122, 1, "Flute Key Click"),
    (123, 0, "Seashore"), (123, 1, "Rain"), (123, 2, "Thunder"), (123, 3, "Wind"),
    (123, 4, "Stream"), (123, 5, "Bubble"),
    (124, 0, "Bird"), (124, 1, "Dog"), (124, 2, "Horse Gallop"), (124, 3, "Bird 2"),
    (125, 0, "Telephone 1"), (125, 1, "Telephone 2"), (125, 2, "Door Creaking"),
    (125, 3, "Door Closing"), (125, 4, "Scratch"), (125, 5, "Wind Chimes"),
    (126, 0, "Helicopter"), (126, 1, "Car Engine"), (126, 2, "Car Stop"),
    (126, 3, "Car Pass"), (126, 4, "Car Crash"), (126, 5, "Siren"), (126, 6, "Train"),
    (126, 7, "Jet Plane"), (126, 8, "Starship"), (126, 9, "Burst Noise"),
    (127, 0, "Applause"), (127, 1, "Laughter"), (127, 2, "Screaming"),
    (127, 3, "Punch"), (127, 4, "Heartbeat"), (127, 5, "Footsteps"),
    (128, 0, "Gunshot"), (128, 1, "Machine Gun"), (128, 2, "Laser gun"),
    (128, 3, "Explosion"),
]

# (programszam 1-tol, nev) - a szabvanyos dobkeszletek.
# A 1/9/17/25/26/33/41 szamozast a pgmusic.com GM osszefoglaloja is
# megerositi; a 49 (Orchestra) es az 57 (SFX) a GS/GM2 bevett keszlete,
# ugyanaz, ami a program beepitett listajaban (AlapertelmezettDobKeszlet) is
# szerepel.
GM2_DOB = [
    (1, "Standard Kit"), (9, "Room Kit"), (17, "Power Kit"),
    (25, "Electronic Kit"), (26, "Analog Kit"), (33, "Jazz Kit"),
    (41, "Brush Kit"), (49, "Orchestra Kit"), (57, "SFX Kit"),
]

MELODIKUS_MSB = 121
DOB_MSB = 120


def ir(utvonal, fejlec, sorok):
    io.open(utvonal, "w", encoding="utf-8", newline="\r\n").write(
        "\n".join(fejlec + [""] + sorok) + "\n")


def main():
    # --- Ellenorzesek, mielott barmit kiirnank ---
    kulcsok = set()
    programok = set()
    for pc, lsb, nev in GM2:
        assert 1 <= pc <= 128, f"programszam tartomanyon kivul: {pc}"
        assert 0 <= lsb <= 127, f"LSB tartomanyon kivul: {lsb}"
        assert nev.strip() and "=" not in nev, f"gyanus nev: {nev!r}"
        k = (pc, lsb)
        assert k not in kulcsok, f"ISMETLODO bejegyzes: PC {pc}, LSB {lsb}"
        kulcsok.add(k)
        programok.add(pc)

    alap = [pc for pc, lsb, _ in GM2 if lsb == 0]
    assert len(alap) == 128, f"{len(alap)} alapvaltozat van 128 helyett"
    assert sorted(alap) == list(range(1, 129)), "hianyzik vagy ismetlodik egy alapprogram"
    assert len(programok) == 128, f"{len(programok)} kulonbozo programszam"

    print(f"GM2 dallamhangszerek : {len(GM2)} bejegyzes, {len(programok)} programszam")
    print(f"  ebbol alapvaltozat : {len(alap)} (LSB 0)")
    print(f"  variacio           : {len(GM2) - len(alap)}")
    print(f"GM2 dobkeszletek     : {len(GM2_DOB)}")

    lst = [f"{MELODIKUS_MSB}.{lsb}.{pc - 1}={nev}" for pc, lsb, nev in GM2]
    drm = [f"{DOB_MSB}.0.{pc - 1}={nev}" for pc, nev in GM2_DOB]

    ir("GM2.lst", [
        "# General MIDI Level 2 hangszerlista (MMA szabvany, 1999)",
        "# Cimzes: Bank Select MSB 121, LSB a variacio, majd Program Change.",
        "# A programszamok itt 0-tol indulnak, mint a GM.lst-ben.",
        "# FIGYELEM: ez a lista MSB 121-et kuld minden hangszernel, ezert",
        "# csak GM2-kepes hangmodullal vagy SoundFonttal ad ertelmes hangot.",
    ], lst)

    ir("GM2.drm", [
        "# General MIDI Level 2 dobkeszletek a 10. csatornara",
        "# Cimzes: Bank Select MSB 120, LSB 0, majd Program Change.",
    ], drm)

    print()
    print("GM2.lst elso es utolso sorai:")
    for s in lst[:3] + ["  ..."] + lst[-3:]:
        print("   ", s)
    print("GM2.drm:")
    for s in drm:
        print("   ", s)


if __name__ == "__main__":
    main()

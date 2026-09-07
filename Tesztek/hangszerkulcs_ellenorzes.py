#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
MidiBill3 - a hangszerkulcs es a .lst index-formatum ellenorzese
=================================================================

MIT ELLENORIZ EZ A SZKRIPT?

A `Modellek/Hangszerkulcs.vb` modul es a `Modellek/HangszerLista.vb`
.lst-index kezelesenek (IndexReszFeldolgozasa / IndexSzoveg) HU
PYTHON-PORTJAT. A ketto ugyanannak a dolognak a ket alakja: a kulcs
SZAMKENT es a kulcs SZOVEGKENT, ahogy a .lst fajlba kerul.

A Hangszerkulcs.vb modul fuzi ossze a Bank Select MSB-t, a Bank Select
LSB-t es a programszamot EGYETLEN egesz szamma, amit a program mindenutt
hasznal: a HangszerLista Dictionary kulcsakent, a preset (.json)
"Hangszer" mezojekent es a HangszerPanel legordulo eleme mogott.

MIERT KELL RA KULON TESZT?

Mert a 13. pont (kulon MSB/LSB bank-kezeles) EGESZ atallasi terve egyetlen
allitason nyugszik:

    MSB = 0 eseten az UJ kulcs SZAMTANILAG AZONOS a regivel.

A regi keplet `bank * 128 + program` volt, ahol a "bank" a Bank Select
LSB-t jelentette. Ha ez az azonossag barmiert megsérulne, a meglevo preset
(.json) es hangszerlista (.lst) fajlok CSENDBEN mas hangszerre mutatnanak
- ezert ellenorizzuk kimeritoen, nem mintavetellel.

HASZNALAT

    python3 hangszerkulcs_ellenorzes.py

Sikeres futas eseten a vegen "MINDEN ELLENORZES RENDBEN" felirat all, es
a kilepesi kod 0. Barmely elteres eseten AssertionError szall el.

Keszult: 2026-09-07, a 13. pont 1. fazisahoz.
"""

import sys

# ---------------------------------------------------------------------
# A Hangszerkulcs.vb hu portja
# ---------------------------------------------------------------------

LSB_SULY = 128
MSB_SULY = 16384
MAX_ADATBAJT = 127
MAX_KULCS = 127 * MSB_SULY + 127 * LSB_SULY + 127


def kulcs(bank_msb, bank_lsb, program):
    return bank_msb * MSB_SULY + bank_lsb * LSB_SULY + program


def bank_msb(k):
    return k // MSB_SULY


def bank_lsb(k):
    return (k // LSB_SULY) % LSB_SULY


def program_szam(k):
    return k % LSB_SULY


def index_resz_feldolgozasa(index_resz):
    """
    A VB IndexReszFeldolgozasa hu portja. Visszaad: kulcs, vagy None, ha a
    sor ervenytelen. Harom alak: "program", "lsb.program",
    "msb.lsb.program"; minden szam 0-127.
    """
    reszek = index_resz.split(".")
    if not (1 <= len(reszek) <= 3):
        return None

    ertekek = [0, 0, 0]        # MSB, LSB, program
    for i in range(len(reszek)):
        szoveg = reszek[len(reszek) - 1 - i].strip()
        try:
            ertek = int(szoveg)
        except ValueError:
            return None
        if ertek < 0 or ertek > MAX_ADATBAJT:
            return None
        ertekek[2 - i] = ertek

    return kulcs(ertekek[0], ertekek[1], ertekek[2])


def sor_feldolgozasa(sor):
    """
    Egy .lst sor feldolgozasa - a VB FajlbolBetoltesBelso ciklusmagjanak
    portja. Visszaad (kulcs, nev), vagy None, ha a sort ki kell hagyni.
    """
    tiszta = sor.strip()
    if not tiszta:
        return None
    if tiszta.startswith("#"):        # megjegyzes-sor
        return None
    i = tiszta.find("=")
    if i <= 0:
        return None
    k = index_resz_feldolgozasa(tiszta[:i].strip())
    nev = tiszta[i + 1:].strip()
    if k is None or not nev:
        return None
    return k, nev


def index_szoveg(k):
    """A VB IndexSzoveg hu portja: a LEGROVIDEBB visszaolvashato alak."""
    msb, lsb, program = bank_msb(k), bank_lsb(k), program_szam(k)
    if msb != 0:
        return "%d.%d.%d" % (msb, lsb, program)
    if lsb != 0:
        return "%d.%d" % (lsb, program)
    return str(program)


def bank_jeloles(k):
    msb, lsb = bank_msb(k), bank_lsb(k)
    if msb == 0 and lsb == 0:
        return ""
    if msb == 0:
        return " (B%d)" % lsb
    return " (B%d.%d)" % (msb, lsb)


# ---------------------------------------------------------------------
# Ellenorzesek
# ---------------------------------------------------------------------

def regi_keplet(bank, program):
    """A 13. pont ELOTTI kulcskepzes: a 'bank' a Bank Select LSB volt."""
    return bank * 128 + program


def main():
    # --- 1. A visszamenoleges kompatibilitas: MSB = 0 eseten AZONOS ---
    # Kimerito: mind a 128 * 128 lehetseges regi kulcsra.
    elteres = 0
    for bank in range(128):
        for program in range(128):
            if regi_keplet(bank, program) != kulcs(0, bank, program):
                elteres += 1
    assert elteres == 0, \
        (f"{elteres} eseten elter a regi es az uj kulcs MSB=0 mellett - "
         f"a meglevo preset es .lst fajlok CSENDBEN elromlananak")
    print("OK  MSB=0 mellett mind a 16384 regi kulcs valtozatlan")

    # --- 2. Oda-vissza: minden ervenyes harmas pontosan visszabomlik ---
    elteres = 0
    for msb in range(128):
        for lsb in range(128):
            for program in (0, 1, 63, 127):
                k = kulcs(msb, lsb, program)
                if (bank_msb(k), bank_lsb(k), program_szam(k)) != (msb, lsb, program):
                    elteres += 1
    assert elteres == 0, f"{elteres} harmas nem bomlik vissza helyesen"
    print("OK  oda-vissza atalakitas pontos (128 * 128 * 4 eset)")

    # --- 3. A kulcsok EGYEDIEK: ket kulonbozo harmas sosem adhat azonosat ---
    # (Ez az elozo pontbol kovetkezik, de kimondva is ellenorizzuk, mert
    #  ha ket hangszer ugyanarra a kulcsra esne, a Dictionary elnyelne az
    #  egyiket - a lista CSENDBEN rovidebb lenne.)
    kulcsok = set()
    for msb in range(128):
        for lsb in range(128):
            kulcsok.add(kulcs(msb, lsb, 0))
    assert len(kulcsok) == 128 * 128, \
        f"utkozo kulcsok: {128 * 128 - len(kulcsok)} veszett el"
    print("OK  a kulcsok egyediek (nincs utkozes)")

    # --- 4. A felso hatar ---
    assert MAX_KULCS == kulcs(127, 127, 127) == 2097151, f"MAX_KULCS = {MAX_KULCS}"
    assert MAX_KULCS > 16383, \
        ("a regi 16383-as felso hatar levagta volna az MSB-t tartalmazo "
         "kulcsokat - a preset-betoltes tartomany-ellenorzeset frissiteni kell")
    print(f"OK  legnagyobb kulcs {MAX_KULCS} (a regi 16383-as hatar tul alacsony volt)")

    # --- 5. A bank rovid jelolese ---
    esetek = [
        (kulcs(0, 0, 24), ""),
        (kulcs(0, 8, 24), " (B8)"),
        (kulcs(121, 1, 48), " (B121.1)"),
        (kulcs(8, 0, 24), " (B8.0)"),
    ]
    for k, vart in esetek:
        kapott = bank_jeloles(k)
        assert kapott == vart, f"bank_jeloles({k}): {vart!r}-t vartunk, ez {kapott!r}"
    print("OK  bank-jeloles: '', ' (B8)', ' (B121.1)', ' (B8.0)'")

    # --- 6. A SoundFont-import cimzese (13. pont, 8. fazis) ---
    # MERESSEL ALATAMASZTVA (Tesztek/bank_select_teszt.mid): a BASSMIDI-alapu
    # VirtualMIDISynth a CC0-t (MSB) tekinti a SoundFont bankjanak, a CC32-t
    # figyelmen kivul hagyja. A SoundFontOlvaso ezert a .sf2 "wBank" mezojet
    # az MSB-be teszi. Korabban az LSB-be tette, es ezert a nem nulla banku
    # hangszerek NEM SZOLALTAK MEG.
    sf_bank, sf_program = 8, 24
    uj_kulcs = kulcs(sf_bank, 0, sf_program)
    assert (bank_msb(uj_kulcs), bank_lsb(uj_kulcs)) == (sf_bank, 0), \
        "a SoundFont bankjanak az MSB-be kell kerulnie"
    assert uj_kulcs != kulcs(0, sf_bank, sf_program), \
        ("az MSB-s es az LSB-s cimzes NEM eshet egybe - ha igen, a javitas "
         "hatastalan lenne")
    assert index_szoveg(uj_kulcs) == "8.0.24", \
        f"a .lst-ben '8.0.24' alakban kell megjelennie, ez {index_szoveg(uj_kulcs)!r}"
    print("OK  SoundFont bank -> MSB (a .lst-ben '8.0.24', nem a regi '8.24')")

    # --- 7. A .lst index-formatum: a REGI alakok jelentese NEM valtozik ---
    assert index_resz_feldolgozasa("40") == kulcs(0, 0, 40), "egytagu alak"
    assert index_resz_feldolgozasa("8.24") == kulcs(0, 8, 24), \
        ("a kettagu alak szama a Bank Select LSB - ha ez MSB-re valtozna, a "
         "meglevo .lst fajlok CSENDBEN mas hangszerre mutatnanak")
    assert index_resz_feldolgozasa("8.24") == regi_keplet(8, 24), \
        "a kettagu alak a regi keplettel is egyeznie kell"
    print("OK  a regi .lst alakok ('40', '8.24') jelentese valtozatlan")

    # --- 8. Az UJ, haromtagu alak ---
    assert index_resz_feldolgozasa("121.1.48") == kulcs(121, 1, 48), "haromtagu alak"
    assert index_resz_feldolgozasa("8.0.24") == kulcs(8, 0, 24), "haromtagu alak, nulla LSB"
    assert index_resz_feldolgozasa("0.8.24") == index_resz_feldolgozasa("8.24"), \
        "a '0.8.24' es a '8.24' UGYANAZ a kulcs"
    print("OK  a haromtagu alak ('121.1.48', '8.0.24') helyesen bomlik szet")

    # --- 9. Ervenytelen sorok: kihagyva, nem talalgatva ---
    ervenytelen = ["", "abc", "8.abc", "-1.24", "128.24", "8.128", "1.2.3.4",
                   "200.24", "24.200"]
    for sor in ervenytelen:
        assert index_resz_feldolgozasa(sor) is None, \
            f"a(z) {sor!r} sort ervenytelennek kellene tekinteni"
    print(f"OK  mind a {len(ervenytelen)} ervenytelen alak elutasitva "
          f"(tartomanyon kivuli szam, tul sok tag, nem szam)")

    # --- 10. Iras + visszaolvasas: minden ervenyes kulcsra ODA-VISSZA ---
    # Ez a legfontosabb allitas a formatumrol: amit a Ment() kiir, azt a
    # Betolt() pontosan vissza kell tudja olvasni. Kulonben egy SoundFont
    # ujraimportalasa utan mas hangszerek jonnenek elo, mint amiket a
    # program kiirt.
    elteres = 0
    for msb in (0, 1, 8, 121, 127):
        for lsb in (0, 1, 8, 127):
            for program in (0, 24, 127):
                k = kulcs(msb, lsb, program)
                if index_resz_feldolgozasa(index_szoveg(k)) != k:
                    elteres += 1
    assert elteres == 0, f"{elteres} kulcs nem olvasodik vissza helyesen"
    print("OK  iras + visszaolvasas pontos (60 eset)")

    # --- 11. A legrovidebb alakot irjuk ---
    esetek = [(kulcs(0, 0, 40), "40"), (kulcs(0, 8, 24), "8.24"),
              (kulcs(121, 1, 48), "121.1.48"), (kulcs(8, 0, 24), "8.0.24")]
    for k, vart in esetek:
        kapott = index_szoveg(k)
        assert kapott == vart, f"index_szoveg({k}): {vart!r}-t vartunk, ez {kapott!r}"
    print("OK  a legrovidebb visszaolvashato alak keszul ('40', '8.24', '121.1.48')")

    # --- 12. Megjegyzes-sorok es egyeb kihagyando sorok ---
    # A "#" kezdetu sorokat AKKOR is ki kell hagyni, ha van bennuk "=".
    # Ez korabban nem igy volt: egy "# MSB = 121" megjegyzes bejegyzesnek
    # latszott (ervenytelennek bizonyult, de a szurest a veletlen vegezte).
    kihagyando = [
        "", "   ", "# egyszeru megjegyzes",
        "# Cimzes: Bank Select MSB = 121, LSB = variacio",
        "#40=Ez sem bejegyzes",
        "nincs benne egyenlosegjel",
        "=nev index nelkul",
        "40=",                        # ures nev
    ]
    for sor in kihagyando:
        assert sor_feldolgozasa(sor) is None, f"a(z) {sor!r} sort ki kellene hagyni"
    print(f"OK  mind a {len(kihagyando)} kihagyando sor kiszurve (megjegyzes, ures nev, hibas alak)")

    assert sor_feldolgozasa("  8.0.24 = Ukulele  ") == (kulcs(8, 0, 24), "Ukulele"), \
        "a sor koruli es az '=' melletti szokozoket le kell vagni"
    print("OK  ervenyes sor beolvasva, a szokozok levagva")

    print()
    print("=" * 52)
    print("MINDEN ELLENORZES RENDBEN")
    print("=" * 52)
    return 0


if __name__ == "__main__":
    sys.exit(main())

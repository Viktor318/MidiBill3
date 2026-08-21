# MidiBill3 (VB.NET) – Aktuális állapot összefoglalója

Ez a dokumentum a **VB6 → VB.NET portolás jelenlegi állapotát** foglalja
össze, hogy egy új beszélgetés gyorsan fel tudjon zárkózni anélkül, hogy
végig kellene olvasni a teljes fejlesztési történetet. A projekt **éles,
működő állapotban van**, aktívan továbbfejlesztés alatt.

---

## Alapadatok

- **Verzió**: 1.7.0 (SemVer), a **`MidiBill3.vbproj`-ban közvetlenül**
  (a mai kör után érdemes lehet léptetni - lásd lent)
- **Cél keretrendszer**: .NET 10 (LTS)
- **UI**: WinForms
- **MIDI réteg**: NAudio.Midi 2.3.0 (+ NAudio.Core 2.3.0)
- **Fájlformátum**: JSON (`System.Text.Json`), emberi olvasást segítő
  `//` megjegyzésekkel kiegészítve
- **Fejlesztői környezet**: Visual Studio 2026 Community, két gépen
  (`C:\Dev\MidiBill3` mindkettőn – **soha ne Documents/OneDrive alá**)
- **Verziókezelés**: GitHub, rendszeres commit+push munkafolyamattal
- **Alkalmazásikon**: `MidiBill3.ico`, beállítva projekt- és Form-szinten is
- **Ablak**: `AutoScaleMode.Dpi` + `ApplicationHighDpiMode = PerMonitorV2`,
  `FormBorderStyle = FixedSingle`, `MaximizeBox = False`, `MinimizeBox`
  alapértelmezett `True`
- **Címsor**: `"MIDI billentyűzet megszólaltatása 16 csatornán : <beállítások
  fájlneve>, <hangszerlista fájlneve>"` (`CimSorUjrarajzolas()`)
- **Névjegy**: 16 csatorna, "a 10. csatorna dedikált dobkészlet-listát
  használ" szöveggel

## Projektstruktúra

```
MidiBill3/                        - EGYETLEN projekt
├── MainForm.vb / .Designer.vb   - Fő ablak
├── HangszerPanel.vb / .Designer.vb - Egy hangszerpanel (UserControl), 4 fizikai
│                                   példány, a kiválasztott csatornacsoportot mutatja
├── ApplicationEvents.vb         - Globális hibanaplózás (HibaNaplo.txt)
├── Modellek/
│   ├── AppInfo.vb               - Verziószám lekérdezés
│   ├── Beallitasok.vb           - Adatmodell (16 CSATORNA, MIND szerkeszthető,
│   │                               beleértve a 10.-et is) + JSON mentés/betöltés
│   ├── MidiEngine.vb            - MIDI be-/kimenet, fan-out (16 csatorna,
│   │                               NINCS letiltott csatorna), split pont,
│   │                               transzponálás, Bank Select-alapú hangszerváltás
│   ├── HangszerLista.vb         - KÉT hangszerlista: Nevek (globális, 15 csatorna)
│   │                               és DobNevek (dedikált, 10. csatorna) -
│   │                               Betolt()/BetoltDobKeszletet()/Ment()
│   ├── SoundFontOlvaso.vb       - .sf2 "phdr" beolvasása: hangszerek (Nevek) ÉS
│   │                               dobkészlet-presetek (Dobok, Bank 128) külön
│   └── Hangnevek.vb             - MIDI hangszám -> hangnév konverzió
└── Adatok/                      - Preset fájlok + hangszerlista fájlok:
                                    GM.lst (hangszerek) + GM.drm (dobkészletek,
                                    PÁROSÍTVA az alapnév alapján), Teszt.lst,
                                    TesztGS.lst stb.
```

## Elkészült funkciók

**Alapfunkciók**: 16 MIDI csatorna (MIND szerkeszthető - lásd lent),
csatorna-választó, Split Point, Globális Transzponálás, csatornánkénti
oktáv-transzponálás (±2, azonnali/retroaktív hatással), globális Reverb
(CC91), pedál-inverzió (passzív "mód"-kapcsoló), "Csend legyen!"
pánikgomb, "Kilépés"/"Kilépés mentés nélkül", MIDI eszközlista
frissítés+validálás, olvasható MIDI üzenet-kijelzés (max. 10x/mp),
hibatűrő MIDI-küldés, robusztus JSON hibakezelés. (Részletek: forráskód
vagy korábbi fejlesztési történet.)

**Fájlkezelés**: JSON mentés/betöltés, eszköznév-alapú MIDI be-/kimenet
tárolás, régi `.mb3` import, `Kezdet.json` automatikus be-/kimentés.

**Segédprogramok beépítve a főprogramba**: "Segédprogramok" menü (Preset
fájlok frissítése, Régi .mb3 fájlok konvertálása).

**Bank Select-alapú kiterjesztett hangszertámogatás (GS/XG-kompatibilis)**:
`MidiEngine.ProgramValtas()` mindig küld Bank Select (CC0+CC32) + Program
Change-t, `.lst` formátum `bank.program=Név` szintaktikával, `HangszerPanel`
`Név (B<bank>)` megjelenítéssel. (Részletek: korábbi fejlesztési történet.)

**10. csatorna feloldása + dedikált dobkészlet-hangszerlista (ÚJ, ez a kör)**

*Miért volt letiltva, és miért oldottuk fel:*
- A 10. csatorna letiltása **saját, szoftveres döntés** volt, NEM MIDI-
  protokoll-korlát - egy GM2-kompatibilis szintetizátor a 10. csatornán
  érkező Program Change-et dobkészlet-**variációként** értelmezi
- A felhasználó feloldást kért, hogy a jövőben (KÜLÖN, KÉSŐBBI lépésben)
  metronóm-funkcióra is használhassa a csatornát (konkrét ütős
  hangokkal, pl. Claves/Side Stick - ez MÉG NINCS megvalósítva)

*"Csatornánkénti hangszerlista" architektúra:*
- `HangszerLista.DobNevek`: ÖNÁLLÓ Dictionary a 10. csatornához, a
  `Nevek`-től (a másik 15 csatorna listája) FÜGGETLENÜL
- `HangszerPanel.HangszerListaForrasBeallitasa(lista)`: a panel mostantól
  tetszőleges Dictionary-t elfogadhat forrásként (`Nothing` = globális
  `Nevek`, egyébként a megadott dedikált lista) - `MainForm.
  CsatornaPanelekFrissitese` ez alapján dönt csatornánként
- A 10. csatorna MOST MÁR: aktiválható, hangszerválasztható (a
  dobkészlet-listából), Split zóna (Alul/Felül) és Oktáv is működik,
  MENTÉS/BETÖLTÉS a normál úton (a korábbi "sosem mentjük/töltjük ezt a
  csatornát" védőblokkok MIND törölve `BeallitasokAlkalmazasa`-ból és
  `AktualisBeallitasokOsszegyujtese`-ből)
- `HangszerekErvenyesitese`: csatornánként a MEGFELELŐ listát ellenőrzi
  (10.-nél `DobNevek`, egyébként `Nevek`)

*Fájlformátum és -párosítás (`.lst` + `.drm`):*
- **Ugyanaz** a szöveges `index=Név` formátum, csak `.drm` kiterjesztéssel
- **Alapértelmezett**: `Adatok/GM.lst` (hangszerek) + `Adatok/GM.drm`
  (dobkészletek) - induláskor mindkettő betöltődik (`HangszerListaBetoltese`)
- **Párosítás elve**: a `.drm` fájlnév mindig az AKTÍV `.lst` fájl
  ALAPNEVÉBŐL származik (`GetFileNameWithoutExtension(lst) & ".drm"`) -
  amikor egy másik `.lst`-et töltesz be, a program AUTOMATIKUSAN,
  CSENDBEN megpróbálja betölteni a párosított `.drm`-et is
  (`HangszerListaAlkalmazasa`); ha nincs ilyen, a JELENLEGI dob-lista
  változatlanul marad (nem hiba, nincs figyelmeztetés)
- **Nincs** külön `Beallitasok.DobListaFajl` mező - a dob-lista fájlneve
  NEM kerül külön mentésre a presetbe, mindig a `.lst`-ből származtatva
  töltődik újra
- Egy jól dokumentált, GM2-stílusú alap `GM.drm` készült (Standard/Room/
  Power/Electronic/Analog/Jazz/Brush/Orchestra/SFX Kit, program-számok:
  0,8,16,24,25,32,40,48,56) - a felhasználó saját szintetizátorán a
  pontos számozás eltérhet, a fájl szabadon szerkeszthető

*SoundFont-import bővítése:*
- `SoundFontOlvaso.Beolvas()`/`FeldolgozPhdr()`: HÁROMFELÉ bontja a
  presetket: `Nevek` (Bank 0-127, hangszerek), `Dobok` (KIZÁRÓLAG Bank
  128, a Program szám a kulcs), `Kihagyottak` (ténylegesen érvénytelen
  rekordok - üres név, Program>127, Bank>128)
  - **FONTOS**: a Bank 128 korábban "érvénytelenként" el lett dobva -
    most FELISMERT, hasznos kategória (SF2-konvenció a dobkészletekhez)
- SoundFont-importáláskor MOST MÁR KÉT fájl jön létre egyszerre, azonos
  alapnévvel: pl. `Teszt.sf2` → `Teszt.lst` (hangszerek) + `Teszt.drm`
  (dobkészletek, ha voltak Bank 128-as presetek a fájlban) - mindkettő
  automatikusan alkalmazódik a `HangszerListaAlkalmazasa` párosítási
  mechanizmusán keresztül
  - Élesben tesztelve a felhasználó saját, valódi SoundFont-jával
    (`JJazzLab-SoundFont.sf2`): 193 hangszer + a korábban "15 érvénytelen
    bejegyzésként" jelzett Bank 128-as dobkészlet-presetek most már
    helyesen a `.drm`-be kerülnek

*HIBAJAVÍTÁS a feloldás során:*
- **Csatorna-felirat duplikáció**: a `"N. csatorna (dob)"` felirat
  összeállítása KÉT helyen volt megírva (`CsatornaPanelekFrissitese` ÉS
  `HangszerPanel_AktivValtozott`) - az egyikből hiányzott a `"(dob)"`
  jelzés, ami a checkbox KÖZVETLEN kattintásakor (nem csoportváltáskor/
  betöltéskor) eltüntette a jelölést. Javítva egy közös
  `CsatornaFelirat(csatornaIndex, aktiv)` segédfüggvénnyel, amit mindkét
  hely hív - nincs többé duplikált logika

## Ismert, tudatosan meghozott döntések

- Hangnevek nemzetközi konvencióval (C, C#, D...)
- ReadyToRun/Trim kikapcsolva, Framework-dependent publish
- Panic gomb pufferelés szándékosan nincs
- ComboBox-ok explicit elvesztik a fókuszt kiválasztás után
- RadioButton a csatorna-választóhoz (natív kizárás)
- Verziószám közvetlenül a `MidiBill3.vbproj`-ban
- **Kódmódosításoknál a meglévő kommenteket MINDIG meg kell tartani**
  (explicit felhasználói elvárás, memóriában rögzítve)
- Hangszerlista fájlformátum: `index=Név` vagy `bank.program=Név`
  szöveges fájl (nem JSON/XML)
- Bank Select mindig MSB=0, mindig elküldve (még bank=0-nál is)
- **`.lst`/`.drm` párosítás fájlnév-alapú, nem preset-be mentett mező** -
  egyszerűbb, kevesebb állapot karbantartása
- A 10. csatorna dobkészlet-listája NEM cserélhető külön menüből (nincs
  "Dobkészlet betöltése..." menüpont) - csak a `.lst`-tel párosítva
  vagy SoundFont-importtal frissül

## Globális hibanaplózás

Az `ApplicationEvents.vb` elkap minden kezeletlen kivételt, `HibaNaplo.txt`-be
ír. Natív (nem managed) összeomlásokat ez sem tud elkapni.

## Következő lépések / nyitott témák

1. **Metronóm-funkció** - a 10. csatorna feloldásának egyik indoka volt;
   konkrét ütős hangok (pl. Claves, Side Stick - fix GM percussion
   billentyű-kiosztás) Note On üzenetként való kiküldése, FÜGGETLENÜL a
   dobkészlet-választástól. MÉG NINCS megtervezve/megvalósítva

2. **Chorus (CC93)** - elvi terv kész, ugyanaz a minta, mint a Reverb.
   Még nincs megvalósítva

3. **MIDI felvétel/visszajátszás** - elvi terv, eldöntött kulcskérdéssel:
   a NYERS BEMENETET vesszük fel (pedál-inverzió UTÁN, split pont/
   transzponálás/csatorna-szétosztás ELŐTT), visszajátszáskor újra
   végigmegy a `MidiEngine.UzenetKuldes()`-en. Még nincs elkezdve

4. **Telepítő (Inno Setup)** - eldöntött irány, még nincs elkezdve

5. **Performancia - MIDI-küldés leválasztása a UI-szálról** - elvi téma,
   átbeszélve, eddig nem volt probléma, nem sürgős

6. Végigtesztelési checklist még nincs összeállítva

7. **Elméleti téma, lezárva**: iOS/iPadOS-portolás - távoli asztali
   elérés (RDP/Chrome Remote Desktop) a gyakorlati megoldás, ha a
   fizikai MIDI billentyűzet a Windows gépnél marad

8. **Bizonytalanság, tesztelésre vár**: a 10. csatornán a Bank Select +
   Program Change küldési mechanizmus (ugyanaz, mint a többi
   csatornánál) NEM garantáltan azonos módon viselkedik minden
   szintetizátoron a dobkészlet-választásnál (némely eszköz esetleg
   Bank Select nélkül, csak sima Program Change-t várna a 10.
   csatornán) - a felhasználó saját GS SoundFont-jával tesztelte,
   eredmény még nincs visszajelezve ebben a beszélgetésben

# MidiBill3 (VB.NET) – Aktuális állapot összefoglalója

Ez a dokumentum a **VB6 → VB.NET portolás jelenlegi állapotát** foglalja
össze, hogy egy új beszélgetés gyorsan fel tudjon zárkózni anélkül, hogy
végig kellene olvasni a teljes fejlesztési történetet. A projekt **éles,
működő állapotban van**, aktívan továbbfejlesztés alatt.

---

## Alapadatok

- **Verzió**: 1.10.0 (SemVer), a **`MidiBill3.vbproj`-ban közvetlenül**
  (a 2026-09-03-i kiadás: "Extra MIDI üzenetek" funkció, a kódátnézés
  három hibajavítása, az `Option Strict On`, valamint a gyári/felhasználói
  tartalom szétválasztása - lásd `CHANGELOG.md`)
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
  alapértelmezett `True`, `ClientSize = 958 x 974`
- **Címsor**: `"MIDI billentyűzet megszólaltatása 16 csatornán : <beállítások
  fájlneve>, <hangszerlista fájlneve>"` (`CimSorUjrarajzolas()`)
- **README**: kétnyelvű - `README.md` (magyar, alapértelmezett) +
  `README.en.md` (angol), kattintható nyelv-váltó linkkel mindkét fájl tetején

## Projektstruktúra

```
MidiBill3/                        - EGYETLEN projekt
├── MainForm.vb / .Designer.vb   - Fő ablak
├── HangszerPanel.vb / .Designer.vb - Egy hangszerpanel (UserControl), 4 fizikai
│                                   példány, a kiválasztott csatornacsoportot mutatja
│                                   (magasság 378, keret 408 - Her/Tér/Vib/Chorus/
│                                   Oktáv csúszkák + Aktív/Alul/Felül checkbox-ok)
├── ApplicationEvents.vb         - Globális hibanaplózás (HibaNaplo.txt)
├── Modellek/
│   ├── AppInfo.vb               - Verziószám lekérdezés
│   ├── Beallitasok.vb           - Adatmodell (16 CSATORNA, MIND szerkeszthető)
│   │                               + JSON mentés/betöltés
│   ├── MidiEngine.vb            - MIDI be-/kimenet, fan-out (16 csatorna,
│   │                               NINCS letiltott csatorna), split pont,
│   │                               transzponálás, Bank Select-alapú hangszerváltás
│   ├── HangszerLista.vb         - KÉT hangszerlista: Nevek (globális, 15 csatorna)
│   │                               és DobNevek (dedikált, 10. csatorna) -
│   │                               Betolt()/BetoltDobKeszletet()/Ment()
│   ├── SoundFontOlvaso.vb       - .sf2 "phdr" beolvasása: hangszerek (Nevek) ÉS
│   │                               dobkészlet-presetek (Dobok, ISMERT DOBKÉSZLET-
│   │                               BANKOK: 120, 127, 128) külön
│   ├── Hangnevek.vb             - MIDI hangszám -> hangnév konverzió
│   └── Utvonalak.vb             - MINDEN fájl-útvonal egy helyen: gyári
│                                   (csak olvasható) vs. felhasználói
│                                   (írható) szétválasztás + egyszeri
│                                   átköltöztetés a régi szerkezetből
├── GM.lst                       - GYÁRI hangszerlista (a telepítő szállítja)
├── GM.drm                       - GYÁRI dobkészlet-lista (a telepítő szállítja)
├── README.md                    - Magyar README (alapértelmezett)
└── README.en.md                 - Angol README
```

**A futásidejű felhasználói mappa** (nem a forrásfa része, a program hozza
létre) - ide megy MINDEN írás:

```
%AppData%\MidiBill3\
├── Kezdet.json                  - induló/kilépéskori beállítások
├── HibaNaplo.txt                - globális hibanapló
├── Presetek\                    - mentett preset fájlok (.json)
└── SF adatok\                   - importált hangszerlisták (.lst / .drm)
```

## Elkészült funkciók

**Alapfunkciók**: 16 MIDI csatorna (MIND szerkeszthető), csatorna-választó,
Split Point, Globális Transzponálás, csatornánkénti oktáv-transzponálás
(±2, azonnali/retroaktív hatással), globális Reverb (CC91), pedál-inverzió
(passzív "mód"-kapcsoló), "Csend legyen!" pánikgomb, "Kilépés"/"Kilépés
mentés nélkül", MIDI eszközlista frissítés+validálás, olvasható MIDI
üzenet-kijelzés (max. 10x/mp), hibatűrő MIDI-küldés, robusztus JSON
hibakezelés. (Részletek: forráskód vagy korábbi fejlesztési történet.)

**Fájlkezelés**: JSON mentés/betöltés, eszköznév-alapú MIDI be-/kimenet
tárolás, régi `.mb3` import, `Kezdet.json` automatikus be-/kimentés,
"Segédprogramok" menü (Preset fájlok frissítése, Régi .mb3 fájlok
konvertálása).

**Bank Select-alapú kiterjesztett hangszertámogatás (GS/XG-kompatibilis)**:
`MidiEngine.ProgramValtas()` mindig küld Bank Select (CC0+CC32) + Program
Change-t, `.lst` formátum `bank.program=Név` szintaktikával, `HangszerPanel`
`Név (B<bank>)` megjelenítéssel. (Részletek: korábbi fejlesztési történet.)

**10. csatorna feloldása + dedikált dobkészlet-hangszerlista (.lst/.drm párosítás)**:
`HangszerLista.DobNevek` önálló Dictionary a 10. csatornához,
`HangszerPanel.HangszerListaForrasBeallitasa()` a panel forrás-cseréjéhez,
a `.drm` fájlnév mindig az aktív `.lst` alapnevéből származik és
automatikusan párosodik. A 10. csatorna teljesen szerkeszthető.

**SoundFont-import: bővíthető dobkészlet-bank-felismerés**
- `SoundFontOlvaso.ismertDobBankok() = {120, 127, 128}` - ELŐSZÖR csak a
  `128`-at ismertük (SF2-szerkesztői konvenció, pl. JJazzLab), majd a
  `120`-at (GM2 szabvány), most a `127`-et is (`SGM-v2_02` SoundFont
  tesztelése közben derült ki: `"GS STANDARD"`, `"GS ROOM"`, `"GS
  TR-909"` stb. nevű dobkészlet-presetek ebben a bankban voltak) - a
  bővíthető tömb-architektúra miatt mindegyik csak **egysoros**
  módosítás volt

**"Alaphelyzetbe állítás" menüpont**
- Új top-level **"Beállítások"** menü a Fájl után, benne az
  **"Alaphelyzetbe állítás"** menüponttal
- Megerősítő kérdés: *"Minden jelenlegi beállítás törlődni fog!
  Folytatja?"*
- A visszaállítás **pontosan úgy** viselkedik, mintha a program a
  `Kezdet.json` beolvasása **NÉLKÜL** indulna:
  - Hangszerlista → **`GM.lst`/`GM.drm`** (elveti az esetleg betöltött
    egyedi/SoundFont-listát)
  - Csatornák → beépített alapérték (csak az 1. csatorna aktív)
  - Split pont → `60` (C4), Transzponálás → `0`, Reverb → `0%`, Pedál
    → nem invertált, "MIDI beállító üzenetek küldése" checkbox →
    kikapcsolva
- **A MIDI In/Out kiválasztás SZÁNDÉKOSAN változatlan marad** (ez
  eszközkapcsolat, nem "beállítás" a felhasználó szemszögéből - külön,
  tudatos döntés volt)
- **Nem törli** a `Kezdet.json` fájlt a lemezről - normál kilépéskor az
  alaphelyzetbe állított állapot felülírja majd, ha a felhasználó nem a
  "Kilépés mentés nélkül" opciót választja
- Teljes egészében **meglévő metódusok újrahasznosításával** valósult
  meg (`HangszerListaBetoltese`, `CsatornaAllapotokInicializalasa`,
  `TranszponalasFrissites`, `CsatornaPanelekFrissitese`,
  `OsszesCsatornaUjrakuldese`, `CimsorFrissitese`) - nincs új, önálló
  logika, `AlaphelyzetbeAllitas()` metódus csak összefűzi ezeket

**Csatornánkénti Chorus (v1.9.0)**
- Új csúszka a `HangszerPanel`-en (0-127, MIDI **CC93**) - pontosan az
  Oktáv-csúszka bevált mintáját követve: `CsatornaBeallitas.Chorus`
  mező, `HangszerPanel.Chorus` property + `ChorusValtozott` esemény,
  mentés/betöltés/tartomány-validáció (`Korlatoz(cs.Chorus, 0, 127)`,
  bekapcsolva az `ertekKorrigalva` figyelmeztető rendszerbe),
  `OsszesCsatornaUjrakuldese`-be is bekötve
- `HangszerPanel` magassága 378, keret 408 (a Chorus sor beillesztése
  miatt nőtt); teljes form-elrendezés konzisztensen igazítva
- **Design-frissítés**: a jobb oldali sáv átrendezve - a Reverb doboz
  feljebb került, közvetlenül az Alap hangerő mellé; a Transzponálás
  doboz és a Csend legyen!/Kilépés gombok között akkor még
  szándékosan üresen hagyott hely ekkor még egy jövőbeli funkciónak
  volt fenntartva - ezt azóta betöltötte az "Extra MIDI üzenetek"
  funkció (lásd lentebb)
- **Ablakszélesség-igazítás**: a második hangszerpanel és a jobb oldali
  vezérlő-oszlop jobbra tolva (a bal margó változatlan maradt), a form
  szélessége 942-ről 958 pixelre nőtt - közel négyzetes (958 x 974)
  arány, szimmetrikusabb elrendezés a magasabbá vált panelek mellett
- **Alap hangerő / Reverb feliratok**: az "Alap hangerő" felirat is
  kiírja a százalékjelet (`"Alap hangerő: 70%"`), a "Reverb" felirat
  pedig "Visszhang"-ra cserélve mindhárom előfordulási helyén
- **Split pont visszajelzés bővítve**: a `"Beállítva: <hangnév>"`
  üzenet eddig csak a "Tanulás" gombbal történő split pont
  beállításnál jelent meg a `lblSplitInfo` címkén - mostantól a
  `cmbSplitPont` legördülőből történő közvetlen kiválasztás is
  ugyanezt az üzenetet írja ki

**Extra MIDI üzenetek (v1.10.0)**
- Új `grpExtraMidi` GroupBox a jobb oszlopban ("Extra MIDI üzenetek"),
  pontosan azon a helyen, amit a Chorus-funkció (v1.9.0) idején
  szándékosan szabadon hagytunk - most felhasználva
- Két gyermek-vezérlő: `chkMidiBeallitoUzenetek` checkbox ("MIDI
  beállító üzenetek küldése") és `lblExtraMidiAllapot` állapotjelző
  felirat (nyugalmi állapotban: "Ha bekapcsolt, az aktív csatorna
  beállítások kiküldésre kerülnek")
- Működés: bepipálás → felirat "Várakozás billentyűre..."; az első
  ténylegesen lejátszott hangnál (Note On) a `MidiUzenetFeldolgozasa`
  meghívja az új `AktivCsatornakAllapotanakKuldese()` metódust, ami
  végigmegy a 16 csatornán, és csak az AKTÍV csatornákra újraküldi a
  Program Change, Volume, Pan, Vibrato, Chorus és Reverb üzeneteket;
  ezután a checkbox kódból kikapcsol, a felirat pedig "MIDI üzenetek
  kiküldve."-re vált - mindez EGYETLEN alkalommal fut le bepipálásonként
  (nincs folyamatos újraküldés játék közben)
- Ha a checkbox-ot a felhasználó manuálisan kapcsolja ki (mielőtt
  billentyűt ütne), a felirat visszaáll a nyugalmi (útmutató) szövegre
  (`chkMidiBeallitoUzenetek_CheckedChanged` eseménykezelő)
- Célja: egy külső programmal megvalósított MIDI-felvételhez szükséges,
  hogy a rögzített fájl tartalmazza a teljes csatorna-beállítást (pl.
  hangszíneket), ne csak a ténylegesen lejátszott hangokat
- Az "Alaphelyzetbe állítás" is kikapcsolt állapotba állítja ezt a
  checkbox-ot
- **Diagnosztizált Cantabile-jelenség**: MIDI-fájl-szintű vizsgálattal
  (a felhasználó két felvett `.mid` fájlját elemezve) igazolást nyert,
  hogy a funkció HELYESEN küldi ki a teljes csatorna-állapotot (Bank
  Select + Program Change + Volume + Pan + Vibrato + Chorus + Reverb,
  csatornánként), DE ha ez a felvétel/munkamenet szó szerint első
  MIDI-eseménye, a Cantabile (külső DAW/host) elveszítheti - ez egy,
  a Cantabile "bemelegedési" időszakával kapcsolatos jelenség, NEM a
  MidiBill3 kód hibája. Gyakorlati munkafolyamat-javaslat: a felhasználó
  játsszon le 1-2 "bemelegítő" hangot a Cantabile felvétel indítása
  után, és csak utána pipálja be a checkboxot - ez már megbízhatóan
  működik (MIDI-szinten ellenőrizve)
- Egy ideiglenes teszt (`TesztNoteOffKuldese`/`NoteOffKuldes`, ártalmatlan
  Note Off küldése a beállító csomag előtt) NEM hozott javulást,
  eltávolításra került - a végleges válasz a fenti "bemelegítő hang"
  munkafolyamat

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
- `.lst`/`.drm` párosítás fájlnév-alapú, nem preset-be mentett mező
- Dobkészlet-bankok bővíthető listája (`ismertDobBankok`) - eddig
  háromszor bővült ({128} → {120,128} → {120,127,128}), valószínűleg
  a jövőben is bővülhet még, ha újabb SoundFont-konvencióval
  találkozunk
- "Alaphelyzetbe állítás" a MIDI In/Out kiválasztást SZÁNDÉKOSAN nem
  érinti
- Az "Extra MIDI üzenetek" funkció csak az AKTÍV csatornákra küld
  üzenetet, és csak EGYETLEN alkalommal, bepipálásonként - ez tudatos
  döntés volt, hogy ne terhelje feleslegesen a MIDI kimenetet
  folyamatos játék közben
- A Reverb/Chorus TÍPUSA (pl. Hall/Room/Plate, Chorus/Flanger) nem
  szabványos General MIDI (GM1) CC - csak a "mennyiséget" (CC91/CC93
  send level) fedi a GM1. A típusválasztáshoz gyártó-specifikus SysEx
  kell (GM2 Global Parameter Control, Roland GS Reverb/Chorus Macro,
  Yamaha XG) - mivel a felhasználó szintetizátora GS-kompatibilis, a
  jövőbeli megvalósítás iránya a Roland GS SysEx (`F0 41 <dev> 42 12
  40 01 30 <érték> <checksum> F7` Reverb Macro-hoz, `40 01 38` Chorus
  Macro-hoz) - FONTOS: ez GLOBÁLIS/rendszerszintű paraméter GS-ben,
  NEM csatornánkénti, eltérően a Chorus-intenzitástól, ami csatornánkénti

## Globális hibanaplózás

Az `ApplicationEvents.vb` elkap minden kezeletlen kivételt, `HibaNaplo.txt`-be
ír. Natív (nem managed) összeomlásokat ez sem tud elkapni.

## Kódátnézés (2026-09-03)

A MIDI felvétel/visszajátszás kódolásának megkezdése ELŐTT teljes
kódátnézés készült (MainForm, MidiEngine, HangszerPanel, Beallitasok,
HangszerLista, SoundFontOlvaso, ApplicationEvents, .vbproj). Az
összkép: a kód konzisztens, jól kommentelt, a felelősségek tisztán
szét vannak választva, és a szálkezelés is helyes (a MIDI bemenet a UI
szálra kerül át, így a `MidiEngine` állapotát egyetlen szál módosítja).
Az alábbi megállapítás-lista született, súlyosság szerint - ez egyben a
munkalista is a további javításokhoz.

### JAVÍTVA (2026-09-03) - a három valódi hiba

1. **Friss indításkor nem szólalt meg semmi `Kezdet.json` nélkül**
   (`MainForm.KezdetBeallitasokBetoltese`). Az egyetlen hely, ami a
   `csatornaAllapotok` tömböt a `MidiEngine`-be szinkronizálja, az
   `OsszesCsatornaUjrakuldese()` - ez viszont a "nincs fájl" ágban soha
   nem futott le (a `cmbMidiOut_SelectedIndexChanged`-ből induló hívása
   pedig még `inicializalasKesz = False` mellett tér vissza). Emiatt a
   motor mind a 16 csatornát inaktívnak látta → néma program. Javítás:
   `OsszesCsatornaUjrakuldese()` hívása a "nincs fájl" ágban ÉS a
   `Catch` ágban is.
2. **A státuszbájt csatorna-nibble-je nem volt kimaszkolva** - minden
   típusvizsgálat csak az 1. MIDI csatornán működött, egy 2. csatornára
   állított billentyűzetnél némán kimaradt a pedál-inverzió, a split
   pont tanulás, az Extra MIDI üzenetek, a split zóna, a globális
   transzponálás és a csatornánkénti oktáv-eltolás (plusz beragadó
   hangok, mert a hangok nem kerültek be a `noteOn` nyilvántartásba).
   Javítás: `uzenetTipus = statuszBajt And &HF0` bevezetése a
   `MainForm.MidiUzenetFeldolgozasa`-ban és a `MidiEngine.UzenetKuldes`-ben,
   plusz a `HangUzenetE()` is maszkol már magában.
3. **Kilépéskori befagyás (deadlock) kockázata**: a MIDI callback-szál
   szinkron `Invoke`-ja és a `FormClosing`-ban futó MIDI-bemenet-lezárás
   kölcsönösen egymásra várhatott. Javítás: `BeginInvoke` (aszinkron, de
   sorrendtartó - a leütésenkénti UI-késleltetést is megszünteti), új
   `zarasFolyamatban` jelző, és átrendezett lezárási sorrend (jelző →
   `BemenetBezaras()` → `midiKijelzoIdozito.Stop()` → hangok leállítása
   → mentés → `Dispose`). **FIGYELEM**: amikor a MIDI felvevő ablak
   "van aktív felvétel, leállítsuk?" kérdése bekerül a `FormClosing`-ba,
   annak az `e.Cancel` ellenőrzésnek az EGÉSZ takarítás ELÉ kell
   kerülnie - különben megszakított kilépés után a MIDI bemenet lezárva
   maradna. (Erre a kódban is került figyelmeztető megjegyzés.)

### MÉG NYITOTT - érdemes javítani

4. ~~Írható-e a program mappája?~~ - **JAVÍTVA (2026-09-03)**. Korábban
   MINDEN felhasználói adat (`Kezdet.json`, `HibaNaplo.txt`, a preset
   fájlok és a hangszerlisták) a program mappájába, azon belül egy közös
   `Adatok` mappába került. A tervezett Inno Setup telepítő után
   (`C:\Program Files\...`) ezek normál felhasználóként CSENDBEN
   elhasaltak volna. Megoldás - lásd az ÚJ `Modellek\Utvonalak.vb`
   modult, ami minden útvonalat egyetlen helyen tart. Az új, tiszta
   szétválasztás:

   **GYÁRI tartalom** (a program mappájában, CSAK OLVASHATÓ - ezt
   szállítja a telepítő, ide a program soha nem ír):
   - `GM.lst`, `GM.drm` - a beépített General MIDI hangszer- és
     dobkészlet-lista, **közvetlenül a program mappájában**. NINCS többé
     `Adatok` almappa sem a forrásfában, sem a kimenetben.

   **FELHASZNÁLÓI tartalom** (`%AppData%\MidiBill3`, ÍRHATÓ - minden
   írás ide megy):
   - `Kezdet.json` - az induló/kilépéskori beállítások
   - `HibaNaplo.txt` - a globális hibanapló
   - `Presetek\` - a mentett preset fájlok (`.json`)
   - `SF adatok\` - a SoundFont-ból importált vagy kézzel betöltött
     hangszerlisták (`.lst` / `.drm`)

   **A GM.lst / GM.drm védett**: mindig a gyári mappából olvasódnak
   (`Utvonalak.HangszerListaOlvasasra`), akkor is, ha a felhasználói
   mappában lenne ilyen nevű fájl - ÉS a program meg is akadályozza,
   hogy ilyen néven jöjjön létre felhasználói fájl (új
   `MainForm.GyariNevUtkozes()`, ami a hangszerlista-betöltésnél és a
   SoundFont-importnál is fut, pl. egy "GM.sf2" nevű SoundFont esetén).
   Így nem keletkezhet olyan árva fájl, amit a program sosem használna.

   **Egyszeri, automatikus átköltöztetés** (`Utvonalak.Elokeszites`, a
   `MainForm_Load` LEGELSŐ hívása) - mindkettő csak akkor, ha az új
   helyen még nincs meg a fájl, tehát ismételt indításnál már nem
   csinál semmit és sosem ír felül újabb felhasználói adatot:
   - a program mappájában maradt `Kezdet.json`,
   - a régi, közös `Adatok` mappa tartalma szétosztva: `.json` a
     `Presetek`, `.lst`/`.drm` az `SF adatok` mappába (a gyári
     `GM.lst`/`GM.drm`, illetve minden más kiterjesztés - pl. a
     fejlesztéshez használt `.sf2` tesztfájl - kihagyásával).
   A régi `Adatok` mappát SZÁNDÉKOSAN nem törli, maradjon biztonsági
   másolatnak.

   További részletek:
   - A fájlválasztó dialógusok alapmappája a tartalom szerint igazodik:
     a preset-műveleteknél a `Presetek`, a hangszerlista- és
     SoundFont-importnál az `SF adatok` mappa.
   - A konvertáló segédprogramok (`Konvertalva` almappa a forrásfájlok
     mellett) sem szakadnak meg többé kezeletlen hibával írásvédett
     forrásmappánál: az új `KonvertalasKimenetiMappa()` ilyenkor a
     felhasználói mappába esik vissza.
   - A `HibaNaplo.txt` írása előtt a napló maga is létrehozza a
     felhasználói mappát, mert az `ApplicationEvents` kód lefuthat a
     `MainForm_Load` ELŐTT is.
   - A `.vbproj` már csak a `GM.lst`-et és a `GM.drm`-et másolja a
     kimeneti mappába (korábban az `Adatok\**\*.*` mintával MINDENT,
     így egy telepítő a felhasználó személyes presetjeit is szállította
     volna).
   - Mellékhaszon fejlesztés közben: a `Kezdet.json` és a presetek
     kikerültek a `bin\Debug` mappából, tehát **túlélik a
     Clean/Rebuild műveleteket**.
5. **MIDI kimenet váltásakor beragadhatnak a hangok** - a
   `MidiEngine.KimenetBezaras()` a `Dispose` előtt nem küld
   hangkikapcsolást, így ami a régi eszközön szólt, ott is marad.
6. **`BeallitasokAlkalmazasa`**: a `TranszponalasFrissites(b.Transzponalas)`
   a nyers értéket kapja a kiszámolt `ujTranszponalas` helyett. Működik
   (a metódus maga is korlátoz), de kilóg a körülötte lévő mintából.
7. **`Beallitasok.Betoltes`**: ha a JSON tartalma literálisan `null`, a
   `Deserialize` `Nothing`-ot ad vissza, és a `Kiegeszit16Csatornara`
   NullReference-t dob - a felhasználó értelmetlen angol üzenetet lát a
   szép magyar hibaüzenet helyett.
8. ~~`midiKijelzoIdozito` nincs leállítva a `FormClosing`-ban~~ -
   **JAVÍTVA** a 3. ponttal együtt (ugyanabba a lezárási sorrendbe
   tartozik).

### MÉG NYITOTT - optimalizálás

9. **Fölösleges string-építés minden MIDI üzenetnél**: a
   `MidiUzenetFeldolgozasa` MINDEN beérkező üzenetnél meghívja a
   `MidiUzenetSzoveg()`-et (string-összefűzés + dictionary-keresés),
   pedig a kijelzés amúgy is csak 10x/mp frissül. Jobb: a 3 nyers
   `Integer` eltárolása, és a szöveg előállítása csak a `Tick`-ben - így
   teljesül igazán a throttling eredeti szándéka.
10. **Csúszkák MIDI-elárasztása**: a `trkReverb_ValueChanged` és a
    `trkAlapHangero_ValueChanged` húzás közben MINDEN tick-nél 16
    üzenetet küld. Ugyanaz a throttling-minta alkalmazható, mint a
    kijelzésnél (vagy csak a húzás végén küldeni).
11. **A pánik-söprés 2048 üzenet**: a `HangokKikapcsolasa(c, True)` mind
    a 16 csatornára 128 Note Off-ot küld. Valódi hardveres MIDI porton
    (31250 baud) ez ~2 másodperc blokkolás, és ez fut a `FormClosing`-ban
    is, tehát a kilépést is lassítja. Alternatíva: először CC120 (All
    Sound Off) + CC123 (All Notes Off) csatornánként (32 üzenet), és a
    teljes söprés csak tartalékként.
12. **A FELVÉTEL SZEMPONTJÁBÓL FONTOS**: mivel a bejövő MIDI a UI
    üzenetsoron megy át, a felvétel időbélyegei annyi jittert
    örökölnek, amennyit a UI szál terheltsége okoz. A "Következő
    lépések" 6. pontja (MIDI-küldés leválasztása a UI-szálról) eddig nem
    volt sürgős - a felvétel funkcióval viszont felértékelődik.
    Minimum: az időbélyeget már a callback-szálon rögzíteni
    (`Stopwatch`), és csak a feldolgozást marshalolni.

### MÉG NYITOTT - kód-higiénia

13. ~~`Option Strict` nincs bekapcsolva~~ - **JAVÍTVA (2026-09-03)**.
    A bekapcsolás előtt teljes statikus átvizsgálás készült a tipikus
    szabálysértés-kategóriákra, és **egyetlen biztos szabálysértés sem
    volt** a kódban - vagyis a projekt már eddig is "Option Strict On
    stílusban" íródott:
    - szűkítő konverziók: mindenhol egész osztás (`\`) szerepel, az
      egyetlen lebegőpontos osztásnál pedig már ki volt írva a `CInt`
    - késői kötés: nulla eset; a `sender As Object` paramétereken
      mindenhol `TryCast`/`CType` előzi meg a tagelérést, a
      `.ToString()` hívások pedig az `Object`-en deklarált tagra
      mennek (korai kötés)
    - string-összefűzés: végig `&` operátorral (ez `Option Strict On`
      mellett is megengedi a vegyes típusú operandusokat), soha nem `+`
    - vegyes előjeles/előjel nélküli aritmetika (`SoundFontOlvaso`):
      minden konverzió BŐVÍTŐ (`Integer + UInteger → Long`,
      `UShort → Integer`), a szűkítőnél már ki volt írva a `CInt`
    A `.vbproj` `PropertyGroup`-jába bekerült az `OptionStrict`,
    `OptionInfer` és `OptionExplicit` = `On`. Az **`OptionInfer` azért
    van kiírva külön** (nem az alapértelmezettre hagyva), mert a kód
    több helyen a lambda-paraméterek típuskövetkeztetésére támaszkodik
    (pl. `OrderBy(Function(k) k)` a `MainForm`-ban, a `HangszerLista`-ban
    és a `HangszerPanel`-ben) - `Infer = Off` mellett ezek `Object`-té
    válnának, ami `Option Strict On` mellett fordítási hiba lenne.
    A haszon tehát nem a meglévő kód megjavítása volt, hanem hogy
    **innentől a fordító tartja is ezt a szintet** - különösen a most
    következő MIDI felvétel/visszajátszás kódjánál, ahol a NAudio
    `MidiEvent`-hierarchiájában (`NoteOnEvent`, `ControlChangeEvent`,
    `TempoEvent` stb.) sok lesz a típuskonverzió.
14. **`AppInfo.vb` kommentje elavult** - `Directory.Build.props`-ra és a
    már nem létező `PresetKonvertalo`/`Mb3Konvertalo` projektekre
    hivatkozik, holott a verzió most közvetlenül a `.vbproj`-ban van, és
    egyetlen projekt van.
15. **Apróságok**: a `.vbproj` `System.Data` importja használatlan; a
    `Hangnevek.HangNev` negatív bemenetre `IndexOutOfRange`-et dobna
    (védekező egysoros); a JSON-kommentelés a 10. csatorna hangszerénél a
    dallamhangszer-listából keresi a nevet (kozmetikai); a
    SoundFont-import a 120-as (GM2) dobbankot a program-számra
    redukálja, így a bank elveszik a Program Change-nél (ez tudatos
    döntés volt, csak jelezve).

## Következő lépések / nyitott témák

1. **GS-támogatás további tesztelése** - folyamatban, a felhasználó
   több különböző SoundFont-tal teszteli (JJazzLab, SGM-v2_02 eddig)

2. **10. csatorna működésének további tesztelése** - konkrétan, hogy a
   Bank Select + Program Change mechanizmus a dobkészlet-választásnál
   minden esetben helyesen viselkedik-e a felhasználó szintetizátorán -
   MÉG NINCS végleges visszajelzés erről

3. **Metronóm-funkció** - a 10. csatorna feloldásának egyik indoka volt;
   konkrét ütős hangok Note On üzenetként való kiküldése, FÜGGETLENÜL a
   dobkészlet-választástól. A Tempó (BPM), ütem és metronóm vezérlők a
   `MidiFelvevoForm` (lásd 4. pont) ablakában fognak megjelenni, NEM a
   MainForm-on és nem önálló ablakban - a metronóm-funkció és a MIDI
   felvétel/visszajátszás funkció felülete egyesül. A felhasználóval
   megkezdtük a RÉSZLETES, témánkénti tervezést (a felvétel/lejátszás
   funkcióhoz hasonlóan) - eddig lezárt döntések:

   **Hangzás (lezárva)**
   - Az ütem ELSŐ (hangsúlyos) ütése MEGKÜLÖNBÖZTETVE szól a többi
     ütéstől - külön hangszín ÉS külön hangerő az első ütésre, mint a
     többire
   - A metronóm-hang MINDIG a 10. csatornán (dob-csatorna) megy ki,
     FÜGGETLENÜL attól, hogy a 10. csatorna a felhasználó beállításai
     szerint éppen aktív-e vagy sem - ez tudatos döntés, a metronóm
     ettől függetlenül működik
   - NEM probléma, ha a metronóm-hang keveredik a felhasználó által a
     10. csatornán élőben játszott dobhangokkal - nincs kizárás/
     elkülönítés ebben az irányban
   - **Hangszín-választás**: KÉT ComboBox (első ütés / többi ütés
     hangszíne), mindkettő UGYANAZT a már létező, betöltött 10.
     csatorna dobkészlet-hangszerlistát (`HangszerLista.DobNevek`, a
     `.drm` fájlból, amit a `HangszerPanel` is használ) listázza - ez
     a döntés (nem egy külön, fix "tipikus metronóm-hangok" lista)
     azért esett, mert így konzisztens marad a program többi
     részével, és a felhasználó szintetizátorán/SoundFontján ténylegesen
     elérhető hangok jelennek meg. **VÉGLEGES alapértelmezett érték**
     (a felhasználó jóváhagyta a vázlaton szereplő értékeket): első
     ütés = **Claves** (GM 75), többi ütés = **Side Stick** (GM 37) -
     ha léteznek a betöltött `.drm`-ben (ha nem, akkor a lista első
     eleme legyen az alapérték, mint fallback)
   - **Hangerő-választás**: KÉT csúszka (első ütés / többi ütés
     hangereje), SZÁZALÉKBAN megjelenítve (a program más hangerő-
     csúszkáihoz hasonlóan, pl. "Alap hangerő: 70%" minta). **VÉGLEGES
     alapértelmezett érték**: első ütés hangereje = **90%**, többi
     ütés hangereje = **65%** (a hangsúlyos ütés érezhetően hangosabb,
     mint a többi) - a felhasználó jóváhagyta a vázlaton szereplő
     értékeket alapértéknek

   **Tempó (BPM) és ütem (lezárva)**
   - **BPM**: `NumericUpDown`, tartomány 30-300, alapérték 120
   - **Ütem**: KÉT külön, szabadon állítható vezérlő - NEM legördülő
     lista előre definiált ütemekkel, hanem szabad számláló/nevező
     párosítás:
     - **Számláló**: `NumericUpDown`, tartomány 1-16, alapérték 4
       (ez tágabb, mint egy egyszerű 2-8-as tartomány, hogy beleférjen
       pl. a 9/8, 12/8 is)
     - **Nevező**: `ComboBox`, CSAK a zeneileg értelmes {2, 4, 8, 16}
       értékekkel, alapérték 4 - technikai okból NEM `NumericUpDown`,
       mert annak a lépésköze (`Increment`) állandó kellene legyen,
       a nevező viszont csak 2-hatványokra korlátozódik (nem
       egyenletes lépésköz) - erre a `ComboBox` a természetesebb
       vezérlő

   **Start/Stop viselkedés (lezárva)**
   - A metronómnak SAJÁT, önálló Start/Stop gombja van (nem osztja meg
     a felvétel Start/Stop gombjával)
   - A gombokon NINCS szükség színváltásra (eltérően a Felvétel/
     Lejátszás gomboktól)
   - **"Metronóm indítása felvételnél" checkbox**: **VÉGLEGES
     alapértelmezett állapot: BEKAPCSOLVA** (a felhasználó jóváhagyta a
     vázlaton szereplő állapotot alapértéknek). Ha BE van pipálva,
     a felvétel Start gombjának megnyomásakor a metronóm IS
     automatikusan elindul; a felvétel Stop gombjának megnyomásakor a
     metronóm IS automatikusan leáll vele együtt (szimmetrikus
     viselkedés - mindkét irányban automatikus, ha a checkbox be van
     kapcsolva). Ha a checkbox KI van kapcsolva, a metronóm indítása/
     leállítása teljesen független a felvételtől, csak a saját
     Start/Stop gombjával vezérelhető
   - **Vizuális visszajelzés (működő metronóm) - LEZÁRVA**: minimál
     design-os, kicsi piktogram a formon - egy klasszikus metronóm-
     házat sematikusan ábrázoló háromszög-körvonal, benne egy mutató
     (needle), ami MINDEN ütésre átvált a másik állapotba (pl. balra
     döntött ↔ jobbra döntött) - NEM folyamatosan lengő animáció, hanem
     egy DISZKRÉT, azonnali állapotváltás ütésenként (ez felel meg a
     "minimál design" elvárásnak, és egyszerűbb is megvalósítani)
   - Megvalósítási ötlet: `PictureBox` (vagy közvetlenül a Form-ra
     rajzolt terület) `Paint` eseményében rajzolva (GDI+, néhány
     vonal/háromszög), egy `Timer` a BPM-ből számított ütésközzel
     (`60000 / BPM` ms) váltogatja egy `Boolean` állapotváltozó
     értékét (pl. `mutatoJobbra`), és minden váltáskor `Invalidate()`-
     tel újrarajzoltatja a piktogramot az új szöggel - nincs szükség
     bonyolult animációs rendszerre, csak egy egyszerű két-állapotú
     kapcsolgatásra

   **Metronóm-hangok kizárása a felvételből (lezárva)**
   - A metronóm IS a meglévő `KuldesEgyCsatornara()` metóduson megy ki
     (mint minden más MIDI üzenet - így a program normál MIDI-
     életciklusán megy át, pl. a "Csend legyen!" pánikgomb is
     elnémítja, ha szükséges), DE egy ÚJ, extra `metronomBol As
     Boolean = False` paramétert kap - az összes jelenlegi, élő hívás
     változatlan marad (alapértelmezett `False`), a metronóm-küldő
     metódus `True`-val hívja
   - Az `UzenetKikuldve` esemény (amit a lejátszás/overdub tervezésekor
     már bevezettünk `lejatszasBol` paraméterrel) egy TOVÁBBI
     `metronomBol As Boolean` paramétert is kap: `Public Event
     UzenetKikuldve(statuszBajt, adat1, adat2, lejatszasBol,
     metronomBol)`
   - A felvevő az esemény kezelésekor: ha `metronomBol = True` →
     MINDIG kihagyja a rögzítésből (nincs hozzá kapcsolódó checkbox,
     ellentétben a lejátszás overdub-jával - a metronóm-hang SOHA nem
     kerülhet bele a mentett `.mid` fájlba)
   - Ez a megoldás konzisztens a lejátszás-kizárás (`lejatszasBol`)
     mechanizmusával, ugyanazt a mintát követi, nem igényel új,
     különálló küldési útvonalat

4. **MIDI felvétel/visszajátszás** - RÉSZLETESEN ÁTBESZÉLVE, tervezés
   LEZÁRVA, a kódolás még nem kezdődött el (a felhasználó egy
   szünet után tér vissza a megvalósításhoz). A teljes, végleges terv:

   **Alapkoncepció**
   - A TÉNYLEGES kimenetet vesszük fel (a már csatornánként szétosztott,
     split/transzponálás/effektek alkalmazása UTÁNI állapotot) - ez egy
     KORÁBBI TERVET FELÜLÍR (a régi terv a nyers bemenetet vette volna
     fel, és `MidiEngine.UzenetKuldes()`-en át játszotta volna vissza -
     ez a döntés ÉRVÉNYTELEN, az új irány a lenti)
   - Szabványos `.mid` fájl az EGYETLEN formátum (nincs külön belső/JSON
     formátum) - NAudio-val írva (`MidiEventCollection` + `MidiFile.Export()`)
     és olvasva; ez egyszerre szolgálja a program saját visszajátszását
     ÉS a külső szoftveres/hardveres szintiken való lejátszhatóságot
   - Méretbecslés: egy `.mid` fájl bináris tömörsége miatt (kb. 3-5
     bájt/esemény) egy 10 perces, akár sűrű játékú felvétel is tipikusan
     40-200 KB körül marad - nem probléma

   **Fájlkezelés**
   - Dedikált `Felvetelek` mappa (az `Adatok` mellett) - MINDEN felvétel
     (kézi Stop-tal mentett ÉS az Automatikus felvétel mód
     szegmensenkénti mentései) ide, illetve az alábbi almappa-logika
     szerint ide kerül - NINCS `SaveFileDialog`, a mentés automatikus,
     nem kér külön útvonalat mentéskor
   - NINCS beépített könyvtárkezelő/lista a programban - a Lejátszáshoz
     sima `OpenFileDialog` szolgál, alapból a `Felvetelek` mappára
     nyitva (almappástól is bejárható)
   - **"Mentett fájl neve" textbox** a felvevő ablakban - a mentett
     fájlok ALAPNEVÉT adja meg, alapértelmezett érték: `"Felvetel"`
   - **"Almappák dátum szerint" checkbox** (alapból BEKAPCSOLVA) -
     szabályozza, hogy a fájlnév-képzés almappákba rendezve vagy
     laposan történjen:
     - BEKAPCSOLVA (alapértelmezett): a fájl a
       `Felvetelek\ÉÉÉÉ-HH-NN\` dátum szerinti almappába kerül, a
       fájlnévhez pedig CSAK az időbélyeg (óra:perc:másodperc) kerül
       hozzáfűzve, mivel a dátum már az almappa nevében szerepel -
       pl. `Felvetelek\2026-08-30\Felvetel_143025.mid`
     - KIKAPCSOLVA: a fájl közvetlenül a `Felvetelek` mappába kerül,
       laposan, a fájlnévhez a TELJES dátum ÉS időbélyeg (óra, perc,
       MÁSODPERC is) hozzáfűzve - pl.
       `Felvetelek\Felvetel_2026-08-30_143025.mid`
   - A másodperc-pontosságú időbélyeg SZÁNDÉKOSAN kell (nemcsak
     óra:perc) - az Automatikus felvétel mód percenként akár több
     szegmenst is menthet (rövid szünet-küszöbök esetén), a
     másodperc nélkül fájlnév-ütközés (felülírás) történhetne

   **Felület: külön `MidiFelvevoForm` ablak**
   - Nem-modális (`Show()`, nem `ShowDialog()`) - a Main formon lehet
     tovább dolgozni/játszani, amíg nyitva van
   - A `MainForm`-on EGY gomb nyitja meg: `btnMidiFelvetel` ("MIDI
     felvétel indítása"), MÁR HOZZÁADVA a `MainForm.Designer.vb`-hez
     (Location 799,411; Size 134x50, a zsugorított `btnMidiFrissites`
     134x50 mellett) - egyelőre csak UI-váz, eseménykezelő NINCS még
     hozzá írva
   - Kilépéskor (`MainForm` bezárásakor): ha a felvevő ablak nyitva van
     és NINCS aktív felvétel → automatikusan bezáródik kérdés nélkül;
     ha VAN aktív felvétel → megerősítő kérdés ("Aktív MIDI felvétel van
     folyamatban. Leállítsuk és bezárjuk az ablakokat?"), csak Igen
     esetén áll le (menti a felvételt) és zár be mindent, Nem esetén a
     kilépés teljesen megszakad
   - **ÚJ, KÉSŐBBI kiegészítés (NEM az első kör része)**: a
     `MidiFelvevoForm` a jövőben a Tempó (BPM), ütem és metronóm
     vezérlőket IS tartalmazni fogja (lásd fent, 3. pont) - ezt a
     felhasználó explicit módon egy KÉSŐBBI fejlesztési körnek szánja,
     miután a sima felvétel/visszajátszás elkészült. A metronóm
     hangjainak a felvételből való KIZÁRÁSA a fő technikai
     kihívás ennél a résznél, ennek megoldási módját még nem
     beszéltük át

   **Felvétel indítása/vezérlése**
   - Start ÉS Stop gomb is van a felvevő ablakban
   - Start = "felfegyverzés" (az Extra MIDI üzenetek checkbox mintájára):
     nem indul azonnal, hanem várja az első ténylegesen kiküldött
     üzenetet
   - **Elrendezés (véglegesítve)**: NINCS külön "Automatikus felvétel"
     groupbox - minden a közös "Felvétel" groupbox-on belül van: felül
     a Start/Stop gombok + állapotjelző, alattuk (elválasztó vonallal)
     az "Automatikus felvétel" checkbox, alatta pedig a Játékidő-küszöb
     és Szünet-küszöb két `NumericUpDown`-ja EGY közös sorban (nem
     külön-külön sorban, ahogy egy korábbi vázlatverzióban szerepelt)
   - **"Automatikus felvétel" checkbox** (alapból BEKAPCSOLVA), mellette
     2 `NumericUpDown` paraméter, mp-ben, alapból 5-5:
     - **Játékidő-küszöb**: ha a folyamatos felvétel eléri ezt az időt,
       a felvétel "véglegesnek" számít (biztosan mentésre kerül)
     - **Szünet-küszöb**: ha ennyi ideig nem érkezik semmi (csend), a
       program lezárja az aktuális felvételt
     - Logika: ha a szünet-küszöb ELŐBB következik be, mint hogy a
       játékidő-küszöböt elérnénk → a felvétel ELDOBÓDIK (nincs fájl),
       majd a rendszer AZONNAL újra felfegyverzi magát; ha a
       játékidő-küszöböt MÁR elértük, amikor bekövetkezik a
       szünet-küszöb → a felvétel MENTÉSRE kerül (időbélyeges `.mid`
       fájl a `Felvetelek` mappába), majd a rendszer AZONNAL újra
       felfegyverzi magát a következő felvételhez - ez így folyamatosan
       megy, "session-eket daraboló" módon, amíg Stop-ot nem nyomsz
       vagy be nem zárod az ablakot
   - **Kézi Stop gomb**: bármikor azonnal lezárja ÉS MINDIG elmenti az
     addig összegyűjtött anyagot (a küszöböktől FÜGGETLENÜL, akkor is,
     ha rövidebb a játékidő-küszöbnél) - ez a felhasználó explicit
     szándéka, felülírja az automatikus eldobási szabályt. Stop után
     NINCS újra-felfegyverzés (teljesen leáll). **PONTOSÍTVA**: ha a
     kézi Stop gombot Automatikus felvétel módban nyomjuk meg, a
     mentés mellett a program a **"Automatikus felvétel" checkbox-ból
     is kiveszi a pipát** (vizuálisan is jelezve, hogy a rendszer
     "kézi" módra váltott) - egy következő Start megnyomása ETTŐL
     KEZDVE már NEM indít automatikus szegmens-sorozatot, amíg a
     felhasználó vissza nem pipálja a checkbox-ot
   - Ha az "Automatikus felvétel" KI van kapcsolva: a Start gomb
     felfegyverez, az első hangnál elindul a rögzítés, és VÉGTELEN
     ideig megy (nincs automatikus lezárás csend alapján), amíg a
     felhasználó meg nem nyomja a Stop-ot

   **Lejátszás felület**
   - A Lejátszás csoport 3 gombja (Fájl megnyitása..., Lejátszás, Stop)
     EGY sorban - a betöltött fájl neve NEM külön feliraton, hanem a
     Lejátszás groupbox CÍMSORÁBAN jelenik meg: `"Lejátszás: <fájlnév>"`
     (alapból üres, amíg nincs betöltött/kiválasztott fájl)
   - A "Fájl megnyitása..." (`OpenFileDialog`) útján kiválasztott fájl
     neve automatikusan bekerül a Lejátszás groupbox címsorába
   - **"Mentett fájl automatikus lejátszása" checkbox** (a Mentés
     csoport ALJÁN, a fájlnév-mező és az almappa-checkbox alatt) -
     **VÉGLEGES alapértelmezett állapot: BEKAPCSOLVA** (a felhasználó
     módosította a korábbi, kikapcsolt alapértéket) - ha BE van
     pipálva, MINDEN mentés után (a kézi Stop-mentés ÉS az
     Automatikus felvétel mód összes szegmens-mentése után egyaránt)
     az imént elmentett fájl neve automatikusan bekerül a Lejátszás
     groupbox címsorába - FONTOS: a checkbox neve ellenére ez CSAK a
     fájlnevet tölti be, NEM indítja el automatikusan a lejátszást -
     a Lejátszás gombot ilyenkor is kézzel kell megnyomni

   **Lejátszás vizuális visszajelzés és tekerés**
   - A **Lejátszás gomb** is színt vált, STABIL (nem villogó) színnel,
     az Extra MIDI checkbox / Felvétel Stop gomb mintájára: aktív
     lejátszás közben ZÖLD (`Button.BackColor`,
     `UseVisualStyleBackColor = False`), Stop megnyomására (vagy a
     fájl végére éréskor) visszavált az alapértelmezett (szürke)
     színre
   - A groupbox alján egy **húzható tekerősáv** (feltehetően WinForms
     `TrackBar`, vagy egyedi rajzolt csúszka, ha a `TrackBar` vizuálisan
     nem elég rugalmas) jelzi a lejátszás előrehaladását - EGÉRREL
     FOGHATÓ és húzható, így a fájlon belül bárhová lehet tekerni
     (nem csak lejátszás közben, hanem megállított állapotban is
     beállíthatja a kezdőpontot)
   - A tekerősáv mellett/alatt idő-kijelzés: `"<aktuális
     perc>:<aktuális másodperc> / <teljes hossz perc>:<teljes hossz
     másodperc>"` formátumban (pl. `"1:22 / 3:41"`), folyamatosan
     frissülő az aktuális pozícióval lejátszás közben
   - **Tekerés utáni "beragadt hang" védelem**: mivel tekeréskor
     átugorhatunk már elindított, de még le nem zárt (Note Off nélküli)
     hangokon, a program a tekerés (csúszka elengedése) UTÁN
     automatikusan elküld egy, a meglévő "Csend legyen!" panik-gombhoz
     hasonló All Notes Off/All Sound Off üzenetet minden aktív
     csatornára, hogy ne ragadjanak be végtelenítve szóló hangok - ez
     a felhasználó explicit kérése volt
   - Megvalósítási ötlet (később finomítható): a `TrackBar` értéke a
     fájl teljes hosszát (pl. ezredmásodpercben vagy MIDI tick-ben)
     reprezentálja; lejátszás közben egy `Timer` frissíti a csúszka
     pozícióját és az idő-feliratot; a felhasználói húzást (egér)
     `MouseDown`/`MouseUp` eseményekkel kell megkülönböztetni a
     programozott (Timer general) pozíció-frissítéstől, hogy ne
     alakuljon ki visszacsatolási hurok, és hogy az All Notes Off
     csak a felhasználói tekerés után fusson le, ne minden Timer-tick
     után

   **Lejátszás és élő játék keveredése (overdub)**
   - Élő játék és lejátszás EGYSZERRE szólhatnak a MIDI kimeneten
     (nincs kizárás/letiltás egyik irányban sem)
   - Új **"A lejátszott hangok rögzítése" checkbox** (alapból
     KIKAPCSOLVA), a felvevő ablakban, a Lejátszás gomb közelében:
     - Ha BEKAPCSOLVA: a lejátszott anyag IS bekerül egy éppen aktív
       felvételbe (valódi overdub/rájátszás)
     - Ha KIKAPCSOLVA: csak a felhasználó ÉLŐ tevékenysége (hangok +
       csúszka-mozgatások + pedál, MINDEN élő MIDI-kimenet) kerül a
       felvételbe, a lejátszás soha
   - Technikai megvalósítás (a legegyszerűbb működő megoldás, egyetlen
     közös küldési pont, nincs külön "bypass" útvonal a lejátszásnak):
     - A `MidiEngine.KuldesEgyCsatornara()` kap egy opcionális
       `lejatszasBol As Boolean = False` paramétert - minden élő hívás
       (jelenlegi kód) változatlan marad (alapértelmezett `False`), a
       lejátszó metódus `True`-val hívja
     - **KRITIKUS, EXPLICIT MEGERŐSÍTETT PONT**: a lejátszó metódus a
       `KuldesEgyCsatornara()`-t **KÖZVETLENÜL** hívja, a felvett `.mid`
       fájlból beolvasott üzeneteket **VÁLTOZTATÁS NÉLKÜL, egy az
       egyben** küldve - NEM megy át előtte a split pont / globális
       transzponálás / csatorna-szétosztás (fan-out) logikáján (az a
       logika a NYERS bemenet feldolgozásánál fut le, pl.
       `MidiUzenetFeldolgozasa`/`UzenetKuldes`-ben, MIELŐTT az üzenet
       eljutna a `KuldesEgyCsatornara`-ig). Ez logikus következménye
       annak, hogy a felvétel is a MÁR FELDOLGOZOTT (split/
       transzponálás/fan-out UTÁNI) kimenetet rögzíti - a lejátszásnak
       ezt a feldolgozott állapotot kell 1:1-ben visszaadnia, a
       feldolgozó logika ÚJRA-lefuttatása hibás (duplán feldolgozott)
       eredményt adna
     - **UGYANEBBŐL KÖVETKEZIK (explicit megerősítve)**: a lejátszás
       NEM veszi figyelembe, hogy a `MainForm`-on éppen melyik
       csatorna van AKTÍVNAK/INAKTÍVNAK beállítva - az "aktív
       csatorna" fogalom KIZÁRÓLAG az ÉLŐ játékra/bemenet-feldolgozásra
       vonatkozik (pl. a `AktivCsatornakAllapotanakKuldese()` is csak
       az aktív csatornákra küld). A lejátszás ATTÓL FÜGGETLENÜL
       lejátssza a `.mid` fájlban lévő ÖSSZES üzenetet, MINDEN
       csatornán, amin abban ténylegesen szerepel adat - még akkor is,
       ha a felhasználó időközben inaktívvá állított egy olyan
       csatornát, amelyiken a felvétel készült
     - ÚJ `Public Event UzenetKikuldve(statuszBajt, adat1, adat2,
       lejatszasBol, metronomBol)` a `MidiEngine`-ben (a `metronomBol`
       paramétert a metronóm-funkció tervezésekor vezettük be, lásd
       lentebb a "Metronóm-hangok kizárása a felvételből" pontnál),
       ami minden sikeres küldés után kiváltódik - erre iratkozik fel
       a felvevő ablak/logika
     - A felvevő az esemény kezelésekor: ha `lejatszasBol = True` ÉS a
       "lejátszott hangok rögzítése" checkbox NINCS bepipálva → kihagyja
       az üzenetet; egyébként rögzíti (időbélyeggel együtt)
     - **EXPLICIT MEGERŐSÍTVE**: az overdub-bal rögzített, lejátszásból
       (`lejatszasBol = True`) származó rész a `MainForm` aktuális
       aktív/inaktív csatorna-beállításaitól FÜGGETLENÜL, VÁLTOZATLANUL
       kerül a felvételbe (ez automatikusan következik abból, hogy a
       lejátszás maga sem megy át a split/transzponálás/aktív-csatorna
       logikán - lásd fent). Az élőben játszott, overdub-bal egyszerre
       rögzített rész (`lejatszasBol = False`) ezzel szemben a NORMÁL,
       élő bemenet-feldolgozási úton megy (split pont, transzponálás,
       aktív csatornák figyelembevételével), tehát ez a rész igenis
       tükrözi a `MainForm` aktuális beállításait - a két forrás
       (lejátszás vs. élő játék) emiatt eltérően viselkedik ebben a
       tekintetben, ez a helyes, szándékolt működés
   - ELFOGADOTT mellékhatás: mivel élő és lejátszott üzenetek egyetlen
     közös ponton futnak át, ha a felvétel Auto módban FELFEGYVERZETT
     állapotban van, és a felhasználó egy lejátszást indít el (anélkül,
     hogy élőben játszana), a lejátszás első üzenete is kiváltja a
     tényleges felvétel indulását, pontosan úgy, mint egy élő hang -
     ez akár egy szinte változatlan MÁSOLAT felvételét is eredményezheti,
     ha a felhasználó csak visszahallgat, nem játszik hozzá. A
     felhasználó ELFOGADTA ezt, NEM kér ellene külön védelmet

   **Vizuális visszajelzés (aktív felvétel állapota)**
   - Egy állapot-label ÉS a Stop gomb színe is jelzi az állapotot,
     STABIL (nem villogó) színekkel:
     - Nyugalmi (nincs felvétel): alapértelmezett szín, semleges felirat
     - Felfegyverezve (várja az első üzenetet): SÁRGA/borostyán,
       "Várakozás billentyűre..." jellegű felirat
     - Aktív felvétel: PIROS (`Button.BackColor`, `UseVisualStyleBackColor
       = False` szükséges hozzá), pl. "● FELVÉTEL" felirat
   - Villogás KIFEJEZETTEN NEM kell (a felhasználó explicit döntése) -
     egyszerű, stabil színváltás elég

   **A TERVEZÉS TELJES EGÉSZÉBEN LEZÁRULT** (a felvétel, mentés,
   lejátszás ÉS a metronóm-funkció is) - a `MidiFelvevoForm` teljes
   vizuális vázlata (HTML mockup formájában, a felhasználóval közösen,
   lépésről lépésre finomítva) elkészült és jóváhagyásra került, minden
   vezérlő, elrendezés és alapérték rögzítve ebben a dokumentumban. A
   pontos Designer pixel-koordináták és a `.Designer.vb` kód
   értelemszerűen csak a tényleges kódolás közben készülnek el, de
   ÉRDEMI, még eldöntendő DESIGN kérdés nincs hátra - a következő lépés
   a KÓDOLÁS elkezdése (lásd az ehhez javasolt sorrendet a fejezet
   végén)

5. **Telepítő (Inno Setup)** - eldöntött irány, még nincs elkezdve.
   Az előfeltétele (felhasználói adatok kikerülése a program mappájából)
   2026-09-03-án **elkészült**, lásd "Kódátnézés" 4. pont: minden írás a
   `%AppData%\MidiBill3` mappába megy, a program melletti `Adatok` mappa
   csak olvasható. A telepítőnek így elég a programfájlokat és a gyári
   `Adatok` mappát kitennie; felhasználói adatot nem kell kezelnie

6. **Performancia - MIDI-küldés leválasztása a UI-szálról** - elvi téma,
   átbeszélve, eddig nem volt probléma, nem sürgős. **A MIDI felvétel
   miatt viszont felértékelődik**: a felvétel időbélyegei a UI szál
   terheltségéből eredő jittert örökölnék (lásd "Kódátnézés" 12. pont).
   Az `Invoke` → `BeginInvoke` csere (2026-09-03) már megszüntette a
   callback-szál blokkolását, de a marshalolás maga megmaradt

7. Végigtesztelési checklist még nincs összeállítva

8. **Reverb/Chorus TÍPUS-választás (GS SysEx)** - a felhasználó
   GS-kompatibilis szintetizátort használ, tervezett irány a Roland GS
   Reverb Macro/Chorus Macro SysEx (lásd "Ismert, tudatosan meghozott
   döntések"). A felhasználó ezt KÉSŐBBI fejlesztésnek szánja, jelenleg
   nincs konkrét ütemezés hozzá

9. **Többnyelvű GUI (magyar + angol, később bővíthető)** - ELHALASZTVA,
   jelenleg nem sürgős, mivel egyelőre csak a felhasználó saját maga
   használja a programot, neki tökéletesen megfelel a magyar felület.
   A fejlesztés (kódkommentek, változónevek) továbbra is magyarul
   folytatódik, csak a MEGJELENÍTETT felület lenne választhatóan
   angol is. Végiggondolt megközelítés, ha eljön az ideje:
   - Kulcs-alapú szöveges fájlok (`lang_hu.txt`, `lang_en.txt`,
     `kulcs=szöveg` formátumban) - ugyanaz a minta, mint a
     `HangszerLista` fájlbetöltése, NEM a WinForms beépített
     resx/satellite assembly-alapú lokalizációja (utóbbi nehezebben
     illeszkedne a projekt stílusához és a dinamikusan összerakott
     szövegekhez, pl. `CsatornaFelirat()`)
   - Egy `Nyelv` modul (`Szoveg("kulcs")` lekérő függvénnyel) +
     egy "Beállítások" menü alatti "Nyelv" almenü Magyar/Angol
     választással, AZONNALI váltással (nincs szükség újraindításra)
   - A tényleges munka dandárja a meglévő, szó szerint bedrótozott
     magyar szövegek kulcsosítása lenne: `MainForm.Designer.vb`
     menü/gomb/checkbox feliratai, a `HangszerPanel` feliratai, a
     dinamikus sablonszövegek (`CsatornaFelirat()`,
     `CimSorUjrarajzolas()`), és becsülhetően 30-40 MsgBox-üzenet -
     összesen kb. 100-200 különálló szöveg, mechanikus, alacsony
     kockázatú, de munkaigényes refaktor
   - A kiválasztott nyelvet NEM a `Beallitasok` (JSON preset) tárolná,
     hasonló logikával, mint a MIDI In/Out kiválasztás: ez
     eszköz/környezet-szintű beállítás, nem preset-tartalom, ezért
     külön kellene tárolni (pl. saját kis fájl vagy `My.Settings`),
     hogy se preset-váltás, se "Alaphelyzetbe állítás" ne érintse

10. **Elméleti téma, lezárva**: iOS/iPadOS-portolás - távoli asztali
    elérés (RDP/Chrome Remote Desktop) a gyakorlati megoldás, ha a
    fizikai MIDI billentyűzet a Windows gépnél marad

11. **Elméleti téma, lezárva**: Claude Desktop Cowork mód - a
    felhasználó megpróbálta, de az adott beszélgetésben nem volt
    elérhető a váltás, ezért a hagyományos (fájl-beillesztős)
    munkamódszernél maradtunk
</content>

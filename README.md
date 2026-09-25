# MidiBill3 (VB.NET)

🇭🇺 Magyar | [🇬🇧 English](README.en.md)

MIDI billentyűzetről érkező jeleket szétosztó alkalmazás: egyetlen
billentyűlenyomásból akár **16 MIDI csatorna** bármelyike megszólalhat
egyidejűleg, mindegyik saját hangszínnel, hangerővel, sztereó-elhelyezéssel
(pan) és vibratóval (moduláció) - négyesével csoportosítva, egyszerre 4
csatorna szerkeszthető a felületen. Mind a 16 csatorna (a 10., dob-csatorna
is) teljesen szerkeszthető.

Ez a projekt egy korábbi, VB6-ban íródott és stabilan futó alkalmazás
(`MidiBill3` VB6) tudatos, **kézi újraírása** VB.NET-re – nem automata
migráció. A cél ugyanaz a funkcionalitás és felhasználói élmény, modern
.NET alapokon, kibővítve olyan funkciókkal, amikre a régi VB6-os verzió
architektúrája nem adott lehetőséget.

## Technológiák

- **.NET 10 (LTS)**
- **WinForms** – felhasználói felület, DPI-alapú skálázás, fix ablakméret
- **NAudio.Midi** – MIDI be- és kimenet kezelése
- **System.Text.Json** – beállítások mentése/betöltése (a régi, pozíció-alapú
  `.mb3` fájlformátum helyett), emberi olvasást segítő megjegyzésekkel
- **Saját, függőségmentes MIDI fájl-kezelés** – a Standard MIDI File (SMF)
  olvasása és írása külső könyvtár nélkül, saját kódból

## Fő funkciók

### Játék és hangzás

- **16 MIDI csatorna**, négyesével csoportosítva (1-4 / 5-8 / 9-12 / 13-16),
  a felületen egyszerre egy csoport szerkeszthető - mind a 16 csatorna
  (a dob-csatorna, 10. is) teljesen konfigurálható
- Csatornánkénti be-/kikapcsolás, automatikus "csatlakozás" a már lenyomott
  hangokhoz visszakapcsoláskor
- **Split Point**: a billentyűzet egy törésponti hangtól két zónára osztható
  (Alul/Felül), csatornánként külön beállítható, kézzel vagy "Tanulás"
  gombbal (a következő lenyomott billentyű állítja be)
- **Globális transzponálás** félhangos/oktávos lépésekben (±24 félhang)
- **Csatornánkénti oktáv-transzponálás** (±2 oktáv), azonnali (retroaktív)
  hatással - összeadódik a globális transzponálással
- **Globális Reverb** (MIDI CC91), 0-100%-os felhasználói skálán
- Note-tracking rendszer a célzott (nem felesleges) hangkikapcsoláshoz -
  split pont vagy transzponálás menet közbeni váltása sem hagy beragadt
  hangot
- "Csend legyen!" pánikgomb (mind a 16 csatornát végigsöpri)
- Pedál-inverzió (passzív "mód"-kapcsoló - önmagában sosem küld MIDI-t)
- Hibatűrő MIDI-küldés (pl. kihúzott USB-MIDI interfész esetén sem áll le
  a program), MIDI eszközlista frissítése és automatikus validálása
- Olvasható MIDI üzenet-kijelzés (pl. "NoteOn C4 v90"), korlátozott
  frissítési ütemmel

### Hangszerlisták és bankok

- **Betölthető hangszerlisták** (`.lst` fájlok): a beépített General MIDI
  listán túl egyedi, akár ritkás (nem folytonos indexelésű) listák is
  betölthetők/generálhatók
- **Külön kezelt Bank Select MSB (CC0) és LSB (CC32)**: a hangszert egy
  hármas azonosítja, a listafájlokban `msb.lsb.program=Név` alakban - a
  rövidebb `lsb.program=Név` és `program=Név` írásmód is érvényes (a hiányzó
  tagok nullák). Így elérhetők a General MIDI 128 hangszerén túli,
  bankokban tárolt variációk is, tetszőleges bankolási konvenció szerint
  (**GM2**: MSB=121 / dob MSB=120, **Roland GS**: variáció az MSB-ben,
  **Yamaha XG**: variáció az LSB-ben)
- **Gyári lista**: `GM.lst` / `GM.drm` (General MIDI)
- **Dedikált dobkészlet-hangszerlista** (`.drm` fájlok) a 10. csatornához,
  automatikusan párosítva az aktív hangszerlistával (azonos alapnév)
- **SoundFont (.sf2) importálás**: egy `.sf2` fájlból automatikusan
  legenerálja a hozzá tartozó `.lst` (hangszerek) és `.drm` (dobkészletek)
  fájlokat, és azonnal alkalmazza is őket. A SoundFont bankja a Bank Select
  **MSB-be** kerül - ez méréssel alátámasztott: a BASSMIDI-alapú
  VirtualMIDISynth (és a FluidSynth az alapértelmezett `gs` módjában) a
  CC0-t tekinti a SoundFont bankjának, a CC32-t figyelmen kívül hagyja
- **Több SoundFont importálása** egy menetben (többszörös fájlkiválasztás,
  jóváhagyás nélküli felülírás, a végén összegzés a létrehozott és
  lecserélt fájlok számáról)

### MIDI fájlok

- **MIDI felvevő**: a játék rögzítése Standard MIDI File-ba, **csatornánként
  külön track**-kel (a tracknév `"NN - Hangszernév"` alakú, NN a
  csatornaszám), a csatorna induló beállításaival a saját trackje elején
  - Külön, nem modális **felvevő ablak**, **F9** = Start / Stop (a
    főablakban is); a felvétel az **első leütéssel** indul, így az eleje
    nem néma; a felfegyverzés alatt lenyomott pedál és moduláció is bekerül
  - Színes állapotjelzés futó idővel ("Várakozás billentyűre..." / "●
    FELVÉTEL 0:42"), a főablak gombja is mutatja az állapotot
  - **Automatikus felvétel mód**: a felvevő a csendek mentén szegmensekre
    bontja a játékot - a szünet-küszöb (alapból 3 mp) után a
    játékidő-küszöbnél (alapból 5 mp) rövidebb próbálkozást eldobja, a
    hosszabbat külön fájlba menti, és azonnal várja a következőt. A csend
    csak akkor kezdődik, ha minden billentyű fel van engedve **és** a
    kitartó pedál is fent van, így kitartott akkord vagy pedálos
    lecsengés közben nem vág
  - Automatikus mentés a `Felvetelek` mappába (a fájlnév alapja a
    betöltött preset neve, dátum szerinti almappa választható), "Utoljára
    mentve" sor és "Mappa megnyitása" gomb; bezáráskor felvétel közben
    rákérdez (mentés / eldobás / mégse)
- **Jelentés a felvétel mellé**: a mentett `.mid` mellé automatikusan
  készül egy azonos nevű `.txt` a fájl tartalmáról (csatornák, hangszerek,
  vezérlők, tempó, hangnem, az aktuális preset fájl neve)
- **Segédprogramok menü** - önálló segédprogramok nélkül, közvetlenül a
  főprogramból:
  - Preset fájlok tömeges frissítése
  - Régi `.mb3` fájlok tömeges konvertálása
  - **Preset készítése MIDI fájlból** (a fájl csatorna-beállításaiból)
  - **MIDI fájlok módosítása az aktuális beállításokkal**
  - **MIDI fájl beállításainak mentése olvasható formában** (`.txt`
    jelentés bármelyik MIDI fájlról)

### Beállítások

- Beállítások mentése/betöltése JSON fájlba, automatikus kezdő-beállítás
  betöltése induláskor, "Kilépés mentés nélkül" opció
- A preset JSON-ban a hangszerhez emberi olvasásra szánt megjegyzés kerül,
  a bank jelölésével (pl. `"Hangszer": 131096,  // Ukulele (B8.0)`)
- Régi, VB6-os `.mb3` presetek importálása
- **`MidiBill3.ini`** a nem preset-jellegű beállításoknak: a két ablak
  pozíciója (ha a mentett hely már nem látszik, pl. leválasztott monitor
  miatt, az alapértelmezett helyre kerül), a felvevő ablak újranyitása, ha
  kilépéskor nyitva volt, és a felvevő beállításai (automatikus mód, a
  két küszöb, dátum szerinti almappa, jelentés)
- Robusztus hibakezelés sérült/kézzel szerkesztett JSON és `.lst`/`.sf2`
  fájlokra

## Projektstruktúra

```
MidiBill3/                      - egyetlen projekt
├── MainForm.vb                 - Fő ablak: MIDI eszközök, csatorna-választó,
│                                 split pont, transzponálás, Reverb, Segédprogramok
├── HangszerPanel.vb            - Egy hangszerpanel (UserControl), 4 fizikai
│                                 példány, a kiválasztott csatornacsoportot mutatja
├── MidiFelvevoForm.vb          - A MIDI felvevő ablak: Start/Stop (F9), futó idő,
│                                 automatikus felvétel (szegmensek a csendek
│                                 mentén), mentési beállítások, jelentés
├── ExtraBeallitasok.vb         - A MidiBill3.ini: ablakpozíciók, a felvevő
│                                 beállításai (ami nem preset-adat)
├── ApplicationEvents.vb        - Globális hibanaplózás (HibaNaplo.txt)
├── Modellek/
│   ├── AppInfo.vb              - Verziószám lekérdezés
│   ├── Beallitasok.vb          - Adatmodell (16 csatorna) + JSON mentés/betöltés
│   ├── Hangszerkulcs.vb        - A hangszerkulcs egyetlen igazságforrása
│   │                             (MSB/LSB/program <-> kombinált egész szám)
│   ├── HangszerLista.vb        - Betölthető hangszerlisták (hangszerek +
│   │                             dedikált dobkészlet-lista)
│   ├── SoundFontOlvaso.vb      - .sf2 fájlok beolvasása .lst/.drm generáláshoz
│   ├── MidiEngine.vb           - MIDI be-/kimenet, fan-out, split pont,
│   │                             transzponálás, Bank Select-alapú hangszerváltás
│   ├── MidiFelvevo.vb          - MIDI felvétel, Standard MIDI File írása
│   │                             (csatornánként külön track)
│   ├── AutomatikusSzegmentalo.vb - Az automatikus felvétel döntése
│   │                             (mentés / eldobás a csend után)
│   ├── MidiFajlOlvaso.vb       - Standard MIDI File beolvasása és elemzése
│   ├── MidiFajlSzerkeszto.vb   - MIDI fájlok módosítása az aktuális beállításokkal
│   ├── MidiPresetKeszito.vb    - Preset készítése egy MIDI fájl alapján
│   ├── MidiJelentes.vb         - Olvasható .txt jelentés egy MIDI fájlról
│   ├── CsatornaUzenetek.vb     - Egy csatorna induló MIDI-üzeneteinek összeállítása
│   ├── Hangnevek.vb            - MIDI hangszám -> hangnév konverzió
│   ├── IniFajl.vb              - Egyszerű INI fájl olvasása és írása
│   └── Utvonalak.vb            - Minden fájl-útvonal egy helyen (gyári vs.
│                                 felhasználói tartalom szétválasztása)
├── Tesztek/                    - Python ellenőrző szkriptek (a VB-logika
│                                 független újraimplementálása + mutációs tesztek)
├── GM.lst / GM.drm             - GYÁRI General MIDI listák (a telepítő szállítja)
├── CHANGELOG.md                - Részletes változásnapló
├── MidiBill3_VBNET_aktualis_allapot.md  - A projekt aktuális állapotának leírása
├── MIDI_felvetel_fazisterv.md  - A MIDI felvevő fázisterve
└── MSB_LSB_GM2_terv.md         - Az MSB/LSB szétválasztás terve
```

### Hol tárolja az adatokat?

A program **soha nem ír a saját mappájába** - így telepítve (pl. a
`C:\Program Files` alá) is működik, ahol egy normál jogosultságú
felhasználó nem tud írni. A két hely szétválik:

| Hely | Tartalom |
|---|---|
| A program mappája (csak olvasható) | `GM.lst`, `GM.drm` - a gyári General MIDI listák |
| `%AppData%\MidiBill3\` | `Kezdet.json` (induló beállítások), `MidiBill3.ini` (ablakpozíciók, a felvevő beállításai), `HibaNaplo.txt` |
| `%AppData%\MidiBill3\Presetek\` | A mentett preset fájlok (`.json`) |
| `%AppData%\MidiBill3\SF adatok\` | Az importált hangszerlisták (`.lst` / `.drm`) |
| `%AppData%\MidiBill3\Felvetelek\` | A MIDI felvevő által mentett `.mid` fájlok (+ a mellettük készülő `.txt` jelentések) |

A `GM.lst` és a `GM.drm` védett: mindig a gyári változat töltődik be, és a
program nem is enged ilyen néven felhasználói fájlt létrehozni.

## Előzmény

A VB6-os verziót egy korábbi, hosszabb közös munka során refaktoráltuk és
letisztítottuk (bugfixek, vezérlőtömbösítés, hibakezelés, note-tracking
rendszer, leállítási/eszközkezelési robusztusság). Ez a VB.NET-es verzió
ennek a letisztult állapotnak a tudatos újraírása, modern eszközökkel -
és azóta jelentősen túl is nőtte az eredeti VB6-os verzió képességeit
(16 teljesen szerkeszthető csatorna, split pont, transzponálás, Reverb,
külön MSB/LSB bankkezelés, SoundFont-import, MIDI felvétel és
fájlkezelés, beépített segédprogramok).

## Fejlesztés alatt

A projekt jelenleg aktív fejlesztés alatt áll. A MIDI felvétel kész, kézi
és automatikus módban is (felvevő ablak, indítás az első leütésre,
szegmensekre bontás a csendek mentén, mentés és jelentés); a
visszajátszás és a metronóm fejlesztése folyamatban van.

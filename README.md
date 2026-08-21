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

## Fő funkciók

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
- **Betölthető hangszerlisták** (`.lst` fájlok): a beépített General MIDI
  listán túl egyedi, akár ritkás (nem folytonos indexelésű) listák is
  betölthetők/generálhatók
- **Bank Select-alapú kiterjesztett hangszertámogatás** (Roland GS / Yamaha
  XG-kompatibilis): a General MIDI 128 hangszerén túli, bankokban tárolt
  variációk elérése, `bank.program=Név` formátumú `.lst` fájlokkal
- **Dedikált dobkészlet-hangszerlista** (`.drm` fájlok) a 10. csatornához,
  automatikusan párosítva az aktív hangszerlistával (azonos alapnév)
- **SoundFont (.sf2) importálás**: egy `.sf2` fájlból automatikusan
  legenerálja a hozzá tartozó `.lst` (hangszerek) és `.drm` (dobkészletek)
  fájlokat, és azonnal alkalmazza is őket
- Note-tracking rendszer a célzott (nem felesleges) hangkikapcsoláshoz -
  split pont vagy transzponálás menet közbeni váltása sem hagy beragadt
  hangot
- "Csend legyen!" pánikgomb (mind a 16 csatornát végigsöpri)
- Pedál-inverzió (passzív "mód"-kapcsoló - önmagában sosem küld MIDI-t)
- Hibatűrő MIDI-küldés (pl. kihúzott USB-MIDI interfész esetén sem áll le
  a program), MIDI eszközlista frissítése és automatikus validálása
- Olvasható MIDI üzenet-kijelzés (pl. "NoteOn C4 v90"), korlátozott
  frissítési ütemmel
- Beállítások mentése/betöltése JSON fájlba, automatikus kezdő-beállítás
  betöltése induláskor, "Kilépés mentés nélkül" opció
- Régi, VB6-os `.mb3` presetek importálása
- **Segédprogramok menü**: preset fájlok tömeges frissítése, régi `.mb3`
  fájlok tömeges konvertálása - közvetlenül a főprogramból, önálló
  segédprogramok nélkül
- Robusztus hibakezelés sérült/kézzel szerkesztett JSON és `.lst`/`.sf2`
  fájlokra

## Projektstruktúra

```
MidiBill3/                    - egyetlen projekt
├── MainForm.vb                - Fő ablak: MIDI eszközök, csatorna-választó,
│                                 split pont, transzponálás, Reverb, Segédprogramok
├── HangszerPanel.vb            - Egy hangszerpanel (UserControl), 4 fizikai
│                                 példány, a kiválasztott csatornacsoportot mutatja
├── ApplicationEvents.vb        - Globális hibanaplózás (HibaNaplo.txt)
├── Modellek/
│   ├── AppInfo.vb              - Verziószám lekérdezés
│   ├── Beallitasok.vb          - Adatmodell (16 csatorna) + JSON mentés/betöltés
│   ├── MidiEngine.vb           - MIDI be-/kimenet, fan-out, split pont,
│   │                             transzponálás, Bank Select-alapú hangszerváltás
│   ├── HangszerLista.vb        - Betölthető hangszerlisták (hangszerek +
│   │                             dedikált dobkészlet-lista)
│   └── SoundFontOlvaso.vb      - .sf2 fájlok beolvasása .lst/.drm generáláshoz
│   └── Hangnevek.vb            - MIDI hangszám -> hangnév konverzió
└── Adatok/                     - Preset fájlok + hangszerlista fájlok
                                  (GM.lst, GM.drm, egyedi .lst/.drm párok stb.)
```

## Előzmény

A VB6-os verziót egy korábbi, hosszabb közös munka során refaktoráltuk és
letisztítottuk (bugfixek, vezérlőtömbösítés, hibakezelés, note-tracking
rendszer, leállítási/eszközkezelési robusztusság). Ez a VB.NET-es verzió
ennek a letisztult állapotnak a tudatos újraírása, modern eszközökkel -
és azóta jelentősen túl is nőtte az eredeti VB6-os verzió képességeit
(16 teljesen szerkeszthető csatorna, split pont, transzponálás, Reverb,
Bank Select-alapú GS/XG-támogatás, SoundFont-import, beépített
segédprogramok).

## Fejlesztés alatt

A projekt jelenleg aktív fejlesztés alatt áll.

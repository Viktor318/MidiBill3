# MidiBill3 (VB.NET)

MIDI billentyűzetről érkező jeleket szétosztó alkalmazás: egyetlen
billentyűlenyomásból akár **16 MIDI csatorna** bármelyike megszólalhat
egyidejűleg, mindegyik saját hangszínnel, hangerővel, sztereó-elhelyezéssel
(pan) és vibratóval (moduláció) - négyesével csoportosítva, egyszerre 4
csatorna szerkeszthető a felületen.

Ez a projekt egy korábbi, VB6-ban íródott és stabilan futó alkalmazás
(`MidiBill3` VB6) tudatos, **kézi újraírása** VB.NET-re – nem automata
migráció. A cél ugyanaz a funkcionalitás és felhasználói élmény, modern
.NET alapokon, kibővítve olyan funkciókkal, amikre a régi VB6-os verzió
architektúrája nem adott lehetőséget.

## Technológiák

- **.NET 10 (LTS)**
- **WinForms** – felhasználói felület
- **NAudio.Midi** – MIDI be- és kimenet kezelése
- **System.Text.Json** – beállítások mentése/betöltése (a régi, pozíció-alapú
  `.mb3` fájlformátum helyett), emberi olvasást segítő megjegyzésekkel

## Fő funkciók

- **16 MIDI csatorna**, négyesével csoportosítva (1-4 / 5-8 / 9-12 / 13-16),
  a felületen egyszerre egy csoport szerkeszthető - a 10. (dob) csatorna
  szándékosan nem konfigurálható
- Csatornánkénti be-/kikapcsolás, automatikus "csatlakozás" a már lenyomott
  hangokhoz visszakapcsoláskor
- **Split Point**: a billentyűzet egy törésponti hangtól két zónára osztható
  (Alul/Felül), csatornánként külön beállítható, kézzel vagy "Tanulás"
  gombbal (a következő lenyomott billentyű állítja be)
- **Globális transzponálás** félhangos/oktávos lépésekben (±24 félhang)
- Note-tracking rendszer a célzott (nem felesleges) hangkikapcsoláshoz -
  split pont vagy transzponálás menet közbeni váltása sem hagy beragadt
  hangot
- "Csend legyen!" pánikgomb (mind a 16 csatornát végigsöpri)
- Pedál-inverzió
- Hibatűrő MIDI-küldés (pl. kihúzott USB-MIDI interfész esetén sem áll le
  a program), MIDI eszközlista frissítése és automatikus validálása
- Beállítások mentése/betöltése JSON fájlba, automatikus kezdő-beállítás
  betöltése induláskor, "Kilépés mentés nélkül" opció
- Régi, VB6-os `.mb3` presetek importálása
- Robusztus hibakezelés sérült/kézzel szerkesztett JSON fájlokra

## Projektstruktúra

```
MidiBill3/
├── MainForm.vb              - Fő ablak: MIDI eszközök, csatorna-választó,
│                               split pont, transzponálás, globális hangerő
├── HangszerPanel.vb          - Egy hangszerpanel (UserControl), 4 fizikai
│                               példány, a kiválasztott csatornacsoportot mutatja
├── ApplicationEvents.vb      - Globális hibanaplózás (HibaNaplo.txt)
├── Directory.Build.props     - Egységes verziószám mindhárom projekthez
├── Modellek/
│   ├── AppInfo.vb            - Verziószám lekérdezés
│   ├── Beallitasok.vb        - Adatmodell (16 csatorna) + JSON mentés/betöltés
│   ├── MidiEngine.vb         - MIDI be-/kimenet kezelése, fan-out, split pont,
│   │                           transzponálás
│   ├── HangszerLista.vb      - General MIDI hangszernevek
│   └── Hangnevek.vb          - MIDI hangszám -> hangnév konverzió
├── PresetKonvertalo/         - Segédprogram: régi presetek frissítése
├── Mb3Konvertalo/            - Segédprogram: régi .mb3 -> .json konvertálás
└── Adatok/                   - Megőrzött preset fájlok
```

## Előzmény

A VB6-os verziót egy korábbi, hosszabb közös munka során refaktoráltuk és
letisztítottuk (bugfixek, vezérlőtömbösítés, hibakezelés, note-tracking
rendszer, leállítási/eszközkezelési robusztusság). Ez a VB.NET-es verzió
ennek a letisztult állapotnak a tudatos újraírása, modern eszközökkel -
és azóta jelentősen túl is nőtte az eredeti VB6-os verzió képességeit
(16 csatorna, split pont, transzponálás, egységes verziószám-kezelés,
segédprogramok).

## Fejlesztés alatt

A projekt jelenleg aktív fejlesztés alatt áll.

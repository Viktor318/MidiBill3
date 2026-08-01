# MidiBill3 (VB.NET)

MIDI billentyűzetről érkező jeleket szétosztó alkalmazás: egyetlen
billentyűlenyomásból akár **4 hangszer szólal meg egyidejűleg**, mindegyik
saját hangszínnel, hangerővel, sztereó-elhelyezéssel (pan) és vibratóval
(moduláció).

Ez a projekt egy korábbi, VB6-ban íródott és stabilan futó alkalmazás
(`MidiBill3` VB6) tudatos, **kézi újraírása** VB.NET-re – nem automata
migráció. A cél ugyanaz a funkcionalitás és felhasználói élmény, modern
.NET alapokon.

## Technológiák

- **.NET 8 (LTS)**
- **WinForms** – felhasználói felület
- **NAudio.Midi** – MIDI be- és kimenet kezelése
- **System.Text.Json** – beállítások mentése/betöltése (a régi, pozíció-alapú
  `.mb3` fájlformátum helyett)

## Fő funkciók

- 4 független kimeneti csatorna, mindegyiken saját hangszer, hangerő, pan és
  vibrato beállítással
- Csatornánkénti be-/kikapcsolás, automatikus "csatlakozás" a már lenyomott
  hangokhoz visszakapcsoláskor
- Note-tracking rendszer a célzott (nem felesleges) hangkikapcsoláshoz
- "Csend legyen!" pánikgomb (teljes 128-hangos seprés minden csatornán)
- Hibatűrő MIDI-küldés (pl. kihúzott USB-MIDI interfész esetén sem áll le
  a program)
- Beállítások mentése/betöltése JSON fájlba, automatikus kezdő-beállítás
  betöltése induláskor

## Projektstruktúra

```
MidiBill3/
├── MainForm.vb           - Fő ablak: MIDI eszközök, globális hangerő, vezérlőgombok
├── HangszerPanel.vb       - Egy hangszerpanel (UserControl), 4 példányban használva
├── Modellek/
│   ├── Beallitasok.vb     - Adatmodell + JSON mentés/betöltés
│   └── MidiEngine.vb      - MIDI be-/kimenet kezelése (NAudio.Midi), fan-out logika
```

## Előzmény

A VB6-os verziót egy korábbi, hosszabb közös munka során refaktoráltuk és
letisztítottuk (bugfixek, vezérlőtömbösítés, hibakezelés, note-tracking
rendszer, leállítási/eszközkezelési robusztusság). Ez a VB.NET-es verzió
ennek a letisztult állapotnak a tudatos újraírása, modern eszközökkel.

## Fejlesztés alatt

A projekt jelenleg aktív fejlesztés alatt áll.

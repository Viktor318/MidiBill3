# MidiBill3 (VB.NET) – Aktuális állapot összefoglalója

Ez a dokumentum a **VB6 → VB.NET portolás jelenlegi állapotát** foglalja
össze, hogy egy új beszélgetés gyorsan fel tudjon zárkózni anélkül, hogy
végig kellene olvasni a teljes fejlesztési történetet. A projekt **éles,
működő állapotban van**, aktívan továbbfejlesztés alatt.

---

## Alapadatok

- **Verzió**: 1.4.0 (SemVer), egységesen kezelve a `Directory.Build.props`-ban
- **Cél keretrendszer**: .NET 10 (LTS)
- **UI**: WinForms
- **MIDI réteg**: NAudio.Midi 2.3.0 (+ NAudio.Core 2.3.0)
- **Fájlformátum**: JSON (`System.Text.Json`), emberi olvasást segítő
  `//` megjegyzésekkel kiegészítve
- **Fejlesztői környezet**: Visual Studio 2026 Community, két gépen
  (`C:\Dev\MidiBill3` mindkettőn – **soha ne Documents/OneDrive alá**)
- **Verziókezelés**: GitHub, rendszeres commit+push munkafolyamattal
- **Alkalmazásikon**: `MidiBill3.ico`, beállítva projekt- és Form-szinten is
- **Ablak**: `AutoScaleMode.Dpi` + `ApplicationHighDpiMode = PerMonitorV2`
  (éles megjelenítés minden DPI-n), `FormBorderStyle = FixedSingle`,
  `MaximizeBox = False` (fix méretű ablak, a pixel-pontos elrendezés
  védelmére), `MinimizeBox` alapértelmezett `True`

## Projektstruktúra

```
MidiBill3/
├── MainForm.vb / .Designer.vb   - Fő ablak
├── HangszerPanel.vb / .Designer.vb - Egy hangszerpanel (UserControl), 4 fizikai
│                                   példány, a kiválasztott csatornacsoportot mutatja
├── ApplicationEvents.vb         - Globális hibanaplózás (HibaNaplo.txt)
├── Directory.Build.props        - Egységes verziószám mindhárom projekthez
├── Modellek/
│   ├── AppInfo.vb               - Közös verziószám lekérdezés
│   ├── Beallitasok.vb           - Adatmodell (16 csatorna) + JSON mentés/betöltés
│   ├── MidiEngine.vb            - MIDI be-/kimenet, fan-out, split pont,
│   │                               transzponálás (globális + csatornánkénti)
│   ├── HangszerLista.vb         - Betölthető hangszerlista (Dictionary-alapú)
│   └── Hangnevek.vb             - MIDI hangszám -> hangnév konverzió
├── PresetKonvertalo/            - Segédprogram: régi presetek frissítése
├── Mb3Konvertalo/                - Segédprogram: régi .mb3 -> .json konvertálás
└── Adatok/                      - Preset fájlok + hangszerlista fájlok (GM.lst,
                                    Teszt.lst stb.)
```

## Elkészült funkciók

**Alapfunkciók**: 16 MIDI csatorna, csatorna-választó, Split Point,
Globális Transzponálás, csatornánkénti oktáv-transzponálás (±2, azonnali/
retroaktív hatással), globális Reverb (CC91), pedál-inverzió (passzív
"mód"-kapcsoló), "Csend legyen!" pánikgomb, "Kilépés"/"Kilépés mentés
nélkül", MIDI eszközlista frissítés+validálás, olvasható MIDI
üzenet-kijelzés (max. 10x/mp), hibatűrő MIDI-küldés, robusztus JSON
hibakezelés. (Részletek: forráskód vagy korábbi fejlesztési történet.)

**Fájlkezelés**: JSON mentés/betöltés, eszköznév-alapú MIDI be-/kimenet
tárolás, régi `.mb3` import, `Kezdet.json` automatikus be-/kimentés,
`PresetKonvertalo`/`Mb3Konvertalo` segédprogramok.

**Betölthető hangszerlisták (új, nagy funkció - 1. lépés)**
- `Modellek/HangszerLista.vb`: **`Dictionary(Of Integer, String)`**-alapú
  (patch-szám -> hangszernév), NEM fix 128-elemű tömb - ez teszi lehetővé,
  hogy a jövőben **ritkás** (nem folytonos indexelésű, akár csak néhány
  hangszert tartalmazó) egyedi listák is betölthetők legyenek
- **Fájlformátum**: `index=Hangszernév` soronként, tetszőleges sorrendben
  (pl. `Adatok/GM.lst` - a beépített 128 General MIDI név kiszervezve
  külső fájlba; `Adatok/Teszt.lst` - egy 10 elemű, ritkás teszt-lista)
- `HangszerLista.Betolt(fajlUtvonal)`: sikertelen betöltés esetén a
  **jelenleg aktív** lista változatlan marad (NEM esik vissza csendben a
  beépített GM-listára) - a hívó felelőssége a felhasználót értesíteni
- **`Beallitasok.HangszerListaFajl`** mező: minden preset megjegyzi,
  melyik hangszerlistával készült; betöltéskor a program megpróbálja azt
  is betölteni az `Adatok` mappából, sikertelenség esetén figyelmeztet,
  de a jelenlegi lista marad érvényben
- Indításkor a `MainForm` mindig a **beépített** listával hozza létre a
  paneleket (nincs fájlfüggés a induló panel-létrehozásnál), a
  `KezdetBeallitasokBetoltese()` → `BeallitasokAlkalmazasa()` folyamat
  végén váltja a listát a preset kérése szerint - ugyanez a mechanizmus
  kezel minden preset-betöltést (Kezdet.json, kézi "Adatok betöltése",
  `.mb3` import)

**KRITIKUS HIBAJAVÍTÁS: HangszerPanel patch-szám kezelés**
- **A hiba**: a `HangszerPanel.Hangszer` property eredetileg a
  `cmbHangszer.SelectedIndex`-et (a combo POZÍCIÓJÁT) adta át/fogadta
  MIDI patch-számként - ez csak addig volt helyes, amíg a lista teljes,
  folytonos 0-127 volt. Ritkás listánál (pl. `Teszt.lst`) ez vagy
  `ArgumentOutOfRangeException`-t dobott (`SelectedIndex` túl nagy érték),
  vagy - ha nem dobott kivételt - HIBÁS Program Change-t küldött (a UI
  egy hangszert mutatott, miközben egy MÁSIK, a nyers tárolt patch-szám
  szerinti hangszer szólalt meg - "csendes" félrevezető állapot)
- **A javítás**: `HangszerPanel`-ben új `hangszerKulcsok As List(Of
  Integer)` leképezés (combo pozíció -> valódi patch-szám). A `Hangszer`
  property Get/Set mostantól MINDIG a valódi patch-számmal dolgozik,
  biztonságos visszaeséssel (ha egy patch nem szerepel a listában, az
  első elemre esik, SOHA nem dob kivételt)
- **`HangszerListaFrissitese()`**: lista-váltáskor a kiválasztás VALÓDI
  PATCH-SZÁMA alapján (nem pozíció alapján) próbálja megtartani a
  kiválasztást
- **`MainForm.BeallitasokAlkalmazasa`**: a hangszerlista-váltás
  (`HangszerListaAlkalmazasa`) a csatorna-adatok panelre-alkalmazása ELÉ
  került (helyes sorrend), ÉS egy új ellenőrző kör fut le MIND A 16
  CSATORNÁRA a lista-váltás után: ha egy tárolt `Hangszer` patch-szám nem
  szerepel az aktuális (új) listában - pl. kézzel szerkesztett,
  ellentmondásos preset esetén -, az első elérhető hangszerre esik
  vissza, ÉS ez bekapcsolódik a meglévő `ertekKorrigalva` figyelmeztető
  rendszerbe. Ez biztosítja, hogy a UI és a ténylegesen kiküldött MIDI
  MINDIG szinkronban maradjon, nem csak a látszat (a combo) korrigálódik

## Ismert, tudatosan meghozott döntések

- Hangnevek nemzetközi konvencióval (C, C#, D...)
- ReadyToRun/Trim kikapcsolva, Framework-dependent publish
- Panic gomb pufferelés szándékosan nincs
- ComboBox-ok explicit elvesztik a fókuszt kiválasztás után
- RadioButton a csatorna-választóhoz (natív kizárás)
- Verziószám egységesen a `Directory.Build.props`-ban
- **Kódmódosításoknál a meglévő kommenteket MINDIG meg kell tartani**
  (explicit felhasználói elvárás, memóriában rögzítve)
- Hangszerlista fájlformátum: `index=Név`, Dictionary-alapú, ritkás
  listákra is felkészítve (nem a "sorszám = patch" egyszerű formátum)

## Globális hibanaplózás

Az `ApplicationEvents.vb` elkap minden kezeletlen kivételt, `HibaNaplo.txt`-be
ír. Natív (nem managed) összeomlásokat ez sem tud elkapni.

## Következő lépések / nyitott témák

1. **Hangszerlista-funkció folytatása** (a "1. lépés" - fájlból betöltés
   - most készült el):
   - **Élő listaváltás UI-ból**: jelenleg NINCS menü/gomb a hangszerlista
     futásidejű, manuális kiválasztásához - ezt csak a preset fájl
     `HangszerListaFajl` mezőjének kézi szerkesztésével lehet tesztelni.
     Ez a következő lépés: egy legördülő/menüpont az `Adatok` mappában
     található `.lst` fájlok közötti váltáshoz
   - **Aktuális lista neve a címsorban** - még nincs megvalósítva (eredeti
     elképzelés része volt)
   - **GS formátum támogatása** - csak GM formátum van most kész (a GS
     bank-alapú variációkezelés bonyolultabb, még nincs megtervezve)
   - **SoundFont (.SF2) alapú egyedi listák generálása** - a fájlFORMÁTUM
     (ritkás Dictionary) már felkészült erre, de maga a "hozz létre egy
     .lst-et egy .SF2 fájlból" funkció még nincs megtervezve/megvalósítva

2. **Csatornánkénti transzponálás** már kész (±2 oktáv) - a Chorus (CC93)
   még nincs megvalósítva, elvi terv kész (ugyanaz a minta, mint a Reverb)

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

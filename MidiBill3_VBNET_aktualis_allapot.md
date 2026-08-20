# MidiBill3 (VB.NET) – Aktuális állapot összefoglalója

Ez a dokumentum a **VB6 → VB.NET portolás jelenlegi állapotát** foglalja
össze, hogy egy új beszélgetés gyorsan fel tudjon zárkózni anélkül, hogy
végig kellene olvasni a teljes fejlesztési történetet. A projekt **éles,
működő állapotban van**, aktívan továbbfejlesztés alatt.

---

## Alapadatok

- **Verzió**: 1.6.0 (SemVer), a **`MidiBill3.vbproj`-ban közvetlenül**
  (a `Directory.Build.props` megszűnt - lásd lent)
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
- **Címsor**: rövidített forma - `"MIDI billentyűzet megszólaltatása 16
  csatornán : <beállítások fájlneve>, <hangszerlista fájlneve>"`
  (`CimSorUjrarajzolas()` - az EGYETLEN hely, ahol a `Me.Text` összeáll)

## Projektstruktúra

```
MidiBill3/                        - EGYETLEN projekt (a korábbi
│                                    PresetKonvertalo/Mb3Konvertalo
│                                    konzolos projektek megszűntek)
├── MainForm.vb / .Designer.vb   - Fő ablak
├── HangszerPanel.vb / .Designer.vb - Egy hangszerpanel (UserControl), 4 fizikai
│                                   példány, a kiválasztott csatornacsoportot mutatja
├── ApplicationEvents.vb         - Globális hibanaplózás (HibaNaplo.txt)
├── Modellek/
│   ├── AppInfo.vb               - Verziószám lekérdezés
│   ├── Beallitasok.vb           - Adatmodell (16 csatorna) + JSON mentés/betöltés
│   ├── MidiEngine.vb            - MIDI be-/kimenet, fan-out, split pont,
│   │                               transzponálás (globális + csatornánkénti)
│   ├── HangszerLista.vb         - Betölthető hangszerlista (Dictionary-alapú,
│   │                               Betolt()/Ment())
│   ├── SoundFontOlvaso.vb       - .sf2 fájlok "phdr" blokkjának beolvasása
│   └── Hangnevek.vb             - MIDI hangszám -> hangnév konverzió
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
tárolás, régi `.mb3` import, `Kezdet.json` automatikus be-/kimentés.

**Betölthető hangszerlisták - TELJES**: `Dictionary(Of Integer, String)`
alapú (ritkás listákra is felkészítve), `.lst` fájlformátum
(`index=Hangszernév`), "Hangszerlista betöltése..." menüpont (tetszőleges
helyről, automatikus másolás az `Adatok` mappába), `HangszerekErvenyesitese()`
robusztus validálás (csak a patch-szám számít, a név nem), SoundFont
(.sf2) importálás "phdr" blokk beolvasásával (csak Bank=0/GM-kompatibilis
presetek, magasabb bankok kihagyva és jelentve), címsorban megjelenő
aktuális lista. (Részletek: korábbi fejlesztési történet.)

**Segédprogramok beépítve a főprogramba (ÚJ, ez a kör)**
- **A korábbi különálló `PresetKonvertalo` és `Mb3Konvertalo` konzolos
  projektek MEGSZŰNTEK** - funkciójukat egy új **"Segédprogramok"** menü
  váltotta ki, közvetlenül a `MainForm`-ban:
  - **"Preset fájlok frissítése..."** - egy vagy több `.json` preset
    (`OpenFileDialog` `Multiselect=True`) újramentése az aktuális
    formátumra
  - **"Régi .mb3 fájlok konvertálása..."** - egy vagy több régi `.mb3`
    fájl JSON-ná alakítása; a MIDI be-/kimeneti eszköznevet `InputBox`
    kéri be, alapértelmezettként a jelenleg kiválasztott eszközökkel
    előtöltve
  - Mindkettő a `Beallitasok.Betoltes()`/`ImportMb3()`/`Mentes()`-t
    használja közvetlenül (nincs szükség linkelt fájlokra, hiszen már a
    főprojekt része) - a forrásfájl(ok) mappájában létrejövő
    `Konvertalva` almappába menti az eredményt, névazonos fájlt kérdés
    nélkül felülír (ugyanaz a viselkedés, mint a régi konzolos
    verzióknál volt)
  - Összegző üzenet a végén (sikeres/hibás fájlok száma + hibalista)
- **HIBAJAVÍTÁS**: az áthelyezéskor kimaradt a `b.Verzio =
  AppInfo.AppVerzio()` sor mindkét eseménykezelőből - emiatt a
  konvertált fájlokban a régi (preset-frissítésnél) vagy üres
  (`.mb3`-konvertálásnál) verziószám maradt. Javítva, mindkét helyen
  a `Mentes()` hívás előtt beállítva
- **`Directory.Build.props` megszüntetve**: mivel most már csak EGY
  projekt van (nincs több konzolos segédprogram, aminek szinkronban
  kellett volna tartania a verziószámát), feleslegessé vált a közös
  verziószám-kezelő fájl - a `<Version>` visszakerült közvetlenül a
  `MidiBill3.vbproj`-ba, egyszerűbb

## Ismert, tudatosan meghozott döntések

- Hangnevek nemzetközi konvencióval (C, C#, D...)
- ReadyToRun/Trim kikapcsolva, Framework-dependent publish
- Panic gomb pufferelés szándékosan nincs
- ComboBox-ok explicit elvesztik a fókuszt kiválasztás után
- RadioButton a csatorna-választóhoz (natív kizárás)
- **Verziószám most már közvetlenül a `MidiBill3.vbproj`-ban** (a
  `Directory.Build.props`-os megközelítést elhagytuk, miután a
  segédprogramok beépültek a főprojektbe - most már csak 1 helyen kell
  kezelni, nincs szükség a közös fájlra)
- **Kódmódosításoknál a meglévő kommenteket MINDIG meg kell tartani**
  (explicit felhasználói elvárás, memóriában rögzítve)
- Hangszerlista fájlformátum: `index=Név` szöveges fájl (nem JSON/XML)
- SoundFont-import: csak Bank=0, a Bank>0 presetek kihagyva

## Globális hibanaplózás

Az `ApplicationEvents.vb` elkap minden kezeletlen kivételt, `HibaNaplo.txt`-be
ír. Natív (nem managed) összeomlásokat ez sem tud elkapni.

## Következő lépések / nyitott témák

1. **GS-támogatás** - a felhasználó jelezte, hogy ebbe az irányba
   szeretne továbbmenni. Ehhez szükséges lenne:
   - Bank Select (CC0 MSB + CC32 LSB) küldése Program Change előtt
   - A `.lst` formátum kiterjesztése `bank.program=Név` szintaktikára
     (visszamenőleg kompatibilis - pont nélküli sor = bank 0)
   - A `Dictionary(Of Integer, String)` kulcsot `bank*128+program`-ra
     váltani
   - A SoundFont-importálás jelenleg kihagyja a Bank>0 preseteket - ezt
     GS-támogatással be lehetne kapcsolni

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

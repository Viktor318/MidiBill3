# MidiBill3 (VB.NET) – Aktuális állapot összefoglalója

Ez a dokumentum a **VB6 → VB.NET portolás jelenlegi állapotát** foglalja
össze, hogy egy új beszélgetés gyorsan fel tudjon zárkózni anélkül, hogy
végig kellene olvasni a teljes fejlesztési történetet. A projekt **éles,
működő állapotban van**, aktívan továbbfejlesztés alatt.

---

## Alapadatok

- **Verzió**: 1.5.0 (SemVer), egységesen kezelve a `Directory.Build.props`-ban
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
  fájlneve>, <hangszerlista fájlneve>"` (`CimSorUjrarajzolas()` metódus -
  ez az EGYETLEN hely, ahol a `Me.Text` ténylegesen összeáll)

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
│   ├── HangszerLista.vb         - Betölthető hangszerlista (Dictionary-alapú,
│   │                               Betolt()/Ment())
│   ├── SoundFontOlvaso.vb       - .sf2 fájlok "phdr" blokkjának beolvasása
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

**Betölthető hangszerlisták - TELJES (fájlból betöltés, menüpontos váltás, SoundFont-import)**

*Adatmodell (korábbi kör):*
- `Modellek/HangszerLista.vb`: **`Dictionary(Of Integer, String)`**-alapú
  (patch-szám -> hangszernév) - RITKÁS, nem folytonos indexelésű listákat
  is támogat (a jövőbeli GS-támogatáshoz és egyedi SoundFont-listákhoz)
- **Fájlformátum**: `index=Hangszernév` soronként (`.lst` kiterjesztés),
  `Adatok/GM.lst` a beépített 128 General MIDI hangszer
- `HangszerLista.Betolt()`/`Ment()`: fájlból olvasás / fájlba írás
- `HangszerPanel.Hangszer` property: **VALÓDI patch-számot** kezel, nem a
  combo pozícióját (`hangszerKulcsok` leképezéssel) - kritikus javítás
  ritkás listáknál, hogy a UI és a ténylegesen kiküldött MIDI mindig
  szinkronban maradjon
- `Beallitasok.HangszerListaFajl` mező: minden preset megjegyzi, melyik
  listával készült

*Menüpontos betöltés + robusztus validálás (ez a kör eleje):*
- **"Hangszerlista betöltése..." menüpont** (Fájl menü): tetszőleges
  helyről választható `.lst`, ha nem az `Adatok` mappából jön, megerősítő
  kérdés után átmásolódik oda (felülírás-figyelmeztetéssel, ha szükséges)
- **`HangszerekErvenyesitese()`**: közös, kiemelt validáló metódus - mind
  a 16 csatornát ellenőrzi (CSAK a patch-szám/index számít, a hangszernév
  NEM), a nem szereplő hangszereket az első elérhetőre állítja. Preset
  betöltésnél ÉS listaváltásnál is ugyanez fut
- Listaváltás után MINDIG újraküldi a MIDI-t minden csatornára
  (`OsszesCsatornaUjrakuldese`)
- Címsor mutatja az aktuális hangszerlista fájlnevét is

**SoundFont (.sf2) importálása hangszerlistává (v1.5.0, ez a kör vége)**
- `Modellek/SoundFontOlvaso.vb`: RIFF/SF2 fájlok `phdr` (Preset Headers)
  blokkjának beolvasása - CSAK metaadat, a hangminta-adatot (`sdta`)
  átugorja, gyors nagy fájloknál is
- **Csak Bank=0 (GM-kompatibilis) preseteket** vesz figyelembe -
  magasabb bankok (pl. dobkészletek) egyelőre kihagyva és jelentve
  (számuk megjelenik az összegzésben) - GS-támogatás hiányában a program
  úgysem tudná ezeket helyesen megszólaltatni (nincs Bank Select küldés)
- **"SoundFont importálása..." menüpont** (Fájl menü): fájlválasztás →
  beolvasási kísérlet (siker/hiba) → (ha 0 hangszer, jelzés és vége) →
  CSAK sikeres beolvasás UTÁN felülírás-megerősítés, ha a célnévvel már
  van `.lst` → `HangszerLista.Ment()` a generált `.lst`-hez → azonnali
  betöltés+validálás+MIDI-újraküldés+címsor → záró összegző üzenet
  (betöltött/kihagyott hangszerek száma)
- `Teszt.sf2`: szintetikus, csak `phdr`-blokkot tartalmazó teszt-fájl a
  konverter logika ellenőrzéséhez (NEM valódi, lejátszható SoundFont)

## Ismert, tudatosan meghozott döntések

- Hangnevek nemzetközi konvencióval (C, C#, D...)
- ReadyToRun/Trim kikapcsolva, Framework-dependent publish
- Panic gomb pufferelés szándékosan nincs
- ComboBox-ok explicit elvesztik a fókuszt kiválasztás után
- RadioButton a csatorna-választóhoz (natív kizárás)
- Verziószám egységesen a `Directory.Build.props`-ban
- **Kódmódosításoknál a meglévő kommenteket MINDIG meg kell tartani**
  (explicit felhasználói elvárás, memóriában rögzítve)
- Hangszerlista fájlformátum: `index=Név` szöveges fájl (nem JSON/XML) -
  tudatos döntés, konzisztens a projekt "kézzel is olvasható/szerkeszthető"
  filozófiájával; a jövőbeli GS-bővítés (`bank.program=Név`) visszamenőleg
  kompatibilis lesz ezzel a formátummal
- SoundFont-import: csak Bank=0, a Bank>0 presetek kihagyva (nem beleírva
  nagy kulcsokkal) - egyszerűbb, amíg nincs GS-támogatás

## Globális hibanaplózás

Az `ApplicationEvents.vb` elkap minden kezeletlen kivételt, `HibaNaplo.txt`-be
ír. Natív (nem managed) összeomlásokat ez sem tud elkapni.

## Következő lépések / nyitott témák

1. **GS-támogatás** - a felhasználó jelezte, hogy hamarosan ebbe az
   irányba szeretne továbbmenni. Ehhez szükséges lenne:
   - Bank Select (CC0 MSB + CC32 LSB) küldése Program Change előtt
   - A `.lst` formátum kiterjesztése `bank.program=Név` szintaktikára
     (visszamenőleg kompatibilis - pont nélküli sor = bank 0)
   - A `Dictionary(Of Integer, String)` kulcsot `bank*128+program`-ra
     váltani (ELVILEG már ma is működne emiatt, csak a MIDI-küldés oldalán
     kellene Bank Select-et hozzáadni)
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

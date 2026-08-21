# MidiBill3 (VB.NET) – Aktuális állapot összefoglalója

Ez a dokumentum a **VB6 → VB.NET portolás jelenlegi állapotát** foglalja
össze, hogy egy új beszélgetés gyorsan fel tudjon zárkózni anélkül, hogy
végig kellene olvasni a teljes fejlesztési történetet. A projekt **éles,
működő állapotban van**, aktívan továbbfejlesztés alatt.

---

## Alapadatok

- **Verzió**: 1.7.0 (SemVer), a **`MidiBill3.vbproj`-ban közvetlenül**
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

## Projektstruktúra

```
MidiBill3/                        - EGYETLEN projekt
├── MainForm.vb / .Designer.vb   - Fő ablak
├── HangszerPanel.vb / .Designer.vb - Egy hangszerpanel (UserControl), 4 fizikai
│                                   példány, a kiválasztott csatornacsoportot mutatja
├── ApplicationEvents.vb         - Globális hibanaplózás (HibaNaplo.txt)
├── Modellek/
│   ├── AppInfo.vb               - Verziószám lekérdezés
│   ├── Beallitasok.vb           - Adatmodell (16 csatorna) + JSON mentés/betöltés
│   ├── MidiEngine.vb            - MIDI be-/kimenet, fan-out, split pont,
│   │                               transzponálás, Bank Select-alapú hangszerváltás
│   ├── HangszerLista.vb         - Betölthető hangszerlista (Dictionary-alapú,
│   │                               Betolt()/Ment(), bank.program formátum)
│   ├── SoundFontOlvaso.vb       - .sf2 fájlok "phdr" blokkjának beolvasása (MINDEN bank)
│   └── Hangnevek.vb             - MIDI hangszám -> hangnév konverzió
└── Adatok/                      - Preset fájlok + hangszerlista fájlok (GM.lst,
                                    Teszt.lst, TesztGS.lst stb.)
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

**Segédprogramok beépítve a főprogramba**: "Segédprogramok" menü két
menüponttal (Preset fájlok frissítése, Régi .mb3 fájlok konvertálása) -
a korábbi, önálló `PresetKonvertalo`/`Mb3Konvertalo` konzolos projektek
megszűntek, funkciójuk beépült a `MainForm`-ba (Multiselect
`OpenFileDialog`, `Konvertalva` almappa a forrásmappában).

**Betölthető hangszerlisták - GM/egyedi (Bank Select nélkül)**:
`Dictionary(Of Integer, String)` alapú, `.lst` fájlformátum, "Hangszerlista
betöltése..." menüpont, `HangszerekErvenyesitese()` robusztus validálás,
SoundFont (.sf2) importálás, címsorban megjelenő aktuális lista.
(Részletek: korábbi fejlesztési történet.)

**Bank Select-alapú kiterjesztett hangszertámogatás (ÚJ, v1.7.0 - Roland GS / Yamaha XG-kompatibilis)**
- **Fontos terminológiai pontosítás**: ez NEM egy szigorúan Roland
  GS-specifikus vagy Yamaha XG-specifikus megvalósítás, hanem a **Bank
  Select (CC0+CC32) + Program Change** device-agnosztikus MIDI-mechanizmus
  támogatása - ez a MIDI szabvány része, bármely ezt tiszteletben tartó
  szintetizátorral/SoundFont-lejátszóval (pl. VirtualMIDISynth) működik,
  függetlenül attól, hogy az adott hangkészlet "hivatalosan" GS-nek vagy
  XG-nek vallja-e magát
- **`MidiEngine.ProgramValtas(csatornaSzam, kombinaltKulcs)`**: a
  kombinált kulcsot (`bank*128+program`) szétbontja, és **MINDIG**
  elküldi a teljes Bank Select párost (CC0 MSB=0, CC32 LSB=bank) a
  Program Change előtt - még sima GM (bank=0) hangszernél is,
  egyszerűség és konzisztencia kedvéért
  - GS-nél a Bank Select MSB (CC0) mindig 0, csak az LSB (CC32) változik
  - XG bizonyos speciális kategóriáinál (SFX-hangok) az MSB is eltérhetne
    0-tól, de ez jelenleg NEM támogatott (nem is releváns, mert a
    SoundFont `phdr` egyetlen Bank-számot ad, amit közvetlenül az LSB-be
    küldünk) - ha valaha szükség lenne rá, egy jövőbeli, visszamenőleg
    kompatibilis bővítés oldaná meg (`msb.lsb.program=Név` formátum)
- **`HangszerLista.Betolt()`/`Ment()`**: `.lst` formátum bővítve
  `bank.program=Név` szintaktikával (pl. `"8.0=Variation Piano"`),
  visszamenőleg kompatibilis a sima `program=Név` formával (bank=0
  implicit) - `IndexReszFeldolgozasa()` közös segédfüggvény dolgozza fel
  mindkét formátumot
- **`BeallitasokAlkalmazasa`**: a `Hangszer` mező tartományellenőrzése
  `0-127`-ről `0-16383`-ra bővítve (127×128+127 = a legnagyobb lehetséges
  kombinált kulcs)
- **`SoundFontOlvaso`**: a korábbi "csak Bank=0" szűrés MEGSZŰNT -
  mostantól minden bankból beolvassa a preseteket; a `KihagyottSzam`
  jelentése megváltozott (most csak a ténylegesen érvénytelen/sérült
  rekordokat számolja, nem a magasabb bankokat)
- **`HangszerPanel`**: a legördülő listában a Bank≠0 hangszerek `Név
  (B<bank>)` formában jelennek meg (pl. `"Variation Piano (B8)"`), a
  Bank=0 hangszerek változatlanul, jelzés nélkül - a lista automatikusan
  a kombinált kulcs szerint rendezve, ami bankonként csoportosítva
  mutatja a hangszereket görgetéskor
- **Miért volt (majdnem) ingyen**: a korábbi, ritkás listákra felkészített
  architektúra (`HangszerPanel.Hangszer` VALÓDI patch-számmal, nem
  pozícióval dolgozik; `csatornaAllapotok` tetszőleges egész számot
  kezel) már eleve készen állt erre a bővítésre - a `MainForm.vb`-hez
  gyakorlatilag NEM kellett hozzányúlni
- **Nincs egyetlen "hivatalos" GS-lista beépítve** (`GS.lst`) - a Roland
  GS-lista generációnként bővült (SC-55 → SC-88 → SC-8850), nincs
  egyetlen végleges verzió, ezért a **saját SoundFont-importálás** a
  javasolt, megbízható út egy adott GS/XG-kompatibilis hangkészlethez -
  ez pontosan azt adja vissza, amit a felhasználó szintetizátora
  ténylegesen támogat
- `TesztGS.lst`: teszt-fájl a `bank.program` formátum ellenőrzéséhez
  (kevert bank=0 és bank≠0 bejegyzésekkel)
- **Élesben tesztelve** a felhasználó saját, valódi GS-kompatibilis
  SoundFont-jával - működik

## Ismert, tudatosan meghozott döntések

- Hangnevek nemzetközi konvencióval (C, C#, D...)
- ReadyToRun/Trim kikapcsolva, Framework-dependent publish
- Panic gomb pufferelés szándékosan nincs
- ComboBox-ok explicit elvesztik a fókuszt kiválasztás után
- RadioButton a csatorna-választóhoz (natív kizárás)
- Verziószám közvetlenül a `MidiBill3.vbproj`-ban (egy projekt van)
- **Kódmódosításoknál a meglévő kommenteket MINDIG meg kell tartani**
  (explicit felhasználói elvárás, memóriában rögzítve)
- Hangszerlista fájlformátum: `index=Név` vagy `bank.program=Név`
  szöveges fájl (nem JSON/XML)
- Bank Select mindig MSB=0 (nincs teljes MSB+LSB rugalmasság - jövőbeli,
  opcionális bővítés, ha valaha szükség lenne rá)
- Bank Select mindig elküldve, még bank=0-nál is (egyszerűség, nem
  feltételes logika)

## Globális hibanaplózás

Az `ApplicationEvents.vb` elkap minden kezeletlen kivételt, `HibaNaplo.txt`-be
ír. Natív (nem managed) összeomlásokat ez sem tud elkapni.

## Következő lépések / nyitott témák

1. **Chorus (CC93)** - elvi terv kész, ugyanaz a minta, mint a Reverb.
   Még nincs megvalósítva

2. **MIDI felvétel/visszajátszás** - elvi terv, eldöntött kulcskérdéssel:
   a NYERS BEMENETET vesszük fel (pedál-inverzió UTÁN, split pont/
   transzponálás/csatorna-szétosztás ELŐTT), visszajátszáskor újra
   végigmegy a `MidiEngine.UzenetKuldes()`-en. Még nincs elkezdve

3. **Telepítő (Inno Setup)** - eldöntött irány, még nincs elkezdve

4. **Performancia - MIDI-küldés leválasztása a UI-szálról** - elvi téma,
   átbeszélve, eddig nem volt probléma, nem sürgős

5. Végigtesztelési checklist még nincs összeállítva

6. **Elméleti téma, lezárva**: iOS/iPadOS-portolás - távoli asztali
   elérés (RDP/Chrome Remote Desktop) a gyakorlati megoldás, ha a
   fizikai MIDI billentyűzet a Windows gépnél marad

7. **Lehetséges jövőbeli finomítás (nem sürgős)**: "Bank váltó" külön UI
   (a jelenlegi "lapos", egy legördülős megoldás helyett/mellett), ha
   nagy, sok-bankos GS/XG-listáknál a görgetés kényelmetlenné válna a
   gyakorlatban - egyelőre az egyszerű, "Név (B<bank>)" jelzéses lapos
   lista elegendőnek bizonyult

8. **Lehetséges jövőbeli finomítás (nem sürgős)**: teljes Bank Select
   MSB+LSB rugalmasság (`msb.lsb.program=Név` formátum), ha valaha
   szükség lenne rá olyan XG-hangkészleteknél, ahol az MSB is eltér 0-tól

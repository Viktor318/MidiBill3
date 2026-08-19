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
│   ├── HangszerLista.vb         - General MIDI hangszernevek
│   └── Hangnevek.vb             - MIDI hangszám -> hangnév konverzió
├── PresetKonvertalo/            - Segédprogram: régi presetek frissítése
├── Mb3Konvertalo/                - Segédprogram: régi .mb3 -> .json konvertálás
└── Adatok/                      - Megőrzött preset fájlok
```

## Elkészült funkciók

**Alapfunkciók**: 16 MIDI csatorna, csatorna-választó (1-4/5-8/9-12/13-16
RadioButton-okkal, egyszerre 4 szerkeszthető, 10. csatorna dob-ra letiltva),
Split Point, pedál-inverzió, "Csend legyen!" pánikgomb, "Kilépés"/"Kilépés
mentés nélkül", hibatűrő MIDI-küldés, egységes `FormClosing`.

**Fájlkezelés**: JSON mentés/betöltés, eszköznév-alapú MIDI be-/kimenet
tárolás, régi `.mb3` import, `Kezdet.json` automatikus be-/kimentés,
`Adatok` mappa build/publish-kor automatikusan másolva, `PresetKonvertalo`/
`Mb3Konvertalo` segédprogramok (linkelt fájlokkal).

**MIDI eszközkezelés**: "MIDI eszközök frissítése" gomb, `EszkozListaEgyezikE()`
minden kiválasztás előtt validál (véd az eszköz-index-eltolódás ellen),
induláskori bemenet/kimenet-hiány kezelése (`Environment.Exit(1)` ha nincs
kimenet).

**JSON betöltés hibakezelése**: `null` csatorna-elem védelem, figyelmeztető
üzenetek (hiányzó MIDI eszköznév / tartományon kívüli, korrigált érték),
10. csatorna adatai sosem mentve/beolvasva, csatorna-sorszám kommentek,
`Verzio` mező mentve (tisztán tájékoztató).

**Globális Transzponálás**: `GroupBox` 4 gombbal (Félhang fel/le, Oktáv
fel/le, ±24 félhang), NEM retroaktív (élő váltás nem érinti a már tartott
hangokat).

**Split Point**: csatornánként Alul/Felül zóna, kézi vagy "Tanulás" gombos
beállítás, NEM retroaktív, Note Off mindig "hazatalál" a ténylegesen
kiküldött hangmagasságra.

**Csatornánkénti oktáv-transzponálás (új)**
- `HangszerPanel`-en új `TrackBar` (Her/Tér/Vib sorok alatt): **-2..+2 oktáv**
  (panel magassága 270→324, keret 300→354, `MainForm` 942×866 véglegesen)
- **Összeadódik** a globális (félhangos) transzponálással:
  `kimeno = eredeti + globális_félhang + csatorna_oktáv*12`
- **AZONNALI (retroaktív)** hatású, eltérően a globálistól - csúszka
  mozgatásakor a már tartott hangok is azonnal az új oktávra váltanak
  (`MidiEngine.OktavValtas()`, a `HangokKikapcsolasa`+
  `LenyomottHangokMegszolaltatasa` páros újrahasznosításával)
- `CsatornaBeallitas.Oktav` mentve/betöltve, korlátozás `-2..2`

**MIDI üzenet-kijelzés (finomítva)**
- Olvasható formátum nyers számok helyett: `"MIDI üzenetek: NoteOn C4 v90"`,
  `"PC Acoustic Grand Piano"` stb. (`MidiUzenetSzoveg()`, felhasználva a
  `Hangnevek.HangNev()`-et és az új `HangszerLista.Nev()`-et)
- **Max. 10×/mp frissítési korlát** (`midiKijelzoIdozito`, Timer
  Interval=100) - sűrű játék (gyors arpeggio) esetén a felirat nem villog
  kaotikusan; a tényleges MIDI-küldés időzítését ez NEM érinti, csak a
  kijelzést
- `lblMidiUzenet` szélesítve (280 px) a hosszabb szöveges üzenetekhez

**Pedál-invertálás (javítva)**
- **Hiba volt**: `chkPedal.Checked` bepipálása/betöltése aktívan küldött
  egy CC64 üzenetet - ez preset betöltéskor (programozott
  `chkPedal.Checked = ...`) egy HAMIS pedál-állapotot küldött ki, elrontva
  a szintetizátor sustain-szinkronját a valódi fizikai pedálhoz képest
- **Javítva**: a checkbox mostantól **teljesen passzív "mód"-kapcsoló**
  (mint egy hardveres invertáló kapcsoló a pedál vezetékében) - sosem
  kezdeményez MIDI-küldést, csak azt szabályozza, hogy a KÖVETKEZŐ
  ténylegesen beérkező CC64-et invertáljuk-e. `chkPedal_CheckedChanged`
  és `utolsoPedalErtek` mező törölve, a logika állapotmentes lett

**Globális Reverb (CC91, új)**
- `grpReverb` + `trkReverb` csúszka, 0-100%-os felhasználói skálán
  (a Transzponálás és a Csend legyen!/Kilépés gombok között - **a gombok
  visszakerültek az eredeti elrendezéshez**: alsó szélük ismét pontosan
  egybeesik a hangszerpanel aljával, `Y=709`, `709+60=769`)
- A 0-100%-os érték MIDI CC91-re (0-127) számítódik át küldéskor
  (`ertek*127\100`), hogy a teljes MIDI-tartomány kihasználható legyen
- **Globális, nem csatornánkénti**: minden mozdításkor MIND a 16
  csatornára kimegy, függetlenül azok aktiváltságától - ugyanaz a minta,
  mint a Hangszer/Hangerő/Tér/Vibrato (`OsszesCsatornaUjrakuldese`-be is
  bekötve)
- `Beallitasok.Reverb` mentve/betöltve
- **Chorus (CC93) szándékosan kihagyva** - nincs rá jelenlegi igény,
  elvi terv megvan, ha valaha kellene

## Ismert, tudatosan meghozott döntések

- Hangnevek nemzetközi konvencióval (C, C#, D...)
- ReadyToRun/Trim kikapcsolva, Framework-dependent publish
- Panic gomb pufferelés szándékosan nincs
- ComboBox-ok explicit elvesztik a fókuszt kiválasztás után
- RadioButton a csatorna-választóhoz (natív kizárás)
- Verziószám egységesen a `Directory.Build.props`-ban
- **Kódmódosításoknál a meglévő kommenteket MINDIG meg kell tartani**
  (explicit felhasználói elvárás, memóriában rögzítve)

## Globális hibanaplózás

Az `ApplicationEvents.vb` elkap minden kezeletlen kivételt, `HibaNaplo.txt`-be
ír. Natív (nem managed) összeomlásokat ez sem tud elkapni.

## Következő lépések / nyitott témák

1. **Chorus (CC93)** - elvi terv kész, ugyanaz a minta lenne, mint a
   Reverb (globális vagy esetleg később csatornánkénti - még nem
   eldöntött, ha egyáltalán szükség lesz rá)

2. **MIDI felvétel/visszajátszás** - elvi terv, eldöntött kulcskérdéssel:
   - **A NYERS BEMENETET vesszük fel** (pedál-inverzió UTÁN, split pont/
     transzponálás/csatorna-szétosztás ELŐTT), NEM a feldolgozott kimenetet
   - Visszajátszáskor a felvett jel újra végigmegy a
     `MidiEngine.UzenetKuldes()`-en, a **visszajátszás pillanatában
     érvényes** beállításokkal
   - NAudio.Midi ad építőelemeket, a pontos időzítésű lejátszó saját
     fejlesztés lesz. Még nincs elkezdve

3. **Telepítő (Inno Setup)** - eldöntött irány (`.iss` szkript), még
   nincs elkezdve

4. **`Segedprogramok` közös mappa** - felmerült ötlet a két konvertáló
   egy közös mappába rendezésére, nincs sürgető, bármikor megcsinálható

5. **Performancia - MIDI-küldés leválasztása a UI-szálról** - elvi
   téma, átbeszélve: jelenleg a `MidiUzenetFeldolgozasa` (hangküldést
   IS tartalmazó) szinkron `Invoke`-on megy a NAudio háttérszáláról.
   Ha valaha hallható döccenést tapasztalnál (pl. amikor egy `MsgBox`
   van nyitva, és közben játszol), ez a hely, ahol érdemes lenne a
   hangküldést leválasztani a UI-szálról (szál-biztonsági `SyncLock`
   bevezetésével a `MidiEngine` belső állapotára). **Eddig nem volt
   probléma, nem sürgős.**

6. Végigtesztelési checklist még nincs összeállítva

7. **Elméleti téma, lezárva**: iOS/iPadOS-portolás - jelentős újraírás
   lenne (.NET MAUI + VB.NET class library reuse, vagy natív Swift).
   Rövid távú alternatíva: távoli asztali elérés (RDP/Chrome Remote
   Desktop) iPadről, ha a fizikai MIDI billentyűzet a Windows gépnél
   marad (hangot NEM szabad távoli munkamenetbe vonni, késleltetés miatt).

8. **Csatornánkénti oktáv bővítése ±3-ra** - átbeszélve, egyszerű
   módosítás lenne (TrackBar Min/Max + `Korlatoz()` határok a
   `BeallitasokAlkalmazasa`-ban), jelenleg nem szükséges, ±2 elegendő.

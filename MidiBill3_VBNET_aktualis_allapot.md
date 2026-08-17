# MidiBill3 (VB.NET) – Aktuális állapot összefoglalója

Ez a dokumentum a **VB6 → VB.NET portolás jelenlegi állapotát** foglalja
össze, hogy egy új beszélgetés gyorsan fel tudjon zárkózni anélkül, hogy
végig kellene olvasni a teljes fejlesztési történetet. A projekt **éles,
működő állapotban van**, aktívan továbbfejlesztés alatt.

---

## Alapadatok

- **Verzió**: 1.2.0 (SemVer: Major.Minor.Patch - Major = kompatibilitást
  törő, Minor = új funkció, Patch = hibajavítás)
- **Cél keretrendszer**: .NET 10 (LTS)
- **UI**: WinForms
- **MIDI réteg**: NAudio.Midi 2.3.0 (+ NAudio.Core 2.3.0)
- **Fájlformátum**: JSON (`System.Text.Json`), emberi olvasást segítő
  `//` megjegyzésekkel kiegészítve (hangszernevek, split pont hangneve,
  transzponálás félhang-szövege, fejléc) – ezeket a `Betoltes()` a
  `JsonCommentHandling.Skip` miatt simán átugorja
- **Fejlesztői környezet**: Visual Studio 2026 Community, két gépen
  (`C:\Dev\MidiBill3` mindkettőn – **soha ne Documents/OneDrive alá**,
  fájlzárolási problémákat okoz)
- **Verziókezelés**: GitHub, rendszeres commit+push munkafolyamattal
- **Alkalmazásikon**: `MidiBill3.ico` (zongorabillentyű + 4 csatornás
  szétosztás motívum), beállítva projekt- és Form-szinten is

## Projektstruktúra

```
MidiBill3/
├── MainForm.vb / .Designer.vb   - Fő ablak: MIDI eszközök, split pont,
│                                   transzponálás, gombok
├── HangszerPanel.vb / .Designer.vb - Egy hangszerpanel (UserControl), 4 példányban
├── ApplicationEvents.vb         - Globális hibanaplózás (HibaNaplo.txt)
├── Modellek/
│   ├── Beallitasok.vb           - Adatmodell + JSON mentés/betöltés + .mb3 import
│   ├── MidiEngine.vb            - MIDI be-/kimenet, fan-out, split pont,
│   │                               transzponálás logika
│   ├── HangszerLista.vb         - 128 General MIDI hangszernév
│   └── Hangnevek.vb             - MIDI hangszám -> hangnév (pl. "C4")
├── Segedprogramok/               - (később: PresetKonvertalo, Mb3Konvertalo
│                                   ide rendezve, ha megtörtént az áthelyezés)
├── Adatok/                      - Régi, megőrzött preset fájlok (verziókövetve)
├── MidiBill3.ico                - Alkalmazásikon
└── Kezdet.json                  - Automatikus induló-állapot (NEM verziókövetve)
```

## Elkészült funkciók

**Alapfunkciók (a VB6-os verzióból átvéve, letisztítva)**
- 4 független MIDI kimeneti csatorna, mindegyiken saját hangszer,
  hangerő, pan (Tér), vibrato beállítással
- Csatornánkénti be-/kikapcsolás (Aktív checkbox), automatikus
  "csatlakozás" a már lenyomott hangokhoz visszakapcsoláskor
- Note-tracking rendszer (célzott hangkikapcsolás, nem vak 128-as seprés)
- "Csend legyen!" pánikgomb (teljes 128-hangos seprés minden csatornán) -
  dőlt+félkövér felirattal kiemelve
- "Kilépés" gomb/menü - félkövér felirattal kiemelve
- **"Kilépés mentés nélkül"** menüpont (Fájl menü): megerősítő kérdéssel,
  a hangok kikapcsolása és a MIDI-portok bezárása megtörténik, csak a
  `Kezdet.json` felülírása marad el - hasznos kísérletezéskor
- Pedál-inverzió (Fordított pedál működés checkbox)
- Hibatűrő MIDI-küldés (kihúzott USB-MIDI interfész esetén sem áll le
  a program, csak egyszer figyelmeztet)
- Egységes `FormClosing` minden kilépési útvonalra (gomb, menü, "X",
  "Kilépés mentés nélkül")

**Fájlkezelés**
- JSON mentés/betöltés, eszköznév-alapú MIDI be-/kimenet tárolás
- Régi `.mb3` fájlok importálása (`Fájl → Régi .mb3 preset importálása`)
- `Kezdet.json` automatikus betöltés induláskor / mentés kilépéskor
- Fájlműveletek (betöltés/mentés/import) külön-külön jegyzik meg a
  legutóbb használt mappájukat
- `Adatok` mappa: verziókövetett preset-gyűjtemény, build/publish
  automatikusan másolja a kimeneti mappába is
- **Segédprogramok** (`PresetKonvertalo`, `Mb3Konvertalo`): önálló
  konzolos projektek, a `Beallitasok.vb`/`HangszerLista.vb`/`Hangnevek.vb`
  fájlokat linkként használják (nincs duplikált logika). Mappánként
  kötegben frissítik/konvertálják a régi preset fájlokat. A
  `MidiBill3.vbproj`-ban explicit ki vannak zárva az implicit
  fájlfelismerésből.

**Split Point**
- Csatornánként külön be-/kikapcsolható **Alul** és **Felül** zóna
- Split pont beállítása kézzel (legördülő lista) vagy "Tanulás" gombbal
- Split pont váltása élőben NEM retroaktív (már tartott hangokat nem érint)
- Note Off mindig "hazatalál" (a ténylegesen szóló csatornára és
  ténylegesen kiküldött hangmagasságra megy, függetlenül attól, hogy
  időközben változott-e a split pont vagy egy csatorna zóna-beállítása)
- Visszamenőleges kompatibilitás: régi fájloknál minden csatorna
  alapból mindkét zónában szól

**Globális Transzponálás (v1.2.0)**
- `GroupBox` a Split pont alatt: fejléc "Transzponálás: +N/-N/0"
- 4 gomb: Félhang fel, Félhang le, Oktáv fel, Oktáv le
- Tartomány: ±24 félhang (±2 oktáv)
- A Split pont zóna-döntését NEM érinti - az mindig az EREDETI
  (lejátszott) hangot nézi; a transzponálás csak a ténylegesen
  kiküldött hangmagasságot tolja el
- Élő váltás NEM retroaktív (már tartott hangokat nem érint)
- Ha a transzponált hang a 0-127 MIDI tartományon kívülre esne, az a
  hang egyszerűen néma marad (nem küldjük ki)
- `MidiEngine`-ben egy `kimenoHang` nyilvántartás biztosítja, hogy a
  Note Off mindig a ténylegesen kiküldött (transzponált) hangmagasságra
  menjen, így menet közbeni transzponálás-váltás sem hagyhat beragadt
  hangot
- Mentve/betöltve a JSON presetekben (`Transzponalas` mező, régi
  fájloknál alapértelmezett 0)

## Ismert, tudatosan meghozott döntések

- **Hangnevek nemzetközi konvencióval** (C, C#, D... nem magyar szolmizáció)
- **ReadyToRun compilation kikapcsolva**, **Trim unused assemblies kikapcsolva**
- **Framework-dependent publish** (nem self-contained)
- **Panic gomb pufferelés szándékosan nincs**
- **ComboBox-ok explicit elvesztik a fókuszt kiválasztás után**
  (`ActiveControl = Nothing`) a konzisztens, "fehér" (nem kék kiemelt)
  megjelenésért

## Globális hibanaplózás

Az `ApplicationEvents.vb` elkap minden kezeletlen kivételt (UI-szálon
és háttérszálon egyaránt), `HibaNaplo.txt`-be ír. Natív (nem managed)
összeomlásokat (pl. driver-szintű `AccessViolationException`) ez sem
tud elkapni - ilyenkor a Windows Eseménynaplót kell megnézni.

## Következő lépések / nyitott témák

Fontossági/tervezett sorrendben:

1. **Csatornánkénti transzponálás** - új `TrackBar` a `HangszerPanel`-en,
   a globális transzponálás mellett/kiegészítéseként (a pontos
   összeadódási logika még nincs eldöntve - ezt a globális
   transzponáláséhoz hasonló elvi átbeszélés fogja megelőzni)

2. **Reverb és Chorus csatornánként** - elvi terv már kész:
   - General MIDI Controller Change üzenetekkel (`Reverb Send Level` =
     controller 91, `Chorus Send Level` = controller 93), pontosan
     ugyanazon a mintán, mint a jelenlegi `ControllerValtas` (Hangerő/
     Pan/Vibrato)
   - Két új `TrackBar` a `HangszerPanel`-en (0-127), ugyanazzal a
     property/esemény-mintával, mint Her/Tér/Vib
   - **Fontos korlát**: ez Controller Change, tehát nem hangmagassághoz
     kötött - a Split pont és a Transzponálás nem érinti
   - **Tényleges hatás eszközfüggő**: a Microsoft GS Wavetable Synth
     korlátozottan/egyáltalán nem reagál rá, a VirtualMIDISynth
     (SoundFont-alapú) általában igen, ha a SoundFont maga támogatja -
     ezt a konkrét MIDI-kimeneti eszközön ki kell majd próbálni

3. **MIDI felvétel/visszajátszás** - elvi terv, eldöntött kulcskérdéssel:
   - **A NYERS BEMENETET vesszük fel** (a pedál-inverzió UTÁN, de a
     split pont/transzponálás/csatorna-szétosztás ELŐTT befutó jelet) -
     NEM a már feldolgozott, csatornákra szétosztott kimenetet
   - Ennek következménye: visszajátszáskor a felvett nyers jel újra
     végigmegy a `MidiEngine.UzenetKuldes()`-en, tehát a
     **visszajátszás pillanatában érvényes** split pont, transzponálás
     és csatorna-beállítások (hangszer, Aktív/Alul/Felül) alkalmazódnak
     rá - rugalmas újrajátszást tesz lehetővé (felvétel most, más
     hangszer-kombinációval visszajátszva később), és nincs
     kód-duplikáció a fan-out logikában
   - NAudio.Midi ehhez ad építőelemeket (`MidiEvent`,
     `MidiEventCollection`, `MidiFile.Export()` szabványos `.mid`
     fájlba mentéshez), de a felvevő és különösen a pontos időzítésű
     lejátszó (nagy pontosságú, dedikált szálú időzítő) saját fejlesztés
   - Még nincs elkezdve, külön tervezési kört fog igényelni

4. **Telepítő (Inno Setup)** - eldöntött irány (`.iss` szkript, egyszerű
   fájlmásolás + parancsikonok), még nincs elkezdve

5. Végigtesztelési checklist még nincs összeállítva

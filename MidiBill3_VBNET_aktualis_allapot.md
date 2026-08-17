# MidiBill3 (VB.NET) – Aktuális állapot összefoglalója

Ez a dokumentum a **VB6 → VB.NET portolás jelenlegi állapotát** foglalja
össze, hogy egy új beszélgetés gyorsan fel tudjon zárkózni anélkül, hogy
végig kellene olvasni a teljes fejlesztési történetet. A projekt **éles,
működő állapotban van**, aktívan továbbfejlesztés alatt.

---

## Alapadatok

- **Verzió**: 1.3.0 (SemVer: Major.Minor.Patch - Major = kompatibilitást
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
│                                   transzponálás, csatorna-választó, gombok
├── HangszerPanel.vb / .Designer.vb - Egy hangszerpanel (UserControl), 4 fizikai
│                                   példány, amik a kiválasztott csatornacsoportot
│                                   "mutatják"
├── ApplicationEvents.vb         - Globális hibanaplózás (HibaNaplo.txt)
├── Modellek/
│   ├── Beallitasok.vb           - Adatmodell (16 csatorna) + JSON mentés/
│   │                               betöltés + .mb3 import
│   ├── MidiEngine.vb            - MIDI be-/kimenet, fan-out (16 csatorna),
│   │                               split pont, transzponálás logika
│   ├── HangszerLista.vb         - 128 General MIDI hangszernév
│   └── Hangnevek.vb             - MIDI hangszám -> hangnév (pl. "C4")
├── Segedprogramok/               - PresetKonvertalo, Mb3Konvertalo
├── Adatok/                      - Régi, megőrzött preset fájlok (verziókövetve)
├── MidiBill3.ico                - Alkalmazásikon
└── Kezdet.json                  - Automatikus induló-állapot (NEM verziókövetve)
```

## Elkészült funkciók

**Alapfunkciók (a VB6-os verzióból átvéve, letisztítva)**
- MIDI kimeneti csatornák, mindegyiken saját hangszer, hangerő, pan (Tér),
  vibrato beállítással (lásd lent: most már **16** csatorna, nem 4)
- Csatornánkénti be-/kikapcsolás (Aktív checkbox), automatikus
  "csatlakozás" a már lenyomott hangokhoz visszakapcsoláskor
- Note-tracking rendszer (célzott hangkikapcsolás, nem vak 128-as seprés)
- "Csend legyen!" pánikgomb (mind a 16 csatornát végigsöpri) - dőlt+félkövér
  felirattal kiemelve
- "Kilépés" gomb/menü - félkövér felirattal kiemelve
- "Kilépés mentés nélkül" menüpont (Fájl menü): megerősítő kérdéssel
- Pedál-inverzió (Fordított pedál működés checkbox)
- Hibatűrő MIDI-küldés
- Egységes `FormClosing` minden kilépési útvonalra

**Fájlkezelés**
- JSON mentés/betöltés, eszköznév-alapú MIDI be-/kimenet tárolás
- Régi `.mb3` fájlok importálása (csak 1-4. csatornára, 5-16. inaktív marad)
- `Kezdet.json` automatikus betöltés induláskor / mentés kilépéskor
- Fájlműveletek külön-külön jegyzik meg a legutóbb használt mappájukat
- `Adatok` mappa: verziókövetett preset-gyűjtemény, build/publish
  automatikusan másolja a kimeneti mappába is
- **Segédprogramok** (`PresetKonvertalo`, `Mb3Konvertalo`): önálló
  konzolos projektek, linkelt fájlokkal (`Beallitasok.vb` stb.),
  automatikusan öröklik a 16-csatornás modellt is

**Split Point**
- Csatornánként külön be-/kikapcsolható **Alul** és **Felül** zóna
- Split pont beállítása kézzel (legördülő lista) vagy "Tanulás" gombbal
- Élőben NEM retroaktív, Note Off mindig "hazatalál"
- Visszamenőleges kompatibilitás: régi fájloknál minden csatorna
  alapból mindkét zónában szól

**Globális Transzponálás**
- `GroupBox` a Split pont alatt: fejléc "Transzponálás: +N/-N/0"
- 4 gomb: Félhang fel, Félhang le, Oktáv fel, Oktáv le (±24 félhang)
- A Split pont zóna-döntését NEM érinti (az mindig az EREDETI hangot nézi)
- Élő váltás NEM retroaktív
- `MidiEngine`-ben egy `kimenoHang` nyilvántartás biztosítja, hogy a
  Note Off mindig a ténylegesen kiküldött (transzponált) hangmagasságra
  menjen

**16 MIDI csatorna támogatása (v1.3.0, legújabb, nagy funkció)**
- A `Beallitasok`/`MidiEngine` adatmodellje **4-ről 16 csatornára**
  bővült (index 0-15 = 1-16. csatorna)
- A 4 fizikai `HangszerPanel` **"kirakattá"** vált: mindig az épp
  kiválasztott 4-es csatornacsoportot mutatja/szerkeszti, nem egy fix
  csatornát
- Egy 16 elemű `csatornaAllapotok` tömb (`MainForm.vb`-ben) tárolja
  **mindig, a háttérben** az összes csatorna élő állapotát - amikor
  csoportot váltasz, a panelek erről töltődnek újra
- **Csoportválasztó**: `grpCsatornaValaszto` GroupBox 4 `RadioButton`-nal
  (1-4 / 5-8 / 9-12 / 13-16), induláskor `radCsoport1.Checked = True`
- **Fontos elv**: a csoportválasztó CSAK azt dönti el, mit látsz/
  szerkesztesz a képernyőn - a MIDI-küldés szempontjából **mind a 16
  csatorna egyszerre szólhat**, függetlenül attól, melyik csoport van
  épp kijelölve
- **10. csatorna (dob)**: sosem aktiválható/konfigurálható - a
  `MidiEngine.CsatornaAktiv` setter védi (index 9 mindig False), a UI-n
  a panel `Enabled = False`-ra állítva, szürkítve látszik, amikor a
  "9-12" csoport van kiválasztva
- Visszamenőleges kompatibilitás: régi 4-csatornás presetek betöltésekor
  az 5-16. csatornák alapból **inaktívak** (a `CsatornaBeallitas.Aktiv`
  alapértéke `False`, kivéve amit a fájl explicit felülír)
- Teljes form-elrendezés újratervezve: `ClientSize = 942 x 762`,
  szimmetrikus **25 px margó** minden oldalon; a hangszerpanelek és a
  jobb oldali vezérlők (`grpVolume`, `grpSplit` stb.) X/Y-koordinátái
  egymáshoz igazítva (pl. `grpSplit` teteje = 4. hangszerpanel teteje)

## Ismert, tudatosan meghozott döntések

- **Hangnevek nemzetközi konvencióval** (C, C#, D... nem magyar szolmizáció)
- **ReadyToRun compilation kikapcsolva**, **Trim unused assemblies kikapcsolva**
- **Framework-dependent publish** (nem self-contained)
- **Panic gomb pufferelés szándékosan nincs**
- **ComboBox-ok explicit elvesztik a fókuszt kiválasztás után**
  (`ActiveControl = Nothing`)
- **RadioButton, nem egymást kizáró CheckBox-ok** a csatorna-választóhoz
  (WinForms natívan kizárja egymást egy konténeren belül)

## Globális hibanaplózás

Az `ApplicationEvents.vb` elkap minden kezeletlen kivételt, `HibaNaplo.txt`-be
ír. Natív (nem managed) összeomlásokat ez sem tud elkapni - ilyenkor a
Windows Eseménynaplót kell megnézni.

## Következő lépések / nyitott témák

Fontossági/tervezett sorrendben:

1. **Csatornánkénti transzponálás** - új `TrackBar` a `HangszerPanel`-en,
   a globális transzponálás mellett/kiegészítéseként. **Most, hogy 16
   csatorna van, ezt a bővített modellhez kell tervezni** (nem a régi,
   4-csatornás alapokhoz) - a pontos összeadódási logika (globális +
   csatornánkénti hogyan kombinálódik) még nincs eldöntve

2. **Reverb és Chorus csatornánként** - elvi terv kész (General MIDI
   Controller Change: Reverb=91, Chorus=93, ugyanaz a minta mint
   Hangerő/Pan/Vibrato). Eszközfüggő tényleges hatás - ki kell próbálni
   a konkrét MIDI-kimeneti eszközön

3. **MIDI felvétel/visszajátszás** - elvi terv, eldöntött kulcskérdéssel:
   - **A NYERS BEMENETET vesszük fel** (pedál-inverzió UTÁN, split pont/
     transzponálás/csatorna-szétosztás ELŐTT), NEM a feldolgozott kimenetet
   - Visszajátszáskor a felvett jel újra végigmegy a
     `MidiEngine.UzenetKuldes()`-en, a **visszajátszás pillanatában
     érvényes** beállításokkal (rugalmas újrajátszás)
   - NAudio.Midi ad építőelemeket, de a pontos időzítésű lejátszó saját
     fejlesztés lesz
   - Még nincs elkezdve

4. **Telepítő (Inno Setup)** - eldöntött irány (`.iss` szkript), még
   nincs elkezdve

5. Végigtesztelési checklist még nincs összeállítva

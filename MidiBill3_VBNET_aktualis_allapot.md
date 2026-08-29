# MidiBill3 (VB.NET) – Aktuális állapot összefoglalója

Ez a dokumentum a **VB6 → VB.NET portolás jelenlegi állapotát** foglalja
össze, hogy egy új beszélgetés gyorsan fel tudjon zárkózni anélkül, hogy
végig kellene olvasni a teljes fejlesztési történetet. A projekt **éles,
működő állapotban van**, aktívan továbbfejlesztés alatt.

---

## Alapadatok

- **Verzió**: 1.9.0 (SemVer), a **`MidiBill3.vbproj`-ban közvetlenül**
  (az "Extra MIDI üzenetek a játék kezdetén" funkció még nem kiadott
  verzióban van, lásd `CHANGELOG.md` - [Nem kiadott])
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
  alapértelmezett `True`, `ClientSize = 958 x 974`
- **Címsor**: `"MIDI billentyűzet megszólaltatása 16 csatornán : <beállítások
  fájlneve>, <hangszerlista fájlneve>"` (`CimSorUjrarajzolas()`)
- **README**: kétnyelvű - `README.md` (magyar, alapértelmezett) +
  `README.en.md` (angol), kattintható nyelv-váltó linkkel mindkét fájl tetején

## Projektstruktúra

```
MidiBill3/                        - EGYETLEN projekt
├── MainForm.vb / .Designer.vb   - Fő ablak
├── HangszerPanel.vb / .Designer.vb - Egy hangszerpanel (UserControl), 4 fizikai
│                                   példány, a kiválasztott csatornacsoportot mutatja
│                                   (magasság 378, keret 408 - Her/Tér/Vib/Chorus/
│                                   Oktáv csúszkák + Aktív/Alul/Felül checkbox-ok)
├── ApplicationEvents.vb         - Globális hibanaplózás (HibaNaplo.txt)
├── Modellek/
│   ├── AppInfo.vb               - Verziószám lekérdezés
│   ├── Beallitasok.vb           - Adatmodell (16 CSATORNA, MIND szerkeszthető)
│   │                               + JSON mentés/betöltés
│   ├── MidiEngine.vb            - MIDI be-/kimenet, fan-out (16 csatorna,
│   │                               NINCS letiltott csatorna), split pont,
│   │                               transzponálás, Bank Select-alapú hangszerváltás
│   ├── HangszerLista.vb         - KÉT hangszerlista: Nevek (globális, 15 csatorna)
│   │                               és DobNevek (dedikált, 10. csatorna) -
│   │                               Betolt()/BetoltDobKeszletet()/Ment()
│   ├── SoundFontOlvaso.vb       - .sf2 "phdr" beolvasása: hangszerek (Nevek) ÉS
│   │                               dobkészlet-presetek (Dobok, ISMERT DOBKÉSZLET-
│   │                               BANKOK: 120, 127, 128) külön
│   └── Hangnevek.vb             - MIDI hangszám -> hangnév konverzió
├── Adatok/                      - Preset fájlok + hangszerlista fájlok:
│                                   GM.lst (hangszerek) + GM.drm (dobkészletek,
│                                   PÁROSÍTVA az alapnév alapján), Teszt.lst,
│                                   TesztGS.lst, JJazzLab-SoundFont.lst/.drm stb.
├── README.md                    - Magyar README (alapértelmezett)
└── README.en.md                 - Angol README
```

## Elkészült funkciók

**Alapfunkciók**: 16 MIDI csatorna (MIND szerkeszthető), csatorna-választó,
Split Point, Globális Transzponálás, csatornánkénti oktáv-transzponálás
(±2, azonnali/retroaktív hatással), globális Reverb (CC91), pedál-inverzió
(passzív "mód"-kapcsoló), "Csend legyen!" pánikgomb, "Kilépés"/"Kilépés
mentés nélkül", MIDI eszközlista frissítés+validálás, olvasható MIDI
üzenet-kijelzés (max. 10x/mp), hibatűrő MIDI-küldés, robusztus JSON
hibakezelés. (Részletek: forráskód vagy korábbi fejlesztési történet.)

**Fájlkezelés**: JSON mentés/betöltés, eszköznév-alapú MIDI be-/kimenet
tárolás, régi `.mb3` import, `Kezdet.json` automatikus be-/kimentés,
"Segédprogramok" menü (Preset fájlok frissítése, Régi .mb3 fájlok
konvertálása).

**Bank Select-alapú kiterjesztett hangszertámogatás (GS/XG-kompatibilis)**:
`MidiEngine.ProgramValtas()` mindig küld Bank Select (CC0+CC32) + Program
Change-t, `.lst` formátum `bank.program=Név` szintaktikával, `HangszerPanel`
`Név (B<bank>)` megjelenítéssel. (Részletek: korábbi fejlesztési történet.)

**10. csatorna feloldása + dedikált dobkészlet-hangszerlista (.lst/.drm párosítás)**:
`HangszerLista.DobNevek` önálló Dictionary a 10. csatornához,
`HangszerPanel.HangszerListaForrasBeallitasa()` a panel forrás-cseréjéhez,
a `.drm` fájlnév mindig az aktív `.lst` alapnevéből származik és
automatikusan párosodik. A 10. csatorna teljesen szerkeszthető.

**SoundFont-import: bővíthető dobkészlet-bank-felismerés**
- `SoundFontOlvaso.ismertDobBankok() = {120, 127, 128}` - ELŐSZÖR csak a
  `128`-at ismertük (SF2-szerkesztői konvenció, pl. JJazzLab), majd a
  `120`-at (GM2 szabvány), most a `127`-et is (`SGM-v2_02` SoundFont
  tesztelése közben derült ki: `"GS STANDARD"`, `"GS ROOM"`, `"GS
  TR-909"` stb. nevű dobkészlet-presetek ebben a bankban voltak) - a
  bővíthető tömb-architektúra miatt mindegyik csak **egysoros**
  módosítás volt

**"Alaphelyzetbe állítás" menüpont**
- Új top-level **"Beállítások"** menü a Fájl után, benne az
  **"Alaphelyzetbe állítás"** menüponttal
- Megerősítő kérdés: *"Minden jelenlegi beállítás törlődni fog!
  Folytatja?"*
- A visszaállítás **pontosan úgy** viselkedik, mintha a program a
  `Kezdet.json` beolvasása **NÉLKÜL** indulna:
  - Hangszerlista → **`GM.lst`/`GM.drm`** (elveti az esetleg betöltött
    egyedi/SoundFont-listát)
  - Csatornák → beépített alapérték (csak az 1. csatorna aktív)
  - Split pont → `60` (C4), Transzponálás → `0`, Reverb → `0%`, Pedál
    → nem invertált, "MIDI beállító üzenetek küldése" checkbox →
    kikapcsolva
- **A MIDI In/Out kiválasztás SZÁNDÉKOSAN változatlan marad** (ez
  eszközkapcsolat, nem "beállítás" a felhasználó szemszögéből - külön,
  tudatos döntés volt)
- **Nem törli** a `Kezdet.json` fájlt a lemezről - normál kilépéskor az
  alaphelyzetbe állított állapot felülírja majd, ha a felhasználó nem a
  "Kilépés mentés nélkül" opciót választja
- Teljes egészében **meglévő metódusok újrahasznosításával** valósult
  meg (`HangszerListaBetoltese`, `CsatornaAllapotokInicializalasa`,
  `TranszponalasFrissites`, `CsatornaPanelekFrissitese`,
  `OsszesCsatornaUjrakuldese`, `CimsorFrissitese`) - nincs új, önálló
  logika, `AlaphelyzetbeAllitas()` metódus csak összefűzi ezeket

**Csatornánkénti Chorus (v1.9.0)**
- Új csúszka a `HangszerPanel`-en (0-127, MIDI **CC93**) - pontosan az
  Oktáv-csúszka bevált mintáját követve: `CsatornaBeallitas.Chorus`
  mező, `HangszerPanel.Chorus` property + `ChorusValtozott` esemény,
  mentés/betöltés/tartomány-validáció (`Korlatoz(cs.Chorus, 0, 127)`,
  bekapcsolva az `ertekKorrigalva` figyelmeztető rendszerbe),
  `OsszesCsatornaUjrakuldese`-be is bekötve
- `HangszerPanel` magassága 378, keret 408 (a Chorus sor beillesztése
  miatt nőtt); teljes form-elrendezés konzisztensen igazítva
- **Design-frissítés**: a jobb oldali sáv átrendezve - a Reverb doboz
  feljebb került, közvetlenül az Alap hangerő mellé; a Transzponálás
  doboz és a Csend legyen!/Kilépés gombok között akkor még
  szándékosan üresen hagyott hely ekkor még egy jövőbeli funkciónak
  volt fenntartva - ezt azóta betöltötte az "Extra MIDI üzenetek a
  játék kezdetén" funkció (lásd lentebb)
- **Ablakszélesség-igazítás**: a második hangszerpanel és a jobb oldali
  vezérlő-oszlop jobbra tolva (a bal margó változatlan maradt), a form
  szélessége 942-ről 958 pixelre nőtt - közel négyzetes (958 x 974)
  arány, szimmetrikusabb elrendezés a magasabbá vált panelek mellett
- **Alap hangerő / Reverb feliratok**: az "Alap hangerő" felirat is
  kiírja a százalékjelet (`"Alap hangerő: 70%"`), a "Reverb" felirat
  pedig "Visszhang"-ra cserélve mindhárom előfordulási helyén
- **Split pont visszajelzés bővítve**: a `"Beállítva: <hangnév>"`
  üzenet eddig csak a "Tanulás" gombbal történő split pont
  beállításnál jelent meg a `lblSplitInfo` címkén - mostantól a
  `cmbSplitPont` legördülőből történő közvetlen kiválasztás is
  ugyanezt az üzenetet írja ki

**Extra MIDI üzenetek a játék kezdetén (ÚJ, még nem kiadott verzióban)**
- Új `grpExtraMidi` GroupBox a jobb oszlopban ("Extra MIDI üzenetek a
  játék kezdetén"), pontosan azon a helyen, amit a Chorus-funkció
  (v1.9.0) idején szándékosan szabadon hagytunk - most felhasználva
- Két gyermek-vezérlő: `chkMidiBeallitoUzenetek` checkbox ("MIDI
  beállító üzenetek küldése") és `lblExtraMidiAllapot` állapotjelző
  felirat (kezdetben üres)
- Működés: bepipálás → felirat "Várakozás billentyűre..."; az első
  ténylegesen lejátszott hangnál (Note On) a `MidiUzenetFeldolgozasa`
  meghívja az új `AktivCsatornakAllapotanakKuldese()` metódust, ami
  végigmegy a 16 csatornán, és csak az AKTÍV csatornákra újraküldi a
  Program Change, Volume, Pan, Vibrato, Chorus és Reverb üzeneteket;
  ezután a checkbox kódból kikapcsol, a felirat pedig "MIDI üzenetek
  kiküldve."-re vált - mindez EGYETLEN alkalommal fut le bepipálásonként
  (nincs folyamatos újraküldés játék közben)
- Ha a checkbox-ot a felhasználó manuálisan kapcsolja ki (mielőtt
  billentyűt ütne), a felirat visszaáll üresre
  (`chkMidiBeallitoUzenetek_CheckedChanged` eseménykezelő)
- Célja: egy külső programmal megvalósított MIDI-felvételhez (lásd
  "Következő lépések" 4. pont) szükséges, hogy a rögzített fájl
  tartalmazza a teljes csatorna-beállítást (pl. hangszíneket), ne csak
  a ténylegesen lejátszott hangokat
- Az "Alaphelyzetbe állítás" is kikapcsolt állapotba állítja ezt a
  checkbox-ot

## Ismert, tudatosan meghozott döntések

- Hangnevek nemzetközi konvencióval (C, C#, D...)
- ReadyToRun/Trim kikapcsolva, Framework-dependent publish
- Panic gomb pufferelés szándékosan nincs
- ComboBox-ok explicit elvesztik a fókuszt kiválasztás után
- RadioButton a csatorna-választóhoz (natív kizárás)
- Verziószám közvetlenül a `MidiBill3.vbproj`-ban
- **Kódmódosításoknál a meglévő kommenteket MINDIG meg kell tartani**
  (explicit felhasználói elvárás, memóriában rögzítve)
- Hangszerlista fájlformátum: `index=Név` vagy `bank.program=Név`
  szöveges fájl (nem JSON/XML)
- Bank Select mindig MSB=0, mindig elküldve (még bank=0-nál is)
- `.lst`/`.drm` párosítás fájlnév-alapú, nem preset-be mentett mező
- Dobkészlet-bankok bővíthető listája (`ismertDobBankok`) - eddig
  háromszor bővült ({128} → {120,128} → {120,127,128}), valószínűleg
  a jövőben is bővülhet még, ha újabb SoundFont-konvencióval
  találkozunk
- "Alaphelyzetbe állítás" a MIDI In/Out kiválasztást SZÁNDÉKOSAN nem
  érinti
- Az "Extra MIDI üzenetek a játék kezdetén" funkció csak az AKTÍV
  csatornákra küld üzenetet, és csak EGYETLEN alkalommal, bepipálásonként
  - ez tudatos döntés volt, hogy ne terhelje feleslegesen a MIDI
  kimenetet folyamatos játék közben

## Globális hibanaplózás

Az `ApplicationEvents.vb` elkap minden kezeletlen kivételt, `HibaNaplo.txt`-be
ír. Natív (nem managed) összeomlásokat ez sem tud elkapni.

## Következő lépések / nyitott témák

1. **GS-támogatás további tesztelése** - folyamatban, a felhasználó
   több különböző SoundFont-tal teszteli (JJazzLab, SGM-v2_02 eddig)

2. **10. csatorna működésének további tesztelése** - konkrétan, hogy a
   Bank Select + Program Change mechanizmus a dobkészlet-választásnál
   minden esetben helyesen viselkedik-e a felhasználó szintetizátorán -
   MÉG NINCS végleges visszajelzés erről

3. **Metronóm-funkció** - a 10. csatorna feloldásának egyik indoka volt;
   konkrét ütős hangok (pl. Claves, Side Stick - fix GM percussion
   billentyű-kiosztás) Note On üzenetként való kiküldése, FÜGGETLENÜL a
   dobkészlet-választástól. MÉG NINCS megtervezve/megvalósítva - a
   felhasználóval már átbeszéltük az alapkövetelményeket (Start/Stop,
   BPM, ütem 3/4-4/4 stb.), konkrét megvalósítási ötletekre vár

4. **MIDI felvétel/visszajátszás** - elvi terv, eldöntött kulcskérdéssel:
   a NYERS BEMENETET vesszük fel, visszajátszáskor újra végigmegy a
   `MidiEngine.UzenetKuldes()`-en. Még nincs elkezdve. A felhasználó
   egyelőre egy KÜLSŐ programmal valósítja meg a MIDI-felvételt - ehhez
   készült az "Extra MIDI üzenetek a játék kezdetén" funkció (lásd
   "Elkészült funkciók"), hogy a külső felvevő megkapja a teljes
   csatorna-állapotot

5. **Telepítő (Inno Setup)** - eldöntött irány, még nincs elkezdve

6. **Performancia - MIDI-küldés leválasztása a UI-szálról** - elvi téma,
   átbeszélve, eddig nem volt probléma, nem sürgős

7. Végigtesztelési checklist még nincs összeállítva

8. **Többnyelvű GUI (magyar + angol, később bővíthető)** - ELHALASZTVA,
   jelenleg nem sürgős, mivel egyelőre csak a felhasználó saját maga
   használja a programot, neki tökéletesen megfelel a magyar felület.
   A fejlesztés (kódkommentek, változónevek) továbbra is magyarul
   folytatódik, csak a MEGJELENÍTETT felület lenne választhatóan
   angol is. Végiggondolt megközelítés, ha eljön az ideje:
   - Kulcs-alapú szöveges fájlok (`lang_hu.txt`, `lang_en.txt`,
     `kulcs=szöveg` formátumban) - ugyanaz a minta, mint a
     `HangszerLista` fájlbetöltése, NEM a WinForms beépített
     resx/satellite assembly-alapú lokalizációja (utóbbi nehezebben
     illeszkedne a projekt stílusához és a dinamikusan összerakott
     szövegekhez, pl. `CsatornaFelirat()`)
   - Egy `Nyelv` modul (`Szoveg("kulcs")` lekérő függvénnyel) +
     egy "Beállítások" menü alatti "Nyelv" almenü Magyar/Angol
     választással, AZONNALI váltással (nincs szükség újraindításra)
   - A tényleges munka dandárja a meglévő, szó szerint bedrótozott
     magyar szövegek kulcsosítása lenne: `MainForm.Designer.vb`
     menü/gomb/checkbox feliratai, a `HangszerPanel` feliratai, a
     dinamikus sablonszövegek (`CsatornaFelirat()`,
     `CimSorUjrarajzolas()`), és becsülhetően 30-40 MsgBox-üzenet -
     összesen kb. 100-200 különálló szöveg, mechanikus, alacsony
     kockázatú, de munkaigényes refaktor
   - A kiválasztott nyelvet NEM a `Beallitasok` (JSON preset) tárolná,
     hasonló logikával, mint a MIDI In/Out kiválasztás: ez
     eszköz/környezet-szintű beállítás, nem preset-tartalom, ezért
     külön kellene tárolni (pl. saját kis fájl vagy `My.Settings`),
     hogy se preset-váltás, se "Alaphelyzetbe állítás" ne érintse

9. **Elméleti téma, lezárva**: iOS/iPadOS-portolás - távoli asztali
   elérés (RDP/Chrome Remote Desktop) a gyakorlati megoldás, ha a
   fizikai MIDI billentyűzet a Windows gépnél marad

10. **Elméleti téma, lezárva**: Claude Desktop Cowork mód - a
    felhasználó megpróbálta, de az adott beszélgetésben nem volt
    elérhető a váltás, ezért a hagyományos (fájl-beillesztős)
    munkamódszernél maradtunk

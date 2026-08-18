# MidiBill3 (VB.NET) – Aktuális állapot összefoglalója

Ez a dokumentum a **VB6 → VB.NET portolás jelenlegi állapotát** foglalja
össze, hogy egy új beszélgetés gyorsan fel tudjon zárkózni anélkül, hogy
végig kellene olvasni a teljes fejlesztési történetet. A projekt **éles,
működő állapotban van**, aktívan továbbfejlesztés alatt.

---

## Alapadatok

- **Verzió**: 1.3.0 (SemVer: Major.Minor.Patch), **egységesen kezelve**
  a `Directory.Build.props`-ban (a megoldás gyökerében) - mindhárom
  projekt (`MidiBill3`, `PresetKonvertalo`, `Mb3Konvertalo`) innen
  örökli automatikusan, nincs saját `<Version>` egyik `.vbproj`-ban sem
- **Cél keretrendszer**: .NET 10 (LTS)
- **UI**: WinForms
- **MIDI réteg**: NAudio.Midi 2.3.0 (+ NAudio.Core 2.3.0)
- **Fájlformátum**: JSON (`System.Text.Json`), emberi olvasást segítő
  `//` megjegyzésekkel kiegészítve (hangszernevek, split pont hangneve,
  transzponálás félhang-szövege, csatorna-sorszámok, fejléc) – ezeket a
  `Betoltes()` a `JsonCommentHandling.Skip` miatt simán átugorja
- **Fejlesztői környezet**: Visual Studio 2026 Community, két gépen
  (`C:\Dev\MidiBill3` mindkettőn – **soha ne Documents/OneDrive alá**)
- **Verziókezelés**: GitHub, rendszeres commit+push munkafolyamattal
- **Alkalmazásikon**: `MidiBill3.ico`, beállítva projekt- és Form-szinten is

## Projektstruktúra

```
MidiBill3/
├── MainForm.vb / .Designer.vb   - Fő ablak
├── HangszerPanel.vb / .Designer.vb - Egy hangszerpanel (UserControl), 4 fizikai
│                                   példány, amik a kiválasztott csatornacsoportot
│                                   "mutatják"
├── ApplicationEvents.vb         - Globális hibanaplózás (HibaNaplo.txt)
├── Directory.Build.props        - Egységes <Version> mindhárom projekthez
├── Modellek/
│   ├── AppInfo.vb               - Közös AppVerzio() függvény (linkelve mindhárom
│   │                               projektbe)
│   ├── Beallitasok.vb           - Adatmodell (16 csatorna) + JSON mentés/
│   │                               betöltés + .mb3 import + hibavédelem
│   ├── MidiEngine.vb            - MIDI be-/kimenet, fan-out (16 csatorna),
│   │                               split pont, transzponálás logika
│   ├── HangszerLista.vb         - 128 General MIDI hangszernév
│   └── Hangnevek.vb             - MIDI hangszám -> hangnév (pl. "C4")
├── PresetKonvertalo/            - Segédprogram: régi .json presetek tömeges
│                                   frissítése az aktuális formátumra
├── Mb3Konvertalo/                - Segédprogram: régi .mb3 -> .json tömeges
│                                   konvertálás
├── Adatok/                      - Régi, megőrzött preset fájlok (verziókövetve)
├── MidiBill3.ico                - Alkalmazásikon
└── Kezdet.json                  - Automatikus induló-állapot (NEM verziókövetve)
```

**Megjegyzés**: a `PresetKonvertalo`/`Mb3Konvertalo` egyelőre a projekt
gyökerében, külön-külön mappában vannak (nem egy közös `Segedprogramok`
mappában) - a `MidiBill3.vbproj` mindkettőt egyenként zárja ki az implicit
fájlfelismerésből. Az egységes mappába rendezés felmerült ötlet, de még
nincs megvalósítva.

## Elkészült funkciók

**Alapfunkciók** - 16 MIDI csatorna, csatorna-választó (1-4/5-8/9-12/13-16
RadioButton-okkal), Split Point, Globális Transzponálás, pedál-inverzió,
"Csend legyen!" pánikgomb, "Kilépés"/"Kilépés mentés nélkül", hibatűrő
MIDI-küldés, egységes `FormClosing`. (Részletek: lásd korábbi fejlesztési
történet vagy a forráskód.)

**Fájlkezelés**
- JSON mentés/betöltés, eszköznév-alapú MIDI be-/kimenet tárolás
- Régi `.mb3` fájlok importálása (csak 1-4. csatornára)
- `Kezdet.json` automatikus betöltés induláskor / mentés kilépéskor
- Fájlműveletek külön-külön jegyzik meg a legutóbb használt mappájukat
- `Adatok` mappa automatikusan másolva a kimeneti mappába build/publish-kor
- **Segédprogramok** (`PresetKonvertalo`, `Mb3Konvertalo`): linkelt
  fájlokat használnak, automatikusan öröklik a 16-csatornás modellt és a
  verziószám-mentést is

**MIDI eszközkezelés (finomítva)**
- **"MIDI eszközök frissítése" gomb** (a `grpMidiOut` alatt, a hangszerpanel
  aljához igazítva): újra lekérdezi a Windows-tól az eszközöket, megtartja
  a jelenlegi kiválasztást, ha még elérhető
- **`EszkozListaEgyezikE()`**: minden MIDI In/Out kiválasztás előtt frissen
  összehasonlítja a `ComboBox` tartalmát a Windows aktuális eszközlistájával
  - ez védi ki azt a "csendes" hibát, amikor egy eszköz ki-/behúzása miatt
    eltolódott index miatt a program véletlenül egy MÁSIK, létező eszközt
    nyitna meg hibaüzenet nélkül
  - eltérés vagy sikertelen megnyitás esetén egy üzenet + azonnali,
    automatikus lista-frissítés (nincs külön rákérdezés)
- **Induláskori bemenet/kimenet-hiány kezelése**:
  - nincs bemeneti eszköz → figyelmeztetés, de a program **folytatódik**
    (üres `cmbMidiIn`, pótolható a frissítés gombbal)
  - nincs kimeneti eszköz → figyelmeztetés + a program **kilép**
    (`Environment.Exit(1)` - azonnali, garantált leállás)
  - korábbi hiba javítva: az `Application.Exit()` nem állította meg
    ténylegesen a `MainForm_Load` további futását, emiatt félkész,
    működésképtelen ablak jelenhetett volna meg MIDI kimenet nélkül

**JSON betöltés hibakezelése (finomítva)**
- `Kiegeszit16Csatornara`: a `Csatornak` listát pontosan 16 elemre
  egészíti ki/vágja, ÉS a `null` elemeket is friss, alapértelmezett
  `CsatornaBeallitas`-ra cseréli (véd `NullReferenceException` ellen egy
  kézzel szerkesztett, sérült JSON esetén)
- **Figyelmeztető üzenet**, ha a betöltött fájl nem létező MIDI
  eszköznevet tartalmazott ("...amit a program figyelmen kívül hagyott")
- **Figyelmeztető üzenet**, ha bármely érték a `Korlatoz()` miatt
  ténylegesen módosult (csak akkor jelenik meg, ha valódi eltérés történt)
- **10. csatorna (dob) - explicit mentés/betöltés védelem**: mivel a UI-ból
  sosem konfigurálható, az adatait **sosem mentjük** (mindig friss
  alapérték kerül a fájlba) és **sosem olvassuk be** (a fájlban esetlegesen
  szereplő adatot figyelmen kívül hagyjuk) - a `MidiEngine.CsatornaAktiv`
  setter is véd (index 9 mindig False)
- **Csatorna-sorszám kommentek** a JSON-ban (`{  // 1. csatorna` stb.) -
  tisztán tájékoztató, a betöltés továbbra is a lista POZÍCIÓJA alapján
  azonosít, nem ezt a kommentet olvassa
- **`Verzio` mező**: a mentéskori programverzió elmentve a JSON-ba (tisztán
  tájékoztató, nem befolyásolja a betöltést; régi fájloknál üres)

## Ismert, tudatosan meghozott döntések

- Hangnevek nemzetközi konvencióval (C, C#, D...)
- ReadyToRun/Trim kikapcsolva, Framework-dependent publish
- Panic gomb pufferelés szándékosan nincs
- ComboBox-ok explicit elvesztik a fókuszt kiválasztás után
- RadioButton a csatorna-választóhoz (natív kizárás)
- **Verziószám egységesen a `Directory.Build.props`-ban** - ha verziót
  lépsz, csak EZT az egy fájlt kell módosítani, mindhárom projekt
  (fő program + 2 konvertáló) automatikusan követi

## Globális hibanaplózás

Az `ApplicationEvents.vb` elkap minden kezeletlen kivételt, `HibaNaplo.txt`-be
ír. Natív (nem managed) összeomlásokat ez sem tud elkapni.

## Következő lépések / nyitott témák

Fontossági/tervezett sorrendben:

1. **Csatornánkénti transzponálás** - új `TrackBar` a `HangszerPanel`-en,
   a globális transzponálás mellett/kiegészítéseként. A bővített,
   16-csatornás modellhez kell tervezni - az összeadódási logika
   (globális + csatornánkénti) még nincs eldöntve

2. **Reverb és Chorus csatornánként** - elvi terv kész (General MIDI
   Controller Change: Reverb=91, Chorus=93). Eszközfüggő tényleges hatás -
   ki kell próbálni a konkrét MIDI-kimeneti eszközön

3. **MIDI felvétel/visszajátszás** - elvi terv, eldöntött kulcskérdéssel:
   - **A NYERS BEMENETET vesszük fel** (pedál-inverzió UTÁN, split pont/
     transzponálás/csatorna-szétosztás ELŐTT), NEM a feldolgozott kimenetet
   - Visszajátszáskor a felvett jel újra végigmegy a
     `MidiEngine.UzenetKuldes()`-en, a **visszajátszás pillanatában
     érvényes** beállításokkal (rugalmas újrajátszás)
   - NAudio.Midi ad építőelemeket, a pontos időzítésű lejátszó saját
     fejlesztés lesz. Még nincs elkezdve

4. **Telepítő (Inno Setup)** - eldöntött irány (`.iss` szkript), még
   nincs elkezdve

5. **`Segedprogramok` közös mappa** - felmerült ötlet a két konvertáló
   egy közös mappába rendezésére (jelenleg külön-külön vannak a gyökérben,
   külön-külön kizárva a fő projektből) - nincs sürgető, bármikor
   megcsinálható

6. Végigtesztelési checklist még nincs összeállítva

7. **Elméleti téma, lezárva**: felmerült az iOS/iPadOS-portolás kérdése -
   megállapítottuk, hogy ez egy jelentős, hónapokat igénylő újraírás lenne
   (.NET MAUI + VB.NET class library reuse, vagy teljes natív Swift
   újraírás). Rövid távú megoldásként a Windows gép távoli asztali
   eléréssel (RDP/Chrome Remote Desktop) használható iPadről, feltéve
   hogy a fizikai MIDI billentyűzet a Windows gépnél marad (a hang és a
   MIDI-feldolgozás helyben történik, alacsony késleltetéssel; a hangot
   NEM szabad a távoli munkamenetbe is belevonni).

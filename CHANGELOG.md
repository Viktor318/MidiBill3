# Changelog

Ez a fájl a MidiBill3 (VB.NET) projekt eddigi fejlesztési történetét
foglalja össze, verziónkénti bontásban. A formátum lazán a
[Keep a Changelog](https://keepachangelog.com/) elveit követi.

> **Megjegyzés**: a korai (1.1.0 és 1.3.0 közötti) verzió-határok
> közelítő rekonstrukción alapulnak - érdemes összevetni a tényleges
> `git log` előzményekkel, és ha szükséges, pontosítani.

---

## [1.11.0] - 2026-09-03

### Hozzáadva
- **"MIDI fájlok módosítása az aktuális beállításokkal"** - új menüpont a
  Segédprogramok alatt (`Modellek/MidiFajlSzerkeszto.vb`). Tetszőleges,
  akár máshonnan (DAW, más program) származó **format 1** Standard MIDI
  File (`.mid`) fájl(ok) elejére beszúrja az AKTÍV csatornák teljes MIDI-
  állapotát (hangszín, hangerő, tér, vibrato, chorus, reverb) - ugyanazt
  a logikát használva, mint a "MIDI beállító üzenetek küldése" funkció
  élőben. A beszúrás egy ÚJ, önálló trackként történik, közvetlenül a 0.
  (conductor) track után - a meglévő trackek bájtjaihoz nem nyúl hozzá
  (nincs running status újraszámolás, nincs delta-time lánc bontása). A
  bemeneti fájlokat nem írja felül: a módosított másolatok a meglévő
  konvertáló segédprogramokéhoz hasonlóan egy "Konvertalva" almappába
  kerülnek, egyszerre több fájl is kijelölhető (`Multiselect`).
  **Védelem**: egy aktív csatorna is csak akkor kap beállítást, ha a
  fájl valamelyik MEGLÉVŐ trackje ténylegesen tartalmaz is arra a
  csatornára vonatkozó adatot (Note On/Off, Control Change stb.) - ha a
  fájl egyáltalán nem használ egy csatornát, arra nem kerül beállítás
  (a "használt" itt bármilyen csatorna-üzenetet jelent, a Control
  Change-eket is beleértve, nem csak a Note On/Off-ot - ez szándékos
  döntés). Ha egy fájlnál egyik aktív csatornához sincs adat, a program
  nem szúr be felesleges üres tracket, a kimenet a bemenet változatlan
  másolata, ezt az összegzésben külön jelzi. **JELENLEG CSAK format 1
  fájlokat támogat** - format 0 esetén az adott fájl hibaként jelenik
  meg az összegzésben, a többi fájl feldolgozása folytatódik (a format 0
  támogatása egy későbbi fejlesztés, lásd az állapot-dokumentum
  "Következő lépések" 12. pontját).

### Javítva
- **MIDI kimenet váltásakor beragadhattak a hangok.** A `MidiEngine.KimenetBezaras()`
  eddig csak `Dispose()`-olta a `MidiOut` eszközt, Note Off küldése nélkül -
  ha éppen szólt egy hang (pl. lenyomva tartott billentyű) a kimeneti eszköz
  váltásakor (`KimenetMegnyitas()` is meghívja `KimenetBezaras()`-t) vagy a
  program bezárásakor, az a korábbi eszközön tovább szólhatott. Új
  `OsszesHangElnemitasa()` metódus a `Dispose()` ELŐTT minden csatornára
  kiküldi az All Sound Off (CC120) és All Notes Off (CC123) üzenetet.

### Módosítva
- **A fájl-dialógusok fejléce (`Title`) mostantól a hívó menüpont
  szövegével egyezik meg** (pl. "Régi .mb3 fájlok konvertálása"), az
  általános "Megnyitás"/"Mentés" felirat helyett - így egyértelmű, melyik
  menüpontot indította el a felhasználó. Mind a 8 fájl-dialógus érintett.
- **"Adatok betöltése"/"Adatok mentése" → "Presetek betöltése"/"Presetek
  mentése"** - a menüpontok, a hozzájuk tartozó dialógus-fejlécek, a
  fájlszűrők ("MidiBill preset fájlok") és a betöltési hibaüzenet szövege
  is a "Presetek" elnevezésre változott, összhangban azzal, hogy a
  korábbi mappaszerkezet-átalakítás (lásd 1.10.0) óta ezek a fájlok a
  felhasználói mappa `Presetek` almappájában laknak, nem egy "Adatok"
  mappában. Az "SF adatok" mappanév (hangszerlisták/SoundFont-importok)
  szándékosan változatlan maradt, az egy másik fogalom.

## [1.10.0] - 2026-09-03

### Hozzáadva
- **Extra MIDI üzenetek a játék kezdetén**: új GroupBox a jobb oszlop
  korábban szándékosan szabadon hagyott helyén (a Transzponálás doboz
  és a Csend legyen!/Kilépés gombok között), benne a "MIDI beállító
  üzenetek küldése" checkbox-szal és egy állapot-kijelző felirattal.
  Bepipálás után az első ténylegesen lejátszott hangnál (Note On) a
  program EGYETLEN egyszer kiküldi az AKTÍV csatornák teljes állapotát
  (Program Change, Volume, Pan, Vibrato, Chorus, Reverb) - új
  `AktivCsatornakAllapotanakKuldese()` metódus a `MainForm`-ban -, majd
  a checkbox automatikusan visszakapcsol. A folyamatot a felirat jelzi:
  "Várakozás billentyűre..." → "MIDI üzenetek kiküldve." (manuális
  kikapcsolásnál a felirat üresre vált). Célja, hogy egy külső
  MIDI-felvevő program is megkapja a teljes csatorna-beállítást (pl. a
  hangszíneket), nem csak a ténylegesen lejátszott hangokat.
- Az "Alaphelyzetbe állítás" is visszaállítja ezt a checkbox-ot
  kikapcsolt állapotba.

### Javítva

Egy teljes kódátnézés során feltárt három valódi hiba javítása (a
részletes megállapítás-listát lásd a `MidiBill3_VBNET_aktualis_allapot.md`
"Kódátnézés" fejezetében):

- **Friss indításkor nem szólalt meg semmi, ha nem létezett még
  `Kezdet.json`** (pl. friss telepítés vagy build után). A
  `csatornaAllapotok` tömböt és a `HangszerPanel` property-ket egyik
  inicializáló lépés sem szinkronizálta a `MidiEngine`-be (a panel
  property-k szándékosan nem tüzelnek eseményt betöltéskor), az egyetlen
  szinkronizáló hívás - `OsszesCsatornaUjrakuldese()` - pedig ilyenkor
  soha nem futott le. Emiatt a `MidiEngine` MIND a 16 csatornát
  inaktívnak látta, és némán eldobta a lejátszott hangokat. Mostantól a
  `KezdetBeallitasokBetoltese()` a "nincs fájl" ágban ÉS a hibaágban is
  kiküldi az induló állapotot.
- **Minden üzenet-vizsgálat csak az 1. MIDI csatornán működött.** A
  státuszbájt alsó négy bitje a küldő csatorna sorszáma, de a
  `MainForm.MidiUzenetFeldolgozasa` és a `MidiEngine.UzenetKuldes` is
  pontos egyenlőséggel hasonlított (`&H90`, `&HB0`). Egy 2. csatornára
  állított billentyűzetnél (`&H91`, `&HB1`) emiatt kimaradt a
  pedál-inverzió, a split pont tanulás és az "Extra MIDI üzenetek",
  a `MidiEngine`-ben pedig a split zóna, a globális transzponálás és a
  csatornánkénti oktáv-eltolás is - ráadásul a hangok nem kerültek be a
  `noteOn` nyilvántartásba, ami beragadó hangokhoz vezethetett. Mindkét
  helyen bevezetésre került az `uzenetTipus = statuszBajt And &HF0`
  maszkolás, és a `HangUzenetE()` is maszkol már magában.
- **Kilépéskori befagyás (deadlock) kockázata.** A MIDI callback-szál
  szinkron `Invoke`-kal adta át az üzeneteket a UI szálnak; kilépéskor a
  UI szál a `FormClosing`-ban a MIDI bemenet lezárására várt, miközben a
  callback-szál a UI szálra várt. A megoldás: `BeginInvoke` (aszinkron,
  de sorrendtartó - egyúttal a leütésenkénti késleltetést is megszünteti),
  új `zarasFolyamatban` jelző a `FormClosing` legelején, és a lezárási
  sorrend átrendezése (jelző → MIDI bemenet lezárása → hangok leállítása
  → mentés → `Dispose`).
- A kijelzés-frissítő időzítő (`midiKijelzoIdozito`) is leáll a
  `FormClosing`-ban - korábban nem volt leállítva.

### Módosítva
- **Új mappaszerkezet: gyári és felhasználói tartalom szétválasztása.**
  Korábban MINDEN adat (`Kezdet.json`, `HibaNaplo.txt`, a preset fájlok és a
  hangszerlisták) a program mappájába, azon belül egy közös `Adatok`
  mappába került - ez a tervezett telepítő után (`C:\Program Files\...`)
  csendben elhasalt volna. Új `Modellek\Utvonalak.vb` modul tartja egy
  helyen az összes útvonalat:
  - **GYÁRI tartalom** (a program mappájában, csak olvasható, ezt szállítja
    a telepítő): `GM.lst` és `GM.drm`, közvetlenül a program mappájában -
    **az `Adatok` mappa megszűnt**.
  - **FELHASZNÁLÓI tartalom** (`%AppData%\MidiBill3`, minden írás ide megy):
    `Kezdet.json`, `HibaNaplo.txt`, `Presetek\` (a `.json` presetek) és
    `SF adatok\` (az importált `.lst`/`.drm` listák).
  - A `GM.lst`/`GM.drm` **védett**: mindig a gyári mappából olvasódnak, és a
    program meg is akadályozza, hogy ilyen néven jöjjön létre felhasználói
    fájl (hangszerlista-betöltésnél és SoundFont-importnál is, pl. egy
    "GM.sf2" nevű SoundFont esetén) - így nem keletkezhet olyan árva fájl,
    amit a program sosem használna fel.
  - **Egyszeri, automatikus átköltöztetés** induláskor: a program
    mappájában maradt `Kezdet.json`, illetve a régi `Adatok` mappa
    tartalma szétosztva (`.json` → `Presetek`, `.lst`/`.drm` → `SF adatok`).
    A régi mappát nem törli, marad biztonsági másolatnak.
  - A fájlválasztó dialógusok alapmappája a tartalom szerint igazodik
    (presetek → `Presetek`, hangszerlisták/SoundFont → `SF adatok`).
  - A konvertáló segédprogramok írásvédett forrásmappa esetén sem
    szakadnak meg kezeletlen hibával, hanem a felhasználói mappába írnak
    (új `KonvertalasKimenetiMappa()`).
  - A `.vbproj` már csak a `GM.lst`-et és a `GM.drm`-et másolja a kimeneti
    mappába (korábban az `Adatok\**\*.*` mintával mindent, így egy telepítő
    a felhasználó személyes presetjeit is szállította volna).

  Mellékhaszon fejlesztés közben: a `Kezdet.json` és a presetek kikerültek
  a `bin\Debug` mappából, tehát túlélik a Clean/Rebuild műveleteket.
- **`Option Strict On` bekapcsolva** (a `MidiBill3.vbproj`-ban, az
  `Option Infer` és `Option Explicit` explicit `On` értékével együtt).
  A bekapcsolás előtti teljes statikus átvizsgálás egyetlen
  szabálysértést sem talált - a projekt már eddig is "Option Strict On
  stílusban" íródott -, tehát a meglévő kód egyetlen sorát sem kellett
  módosítani. A cél az, hogy a fordító INNENTŐL is tartsa ezt a szintet:
  a csendes, szűkítő konverziók és a késői kötés mostantól fordítási
  hibát adnak, nem futásidejű meglepetést. Ez különösen a következő
  fejlesztésnél (MIDI felvétel/visszajátszás) számít, ahol a NAudio
  `MidiEvent`-hierarchiájában sok lesz a típuskonverzió. Az `Option
  Infer` szándékosan van kiírva, mert a kód több helyen a
  lambda-paraméterek típuskövetkeztetésére támaszkodik
  (`OrderBy(Function(k) k)`).

---

## [1.9.0] - 2026-08-29

### Hozzáadva
- **Csatornánkénti Chorus** (MIDI CC93): a Vibratóhoz hasonlóan minden
  csatornán önállóan állítható, új `Chorus` property a
  `CsatornaBeallitas`-on (alapértelmezett 0, visszamenőleg kompatibilis
  a régi presetekkel), új csúszka és értékkijelző a `HangszerPanel`-en,
  teljes MIDI-küldés/mentés/betöltés/alaphelyzetbe állítás integráció
  (a `CHORUS = 93` konstanssal a `MainForm`-ban)
- A `HangszerPanel` és a hangszer-keretek magassága a Chorus-csúszka
  helyének megfelelően nőtt, a lentebbi vezérlők (csatornaválasztó,
  MIDI-üzenet kijelző, pedál-jelölő, Csend legyen!/Kilépés gombok)
  ennek megfelelően lejjebb kerültek

### Javítva
- Az "Alap hangerő" felirat mostantól a Reverbhez hasonlóan kiírja a
  százalékjelet is (pl. `"Alap hangerő: 70%"`)
- A "Reverb" felirat magyar "Visszhang"-ra cserélve mindhárom
  előfordulási helyén (csúszka-változás, beállítások alkalmazása,
  kezdeti Designer-szöveg)

---

## [1.8.0] - 2026-08-22

### Hozzáadva
- **10. csatorna (dob) feloldása**: korábban véglegesen letiltott
  csatorna, mostantól teljesen szerkeszthető (aktiválható, hangszer-
  választható, Split zóna, Oktáv, mentés/betöltés mind normálisan
  működik)
- **Dedikált dobkészlet-hangszerlista** (`.drm` fájlok) a 10.
  csatornához, a `HangszerLista.DobNevek` önálló Dictionary-vel - a
  `.drm` fájlnév automatikusan párosodik az aktív `.lst` alapnevével
  (pl. `GM.lst` + `GM.drm`, `Teszt.lst` + `Teszt.drm`)
- SoundFont-importálás bővítve: a dobkészlet-presetek (korábban
  "érvénytelenként" eldobva) mostantól külön gyűjtve, a `.drm` fájlba
  kerülnek - egyszerre generálódik `.lst` (hangszerek) és `.drm`
  (dobkészletek), azonos alapnévvel
- `SoundFontOlvaso.ismertDobBankok()` bővíthető tömb a dobkészlet-bank
  felismeréshez - fokozatosan bővült `{128}` → `{120, 128}` →
  `{120, 127, 128}`, ahogy különböző SoundFont-ok eltérő konvencióival
  találkoztunk (JJazzLab, GM2 szabvány, SGM-v2_02)
- **"Alaphelyzetbe állítás" menüpont** (új "Beállítások" menü a Fájl
  után): minden beállítást visszaállít, mintha a program a `Kezdet.json`
  beolvasása nélkül indulna (hangszerlista → `GM.lst`/`GM.drm`,
  csatornák/Split pont/Transzponálás/Reverb/Pedál → alapérték) - a
  MIDI In/Out kiválasztás szándékosan érintetlen marad
- Angol nyelvű README (`README.en.md`), kattintható nyelv-váltó linkkel

### Javítva
- Csatorna-felirat duplikáció: a `"N. csatorna (dob)"` felirat két
  helyen volt megírva, az egyikből hiányzott a `"(dob)"` jelzés
  checkbox-kattintáskor - egységesítve egy közös `CsatornaFelirat()`
  segédfüggvénnyel

---

## [1.7.0] - Bank Select-alapú kiterjesztett hangszertámogatás

### Hozzáadva
- `MidiEngine.ProgramValtas()`: mindig Bank Select (CC0 MSB=0 + CC32
  LSB=bank) + Program Change hármast küld, nem csak sima Program
  Change-t - Roland GS / Yamaha XG-kompatibilis szintetizátorokkal is
  működik (device-agnosztikus MIDI-mechanizmus)
- `.lst` fájlformátum bővítve `bank.program=Név` szintaktikával (pl.
  `"8.0=Variation Piano"`), visszamenőleg kompatibilis a sima
  `program=Név` formával
- `HangszerPanel`: a legördülőben a Bank≠0 hangszerek `Név (B<bank>)`
  formában jelennek meg
- `Beallitasok.Hangszer` tartományellenőrzés bővítve `0-16383`-ra
  (a legnagyobb lehetséges kombinált `bank*128+program` kulcs)

---

## [1.6.0] - Segédprogramok beépítése

### Hozzáadva
- Új "Segédprogramok" menü a főprogramban: "Preset fájlok frissítése..."
  és "Régi .mb3 fájlok konvertálása..." menüpontok

### Eltávolítva
- A korábbi, önálló `PresetKonvertalo` és `Mb3Konvertalo` konzolos
  projektek megszűntek - funkciójukat a beépített menüpontok váltják ki
- `Directory.Build.props` megszűnt (feleslegessé vált, mivel most már
  csak egy projekt van) - a verziószám visszakerült közvetlenül a
  `MidiBill3.vbproj`-ba

---

## [1.5.0] - Betölthető hangszerlisták és SoundFont-importálás

### Hozzáadva
- A beépített General MIDI hangszerlista kiszervezve külső `.lst`
  fájlba (`Adatok/GM.lst`), `index=Név` szöveges formátumban
- `HangszerLista.vb`: `Dictionary(Of Integer, String)`-alapú, ritkás
  (nem folytonos indexelésű) listákat is támogató architektúra
- **"Hangszerlista betöltése..." menüpont**: tetszőleges helyről
  választható `.lst` fájl, automatikus másolás az `Adatok` mappába
  felülírás-megerősítéssel
- `HangszerekErvenyesitese()`: robusztus validálás minden csatornára
  (csak a patch-szám számít, a hangszernév nem) - lista-váltás vagy
  preset-betöltés után is helyesen működik
- `HangszerPanel.Hangszer` property javítva: a VALÓDI patch-számot
  kezeli, nem a legördülő pozícióját - kritikus javítás ritkás
  listáknál
- `Modellek/SoundFontOlvaso.vb`: SoundFont (`.sf2`) fájlok `phdr`
  blokkjának beolvasása, csak a metaadatokat olvassa (a hangminta-
  adatot átugorja) - gyors feldolgozás nagy fájloknál is
- **"SoundFont importálása..." menüpont**: egy `.sf2` fájlból
  automatikusan legenerálja és alkalmazza a hozzá tartozó `.lst` fájlt

---

## [1.4.0] - Csatornánkénti transzponálás, Reverb, pedál-javítás

### Hozzáadva
- **Csatornánkénti oktáv-transzponálás** (±2 oktáv) a `HangszerPanel`-en,
  AZONNALI (retroaktív) hatással - összeadódik a globális, félhangos
  transzponálással
- **Globális Reverb** (MIDI CC91), 0-100%-os felhasználói skálán,
  0-127-re átszámítva küldéskor
- Olvasható MIDI üzenet-kijelzés (pl. `"NoteOn C4 v90"`, `"PC Acoustic
  Grand Piano"`) nyers számok helyett
- MIDI üzenet-kijelzés frissítési ütemének korlátozása (max. 10x/mp) -
  nem érinti a tényleges MIDI-küldés időzítését

### Javítva
- **Pedál-inverzió hibája**: a "Fordított pedál működés" checkbox
  korábban aktívan küldött egy hamis MIDI-üzenetet minden állapot-
  váltáskor (beleértve a preset betöltésekori programozott beállítást
  is), ami elrontotta a szintetizátor pedál-szinkronját. Javítva: a
  checkbox mostantól teljesen passzív "mód"-kapcsoló, sosem
  kezdeményez MIDI-küldést önmagától

---

## [1.3.0] - 16 MIDI csatorna támogatása

### Hozzáadva
- A csatornák száma 4-ről 16-ra bővült
- Csatorna-választó (4 RadioButton: 1-4 / 5-8 / 9-12 / 13-16) - a
  felületen egyszerre egy 4-es csoport szerkeszthető, de mind a 16
  csatorna egyszerre szólhat
- A 4 fizikai `HangszerPanel` "kirakattá" vált: mindig az épp
  kiválasztott csatornacsoportot mutatja
- A 10. csatorna (dob) ekkor még véglegesen letiltva marad (később,
  az 1.7.0 utáni körben oldottuk fel)
- Teljes form-elrendezés újratervezve, szimmetrikus margókkal

---

## [~1.2.0] - Korai finomítások *(közelítő verziószám)*

### Hozzáadva
- Alkalmazásikon (`MidiBill3.ico`) - zongorabillentyű + 4 csatornás
  szétosztás motívum
- DPI-alapú ablakskálázás (`AutoScaleMode.Dpi`,
  `ApplicationHighDpiMode = PerMonitorV2`)
- Fix ablakméret (`FormBorderStyle = FixedSingle`, `MaximizeBox = False`)
- "MIDI eszközök frissítése" gomb + `EszkozListaEgyezikE()` validálás
  minden MIDI In/Out kiválasztás előtt - véd az eszköz-index-eltolódás
  ellen (ha egy eszközt ki-/behúznak működés közben)
- Induláskori MIDI bemenet/kimenet hiányának kezelése (bemenet
  hiányánál figyelmeztetés+folytatás, kimenet hiányánál figyelmeztetés+
  kilépés)
- Robusztus JSON hibakezelés: `null` csatorna-elem védelem, figyelmeztető
  üzenetek hiányzó MIDI eszköznévről vagy tartományon kívüli, korrigált
  értékekről
- Program verziószám mentése a JSON presetekbe (tájékoztató jelleggel)
- "Kilépés mentés nélkül" menüpont

---

## [1.1.0] - Verziószámozás bevezetése

### Hozzáadva
- SemVer-alapú verziószámozás bevezetve (`<Version>` a `.vbproj`-ban)
- Névjegy menüpont (Súgó menü)

---

## [1.0.0] - Kezdeti VB.NET port *(nem hivatalosan verziózott alapállapot)*

### Hozzáadva
- VB6 → VB.NET tudatos, kézi újraírás megkezdése
- 4 független MIDI kimeneti csatorna, saját hangszerrel, hangerővel,
  pannal, vibratóval
- Note-tracking rendszer a célzott hangkikapcsoláshoz
- "Csend legyen!" pánikgomb
- **Split Point**: a billentyűzet két zónára osztható (Alul/Felül),
  csatornánként állítható, kézzel vagy "Tanulás" gombbal
- JSON-alapú beállítás-mentés/betöltés (a régi, pozíció-alapú `.mb3`
  formátum helyett), emberi olvasást segítő megjegyzésekkel
- Régi, VB6-os `.mb3` fájlok importálása
- `Kezdet.json` automatikus be-/kimentés induláskor/kilépéskor
- Globális hibanaplózás (`ApplicationEvents.vb` → `HibaNaplo.txt`)
- `PresetKonvertalo` és `Mb3Konvertalo` önálló konzolos segédprogramok
  (később, 1.6.0-nál beépültek a főprogramba)

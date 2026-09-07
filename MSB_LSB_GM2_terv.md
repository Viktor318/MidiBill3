# Külön MSB/LSB bank-kezelés + GM2 hangszerlista - megvalósítási terv

Ez az állapot-dokumentum "Következő lépések" **13. pontjának** részletes
lebontása. A pont 2026-09-05-én került fel, a terv 2026-09-06-án készült.

> **ELKÉSZÜLT: 2026-09-07, az 1.17.0 verzióban.** Mind a kilenc fázis
> megvalósult és a felhasználó élesben kipróbálta. A dokumentum
> megmarad, mert a MIÉRTEKET rögzíti - a részletes változáslistát lásd a
> `CHANGELOG.md` 1.17.0 szakaszában. A megvalósítás közben tett
> eltéréseket és a menet közben talált hibákat a végén, a
> "Megvalósítás - mi lett belőle" szakasz sorolja fel.

---

## 1. Mit akarunk elérni

A program ma **egyetlen számként** tárolja a bankot, és mindig
`MSB = 0 / LSB = bank` alakban küldi ki (`MidiEngine.ProgramValtas`).
Ez megfelel az XG dallamhangszereinek és a SoundFontból importált
bankoknak, de **nem tudja megszólaltatni**:

- a **Roland GS** variációkat (ott a variáció az **MSB**-ben van,
  jellemzően 1, 2, 3, 8, 9, 16, 24; az LSB a hangkészlet-térképet jelöli),
- a **GM2** hangokat (`MSB = 121` dallamhangszereknél, `MSB = 120`
  doboknál, a variáció az LSB-ben).

A cél: a bank két bájtja **külön** legyen tárolva és küldve, és ezután
legyen értelme egy **GM2 hangszerlistának** is.

---

## 2. ELŐFELTÉTEL: a lejátszó viselkedésének mérése

Mielőtt bármit kódolunk, le kell futtatni a `Tesztek/bank_select_teszt.mid`
fájlt a felhasználó tényleges lejátszójában (BASSMIDI-alapú
VirtualMIDISynth). A 13 másodperces fájl ugyanazt a programszámot négy
szakaszban szólaltatja meg: bank nélkül, `CC0=8`-cal, `CC32=8`-cal, majd
újra bank nélkül.

**Miért blokkoló ez?** Mert eldönti a 8. fázis (SoundFont-import)
tervét: a `.sf2` fájl `wBank` mezője egyetlen 0-127 érték, és a kérdés
az, hogy azt az **MSB**-be vagy az **LSB**-be kell-e tenni ahhoz, hogy a
lejátszó megtalálja. A FluidSynth alapértelmezett `gs` módja az **MSB**-t
nézi; a BASSMIDI viselkedése MÉG NINCS ellenőrizve elsődleges forrásból.

| A teszt eredménye | Amit a SoundFont-import tegyen |
|---|---|
| csak a 2. szakasz szól máshogy | a `wBank` az **MSB**-be |
| csak a 3. szakasz szól máshogy | a `wBank` az **LSB**-be (mint ma) |
| mindkettő eltér | a lejátszó `MSB*128+LSB`-t számol - külön mérlegelés |
| a 3. szakasz néma/rossz | a CC32 nem bank a lejátszónál - **MSB**-be |

FONTOS: a teszt csak akkor mond bármit, ha a 8-as bank / 24-es program
páros LÉTEZIK a betöltött SoundFontban. Ha nem, a `bank_select_teszt.py`
tetején át kell írni a `BANK` és `PROGRAM` konstanst egy olyan párosra,
ami a SoundFontból importált `.lst`-ben szerepel.

---

## 3. A kulcsábrázolás - a terv sarokköve

**Döntés (javaslat):** a mostani `bank * 128 + program` kombinált kulcs
helyett **`MSB * 16384 + LSB * 128 + program`** (0 .. 2 097 151).

Ez azért különösen szerencsés, mert **a régi adatok VÁLTOZATLANUL
helyesek maradnak**: a mai kulcs `bank * 128 + program`, ahol a "bank"
valójában az LSB volt. Az új képletbe `MSB = 0`-val behelyettesítve
`0 * 16384 + bank * 128 + program` = pontosan ugyanaz a szám.

Következmények:

- a meglévő **preset `.json` fájlok migrálás nélkül** jók (a `Hangszer`
  mező marad egyetlen egész szám),
- a meglévő **`.lst` fájlok** is jók (`bank.program` = `LSB.program`),
- a `HangszerLista` továbbra is `Dictionary(Of Integer, String)` maradhat,
- nem kell verziószám a preset-formátumba.

**Amit meg kell írni**: három apró segédfüggvényt EGY helyen (javaslat:
`HangszerLista` vagy egy új `Hangszerkulcs` modul), és MINDEN más helyen
csak ezeket hívni - sose a `\ 128` / `Mod 128` képletet:

- `Kulcs(msb, lsb, program) As Integer`
- `Msb(kulcs)`, `Lsb(kulcs)`, `Program(kulcs)`
- `KulcsSzoveg(kulcs)` - egységes megjelenítés (lásd a nyitott kérdéseket)

---

## 4. Fázisok

### 1. fázis - A kulcs-segédfüggvények bevezetése (viselkedés VÁLTOZATLAN)

Tisztán refaktor, semmi új funkció. A cél, hogy a `\ 128` / `Mod 128`
képlet EGY helyen éljen, és a következő fázisok már ne szórt kódot
módosítsanak.

Érintett helyek (a mai állapot szerint):

| Fájl | Mit csinál ma |
|---|---|
| `Modellek/MidiEngine.vb` | `ProgramValtas`: `kombinaltKulcs \ 128`, `Mod 128` |
| `Modellek/CsatornaUzenetek.vb` | `CsatornaAllapotUzenetei`: ugyanaz |
| `Modellek/HangszerLista.vb` | `IndexReszFeldolgozasa`, `Ment` |
| `Modellek/MidiFajlOlvaso.vb` | `bank * 128 + adat1` a Program Change-nél |
| `Modellek/MidiJelentes.vb` | `HangszerSzoveg` |
| `Modellek/MidiFelvevo.vb` | `CsatornaHangszerNeve` |
| `Modellek/MidiPresetKeszito.vb` | a bank kiolvasása |
| `HangszerPanel.vb` | `"(B" & bank & ")"` megjelenítés |
| `MainForm.vb` | `HangszerekErvenyesitese` |

**Ellenőrzés**: a két Python teszt VÁLTOZATLANUL fusson le (a fázis nem
változtat viselkedést), és a program indulás után ugyanúgy szóljon.

### 2. fázis - A `.lst` formátum bővítése

- `IndexReszFeldolgozasa`: fogadja a **három tagú** `msb.lsb.program`
  alakot is. A meglévő két tagú `bank.program` **továbbra is
  `LSB.program`-ként** értendő (visszamenőleges kompatibilitás), az egy
  tagú `program` pedig `0.0.program`.
- `Ment()`: a legrövidebb alakot írja, ami vissza is olvasható
  (`program`, ha msb=lsb=0; `lsb.program`, ha csak az msb=0; egyébként
  `msb.lsb.program`).
- **Ellenőrzés**: új, kicsi Python vagy VB teszt, ami a három alakot
  oda-vissza konvertálja, és egy régi `GM.lst` beolvasása után is
  ugyanazokat a kulcsokat adja.

### 3. fázis - A kiküldés (`MidiEngine.ProgramValtas`)

`CC0 = MSB`, `CC32 = LSB`, majd Program Change. Ugyanez a
`CsatornaUzenetek.CsatornaAllapotUzenetei()`-ben, hogy az élő küldés, a
fájl-patchelés és a felvétel is egyszerre kapja meg.

**FIGYELEM**: ez az a pont, ahol a viselkedés ténylegesen megváltozik.
A mai presetek `MSB = 0`-t adnak, tehát pontosan úgy fognak szólni, mint
eddig - de érdemes a felhasználóval kipróbáltatni, mielőtt tovább megyünk.

### 4. fázis - A beolvasó (`MidiFajlOlvaso`)

Ma a szabály: "amelyik bank-bájt nem nulla, az a bank". Ez GS fájloknál
félremehet (`MSB=8, LSB=1` esetén az LSB nyerne, pedig a 8 a helyes).
Mostantól **mindkét bájt megőrződik**, és a kulcs a 3. pont képletével áll
össze.

Érinti a `Hangszervaltasok` lista JELENTÉSÉT, tehát vele együtt a
`MidiJelentes`-t, a `MidiPresetKeszito`-t és a `MidiFelvevo`
névfeloldását is.

**Ellenőrzés**: a `Tesztek/midi_olvaso_ellenorzes.py` portját frissíteni
kell, és a meglévő "mindkét bank-bájt nem nulla" tesztesetnek MÁS lesz a
várt eredménye (ma az LSB nyer, ezután mindkettő megmarad) - ez jó jel,
pont ezért van benne az az eset.

### 5. fázis - A jelentés és a preset-készítő

- `MidiJelentes.HangszerSzoveg`: a `(bank N, program M)` helyett
  `(MSB a, LSB b, program M)` - és a "nincs a listában" visszaesés
  szabályát is újra kell gondolni: előbb `MSB.LSB.program`, aztán
  `0.LSB.program`, aztán `0.0.program`? (nyitott kérdés)
- `MidiPresetKeszito`: mindkét bájtot eltárolja.
- `MidiFelvevo.CsatornaHangszerNeve`: ugyanaz a visszaesési lánc.

### 6. fázis - A felület (`HangszerPanel`)

A `Név (B<bank>)` megjelenítés helyett olyan alak kell, ami mindkét
bájtot mutatja. (nyitott kérdés - lásd lentebb)

Emellett érdemes átgondolni, hogy a legördülő listában **256+ elem** már
kényelmetlen lehet, ha bejön a GM2 lista.

### 7. fázis - GM2 hangszerlista

Új gyári `GM2.lst` és `GM2.drm`, a **General MIDI Level 2** szabvány
(MMA, 1999) szerint: 128 alap program × variációk, `MSB = 121`
dallamhangszereknél, `MSB = 120` doboknál, összesen 256 hang.

**Külön munka**: a hanglistát elsődleges forrásból (MMA GM2 leírás)
kell összeszedni és ellenőrizni - ez nem "kódolás", hanem adatgyűjtés,
és önmagában egy ülést megérhet. Nem szabad emlékezetből legenerálni.

Kérdés, hogy a `GM2.lst` a telepítővel szállított gyári fájl legyen-e
(mint a `GM.lst`/`GM.drm`), vagy az "SF adatok" mappába kerüljön.

### 8. fázis - SoundFont-import

A `SoundFontOlvaso` a `.sf2` `wBank` mezőjét ma LSB-ként kezeli. A 2.
pont mérése alapján kell eldönteni, hogy marad-e, vagy MSB-be kerül.
Az `ismertDobBankok = {120, 127, 128}` logikát is felül kell vizsgálni:
a 120 mostantól a GM2 dob-MSB, ami már ténylegesen kiküldhető.

### 9. fázis - Dokumentáció, tesztek, verzió

- Mindkét Python teszt frissítése + mutációs teszt bővítése (legalább:
  MSB és LSB felcserélése, a 3 tagú `.lst` alak félreolvasása, a régi
  2 tagú alak MSB-ként értelmezése).
- `CHANGELOG.md`, állapot-dokumentum, a 13. pont lezárása.
- Verziólépés (**minor**, mert a `.lst` formátum bővül és a kiküldés
  viselkedése változik - de nem `major`, mert a régi adatok maradnak).

---

## 5. Nyitott kérdések (a munka ELEJÉN eldöntendő)

1. **A `bank_select_teszt.mid` eredménye** - ez határozza meg a 8. fázist.
2. **Megjelenítés a legördülőben és a jelentésben**: `Név (B121.1)`,
   `Név (MSB 121 / LSB 1)`, vagy valami rövidebb? Elég sok helyen
   megjelenik, ezért egy `KulcsSzoveg()` függvényben kell tartani.
3. **A "nincs a listában" visszaesési lánc** sorrendje a jelentésben
   (lásd 5. fázis).
4. **A `GM2.lst` gyári fájl legyen-e**, vagy az "SF adatok" mappába
   kerüljön?
5. **Kell-e a felületen szabvány-választó** (GM / GM2 / GS / XG), ami
   előre kitölti az MSB-t a bankválasztásnál - vagy a felhasználó
   közvetlenül a `.lst` fájlból választ, és a szabvány kérdése ott dől el?

---

## 6. Kockázatok

- **A 3. fázis élő viselkedést változtat.** Bár a mai presetek `MSB = 0`-t
  adnak (tehát elvileg minden ugyanúgy szól), ezt a felhasználónak
  valódi hangszeren ki kell próbálnia, mielőtt továbbmegyünk.
- **A 4. fázis megváltoztatja a `Hangszervaltasok` jelentését**, amit négy
  másik modul is használ. Ezt egyben kell átvezetni, különben a jelentés
  és a preset-készítő csendben rossz bankot adna.
- **A GM2 lista adatgyűjtés, nem kódolás.** Emlékezetből legenerálni
  hibás listát adna; elsődleges forrásból kell dolgozni.

---

## 8. Megvalósítás - mi lett belőle (2026-09-07)

### A blokkoló mérés eredménye

A `bank_select_teszt.mid` egyértelmű választ adott: a felhasználó
BASSMIDI-alapú VirtualMIDISynth-e a **CC0-t (MSB)** tekinti a SoundFont
bankjának, a CC32-t pedig egyszerűen figyelmen kívül hagyja. Ez cáfolta
a korábbi feltevést, hogy a CC32-t esetleg SoundFont-választásra
használná (akkor a harmadik szakasz elnémult volna).

**Következmény**: a 8. fázisban a `.sf2` `wBank` mezője az MSB-be került.
Egyben kiderült, hogy a 13. pont nem kényelmi bővítés volt, hanem
HIBAJAVÍTÁS: a programban addig egyetlen nem nulla bankú hangszer sem
szólalt meg.

### Eltérések a tervtől

- **A preset-migráció teljesen elmaradt.** A terv a régi bankszámok
  átértelmezésével számolt, és egy `BankFormatum` jelölővel tette volna
  idempotenssé a konverziót. Kiderült, hogy a felhasználó MINDEN
  presetje a 0-ás bankot használja, ahol a régi és az új kulcs
  számtanilag azonos - így nincs mit migrálni. A kockázatos lépés
  egyszerűen kiesett.
- **A `.lst` kéttagú alakja LSB maradt**, nem lett átértelmezve MSB-re.
  Így a régi fájlok jelentése nem változik meg csendben; a helyes
  viselkedést a SoundFontok egyszeri újraimportálása adja.
- **A 6. fázis (felület) nagyrészt az 1.-ben megtörtént**, mert a
  bank-jelölés a közös segédfüggvénybe került. A legördülő - a
  felhasználó döntése alapján - egyelőre sima lista maradt.
- **Nem készült szabvány-választó a felületen** (a terv 5. nyitott
  kérdése): a `.lst` fájl tartalmazza a teljes MSB/LSB párost, tehát a
  szabvány kérdése ott dől el.
- **Bónusz, ami nem volt a tervben**: "Több SoundFont importálása"
  menüpont, hogy a listák újragenerálása egy lépés legyen.

### Menet közben talált hibák

1. **`Korlatoz(cs.Hangszer, 0, 16383)`** a preset-betöltésben - a régi
   képlet maximuma. Ha nem derül ki, az MSB-t tartalmazó kulcsokat
   CSENDBEN levágta volna, és a hiba csak a preset-mentésnél jelentkezik.
2. **A `GM2.lst` be sem töltődött volna**: az `Utvonalak` gyári
   fájlneveinek listája nem tartalmazta, ezért a program az "SF adatok"
   mappában kereste volna.
3. **A `.vbproj` publish-targetje** sem tudott a GM2 fájlokról - ugyanaz
   a hiba, amit az 1.13.0-ban a `GM.lst`-tel javítottunk.
4. **A `.lst` megjegyzés-sorai**: eddig csak a véletlen mentette meg
   őket (nem volt bennük `=`). A saját GM2-fejlécem elbukott ezen.
5. **Felcserélt GS/XG megnevezés** a beolvasó tesztjének kommentjeiben -
   a viselkedés helyes volt, a címke rossz.

### Ami továbbra is nyitott

- A **GM2 dobkészletek 49-es (Orchestra) és 57-es (SFX) programszáma**
  nem volt megerősíthető a fellelhető forrásokból. Ezek a GS/GM2 bevett
  készletei, és a program beépített listájában eddig is szerepeltek, de
  ha egyszer előkerül egy hivatalos táblázat, érdemes összevetni.
- A **MIDI üzenet-kijelzés** (`PC <név>`) a bank nélküli értelmezést
  mutatja, mert egy Program Change üzenet önmagában nem hordozza a
  bankot. Csatornánkénti Bank Select-nyilvántartással pontosítható
  lenne, de az egy nyers üzenet-kijelzőbe túl sok.

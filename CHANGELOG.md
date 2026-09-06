# Changelog

Ez a fájl a MidiBill3 (VB.NET) projekt eddigi fejlesztési történetét
foglalja össze, verziónkénti bontásban. A formátum lazán a
[Keep a Changelog](https://keepachangelog.com/) elveit követi.

> **Megjegyzés**: a korai (1.1.0 és 1.3.0 közötti) verzió-határok
> közelítő rekonstrukción alapulnak - érdemes összevetni a tényleges
> `git log` előzményekkel, és ha szükséges, pontosítani.

---

## [1.16.0] - 2026-09-06

### Változtatva - a felvett .mid szerkezete (csatornánként külön track)

A felvevő eddig KÉT tracket írt: a 0. a tempó/ütemjelzés, az 1. pedig az
ÖSSZES csatorna hangjait együtt, egy kupac csatorna-beállítással a
legelején. **Mostantól minden szereplő csatorna kap egy saját tracket**
(a felhasználó kérése, hogy a fájlba tracknév is kerüljön).

- **A 0. (conductor) track** a darab címét is megkapta:
  `MidiBill3 felvétel`.
- **Minden szereplő csatorna saját trackje** tartalmazza: a
  csatorna-előtagot (`FF 20`), a tracknevet (`FF 03`), a hangszernevet
  (`FF 04`, ha kideríthető), a csatorna induló beállításait tick 0-n, és
  végül a csatorna felvett üzeneteit. Amelyik csatornán nincs anyag, arra
  nem készül üres sáv.
- **A tracknév alakja `"NN - Hangszernév"`** (pl. `03 - Tenor Sax`), ahol
  NN az 1-től induló CSATORNASZÁM két számjegyen - szándékosan nem a
  track sorszáma (a felhasználó kifejezett kérése): így egy DAW
  sávlistájában rögtön látszik, melyik sáv melyik csatorna.
- **A hangszernév a BETÖLTÖTT listából jön**, a csatorna első Program
  Change üzenete alapján - először a felvétel indulásakor készült
  pillanatképből, és ha ott nincs, a felvett anyagból (ez akkor számít,
  ha a csatorna csak menet közben vált aktívvá). A 10. csatornán a
  dobkészlet-listából; ha a bank nincs a listában, visszaesik a puszta
  programszámra, ugyanúgy, ahogy a jelentés.
- **A csatorna induló beállításai a SAJÁT trackjük elejére kerülnek**,
  nem egy közös kupacba - így minden sáv önmagában teljes.
- A nevek a szabvány szerinti **Latin-1** kódolással mennek a fájlba.
  A magyar `ő` és `ű` nincs benne, ezért `ö`/`ü` lesz belőlük - így
  olvasható marad (enélkül kérdőjel kerülne a helyükre).

### Hozzáadva - jelentés automatikusan a felvétel mellé

A `.mid` mentése után elkészül mellé a "MIDI fájl beállításainak mentése
olvasható formában" `.txt` jelentése is, azonos alapnévvel.

- A hangszerlistát és a preset nevét KÉRDÉS NÉLKÜL az éppen betöltöttből
  veszi - a felvétel pontosan azokkal készült, a menüpont felugró
  kérdése itt csak zavaró lenne.
- Új, opcionális `presetFajl` paraméter a
  `MidiJelentes.JelentesKeszitese()`-ben: ha meg van adva, a fejlécbe
  bekerül egy **`Preset fájl`** sor a `Hangszerlista` fölé. A menüből
  indított jelentésben ez SZÁNDÉKOSAN nem jelenik meg: ott egy IDEGEN
  MIDI fájlt elemzünk, aminek semmi köze a betöltött preset-hez.
- A `.mid` mentése a fontos: a jelentés hibája NEM veszélyeztetheti -
  hiba esetén csak egy sor jelzi az összegzésben, a felvétel megmarad.
- A kód a `MainForm.FelvetelJelentesenekKeszitese()` közös
  segédrutinjában van, NEM a `MidiFelvevo`-ben (az maradjon tiszta
  SMF-író, ami nem tud a hangszerlistákról) - a 3. fázis kézi és a 4.
  fázis automatikus mentése is ezt fogja hívni.
- A "Jelentés készítése a felvétel mellé" jelölőnégyzet a 3. fázisban
  készül el a felvevő ablakkal együtt (alapból bekapcsolva); addig az
  ideiglenes teszt-gomb mindig elkészíti.

### Hozzáadva - hangnem a felvett fájlban (nem kötelező)

A felvevő ki tudja írni a hangnemet (`FF 59`) a 0. trackbe. **A megadása
SZÁNDÉKOSAN nem kötelező**: egy rögtönzött felvételnek nem feltétlenül
van értelmes hangneme, és jobb üresen hagyni, mint egy oda nem illőt
beírni. Ha nincs megadva, `FF 59` sem kerül a fájlba.

- Három új property a felvevőn: `HangnemMegadva` (alapból False),
  `HangnemElojegyzes` (-7..+7, negatív = b, pozitív = kereszt) és
  `HangnemMoll`. Azért property és nem konstans, mint a tempó és az
  ütem: itt van egy valódi "nincs megadva" állapot is.
- Az előjegyzés bájtja ELŐJELES; az érték írás előtt -7..+7 közé
  szorítódik, hogy egy hibás beállítás se okozhasson érvénytelen fájlt.
- **A metronóm-körben** a felvevő ablakban, a BPM és az ütem beállítás
  UTÁN kerül egy "Hangnem megadása" jelölőnégyzet (alapból kikapcsolva),
  bepipálva listából választható hangnemmel. A metronóm-értékek
  MUNKAMENET-SZINTŰEK lesznek (a felhasználó döntése): nem kerülnek a
  `Kezdet.json`-ba és a presetekbe sem, tehát a `Beallitasok` osztályt
  az a kör sem érinti.

### Változtatva - menü és gyorsbillentyűk

- A **Segédprogramok menü sorrendje** átrendezve (a felhasználó): elöl a
  három preset-tel dolgozó menüpont, utána a kettő, ami MIDI fájlt ír
  vagy olvas. A "Preset készítése MIDI fájlból" így a rokonai mellé
  került.
- **Gyorsbillentyű-ütközés javítva**: a "Régi .mb3 fájlok konvertálása"
  és a "Preset készítése MIDI fájlból" is `R`-t használt, ami miatt
  egyik sem indult el a billentyűre, csak ugrált köztük. Az utóbbi
  mostantól `k` ("Preset &készítése MIDI fájlból") - a menü mind az öt
  gyorsbillentyűje egyedi (P, R, k, M, o).

### Ellenőrzés

- **A `Tesztek/smf_iro_ellenorzes.py` átírva** a megváltozott
  szerkezethez: a szimulált felvétel most ÖT csatornát fed le, négy
  KÜLÖNBÖZŐ névfeloldási esettel (pillanatképből jövő név, hosszú
  ékezetes név, Program Change nélküli csatorna, menet közben aktivált
  csatorna nem nulla bankkal, dobcsatorna), és minden ellenőrzés lefut
  MINDKÉT hangnem-esetre (nincs megadva / g-moll). Azért pont egy b-s
  moll: csak a negatív előjegyzés mutatja meg, ha valaki elrontja az
  előjeles bájt kódolását.
- A szkript **12 szándékosan elrontott változatot** futtat le a végén,
  és mind a 12 fennakad: VLQ folytatásbit, Program Change adatbájtjai,
  trackszám a fejlécben, kezdőállapot rossz trackben, tracknév a track
  sorszámával, kimaradó bank-visszaesés, dobnév a rossz listából,
  hiányzó csatorna-előtag, hosszú ékezetek Latin-1 cseréje,
  delta-nkénti kerekítés, hangnem beírása amikor nincs megadva, és a
  b-s előjegyzés előjel nélkül.
- A kerekítéses eset kimutatásához a teszt-felvétel kapott egy SŰRŰ
  FUTAMOT (15 ms-onkénti hangok): ez a lépésköz minden lépésnél lefelé
  kerekedik, így egy delta-nként kerekítő megvalósításnál a hiba
  összeadódik és kilép a tűrésből. A korábbi időbélyegeknél a kerekítési
  hibák véletlenül kioltották egymást, és ez a mutáció átcsúszott.

### Javítva - dokumentáció és a repóban lévő teszt

- **A `Tesztek/midi_olvaso_ellenorzes.py` a repóban a RÉGI, kiterjesztés
  előtti változat volt** - egy korábbi mentésnél a régi tartalom került
  vissza a lemezre, miközben a CHANGELOG már a bővített változatról írt.
  A helyes verzió visszaállítva és lefuttatva.

### Hozzáadva - Bank Select diagnosztika

- Új `Tesztek/bank_select_teszt.py` + a belőle generált
  `bank_select_teszt.mid`: 13 másodperces diagnosztikai fájl, ami
  ugyanazt a programszámot négy szakaszban szólaltatja meg - bank
  nélkül, `CC0=8`-cal (Roland GS stílus), `CC32=8`-cal (Yamaha XG
  stílus, a MidiBill3 ezt küldi), majd újra bank nélkül. Egyetlen
  meghallgatásból kiderül, hogy egy adott lejátszó (VirtualMIDISynth,
  FluidSynth, hardveres modul) melyik bájtot veszi figyelembe. A bank és
  a program a szkript tetején állítható a saját SoundFonthoz.

## [1.15.0] - 2026-09-06

### Hozzáadva - két új segédprogram

- **"Preset készítése MIDI fájlból"** (új menüpont a Segédprogramok alatt).
  A "MIDI fájlok módosítása" FORDÍTOTTJA: nem beleírja, hanem KIOLVASSA a
  csatorna-beállításokat egy MIDI fájlból, és presetet (`.json`) készít
  belőlük. Több fájl is választható; a preset a forrásfájl MELLÉ, azonos
  néven készül. A leképezés szabályai (a felhasználóval egyeztetve):
  - **Aktív** az a csatorna, amelyen a fájlban TÉNYLEGESEN megszólal hang.
    Szándékosan szigorúbb ez, mint a fájl-patchelő szabálya (ott bármilyen
    csatorna-üzenet elég): ott a tág definíció ártalmatlan, itt viszont egy
    kósza vezérlő miatt aktívvá tett csatorna ténylegesen megváltoztatná a
    játékot. A nem aktív csatornák MINDEN mezője alapértéken marad.
  - **Alap hangerő = 100%**, a csatorna hangereje pedig a nyers CC7-érték.
    A program íráskor a "csatorna hangerő × alap hangerő / 100" szorzatot
    küldi ki, visszaolvasáskor viszont csak a szorzat látszik - 100%-os
    alap hangerővel a preset visszatöltve pontosan ugyanazt a hangerőt
    adja, mint ami a fájlban van (70%-kal halkabb lenne az eredetinél).
  - **Visszhang**: a MIDI-ben csatornánkénti (CC91), a presetben globális -
    a LEGNAGYOBB talált érték nyer, 0-127-ből 0-100%-ra átszámítva.
  - **Bank Select**: amelyik bájt nem nulla, azt tekintjük banknak - így a
    Roland GS (MSB=0, LSB=bank) és a Yamaha XG (MSB=bank) konvenció is
    helyesen olvasódik. Ha mindkettő nem nulla, az LSB nyer (a program is
    így ír).
  - Ami **változik a fájl közben** (hangerő, hangszín stb.), abból mindig
    az ELSŐ érték kerül a presetbe - vagyis a fájl kezdőállapota.
  - Amit a MIDI nem tartalmaz, az alapértéken marad: split pont 60,
    transzponálás 0, pedál-inverzió ki, Alul/Felül bekapcsolva, oktáv 0. A
    MIDI be-/kimenet neve szándékosan üres, hogy a preset betöltése ne
    írja felül az aktuális eszközválasztást.

- **"MIDI fájl beállításainak mentése olvasható formában"** (új menüpont).
  Egy MIDI fájl kezdeti beállításait menti EMBERI OLVASÁSRA szánt szöveges
  (`.txt`) jelentésként, magyar megnevezésekkel - szándékosan nem JSON vagy
  más gépi formátum. Felépítése: általános adatok (formátum, trackek száma,
  felbontás, tempó, ütemjelzés, hangnem, teljes hossz, mely csatornákon van
  adat), majd csatornánként külön blokk. A csatorna-blokkba a TÁGABB szabály
  szerint kerül be egy csatorna (elég bármilyen üzenet) - egy riportnak az a
  dolga, hogy mindent megmutasson, a "Hangok száma" sorból pedig rögtön
  látszik, ha egy csatornán valójában egy hang sem szól. Csatornánként:
  - hangszín NÉVVEL (a 10. csatornánál a dobkészlet-listából), és ha a
    fájlban több hangszínváltás van, MINDEGYIK kiírásra kerül egymás alatt,
    a fájlbeli sorrendben (a felhasználó kifejezett kérése);
  - a program által kezelt vezérlők (hangerő, tér, vibrato, chorus,
    visszhang) MINDIG megjelennek - ha nincsenek a fájlban, azt is kiírja,
    az alapértékkel együtt;
  - statisztikák: hangok száma és hangterjedelem.

- **Két új menüpont a Segédprogramok menüben** (`MainForm.Designer.vb`),
  és a hozzájuk tartozó kezelők a `MainForm.vb`-ben. A két segédprogram
  munkamenete majdnem azonos (fájlválasztás -> feldolgozás -> összegzés),
  ezért a közös lépések KÜLÖN, újrahasznosítható rutinokba kerültek:
  `HangszerlistaValasztasa()`, `KimenetiUtvonal()`,
  `FelulirasEngedelyezese()` és `OsszegzesMegjelenitese()` - így a két
  menüpont kezelője rövid maradt, és a harmadik ilyen segédprogram is
  ezekre tud majd épülni.

- **Új `Modellek/MidiFajlOlvaso.vb` - a program KÖZÖS SMF-értelmezője.**
  Mindkét új segédprogram (és a fájl-patchelő) erre épül. Format 0 ÉS
  format 1 fájlokat is kezel - a patchelő továbbra is csak format 1-be tud
  BESZÚRNI, de az az írás korlátja, nem az olvasásé, így a két új
  segédprogram format 0 fájlokkal is működik. Kezeli a running statust, a
  meta- és SysEx-eseményeket, a Bank Select + Program Change párosítást,
  és a tempóváltásokat is figyelembe véve számol teljes hosszt.

### Hozzáadva - a MIDI fájlok szöveges információi

A "MIDI fájl beállításainak mentése olvasható formában" segédprogram
mostantól a fájlba írt SZÖVEGES adatokat is kiolvassa és megjeleníti. A
MIDI szabvány több szöveges meta-eseményt is definiál - eddig a beolvasó
mindet átugrotta.

- **Két hangszín-sor csatornánként.** A `Hangszín (MIDI fájlban)` a
  FÁJLBA ÍRT szöveges nevet mutatja, a `Hangszín (lista szerint)` pedig
  azt, amit a betöltött hangszerlista mond ugyanarra a bank+program
  párosra. A kettő egymás mellett rögtön megmutatja, ha eltérés van.
  A fájlbeli név forrásának sorrendje: hangszernév (`FF 04`) >
  programnév (`FF 08`) > tracknév (`FF 03`) - az első kettő kifejezetten
  a hangszínt nevezi meg, a tracknév általánosabb. A név TRACK-szintű,
  ezért csatornánként EGYSZER szerepel: a hangszínváltásoknál nem tud
  különbözni, ott a lista szerinti nevek sorakoznak.
- **Cím, szerzői jog, eszköznév, megjegyzés** az általános blokkban
  (`FF 03` a 0. trackben, `FF 02`, `FF 09`, `FF 01`). Csak akkor
  jelennek meg, ha a fájl ténylegesen tartalmazza őket.
- **Szerkezeti jelölők** (`FF 06`) külön blokkban a jelentés végén,
  IDŐPONTTAL együtt (pl. `0:32  Refrén`) - ezek mutatják a darab
  felépítését. Legfeljebb 30 jelölő jelenik meg, hogy egy sűrűn jelölt
  fájl ne öntse el a jelentést.
- **Szabvány-felismerés a SysEx üzenetből**: a fájlok a GM System On /
  GM2 System On / GS Reset / XG System On üzenettel jelzik, melyik
  szabványra készültek. Ez a program szempontjából fontos, mert a
  bankszámok értelmezése szabványonként más - lásd az állapot-dokumentum
  "Következő lépések" 13. pontját.
- **Csatorna-előtag (`FF 20`) támogatása**: a szabvány szerinti módja
  annak, hogy egy több csatornát tartalmazó trackben is egyértelmű
  legyen, melyik csatornáról szól egy név. Ha nincs előtag, a név csak
  akkor társítható, ha a track PONTOSAN EGY csatornát használ - egyébként
  kétértelmű lenne, ezért ilyenkor nem társítjuk.
- **A hangszernév visszaesése a programszámra**: ha a pontos bank+program
  páros nincs a betöltött listában (tipikusan azért, mert a fájl nem
  nulla bankot használ, a `GM.lst` viszont csak a 0-ás bankot ismeri),
  a jelentés a puszta programszám nevét mutatja - így nem marad üres
  kézzel a felhasználó, de látja, hogy ez már csak közelítés. A sor
  szándékosan TÖMÖR (a felhasználó kérése):
  `nincs a listában (bank 124, program 80) - GM: Square Wave`.
  A visszaesés az AKTÍV listából olvas, nem külön GM-listából - egy
  bővebb, kétforrású változat (`Bank 0: GM - ..., Aktuális - ...`)
  elkészült, majd a felhasználó döntése alapján visszavonásra került,
  a `HangszerLista` így továbbra is CSAK az aktív listát ismeri.

### Változtatva

- **A jelentésből kikerült a "Felbontás" sor** és a tempó utáni
  zárójeles mikroszekundum-érték (a felhasználó kérése) - a felbontás
  belül továbbra is kell a hossz kiszámításához, csak nem jelenik meg.
  SMPTE-időzítésű fájlnál ezt mostantól a "Teljes hossz" sor jelzi.
- **A jelentésből kikerült az "Egyéb vezérlők" blokk is** (a felhasználó
  kérése) - a csatorna-blokkban a kiemelt vezérlők után rögtön a "Hangok
  száma" jön. A blokkal együtt törlésre került a `VezerloNevSzoveg()`
  segédfüggvény (a CC-nevek felsorolása) is, mert semmi más nem használta -
  nem maradt holt kód. A közös beolvasó továbbra is GYŰJTI az összes
  kezdő vezérlőértéket (`KezdoVezerlok`), csak a jelentés nem írja ki
  őket: ez az adat kell a preset-készítőnek, és bármikor újra
  megjeleníthető.

### Ellenőrzés

- A `Tesztek/midi_olvaso_ellenorzes.py` kibővítve az új részekkel: a
  szöveges meta-események kiolvasása, a névforrások sorrendje, a
  szabvány-felismerés a SysEx-ből, a jelölők időpontja, a csatorna-előtag
  (`FF 20`), a vezérlőkarakterek kiszűrése a szövegekből, és hogy a darab
  CÍME nem szivárog át csatornanévként (sem format 1-nél, sem format
  0-nál, ahol ugyanaz a track hordozza a címet és a csatorna-adatokat).
  A hibafelismerő képesség igazolt: 7 szándékosan elrontott változatból
  mind a 7 fennakadt. Két hézag menet közben derült ki és pótlásra
  került: nem volt olyan teszteset, amiben egy track TÖBB csatornát
  használ ÉS neve is van, illetve amiben a format 0 fájl trackje nevet
  kap - e nélkül két hibás változat észrevétlen maradt volna.

### Változtatva (szervezés)

- **A `Tesztek` mappa bekerült a repóba.** A verifikáló Python programok
  eddig csak a beszélgetésben léteztek (illetve a `Claude outputs`
  mappában); mostantól a projekt részei, hogy egy későbbi módosítás után
  bárki újra le tudja futtatni őket. Futtatásukhoz a `mido` (1.3.3)
  csomag kell; a projekt fordításához NEM szükségesek.

### Változtatva (refaktor)

- **A `MidiFajlSzerkeszto` átállt a közös beolvasóra**: a saját, ~100 soros
  track-bejáró rutinja (`HasznaltCsatornakFelderitese`,
  `TrackCsatornaiFelderitese` és a hozzájuk tartozó VLQ-olvasók) törölve, a
  helyükön a `MidiFajlOlvaso.BeolvasBajtokbol()` hívása áll. Az
  SMF-értelmezés így egyetlen helyen él, ahogy a csatorna-üzeneteknél is
  tettük. A patchelő viselkedése változatlan: továbbra is a TÁGABB szabály
  szerint dönti el, mely csatornákra írjon beállítást.

### Ellenőrzés

- **Új `Tesztek/midi_olvaso_ellenorzes.py`**: a beolvasó logika hű
  Python-portját futtatja olyan teszt-MIDI-fájlokon, amelyeket a FÜGGETLEN
  `mido` könyvtár állít elő, majd összeveti a két eredményt. Lefedi a
  format 0 és format 1 fájlokat, a running statust, a meta- és
  SysEx-eseményeket, a GS és XG bank-konvenciót (beleértve azt az esetet,
  amikor MINDKÉT bank-bájt nem nulla), a vezérlők első értékének
  megőrzését, a hangszámlálást és a tempóváltásos hosszszámítást. Két
  kézzel összerakott, SZÁNDÉKOSAN hibás fájl is szerepel benne: egy
  running statusszal meta-esemény után, és egy csonka fájl - ezeken azt
  ellenőrzi, hogy a beolvasó nem talál ki nem létező hangokat, és nem száll
  el kivétellel. A hibafelismerő képesség igazolt: 7 szándékosan elrontott
  változatból mind a 7 fennakadt a teszten.

## [1.14.0] - 2026-09-04

### Hozzáadva (MIDI felvétel - 0. és 1. fázis)

A MIDI felvétel/visszajátszás funkció megvalósításának első két,
alapozó fázisa. Működésbeli változás EGYELŐRE NINCS - ezek a lépések
csak a talajt készítik elő a felvevőnek. A teljes fázisterv a Claude
Project `claude/MIDI_felvetel_fazisterv.md` dokumentumában van.

- **`Utvonalak.vb` - `Felvetelek` mappa és fájlnév-képzés (0. fázis).**
  Új `FelvetelekMappa` property (`%AppData%\MidiBill3\Felvetelek`,
  amit az `Elokeszites()` már induláskor létre is hoz), és új
  `UjFelvetelUtvonala()` függvény, ami a felvevő két beállításából
  (fájlnév-alap + "Almappák dátum szerint") előállítja a teljes mentési
  útvonalat: bekapcsolt almappázásnál
  `Felvetelek\2026-08-30\Felvetel_143025.mid`, kikapcsolva
  `Felvetelek\Felvetel_2026-08-30_143025.mid`. Az időbélyeg
  másodperc-pontos (az Automatikus felvétel mód percenként több
  szegmenst is menthet). A felhasználó által beírt nevet megtisztítja a
  fájlnévben tiltott karakterektől, és ütközés esetén `_2`, `_3`
  utótaggal véd a felülírás ellen.

- **`MidiEngine.vb` - a kimenet "csapja" (1. fázis).** Új
  `UzenetKikuldve` esemény, ami MINDEN sikeresen kiküldött üzenet után
  kiváltódik - ez lesz az az egyetlen pont, ahonnan a felvevő
  "hallgatózik". Azért a kimenetre (és nem a bemenetre), mert a felvétel
  a TÉNYLEGES, már feldolgozott hangot rögzíti (split pont,
  transzponálás, oktáv-eltolás és a 16 csatornás szétosztás UTÁN). Az
  esemény `lejatszasBol` és `metronomBol` jelzőt is szállít (a
  metronóm-funkció maga még nem készült el, a paraméter előre került be,
  hogy a szignatúrához ne kelljen kétszer hozzányúlni), valamint az
  üzenet időbélyegét.

- **Az időbélyeg átvezetése a teljes láncon.** A kódátnézés 12.
  pontjában beépített, a NAudio callback-szálán rögzített időbélyeg
  eddig csak "készenlétben állt" - mostantól a
  `MainForm.MidiUzenetFeldolgozasa` -> `MidiEngine.UzenetKuldes` ->
  `KuldesUzenetCsatornara` -> `KuldesEgyCsatornara` láncon végig át van
  vezetve, így a felvétel a hangokat a TÉNYLEGES beérkezési idejükkel
  fogja rögzíteni, nem a UI szál terheltségétől függő, késleltetett
  idővel. Egy leütésből származó, akár 16 csatornára szétosztott üzenet
  azonos időbélyeget kap, hiszen egyszerre szólalnak meg. A UI-ból eredő
  üzenetek (csúszkák, hangszerváltás, pánikgomb) a küldés pillanatában
  kiolvasott értéket kapják - ott ezredmásodperces pontosságra nincs
  szükség.

- **`Modellek/MidiFelvevo.vb` - a felvevő motor (2. fázis).** Új osztály,
  ami időbélyeggel gyűjti a kiküldött MIDI üzeneteket, és szabványos
  `.mid` fájlba menti őket. SZÁNDÉKOSAN nem tud semmit a felhasználói
  felületről (a felvevő ablak hívja majd a metódusait), így ablak nélkül
  is tesztelhető. Állapotgépe a tervezett működést követi:
  `Alap` -> `Felfegyverezve` (a Start megnyomása után, az ELSŐ üzenetre
  várva) -> `Rogzit`. Ami NEM kerül a felvételbe: a metronóm hangja
  (soha), a visszajátszásból származó üzenet (ha az overdub nincs
  bekapcsolva), és a rendszer-üzenetek (`&HF0` felett) - utóbbi azért,
  mert sok billentyűzet folyamatosan küld Active Sensing (`&HFE`) vagy
  MIDI Clock (`&HF8`) üzenetet, ami csak feleslegesen hizlalná a fájlt.
- **"Lógó" hangok automatikus lezárása.** A felvétel lezárásakor a
  felvevő minden még lenyomott hangra Note Off-ot generál (az utolsó
  rögzített üzenet időpontjában) - enélkül egy lenyomva tartott
  billentyű mellett megnyomott Stop után a mentett fájlban örökké szóló
  hang maradna.
- **Saját, bájt-szintű `.mid` írás** (a `MidiFajlSzerkeszto`-ben már
  bevált megközelítéssel, NAudio helyett): a NAudio
  `MidiEventCollection.PrepareForExport()` validálja a Note On/Note Off
  párokat, és a felvétel szélső eseteitől kivételt dobhatna. A mentés
  **format 1**, két trackkel (0. = conductor/tempó, 1. = a felvett
  anyag) - SZÁNDÉKOSAN nem format 0, mert a program saját "MIDI fájlok
  módosítása" segédprogramja jelenleg csak format 1-et kezel, és így a
  felhasználó a SAJÁT felvételeire is rá tudja engedni azt a funkciót.
  Időzítés: 120 BPM, 480 tick/negyed - a hangzást nem befolyásolja, csak
  azt, milyen ütembeosztást mutat egy külső szerkesztő. Az
  ezredmásodperces időbélyegek ABSZOLÚT tick-értékké alakulnak, és csak
  utána vonódnak ki egymásból, hogy a kerekítési hibák egy hosszabb
  felvételen ne adódjanak össze.
- **Az SMF-író ellenőrizve, független MIDI-könyvtárral.** Az író
  algoritmus hű Python-portjával generált teszt-fájlt a **mido** (1.3.3)
  könyvtár hibátlanul beolvasta: format 1, 2 track, 480 PPQ, helyes
  tempó, és az ezredmásodperces időzítés pontosan visszakapható
  (10500 ms -> 0,5 s; 13456 ms -> 3,456 s; 70000 ms -> 60,0 s), a
  Program Change egybájtos adatrészével és a többbájtos VLQ
  delta-time-okkal együtt. Az ellenőrző szkript állításokkal (assert)
  dolgozik, és a hibafelismerő képessége is igazolt (szándékosan
  elrontott felbontás, VLQ-folytatásbit és format-mező mindegyike
  fennakadt rajta). A szkript a repóban, a
  `Tesztek\smf_iro_ellenorzes.py` fájlban található (és a Claude
  Projectben is el van mentve ugyanezen a néven). Futtatás:
  `pip install mido`, majd `python smf_iro_ellenorzes.py` - sikeres
  esetben "MINDEN ELLENORZES RENDBEN" a kimenet, 0 kilépési kóddal.
  FONTOS: ez a VB-kód PORTJA, nem a VB futtatása, tehát ha a
  `MidiFelvevo.vb` SMF-író része változik, a szkriptet is frissíteni
  kell, különben már nem ugyanazt a logikát vizsgálja.
- **A felvett `.mid` fájl mostantól tartalmazza a csatorna-beállításokat
  is.** Eddig csak a felvétel INDULÁSA UTÁN kiküldött üzenetek kerültek a
  fájlba - a hangszíneket viszont a felhasználó jellemzően ELŐTTE állítja
  be, ezért a felvétel egy külső lejátszóban/DAW-ban alapértelmezett
  zongorával szólalt meg. Mostantól a felvevő a rögzítés indulásának
  pillanatában pillanatképet készít az aktív csatornák teljes állapotáról
  (Bank Select + Program Change, hangerő, tér, vibrato, chorus, reverb),
  és azt a zenei track LEGELEJÉRE, tick 0-ra írja, a felvett anyag elé.
  - A pillanatkép SZÁNDÉKOSAN az INDULÁSKORI és nem a mentéskori
    állapotról készül: ha felvétel közben hangszert váltunk, a váltás
    Program Change üzenete magától rögzül, így a mentéskori állapot
    beírása a felvétel elejét hibás hangszínnel szólaltatná meg.
  - A blokk SZÁNDÉKOSAN nem a 0. (conductor) trackbe kerül: az a
    szabvány szerinti szokás alapján csak meta-eseményeket tartalmaz.
  - Csak azokra a csatornákra ír beállítást, amelyeken ténylegesen van
    felvett anyag - ugyanaz a védelmi elv, mint a "MIDI fájlok
    módosítása" segédprogramban (számít pl. akkor, ha egy aktív csatorna
    a split zóna miatt maradt néma).
- **Ütemjelzés (time signature) a mentett fájlban.** Eddig csak a tempó
  szerepelt a conductor trackben, az ütemjelzés nem. Mostantól 4/4-et is
  kiír a program (a hangzást nem befolyásolja, de tisztább, és nem a
  lejátszó feltételezésére bízza) - a metronóm-kör majd a ténylegesen
  beállított ütemet írja ide.

### Javítva
- **Az "Alaphelyzetbe állítás" nem élte túl az újraindítást.** A menüpont
  betöltötte ugyan a gyári `GM.lst`/`GM.drm` listát, de a nyilvántartott
  lista-nevet (`aktualisHangszerListaFajl`) nem állította vissza, mert azt
  csak a `HangszerListaAlkalmazasa` írta - a `HangszerListaBetoltese` nem.
  Két következménye volt:
  - az ablak fejlécében továbbra is a KORÁBBI (pl. SoundFontból
    importált) lista neve látszott - ez volt a szemmel látható tünet;
  - ami sokkal komolyabb: kilépéskor ez a régi név mentődött a
    `Kezdet.json`-ba, így a KÖVETKEZŐ INDULÁSNÁL a program újra a régi
    listát töltötte be. Az alaphelyzetbe állítás tehát az adott
    munkamenetben helyesnek látszott, de újraindítás után visszaállt a
    régi hangszerlista.

  Javítás: a `HangszerListaBetoltese` mostantól maga állítja be a
  lista-nevet is. Így egy helyen dől el, hogy melyik lista aktív, és az
  induláskori hívásnál sincs hatása (a mező amúgy is `GM.lst`-tel indul).
- **Menet közben aktivált csatorna beállításai hiányoztak a felvételből.**
  Ha a felhasználó FELVÉTEL KÖZBEN tett aktívvá egy csatornát, annak a
  hangjai bekerültek a felvételbe, a beállításai (hangszín, hangerő, tér,
  vibrato, chorus, reverb) viszont nem - így egy külső lejátszóban az a
  csatorna alapértelmezett zongorával szólalt meg.
  - OK: a csatorna aktiválása eddig CSAK a motor aktív-jelzőjét állította
    át és a lenyomva tartott hangokat szólaltatta meg újra, állapotot nem
    küldött. Élőben ez nem tűnt fel, mert a szintetizátor már induláskor
    megkapta MIND a 16 csatorna beállítását (az `OsszesCsatornaUjrakuldese`
    szándékosan nem szűr aktivitásra). A felvételbe viszont csak az kerül,
    ami a felvétel INDULÁSA UTÁN ténylegesen kiment - a felvétel elejére
    írt pillanatkép pedig csak az AKKOR aktív csatornákat tartalmazza.
  - Javítás: a `HangszerPanel_AktivValtozott` aktiváláskor mostantól
    kiküldi a csatorna teljes állapotát (új `CsatornaAllapotKuldese`, a
    közös `CsatornaUzenetek` modulra építve), MIELŐTT a lenyomva tartott
    hangok megszólalnának rajta. Így az állapot a felvételbe is bekerül,
    méghozzá a valódi időpontjában - és élőben is helyesebb, mert az
    aktiválás minden esetben érvényesíti a csatorna beállításait.

### Változtatva (refaktor)
- **A "csatornaállapot -> MIDI üzenetek" logika egyetlen közös helyre
  került**: új `Modellek/CsatornaUzenetek.vb` modul (`MidiUzenet`
  struktúra + `CsatornaAllapotUzenetei()` /
  `AktivCsatornakAllapotUzenetei()`). Eddig ugyanez a 8 üzenetes sorozat
  KÉT helyen, egymástól függetlenül létezett
  (`MainForm.AktivCsatornakAllapotanakKuldese` és
  `MidiFajlSzerkeszto`), a felvevővel pedig harmadszor is le kellett
  volna írni. Mostantól mindhárom felhasználás ugyanabból a forrásból
  dolgozik, tehát nem fordulhat elő, hogy az élőben kiküldött állapot
  eltér attól, amit a fájlokba írunk - és egy jövőbeli bővítésnél (pl.
  új CC) elég egy helyen módosítani. A csatorna-állapot CC-számai
  (`MAIN_VOLUME`, `PAN`, `MOD_WHEEL`, `CHORUS`, `REVERB`,
  `BANK_SELECT_MSB`/`LSB`) is ide kerültek, a korábbi két deklaráció
  helyett.
- **Új `MidiEngine.KozvetlenUzenetKuldese()`**: egy kész, összeállított
  csatorna-üzenet kiküldése a feldolgozó logika (split, transzponálás,
  fan-out) kikerülésével - ezen keresztül küldi ki a `MainForm` a közös
  modultól kapott üzenetsorozatot.
- **Az ellenőrző szkript kibővítve** az ütemjelzés és a
  kezdőállapot-blokk vizsgálatával (helyes sorrend, tick 0, és hogy a
  nem játszó csatornák kimaradnak). A hibafelismerő képesség itt is
  igazolt: a szűrő kikapcsolása, a rossz ütemjelzés és a blokk rossz
  helyre kerülése mind fennakadt rajta.

### Hozzáadva (MIDI felvétel - folytatás)
- **IDEIGLENES teszt-gomb a `MainForm`-ban (a 3. fázisban TÖRLENDŐ).**
  Hogy a felvevő motorját valódi hardveren, még a felvevő ablak
  megépítése ELŐTT ki lehessen próbálni, a meglévő `btnMidiFelvetel`
  gomb kapott egy ideiglenes, ide-oda kapcsoló kezelőt: 1. kattintás =
  felfegyverzés (sárga, "Várakozás billentyűre..."), az első leütésre
  automatikusan felvételbe vált (piros, "● FELVÉTEL"), 2. kattintás =
  lezárás és mentés a `Felvetelek` mappába, dátum szerinti almappába,
  majd összegző üzenetablak (fájlnév, üzenetszám, hossz). A kód a
  `MainForm.vb` végén, egyértelműen megjelölt blokkban van - a 3.
  fázisban, amikor a gomb dolga a `MidiFelvevoForm` megnyitása lesz, az
  EGÉSZ blokk törlendő.
- **Új `MidiEngine.ElteltIdoMs` property.** A felvevő automatikus módjának
  kell majd: a szünet-küszöböt csak úgy lehet helyesen mérni, ha
  ugyanarról az óráról olvasunk, amelyikről a rögzített üzenetek
  időbélyegei is származnak.
- **Új `MidiEngine.LejatszottUzenetKuldese()`.** A visszajátszás
  belépési pontja: az üzenetet VÁLTOZATLANUL, egy az egyben küldi ki, a
  feldolgozó logika (split/transzponálás/fan-out) SZÁNDÉKOS kikerülésével
  - a felvétel is a feldolgozott kimenetet rögzítette, tehát a
  lejátszásnak 1:1-ben azt kell visszaadnia. Így a privát
  `KuldesEgyCsatornara` nem kerül kifelé a motorból.

## [1.13.0] - 2026-09-03

### Javítva (kód-higiénia)
- **`AppInfo.vb` elavult komment**: a `Directory.Build.props`-ra és a
  már nem létező `PresetKonvertalo`/`Mb3Konvertalo` projektekre
  hivatkozott. Mostantól a tényleges állapotot írja le: a verziószám
  forrása a `MidiBill3.vbproj` `<Version>` eleme, a projekt egyetlen
  darabból áll.
- **`.vbproj`**: eltávolítva a használaton kívüli
  `<Import Include="System.Data" />` sor.
- **`Hangnevek.HangNev`**: az érvényes MIDI-tartományon (0-127) kívüli
  bemenetre (pl. egy kézzel szerkesztett, hibás preset JSON negatív
  `SplitPont` értékére) eddig kezeletlen `IndexOutOfRangeException`-t
  dobott volna - mostantól ilyenkor magát a számot adja vissza
  hangnév helyett.
- **`Beallitasok.KommentekHozzaadasa`**: a preset JSON mentésekor
  generált `// <hangszernév>` komment a 10. (dob) csatorna
  `Hangszer` mezőjénél eddig a dallamhangszer-listából kereste a
  nevet, holott ott dobkészlet-variáció szerepel. Mostantól a
  találatok sorszámából felismeri a 10. csatornát, és a dedikált
  dobkészlet-listából (új `HangszerLista.DobNev()`, a meglévő
  `Nev()` párja) nevez meg hangszert.
- A SoundFont-import GM2 (120-as) dobbank-kezelése SZÁNDÉKOSAN
  változatlan maradt - elfogadott, tudatos tervezési döntés, nem
  hiba (lásd az állapot-dokumentum "Ismert, tudatosan meghozott
  döntések" szakaszát).
- Ismételt, teljes kódátnézés (a felvétel-fejlesztés megkezdése előtt):
  funkcionális hibát nem talált, csak két elavult/duplikált doc-kommentet
  javított (`MainForm.midiKijelzoIdozito_Tick` still 10 Hz/Interval=100-at
  írt a tényleges 5 Hz/200 ms helyett; `HangszerPanel.vb`-ben egy
  duplikált `<summary>` XML-doc tag volt).

### Javítva (Publish)
- **`GM.lst`/`GM.drm` hiányzott a Publish (self-contained, single-file)
  kimenetéből.** Self-contained, single-file publish módnál (pl. Folder
  profil, win-x64 RID-del, "Produce single file" bekapcsolva) a .NET SDK
  minden managed assembly-t egyetlen `.exe`-be olvaszt össze, és a
  TÉNYLEGES publish-kimenet (`bin\...\publish\<RID>\`) ilyenkor a
  szokásos `CopyToOutputDirectory`/`CopyToPublishDirectory` metaadat
  ellenére sem tartalmazta a laza (nem-assembly) `GM.lst`/`GM.drm`
  fájlokat - ez egy ismert SDK-sajátosság. Megoldás: új
  `GyariAdatokMasolasaPublishUtan` MSBuild `Target`
  (`AfterTargets="Publish"`) a `.vbproj`-ban, ami a publish célfeladat
  után feltétel nélkül, közvetlenül a `$(PublishDir)`-be másolja mindkét
  fájlt - így Folder, self-contained és single-file publish esetén is
  megbízhatóan megjelennek az `.exe` mellett.

## [1.12.0] - 2026-09-03

### Javítva
- **`BeallitasokAlkalmazasa`**: a `TranszponalasFrissites()` hívása
  mostantól a már kiszámolt, korlátozott `ujTranszponalas` értéket kapja
  a nyers `b.Transzponalas` helyett - ugyanazt a mintát követve, mint a
  metódus összes többi beállítása. Viselkedésbeli hatása nem volt (a
  `TranszponalasFrissites()` maga is korlátoz), tisztán kód-konzisztencia
  javítás.
- **`Beallitasok.Betoltes`**: egy szó szerint `null` tartalmú (vagy csak
  whitespace-eket/kommenteket tartalmazó) JSON fájl betöltésekor eddig
  kezeletlen `NullReferenceException` szállt el, és a felhasználó egy
  angol, értelmezhetetlen hibaüzenetet látott a szép magyar helyett.
  Mostantól a `Deserialize` után közvetlenül ellenőrzi, hogy az eredmény
  nem `Nothing`, és ha az, egy értelmes, magyar `InvalidDataException`-t
  dob helyette.

### Változtatva (teljesítmény)
- **A MIDI-üzenet szöveggé alakítása** (`MidiUzenetSzoveg()` - string-
  összefűzés + `Hangnevek`/`HangszerLista`-keresés) eddig MINDEN
  beérkező MIDI-üzenetnél lefutott a `MidiUzenetFeldolgozasa`-ban, pedig
  a kijelzés (`lblMidiUzenet`) amúgy is csak a `midiKijelzoIdozito`
  ütemében, max. 10x/mp frissül. Mostantól minden üzenetnél csak a 3
  nyers `Integer` értéket tároljuk el (`utolsoMidiUzenetStatusz`/
  `utolsoMidiAdat1`/`utolsoMidiAdat2`), a tényleges szöveggé alakítás
  átkerült a `midiKijelzoIdozito_Tick`-be - így a throttling eredeti
  szándéka (a kijelzés-frissítés korlátozása) most már a string-építésre
  is kiterjed, nem csak a UI-frissítésre.
- **`midiKijelzoIdozito.Interval`: 100 ms (10 Hz) → 200 ms (5 Hz).** A
  felhasználó jelezte, hogy 10 Hz-en a rövid szöveges MIDI-üzenet
  felirat gyakorlatilag csak villog, nem olvasható el - egy emberi
  szem/agy kb. 250-400 ms-ra van szüksége egy ilyen rövid felirat
  tényleges elolvasásához. 200 ms kényelmesen olvasható, mégis élőnek
  hat.
- **Az Alap hangerő és a Reverb csúszka MIDI-elárasztása megszűnt.** A
  `trkAlapHangero_ValueChanged`/`trkReverb_ValueChanged` eddig a `TrackBar`
  `ValueChanged` eseményének MINDEN tüzelésénél (nemcsak a húzás végén,
  hanem gyakorlatilag minden pixelnyi elmozdulásnál) kiküldött 16-16 MIDI
  Controller Change üzenetet. Új `csuszkaMidiIdozito` (`Interval = 50`,
  20 Hz) a `midiKijelzoIdozito` mintáját követve: a felirat
  (`grpVolume.Text`/`grpReverb.Text`) továbbra is azonnal frissül, de a
  tényleges MIDI-küldés a `csuszkaMidiIdozito_Tick`-ben, legfeljebb
  20x/mp történik, mindig a csúszka legfrissebb értékét kiküldve.
- **A pánik-söprés ("Csend legyen!" gomb és minden kilépés) ~60-szor
  gyorsabb lett.** A `MidiEngine.HangokKikapcsolasa(c, True)` eddig
  csatornánként 128 explicit Note Off üzenetet küldött (16 csatornára
  összesen 2048-at), ami egy valódi hardveres MIDI porton (31250 baud)
  kb. 2 másodperces BLOKKOLÓ küldést jelentett - ez fut le a
  pánikgombnál ÉS minden kilépéskor is (`MainForm_FormClosing`), tehát
  mindkettőt feleslegesen lassította. Mostantól csatornánként csak az
  All Sound Off (CC120) + All Notes Off (CC123) párost küldi (2
  üzenet/csatorna, összesen ~32), ami a MIDI-szabvány szerint azonnal
  elnémít minden szóló hangot - ugyanaz a minta, amit a kimenetváltási
  hangelnémításnál (`OsszesHangElnemitasa`, lásd fentebb) is használunk.

### Hozzáadva (előkészítés)
- **Callback-szál-pontos MIDI időbélyeg-infrastruktúra** a jövőbeli
  MIDI-felvétel funkcióhoz (`MidiFelvevoForm`). A `MidiEngine` mostantól
  a konstruktorban elindított `Stopwatch`-csal (`idozito`) MÉG a
  callback-szálon, a UI-ra való `BeginInvoke`-átadás ELŐTT rögzíti az
  üzenet beérkezésének pillanatát - így az időbélyeg független a UI szál
  esetleges terheltségétől (jitter). A `MidiUzenetErkezett` eseménynek
  (és a `MainForm.midiMotor_MidiUzenetErkezett` / `MidiUzenetFeldolgozasa`
  metódusoknak) új `idobelyegMs As Long` paramétere lett - a
  `BeginInvoke` lambdája ezt SZÁNDÉKOSAN elcsípett (captured) helyi
  paraméterként adja tovább, nem egy megosztott mezőn keresztül, hogy egy
  gyorsabban beérkező későbbi üzenet ne írhassa felül egy korábban sorba
  állított üzenet időbélyegét. Ez önmagában még NEM felvétel funkció -
  a paramétert egyelőre semmi nem használja fel -, csupán a talaj-
  előkészítés, amit a kódátnézés 12. pontja jelzett szükségesnek.

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

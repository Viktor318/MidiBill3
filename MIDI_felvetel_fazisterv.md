# MIDI felvétel/lejátszás - megvalósítási fázisterv

Ez a dokumentum a `MidiFelvevoForm` (MIDI felvétel + visszajátszás)
funkció KÓDOLÁSI sorrendjét rögzíti. A funkció TARTALMI terve (mit
csináljon, milyen vezérlőkkel, milyen alapértékekkel) NEM itt van, hanem
a `MidiBill3_VBNET_aktualis_allapot.md` "Következő lépések" 4. pontjában
- az a terv LEZÁRT, érdemi design-kérdés nincs hátra. Ez a fájl csak azt
mondja meg, milyen sorrendben építjük fel, és hol tartunk éppen.

**Terv elfogadva**: 2026-09-03. **Jelenlegi állapot**: a 0-2. fázis kész
és a felhasználó által tesztelve (kiadva az 1.14.0 verzióban), a
következő lépés a **3. fázis**.

---

## Eldöntött technikai kérdések

### 1. Az időbélyeg útja (FONTOS)

A felvétel a KIMENŐ üzeneteket rögzíti, azok viszont a UI szálon
keletkeznek - így a kódátnézés 12. pontjában beépített, callback-szálon
rögzített pontos időbélyeg önmagában nem hasznosulna.

**Döntés**: az élő játéknál a bejövő `idobelyegMs` végig át van vezetve
a `MainForm.MidiUzenetFeldolgozasa` -> `MidiEngine.UzenetKuldes` ->
`KuldesUzenetCsatornara` -> `KuldesEgyCsatornara` láncon, és a felvevő
EZT kapja meg. A UI-ból eredő üzenetek (csúszkák, hangszerváltás) a
küldés pillanatában olvasott `Stopwatch`-értéket kapják
(`idobelyegMs = -1` alapérték jelenti azt, hogy "olvasd ki most").

A felhasználó megjegyzése ehhez: **csúszkamozgatás játék közben nagyon
ritkán fog előfordulni**, ezért ott a pontosságra nem kell hangsúlyt
fektetni - a lényeg a hangok időzítésének pontossága.

### 2. A `metronomBol` paraméter MOST kerül be

Az eredeti terv szerint ez majd a metronóm-körben került volna be, de
akkor kétszer kellene ugyanahhoz a szignatúrához hozzányúlni. Ezért már
az 1. fázisban bekerült (a metronóm-funkció maga továbbra is későbbi kör).

### 3. `.mid` írás saját kóddal, olvasás NAudio-val

Az eredeti terv NAudio-t mondott mindkét irányra. **Módosítás**: a
MENTÉS saját, bájt-szintű SMF-írással készül (ugyanaz a bevált
megközelítés, mint a `Modellek/MidiFajlSzerkeszto.vb`-ben), mert a
NAudio `MidiEventCollection.PrepareForExport()` validálja a Note On /
Note Off párokat, és a felvétel végén "lógva maradt" hangoktól elszállhat
- saját írással teljes a kontroll. A BEOLVASÁS (lejátszáshoz) viszont
NAudio-val történik, ott tényleg kényelmesebb.

### 4. Tempó, felbontás, ütemjelzés és hangnem a mentett fájlban

Valós időben (ms-ban) veszünk fel, de a `.mid`-be kell egy tempó- és
tick-felbontás-érték, mert ez határozza meg, hány ütemre osztva mutatja
majd egy DAW. **Döntés**: 120 BPM, 480 tick/negyed - semleges alapérték,
a hangzást nem befolyásolja. A conductor trackbe **ütemjelzés (4/4) is
kerül** (ez eredetileg kimaradt, a felhasználó jelezte).

**A hangnem (`FF 59`) viszont NEM kötelező** (2026-09-06, a felhasználó
kérése - eredetileg alapértelmezett C-dúr volt tervben, de elvetettük):
egy rögtönzött felvételnek nem feltétlenül van értelmes hangneme, és
jobb üresen hagyni, mint egy oda nem illőt beírni. Ha nincs megadva,
`FF 59` sem kerül a fájlba, és a jelentésben a "nincs a fájlban" sor
jelenik meg.

A felvevő ehhez HÁROM property-t kapott (nem konstansokat, mint a tempó
és az ütem - mert itt van egy explicit "nincs megadva" állapot is):
`HangnemMegadva` (alapból False), `HangnemElojegyzes` (-7..+7, negatív =
b, pozitív = kereszt) és `HangnemMoll`.

**A metronóm-körben** a felvevő ablak metronóm-részébe, a BPM és az ütem
beállítás UTÁN kerül egy **"Hangnem megadása" jelölőnégyzet** (alapból
KIkapcsolva), és bepipálva egy **listából** lehet hangnemet választani.
Ekkor a tempó és az ütemjelzés is onnan jön majd, a mostani konstansok
(`MIKROSZEC_PER_NEGYED`, `UTEM_*`) helyett.

**Ezek az értékek MUNKAMENET-SZINTŰEK** (a felhasználó döntése,
2026-09-06): a hangnem és a többi metronóm-érték NEM kerül a
`Kezdet.json`-ba és a presetekbe sem, tehát a program újraindításakor az
alapértékre áll vissza. Következmény: a `Beallitasok` osztályt EZ A KÖR
NEM ÉRINTI - a metronóm-beállítások a felvevő ablak saját mezői
maradnak, és a régi preset fájlok is változatlanul betölthetők.

### 5. A mentett fájl format 1, csatornánként külön trackkel

Egyetlen track esetén a format 0 egyszerűbb lenne, DE a program saját
"MIDI fájlok módosítása" segédprogramja (`MidiFajlSzerkeszto`) jelenleg
csak **format 1** fájlokat kezel - format 0-val mentve a felhasználó a
SAJÁT felvételeire nem tudná ráengedni ezt a funkciót.

A 2. fázisban a mentés még KÉT trackkel készült (conductor + egy közös
zenei track). **2026-09-06-től csatornánként külön track** (a felhasználó
kérése, hogy a fájlba tracknév is kerüljön): a 0. a conductor track (a
darab címe, tempó, ütemjelzés, hangnem), utána MINDEN SZEREPLŐ CSATORNA
kap egy sajátot. Egy csatorna-track tartalma:

- csatorna-előtag (`FF 20`),
- tracknév (`FF 03`) `"NN - Hangszernév"` alakban, ahol NN az 1-től
  induló CSATORNASZÁM két számjegyen (nem a track sorszáma - a
  felhasználó ezt kifejezetten így kérte),
- hangszernév (`FF 04`), ha kideríthető,
- a csatorna induló beállításai tick 0-n (lásd 7. pont),
- végül a csatorna felvett üzenetei.

Ez a DAW-okban bevett elrendezés (nevesített, elkülönülő sávok), és
ettől a saját jelentésünk `Hangszín (MIDI fájlban)` sora is értelmes
nevet mutat. A hangszernév a BETÖLTÖTT listából jön, a csatorna első
Program Change-e alapján; a 10. csatornán a dobkészlet-listából, és ha a
bank nincs a listában, visszaesik a puszta programszámra.

A nevek `FF`-szövegként Latin-1 kódolással mennek a fájlba (ezt írja elő
a szabvány) - a magyar `ő` és `ű` nincs benne, ezért `ö`/`ü` lesz
belőlük, hogy olvasható maradjon.

### 6. Rendszer-üzenetek kiszűrése a felvételből (a 2. fázisban derült ki)

A felvevő csak a csatorna-üzeneteket (`&H80`-`&HEF`) rögzíti. Sok
billentyűzet folyamatosan küld Active Sensing (`&HFE`) vagy MIDI Clock
(`&HF8`) üzenetet, amelyek a `MidiEngine.UzenetKuldes` "egyéb" ágán
átmennének a kimenetre - ezek csak feleslegesen hizlalnák a fájlt, és a
felvétel szempontjából nincs értelmük.

### 7. Csatorna-állapot a felvétel elején (a felhasználó kérésére, 2026-09-04)

A felvétel csak azokat az üzeneteket rögzíti, amelyek az indulás UTÁN
mennek ki - a hangszíneket viszont a felhasználó jellemzően ELŐTTE
állítja be, ezért a felvétel egy külső lejátszóban alapértelmezett
zongorával szólalt meg. **Döntés**: a felvevő a rögzítés INDULÁSÁNAK
pillanatában pillanatképet készít az aktív csatornák állapotáról, és azt
tick 0-ra írja. (A conductor trackbe SEM akkor, sem most nem kerül
csatorna-üzenet - abba a szabvány szerinti szokás alapján csak
meta-események valók.)

**2026-09-06-től** minden csatorna beállításai a SAJÁT trackjük elejére
kerülnek, nem egy közös kupacba a zenei track elejére - így minden sáv
önmagában teljes.

Két fontos részlet:
- A pillanatkép az INDULÁSKORI és nem a mentéskori állapotról készül:
  felvétel közbeni hangszerváltás Program Change üzenete magától rögzül,
  így a mentéskori állapot beírása a felvétel elejét rontaná el. Ezért a
  `Felfegyverzes()` egy `Func(Of List(Of MidiUzenet))` lekérdezőt kap, és
  a felvevő PONTOSAN a rögzítés indulásakor hívja meg.
- Csak azokra a csatornákra ír beállítást, amelyeken ténylegesen van
  felvett anyag - ugyanaz a védelmi elv, mint a `MidiFajlSzerkeszto`-ben.

### 8. A "csatornaállapot -> MIDI üzenetek" logika EGY helyen (2026-09-04)

Új `Modellek/CsatornaUzenetek.vb` modul: `MidiUzenet` struktúra,
a csatorna-állapot CC-számai, és a `CsatornaAllapotUzenetei()` /
`AktivCsatornakAllapotUzenetei()` függvények. Korábban ugyanaz a 8
üzenetes sorozat KÉT helyen létezett egymástól függetlenül
(`MainForm.AktivCsatornakAllapotanakKuldese` és `MidiFajlSzerkeszto`),
a felvevővel harmadszor is le kellett volna írni. Mostantól mind a
három ugyanabból a forrásból dolgozik. A `MainForm` a kész
üzenetsorozatot az új `MidiEngine.KozvetlenUzenetKuldese()`-n át küldi
ki.

### 9. Jelentés a felvétel mellé (a felhasználó kérésére, 2026-09-06)

A `.mid` mentése után automatikusan elkészül mellé a "MIDI fájl
beállításainak mentése olvasható formában" `.txt` jelentése is, azonos
alapnévvel. **Döntések**:

- A hangszerlistát és a preset nevét KÉRDÉS NÉLKÜL az éppen betöltöttből
  veszi (`aktualisHangszerListaFajl`, `aktualisFajlNev`) - a felvétel
  pontosan azokkal készült. A menüpont felugró kérdése itt zavaró lenne.
- A jelentés fejlécében megjelenik egy **`Preset fájl`** sor. Ez CSAK a
  felvétel melletti jelentésben van: a menüből indított jelentés egy
  IDEGEN MIDI fájlt elemez, aminek semmi köze a betöltött preset-hez.
  Technikailag a `MidiJelentes.JelentesKeszitese()` opcionális
  `presetFajl` paramétere - üres sztringnél a sor elmarad.
- A `.mid` mentése a fontos: a jelentés hibája NEM veszélyeztetheti.
  Hiba esetén csak egy sor jelzi az összegzésben, a felvétel megmarad.
- A kód NEM a `MidiFelvevo`-ben van (az maradjon tiszta SMF-író, ami nem
  tud a hangszerlistákról), hanem a `MainForm.FelvetelJelentesenekKeszitese()`
  közös segédrutinjában - ezt hívja majd a 3. fázis kézi és a 4. fázis
  automatikus mentése is.
- **Kapcsolható lesz**: a felvevő ablakban egy "Jelentés készítése a
  felvétel mellé" jelölőnégyzet, **alapból BEKAPCSOLVA**. A négyzet a
  3. fázisban készül el (addig az ideiglenes teszt-gomb mindig elkészíti
  a jelentést). Ez a 4. fázis miatt fontos: az automatikus mód sok rövid
  fájlt gyárthat, mindegyik mellé egy `.txt` sok lehet.

---

## Fázisok és haladás

- [x] **0. fázis - Útvonalak és fájlnév-képzés.** `Utvonalak.vb`:
  `FelvetelekMappa` property (`%AppData%\MidiBill3\Felvetelek`),
  `UjFelvetelUtvonala()` a dátum-almappás/lapos névképzéssel
  (másodperc-pontos időbélyeg), fájlnév-tisztítás (a felhasználó által
  beírt névből), ütközésvédelem, és a mappa létrehozása az
  `Elokeszites()`-ben.

- [x] **1. fázis - A MIDI-motor "kimeneti csapja".** `MidiEngine`:
  `KuldesEgyCsatornara` + `idobelyegMs` / `lejatszasBol` / `metronomBol`
  paraméterek, új `UzenetKikuldve` esemény minden SIKERES küldés után,
  az időbélyeg átvezetése a teljes láncon, és publikus
  `LejatszottUzenetKuldese()` / `KozvetlenUzenetKuldese()` metódusok (a
  privát `KuldesEgyCsatornara` így nem kerül kifelé).
  **A felhasználó tesztelte: nem romlott el semmi.**

- [x] **2. fázis - A felvevő motor, UI nélkül.** Új
  `Modellek/MidiFelvevo.vb`. Tartalma:
  - `FelvevoAllapot` enum: `Alap` -> `Felfegyverezve` -> `Rogzit`
  - `Felfegyverzes(kezdoAllapotLekerdezo)`, `UzenetRogzitese(...)`,
    `LezarasEsMentes(utvonal)`, `Eldobas()` - ez a négy metódus fedi le a
    3. és 4. fázis igényeit
  - `LejatszottHangokRogzitese` property (overdub, alapból KI) - ezzel a
    7. fázis szűrési logikája már készen van
  - `RogzitettHosszMs` és `UtolsoEsemenyIdobelyegMs` property-k - a 4.
    fázis két küszöbének méréséhez
  - "Lógó" hangok automatikus lezárása: a felvétel végén minden még
    lenyomott hangra Note Off generálódik
  - Csatorna-állapot pillanatkép (lásd 7. döntés)
  - Saját, bájt-szintű SMF-írás (lásd 3-5. döntés)
  - Új `MidiEngine.ElteltIdoMs` property a küszöbfigyeléshez

  **A felhasználó valódi hardveren tesztelte: a felvétel jól működik**
  (egy IDEIGLENES teszt-gombbal a `MainForm`-ban, lásd lentebb).

  **Utólagos bővítés (2026-09-06, kiadás előtt)**: csatornánkénti
  trackek + tracknév/hangszernév + hangnem a fájlban, és automatikus
  jelentés a felvétel mellé - lásd a 4., 5. és 9. döntést.

### A 4. fázis küszöbfigyeléséhez használt három tag

- `MidiEngine.ElteltIdoMs` - a "most" (a közös stopperóra állása).
  A név a felhasználó kérésére `EltelIdoMs`-ről lett javítva
  `ElteltIdoMs`-re (magyarosabb alak).
- `MidiFelvevo.UtolsoEsemenyIdobelyegMs` - a legutóbb rögzített üzenet
  ideje. **Szünet-küszöb** = `ElteltIdoMs - UtolsoEsemenyIdobelyegMs`.
- `MidiFelvevo.RogzitettHosszMs` - az első és utolsó rögzített üzenet
  közt eltelt idő = **játékidő-küszöb**.

FONTOS: mindkét oldalnak UGYANARRÓL az óráról kell olvasnia - egy másik
időforrás (pl. `DateTime.Now`) keverése lassú, nehezen észrevehető
csúszást okozna.

### FIGYELEM: ideiglenes teszt-kód a MainForm végén

Hogy a felvevőt a felvevő ablak megépítése ELŐTT is ki lehessen próbálni
valódi hardveren, a `btnMidiFelvetel` gomb kapott egy IDEIGLENES,
ide-oda kapcsoló kezelőt (felfegyverzés -> sárga; első leütésre piros;
második kattintás = mentés + összegző üzenetablak). A kód a `MainForm.vb`
VÉGÉN van, egyértelműen megjelölt blokkban, három mezővel és három
metódussal. **A 3. fázisban ez az EGÉSZ blokk törlendő**, mert a gomb
dolga akkor a `MidiFelvevoForm` megnyitása lesz.

FONTOS: a blokkon KÍVÜL, tőle FÜGGETLENÜL áll a
`MainForm.FelvetelJelentesenekKeszitese()` - az VÉGLEGES kód, nem
törlendő, a 3. és 4. fázis is hívni fogja.

---

- [ ] **3. fázis - `MidiFelvevoForm` váz + kézi felvétel.** Az ablak, a
  "Felvétel" és "Mentés" csoport, a felfegyverzés-logika, a színes
  állapotjelzés (nyugalmi / sárga "Várakozás billentyűre..." / piros
  "● FELVÉTEL"), a meglévő `btnMidiFelvetel` bekötése (az ideiglenes
  teszt-blokk törlésével együtt), és a `MainForm_FormClosing`
  kiegészítése. **FIGYELEM**: az "aktív felvétel van, leállítsuk?"
  kérdésnek az EGÉSZ takarítás ELÉ kell kerülnie, különben megszakított
  kilépés után a MIDI bemenet lezárva maradna (erről már van
  figyelmeztető komment a `MainForm`-ban).
  Ide tartozik még a **"Jelentés készítése a felvétel mellé"
  jelölőnégyzet** (alapból bekapcsolva, lásd 9. döntés).

- [ ] **4. fázis - Automatikus felvétel mód.** A két küszöb
  (játékidő/szünet), a szegmensekre bontás, az automatikus
  újra-felfegyverzés, és hogy a kézi Stop kiveszi a pipát az
  "Automatikus felvétel" checkboxból.

- [ ] **5. fázis - Lejátszás.** `.mid` beolvasása (NAudio), lejátszó
  időzítővel, közvetlen küldés a kimeneti csapra
  (`LejatszottUzenetKuldese`), zöld gomb aktív lejátszás közben, Stop.

- [ ] **6. fázis - Tekerés és időkijelzés.** Húzható csúszka,
  `MouseDown`/`MouseUp` megkülönböztetés a programozott frissítéstől,
  "1:22 / 3:41" kijelzés, és a tekerés utáni All Notes Off (beragadt
  hangok ellen).

- [ ] **7. fázis - Overdub.** "A lejátszott hangok rögzítése" checkbox
  (alapból KI) bekötése a `MidiFelvevo.LejatszottHangokRogzitese`
  property-re - a szűrési logika maga már a 2. fázisban elkészült.

- [ ] **8. fázis - Tesztelés, dokumentáció, verzió.** Végigtesztelési
  lista, `CHANGELOG.md`, állapot-dokumentum, verziólépés.

**Ezután, KÜLÖN körben**: metronóm-funkció (tempó, ütem, **hangnem**,
hangszínek, vizuális piktogram) - a terve szintén lezárt, lásd az
állapot-dokumentum "Következő lépések" 3. pontját. A hangnem-rész a
felhasználó 2026-09-06-i kérése: a BPM és az ütem beállítás UTÁN egy
"Hangnem megadása" jelölőnégyzet (alapból ki), bepipálva listából
választható hangnemmel - és amit ott beállít, az menjen a mentett
fájlba.

---

## Ellenőrző szkript

`Tesztek/smf_iro_ellenorzes.py` (a repóban és a Claude Projectben is).
Az SMF-író logika hű Python-portját futtatja, és a keletkezett fájlt a
FÜGGETLEN `mido` könyvtárral olvassa vissza, állításokkal ellenőrizve:
format 1, 1 + N track, 480 PPQ, tempó, 4/4 ütemjelzés, a hangnem
MINDKÉT esete (nincs megadva / meg van adva), a darab címe, a csatorna-trackek sorrendje, a csatorna-előtag, a
tracknevek és hangszernevek (a névfeloldás mind a négy esetére), a
kezdőállapot helye csatornánként, és az ezredmásodperces időzítés
pontossága. Futtatás: `pip install mido`, majd
`python smf_iro_ellenorzes.py`.

**FONTOS**: ez a VB-kód PORTJA, nem a VB futtatása - ha a
`MidiFelvevo.vb` SMF-író része változik, a szkriptet is frissíteni kell.

A hibafelismerő képessége igazolt: a szkript végén **12 szándékosan
elrontott változat** fut le, és mindegyiknek meg KELL buknia (VLQ
folytatásbit, Program Change adatbájtjai, trackszám a fejlécben, a
kezdőállapot rossz trackben, tracknév a track sorszámával, kimaradó
bank-visszaesés, dobnév a rossz listából, hiányzó csatorna-előtag,
hosszú ékezetek Latin-1 cseréje, delta-nkénti kerekítés, hangnem akkor
is beírva, amikor nincs megadva, és a b-s előjegyzés előjel nélkül).
Ha egy elrontott változat ÁTMEGY, a teszt nem képes észrevenni azt a
hibafajtát, és bővíteni kell.

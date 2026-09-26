# MIDI felvétel/lejátszás - megvalósítási fázisterv

Ez a dokumentum a `MidiFelvevoForm` (MIDI felvétel + visszajátszás)
funkció KÓDOLÁSI sorrendjét rögzíti. A funkció TARTALMI terve (mit
csináljon, milyen vezérlőkkel, milyen alapértékekkel) NEM itt van, hanem
a `MidiBill3_VBNET_aktualis_allapot.md` "Következő lépések" 4. pontjában
- az a terv LEZÁRT, érdemi design-kérdés nincs hátra. Ez a fájl csak azt
mondja meg, milyen sorrendben építjük fel, és hol tartunk éppen.

**Terv elfogadva**: 2026-09-03. **Jelenlegi állapot**: a 0-2. fázis kész
és a felhasználó által tesztelve (kiadva az 1.14.0 verzióban). A **3.
fázis** is kész és tesztelve (kiadva az **1.18.0** verzióban,
2026-09-24). A felvevő beállításainak és az ablakpozícióknak a
megőrzése (`MidiBill3.ini`) önálló lépésként elkészült az **1.19.0**
verzióban. A **4. fázis** is kész és tesztelve (kiadva az **1.20.0**
verzióban, 2026-09-25). Az **5. fázis** (lejátszás) is kész és tesztelve
(kiadva az **1.21.0** verzióban, 2026-09-26). Következik a **6. fázis**
(tekerés és időkijelzés). A major
verzióemelés (2.0.0) a teljes felvevő ablak elkészültekor esedékes.

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

### 10. A felvételt az első LEÜTÉS indítja (a felhasználó döntése, 2026-09-23)

A 2. fázisban a felfegyverzett felvevőt BÁRMILYEN kiküldött üzenet
elindította - holott a kódkomment azt állította, hogy ugyanúgy működik,
mint az "Extra MIDI üzenetek" jelölőnégyzet, ami az első leütésre vár.
Egy felfegyverzés utáni csúszkamozgatás, hangszerváltás, preset-betöltés
vagy eszközváltás így már elindította a felvételt, pont azt a néma
előteret gyártva, amit a felfegyverzés el akart kerülni.

**Döntés**: felfegyverzett állapotban CSAK egy valódi leütés (Note On,
nullánál nagyobb sebességgel) indít. A többi üzenet nem indít és nem is
rögzül - ami közülük számít, azt a 7. pont szerinti pillanatkép úgyis
tartalmazza, mert az a rögzítés INDULÁSÁNAK pillanatában olvassa ki a
csatorna-beállításokat. Egy felfegyverzés után átállított hangszín tehát
automatikusan bekerül.

**Kivétel - az előadói vezérlők**: a kitartó pedál (CC64) és a
modulációs kerék (CC1) a billentyűzetről jön, nem a panelről, ezért a
pillanatkép nem tudna róluk. A felvevő a felfegyverzés alatt csatornánként
megjegyzi az UTOLSÓ értéküket, és a pillanatkép UTÁN tick 0-ra írja - így
az első hang előtt lenyomott pedál is benne lesz a fájlban, a moduláció
esetén pedig a ténylegesen kiküldött érték írja felül a panelét. A lista
bővíthető (`MidiFelvevo.eloadoiVezerlok`).

A szűrők SORRENDJE is rögzített: a metronóm, a visszajátszás (overdub
nélkül) és a rendszer-üzenetek kiszűrése az indítás ELŐTT fut, tehát
ezek sem indíthatnak.

Mellékhatás: a kimeneti eszközváltás söprése felfegyverzett állapotban
már nem indítja el a felvételt.

### 11. A program belső karbantartó üzenetei nem kerülnek a felvételbe (2026-09-23)

A kimeneti eszköz váltásakor a program elnémítja a régi eszközt (16
csatornán All Sound Off + All Notes Off), majd az új eszközre mind a 16
csatorna beállítását újraküldi. AKTÍV felvétel közben ez eddig mind
belekerült volna a fájlba - az eredmény 16 track, akkor is, ha csak egy
csatornán szólt hang.

**Döntés**: az `UzenetKikuldve` esemény új `belsoUzenet` jelzőt kapott
(a `lejatszasBol` / `metronomBol` mintájára). A `MidiEngine`
`BelsoKarbantartasKent(muvelet)` metódusa alatt kiküldött minden üzenet
ezzel megy ki - ide tartozik az `OsszesHangElnemitasa` és a `MainForm`
kimenetváltó kezelője (a megnyitás + `OsszesCsatornaUjrakuldese`). A
felvevő ezeket se nem rögzíti, se nem indul el tőlük.

Egy apró, de fontos részlet: a kimenet cseréjekor a motor a saját
hangnyilvántartását is törli, tehát a billentyű későbbi felengedésekor
már NEM küld Note Off-ot. Ezért a felvevő a belső elnémításnál maga zárja
le az épp szóló hangokat, abban a pillanatban - különben a fájlban a
felvétel végéig szólnának.

A pánikgomb viszont NEM belső: az a felhasználó zenei döntése, rögzül.

### 12. Bemenetváltáskor a lenyomott hangok elengedése (2026-09-23)

Ha egy billentyű lenyomva volt, amikor a felhasználó MIDI bemenetet
váltott, a felengedése már a lezárt eszközről jött volna, vagyis soha - a
hang a pánikgombig szólt. A `MidiEngine.BemenetBezaras` mostantól az
aktív csatornákon felengedi a kitartó pedált, és a nyilvántartás szerint
szóló minden hangra célzott Note Off-ot küld. Ezek NORMÁL üzenetek: egy
futó felvételben a hangoknak ott is ezen a ponton kell véget érniük.

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
  ideje. **Módosítva (2026-09-25)**: a szünet-küszöb végül NEM ehhez
  mér, hanem az új `MidiFelvevo.CsendKezdeteMs`-hez (attól számít, hogy
  semmi sem szól - billentyű és pedál sem): **Szünet-küszöb** =
  `ElteltIdoMs - CsendKezdeteMs`.
- `MidiFelvevo.RogzitettHosszMs` - az első és utolsó rögzített üzenet
  közt eltelt idő = **játékidő-küszöb**.

FONTOS: mindkét oldalnak UGYANARRÓL az óráról kell olvasnia - egy másik
időforrás (pl. `DateTime.Now`) keverése lassú, nehezen észrevehető
csúszást okozna.

### Az ideiglenes teszt-kód a 3. fázisban KIKERÜLT

A 2. fázisban a `btnMidiFelvetel` gomb egy IDEIGLENES, ide-oda kapcsoló
kezelőt kapott, hogy a felvevőt az ablak megépítése előtt is ki lehessen
próbálni valódi hardveren. A 3. fázisban ez a blokk törlődött; a helyén a
végleges "MIDI FELVÉTEL" szakasz áll a `MainForm.vb` végén. A
`MainForm.FelvetelJelentesenekKeszitese()` továbbra is ott van (a felvevő
ablak ezt hívja a mentés után).

---

- [x] **3. fázis - `MidiFelvevoForm` váz + kézi felvétel.** *(Kész és
  a felhasználó által tesztelve, 2026-09-24 - kiadva az 1.18.0
  verzióban.)* Az ablak, a
  "Felvétel" és "Mentés" csoport, a felfegyverzés-logika, a színes
  állapotjelzés (nyugalmi / sárga "Várakozás billentyűre..." / piros
  "● FELVÉTEL"), a meglévő `btnMidiFelvetel` bekötése (az ideiglenes
  teszt-blokk törlésével együtt), a `MainForm_FormClosing` kiegészítése
  és a **"Jelentés készítése a felvétel mellé" jelölőnégyzet**.

  **A felhasználó által 2026-09-24-én elfogadott kiegészítések:**
  - futó idő az állapotjelzőben ("● FELVÉTEL 0:42"), a `MidiEngine`
    órája szerint (új: `MidiFelvevo.KezdoIdobelyegMs`);
  - a főablak gombja is mutatja az állapotot (sárga / piros), és
    megnyitja / előhozza a felvevő ablakot ("MIDI felvétel...");
  - **F9** = Start / Stop, a felvevő ablakban ÉS a főablakban is (ha az
    ablak még nincs nyitva, az F9 megnyitja);
  - "Utoljára mentve" sor + "Mappa megnyitása" gomb (az Intézőben a
    mentett fájlt kijelölve - az %AppData% alapból rejtett);
  - a fájlnév alapértéke a betöltött preset neve (a `Kezdet.json`
    esetén "Kezdet"; a "Felvetel" csak akkor, ha nincs név), és nyitott
    ablaknál preset-betöltéskor is frissül, ha nem írtál bele kézzel;
  - nem modális ablak, a főablak a tulajdonosa, és a főablak mellé
    (jobbra) nyílik, ha ott elfér;
  - bezárás felvétel közben: Igen = mentés, Nem = eldobás, Mégse =
    vissza - a felvevő ablaknál ÉS a főablaknál is;
  - Stop, mielőtt egyetlen hang szólt volna: csendben visszaáll,
    üzenetablak nélkül.

  **Tervezési döntés - ki birtokolja a felvevőt?** A `MidiFelvevo`
  példány a FŐABLAKÉ, nem a felvevő ablaké, és a kimeneti csapra is a
  főablak iratkozik fel. Oka a bezárás: a WinForms a főablak bezárásakor
  a hozzá tartozó ablakokat is bezárja, és a sorrendre nem jó építeni. A
  `MidiFelvevoForm.BezarasElottiKezeles()` ezért IDEMPOTENS - a felvevő
  ablak és a főablak is hívja a saját bezárásakor, és bármelyik jön előbb,
  a felhasználó pontosan egyszer kap kérdést. A `MainForm_FormClosing` az
  elején megnézi az `e.Cancel`-t is: ha a felvevő ablak már megszakította
  a kilépést, semmihez nem nyúl.

  Az "Automatikus felvétel" sor a helyén van, de letiltva - a 4. fázis
  kapcsolja be.

  **Ellenőrzés**: a `MidiFelvevoForm` (kód + Designer) és a `MainForm` új
  része egy szűk WinForms-csonkkal Linuxon, `Option Strict On` mellett
  lefordult, és a felvevő ablak logikája a VALÓDI felvevővel együtt
  lefutott (F9 felfegyverez; hang nélküli Stop csendben visszaáll; futó
  idő; mentés + jelentés; bezárás nyugalmi és felfegyverzett állapotban).
  Amit ott NEM lehetett kipróbálni: a valódi megjelenést, a bezárás
  kérdését felvétel közben, és a két ablak bezárási sorrendjét.

- [x] **4. fázis - Automatikus felvétel mód.** *(Kész és a felhasználó
  által tesztelve, 2026-09-25 - kiadva az 1.20.0 verzióban. A
  megvalósítás a lenti teendőlistát követi; részletek: `CHANGELOG.md`
  1.20.0.)* A két küszöb
  (játékidő/szünet), a szegmensekre bontás, az automatikus
  újra-felfegyverzés, és hogy a kézi Stop kiveszi a pipát az
  "Automatikus felvétel" checkboxból.

  **Részletes teendőlista (2026-09-25, a kód átnézése után):**

  *A. A felvevő motor bővítése (`MidiFelvevo.vb`, felület nélkül)*
  1. A kitartó pedál követése csatornánként (CC64 >= 64 = lenyomva). A
     felfegyverzés alatt lenyomott pedál is lenyomottként induljon (az
     a fájl elejére is bekerül).
  2. "Szól-e valami?": van-e lenyomott billentyű (a meglévő `hangSzol`
     nyilvántartás) VAGY lenyomott pedál bármelyik csatornán.
  3. Új `CsendKezdeteMs`: annak az eseménynek az ideje, amely után már
     semmi sem szól (utolsó elengedés / pedál felengedése); ha valami
     szól, nincs csend. Szünet = `ElteltIdoMs - CsendKezdeteMs`.
  4. A szegmens vége: NINCS teendő - a mentett fájl már most is az
     utolsó rögzített eseménynél ér véget (a csend nem kerül bele).
     Teszttel ellenőrizendő.

  *B. A szegmentáló döntés (új, felület nélküli osztály)*
  5. `Modellek/AutomatikusSzegmentalo.vb`, `Dontes(mostMs)` -> semmi /
     eldobás / mentés: nincs csend, vagy rövidebb a szünet-küszöbnél ->
     semmi; letelt, és a játékidő (`RogzitettHosszMs`) kisebb a
     játékidő-küszöbnél -> eldobás; letelt, és elérte -> mentés.
  6. Pontos vágás: a döntés MINDEN beérkező leütés ELŐTT is lefut (nem
     csak a 250 ms-os időzítőből) - ha a küszöb már letelt, előbb a régi
     szegmens zárul le, és az új hang már az új szegmenst indítja.

  *C. Bekötés*
  7. Az automatikus mód állapota a FŐABLAKBAN él (az ablak bezárása és
     újranyitása között is megmarad); programindításkor bekapcsolt.
  8. Megnyitáskor felfegyverzés, ha az automatikus mód be van kapcsolva
     (az indításkori automatikus újranyitásnál is).
  9. Mentés -> a meglévő mentés (jelentéssel), majd azonnali
     újra-felfegyverzés; eldobás -> csendben, majd újra-felfegyverzés.
  10. Kézi Stop automatikus módban: felvétel közben MINDIG ment (a
      küszöböktől függetlenül), felfegyverezve csak leáll; mindkét
      esetben kiveszi a pipát, nincs újra-felfegyverzés.
  11. Mentési hiba automatikus módban: EGY üzenetablak, az automatikus
      mód kikapcsol, a szegmens megmarad (Stop-pal újra menthető).
  12. Menet közben: a pipa kivétele felvétel közben kézi módra vált (a
      futó szegmenst csak a Stop zárja le); a pipa visszatétele
      nyugalmi állapotban AZONNAL felfegyverez (lásd a döntéseket); a
      küszöbök módosítása azonnal érvényes.
  13. A jelölőnégyzet és a két számmező engedélyezése, a "(a 4.
      fázisban kapcsolódik be)" felirat törlése.

  *D. Kijelzés*
  14. "Várakozás billentyűre... (AUTO)", "● AUTO FELVÉTEL 0:42", és egy
      ✓, ha a szegmens elérte a játékidő-küszöböt.
  15. "N szegmens mentve" számláló; az "Utoljára mentve" a legutóbbi
      fájlt mutatja.
  16. A főablak gombja is jelzi az automatikus módot.

  *E. Ellenőrzés*
  17. Python-port (szegmentáló + csend-követés) forgatókönyvekkel:
      rövid próbálkozás, hosszú játék, kitartott akkord, pedálos
      lecsengés, több szegmens, a küszöb határán érkező hang, kézi Stop,
      a pipa kivétele / visszatétele.
  18. Mutációs tesztek (pl. a pedál figyelmen kívül hagyása, csend
      lenyomott billentyű mellett, rossz irányú összehasonlítás, a
      határon érkező hang rossz szegmensben).
  19. A valódi VB kód Linuxon, szimulált órával, bájtra azonos kimenet
      a Python-porttal.
  20. Ablak-szintű próba WinForms-csonkkal.
  21. A felhasználó tesztje valódi billentyűzettel.

  *F. Lezárás*
  22. Dokumentáció + **1.20.0** (a felhasználó jóváhagyásával).

  **A felhasználó döntései (2026-09-24):**
  - **Csend = semmi nem szól.** A szünet-küszöb számlálása csak akkor
    fut, ha EGYETLEN hang sem szól (minden billentyű felengedve) ÉS a
    kitartó pedál is fel van engedve; az utolsó elengedéstől számít.
    Így egy hosszan kitartott akkord vagy pedálos lecsengés közepén nem
    vág a program.
  - **Az automatikus mód alapállapota.** *(MÓDOSÍTVA 2026-09-25, a
    felhasználó kérésére.)* A jelölőnégyzet állapota a `MidiBill3.ini`-be
    kerül (`[Felvevo] Automatikus=1/0`), és a program az utoljára
    elmentett értékkel indul; CSAK ha még nincs mentett érték, akkor
    bekapcsolt. Mindig a jelölőnégyzet tényleges állapota mentődik - a
    Stop miatt kikerült pipa is (a felhasználó döntése). *(Korábban,
    2026-09-24: minden programindításkor bekapcsolt, és nem került az
    ini-be.)* Felvétel (automatikus is) csak attól kezdve történhet,
    hogy a felvevő ablak nyitva van - amíg a felhasználó nem kattint a
    "MIDI felvétel..." gombra (vagy nem nyom F9-et), addig semmi nem
    rögzül.
  - **Az ablak megnyitása felfegyverez** (a felhasználó döntése,
    2026-09-24). Ha az "Automatikus felvétel" be van kapcsolva, a
    felvevő ablak megnyitása (a "MIDI felvétel..." gomb vagy F9)
    AZONNAL felfegyverzi a felvevőt - a Start gombot nem kell
    megnyomni, az első leütés indítja a felvételt. Ez az alapértelmezés,
    de kézzel kikapcsolható: ha a felhasználó kiveszi a pipát, a
    következő megnyitás már NEM fegyverez fel (kézi mód: Start / F9).
    A megnyitás előtt, vagyis amíg az ablak nincs nyitva, semmi nem
    rögzül.
  - **A felvevő beállításai megmaradnak** - de NEM a `Kezdet.json`-ban,
    hanem a `MidiBill3.ini`-ben (módosítva 2026-09-24: a `Beallitasok`
    osztályt minden preset mentése is használja, így a felvevő
    beállításai minden preset fájlba bekerültek volna). Megmarad: dátum
    szerinti almappa, jelentés készítése és a két küszöb értéke, és
    2026-09-25-től az "Automatikus felvétel" jelölőnégyzet is (lásd
    fent). (A
    fájlnév-mező nem kerül be: az a betöltött preset nevét követi, egy
    elmentett érték ezzel ütközne.) **Elkészült** a 4. fázis előtti
    önálló lépésben, az 1.19.0 verzióban (az ablakpozíciók mentésével
    együtt, lásd `CHANGELOG.md`) - a 4. fázisnak már csak használnia
    kell.
  - **Beragadt hang: NINCS külön védelem** (a felhasználó döntése,
    2026-09-25). Ha a billentyűzet nem küld Note Off-ot, a program úgy
    látja, hogy a hang még szól, így a szegmens magától nem zárul le -
    a felhasználó ezt füllel hallja, és kézzel (Stop) lezárja.
  - **A pipa visszatétele AZONNAL felfegyverez** (a felhasználó
    döntése, 2026-09-25). Ha az "Automatikus felvétel" be van pipálva,
    nem kell külön Startot nyomni - ettől automatikus. Tehát ha nyitott
    ablaknál, nyugalmi állapotban visszateszi a pipát, a felvevő rögtön
    felfegyverez, és az első leütés indítja a felvételt.
  - **A pipa kivétele felfegyverzett állapotban LESZEREL** (a
    felhasználó döntése, 2026-09-25). Felvétel közben viszont a futó
    felvétel kézi módban megy tovább, csak a Stop zárja le - egy
    véletlen kattintás így nem vágja el a játékot.
  - **A szünet-küszöb alapértéke 3 mp** (a felhasználó kérése,
    2026-09-25 - az 5 mp túl sok volt). A játékidő-küszöb marad 5 mp. A
    már mentett ini-érték felülírja az alapértéket.

- [x] **5. fázis - Lejátszás.** *(Kész és a felhasználó által tesztelve,
  2026-09-26 - kiadva az 1.21.0 verzióban; részletek: `CHANGELOG.md`
  1.21.0.)*
  `.mid` beolvasása, lejátszó, közvetlen
  küldés a kimeneti csapra (`LejatszottUzenetKuldese`), zöld gomb aktív
  lejátszás közben, Stop.

  **Technikai pontosítások (2026-09-26):**
  - **Beolvasás SAJÁT kóddal, nem NAudio-val** (az eredeti terv
    módosítása): a meglévő `MidiFajlOlvaso` bővül egy eseménylista-
    függvénnyel (csatorna-üzenetek időrendben, ms-ban, a tempótérképpel
    átszámolva; SysEx és meta kimarad; SMPTE-időzítésű fájl érthető
    hibával). Így egy helyen van az SMF-értelmezés, ugyanúgy tűri a
    sérült fájlokat, és `mido`-val ellenőrizhető.
  - **A lejátszás SAJÁT HÁTTÉRSZÁLON fut** a `MidiEngine` stopperórájához
    igazítva - a WinForms időzítő kb. 15 ms-os felbontása és a UI szál
    terheltsége hallható csúszást okozna. Ehhez: a kimenet megnyitása /
    lezárása és a küldés kölcsönös kizárása (eszközváltás közben se
    ütközzenek), és a főablak a kimeneti eseményt a UI szálra adja
    vissza (a felvevő továbbra is egyetlen szálon dolgozik - a 7. fázis,
    az overdub miatt is fontos).

  **Lépések:**
  1. `MidiFajlOlvaso.EsemenyekBeolvasasa()` - eseménylista.
  2. Új `Modellek/MidiLejatszo.vb` (felület nélkül): indítás / Stop /
     fájl vége / állapot, háttérszálas időzítés, adott pontról indítás
     (a 6. fázis tekeréséhez).
  3. A lejátszó nyilvántartja az általa megszólaltatott hangokat: Stopnál
     CSAK ezeket némítja (+ a pedált a lejátszott csatornákon).
  4. Kimenetváltás / -lezárás lejátszás közben: a lejátszás leáll.
  5. Felület a felvevő ablakban: "Lejátszás" csoport, három gomb egy
     sorban ("Fájl megnyitása...", "Lejátszás", "Stop"), a csoport címe
     "Lejátszás: <fájlnév>", a Lejátszás gomb lejátszás közben ZÖLD.
  6. "Fájl megnyitása...": a `Felvetelek` mappában nyílik, utána a
     legutóbb használt mappát jegyzi meg.
  7. "Mentett fájl betöltése a lejátszóba" jelölőnégyzet (lásd a
     döntéseket).
  8. A fájl végén magától leáll; az ablak / a program bezárása leállítja.
  9. Ellenőrzés: Python (az eseménylista a `mido` időzítésével
     összevetve, tempóváltásos fájlon is) + mutációs teszt; VB a
     lejátszóval, szimulált órával és rögzített küldéssel (sorrend,
     időzítés, Stop utáni némítás, az élő hangok érintetlenek); végül a
     felhasználó tesztje valódi szintetizátorral.
  10. Dokumentáció + verzió (a felhasználó jóváhagyásával).

  **A felhasználó döntései (2026-09-26):**
  - **A lejátszott fájl beállításai - a felhasználó választja**
    (2026-09-26; az aznapi első döntést - "a végén sem áll vissza" -
    ez váltotta fel). Új jelölőnégyzet a Lejátszás csoportban: **"A
    lejátszott fájl beállításainak megtartása"**, alapból KIKAPCSOLVA,
    az állapota a `MidiBill3.ini`-be kerül (mentett érték híján
    kikapcsolt). Lejátszás közben nem állítható - az indításkor dől el,
    mi történik:
    - **Kikapcsolva**: a lejátszás a fájl beállításaival szól, Stopnál
      és a fájl végén pedig a program VISSZAKÜLDI a panelek beállításait
      a szintetizátorra - az élő játék utána ugyanúgy szól, mint előtte.
    - **Bekapcsolva**: a lejátszás INDÍTÁSAKOR a felület átáll a fájl
      szerint, PONTOSAN a "Preset készítése MIDI fájlból" logikájával
      (`MidiPresetKeszito`), mintha egy abból készült presetet töltenél
      be: a fájlban hangot játszó csatornák aktívak a fájl kezdő
      hangszínével, hangerejével, terével, vibratójával és chorusával; a
      többi csatorna inaktív; split pont C4, transzponálás 0, alap
      hangerő 100%, a reverb a fájl legerősebb értéke; a hangszerlista és
      a MIDI eszközök maradnak. A felhasználó saját beállításai tehát
      felülíródnak (a felhasználó döntése). Stopnál / a fájl végén nincs
      visszaállítás.
    - Mindkét esetben a panelek kiküldése (a visszaállítás, illetve az
      átállás) BELSŐ üzenetként megy ki, így egy futó felvételbe nem
      kerül bele.
  - **Stop: CSAK a lejátszás némul** (a lejátszó által megszólaltatott
    hangok + a pedál a lejátszott csatornákon); az élő játék szól tovább.
  - **"Mentett fájl betöltése a lejátszóba"** jelölőnégyzet (az eredeti
    "Mentett fájl automatikus lejátszása" név helyett - ez csak betölti,
    nem indítja el): a Mentés csoport alján, alapból BEKAPCSOLVA, az
    állapota a `MidiBill3.ini`-be kerül. Minden mentés után (kézi Stop és
    automatikus szegmens is) a friss fájl betöltődik a lejátszóba.
  - **F8 = Lejátszás / Stop** (a felvevő ablakban és a főablakban is). Az
    F10 azért nem, mert a Windows a menüsornak foglalja.

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
pontossága. 2026-09-23-tól a felvevő ÁLLAPOTGÉPÉT is portolja és
ellenőrzi (lásd a 10. döntést): indítás csak az első leütésre, a szűrők
sorrendje, a pillanatkép időpontja, az előadói vezérlők helye és a
"lógó" hangok lezárása. Futtatás: `pip install mido`, majd
`python smf_iro_ellenorzes.py`.

**FONTOS**: ez a VB-kód PORTJA, nem a VB futtatása - ha a
`MidiFelvevo.vb` SMF-író része változik, a szkriptet is frissíteni kell.

A hibafelismerő képessége igazolt: a szkript végén **33 szándékosan
elrontott változat** fut le, és mindegyiknek meg KELL buknia (VLQ
folytatásbit, Program Change adatbájtjai, trackszám a fejlécben, a
kezdőállapot rossz trackben, tracknév a track sorszámával, kimaradó
bank-visszaesés, dobnév a rossz listából, hiányzó csatorna-előtag,
hosszú ékezetek Latin-1 cseréje, delta-nkénti kerekítés, hangnem akkor
is beírva, amikor nincs megadva, a b-s előjegyzés előjel nélkül, a
régi "amelyik nem nulla, az a bank" névfeloldás, és kilenc
állapotgép-hiba: indítás bármilyen üzenetre, 0 sebességű Note On-ra,
metronómra/visszajátszásra, pillanatkép felfegyverzéskor, kimaradó,
rossz helyre kerülő vagy túl sok megjegyzett vezérlő, lezáratlan hang,
rögzülő belső üzenet, és a belső elnémítás, ami nem zárja le a hangokat;
2026-09-25-től tíz automatikus-mód hiba is: a pedál figyelmen kívül
hagyása, a felfegyverzés alatti pedál elvesztése, csend egy felengedett
hangtól, miközben más még szól, a csend újraindulása egy csúszkától,
"meghaladta" az "elérte" helyett, fordított játékidő-döntés, a hiányzó
üzenet előtti ellenőrzés, a hiányzó újra-felfegyverzés, a pipa
kivétele, ami nem szerel le, és a rövid
felvételt eldobó kézi Stop).

**Az automatikus mód a VALÓDI VB kódon is ellenőrizve (2026-09-25)**: a
`MidiFelvevoForm` + `MidiFelvevo` + `AutomatikusSzegmentalo` Linuxon,
WinForms-csonkkal és szimulált órával lefuttatva ugyanarra a
forgatókönyvre bájtra azonos fájlokat ment, mint a Python-port (6 mentett,
1 eldobott szegmens). A VB oldalon az üzenet előtti ellenőrzést, illetve
a pedálfigyelést kikapcsolva a próba elbukik.

**A port hűsége ellenőrizve (2026-09-23)**: a `Modellek` fájljait (a
NAudio-függő `MidiEngine` kivételével) Linuxon, .NET 8 alatt, `Option
Strict On` mellett lefordítottuk, és a VALÓDI VB `MidiFelvevo`-t
lefuttattuk ugyanazokon a forgatókönyveken - a kapott `.mid` fájlok
**bájtra azonosak** a Python-port kimenetével (mindkét hangnem-esetben
és az állapotgép-forgatókönyvön is). Ugyanez a próba a régi, módosítatlan
`MidiFelvevo.vb`-n elbukik: ott már egy felfegyverzés utáni hangerő-
csúszka elindította a felvételt. A `MidiEngine`-t egy NAudio-csonkkal
szintén lefordítottuk, és a VALÓDI motort a valódi felvevővel együtt,
a `MainForm`-mal azonos bekötéssel is lefuttattuk (bemenetváltás
lenyomott billentyűvel, kimenetváltás felvétel közben, pánikgomb).
Ha egy elrontott változat ÁTMEGY, a teszt nem képes észrevenni azt a
hibafajtát, és bővíteni kell.

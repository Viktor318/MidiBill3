# MIDI felvevő ablak - végigtesztelési lista (8. fázis)

**Verzió**: 1.23.1 alapján, 2026-11-04. **Cél**: a teljes felvevő ablak
(`MidiFelvevoForm`) végigpróbálása valódi billentyűzettel és
szintetizátorral, a 2.0.0 kiadás előtt.

**Használat**: minden pont mellé `[x]` = rendben, vagy `[!]` + egy rövid
megjegyzés, ha valami nem az elvárt módon működik. A hibákat a lap
végén lévő "Talált hibák" részbe is érdemes felírni (pont-azonosítóval),
onnan javítjuk őket. Ahol egy pontnál "(figyeld meg)" áll, ott a
viselkedés nincs előre rögzítve - csak jegyezd fel, mit tapasztalsz.

A metronóm NEM része ennek a körnek.

---

## 0. Előkészületek

- [ ] **0.1** Biztonsági másolat: `%AppData%\MidiBill3\MidiBill3.ini` és
  `Kezdet.json` (a teszt közben többször is módosulnak).
- [ ] **0.2** `HibaNaplo.txt` kiürítése vagy átnevezése - a végén (K.1)
  meg kell nézni, került-e bele valami.
- [ ] **0.3** Eszközök: billentyűzet **kitartó pedállal** és
  **modulációs kerékkel**, a szintetizátor (VirtualMIDISynth), és ha van,
  egy második MIDI kimenet (a kimenetváltós pontokhoz).
- [ ] **0.4** Tesztfájlok a lejátszáshoz: (a) egy saját felvétel,
  (b) egy külső, több csatornás GM fájl tempóváltással, (c) egy format 0
  fájl. Opcionálisan egy DAW (pl. Cantabile) a fájlok egymásra rakásához.
- [ ] **0.5** Egy preset legalább két aktív csatornával (pl. zongora +
  vonós splittel), hogy a csatornánkénti trackek is látszódjanak.

---

## A. Az ablak megnyitása és alapállapota

- [ ] **A.1** Első megnyitás (ini nélkül vagy törölt `[FelvevoAblak]`
  szakasszal) a "MIDI felvétel..." gombbal. *Elvárt*: a főablak mellé,
  jobbra nyílik (ha elfér); nem modális - a főablakon tovább lehet
  játszani és állítani.
- [ ] **A.2** A fájlnév mező a betöltött preset nevét mutatja; friss
  indulásnál / alaphelyzet után "Kezdet".
- [ ] **A.3** Nyitott ablaknál másik preset betöltése. *Elvárt*: a
  fájlnév mező átveszi az új nevet.
- [ ] **A.4** Írj saját nevet a mezőbe, majd tölts be másik presetet.
  *Elvárt*: a saját név marad.
- [ ] **A.5** Alapértékek ini nélkül: "Automatikus felvétel" BE, dátum
  szerinti almappa BE, "Jelentés készítése a felvétel mellé" BE,
  "Mentett fájl betöltése a lejátszóba" BE, játékidő-küszöb 5 mp,
  szünet-küszöb 3 mp, "A lejátszott fájl beállításainak megtartása" KI,
  "A lejátszott hangok rögzítése" KI, "Felvétel indításakor a lejátszás
  is induljon" KI.
- [ ] **A.6** Automatikus módban a megnyitás **azonnal felfegyverez**
  (sárga "Várakozás billentyűre... (AUTO)"), a főablak gombja is sárga.
- [ ] **A.7** Vedd ki az automatikus pipát, zárd be és nyisd újra az
  ablakot. *Elvárt*: nyugalmi állapotban nyílik, nem fegyverez fel.
- [ ] **A.8** **F9 a főablakban**, zárt felvevő ablaknál:
  - kézi módban megnyitja az ablakot és felfegyverez (figyeld meg);
  - automatikus módban csak megnyitja (az már felfegyverez) - nem állítja
    le rögtön.
- [ ] **A.9** **F8 a főablakban**, zárt felvevő ablaknál: megnyitja az
  ablakot, és ha van betöltött fájl, elindítja a lejátszást (figyeld meg,
  mi történik betöltött fájl nélkül).
- [ ] **A.10** Nyitott, háttérbe került felvevő ablaknál a "MIDI
  felvétel..." gomb előhozza az ablakot (nem nyit másodikat).

---

## B. Kézi felvétel (az automatikus pipa KIVÉVE)

- [ ] **B.1** Start (vagy F9 a felvevő ablakban). *Elvárt*: sárga
  "Várakozás billentyűre...", a főablak gombja sárga.
- [ ] **B.2** Felfegyverzett állapotban mozgass csúszkát, válts
  hangszínt, tölts be presetet. *Elvárt*: a felvétel NEM indul el.
- [ ] **B.3** Az első leütés. *Elvárt*: piros "● FELVÉTEL 0:00", az idő
  folyamatosan nő; a főablak gombja piros.
- [ ] **B.4** Stop (F9). *Elvárt*: mentés, az "Utoljára mentve" sor a
  friss fájlt mutatja, visszaáll nyugalmi állapotba, a gombok színe
  alapszínű.
- [ ] **B.5** Start, majd Stop egyetlen hang nélkül. *Elvárt*: csendben
  visszaáll, nincs üzenetablak, nincs mentett fájl.
- [ ] **B.6** Stop **lenyomva tartott billentyűvel**. *Elvárt*: a mentett
  fájlban a hang a Stop pillanatában véget ér (nem szól a végtelenségig -
  lejátszással ellenőrizhető).
- [ ] **B.7** F9 a főablakban nyitott felvevő ablaknál is Start / Stop.
- [ ] **B.8** Kézi módban a felvétel hosszú csend után sem vág és nem
  áll le magától (pl. 10 mp szünet a játékban).

---

## C. Mi kerül a felvételbe

*(Ellenőrzés: a mentett fájl lejátszása a programban, a mellé készült
`.txt` jelentés, vagy egy DAW.)*

- [ ] **C.1** A hangszínek a **felvétel indulásakor** érvényes beállítás
  szerint szólnak (nem alapértelmezett zongorával) - külső lejátszóban
  is.
- [ ] **C.2** Felfegyverzés után, de az első leütés ELŐTT átállított
  hangszín. *Elvárt*: a fájl már az új hangszínnel kezdődik.
- [ ] **C.3** Felvétel közbeni hangszerváltás. *Elvárt*: a fájl eleje a
  régi, a váltás után a rész az új hangszínnel szól.
- [ ] **C.4** **Az első hang előtt lenyomott pedál**: pedál le, majd
  játék. *Elvárt*: a fájlban az első hangok is pedállal szólnak.
- [ ] **C.5** Modulációs kerék az első hang előtt felhúzva. *Elvárt*: a
  fájl elején a ténylegesen kiküldött érték szerepel.
- [ ] **C.6** Felvétel közben **aktívvá tett csatorna**. *Elvárt*: a
  hangjai a saját beállításaival (hangszín, hangerő stb.) szólnak.
- [ ] **C.7** Felvétel közben a **"Csend legyen!"** gomb. *Elvárt*: a
  pánik rögzül (a fájlban is ott némul el minden).
- [ ] **C.8** **Kimeneti eszköz váltása** felvétel közben (ha van második
  kimenet), lenyomott hanggal. *Elvárt*: a fájlba NEM kerül 16 csatornás
  söprés / újraküldés (csak a játszott csatornák trackjei vannak meg), és
  a szóló hang a váltás pillanatában véget ér.
- [ ] **C.9** Felfegyverzett állapotban kimenetváltás. *Elvárt*: nem
  indítja el a felvételt.
- [ ] **C.10** **Bemeneti eszköz váltása** lenyomott billentyűvel (és
  pedállal). *Elvárt*: a hang nem ragad be, a fájlban is ott ér véget.
- [ ] **C.11** A fájl szerkezete (jelentés vagy DAW): format 1; a 0.
  track címe "MidiBill3 felvétel", 120 BPM, 4/4, hangnem NINCS;
  csatornánként egy-egy track **"NN - Hangszernév"** névvel, ahol NN a
  csatornaszám; néma (aktív, de nem játszott) csatornára nincs track.
- [ ] **C.12** 10. (dob) csatornán felvett anyag: a tracknév a
  dobkészlet nevét mutatja.
- [ ] **C.13** Split + transzponálás + oktáv-eltolás mellett felvett
  fájl: a lejátszás ugyanazt adja vissza, amit hallottál (a feldolgozott
  kimenet rögzül, nem a nyers bemenet).

---

## D. Mentés, fájlnév, jelentés

- [ ] **D.1** Dátum szerinti almappa BE: `Felvetelek\ÉÉÉÉ-HH-NN\<név>_óóppmm.mid`.
- [ ] **D.2** Dátum szerinti almappa KI: `Felvetelek\<név>_ÉÉÉÉ-HH-NN_óóppmm.mid`.
- [ ] **D.3** A mentési hely előnézete a beállítás váltásakor azonnal
  frissül.
- [ ] **D.4** Tiltott karakterek a fájlnévben (pl. `a/b:c?`). *Elvárt*: a
  név megtisztítva kerül a fájlba, nincs hiba.
- [ ] **D.5** Üres fájlnév mező. *Elvárt*: értelmes alapnév (figyeld meg,
  mi lesz).
- [ ] **D.6** "Jelentés készítése" BE: a `.mid` mellett azonos nevű
  `.txt` jelentés; a fejlécben **"Preset fájl"** sor, a hangszerlista a
  betöltött.
- [ ] **D.7** "Jelentés készítése" KI: csak `.mid` készül.
- [ ] **D.8** "Mappa megnyitása": az Intéző a mentett fájlt kijelölve
  nyílik meg.
- [ ] **D.9** "Mentett fájl betöltése a lejátszóba" BE: mentés után a
  friss fájl a lejátszóban van (a csoport címe "Lejátszás: <fájlnév>"),
  de NEM indul el magától.
- [ ] **D.10** Ugyanez KI: a lejátszóban a korábbi fájl marad.

---

## E. Automatikus felvétel

*(Alapértékek: játékidő-küszöb 5 mp, szünet-küszöb 3 mp.)*

- [ ] **E.1** Rövid próbálkozás (2-3 mp játék), utána csend. *Elvárt*: 3
  mp után eldobás (nincs fájl), azonnal újra felfegyverez.
- [ ] **E.2** Hosszabb játék (>5 mp), utána csend. *Elvárt*: a játékidő
  elérésekor megjelenik a ✓; 3 mp csend után mentés (+ jelentés),
  "(1 szegmens mentve)", azonnali újra-felfegyverzés.
- [ ] **E.3** A mentett szegmens az utolsó eseménynél ér véget - a 3 mp
  csend nincs a fájl végén.
- [ ] **E.4** **Kitartott akkord** (billentyűk lenyomva >3 mp). *Elvárt*:
  nem vág.
- [ ] **E.5** **Pedálos lecsengés**: elengedett billentyűk, lenyomott
  pedál >3 mp. *Elvárt*: nem vág; a szünet a pedál felengedésétől
  számít.
- [ ] **E.6** Csend alatt csúszka mozgatása / moduláció. *Elvárt*: a
  szünet-számlálás nem indul újra.
- [ ] **E.7** Több szegmens egymás után: külön fájlok, a számláló nő, az
  "Utoljára mentve" a legutóbbit mutatja; két szegmens ugyanabban a
  másodpercben sem írja felül egymást.
- [ ] **E.8** A küszöb letelte után azonnal érkező hang: az már az ÚJ
  szegmens elején van, nem az előző végén.
- [ ] **E.9** **Kézi Stop felvétel közben** (rövid, <5 mp játék).
  *Elvárt*: MINDIG ment, kiveszi az automatikus pipát, nincs
  újra-felfegyverzés.
- [ ] **E.10** Kézi Stop felfegyverzett állapotban: leáll, kiveszi a
  pipát, nincs fájl.
- [ ] **E.11** Pipa kivétele **felfegyverzett** állapotban: leszerel
  (nyugalmi állapot).
- [ ] **E.12** Pipa kivétele **felvétel közben**: a felvétel kézi módban
  megy tovább, hosszú csend után sem vág; csak a Stop zárja le.
- [ ] **E.13** Pipa visszatétele nyugalmi állapotban: **azonnal**
  felfegyverez.
- [ ] **E.14** Küszöbök módosítása menet közben (pl. szünet 3 -> 6 mp):
  azonnal érvényes.
- [ ] **E.15** Kijelzés: "● AUTO FELVÉTEL 0:42", a főablak gombja is jelzi
  az automatikus módot.
- [ ] **E.16** Beragadt hang (ha előidézhető): a szegmens magától nem
  zárul le, a Stop lezárja (elfogadott viselkedés).
- [ ] **E.17** *(Opcionális)* Mentési hiba automatikus módban - pl. a
  `Felvetelek` mappára írásmegtagadás (Tulajdonságok -> Biztonság).
  *Elvárt*: egyetlen üzenetablak, az automatikus mód kikapcsol, a szegmens
  megmarad, a jog visszaadása után Stop-pal elmenthető.

---

## F. Lejátszás

- [ ] **F.1** "Fájl megnyitása..." először a `Felvetelek` mappában nyílik,
  egy másik mappából választás után legközelebb ott.
- [ ] **F.2** Lejátszás (F8): a gomb **zöld**, a fájl végén magától leáll
  és visszavált alapszínre.
- [ ] **F.3** Stop (F8) lejátszás közben, miközben élőben is játszol.
  *Elvárt*: csak a lejátszás némul el (a pedálja is felenged), az élő
  hangok szólnak tovább.
- [ ] **F.4** Külső GM fájl tempóváltással (0.4 b): időben helyesen szól,
  minden csatornán (a főablakban inaktív csatornákon is).
- [ ] **F.5** Format 0 fájl (0.4 c) lejátszása.
- [ ] **F.6** "A lejátszott fájl beállításainak megtartása" **KI**: a
  fájl hangszíneivel szól, Stop és fájlvég után az élő játék ismét a
  panelek hangszíneivel szól.
- [ ] **F.7** Ugyanez **BE**: indításkor a felület átáll a fájl szerint
  (a szóló csatornák aktívak, a többi inaktív, split C4, transzponálás 0,
  alap hangerő 100%); Stop után nincs visszaállítás.
- [ ] **F.8** A fenti jelölőnégyzet lejátszás közben nem állítható.
- [ ] **F.9** Kimeneti eszköz váltása lejátszás közben: a lejátszás leáll,
  nem marad beragadt hang.
- [ ] **F.10** A felvevő ablak bezárása lejátszás közben: a lejátszás
  leáll.
- [ ] **F.11** Hibás vagy nem MIDI fájl megnyitása: érthető hibaüzenet,
  a program nem száll el.
- [ ] **F.12** A lejátszás időzítése hosszabb fájlon (2-3 perc) - nem
  csúszik, nem akad (fülre).

---

## G. Tekerés és időkijelzés

- [ ] **G.1** Az időkijelzés "1:22 / 3:41" alakú, lejátszás közben
  folyamatosan frissül.
- [ ] **G.2** Kattintás a csúszkára: a kattintás helyére ugrik (nem
  lapoz).
- [ ] **G.3** Húzás lejátszás közben: a kijelzés a célpontot mutatja, a
  lejátszás nem rántja vissza a csúszkát; felengedéskor onnan szól.
- [ ] **G.4** ← / → = 5 mp vissza / előre, Home = a fájl eleje
  (lejátszás közben és megállítva is).
- [ ] **G.5** A fájlnév mezőben és a küszöb-számmezőkben a nyilak NEM
  tekernek (a megszokott módon működnek).
- [ ] **G.6** Megállított állapotban a csúszka a kezdőpontot állítja, a
  Lejátszás onnan indul.
- [ ] **G.7** A Stop ott hagyja a pozíciót; F8 onnan folytatja.
- [ ] **G.8** A fájl vége után, illetve új fájl betöltésekor a pozíció a
  fájl elejére áll.
- [ ] **G.9** **Középről indítás**: a csatornák a helyes hangszínnel,
  hangerővel, pitch bend tartománnyal szólnak (próbáld olyan fájllal,
  amelyben menet közben hangszínváltás van).
- [ ] **G.10** Tekerés szóló hangok közben, miközben élőben is játszol:
  csak a lejátszás hangjai és pedálja engednek el, nincs beragadt hang,
  az élő hangok szólnak tovább.

---

## H. Ráfelvétel (overdub)

- [ ] **H.1** Lejátszás közben Start, élő játék, "A lejátszott hangok
  rögzítése" **KI**. *Elvárt*: a mentett fájlban csak az élő játék van.
- [ ] **H.2** **Időigazítás**: a H.1 felvételét lejátszva az első hang
  pontosan ott szólal meg, ahol a lejátszott fájlban járt (pl. ha 0:25-nél
  kezdtél játszani, a mentett fájlban is 0:25-nél) - a fájl eleje csend.
  DAW-ban a két fájl egymásra rakva együtt szól.
- [ ] **H.3** "A lejátszott hangok rögzítése" **BE**: a mentett fájlban
  az élő játék és a lejátszott anyag is benne van.
- [ ] **H.4** Ugyanez BE, felfegyverezve, élő játék nélkül indított
  lejátszással. *Elvárt*: a lejátszás első hangja elindítja a felvételt.
- [ ] **H.5** Ugyanez KI: a lejátszás nem indítja el a felfegyverzett
  felvételt.
- [ ] **H.6** A H.3 felvétele a fájl **közepéről** indított lejátszással:
  a mentett fájl az elejétől helyes hangszínekkel szól (a lejátszás
  csatorna-állapota a fájl elején).
- [ ] **H.7** A jelölőnégyzet felvétel közben átállítható, onnantól
  érvényes.
- [ ] **H.8** "Felvétel indításakor a lejátszás is induljon" BE, kézi
  mód: Start (F9) felfegyverez ÉS elindítja a lejátszást a csúszka
  pozíciójáról.
- [ ] **H.9** Ugyanez automatikus módban (már felfegyverezve): a Start
  csak a lejátszást indítja.
- [ ] **H.10** Ugyanez BE mellett az ablak megnyitása és az automatikus
  újra-felfegyverzés NEM indít lejátszást.
- [ ] **H.11** Automatikus mód lejátszás közben: amíg a lejátszás szól,
  a felvétel hosszú szünetek mellett sem vág; a szünet a lejátszás
  végétől (Stop vagy fájlvég) számít.
- [ ] **H.12** **Alapsáv megtartása (1.23.1)**: "Mentett fájl betöltése a
  lejátszóba" BE, rögzítés KI, felvétel lejátszás közben -> mentés után
  a lejátszóban az **alapsáv marad**; több próbálkozás egymás után
  felvehető ugyanarra (automatikus módban is, szegmensenként).
- [ ] **H.13** Rögzítés BE (rétegezés) mellett mentés után a **friss
  felvétel** töltődik be.
- [ ] **H.14** Lejátszás nélküli felvétel után a friss felvétel töltődik
  be.
- [ ] **H.15** Tekerés felvétel közben (figyeld meg): a felvétel nem
  szakad meg; az igazítás az indulási nullpontot tartja (ez elfogadott).

---

## I. Bezárás, kilépés, ini, újraindítás

- [ ] **I.1** A felvevő ablak bezárása **felvétel közben**: kérdés; Igen =
  mentés, Nem = eldobás, Mégse = vissza (a felvétel megy tovább).
- [ ] **I.2** A **főablak** bezárása felvétel közben: ugyanez a kérdés,
  **pontosan egyszer**; Mégse után a program teljesen működik tovább
  (MIDI bemenet is él - játssz utána!).
- [ ] **I.3** Bezárás felfegyverzett és nyugalmi állapotban: kérdés
  nélkül.
- [ ] **I.4** "Kilépés mentés nélkül" felvétel közben: kérdés, és az ini
  ekkor is mentődik.
- [ ] **I.5** Az ablakok pozíciója: mindkét ablak a legutóbbi helyén
  nyílik újraindítás után.
- [ ] **I.6** Kilépés **nyitott** felvevő ablakkal -> újraindításkor
  magától megnyílik (automatikus módban fel is fegyverez), a fókusz a
  főablakon marad. Kilépés **zárt** felvevő ablakkal -> nem nyílik meg.
- [ ] **I.7** Minden jelölőnégyzet és a két küszöb megmarad újraindítás
  után (a Stop miatt kikerült automatikus pipa is).
- [ ] **I.8** *(Ha van második monitor)* Az ablak a második monitoron,
  majd a monitor leválasztása után indítás: az ablak látható helyre
  kerül.
- [ ] **I.9** Kézzel elrontott ini (pl. az egyik küszöb értéke 999, vagy
  egy értelmetlen sor a `[Felvevo]` szakaszban): a program elindul, az
  érték tartományba szorul, nincs hibaüzenet.

---

## J. Vegyes és terhelés

- [ ] **J.1** Hosszú felvétel (10+ perc) sűrű játékkal: a mentett fájl
  időzítése a végén sem csúszik (a lejátszás a felvétel végén is együtt
  van az emlékezettel / a futó idővel).
- [ ] **J.2** Gyors futamok, trillák: nincs kimaradó vagy beragadt hang a
  lejátszásban.
- [ ] **J.3** "Csend legyen!" lejátszás közben (figyeld meg: a lejátszás
  tovább megy-e, ragad-e be hang).
- [ ] **J.4** "Alaphelyzetbe állítás" nyitott felvevő ablakkal: a
  fájlnév "Kezdet" lesz; a felvevő állapota nem sérül.
- [ ] **J.5** Az "Extra MIDI üzenetek" jelölőnégyzet felfegyverzett
  felvevő mellett: az első leütés mindkettőt kiváltja, a felvétel elején
  nincs duplikált/hibás beállítás (figyeld meg).

---

## K. Zárás

- [ ] **K.1** `HibaNaplo.txt`: nem került bele új bejegyzés a teszt alatt.
- [ ] **K.2** A 0.1-ben mentett ini / `Kezdet.json` visszaállítása, ha
  szükséges.

---

## Talált hibák

| Pont | Mit tapasztaltál | Mit vártál |
|---|---|---|
| | | |

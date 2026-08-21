# MidiBill3 (VB.NET) – Aktuális állapot összefoglalója

Ez a dokumentum a **VB6 → VB.NET portolás jelenlegi állapotát** foglalja
össze, hogy egy új beszélgetés gyorsan fel tudjon zárkózni anélkül, hogy
végig kellene olvasni a teljes fejlesztési történetet. A projekt **éles,
működő állapotban van**, aktívan továbbfejlesztés alatt.

---

## Alapadatok

- **Verzió**: 1.7.0 (SemVer), a **`MidiBill3.vbproj`-ban közvetlenül**
  (érdemes lehet léptetni a mai/legutóbbi hibajavítások után)
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
  alapértelmezett `True`
- **Címsor**: `"MIDI billentyűzet megszólaltatása 16 csatornán : <beállítások
  fájlneve>, <hangszerlista fájlneve>"` (`CimSorUjrarajzolas()`)
- **Névjegy**: 16 csatorna, "a 10. csatorna dedikált dobkészlet-listát
  használ" szöveggel

## Projektstruktúra

```
MidiBill3/                        - EGYETLEN projekt
├── MainForm.vb / .Designer.vb   - Fő ablak
├── HangszerPanel.vb / .Designer.vb - Egy hangszerpanel (UserControl), 4 fizikai
│                                   példány, a kiválasztott csatornacsoportot mutatja
├── ApplicationEvents.vb         - Globális hibanaplózás (HibaNaplo.txt)
├── Modellek/
│   ├── AppInfo.vb               - Verziószám lekérdezés
│   ├── Beallitasok.vb           - Adatmodell (16 CSATORNA, MIND szerkeszthető,
│   │                               beleértve a 10.-et is) + JSON mentés/betöltés
│   ├── MidiEngine.vb            - MIDI be-/kimenet, fan-out (16 csatorna,
│   │                               NINCS letiltott csatorna), split pont,
│   │                               transzponálás, Bank Select-alapú hangszerváltás
│   ├── HangszerLista.vb         - KÉT hangszerlista: Nevek (globális, 15 csatorna)
│   │                               és DobNevek (dedikált, 10. csatorna) -
│   │                               Betolt()/BetoltDobKeszletet()/Ment()
│   ├── SoundFontOlvaso.vb       - .sf2 "phdr" beolvasása: hangszerek (Nevek) ÉS
│   │                               dobkészlet-presetek (Dobok, ISMERT DOBKÉSZLET-
│   │                               BANKOK: 120 [GM2 szabvány] és 128 [SF2-
│   │                               szerkesztői konvenció]) külön
│   └── Hangnevek.vb             - MIDI hangszám -> hangnév konverzió
└── Adatok/                      - Preset fájlok + hangszerlista fájlok:
                                    GM.lst (hangszerek) + GM.drm (dobkészletek,
                                    PÁROSÍTVA az alapnév alapján), Teszt.lst,
                                    TesztGS.lst stb.
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
tárolás, régi `.mb3` import, `Kezdet.json` automatikus be-/kimentés.

**Segédprogramok beépítve a főprogramba**: "Segédprogramok" menü (Preset
fájlok frissítése, Régi .mb3 fájlok konvertálása).

**Bank Select-alapú kiterjesztett hangszertámogatás (GS/XG-kompatibilis)**:
`MidiEngine.ProgramValtas()` mindig küld Bank Select (CC0+CC32) + Program
Change-t, `.lst` formátum `bank.program=Név` szintaktikával, `HangszerPanel`
`Név (B<bank>)` megjelenítéssel. (Részletek: korábbi fejlesztési történet.)

**10. csatorna feloldása + dedikált dobkészlet-hangszerlista (.lst/.drm párosítás)**:
`HangszerLista.DobNevek` önálló Dictionary a 10. csatornához,
`HangszerPanel.HangszerListaForrasBeallitasa()` a panel forrás-cseréjéhez,
a `.drm` fájlnév mindig az aktív `.lst` alapnevéből származik és
automatikusan párosodik. A 10. csatorna teljesen szerkeszthető (aktiválás,
hangszerválasztás, Split zóna, Oktáv, mentés/betöltés mind normálisan
működik). (Részletek: korábbi fejlesztési történet.)

**SoundFont-import - dobkészlet-bank felismerés bővítve (ÚJ, ez a kör)**
- **A hiba, amit a felhasználó saját GS SoundFont-jával (`JJazzLab-
  SoundFont.sf2`) tesztelve talált**: a `SoundFontOlvaso` korábban CSAK a
  `Bank 128`-at (SoundFont-szerkesztői, pl. JJazzLab belső konvenciója)
  ismerte fel dobkészletként - a `Bank 120` (a **GM2 SZABVÁNY** szerinti,
  ténylegesen Bank Select-tel is elküldhető dobkészlet-bank, hiszen
  `≤127`) tévesen a hangszerlistába (`.lst`) került, pl. `"Std Kit 1-
  GM2"`, `"Room Kit-GM2"` stb. néven
- **Javítva**: új `ismertDobBankok() = {120, 128}` tömb - bővíthető, ha a
  jövőben további dobkészlet-bank-konvenciókkal találkoznánk. A
  `FeldolgozPhdr` mostantól `Array.IndexOf(ismertDobBankok, bank) >= 0`
  alapján dönt, nem csak `bank = 128`-at nézi
- Dokumentációs kommentek pontosítva (a korábbi, mostanra téves
  állítás - "a dobkészlet-presetek sosem küldhetők ki Bank Select-tel" -
  csak a Bank 128-ra igaz, a Bank 120-ra NEM, ez most már helyesen
  van dokumentálva)
- **Mellékes tesztelési tanulság (nem programhiba)**: egy csatorna néma
  maradása leggyakrabban a Hangerő=0 beállításra, az Aktív checkbox-ra,
  vagy a Split zóna (Alul/Felül) egyikének sem bepipálására vezethető
  vissza - érdemes ezt a hármat ellenőrizni elsőként "néma csatorna"
  esetén, mielőtt kódhibát feltételeznénk

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
- Dobkészlet-bankok bővíthető listája (`ismertDobBankok`), nem egyetlen
  fixen kódolt szám - felkészítve további konvenciókra

## Globális hibanaplózás

Az `ApplicationEvents.vb` elkap minden kezeletlen kivételt, `HibaNaplo.txt`-be
ír. Natív (nem managed) összeomlásokat ez sem tud elkapni.

## Következő lépések / nyitott témák

1. **GS-támogatás további tesztelése** - a felhasználó folytatja a saját
   GS SoundFont-jával való élesben tesztelést (hangszerek, dobkészletek)

2. **10. csatorna működésének további tesztelése** - konkrétan, hogy a
   Bank Select + Program Change mechanizmus a dobkészlet-választásnál
   minden esetben helyesen viselkedik-e a felhasználó szintetizátorán

3. **Új tervezett menüpont: "Beállítások törlése" / "Minden beállítás
   alaphelyzetbe"** - a Fájl menübe. MÉG NINCS részletesen megtervezve,
   tisztázandó kérdés lesz: mit jelentsen pontosan az "alaphelyzet"
   (visszaálljon-e a hangszerlista is GM.lst/GM.drm-re, vagy csak a
   csatorna-beállítások; törlődjön-e a Kezdet.json fájl is, vagy csak a
   memóriában lévő állapot álljon vissza)

4. **Metronóm-funkció** - a 10. csatorna feloldásának egyik indoka volt;
   konkrét ütős hangok (pl. Claves, Side Stick - fix GM percussion
   billentyű-kiosztás) Note On üzenetként való kiküldése, FÜGGETLENÜL a
   dobkészlet-választástól. MÉG NINCS megtervezve/megvalósítva

5. **Chorus (CC93)** - elvi terv kész, ugyanaz a minta, mint a Reverb.
   Még nincs megvalósítva

6. **MIDI felvétel/visszajátszás** - elvi terv, eldöntött kulcskérdéssel:
   a NYERS BEMENETET vesszük fel, visszajátszáskor újra végigmegy a
   `MidiEngine.UzenetKuldes()`-en. Még nincs elkezdve

7. **Telepítő (Inno Setup)** - eldöntött irány, még nincs elkezdve

8. **Performancia - MIDI-küldés leválasztása a UI-szálról** - elvi téma,
   átbeszélve, eddig nem volt probléma, nem sürgős

9. Végigtesztelési checklist még nincs összeállítva

10. **Elméleti téma, lezárva**: iOS/iPadOS-portolás - távoli asztali
    elérés (RDP/Chrome Remote Desktop) a gyakorlati megoldás, ha a
    fizikai MIDI billentyűzet a Windows gépnél marad

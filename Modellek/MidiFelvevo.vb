Namespace Modellek

    ''' <summary>A MIDI felvevő állapotai.</summary>
    Public Enum FelvevoAllapot
        ''' <summary>Nyugalmi állapot - nem gyűjtünk semmit.</summary>
        Alap = 0
        ''' <summary>
        ''' "Felfegyverezve": a Start gomb megnyomása után várjuk az ELSŐ
        ''' LEÜTÉST (Note On, nullánál nagyobb sebességgel) - a felvétel csak
        ''' akkor indul el, ugyanúgy, mint az "Extra MIDI üzenetek"
        ''' jelölőnégyzetnél. Így a felvétel eleje nem lesz tele üres csenddel
        ''' amiatt, hogy a felhasználó a Start után még a hangszínt kereste
        ''' vagy a billentyűre várt.
        '''
        ''' A közben kiküldött többi üzenet (csúszkák, hangszerváltás,
        ''' preset-betöltés, eszközváltás) NEM indítja el a felvételt, és nem
        ''' is rögzül: ami ezekből számít, azt a rögzítés INDULÁSAKOR készülő
        ''' kezdőállapot-pillanatkép úgyis tartalmazza (lásd
        ''' MidiFelvevo.kezdoAllapotUzenetek). Kivétel a billentyűzet két
        ''' előadói vezérlője - lásd MidiFelvevo.eloadoiVezerlok.
        '''
        ''' Korábban (2026-09-23 előtt) BÁRMILYEN kiküldött üzenet elindította.
        ''' </summary>
        Felfegyverezve = 1
        ''' <summary>Aktív felvétel - minden ide illő üzenet rögzül.</summary>
        Rogzit = 2
    End Enum

    ''' <summary>
    ''' A MIDI felvevő "motorja": időbélyeggel gyűjti a KIKÜLDÖTT MIDI
    ''' üzeneteket, és szabványos Standard MIDI File (.mid) fájlba menti
    ''' őket. SZÁNDÉKOSAN nem tud semmit a felhasználói felületről - a
    ''' MidiFelvevoForm hívja a metódusait, és ő iratkozik fel a
    ''' MidiEngine.UzenetKikuldve eseményére, majd továbbadja ide az
    ''' üzeneteket. Így ez az osztály önmagában, ablak nélkül is tesztelhető.
    '''
    ''' MIÉRT A KIMENETET VESSZÜK FEL? A felvétel a TÉNYLEGES, már
    ''' feldolgozott hangot rögzíti: a split pont, a globális transzponálás,
    ''' a csatornánkénti oktáv-eltolás és a 16 csatornás szétosztás
    ''' (fan-out) UTÁNI állapotot - így a mentett fájl pontosan azt adja
    ''' vissza, ami ténylegesen megszólalt, és egy külső lejátszó/DAW is
    ''' ugyanazt hallja majd.
    '''
    ''' SZÁLKEZELÉS: minden MIDI-küldés a UI szálon történik (a bejövő
    ''' üzeneteket a MainForm BeginInvoke-kal marshalolja oda, a csúszkák/
    ''' menük/időzítők pedig eleve ott futnak), ezért a MidiEngine
    ''' UzenetKikuldve eseménye is mindig a UI szálról érkezik - ez az
    ''' osztály emiatt SZÁNDÉKOSAN nem zárol semmit. Az időbélyeg viszont
    ''' NEM a UI szálon készül: élő játéknál a NAudio callback-száláról
    ''' származik, még a UI-ra való átadás előttről (lásd MidiEngine.idozito),
    ''' tehát a felvétel időzítését nem rontja el a UI szál terheltsége.
    ''' </summary>
    Public Class MidiFelvevo

        ' --- A mentett .mid fájl időzítési paraméterei ---
        '
        ' Valós időben (ezredmásodpercben) veszünk fel, a Standard MIDI
        ' File-nak viszont kell egy tempó- és tick-felbontás-érték, mert ez
        ' határozza meg, hogy egy DAW hány ütemre osztva mutatja majd a
        ' felvételt. A 120 BPM / 480 tick per negyed hangjegy semleges,
        ' bevett alapérték: a HANGZÁST egyáltalán nem befolyásolja (a
        ' hangok pontosan ott szólalnak meg, ahol felvettük őket), csak azt,
        ' milyen ütembeosztást lát majd a felhasználó egy szerkesztőben.
        ' Amikor a metronóm-funkció elkészül, ott logikus lesz a ténylegesen
        ' beállított BPM-et beleírni.
        Private Const TICK_PER_NEGYED As Integer = 480
        Private Const MIKROSZEC_PER_NEGYED As Integer = 500000   ' 500 000 us = 120 BPM

        ' Ütemjelzés (time signature): 4/4. A tempóhoz hasonlóan ez sem
        ' befolyásolja a HANGZÁST, csak azt, milyen ütembeosztást mutat egy
        ' külső szerkesztő. Ha hiányzik a fájlból, a legtöbb lejátszó amúgy is
        ' 4/4-et feltételez - de tisztább kiírni. A nevezőt a szabvány 2
        ' HATVÁNYKITEVŐJEKÉNT tárolja: 2 jelenti a 4-es nevezőt (2^2 = 4).
        Private Const UTEM_SZAMLALO As Integer = 4
        Private Const UTEM_NEVEZO_KITEVO As Integer = 2           ' 2^2 = 4 -> 4/4
        Private Const METRONOM_UTES_TICK As Integer = 24          ' MIDI-óraütés metronóm-kattintásonként (szabványos alapérték)
        Private Const HARMINCKETTED_NEGYEDENKENT As Integer = 8   ' szabványos alapérték

        ''' <summary>Az előjegyzések számának szabvány szerinti határa (-7 .. +7).</summary>
        Private Const MAX_ELOJEGYZES As Integer = 7

        ''' <summary>Hány MIDI tick felel meg egy ezredmásodpercnek (120 BPM / 480 PPQ mellett 0,96).</summary>
        Private Const TICK_PER_MS As Double = TICK_PER_NEGYED * 1000.0R / MIKROSZEC_PER_NEGYED

        ''' <summary>A dob-csatorna 0-tól számított indexe (a 10. csatorna) - ott a dobkészlet-lista adja a neveket.</summary>
        Private Const DOB_CSATORNA_INDEX As Integer = 9

        ''' <summary>Egyetlen rögzített MIDI üzenet, a MidiEngine órája szerinti abszolút időbélyeggel.</summary>
        Private Structure FelvettEsemeny
            Public StatuszBajt As Integer
            Public Adat1 As Integer
            Public Adat2 As Integer
            Public IdobelyegMs As Long
        End Structure

        Private ReadOnly esemenyek As New List(Of FelvettEsemeny)

        ''' <summary>
        ''' Csatornánként/hangonként nyilvántartja, mely hangok vannak éppen
        ''' lenyomva - a felvétel lezárásakor ezekre generálunk Note Off-ot,
        ''' hogy ne maradjon a fájlban "lógó", soha el nem engedett hang
        ''' (ilyet egy külső lejátszó vagy örökké zúgva játszana le, vagy
        ''' hibásnak tekintené a fájlt).
        ''' </summary>
        Private hangSzol(15, 127) As Boolean

        ''' <summary>Hány hang szól éppen (a hangSzol igaz elemeinek száma) - hogy ne kelljen minden üzenetnél végignézni.</summary>
        Private szoloHangokSzama As Integer = 0

        ''' <summary>
        ''' Csatornánként: le van-e nyomva a kitartó pedál (CC64 >= 64). Az
        ''' Automatikus felvétel mód csendfigyeléséhez kell: amíg a pedál
        ''' lent van, a felengedett billentyűk hangja még lecseng, tehát
        ''' "szól valami".
        ''' </summary>
        Private pedalLent(15) As Boolean

        ''' <summary>
        ''' Annak az eseménynek az időbélyege, amely után már SEMMI nem szól
        ''' (az utolsó billentyű felengedése, illetve a pedál felengedése),
        ''' vagy -1, ha valami szól. Lásd CsendKezdeteMs.
        ''' </summary>
        Private csendKezdeteBelso As Long = -1

        Private allapotBelso As FelvevoAllapot = FelvevoAllapot.Alap
        Private elsoIdobelyegMs As Long = 0
        Private utolsoIdobelyegMs As Long = 0

        ''' <summary>
        ''' Az óra állása, ami a mentett fájl 0. tickjének felel meg. Alapból a
        ''' felvétel kezdete (az első leütés); ráfelvételnél (lásd
        ''' nullpontLekerdezo) a lejátszott fájl kezdete - ilyenkor a fájl
        ''' elején annyi csend van, amennyi a lejátszásból addig eltelt.
        ''' </summary>
        Private nullpontMs As Long = 0

        ''' <summary>
        ''' A felfegyverzéskor kapott függvény, amivel a felvevő a rögzítés
        ''' indulásakor megkérdezi, hol tart a lejátszás: az óra azon
        ''' állását adja vissza, ami a lejátszott fájl elejének felel meg,
        ''' vagy Nothing-ot, ha nincs lejátszás.
        ''' </summary>
        Private nullpontLekerdezo As Func(Of Long?) = Nothing

        ''' <summary>
        ''' A felvétel INDULÁSAKOR készült pillanatkép az aktív csatornák
        ''' MIDI-állapotáról (hangszín, hangerő, tér, vibrato, chorus,
        ''' reverb) - ez kerül a mentett fájlban minden csatorna SAJÁT
        ''' trackjének elejére, tick 0-ra.
        '''
        ''' MIÉRT KELL? A felvétel csak azokat az üzeneteket rögzíti, amelyek
        ''' az indulás UTÁN mennek ki - a hangszíneket viszont a felhasználó
        ''' jellemzően ELŐTTE állítja be. E nélkül a blokk nélkül a felvétel
        ''' egy külső lejátszóban/DAW-ban alapértelmezett zongorával szólalna
        ''' meg, nem azzal, amit a felhasználó ténylegesen hallott.
        '''
        ''' MIÉRT AZ INDULÁSKORI ÁLLAPOT, ÉS NEM A MENTÉSKORI? Mert ha
        ''' felvétel közben hangszert váltunk, a váltás Program Change
        ''' üzenete magától rögzül a felvételben - ha a fájl elejére a
        ''' MENTÉSKORI (végső) állapotot írnánk, a felvétel eleje már a
        ''' későbbi hangszínnel szólna, ami hibás.
        '''
        ''' A pillanatkép UTÁN ide kerül a felfegyverzés alatt kiküldött
        ''' előadói vezérlők utolsó értéke is (lásd eloadoiVezerlok) - azért
        ''' UTÁNA, hogy a ténylegesen kiküldött érték írja felül a panelét.
        ''' </summary>
        Private kezdoAllapotUzenetek As New List(Of MidiUzenet)

        ''' <summary>
        ''' A felfegyverzéskor kapott függvény, amivel a felvevő PONTOSAN a
        ''' rögzítés indulásának pillanatában kéri le a csatorna-állapotot.
        ''' Szándékosan függvény és nem kész lista: a felfegyverzés és az első
        ''' leütés között a felhasználó még változtathat a beállításokon.
        ''' </summary>
        Private kezdoAllapotLekerdezo As Func(Of List(Of MidiUzenet)) = Nothing

        ''' <summary>A kitartó (sustain) pedál Controller Change száma.</summary>
        Private Const SUSTAIN_PEDAL As Integer = 64

        ''' <summary>A pedál 64-es és afölötti értéke számít lenyomottnak (a MIDI szabvány szerint).</summary>
        Private Const PEDAL_LENT_HATAR As Integer = 64

        ''' <summary>A "mindent elnémító" Controller Change-ek (lásd MidiEngine.OsszesHangElnemitasa).</summary>
        Private Const ALL_SOUND_OFF As Integer = 120
        Private Const ALL_NOTES_OFF As Integer = 123

        ''' <summary>
        ''' Az ELŐADÓI vezérlők, amelyeknek a felfegyverzés ALATT kiküldött
        ''' utolsó értékét csatornánként megjegyezzük, és a felvétel
        ''' indulásakor a kezdőállapot-pillanatkép UTÁN, tick 0-ra írjuk.
        '''
        ''' MIÉRT KELL? A felvételt az első leütés indítja, az előtte
        ''' kiküldött üzenetek nem rögzülnek. A hangszín, hangerő, tér stb.
        ''' ettől nem vész el, mert azt a pillanatkép a csatorna-
        ''' beállításokból olvassa ki. Ez a kettő viszont a BILLENTYŰZETRŐL
        ''' jön, nem a panelről: egy az első hang ELŐTT lenyomott pedál, vagy
        ''' egy előre felhúzott modulációs kerék nélküle kimaradna a fájlból.
        ''' (A moduláció a pillanatképben is szerepel, a panel "Vibrato"
        ''' értékével - de ha a billentyűzet kereke ezt felülírta, a
        ''' ténylegesen kiküldött érték a helyes.)
        '''
        ''' Bővíthető: egy újabb vezérlő (pl. sostenuto, CC66) egyetlen elem.
        ''' </summary>
        Private Shared ReadOnly eloadoiVezerlok As Integer() = {CsatornaUzenetek.MOD_WHEEL, SUSTAIN_PEDAL}

        ''' <summary>
        ''' [csatorna, sorszám az eloadoiVezerlok-ben] -> a felfegyverzés
        ''' alatt utoljára kiküldött érték, vagy -1, ha nem volt ilyen.
        ''' </summary>
        Private felfegyverzesAlattiErtekek As Integer(,) = UresVezerloTabla()

        ''' <summary>
        ''' Overdub: rögzüljenek-e a VISSZAJÁTSZÁSBÓL származó üzenetek is?
        ''' Alapból KI - ilyenkor csak a felhasználó ÉLŐ tevékenysége (hangok,
        ''' csúszkák, pedál) kerül a felvételbe, a lejátszott anyag soha.
        ''' A felvevő ablak "A lejátszott hangok rögzítése" checkboxa állítja.
        ''' </summary>
        Public Property LejatszottHangokRogzitese As Boolean = False

        ''' <summary>
        ''' Kerüljön-e hangnem (key signature, `FF 59`) a mentett fájlba?
        ''' Alapból NEM: a hangnem megadása SZÁNDÉKOSAN nem kötelező - egy
        ''' rögtönzött felvételnek nem feltétlenül van értelmes hangneme, és
        ''' jobb üresen hagyni, mint egy oda nem illő C-dúrt állítani.
        ''' Ilyenkor a jelentésben a "nincs a fájlban" sor jelenik meg.
        '''
        ''' A metronóm-körben a felvevő ablak "Hangnem megadása" jelölő-
        ''' négyzete állítja majd (alapból kikapcsolva), és bekapcsolva egy
        ''' listából lehet választani a hangnemet.
        ''' </summary>
        Public Property HangnemMegadva As Boolean = False

        ''' <summary>
        ''' Az előjegyzések száma: -7..+7, ahol a negatív a b-ket, a pozitív a
        ''' kereszteket jelenti (0 = nincs előjegyzés). Csak akkor számít, ha
        ''' a HangnemMegadva be van kapcsolva.
        ''' </summary>
        Public Property HangnemElojegyzes As Integer = 0

        ''' <summary>Moll hangnem? (False = dúr.) Csak akkor számít, ha a HangnemMegadva be van kapcsolva.</summary>
        Public Property HangnemMoll As Boolean = False

        Public ReadOnly Property Allapot As FelvevoAllapot
            Get
                Return allapotBelso
            End Get
        End Property

        ''' <summary>Az eddig rögzített üzenetek száma (0, ha még nem indult el a felvétel).</summary>
        Public ReadOnly Property EsemenySzam As Integer
            Get
                Return esemenyek.Count
            End Get
        End Property

        ''' <summary>
        ''' Az eddigi felvétel hossza ezredmásodpercben (az első és az utolsó
        ''' rögzített üzenet között eltelt idő). Az Automatikus felvétel mód
        ''' JÁTÉKIDŐ-küszöbét ehhez méri majd a felvevő ablak.
        ''' </summary>
        Public ReadOnly Property RogzitettHosszMs As Long
            Get
                If esemenyek.Count = 0 Then Return 0
                Return utolsoIdobelyegMs - elsoIdobelyegMs
            End Get
        End Property

        ''' <summary>
        ''' A felvétel kezdetének (az első leütésnek) időbélyege a MidiEngine
        ''' órája szerint, vagy 0, ha a felvétel még nem indult el. A felvevő
        ''' ablak ebből számolja a futó időt: MidiEngine.ElteltIdoMs -
        ''' KezdoIdobelyegMs (ugyanarról az óráról, lásd UtolsoEsemenyIdobelyegMs).
        ''' </summary>
        Public ReadOnly Property KezdoIdobelyegMs As Long
            Get
                Return elsoIdobelyegMs
            End Get
        End Property

        ''' <summary>
        ''' Ráfelvételnél: hány ezredmásodperc csend kerül a mentett fájl
        ''' elejére, hogy a felvétel a lejátszott fájlhoz igazodjon (= hol
        ''' tartott a lejátszás, amikor a felvétel elindult). 0, ha a felvétel
        ''' nem lejátszás közben indult, vagy még el sem indult.
        ''' </summary>
        Public ReadOnly Property IgazitasMs As Long
            Get
                If allapotBelso <> FelvevoAllapot.Rogzit Then Return 0
                Return elsoIdobelyegMs - nullpontMs
            End Get
        End Property

        ''' <summary>
        ''' A legutóbb rögzített üzenet abszolút időbélyege (a MidiEngine
        ''' órája szerint). Az Automatikus felvétel mód SZÜNET-küszöbét ehhez
        ''' méri majd a felvevő ablak, a MidiEngine.ElteltIdoMs-hez hasonlítva
        ''' - ezért fontos, hogy mindkettő UGYANARRÓL az óráról származzon.
        ''' </summary>
        Public ReadOnly Property UtolsoEsemenyIdobelyegMs As Long
            Get
                Return utolsoIdobelyegMs
            End Get
        End Property

        ''' <summary>
        ''' Szól-e éppen valami: van-e lenyomott billentyű, vagy lenyomott
        ''' kitartó pedál bármelyik csatornán. (Csak a felvétel indulása óta
        ''' kiküldött üzenetek alapján - plusz a felfegyverzés alatt
        ''' lenyomott pedál, ami a fájl elejére is bekerül.)
        ''' </summary>
        Public ReadOnly Property SzolValami As Boolean
            Get
                If szoloHangokSzama > 0 Then Return True
                For csatorna As Integer = 0 To 15
                    If pedalLent(csatorna) Then Return True
                Next
                Return False
            End Get
        End Property

        ''' <summary>
        ''' A CSEND kezdete a MidiEngine órája szerint: annak az eseménynek az
        ''' időbélyege, amely után már semmi nem szól (minden billentyű fel
        ''' van engedve ÉS a kitartó pedál is fent van) - vagy -1, ha valami
        ''' szól, illetve ha nincs aktív felvétel.
        '''
        ''' Az Automatikus felvétel mód SZÜNET-küszöbét ehhez méri (lásd
        ''' AutomatikusSzegmentalo): MidiEngine.ElteltIdoMs - CsendKezdeteMs.
        ''' A felhasználó döntése (2026-09-24): a szünet NEM az utolsó
        ''' leütéstől számít, hanem attól, hogy semmi sem szól - így egy
        ''' hosszan kitartott akkord vagy egy pedálos lecsengés közepén nem
        ''' vág a program. A csend alatt kiküldött egyéb üzenet (pl. egy
        ''' csúszka) nem indítja újra a számlálást.
        ''' </summary>
        Public ReadOnly Property CsendKezdeteMs As Long
            Get
                Return csendKezdeteBelso
            End Get
        End Property

        ''' <summary>
        ''' Olyan üzenet-e, amit a felvevő egyáltalán figyelembe vesz: nem a
        ''' program belső karbantartó üzenete, nem metronóm, nem (overdub
        ''' nélkül) visszajátszás, és csatorna-üzenet. Az Automatikus
        ''' felvétel mód ezek ELŐTT végzi el a szegmens-ellenőrzést, hogy a
        ''' küszöb letelte után érkező üzenet már az ÚJ szegmensbe kerüljön.
        ''' </summary>
        Public Function FigyelembeVettUzenetE(statuszBajt As Integer, lejatszasBol As Boolean,
                                              metronomBol As Boolean, belsoUzenet As Boolean) As Boolean
            If belsoUzenet OrElse metronomBol Then Return False
            If lejatszasBol AndAlso Not LejatszottHangokRogzitese Then Return False
            Return statuszBajt >= &H80 AndAlso statuszBajt < &HF0
        End Function

        ''' <summary>
        ''' "Felfegyverzés": eldobja az esetleges korábbi anyagot, és
        ''' várakozó állapotba lép. A tényleges rögzítés az ELSŐ leütésnél
        ''' indul el (lásd FelvevoAllapot.Felfegyverezve).
        ''' </summary>
        ''' <param name="kezdoAllapotLekerdezo">
        ''' Opcionális függvény, amit a felvevő PONTOSAN a rögzítés
        ''' indulásakor hív meg, hogy pillanatképet kérjen az aktív csatornák
        ''' MIDI-állapotáról (lásd a kezdoAllapotUzenetek mezőt). Ha Nothing,
        ''' a mentett fájl nem kap kezdőállapot-blokkot.
        ''' </param>
        ''' <param name="nullpontLekerdezo">
        ''' Opcionális függvény a ráfelvételhez (7. fázis): a rögzítés
        ''' indulásakor megadja, az óra melyik állása felel meg a LEJÁTSZOTT
        ''' fájl elejének (Nothing, ha nincs lejátszás). Ha van ilyen, a
        ''' mentett fájl ehhez igazodik, így a lejátszott fájllal később
        ''' egymásra rakható (a felhasználó döntése, 2026-11-03).
        ''' </param>
        Public Sub Felfegyverzes(Optional kezdoAllapotLekerdezo As Func(Of List(Of MidiUzenet)) = Nothing,
                                 Optional nullpontLekerdezo As Func(Of Long?) = Nothing)
            Alaphelyzet()
            Me.kezdoAllapotLekerdezo = kezdoAllapotLekerdezo
            Me.nullpontLekerdezo = nullpontLekerdezo
            allapotBelso = FelvevoAllapot.Felfegyverezve
        End Sub

        ''' <summary>
        ''' Egy kiküldött MIDI üzenet felajánlása rögzítésre. Ezt hívja a
        ''' felvevő ablak a MidiEngine.UzenetKikuldve eseménykezelőjéből,
        ''' MINDEN kiküldött üzenetre - hogy az üzenet ténylegesen bekerül-e
        ''' a felvételbe, azt ez a metódus dönti el.
        '''
        ''' Kihagyásra kerül:
        ''' - minden üzenet, ha nem vagyunk felfegyverzett/rögzítő állapotban,
        ''' - a metronóm hangja MINDIG (nem kerülhet a mentett fájlba),
        ''' - a visszajátszásból származó üzenet, ha az overdub nincs
        '''   bekapcsolva (lásd LejatszottHangokRogzitese),
        ''' - a nem csatorna-üzenetek (rendszer-üzenetek, &amp;HF0 felett):
        '''   sok billentyűzet folyamatosan küld Active Sensing (&amp;HFE) vagy
        '''   MIDI Clock (&amp;HF8) üzenetet, ezek csak feleslegesen hizlalnák a
        '''   fájlt, és a felvétel szempontjából nincs értelmük,
        ''' - felfegyverzett állapotban minden, ami NEM leütés - ezek nem is
        '''   indítják el a felvételt (lásd FelvevoAllapot.Felfegyverezve),
        ''' - a program BELSŐ karbantartó üzenetei (lásd
        '''   MidiEngine.BelsoKarbantartasKent): a kimeneti eszköz váltásakor
        '''   kiküldött elnémító söprés és a beállítások újraküldése. Ezek sem
        '''   indítanak; felvétel közben csak az általuk elnémított hangok
        '''   kapnak Note Off-ot (lásd BelsoElnemitasKezelese).
        '''
        ''' A szűrők SORRENDJE számít: a metronóm, a visszajátszás és a
        ''' rendszer-üzenetek kiszűrése az indítás ELŐTT fut, tehát egy
        ''' metronóm-kattanás vagy egy visszajátszott hang (overdub nélkül)
        ''' sem indíthatja el a felvételt.
        ''' </summary>
        Public Sub UzenetRogzitese(statuszBajt As Integer, adat1 As Integer, adat2 As Integer,
                                   idobelyegMs As Long, lejatszasBol As Boolean, metronomBol As Boolean,
                                   belsoUzenet As Boolean)

            If allapotBelso = FelvevoAllapot.Alap Then Return
            If metronomBol Then Return
            If lejatszasBol AndAlso Not LejatszottHangokRogzitese Then Return
            If statuszBajt < &H80 OrElse statuszBajt >= &HF0 Then Return

            ' Belső karbantartó üzenet: se nem indít, se nem rögzül - felvétel
            ' közben csak az általa elnémított hangokat zárjuk le.
            If belsoUzenet Then
                If allapotBelso = FelvevoAllapot.Rogzit Then BelsoElnemitasKezelese(statuszBajt, adat1, idobelyegMs)
                Return
            End If

            If allapotBelso = FelvevoAllapot.Felfegyverezve Then
                ' CSAK egy valódi leütés indítja a felvételt - minden más
                ' üzenet elmarad, az előadói vezérlők értékét viszont
                ' megjegyezzük (lásd eloadoiVezerlok).
                If Not ValodiLeutesE(statuszBajt, adat2) Then
                    FelfegyverzesAlattiVezerloMegjegyzese(statuszBajt, adat1, adat2)
                    Return
                End If

                ' Ez az első leütés: innentől számít a felvétel kezdete.
                allapotBelso = FelvevoAllapot.Rogzit
                elsoIdobelyegMs = idobelyegMs

                ' Ráfelvétel: a fájl a lejátszott fájl elejéhez igazodik (a
                ' lejátszás az első leütés előtt indult, így a nullpont
                ' korábbi - egy későbbi érték értelmetlen lenne).
                nullpontMs = idobelyegMs
                If nullpontLekerdezo IsNot Nothing Then
                    Dim lejatszasKezdete As Long? = nullpontLekerdezo()
                    If lejatszasKezdete.HasValue AndAlso lejatszasKezdete.Value <= idobelyegMs Then
                        nullpontMs = lejatszasKezdete.Value
                    End If
                End If

                ' A csatorna-állapot pillanatképe PONTOSAN itt készül el -
                ' lásd a kezdoAllapotUzenetek mező magyarázatát.
                If kezdoAllapotLekerdezo IsNot Nothing Then
                    Dim pillanatkep As List(Of MidiUzenet) = kezdoAllapotLekerdezo()
                    If pillanatkep IsNot Nothing Then kezdoAllapotUzenetek.AddRange(pillanatkep)
                End If

                ' A pillanatkép UTÁN, hogy a ténylegesen kiküldött érték
                ' írja felül a panelét.
                FelfegyverzesAlattiVezerlokHozzaadasa()

                ' A felfegyverzés alatt lenyomva hagyott pedál a fájl elején
                ' is lenyomott - a csendfigyelés szempontjából is.
                FelfegyverzesAlattiPedalAtvetele()
            End If

            utolsoIdobelyegMs = idobelyegMs

            esemenyek.Add(New FelvettEsemeny With {
                .StatuszBajt = statuszBajt,
                .Adat1 = adat1,
                .Adat2 = adat2,
                .IdobelyegMs = idobelyegMs
            })

            HangNyilvantartasFrissitese(statuszBajt, adat1, adat2)
            PedalNyilvantartasFrissitese(statuszBajt, adat1, adat2)
            CsendFrissitese(idobelyegMs)
        End Sub

        ''' <summary>A kitartó pedál állapotának követése (CC64: 64-től lenyomva).</summary>
        Private Sub PedalNyilvantartasFrissitese(statuszBajt As Integer, adat1 As Integer, adat2 As Integer)
            If (statuszBajt And &HF0) <> &HB0 OrElse adat1 <> SUSTAIN_PEDAL Then Return
            pedalLent(statuszBajt And &HF) = (adat2 >= PEDAL_LENT_HATAR)
        End Sub

        ''' <summary>
        ''' A csend kezdetének karbantartása egy rögzített esemény után: ha
        ''' valami szól, nincs csend; ha most lett csend (ez az esemény
        ''' hallgattatta el az utolsó hangot / pedált), itt kezdődik; ha már
        ''' eddig is csend volt, marad a korábbi kezdete.
        ''' </summary>
        Private Sub CsendFrissitese(idobelyegMs As Long)
            If SzolValami Then
                csendKezdeteBelso = -1
            ElseIf csendKezdeteBelso < 0 Then
                csendKezdeteBelso = idobelyegMs
            End If
        End Sub

        ''' <summary>
        ''' A felvétel indulásakor: a felfegyverzés alatt utoljára kiküldött
        ''' pedál-érték csatornánként (lásd felfegyverzesAlattiErtekek).
        ''' </summary>
        Private Sub FelfegyverzesAlattiPedalAtvetele()
            Dim sorszam As Integer = Array.IndexOf(eloadoiVezerlok, SUSTAIN_PEDAL)
            For csatorna As Integer = 0 To 15
                Dim ertek As Integer = felfegyverzesAlattiErtekek(csatorna, sorszam)
                If ertek >= 0 Then pedalLent(csatorna) = (ertek >= PEDAL_LENT_HATAR)
            Next
        End Sub

        ''' <summary>
        ''' A lenyomott hangok nyilvántartásának karbantartása. A MIDI
        ''' szabvány szerint a 0 sebességű (velocity) Note On ugyanazt
        ''' jelenti, mint egy Note Off - sok billentyűzet ezt is használja,
        ''' ezért mindkét formát kezelni kell.
        ''' </summary>
        Private Sub HangNyilvantartasFrissitese(statuszBajt As Integer, adat1 As Integer, adat2 As Integer)
            Dim uzenetTipus As Integer = statuszBajt And &HF0
            If uzenetTipus <> &H90 AndAlso uzenetTipus <> &H80 Then Return

            Dim csatorna As Integer = statuszBajt And &HF
            If adat1 < 0 OrElse adat1 > 127 Then Return

            Dim hangIndul As Boolean = (uzenetTipus = &H90 AndAlso adat2 > 0)
            If hangSzol(csatorna, adat1) = hangIndul Then Return
            hangSzol(csatorna, adat1) = hangIndul
            szoloHangokSzama += If(hangIndul, 1, -1)
        End Sub

        ''' <summary>
        ''' Belső "mindent elnémító" üzenet (All Sound Off / All Notes Off, lásd
        ''' MidiEngine.OsszesHangElnemitasa) felvétel közben: maga az üzenet NEM
        ''' kerül a fájlba, az általa elhallgattatott hangok viszont ebben a
        ''' pillanatban Note Off-ot kapnak. Enélkül a fájlban tovább szólnának:
        ''' a billentyű felengedésekor a motor már nem küld Note Off-ot, mert
        ''' a kimenet cseréjekor a saját nyilvántartását is törölte.
        ''' </summary>
        Private Sub BelsoElnemitasKezelese(statuszBajt As Integer, adat1 As Integer, idobelyegMs As Long)
            If (statuszBajt And &HF0) <> &HB0 Then Return
            If adat1 <> ALL_SOUND_OFF AndAlso adat1 <> ALL_NOTES_OFF Then Return

            Dim csatorna As Integer = statuszBajt And &HF
            For hang As Integer = 0 To 127
                If Not hangSzol(csatorna, hang) Then Continue For
                esemenyek.Add(New FelvettEsemeny With {
                    .StatuszBajt = &H80 Or csatorna,
                    .Adat1 = hang,
                    .Adat2 = 0,
                    .IdobelyegMs = idobelyegMs
                })
                hangSzol(csatorna, hang) = False
                szoloHangokSzama -= 1
                utolsoIdobelyegMs = idobelyegMs
            Next
            CsendFrissitese(idobelyegMs)
        End Sub

        ''' <summary>
        ''' Valódi leütés-e: Note On, nullánál NAGYOBB sebességgel. A 0
        ''' sebességű Note On a MIDI szabvány szerint hangkikapcsolás - az nem
        ''' indíthatja el a felvételt.
        ''' </summary>
        Private Shared Function ValodiLeutesE(statuszBajt As Integer, adat2 As Integer) As Boolean
            Return (statuszBajt And &HF0) = &H90 AndAlso adat2 > 0
        End Function

        ''' <summary>
        ''' Felfegyverzett állapotban: ha az üzenet valamelyik előadói vezérlő
        ''' (lásd eloadoiVezerlok), megjegyzi az értékét a csatornájához. Csak
        ''' az UTOLSÓ érték számít - egy lenyomott, majd felengedett pedál
        ''' így helyesen felengedettként kerül a felvétel elejére.
        ''' </summary>
        Private Sub FelfegyverzesAlattiVezerloMegjegyzese(statuszBajt As Integer, adat1 As Integer, adat2 As Integer)
            If (statuszBajt And &HF0) <> &HB0 Then Return
            Dim sorszam As Integer = Array.IndexOf(eloadoiVezerlok, adat1)
            If sorszam < 0 Then Return
            felfegyverzesAlattiErtekek(statuszBajt And &HF, sorszam) = adat2
        End Sub

        ''' <summary>
        ''' A felfegyverzés alatt megjegyzett előadói vezérlők hozzáfűzése a
        ''' kezdőállapothoz - csatornánként, az eloadoiVezerlok sorrendjében,
        ''' és csak azokat, amelyekre ténylegesen érkezett érték.
        ''' </summary>
        Private Sub FelfegyverzesAlattiVezerlokHozzaadasa()
            For csatorna As Integer = 0 To 15
                For sorszam As Integer = 0 To eloadoiVezerlok.Length - 1
                    Dim ertek As Integer = felfegyverzesAlattiErtekek(csatorna, sorszam)
                    If ertek < 0 Then Continue For
                    kezdoAllapotUzenetek.Add(New MidiUzenet(&HB0 Or csatorna, eloadoiVezerlok(sorszam), ertek, 2))
                Next
            Next
        End Sub

        ''' <summary>Üres (csupa -1) előadóivezérlő-tábla: 16 csatorna × az eloadoiVezerlok elemei.</summary>
        Private Shared Function UresVezerloTabla() As Integer(,)
            Dim tabla(15, eloadoiVezerlok.Length - 1) As Integer
            For csatorna As Integer = 0 To 15
                For sorszam As Integer = 0 To eloadoiVezerlok.Length - 1
                    tabla(csatorna, sorszam) = -1
                Next
            Next
            Return tabla
        End Function

        ''' <summary>
        ''' Lezárja a felvételt és elmenti a megadott útvonalra. Lezárás
        ''' előtt minden még lenyomott hangra Note Off-ot generál (az utolsó
        ''' rögzített üzenet időpontjában), hogy ne maradjon "lógó" hang a
        ''' fájlban. A mentés után az osztály nyugalmi állapotba kerül, a
        ''' gyűjtött anyag pedig törlődik.
        ''' </summary>
        ''' <returns>
        ''' True, ha ténylegesen készült fájl; False, ha nem volt mit menteni
        ''' (a felvétel el sem indult, tehát egyetlen üzenet sem rögzült).
        ''' </returns>
        ''' <exception cref="IO.IOException">A fájl írása nem sikerült - a hívó dolga jelezni a felhasználónak.</exception>
        Public Function LezarasEsMentes(utvonal As String) As Boolean
            LogoHangokLezarasa()

            If esemenyek.Count = 0 Then
                Alaphelyzet()
                Return False
            End If

            Dim tartalom As Byte() = SmfFajlEpitese()
            IO.File.WriteAllBytes(utvonal, tartalom)

            Alaphelyzet()
            Return True
        End Function

        ''' <summary>
        ''' Lezárja és ELDOBJA az eddig gyűjtött anyagot, mentés nélkül. Az
        ''' Automatikus felvétel mód használja: ha a szünet-küszöb előbb
        ''' következik be, mint hogy a játékidő-küszöböt elértük volna, a
        ''' rövid töredék nem érdemel fájlt.
        ''' </summary>
        Public Sub Eldobas()
            Alaphelyzet()
        End Sub

        ''' <summary>Teljes belső alaphelyzet: üres puffer, tiszta hang-nyilvántartás, nyugalmi állapot.</summary>
        Private Sub Alaphelyzet()
            esemenyek.Clear()
            kezdoAllapotUzenetek.Clear()
            kezdoAllapotLekerdezo = Nothing
            nullpontLekerdezo = Nothing
            nullpontMs = 0
            Array.Clear(hangSzol, 0, hangSzol.Length)
            szoloHangokSzama = 0
            Array.Clear(pedalLent, 0, pedalLent.Length)
            csendKezdeteBelso = -1
            felfegyverzesAlattiErtekek = UresVezerloTabla()
            allapotBelso = FelvevoAllapot.Alap
            elsoIdobelyegMs = 0
            utolsoIdobelyegMs = 0
        End Sub

        ''' <summary>
        ''' A felvétel végén még lenyomva lévő hangokra Note Off üzenetet fűz
        ''' a felvétel végére (az utolsó rögzített üzenet időpontjában). Ez
        ''' tipikusan akkor fordul elő, ha a felhasználó épp lenyomva tartott
        ''' billentyűk mellett nyomott Stopot, vagy ha az Automatikus mód
        ''' zárta le a szegmenst.
        ''' </summary>
        Private Sub LogoHangokLezarasa()
            If esemenyek.Count = 0 Then Return

            For csatorna As Integer = 0 To 15
                For hang As Integer = 0 To 127
                    If Not hangSzol(csatorna, hang) Then Continue For

                    esemenyek.Add(New FelvettEsemeny With {
                        .StatuszBajt = &H80 Or csatorna,
                        .Adat1 = hang,
                        .Adat2 = 0,
                        .IdobelyegMs = utolsoIdobelyegMs
                    })
                    hangSzol(csatorna, hang) = False
                Next
            Next
            szoloHangokSzama = 0
        End Sub

        ' ============================================================
        ' Standard MIDI File (.mid) írása - saját, bájt-szintű kóddal
        ' ============================================================
        '
        ' MIÉRT NEM NAudio-val? Az NAudio MidiEventCollection.PrepareForExport()
        ' metódusa validálja a Note On / Note Off párokat, és a felvétel
        ' természetéből adódó szélső esetektől (pl. váratlanul félbeszakadt
        ' hang) kivételt dobhat. Saját írással a kontroll teljes, és a
        ' projektben már bevált ez a megközelítés - lásd
        ' Modellek\MidiFajlSzerkeszto.vb, ami ugyanígy, kézzel dolgozik a
        ' chunk-okkal és a variable-length quantity delta-time-okkal.
        ' (A .mid BEOLVASÁSA a lejátszáshoz viszont majd NAudio-val
        ' történik - ott tényleg kényelmesebb.)
        '
        ' MIÉRT FORMAT 1 ÉS NEM FORMAT 0? Egyetlen track esetén a format 0
        ' egyszerűbb lenne, de a program saját "MIDI fájlok módosítása"
        ' segédprogramja (MidiFajlSzerkeszto) JELENLEG csak format 1
        ' fájlokat kezel - format 0-val mentve a felhasználó a SAJÁT
        ' felvételeire nem tudná ráengedni ezt a funkciót.
        '
        ' A TRACK-SZERKEZET: a 0. a szabványos "conductor" track (a darab
        ' címe, a tempó és az ütemjelzés), utána pedig MINDEN SZEREPLŐ
        ' CSATORNA KAP EGY SAJÁT TRACKET. Ez a DAW-okban bevett elrendezés:
        ' minden csatorna külön, névvel ellátott sávként jelenik meg, és a
        ' csatorna induló beállításai (hangszín, hangerő, tér, vibrato,
        ' chorus, reverb) is a SAJÁT trackje elején, tick 0-n ülnek - így
        ' minden sáv önmagában teljes. Korábban egyetlen közös zenei track
        ' volt, egy kupac kezdőállapottal a legelején.

        ''' <summary>A darab címe a 0. trackben - minden felvételnél ugyanez.</summary>
        Private Const DARAB_CIME As String = "MidiBill3 felvétel"

        ' Szöveges meta-események azonosítói (lásd Modellek\MidiFajlOlvaso.vb).
        Private Const META_TRACKNEV As Integer = &H3
        Private Const META_HANGSZERNEV As Integer = &H4
        Private Const META_CSATORNA_ELOTAG As Integer = &H20

        ''' <summary>A fájlba írt nevek felső hossza - egy sérült lista se hizlalhassa értelmetlenre a fájlt.</summary>
        Private Const MAX_NEV_HOSSZ As Integer = 120

        Private Function SmfFajlEpitese() As Byte()
            Dim csatornak As List(Of Integer) = SzereploCsatornak()
            Dim fajl As New List(Of Byte)

            ' --- MThd fejléc ---
            fajl.AddRange(AzonositoBajtok("MThd"))
            fajl.AddRange(BeIntBajtok(6, 4))                        ' a fejléc adatrészének hossza mindig 6
            fajl.AddRange(BeIntBajtok(1, 2))                        ' format 1
            fajl.AddRange(BeIntBajtok(1 + csatornak.Count, 2))      ' conductor + csatornánként egy track
            fajl.AddRange(BeIntBajtok(TICK_PER_NEGYED, 2))          ' felbontás: tick / negyed hangjegy

            fajl.AddRange(ChunkEpitese("MTrk", ConductorTrackEpitese()))
            For Each csatorna As Integer In csatornak
                fajl.AddRange(ChunkEpitese("MTrk", CsatornaTrackEpitese(csatorna)))
            Next

            Return fajl.ToArray()
        End Function

        ''' <summary>
        ''' A felvételben TÉNYLEGESEN szereplő csatornák, növekvő sorrendben -
        ''' minden ilyen csatorna kap egy saját tracket. Amelyik csatornán
        ''' egyetlen üzenet sincs, arra nem készül üres sáv.
        ''' </summary>
        Private Function SzereploCsatornak() As List(Of Integer)
            Dim van(15) As Boolean
            For Each esemeny As FelvettEsemeny In esemenyek
                van(esemeny.StatuszBajt And &HF) = True
            Next

            Dim eredmeny As New List(Of Integer)
            For csatorna As Integer = 0 To 15
                If van(csatorna) Then eredmeny.Add(csatorna)
            Next
            Return eredmeny
        End Function

        ''' <summary>A 0. (conductor) track: a darab címe, a tempó, az ütemjelzés és a kötelező End of Track.</summary>
        Private Function ConductorTrackEpitese() As Byte()
            Dim adat As New List(Of Byte)

            ' A 0. track neve a szabvány szerint magának a DARABNAK a címe.
            SzovegesMetaHozzaadasa(adat, META_TRACKNEV, DARAB_CIME)

            ' delta 0 + Set Tempo meta-esemény (FF 51 03 + 3 bájt mikroszekundum/negyed)
            adat.Add(0)
            adat.Add(&HFF)
            adat.Add(&H51)
            adat.Add(3)
            adat.AddRange(BeIntBajtok(MIKROSZEC_PER_NEGYED, 3))

            ' delta 0 + Time Signature meta-esemény (FF 58 04 + számláló,
            ' nevező-kitevő, metronóm-ütés, harminckettedek)
            adat.Add(0)
            adat.Add(&HFF)
            adat.Add(&H58)
            adat.Add(4)
            adat.Add(CByte(UTEM_SZAMLALO))
            adat.Add(CByte(UTEM_NEVEZO_KITEVO))
            adat.Add(CByte(METRONOM_UTES_TICK))
            adat.Add(CByte(HARMINCKETTED_NEGYEDENKENT))

            ' delta 0 + Key Signature meta-esemény (FF 59 02 + előjegyzés, dúr/moll),
            ' de CSAK ha a felhasználó meg is adott hangnemet - lásd HangnemMegadva.
            ' Az előjegyzés bájtja ELŐJELES: a maszkolás a negatív értékeket is
            ' helyes kettes komplemensű bájttá alakítja (pl. -2 -> &HFE).
            If HangnemMegadva Then
                Dim elojegyzes As Integer = Math.Max(-MAX_ELOJEGYZES, Math.Min(MAX_ELOJEGYZES, HangnemElojegyzes))
                adat.Add(0)
                adat.Add(&HFF)
                adat.Add(&H59)
                adat.Add(2)
                adat.Add(CByte(elojegyzes And &HFF))
                adat.Add(CByte(If(HangnemMoll, 1, 0)))
            End If

            VegeJelHozzaadasa(adat)
            Return adat.ToArray()
        End Function

        ''' <summary>
        ''' EGY csatorna saját trackje: a csatorna-előtag, a nevek, a csatorna
        ''' induló beállításai (tick 0-n), végül a csatorna felvett üzenetei
        ''' delta-time + esemény párokként.
        '''
        ''' Az ezredmásodperces időbélyegeket ABSZOLÚT tick-értékké alakítjuk,
        ''' és csak UTÁNA vonjuk ki őket egymásból - ha delta-nként kerekítenénk,
        ''' a kerekítési hibák egy hosszabb felvételen összeadódnának, és a vége
        ''' észrevehetően elcsúszna. Minden track a felvétel KEZDETÉTŐL (tick 0)
        ''' számol, tehát a sávok időben egymáshoz vannak igazítva. (Ráfelvételnél a
        ''' tick 0 a LEJÁTSZOTT fájl eleje, lásd nullpontMs.)
        ''' </summary>
        Private Function CsatornaTrackEpitese(csatorna As Integer) As Byte()
            Dim adat As New List(Of Byte)
            Dim hangszerNev As String = CsatornaHangszerNeve(csatorna)

            ' Csatorna-előtag (FF 20): a szabvány szerinti módja annak, hogy a
            ' track nevei egyértelműen ehhez a csatornához kötődjenek. Egy
            ' csatornás tracknél önmagában is egyértelmű lenne, de kiírva a
            ' beolvasók dolga sem kérdéses.
            adat.Add(0)
            adat.Add(&HFF)
            adat.Add(CByte(META_CSATORNA_ELOTAG))
            adat.Add(1)
            adat.Add(CByte(csatorna))

            SzovegesMetaHozzaadasa(adat, META_TRACKNEV, TracknevSzoveg(csatorna, hangszerNev))
            If hangszerNev.Length > 0 Then
                SzovegesMetaHozzaadasa(adat, META_HANGSZERNEV, hangszerNev)
            End If

            ' A csatorna induló beállításai a SAJÁT trackje legelejére kerülnek,
            ' tick 0-ra - így egy külső lejátszó/DAW a helyes hangszínnel,
            ' hangerővel és effektekkel szólaltatja meg a sávot, és a sáv
            ' önmagában, a többitől függetlenül is teljes.
            For Each uzenet As MidiUzenet In kezdoAllapotUzenetek
                If uzenet.Csatorna <> csatorna Then Continue For
                adat.Add(0)  ' delta-time = 0: mind a felvétel legelső pillanatában
                adat.Add(CByte(uzenet.StatuszBajt And &HFF))
                adat.Add(Adatbajt(uzenet.Adat1))
                If uzenet.AdatbajtokSzama = 2 Then
                    adat.Add(Adatbajt(uzenet.Adat2))
                End If
            Next

            Dim elozoTick As Long = 0
            For Each esemeny As FelvettEsemeny In esemenyek
                If (esemeny.StatuszBajt And &HF) <> csatorna Then Continue For

                Dim relativMs As Long = esemeny.IdobelyegMs - nullpontMs
                Dim abszolutTick As Long = CLng(Math.Round(relativMs * TICK_PER_MS))
                If abszolutTick < elozoTick Then abszolutTick = elozoTick

                VlqHozzaadasa(adat, abszolutTick - elozoTick)
                elozoTick = abszolutTick

                adat.Add(CByte(esemeny.StatuszBajt And &HFF))
                adat.Add(Adatbajt(esemeny.Adat1))
                If AdatbajtokSzama(esemeny.StatuszBajt) = 2 Then
                    adat.Add(Adatbajt(esemeny.Adat2))
                End If
            Next

            VegeJelHozzaadasa(adat)
            Return adat.ToArray()
        End Function

        ''' <summary>
        ''' A track neve: KÉTJEGYŰ CSATORNASZÁM - HANGSZERNÉV (pl. "03 - Tenor
        ''' Sax"). A csatornaszám a felületen és a jelentésben látható, 1-től
        ''' induló sorszám (nem a track sorszáma, és nem a 0-tól induló belső
        ''' index) - így egy DAW sávlistájában rögtön látszik, melyik sáv
        ''' melyik csatorna. Ha a hangszernév nem deríthető ki, csak a szám
        ''' marad.
        ''' </summary>
        Private Shared Function TracknevSzoveg(csatorna As Integer, hangszerNev As String) As String
            Dim szam As String = (csatorna + 1).ToString("00", Globalization.CultureInfo.InvariantCulture)
            If hangszerNev.Length = 0 Then Return szam
            Return szam & " - " & hangszerNev
        End Function

        ''' <summary>
        ''' Egy csatorna hangszerneve a BETÖLTÖTT hangszerlistából, üres
        ''' sztring, ha nem deríthető ki.
        '''
        ''' A bank+program párost a csatorna ELSŐ Program Change üzenetéből
        ''' olvassuk ki: először a felvétel indulásakor készült pillanatképből
        ''' (ott van a legtöbb csatorna hangszíne), és ha ott nincs, a felvett
        ''' anyagból - ez utóbbi akkor számít, ha a csatorna csak MENET KÖZBEN
        ''' vált aktívvá. MINDKÉT bank-bájt számít, ugyanúgy, mint a
        ''' beolvasónál (MidiFajlOlvaso) - lásd Hangszerkulcs.vb.
        ''' </summary>
        Private Function CsatornaHangszerNeve(csatorna As Integer) As String
            Dim bankMsb As Integer = 0
            Dim bankLsb As Integer = 0
            Dim program As Integer = -1

            For Each uzenet As MidiUzenet In kezdoAllapotUzenetek
                If uzenet.Csatorna <> csatorna Then Continue For
                If ProgramFigyelese(uzenet.StatuszBajt, uzenet.Adat1, uzenet.Adat2, bankMsb, bankLsb, program) Then Exit For
            Next

            If program < 0 Then
                For Each esemeny As FelvettEsemeny In esemenyek
                    If (esemeny.StatuszBajt And &HF) <> csatorna Then Continue For
                    If ProgramFigyelese(esemeny.StatuszBajt, esemeny.Adat1, esemeny.Adat2, bankMsb, bankLsb, program) Then Exit For
                Next
            End If

            If program < 0 Then Return ""

            Dim dobCsatorna As Boolean = (csatorna = DOB_CSATORNA_INDEX)

            ' Ugyanaz a KÉT lépcsős visszaesés, mint a jelentésben (lásd
            ' MidiJelentes.VisszaeseskorNev): pontos hármas -> MSB nélkül ->
            ' puszta programszám. Itt csak a nevet használjuk, azt nem
            ' jelezzük, melyik lépcsőn találtunk - a tracknévbe úgysem férne.
            Dim nev As String = HangszerNev(Hangszerkulcs.Kulcs(bankMsb, bankLsb, program), dobCsatorna)
            If nev.Length > 0 Then Return nev

            If bankMsb <> 0 AndAlso bankLsb <> 0 Then
                nev = HangszerNev(Hangszerkulcs.Kulcs(0, bankLsb, program), dobCsatorna)
                If nev.Length > 0 Then Return nev
            End If

            Return HangszerNev(Hangszerkulcs.Kulcs(0, 0, program), dobCsatorna)
        End Function

        ''' <summary>
        ''' Egyetlen üzenet feldolgozása a hangszín kiderítéséhez: a Bank
        ''' Select bájtokat eltárolja, Program Change-nél pedig beállítja a
        ''' programot.
        ''' </summary>
        ''' <returns>True, ha ez volt a Program Change, tehát megvan a hangszín.</returns>
        Private Shared Function ProgramFigyelese(statuszBajt As Integer, adat1 As Integer, adat2 As Integer,
                                                 ByRef bankMsb As Integer, ByRef bankLsb As Integer,
                                                 ByRef program As Integer) As Boolean
            Dim uzenetTipus As Integer = statuszBajt And &HF0

            If uzenetTipus = &HB0 Then
                If adat1 = CsatornaUzenetek.BANK_SELECT_MSB Then bankMsb = adat2
                If adat1 = CsatornaUzenetek.BANK_SELECT_LSB Then bankLsb = adat2
                Return False
            End If

            If uzenetTipus = &HC0 Then
                program = adat1
                Return True
            End If

            Return False
        End Function

        ''' <summary>Hangszernév a megfelelő listából: a 10. csatornán a dobkészlet-listából, egyébként a hangszerlistából.</summary>
        Private Shared Function HangszerNev(kombinaltKulcs As Integer, dobCsatorna As Boolean) As String
            If dobCsatorna Then Return HangszerLista.DobNev(kombinaltKulcs, "")
            Return HangszerLista.Nev(kombinaltKulcs, "")
        End Function

        ''' <summary>
        ''' Szöveges meta-esemény hozzáadása delta 0-val (FF &lt;típus&gt;
        ''' &lt;hossz&gt; &lt;bájtok&gt;).
        '''
        ''' A Standard MIDI File szabvány a szövegeket 8 bites, Latin-1
        ''' karakterekként tárolja - a magyar "ő" és "ű" viszont NINCS benne.
        ''' Ezeket ezért "ö"/"ü"-re cseréljük, ami olvasható marad; e nélkül
        ''' a kódolás kérdőjelet tenne a helyükre. A többi magyar ékezetes
        ''' betű (á é í ó ö ú ü) benne van a Latin-1-ben.
        ''' </summary>
        Private Shared Sub SzovegesMetaHozzaadasa(adat As List(Of Byte), metaTipus As Integer, szoveg As String)
            Dim tisztitott As String = szoveg.Replace("ő"c, "ö"c).Replace("Ő"c, "Ö"c).
                                             Replace("ű"c, "ü"c).Replace("Ű"c, "Ü"c)
            If tisztitott.Length > MAX_NEV_HOSSZ Then tisztitott = tisztitott.Substring(0, MAX_NEV_HOSSZ)

            Dim bajtok As Byte() = Text.Encoding.Latin1.GetBytes(tisztitott)

            adat.Add(0)
            adat.Add(&HFF)
            adat.Add(CByte(metaTipus))
            VlqHozzaadasa(adat, bajtok.Length)
            adat.AddRange(bajtok)
        End Sub

        ''' <summary>
        ''' Hány adatbájt tartozik egy csatorna-üzenethez? A Program Change
        ''' (&amp;HC0) és a Channel Pressure (&amp;HD0) egybájtos, minden más
        ''' csatorna-üzenet kétbájtos.
        ''' </summary>
        Private Shared Function AdatbajtokSzama(statuszBajt As Integer) As Integer
            Dim uzenetTipus As Integer = statuszBajt And &HF0
            If uzenetTipus = &HC0 OrElse uzenetTipus = &HD0 Then Return 1
            Return 2
        End Function

        ''' <summary>Minden MTrk chunk kötelező záró meta-eseménye: delta 0 + FF 2F 00.</summary>
        Private Shared Sub VegeJelHozzaadasa(adat As List(Of Byte))
            adat.Add(0)
            adat.Add(&HFF)
            adat.Add(&H2F)
            adat.Add(0)
        End Sub

        ''' <summary>
        ''' Variable-length quantity (VLQ) kódolás - a MIDI fájlok
        ''' delta-time formátuma: 7 bites csoportokra bontva, és az utolsó
        ''' KIVÉTELÉVEL minden bájt felső bitje 1 (ez jelzi, hogy még
        ''' folytatódik).
        ''' </summary>
        Private Shared Sub VlqHozzaadasa(adat As List(Of Byte), ertek As Long)
            Dim ertekBelso As Long = If(ertek < 0, 0L, ertek)

            ' Előbb a 7 bites csoportok a legkisebb helyiértéktől, utána
            ' fordított sorrendben írjuk ki őket.
            Dim csoportok As New List(Of Byte)
            csoportok.Add(CByte(ertekBelso And &H7FL))

            Dim maradek As Long = ertekBelso >> 7
            While maradek > 0
                csoportok.Add(CByte((maradek And &H7FL) Or &H80L))
                maradek >>= 7
            End While

            For i As Integer = csoportok.Count - 1 To 0 Step -1
                adat.Add(csoportok(i))
            Next
        End Sub

        ''' <summary>Egy MIDI adatbájt (0-127) tartományra szorítása - a felső bit a szabvány szerint mindig 0.</summary>
        Private Shared Function Adatbajt(ertek As Integer) As Byte
            Return CByte(Math.Max(0, Math.Min(127, ertek)))
        End Function

        Private Shared Function AzonositoBajtok(azonosito As String) As Byte()
            Dim eredmeny(3) As Byte
            For i As Integer = 0 To 3
                eredmeny(i) = CByte(Asc(azonosito(i)))
            Next
            Return eredmeny
        End Function

        ''' <summary>Egy egész szám big-endian bájtsorozattá alakítása (a MIDI fájlformátum végig big-endian).</summary>
        Private Shared Function BeIntBajtok(ertek As Integer, hossz As Integer) As Byte()
            Dim eredmeny(hossz - 1) As Byte
            For i As Integer = 0 To hossz - 1
                eredmeny(i) = CByte((ertek >> (8 * (hossz - 1 - i))) And &HFF)
            Next
            Return eredmeny
        End Function

        ''' <summary>Egy teljes chunk összeállítása: 4 bájtos azonosító + 4 bájtos hossz + adatrész.</summary>
        Private Shared Function ChunkEpitese(azonosito As String, adatResz As Byte()) As Byte()
            Dim eredmeny As New List(Of Byte)
            eredmeny.AddRange(AzonositoBajtok(azonosito))
            eredmeny.AddRange(BeIntBajtok(adatResz.Length, 4))
            eredmeny.AddRange(adatResz)
            Return eredmeny.ToArray()
        End Function

    End Class

End Namespace

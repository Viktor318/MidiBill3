Imports System.Runtime.InteropServices
Imports System.Threading

Namespace Modellek

    ''' <summary>
    ''' A MIDI fájl LEJÁTSZÓJA (a felvétel 5. fázisa), felület nélkül.
    '''
    ''' A betöltött eseménylistát (lásd MidiFajlOlvaso.EsemenyekBeolvasasa)
    ''' SAJÁT HÁTTÉRSZÁLON küldi ki, a megadott órához igazítva (a program a
    ''' MidiEngine stopperóráját adja). Azért nem WinForms-időzítővel, mert
    ''' annak kb. 15 ms-os felbontása és a felületi szál terheltsége hallható
    ''' csúszást okozna. A szál a lejátszás idejére 1 ms-ra állítja a Windows
    ''' időzítő-felbontását (timeBeginPeriod), a végén visszaállítja.
    '''
    ''' A lejátszó NYILVÁNTARTJA az általa megszólaltatott hangokat és a
    ''' lenyomott pedált: Stopnál (és a fájl végén) CSAK ezeket engedi el - az
    ''' élő játékhoz nem nyúl (a felhasználó döntése, 2026-09-26). Szándékosan
    ''' nem All Notes Off / All Sound Off: az a közben élőben játszott hangokat
    ''' is elhallgattatná.
    '''
    ''' A kiküldést a konstruktorban kapott függvény végzi (a program a
    ''' MidiEngine.LejatszottUzenetKuldese-t adja) - így a lejátszó a
    ''' MIDI-motor nélkül, szimulált órával is tesztelhető (lásd Lepes).
    ''' </summary>
    Public Class MidiLejatszo
        Implements IDisposable

        Private ReadOnly kuldo As Action(Of Integer, Integer, Integer)
        Private ReadOnly ora As Func(Of Long)

        ''' <summary>A szál és a felület közös adatait védi (esemény-index, hang-nyilvántartás).</summary>
        Private ReadOnly zar As New Object()
        Private ReadOnly leallitasJel As New ManualResetEvent(False)
        Private szal As Thread = Nothing

        Private esemenyek As New List(Of MidiLejatszasiEsemeny)
        Private hosszMsBelso As Double = 0
        Private kovetkezo As Integer = 0
        ''' <summary>Az óra állása, ami a fájl 0. ezredmásodpercének felel meg.</summary>
        Private kezdoOraMs As Long = 0
        Private futBelso As Boolean = False

        ''' <summary>
        ''' A lejátszási pozíció ezredmásodpercben, amikor a lejátszás NEM fut:
        ''' innen indul a következő lejátszás. A Stop ott hagyja, ahol
        ''' megállt; a fájl vége és egy új fájl betöltése a fájl elejére
        ''' állítja; megállított állapotban a tekerés (Ugras) állítja.
        ''' </summary>
        Private allasMs As Double = 0

        ''' <summary>Az óra állása a legutóbbi leálláskor (Stop, fájl vége, hiba), vagy Nothing, ha még nem futott.</summary>
        Private leallasOraMs As Long? = Nothing

        ''' <summary>Hányszor szól éppen egy (csatorna, hang) a lejátszásból - az átfedő azonos hangokat is helyesen kezeli.</summary>
        Private ReadOnly szoloHangok(15, 127) As Integer
        Private ReadOnly pedalLent(15) As Boolean

        ''' <summary>A kitartó pedál Controller Change száma, és a lenyomott állapot alsó határa.</summary>
        Private Const SUSTAIN_PEDAL As Integer = 64
        Private Const PEDAL_LENT_HATAR As Integer = 64

        ''' <summary>Legfeljebb ennyit vár egyszerre a szál (ms) - a Stop így gyorsan érvényesül.</summary>
        Private Const MAX_VARAKOZAS_MS As Integer = 20

        ''' <summary>
        ''' A fájl végére érve kiváltódik - a LEJÁTSZÓ SZÁLÁN. (A Stop nem
        ''' váltja ki.) A felület BeginInvoke-kal adja át a saját szálának.
        ''' </summary>
        Public Event Befejezodott()

        ''' <param name="kuldo">Egy üzenet kiküldése (státuszbájt, adat1, adat2).</param>
        ''' <param name="ora">Az óra ezredmásodpercben (a MidiEngine stopperórája).</param>
        Public Sub New(kuldo As Action(Of Integer, Integer, Integer), ora As Func(Of Long))
            Me.kuldo = kuldo
            Me.ora = ora
        End Sub

        ''' <summary>Fut-e éppen a lejátszás.</summary>
        Public ReadOnly Property Fut As Boolean
            Get
                SyncLock zar
                    Return futBelso
                End SyncLock
            End Get
        End Property

        ''' <summary>A betöltött fájl hossza ezredmásodpercben.</summary>
        Public ReadOnly Property HosszMs As Double
            Get
                Return hosszMsBelso
            End Get
        End Property

        ''' <summary>
        ''' Az aktuális lejátszási pozíció ezredmásodpercben (0 .. HosszMs):
        ''' lejátszás közben az óra szerint, egyébként a megállított pozíció.
        ''' </summary>
        Public ReadOnly Property PozicioMs As Double
            Get
                SyncLock zar
                    Return PozicioBelso()
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Lejátszás közben az óra állása, ami a fájl 0. ezredmásodpercének
        ''' felel meg (tekeréskor változik); megállított állapotban Nothing.
        ''' A ráfelvétel (7. fázis) ehhez igazítja a mentett fájl elejét: a
        ''' felvett esemény ideje a fájlban = időbélyeg - FajlNullpontOraMs.
        ''' </summary>
        Public ReadOnly Property FajlNullpontOraMs As Long?
            Get
                SyncLock zar
                    If Not futBelso Then Return Nothing
                    Return kezdoOraMs
                End SyncLock
            End Get
        End Property

        ''' <summary>
        ''' Az óra állása a legutóbbi leálláskor (Stop, a fájl vége, hiba),
        ''' vagy Nothing, ha a lejátszás még nem futott. Az Automatikus
        ''' felvétel mód a szünetet legkorábban ettől számolja (a felhasználó
        ''' döntése, 2026-11-03: lejátszás alatt nincs vágás).
        ''' </summary>
        Public ReadOnly Property UtolsoLeallasOraMs As Long?
            Get
                SyncLock zar
                    Return leallasOraMs
                End SyncLock
            End Get
        End Property

        Private Function PozicioBelso() As Double
            If Not futBelso Then Return allasMs
            Return Math.Max(0, Math.Min(hosszMsBelso, CDbl(ora() - kezdoOraMs)))
        End Function

        ''' <summary>Van-e betöltött, lejátszható esemény.</summary>
        Public ReadOnly Property VanBetoltottEsemeny As Boolean
            Get
                Return esemenyek.Count > 0
            End Get
        End Property

        ''' <summary>Új eseménylista betöltése. Futó lejátszást előbb leállít.</summary>
        Public Sub Betoltes(lista As MidiEsemenyLista)
            Leallitas()
            SyncLock zar
                esemenyek = New List(Of MidiLejatszasiEsemeny)(lista.Esemenyek)
                hosszMsBelso = lista.HosszMs
                kovetkezo = 0
                allasMs = 0
            End SyncLock
        End Sub

        ''' <summary>
        ''' A lejátszás indítása a háttérszálon, az aktuális pozícióról (lásd
        ''' PozicioMs - alapból a fájl eleje, tekerés után a beállított pont).
        ''' </summary>
        Public Sub Inditas()
            If Fut OrElse esemenyek.Count = 0 Then Return
            Dim kezdoMs As Double
            SyncLock zar
                kezdoMs = allasMs
            End SyncLock
            Elokeszites(kezdoMs)
            leallitasJel.Reset()
            szal = New Thread(AddressOf Futtatas) With {
                .IsBackground = True,
                .Priority = ThreadPriority.AboveNormal,
                .Name = "MidiBill lejátszó"
            }
            szal.Start()
        End Sub

        ''' <summary>
        ''' A lejátszás előkészítése szál nélkül (a Lepes-sel együtt a tesztek
        ''' szimulált órájához; az Inditas is ezt hívja). Ha nem a fájl
        ''' elejéről indul, előbb felépíti a csatornák addigi állapotát (lásd
        ''' AllapotFelepitese).
        ''' </summary>
        Friend Sub Elokeszites(kezdoMs As Double)
            SyncLock zar
                Pozicionalas(kezdoMs)
                futBelso = True
            End SyncLock
        End Sub

        ''' <summary>
        ''' Tekerés a megadott pozícióra (ezredmásodperc, a fájl hosszára
        ''' szorítva). Megállított állapotban csak a következő lejátszás
        ''' kezdőpontját állítja. Lejátszás közben azonnal odaugrik: előbb CSAK
        ''' a lejátszás saját, épp szóló hangjait és pedálját engedi el (az élő
        ''' játék szól tovább - a felhasználó döntése, 2026-11-01), majd
        ''' felépíti a csatornák állapotát a célpontig, és onnan folytatja.
        ''' </summary>
        Public Sub Ugras(celMs As Double)
            SyncLock zar
                Dim cel As Double = Math.Max(0, Math.Min(hosszMsBelso, celMs))
                If Not futBelso Then
                    allasMs = cel
                    Return
                End If
                SajatHangokElengedese()
                Pozicionalas(cel)
            End SyncLock
        End Sub

        ''' <summary>
        ''' (A zár alatt hívandó.) A következő esemény és az óra beállítása a
        ''' kezdőpontra, és - ha az nem a fájl eleje - a csatornák állapotának
        ''' felépítése.
        ''' </summary>
        Private Sub Pozicionalas(kezdoMs As Double)
            kovetkezo = 0
            While kovetkezo < esemenyek.Count AndAlso esemenyek(kovetkezo).IdoMs < kezdoMs
                kovetkezo += 1
            End While
            If kovetkezo > 0 Then AllapotFelepitese(kovetkezo)
            kezdoOraMs = ora() - CLng(Math.Round(kezdoMs))
        End Sub

        ''' <summary>
        ''' A fájl közepéről induló lejátszás előtt kiküldi csatornánként az
        ''' addig érvényes állapotot - ahogy a zenei programok is teszik (a
        ''' felhasználó döntése, 2026-11-01). Enélkül egy, a fájl elején
        ''' beállított hangszín vagy hangerő középről indítva kimaradna.
        '''
        ''' Csatornánként, ebben a sorrendben:
        ''' 1. a legutóbbi Program Change - előtte a HOZZÁ tartozó Bank Select
        '''    (az a CC0 / CC32 érték, ami a Program Change pillanatában élt);
        ''' 2. minden vezérlő (CC) legutóbbi értéke, kivéve a csatorna-mód
        '''    üzeneteket (CC120-127: All Notes Off stb.) és az RPN/NRPN
        '''    adatbevitelt (CC6, 38, 96-101) - ha a bank az utolsó Program
        '''    Change óta változott, a CC0 / CC32 itt az új értékkel megy ki;
        ''' 3. az RPN/NRPN üzenetek (pl. a pitch bend tartomány) EREDETI
        '''    sorrendben, mert ezeknél a sorrend számít - a fájlokban ritkák;
        ''' 4. a legutóbbi pitch bend és channel pressure.
        ''' A hangokat (Note On) nem indítja újra: egy a kezdőpont előtt
        ''' leütött, még szóló hang a tekerés után már nem szól.
        ''' </summary>
        Private Sub AllapotFelepitese(eddig As Integer)
            For Each u As MidiUzenet In AllapotUzenetei(eddig)
                Kuldes(u.StatuszBajt, u.Adat1, u.Adat2)
            Next
        End Sub

        ''' <summary>
        ''' A ráfelvételhez (7. fázis): a csatornák állapota (lásd
        ''' AllapotFelepitese) annál a fájlbeli pontnál, ami a megadott
        ''' óraállásnak felel meg - azokból az eseményekből, amelyek addig már
        ''' ki is mentek. Ha a felvétel a lejátszott hangokat is rögzíti, ez
        ''' kerül a mentett fájl elejére, hogy a lejátszott rész a fájlban is
        ''' a helyes hangszínekkel szóljon. Megállított lejátszónál üres.
        ''' </summary>
        Public Function AllapotPillanatkep(oraMs As Long) As List(Of MidiUzenet)
            SyncLock zar
                If Not futBelso Then Return New List(Of MidiUzenet)()
                Dim fajlIdoMs As Double = oraMs - kezdoOraMs
                Dim eddig As Integer = 0
                While eddig < kovetkezo AndAlso esemenyek(eddig).IdoMs <= fajlIdoMs
                    eddig += 1
                End While
                Return AllapotUzenetei(eddig)
            End SyncLock
        End Function

        ''' <summary>Az állapot-felépítés üzenetei az első "eddig" esemény alapján (a sorrendet lásd AllapotFelepitese).</summary>
        Private Function AllapotUzenetei(eddig As Integer) As List(Of MidiUzenet)
            Dim kimenet As New List(Of MidiUzenet)
            Dim program(15) As Integer
            Dim pcMsb(15) As Integer
            Dim pcLsb(15) As Integer
            Dim cc(15, 127) As Integer
            Dim pitchBend(15) As Integer
            Dim nyomas(15) As Integer
            For c As Integer = 0 To 15
                program(c) = -1 : pitchBend(c) = -1 : nyomas(c) = -1
                For n As Integer = 0 To 127
                    cc(c, n) = -1
                Next
            Next
            Dim rpn As New List(Of MidiLejatszasiEsemeny)

            For i As Integer = 0 To eddig - 1
                Dim e As MidiLejatszasiEsemeny = esemenyek(i)
                Dim c As Integer = e.StatuszBajt And &HF
                Select Case e.StatuszBajt And &HF0
                    Case &HC0
                        program(c) = e.Adat1
                        pcMsb(c) = Math.Max(0, cc(c, CsatornaUzenetek.BANK_SELECT_MSB))
                        pcLsb(c) = Math.Max(0, cc(c, CsatornaUzenetek.BANK_SELECT_LSB))
                    Case &HB0
                        If RpnVezerloE(e.Adat1) Then
                            rpn.Add(e)
                        ElseIf e.Adat1 < CSATORNA_MOD_ELSO Then
                            cc(c, e.Adat1) = e.Adat2
                        End If
                    Case &HE0
                        pitchBend(c) = e.Adat1 Or (e.Adat2 << 7)
                    Case &HD0
                        nyomas(c) = e.Adat1
                End Select
            Next

            For c As Integer = 0 To 15
                If program(c) >= 0 Then
                    kimenet.Add(New MidiUzenet(&HB0 Or c, CsatornaUzenetek.BANK_SELECT_MSB, pcMsb(c), 2))
                    kimenet.Add(New MidiUzenet(&HB0 Or c, CsatornaUzenetek.BANK_SELECT_LSB, pcLsb(c), 2))
                    kimenet.Add(New MidiUzenet(&HC0 Or c, program(c), 0, 1))
                End If
                For n As Integer = 0 To CSATORNA_MOD_ELSO - 1
                    If cc(c, n) < 0 Then Continue For
                    If program(c) >= 0 AndAlso
                       ((n = CsatornaUzenetek.BANK_SELECT_MSB AndAlso cc(c, n) = pcMsb(c)) OrElse
                        (n = CsatornaUzenetek.BANK_SELECT_LSB AndAlso cc(c, n) = pcLsb(c))) Then Continue For
                    kimenet.Add(New MidiUzenet(&HB0 Or c, n, cc(c, n), 2))
                Next
                For Each e As MidiLejatszasiEsemeny In rpn
                    If (e.StatuszBajt And &HF) = c Then kimenet.Add(New MidiUzenet(e.StatuszBajt, e.Adat1, e.Adat2, 2))
                Next
                If pitchBend(c) >= 0 Then kimenet.Add(New MidiUzenet(&HE0 Or c, pitchBend(c) And &H7F, pitchBend(c) >> 7, 2))
                If nyomas(c) >= 0 Then kimenet.Add(New MidiUzenet(&HD0 Or c, nyomas(c), 0, 1))
            Next
            Return kimenet
        End Function

        ''' <summary>Az első csatorna-mód vezérlő (CC120 All Sound Off ... CC127) - ezek nem állapotok.</summary>
        Private Const CSATORNA_MOD_ELSO As Integer = 120

        ''' <summary>RPN / NRPN kiválasztás és adatbevitel (CC6, 38, 96-101) - ezeknél a sorrend számít.</summary>
        Private Shared Function RpnVezerloE(vezerlo As Integer) As Boolean
            Return vezerlo = 6 OrElse vezerlo = 38 OrElse (vezerlo >= 96 AndAlso vezerlo <= 101)
        End Function

        ''' <summary>
        ''' Egy ütem: kiküldi az órához képest esedékes eseményeket. A
        ''' visszatérési érték, hány ms múlva esedékes a következő esemény,
        ''' vagy -1, ha vége (a fájl végén a lejátszás magától leáll, és a
        ''' lógó hangok / a pedál elengedése is itt történik).
        ''' </summary>
        Friend Function Lepes(oraMs As Long) As Long
            SyncLock zar
                If Not futBelso Then Return -1
                Dim relativMs As Double = oraMs - kezdoOraMs
                While kovetkezo < esemenyek.Count AndAlso esemenyek(kovetkezo).IdoMs <= relativMs
                    Dim e As MidiLejatszasiEsemeny = esemenyek(kovetkezo)
                    Kuldes(e.StatuszBajt, e.Adat1, e.Adat2)
                    kovetkezo += 1
                End While
                If kovetkezo >= esemenyek.Count Then
                    SajatHangokElengedese()
                    futBelso = False
                    allasMs = 0   ' a fájl vége után a következő lejátszás az elejéről indul
                    leallasOraMs = oraMs
                    Return -1
                End If
                Return Math.Max(0L, CLng(Math.Ceiling(esemenyek(kovetkezo).IdoMs - relativMs)))
            End SyncLock
        End Function

        ''' <summary>
        ''' A lejátszás leállítása: a szál megáll, és CSAK a lejátszás által
        ''' megszólaltatott hangok (+ a pedál) engedődnek el.
        ''' </summary>
        Public Sub Leallitas()
            Dim futoSzal As Thread = szal
            If futoSzal IsNot Nothing Then
                leallitasJel.Set()
                If futoSzal IsNot Thread.CurrentThread Then futoSzal.Join(2000)
                szal = Nothing
            End If
            SyncLock zar
                If futBelso Then
                    allasMs = PozicioBelso()   ' a Stop ott hagyja a pozíciót, ahol megállt
                    SajatHangokElengedese()
                    futBelso = False
                    leallasOraMs = ora()
                End If
            End SyncLock
        End Sub

        Private Sub Futtatas()
            Dim termeszetesVege As Boolean = False
            Dim felbontasBeallitva As Boolean = IdozitoFelbontas(True)
            Try
                Do
                    Dim varakozas As Long = Lepes(ora())
                    If varakozas < 0 Then
                        termeszetesVege = True
                        Exit Do
                    End If
                    If leallitasJel.WaitOne(CInt(Math.Min(varakozas, MAX_VARAKOZAS_MS))) Then Exit Do
                Loop
            Catch ex As Exception
                ' A lejátszás hibája (pl. a kimenet váratlan lezárása) ne döntse
                ' le a programot - a lejátszás egyszerűen véget ér.
                SyncLock zar
                    futBelso = False
                    allasMs = 0
                    leallasOraMs = ora()
                End SyncLock
                termeszetesVege = True
            Finally
                If felbontasBeallitva Then IdozitoFelbontas(False)
            End Try
            If termeszetesVege Then RaiseEvent Befejezodott()
        End Sub

        ''' <summary>Kiküldés + a saját hangok és a pedál nyilvántartása.</summary>
        Private Sub Kuldes(statuszBajt As Integer, adat1 As Integer, adat2 As Integer)
            kuldo(statuszBajt, adat1, adat2)
            Dim csatorna As Integer = statuszBajt And &HF
            Select Case statuszBajt And &HF0
                Case &H90
                    If adat2 > 0 Then
                        szoloHangok(csatorna, adat1) += 1
                    ElseIf szoloHangok(csatorna, adat1) > 0 Then
                        szoloHangok(csatorna, adat1) -= 1
                    End If
                Case &H80
                    If szoloHangok(csatorna, adat1) > 0 Then szoloHangok(csatorna, adat1) -= 1
                Case &HB0
                    If adat1 = SUSTAIN_PEDAL Then pedalLent(csatorna) = (adat2 >= PEDAL_LENT_HATAR)
            End Select
        End Sub

        ''' <summary>
        ''' A lejátszás által lenyomva hagyott hangok és pedál elengedése -
        ''' célzott Note Off-okkal, hogy az élő játék szóljon tovább.
        ''' </summary>
        Private Sub SajatHangokElengedese()
            For csatorna As Integer = 0 To 15
                If pedalLent(csatorna) Then
                    kuldo(&HB0 Or csatorna, SUSTAIN_PEDAL, 0)
                    pedalLent(csatorna) = False
                End If
                For hang As Integer = 0 To 127
                    While szoloHangok(csatorna, hang) > 0
                        kuldo(&H80 Or csatorna, hang, 0)
                        szoloHangok(csatorna, hang) -= 1
                    End While
                Next
            Next
        End Sub

        ' --- A Windows időzítő felbontása (winmm) ---

        <DllImport("winmm.dll")>
        Private Shared Function timeBeginPeriod(uPeriod As UInteger) As UInteger
        End Function

        <DllImport("winmm.dll")>
        Private Shared Function timeEndPeriod(uPeriod As UInteger) As UInteger
        End Function

        ''' <summary>
        ''' 1 ms-os időzítő-felbontás be/ki a lejátszás idejére. Ha nem
        ''' elérhető (pl. nem Windows alatt futó teszt), a lejátszás a
        ''' rendszer alapfelbontásával megy tovább.
        ''' </summary>
        Private Shared Function IdozitoFelbontas(bekapcsolas As Boolean) As Boolean
            Try
                If bekapcsolas Then
                    timeBeginPeriod(1)
                Else
                    timeEndPeriod(1)
                End If
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        Public Sub Dispose() Implements IDisposable.Dispose
            Leallitas()
            leallitasJel.Dispose()
        End Sub

    End Class

End Namespace

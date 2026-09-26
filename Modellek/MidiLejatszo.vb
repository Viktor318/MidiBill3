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
            End SyncLock
        End Sub

        ''' <summary>
        ''' A lejátszás indítása a megadott pontról (alapból a fájl elejéről)
        ''' a háttérszálon. A kezdőpont a 6. fázis tekeréséhez kell.
        ''' </summary>
        Public Sub Inditas(Optional kezdoMs As Double = 0)
            If Fut OrElse esemenyek.Count = 0 Then Return
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
        ''' szimulált órájához; az Inditas is ezt hívja).
        ''' </summary>
        Friend Sub Elokeszites(kezdoMs As Double)
            SyncLock zar
                kovetkezo = 0
                While kovetkezo < esemenyek.Count AndAlso esemenyek(kovetkezo).IdoMs < kezdoMs
                    kovetkezo += 1
                End While
                kezdoOraMs = ora() - CLng(Math.Round(kezdoMs))
                futBelso = True
            End SyncLock
        End Sub

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
                    SajatHangokElengedese()
                    futBelso = False
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

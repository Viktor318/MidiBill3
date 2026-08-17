Imports NAudio.Midi
Imports Modellek

Namespace Modellek

    ''' <summary>
    ''' MIDI be- és kimenet kezelése NAudio.Midi-vel.
    ''' Ez az osztály NEM tud semmit a UI-ról (Form, vezérlők) - csak
    ''' eseményeket told ki, amikre a MainForm iratkozik fel.
    ''' A bejövő MIDI-üzenetek NAudio háttérszálon érkeznek, ezért a
    ''' feliratkozónak (MainForm) Invoke-kal kell a UI-szálra váltania,
    ''' mielőtt vezérlőt módosítana.
    ''' </summary>
    Public Class MidiEngine
        Implements IDisposable

        Private Const NOTE_ON_STATUS As Integer = &H90
        Private Const NOTE_OFF_STATUS As Integer = &H80
        Private Const POLY_PRESSURE_STATUS As Integer = &HA0

        Private midiKimenet As MidiOut = Nothing
        Private midiBemenet As MidiIn = Nothing

        ' Ugyanaz a nyilvántartás, mint a VB6-os NoteOn(1 To 4, 0 To 127):
        ' melyik hang szól ténylegesen az egyes KIMENETI csatornákon.
        Private noteOn(3, 127) As Boolean

        ' A VB6-os HeldNotes(0 To 127) megfelelője: melyik FIZIKAI billentyű
        ' van lenyomva (0=nincs, egyébként a velocity), a csatornák be/ki
        ' állapotától FÜGGETLENÜL frissül.
        Private heldNotes(127) As Integer

        ' A VB6-os MidiOutHibaJelezve megfelelője: hogy a kimeneti hiba
        ' csak egyszer jelezzen, ne minden üzenetnél.
        Private kimenetiHibaJelezve As Boolean = False

        ''' <summary>Melyik csatornák aktívak (0-3 index, a J1(0..3) megfelelője)</summary>
        Public Property CsatornaAktiv(index As Integer) As Boolean
            Get
                Return csatornaAktivTomb(index)
            End Get
            Set(value As Boolean)
                csatornaAktivTomb(index) = value
            End Set
        End Property
        Private csatornaAktivTomb(3) As Boolean

        ''' <summary>
        ''' Szól-e az adott csatorna a Split ponttól ALUL. Egyszerű, mellékhatás
        ''' nélküli property (induló szinkronizáláshoz) - az interaktív
        ''' be-/kikapcsoláshoz lásd AlulValtas.
        ''' </summary>
        Public Property CsatornaAlul(index As Integer) As Boolean
            Get
                Return csatornaAlulTomb(index)
            End Get
            Set(value As Boolean)
                csatornaAlulTomb(index) = value
            End Set
        End Property
        Private csatornaAlulTomb(3) As Boolean

        ''' <summary>Szól-e az adott csatorna a Split ponttól FELÜL (lásd CsatornaAlul megjegyzését)</summary>
        Public Property CsatornaFelul(index As Integer) As Boolean
            Get
                Return csatornaFelulTomb(index)
            End Get
            Set(value As Boolean)
                csatornaFelulTomb(index) = value
            End Set
        End Property
        Private csatornaFelulTomb(3) As Boolean

        ''' <summary>Split pont: MIDI hangszám (0-127), amitől a "Felül" zóna kezdődik. Alapértelmezett: 60 (C4)</summary>
        Public Property SplitPont As Integer = 60

        ''' <summary>Bejövő MIDI üzenet érkezett (a UI-szálra váltás a feliratkozó feladata!)</summary>
        Public Event MidiUzenetErkezett(uzenet As Integer, adat1 As Integer, adat2 As Integer)

        ''' <summary>Kimeneti hiba történt (pl. eszköz eltűnt) - csak egyszer tüzel, amíg újra nem nyitjuk sikeresen</summary>
        Public Event KimenetiHiba(hibaUzenet As String)

        Public Sub New()
            For c As Integer = 0 To 3
                csatornaAktivTomb(c) = False
                csatornaAlulTomb(c) = False
                csatornaFelulTomb(c) = False
            Next
        End Sub

        ''' <summary>Elérhető MIDI bemeneti eszközök nevei</summary>
        Public Shared Function BemenetiEszkozok() As List(Of String)
            Dim lista As New List(Of String)
            For i As Integer = 0 To MidiIn.NumberOfDevices - 1
                lista.Add(MidiIn.DeviceInfo(i).ProductName)
            Next
            Return lista
        End Function

        ''' <summary>Elérhető MIDI kimeneti eszközök nevei</summary>
        Public Shared Function KimenetiEszkozok() As List(Of String)
            Dim lista As New List(Of String)
            For i As Integer = 0 To MidiOut.NumberOfDevices - 1
                lista.Add(MidiOut.DeviceInfo(i).ProductName)
            Next
            Return lista
        End Function

        ''' <summary>MIDI bemeneti eszköz megnyitása index alapján. Kivételt dob hiba esetén.</summary>
        Public Sub BemenetMegnyitas(eszkozIndex As Integer)
            BemenetBezaras()

            ' Új bemeneti eszközön a régi "lenyomott" állapot már nem releváns
            Array.Clear(heldNotes, 0, heldNotes.Length)

            midiBemenet = New MidiIn(eszkozIndex)
            AddHandler midiBemenet.MessageReceived, AddressOf Bemenet_UzenetErkezett
            AddHandler midiBemenet.ErrorReceived, AddressOf Bemenet_HibaErkezett
            midiBemenet.Start()
        End Sub

        Public Sub BemenetBezaras()
            If midiBemenet IsNot Nothing Then
                Try
                    midiBemenet.Stop()
                    RemoveHandler midiBemenet.MessageReceived, AddressOf Bemenet_UzenetErkezett
                    RemoveHandler midiBemenet.ErrorReceived, AddressOf Bemenet_HibaErkezett
                    midiBemenet.Dispose()
                Catch
                    ' Ha az eszköz már nem elérhető (pl. kihúzták), ne akadjon el a bezárás
                End Try
                midiBemenet = Nothing
            End If
        End Sub

        ''' <summary>MIDI kimeneti eszköz megnyitása index alapján. Kivételt dob hiba esetén.</summary>
        Public Sub KimenetMegnyitas(eszkozIndex As Integer)
            KimenetBezaras()
            midiKimenet = New MidiOut(eszkozIndex)
            kimenetiHibaJelezve = False ' Új sikeres nyitás után újra figyelmeztethetünk
            Array.Clear(noteOn, 0, noteOn.Length) ' Új porton biztosan nem szól semmi
        End Sub

        Public Sub KimenetBezaras()
            If midiKimenet IsNot Nothing Then
                Try
                    midiKimenet.Dispose()
                Catch
                End Try
                midiKimenet = Nothing
            End If
        End Sub

        ''' <summary>
        ''' A NAudio háttérszáláról hívódik! Csak az eseményt adjuk tovább,
        ''' a feldolgozás (UI-frissítés, fan-out) a feliratkozó (MainForm)
        ''' dolga, Invoke-on keresztül.
        ''' </summary>
        Private Sub Bemenet_UzenetErkezett(sender As Object, e As MidiInMessageEventArgs)
            Dim raw As Integer = e.RawMessage
            Dim statuszBajt As Integer = raw And &HFF
            Dim adat1 As Integer = (raw >> 8) And &HFF
            Dim adat2 As Integer = (raw >> 16) And &HFF

            RaiseEvent MidiUzenetErkezett(statuszBajt, adat1, adat2)
        End Sub

        Private Sub Bemenet_HibaErkezett(sender As Object, e As MidiInMessageEventArgs)
            ' Csendben elnyeljük, ahogy a VB6-os BemenetHiba is tette -
            ' egy hibás/félreértelmezett bemeneti üzenet ne állítsa meg a programot.
        End Sub

        ''' <summary>
        ''' A VB6-os Send_MIDI megfelelője: a bejövő MIDI-üzenetet szétosztja
        ''' a 4 aktív kimeneti csatornára - Split pont szerinti zóna-szűréssel
        ''' a hangmagassághoz kötött üzeneteknél (Note On/Off, Poly Pressure),
        ''' vagy - rendszerüzenet esetén - változatlanul, egyszer küldi tovább.
        ''' </summary>
        Public Sub UzenetKuldes(statuszBajt As Integer, adat1 As Integer, adat2 As Integer)

            If statuszBajt >= &H80 And statuszBajt < &HF0 Then
                ' Valódi "channel voice" üzenet - az alsó 4 bit hordozza a
                ' csatornaszámot, itt mindig csatorna 0-ra normálva érkezik be.

                Dim tenylegesNoteOn As Boolean = (statuszBajt = NOTE_ON_STATUS And adat2 > 0)
                Dim tenylegesNoteOff As Boolean = (statuszBajt = NOTE_ON_STATUS And adat2 = 0) OrElse statuszBajt = NOTE_OFF_STATUS

                ' HeldNotes a csatornák be/ki állapotától FÜGGETLENÜL frissül
                If tenylegesNoteOn Then
                    heldNotes(adat1) = adat2
                ElseIf tenylegesNoteOff Then
                    heldNotes(adat1) = 0
                End If

                Dim hangUzenet As Boolean = HangUzenetE(statuszBajt)
                Dim felsoZona As Boolean = adat1 >= SplitPont

                For c As Integer = 0 To 3
                    If Not csatornaAktivTomb(c) Then Continue For

                    Dim kuldheto As Boolean
                    If tenylegesNoteOff Then
                        ' Note Off: MINDIG ahhoz a csatornához küldjük, amelyiken
                        ' ténylegesen szól ez a hang (a nyilvántartás alapján) -
                        ' így split pont váltás vagy zóna-átállítás közben sem
                        ' maradhat "beragadt" hang egyetlen csatornán sem.
                        kuldheto = noteOn(c, adat1)
                    ElseIf hangUzenet Then
                        ' Note On / Poly Pressure: az AKTUÁLIS zóna-beállítás dönt
                        kuldheto = If(felsoZona, csatornaFelulTomb(c), csatornaAlulTomb(c))
                    Else
                        ' Nem hangmagassághoz kötött üzenet (Controller Change,
                        ' Program Change, Pitch Bend stb.) - minden aktív
                        ' csatornára megy, a Split pont ezt nem érinti.
                        kuldheto = True
                    End If

                    If kuldheto Then
                        KuldesEgyCsatornara(statuszBajt + c, adat1, adat2)
                        HangNyilvantartasFrissit(c, statuszBajt, adat1, adat2)
                    End If
                Next
            Else
                ' Rendszerüzenet - változatlanul, egyszer
                KuldesEgyCsatornara(statuszBajt, adat1, adat2)
            End If

        End Sub

        Private Function HangUzenetE(statuszBajt As Integer) As Boolean
            Return statuszBajt = NOTE_ON_STATUS OrElse statuszBajt = NOTE_OFF_STATUS OrElse statuszBajt = POLY_PRESSURE_STATUS
        End Function

        ''' <summary>Egyetlen alacsonyszintű MIDI üzenet kiküldése, hibatűrő módon</summary>
        Private Sub KuldesEgyCsatornara(statuszBajt As Integer, adat1 As Integer, adat2 As Integer)
            If midiKimenet Is Nothing Then Return
            Try
                Dim uzenet As Integer = statuszBajt Or (adat1 << 8) Or (adat2 << 16)
                midiKimenet.Send(uzenet)
            Catch ex As Exception
                If Not kimenetiHibaJelezve Then
                    kimenetiHibaJelezve = True
                    RaiseEvent KimenetiHiba("A MIDI kimeneti eszköz nem érhető el (lehet, hogy időközben eltávolították). Válassz újra egy eszközt.")
                End If
            End Try
        End Sub

        Private Sub HangNyilvantartasFrissit(csatornaIndex As Integer, statuszBajt As Integer, noteNum As Integer, velocity As Integer)
            If statuszBajt = NOTE_ON_STATUS And velocity > 0 Then
                noteOn(csatornaIndex, noteNum) = True
            ElseIf (statuszBajt = NOTE_ON_STATUS And velocity = 0) OrElse statuszBajt = NOTE_OFF_STATUS Then
                noteOn(csatornaIndex, noteNum) = False
            End If
        End Sub

        ''' <summary>
        ''' A VB6-os Turn_Notes_Off megfelelője. FullSweep=True esetén (pánikgomb)
        ''' mind a 128 hangot lekapcsolja, függetlenül a nyilvántartástól.
        ''' </summary>
        Public Sub HangokKikapcsolasa(csatornaSzam As Integer, Optional teljesSepres As Boolean = False)
            Dim csatornaIndex As Integer = csatornaSzam - 1

            KuldesEgyCsatornara(&HB0 + csatornaIndex, 64, 0) ' CONTROLLER_CHANGE, DAMPER_PEDAL

            If teljesSepres Then
                For n As Integer = 0 To 127
                    KuldesEgyCsatornara(NOTE_ON_STATUS + csatornaIndex, n, 0)
                    noteOn(csatornaIndex, n) = False
                Next
            Else
                For n As Integer = 0 To 127
                    If noteOn(csatornaIndex, n) Then
                        KuldesEgyCsatornara(NOTE_ON_STATUS + csatornaIndex, n, 0)
                        noteOn(csatornaIndex, n) = False
                    End If
                Next
            End If
        End Sub

        ''' <summary>
        ''' Egy adott zóna (alul vagy felül) hangjainak lekapcsolása egy
        ''' csatornán - csak azokat, amik ténylegesen szólnak ott ÉS az
        ''' adott zónába esnek. Az Alul/Felul checkbox kikapcsolásakor hívjuk.
        ''' </summary>
        Private Sub ZonaHangokKikapcsolasa(csatornaSzam As Integer, felsoZona As Boolean)
            Dim csatornaIndex As Integer = csatornaSzam - 1
            For n As Integer = 0 To 127
                If ZonaIllik(n, felsoZona) AndAlso noteOn(csatornaIndex, n) Then
                    KuldesEgyCsatornara(NOTE_ON_STATUS + csatornaIndex, n, 0)
                    noteOn(csatornaIndex, n) = False
                End If
            Next
        End Sub

        ''' <summary>
        ''' Egy adott zóna (alul vagy felül) jelenleg lenyomott hangjainak
        ''' megszólaltatása egy csatornán - az Alul/Felül checkbox
        ''' bekapcsolásakor, illetve a csatorna aktiválásakor hívjuk.
        ''' </summary>
        Private Sub ZonaHangokMegszolaltatasa(csatornaSzam As Integer, felsoZona As Boolean)
            Dim csatornaIndex As Integer = csatornaSzam - 1
            For n As Integer = 0 To 127
                If ZonaIllik(n, felsoZona) AndAlso heldNotes(n) > 0 Then
                    KuldesEgyCsatornara(NOTE_ON_STATUS + csatornaIndex, n, heldNotes(n))
                    noteOn(csatornaIndex, n) = True
                End If
            Next
        End Sub

        Private Function ZonaIllik(hang As Integer, felsoZona As Boolean) As Boolean
            If felsoZona Then
                Return hang >= SplitPont
            Else
                Return hang < SplitPont
            End If
        End Function

        ''' <summary>
        ''' A VB6-os Sound_Held_Notes megfelelője: egy most bekapcsolt csatornán
        ''' megszólaltatja az éppen lenyomva tartott hangokat - de csak azokban
        ''' a zónákban, amik a csatornán aktuálisan be vannak kapcsolva
        ''' (Alul és/vagy Felül).
        ''' </summary>
        Public Sub LenyomottHangokMegszolaltatasa(csatornaSzam As Integer)
            Dim csatornaIndex As Integer = csatornaSzam - 1
            If csatornaAlulTomb(csatornaIndex) Then ZonaHangokMegszolaltatasa(csatornaSzam, False)
            If csatornaFelulTomb(csatornaIndex) Then ZonaHangokMegszolaltatasa(csatornaSzam, True)
        End Sub

        ''' <summary>
        ''' Az "Alul" zóna interaktív be-/kikapcsolása egy csatornán (a
        ''' HangszerPanel chkAlul checkbox-ából hívva). Kikapcsoláskor
        ''' elnémítja az ott ténylegesen szóló hangokat, bekapcsoláskor
        ''' megszólaltatja az adott zónában jelenleg lenyomott hangokat.
        ''' </summary>
        Public Sub AlulValtas(csatornaSzam As Integer, ujErtek As Boolean)
            Dim csatornaIndex As Integer = csatornaSzam - 1
            Dim regi As Boolean = csatornaAlulTomb(csatornaIndex)
            csatornaAlulTomb(csatornaIndex) = ujErtek

            If Not csatornaAktivTomb(csatornaIndex) Then Return

            If regi AndAlso Not ujErtek Then
                ZonaHangokKikapcsolasa(csatornaSzam, False)
            ElseIf Not regi AndAlso ujErtek Then
                ZonaHangokMegszolaltatasa(csatornaSzam, False)
            End If
        End Sub

        ''' <summary>A "Felül" zóna interaktív be-/kikapcsolása (lásd AlulValtas megjegyzését)</summary>
        Public Sub FelulValtas(csatornaSzam As Integer, ujErtek As Boolean)
            Dim csatornaIndex As Integer = csatornaSzam - 1
            Dim regi As Boolean = csatornaFelulTomb(csatornaIndex)
            csatornaFelulTomb(csatornaIndex) = ujErtek

            If Not csatornaAktivTomb(csatornaIndex) Then Return

            If regi AndAlso Not ujErtek Then
                ZonaHangokKikapcsolasa(csatornaSzam, True)
            ElseIf Not regi AndAlso ujErtek Then
                ZonaHangokMegszolaltatasa(csatornaSzam, True)
            End If
        End Sub

        ''' <summary>Program Change küldése egy csatornára</summary>
        Public Sub ProgramValtas(csatornaSzam As Integer, hangszerIndex As Integer)
            KuldesEgyCsatornara(&HC0 + (csatornaSzam - 1), hangszerIndex, 0)
        End Sub

        ''' <summary>Controller Change küldése egy csatornára (hangerő, pan, vibrato mind ezen megy)</summary>
        Public Sub ControllerValtas(csatornaSzam As Integer, controllerSzam As Integer, ertek As Integer)
            KuldesEgyCsatornara(&HB0 + (csatornaSzam - 1), controllerSzam, ertek)
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            BemenetBezaras()
            KimenetBezaras()
        End Sub

    End Class

End Namespace
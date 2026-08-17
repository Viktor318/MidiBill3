Imports NAudio.Midi

Namespace Modellek

    Public Class MidiEngine
        Implements IDisposable

        Private Const NOTE_ON_STATUS As Integer = &H90
        Private Const NOTE_OFF_STATUS As Integer = &H80
        Private Const POLY_PRESSURE_STATUS As Integer = &HA0
        Private Const DOB_CSATORNA_INDEX As Integer = 9

        Private midiKimenet As MidiOut = Nothing
        Private midiBemenet As MidiIn = Nothing

        Private noteOn(15, 127) As Boolean
        Private kimenoHang(15, 127) As Integer
        Private heldNotes(127) As Integer
        Private kimenetiHibaJelezve As Boolean = False

        Public Property CsatornaAktiv(index As Integer) As Boolean
            Get
                Return csatornaAktivTomb(index)
            End Get
            Set(value As Boolean)
                csatornaAktivTomb(index) = If(index = DOB_CSATORNA_INDEX, False, value)
            End Set
        End Property
        Private csatornaAktivTomb(15) As Boolean

        Public Property CsatornaAlul(index As Integer) As Boolean
            Get
                Return csatornaAlulTomb(index)
            End Get
            Set(value As Boolean)
                csatornaAlulTomb(index) = value
            End Set
        End Property
        Private csatornaAlulTomb(15) As Boolean

        Public Property CsatornaFelul(index As Integer) As Boolean
            Get
                Return csatornaFelulTomb(index)
            End Get
            Set(value As Boolean)
                csatornaFelulTomb(index) = value
            End Set
        End Property
        Private csatornaFelulTomb(15) As Boolean

        Public Property SplitPont As Integer = 60
        Public Property Transpozicio As Integer = 0

        Public Event MidiUzenetErkezett(uzenet As Integer, adat1 As Integer, adat2 As Integer)
        Public Event KimenetiHiba(hibaUzenet As String)

        Public Sub New()
            For c As Integer = 0 To 15
                csatornaAktivTomb(c) = False
                csatornaAlulTomb(c) = False
                csatornaFelulTomb(c) = False
            Next
        End Sub

        Public Shared Function BemenetiEszkozok() As List(Of String)
            Dim lista As New List(Of String)
            For i As Integer = 0 To MidiIn.NumberOfDevices - 1
                lista.Add(MidiIn.DeviceInfo(i).ProductName)
            Next
            Return lista
        End Function

        Public Shared Function KimenetiEszkozok() As List(Of String)
            Dim lista As New List(Of String)
            For i As Integer = 0 To MidiOut.NumberOfDevices - 1
                lista.Add(MidiOut.DeviceInfo(i).ProductName)
            Next
            Return lista
        End Function

        Public Sub BemenetMegnyitas(eszkozIndex As Integer)
            BemenetBezaras()
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
                End Try
                midiBemenet = Nothing
            End If
        End Sub

        Public Sub KimenetMegnyitas(eszkozIndex As Integer)
            KimenetBezaras()
            midiKimenet = New MidiOut(eszkozIndex)
            kimenetiHibaJelezve = False
            Array.Clear(noteOn, 0, noteOn.Length)
            Array.Clear(kimenoHang, 0, kimenoHang.Length)
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

        Private Sub Bemenet_UzenetErkezett(sender As Object, e As MidiInMessageEventArgs)
            Dim raw As Integer = e.RawMessage
            Dim statuszBajt As Integer = raw And &HFF
            Dim adat1 As Integer = (raw >> 8) And &HFF
            Dim adat2 As Integer = (raw >> 16) And &HFF

            RaiseEvent MidiUzenetErkezett(statuszBajt, adat1, adat2)
        End Sub

        Private Sub Bemenet_HibaErkezett(sender As Object, e As MidiInMessageEventArgs)
        End Sub

        Public Sub UzenetKuldes(statuszBajt As Integer, adat1 As Integer, adat2 As Integer)

            If statuszBajt >= &H80 And statuszBajt < &HF0 Then

                Dim tenylegesNoteOn As Boolean = (statuszBajt = NOTE_ON_STATUS And adat2 > 0)
                Dim tenylegesNoteOff As Boolean = (statuszBajt = NOTE_ON_STATUS And adat2 = 0) OrElse statuszBajt = NOTE_OFF_STATUS

                If tenylegesNoteOn Then
                    heldNotes(adat1) = adat2
                ElseIf tenylegesNoteOff Then
                    heldNotes(adat1) = 0
                End If

                Dim hangUzenet As Boolean = HangUzenetE(statuszBajt)
                Dim felsoZona As Boolean = adat1 >= SplitPont

                For c As Integer = 0 To 15
                    If Not csatornaAktivTomb(c) Then Continue For

                    If tenylegesNoteOff Then
                        If noteOn(c, adat1) Then
                            KuldesUzenetCsatornara(c, statuszBajt, kimenoHang(c, adat1), adat2)
                            noteOn(c, adat1) = False
                        End If
                    ElseIf hangUzenet Then
                        Dim zonaIlleszkedik As Boolean = If(felsoZona, csatornaFelulTomb(c), csatornaAlulTomb(c))
                        If zonaIlleszkedik Then
                            Dim kimeno As Integer = adat1 + Transpozicio
                            If ErvenyesHangE(kimeno) Then
                                KuldesUzenetCsatornara(c, statuszBajt, kimeno, adat2)
                                If tenylegesNoteOn Then
                                    noteOn(c, adat1) = True
                                    kimenoHang(c, adat1) = kimeno
                                End If
                            End If
                        End If
                    Else
                        KuldesUzenetCsatornara(c, statuszBajt, adat1, adat2)
                    End If
                Next
            Else
                KuldesEgyCsatornara(statuszBajt, adat1, adat2)
            End If

        End Sub

        Private Sub KuldesUzenetCsatornara(csatornaIndex As Integer, statuszBajtAlap As Integer, adat1 As Integer, adat2 As Integer)
            KuldesEgyCsatornara((statuszBajtAlap And &HF0) + csatornaIndex, adat1, adat2)
        End Sub

        Private Function HangUzenetE(statuszBajt As Integer) As Boolean
            Return statuszBajt = NOTE_ON_STATUS OrElse statuszBajt = NOTE_OFF_STATUS OrElse statuszBajt = POLY_PRESSURE_STATUS
        End Function

        Private Function ErvenyesHangE(hang As Integer) As Boolean
            Return hang >= 0 AndAlso hang <= 127
        End Function

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

        Public Sub HangokKikapcsolasa(csatornaSzam As Integer, Optional teljesSepres As Boolean = False)
            Dim csatornaIndex As Integer = csatornaSzam - 1

            KuldesEgyCsatornara(&HB0 + csatornaIndex, 64, 0)

            If teljesSepres Then
                For n As Integer = 0 To 127
                    KuldesEgyCsatornara(NOTE_ON_STATUS + csatornaIndex, n, 0)
                Next
                For n As Integer = 0 To 127
                    noteOn(csatornaIndex, n) = False
                Next
            Else
                For n As Integer = 0 To 127
                    If noteOn(csatornaIndex, n) Then
                        KuldesEgyCsatornara(NOTE_ON_STATUS + csatornaIndex, kimenoHang(csatornaIndex, n), 0)
                        noteOn(csatornaIndex, n) = False
                    End If
                Next
            End If
        End Sub

        Private Sub ZonaHangokKikapcsolasa(csatornaSzam As Integer, felsoZona As Boolean)
            Dim csatornaIndex As Integer = csatornaSzam - 1
            For n As Integer = 0 To 127
                If ZonaIllik(n, felsoZona) AndAlso noteOn(csatornaIndex, n) Then
                    KuldesEgyCsatornara(NOTE_ON_STATUS + csatornaIndex, kimenoHang(csatornaIndex, n), 0)
                    noteOn(csatornaIndex, n) = False
                End If
            Next
        End Sub

        Private Sub ZonaHangokMegszolaltatasa(csatornaSzam As Integer, felsoZona As Boolean)
            Dim csatornaIndex As Integer = csatornaSzam - 1
            For n As Integer = 0 To 127
                If ZonaIllik(n, felsoZona) AndAlso heldNotes(n) > 0 Then
                    Dim kimeno As Integer = n + Transpozicio
                    If ErvenyesHangE(kimeno) Then
                        KuldesEgyCsatornara(NOTE_ON_STATUS + csatornaIndex, kimeno, heldNotes(n))
                        noteOn(csatornaIndex, n) = True
                        kimenoHang(csatornaIndex, n) = kimeno
                    End If
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

        Public Sub LenyomottHangokMegszolaltatasa(csatornaSzam As Integer)
            Dim csatornaIndex As Integer = csatornaSzam - 1
            If csatornaAlulTomb(csatornaIndex) Then ZonaHangokMegszolaltatasa(csatornaSzam, False)
            If csatornaFelulTomb(csatornaIndex) Then ZonaHangokMegszolaltatasa(csatornaSzam, True)
        End Sub

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

        Public Sub ProgramValtas(csatornaSzam As Integer, hangszerIndex As Integer)
            KuldesEgyCsatornara(&HC0 + (csatornaSzam - 1), hangszerIndex, 0)
        End Sub

        Public Sub ControllerValtas(csatornaSzam As Integer, controllerSzam As Integer, ertek As Integer)
            KuldesEgyCsatornara(&HB0 + (csatornaSzam - 1), controllerSzam, ertek)
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            BemenetBezaras()
            KimenetBezaras()
        End Sub

    End Class

End Namespace
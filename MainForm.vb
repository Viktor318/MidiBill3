Imports MidiBill3.Modellek

Public Class MainForm

    ' --- MIDI-vezérlő motor ---
    ' WithEvents, hogy a "Handles" szintaxissal iratkozhassunk fel az
    ' eseményeire (MidiUzenetErkezett, KimenetiHiba) - ugyanúgy, mint
    ' bármelyik Designer-generált vezérlőnél.
    Private WithEvents midiMotor As New MidiEngine()

    ' A 4 hangszerpanel és a köréjük tartozó keret (GroupBox)
    Private hangszerKeretek(3) As GroupBox
    Private hangszerPanelek(3) As HangszerPanel

    ' Pedál-inverzió állapota - a VB6-os Last_pedal megfelelője (a legutóbb
    ' KIKÜLDÖTT pedál-érték, invertálástól függetlenül)
    Private utolsoPedalErtek As Integer = 0

    ' Jelzi, hogy a form inicializálása (HangszerPanelekLetrehozasa) már
    ' lefutott-e. Erre azért van szükség, mert a Designer-generált
    ' InitializeComponent() már a konstruktorban beállítja pl. a
    ' trkAlapHangero.Value-t, ami kiváltja a ValueChanged eseményt MÉG
    ' azelőtt, hogy a hangszerPanelek tömb feltöltődne (az csak a
    ' MainForm_Load-ban történik meg) - enélkül a jelző nélkül
    ' NullReferenceException-t dobna.
    Private inicializalasKesz As Boolean = False

    ' Néhány MIDI-konstans, amire itt, a MainForm-ban van szükség
    ' (a teljes MIDCONST-készletet majd csak akkor visszük át, ha ténylegesen kelleni fog)
    Private Const CONTROLLER_CHANGE As Integer = &HB0
    Private Const DAMPER_PEDAL As Integer = 64
    Private Const MAIN_VOLUME As Integer = 7
    Private Const PAN As Integer = 10
    Private Const MOD_WHEEL As Integer = 1

    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        HangszerPanelekLetrehozasa()
        MidiEszkozokFeltoltese()
    End Sub

    ''' <summary>
    ''' Létrehozza a 4 hangszerpanelt (és a köréjük tartozó keretet) kódból,
    ''' ciklusban, és bekötia az összes eseményét a megfelelő MIDI-küldésre.
    ''' </summary>
    Private Sub HangszerPanelekLetrehozasa()
        Dim xKoordinatak() As Integer = {10, 316}
        Dim yKoordinatak() As Integer = {32, 340}

        For i As Integer = 0 To 3
            Dim oszlop As Integer = i Mod 2
            Dim sor As Integer = i \ 2

            Dim keret As New GroupBox()
            keret.Text = (i + 1).ToString() & ". hangszer"
            keret.Location = New Point(xKoordinatak(oszlop), yKoordinatak(sor))
            keret.Size = New Size(300, 300)

            Dim panel As New HangszerPanel()
            panel.Location = New Point(10, 20)
            keret.Controls.Add(panel)

            Me.Controls.Add(keret)

            hangszerKeretek(i) = keret
            hangszerPanelek(i) = panel

            AddHandler panel.HangszerValtozott, AddressOf HangszerPanel_HangszerValtozott
            AddHandler panel.HangeroValtozott, AddressOf HangszerPanel_HangeroValtozott
            AddHandler panel.TerValtozott, AddressOf HangszerPanel_TerValtozott
            AddHandler panel.VibratoValtozott, AddressOf HangszerPanel_VibratoValtozott
            AddHandler panel.AktivValtozott, AddressOf HangszerPanel_AktivValtozott
        Next

        ' Alapértelmezetten csak az 1. csatorna aktív - ugyanaz a kezdőállapot, mint a VB6-ban.
        ' A property-írás nem vált ki eseményt, ezért a keret feliratát kézzel is beállítjuk.
        hangszerPanelek(0).Aktiv = True
        hangszerKeretek(0).Text = "1. hangszer: Aktív"

        inicializalasKesz = True
    End Sub

    ''' <summary>MIDI be- és kimeneti eszközök listájának feltöltése</summary>
    Private Sub MidiEszkozokFeltoltese()
        cmbMidiIn.Items.Clear()
        For Each nev As String In MidiEngine.BemenetiEszkozok()
            cmbMidiIn.Items.Add(nev)
        Next

        If cmbMidiIn.Items.Count = 0 Then
            MsgBox("MIDI bemenet nem található! A program most kilép.")
            Application.Exit()
            Return
        End If

        cmbMidiOut.Items.Clear()
        For Each nev As String In MidiEngine.KimenetiEszkozok()
            cmbMidiOut.Items.Add(nev)
        Next

        If cmbMidiOut.Items.Count = 0 Then
            MsgBox("MIDI kimenet nem található! A program most kilép.")
            Application.Exit()
            Return
        End If

        ' A SelectedIndex beállítása kiváltja a SelectedIndexChanged eseményt,
        ' ami ténylegesen megnyitja az eszközöket - lásd cmbMidiIn/Out_SelectedIndexChanged lent
        cmbMidiIn.SelectedIndex = 0
        cmbMidiOut.SelectedIndex = 0
    End Sub

    ' ============================================================
    ' MIDI eszközök megnyitása/váltása
    ' ============================================================

    Private Sub cmbMidiIn_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbMidiIn.SelectedIndexChanged
        If cmbMidiIn.SelectedIndex < 0 Then Return
        Try
            midiMotor.BemenetMegnyitas(cmbMidiIn.SelectedIndex)
        Catch ex As Exception
            MsgBox("Nem sikerült megnyitni a kiválasztott MIDI bemeneti eszközt." & vbCrLf &
                   "Lehet, hogy időközben eltávolították, vagy más program használja." & vbCrLf & vbCrLf &
                   "Hiba: " & ex.Message, MsgBoxStyle.Exclamation, "MidiBill")
        End Try
    End Sub

    Private Sub cmbMidiOut_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbMidiOut.SelectedIndexChanged
        If cmbMidiOut.SelectedIndex < 0 Then Return
        Try
            midiMotor.KimenetMegnyitas(cmbMidiOut.SelectedIndex)
            OsszesCsatornaUjrakuldese() ' a VB6-os Init_Channels megfelelője
        Catch ex As Exception
            MsgBox("Nem sikerült megnyitni a kiválasztott MIDI kimeneti eszközt." & vbCrLf &
                   "Lehet, hogy időközben eltávolították, vagy más program használja." & vbCrLf & vbCrLf &
                   "Hiba: " & ex.Message, MsgBoxStyle.Exclamation, "MidiBill")
        End Try
    End Sub

    ''' <summary>
    ''' Új kimeneti port nyitásakor minden csatorna aktuális állapotát
    ''' (hangszer, hangerő, pan, vibrato, aktív/inaktív) újraküldi -
    ''' a VB6-os Init_Channels megfelelője.
    ''' </summary>
    Private Sub OsszesCsatornaUjrakuldese()
        If Not inicializalasKesz Then Return

        For i As Integer = 0 To 3
            Dim csatornaSzam As Integer = i + 1
            midiMotor.CsatornaAktiv(i) = hangszerPanelek(i).Aktiv
            midiMotor.ProgramValtas(csatornaSzam, hangszerPanelek(i).Hangszer)
            CsatornaHangeroKuldes(csatornaSzam, hangszerPanelek(i).Hangero)
            midiMotor.ControllerValtas(csatornaSzam, PAN, hangszerPanelek(i).Ter + 64)
            midiMotor.ControllerValtas(csatornaSzam, MOD_WHEEL, hangszerPanelek(i).Vibrato)
        Next
    End Sub

    ' ============================================================
    ' HangszerPanel események -> MIDI küldés
    ' ============================================================

    ''' <summary>Visszaadja, hányadik csatorna (1-4) a küldő HangszerPanel, vagy 0-t, ha nem található</summary>
    Private Function CsatornaSzam(sender As Object) As Integer
        Dim panel As HangszerPanel = TryCast(sender, HangszerPanel)
        If panel Is Nothing Then Return 0
        Dim index As Integer = Array.IndexOf(hangszerPanelek, panel)
        If index < 0 Then Return 0
        Return index + 1
    End Function

    Private Sub HangszerPanel_HangszerValtozott(sender As Object, patchIndex As Integer)
        Dim cs As Integer = CsatornaSzam(sender)
        If cs = 0 Then Return
        midiMotor.ProgramValtas(cs, patchIndex)
    End Sub

    Private Sub HangszerPanel_HangeroValtozott(sender As Object, ertek As Integer)
        Dim cs As Integer = CsatornaSzam(sender)
        If cs = 0 Then Return
        CsatornaHangeroKuldes(cs, ertek)
    End Sub

    Private Sub HangszerPanel_TerValtozott(sender As Object, ertek As Integer)
        Dim cs As Integer = CsatornaSzam(sender)
        If cs = 0 Then Return
        midiMotor.ControllerValtas(cs, PAN, ertek + 64)
    End Sub

    Private Sub HangszerPanel_VibratoValtozott(sender As Object, ertek As Integer)
        Dim cs As Integer = CsatornaSzam(sender)
        If cs = 0 Then Return
        midiMotor.ControllerValtas(cs, MOD_WHEEL, ertek)
    End Sub

    ''' <summary>
    ''' A keret feliratának frissítése + a csatorna tényleges be-/kikapcsolása
    ''' a MidiEngine-ben (Sound_Held_Notes / Turn_Notes_Off megfelelője).
    ''' </summary>
    Private Sub HangszerPanel_AktivValtozott(sender As Object, aktiv As Boolean)
        Dim panel As HangszerPanel = CType(sender, HangszerPanel)
        Dim index As Integer = Array.IndexOf(hangszerPanelek, panel)
        If index < 0 Then Return

        Dim alapNev As String = (index + 1).ToString() & ". hangszer"
        hangszerKeretek(index).Text = If(aktiv, alapNev & ": Aktív", alapNev)

        Dim csatornaSzam As Integer = index + 1
        midiMotor.CsatornaAktiv(index) = aktiv

        If aktiv Then
            midiMotor.LenyomottHangokMegszolaltatasa(csatornaSzam)
        Else
            midiMotor.HangokKikapcsolasa(csatornaSzam)
        End If
    End Sub

    ''' <summary>
    ''' A csatorna hangereje mindig a globális alaphangerővel szorzódik -
    ''' a VB6-os Her_Change: "Data * (Volume.Value / 100)" megfelelője.
    ''' </summary>
    Private Sub CsatornaHangeroKuldes(csatornaSzam As Integer, csatornaErtek As Integer)
        Dim vegsoErtek As Integer = CInt(csatornaErtek * (trkAlapHangero.Value / 100.0))
        midiMotor.ControllerValtas(csatornaSzam, MAIN_VOLUME, vegsoErtek)
    End Sub

    ' ============================================================
    ' Globális hangerő
    ' ============================================================

    Private Sub trkAlapHangero_ValueChanged(sender As Object, e As EventArgs) Handles trkAlapHangero.ValueChanged
        If Not inicializalasKesz Then Return

        grpVolume.Text = "Alap hangerő beállítása: " & trkAlapHangero.Value

        ' Minden csatorna hangerejét újraküldjük az új globális szorzóval -
        ' a VB6-os Volume_Refresh megfelelője
        For i As Integer = 0 To 3
            CsatornaHangeroKuldes(i + 1, hangszerPanelek(i).Hangero)
        Next
    End Sub

    ' ============================================================
    ' Bejövő MIDI üzenetek fogadása (NAudio háttérszáláról!)
    ' ============================================================

    ''' <summary>
    ''' A midiMotor háttérszálról tüzeli ezt az eseményt - kötelező Invoke-kal
    ''' a UI-szálra váltani, mielőtt bármilyen vezérlőt módosítanánk.
    ''' </summary>
    Private Sub midiMotor_MidiUzenetErkezett(uzenet As Integer, adat1 As Integer, adat2 As Integer) Handles midiMotor.MidiUzenetErkezett
        If InvokeRequired Then
            Invoke(Sub() MidiUzenetFeldolgozasa(uzenet, adat1, adat2))
        Else
            MidiUzenetFeldolgozasa(uzenet, adat1, adat2)
        End If
    End Sub

    ''' <summary>
    ''' Ez már biztosan a UI-szálon fut. A VB6-os MIDIInput1_Message logikáját
    ''' követi: pedál-inverzió kezelése, üzenet megjelenítése, majd fan-out küldés.
    ''' </summary>
    Private Sub MidiUzenetFeldolgozasa(uzenet As Integer, adat1 As Integer, adat2 As Integer)
        Dim vegsoAdat2 As Integer = adat2

        ' Sustain pedál üzenet invertálása, ha a "Fordított pedál működés" be van pipálva
        If uzenet = CONTROLLER_CHANGE And adat1 = DAMPER_PEDAL Then
            If chkPedal.Checked Then
                vegsoAdat2 = 127 - adat2
            End If
            utolsoPedalErtek = vegsoAdat2
        End If

        lblMidiUzenet.Text = "MIDI üzenetek: " & uzenet & ", " & adat1 & ", " & vegsoAdat2

        midiMotor.UzenetKuldes(uzenet, adat1, vegsoAdat2)
    End Sub

    ''' <summary>
    ''' Pedál-inverzió bekapcsolásakor/kikapcsolásakor azonnal ki is küldjük
    ''' az invertált pedálállapotot - a VB6-os Pedal_Click megfelelője.
    ''' </summary>
    Private Sub chkPedal_CheckedChanged(sender As Object, e As EventArgs) Handles chkPedal.CheckedChanged
        utolsoPedalErtek = 127 - utolsoPedalErtek
        lblMidiUzenet.Text = "MIDI üzenetek: " & CONTROLLER_CHANGE & ", " & DAMPER_PEDAL & ", " & utolsoPedalErtek
        midiMotor.UzenetKuldes(CONTROLLER_CHANGE, DAMPER_PEDAL, utolsoPedalErtek)
    End Sub

    ' ============================================================
    ' Kimeneti hiba jelzése
    ' ============================================================

    Private Sub midiMotor_KimenetiHiba(hibaUzenet As String) Handles midiMotor.KimenetiHiba
        If InvokeRequired Then
            Invoke(Sub() MsgBox(hibaUzenet, MsgBoxStyle.Exclamation, "MidiBill"))
        Else
            MsgBox(hibaUzenet, MsgBoxStyle.Exclamation, "MidiBill")
        End If
    End Sub

    ' ============================================================
    ' Pánikgomb
    ' ============================================================

    Private Sub btnReset_Click(sender As Object, e As EventArgs) Handles btnReset.Click
        For c As Integer = 1 To 4
            midiMotor.HangokKikapcsolasa(c, True) ' teljes 128-hangos seprés, nyilvántartástól függetlenül
        Next
    End Sub

End Class
Imports MidiBill3.Modellek

Public Class MainForm

    Private WithEvents midiMotor As New MidiEngine()

    Private hangszerKeretek(3) As GroupBox
    Private hangszerPanelek(3) As HangszerPanel
    Private csatornaAllapotok(15) As CsatornaBeallitas
    Private aktivCsoportIndex As Integer = 0

    Private Const DOB_CSATORNA_INDEX As Integer = 9

    Private utolsoPedalErtek As Integer = 0
    Private inicializalasKesz As Boolean = False
    Private aktualisFajlNev As String = ""
    Private utolsoBetoltesMappa As String = ""
    Private utolsoMentesMappa As String = ""
    Private utolsoImportMappa As String = ""
    Private aktualisSplitPont As Integer = 60
    Private splitTanulasAktiv As Boolean = False
    Private mentesKihagyasa As Boolean = False
    Private aktualisTranszponalas As Integer = 0
    Private Const TRANSZPONALAS_MIN As Integer = -24
    Private Const TRANSZPONALAS_MAX As Integer = 24

    Private ReadOnly kezdetFile As String = IO.Path.Combine(Application.StartupPath, "Kezdet.json")

    Private Const CONTROLLER_CHANGE As Integer = &HB0
    Private Const DAMPER_PEDAL As Integer = 64
    Private Const MAIN_VOLUME As Integer = 7
    Private Const PAN As Integer = 10
    Private Const MOD_WHEEL As Integer = 1
    Private Const NOTE_ON As Integer = &H90

    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        HangszerPanelekLetrehozasa()
        CsatornaAllapotokInicializalasa()
        SplitPontFeltoltese()
        MidiEszkozokFeltoltese()

        inicializalasKesz = True
        CsatornaPanelekFrissitese()

        KezdetBeallitasokBetoltese()
    End Sub

    Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        For c As Integer = 1 To 16
            midiMotor.HangokKikapcsolasa(c, True)
        Next

        If Not mentesKihagyasa Then
            Try
                AktualisBeallitasokOsszegyujtese().Mentes(kezdetFile)
            Catch ex As Exception
            End Try
        End If

        midiMotor.Dispose()
    End Sub

    ''' <summary>A 4 fizikai HangszerPanel és keret létrehozása - a végleges elrendezéshez igazítva</summary>
    Private Sub HangszerPanelekLetrehozasa()
        Dim xKoordinatak() As Integer = {25, 331}
        Dim yKoordinatak() As Integer = {53, 361}

        For i As Integer = 0 To 3
            Dim oszlop As Integer = i Mod 2
            Dim sor As Integer = i \ 2

            Dim keret As New GroupBox()
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
            AddHandler panel.AlulValtozott, AddressOf HangszerPanel_AlulValtozott
            AddHandler panel.FelulValtozott, AddressOf HangszerPanel_FelulValtozott
        Next
    End Sub

    Private Sub CsatornaAllapotokInicializalasa()
        For i As Integer = 0 To 15
            csatornaAllapotok(i) = New CsatornaBeallitas()
        Next
        csatornaAllapotok(0).Aktiv = True
    End Sub

    Private Sub SplitPontFeltoltese()
        cmbSplitPont.Items.Clear()
        For szam As Integer = 0 To 127
            cmbSplitPont.Items.Add(Hangnevek.HangNev(szam) & " (" & szam & ")")
        Next
        cmbSplitPont.SelectedIndex = 60
    End Sub

    Private Sub MidiEszkozokFeltoltese()
        cmbMidiIn.Items.Clear()
        'For Each nev As String In New List(Of String)() ' TESZT: üres bemeneti lista szimulálása
        For Each nev As String In MidiEngine.BemenetiEszkozok()
            cmbMidiIn.Items.Add(nev)
        Next

        cmbMidiOut.Items.Clear()
        'For Each nev As String In New List(Of String)() ' TESZT: üres kimeneti lista szimulálása
        For Each nev As String In MidiEngine.KimenetiEszkozok()
            cmbMidiOut.Items.Add(nev)
        Next

        If cmbMidiIn.Items.Count = 0 Then
            MsgBox("Nem található csatlakoztatott MIDI bemeneti eszköz." & vbCrLf &
               "Csatlakoztass egy MIDI billentyűzetet, majd a ""MIDI eszközök frissítése"" gombbal töltsd be.",
               MsgBoxStyle.Exclamation, "MidiBill")
        Else
            cmbMidiIn.SelectedIndex = 0
        End If

        If cmbMidiOut.Items.Count = 0 Then
            MsgBox("Nem található csatlakoztatott MIDI kimeneti eszköz." & vbCrLf &
               "MIDI kimenet nélkül a program nem tud hangot megszólaltatni, ezért most kilép." & vbCrLf &
               "Csatlakoztass egy MIDI kimeneti eszközt, majd indítsd el újra a programot.",
               MsgBoxStyle.Critical, "MidiBill")
            Environment.Exit(1)
        Else
            cmbMidiOut.SelectedIndex = 0
        End If
    End Sub

    Private Sub btnMidiFrissites_Click(sender As Object, e As EventArgs) Handles btnMidiFrissites.Click
        MidiEszkozokUjratoltese()
    End Sub

    ''' <summary>
    ''' Újra lekérdezi a Windows-tól az aktuálisan csatlakoztatott MIDI eszközöket,
    ''' és frissíti a legördülő listákat - a "MIDI eszközök frissítése" gomb hívja.
    ''' Amennyire lehet, megtartja a jelenleg kiválasztott eszközt (ha az még
    ''' mindig szerepel a listában), hogy egy új eszköz bedugása ne zavarja meg
    ''' a már beállított MIDI In/Out kiválasztást.
    ''' </summary>
    Private Sub MidiEszkozokUjratoltese()
        Dim regiBemenet As String = If(cmbMidiIn.SelectedItem IsNot Nothing, cmbMidiIn.SelectedItem.ToString(), "")
        Dim regiKimenet As String = If(cmbMidiOut.SelectedItem IsNot Nothing, cmbMidiOut.SelectedItem.ToString(), "")

        cmbMidiIn.Items.Clear()
        For Each nev As String In MidiEngine.BemenetiEszkozok()
            cmbMidiIn.Items.Add(nev)
        Next

        cmbMidiOut.Items.Clear()
        For Each nev As String In MidiEngine.KimenetiEszkozok()
            cmbMidiOut.Items.Add(nev)
        Next

        If cmbMidiIn.Items.Count = 0 Then
            MsgBox("MIDI bemenet nem található.", MsgBoxStyle.Exclamation, "MidiBill")
            Return
        End If

        If cmbMidiOut.Items.Count = 0 Then
            MsgBox("MIDI kimenet nem található.", MsgBoxStyle.Exclamation, "MidiBill")
            Return
        End If

        Dim ujBemenetIndex As Integer = cmbMidiIn.Items.IndexOf(regiBemenet)
        cmbMidiIn.SelectedIndex = If(ujBemenetIndex >= 0, ujBemenetIndex, 0)

        Dim ujKimenetIndex As Integer = cmbMidiOut.Items.IndexOf(regiKimenet)
        cmbMidiOut.SelectedIndex = If(ujKimenetIndex >= 0, ujKimenetIndex, 0)

        MsgBox("MIDI In/Out eszközlista sikeresen frissítve.", MsgBoxStyle.Information, "MidiBill")
    End Sub

    Private Sub KezdetBeallitasokBetoltese()
        If Not IO.File.Exists(kezdetFile) Then
            CimsorFrissitese(kezdetFile)
            Return
        End If

        Try
            Dim b As Beallitasok = Beallitasok.Betoltes(kezdetFile)
            BeallitasokAlkalmazasa(b, True)
            CimsorFrissitese(kezdetFile)
        Catch ex As Exception
            MsgBox("Nem sikerült betölteni a korábban elmentett beállításokat." & vbCrLf &
                   "Hiba: " & ex.Message, MsgBoxStyle.Exclamation, "MidiBill")
        End Try
    End Sub

    ' ============================================================
    ' MIDI eszközök megnyitása/váltása
    ' ============================================================

    Private Sub cmbMidiIn_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbMidiIn.SelectedIndexChanged
        If cmbMidiIn.SelectedIndex < 0 Then Return

        Dim frissLista As List(Of String) = MidiEngine.BemenetiEszkozok()
        If Not EszkozListaEgyezikE(cmbMidiIn, frissLista) Then
            MsgBox("A MIDI bemeneti eszközök listája időközben megváltozott (pl. egy" & vbCrLf &
               "eszközt kihúztak vagy csatlakoztattak), ezért frissítem a MIDI In/Out listát." & vbCrLf &
               "A korábbi kiválasztás visszaáll, ha az eszköz még elérhető, " &
               "egyébként az első elérhető eszköz kerül kiválasztásra.",
               MsgBoxStyle.Information, "MidiBill")
            MidiEszkozokUjratoltese()
            ActiveControl = Nothing
            Return
        End If

        Try
            midiMotor.BemenetMegnyitas(cmbMidiIn.SelectedIndex)
        Catch ex As Exception
            MsgBox("Nem sikerült megnyitni a kiválasztott MIDI bemeneti eszközt." & vbCrLf &
               "Lehet, hogy időközben eltávolították, vagy más program használja." & vbCrLf & vbCrLf &
               "Hiba: " & ex.Message & vbCrLf & vbCrLf &
               "Frissítem a MIDI eszközlistát.", MsgBoxStyle.Exclamation, "MidiBill")
            MidiEszkozokUjratoltese()
        End Try

        ActiveControl = Nothing
    End Sub

    Private Sub cmbMidiOut_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbMidiOut.SelectedIndexChanged
        If cmbMidiOut.SelectedIndex < 0 Then Return

        Dim frissLista As List(Of String) = MidiEngine.KimenetiEszkozok()
        If Not EszkozListaEgyezikE(cmbMidiOut, frissLista) Then
            MsgBox("A MIDI kimeneti eszközök listája időközben megváltozott (pl. egy" & vbCrLf &
               "eszközt kihúztak vagy csatlakoztattak), ezért frissítem a MIDI In/Out listát." & vbCrLf &
               "A korábbi kiválasztás visszaáll, ha az eszköz még elérhető, " &
               "egyébként az első elérhető eszköz kerül kiválasztásra.",
               MsgBoxStyle.Information, "MidiBill")
            MidiEszkozokUjratoltese()
            ActiveControl = Nothing
            Return
        End If

        Try
            midiMotor.KimenetMegnyitas(cmbMidiOut.SelectedIndex)
            OsszesCsatornaUjrakuldese()
        Catch ex As Exception
            MsgBox("Nem sikerült megnyitni a kiválasztott MIDI kimeneti eszközt." & vbCrLf &
               "Lehet, hogy időközben eltávolították, vagy más program használja." & vbCrLf & vbCrLf &
               "Hiba: " & ex.Message & vbCrLf & vbCrLf &
               "Frissítem a MIDI eszközlistát.", MsgBoxStyle.Exclamation, "MidiBill")
            MidiEszkozokUjratoltese()
        End Try

        ActiveControl = Nothing
    End Sub

    ''' <summary>
    ''' Ellenőrzi, hogy a ComboBox jelenlegi tartalma (sorrendben) pontosan
    ''' megegyezik-e a Windows-tól frissen lekérdezett eszközlistával. Ha nem,
    ''' az azt jelenti, hogy időközben eszköz csatlakozott/leválasztódott, és a
    ''' jelenlegi kiválasztás (index) már nem biztos, hogy a várt eszközre mutat.
    ''' </summary>
    Private Function EszkozListaEgyezikE(cmb As ComboBox, frissLista As List(Of String)) As Boolean
        If cmb.Items.Count <> frissLista.Count Then Return False
        For i As Integer = 0 To cmb.Items.Count - 1
            If cmb.Items(i).ToString() <> frissLista(i) Then Return False
        Next
        Return True
    End Function

    Private Sub OsszesCsatornaUjrakuldese()
        If Not inicializalasKesz Then Return

        midiMotor.SplitPont = aktualisSplitPont
        midiMotor.Transpozicio = aktualisTranszponalas

        For i As Integer = 0 To 15
            Dim csatornaSzam As Integer = i + 1
            Dim adat As CsatornaBeallitas = csatornaAllapotok(i)
            midiMotor.CsatornaAktiv(i) = adat.Aktiv
            midiMotor.CsatornaAlul(i) = adat.Alul
            midiMotor.CsatornaFelul(i) = adat.Felul
            midiMotor.ProgramValtas(csatornaSzam, adat.Hangszer)
            CsatornaHangeroKuldes(csatornaSzam, adat.Hangero)
            midiMotor.ControllerValtas(csatornaSzam, PAN, adat.Ter + 64)
            midiMotor.ControllerValtas(csatornaSzam, MOD_WHEEL, adat.Vibrato)
        Next
    End Sub

    ' ============================================================
    ' Csatornacsoport váltás (1-4 / 5-8 / 9-12 / 13-16)
    ' ============================================================

    Private Sub radCsoport1_CheckedChanged(sender As Object, e As EventArgs) Handles radCsoport1.CheckedChanged
        If radCsoport1.Checked Then CsoportValtas(0)
    End Sub

    Private Sub radCsoport2_CheckedChanged(sender As Object, e As EventArgs) Handles radCsoport2.CheckedChanged
        If radCsoport2.Checked Then CsoportValtas(1)
    End Sub

    Private Sub radCsoport3_CheckedChanged(sender As Object, e As EventArgs) Handles radCsoport3.CheckedChanged
        If radCsoport3.Checked Then CsoportValtas(2)
    End Sub

    Private Sub radCsoport4_CheckedChanged(sender As Object, e As EventArgs) Handles radCsoport4.CheckedChanged
        If radCsoport4.Checked Then CsoportValtas(3)
    End Sub

    Private Sub CsoportValtas(ujCsoportIndex As Integer)
        If Not inicializalasKesz Then Return
        aktivCsoportIndex = ujCsoportIndex
        CsatornaPanelekFrissitese()
    End Sub

    Private Sub CsatornaPanelekFrissitese()
        For i As Integer = 0 To 3
            Dim csatornaIndex As Integer = aktivCsoportIndex * 4 + i
            Dim csatornaSzam As Integer = csatornaIndex + 1
            Dim adat As CsatornaBeallitas = csatornaAllapotok(csatornaIndex)
            Dim panel As HangszerPanel = hangszerPanelek(i)
            Dim keret As GroupBox = hangszerKeretek(i)

            panel.Hangszer = adat.Hangszer
            panel.Hangero = adat.Hangero
            panel.Ter = adat.Ter
            panel.Vibrato = adat.Vibrato
            panel.Aktiv = adat.Aktiv
            panel.Alul = adat.Alul
            panel.Felul = adat.Felul

            Dim dobCsatorna As Boolean = (csatornaIndex = DOB_CSATORNA_INDEX)
            panel.Enabled = Not dobCsatorna

            Dim alapNev As String = csatornaSzam.ToString() & ". csatorna"
            If dobCsatorna Then
                keret.Text = alapNev & " (dob - nem konfigurálható)"
            Else
                keret.Text = If(adat.Aktiv, alapNev & ": Aktív", alapNev)
            End If
        Next
    End Sub

    ' ============================================================
    ' HangszerPanel események -> MIDI küldés
    ' ============================================================

    Private Function CsatornaIndexTol(sender As Object) As Integer
        Dim panel As HangszerPanel = TryCast(sender, HangszerPanel)
        If panel Is Nothing Then Return -1
        Dim panelPozicio As Integer = Array.IndexOf(hangszerPanelek, panel)
        If panelPozicio < 0 Then Return -1
        Return aktivCsoportIndex * 4 + panelPozicio
    End Function

    Private Sub HangszerPanel_HangszerValtozott(sender As Object, patchIndex As Integer)
        Dim idx As Integer = CsatornaIndexTol(sender)
        If idx < 0 Then Return
        csatornaAllapotok(idx).Hangszer = patchIndex
        midiMotor.ProgramValtas(idx + 1, patchIndex)
    End Sub

    Private Sub HangszerPanel_HangeroValtozott(sender As Object, ertek As Integer)
        Dim idx As Integer = CsatornaIndexTol(sender)
        If idx < 0 Then Return
        csatornaAllapotok(idx).Hangero = ertek
        CsatornaHangeroKuldes(idx + 1, ertek)
    End Sub

    Private Sub HangszerPanel_TerValtozott(sender As Object, ertek As Integer)
        Dim idx As Integer = CsatornaIndexTol(sender)
        If idx < 0 Then Return
        csatornaAllapotok(idx).Ter = ertek
        midiMotor.ControllerValtas(idx + 1, PAN, ertek + 64)
    End Sub

    Private Sub HangszerPanel_VibratoValtozott(sender As Object, ertek As Integer)
        Dim idx As Integer = CsatornaIndexTol(sender)
        If idx < 0 Then Return
        csatornaAllapotok(idx).Vibrato = ertek
        midiMotor.ControllerValtas(idx + 1, MOD_WHEEL, ertek)
    End Sub

    Private Sub HangszerPanel_AktivValtozott(sender As Object, aktiv As Boolean)
        Dim idx As Integer = CsatornaIndexTol(sender)
        If idx < 0 Then Return
        If idx = DOB_CSATORNA_INDEX Then Return

        csatornaAllapotok(idx).Aktiv = aktiv

        Dim panel As HangszerPanel = CType(sender, HangszerPanel)
        Dim panelPozicio As Integer = Array.IndexOf(hangszerPanelek, panel)
        If panelPozicio >= 0 Then
            Dim alapNev As String = (idx + 1).ToString() & ". csatorna"
            hangszerKeretek(panelPozicio).Text = If(aktiv, alapNev & ": Aktív", alapNev)
        End If

        Dim csatornaSzam As Integer = idx + 1
        midiMotor.CsatornaAktiv(idx) = aktiv

        If aktiv Then
            midiMotor.LenyomottHangokMegszolaltatasa(csatornaSzam)
        Else
            midiMotor.HangokKikapcsolasa(csatornaSzam)
        End If
    End Sub

    Private Sub HangszerPanel_AlulValtozott(sender As Object, aktiv As Boolean)
        Dim idx As Integer = CsatornaIndexTol(sender)
        If idx < 0 Then Return
        csatornaAllapotok(idx).Alul = aktiv
        midiMotor.AlulValtas(idx + 1, aktiv)
    End Sub

    Private Sub HangszerPanel_FelulValtozott(sender As Object, aktiv As Boolean)
        Dim idx As Integer = CsatornaIndexTol(sender)
        If idx < 0 Then Return
        csatornaAllapotok(idx).Felul = aktiv
        midiMotor.FelulValtas(idx + 1, aktiv)
    End Sub

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

        For i As Integer = 0 To 15
            CsatornaHangeroKuldes(i + 1, csatornaAllapotok(i).Hangero)
        Next
    End Sub

    ' ============================================================
    ' Split pont
    ' ============================================================

    Private Sub cmbSplitPont_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbSplitPont.SelectedIndexChanged
        If Not inicializalasKesz Then Return
        If cmbSplitPont.SelectedIndex < 0 Then Return

        aktualisSplitPont = cmbSplitPont.SelectedIndex
        midiMotor.SplitPont = aktualisSplitPont
        grpSplit.Text = "Split pont: " & Hangnevek.HangNev(aktualisSplitPont)

        ActiveControl = Nothing
    End Sub

    Private Sub btnSplitTanulas_Click(sender As Object, e As EventArgs) Handles btnSplitTanulas.Click
        splitTanulasAktiv = True
        lblSplitInfo.Text = "Várakozás billentyűre..."
    End Sub

    Private Sub SplitPontBeallitasaTanulassal(hang As Integer)
        splitTanulasAktiv = False
        cmbSplitPont.SelectedIndex = hang
        lblSplitInfo.Text = "Beállítva: " & Hangnevek.HangNev(hang)
    End Sub

    ' ============================================================
    ' Globális transzponálás
    ' ============================================================

    Private Function ElojelesSzoveg(ertek As Integer) As String
        Return If(ertek > 0, "+" & ertek, ertek.ToString())
    End Function

    Private Sub TranszponalasFrissites(ujErtek As Integer)
        aktualisTranszponalas = Korlatoz(ujErtek, TRANSZPONALAS_MIN, TRANSZPONALAS_MAX)
        midiMotor.Transpozicio = aktualisTranszponalas
        grpTranspose.Text = "Transzponálás: " & ElojelesSzoveg(aktualisTranszponalas)
    End Sub

    Private Sub btnFelhangFel_Click(sender As Object, e As EventArgs) Handles btnFelhangFel.Click
        TranszponalasFrissites(aktualisTranszponalas + 1)
    End Sub

    Private Sub btnFelhangLe_Click(sender As Object, e As EventArgs) Handles btnFelhangLe.Click
        TranszponalasFrissites(aktualisTranszponalas - 1)
    End Sub

    Private Sub btnOktavFel_Click(sender As Object, e As EventArgs) Handles btnOktavFel.Click
        TranszponalasFrissites(aktualisTranszponalas + 12)
    End Sub

    Private Sub btnOktavLe_Click(sender As Object, e As EventArgs) Handles btnOktavLe.Click
        TranszponalasFrissites(aktualisTranszponalas - 12)
    End Sub

    ' ============================================================
    ' Bejövő MIDI üzenetek fogadása (NAudio háttérszáláról!)
    ' ============================================================

    Private Sub midiMotor_MidiUzenetErkezett(uzenet As Integer, adat1 As Integer, adat2 As Integer) Handles midiMotor.MidiUzenetErkezett
        If InvokeRequired Then
            Invoke(Sub() MidiUzenetFeldolgozasa(uzenet, adat1, adat2))
        Else
            MidiUzenetFeldolgozasa(uzenet, adat1, adat2)
        End If
    End Sub

    Private Sub MidiUzenetFeldolgozasa(uzenet As Integer, adat1 As Integer, adat2 As Integer)
        Dim vegsoAdat2 As Integer = adat2

        If uzenet = CONTROLLER_CHANGE And adat1 = DAMPER_PEDAL Then
            If chkPedal.Checked Then
                vegsoAdat2 = 127 - adat2
            End If
            utolsoPedalErtek = vegsoAdat2
        End If

        lblMidiUzenet.Text = "MIDI üzenetek: " & uzenet & ", " & adat1 & ", " & vegsoAdat2

        If splitTanulasAktiv AndAlso uzenet = NOTE_ON AndAlso vegsoAdat2 > 0 Then
            SplitPontBeallitasaTanulassal(adat1)
            Return
        End If

        midiMotor.UzenetKuldes(uzenet, adat1, vegsoAdat2)
    End Sub

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
        For c As Integer = 1 To 16
            midiMotor.HangokKikapcsolasa(c, True)
        Next
    End Sub

    ' ============================================================
    ' Kilépés
    ' ============================================================

    Private Sub btnKilepes_Click(sender As Object, e As EventArgs) Handles btnKilepes.Click
        Close()
    End Sub

    Private Sub mnuKilepes_Click(sender As Object, e As EventArgs) Handles mnuKilepes.Click
        Close()
    End Sub

    Private Sub mnuKilepesMentesNelkul_Click(sender As Object, e As EventArgs) Handles mnuKilepesMentesNelkul.Click
        Dim valasz As MsgBoxResult = MsgBox("Biztosan mentés nélkül szeretnél kilépni?" & vbCrLf &
                                             "A jelenlegi beállítások elvesznek, legközelebb a korábban elmentett állapot töltődik be.",
                                             MsgBoxStyle.YesNo Or MsgBoxStyle.Question, "MidiBill - Kilépés mentés nélkül")
        If valasz = MsgBoxResult.Yes Then
            mentesKihagyasa = True
            Close()
        End If
    End Sub

    ' ============================================================
    ' Névjegy
    ' ============================================================

    Private Sub mnuNevjegy_Click(sender As Object, e As EventArgs) Handles mnuNevjegy.Click
        MsgBox("MidiBill3 2026, " & "Verzió: " & AppInfo.AppVerzio() & vbCrLf & vbCrLf &
           "MIDI billentyűzet megszólaltatása 15 csatornán." & vbCrLf &
           "(A dob csatorna nem szerkeszthető)" & vbCrLf &
           ".NET 10 / WinForms port a VB6-os eredetiből.",
           MsgBoxStyle.Information, "Névjegy - MidiBill3")
    End Sub

    ' ============================================================
    ' Fájl mentés / betöltés (JSON) + régi .mb3 importálás
    ' ============================================================

    Private Function AktualisBeallitasokOsszegyujtese() As Beallitasok
        Dim b As New Beallitasok With {
            .Verzio = AppInfo.AppVerzio()
        }

        For i As Integer = 0 To 15
            Dim cs As New CsatornaBeallitas()

            If i = DOB_CSATORNA_INDEX Then
                ' A 10. csatorna (dob) sosem konfigurálható, ezért az adatait
                ' sosem mentjük - mindig friss, alapértelmezett érték kerül a
                ' fájlba, függetlenül attól, mi van a memóriában.
                b.Csatornak.Add(cs)
                Continue For
            End If

            Dim forras As CsatornaBeallitas = csatornaAllapotok(i)
            cs.Hangszer = forras.Hangszer
            cs.Hangero = forras.Hangero
            cs.Ter = forras.Ter
            cs.Vibrato = forras.Vibrato
            cs.Aktiv = forras.Aktiv
            cs.Alul = forras.Alul
            cs.Felul = forras.Felul
            b.Csatornak.Add(cs)
        Next

        b.AlapHangero = trkAlapHangero.Value
        b.MidiBemenetNev = If(cmbMidiIn.SelectedItem IsNot Nothing, cmbMidiIn.SelectedItem.ToString(), "")
        b.MidiKimenetNev = If(cmbMidiOut.SelectedItem IsNot Nothing, cmbMidiOut.SelectedItem.ToString(), "")
        b.PedalInvertalva = chkPedal.Checked
        b.SplitPont = aktualisSplitPont
        b.Transzponalas = aktualisTranszponalas

        Return b
    End Function

    ''' <summary>
    ''' Egy betöltött/importált Beallitasok alkalmazása a UI-ra és a MIDI-re.
    ''' frissitsdEszkozoketIs: JSON-betöltésnél True (van eszköznév-adat),
    ''' .mb3 importnál False (a régi fájl eszközindexei nem megbízhatóak).
    ''' A folyamat végén figyelmeztet, ha a fájl hibás/hiányzó MIDI eszköznevet
    ''' vagy tartományon kívüli, korrigált értéket tartalmazott.
    ''' </summary>
    Private Sub BeallitasokAlkalmazasa(b As Beallitasok, frissitsdEszkozoketIs As Boolean)
        Dim ertekKorrigalva As Boolean = False

        For i As Integer = 0 To 15
            Dim adat As New CsatornaBeallitas()

            If i = DOB_CSATORNA_INDEX Then
                ' A 10. csatorna (dob) sosem konfigurálható, ezért a fájlban
                ' esetlegesen szereplő adatát figyelmen kívül hagyjuk - mindig
                ' friss, alapértelmezett (inaktív) állapotba kerül.
                csatornaAllapotok(i) = adat
                Continue For
            End If

            Dim cs As CsatornaBeallitas = b.Csatornak(i)

            Dim ujHangszer As Integer = Korlatoz(cs.Hangszer, 0, 127)
            Dim ujHangero As Integer = Korlatoz(cs.Hangero, 0, 127)
            Dim ujTer As Integer = Korlatoz(cs.Ter, -64, 63)
            Dim ujVibrato As Integer = Korlatoz(cs.Vibrato, 0, 127)

            If ujHangszer <> cs.Hangszer OrElse ujHangero <> cs.Hangero OrElse
           ujTer <> cs.Ter OrElse ujVibrato <> cs.Vibrato Then
                ertekKorrigalva = True
            End If

            adat.Hangszer = ujHangszer
            adat.Hangero = ujHangero
            adat.Ter = ujTer
            adat.Vibrato = ujVibrato
            adat.Aktiv = cs.Aktiv
            adat.Alul = cs.Alul
            adat.Felul = cs.Felul
            csatornaAllapotok(i) = adat
        Next

        Dim ujAlapHangero As Integer = Korlatoz(b.AlapHangero, 0, 100)
        If ujAlapHangero <> b.AlapHangero Then ertekKorrigalva = True
        trkAlapHangero.Value = ujAlapHangero

        chkPedal.Checked = b.PedalInvertalva

        splitTanulasAktiv = False
        lblSplitInfo.Text = ""

        Dim ujSplitPont As Integer = Korlatoz(b.SplitPont, 0, 127)
        If ujSplitPont <> b.SplitPont Then ertekKorrigalva = True
        cmbSplitPont.SelectedIndex = ujSplitPont

        Dim ujTranszponalas As Integer = Korlatoz(b.Transzponalas, TRANSZPONALAS_MIN, TRANSZPONALAS_MAX)
        If ujTranszponalas <> b.Transzponalas Then ertekKorrigalva = True
        TranszponalasFrissites(b.Transzponalas)

        Dim eszkozHianyzik As Boolean = False

        If frissitsdEszkozoketIs Then
            Dim beIdx As Integer = cmbMidiIn.Items.IndexOf(b.MidiBemenetNev)
            If beIdx >= 0 Then
                cmbMidiIn.SelectedIndex = beIdx
            ElseIf Not String.IsNullOrEmpty(b.MidiBemenetNev) Then
                eszkozHianyzik = True
            End If

            Dim kiIdx As Integer = cmbMidiOut.Items.IndexOf(b.MidiKimenetNev)
            If kiIdx >= 0 Then
                cmbMidiOut.SelectedIndex = kiIdx
            ElseIf Not String.IsNullOrEmpty(b.MidiKimenetNev) Then
                eszkozHianyzik = True
            End If
        End If

        CsatornaPanelekFrissitese()
        OsszesCsatornaUjrakuldese()

        If eszkozHianyzik Then
            MsgBox("A betölteni kívánt json fájl nem létező MIDI eszköznevet tartalmazott, amit a program figyelmen kívül hagyott.",
               MsgBoxStyle.Exclamation, "MidiBill")
        End If

        If ertekKorrigalva Then
            MsgBox("A betölteni kívánt json fájl hibás adatokat tartalmazott, amiket a program figyelmen kívül hagyott.",
               MsgBoxStyle.Exclamation, "MidiBill")
        End If
    End Sub

    Private Function Korlatoz(ertek As Integer, min As Integer, max As Integer) As Integer
        Return Math.Max(min, Math.Min(max, ertek))
    End Function

    Private Sub CimsorFrissitese(teljesUtvonal As String)
        aktualisFajlNev = IO.Path.GetFileName(teljesUtvonal)
        Me.Text = "MIDI billentyűzet megszólaltatása 16 csatornán : " & aktualisFajlNev
    End Sub

    Private Sub DialogusMappaBeallitasa(dlg As FileDialog, ByRef megjegyzettMappa As String)
        dlg.InitialDirectory = If(String.IsNullOrEmpty(megjegyzettMappa), Application.StartupPath, megjegyzettMappa)
    End Sub

    Private Sub DialogusAllapotMentese(dlg As FileDialog, teljesUtvonal As String, ByRef megjegyzettMappa As String)
        dlg.FileName = IO.Path.GetFileName(teljesUtvonal)
        megjegyzettMappa = IO.Path.GetDirectoryName(teljesUtvonal)
    End Sub

    Private Sub mnuBetolt_Click(sender As Object, e As EventArgs) Handles mnuBetolt.Click
        DialogusMappaBeallitasa(dlgMegnyitas, utolsoBetoltesMappa)

        If dlgMegnyitas.ShowDialog() = DialogResult.OK Then
            Try
                Dim b As Beallitasok = Beallitasok.Betoltes(dlgMegnyitas.FileName)
                BeallitasokAlkalmazasa(b, True)
                CimsorFrissitese(dlgMegnyitas.FileName)
                DialogusAllapotMentese(dlgMegnyitas, dlgMegnyitas.FileName, utolsoBetoltesMappa)
            Catch ex As Exception
                MsgBox("Nem sikerült betölteni a(z) """ & dlgMegnyitas.FileName & """ fájlt." & vbCrLf &
                       "Lehet, hogy sérült, vagy nem MidiBill adatfájl." & vbCrLf & vbCrLf &
                       "Hiba: " & ex.Message, MsgBoxStyle.Exclamation, "MidiBill")
            End Try
        End If
    End Sub

    Private Sub mnuMent_Click(sender As Object, e As EventArgs) Handles mnuMent.Click
        DialogusMappaBeallitasa(dlgMentes, utolsoMentesMappa)
        dlgMentes.FileName = IO.Path.GetFileNameWithoutExtension(aktualisFajlNev)

        If dlgMentes.ShowDialog() = DialogResult.OK Then
            Try
                AktualisBeallitasokOsszegyujtese().Mentes(dlgMentes.FileName)
                CimsorFrissitese(dlgMentes.FileName)
                DialogusAllapotMentese(dlgMentes, dlgMentes.FileName, utolsoMentesMappa)
            Catch ex As Exception
                MsgBox("Nem sikerült elmenteni a beállításokat ide:" & vbCrLf & dlgMentes.FileName & vbCrLf & vbCrLf &
                       "Hiba: " & ex.Message, MsgBoxStyle.Exclamation, "MidiBill")
            End Try
        End If
    End Sub

    Private Sub mnuImportMb3_Click(sender As Object, e As EventArgs) Handles mnuImportMb3.Click
        DialogusMappaBeallitasa(dlgImportMb3, utolsoImportMappa)

        If dlgImportMb3.ShowDialog() = DialogResult.OK Then
            Try
                Dim b As Beallitasok = Beallitasok.ImportMb3(dlgImportMb3.FileName)
                BeallitasokAlkalmazasa(b, False)
                CimsorFrissitese(dlgImportMb3.FileName)
                DialogusAllapotMentese(dlgImportMb3, dlgImportMb3.FileName, utolsoImportMappa)

                MsgBox("A régi preset sikeresen importálva (1-4. csatornára)." & vbCrLf &
                       "A MIDI be-/kimeneti eszköz kiválasztása változatlan maradt - " &
                       "a régi fájl eszközindexei a mai gépen már nem lennének megbízhatóak.",
                       MsgBoxStyle.Information, "MidiBill")
            Catch ex As Exception
                MsgBox("Nem sikerült importálni a(z) """ & dlgImportMb3.FileName & """ fájlt." & vbCrLf &
                       "Lehet, hogy sérült, vagy nem régi MidiBill .mb3 fájl." & vbCrLf & vbCrLf &
                       "Hiba: " & ex.Message, MsgBoxStyle.Exclamation, "MidiBill")
            End Try
        End If
    End Sub

End Class
Imports MidiBill3.Modellek

Public Class MainForm

    ' A 4 hangszerpanel és a köréjük tartozó keret (GroupBox) - a VB6-os
    ' Keret1(0..3) / a bennük lévő vezérlők vezérlőtömbjének megfelelője.
    ' Index: 0 = 1. csatorna, ..., 3 = 4. csatorna (ugyanaz a konvenció, mint a VB6-ban).
    Private hangszerKeretek(3) As GroupBox
    Private hangszerPanelek(3) As HangszerPanel

    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        HangszerPanelekLetrehozasa()
        MidiEszkozokFeltoltese()
    End Sub

    ''' <summary>
    ''' Létrehozza a 4 hangszerpanelt (és a köréjük tartozó keretet) kódból,
    ''' ciklusban - ez váltja ki a VB6-os Tervezőben kézzel elhelyezett,
    ''' vezérlőtömbösített Keret1(0..3) elemeket.
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

            ' Az esemény bekötését (Aktiv csatorna feliratának frissítése,
            ' illetve a MIDI-küldés) a következő lépésben, a MidiEngine
            ' bekötésekor egészítjük ki - egyelőre csak a feliratfrissítést
            ' oldjuk meg, hogy lássuk működni a dinamikus létrehozást.
            AddHandler panel.AktivValtozott, AddressOf HangszerPanel_AktivValtozott
        Next

        ' Alapértelmezetten csak az 1. csatorna aktív - ugyanaz a kezdőállapot,
        ' mint a VB6-os Tervezőben (J1(0).Value = 1 volt bejelölve, a többi nem).
        hangszerPanelek(0).Aktiv = True
        hangszerKeretek(0).Text = "1. hangszer: Aktív"
    End Sub

    ''' <summary>
    ''' A keret feliratának frissítése aktív/inaktív állapot szerint -
    ''' a VB6-os J1_Click "Keret1(Index).Caption = ... & ': Aktív'" logikájának megfelelője.
    ''' </summary>
    Private Sub HangszerPanel_AktivValtozott(sender As Object, aktiv As Boolean)
        Dim panel As HangszerPanel = CType(sender, HangszerPanel)
        Dim index As Integer = Array.IndexOf(hangszerPanelek, panel)
        If index < 0 Then Return

        Dim alapNev As String = (index + 1).ToString() & ". hangszer"
        hangszerKeretek(index).Text = If(aktiv, alapNev & ": Aktív", alapNev)

        ' A tényleges MIDI-küldést (Sound_Held_Notes / Turn_Notes_Off hívása)
        ' a következő lépésben, a MidiEngine bekötésekor adjuk hozzá ide.
    End Sub

    ''' <summary>
    ''' MIDI be- és kimeneti eszközök listájának feltöltése a legördülő
    ''' listákba. A tényleges eszköz-megnyitást (MidiEngine.BemenetMegnyitas/
    ''' KimenetMegnyitas) a következő lépésben kötjük be.
    ''' </summary>
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

        cmbMidiIn.SelectedIndex = 0
        cmbMidiOut.SelectedIndex = 0
    End Sub

End Class
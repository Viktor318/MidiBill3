Imports MidiBill3.Modellek

Public Class HangszerPanel

    ' Belső jelző: betöltés/kezdeti feltöltés közben ne tüzeljünk felesleges eseményeket
    Private betoltesAlatt As Boolean = False

    Public Sub New()
        InitializeComponent()

        cmbHangszer.DropDownStyle = ComboBoxStyle.DropDownList
        betoltesAlatt = True
        For Each nev As String In HangszerLista.Nevek
            cmbHangszer.Items.Add(nev)
        Next
        cmbHangszer.SelectedIndex = 0 ' alapértelmezett hangszer: az első a listában (ua., mint a VB6-ban)
        betoltesAlatt = False

        ' Kezdeti értékkijelzés (a Tervezőben beállított alapértékek alapján)
        lblHerErtek.Text = trkHangero.Value.ToString()
        lblTerErtek.Text = trkTer.Value.ToString()
        lblVibErtek.Text = trkVibrato.Value.ToString()
    End Sub

    ' --- Eseménydeklarációk: ezekre iratkozik fel a MainForm ---

    ''' <summary>Hangszerválasztás változott (a paraméter a 0-127 patch index)</summary>
    Public Event HangszerValtozott(sender As Object, patchIndex As Integer)

    ''' <summary>Hangerő csúszka változott (0-127)</summary>
    Public Event HangeroValtozott(sender As Object, ertek As Integer)

    ''' <summary>Pan (Tér) csúszka változott (-64..63)</summary>
    Public Event TerValtozott(sender As Object, ertek As Integer)

    ''' <summary>Vibrato csúszka változott (0-127)</summary>
    Public Event VibratoValtozott(sender As Object, ertek As Integer)

    ''' <summary>Aktív jelölőnégyzet állapota változott</summary>
    Public Event AktivValtozott(sender As Object, aktiv As Boolean)

    ' --- Publikus property-k: kívülről is olvasható/írható állapot ---
    ' (íráskor NEM tüzelünk eseményt, hogy betöltéskor a MainForm ne
    '  generáljon felesleges MIDI-küldést minden egyes mező beállításakor -
    '  a betöltés végén a MainForm majd egyszer, explicit módon frissíti a MIDI-t)

    Public Property Hangszer As Integer
        Get
            Return cmbHangszer.SelectedIndex
        End Get
        Set(value As Integer)
            betoltesAlatt = True
            cmbHangszer.SelectedIndex = value
            betoltesAlatt = False
        End Set
    End Property

    Public Property Hangero As Integer
        Get
            Return trkHangero.Value
        End Get
        Set(value As Integer)
            betoltesAlatt = True
            trkHangero.Value = value
            betoltesAlatt = False
        End Set
    End Property

    Public Property Ter As Integer
        Get
            Return trkTer.Value
        End Get
        Set(value As Integer)
            betoltesAlatt = True
            trkTer.Value = value
            betoltesAlatt = False
        End Set
    End Property

    Public Property Vibrato As Integer
        Get
            Return trkVibrato.Value
        End Get
        Set(value As Integer)
            betoltesAlatt = True
            trkVibrato.Value = value
            betoltesAlatt = False
        End Set
    End Property

    Public Property Aktiv As Boolean
        Get
            Return chkAktiv.Checked
        End Get
        Set(value As Boolean)
            betoltesAlatt = True
            chkAktiv.Checked = value
            betoltesAlatt = False
        End Set
    End Property

    ' --- Belső eseménykezelők: a vezérlők változásait továbbítjuk kifelé ---

    Private Sub cmbHangszer_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbHangszer.SelectedIndexChanged
        If betoltesAlatt Then Return
        If cmbHangszer.SelectedIndex < 0 Then Return
        RaiseEvent HangszerValtozott(Me, cmbHangszer.SelectedIndex)
    End Sub
    Private Sub trkHangero_ValueChanged(sender As Object, e As EventArgs) Handles trkHangero.ValueChanged
        lblHerErtek.Text = trkHangero.Value.ToString()
        If betoltesAlatt Then Return
        RaiseEvent HangeroValtozott(Me, trkHangero.Value)
    End Sub

    Private Sub trkTer_ValueChanged(sender As Object, e As EventArgs) Handles trkTer.ValueChanged
        lblTerErtek.Text = trkTer.Value.ToString()
        If betoltesAlatt Then Return
        RaiseEvent TerValtozott(Me, trkTer.Value)
    End Sub

    Private Sub trkVibrato_ValueChanged(sender As Object, e As EventArgs) Handles trkVibrato.ValueChanged
        lblVibErtek.Text = trkVibrato.Value.ToString()
        If betoltesAlatt Then Return
        RaiseEvent VibratoValtozott(Me, trkVibrato.Value)
    End Sub

    Private Sub chkAktiv_CheckedChanged(sender As Object, e As EventArgs) Handles chkAktiv.CheckedChanged
        If betoltesAlatt Then Return
        RaiseEvent AktivValtozott(Me, chkAktiv.Checked)
    End Sub

End Class
<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class MidiFelvevoForm
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        grpFelvetel = New GroupBox()
        btnStart = New Button()
        btnStop = New Button()
        lblAllapot = New Label()
        lblGyorsbillentyu = New Label()
        lblElvalaszto1 = New Label()
        chkAutomatikus = New CheckBox()
        lblSzegmensSzamlalo = New Label()
        lblJatekidoKuszob = New Label()
        nudJatekidoKuszob = New NumericUpDown()
        lblSzunetKuszob = New Label()
        nudSzunetKuszob = New NumericUpDown()
        grpMentes = New GroupBox()
        lblFajlNev = New Label()
        txtFajlNev = New TextBox()
        chkDatumAlmappa = New CheckBox()
        lblUtvonalMinta = New Label()
        chkJelentes = New CheckBox()
        lblElvalaszto2 = New Label()
        lblUtoljaraMentve = New Label()
        btnMappaMegnyitasa = New Button()
        chkBetoltesLejatszoba = New CheckBox()
        grpLejatszas = New GroupBox()
        btnFajlMegnyitasa = New Button()
        btnLejatszas = New Button()
        btnLejatszasStop = New Button()
        chkBeallitasokMegtartasa = New CheckBox()
        grpFelvetel.SuspendLayout()
        CType(nudJatekidoKuszob, ComponentModel.ISupportInitialize).BeginInit()
        CType(nudSzunetKuszob, ComponentModel.ISupportInitialize).BeginInit()
        grpMentes.SuspendLayout()
        grpLejatszas.SuspendLayout()
        SuspendLayout()
        ' 
        ' grpFelvetel
        ' 
        grpFelvetel.Controls.Add(btnStart)
        grpFelvetel.Controls.Add(btnStop)
        grpFelvetel.Controls.Add(lblAllapot)
        grpFelvetel.Controls.Add(lblGyorsbillentyu)
        grpFelvetel.Controls.Add(lblElvalaszto1)
        grpFelvetel.Controls.Add(chkAutomatikus)
        grpFelvetel.Controls.Add(lblSzegmensSzamlalo)
        grpFelvetel.Controls.Add(lblJatekidoKuszob)
        grpFelvetel.Controls.Add(nudJatekidoKuszob)
        grpFelvetel.Controls.Add(lblSzunetKuszob)
        grpFelvetel.Controls.Add(nudSzunetKuszob)
        grpFelvetel.Location = New Point(12, 12)
        grpFelvetel.Name = "grpFelvetel"
        grpFelvetel.Size = New Size(536, 206)
        grpFelvetel.TabIndex = 0
        grpFelvetel.TabStop = False
        grpFelvetel.Text = "Felvétel"
        ' 
        ' btnStart
        ' 
        btnStart.Location = New Point(18, 30)
        btnStart.Name = "btnStart"
        btnStart.Size = New Size(100, 42)
        btnStart.TabIndex = 0
        btnStart.Text = "Start"
        btnStart.UseVisualStyleBackColor = True
        ' 
        ' btnStop
        ' 
        btnStop.Location = New Point(126, 30)
        btnStop.Name = "btnStop"
        btnStop.Size = New Size(100, 42)
        btnStop.TabIndex = 1
        btnStop.Text = "Stop"
        btnStop.UseVisualStyleBackColor = True
        ' 
        ' lblAllapot
        ' 
        lblAllapot.BorderStyle = BorderStyle.FixedSingle
        lblAllapot.Font = New Font("Segoe UI", 10F, FontStyle.Bold)
        lblAllapot.Location = New Point(240, 34)
        lblAllapot.Name = "lblAllapot"
        lblAllapot.Size = New Size(278, 34)
        lblAllapot.TabIndex = 2
        lblAllapot.Text = "Nincs felvétel"
        lblAllapot.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' lblGyorsbillentyu
        ' 
        lblGyorsbillentyu.AutoSize = True
        lblGyorsbillentyu.ForeColor = SystemColors.GrayText
        lblGyorsbillentyu.Location = New Point(18, 80)
        lblGyorsbillentyu.Name = "lblGyorsbillentyu"
        lblGyorsbillentyu.Size = New Size(419, 20)
        lblGyorsbillentyu.TabIndex = 3
        lblGyorsbillentyu.Text = "F9 = felvétel, F8 = lejátszás (a főablakban is működik)"
        ' 
        ' lblElvalaszto1
        ' 
        lblElvalaszto1.BorderStyle = BorderStyle.Fixed3D
        lblElvalaszto1.Location = New Point(18, 110)
        lblElvalaszto1.Name = "lblElvalaszto1"
        lblElvalaszto1.Size = New Size(500, 2)
        lblElvalaszto1.TabIndex = 4
        ' 
        ' chkAutomatikus
        ' 
        chkAutomatikus.AutoSize = True
        chkAutomatikus.Location = New Point(18, 124)
        chkAutomatikus.Name = "chkAutomatikus"
        chkAutomatikus.Size = New Size(172, 24)
        chkAutomatikus.TabIndex = 5
        chkAutomatikus.Text = "Automatikus felvétel"
        chkAutomatikus.UseVisualStyleBackColor = True
        ' 
        ' lblSzegmensSzamlalo
        ' 
        lblSzegmensSzamlalo.AutoSize = True
        lblSzegmensSzamlalo.ForeColor = SystemColors.GrayText
        lblSzegmensSzamlalo.Location = New Point(200, 125)
        lblSzegmensSzamlalo.Name = "lblSzegmensSzamlalo"
        lblSzegmensSzamlalo.Size = New Size(203, 20)
        lblSzegmensSzamlalo.TabIndex = 6
        lblSzegmensSzamlalo.Text = ""
        ' 
        ' lblJatekidoKuszob
        ' 
        lblJatekidoKuszob.AutoSize = True
        lblJatekidoKuszob.Location = New Point(18, 163)
        lblJatekidoKuszob.Name = "lblJatekidoKuszob"
        lblJatekidoKuszob.Size = New Size(152, 20)
        lblJatekidoKuszob.TabIndex = 7
        lblJatekidoKuszob.Text = "Játékidő-küszöb (mp):"
        ' 
        ' nudJatekidoKuszob
        ' 
        nudJatekidoKuszob.Location = New Point(180, 160)
        nudJatekidoKuszob.Maximum = New Decimal(New Integer() {600, 0, 0, 0})
        nudJatekidoKuszob.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        nudJatekidoKuszob.Name = "nudJatekidoKuszob"
        nudJatekidoKuszob.Size = New Size(66, 27)
        nudJatekidoKuszob.TabIndex = 8
        nudJatekidoKuszob.Value = New Decimal(New Integer() {5, 0, 0, 0})
        ' 
        ' lblSzunetKuszob
        ' 
        lblSzunetKuszob.AutoSize = True
        lblSzunetKuszob.Location = New Point(276, 163)
        lblSzunetKuszob.Name = "lblSzunetKuszob"
        lblSzunetKuszob.Size = New Size(143, 20)
        lblSzunetKuszob.TabIndex = 9
        lblSzunetKuszob.Text = "Szünet-küszöb (mp):"
        ' 
        ' nudSzunetKuszob
        ' 
        nudSzunetKuszob.Location = New Point(430, 160)
        nudSzunetKuszob.Maximum = New Decimal(New Integer() {600, 0, 0, 0})
        nudSzunetKuszob.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        nudSzunetKuszob.Name = "nudSzunetKuszob"
        nudSzunetKuszob.Size = New Size(66, 27)
        nudSzunetKuszob.TabIndex = 10
        nudSzunetKuszob.Value = New Decimal(New Integer() {3, 0, 0, 0})
        ' 
        ' grpMentes
        ' 
        grpMentes.Controls.Add(lblFajlNev)
        grpMentes.Controls.Add(txtFajlNev)
        grpMentes.Controls.Add(chkDatumAlmappa)
        grpMentes.Controls.Add(lblUtvonalMinta)
        grpMentes.Controls.Add(chkJelentes)
        grpMentes.Controls.Add(chkBetoltesLejatszoba)
        grpMentes.Controls.Add(lblElvalaszto2)
        grpMentes.Controls.Add(lblUtoljaraMentve)
        grpMentes.Controls.Add(btnMappaMegnyitasa)
        grpMentes.Location = New Point(12, 228)
        grpMentes.Name = "grpMentes"
        grpMentes.Size = New Size(536, 292)
        grpMentes.TabIndex = 1
        grpMentes.TabStop = False
        grpMentes.Text = "Mentés"
        ' 
        ' lblFajlNev
        ' 
        lblFajlNev.AutoSize = True
        lblFajlNev.Location = New Point(18, 35)
        lblFajlNev.Name = "lblFajlNev"
        lblFajlNev.Size = New Size(131, 20)
        lblFajlNev.TabIndex = 0
        lblFajlNev.Text = "Mentett fájl neve:"
        ' 
        ' txtFajlNev
        ' 
        txtFajlNev.Location = New Point(160, 32)
        txtFajlNev.MaxLength = 80
        txtFajlNev.Name = "txtFajlNev"
        txtFajlNev.Size = New Size(260, 27)
        txtFajlNev.TabIndex = 1
        ' 
        ' chkDatumAlmappa
        ' 
        chkDatumAlmappa.AutoSize = True
        chkDatumAlmappa.Checked = True
        chkDatumAlmappa.CheckState = CheckState.Checked
        chkDatumAlmappa.Location = New Point(18, 68)
        chkDatumAlmappa.Name = "chkDatumAlmappa"
        chkDatumAlmappa.Size = New Size(193, 24)
        chkDatumAlmappa.TabIndex = 2
        chkDatumAlmappa.Text = "Almappák dátum szerint"
        chkDatumAlmappa.UseVisualStyleBackColor = True
        ' 
        ' lblUtvonalMinta
        ' 
        lblUtvonalMinta.BackColor = SystemColors.Window
        lblUtvonalMinta.BorderStyle = BorderStyle.FixedSingle
        lblUtvonalMinta.Font = New Font("Consolas", 8.5F)
        lblUtvonalMinta.ForeColor = SystemColors.GrayText
        lblUtvonalMinta.Location = New Point(18, 100)
        lblUtvonalMinta.Name = "lblUtvonalMinta"
        lblUtvonalMinta.Padding = New Padding(4, 2, 4, 2)
        lblUtvonalMinta.Size = New Size(500, 48)
        lblUtvonalMinta.TabIndex = 3
        ' 
        ' chkJelentes
        ' 
        chkJelentes.AutoSize = True
        chkJelentes.Checked = True
        chkJelentes.CheckState = CheckState.Checked
        chkJelentes.Location = New Point(18, 160)
        chkJelentes.Name = "chkJelentes"
        chkJelentes.Size = New Size(274, 24)
        chkJelentes.TabIndex = 4
        chkJelentes.Text = "Jelentés készítése a felvétel mellé"
        chkJelentes.UseVisualStyleBackColor = True
        ' 
        ' lblElvalaszto2
        ' 
        lblElvalaszto2.BorderStyle = BorderStyle.Fixed3D
        lblElvalaszto2.Location = New Point(18, 226)
        lblElvalaszto2.Name = "lblElvalaszto2"
        lblElvalaszto2.Size = New Size(500, 2)
        lblElvalaszto2.TabIndex = 5
        ' 
        ' lblUtoljaraMentve
        ' 
        lblUtoljaraMentve.AutoEllipsis = True
        lblUtoljaraMentve.Location = New Point(18, 244)
        lblUtoljaraMentve.Name = "lblUtoljaraMentve"
        lblUtoljaraMentve.Size = New Size(346, 26)
        lblUtoljaraMentve.TabIndex = 6
        lblUtoljaraMentve.Text = "Utoljára mentve: -"
        lblUtoljaraMentve.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' btnMappaMegnyitasa
        ' 
        btnMappaMegnyitasa.Location = New Point(372, 238)
        btnMappaMegnyitasa.Name = "btnMappaMegnyitasa"
        btnMappaMegnyitasa.Size = New Size(146, 36)
        btnMappaMegnyitasa.TabIndex = 7
        btnMappaMegnyitasa.Text = "Mappa megnyitása"
        btnMappaMegnyitasa.UseVisualStyleBackColor = True
        ' 
        ' chkBetoltesLejatszoba
        ' 
        chkBetoltesLejatszoba.AutoSize = True
        chkBetoltesLejatszoba.Checked = True
        chkBetoltesLejatszoba.CheckState = CheckState.Checked
        chkBetoltesLejatszoba.Location = New Point(18, 190)
        chkBetoltesLejatszoba.Name = "chkBetoltesLejatszoba"
        chkBetoltesLejatszoba.Size = New Size(281, 24)
        chkBetoltesLejatszoba.TabIndex = 8
        chkBetoltesLejatszoba.Text = "Mentett fájl betöltése a lejátszóba"
        chkBetoltesLejatszoba.UseVisualStyleBackColor = True
        ' 
        ' grpLejatszas
        ' 
        grpLejatszas.Controls.Add(btnFajlMegnyitasa)
        grpLejatszas.Controls.Add(btnLejatszas)
        grpLejatszas.Controls.Add(btnLejatszasStop)
        grpLejatszas.Controls.Add(chkBeallitasokMegtartasa)
        grpLejatszas.Location = New Point(12, 530)
        grpLejatszas.Name = "grpLejatszas"
        grpLejatszas.Size = New Size(536, 122)
        grpLejatszas.TabIndex = 2
        grpLejatszas.TabStop = False
        grpLejatszas.Text = "Lejátszás"
        ' 
        ' btnFajlMegnyitasa
        ' 
        btnFajlMegnyitasa.Location = New Point(18, 30)
        btnFajlMegnyitasa.Name = "btnFajlMegnyitasa"
        btnFajlMegnyitasa.Size = New Size(170, 42)
        btnFajlMegnyitasa.TabIndex = 0
        btnFajlMegnyitasa.Text = "Fájl megnyitása..."
        btnFajlMegnyitasa.UseVisualStyleBackColor = True
        ' 
        ' btnLejatszas
        ' 
        btnLejatszas.Location = New Point(196, 30)
        btnLejatszas.Name = "btnLejatszas"
        btnLejatszas.Size = New Size(160, 42)
        btnLejatszas.TabIndex = 1
        btnLejatszas.Text = "▶ Lejátszás"
        btnLejatszas.UseVisualStyleBackColor = True
        ' 
        ' btnLejatszasStop
        ' 
        btnLejatszasStop.Location = New Point(364, 30)
        btnLejatszasStop.Name = "btnLejatszasStop"
        btnLejatszasStop.Size = New Size(154, 42)
        btnLejatszasStop.TabIndex = 2
        btnLejatszasStop.Text = "■ Stop"
        btnLejatszasStop.UseVisualStyleBackColor = True
        ' 
        ' chkBeallitasokMegtartasa
        ' 
        chkBeallitasokMegtartasa.AutoSize = True
        chkBeallitasokMegtartasa.Location = New Point(18, 84)
        chkBeallitasokMegtartasa.Name = "chkBeallitasokMegtartasa"
        chkBeallitasokMegtartasa.Size = New Size(341, 24)
        chkBeallitasokMegtartasa.TabIndex = 3
        chkBeallitasokMegtartasa.Text = "A lejátszott fájl beállításainak megtartása"
        chkBeallitasokMegtartasa.UseVisualStyleBackColor = True
        ' 
        ' MidiFelvevoForm
        ' 
        AutoScaleDimensions = New SizeF(120F, 120F)
        AutoScaleMode = AutoScaleMode.Dpi
        ClientSize = New Size(560, 664)
        Controls.Add(grpLejatszas)
        Controls.Add(grpMentes)
        Controls.Add(grpFelvetel)
        FormBorderStyle = FormBorderStyle.FixedSingle
        KeyPreview = True
        MaximizeBox = False
        MinimizeBox = False
        Name = "MidiFelvevoForm"
        ShowInTaskbar = False
        StartPosition = FormStartPosition.Manual
        Text = "MIDI felvétel"
        grpFelvetel.ResumeLayout(False)
        grpFelvetel.PerformLayout()
        CType(nudJatekidoKuszob, ComponentModel.ISupportInitialize).EndInit()
        CType(nudSzunetKuszob, ComponentModel.ISupportInitialize).EndInit()
        grpMentes.ResumeLayout(False)
        grpMentes.PerformLayout()
        grpLejatszas.ResumeLayout(False)
        grpLejatszas.PerformLayout()
        ResumeLayout(False)
    End Sub

    Friend WithEvents grpFelvetel As GroupBox
    Friend WithEvents btnStart As Button
    Friend WithEvents btnStop As Button
    Friend WithEvents lblAllapot As Label
    Friend WithEvents lblGyorsbillentyu As Label
    Friend WithEvents lblElvalaszto1 As Label
    Friend WithEvents chkAutomatikus As CheckBox
    Friend WithEvents lblSzegmensSzamlalo As Label
    Friend WithEvents lblJatekidoKuszob As Label
    Friend WithEvents nudJatekidoKuszob As NumericUpDown
    Friend WithEvents lblSzunetKuszob As Label
    Friend WithEvents nudSzunetKuszob As NumericUpDown
    Friend WithEvents grpMentes As GroupBox
    Friend WithEvents lblFajlNev As Label
    Friend WithEvents txtFajlNev As TextBox
    Friend WithEvents chkDatumAlmappa As CheckBox
    Friend WithEvents lblUtvonalMinta As Label
    Friend WithEvents chkJelentes As CheckBox
    Friend WithEvents lblElvalaszto2 As Label
    Friend WithEvents lblUtoljaraMentve As Label
    Friend WithEvents btnMappaMegnyitasa As Button
    Friend WithEvents chkBetoltesLejatszoba As CheckBox
    Friend WithEvents grpLejatszas As GroupBox
    Friend WithEvents btnFajlMegnyitasa As Button
    Friend WithEvents btnLejatszas As Button
    Friend WithEvents btnLejatszasStop As Button
    Friend WithEvents chkBeallitasokMegtartasa As CheckBox

End Class

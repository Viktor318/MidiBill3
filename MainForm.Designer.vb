<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class MainForm
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
        menuStrip1 = New MenuStrip()
        mnuFajl = New ToolStripMenuItem()
        mnuBetolt = New ToolStripMenuItem()
        mnuMent = New ToolStripMenuItem()
        ToolStripSeparator1 = New ToolStripSeparator()
        mnuKilepes = New ToolStripMenuItem()
        chkPedal = New CheckBox()
        grpVolume = New GroupBox()
        trkAlapHangero = New TrackBar()
        grpMidiIn = New GroupBox()
        cmbMidiIn = New ComboBox()
        grpMidiOut = New GroupBox()
        cmbMidiOut = New ComboBox()
        btnReset = New Button()
        btnKilepes = New Button()
        lblMidiUzenet = New Label()
        dlgMegnyitas = New OpenFileDialog()
        dlgMentes = New SaveFileDialog()
        mnuImportMb3 = New ToolStripMenuItem()
        ToolStripSeparator2 = New ToolStripSeparator()
        dlgImportMb3 = New OpenFileDialog()
        menuStrip1.SuspendLayout()
        grpVolume.SuspendLayout()
        CType(trkAlapHangero, ComponentModel.ISupportInitialize).BeginInit()
        grpMidiIn.SuspendLayout()
        grpMidiOut.SuspendLayout()
        SuspendLayout()
        ' 
        ' menuStrip1
        ' 
        menuStrip1.ImageScalingSize = New Size(20, 20)
        menuStrip1.Items.AddRange(New ToolStripItem() {mnuFajl})
        menuStrip1.Location = New Point(0, 0)
        menuStrip1.Name = "menuStrip1"
        menuStrip1.Size = New Size(942, 28)
        menuStrip1.TabIndex = 0
        menuStrip1.Text = "MenuStrip1"
        ' 
        ' mnuFajl
        ' 
        mnuFajl.DropDownItems.AddRange(New ToolStripItem() {mnuBetolt, mnuMent, ToolStripSeparator2, mnuImportMb3, ToolStripSeparator1, mnuKilepes})
        mnuFajl.Name = "mnuFajl"
        mnuFajl.Size = New Size(45, 24)
        mnuFajl.Text = "&Fájl"
        ' 
        ' mnuBetolt
        ' 
        mnuBetolt.Name = "mnuBetolt"
        mnuBetolt.Size = New Size(287, 26)
        mnuBetolt.Text = "Adatok &betöltése"
        ' 
        ' mnuMent
        ' 
        mnuMent.Name = "mnuMent"
        mnuMent.Size = New Size(287, 26)
        mnuMent.Text = "Adatok &mentése"
        ' 
        ' ToolStripSeparator1
        ' 
        ToolStripSeparator1.Name = "ToolStripSeparator1"
        ToolStripSeparator1.Size = New Size(284, 6)
        ' 
        ' mnuKilepes
        ' 
        mnuKilepes.Name = "mnuKilepes"
        mnuKilepes.Size = New Size(287, 26)
        mnuKilepes.Text = "&Kilépés"
        ' 
        ' chkPedal
        ' 
        chkPedal.Location = New Point(324, 652)
        chkPedal.Name = "chkPedal"
        chkPedal.Size = New Size(242, 25)
        chkPedal.TabIndex = 1
        chkPedal.Text = "Fordított pedál működés"
        chkPedal.UseVisualStyleBackColor = True
        ' 
        ' grpVolume
        ' 
        grpVolume.Controls.Add(trkAlapHangero)
        grpVolume.Location = New Point(625, 32)
        grpVolume.Name = "grpVolume"
        grpVolume.Size = New Size(277, 85)
        grpVolume.TabIndex = 2
        grpVolume.TabStop = False
        grpVolume.Text = "Alap hangerő beállítása: 70"
        ' 
        ' trkAlapHangero
        ' 
        trkAlapHangero.AutoSize = False
        trkAlapHangero.Location = New Point(12, 24)
        trkAlapHangero.Maximum = 100
        trkAlapHangero.Name = "trkAlapHangero"
        trkAlapHangero.Size = New Size(253, 56)
        trkAlapHangero.TabIndex = 3
        trkAlapHangero.TickFrequency = 10
        trkAlapHangero.Value = 70
        ' 
        ' grpMidiIn
        ' 
        grpMidiIn.Controls.Add(cmbMidiIn)
        grpMidiIn.Location = New Point(625, 140)
        grpMidiIn.Name = "grpMidiIn"
        grpMidiIn.Size = New Size(277, 85)
        grpMidiIn.TabIndex = 3
        grpMidiIn.TabStop = False
        grpMidiIn.Text = "MIDI In"
        ' 
        ' cmbMidiIn
        ' 
        cmbMidiIn.FormattingEnabled = True
        cmbMidiIn.Location = New Point(12, 36)
        cmbMidiIn.Name = "cmbMidiIn"
        cmbMidiIn.Size = New Size(253, 28)
        cmbMidiIn.TabIndex = 0
        ' 
        ' grpMidiOut
        ' 
        grpMidiOut.Controls.Add(cmbMidiOut)
        grpMidiOut.Location = New Point(625, 248)
        grpMidiOut.Name = "grpMidiOut"
        grpMidiOut.Size = New Size(277, 85)
        grpMidiOut.TabIndex = 4
        grpMidiOut.TabStop = False
        grpMidiOut.Text = "MIDI Out"
        ' 
        ' cmbMidiOut
        ' 
        cmbMidiOut.FormattingEnabled = True
        cmbMidiOut.Location = New Point(12, 36)
        cmbMidiOut.Name = "cmbMidiOut"
        cmbMidiOut.Size = New Size(253, 28)
        cmbMidiOut.TabIndex = 0
        ' 
        ' btnReset
        ' 
        btnReset.Location = New Point(625, 350)
        btnReset.Name = "btnReset"
        btnReset.Size = New Size(132, 60)
        btnReset.TabIndex = 5
        btnReset.Text = "Csend legyen!"
        btnReset.UseVisualStyleBackColor = True
        ' 
        ' btnKilepes
        ' 
        btnKilepes.Location = New Point(770, 350)
        btnKilepes.Name = "btnKilepes"
        btnKilepes.Size = New Size(132, 60)
        btnKilepes.TabIndex = 6
        btnKilepes.Text = "Kilépés"
        btnKilepes.UseVisualStyleBackColor = True
        ' 
        ' lblMidiUzenet
        ' 
        lblMidiUzenet.Location = New Point(24, 652)
        lblMidiUzenet.Name = "lblMidiUzenet"
        lblMidiUzenet.Size = New Size(270, 25)
        lblMidiUzenet.TabIndex = 7
        lblMidiUzenet.Text = "MIDI üzenetek:"
        ' 
        ' dlgMegnyitas
        ' 
        dlgMegnyitas.Filter = "MidiBill adatok (*.json)|*.json"
        ' 
        ' dlgMentes
        ' 
        dlgMentes.Filter = "MidiBill adatok (*.json)|*.json"
        ' 
        ' mnuImportMb3
        ' 
        mnuImportMb3.Name = "mnuImportMb3"
        mnuImportMb3.Size = New Size(287, 26)
        mnuImportMb3.Text = "Régi .mb3 preset &importálása"
        ' 
        ' ToolStripSeparator2
        ' 
        ToolStripSeparator2.Name = "ToolStripSeparator2"
        ToolStripSeparator2.Size = New Size(284, 6)
        ' 
        ' dlgImportMb3
        ' 
        dlgImportMb3.Filter = "Régi MidiBill adatok (*.mb3)|*.mb3"
        ' 
        ' MainForm
        ' 
        AutoScaleDimensions = New SizeF(8F, 20F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(942, 693)
        Controls.Add(lblMidiUzenet)
        Controls.Add(btnKilepes)
        Controls.Add(btnReset)
        Controls.Add(grpMidiOut)
        Controls.Add(grpMidiIn)
        Controls.Add(grpVolume)
        Controls.Add(chkPedal)
        Controls.Add(menuStrip1)
        MainMenuStrip = menuStrip1
        Name = "MainForm"
        StartPosition = FormStartPosition.CenterScreen
        Text = "MainForm"
        menuStrip1.ResumeLayout(False)
        menuStrip1.PerformLayout()
        grpVolume.ResumeLayout(False)
        CType(trkAlapHangero, ComponentModel.ISupportInitialize).EndInit()
        grpMidiIn.ResumeLayout(False)
        grpMidiOut.ResumeLayout(False)
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents menuStrip1 As MenuStrip
    Friend WithEvents chkPedal As CheckBox
    Friend WithEvents grpVolume As GroupBox
    Friend WithEvents trkAlapHangero As TrackBar
    Friend WithEvents grpMidiIn As GroupBox
    Friend WithEvents grpMidiOut As GroupBox
    Friend WithEvents cmbMidiIn As ComboBox
    Friend WithEvents cmbMidiOut As ComboBox
    Friend WithEvents btnReset As Button
    Friend WithEvents btnKilepes As Button
    Friend WithEvents lblMidiUzenet As Label
    Friend WithEvents mnuFajl As ToolStripMenuItem
    Friend WithEvents mnuBetolt As ToolStripMenuItem
    Friend WithEvents mnuMent As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator1 As ToolStripSeparator
    Friend WithEvents mnuKilepes As ToolStripMenuItem
    Friend WithEvents dlgMegnyitas As OpenFileDialog
    Friend WithEvents dlgMentes As SaveFileDialog
    Friend WithEvents mnuImportMb3 As ToolStripMenuItem
    Friend WithEvents ToolStripSeparator2 As ToolStripSeparator
    Friend WithEvents dlgImportMb3 As OpenFileDialog

End Class

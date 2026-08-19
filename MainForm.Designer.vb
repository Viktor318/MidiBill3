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
        Dim resources As System.ComponentModel.ComponentResourceManager = New System.ComponentModel.ComponentResourceManager(GetType(MainForm))
        menuStrip1 = New MenuStrip()
        mnuFajl = New ToolStripMenuItem()
        mnuBetolt = New ToolStripMenuItem()
        mnuMent = New ToolStripMenuItem()
        ToolStripSeparator2 = New ToolStripSeparator()
        mnuImportMb3 = New ToolStripMenuItem()
        ToolStripSeparator1 = New ToolStripSeparator()
        mnuKilepes = New ToolStripMenuItem()
        mnuKilepesMentesNelkul = New ToolStripMenuItem()
        SúgóToolStripMenuItem = New ToolStripMenuItem()
        mnuNevjegy = New ToolStripMenuItem()
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
        dlgImportMb3 = New OpenFileDialog()
        grpSplit = New GroupBox()
        lblSplitInfo = New Label()
        btnSplitTanulas = New Button()
        cmbSplitPont = New ComboBox()
        lblSplitCim = New Label()
        grpTranspose = New GroupBox()
        btnOktavLe = New Button()
        btnOktavFel = New Button()
        btnFelhangLe = New Button()
        btnFelhangFel = New Button()
        radCsoport1 = New RadioButton()
        radCsoport2 = New RadioButton()
        grpCsatornaValaszto = New GroupBox()
        radCsoport4 = New RadioButton()
        radCsoport3 = New RadioButton()
        btnMidiFrissites = New Button()
        grpReverb = New GroupBox()
        trkReverb = New TrackBar()
        menuStrip1.SuspendLayout()
        grpVolume.SuspendLayout()
        CType(trkAlapHangero, ComponentModel.ISupportInitialize).BeginInit()
        grpMidiIn.SuspendLayout()
        grpMidiOut.SuspendLayout()
        grpSplit.SuspendLayout()
        grpTranspose.SuspendLayout()
        grpCsatornaValaszto.SuspendLayout()
        grpReverb.SuspendLayout()
        CType(trkReverb, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' menuStrip1
        ' 
        menuStrip1.ImageScalingSize = New Size(20, 20)
        menuStrip1.Items.AddRange(New ToolStripItem() {mnuFajl, SúgóToolStripMenuItem})
        menuStrip1.Location = New Point(0, 0)
        menuStrip1.Name = "menuStrip1"
        menuStrip1.Size = New Size(942, 28)
        menuStrip1.TabIndex = 0
        menuStrip1.Text = "MenuStrip1"
        ' 
        ' mnuFajl
        ' 
        mnuFajl.DropDownItems.AddRange(New ToolStripItem() {mnuBetolt, mnuMent, ToolStripSeparator2, mnuImportMb3, ToolStripSeparator1, mnuKilepes, mnuKilepesMentesNelkul})
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
        ' ToolStripSeparator2
        ' 
        ToolStripSeparator2.Name = "ToolStripSeparator2"
        ToolStripSeparator2.Size = New Size(284, 6)
        ' 
        ' mnuImportMb3
        ' 
        mnuImportMb3.Name = "mnuImportMb3"
        mnuImportMb3.Size = New Size(287, 26)
        mnuImportMb3.Text = "Régi .mb3 preset &importálása"
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
        ' mnuKilepesMentesNelkul
        ' 
        mnuKilepesMentesNelkul.Name = "mnuKilepesMentesNelkul"
        mnuKilepesMentesNelkul.Size = New Size(287, 26)
        mnuKilepesMentesNelkul.Text = "Kilépés &mentés nélkül"
        ' 
        ' SúgóToolStripMenuItem
        ' 
        SúgóToolStripMenuItem.DropDownItems.AddRange(New ToolStripItem() {mnuNevjegy})
        SúgóToolStripMenuItem.Name = "SúgóToolStripMenuItem"
        SúgóToolStripMenuItem.Size = New Size(57, 24)
        SúgóToolStripMenuItem.Text = "&Súgó"
        ' 
        ' mnuNevjegy
        ' 
        mnuNevjegy.Name = "mnuNevjegy"
        mnuNevjegy.Size = New Size(155, 26)
        mnuNevjegy.Text = "&Névjegy..."
        ' 
        ' chkPedal
        ' 
        chkPedal.Location = New Point(640, 804)
        chkPedal.Name = "chkPedal"
        chkPedal.Size = New Size(220, 25)
        chkPedal.TabIndex = 1
        chkPedal.Text = "Fordított pedál működés"
        chkPedal.UseVisualStyleBackColor = True
        ' 
        ' grpVolume
        ' 
        grpVolume.Controls.Add(trkAlapHangero)
        grpVolume.Location = New Point(640, 53)
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
        grpMidiIn.Location = New Point(640, 143)
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
        grpMidiOut.Location = New Point(640, 233)
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
        btnReset.Font = New Font("Segoe UI", 9F, FontStyle.Bold Or FontStyle.Italic)
        btnReset.Location = New Point(640, 709)
        btnReset.Name = "btnReset"
        btnReset.Size = New Size(132, 60)
        btnReset.TabIndex = 5
        btnReset.Text = "Csend legyen!"
        btnReset.UseVisualStyleBackColor = True
        ' 
        ' btnKilepes
        ' 
        btnKilepes.Font = New Font("Segoe UI", 9F, FontStyle.Bold)
        btnKilepes.Location = New Point(785, 709)
        btnKilepes.Name = "btnKilepes"
        btnKilepes.Size = New Size(132, 60)
        btnKilepes.TabIndex = 6
        btnKilepes.Text = "Kilépés"
        btnKilepes.UseVisualStyleBackColor = True
        ' 
        ' lblMidiUzenet
        ' 
        lblMidiUzenet.Font = New Font("Segoe UI", 9F)
        lblMidiUzenet.Location = New Point(331, 803)
        lblMidiUzenet.Name = "lblMidiUzenet"
        lblMidiUzenet.Size = New Size(280, 25)
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
        ' dlgImportMb3
        ' 
        dlgImportMb3.Filter = "Régi MidiBill adatok (*.mb3)|*.mb3"
        ' 
        ' grpSplit
        ' 
        grpSplit.Controls.Add(lblSplitInfo)
        grpSplit.Controls.Add(btnSplitTanulas)
        grpSplit.Controls.Add(cmbSplitPont)
        grpSplit.Controls.Add(lblSplitCim)
        grpSplit.Location = New Point(640, 385)
        grpSplit.Name = "grpSplit"
        grpSplit.Size = New Size(277, 110)
        grpSplit.TabIndex = 8
        grpSplit.TabStop = False
        grpSplit.Text = "Split pont"
        ' 
        ' lblSplitInfo
        ' 
        lblSplitInfo.Font = New Font("Segoe UI", 7.8F, FontStyle.Regular, GraphicsUnit.Point, CByte(238))
        lblSplitInfo.Location = New Point(162, 64)
        lblSplitInfo.Name = "lblSplitInfo"
        lblSplitInfo.Size = New Size(104, 34)
        lblSplitInfo.TabIndex = 3
        lblSplitInfo.TextAlign = ContentAlignment.MiddleLeft
        ' 
        ' btnSplitTanulas
        ' 
        btnSplitTanulas.Location = New Point(32, 64)
        btnSplitTanulas.Name = "btnSplitTanulas"
        btnSplitTanulas.Size = New Size(120, 34)
        btnSplitTanulas.TabIndex = 2
        btnSplitTanulas.Text = "Tanulás..."
        btnSplitTanulas.UseVisualStyleBackColor = True
        ' 
        ' cmbSplitPont
        ' 
        cmbSplitPont.FormattingEnabled = True
        cmbSplitPont.Location = New Point(122, 26)
        cmbSplitPont.Name = "cmbSplitPont"
        cmbSplitPont.Size = New Size(120, 28)
        cmbSplitPont.TabIndex = 1
        ' 
        ' lblSplitCim
        ' 
        lblSplitCim.Location = New Point(32, 28)
        lblSplitCim.Name = "lblSplitCim"
        lblSplitCim.Size = New Size(85, 26)
        lblSplitCim.TabIndex = 0
        lblSplitCim.Text = "Split pont:"
        ' 
        ' grpTranspose
        ' 
        grpTranspose.Controls.Add(btnOktavLe)
        grpTranspose.Controls.Add(btnOktavFel)
        grpTranspose.Controls.Add(btnFelhangLe)
        grpTranspose.Controls.Add(btnFelhangFel)
        grpTranspose.Location = New Point(640, 500)
        grpTranspose.Name = "grpTranspose"
        grpTranspose.Size = New Size(277, 112)
        grpTranspose.TabIndex = 9
        grpTranspose.TabStop = False
        grpTranspose.Text = "Transzponálás: 0"
        ' 
        ' btnOktavLe
        ' 
        btnOktavLe.Location = New Point(142, 62)
        btnOktavLe.Name = "btnOktavLe"
        btnOktavLe.Size = New Size(122, 34)
        btnOktavLe.TabIndex = 3
        btnOktavLe.Text = "Oktáv le"
        btnOktavLe.UseVisualStyleBackColor = True
        ' 
        ' btnOktavFel
        ' 
        btnOktavFel.Location = New Point(12, 62)
        btnOktavFel.Name = "btnOktavFel"
        btnOktavFel.Size = New Size(122, 34)
        btnOktavFel.TabIndex = 2
        btnOktavFel.Text = "Oktáv fel"
        btnOktavFel.UseVisualStyleBackColor = True
        ' 
        ' btnFelhangLe
        ' 
        btnFelhangLe.Location = New Point(142, 22)
        btnFelhangLe.Name = "btnFelhangLe"
        btnFelhangLe.Size = New Size(122, 34)
        btnFelhangLe.TabIndex = 1
        btnFelhangLe.Text = "Félhang le"
        btnFelhangLe.UseVisualStyleBackColor = True
        ' 
        ' btnFelhangFel
        ' 
        btnFelhangFel.Location = New Point(12, 22)
        btnFelhangFel.Name = "btnFelhangFel"
        btnFelhangFel.Size = New Size(122, 34)
        btnFelhangFel.TabIndex = 0
        btnFelhangFel.Text = "Félhang fel"
        btnFelhangFel.UseVisualStyleBackColor = True
        ' 
        ' radCsoport1
        ' 
        radCsoport1.Checked = True
        radCsoport1.Location = New Point(12, 26)
        radCsoport1.Name = "radCsoport1"
        radCsoport1.Size = New Size(55, 25)
        radCsoport1.TabIndex = 11
        radCsoport1.TabStop = True
        radCsoport1.Text = "1-4"
        radCsoport1.UseVisualStyleBackColor = True
        ' 
        ' radCsoport2
        ' 
        radCsoport2.Location = New Point(80, 26)
        radCsoport2.Name = "radCsoport2"
        radCsoport2.Size = New Size(55, 25)
        radCsoport2.TabIndex = 12
        radCsoport2.Text = "5-8"
        radCsoport2.UseVisualStyleBackColor = True
        ' 
        ' grpCsatornaValaszto
        ' 
        grpCsatornaValaszto.Controls.Add(radCsoport4)
        grpCsatornaValaszto.Controls.Add(radCsoport3)
        grpCsatornaValaszto.Controls.Add(radCsoport1)
        grpCsatornaValaszto.Controls.Add(radCsoport2)
        grpCsatornaValaszto.Location = New Point(25, 777)
        grpCsatornaValaszto.Name = "grpCsatornaValaszto"
        grpCsatornaValaszto.Size = New Size(300, 64)
        grpCsatornaValaszto.TabIndex = 13
        grpCsatornaValaszto.TabStop = False
        grpCsatornaValaszto.Text = "MIDI csatornák"
        ' 
        ' radCsoport4
        ' 
        radCsoport4.Location = New Point(224, 26)
        radCsoport4.Name = "radCsoport4"
        radCsoport4.Size = New Size(70, 25)
        radCsoport4.TabIndex = 14
        radCsoport4.Text = "13-16"
        radCsoport4.UseVisualStyleBackColor = True
        ' 
        ' radCsoport3
        ' 
        radCsoport3.Location = New Point(148, 26)
        radCsoport3.Name = "radCsoport3"
        radCsoport3.Size = New Size(60, 25)
        radCsoport3.TabIndex = 13
        radCsoport3.Text = "9-12"
        radCsoport3.UseVisualStyleBackColor = True
        ' 
        ' btnMidiFrissites
        ' 
        btnMidiFrissites.Location = New Point(640, 325)
        btnMidiFrissites.Name = "btnMidiFrissites"
        btnMidiFrissites.Size = New Size(277, 50)
        btnMidiFrissites.TabIndex = 14
        btnMidiFrissites.Text = "MIDI eszközök frissítése"
        btnMidiFrissites.UseVisualStyleBackColor = True
        ' 
        ' grpReverb
        ' 
        grpReverb.Controls.Add(trkReverb)
        grpReverb.Location = New Point(640, 617)
        grpReverb.Name = "grpReverb"
        grpReverb.Size = New Size(277, 85)
        grpReverb.TabIndex = 15
        grpReverb.TabStop = False
        grpReverb.Text = "Reverb: 0%"
        ' 
        ' trkReverb
        ' 
        trkReverb.AutoSize = False
        trkReverb.Location = New Point(12, 24)
        trkReverb.Maximum = 100
        trkReverb.Name = "trkReverb"
        trkReverb.Size = New Size(253, 56)
        trkReverb.TabIndex = 0
        trkReverb.TickFrequency = 10
        ' 
        ' MainForm
        ' 
        AutoScaleDimensions = New SizeF(120.0F, 120.0F)
        AutoScaleMode = AutoScaleMode.Dpi
        ClientSize = New Size(942, 866)
        Controls.Add(grpReverb)
        Controls.Add(btnMidiFrissites)
        Controls.Add(grpCsatornaValaszto)
        Controls.Add(grpTranspose)
        Controls.Add(grpSplit)
        Controls.Add(lblMidiUzenet)
        Controls.Add(btnKilepes)
        Controls.Add(btnReset)
        Controls.Add(grpMidiOut)
        Controls.Add(grpMidiIn)
        Controls.Add(grpVolume)
        Controls.Add(chkPedal)
        Controls.Add(menuStrip1)
        FormBorderStyle = FormBorderStyle.FixedSingle
        Icon = CType(resources.GetObject("$this.Icon"), Icon)
        MainMenuStrip = menuStrip1
        MaximizeBox = False
        MinimizeBox = False
        Name = "MainForm"
        StartPosition = FormStartPosition.CenterScreen
        Text = "MainForm"
        menuStrip1.ResumeLayout(False)
        menuStrip1.PerformLayout()
        grpVolume.ResumeLayout(False)
        CType(trkAlapHangero, ComponentModel.ISupportInitialize).EndInit()
        grpMidiIn.ResumeLayout(False)
        grpMidiOut.ResumeLayout(False)
        grpSplit.ResumeLayout(False)
        grpTranspose.ResumeLayout(False)
        grpCsatornaValaszto.ResumeLayout(False)
        grpReverb.ResumeLayout(False)
        CType(trkReverb, ComponentModel.ISupportInitialize).EndInit()
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
    Friend WithEvents grpSplit As GroupBox
    Friend WithEvents lblSplitCim As Label
    Friend WithEvents btnSplitTanulas As Button
    Friend WithEvents cmbSplitPont As ComboBox
    Friend WithEvents lblSplitInfo As Label
    Friend WithEvents SúgóToolStripMenuItem As ToolStripMenuItem
    Friend WithEvents mnuNevjegy As ToolStripMenuItem
    Friend WithEvents grpTranspose As GroupBox
    Friend WithEvents btnOktavLe As Button
    Friend WithEvents btnOktavFel As Button
    Friend WithEvents btnFelhangLe As Button
    Friend WithEvents btnFelhangFel As Button
    Friend WithEvents mnuKilepesMentesNelkul As ToolStripMenuItem
    Friend WithEvents radCsoport1 As RadioButton
    Friend WithEvents radCsoport2 As RadioButton
    Friend WithEvents grpCsatornaValaszto As GroupBox
    Friend WithEvents radCsoport3 As RadioButton
    Friend WithEvents radCsoport4 As RadioButton
    Friend WithEvents btnMidiFrissites As Button
    Friend WithEvents grpReverb As GroupBox
    Friend WithEvents trkReverb As TrackBar

End Class

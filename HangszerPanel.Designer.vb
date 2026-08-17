<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class HangszerPanel
    Inherits System.Windows.Forms.UserControl

    'UserControl overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
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
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        cmbHangszer = New ComboBox()
        trkHangero = New TrackBar()
        trkTer = New TrackBar()
        trkVibrato = New TrackBar()
        lblHer = New Label()
        lblTer = New Label()
        lblVib = New Label()
        chkAktiv = New CheckBox()
        lblHerErtek = New Label()
        lblTerErtek = New Label()
        lblVibErtek = New Label()
        chkAlul = New CheckBox()
        chkFelul = New CheckBox()
        CType(trkHangero, ComponentModel.ISupportInitialize).BeginInit()
        CType(trkTer, ComponentModel.ISupportInitialize).BeginInit()
        CType(trkVibrato, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' cmbHangszer
        ' 
        cmbHangszer.FormattingEnabled = True
        cmbHangszer.Location = New Point(24, 12)
        cmbHangszer.Name = "cmbHangszer"
        cmbHangszer.Size = New Size(230, 28)
        cmbHangszer.TabIndex = 0
        ' 
        ' trkHangero
        ' 
        trkHangero.AutoSize = False
        trkHangero.Location = New Point(48, 54)
        trkHangero.Maximum = 127
        trkHangero.Name = "trkHangero"
        trkHangero.Size = New Size(218, 50)
        trkHangero.TabIndex = 1
        trkHangero.TickFrequency = 16
        trkHangero.Value = 127
        ' 
        ' trkTer
        ' 
        trkTer.AutoSize = False
        trkTer.Location = New Point(48, 108)
        trkTer.Maximum = 63
        trkTer.Minimum = -64
        trkTer.Name = "trkTer"
        trkTer.Size = New Size(218, 50)
        trkTer.TabIndex = 2
        trkTer.TickFrequency = 16
        ' 
        ' trkVibrato
        ' 
        trkVibrato.AutoSize = False
        trkVibrato.Location = New Point(48, 162)
        trkVibrato.Maximum = 127
        trkVibrato.Name = "trkVibrato"
        trkVibrato.Size = New Size(218, 50)
        trkVibrato.TabIndex = 3
        trkVibrato.TickFrequency = 16
        ' 
        ' lblHer
        ' 
        lblHer.Location = New Point(12, 54)
        lblHer.Name = "lblHer"
        lblHer.Size = New Size(38, 25)
        lblHer.TabIndex = 4
        lblHer.Text = "Her"
        lblHer.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' lblTer
        ' 
        lblTer.Location = New Point(12, 108)
        lblTer.Name = "lblTer"
        lblTer.Size = New Size(38, 25)
        lblTer.TabIndex = 5
        lblTer.Text = "Tér"
        lblTer.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' lblVib
        ' 
        lblVib.Location = New Point(12, 162)
        lblVib.Name = "lblVib"
        lblVib.Size = New Size(38, 25)
        lblVib.TabIndex = 6
        lblVib.Text = "Vib"
        lblVib.TextAlign = ContentAlignment.MiddleCenter
        ' 
        ' chkAktiv
        ' 
        chkAktiv.Location = New Point(24, 222)
        chkAktiv.Name = "chkAktiv"
        chkAktiv.Size = New Size(70, 24)
        chkAktiv.TabIndex = 7
        chkAktiv.Text = "Aktív"
        chkAktiv.UseVisualStyleBackColor = True
        ' 
        ' lblHerErtek
        ' 
        lblHerErtek.Location = New Point(12, 79)
        lblHerErtek.Name = "lblHerErtek"
        lblHerErtek.Size = New Size(38, 25)
        lblHerErtek.TabIndex = 8
        lblHerErtek.Text = "127"
        lblHerErtek.TextAlign = ContentAlignment.TopCenter
        ' 
        ' lblTerErtek
        ' 
        lblTerErtek.Location = New Point(12, 133)
        lblTerErtek.Name = "lblTerErtek"
        lblTerErtek.Size = New Size(38, 25)
        lblTerErtek.TabIndex = 9
        lblTerErtek.Text = "0"
        lblTerErtek.TextAlign = ContentAlignment.TopCenter
        ' 
        ' lblVibErtek
        ' 
        lblVibErtek.Location = New Point(12, 187)
        lblVibErtek.Name = "lblVibErtek"
        lblVibErtek.Size = New Size(38, 25)
        lblVibErtek.TabIndex = 10
        lblVibErtek.Text = "0"
        lblVibErtek.TextAlign = ContentAlignment.TopCenter
        ' 
        ' chkAlul
        ' 
        chkAlul.Location = New Point(110, 222)
        chkAlul.Name = "chkAlul"
        chkAlul.Size = New Size(70, 24)
        chkAlul.TabIndex = 11
        chkAlul.Text = "Alul"
        chkAlul.UseVisualStyleBackColor = True
        ' 
        ' chkFelul
        ' 
        chkFelul.Location = New Point(196, 222)
        chkFelul.Name = "chkFelul"
        chkFelul.Size = New Size(70, 24)
        chkFelul.TabIndex = 12
        chkFelul.Text = "Felül"
        chkFelul.UseVisualStyleBackColor = True
        ' 
        ' HangszerPanel
        ' 
        AutoScaleDimensions = New SizeF(8.0F, 20.0F)
        AutoScaleMode = AutoScaleMode.Font
        Controls.Add(chkFelul)
        Controls.Add(chkAlul)
        Controls.Add(lblVibErtek)
        Controls.Add(lblTerErtek)
        Controls.Add(lblHerErtek)
        Controls.Add(chkAktiv)
        Controls.Add(lblVib)
        Controls.Add(lblTer)
        Controls.Add(lblHer)
        Controls.Add(trkVibrato)
        Controls.Add(trkTer)
        Controls.Add(trkHangero)
        Controls.Add(cmbHangszer)
        Name = "HangszerPanel"
        Size = New Size(280, 270)
        CType(trkHangero, ComponentModel.ISupportInitialize).EndInit()
        CType(trkTer, ComponentModel.ISupportInitialize).EndInit()
        CType(trkVibrato, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
    End Sub

    Friend WithEvents cmbHangszer As ComboBox
    Friend WithEvents trkHangero As TrackBar
    Friend WithEvents trkTer As TrackBar
    Friend WithEvents trkVibrato As TrackBar
    Friend WithEvents lblHer As Label
    Friend WithEvents lblTer As Label
    Friend WithEvents lblVib As Label
    Friend WithEvents chkAktiv As CheckBox
    Friend WithEvents lblHerErtek As Label
    Friend WithEvents lblTerErtek As Label
    Friend WithEvents lblVibErtek As Label
    Friend WithEvents chkAlul As CheckBox
    Friend WithEvents chkFelul As CheckBox

End Class

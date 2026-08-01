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
        MenuStrip1 = New MenuStrip()
        chkPedal = New CheckBox()
        grpVolume = New GroupBox()
        trkAlapHangero = New TrackBar()
        grpMidiIn = New GroupBox()
        grpMidiOut = New GroupBox()
        grpVolume.SuspendLayout()
        CType(trkAlapHangero, ComponentModel.ISupportInitialize).BeginInit()
        SuspendLayout()
        ' 
        ' MenuStrip1
        ' 
        MenuStrip1.ImageScalingSize = New Size(20, 20)
        MenuStrip1.Location = New Point(0, 0)
        MenuStrip1.Name = "MenuStrip1"
        MenuStrip1.Size = New Size(942, 24)
        MenuStrip1.TabIndex = 0
        MenuStrip1.Text = "MenuStrip1"
        ' 
        ' chkPedal
        ' 
        chkPedal.Location = New Point(324, 642)
        chkPedal.Name = "chkPedal"
        chkPedal.Size = New Size(242, 25)
        chkPedal.TabIndex = 1
        chkPedal.Text = "Fordított pedál működés"
        chkPedal.UseVisualStyleBackColor = True
        ' 
        ' grpVolume
        ' 
        grpVolume.Controls.Add(trkAlapHangero)
        grpVolume.Location = New Point(624, 138)
        grpVolume.Name = "grpVolume"
        grpVolume.Size = New Size(277, 92)
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
        grpMidiIn.Location = New Point(624, 246)
        grpMidiIn.Name = "grpMidiIn"
        grpMidiIn.Size = New Size(277, 85)
        grpMidiIn.TabIndex = 3
        grpMidiIn.TabStop = False
        grpMidiIn.Text = "MIDI In"
        ' 
        ' grpMidiOut
        ' 
        grpMidiOut.Location = New Point(624, 354)
        grpMidiOut.Name = "grpMidiOut"
        grpMidiOut.Size = New Size(277, 85)
        grpMidiOut.TabIndex = 4
        grpMidiOut.TabStop = False
        grpMidiOut.Text = "MIDI Out"
        ' 
        ' MainForm
        ' 
        AutoScaleDimensions = New SizeF(8F, 20F)
        AutoScaleMode = AutoScaleMode.Font
        ClientSize = New Size(942, 693)
        Controls.Add(grpMidiOut)
        Controls.Add(grpMidiIn)
        Controls.Add(grpVolume)
        Controls.Add(chkPedal)
        Controls.Add(MenuStrip1)
        MainMenuStrip = MenuStrip1
        Name = "MainForm"
        Text = "MainForm"
        grpVolume.ResumeLayout(False)
        CType(trkAlapHangero, ComponentModel.ISupportInitialize).EndInit()
        ResumeLayout(False)
        PerformLayout()
    End Sub

    Friend WithEvents MenuStrip1 As MenuStrip
    Friend WithEvents chkPedal As CheckBox
    Friend WithEvents grpVolume As GroupBox
    Friend WithEvents trkAlapHangero As TrackBar
    Friend WithEvents grpMidiIn As GroupBox
    Friend WithEvents grpMidiOut As GroupBox

End Class

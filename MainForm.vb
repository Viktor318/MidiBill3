Imports MidiBill3.Modellek

Public Class MainForm

    Private WithEvents midiMotor As New MidiEngine()

    Private hangszerKeretek(3) As GroupBox
    Private hangszerPanelek(3) As HangszerPanel

    Private utolsoPedalErtek As Integer = 0
    Private inicializalasKesz As Boolean = False

    ' A jelenleg "aktív" fájl neve (útvonal nélkül) - mindig szinkronban az
    ' ablak címsorával. Ez alapján ajánljuk fel alapértelmezettként a nevet
    ' a mentés-dialógusnál, függetlenül attól, hogy korábban betöltés,
    ' mentés vagy .mb3 import történt.
    Private aktualisFajlNev As String = ""

    ' Az egyes fájlműveletek (betöltés/mentés/import) LEGUTÓBB HASZNÁLT
    ' mappája, KÜLÖN-KÜLÖN nyilvántartva. Üres string = még nem volt
    ' sikeres művelet ebben a munkamenetben -> ilyenkor a program mappáját
    ' használjuk alapértelmezettként. Az első sikeres művelet után viszont
    ' mindegyik a saját, legutóbb használt mappáját jegyzi meg.
    Private utolsoBetoltesMappa As String = ""
    Private utolsoMentesMappa As String = ""
    Private utolsoImportMappa As String = ""

    ' A jelenlegi Split pont (MIDI hangszám, 0-127), szinkronban a
    ' cmbSplitPont kiválasztásával és a grpSplit fejlécével.
    Private aktualisSplitPont As Integer = 60

    ' Jelzi, hogy a "Tanulás" gomb után a következő lenyomott billentyű
    ' állítja be a Split pontot (ahelyett, hogy megszólalna).
    Private splitTanulasAktiv As Boolean = False

    ' A beállítások automatikus be-/kimentési útvonala - a VB6-os
    ' KezdetFile (Kezdet.mb3) megfelelője, JSON formátumban.
    Private ReadOnly kezdetFile As String = IO.Path.Combine(Application.StartupPath, "Kezdet.json")

    Private Const CONTROLLER_CHANGE As Integer = &HB0
    Private Const DAMPER_PEDAL As Integer = 64
    Private Const MAIN_VOLUME As Integer = 7
    Private Const PAN As Integer = 10
    Private Const MOD_WHEEL As Integer = 1
    Private Const NOTE_ON As Integer = &H90

    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        HangszerPanelekLetrehozasa()
        SplitPontFeltoltese()
        MidiEszkozokFeltoltese()
        KezdetBeallitasokBetoltese()
    End Sub

    ''' <summary>
    ''' Kilépéskor: hangok elnémítása minden csatornán, aktuális állapot
    ''' mentése a Kezdet.json-ba, majd a MIDI portok bezárása - ez az
    ''' EGYETLEN, központi hely a leállításhoz, függetlenül attól, hogy a
    ''' felhasználó gombbal, menüből vagy az "X"-szel zárta be az ablakot.
    ''' </summary>
    Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        For c As Integer = 1 To 4
            midiMotor.HangokKikapcsolasa(c, True)
        Next

        Try
            AktualisBeallitasokOsszegyujtese().Mentes(kezdetFile)
        Catch ex As Exception
            ' Kilépéskor csendben elnyeljük - a bezárás ne akadjon el emiatt
        End Try

        midiMotor.Dispose()
    End Sub

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
            AddHandler panel.AlulValtozott, AddressOf HangszerPanel_AlulValtozott
            AddHandler panel.FelulValtozott, AddressOf HangszerPanel_FelulValtozott
        Next

        hangszerPanelek(0).Aktiv = True
        hangszerKeretek(0).Text = "1. hangszer: Aktív"

        inicializalasKesz = True
    End Sub

    ''' <summary>A Split pont legördülő lista feltöltése mind a 128 MIDI hanggal, hangnévvel</summary>
    Private Sub SplitPontFeltoltese()
        cmbSplitPont.Items.Clear()
        For szam As Integer = 0 To 127
            cmbSplitPont.Items.Add(Hangnevek.HangNev(szam) & " (" & szam & ")")
        Next
        cmbSplitPont.SelectedIndex = 60 ' alapértelmezett törésponti hang: C4
    End Sub

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

    ''' <summary>Induláskor, ha van korábban elmentett Kezdet.json, betölti azt</summary>
    Private Sub KezdetBeallitasokBetoltese()
        If Not IO.File.Exists(kezdetFile) Then
            CimsorFrissitese(kezdetFile) ' Nincs elmentett fájl - de a nevét (Kezdet.json) így is mutatjuk
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
            OsszesCsatornaUjrakuldese()
        Catch ex As Exception
            MsgBox("Nem sikerült megnyitni a kiválasztott MIDI kimeneti eszközt." & vbCrLf &
                   "Lehet, hogy időközben eltávolították, vagy más program használja." & vbCrLf & vbCrLf &
                   "Hiba: " & ex.Message, MsgBoxStyle.Exclamation, "MidiBill")
        End Try
    End Sub

    ''' <summary>
    ''' Új kimeneti port nyitásakor minden csatorna aktuális állapotát
    ''' (hangszer, hangerő, pan, vibrato, aktív/inaktív, zóna) újraküldi -
    ''' a VB6-os Init_Channels megfelelője.
    ''' </summary>
    Private Sub OsszesCsatornaUjrakuldese()
        If Not inicializalasKesz Then Return

        midiMotor.SplitPont = aktualisSplitPont

        For i As Integer = 0 To 3
            Dim csatornaSzam As Integer = i + 1
            midiMotor.CsatornaAktiv(i) = hangszerPanelek(i).Aktiv
            midiMotor.CsatornaAlul(i) = hangszerPanelek(i).Alul
            midiMotor.CsatornaFelul(i) = hangszerPanelek(i).Felul
            midiMotor.ProgramValtas(csatornaSzam, hangszerPanelek(i).Hangszer)
            CsatornaHangeroKuldes(csatornaSzam, hangszerPanelek(i).Hangero)
            midiMotor.ControllerValtas(csatornaSzam, PAN, hangszerPanelek(i).Ter + 64)
            midiMotor.ControllerValtas(csatornaSzam, MOD_WHEEL, hangszerPanelek(i).Vibrato)
        Next
    End Sub

    ' ============================================================
    ' HangszerPanel események -> MIDI küldés
    ' ============================================================

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

    ''' <summary>"Alul" zóna be-/kikapcsolása egy csatornán - a MidiEngine kezeli a hang-nyilvántartást</summary>
    Private Sub HangszerPanel_AlulValtozott(sender As Object, aktiv As Boolean)
        Dim cs As Integer = CsatornaSzam(sender)
        If cs = 0 Then Return
        midiMotor.AlulValtas(cs, aktiv)
    End Sub

    ''' <summary>"Felül" zóna be-/kikapcsolása egy csatornán - a MidiEngine kezeli a hang-nyilvántartást</summary>
    Private Sub HangszerPanel_FelulValtozott(sender As Object, aktiv As Boolean)
        Dim cs As Integer = CsatornaSzam(sender)
        If cs = 0 Then Return
        midiMotor.FelulValtas(cs, aktiv)
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

        For i As Integer = 0 To 3
            CsatornaHangeroKuldes(i + 1, hangszerPanelek(i).Hangero)
        Next
    End Sub

    ' ============================================================
    ' Split pont
    ' ============================================================

    ''' <summary>
    ''' A Split pont manuális kiválasztása a legördülő listából - a "Tanulás"
    ''' gomb is ugyanezt az egy állapotot állítja be, így a két bemeneti mód
    ''' mindig szinkronban marad.
    ''' </summary>
    Private Sub cmbSplitPont_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbSplitPont.SelectedIndexChanged
        If Not inicializalasKesz Then Return
        If cmbSplitPont.SelectedIndex < 0 Then Return

        aktualisSplitPont = cmbSplitPont.SelectedIndex
        midiMotor.SplitPont = aktualisSplitPont
        grpSplit.Text = "Split pont: " & Hangnevek.HangNev(aktualisSplitPont)
    End Sub

    ''' <summary>"Tanulás" gomb: várakozás a következő billentyűlenyomásra</summary>
    Private Sub btnSplitTanulas_Click(sender As Object, e As EventArgs) Handles btnSplitTanulas.Click
        splitTanulasAktiv = True
        lblSplitInfo.Text = "Várakozás billentyűre..."
    End Sub

    ''' <summary>
    ''' A "Tanulás" mód lezárása: a lenyomott hang lesz az új Split pont.
    ''' A cmbSplitPont.SelectedIndex beállítása automatikusan kiváltja a
    ''' cmbSplitPont_SelectedIndexChanged-et, ami mindent (MidiEngine, felirat) frissít.
    ''' </summary>
    Private Sub SplitPontBeallitasaTanulassal(hang As Integer)
        splitTanulasAktiv = False
        cmbSplitPont.SelectedIndex = hang
        lblSplitInfo.Text = "Beállítva: " & Hangnevek.HangNev(hang)
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
    ''' Ha "Tanulás" módban vagyunk, a következő Note On beállítja a Split
    ''' pontot, és NEM szólal meg (nem kerül továbbküldésre).
    ''' </summary>
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
            Return ' a tanuláshoz használt billentyű nem szólal meg
        End If

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
            midiMotor.HangokKikapcsolasa(c, True)
        Next
    End Sub

    ' ============================================================
    ' Kilépés (gomb + menü) - a tényleges munkát a FormClosing végzi
    ' ============================================================

    Private Sub btnKilepes_Click(sender As Object, e As EventArgs) Handles btnKilepes.Click
        Close()
    End Sub

    Private Sub mnuKilepes_Click(sender As Object, e As EventArgs) Handles mnuKilepes.Click
        Close()
    End Sub

    ' ============================================================
    ' Fájl mentés / betöltés (JSON) + régi .mb3 importálás
    ' ============================================================

    ''' <summary>Az aktuális UI-állapot összegyűjtése egy Beallitasok objektumba (mentéshez)</summary>
    Private Function AktualisBeallitasokOsszegyujtese() As Beallitasok
        Dim b As New Beallitasok()

        For i As Integer = 0 To 3
            Dim cs As New CsatornaBeallitas()
            cs.Hangszer = hangszerPanelek(i).Hangszer
            cs.Hangero = hangszerPanelek(i).Hangero
            cs.Ter = hangszerPanelek(i).Ter
            cs.Vibrato = hangszerPanelek(i).Vibrato
            cs.Aktiv = hangszerPanelek(i).Aktiv
            cs.Alul = hangszerPanelek(i).Alul
            cs.Felul = hangszerPanelek(i).Felul
            b.Csatornak.Add(cs)
        Next

        b.AlapHangero = trkAlapHangero.Value
        b.MidiBemenetNev = If(cmbMidiIn.SelectedItem IsNot Nothing, cmbMidiIn.SelectedItem.ToString(), "")
        b.MidiKimenetNev = If(cmbMidiOut.SelectedItem IsNot Nothing, cmbMidiOut.SelectedItem.ToString(), "")
        b.PedalInvertalva = chkPedal.Checked
        b.SplitPont = aktualisSplitPont

        Return b
    End Function

    ''' <summary>
    ''' Egy betöltött/importált Beallitasok alkalmazása a UI-ra és a MIDI-re.
    ''' frissitsdEszkozoketIs: JSON-betöltésnél True (van eszköznév-adat),
    ''' .mb3 importnál False (a régi fájl eszközindexei nem megbízhatóak).
    ''' </summary>
    Private Sub BeallitasokAlkalmazasa(b As Beallitasok, frissitsdEszkozoketIs As Boolean)
        For i As Integer = 0 To 3
            Dim cs As CsatornaBeallitas = b.Csatornak(i)

            ' Korlátozás: ha a fájl sérült/kézzel szerkesztett és érvénytelen
            ' tartományú értéket tartalmazna, ne omoljon össze rajta a program.
            hangszerPanelek(i).Hangszer = Korlatoz(cs.Hangszer, 0, 127)
            hangszerPanelek(i).Hangero = Korlatoz(cs.Hangero, 0, 127)
            hangszerPanelek(i).Ter = Korlatoz(cs.Ter, -64, 63)
            hangszerPanelek(i).Vibrato = Korlatoz(cs.Vibrato, 0, 127)
            hangszerPanelek(i).Aktiv = cs.Aktiv
            hangszerPanelek(i).Alul = cs.Alul
            hangszerPanelek(i).Felul = cs.Felul

            Dim alapNev As String = (i + 1).ToString() & ". hangszer"
            hangszerKeretek(i).Text = If(cs.Aktiv, alapNev & ": Aktív", alapNev)
        Next

        trkAlapHangero.Value = Korlatoz(b.AlapHangero, 0, 100)
        chkPedal.Checked = b.PedalInvertalva
        cmbSplitPont.SelectedIndex = Korlatoz(b.SplitPont, 0, 127)

        If frissitsdEszkozoketIs Then
            Dim beIdx As Integer = cmbMidiIn.Items.IndexOf(b.MidiBemenetNev)
            If beIdx >= 0 Then cmbMidiIn.SelectedIndex = beIdx

            Dim kiIdx As Integer = cmbMidiOut.Items.IndexOf(b.MidiKimenetNev)
            If kiIdx >= 0 Then cmbMidiOut.SelectedIndex = kiIdx
        End If

        OsszesCsatornaUjrakuldese()
    End Sub

    Private Function Korlatoz(ertek As Integer, min As Integer, max As Integer) As Integer
        Return Math.Max(min, Math.Min(max, ertek))
    End Function

    ''' <summary>
    ''' Az ablak címsorának frissítése a mentett/betöltött/importált fájl
    ''' nevével (csak a fájlnév, nem a teljes útvonal), és az aktuális
    ''' fájlnév eltárolása is - ezt ajánljuk fel majd alapértelmezettként
    ''' a legközelebbi "Adatok mentése" dialógusnál.
    ''' </summary>
    Private Sub CimsorFrissitese(teljesUtvonal As String)
        aktualisFajlNev = IO.Path.GetFileName(teljesUtvonal)
        Me.Text = "MIDI billentyűzet megszólaltatása 4 hangszeren egyszerre : " & aktualisFajlNev
    End Sub

    ''' <summary>
    ''' A dialógus kezdő mappáját állítja be: ha a megjegyzettMappa még üres
    ''' (nem volt még sikeres művelet ennél a típusnál ebben a munkamenetben),
    ''' a program mappáját használja; egyébként a legutóbb használt, saját
    ''' mappáját az adott művelettípusnak (betöltés/mentés/import külön-külön).
    ''' </summary>
    Private Sub DialogusMappaBeallitasa(dlg As FileDialog, ByRef megjegyzettMappa As String)
        dlg.InitialDirectory = If(String.IsNullOrEmpty(megjegyzettMappa), Application.StartupPath, megjegyzettMappa)
    End Sub

    ''' <summary>
    ''' Sikeres fájlművelet után: a dialógus FileName mezőjét rövid névre
    ''' állítja (legközelebbi megnyitáskor ne a teljes útvonalat mutassa),
    ''' ÉS elmenti a használt mappát a megfelelő "megjegyzett mappa" mezőbe,
    ''' hogy legközelebb onnan folytassa ugyanaz a művelettípus.
    ''' </summary>
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
                BeallitasokAlkalmazasa(b, False) ' a MIDI eszközválasztást szándékosan nem érintjük
                CimsorFrissitese(dlgImportMb3.FileName)
                DialogusAllapotMentese(dlgImportMb3, dlgImportMb3.FileName, utolsoImportMappa)

                MsgBox("A régi preset sikeresen importálva." & vbCrLf &
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
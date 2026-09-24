Imports MidiBill3.Modellek
Imports System.Diagnostics

''' <summary>
''' A MIDI felvevő ablak (3. fázis): Start/Stop felfegyverzéssel, színes
''' állapotjelző futó idővel, F9 gyorsbillentyű, a mentés beállításai
''' (fájlnév, dátum szerinti almappa, jelentés a felvétel mellé), és az
''' utoljára mentett fájl mappájának megnyitása.
'''
''' SZÁNDÉKOSAN NEM ez az ablak birtokolja a felvevő motort (MidiFelvevo),
''' hanem a főablak - lásd MainForm.midiFelvevo. Ennek oka a bezárás: a
''' WinForms a főablak bezárásakor a hozzá tartozó ablakokat is bezárja, és
''' a sorrendre nem jó építeni. Így a futó felvétel sorsáról mindig lehet
''' dönteni, bármelyik ablak záródik is be előbb (lásd BezarasElottiKezeles).
''' Ugyanezért a kimeneti csapra (MidiEngine.UzenetKikuldve) is a főablak
''' iratkozik fel, és ő adja tovább az üzeneteket a felvevőnek.
'''
''' Nem modális ablak, a főablak a tulajdonosa (Show(Me)): felvétel közben a
''' főablakban szabadon lehet hangszínt váltani, és a felvevő ablak a
''' főablak előtt marad, de más programok elé nem ugrik.
''' </summary>
Public Class MidiFelvevoForm

    Private ReadOnly felvevo As MidiFelvevo
    Private ReadOnly motor As MidiEngine
    Private ReadOnly pillanatkepLekerdezo As Func(Of List(Of MidiUzenet))
    Private ReadOnly jelentesKeszito As Func(Of String, String)

    ''' <summary>
    ''' A futó idő kijelzése - csak felvétel közben ír a feliratba. 250 ms
    ''' elég ahhoz, hogy a másodperc-váltás egyenletesnek tűnjön.
    ''' </summary>
    Private WithEvents kijelzoIdozito As New Timer With {.Interval = 250}

    ''' <summary>A hosszú (levágott) "Utoljára mentve" sor teljes szövege egérrel rámutatva.</summary>
    Private ReadOnly tipp As New ToolTip()

    ''' <summary>Az utoljára mentett .mid teljes útvonala, vagy üres, ha ebben az ablakban még nem volt mentés.</summary>
    Private utoljaraMentettFajl As String = ""
    ''' <summary>A fájlnév mezőbe legutóbb AUTOMATIKUSAN beírt név (a preset neve) - ebből látszik, hogy a felhasználó átírta-e.</summary>
    Private javasoltFajlNev As String = ""

    ''' <summary>
    ''' Kiváltódik, ha a felvevő állapota a FELÜLETRŐL indított művelet miatt
    ''' változott (Start, Stop, eldobás) - a főablak ebből frissíti a saját
    ''' "MIDI felvétel" gombját. Az első leütés okozta váltást (felfegyverzett
    ''' -> rögzít) a főablak magától észleli, mert az üzenetek rajta mennek át.
    ''' </summary>
    Public Event AllapotValtozott()

    ''' <param name="felvevo">A főablak felvevő motorja.</param>
    ''' <param name="motor">A MIDI motor - a futó idő ugyanarról az óráról számolódik, mint a felvétel időbélyegei.</param>
    ''' <param name="pillanatkepLekerdezo">A csatorna-állapot pillanatképe, amit a felvevő PONTOSAN az első leütéskor kér le.</param>
    ''' <param name="jelentesKeszito">A felvétel melletti .txt jelentés elkészítése; üres sztring = siker, egyébként a hiba szövege.</param>
    ''' <param name="alapFajlNev">A "Mentett fájl neve" mező kezdőértéke (a betöltött preset neve).</param>
    Public Sub New(felvevo As MidiFelvevo, motor As MidiEngine,
                   pillanatkepLekerdezo As Func(Of List(Of MidiUzenet)),
                   jelentesKeszito As Func(Of String, String),
                   alapFajlNev As String)
        InitializeComponent()

        Me.felvevo = felvevo
        Me.motor = motor
        Me.pillanatkepLekerdezo = pillanatkepLekerdezo
        Me.jelentesKeszito = jelentesKeszito

        FelvevoBeallitasokBetoltese()
        txtFajlNev.Text = alapFajlNev
        javasoltFajlNev = alapFajlNev
        UtvonalMintaFrissitese()
        AllapotKijelzesFrissitese()
    End Sub

    ''' <summary>
    ''' Az ablak a legutóbbi helyén nyílik meg (MidiBill3.ini). Ha nincs
    ''' mentett hely, vagy az már nem látszik egyik képernyőn sem, a
    ''' főablak MELLÉ (jobbra) kerül, ha ott elfér a képernyőn, különben a
    ''' főablak közepére - így nem takarja el a csatornapaneleket.
    ''' </summary>
    Private Sub MidiFelvevoForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If Not ExtraBeallitasok.AblakHelyzetVisszaallitasa(Me, ExtraBeallitasok.SZAKASZ_FELVEVO_ABLAK) AndAlso
           Owner IsNot Nothing Then
            Dim terulet As Rectangle = Screen.FromControl(Owner).WorkingArea
            Dim x As Integer = Owner.Right + 8
            If x + Width > terulet.Right Then
                x = Math.Max(terulet.Left, Owner.Left + (Owner.Width - Width) \ 2)
            End If
            Dim y As Integer = Math.Max(terulet.Top, Math.Min(Owner.Top, terulet.Bottom - Height))
            Location = New Point(x, y)
        End If

        kijelzoIdozito.Start()
    End Sub

    ' ============================================================
    ' Start / Stop
    ' ============================================================

    ''' <summary>
    ''' Az F9 és a gombok közös belépési pontja: nyugalmi állapotban
    ''' felfegyverez, egyébként leállít. A főablak F9-e is ezt hívja.
    ''' </summary>
    Public Sub FelvetelValtasa()
        If felvevo.Allapot = FelvevoAllapot.Alap Then
            Inditas()
        Else
            Leallitas()
        End If
    End Sub

    Private Sub btnStart_Click(sender As Object, e As EventArgs) Handles btnStart.Click
        Inditas()
    End Sub

    Private Sub btnStop_Click(sender As Object, e As EventArgs) Handles btnStop.Click
        Leallitas()
    End Sub

    ''' <summary>
    ''' Felfegyverzés: a felvétel az első LEÜTÉSSEL indul el (lásd
    ''' FelvevoAllapot.Felfegyverezve).
    ''' </summary>
    Private Sub Inditas()
        If felvevo.Allapot <> FelvevoAllapot.Alap Then Return
        felvevo.Felfegyverzes(pillanatkepLekerdezo)
        AllapotValtozasJelzese()
    End Sub

    ''' <summary>
    ''' Leállítás. Felfegyverzett, de még el nem indult állapotban nincs mit
    ''' menteni: csendben visszaáll, üzenetablak nélkül (a felhasználóval
    ''' egyeztetett viselkedés). Felvétel közben a kézi Stop MINDIG ment.
    ''' </summary>
    Private Sub Leallitas()
        Select Case felvevo.Allapot
            Case FelvevoAllapot.Felfegyverezve
                felvevo.Eldobas()
            Case FelvevoAllapot.Rogzit
                ' Sikertelen mentésnél a felvett anyag megmarad, és a Stop
                ' újra megpróbálható - ezért ilyenkor nincs állapotváltás.
                If Not Mentes() Then Return
            Case Else
                Return
        End Select
        AllapotValtozasJelzese()
    End Sub

    ''' <summary>
    ''' A felvétel lezárása és mentése, majd - ha be van kapcsolva - a
    ''' jelentés elkészítése mellé. A jelentés hibája a felvételt NEM
    ''' veszélyezteti: a .mid ilyenkor is megvan, csak jelezzük.
    ''' </summary>
    ''' <returns>True, ha a mentés sikerült; False, ha nem (a felvett anyag ilyenkor megmarad).</returns>
    Private Function Mentes() As Boolean
        Dim utvonal As String
        Try
            utvonal = Utvonalak.UjFelvetelUtvonala(txtFajlNev.Text, chkDatumAlmappa.Checked)
            If Not felvevo.LezarasEsMentes(utvonal) Then Return True   ' nem volt mit menteni
        Catch ex As Exception
            MsgBox("Nem sikerült elmenteni a felvételt." & vbCrLf & vbCrLf &
                   "Hiba: " & ex.Message & vbCrLf & vbCrLf &
                   "A felvett anyag megmaradt - a Stop gombbal újra megpróbálhatod.",
                   MsgBoxStyle.Exclamation, "MidiBill - felvétel")
            Return False
        End Try

        utoljaraMentettFajl = utvonal

        Dim sor As String = "Utoljára mentve: " & IO.Path.GetFileName(utvonal)
        If chkJelentes.Checked Then
            Dim jelentesHiba As String = jelentesKeszito(utvonal)
            If jelentesHiba.Length > 0 Then sor &= " (a jelentés nem készült el: " & jelentesHiba & ")"
        End If
        lblUtoljaraMentve.Text = sor
        tipp.SetToolTip(lblUtoljaraMentve, sor & vbCrLf & utvonal)
        Return True
    End Function

    ''' <summary>
    ''' A felvétel sorsáról dönt az ablak - vagy a főablak - bezárása előtt.
    ''' Felvétel közben rákérdez: Igen = mentés, Nem = eldobás, Mégse = a
    ''' bezárás megszakítása. Felfegyverzett (még el nem indult) állapotban
    ''' csendben leszerel.
    '''
    ''' IDEMPOTENS: ha már döntöttünk (a felvevő nyugalmi állapotban van),
    ''' újra nem kérdez. Ezért hívhatja a felvevő ablak ÉS a főablak is a
    ''' saját bezárásakor - bármelyik jön előbb, a felhasználó pontosan egyszer
    ''' kap kérdést.
    ''' </summary>
    ''' <returns>True, ha a bezárás folytatódhat.</returns>
    Public Function BezarasElottiKezeles() As Boolean
        Select Case felvevo.Allapot
            Case FelvevoAllapot.Felfegyverezve
                felvevo.Eldobas()

            Case FelvevoAllapot.Rogzit
                Dim valasz As MsgBoxResult = MsgBox(
                    "Felvétel van folyamatban." & vbCrLf & vbCrLf &
                    "Igen: a felvétel mentése, és bezárás" & vbCrLf &
                    "Nem: a felvétel eldobása, és bezárás" & vbCrLf &
                    "Mégse: vissza a felvételhez",
                    MsgBoxStyle.YesNoCancel Or MsgBoxStyle.Question, "MidiBill - felvétel")

                Select Case valasz
                    Case MsgBoxResult.Yes
                        If Not Mentes() Then Return False
                    Case MsgBoxResult.No
                        felvevo.Eldobas()
                    Case Else
                        Return False
                End Select

            Case Else
                Return True
        End Select

        AllapotValtozasJelzese()
        Return True
    End Function

    Private Sub AllapotValtozasJelzese()
        AllapotKijelzesFrissitese()
        RaiseEvent AllapotValtozott()
    End Sub

    ' ============================================================
    ' Kijelzés
    ' ============================================================

    ''' <summary>
    ''' Az állapotjelző, a Stop gomb színe és a gombok engedélyezése a
    ''' felvevő állapota szerint: nyugalmi -> sárga "Várakozás billentyűre..."
    ''' -> piros "● FELVÉTEL m:ss". A főablak is hívja, amikor az első leütés
    ''' elindítja a felvételt.
    ''' </summary>
    Public Sub AllapotKijelzesFrissitese()
        Select Case felvevo.Allapot
            Case FelvevoAllapot.Felfegyverezve
                AllapotSzinezese("Várakozás billentyűre...", Color.FromArgb(253, 240, 213), Color.FromArgb(122, 74, 0), Color.Gold)
            Case FelvevoAllapot.Rogzit
                AllapotSzinezese(FelvetelFelirata(), Color.FromArgb(253, 236, 235), Color.FromArgb(161, 40, 31), Color.IndianRed)
            Case Else
                lblAllapot.Text = "Nincs felvétel"
                lblAllapot.BackColor = SystemColors.Control
                lblAllapot.ForeColor = SystemColors.GrayText
                btnStop.UseVisualStyleBackColor = True
        End Select

        btnStart.Enabled = (felvevo.Allapot = FelvevoAllapot.Alap)
        btnStop.Enabled = (felvevo.Allapot <> FelvevoAllapot.Alap)
    End Sub

    Private Sub AllapotSzinezese(szoveg As String, hatter As Color, betu As Color, stopGombSzine As Color)
        lblAllapot.Text = szoveg
        lblAllapot.BackColor = hatter
        lblAllapot.ForeColor = betu
        btnStop.UseVisualStyleBackColor = False
        btnStop.BackColor = stopGombSzine
    End Sub

    ''' <summary>
    ''' "● FELVÉTEL 0:42" - a futó idő az első leütés óta, a MidiEngine
    ''' órája szerint (ugyanarról az óráról, mint a felvett időbélyegek).
    ''' </summary>
    Private Function FelvetelFelirata() As String
        Dim eltelt As Long = Math.Max(0L, motor.ElteltIdoMs - felvevo.KezdoIdobelyegMs)
        Dim ido As TimeSpan = TimeSpan.FromMilliseconds(eltelt)
        Dim idoSzoveg As String = If(ido.TotalHours >= 1,
                                     ido.ToString("h\:mm\:ss", Globalization.CultureInfo.InvariantCulture),
                                     ido.ToString("m\:ss", Globalization.CultureInfo.InvariantCulture))
        Return "● FELVÉTEL  " & idoSzoveg
    End Function

    Private Sub kijelzoIdozito_Tick(sender As Object, e As EventArgs) Handles kijelzoIdozito.Tick
        If felvevo.Allapot = FelvevoAllapot.Rogzit Then lblAllapot.Text = FelvetelFelirata()
    End Sub

    ' ============================================================
    ' Mentési beállítások
    ' ============================================================

    ''' <summary>
    ''' A mentett fájl(ok) helyének előnézete - ugyanazzal a névképzéssel,
    ''' mint a valódi mentés (Utvonalak.FelvetelUtvonalMintaja), a mostani
    ''' időponttal példaként.
    ''' </summary>
    Private Sub UtvonalMintaFrissitese()
        Dim minta As String = Utvonalak.FelvetelUtvonalMintaja(txtFajlNev.Text, chkDatumAlmappa.Checked, DateTime.Now)
        If chkJelentes.Checked Then minta &= vbCrLf & IO.Path.ChangeExtension(minta, ".txt")
        lblUtvonalMinta.Text = minta
    End Sub

    ''' <summary>
    ''' A főablak hívja, ha másik preset töltődött be (vagy a preset más
    ''' néven lett elmentve): a fájlnév mező átveszi az új preset nevét -
    ''' de CSAK akkor, ha a felhasználó nem írt bele saját nevet (azaz a mező
    ''' még az előző javaslatot tartalmazza). A kézzel beírt név megmarad.
    ''' </summary>
    Public Sub AlapFajlNevFrissitese(ujNev As String)
        If String.Equals(txtFajlNev.Text, javasoltFajlNev, StringComparison.Ordinal) Then
            txtFajlNev.Text = ujNev
        End If
        javasoltFajlNev = ujNev
    End Sub

    Private Sub txtFajlNev_TextChanged(sender As Object, e As EventArgs) Handles txtFajlNev.TextChanged
        UtvonalMintaFrissitese()
    End Sub

    Private Sub chkDatumAlmappa_CheckedChanged(sender As Object, e As EventArgs) Handles chkDatumAlmappa.CheckedChanged
        UtvonalMintaFrissitese()
    End Sub

    Private Sub chkJelentes_CheckedChanged(sender As Object, e As EventArgs) Handles chkJelentes.CheckedChanged
        UtvonalMintaFrissitese()
    End Sub

    ''' <summary>
    ''' A felvételek mappájának megnyitása a Windows Intézőben, az
    ''' utoljára mentett fájlt kijelölve (ha van). Az %AppData% mappa az
    ''' Intézőben alapból rejtett - enélkül körülményes lenne megtalálni.
    ''' </summary>
    Private Sub btnMappaMegnyitasa_Click(sender As Object, e As EventArgs) Handles btnMappaMegnyitasa.Click
        Try
            If utoljaraMentettFajl.Length > 0 AndAlso IO.File.Exists(utoljaraMentettFajl) Then
                Process.Start("explorer.exe", "/select,""" & utoljaraMentettFajl & """")
            Else
                IO.Directory.CreateDirectory(Utvonalak.FelvetelekMappa)
                Process.Start("explorer.exe", """" & Utvonalak.FelvetelekMappa & """")
            End If
        Catch ex As Exception
            MsgBox("Nem sikerült megnyitni a mappát." & vbCrLf & vbCrLf & "Hiba: " & ex.Message,
                   MsgBoxStyle.Exclamation, "MidiBill - felvétel")
        End Try
    End Sub

    ' ============================================================
    ' A felvevő beállításai és az ablak helye (MidiBill3.ini)
    ' ============================================================

    Private Const KULCS_DATUM_ALMAPPA As String = "DatumAlmappa"
    Private Const KULCS_JELENTES As String = "Jelentes"
    Private Const KULCS_JATEKIDO_KUSZOB As String = "JatekidoKuszob"
    Private Const KULCS_SZUNET_KUSZOB As String = "SzunetKuszob"

    ''' <summary>
    ''' A felvevő beállításai a MidiBill3.ini-ből; ami hiányzik, annál a
    ''' Designerben megadott alapérték marad. Az "Automatikus felvétel"
    ''' jelölőnégyzet SZÁNDÉKOSAN nincs itt (a felhasználó döntése: minden
    ''' programindítás után bekapcsolt állapotból indul), és a fájlnév sem
    ''' (az a betöltött preset nevét követi).
    ''' </summary>
    Private Sub FelvevoBeallitasokBetoltese()
        Dim ini As IniFajl = ExtraBeallitasok.Ini
        Dim szakasz As String = ExtraBeallitasok.SZAKASZ_FELVEVO
        chkDatumAlmappa.Checked = ini.Logikai(szakasz, KULCS_DATUM_ALMAPPA, chkDatumAlmappa.Checked)
        chkJelentes.Checked = ini.Logikai(szakasz, KULCS_JELENTES, chkJelentes.Checked)
        nudJatekidoKuszob.Value = KorlatokKozott(nudJatekidoKuszob,
            ini.Egesz(szakasz, KULCS_JATEKIDO_KUSZOB, CInt(nudJatekidoKuszob.Value)))
        nudSzunetKuszob.Value = KorlatokKozott(nudSzunetKuszob,
            ini.Egesz(szakasz, KULCS_SZUNET_KUSZOB, CInt(nudSzunetKuszob.Value)))
    End Sub

    ''' <summary>Egy (akár kézzel elrontott) ini-érték a mező megengedett tartományába szorítva.</summary>
    Private Shared Function KorlatokKozott(mezo As NumericUpDown, ertek As Integer) As Decimal
        Return Math.Max(mezo.Minimum, Math.Min(mezo.Maximum, CDec(ertek)))
    End Function

    ''' <summary>
    ''' Az ablak helyének és a felvevő beállításainak bejegyzése a
    ''' MidiBill3.ini memóriabeli példányába (a fájlt az
    ''' ExtraBeallitasok.Mentes() írja ki). A saját bezáráskor és a
    ''' főablak bezárásakor is lefut - a kettő sorrendje nem garantált.
    ''' </summary>
    Public Sub BeallitasokRogzitese()
        ExtraBeallitasok.AblakHelyzetRogzitese(Me, ExtraBeallitasok.SZAKASZ_FELVEVO_ABLAK)
        Dim ini As IniFajl = ExtraBeallitasok.Ini
        Dim szakasz As String = ExtraBeallitasok.SZAKASZ_FELVEVO
        ini.Beallitas(szakasz, KULCS_DATUM_ALMAPPA, chkDatumAlmappa.Checked)
        ini.Beallitas(szakasz, KULCS_JELENTES, chkJelentes.Checked)
        ini.Beallitas(szakasz, KULCS_JATEKIDO_KUSZOB, CInt(nudJatekidoKuszob.Value))
        ini.Beallitas(szakasz, KULCS_SZUNET_KUSZOB, CInt(nudSzunetKuszob.Value))
    End Sub

    ' ============================================================
    ' Billentyűzet és bezárás
    ' ============================================================

    Private Sub MidiFelvevoForm_KeyDown(sender As Object, e As KeyEventArgs) Handles MyBase.KeyDown
        If e.KeyCode <> Keys.F9 Then Return
        e.Handled = True
        e.SuppressKeyPress = True
        FelvetelValtasa()
    End Sub

    ''' <summary>
    ''' Bezárás előtt dönteni kell a futó felvételről (lásd
    ''' BezarasElottiKezeles). Ha a felhasználó a Mégse-t választja, a
    ''' bezárás megszakad - és ha ezt a főablak bezárása váltotta ki
    ''' (CloseReason.FormOwnerClosing), a WinForms a főablak bezárását is
    ''' megszakítja.
    ''' </summary>
    ''' <summary>
    ''' Nyissa-e meg a következő indítás automatikusan az ablakot? Csak
    ''' akkor NEM, ha a felhasználó maga zárta be (X gomb, Alt+F4) - ha a
    ''' főablakkal együtt zárult be (vagy a Windows leállítása zárta be),
    ''' akkor igen.
    ''' </summary>
    Private Shared Function NyitvaMaradE(ok As CloseReason) As Boolean
        Return ok <> CloseReason.UserClosing AndAlso ok <> CloseReason.None
    End Function

    Private Sub MidiFelvevoForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If Not BezarasElottiKezeles() Then e.Cancel = True
    End Sub

    Private Sub MidiFelvevoForm_FormClosed(sender As Object, e As FormClosedEventArgs) Handles MyBase.FormClosed
        BeallitasokRogzitese()
        ExtraBeallitasok.Ini.Beallitas(ExtraBeallitasok.SZAKASZ_FELVEVO_ABLAK, ExtraBeallitasok.KULCS_NYITVA,
                                       NyitvaMaradE(e.CloseReason))
        ExtraBeallitasok.Mentes()
        kijelzoIdozito.Stop()
        kijelzoIdozito.Dispose()
        tipp.Dispose()
    End Sub

End Class

Imports MidiBill3.Modellek
Imports System.Diagnostics

''' <summary>
''' A MIDI felvevő ablak: Start/Stop felfegyverzéssel, színes állapotjelző
''' futó idővel, F9 gyorsbillentyű, a mentés beállításai (fájlnév, dátum
''' szerinti almappa, jelentés a felvétel mellé), az utoljára mentett fájl
''' mappájának megnyitása (3. fázis), és az AUTOMATIKUS FELVÉTEL mód (4.
''' fázis): a csendek mentén szegmensekre bontja a játékot, a rövid
''' próbálkozásokat eldobja, a hosszabbakat külön fájlba menti, és utána
''' azonnal újra felfegyverez - lásd SzegmensEllenorzese. Az 5. fázisban
''' bekerült a LEJÁTSZÁS is (lásd a "Lejátszás" szakaszt): egy .mid fájl
''' visszajátszása háttérszálon, F8 gyorsbillentyűvel; a 6. fázisban a
''' tekerés, a 7.-ben a RÁFELVÉTEL (lásd a "Ráfelvétel" szakaszt).
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
    ''' Az "Automatikus felvétel" jelölőnégyzet aktuális állapota, Shared
    ''' másolatban - a főablak gombja ebből olvassa (lásd AutomatikusMod).
    ''' Maga az állapot a MidiBill3.ini-ben él (lásd
    ''' FelvevoBeallitasokBetoltese / BeallitasokRogzitese): a program az
    ''' utoljára elmentett értékkel indul, és csak ha még nincs mentett
    ''' érték, akkor BEKAPCSOLT (a felhasználó döntése, 2026-09-25 - a
    ''' korábbi "mindig bekapcsoltan indul" helyett).
    ''' </summary>
    Private Shared automatikusModAllapot As Boolean = True

    ''' <summary>
    ''' Be van-e kapcsolva az Automatikus felvétel mód - a főablak gombjának
    ''' feliratához ("● AUTO FELVÉTEL").
    ''' </summary>
    Public Shared ReadOnly Property AutomatikusMod As Boolean
        Get
            Return automatikusModAllapot
        End Get
    End Property

    ''' <summary>Igaz, amíg a konstruktor tölti a vezérlőket - ilyenkor a CheckedChanged nem fegyverezhet fel.</summary>
    Private betoltesAlatt As Boolean = True

    ''' <summary>
    ''' Véd az újrabelépés ellen: egy mentési hiba üzenetablaka alatt az
    ''' időzítő és a beérkező MIDI üzenetek tovább futnak, és nem indíthatnak
    ''' újabb szegmens-lezárást.
    ''' </summary>
    Private szegmensLezarasFolyamatban As Boolean = False

    ''' <summary>A legutóbbi (felhasználói) indítás óta mentett szegmensek száma.</summary>
    Private mentettSzegmensek As Integer = 0

    ''' <summary>
    ''' Az óra, amihez a futó időt és a szünetet mérjük: alapból a MidiEngine
    ''' órája (ugyanarról az óráról, mint a felvett időbélyegek). A Friend
    ''' hozzáférés a tesztek szimulált órájáért van. SZÁNDÉKOSAN mező, nem
    ''' property: egy ablak publikus/Friend property-jét a WinForms tervező
    ''' szerializálni próbálná (WFO1000 build-hiba), egy mezőt nem.
    ''' </summary>
    Friend Ora As Func(Of Long)

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
    ''' <param name="panelekVisszakuldese">A panelek beállításainak visszaküldése a szintetizátorra (lejátszás után, ha a beállítások megtartása ki van kapcsolva).</param>
    ''' <param name="fajlBeallitasainakAtvetele">A felület átállítása egy .mid fájl szerint (a "Preset készítése MIDI fájlból" logikájával); üres sztring = siker, egyébként a hiba szövege.</param>
    Public Sub New(felvevo As MidiFelvevo, motor As MidiEngine,
                   pillanatkepLekerdezo As Func(Of List(Of MidiUzenet)),
                   jelentesKeszito As Func(Of String, String),
                   alapFajlNev As String,
                   panelekVisszakuldese As Action,
                   fajlBeallitasainakAtvetele As Func(Of String, String))
        InitializeComponent()

        Me.felvevo = felvevo
        Me.motor = motor
        Me.pillanatkepLekerdezo = pillanatkepLekerdezo
        Me.jelentesKeszito = jelentesKeszito
        Me.panelekVisszakuldese = panelekVisszakuldese
        Me.fajlBeallitasainakAtvetele = fajlBeallitasainakAtvetele
        Ora = Function() motor.ElteltIdoMs

        lejatszo = New MidiLejatszo(AddressOf motor.LejatszottUzenetKuldese, Function() motor.ElteltIdoMs)
        AddHandler lejatszo.Befejezodott, AddressOf Lejatszo_Befejezodott
        tipp.SetToolTip(trkPozicio, "Tekerés: húzd vagy kattints; billentyűvel ← / → = 5 mp vissza / előre, Home = a fájl eleje")
        tipp.SetToolTip(chkLejatszottRogzitese, "Bekapcsolva a lejátszás is a felvételbe kerül (ráfelvétel); kikapcsolva csak az élő játék")
        tipp.SetToolTip(chkLejatszasFelvetellel, "A Start (F9) a lejátszást is elindítja a csúszka pozíciójáról")

        FelvevoBeallitasokBetoltese()
        felvevo.LejatszottHangokRogzitese = chkLejatszottRogzitese.Checked
        automatikusModAllapot = chkAutomatikus.Checked
        txtFajlNev.Text = alapFajlNev
        javasoltFajlNev = alapFajlNev
        UtvonalMintaFrissitese()
        AllapotKijelzesFrissitese()
        LejatszasKijelzesFrissitese()
        betoltesAlatt = False
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

        ' Automatikus módban már a megnyitás felfegyverez (a felhasználó
        ' döntése, 2026-09-24) - a Startot nem kell külön megnyomni.
        If chkAutomatikus.Checked Then Inditas()
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
            FelhasznaloiInditas()
        Else
            Leallitas()
        End If
    End Sub

    Private Sub btnStart_Click(sender As Object, e As EventArgs) Handles btnStart.Click
        FelhasznaloiInditas()
    End Sub

    ''' <summary>
    ''' A Start gomb és az F9: felfegyverzés, és ha a "Felvétel indításakor a
    ''' lejátszás is induljon" be van kapcsolva, a lejátszás is elindul a
    ''' csúszka pozíciójáról (a felhasználó döntése, 2026-11-03). Automatikus
    ''' módban a felvevő jellemzően már felfegyverzett - a Start ilyenkor
    ''' csak a lejátszást indítja el. Az ablak megnyitása és az automatikus
    ''' újra-felfegyverzés NEM indít lejátszást.
    ''' </summary>
    Private Sub FelhasznaloiInditas()
        Inditas()
        If chkLejatszasFelvetellel.Checked AndAlso felvevo.Allapot = FelvevoAllapot.Felfegyverezve AndAlso
           lejatszo.VanBetoltottEsemeny AndAlso Not lejatszo.Fut Then
            LejatszasInditasa()
        End If
    End Sub

    Private Sub btnStop_Click(sender As Object, e As EventArgs) Handles btnStop.Click
        Leallitas()
    End Sub

    ''' <summary>
    ''' Felfegyverzés a FELHASZNÁLÓ kérésére (Start, F9, az ablak megnyitása
    ''' vagy a pipa visszatétele automatikus módban): a felvétel az első
    ''' LEÜTÉSSEL indul el (lásd FelvevoAllapot.Felfegyverezve). Itt indul
    ''' újra a mentett szegmensek számlálója is - az automatikus
    ''' újra-felfegyverzés (lásd SzegmensEllenorzese) nem nullázza.
    ''' </summary>
    Private Sub Inditas()
        If felvevo.Allapot <> FelvevoAllapot.Alap Then Return
        mentettSzegmensek = 0
        felvevo.Felfegyverzes(AddressOf KezdoAllapotLekerdezese, AddressOf NullpontLekerdezese)
        AllapotValtozasJelzese()
    End Sub

    ''' <summary>
    ''' Leállítás. Felfegyverzett, de még el nem indult állapotban nincs mit
    ''' menteni: csendben visszaáll, üzenetablak nélkül (a felhasználóval
    ''' egyeztetett viselkedés). Felvétel közben a kézi Stop MINDIG ment -
    ''' automatikus módban is, a küszöböktől függetlenül (az a felhasználó
    ''' kifejezett szándéka).
    '''
    ''' Automatikus módban a Stop a pipát is kiveszi: a rendszer láthatóan
    ''' "kézi" módra vált, és nincs újra-felfegyverzés.
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
        chkAutomatikus.Checked = False
        AllapotValtozasJelzese()
    End Sub

    ' ============================================================
    ' Automatikus felvétel (4. fázis)
    ' ============================================================

    ''' <summary>
    ''' A főablak hívja MINDEN kiküldött üzenet előtt, mielőtt a felvevőnek
    ''' továbbadná. Ha a szünet-küszöb már letelt, a régi szegmens ITT zárul
    ''' le - így a küszöb után érkező üzenet (egy új leütés, vagy egy új
    ''' szakasz előtt lenyomott pedál) már az ÚJ szegmensbe kerül, akkor is,
    ''' ha az időzítő még nem ért oda. Az időzítő csak negyedmásodpercenként
    ''' néz rá - enélkül a határon érkező hang a régi szegmens végére
    ''' kerülhetne.
    ''' </summary>
    Public Sub UzenetElottiEllenorzes(statuszBajt As Integer, idobelyegMs As Long,
                                      lejatszasBol As Boolean, metronomBol As Boolean, belsoUzenet As Boolean)
        If Not felvevo.FigyelembeVettUzenetE(statuszBajt, lejatszasBol, metronomBol, belsoUzenet) Then Return
        SzegmensEllenorzese(idobelyegMs)
    End Sub

    ''' <summary>
    ''' Az Automatikus felvétel mód lényege: ha a szünet-küszöb letelt (lásd
    ''' AutomatikusSzegmentalo), a szegmens mentésre vagy eldobásra kerül, és
    ''' a felvevő AZONNAL újra felfegyverez a következő szegmenshez.
    '''
    ''' Mentési hiba esetén az automatikus mód kikapcsol (EGY üzenetablak),
    ''' és a szegmens megmarad - a Stop gombbal újra menthető.
    '''
    ''' Lejátszás alatt NINCS vágás, és a szünet legkorábban a lejátszás
    ''' végétől számít (a felhasználó döntése, 2026-11-03): ráfelvételnél a
    ''' játékban lehetnek hosszabb szünetek, miközben a lejátszás szól.
    ''' </summary>
    Friend Sub SzegmensEllenorzese(mostMs As Long)
        If szegmensLezarasFolyamatban OrElse Not chkAutomatikus.Checked Then Return
        If lejatszo.Fut Then Return

        Dim csendKezdete As Long = felvevo.CsendKezdeteMs
        Dim lejatszasVege As Long? = lejatszo.UtolsoLeallasOraMs
        If csendKezdete >= 0 AndAlso lejatszasVege.HasValue AndAlso lejatszasVege.Value > csendKezdete Then
            csendKezdete = lejatszasVege.Value
        End If

        Dim dontes As SzegmensDontes = AutomatikusSzegmentalo.Dontes(felvevo.Allapot, csendKezdete, felvevo.RogzitettHosszMs,
                                                                     mostMs, SzunetKuszobMs, JatekidoKuszobMs)
        If dontes = SzegmensDontes.Semmi Then Return

        szegmensLezarasFolyamatban = True
        Try
            If dontes = SzegmensDontes.Mentes Then
                If Not Mentes(automatikusMentes:=True) Then
                    AllapotValtozasJelzese()
                    Return
                End If
            Else
                felvevo.Eldobas()
            End If

            felvevo.Felfegyverzes(AddressOf KezdoAllapotLekerdezese, AddressOf NullpontLekerdezese)
            AllapotValtozasJelzese()
        Finally
            szegmensLezarasFolyamatban = False
        End Try
    End Sub

    Private ReadOnly Property SzunetKuszobMs As Long
        Get
            Return CLng(nudSzunetKuszob.Value) * 1000L
        End Get
    End Property

    Private ReadOnly Property JatekidoKuszobMs As Long
        Get
            Return CLng(nudJatekidoKuszob.Value) * 1000L
        End Get
    End Property

    ''' <summary>
    ''' A pipa KIVÉTELE felfegyverzett állapotban megszünteti a
    ''' felfegyverzést; felvétel közben viszont kézi módra vált - a futó
    ''' felvétel megy tovább, csak a Stop zárja le (egy véletlen kattintás
    ''' így nem vágja el a játékot). A pipa VISSZATÉTELE nyugalmi állapotban
    ''' azonnal felfegyverez: ha be van pipálva, nem kell Startot nyomni -
    ''' ettől automatikus. (A felhasználó döntései, 2026-09-25.)
    ''' </summary>
    Private Sub chkAutomatikus_CheckedChanged(sender As Object, e As EventArgs) Handles chkAutomatikus.CheckedChanged
        If betoltesAlatt Then Return
        automatikusModAllapot = chkAutomatikus.Checked
        If chkAutomatikus.Checked Then
            If felvevo.Allapot = FelvevoAllapot.Alap Then
                Inditas()
                Return
            End If
        ElseIf felvevo.Allapot = FelvevoAllapot.Felfegyverezve Then
            felvevo.Eldobas()
        End If
        AllapotValtozasJelzese()
    End Sub

    ''' <summary>
    ''' A felvétel lezárása és mentése, majd - ha be van kapcsolva - a
    ''' jelentés elkészítése mellé. A jelentés hibája a felvételt NEM
    ''' veszélyezteti: a .mid ilyenkor is megvan, csak jelezzük.
    ''' </summary>
    ''' <param name="automatikusMentes">
    ''' Az Automatikus felvétel mód mentése: hiba esetén a mód KIKAPCSOL (a
    ''' pipa kikerül), hogy a következő szegmensek ne ütközzenek újra és újra
    ''' ugyanabba a hibába - és ezt az üzenet is megmondja.
    ''' </param>
    ''' <returns>True, ha a mentés sikerült; False, ha nem (a felvett anyag ilyenkor megmarad).</returns>
    Private Function Mentes(Optional automatikusMentes As Boolean = False) As Boolean
        Dim utvonal As String
        Try
            utvonal = Utvonalak.UjFelvetelUtvonala(txtFajlNev.Text, chkDatumAlmappa.Checked)
            If Not felvevo.LezarasEsMentes(utvonal) Then Return True   ' nem volt mit menteni
        Catch ex As Exception
            If automatikusMentes Then chkAutomatikus.Checked = False
            MsgBox("Nem sikerült elmenteni a felvételt." & vbCrLf & vbCrLf &
                   "Hiba: " & ex.Message & vbCrLf & vbCrLf &
                   If(automatikusMentes, "Az automatikus felvétel kikapcsolt. ", "") &
                   "A felvett anyag megmaradt - a Stop gombbal újra megpróbálhatod.",
                   MsgBoxStyle.Exclamation, "MidiBill - felvétel")
            Return False
        End Try

        utoljaraMentettFajl = utvonal
        mentettSzegmensek += 1

        ' A friss felvétel betöltése a lejátszóba (NEM indítja el). Futó
        ' lejátszást nem szakít meg - egy lejátszás alatti automatikus
        ' szegmens-mentés így nem állítja le a lejátszást.
        If chkBetoltesLejatszoba.Checked AndAlso Not lejatszo.Fut Then LejatszasiFajlBetoltese(utvonal, csendes:=True)

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
                AllapotSzinezese(If(chkAutomatikus.Checked, "Várakozás billentyűre (AUTO)", "Várakozás billentyűre..."),
                                 Color.FromArgb(253, 240, 213), Color.FromArgb(122, 74, 0), Color.Gold)
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

        lblSzegmensSzamlalo.Text = If(chkAutomatikus.Checked AndAlso mentettSzegmensek > 0,
                                      "(" & mentettSzegmensek.ToString(Globalization.CultureInfo.InvariantCulture) & " szegmens mentve)",
                                      "")
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
    ''' Automatikus módban "● AUTO FELVÉTEL 0:42", és ha a szegmens már
    ''' elérte a játékidő-küszöböt (tehát biztosan mentésre kerül), egy ✓.
    ''' </summary>
    Private Function FelvetelFelirata() As String
        Dim eltelt As Long = Math.Max(0L, Ora.Invoke() - felvevo.KezdoIdobelyegMs)
        Dim ido As TimeSpan = TimeSpan.FromMilliseconds(eltelt)
        Dim idoSzoveg As String = If(ido.TotalHours >= 1,
                                     ido.ToString("h\:mm\:ss", Globalization.CultureInfo.InvariantCulture),
                                     ido.ToString("m\:ss", Globalization.CultureInfo.InvariantCulture))
        If Not chkAutomatikus.Checked Then Return "● FELVÉTEL  " & idoSzoveg
        Dim biztosMentes As Boolean = felvevo.RogzitettHosszMs >= JatekidoKuszobMs
        Return "● AUTO FELVÉTEL  " & idoSzoveg & If(biztosMentes, "  ✓", "")
    End Function

    ''' <summary>
    ''' Negyedmásodpercenként: az Automatikus felvétel mód szünet-figyelése
    ''' (hogy csendben, üzenet nélkül is lezáruljon a szegmens), és a futó
    ''' idő kijelzése.
    ''' </summary>
    Private Sub kijelzoIdozito_Tick(sender As Object, e As EventArgs) Handles kijelzoIdozito.Tick
        SzegmensEllenorzese(Ora.Invoke())
        If felvevo.Allapot = FelvevoAllapot.Rogzit Then lblAllapot.Text = FelvetelFelirata()
        If lejatszo.Fut Then PozicioKijelzesFrissitese()
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
    ' Lejátszás (5. fázis)
    ' ============================================================

    Private ReadOnly panelekVisszakuldese As Action
    Private ReadOnly fajlBeallitasainakAtvetele As Func(Of String, String)

    ''' <summary>A lejátszó - háttérszálon küld, lásd Modellek\MidiLejatszo.vb.</summary>
    Private ReadOnly lejatszo As MidiLejatszo

    ''' <summary>A lejátszóba betöltött fájl teljes útvonala, vagy üres.</summary>
    Private betoltottLejatszasiFajl As String = ""

    ''' <summary>Ahol a "Fájl megnyitása..." legutóbb járt (MidiBill3.ini); üresen a Felvetelek mappa.</summary>
    Private utolsoLejatszasMappa As String = ""

    ''' <summary>
    ''' A futó lejátszás végén vissza kell-e küldeni a panelek beállításait.
    ''' Az INDÍTÁSKOR dől el (a "beállítások megtartása" jelölőnégyzet
    ''' szerint), ezért a jelölőnégyzet lejátszás közben nem állítható.
    ''' </summary>
    Private panelekVisszaallitasaAVegen As Boolean = False

    Private ReadOnly dlgLejatszasMegnyitas As New OpenFileDialog With {
        .Title = "Lejátszandó MIDI fájl",
        .Filter = "MIDI fájlok (*.mid;*.midi)|*.mid;*.midi|Minden fájl (*.*)|*.*"
    }

    ''' <summary>F8 és a főablak F8-a: lejátszás közben leállít, egyébként elindít.</summary>
    Public Sub LejatszasValtasa()
        If lejatszo.Fut Then
            LejatszasLeallitasa()
        Else
            LejatszasInditasa()
        End If
    End Sub

    Private Sub btnFajlMegnyitasa_Click(sender As Object, e As EventArgs) Handles btnFajlMegnyitasa.Click
        LejatszasiFajlValasztasa()
    End Sub

    Private Sub btnLejatszas_Click(sender As Object, e As EventArgs) Handles btnLejatszas.Click
        LejatszasInditasa()
    End Sub

    Private Sub btnLejatszasStop_Click(sender As Object, e As EventArgs) Handles btnLejatszasStop.Click
        LejatszasLeallitasa()
    End Sub

    ''' <summary>
    ''' A lejátszás indítása. Ha nincs betöltött fájl, előbb a
    ''' fájlválasztó nyílik meg.
    '''
    ''' A "A lejátszott fájl beállításainak megtartása" jelölőnégyzet
    ''' (a felhasználó döntése, 2026-09-26):
    ''' - bekapcsolva a felület MOST átáll a fájl szerint, a "Preset készítése
    '''   MIDI fájlból" logikájával, és a végén nincs visszaállítás;
    ''' - kikapcsolva a lejátszás a fájl beállításaival szól, a végén (Stop
    '''   vagy a fájl vége) pedig a panelek beállításai visszamennek a
    '''   szintetizátorra.
    ''' </summary>
    Private Sub LejatszasInditasa()
        If lejatszo.Fut Then Return
        If Not lejatszo.VanBetoltottEsemeny Then
            If Not LejatszasiFajlValasztasa() OrElse Not lejatszo.VanBetoltottEsemeny Then Return
        End If

        If chkBeallitasokMegtartasa.Checked Then
            Dim hiba As String = fajlBeallitasainakAtvetele(betoltottLejatszasiFajl)
            If hiba.Length > 0 Then
                MsgBox("A fájl beállításait nem sikerült átvenni a felületre - a lejátszás a fájl beállításaival szól." &
                       vbCrLf & vbCrLf & "Hiba: " & hiba, MsgBoxStyle.Exclamation, "MidiBill - lejátszás")
            End If
            panelekVisszaallitasaAVegen = False
        Else
            panelekVisszaallitasaAVegen = True
        End If

        lejatszo.Inditas()
        LejatszasKijelzesFrissitese()
    End Sub

    ''' <summary>
    ''' A lejátszás leállítása: csak a lejátszás hangjai némulnak el (lásd
    ''' MidiLejatszo.Leallitas). A főablak kimenetváltáskor és kilépéskor
    ''' panelekVissza:=False-szal hívja (ott a panelek úgyis újra kimennek,
    ''' illetve már nincs értelme).
    ''' </summary>
    Public Sub LejatszasLeallitasa(Optional panelekVissza As Boolean = True)
        lejatszo.Leallitas()
        LejatszasVegeKezelese(panelekVissza)
    End Sub

    Private Sub LejatszasVegeKezelese(panelekVissza As Boolean)
        If panelekVisszaallitasaAVegen Then
            panelekVisszaallitasaAVegen = False
            If panelekVissza Then panelekVisszakuldese()
        End If
        LejatszasKijelzesFrissitese()
    End Sub

    ''' <summary>A fájl vége - a LEJÁTSZÓ SZÁLÁN érkezik, a felületi szálra adjuk át.</summary>
    Private Sub Lejatszo_Befejezodott()
        If IsDisposed OrElse Not IsHandleCreated Then Return
        Try
            BeginInvoke(Sub()
                            ' Ha közben már új lejátszás indult, ez a jelzés elavult.
                            If Not lejatszo.Fut Then LejatszasVegeKezelese(True)
                        End Sub)
        Catch ex As InvalidOperationException
            ' Az ablak épp bezárul - nincs mit frissíteni.
        End Try
    End Sub

    ''' <summary>
    ''' A lejátszandó fájl kiválasztása. Először a Felvetelek mappában nyílik
    ''' meg, utána a legutóbb használt mappában.
    ''' </summary>
    ''' <returns>True, ha a felhasználó választott és a fájl betöltődött.</returns>
    Private Function LejatszasiFajlValasztasa() As Boolean
        Dim mappa As String = utolsoLejatszasMappa
        If mappa.Length = 0 OrElse Not IO.Directory.Exists(mappa) Then
            IO.Directory.CreateDirectory(Utvonalak.FelvetelekMappa)
            mappa = Utvonalak.FelvetelekMappa
        End If
        dlgLejatszasMegnyitas.InitialDirectory = mappa
        dlgLejatszasMegnyitas.FileName = ""
        If dlgLejatszasMegnyitas.ShowDialog(Me) <> DialogResult.OK Then Return False

        utolsoLejatszasMappa = IO.Path.GetDirectoryName(dlgLejatszasMegnyitas.FileName)
        Return LejatszasiFajlBetoltese(dlgLejatszasMegnyitas.FileName, csendes:=False)
    End Function

    ''' <summary>
    ''' Egy .mid fájl betöltése a lejátszóba (futó lejátszást leállít). A
    ''' mentés utáni automatikus betöltés csendes: hiba esetén nem jelez.
    ''' </summary>
    Friend Function LejatszasiFajlBetoltese(utvonal As String, csendes As Boolean) As Boolean
        Dim lista As MidiEsemenyLista
        Try
            lista = MidiFajlOlvaso.EsemenyekBeolvasasa(utvonal)
            If lista.Esemenyek.Count = 0 Then
                Throw New IO.InvalidDataException("A fájl nem tartalmaz lejátszható MIDI üzenetet.")
            End If
        Catch ex As Exception
            If Not csendes Then
                MsgBox("Nem sikerült betölteni a(z) """ & IO.Path.GetFileName(utvonal) & """ fájlt." & vbCrLf & vbCrLf &
                       "Hiba: " & ex.Message, MsgBoxStyle.Exclamation, "MidiBill - lejátszás")
            End If
            Return False
        End Try

        If lejatszo.Fut Then LejatszasLeallitasa()
        lejatszo.Betoltes(lista)
        betoltottLejatszasiFajl = utvonal
        trkPozicio.Value = 0
        trkPozicio.Maximum = Math.Max(1, CInt(Math.Min(Integer.MaxValue, Math.Ceiling(lejatszo.HosszMs - 0.001))))   ' a -0,001: lebegőpontos zaj ne adjon +1 ms-t
        LejatszasKijelzesFrissitese()
        Return True
    End Function

    ' ------------------------------------------------------------
    ' Ráfelvétel (7. fázis)
    ' ------------------------------------------------------------

    ''' <summary>
    ''' A felvevő ezt kéri le az első leütéskor: a panelek pillanatképe, és
    ''' ha a lejátszott hangok is rögzülnek, utána a LEJÁTSZÁS csatornáinak
    ''' addigi állapota (hangszín, vezérlők stb. - lásd
    ''' MidiLejatszo.AllapotPillanatkep). Így a fájlba másolt lejátszás akkor
    ''' is a helyes hangszínekkel szól, ha a felvétel a fájl közepén indult.
    ''' A lejátszás állapota a panelé UTÁN áll, mert a szintetizátoron is az
    ''' él: a lejátszás induláskor kiküldte a magáét.
    ''' </summary>
    Private Function KezdoAllapotLekerdezese() As List(Of MidiUzenet)
        Dim lista As New List(Of MidiUzenet)
        Dim panelek As List(Of MidiUzenet) = pillanatkepLekerdezo()
        If panelek IsNot Nothing Then lista.AddRange(panelek)
        If felvevo.LejatszottHangokRogzitese Then lista.AddRange(lejatszo.AllapotPillanatkep(felvevo.KezdoIdobelyegMs))
        Return lista
    End Function

    ''' <summary>
    ''' A felvevő ezt kérdezi meg az első leütéskor: ha épp szól a
    ''' lejátszás, a mentett fájl a lejátszott fájl elejéhez igazodik (a
    ''' felhasználó döntése, 2026-11-03), így a kettő egymásra rakható.
    ''' </summary>
    Private Function NullpontLekerdezese() As Long?
        Return lejatszo.FajlNullpontOraMs
    End Function

    ''' <summary>
    ''' "A lejátszott hangok rögzítése": bekapcsolva a lejátszás is a
    ''' felvételbe kerül (és az első lejátszott hang is elindítja a
    ''' felfegyverzett felvételt - a felhasználó döntése, 2026-11-03);
    ''' kikapcsolva csak az élő játék. Felvétel közben is átállítható: a
    ''' következő üzenettől érvényes.
    ''' </summary>
    Private Sub chkLejatszottRogzitese_CheckedChanged(sender As Object, e As EventArgs) Handles chkLejatszottRogzitese.CheckedChanged
        felvevo.LejatszottHangokRogzitese = chkLejatszottRogzitese.Checked
    End Sub

    ' ------------------------------------------------------------
    ' Tekerés és időkijelzés (6. fázis)
    ' ------------------------------------------------------------

    ''' <summary>A billentyűs tekerés lépése: ← / → 5 másodperc (a felhasználó döntése, 2026-11-01).</summary>
    Private Const TEKERES_LEPES_MS As Double = 5000

    ''' <summary>
    ''' Igaz, amíg a felhasználó egérrel húzza a csúszkát: ilyenkor a
    ''' program NEM állítja a csúszkát (különben a lejátszás pozíciója
    ''' visszarántaná), és a tekerés csak az egérgomb felengedésekor történik
    ''' meg - húzás közben a kijelzés a célpontot mutatja.
    ''' </summary>
    Private egerrelHuz As Boolean = False

    ''' <summary>
    ''' Tekerés a megadott pontra. Lejátszás közben azonnal odaugrik (csak a
    ''' lejátszás saját hangjai némulnak el, és a csatornák állapota a
    ''' célpontig felépül - lásd MidiLejatszo.Ugras); megállított állapotban
    ''' a következő lejátszás kezdőpontját állítja.
    ''' </summary>
    Friend Sub Tekeres(celMs As Double)
        If Not lejatszo.VanBetoltottEsemeny Then Return
        lejatszo.Ugras(celMs)
        PozicioKijelzesFrissitese()
    End Sub

    ''' <summary>
    ''' A csúszka és a "1:22 / 3:41" felirat a lejátszó pozíciója szerint.
    ''' A csúszka értékének programból való állítása NEM vált ki Scroll
    ''' eseményt - így nincs visszacsatolás a tekerés felé.
    ''' </summary>
    Private Sub PozicioKijelzesFrissitese()
        If egerrelHuz Then Return
        Dim pozicio As Double = lejatszo.PozicioMs
        trkPozicio.Value = Math.Max(trkPozicio.Minimum, Math.Min(trkPozicio.Maximum, CInt(Math.Round(pozicio))))
        lblIdo.Text = IdoFelirat(pozicio)
    End Sub

    ''' <summary>"1:22 / 3:41" (egy óránál hosszabb fájlnál "h:mm:ss").</summary>
    Private Function IdoFelirat(pozicioMs As Double) As String
        Dim hossz As Double = lejatszo.HosszMs
        Return IdoSzoveg(pozicioMs, hossz) & " / " & IdoSzoveg(hossz, hossz)
    End Function

    Private Shared Function IdoSzoveg(ms As Double, hosszMs As Double) As String
        Dim ido As TimeSpan = TimeSpan.FromSeconds(Math.Floor(Math.Max(0, ms) / 1000))
        If hosszMs >= 3600000 Then
            Return ido.ToString("h\:mm\:ss", Globalization.CultureInfo.InvariantCulture)
        End If
        Return CInt(Math.Floor(ido.TotalMinutes)).ToString(Globalization.CultureInfo.InvariantCulture) &
               ":" & ido.Seconds.ToString("00", Globalization.CultureInfo.InvariantCulture)
    End Function

    ''' <summary>
    ''' Egérrel a csúszkára kattintva a pozíció azonnal a kattintás helyére
    ''' ugrik (nem lapoz, ahogy a TrackBar alapból tenné - ezért LargeChange
    ''' = 0), és a húzás innen folytatódik.
    ''' </summary>
    Private Sub trkPozicio_MouseDown(sender As Object, e As MouseEventArgs) Handles trkPozicio.MouseDown
        If e.Button <> MouseButtons.Left Then Return
        egerrelHuz = True
        Dim margo As Integer = CInt(13 * DeviceDpi / 96.0)
        Dim hasznos As Integer = Math.Max(1, trkPozicio.Width - 2 * margo)
        Dim arany As Double = Math.Max(0, Math.Min(1, (e.X - margo) / CDbl(hasznos)))
        trkPozicio.Value = CInt(Math.Round(trkPozicio.Minimum + arany * (trkPozicio.Maximum - trkPozicio.Minimum)))
        lblIdo.Text = IdoFelirat(trkPozicio.Value)
    End Sub

    ''' <summary>Húzás közben csak a kijelzés követi; billentyűvel (pl. End) azonnal teker.</summary>
    Private Sub trkPozicio_Scroll(sender As Object, e As EventArgs) Handles trkPozicio.Scroll
        If egerrelHuz Then
            lblIdo.Text = IdoFelirat(trkPozicio.Value)
        Else
            Tekeres(trkPozicio.Value)
        End If
    End Sub

    Private Sub trkPozicio_MouseUp(sender As Object, e As MouseEventArgs) Handles trkPozicio.MouseUp
        If Not egerrelHuz Then Return
        egerrelHuz = False
        Tekeres(trkPozicio.Value)
    End Sub

    ''' <summary>
    ''' ← / → = 5 mp vissza / előre, Home = a fájl eleje - csak a felvevő
    ''' ablakban, és nem akkor, ha egy szövegmezőben vagy számmezőben áll a
    ''' kurzor (ott a nyilaknak a megszokott dolguk van). A ProcessCmdKey
    ''' kell, mert a nyilakat a Windows a gombokon a fókusz mozgatására
    ''' használja, így a KeyDown-ig el sem jutnának.
    ''' </summary>
    Protected Overrides Function ProcessCmdKey(ByRef msg As Message, keyData As Keys) As Boolean
        If TekeresBillentyuvel(keyData) Then Return True
        Return MyBase.ProcessCmdKey(msg, keyData)
    End Function

    ''' <returns>True, ha a billentyűt a tekerés kezelte.</returns>
    Friend Function TekeresBillentyuvel(keyData As Keys) As Boolean
        If keyData <> Keys.Left AndAlso keyData <> Keys.Right AndAlso keyData <> Keys.Home Then Return False
        If Not lejatszo.VanBetoltottEsemeny OrElse egerrelHuz Then Return False
        If TypeOf ActiveControl Is TextBoxBase OrElse TypeOf ActiveControl Is UpDownBase Then Return False
        Select Case keyData
            Case Keys.Left
                Tekeres(lejatszo.PozicioMs - TEKERES_LEPES_MS)
            Case Keys.Right
                Tekeres(lejatszo.PozicioMs + TEKERES_LEPES_MS)
            Case Else
                Tekeres(0)
        End Select
        Return True
    End Function

    ''' <summary>
    ''' A Lejátszás csoport kijelzése: a címben a betöltött fájl neve, a
    ''' Lejátszás gomb lejátszás közben ZÖLD (stabil szín), a Stop csak
    ''' ilyenkor aktív, a "beállítások megtartása" pedig csak nyugalomban
    ''' állítható.
    ''' </summary>
    Private Sub LejatszasKijelzesFrissitese()
        Dim fut As Boolean = lejatszo.Fut
        grpLejatszas.Text = If(betoltottLejatszasiFajl.Length = 0, "Lejátszás",
                               "Lejátszás: " & IO.Path.GetFileName(betoltottLejatszasiFajl))
        btnLejatszas.Enabled = Not fut
        btnLejatszasStop.Enabled = fut
        chkBeallitasokMegtartasa.Enabled = Not fut
        trkPozicio.Enabled = lejatszo.VanBetoltottEsemeny
        PozicioKijelzesFrissitese()
        If fut Then
            btnLejatszas.UseVisualStyleBackColor = False
            btnLejatszas.BackColor = Color.LightGreen
        Else
            btnLejatszas.UseVisualStyleBackColor = True
        End If
    End Sub

    ' ============================================================
    ' A felvevő beállításai és az ablak helye (MidiBill3.ini)
    ' ============================================================

    Private Const KULCS_DATUM_ALMAPPA As String = "DatumAlmappa"
    Private Const KULCS_JELENTES As String = "Jelentes"
    Private Const KULCS_JATEKIDO_KUSZOB As String = "JatekidoKuszob"
    Private Const KULCS_SZUNET_KUSZOB As String = "SzunetKuszob"
    Private Const KULCS_AUTOMATIKUS As String = "Automatikus"
    Private Const KULCS_BETOLTES_LEJATSZOBA As String = "BetoltesLejatszoba"
    Private Const KULCS_BEALLITASOK_MEGTARTASA As String = "LejatszottBeallitasokMegtartasa"
    Private Const KULCS_LEJATSZAS_MAPPA As String = "LejatszasMappa"
    Private Const KULCS_LEJATSZOTT_ROGZITESE As String = "LejatszottHangokRogzitese"
    Private Const KULCS_LEJATSZAS_FELVETELLEL As String = "LejatszasFelvetellel"

    ''' <summary>
    ''' A felvevő beállításai a MidiBill3.ini-ből; ami hiányzik, annál a
    ''' Designerben megadott alapérték marad - az "Automatikus felvétel"
    ''' jelölőnégyzetnél a BEKAPCSOLT (a felhasználó döntése, 2026-09-25:
    ''' a program az utoljára elmentett állapottal indul, és csak mentett
    ''' érték híján automatikus). A fájlnév SZÁNDÉKOSAN nincs itt: az a
    ''' betöltött preset nevét követi.
    ''' </summary>
    Private Sub FelvevoBeallitasokBetoltese()
        Dim ini As IniFajl = ExtraBeallitasok.Ini
        Dim szakasz As String = ExtraBeallitasok.SZAKASZ_FELVEVO
        chkAutomatikus.Checked = ini.Logikai(szakasz, KULCS_AUTOMATIKUS, True)
        chkDatumAlmappa.Checked = ini.Logikai(szakasz, KULCS_DATUM_ALMAPPA, chkDatumAlmappa.Checked)
        chkJelentes.Checked = ini.Logikai(szakasz, KULCS_JELENTES, chkJelentes.Checked)
        nudJatekidoKuszob.Value = KorlatokKozott(nudJatekidoKuszob,
            ini.Egesz(szakasz, KULCS_JATEKIDO_KUSZOB, CInt(nudJatekidoKuszob.Value)))
        nudSzunetKuszob.Value = KorlatokKozott(nudSzunetKuszob,
            ini.Egesz(szakasz, KULCS_SZUNET_KUSZOB, CInt(nudSzunetKuszob.Value)))
        chkBetoltesLejatszoba.Checked = ini.Logikai(szakasz, KULCS_BETOLTES_LEJATSZOBA, True)
        chkBeallitasokMegtartasa.Checked = ini.Logikai(szakasz, KULCS_BEALLITASOK_MEGTARTASA, False)
        utolsoLejatszasMappa = ini.Szoveg(szakasz, KULCS_LEJATSZAS_MAPPA, "")
        chkLejatszottRogzitese.Checked = ini.Logikai(szakasz, KULCS_LEJATSZOTT_ROGZITESE, False)
        chkLejatszasFelvetellel.Checked = ini.Logikai(szakasz, KULCS_LEJATSZAS_FELVETELLEL, False)
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
        ini.Beallitas(szakasz, KULCS_AUTOMATIKUS, chkAutomatikus.Checked)
        ini.Beallitas(szakasz, KULCS_BETOLTES_LEJATSZOBA, chkBetoltesLejatszoba.Checked)
        ini.Beallitas(szakasz, KULCS_BEALLITASOK_MEGTARTASA, chkBeallitasokMegtartasa.Checked)
        ini.Beallitas(szakasz, KULCS_LEJATSZAS_MAPPA, utolsoLejatszasMappa)
        ini.Beallitas(szakasz, KULCS_LEJATSZOTT_ROGZITESE, chkLejatszottRogzitese.Checked)
        ini.Beallitas(szakasz, KULCS_LEJATSZAS_FELVETELLEL, chkLejatszasFelvetellel.Checked)
    End Sub

    ' ============================================================
    ' Billentyűzet és bezárás
    ' ============================================================

    Private Sub MidiFelvevoForm_KeyDown(sender As Object, e As KeyEventArgs) Handles MyBase.KeyDown
        Select Case e.KeyCode
            Case Keys.F9
                FelvetelValtasa()
            Case Keys.F8
                LejatszasValtasa()
            Case Else
                Return
        End Select
        e.Handled = True
        e.SuppressKeyPress = True
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
        LejatszasLeallitasa()
        RemoveHandler lejatszo.Befejezodott, AddressOf Lejatszo_Befejezodott
        lejatszo.Dispose()
        dlgLejatszasMegnyitas.Dispose()
        BeallitasokRogzitese()
        ExtraBeallitasok.Ini.Beallitas(ExtraBeallitasok.SZAKASZ_FELVEVO_ABLAK, ExtraBeallitasok.KULCS_NYITVA,
                                       NyitvaMaradE(e.CloseReason))
        ExtraBeallitasok.Mentes()
        kijelzoIdozito.Stop()
        kijelzoIdozito.Dispose()
        tipp.Dispose()
    End Sub

End Class

Imports MidiBill3.Modellek
Imports System.ComponentModel
Imports System.Linq

Public Class HangszerPanel

    ' Belső jelző: betöltés/kezdeti feltöltés közben ne tüzeljünk felesleges eseményeket
    Private betoltesAlatt As Boolean = False

    ' A cmbHangszer minden pozíciójához tartozó VALÓDI MIDI patch-szám,
    ' index-igazítva (hangszerKulcsok(0) = a combo 0. elemének patch-száma,
    ' stb.) - ritkás (nem 0-127 folytonos) hangszerlistáknál a combo
    ' pozíciója NEM egyezik meg a patch-számmal, ezért kell ez a leképezés.
    Private hangszerKulcsok As New List(Of Integer)

    ' Ha NEM Nothing, ez a lista van használva HangszerLista.Nevek helyett -
    ' a 10. (dob) csatornánál a MainForm ide adja át a HangszerLista.DobNevek-et.
    Private aktivHangszerforras As Dictionary(Of Integer, String) = Nothing

    Public Sub New()
        InitializeComponent()

        cmbHangszer.DropDownStyle = ComboBoxStyle.DropDownList
        betoltesAlatt = True
        HangszerListaItemekFeltoltese()
        cmbHangszer.SelectedIndex = 0 ' alapértelmezett hangszer: az első a listában (ua., mint a VB6-ban)
        betoltesAlatt = False

        ' Kezdeti értékkijelzés (a Tervezőben beállított alapértékek alapján)
        lblHerErtek.Text = trkHangero.Value.ToString()
        lblTerErtek.Text = trkTer.Value.ToString()
        lblVibErtek.Text = trkVibrato.Value.ToString()
        lblChorusErtek.Text = trkChorus.Value.ToString()
        lblOktavErtek.Text = OktavSzoveg(trkOktav.Value)
    End Sub

    ''' <summary>
    ''' Feltölti a cmbHangszer Items listáját és a hangszerKulcsok leképezést
    ''' az aktuális hangszerforrás (AktivHangszerLista - a globális
    ''' HangszerLista.Nevek, vagy a HangszerListaForrasBeallitasa()-val
    ''' beállított dedikált lista) alapján, KULCS szerint rendezve.
    '''
    ''' A kulcs szerinti rendezés a bankokat is értelmesen csoportosítja
    ''' (lásd Modellek\Hangszerkulcs.vb): elöl a bank nélküli, sima General
    ''' MIDI hangszerek, utánuk az LSB-s bankok, majd MSB szerint a többi -
    ''' vagyis egy GM2 vagy GS lista a végén, egy tömbben jelenik meg.
    ''' Bankos hangszereknél a névhez hozzáfűzi a bank rövid jelzését
    ''' ("(B8)", illetve "(B121.1)", ha MSB is van), hogy egyértelmű legyen,
    ''' ha egy hangszer nem a sima GM-bankból jön - lásd Hangszerkulcs.vb.
    ''' </summary>
    Private Sub HangszerListaItemekFeltoltese()
        cmbHangszer.Items.Clear()
        hangszerKulcsok.Clear()
        For Each kulcs As Integer In AktivHangszerLista.Keys.OrderBy(Function(k) k)
            hangszerKulcsok.Add(kulcs)
            ' A bank rövid jelölése egy helyen készül - lásd
            ' Modellek\Hangszerkulcs.vb (üres, " (B8)" vagy " (B121.1)").
            cmbHangszer.Items.Add(AktivHangszerLista(kulcs) & Hangszerkulcs.BankJeloles(kulcs))
        Next
    End Sub

    Private ReadOnly Property AktivHangszerLista As Dictionary(Of Integer, String)
        Get
            Return If(aktivHangszerforras, HangszerLista.Nevek)
        End Get
    End Property

    ''' <summary>
    ''' Újratölti a hangszer legördülőt az AKTUÁLIS hangszerforrás alapján
    ''' (lásd AktivHangszerLista) - akkor hívjuk, amikor menet közben
    ''' (induláskor vagy preset betöltésekor) megváltozik az aktív hangszerlista. 
    ''' Megpróbálja megtartani a jelenlegi kiválasztás VALÓDI PATCH-SZÁMÁT,
    ''' ha az még szerepel az új listában; ha nem, az első elemre esik vissza -
    ''' SOHA nem dob kivételt, még akkor sem, ha az új lista sokkal kisebb/ritkásabb,
    ''' mint a régi volt.
    ''' </summary>
    Public Sub HangszerListaFrissitese()
        UjratoltesMegorzottPatchhal()
    End Sub

    ''' <summary>
    ''' Beállítja, MELYIK hangszerlistát használja a panel: Nothing esetén a
    ''' globális, aktív HangszerLista.Nevek-et (a legtöbb csatornánál),
    ''' egyébként a megadott, DEDIKÁLT listát (pl. HangszerLista.DobNevek a
    ''' 10. csatornánál). Azonnal újratölti a combo-t az új listával, a
    ''' jelenlegi kiválasztás patch-számát megőrizve, ha az szerepel benne.
    ''' </summary>
    Public Sub HangszerListaForrasBeallitasa(lista As Dictionary(Of Integer, String))
        aktivHangszerforras = lista
        UjratoltesMegorzottPatchhal()
    End Sub

    Private Sub UjratoltesMegorzottPatchhal()
        Dim regiPatch As Integer = Hangszer
        betoltesAlatt = True
        HangszerListaItemekFeltoltese()
        Dim ujPozicio As Integer = hangszerKulcsok.IndexOf(regiPatch)
        If ujPozicio >= 0 Then
            cmbHangszer.SelectedIndex = ujPozicio
        ElseIf cmbHangszer.Items.Count > 0 Then
            cmbHangszer.SelectedIndex = 0
        End If
        betoltesAlatt = False
    End Sub

    ' --- Eseménydeklarációk: ezekre iratkozik fel a MainForm ---

    ''' <summary>
    ''' Hangszerválasztás változott (a paraméter a VALÓDI kombinált kulcs,
    ''' lásd Modellek\Hangszerkulcs.vb - nem a combo pozíciója)
    ''' </summary>
    Public Event HangszerValtozott(sender As Object, patchIndex As Integer)

    ''' <summary>Hangerő csúszka változott (0-127)</summary>
    Public Event HangeroValtozott(sender As Object, ertek As Integer)

    ''' <summary>Pan (Tér) csúszka változott (-64..63)</summary>
    Public Event TerValtozott(sender As Object, ertek As Integer)

    ''' <summary>Vibrato csúszka változott (0-127)</summary>
    Public Event VibratoValtozott(sender As Object, ertek As Integer)

    ''' <summary>Csatornánkénti Chorus csúszka változott (0-127)</summary>
    Public Event ChorusValtozott(sender As Object, ertek As Integer)

    ''' <summary>Aktív jelölőnégyzet állapota változott</summary>
    Public Event AktivValtozott(sender As Object, aktiv As Boolean)

    ''' <summary>"Alul" (Split ponttól alul szól) jelölőnégyzet állapota változott</summary>
    Public Event AlulValtozott(sender As Object, aktiv As Boolean)

    ''' <summary>"Felül" (Split ponttól felül szól) jelölőnégyzet állapota változott</summary>
    Public Event FelulValtozott(sender As Object, aktiv As Boolean)

    ''' <summary>Csatornánkénti oktáv-eltolás változott (-2..+2)</summary>
    Public Event OktavValtozott(sender As Object, ertek As Integer)

    ' --- Publikus property-k: kívülről is olvasható/írható állapot ---
    ' (íráskor NEM tüzelünk eseményt, hogy betöltéskor a MainForm ne
    '  generáljon felesleges MIDI-küldést minden egyes mező beállításakor -
    '  a betöltés végén a MainForm majd egyszer, explicit módon frissíti a MIDI-t)
    ' A DesignerSerializationVisibility.Hidden jelzi a WinForms Tervezőnek,
    ' hogy ezek tisztán futásidejű property-k, sose próbálja szerializálni
    ' őket a .Designer.vb fájlba (WFO1000 figyelmeztetés/hiba elkerülése).

    ''' <summary>
    ''' A kiválasztott hangszer VALÓDI kombinált kulcsa (a felépítését lásd
    ''' Modellek\Hangszerkulcs.vb) - NEM a combo pozíciója! Íráskor, ha a
    ''' megadott patch-szám
    ''' nem szerepel a jelenlegi hangszerlistában, biztonságosan az első elemre
    ''' esik vissza (nem dob kivételt).
    ''' </summary>
    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property Hangszer As Integer
        Get
            If cmbHangszer.SelectedIndex >= 0 AndAlso cmbHangszer.SelectedIndex < hangszerKulcsok.Count Then
                Return hangszerKulcsok(cmbHangszer.SelectedIndex)
            End If
            Return 0
        End Get
        Set(value As Integer)
            betoltesAlatt = True
            Dim pozicio As Integer = hangszerKulcsok.IndexOf(value)
            If pozicio >= 0 Then
                cmbHangszer.SelectedIndex = pozicio
            ElseIf cmbHangszer.Items.Count > 0 Then
                cmbHangszer.SelectedIndex = 0
            End If
            betoltesAlatt = False
        End Set
    End Property

    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
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

    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
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

    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
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

    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property Chorus As Integer
        Get
            Return trkChorus.Value
        End Get
        Set(value As Integer)
            betoltesAlatt = True
            trkChorus.Value = value
            betoltesAlatt = False
        End Set
    End Property

    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
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

    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property Alul As Boolean
        Get
            Return chkAlul.Checked
        End Get
        Set(value As Boolean)
            betoltesAlatt = True
            chkAlul.Checked = value
            betoltesAlatt = False
        End Set
    End Property

    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property Felul As Boolean
        Get
            Return chkFelul.Checked
        End Get
        Set(value As Boolean)
            betoltesAlatt = True
            chkFelul.Checked = value
            betoltesAlatt = False
        End Set
    End Property

    ''' <summary>Csatornánkénti oktáv-eltolás (-2..+2)</summary>
    <DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)>
    Public Property Oktav As Integer
        Get
            Return trkOktav.Value
        End Get
        Set(value As Integer)
            betoltesAlatt = True
            trkOktav.Value = value
            betoltesAlatt = False
        End Set
    End Property

    ' --- Belső eseménykezelők: a vezérlők változásait továbbítjuk kifelé ---

    Private Sub cmbHangszer_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbHangszer.SelectedIndexChanged
        If betoltesAlatt Then Return
        If cmbHangszer.SelectedIndex < 0 Then Return
        RaiseEvent HangszerValtozott(Me, hangszerKulcsok(cmbHangszer.SelectedIndex))
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

    Private Sub trkChorus_ValueChanged(sender As Object, e As EventArgs) Handles trkChorus.ValueChanged
        lblChorusErtek.Text = trkChorus.Value.ToString()
        If betoltesAlatt Then Return
        RaiseEvent ChorusValtozott(Me, trkChorus.Value)
    End Sub

    Private Sub trkOktav_ValueChanged(sender As Object, e As EventArgs) Handles trkOktav.ValueChanged
        lblOktavErtek.Text = OktavSzoveg(trkOktav.Value)
        If betoltesAlatt Then Return
        RaiseEvent OktavValtozott(Me, trkOktav.Value)
    End Sub

    ''' <summary>Előjeles szöveg az oktáv-értékhez (pl. "+1", "-2", "0")</summary>
    Private Function OktavSzoveg(ertek As Integer) As String
        Return If(ertek > 0, "+" & ertek, ertek.ToString())
    End Function

    Private Sub chkAktiv_CheckedChanged(sender As Object, e As EventArgs) Handles chkAktiv.CheckedChanged
        If betoltesAlatt Then Return
        RaiseEvent AktivValtozott(Me, chkAktiv.Checked)
    End Sub

    Private Sub chkAlul_CheckedChanged(sender As Object, e As EventArgs) Handles chkAlul.CheckedChanged
        If betoltesAlatt Then Return
        RaiseEvent AlulValtozott(Me, chkAlul.Checked)
    End Sub

    Private Sub chkFelul_CheckedChanged(sender As Object, e As EventArgs) Handles chkFelul.CheckedChanged
        If betoltesAlatt Then Return
        RaiseEvent FelulValtozott(Me, chkFelul.Checked)
    End Sub

End Class
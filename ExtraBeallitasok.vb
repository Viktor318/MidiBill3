Imports System.IO
Imports MidiBill3.Modellek

''' <summary>
''' A MidiBill3.ini (%AppData%\MidiBill3) "extra" beállításai: azok, amik
''' NEM preset-adatok - az ablakok pozíciója és a felvevő beállításai.
''' Ezek szándékosan nincsenek a Beallitasok osztályban: azt minden preset
''' mentése is használja, így egy ablakpozíció vagy egy felvételi küszöb
''' bekerülne minden Zongora.json-ba, és preset betöltésekor felülírná a
''' felhasználó aktuális beállítását.
'''
''' Működés: a fájl az első hozzáféréskor töltődik be a memóriába; az
''' ablakok bezáráskor ide jegyzik be az állapotukat (a ...Rogzitese
''' függvényekkel), a Mentes() pedig kiírja a fájlt. Hiányzó vagy sérült
''' fájl esetén minden az alapértékével indul, hibaüzenet nélkül - ezek
''' kényelmi beállítások, elvesztésük nem baj.
''' </summary>
Public Module ExtraBeallitasok

    Public Const SZAKASZ_FOABLAK As String = "FoAblak"
    Public Const SZAKASZ_FELVEVO_ABLAK As String = "FelvevoAblak"
    Public Const SZAKASZ_FELVEVO As String = "Felvevo"

    ''' <summary>
    ''' [FelvevoAblak] Nyitva=1: a főablak bezárásakor nyitva volt a
    ''' felvevő ablak, ezért a következő indításkor automatikusan
    ''' megnyílik (a mentett helyén).
    ''' </summary>
    Public Const KULCS_NYITVA As String = "Nyitva"

    Private Const KULCS_BAL As String = "Bal"
    Private Const KULCS_FELSO As String = "Felso"

    ''' <summary>
    ''' Legalább ekkora darabnak (px) kell a címsorból valamelyik képernyő
    ''' munkaterületén lennie, hogy az ablakot láthatónak - és egérrel
    ''' megfoghatónak - tekintsük.
    ''' </summary>
    Private Const LATHATO_MIN_SZELESSEG As Integer = 100
    Private Const LATHATO_MIN_MAGASSAG As Integer = 20
    Private Const CIMSOR_MAGASSAG As Integer = 30

    Private betoltott As IniFajl = Nothing

    ''' <summary>A MidiBill3.ini tartalma (az első hozzáféréskor töltődik be).</summary>
    Public ReadOnly Property Ini As IniFajl
        Get
            If betoltott Is Nothing Then
                Try
                    betoltott = IniFajl.Betoltes(Utvonalak.ExtraBeallitasokFajl)
                Catch ex As Exception
                    ' Olvashatatlan fájl: alapértékekkel indulunk, a
                    ' következő mentés felülírja egy ép fájllal.
                    betoltott = New IniFajl()
                End Try
            End If
            Return betoltott
        End Get
    End Property

    ''' <summary>A MidiBill3.ini kiírása. Hiba esetén csendben nem ment (kényelmi adat).</summary>
    Public Sub Mentes()
        If betoltott Is Nothing Then Return
        Try
            Directory.CreateDirectory(Utvonalak.FelhasznaloiMappa)
            betoltott.Mentes(Utvonalak.ExtraBeallitasokFajl)
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>
    ''' Az ablak mentett pozíciójának visszaállítása. Hamisat ad vissza (és
    ''' nem nyúl az ablakhoz), ha nincs mentett pozíció, vagy az már nem
    ''' látszana egyik képernyőn sem (pl. leválasztott második monitor) -
    ''' ilyenkor a hívó alapértelmezett elhelyezése érvényes.
    ''' Az ablak Load eseményéből kell hívni.
    ''' </summary>
    Public Function AblakHelyzetVisszaallitasa(ablak As Form, szakasz As String) As Boolean
        Dim bal As Integer = Ini.Egesz(szakasz, KULCS_BAL, Integer.MinValue)
        Dim felso As Integer = Ini.Egesz(szakasz, KULCS_FELSO, Integer.MinValue)
        If bal = Integer.MinValue OrElse felso = Integer.MinValue Then Return False

        Dim hely As New Rectangle(bal, felso, ablak.Width, ablak.Height)
        If Not LathatoE(hely) Then Return False

        ' Manual NÉLKÜL a CenterScreen/CenterParent a Load után még
        ' újraközépre tehetné az ablakot.
        ablak.StartPosition = FormStartPosition.Manual
        ablak.Location = hely.Location
        Return True
    End Function

    ''' <summary>
    ''' Az ablak pozíciójának bejegyzése (még nem írja ki a fájlt - azt a
    ''' Mentes() teszi). Kis méretre tett ablaknál a normál helyzetét
    ''' jegyzi fel, nem a tálcára tett ablak (-32000, -32000) helyét.
    ''' </summary>
    Public Sub AblakHelyzetRogzitese(ablak As Form, szakasz As String)
        Dim hely As Point = If(ablak.WindowState = FormWindowState.Normal,
                               ablak.Location, ablak.RestoreBounds.Location)
        Ini.Beallitas(szakasz, KULCS_BAL, hely.X)
        Ini.Beallitas(szakasz, KULCS_FELSO, hely.Y)
    End Sub

    ''' <summary>
    ''' Látszik-e az ablak címsorából egy megfogható darab valamelyik
    ''' képernyő munkaterületén.
    ''' </summary>
    Private Function LathatoE(hely As Rectangle) As Boolean
        Dim cimsor As New Rectangle(hely.Left, hely.Top, hely.Width, CIMSOR_MAGASSAG)
        Dim minSzelesseg As Integer = Math.Min(LATHATO_MIN_SZELESSEG, hely.Width)
        For Each kepernyo As Screen In Screen.AllScreens
            Dim metszet As Rectangle = Rectangle.Intersect(kepernyo.WorkingArea, cimsor)
            If metszet.Width >= minSzelesseg AndAlso metszet.Height >= LATHATO_MIN_MAGASSAG Then Return True
        Next
        Return False
    End Function

End Module

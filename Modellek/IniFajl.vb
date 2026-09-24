Imports System.Globalization
Imports System.IO
Imports System.Text

Namespace Modellek

    ''' <summary>
    ''' Egyszerű, kézzel is szerkeszthető INI fájl (szakaszok + kulcs=érték
    ''' sorok) beolvasása és kiírása. A MidiBill3.ini "extra" beállításaihoz
    ''' készült (ablakpozíciók, a felvevő beállításai) - azokhoz, amik NEM
    ''' preset-adatok, ezért nem valók a Kezdet.json-ba és a preset fájlokba.
    '''
    ''' Szabályok:
    ''' - A szakasz- és kulcsnevek kis- és nagybetűre érzéketlenek.
    ''' - A ";" és "#" kezdetű sorok megjegyzések; az üres sorok és a
    '''   szakaszon kívüli vagy "=" nélküli sorok figyelmen kívül maradnak.
    ''' - Az ismeretlen kulcsok is megmaradnak (beolvasás után változatlanul
    '''   visszaíródnak), így egy újabb programverzió beállításait egy
    '''   régebbi sem törli.
    ''' - A számok kultúrafüggetlenül (InvariantCulture) íródnak és
    '''   olvasódnak; a logikai érték "1" / "0" (olvasáskor a "true" /
    '''   "false" is elfogadott).
    ''' - Hibás vagy hiányzó érték esetén a hívó által megadott alapérték
    '''   érvényes - az olvasó függvények soha nem dobnak kivételt.
    ''' </summary>
    Public Class IniFajl

        Private Class IniSzakasz
            Public Nev As String = ""
            ''' <summary>A kulcsok eredeti sorrendje - a kiírás ebben a sorrendben történik.</summary>
            Public ReadOnly Kulcsok As New List(Of String)
            Public ReadOnly Ertekek As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        End Class

        Private ReadOnly szakaszok As New List(Of IniSzakasz)

        ''' <summary>
        ''' Beolvasás fájlból. Ha a fájl nem létezik, üres IniFajl-t ad vissza.
        ''' Olvasási (I/O) hiba esetén kivételt dob - a hívó dönti el, mit
        ''' kezd vele.
        ''' </summary>
        Public Shared Function Betoltes(utvonal As String) As IniFajl
            Dim ini As New IniFajl()
            If Not File.Exists(utvonal) Then Return ini
            ini.Feldolgozas(File.ReadAllLines(utvonal, Encoding.UTF8))
            Return ini
        End Function

        ''' <summary>Beolvasás kész sorokból (a tesztekhez is).</summary>
        Public Shared Function Szovegbol(sorok As IEnumerable(Of String)) As IniFajl
            Dim ini As New IniFajl()
            ini.Feldolgozas(sorok)
            Return ini
        End Function

        Private Sub Feldolgozas(sorok As IEnumerable(Of String))
            Dim aktualis As IniSzakasz = Nothing
            For Each nyersSor As String In sorok
                Dim sor As String = nyersSor.Trim()
                If sor.Length = 0 OrElse sor.StartsWith(";"c) OrElse sor.StartsWith("#"c) Then Continue For

                If sor.StartsWith("["c) AndAlso sor.EndsWith("]"c) Then
                    Dim nev As String = sor.Substring(1, sor.Length - 2).Trim()
                    aktualis = If(nev.Length = 0, Nothing, SzakaszKeresese(nev, True))
                    Continue For
                End If

                Dim egyenloseg As Integer = sor.IndexOf("="c)
                If aktualis Is Nothing OrElse egyenloseg <= 0 Then Continue For
                Dim kulcs As String = sor.Substring(0, egyenloseg).Trim()
                If kulcs.Length = 0 Then Continue For
                ErtekBeallitasa(aktualis, kulcs, sor.Substring(egyenloseg + 1).Trim())
            Next
        End Sub

        ''' <summary>
        ''' Kiírás fájlba (UTF-8 BOM-mal, CRLF sorvégekkel, hogy a Jegyzettömb
        ''' is helyesen mutassa az ékezeteket). Előbb egy ideiglenes fájlba ír,
        ''' és csak a sikeres írás után cseréli le vele a régit - így egy
        ''' félbeszakadt írás nem teheti tönkre a meglévő fájlt.
        ''' </summary>
        Public Sub Mentes(utvonal As String)
            Dim ideiglenes As String = utvonal & ".tmp"
            File.WriteAllText(ideiglenes, Szovegkent(), New UTF8Encoding(True))
            File.Move(ideiglenes, utvonal, True)
        End Sub

        ''' <summary>A teljes tartalom szövegként, pontosan úgy, ahogy a fájlba kerül.</summary>
        Public Function Szovegkent() As String
            Dim sb As New StringBuilder()
            sb.Append("; MidiBill3 - extra beállítások (ablakpozíciók, a felvevő beállításai)").Append(vbCrLf)
            sb.Append("; A program az ablakok bezárásakor felülírja - kézzel csak zárt program mellett érdemes szerkeszteni.").Append(vbCrLf)
            For Each szakasz As IniSzakasz In szakaszok
                If szakasz.Kulcsok.Count = 0 Then Continue For
                sb.Append(vbCrLf).Append("["c).Append(szakasz.Nev).Append("]"c).Append(vbCrLf)
                For Each kulcs As String In szakasz.Kulcsok
                    sb.Append(kulcs).Append("="c).Append(szakasz.Ertekek(kulcs)).Append(vbCrLf)
                Next
            Next
            Return sb.ToString()
        End Function

        ' ------------------------------------------------------------
        ' Olvasás
        ' ------------------------------------------------------------

        ''' <summary>A nyers szöveges érték, vagy az alapérték, ha nincs ilyen kulcs.</summary>
        Public Function Szoveg(szakaszNev As String, kulcs As String, alapertek As String) As String
            Dim szakasz As IniSzakasz = SzakaszKeresese(szakaszNev, False)
            Dim ertek As String = Nothing
            If szakasz IsNot Nothing AndAlso szakasz.Ertekek.TryGetValue(kulcs, ertek) Then Return ertek
            Return alapertek
        End Function

        ''' <summary>Egész szám, vagy az alapérték, ha a kulcs hiányzik vagy nem egész szám.</summary>
        Public Function Egesz(szakaszNev As String, kulcs As String, alapertek As Integer) As Integer
            Dim nyers As String = Szoveg(szakaszNev, kulcs, Nothing)
            Dim ertek As Integer
            If nyers IsNot Nothing AndAlso
               Integer.TryParse(nyers, NumberStyles.Integer, CultureInfo.InvariantCulture, ertek) Then
                Return ertek
            End If
            Return alapertek
        End Function

        ''' <summary>Logikai érték ("1"/"0", "true"/"false"), vagy az alapérték.</summary>
        Public Function Logikai(szakaszNev As String, kulcs As String, alapertek As Boolean) As Boolean
            Dim nyers As String = Szoveg(szakaszNev, kulcs, Nothing)
            If nyers Is Nothing Then Return alapertek
            If nyers = "1" OrElse nyers.Equals("true", StringComparison.OrdinalIgnoreCase) Then Return True
            If nyers = "0" OrElse nyers.Equals("false", StringComparison.OrdinalIgnoreCase) Then Return False
            Return alapertek
        End Function

        ' ------------------------------------------------------------
        ' Írás
        ' ------------------------------------------------------------

        Public Sub Beallitas(szakaszNev As String, kulcs As String, ertek As String)
            ErtekBeallitasa(SzakaszKeresese(szakaszNev, True), kulcs, If(ertek, ""))
        End Sub

        Public Sub Beallitas(szakaszNev As String, kulcs As String, ertek As Integer)
            Beallitas(szakaszNev, kulcs, ertek.ToString(CultureInfo.InvariantCulture))
        End Sub

        Public Sub Beallitas(szakaszNev As String, kulcs As String, ertek As Boolean)
            Beallitas(szakaszNev, kulcs, If(ertek, "1", "0"))
        End Sub

        ' ------------------------------------------------------------

        Private Function SzakaszKeresese(nev As String, letrehozas As Boolean) As IniSzakasz
            For Each szakasz As IniSzakasz In szakaszok
                If String.Equals(szakasz.Nev, nev, StringComparison.OrdinalIgnoreCase) Then Return szakasz
            Next
            If Not letrehozas Then Return Nothing
            Dim uj As New IniSzakasz With {.Nev = nev}
            szakaszok.Add(uj)
            Return uj
        End Function

        Private Shared Sub ErtekBeallitasa(szakasz As IniSzakasz, kulcs As String, ertek As String)
            If Not szakasz.Ertekek.ContainsKey(kulcs) Then szakasz.Kulcsok.Add(kulcs)
            szakasz.Ertekek(kulcs) = ertek
        End Sub

    End Class

End Namespace

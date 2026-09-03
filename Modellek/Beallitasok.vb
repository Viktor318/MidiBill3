Imports System.IO
Imports System.Text.Json
Imports System.Text.RegularExpressions

Namespace Modellek

    Public Class CsatornaBeallitas

        Public Property Hangszer As Integer = 0
        Public Property Hangero As Integer = 127
        Public Property Ter As Integer = 0
        Public Property Vibrato As Integer = 0
        Public Property Chorus As Integer = 0   ' ÚJ - csatornánkénti Chorus (MIDI CC93, 0-127)

        ''' <summary>
        ''' Aktív-e ez a csatorna. Alapértelmezett FALSE - a 16-csatornás
        ''' modellben csak az 1. csatornát aktiváljuk explicit módon indításkor,
        ''' a többi (beleértve az újonnan bevezetett 5-16. csatornákat is)
        ''' alapból inaktív marad, hogy régi, 4-csatornás presetek betöltésekor
        ''' ne "ébredjen fel" váratlanul 12 plusz csatorna.
        ''' </summary>
        Public Property Aktiv As Boolean = False

        Public Property Alul As Boolean = True
        Public Property Felul As Boolean = True

        ''' <summary>
        ''' Csatornánkénti oktáv-transzponálás (-2..+2). A globális
        ''' (félhangos) transzponálással ÖSSZEADÓDIK a kiküldött
        ''' hangmagasság kiszámításakor. Alapértelmezett 0.
        ''' </summary>
        Public Property Oktav As Integer = 0

    End Class

    Public Class Beallitasok

        ''' <summary>
        ''' A MidiBill3 verziószáma, amivel a fájl készült (pl. "1.3.0").
        ''' Tisztán tájékoztató jellegű - betöltéskor nem befolyásol semmit,
        ''' csak segít later visszakeresni, melyik verzióval mentődött a preset.
        ''' Régi fájloknál (ahol ez a mező nem szerepel) üres marad.
        ''' </summary>
        Public Property Verzio As String = ""

        ''' <summary>A 16 MIDI csatorna beállításai (mindig pontosan 16 elem, index 0 = 1. csatorna)</summary>
        Public Property Csatornak As New List(Of CsatornaBeallitas)

        Public Property AlapHangero As Integer = 70
        Public Property MidiBemenetNev As String = ""
        Public Property MidiKimenetNev As String = ""
        Public Property PedalInvertalva As Boolean = False
        Public Property SplitPont As Integer = 60
        Public Property Transzponalas As Integer = 0

        ''' <summary>
        ''' Globális Reverb (visszhang) mélysége, 0-100% (a felhasználói
        ''' felületen megjelenő, kerek érték - a tényleges MIDI CC91
        ''' kiküldésekor 0-127-re számítódik át, hogy a teljes MIDI-tartomány
        ''' kihasználható legyen). Egységes minden csatornára, nem
        ''' csatornánkénti beállítás.
        ''' </summary>
        Public Property Reverb As Integer = 0

        ''' <summary>
        ''' A jelenleg aktív hangszerlista fájlneve (az Adatok mappában, pl.
        ''' "GM.lst" vagy egy egyedi lista neve). Preset betöltésekor a
        ''' program megpróbálja ezt a fájlt betölteni az Adatok mappából.
        ''' </summary>
        Public Property HangszerListaFajl As String = "GM.lst"

        Public Shared Function Alapertelmezett() As Beallitasok
            Dim b As New Beallitasok()
            Kiegeszit16Csatornara(b)
            b.Csatornak(0).Aktiv = True
            Return b
        End Function

        Public Sub Mentes(fajlNev As String)
            Dim opciok As New JsonSerializerOptions With {
                .WriteIndented = True
            }
            Dim nyersJson As String = JsonSerializer.Serialize(Me, opciok)
            Dim vegsoJson As String = KommentekHozzaadasa(nyersJson)
            IO.File.WriteAllText(fajlNev, vegsoJson)
        End Sub

        Public Shared Function Betoltes(fajlNev As String) As Beallitasok
            Dim json As String = IO.File.ReadAllText(fajlNev)

            Dim opciok As New JsonSerializerOptions With {
                .ReadCommentHandling = JsonCommentHandling.Skip,
                .AllowTrailingCommas = True
            }

            Dim eredmeny As Beallitasok = JsonSerializer.Deserialize(Of Beallitasok)(json, opciok)
            If eredmeny Is Nothing Then
                ' A Deserialize szabványos viselkedése: egy szó szerint "null" tartalmú
                ' (vagy csak whitespace-eket/kommenteket tartalmazó) JSON fájlra Nothing-ot
                ' ad vissza, kivétel dobása nélkül - enélkül az ellenőrzés nélkül a következő
                ' sor (Kiegeszit16Csatornara) egy kezeletlen NullReferenceException-t dobna,
                ' és a felhasználó egy angol, értelmezhetetlen hibaüzenetet látna a szép
                ' magyar helyett.
                Throw New InvalidDataException("A fájl nem tartalmaz érvényes MidiBill beállításokat (üres vagy 'null' JSON).")
            End If
            Kiegeszit16Csatornara(eredmeny)

            Return eredmeny
        End Function

        Public Shared Function ImportMb3(fajlNev As String) As Beallitasok
            Dim sorok() As String = IO.File.ReadAllLines(fajlNev)

            If sorok.Length < 5 Then
                Throw New InvalidDataException("A fájl nem tűnik érvényes MidiBill .mb3 fájlnak (túl kevés sor).")
            End If

            Dim b As New Beallitasok()

            For c As Integer = 0 To 3
                Dim resz() As String = sorok(c).Split(","c)
                If resz.Length < 4 Then
                    Throw New InvalidDataException("A(z) " & (c + 1).ToString() & ". sor hibás formátumú.")
                End If

                Dim cs As New CsatornaBeallitas()
                cs.Hangszer = Integer.Parse(resz(0).Trim(), Globalization.CultureInfo.InvariantCulture)
                cs.Hangero = Integer.Parse(resz(1).Trim(), Globalization.CultureInfo.InvariantCulture)
                cs.Ter = Integer.Parse(resz(2).Trim(), Globalization.CultureInfo.InvariantCulture)
                cs.Vibrato = Integer.Parse(resz(3).Trim(), Globalization.CultureInfo.InvariantCulture)
                b.Csatornak.Add(cs)
            Next

            Dim altalanos() As String = sorok(4).Split(","c)
            If altalanos.Length < 5 Then
                Throw New InvalidDataException("Az 5. sor hibás formátumú.")
            End If

            b.AlapHangero = Integer.Parse(altalanos(0).Trim(), Globalization.CultureInfo.InvariantCulture)
            For c As Integer = 0 To 3
                b.Csatornak(c).Aktiv = (Integer.Parse(altalanos(c + 1).Trim(), Globalization.CultureInfo.InvariantCulture) <> 0)
            Next

            Kiegeszit16Csatornara(b)

            Return b
        End Function

        ''' <summary>
        ''' A Csatornak listát pontosan 16 elemre egészíti ki: a hiányzó elemeket
        ''' inaktív alapértékkel pótolja, a fölösleseket levágja, ÉS bármelyik
        ''' meglévő elemet, ami null (pl. kézzel szerkesztett, sérült JSON esetén),
        ''' szintén friss, alapértelmezett CsatornaBeallitas-ra cseréli.
        ''' </summary>
        Private Shared Sub Kiegeszit16Csatornara(b As Beallitasok)
            For i As Integer = 0 To b.Csatornak.Count - 1
                If b.Csatornak(i) Is Nothing Then
                    Dim uj As New CsatornaBeallitas()
                    uj.Aktiv = False
                    b.Csatornak(i) = uj
                End If
            Next

            While b.Csatornak.Count < 16
                Dim uj As New CsatornaBeallitas()
                uj.Aktiv = False
                b.Csatornak.Add(uj)
            End While
            While b.Csatornak.Count > 16
                b.Csatornak.RemoveAt(b.Csatornak.Count - 1)
            End While
        End Sub

        ''' <summary>Egy patch index alapján visszaadja a hangszer nevét a jelenleg aktív listából</summary>
        Private Shared Function HangszerNev(index As Integer) As String
            Return HangszerLista.Nev(index, "Ismeretlen hangszer")
        End Function

        Private Shared Function KommentekHozzaadasa(json As String) As String
            Dim hangszerMinta As New Regex("(""Hangszer"":\s*)(-?\d+)(,?)")
            Dim jsonKommentekkel As String = hangszerMinta.Replace(json,
        Function(m As Match) As String
            Dim idx As Integer = 0
            Integer.TryParse(m.Groups(2).Value, idx)
            Return m.Groups(1).Value & m.Groups(2).Value & m.Groups(3).Value & "  // " & HangszerNev(idx)
        End Function)

            Dim splitMinta As New Regex("(""SplitPont"":\s*)(-?\d+)(,?)")
            jsonKommentekkel = splitMinta.Replace(jsonKommentekkel,
        Function(m As Match) As String
            Dim idx As Integer = 0
            Integer.TryParse(m.Groups(2).Value, idx)
            Return m.Groups(1).Value & m.Groups(2).Value & m.Groups(3).Value & "  // " & Hangnevek.HangNev(idx)
        End Function)

            Dim transzMinta As New Regex("(""Transzponalas"":\s*)(-?\d+)(,?)")
            jsonKommentekkel = transzMinta.Replace(jsonKommentekkel,
        Function(m As Match) As String
            Dim ertek As Integer = 0
            Integer.TryParse(m.Groups(2).Value, ertek)
            Dim elojel As String = If(ertek > 0, "+", "")
            Return m.Groups(1).Value & m.Groups(2).Value & m.Groups(3).Value & "  // " & elojel & ertek & " félhang"
        End Function)

            Dim sorok As New List(Of String)(jsonKommentekkel.Split({Environment.NewLine}, StringSplitOptions.None))

            ' Minden csatorna-objektum nyitó "{" sora elé beszúrjuk, hányadik
            ' csatornáról van szó - tisztán tájékoztató komment, a program a
            ' betöltésnél továbbra is a lista POZÍCIÓJA alapján azonosítja a
            ' csatornákat, ez a szám nem kerül ténylegesen beolvasásra.
            Dim csatornaSorMinta As New Regex("^\s*\{\s*$")
            Dim csatornaSorszam As Integer = 0
            Dim csatornakBlokkban As Boolean = False

            For i As Integer = 0 To sorok.Count - 1
                If sorok(i).Contains("""Csatornak"": [") Then
                    csatornakBlokkban = True
                    Continue For
                End If

                If csatornakBlokkban Then
                    If sorok(i).TrimStart().StartsWith("]") Then
                        csatornakBlokkban = False
                    ElseIf csatornaSorMinta.IsMatch(sorok(i)) Then
                        csatornaSorszam += 1
                        Dim behuzas As String = sorok(i).Substring(0, sorok(i).IndexOf("{"c))
                        sorok(i) = behuzas & "{  // " & csatornaSorszam & ". csatorna"
                    End If
                End If
            Next

            Dim fejlec() As String = {
        "  // MidiBill3 beállítások - JSON formátumú preset fájl",
        "  // Készült: " & DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss", Globalization.CultureInfo.InvariantCulture)
    }

            If sorok.Count > 0 Then
                sorok.InsertRange(1, fejlec)
            End If

            Return String.Join(Environment.NewLine, sorok)
        End Function

    End Class

End Namespace
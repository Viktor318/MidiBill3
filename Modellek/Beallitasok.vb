Imports System.IO
Imports System.Text.Json
Imports System.Text.Json.Serialization

Namespace Modellek

    ''' <summary>
    ''' Egyetlen hangszerpanel (csatorna) beállításai.
    ''' A VB6-os Volume1/Tér1/Vibrato1/Hangszerek1 vezérlőtömbök egy-egy
    ''' indexének felel meg.
    ''' </summary>
    Public Class CsatornaBeallitas

        ''' <summary>Hangszer sorszáma (0-127, General MIDI patch index)</summary>
        Public Property Hangszer As Integer = 0

        ''' <summary>Hangerő (0-127)</summary>
        Public Property Hangero As Integer = 127

        ''' <summary>Pan / térbeli elhelyezés (-64..63, ahogy a VB6-os Tér1 csúszka is)</summary>
        Public Property Ter As Integer = 0

        ''' <summary>Vibrato / moduláció mértéke (0-127)</summary>
        Public Property Vibrato As Integer = 0

        ''' <summary>Aktív-e ez a csatorna (a J1 checkbox megfelelője)</summary>
        Public Property Aktiv As Boolean = True

    End Class

    ''' <summary>
    ''' A teljes alkalmazásállapot, amit elmentünk/betöltünk.
    ''' Ez váltja fel a régi pozíció-alapú .mb3 formátumot JSON-nal.
    ''' </summary>
    Public Class Beallitasok

        ''' <summary>A 4 hangszerpanel beállításai (mindig pontosan 4 elem)</summary>
        Public Property Csatornak As New List(Of CsatornaBeallitas)

        ''' <summary>Alap (globális) hangerő, 0-100 - ezzel szorzódik minden csatorna Hangero értéke</summary>
        Public Property AlapHangero As Integer = 70

        ''' <summary>A kiválasztott MIDI bemeneti eszköz neve (nem indexe! - lásd megjegyzés lent)</summary>
        Public Property MidiBemenetNev As String = ""

        ''' <summary>A kiválasztott MIDI kimeneti eszköz neve</summary>
        Public Property MidiKimenetNev As String = ""

        ''' <summary>Pedál-működés invertálva van-e</summary>
        Public Property PedalInvertalva As Boolean = False

        ''' <summary>
        ''' Új, alapértelmezett beállítások létrehozása (4 csatorna, gyári értékekkel).
        ''' Ezt hívjuk, ha nincs Kezdet.json, vagy ha betöltés hibázik.
        ''' </summary>
        Public Shared Function Alapertelmezett() As Beallitasok
            Dim b As New Beallitasok()
            For i As Integer = 0 To 3
                b.Csatornak.Add(New CsatornaBeallitas())
            Next
            Return b
        End Function

        ''' <summary>Beállítások mentése JSON fájlba</summary>
        Public Sub Mentes(fajlNev As String)
            Dim opciok As New JsonSerializerOptions With {
                .WriteIndented = True
            }
            Dim json As String = JsonSerializer.Serialize(Me, opciok)
            IO.File.WriteAllText(fajlNev, json)
        End Sub

        ''' <summary>Beállítások betöltése JSON fájlból</summary>
        Public Shared Function Betoltes(fajlNev As String) As Beallitasok
            Dim json As String = IO.File.ReadAllText(fajlNev)
            Dim eredmeny As Beallitasok = JsonSerializer.Deserialize(Of Beallitasok)(json)

            ' Biztonsági háló: ha a JSON hiányos vagy sérült volt, és a
            ' Csatornak lista nem pontosan 4 elemű, egészítsük ki/vágjuk
            ' vissza 4-re, hogy a program ne omoljon össze emiatt.
            While eredmeny.Csatornak.Count < 4
                eredmeny.Csatornak.Add(New CsatornaBeallitas())
            End While

            Return eredmeny
        End Function

        ''' <summary>
        ''' A régi, VB6-os pozíció-alapú .mb3 fájlformátum beolvasása.
        ''' Csak a 4 csatorna (hangszer/hangerő/pan/vibrato/aktív) és az
        ''' alaphangerő kerül át - a MIDI be-/kimeneti eszköz SZÁNDÉKOSAN
        ''' nem, mert a régi fájlban tárolt eszközindex a mai gépen már nem
        ''' feleltethető meg megbízhatóan a jelenlegi eszközlistának.
        ''' </summary>
        Public Shared Function ImportMb3(fajlNev As String) As Beallitasok
            Dim sorok() As String = IO.File.ReadAllLines(fajlNev)

            If sorok.Length < 5 Then
                Throw New InvalidDataException("A fájl nem tűnik érvényes MidiBill .mb3 fájlnak (túl kevés sor).")
            End If

            Dim b As New Beallitasok()

            ' Első 4 sor: csatornánként hangszer, hangerő, pan, vibrato
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

            ' 5. sor: alaphangerő + a 4 csatorna aktív/inaktív állapota
            Dim altalanos() As String = sorok(4).Split(","c)
            If altalanos.Length < 5 Then
                Throw New InvalidDataException("Az 5. sor hibás formátumú.")
            End If

            b.AlapHangero = Integer.Parse(altalanos(0).Trim(), Globalization.CultureInfo.InvariantCulture)
            For c As Integer = 0 To 3
                b.Csatornak(c).Aktiv = (Integer.Parse(altalanos(c + 1).Trim(), Globalization.CultureInfo.InvariantCulture) <> 0)
            Next

            ' A 6. (opcionális) sor a régi MIDI In/Out eszközindexeket tartalmazza -
            ' ezt szándékosan figyelmen kívül hagyjuk, lásd fent.

            Return b
        End Function

    End Class

End Namespace
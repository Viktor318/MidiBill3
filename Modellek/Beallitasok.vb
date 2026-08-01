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

    End Class

End Namespace
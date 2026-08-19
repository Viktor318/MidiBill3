Imports System.Linq
Namespace Modellek

    ''' <summary>
    ''' A jelenleg aktív hangszerlista (patch index -> hangszernév), a
    ''' cmbHangszer és a JSON-kommentelés/MIDI-üzenet-kijelzés is ezt
    ''' használja. Dictionary-alapú, hogy a jövőben RITKÁS, nem
    ''' folytonos indexelésű, egyedi (SoundFont-alapú) listákat is
    ''' problémamentesen kezelhessen, nem csak a teljes 128-elemű
    ''' General MIDI listát.
    ''' </summary>
    Public Module HangszerLista

        ' Beépített, "gyári" General MIDI lista - ez a végső biztonsági
        ' háló, ha a GM.lst fájl hiányzik vagy sérült: a program EKKOR IS
        ' el tud indulni, sosem omlik össze emiatt.
        Private ReadOnly alapNevek() As String = {
            "Acoustic Grand Piano", "Bright Acoustic Piano", "Electric Grand Piano", "Honkytonk Piano",
            "Rhodes Piano", "Chorus Piano", "Harpsichord", "Clavinet",
            "Celesta", "Glockenspiel", "Music Box", "Vibraphone",
            "Marimba", "Xylophone", "Tubular Bells", "Dulcimer",
            "Hammond Organ", "Percuss. Organ", "Rock Organ", "Church Organ",
            "Reed Organ", "Accordion", "Harmonica", "Tango Accordion",
            "Acoustic Guitar (nylon)", "Acoustic Guitar (steel)", "Electric Guitar (jazz)", "Electric Guitar (clean)",
            "Electric Guitar (muted)", "Overdriven Guitar", "Distortion Guitar", "Guitar Harmonics",
            "Acoustic Bass", "Electric Bass (finger)", "Electric Bass (pick)", "Fretless Bass",
            "Slap Bass 1", "Slap Bass 2", "Synth Bass 1", "Synth Bass 2",
            "Violin", "Viola", "Cello", "Contra Bass",
            "Tremolo Strings", "Pizzicato Strings", "Orchestral Harp", "Timpani",
            "String Ensemble 1", "String Ensemble 2", "Synth Strings 1", "Synth Strings 2",
            "Choir Aahs", "Voice Oohs", "Synth Voice", "Orchestra Hit",
            "Trumpet", "Trombone", "Tuba", "Muted Trumpet",
            "French Horn", "Brass Section", "Synth Brass 1", "Synth Brass 2",
            "Soprano Sax", "Alto Sax", "Tenor Sax", "Baritone Sax",
            "Oboe", "English Horn", "Bassoon", "Clarinet",
            "Piccolo", "Flute", "Recorder", "Pan Flute",
            "Bottle Blow", "Shaku", "Whistle", "Ocarina",
            "Lead 1 (square)", "Lead 2 (saw tooth)", "Lead 3 (calliope lead)", "Lead 4 (chiff lead)",
            "Lead 5 (charang)", "Lead 6 (voice)", "Lead 7 (fifths)", "Lead 8 (bass + lead)",
            "Pad 1 (new age)", "Pad 2 (warm)", "Pad 3 (poly synth)", "Pad 4 (choir)",
            "Pad 5 (bowed)", "Pad 6 (metallic)", "Pad 7 (halo)", "Pad 8 (sweep)",
            "FX 1 (rain)", "FX 2 (sound track)", "FX 3 (crystal)", "FX 4 (atmosphere)",
            "FX 5 (bright)", "FX 6 (goblins)", "FX 7 (echoes)", "FX 8 (sci-fi)",
            "Sitar", "Banjo", "Shamisen", "Koto",
            "Kalimba", "Bagpipe", "Fiddle", "Shanai",
            "Tinkle Bell", "Agogo", "Steel Drums", "Wood block",
            "Taiko Drum", "Melodic Tom", "Synth Drum", "Reverse Cymbal",
            "Guitar Fret Noise", "Breath Noise", "Seashore", "Bird Tweet",
            "Telephone Ring", "Helicopter", "Applause", "Gunshot"
        }

        ''' <summary>A jelenleg aktív hangszerlista: patch index -> hangszernév</summary>
        Public Property Nevek As Dictionary(Of Integer, String) = Alapertelmezett()

        ''' <summary>A beépített General MIDI lista Dictionary-vé alakítva (0-127, folytonos)</summary>
        Public Function Alapertelmezett() As Dictionary(Of Integer, String)
            Dim d As New Dictionary(Of Integer, String)
            For i As Integer = 0 To alapNevek.Length - 1
                d(i) = alapNevek(i)
            Next
            Return d
        End Function

        ''' <summary>
        ''' Egy patch index alapján biztonságosan visszaadja a hangszer nevét
        ''' a JELENLEG AKTÍV listából - ha az index nem szerepel benne (pl.
        ''' ritkás egyedi lista esetén), az ismeretlenSzoveg-et adja vissza.
        ''' </summary>
        Public Function Nev(index As Integer, Optional ismeretlenSzoveg As String = "?") As String
            Dim ertek As String = Nothing
            If Nevek.TryGetValue(index, ertek) Then
                Return ertek
            End If
            Return ismeretlenSzoveg
        End Function

        ''' <summary>
        ''' Betölti a hangszerlistát egy "index=Hangszernév" formátumú
        ''' szöveges fájlból (pl. Adatok/GM.lst). Sikertelen betöltés
        ''' (hiányzó/sérült/üres fájl) esetén a JELENLEG AKTÍV lista
        ''' VÁLTOZATLANUL megmarad (nem áll vissza a beépített GM-listára) -
        ''' a hívó felelőssége eldönteni, mit tegyen a sikertelenség esetén
        ''' (pl. figyelmeztető üzenet, a régi lista megtartása).
        ''' </summary>
        ''' <returns>True, ha sikerült a fájlból betölteni; False, ha nem (a Nevek ekkor érintetlen marad)</returns>
        Public Function Betolt(fajlUtvonal As String) As Boolean
            Try
                If Not IO.File.Exists(fajlUtvonal) Then Return False

                Dim uj As New Dictionary(Of Integer, String)
                For Each sor As String In IO.File.ReadAllLines(fajlUtvonal)
                    Dim tisztaSor As String = sor.Trim()
                    If tisztaSor.Length = 0 Then Continue For

                    Dim elvalaszto As Integer = tisztaSor.IndexOf("="c)
                    If elvalaszto <= 0 Then Continue For

                    Dim indexResz As String = tisztaSor.Substring(0, elvalaszto).Trim()
                    Dim nevResz As String = tisztaSor.Substring(elvalaszto + 1).Trim()

                    Dim index As Integer
                    If Integer.TryParse(indexResz, index) AndAlso nevResz.Length > 0 Then
                        uj(index) = nevResz
                    End If
                Next

                If uj.Count = 0 Then Return False

                Nevek = uj
                Return True
            Catch ex As Exception
                Return False
            End Try
        End Function

        ''' <summary>
        ''' Egy hangszerlistát (index=Név párok) elment egy .lst fájlba -
        ''' ugyanabban a formátumban, amit a Betolt() beolvas. A
        ''' SoundFont-importálás használja, hogy a beolvasott presetekből
        ''' ténylegesen .lst fájlt hozzon létre.
        ''' </summary>
        Public Sub Ment(fajlUtvonal As String, nevek As Dictionary(Of Integer, String))
            Dim sorok As New List(Of String)
            For Each kulcs As Integer In nevek.Keys.OrderBy(Function(k) k)
                sorok.Add(kulcs & "=" & nevek(kulcs))
            Next
            IO.File.WriteAllLines(fajlUtvonal, sorok)
        End Sub

    End Module

End Namespace
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
        ''' A 10. (dob) csatornán megjelenő, KÜLÖN hangszerlista - független
        ''' az általános Nevek listától, mivel dob-csatornán a Program Change
        ''' a GM2 szabvány szerint dobkészlet-VARIÁCIÓT választ, nem
        ''' hagyományos hangszert.
        ''' </summary>
        Public Property DobNevek As Dictionary(Of Integer, String) = AlapertelmezettDobKeszlet()

        ''' <summary>Beépített, "gyári" GM2-stílusú dobkészlet-lista,
        ''' ha a GM.drm hiányzik/sérült</summary>
        Public Function AlapertelmezettDobKeszlet() As Dictionary(Of Integer, String)
            Return New Dictionary(Of Integer, String) From {
                {0, "Standard Kit"},
                {8, "Room Kit"},
                {16, "Power Kit"},
                {24, "Electronic Kit"},
                {25, "Analog Kit"},
                {32, "Jazz Kit"},
                {40, "Brush Kit"},
                {48, "Orchestra Kit"},
                {56, "SFX Kit"}
            }
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
        ''' Egy dobkészlet-patch index alapján biztonságosan visszaadja a
        ''' nevét a JELENLEG AKTÍV dobkészlet-listából (DobNevek) - a Nev()
        ''' párja, csak a 10. (dob) csatornához.
        ''' </summary>
        Public Function DobNev(index As Integer, Optional ismeretlenSzoveg As String = "?") As String
            Dim ertek As String = Nothing
            If DobNevek.TryGetValue(index, ertek) Then
                Return ertek
            End If
            Return ismeretlenSzoveg
        End Function

        ''' <summary>
        ''' Betölti a hangszerlistát egy "index=Hangszernév" formátumú
        ''' szöveges fájlból (pl. Adatok/GM.lst). Három index-formátumot
        ''' ismer fel - "program", "lsb.program" és "msb.lsb.program" -,
        ''' a részleteket lásd az IndexReszFeldolgozasa-nál. Sikertelen betöltés
        ''' (hiányzó/sérült/üres fájl) esetén a JELENLEG AKTÍV lista
        ''' VÁLTOZATLANUL megmarad (nem áll vissza a beépített GM-listára) -
        ''' a hívó felelőssége eldönteni, mit tegyen a sikertelenség esetén
        ''' (pl. figyelmeztető üzenet, a régi lista megtartása).
        ''' </summary>
        ''' <returns>True, ha sikerült a fájlból betölteni; False, ha nem (a Nevek ekkor érintetlen marad)</returns>
        Public Function Betolt(fajlUtvonal As String) As Boolean
            Dim eredmeny As Dictionary(Of Integer, String) = FajlbolBetoltesBelso(fajlUtvonal)
            If eredmeny Is Nothing Then Return False
            Nevek = eredmeny
            Return True
        End Function

        ''' <summary>
        ''' Betölti a 10. csatorna dedikált dobkészlet-listáját egy .lst
        ''' fájlból - ugyanazt a fájlformátumot használja, mint a Betolt(),
        ''' csak a DobNevek property-be tölt, nem a Nevek-be.
        ''' </summary>
        Public Function BetoltDobKeszletet(fajlUtvonal As String) As Boolean
            Dim eredmeny As Dictionary(Of Integer, String) = FajlbolBetoltesBelso(fajlUtvonal)
            If eredmeny Is Nothing Then Return False
            DobNevek = eredmeny
            Return True
        End Function

        ''' <summary>A tényleges fájlolvasó/elemző logika - a Betolt() és a BetoltDobKeszletet() közös alapja</summary>
        Private Function FajlbolBetoltesBelso(fajlUtvonal As String) As Dictionary(Of Integer, String)
            Try
                If Not IO.File.Exists(fajlUtvonal) Then Return Nothing

                Dim uj As New Dictionary(Of Integer, String)
                For Each sor As String In IO.File.ReadAllLines(fajlUtvonal)
                    Dim tisztaSor As String = sor.Trim()
                    If tisztaSor.Length = 0 Then Continue For

                    ' MEGJEGYZÉS-SOR: a "#"-tel kezdődő sorokat átugorjuk.
                    ' Eddig csak az mentette meg őket, hogy nem volt bennük
                    ' "=" - egy olyan megjegyzés viszont, ami történetesen
                    ' tartalmaz egyenlőségjelet (pl. "# MSB = 121"), bejegyzésnek
                    ' látszott. Így a listafájlok tetejére nyugodtan írható
                    ' magyarázat.
                    If tisztaSor.StartsWith("#", StringComparison.Ordinal) Then Continue For

                    Dim elvalaszto As Integer = tisztaSor.IndexOf("="c)
                    If elvalaszto <= 0 Then Continue For

                    Dim indexResz As String = tisztaSor.Substring(0, elvalaszto).Trim()
                    Dim nevResz As String = tisztaSor.Substring(elvalaszto + 1).Trim()

                    Dim kombinaltKulcs As Integer
                    If Not IndexReszFeldolgozasa(indexResz, kombinaltKulcs) Then Continue For

                    If nevResz.Length > 0 Then
                        uj(kombinaltKulcs) = nevResz
                    End If
                Next

                If uj.Count = 0 Then Return Nothing
                Return uj
            Catch ex As Exception
                Return Nothing
            End Try
        End Function

        ''' <summary>
        ''' Egy .lst sor bal oldalát (az "=" előtti részt) alakítja kombinált
        ''' kulccsá. HÁROM formátumot ismer fel:
        '''
        ''' - "program" (pl. "40") -> nincs bank; visszamenőleg kompatibilis
        '''   a régi, sima GM .lst fájlokkal;
        ''' - "lsb.program" (pl. "8.0") -> a szám a Bank Select LSB.
        '''   SZÁNDÉKOSAN maradt LSB: a régi fájlok jelentése így NEM változik
        '''   meg csendben. (A SoundFontból importált listákban ez volt a
        '''   bankszám - lásd a 13. pont tervét arról, miért kell azokat
        '''   egyszer újragenerálni.)
        ''' - "msb.lsb.program" (pl. "121.1.48") -> mindkét bank-bájt, ez az
        '''   EGYÉRTELMŰ alak. A Roland GS variációk (MSB), a General MIDI 2
        '''   hangok (MSB = 121/120) és a Yamaha XG variációk (LSB) csak így
        '''   írhatók le pontosan.
        '''
        ''' Minden szám 0-127 lehet: ami ezen kívül esik, azt ÉRVÉNYTELEN
        ''' sornak tekintjük és kihagyjuk. Korábban nem volt tartomány-
        ''' ellenőrzés, és egy hibás sor (pl. "200.24") érvénytelen MIDI
        ''' adatbájtot eredményezett volna a kiküldésnél.
        ''' </summary>
        Private Function IndexReszFeldolgozasa(indexResz As String, ByRef kombinaltKulcs As Integer) As Boolean
            Dim reszek As String() = indexResz.Split("."c)
            If reszek.Length < 1 OrElse reszek.Length > 3 Then Return False

            ' Jobbról balra töltjük fel: az UTOLSÓ tag mindig a programszám,
            ' az előtte lévő az LSB, az az előtti az MSB.
            Dim ertekek As Integer() = {0, 0, 0}   ' {MSB, LSB, program}
            For i As Integer = 0 To reszek.Length - 1
                Dim ertek As Integer
                If Not Integer.TryParse(reszek(reszek.Length - 1 - i).Trim(), ertek) Then Return False
                If ertek < 0 OrElse ertek > Hangszerkulcs.MAX_ADATBAJT Then Return False
                ertekek(2 - i) = ertek
            Next

            kombinaltKulcs = Hangszerkulcs.Kulcs(ertekek(0), ertekek(1), ertekek(2))
            Return True
        End Function

        ''' <summary>
        ''' Egy hangszerlistát (index=Név párok) elment egy .lst fájlba -
        ''' ugyanabban a formátumban, amit a Betolt() beolvas. Mindig a
        ''' LEGRÖVIDEBB alakot írja, ami hibátlanul visszaolvasható (lásd
        ''' IndexSzoveg) - így egy sima GM lista ugyanolyan olvasható marad,
        ''' mint eddig. A SoundFont-importálás használja, hogy a beolvasott
        ''' presetekből ténylegesen .lst fájlt hozzon létre.
        ''' </summary>
        Public Sub Ment(fajlUtvonal As String, nevek As Dictionary(Of Integer, String))
            Dim sorok As New List(Of String)
            For Each kulcs As Integer In nevek.Keys.OrderBy(Function(k) k)
                sorok.Add(IndexSzoveg(kulcs) & "=" & nevek(kulcs))
            Next
            IO.File.WriteAllLines(fajlUtvonal, sorok)
        End Sub

        ''' <summary>
        ''' Egy kulcs .lst-beli, szöveges alakja - a lehető legrövidebben:
        ''' "program", ha nincs bank; "lsb.program", ha csak LSB van;
        ''' "msb.lsb.program", ha MSB is. Az IndexReszFeldolgozasa PÁRJA:
        ''' amit ez ír, azt annak vissza kell tudnia olvasni.
        ''' </summary>
        Private Function IndexSzoveg(kulcs As Integer) As String
            Dim msb As Integer = Hangszerkulcs.BankMsb(kulcs)
            Dim lsb As Integer = Hangszerkulcs.BankLsb(kulcs)
            Dim program As Integer = Hangszerkulcs.ProgramSzam(kulcs)

            If msb <> 0 Then Return msb & "." & lsb & "." & program
            If lsb <> 0 Then Return lsb & "." & program
            Return program.ToString()
        End Function

    End Module

End Namespace
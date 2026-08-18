Namespace Modellek

    ''' <summary>
    ''' A General MIDI szabvány 128 hangszerneve, sorrendben (a lista indexe
    ''' = a MIDI patch szám, 0-127). Közös hely, hogy a HangszerPanel (UI)
    ''' és a Beallitasok (JSON-mentés, névkommentek generálása) ugyanabból
    ''' a listából dolgozzon, ne legyen duplikálva.
    ''' </summary>
    Public Module HangszerLista

        Public ReadOnly Nevek() As String = {
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

        ''' <summary>Egy patch index alapján visszaadja a hangszer nevét, tartományellenőrzéssel - "?" ha érvénytelen</summary>
        Public Function Nev(index As Integer) As String
            If index >= 0 AndAlso index < Nevek.Length Then
                Return Nevek(index)
            End If
            Return "?"
        End Function

    End Module

End Namespace
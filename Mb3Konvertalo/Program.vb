Imports System.IO
Imports Modellek

Module Program

    Sub Main(args As String())
        Console.WriteLine("=== MidiBill3 .mb3 -> .json preset konvertáló ===")
        Console.WriteLine("Régi, VB6-os .mb3 fájlokat alakít át a jelenlegi JSON formátumra." & vbCrLf &
                           "A MIDI eszközválasztást a régi fájl nem tartalmazza megbízhatóan," & vbCrLf &
                           "ezért ezt egyszer, a teljes köteghez kérjük be." & vbCrLf)

        Dim mappa As String
        If args.Length >= 1 Then
            mappa = args(0)
        Else
            Console.Write("Add meg a .mb3 fájlokat tartalmazó mappa elérési útját: ")
            mappa = Console.ReadLine()
        End If

        If Not Directory.Exists(mappa) Then
            Console.WriteLine("A megadott mappa nem létezik: " & mappa)
            VarakozasEsKilepes()
            Return
        End If

        Console.WriteLine()
        Console.WriteLine("Add meg a MIDI eszközök nevét PONTOSAN úgy, ahogy a MidiBill3" & vbCrLf &
                           "programban a MIDI In / MIDI Out legördülő listában szerepelnek" & vbCrLf &
                           "(üresen hagyva később, a programban kézzel is kiválaszthatod).")
        Console.Write("MIDI bemenet neve: ")
        Dim midiBe As String = Console.ReadLine()
        Console.Write("MIDI kimenet neve: ")
        Dim midiKi As String = Console.ReadLine()

        ' A konvertált fájlok egy ALMAPPÁBA kerülnek, az eredetiek
        ' változatlanul megmaradnak.
        Dim kimenoMappa As String = Path.Combine(mappa, "Konvertalva")
        Directory.CreateDirectory(kimenoMappa)

        Dim fajlok As String() = Directory.GetFiles(mappa, "*.mb3")
        Dim sikeres As Integer = 0
        Dim hibas As Integer = 0

        Console.WriteLine()
        Console.WriteLine(fajlok.Length & " .mb3 fájl található a mappában." & vbCrLf)

        For Each fajl As String In fajlok
            Dim nev As String = Path.GetFileName(fajl)
            Try
                Dim b As Beallitasok = Beallitasok.ImportMb3(fajl)
                b.MidiBemenetNev = midiBe
                b.MidiKimenetNev = midiKi

                Dim ujNev As String = Path.GetFileNameWithoutExtension(nev) & ".json"
                Dim celUtvonal As String = Path.Combine(kimenoMappa, ujNev)
                b.Mentes(celUtvonal)

                Console.WriteLine("OK:   " & nev & " -> " & ujNev)
                sikeres += 1
            Catch ex As Exception
                Console.WriteLine("HIBA: " & nev & " - " & ex.Message)
                hibas += 1
            End Try
        Next

        Console.WriteLine()
        Console.WriteLine("Kész. Sikeres: " & sikeres & ", hibás: " & hibas)
        Console.WriteLine("A konvertált fájlok itt találhatók: " & kimenoMappa)

        VarakozasEsKilepes()
    End Sub

    Private Sub VarakozasEsKilepes()
        Console.WriteLine(vbCrLf & "Nyomj Entert a kilépéshez...")
        Console.ReadLine()
    End Sub

End Module
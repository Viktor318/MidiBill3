Imports System.IO
Imports Modellek

Module Program

    Sub Main(args As String())
        Console.WriteLine("=== MidiBill3 preset konvertáló ===")
        Console.WriteLine("Régi formátumú .json fájlokat frissít a jelenlegi szintaktikára" & vbCrLf &
                           "(explicit Alul/Felul/SplitPont mezők + hangnév-megjegyzések)." & vbCrLf)

        Dim mappa As String
        If args.Length >= 1 Then
            mappa = args(0)
        Else
            Console.Write("Add meg a régi preset fájlokat tartalmazó mappa elérési útját: ")
            mappa = Console.ReadLine()
        End If

        If Not Directory.Exists(mappa) Then
            Console.WriteLine("A megadott mappa nem létezik: " & mappa)
            VarakozasEsKilepes()
            Return
        End If

        ' A konvertált fájlok egy ALMAPPÁBA kerülnek, az eredetiek
        ' változatlanul megmaradnak - biztonsági okból nem írjuk felül
        ' helyben őket.
        Dim kimenoMappa As String = Path.Combine(mappa, "Konvertalva")
        Directory.CreateDirectory(kimenoMappa)

        Dim fajlok As String() = Directory.GetFiles(mappa, "*.json")
        Dim sikeres As Integer = 0
        Dim hibas As Integer = 0

        Console.WriteLine(fajlok.Length & " .json fájl található a mappában." & vbCrLf)

        For Each fajl As String In fajlok
            Dim nev As String = Path.GetFileName(fajl)
            Try
                Dim b As Beallitasok = Beallitasok.Betoltes(fajl)
                b.Verzio = AppInfo.AppVerzio()
                Dim celUtvonal As String = Path.Combine(kimenoMappa, nev)
                b.Mentes(celUtvonal)
                Console.WriteLine("OK:   " & nev)
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
Namespace Modellek

    ''' <summary>
    ''' SoundFont (.sf2) fájlok "phdr" (Preset Headers) blokkjának beolvasása,
    ''' hogy a benne definiált hangszerek egy .lst formátumú hangszerlistává
    ''' alakíthatók legyenek. A tényleges hangminta-adatokat (sdta blokk) NEM
    ''' olvassuk be - csak a metaadatokat, ez gyors feldolgozást tesz
    ''' lehetővé nagy fájloknál is.
    ''' Jelenleg csak a Bank=0 (General MIDI-kompatibilis) preseteket vesszük
    ''' figyelembe - a magasabb bankokban (pl. dobkészletek) lévő presetek
    ''' egyelőre kimaradnak (GS-támogatás híján a program úgysem tudná ezeket
    ''' helyesen kiválasztani, mert nem küld Bank Select üzenetet), csak a
    ''' számukat jelentjük vissza.
    ''' </summary>
    Public Module SoundFontOlvaso

        ''' <summary>
        ''' Beolvassa egy .sf2 fájl preset-listáját. A visszaadott Nevek
        ''' Dictionary közvetlenül felhasználható .lst fájl generálásához
        ''' (a kulcs már a MIDI Program Change szám, 0-127).
        ''' </summary>
        ''' <exception cref="IO.InvalidDataException">Ha a fájl nem érvényes RIFF/SoundFont formátumú, vagy nincs benne phdr blokk</exception>
        Public Function Beolvas(fajlUtvonal As String) As (Nevek As Dictionary(Of Integer, String), KihagyottSzam As Integer)
            Using fs As New IO.FileStream(fajlUtvonal, IO.FileMode.Open, IO.FileAccess.Read)
                Using br As New IO.BinaryReader(fs, Text.Encoding.ASCII)

                    Dim riffId As String = New String(br.ReadChars(4))
                    If riffId <> "RIFF" Then
                        Throw New IO.InvalidDataException("A fájl nem RIFF-formátumú (nem SoundFont fájl).")
                    End If

                    Dim riffMeret As UInteger = br.ReadUInt32()
                    Dim formaTipus As String = New String(br.ReadChars(4))
                    If formaTipus <> "sfbk" Then
                        Throw New IO.InvalidDataException("A fájl nem SoundFont (sfbk) formátumú.")
                    End If

                    Dim vegPozicio As Long = 8 + riffMeret
                    Dim phdrAdat As Byte() = Nothing

                    ' Végigmegyünk a felső szintű RIFF chunk-okon (LIST INFO, LIST sdta,
                    ' LIST pdta stb.) - csak a "pdta" listát nyitjuk meg belülről, a
                    ' többit (főleg a hatalmas hangminta-adatot tartalmazó "sdta"-t)
                    ' egyszerűen átugorjuk a mérete alapján, be sem olvassuk.
                    While fs.Position < vegPozicio AndAlso fs.Position < fs.Length - 8
                        Dim ckId As String = New String(br.ReadChars(4))
                        Dim ckMeret As UInteger = br.ReadUInt32()
                        Dim ckVege As Long = fs.Position + ckMeret + (ckMeret Mod 2UI)

                        If ckId = "LIST" Then
                            Dim listaTipus As String = New String(br.ReadChars(4))
                            If listaTipus = "pdta" Then
                                While fs.Position < ckVege
                                    Dim subId As String = New String(br.ReadChars(4))
                                    Dim subMeret As UInteger = br.ReadUInt32()
                                    Dim subVege As Long = fs.Position + subMeret + (subMeret Mod 2UI)

                                    If subId = "phdr" Then
                                        phdrAdat = br.ReadBytes(CInt(subMeret))
                                        If subMeret Mod 2UI = 1UI Then br.ReadByte()
                                    Else
                                        fs.Position = subVege
                                    End If
                                End While
                            Else
                                fs.Position = ckVege
                            End If
                        Else
                            fs.Position = ckVege
                        End If
                    End While

                    If phdrAdat Is Nothing Then
                        Throw New IO.InvalidDataException("Nem található ""phdr"" (preset header) blokk a fájlban - lehet, hogy sérült vagy nem érvényes SoundFont fájl.")
                    End If

                    Return FeldolgozPhdr(phdrAdat)

                End Using
            End Using
        End Function

        ''' <summary>A phdr blokk nyers bájtjait alakítja Dictionary-vé (csak Bank=0 presetek), + a kihagyottak száma</summary>
        Private Function FeldolgozPhdr(phdrAdat As Byte()) As (Nevek As Dictionary(Of Integer, String), KihagyottSzam As Integer)
            Const REKORD_MERET As Integer = 38
            Dim rekordSzam As Integer = phdrAdat.Length \ REKORD_MERET

            Dim eredmeny As New Dictionary(Of Integer, String)
            Dim kihagyottSzam As Integer = 0

            ' Az utolsó rekord mindig a lezáró "EOP" (End Of Preset) bejegyzés - ezt kihagyjuk
            For i As Integer = 0 To rekordSzam - 2
                Dim offset As Integer = i * REKORD_MERET

                Dim nyersNev As String = Text.Encoding.ASCII.GetString(phdrAdat, offset, 20)
                Dim nullPoz As Integer = nyersNev.IndexOf(Chr(0))
                Dim nev As String = If(nullPoz >= 0, nyersNev.Substring(0, nullPoz), nyersNev).Trim()

                Dim program As Integer = BitConverter.ToUInt16(phdrAdat, offset + 20)
                Dim bank As Integer = BitConverter.ToUInt16(phdrAdat, offset + 22)

                If bank = 0 AndAlso program >= 0 AndAlso program <= 127 AndAlso nev.Length > 0 Then
                    eredmeny(program) = nev
                Else
                    kihagyottSzam += 1
                End If
            Next

            Return (eredmeny, kihagyottSzam)
        End Function

    End Module

End Namespace
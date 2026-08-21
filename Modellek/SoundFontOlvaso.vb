Namespace Modellek

    ''' <summary>
    ''' SoundFont (.sf2) fájlok "phdr" (Preset Headers) blokkjának beolvasása,
    ''' hogy a benne definiált hangszerek egy .lst, a dobkészlet-presetek
    ''' (Bank 120 - GM2 szabvány, vagy Bank 128 - SoundFont-szerkesztői
    ''' konvenció) pedig egy .drm formátumú fájllá alakíthatók legyenek. A
    ''' tényleges hangminta-adatokat (sdta blokk) NEM olvassuk be - csak a
    ''' metaadatokat, ez gyors feldolgozást tesz lehetővé nagy fájloknál is.
    ''' A hangszereknél a kulcs bank*128+program formában kombinálva (bank=0
    ''' esetén ez megegyezik a puszta patch-számmal, visszamenőleg
    ''' kompatibilis marad a GM-only listákkal); a dobkészleteknél a kulcs
    ''' közvetlenül a Program szám (0-127).
    ''' </summary>
    Public Module SoundFontOlvaso

        ''' <summary>
        ''' Beolvassa egy .sf2 fájl preset-listáját. A visszaadott Nevek
        ''' Dictionary közvetlenül felhasználható .lst fájl generálásához
        ''' (a kulcs bank*128+program formában kombinálva, 0-16383). A
        ''' Dobok Dictionary az ISMERT DOBKÉSZLET-BANKOKBÓL (lásd
        ''' ismertDobBankok: 120 - GM2 szabvány, 128 - SoundFont-szerkesztői
        ''' konvenció) származó preseteket tartalmazza KÜLÖN, a .drm fájlhoz
        ''' - a Bank 128 sosem küldhető ki szabványos Bank Select-tel (túllépi
        ''' a MIDI 7-bites adatbájt-korlátot), a Bank 120 viszont igen (ez a
        ''' GM2 szabvány szerinti, ténylegesen elküldhető dobkészlet-bank);
        ''' mindkettő közvetlenül felhasználható a 10. csatorna dedikált
        ''' dobkészlet-listájában.
        ''' </summary>
        ''' <exception cref="IO.InvalidDataException">Ha a fájl nem érvényes RIFF/SoundFont formátumú, vagy nincs benne phdr blokk</exception>
        Public Function Beolvas(fajlUtvonal As String) As (Nevek As Dictionary(Of Integer, String), Dobok As Dictionary(Of Integer, String), Kihagyottak As List(Of String))
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

        ' Ismert "dobkészlet" bank-számok - ezekben a SoundFont ténylegesen
        ' dobkészlet-preseteket tárol, nem hagyományos hangszereket, ezért a
        ' .drm fájlba (Dobok) kerülnek, nem a .lst-be (Nevek):
        ' - 120: GM2 szabvány szerinti dobkészlet-bank (ténylegesen
        '   elküldhető Bank Select-tel is)
        ' - 128: SoundFont-szerkesztők (pl. JJazzLab) belső, csak a fájlon
        '   belüli szervezésre használt konvenciója (sosem küldhető ki
        '   szabványos Bank Select-tel, mivel túllépi a 7-bites korlátot)
        Private ReadOnly ismertDobBankok() As Integer = {120, 128}

        ''' <summary>A phdr blokk nyers bájtjait alakítja két Dictionary-vé
        ''' (hangszerek + ismert dobkészlet-bankokból származó dobkészletek), + az érvénytelen rekordok listája</summary>
        Private Function FeldolgozPhdr(phdrAdat As Byte()) As (Nevek As Dictionary(Of Integer, String), Dobok As Dictionary(Of Integer, String), Kihagyottak As List(Of String))
            Const REKORD_MERET As Integer = 38
            Dim rekordSzam As Integer = phdrAdat.Length \ REKORD_MERET

            Dim eredmeny As New Dictionary(Of Integer, String)
            Dim dobEredmeny As New Dictionary(Of Integer, String)
            Dim kihagyottak As New List(Of String)

            ' Az utolsó rekord mindig a lezáró "EOP" (End Of Preset) bejegyzés - ezt kihagyjuk
            For i As Integer = 0 To rekordSzam - 2
                Dim offset As Integer = i * REKORD_MERET

                Dim nyersNev As String = Text.Encoding.ASCII.GetString(phdrAdat, offset, 20)
                Dim nullPoz As Integer = nyersNev.IndexOf(Chr(0))
                Dim nev As String = If(nullPoz >= 0, nyersNev.Substring(0, nullPoz), nyersNev).Trim()

                Dim program As Integer = BitConverter.ToUInt16(phdrAdat, offset + 20)
                Dim bank As Integer = BitConverter.ToUInt16(phdrAdat, offset + 22)

                If program >= 0 AndAlso program <= 127 AndAlso Array.IndexOf(ismertDobBankok, bank) >= 0 AndAlso nev.Length > 0 Then
                    ' Ismert dobkészlet-bank (120 vagy 128) - a .drm fájlhoz
                    ' gyűjtjük külön, a Program szám itt közvetlenül a
                    ' dobkészlet-lista kulcsa lesz.
                    dobEredmeny(program) = nev
                ElseIf program >= 0 AndAlso program <= 127 AndAlso bank >= 0 AndAlso bank <= 127 AndAlso nev.Length > 0 Then
                    Dim kombinaltKulcs As Integer = bank * 128 + program
                    eredmeny(kombinaltKulcs) = nev
                Else
                    Dim ok As String
                    If nev.Length = 0 Then
                        ok = "üres név"
                    ElseIf program > 127 Then
                        ok = "Program túl nagy (" & program & ")"
                    ElseIf bank > 128 Then
                        ok = "Bank túl nagy (" & bank & ")"
                    Else
                        ok = "ismeretlen ok"
                    End If
                    kihagyottak.Add("Bank " & bank & ", Program " & program & ": """ & nev & """ (" & ok & ")")
                End If
            Next

            Return (eredmeny, dobEredmeny, kihagyottak)
        End Function

    End Module

End Namespace
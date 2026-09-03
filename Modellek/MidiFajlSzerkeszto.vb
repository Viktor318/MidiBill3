Namespace Modellek

    ''' <summary>
    ''' Meglévő Standard MIDI File (.mid) fájlokba szúrja be az AKTÍV
    ''' csatornák teljes MIDI-állapotát (hangszín, hangerő, tér, vibrato,
    ''' chorus, reverb) - ugyanazokkal az üzenetekkel, mint amit a
    ''' "MIDI beállító üzenetek küldése" funkció élőben kiküld (lásd
    ''' MainForm.AktivCsatornakAllapotanakKuldese()). Célja, hogy egy külső
    ''' lejátszó/DAW is helyesen szólaltassa meg a fájlt a program aktuális
    ''' beállításaival, akkor is, ha a fájl eredetileg máshonnan származik.
    '''
    ''' VÉDELEM: egy AKTÍV csatornára is csak akkor kerül beállítás, ha a
    ''' fájl valamelyik MEGLÉVŐ trackje ténylegesen tartalmaz is arra a
    ''' csatornára vonatkozó adatot (Note On/Off, Control Change stb.) -
    ''' ha egy csatorna a programban aktív, de a fájlban egyáltalán nem
    ''' szerepel, a program szándékosan NEM ír bele semmit (nincs értelme
    ''' egy olyan csatornát "beállítani", amit a fájl amúgy sem használ).
    '''
    ''' JELENLEG CSAK format 1 (több track, közös időzítés) fájlokat
    ''' támogat - format 0 (egyetlen közös track) egy KÉSŐBBI fejlesztési
    ''' pont (lásd MidiBill3_VBNET_aktualis_allapot.md, "Következő lépések"
    ''' 12. pont).
    ''' </summary>
    Public Module MidiFajlSzerkeszto

        Private Const MAIN_VOLUME As Integer = 7
        Private Const PAN As Integer = 10
        Private Const MOD_WHEEL As Integer = 1
        Private Const REVERB As Integer = 91
        Private Const CHORUS As Integer = 93
        Private Const BANK_SELECT_MSB As Integer = 0
        Private Const BANK_SELECT_LSB As Integer = 32

        ''' <summary>
        ''' Beolvassa a bemenetiFajl-t, az AKTÍV ÉS a fájlban ténylegesen
        ''' használt csatornák állapotát egy ÚJ, önálló trackként beszúrja
        ''' a 0. track (conductor track) UTÁN, és az eredményt a
        ''' kimenetiFajl-ba írja - a bemeneti fájlt NEM írja felül. A
        ''' meglévő trackek bájtjaihoz nem nyúl hozzá: format 1-ben minden
        ''' track a közös tick-időzítést használja és párhuzamosan
        ''' játszódik le, a csatornát maga az üzenet státuszbájtja
        ''' határozza meg, nem a track - ezért egy teljesen új track
        ''' beszúrása biztonságos (nincs running status újraszámolás, nincs
        ''' delta-time lánc bontása egy meglévő trackben, és nem kell
        ''' kitalálni, melyik track melyik csatornához tartozik). Ha egyik
        ''' aktív csatornához sem talál adatot a fájlban, NEM szúr be új
        ''' tracket - a kimenet ilyenkor a bemenet változatlan másolata.
        ''' </summary>
        ''' <param name="bemenetiFajl">A forrás .mid fájl teljes útvonala.</param>
        ''' <param name="kimenetiFajl">A módosított másolat célútvonala.</param>
        ''' <param name="csatornak">A 16 csatorna aktuális állapota (index = csatorna, 0-tól).</param>
        ''' <param name="alapHangero">Globális hangerő-szorzó, százalékban (pl. MainForm trkAlapHangero.Value).</param>
        ''' <param name="reverbSzazalek">Globális Reverb, százalékban (pl. MainForm trkReverb.Value).</param>
        ''' <returns>Azon csatornák száma, amelyekhez ténylegesen beszúrásra került a beállítás.</returns>
        ''' <exception cref="IO.InvalidDataException">A fájl nem érvényes Standard MIDI File.</exception>
        ''' <exception cref="NotSupportedException">A fájl format 0 - lásd az osztály-szintű megjegyzést.</exception>
        Public Function BeallitasokBeszurasa(bemenetiFajl As String, kimenetiFajl As String,
                                              csatornak As IList(Of CsatornaBeallitas),
                                              alapHangero As Integer, reverbSzazalek As Integer) As Integer

            Dim adat As Byte() = IO.File.ReadAllBytes(bemenetiFajl)

            If adat.Length < 14 OrElse Not ChunkAzonositoEgyezikE(adat, 0, "MThd") Then
                Throw New IO.InvalidDataException("A fájl nem érvényes Standard MIDI File (hiányzik az 'MThd' fejléc).")
            End If

            Dim headerHossz As Integer = OlvasBeInt32(adat, 4)
            Dim format As Integer = OlvasBeInt16(adat, 8)
            Dim trackSzam As Integer = OlvasBeInt16(adat, 10)

            If format <> 1 Then
                Throw New NotSupportedException(
                    "Csak format 1 MIDI fájlokat lehet jelenleg módosítani (ez a fájl format " & format &
                    "). A format 0 támogatása egy későbbi fejlesztés.")
            End If
            If trackSzam < 1 OrElse adat.Length < 8 + headerHossz + 8 Then
                Throw New IO.InvalidDataException("A fájl nem tartalmaz egyetlen érvényes tracket sem.")
            End If

            Dim elsoTrackKezdete As Integer = 8 + headerHossz
            If Not ChunkAzonositoEgyezikE(adat, elsoTrackKezdete, "MTrk") Then
                Throw New IO.InvalidDataException("A fájl 0. tracke nem érvényes ('MTrk' chunk hiányzik).")
            End If
            Dim elsoTrackHossz As Integer = OlvasBeInt32(adat, elsoTrackKezdete + 4)
            Dim elsoTrackVege As Integer = elsoTrackKezdete + 8 + elsoTrackHossz
            If elsoTrackVege > adat.Length Then
                Throw New IO.InvalidDataException("A fájl 0. tracke csonka (a jelzett hossz túlnyúlik a fájl végén).")
            End If

            Dim hasznaltCsatornak As Boolean() = HasznaltCsatornakFelderitese(adat, elsoTrackKezdete, trackSzam)

            Dim irtCsatornaSzam As Integer
            Dim ujTrackAdat As Byte() = UjTrackAdatEpitese(csatornak, alapHangero, reverbSzazalek, hasznaltCsatornak, irtCsatornaSzam)

            Using kimenet As New IO.FileStream(kimenetiFajl, IO.FileMode.Create, IO.FileAccess.Write)
                If irtCsatornaSzam = 0 Then
                    ' Egyik aktív csatornához sem volt adat a fájlban - a fájl
                    ' változatlan másolata készül, felesleges üres tracket nem szúrunk be.
                    kimenet.Write(adat, 0, adat.Length)
                Else
                    Dim ujTrackChunk As Byte() = ChunkEpitese("MTrk", ujTrackAdat)

                    ' Fejléc: "MThd" + headerHossz (változatlan) + format (változatlan) +
                    ' ÚJ track-szám (eggyel nő) + division és az esetleges további header-bájtok (változatlan)
                    kimenet.Write(adat, 0, 8)
                    kimenet.Write(BeIntBajtok(format, 2), 0, 2)
                    kimenet.Write(BeIntBajtok(trackSzam + 1, 2), 0, 2)
                    kimenet.Write(adat, 12, headerHossz - 4)

                    kimenet.Write(adat, elsoTrackKezdete, elsoTrackVege - elsoTrackKezdete)   ' 0. track (conductor) változatlanul
                    kimenet.Write(ujTrackChunk, 0, ujTrackChunk.Length)                       ' ÚJ track beszúrva
                    kimenet.Write(adat, elsoTrackVege, adat.Length - elsoTrackVege)           ' a többi track változatlanul
                End If
            End Using

            Return irtCsatornaSzam
        End Function

        ''' <summary>
        ''' Végigmegy a fájl ÖSSZES trackjén (a 0. tracktől kezdve), és
        ''' megállapítja, mely csatornákon (0-15) fordul elő ténylegesen
        ''' bármilyen csatorna-üzenet (Note On/Off, Control Change, Program
        ''' Change stb.) - a meta- és SysEx-eseményeket csak átugorja,
        ''' azok nem csatorna-specifikusak.
        ''' </summary>
        Private Function HasznaltCsatornakFelderitese(adat As Byte(), elsoTrackKezdete As Integer, trackSzam As Integer) As Boolean()
            Dim hasznalt(15) As Boolean
            Dim offset As Integer = elsoTrackKezdete

            For t As Integer = 0 To trackSzam - 1
                If Not ChunkAzonositoEgyezikE(adat, offset, "MTrk") Then Exit For
                Dim trackHossz As Integer = OlvasBeInt32(adat, offset + 4)
                Dim trackVege As Integer = offset + 8 + trackHossz
                If trackVege > adat.Length Then Exit For

                TrackCsatornaiFelderitese(adat, offset + 8, trackVege, hasznalt)

                offset = trackVege
            Next

            Return hasznalt
        End Function

        ''' <summary>
        ''' Egyetlen track adat-részét járja végig (delta-time + esemény
        ''' párok, running status-t is kezelve), és a talált csatorna-
        ''' üzenetek csatornáit True-ra állítja a hasznalt tömbben. Hibás/
        ''' felismerhetetlen bájtnál egyszerűen megszakítja a track
        ''' feldolgozását (a már addig felderített csatornák megmaradnak) -
        ''' ez csak a védelmi funkciót gyengítené egy sérült fájlnál, magát
        ''' a beszúrást nem hiúsítja meg.
        ''' </summary>
        Private Sub TrackCsatornaiFelderitese(adat As Byte(), kezdet As Integer, vege As Integer, hasznalt As Boolean())
            Dim pos As Integer = kezdet
            Dim elozoStatusz As Byte = 0

            While pos < vege
                pos = VlqAtugras(adat, pos, vege)
                If pos >= vege Then Exit While

                Dim elsoBajt As Byte = adat(pos)
                Dim statuszBajt As Byte
                If elsoBajt >= &H80 Then
                    statuszBajt = elsoBajt
                    pos += 1
                Else
                    statuszBajt = elozoStatusz  ' running status - az előző csatorna-üzenet státuszbájtja érvényes
                End If

                If statuszBajt = &HFF Then
                    pos += 1 ' meta-esemény típusbájtja
                    Dim hossz As Integer
                    pos = VlqOlvasas(adat, pos, vege, hossz)
                    pos += hossz
                    elozoStatusz = 0
                ElseIf statuszBajt = &HF0 OrElse statuszBajt = &HF7 Then
                    Dim hossz As Integer
                    pos = VlqOlvasas(adat, pos, vege, hossz)
                    pos += hossz
                    elozoStatusz = 0
                ElseIf statuszBajt >= &H80 AndAlso statuszBajt < &HF0 Then
                    Dim csatorna As Integer = statuszBajt And &HF
                    hasznalt(csatorna) = True
                    elozoStatusz = statuszBajt

                    Dim tipus As Integer = statuszBajt And &HF0
                    If tipus = &HC0 OrElse tipus = &HD0 Then
                        pos += 1 ' Program Change / Channel Pressure: 1 adatbájt
                    Else
                        pos += 2 ' a többi csatorna-üzenet: 2 adatbájt
                    End If
                Else
                    Exit While ' ismeretlen/érvénytelen bájt - védelmi kilépés
                End If
            End While
        End Sub

        Private Function VlqAtugras(adat As Byte(), pos As Integer, hatar As Integer) As Integer
            Dim p As Integer = pos
            While p < hatar
                Dim b As Byte = adat(p)
                p += 1
                If (b And &H80) = 0 Then Exit While
            End While
            Return p
        End Function

        Private Function VlqOlvasas(adat As Byte(), pos As Integer, hatar As Integer, ByRef ertek As Integer) As Integer
            Dim p As Integer = pos
            Dim v As Integer = 0
            While p < hatar
                Dim b As Byte = adat(p)
                p += 1
                v = (v << 7) Or (b And &H7F)
                If (b And &H80) = 0 Then Exit While
            End While
            ertek = v
            Return p
        End Function

        ''' <summary>
        ''' Az AKTÍV ÉS a fájlban ténylegesen használt csatornák MIDI-
        ''' üzeneteit építi fel egy új track adat-részeként (delta-time +
        ''' esemény párok), a végén a kötelező End of Track meta-
        ''' eseménnyel. Minden üzenet delta-time=0-val szerepel, mert
        ''' mindegyiknek a fájl legelső pillanatában kell megszólalnia.
        ''' </summary>
        Private Function UjTrackAdatEpitese(csatornak As IList(Of CsatornaBeallitas), alapHangero As Integer, reverbSzazalek As Integer,
                                             hasznaltCsatornak As Boolean(), ByRef irtCsatornaSzam As Integer) As Byte()
            Dim esemenyek As New List(Of Byte)
            Dim reverbErtek As Byte = Adatbajt(reverbSzazalek * 127 \ 100)
            irtCsatornaSzam = 0

            Dim csatornaSzam As Integer = Math.Min(16, csatornak.Count)
            For c As Integer = 0 To csatornaSzam - 1
                Dim adat As CsatornaBeallitas = csatornak(c)
                If Not adat.Aktiv Then Continue For
                If Not hasznaltCsatornak(c) Then Continue For ' VÉDELEM: nincs adat erre a csatornára a fájlban

                Dim statuszCC As Byte = CByte(&HB0 + c)
                Dim statuszPC As Byte = CByte(&HC0 + c)
                Dim bank As Integer = adat.Hangszer \ 128
                Dim program As Integer = adat.Hangszer Mod 128
                Dim hangeroVegso As Integer = CInt(adat.Hangero * (alapHangero / 100.0))

                EsemenyHozzaadasa(esemenyek, statuszCC, CByte(BANK_SELECT_MSB), CByte(0))
                EsemenyHozzaadasa(esemenyek, statuszCC, CByte(BANK_SELECT_LSB), Adatbajt(bank))
                EsemenyHozzaadasa(esemenyek, statuszPC, Adatbajt(program))
                EsemenyHozzaadasa(esemenyek, statuszCC, CByte(MAIN_VOLUME), Adatbajt(hangeroVegso))
                EsemenyHozzaadasa(esemenyek, statuszCC, CByte(PAN), Adatbajt(adat.Ter + 64))
                EsemenyHozzaadasa(esemenyek, statuszCC, CByte(MOD_WHEEL), Adatbajt(adat.Vibrato))
                EsemenyHozzaadasa(esemenyek, statuszCC, CByte(CHORUS), Adatbajt(adat.Chorus))
                EsemenyHozzaadasa(esemenyek, statuszCC, CByte(REVERB), reverbErtek)

                irtCsatornaSzam += 1
            Next

            If irtCsatornaSzam = 0 Then Return Array.Empty(Of Byte)()

            ' End of Track - minden MTrk chunk kötelező záró meta-eseménye
            esemenyek.Add(0)
            esemenyek.Add(&HFF)
            esemenyek.Add(&H2F)
            esemenyek.Add(0)

            Return esemenyek.ToArray()
        End Function

        ''' <summary>
        ''' Egy delta-time=0 + státuszbájt + tetszőleges számú adatbájt
        ''' hármast/párost fűz a listához (Controller Change: 2 adatbájt,
        ''' Program Change: 1 adatbájt).
        ''' </summary>
        Private Sub EsemenyHozzaadasa(lista As List(Of Byte), statuszBajt As Byte, ParamArray adatBajtok() As Byte)
            lista.Add(0)
            lista.Add(statuszBajt)
            lista.AddRange(adatBajtok)
        End Sub

        ''' <summary>
        ''' Egy MIDI adatbájt (0-127) tartományra szorítása - a MIDI
        ''' szabvány szerint az adatbájtok felső bitje mindig 0 kell,
        ''' hogy legyen, enélkül a CByte konverzió (érvénytelen, pl.
        ''' negatív vagy 128 fölötti bemenet esetén) kivételt dobna.
        ''' </summary>
        Private Function Adatbajt(ertek As Integer) As Byte
            Return CByte(Math.Max(0, Math.Min(127, ertek)))
        End Function

        Private Function ChunkAzonositoEgyezikE(adat As Byte(), offset As Integer, azonosito As String) As Boolean
            If offset + 4 > adat.Length Then Return False
            For i As Integer = 0 To 3
                If adat(offset + i) <> CByte(Asc(azonosito(i))) Then Return False
            Next
            Return True
        End Function

        Private Function OlvasBeInt32(adat As Byte(), offset As Integer) As Integer
            Return (CInt(adat(offset)) << 24) Or (CInt(adat(offset + 1)) << 16) Or (CInt(adat(offset + 2)) << 8) Or CInt(adat(offset + 3))
        End Function

        Private Function OlvasBeInt16(adat As Byte(), offset As Integer) As Integer
            Return (CInt(adat(offset)) << 8) Or CInt(adat(offset + 1))
        End Function

        Private Function BeIntBajtok(ertek As Integer, hossz As Integer) As Byte()
            Dim eredmeny(hossz - 1) As Byte
            For i As Integer = 0 To hossz - 1
                eredmeny(i) = CByte((ertek >> (8 * (hossz - 1 - i))) And &HFF)
            Next
            Return eredmeny
        End Function

        Private Function ChunkEpitese(azonosito As String, adatResz As Byte()) As Byte()
            Dim eredmeny(7 + adatResz.Length) As Byte
            For i As Integer = 0 To 3
                eredmeny(i) = CByte(Asc(azonosito(i)))
            Next
            Array.Copy(BeIntBajtok(adatResz.Length, 4), 0, eredmeny, 4, 4)
            Array.Copy(adatResz, 0, eredmeny, 8, adatResz.Length)
            Return eredmeny
        End Function

    End Module

End Namespace

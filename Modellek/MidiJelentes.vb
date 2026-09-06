Imports System.Linq
Imports System.Text

Namespace Modellek

    ''' <summary>
    ''' Egy beolvasott .mid fájl tartalmának megjelenítése EMBERI OLVASÁSRA
    ''' szánt szöveges jelentésként - a "MIDI fájl beállításainak mentése
    ''' olvasható formában" segédprogram logikája.
    '''
    ''' SZÁNDÉKOSAN nem JSON vagy más gépi formátum: a cél, hogy a felhasználó
    ''' egy Jegyzettömbben megnyitva azonnal lássa, mi van a fájlban. Az
    ''' értékek pontozott kitöltéssel egy oszlopba rendeződnek, így két fájl
    ''' jelentése egymás mellett is könnyen összehasonlítható.
    '''
    ''' A csatorna-blokkokba a TÁGABB szabály szerint kerül be egy csatorna:
    ''' elég, ha BÁRMILYEN üzenet van rajta. Ez eltér a preset-készítőtől (ott
    ''' csak a ténylegesen megszólaló hang számít), és szándékos: egy riportnak
    ''' az a dolga, hogy mindent megmutasson, a "Hangok száma" sorból pedig
    ''' rögtön látszik, ha egy csatornán valójában egy hang sem szól.
    ''' </summary>
    Public Module MidiJelentes

        ''' <summary>A címke-oszlop szélessége - eddig tart a pontozott kitöltés.</summary>
        Private Const CIMKE_SZELESSEG As Integer = 27

        Private Const VONAL_HOSSZ As Integer = 60

        ''' <summary>Legfeljebb ennyi jelölőt sorolunk fel - egy sűrűn jelölt fájl ne öntse el a jelentést.</summary>
        Private Const MAX_JELOLO As Integer = 30

        ''' <summary>
        ''' A csatorna-állapotot leíró vezérlők, amelyek MINDIG megjelennek a
        ''' jelentésben (akkor is, ha a fájl nem tartalmazza őket) - ezeket
        ''' kezeli a program felülete is. A fájlban talált TÖBBI vezérlő
        ''' szándékosan NEM jelenik meg a jelentésben.
        ''' </summary>
        Private ReadOnly kiemeltVezerlok As (Szam As Integer, Cimke As String, Alapertek As Integer)() = {
            (CsatornaUzenetek.MAIN_VOLUME, "Hangerő (CC7)", 127),
            (CsatornaUzenetek.PAN, "Tér (CC10)", 64),
            (CsatornaUzenetek.MOD_WHEEL, "Vibrato (CC1)", 0),
            (CsatornaUzenetek.CHORUS, "Chorus (CC93)", 0),
            (CsatornaUzenetek.REVERB, "Visszhang (CC91)", 0)
        }

        ''' <summary>
        ''' Elkészíti a teljes szöveges jelentést egy beolvasott MIDI fájlról.
        ''' </summary>
        ''' <param name="fajlAdat">A MidiFajlOlvaso.Beolvas() eredménye.</param>
        ''' <param name="fajlNev">A forrás .mid fájl neve (csak megjelenítésre).</param>
        ''' <param name="hangszerListaFajl">A hangszernevek feloldásához használt lista neve.</param>
        ''' <param name="presetFajl">
        ''' A jelentés készítésekor betöltött preset (.json) fájl neve, VAGY
        ''' üres sztring, ha nem tartozik ide. Csak a FELVÉTEL mellé készülő
        ''' jelentésnél van értelme: ott a felvétel ténylegesen azzal a
        ''' preset-tel készült, tehát fontos adat. A menüből indított
        ''' jelentésnél viszont egy IDEGEN MIDI fájlt elemzünk, aminek semmi
        ''' köze a nálunk éppen betöltött preset-hez - ezért ott üresen marad,
        ''' és a sor meg sem jelenik (a felhasználó kifejezett kérése).
        ''' </param>
        Public Function JelentesKeszitese(fajlAdat As MidiFajlAdat, fajlNev As String, hangszerListaFajl As String,
                                          Optional presetFajl As String = "") As String
            Dim sb As New StringBuilder()

            FejlecIrasa(sb, fajlNev, hangszerListaFajl, presetFajl)
            AltalanosAdatokIrasa(sb, fajlAdat)

            For csatornaIndex As Integer = 0 To 15
                Dim csatornaAdat As MidiCsatornaAdat = fajlAdat.Csatornak(csatornaIndex)
                If Not csatornaAdat.VanAdat Then Continue For
                CsatornaBlokkIrasa(sb, csatornaIndex, csatornaAdat)
            Next

            JelolokIrasa(sb, fajlAdat)

            Return sb.ToString()
        End Function

        Private Sub FejlecIrasa(sb As StringBuilder, fajlNev As String, hangszerListaFajl As String, presetFajl As String)
            sb.AppendLine(New String("="c, VONAL_HOSSZ))
            sb.AppendLine(" MidiBill3 - MIDI fájl beállításai")
            sb.AppendLine(New String("="c, VONAL_HOSSZ))
            Sor(sb, "Fájl", fajlNev)
            Sor(sb, "Készült", DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss", Globalization.CultureInfo.InvariantCulture))
            If presetFajl.Length > 0 Then Sor(sb, "Preset fájl", presetFajl)
            Sor(sb, "Hangszerlista", hangszerListaFajl)
            sb.AppendLine()
        End Sub

        Private Sub AltalanosAdatokIrasa(sb As StringBuilder, fajlAdat As MidiFajlAdat)
            Alcim(sb, "ÁLTALÁNOS ADATOK")

            ' A fájlba írt szöveges információk - csak akkor jelennek meg, ha a
            ' fájl ténylegesen tartalmazza őket (sok fájlban egyik sincs).
            If fajlAdat.Cim.Length > 0 Then Sor(sb, "Cím", fajlAdat.Cim)
            If fajlAdat.SzerzoiJog.Length > 0 Then Sor(sb, "Szerzői jog", fajlAdat.SzerzoiJog)
            If fajlAdat.EszkozNev.Length > 0 Then Sor(sb, "Eszköz", fajlAdat.EszkozNev)
            If fajlAdat.AltalanosSzoveg.Length > 0 Then Sor(sb, "Megjegyzés", fajlAdat.AltalanosSzoveg)

            ' Melyik szabványra készült a fájl (a benne lévő SysEx alapján). Ez
            ' azért fontos, mert a bankszámok értelmezése szabványonként más -
            ' lásd az állapot-dokumentum "Következő lépések" 13. pontját.
            If fajlAdat.Szabvany.Length > 0 Then
                Sor(sb, "Szabvány", fajlAdat.Szabvany & " (a fájl SysEx üzenete alapján)")
            Else
                Sor(sb, "Szabvány", "a fájl nem jelzi")
            End If

            Sor(sb, "Formátum", FormatumSzoveg(fajlAdat.Formatum))
            Sor(sb, "Trackek száma", fajlAdat.TrackSzam.ToString())

            If fajlAdat.KezdoTempoMikroszek > 0 Then
                Dim bpm As Double = 60000000.0 / fajlAdat.KezdoTempoMikroszek
                Dim tempoSzoveg As String = bpm.ToString("0.0") & " BPM"
                If fajlAdat.TempoValtasokSzama > 1 Then
                    tempoSzoveg &= "  [a fájlban " & fajlAdat.TempoValtasokSzama & " tempóbeállítás van]"
                End If
                Sor(sb, "Tempó", tempoSzoveg)
            Else
                Sor(sb, "Tempó", "nincs a fájlban (alapérték: 120,0 BPM)")
            End If

            If fajlAdat.VanUtemjelzes Then
                Dim utemSzoveg As String = fajlAdat.UtemSzamlalo & "/" & fajlAdat.UtemNevezo
                If fajlAdat.UtemjelzesValtasokSzama > 1 Then
                    utemSzoveg &= "  [a fájlban " & fajlAdat.UtemjelzesValtasokSzama & " ütemjelzés van]"
                End If
                Sor(sb, "Ütemjelzés", utemSzoveg)
            Else
                Sor(sb, "Ütemjelzés", "nincs a fájlban (alapérték: 4/4)")
            End If

            If fajlAdat.VanHangnem Then
                Sor(sb, "Hangnem", HangnemSzoveg(fajlAdat.HangnemElojegyzes, fajlAdat.HangnemMoll))
            Else
                Sor(sb, "Hangnem", "nincs a fájlban")
            End If

            If fajlAdat.TeljesHosszMasodperc > 0 Then
                Sor(sb, "Teljes hossz", IdoSzoveg(fajlAdat.TeljesHosszMasodperc))
            ElseIf fajlAdat.SmpteE Then
                Sor(sb, "Teljes hossz", "nem meghatározható (SMPTE időzítésű fájl)")
            Else
                Sor(sb, "Teljes hossz", "nem meghatározható")
            End If

            Dim csatornak As List(Of Integer) = fajlAdat.AdattalRendelkezoCsatornak()
            Dim hangotJatszok As List(Of Integer) = fajlAdat.HangotJatszoCsatornak()

            If csatornak.Count = 0 Then
                Sor(sb, "Csatornák adattal", "nincs egyetlen csatorna-üzenet sem a fájlban")
            Else
                Sor(sb, "Csatornák adattal", csatornak.Count & " db (" & String.Join(", ", csatornak.Select(Function(c) c & ".")) & ")")
                Sor(sb, "Ebből hangot játszik", If(hangotJatszok.Count = 0,
                                                   "egyik sem",
                                                   hangotJatszok.Count & " db (" & String.Join(", ", hangotJatszok.Select(Function(c) c & ".")) & ")"))
            End If

            sb.AppendLine()
        End Sub

        Private Sub CsatornaBlokkIrasa(sb As StringBuilder, csatornaIndex As Integer, csatornaAdat As MidiCsatornaAdat)
            Dim dobCsatorna As Boolean = (csatornaIndex = 9)
            Alcim(sb, (csatornaIndex + 1) & ". CSATORNA" & If(dobCsatorna, " (dob)", ""))

            HangszinekIrasa(sb, csatornaAdat, dobCsatorna)

            For Each vezerlo In kiemeltVezerlok
                If csatornaAdat.VanVezerlo(vezerlo.Szam) Then
                    Dim ertek As Integer = csatornaAdat.VezerloErtek(vezerlo.Szam, 0)
                    Dim ertekSzoveg As String = ertek.ToString()
                    If vezerlo.Szam = CsatornaUzenetek.PAN Then ertekSzoveg &= " (" & PanoramaSzoveg(ertek) & ")"
                    Sor(sb, vezerlo.Cimke, ertekSzoveg)
                Else
                    Sor(sb, vezerlo.Cimke, "nincs a fájlban (alapérték: " & vezerlo.Alapertek & ")")
                End If
            Next

            Sor(sb, "Hangok száma", csatornaAdat.HangokSzama.ToString())
            If csatornaAdat.HangokSzama > 0 Then
                Sor(sb, "Hangterjedelem", Hangnevek.HangNev(csatornaAdat.LegmelyebbHang) & " - " &
                                          Hangnevek.HangNev(csatornaAdat.LegmagasabbHang))
            End If

            sb.AppendLine()
        End Sub

        ''' <summary>
        ''' A csatorna hangszínei KÉT sorban: ami a FÁJLBA VAN ÍRVA szövegesen,
        ''' és ami a betöltött hangszerlistából jön. A kettő egymás mellett
        ''' rögtön megmutatja, ha eltérés van (a felhasználó kérése).
        '''
        ''' A felhasználó kifejezett kérése, hogy az ÖSSZES hangszínváltás
        ''' megjelenjen, egymás alatt, a fájlbeli sorrendben - a többi értéknél
        ''' elég a kezdőérték.
        '''
        ''' A fájlbeli név TRACK-szintű, ezért csak EGYSZER szerepel: nem tud
        ''' hangszínváltásonként különbözni.
        ''' </summary>
        Private Sub HangszinekIrasa(sb As StringBuilder, csatornaAdat As MidiCsatornaAdat, dobCsatorna As Boolean)
            If csatornaAdat.FajlbeliNev.Length > 0 Then
                Sor(sb, "Hangszín (MIDI fájlban)", """" & csatornaAdat.FajlbeliNev & """ (" & csatornaAdat.FajlbeliNevForrasa & ")")
            Else
                Sor(sb, "Hangszín (MIDI fájlban)", "nincs név a fájlban")
            End If

            If csatornaAdat.Hangszervaltasok.Count = 0 Then
                Sor(sb, "Hangszín (lista szerint)", "nincs hangszerváltás a fájlban (alapérték: a lista első hangszere)")
                Return
            End If

            Sor(sb, "Hangszín (lista szerint)", HangszerSzoveg(csatornaAdat.Hangszervaltasok(0), dobCsatorna))

            If csatornaAdat.Hangszervaltasok.Count = 1 Then Return

            Sor(sb, "További hangszínváltások", (csatornaAdat.Hangszervaltasok.Count - 1) & " db, sorrendben:")
            For i As Integer = 1 To csatornaAdat.Hangszervaltasok.Count - 1
                sb.AppendLine("    " & i & ". " & HangszerSzoveg(csatornaAdat.Hangszervaltasok(i), dobCsatorna))
            Next
        End Sub

        ' ============================================================
        ' Szöveggé alakító segédfüggvények
        ' ============================================================

        ''' <summary>Egy "címke .......: érték" formájú sor, egy oszlopba rendezett értékekkel.</summary>
        Private Sub Sor(sb As StringBuilder, cimke As String, ertek As String)
            Dim kitoltes As String = ""
            If cimke.Length < CIMKE_SZELESSEG Then
                kitoltes = " " & New String("."c, CIMKE_SZELESSEG - cimke.Length - 1)
            End If
            sb.AppendLine(cimke & kitoltes & ": " & ertek)
        End Sub

        Private Sub Alcim(sb As StringBuilder, szoveg As String)
            Dim eleje As String = "-- " & szoveg & " "
            Dim hossz As Integer = Math.Max(3, VONAL_HOSSZ - eleje.Length)
            sb.AppendLine(eleje & New String("-"c, hossz))
        End Sub

        ''' <summary>
        ''' A fájl szerkezeti jelölői (FF 06) időrendben - ezek mutatják a darab
        ''' felépítését ("Intro", "Verse", "Chorus" stb.). Hosszkorláttal, hogy
        ''' egy sűrűn jelölt fájl ne öntse el a jelentést.
        ''' </summary>
        Private Sub JelolokIrasa(sb As StringBuilder, fajlAdat As MidiFajlAdat)
            If fajlAdat.Jelolok.Count = 0 Then Return

            Alcim(sb, "JELÖLŐK")

            Dim megjelenitheto As Integer = Math.Min(fajlAdat.Jelolok.Count, MAX_JELOLO)
            For i As Integer = 0 To megjelenitheto - 1
                Dim jelolo As MidiJelolo = fajlAdat.Jelolok(i)
                Sor(sb, "  " & RovidIdoSzoveg(jelolo.IdoMasodperc), jelolo.Szoveg)
            Next

            If fajlAdat.Jelolok.Count > MAX_JELOLO Then
                sb.AppendLine("    ... és még " & (fajlAdat.Jelolok.Count - MAX_JELOLO) & " jelölő")
            End If

            sb.AppendLine()
        End Sub

        ''' <summary>Rövid "perc:másodperc" alak a jelölőkhöz.</summary>
        Private Function RovidIdoSzoveg(masodperc As Double) As String
            Dim perc As Integer = CInt(Math.Floor(masodperc / 60))
            Dim maradek As Integer = CInt(Math.Floor(masodperc - perc * 60))
            Return perc & ":" & maradek.ToString("00")
        End Function

        Private Function FormatumSzoveg(formatum As Integer) As String
            Select Case formatum
                Case 0 : Return "0 (egyetlen közös track)"
                Case 1 : Return "1 (több track, közös időzítés)"
                Case 2 : Return "2 (független trackek)"
                Case Else : Return formatum.ToString()
            End Select
        End Function

        ''' <summary>
        ''' Kombinált hangszer-kulcs (bank * 128 + program) olvasható alakban.
        '''
        ''' Ha a pontos bank+program páros nincs a betöltött listában (tipikusan
        ''' azért, mert a fájl nem nulla bankot használ, a GM.lst viszont csak a
        ''' 0-ás bankot ismeri), akkor a puszta PROGRAMSZÁMRA esünk vissza, és a
        ''' beépített General MIDI nevet mutatjuk - így nem marad üres kézzel a
        ''' felhasználó, de látja, hogy ez már csak közelítés.
        ''' </summary>
        Private Function HangszerSzoveg(kombinaltKulcs As Integer, dobCsatorna As Boolean) As String
            Dim bank As Integer = kombinaltKulcs \ 128
            Dim program As Integer = kombinaltKulcs Mod 128
            Dim szamok As String = "(bank " & bank & ", program " & program & ")"

            Dim nev As String
            If dobCsatorna Then
                nev = HangszerLista.DobNev(kombinaltKulcs, "")
            Else
                nev = HangszerLista.Nev(kombinaltKulcs, "")
            End If

            If nev.Length > 0 Then Return nev & " " & szamok

            ' Nincs a listában - visszaesés a puszta programszámra.
            Dim tartalekNev As String
            If dobCsatorna Then
                tartalekNev = HangszerLista.DobNev(program, "")
            Else
                tartalekNev = HangszerLista.Nev(program, "")
            End If

            If tartalekNev.Length > 0 Then
                Return "nincs a listában " & szamok & " - GM: " & tartalekNev
            End If

            Return "nincs a listában " & szamok
        End Function

        Private Function PanoramaSzoveg(ertek As Integer) As String
            If ertek = 64 Then Return "középen"
            If ertek < 64 Then Return "balra " & (64 - ertek)
            Return "jobbra " & (ertek - 64)
        End Function

        ''' <summary>Másodperc -> "perc:másodperc" alak, a pontos másodperccel kiegészítve.</summary>
        Private Function IdoSzoveg(masodperc As Double) As String
            Dim perc As Integer = CInt(Math.Floor(masodperc / 60))
            Dim maradek As Double = masodperc - perc * 60
            Return perc & ":" & CInt(Math.Floor(maradek)).ToString("00") &
                   " (" & masodperc.ToString("0.0") & " másodperc)"
        End Function

        ''' <summary>
        ''' Az előjegyzés (-7..+7) és a dúr/moll jelző alapján a hangnem neve.
        ''' A magyar zenei szokás szerint a dúr hangnemeket nagybetűvel, a moll
        ''' hangnemeket kisbetűvel írjuk.
        ''' </summary>
        Private Function HangnemSzoveg(elojegyzes As Integer, moll As Boolean) As String
            Dim durNevek() As String = {"Cesz", "Gesz", "Desz", "Asz", "Esz", "B", "F",
                                        "C", "G", "D", "A", "E", "H", "Fisz", "Cisz"}
            Dim mollNevek() As String = {"asz", "esz", "b", "f", "c", "g", "d",
                                         "a", "e", "h", "fisz", "cisz", "gisz", "disz", "aisz"}

            Dim index As Integer = elojegyzes + 7
            If index < 0 OrElse index > 14 Then Return "ismeretlen (előjegyzés: " & elojegyzes & ")"

            Dim nev As String = If(moll, mollNevek(index) & "-moll", durNevek(index) & "-dúr")

            Dim elojegyzesSzoveg As String
            If elojegyzes = 0 Then
                elojegyzesSzoveg = "nincs előjegyzés"
            ElseIf elojegyzes > 0 Then
                elojegyzesSzoveg = elojegyzes & " kereszt"
            Else
                elojegyzesSzoveg = Math.Abs(elojegyzes) & " b"
            End If

            Return nev & " (" & elojegyzesSzoveg & ")"
        End Function

    End Module

End Namespace

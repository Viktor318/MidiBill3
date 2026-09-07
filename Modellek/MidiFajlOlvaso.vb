Imports System.Linq
Imports System.Text

Namespace Modellek

    ''' <summary>
    ''' Egyetlen MIDI csatorna kiolvasott adatai egy .mid fájlból.
    ''' </summary>
    Public Class MidiCsatornaAdat

        ''' <summary>Van-e a csatornán BÁRMILYEN üzenet (hang, vezérlő, hangszerváltás stb.).</summary>
        Public Property VanAdat As Boolean = False

        ''' <summary>Ténylegesen megszólaló hangok száma (Note On, nullánál nagyobb sebességgel).</summary>
        Public Property HangokSzama As Integer = 0

        ''' <summary>A legmélyebb megszólaló hang MIDI-száma, vagy -1, ha nincs hang.</summary>
        Public Property LegmelyebbHang As Integer = -1

        ''' <summary>A legmagasabb megszólaló hang MIDI-száma, vagy -1, ha nincs hang.</summary>
        Public Property LegmagasabbHang As Integer = -1

        ''' <summary>
        ''' A csatorna ÖSSZES hangszerváltása, a fájlbeli SORRENDBEN, kombinált
        ''' kulcsként (lásd Modellek\Hangszerkulcs.vb). Az első elem a kezdő
        ''' hangszín.
        ''' A szöveges jelentés mindet kiírja egymás alá, a preset-készítő
        ''' viszont csak az elsőt használja.
        ''' </summary>
        Public Property Hangszervaltasok As New List(Of Integer)

        ''' <summary>
        ''' A csatornán talált vezérlők (Control Change) ELSŐ értéke:
        ''' kulcs = vezérlő száma (0-127), érték = a fájlban először előforduló
        ''' értéke. Szándékosan az ELSŐ, mert a preset a fájl KEZDŐ állapotát
        ''' hivatott tükrözni (lásd a felhasználóval egyeztetett szabályt).
        ''' </summary>
        Public Property KezdoVezerlok As New Dictionary(Of Integer, Integer)

        ''' <summary>
        ''' A csatornához tartozó, a FÁJLBA ÍRT szöveges név (üres, ha a fájl
        ''' nem tartalmaz ilyet). Ez a "Hangszín (MIDI fájlban)" sor forrása.
        ''' </summary>
        Public Property FajlbeliNev As String = ""

        ''' <summary>Honnan származik a FajlbeliNev: "hangszernév", "programnév" vagy "tracknév".</summary>
        Public Property FajlbeliNevForrasa As String = ""

        ''' <summary>A csatorna kezdő hangszíne kombinált kulcsként, vagy -1, ha a fájl nem tartalmaz hangszerváltást.</summary>
        Public ReadOnly Property KezdoHangszer As Integer
            Get
                If Hangszervaltasok.Count = 0 Then Return -1
                Return Hangszervaltasok(0)
            End Get
        End Property

        ''' <summary>Egy vezérlő kezdőértéke, vagy a megadott alapérték, ha a fájl nem tartalmazza.</summary>
        Public Function VezerloErtek(vezerloSzam As Integer, alapertek As Integer) As Integer
            Dim ertek As Integer
            If KezdoVezerlok.TryGetValue(vezerloSzam, ertek) Then Return ertek
            Return alapertek
        End Function

        ''' <summary>Igaz, ha a fájl tartalmaz értéket erre a vezérlőre ezen a csatornán.</summary>
        Public Function VanVezerlo(vezerloSzam As Integer) As Boolean
            Return KezdoVezerlok.ContainsKey(vezerloSzam)
        End Function

    End Class

    ''' <summary>Egy szerkezeti jelölő (marker) a fájlban: hol, és mit mond.</summary>
    Public Class MidiJelolo
        Public Property IdoMasodperc As Double
        Public Property Szoveg As String = ""
    End Class

    ''' <summary>
    ''' Egy beolvasott .mid fájl teljes tartalma strukturált formában.
    ''' </summary>
    Public Class MidiFajlAdat

        Public Property Formatum As Integer = 0
        Public Property TrackSzam As Integer = 0

        ''' <summary>Felbontás: tick / negyed hangjegy. SMPTE-időzítésű fájlnál 0 (lásd SmpteE).</summary>
        Public Property TickPerNegyed As Integer = 0

        ''' <summary>Igaz, ha a fájl SMPTE (képkocka-alapú) időzítést használ a szokásos tick/negyed helyett.</summary>
        Public Property SmpteE As Boolean = False

        ''' <summary>A fájl kezdő tempója mikroszekundum/negyed hangjegy értékben, vagy 0, ha nincs a fájlban.</summary>
        Public Property KezdoTempoMikroszek As Integer = 0

        ''' <summary>Hány tempóbeállítás van összesen a fájlban.</summary>
        Public Property TempoValtasokSzama As Integer = 0

        Public Property VanUtemjelzes As Boolean = False
        Public Property UtemSzamlalo As Integer = 4
        Public Property UtemNevezo As Integer = 4
        Public Property UtemjelzesValtasokSzama As Integer = 0

        Public Property VanHangnem As Boolean = False
        ''' <summary>Előjegyzés: -7 (7 b) ... +7 (7 kereszt).</summary>
        Public Property HangnemElojegyzes As Integer = 0
        Public Property HangnemMoll As Boolean = False

        ''' <summary>A fájl teljes hossza másodpercben (a tempóváltásokat is figyelembe véve).</summary>
        Public Property TeljesHosszMasodperc As Double = 0

        ' --- A fájlba írt szöveges információk ---

        ''' <summary>A darab címe: format 1-nél a 0. track neve, format 0-nál az egyetlen track neve.</summary>
        Public Property Cim As String = ""

        ''' <summary>Szerzői jogi megjegyzés (FF 02).</summary>
        Public Property SzerzoiJog As String = ""

        ''' <summary>Eszköznév (FF 09) - melyik hangmodulra készült a fájl.</summary>
        Public Property EszkozNev As String = ""

        ''' <summary>A fájl első általános szöveges megjegyzése (FF 01).</summary>
        Public Property AltalanosSzoveg As String = ""

        ''' <summary>
        ''' Melyik szabványra készült a fájl, a benne lévő SysEx üzenet alapján
        ''' ("General MIDI", "General MIDI 2", "Roland GS", "Yamaha XG"), vagy
        ''' üres, ha a fájl nem jelzi.
        ''' </summary>
        Public Property Szabvany As String = ""

        ''' <summary>Szerkezeti jelölők (FF 06), időrendben.</summary>
        Public Property Jelolok As New List(Of MidiJelolo)

        ''' <summary>A 16 csatorna adatai (index 0 = 1. csatorna).</summary>
        Public Property Csatornak As MidiCsatornaAdat()

        Public Sub New()
            Dim tomb(15) As MidiCsatornaAdat
            For i As Integer = 0 To 15
                tomb(i) = New MidiCsatornaAdat()
            Next
            Csatornak = tomb
        End Sub

        ''' <summary>Azon csatornák sorszámai (1-16), amelyeken BÁRMILYEN üzenet van.</summary>
        Public Function AdattalRendelkezoCsatornak() As List(Of Integer)
            Dim eredmeny As New List(Of Integer)
            For i As Integer = 0 To 15
                If Csatornak(i).VanAdat Then eredmeny.Add(i + 1)
            Next
            Return eredmeny
        End Function

        ''' <summary>Azon csatornák sorszámai (1-16), amelyeken ténylegesen szól hang.</summary>
        Public Function HangotJatszoCsatornak() As List(Of Integer)
            Dim eredmeny As New List(Of Integer)
            For i As Integer = 0 To 15
                If Csatornak(i).HangokSzama > 0 Then eredmeny.Add(i + 1)
            Next
            Return eredmeny
        End Function

    End Class

    ''' <summary>
    ''' Standard MIDI File (.mid) fájlok BEOLVASÁSA - a program KÖZÖS
    ''' SMF-értelmezője.
    '''
    ''' Három helyen használjuk:
    '''   1. "Preset készítése MIDI fájlból" - a kiolvasott beállításokból
    '''      preset (.json) készül,
    '''   2. "MIDI fájl beállításainak mentése olvasható formában" - ugyanaz
    '''      az adat, csak szöveges jelentésként,
    '''   3. MidiFajlSzerkeszto - a "MIDI fájlok módosítása" segédprogramnak
    '''      azt kell tudnia, mely csatornákon van adat a fájlban.
    '''
    ''' Format 0 (egyetlen közös track) ÉS format 1 (több track, közös
    ''' időzítés) fájlokat is kezel. A beszúró (MidiFajlSzerkeszto) továbbra
    ''' is csak format 1-et támogat, de az az ÍRÁS korlátja, nem az olvasásé.
    '''
    ''' A feldolgozás bájt-szintű, külső könyvtár nélkül - ugyanaz a bevált
    ''' megközelítés, mint a MidiFajlSzerkeszto írási oldalán és a
    ''' MidiFelvevo mentésénél.
    ''' </summary>
    Public Module MidiFajlOlvaso

        Private Const META_ESEMENY As Integer = &HFF
        Private Const SYSEX_KEZDET As Integer = &HF0
        Private Const SYSEX_FOLYTATAS As Integer = &HF7

        ' Szöveges meta-események
        Private Const META_SZOVEG As Integer = &H1
        Private Const META_SZERZOI_JOG As Integer = &H2
        Private Const META_TRACKNEV As Integer = &H3
        Private Const META_HANGSZERNEV As Integer = &H4
        Private Const META_JELOLO As Integer = &H6
        Private Const META_PROGRAMNEV As Integer = &H8
        Private Const META_ESZKOZNEV As Integer = &H9

        ' Egyéb meta-események
        Private Const META_CSATORNA_ELOTAG As Integer = &H20
        Private Const META_TEMPO As Integer = &H51
        Private Const META_UTEMJELZES As Integer = &H58
        Private Const META_HANGNEM As Integer = &H59

        Private Const ALAP_TEMPO_MIKROSZEK As Integer = 500000   ' 120 BPM - ha a fájl nem ad meg tempót

        ''' <summary>A hosszabb szövegek levágási határa - egy fájl néha egész kredit-listát ír a szövegmezőbe.</summary>
        Private Const MAX_SZOVEG_HOSSZ As Integer = 300

        ''' <summary>Egy tempóváltás helye és értéke - a teljes hossz kiszámításához kell.</summary>
        Private Structure TempoValtas
            Public Tick As Long
            Public Mikroszek As Integer
        End Structure

        ''' <summary>
        ''' Egy track feldolgozása közben gyűjtött, TRACK-SZINTŰ adatok - a
        ''' track végén derül ki, mihez tartoznak (lásd TracknevekTarsitasa).
        ''' </summary>
        Private Class TrackAllapot
            Public Property Tracknev As String = ""
            Public Property Hangszernev As String = ""
            Public Property Programnev As String = ""
            ''' <summary>Mely csatornákon van üzenet EBBEN a trackben.</summary>
            Public Property HasznaltCsatornak As New HashSet(Of Integer)
            ''' <summary>Az FF 20 (MIDI Channel Prefix) által kijelölt csatorna, vagy -1.</summary>
            Public Property CsatornaElotag As Integer = -1
            Public Property VegeTick As Long = 0
        End Class

        ''' <summary>Egy jelölő a feldolgozás közben, még tick-pozícióval (a másodperc a végén számítódik).</summary>
        Private Structure NyersJelolo
            Public Tick As Long
            Public Szoveg As String
        End Structure

        ''' <summary>
        ''' Beolvassa a megadott .mid fájlt, és strukturált formában adja
        ''' vissza a tartalmát.
        ''' </summary>
        ''' <exception cref="IO.InvalidDataException">A fájl nem érvényes Standard MIDI File.</exception>
        Public Function Beolvas(fajlUtvonal As String) As MidiFajlAdat
            Return BeolvasBajtokbol(IO.File.ReadAllBytes(fajlUtvonal))
        End Function

        ''' <summary>
        ''' Ugyanaz, mint a Beolvas(), de MÁR BEOLVASOTT bájtokból dolgozik.
        ''' A MidiFajlSzerkeszto hívja: ott a fájl bájtjai amúgy is a memóriában
        ''' vannak (a beszúráshoz kellenek), így nem kell másodszor beolvasni a
        ''' lemezről.
        ''' </summary>
        ''' <exception cref="IO.InvalidDataException">A fájl nem érvényes Standard MIDI File.</exception>
        Public Function BeolvasBajtokbol(adat As Byte()) As MidiFajlAdat
            If adat.Length < 14 OrElse Not ChunkAzonositoEgyezikE(adat, 0, "MThd") Then
                Throw New IO.InvalidDataException("A fájl nem érvényes Standard MIDI File (hiányzik az 'MThd' fejléc).")
            End If

            Dim eredmeny As New MidiFajlAdat()

            Dim headerHossz As Integer = OlvasBeInt32(adat, 4)
            eredmeny.Formatum = OlvasBeInt16(adat, 8)
            eredmeny.TrackSzam = OlvasBeInt16(adat, 10)

            ' A "division" mező kétféle időzítést jelölhet. Ha a felső bitje 0,
            ' a szokásos tick/negyed felbontásról van szó; ha 1, akkor SMPTE
            ' (képkocka-alapú) időzítésről. Utóbbi ritka, de nem szabad
            ' értelmetlen adatot mutatni rá.
            Dim division As Integer = OlvasBeInt16(adat, 12)
            If (division And &H8000) = 0 Then
                eredmeny.TickPerNegyed = division
            Else
                eredmeny.SmpteE = True
            End If

            If headerHossz < 6 OrElse adat.Length < 8 + headerHossz Then
                Throw New IO.InvalidDataException("A fájl fejléce hibás vagy csonka.")
            End If

            Dim tempoValtasok As New List(Of TempoValtas)
            Dim jelolok As New List(Of NyersJelolo)
            Dim leghosszabbTick As Long = 0

            ' A trackek egymás után következnek a fejléc után. A trackSzam
            ' mezőben megadott darabszámnál kevesebbet is elfogadunk (csonka
            ' fájlnál a már beolvasott adat is használható), a chunk-fejlécek
            ' alapján haladunk.
            Dim offset As Integer = 8 + headerHossz
            Dim trackIndex As Integer = 0

            While offset + 8 <= adat.Length
                If Not ChunkAzonositoEgyezikE(adat, offset, "MTrk") Then
                    ' A szabvány szerint az ismeretlen chunk-okat át kell ugrani.
                    Dim ismeretlenHossz As Integer = OlvasBeInt32(adat, offset + 4)
                    If ismeretlenHossz < 0 Then Exit While
                    offset += 8 + ismeretlenHossz
                    Continue While
                End If

                Dim trackHossz As Integer = OlvasBeInt32(adat, offset + 4)
                If trackHossz < 0 Then Exit While

                Dim trackVege As Integer = offset + 8 + trackHossz
                If trackVege > adat.Length Then trackVege = adat.Length   ' csonka track: ameddig van adat

                Dim trackAllapot As New TrackAllapot()
                TrackFeldolgozasa(adat, offset + 8, trackVege, eredmeny, tempoValtasok, jelolok, trackAllapot)

                TracknevekTarsitasa(eredmeny, trackAllapot, trackIndex)

                If trackAllapot.VegeTick > leghosszabbTick Then leghosszabbTick = trackAllapot.VegeTick

                trackIndex += 1
                offset = trackVege
            End While

            If trackIndex = 0 Then
                Throw New IO.InvalidDataException("A fájl nem tartalmaz egyetlen érvényes tracket sem ('MTrk' chunk hiányzik).")
            End If

            eredmeny.TempoValtasokSzama = tempoValtasok.Count
            If tempoValtasok.Count > 0 Then
                ' A kezdő tempó a legkorábbi tempóváltás (nem feltétlenül a
                ' legelső beolvasott, mert format 1-nél több trackből jöhetnek).
                Dim legkorabbi As TempoValtas = tempoValtasok(0)
                For Each tv As TempoValtas In tempoValtasok
                    If tv.Tick < legkorabbi.Tick Then legkorabbi = tv
                Next
                eredmeny.KezdoTempoMikroszek = legkorabbi.Mikroszek
            End If

            ' A tick -> másodperc átváltáshoz a TELJES tempótérkép kell, ezért
            ' csak most, minden track beolvasása után számoljuk ki a hosszt és
            ' a jelölők időpontját.
            Dim terkep As List(Of TempoValtas) = TempoterkepEpitese(tempoValtasok)

            eredmeny.TeljesHosszMasodperc = TickMasodpercre(leghosszabbTick, terkep, eredmeny)

            For Each nyers As NyersJelolo In jelolok.OrderBy(Function(j) j.Tick)
                eredmeny.Jelolok.Add(New MidiJelolo With {
                    .IdoMasodperc = TickMasodpercre(nyers.Tick, terkep, eredmeny),
                    .Szoveg = nyers.Szoveg
                })
            Next

            Return eredmeny
        End Function

        ''' <summary>
        ''' Egyetlen track adat-részének végigjárása.
        '''
        ''' A running status kezelése kötelező: a MIDI fájlokban egy
        ''' csatorna-üzenet elhagyhatja a státuszbájtot, ha az előző üzenet
        ''' ugyanolyan típusú volt. A szabvány szerint a meta- és SysEx
        ''' események ÉRVÉNYTELENÍTIK a running statust - ezért nullázzuk
        ''' utánuk.
        ''' </summary>
        Private Sub TrackFeldolgozasa(adat As Byte(), kezdet As Integer, vege As Integer,
                                      eredmeny As MidiFajlAdat, tempoValtasok As List(Of TempoValtas),
                                      jelolok As List(Of NyersJelolo), trackAllapot As TrackAllapot)

            Dim pos As Integer = kezdet
            Dim abszolutTick As Long = 0
            Dim elozoStatusz As Integer = 0

            ' Csatornánkénti Bank Select állapot: a Program Change mindig a
            ' LEGUTÓBB beállított bankkal együtt értelmezendő.
            Dim bankMsb(15) As Integer
            Dim bankLsb(15) As Integer

            While pos < vege
                Dim delta As Integer
                pos = VlqOlvasas(adat, pos, vege, delta)
                If pos >= vege Then Exit While
                abszolutTick += delta

                Dim elsoBajt As Integer = adat(pos)
                Dim statuszBajt As Integer
                If elsoBajt >= &H80 Then
                    statuszBajt = elsoBajt
                    pos += 1
                Else
                    statuszBajt = elozoStatusz
                    If statuszBajt = 0 Then Exit While   ' running status érvényes státuszbájt nélkül - sérült fájl
                End If

                If statuszBajt = META_ESEMENY Then
                    If pos >= vege Then Exit While
                    Dim metaTipus As Integer = adat(pos)
                    pos += 1

                    Dim hossz As Integer
                    pos = VlqOlvasas(adat, pos, vege, hossz)
                    If hossz < 0 OrElse pos + hossz > vege Then Exit While

                    MetaEsemenyFeldolgozasa(adat, pos, hossz, metaTipus, abszolutTick,
                                            eredmeny, tempoValtasok, jelolok, trackAllapot)

                    pos += hossz
                    elozoStatusz = 0

                ElseIf statuszBajt = SYSEX_KEZDET OrElse statuszBajt = SYSEX_FOLYTATAS Then
                    Dim hossz As Integer
                    pos = VlqOlvasas(adat, pos, vege, hossz)
                    If hossz < 0 OrElse pos + hossz > vege Then Exit While

                    ' A SysEx tartalma elárulja, MELYIK SZABVÁNYRA készült a
                    ' fájl (GM / GM2 / GS / XG) - ez a program szempontjából
                    ' fontos, mert a bankszámok értelmezése szabványonként más.
                    If eredmeny.Szabvany.Length = 0 Then
                        eredmeny.Szabvany = SzabvanyFelismerese(adat, pos, hossz)
                    End If

                    pos += hossz
                    elozoStatusz = 0

                ElseIf statuszBajt >= &H80 AndAlso statuszBajt < &HF0 Then
                    elozoStatusz = statuszBajt

                    Dim csatorna As Integer = statuszBajt And &HF
                    Dim uzenetTipus As Integer = statuszBajt And &HF0
                    Dim adatbajtokSzama As Integer = If(uzenetTipus = &HC0 OrElse uzenetTipus = &HD0, 1, 2)
                    If pos + adatbajtokSzama > vege Then Exit While

                    Dim adat1 As Integer = adat(pos)
                    Dim adat2 As Integer = If(adatbajtokSzama = 2, CInt(adat(pos + 1)), 0)
                    pos += adatbajtokSzama

                    trackAllapot.HasznaltCsatornak.Add(csatorna)

                    CsatornaUzenetFeldolgozasa(eredmeny.Csatornak(csatorna), csatorna,
                                               uzenetTipus, adat1, adat2, bankMsb, bankLsb)
                Else
                    ' Ismeretlen/érvénytelen bájt (pl. sérült fájl) - a track
                    ' további része nem értelmezhető megbízhatóan.
                    Exit While
                End If
            End While

            trackAllapot.VegeTick = abszolutTick
        End Sub

        Private Sub MetaEsemenyFeldolgozasa(adat As Byte(), pos As Integer, hossz As Integer, metaTipus As Integer,
                                            abszolutTick As Long, eredmeny As MidiFajlAdat,
                                            tempoValtasok As List(Of TempoValtas), jelolok As List(Of NyersJelolo),
                                            trackAllapot As TrackAllapot)

            Select Case metaTipus
                Case META_TEMPO
                    If hossz < 3 Then Return
                    Dim mikroszek As Integer = (CInt(adat(pos)) << 16) Or (CInt(adat(pos + 1)) << 8) Or CInt(adat(pos + 2))
                    If mikroszek <= 0 Then Return
                    tempoValtasok.Add(New TempoValtas With {.Tick = abszolutTick, .Mikroszek = mikroszek})

                Case META_UTEMJELZES
                    If hossz < 2 Then Return
                    eredmeny.UtemjelzesValtasokSzama += 1
                    ' Csak a legelső (kezdő) ütemjelzést tartjuk meg - a
                    ' felhasználóval egyeztetve a hangszíneken KÍVÜL minden más
                    ' értéknél a kezdőérték a lényeg.
                    If eredmeny.VanUtemjelzes Then Return
                    eredmeny.VanUtemjelzes = True
                    eredmeny.UtemSzamlalo = adat(pos)
                    ' A nevezőt a szabvány 2 hatványkitevőjeként tárolja (2 -> 4/4).
                    Dim kitevo As Integer = adat(pos + 1)
                    If kitevo > 10 Then kitevo = 10   ' védelem az értelmetlenül nagy értékek ellen
                    eredmeny.UtemNevezo = CInt(2 ^ kitevo)

                Case META_HANGNEM
                    If hossz < 2 OrElse eredmeny.VanHangnem Then Return
                    eredmeny.VanHangnem = True
                    ' Az előjegyzés előjeles bájt: -7 (7 b) ... +7 (7 kereszt).
                    Dim elojegyzes As Integer = adat(pos)
                    If elojegyzes > 127 Then elojegyzes -= 256
                    eredmeny.HangnemElojegyzes = elojegyzes
                    eredmeny.HangnemMoll = (adat(pos + 1) <> 0)

                Case META_TRACKNEV
                    If trackAllapot.Tracknev.Length = 0 Then trackAllapot.Tracknev = SzovegOlvasas(adat, pos, hossz)

                Case META_HANGSZERNEV
                    If trackAllapot.Hangszernev.Length = 0 Then trackAllapot.Hangszernev = SzovegOlvasas(adat, pos, hossz)

                Case META_PROGRAMNEV
                    If trackAllapot.Programnev.Length = 0 Then trackAllapot.Programnev = SzovegOlvasas(adat, pos, hossz)

                Case META_SZERZOI_JOG
                    If eredmeny.SzerzoiJog.Length = 0 Then eredmeny.SzerzoiJog = SzovegOlvasas(adat, pos, hossz)

                Case META_ESZKOZNEV
                    If eredmeny.EszkozNev.Length = 0 Then eredmeny.EszkozNev = SzovegOlvasas(adat, pos, hossz)

                Case META_SZOVEG
                    If eredmeny.AltalanosSzoveg.Length = 0 Then eredmeny.AltalanosSzoveg = SzovegOlvasas(adat, pos, hossz)

                Case META_JELOLO
                    Dim szoveg As String = SzovegOlvasas(adat, pos, hossz)
                    If szoveg.Length > 0 Then
                        jelolok.Add(New NyersJelolo With {.Tick = abszolutTick, .Szoveg = szoveg})
                    End If

                Case META_CSATORNA_ELOTAG
                    ' Az FF 20 kifejezetten CSATORNÁHOZ köti a rákövetkező
                    ' meta-eseményeket - ez a szabvány szerinti módja annak,
                    ' hogy egy több csatornát tartalmazó trackben is egyértelmű
                    ' legyen, melyik csatornáról szól egy név.
                    If hossz < 1 Then Return
                    Dim csatorna As Integer = adat(pos)
                    If csatorna >= 0 AndAlso csatorna <= 15 Then trackAllapot.CsatornaElotag = csatorna
            End Select
        End Sub

        ''' <summary>
        ''' A track végén eldönti, mihez tartoznak a benne talált nevek.
        '''
        ''' - A format 1 fájlok 0. trackje a szabvány szerint a "conductor"
        '''   track: annak a neve a DARAB CÍME, nem egy hangszeré. Format 0-nál
        '''   az egyetlen track neve szintén a cím.
        ''' - Egyébként a track neve egy csatornához tartozik - de csak akkor
        '''   társítható egyértelműen, ha a track PONTOSAN EGY csatornát
        '''   használ (vagy ha az FF 20 kifejezetten megmondja, melyiket).
        '''   Több csatornás tracknél (tipikusan format 0-nál) a név
        '''   kétértelmű lenne, ezért nem társítjuk.
        '''
        ''' A név forrásának SORRENDJE: hangszernév (FF 04) > programnév
        ''' (FF 08) > tracknév (FF 03) - az első kettő kifejezetten a hangszínt
        ''' nevezi meg, a tracknév általánosabb.
        ''' </summary>
        Private Sub TracknevekTarsitasa(eredmeny As MidiFajlAdat, trackAllapot As TrackAllapot, trackIndex As Integer)
            ' A 0. track neve MINDEN formátumban a darab címe (format 1-nél ez
            ' a "conductor" track, format 0-nál az egyetlen track).
            Dim cimTrack As Boolean = (trackIndex = 0)

            If cimTrack AndAlso eredmeny.Cim.Length = 0 Then eredmeny.Cim = trackAllapot.Tracknev

            ' Ha a trackben egyetlen csatorna-üzenet sincs (tiszta conductor
            ' track), nincs mihez társítani a neveket.
            If trackAllapot.HasznaltCsatornak.Count = 0 Then Return

            ' Melyik csatornához tartozzon a név?
            Dim celCsatorna As Integer = -1
            If trackAllapot.CsatornaElotag >= 0 Then
                celCsatorna = trackAllapot.CsatornaElotag
            ElseIf trackAllapot.HasznaltCsatornak.Count = 1 Then
                celCsatorna = trackAllapot.HasznaltCsatornak.First()
            End If

            If celCsatorna < 0 Then Return

            Dim csatornaAdat As MidiCsatornaAdat = eredmeny.Csatornak(celCsatorna)
            If csatornaAdat.FajlbeliNev.Length > 0 Then Return   ' már kapott nevet egy korábbi trackből

            If trackAllapot.Hangszernev.Length > 0 Then
                csatornaAdat.FajlbeliNev = trackAllapot.Hangszernev
                csatornaAdat.FajlbeliNevForrasa = "hangszernév"
            ElseIf trackAllapot.Programnev.Length > 0 Then
                csatornaAdat.FajlbeliNev = trackAllapot.Programnev
                csatornaAdat.FajlbeliNevForrasa = "programnév"
            ElseIf trackAllapot.Tracknev.Length > 0 AndAlso Not cimTrack Then
                csatornaAdat.FajlbeliNev = trackAllapot.Tracknev
                csatornaAdat.FajlbeliNevForrasa = "tracknév"
            End If
        End Sub

        Private Sub CsatornaUzenetFeldolgozasa(csatornaAdat As MidiCsatornaAdat, csatorna As Integer,
                                               uzenetTipus As Integer, adat1 As Integer, adat2 As Integer,
                                               bankMsb As Integer(), bankLsb As Integer())

            csatornaAdat.VanAdat = True

            Select Case uzenetTipus
                Case &H90
                    ' A 0 sebességű Note On a szabvány szerint hangkikapcsolás,
                    ' nem megszólaló hang - csak a ténylegeseket számoljuk.
                    If adat2 > 0 Then
                        csatornaAdat.HangokSzama += 1
                        If csatornaAdat.LegmelyebbHang < 0 OrElse adat1 < csatornaAdat.LegmelyebbHang Then
                            csatornaAdat.LegmelyebbHang = adat1
                        End If
                        If adat1 > csatornaAdat.LegmagasabbHang Then
                            csatornaAdat.LegmagasabbHang = adat1
                        End If
                    End If

                Case &HB0
                    Select Case adat1
                        Case CsatornaUzenetek.BANK_SELECT_MSB
                            bankMsb(csatorna) = adat2
                        Case CsatornaUzenetek.BANK_SELECT_LSB
                            bankLsb(csatorna) = adat2
                    End Select

                    ' Minden vezérlőből az ELSŐ értéket őrizzük meg.
                    If Not csatornaAdat.KezdoVezerlok.ContainsKey(adat1) Then
                        csatornaAdat.KezdoVezerlok(adat1) = adat2
                    End If

                Case &HC0
                    ' MINDKÉT bank-bájtot megőrizzük, ahogy a fájlban áll.
                    '
                    ' Korábban "amelyik nem nulla, az a bank" szabály szerint
                    ' egyetlen számra vontuk össze őket. Ez azért volt hibás,
                    ' mert a szabványok MÁSHOL tárolják a variációt: a Roland
                    ' GS az MSB-ben (az LSB a hangkészlet-térkép), a Yamaha XG
                    ' és a General MIDI 2 az LSB-ben. Egy GS fájlnál, ami az
                    ' LSB-be is ír térkép-számot (pl. MSB = 8, LSB = 1), a régi
                    ' szabály az LSB-t választotta volna - vagyis rossz hangot.
                    csatornaAdat.Hangszervaltasok.Add(
                        Hangszerkulcs.Kulcs(bankMsb(csatorna), bankLsb(csatorna), adat1))
            End Select
        End Sub

        ''' <summary>
        ''' A SysEx üzenet tartalmából felismeri, melyik szabványra készült a
        ''' fájl. Ezek az üzenetek a fájl elején szoktak állni, és a
        ''' hangmodult állítják be a megfelelő módba.
        ''' </summary>
        Private Function SzabvanyFelismerese(adat As Byte(), pos As Integer, hossz As Integer) As String
            If hossz < 4 Then Return ""

            Dim veg As Integer = pos + hossz

            Select Case adat(pos)
                Case &H7E
                    ' Univerzális, nem valós idejű üzenet: 7E <eszköz> 09 <mód>
                    ' 09 01 = General MIDI On, 09 03 = General MIDI 2 On
                    For i As Integer = pos To veg - 2
                        If adat(i) = &H9 Then
                            If adat(i + 1) = &H1 Then Return "General MIDI"
                            If adat(i + 1) = &H3 Then Return "General MIDI 2"
                        End If
                    Next

                Case &H41
                    ' Roland: a GS Reset jellegzetes része a 42 12 40 00 7F sorozat
                    For i As Integer = pos To veg - 5
                        If adat(i) = &H42 AndAlso adat(i + 1) = &H12 AndAlso
                           adat(i + 2) = &H40 AndAlso adat(i + 3) = &H0 AndAlso adat(i + 4) = &H7F Then
                            Return "Roland GS"
                        End If
                    Next
                    Return "Roland (egyedi SysEx)"

                Case &H43
                    ' Yamaha: az XG System On jellegzetes része a 4C 00 00 7E sorozat
                    For i As Integer = pos To veg - 4
                        If adat(i) = &H4C AndAlso adat(i + 1) = &H0 AndAlso
                           adat(i + 2) = &H0 AndAlso adat(i + 3) = &H7E Then
                            Return "Yamaha XG"
                        End If
                    Next
                    Return "Yamaha (egyedi SysEx)"
            End Select

            Return ""
        End Function

        ''' <summary>
        ''' Szöveges meta-esemény tartalmának kiolvasása. A MIDI szabvány nem
        ''' rögzíti a karakterkódolást; a gyakorlatban a Latin-1 a legbiztosabb
        ''' választás (az ASCII-t is helyesen adja vissza, és nem dob hibát
        ''' érvénytelen bájtsorozatra, ellentétben az UTF-8-cal). A vezérlő-
        ''' karaktereket kiszűrjük, a hosszú szövegeket levágjuk.
        ''' </summary>
        Private Function SzovegOlvasas(adat As Byte(), pos As Integer, hossz As Integer) As String
            If hossz <= 0 Then Return ""

            Dim nyers As String = Encoding.Latin1.GetString(adat, pos, hossz)

            Dim sb As New StringBuilder()
            For Each c As Char In nyers
                If Char.IsControl(c) Then
                    If sb.Length > 0 AndAlso sb(sb.Length - 1) <> " "c Then sb.Append(" "c)
                Else
                    sb.Append(c)
                End If
            Next

            Dim eredmeny As String = sb.ToString().Trim()
            If eredmeny.Length > MAX_SZOVEG_HOSSZ Then
                eredmeny = eredmeny.Substring(0, MAX_SZOVEG_HOSSZ) & "..."
            End If

            Return eredmeny
        End Function

        ''' <summary>
        ''' Rendezett tempótérkép. Ha a fájl nem ad meg tempót, a szabvány
        ''' szerinti 120 BPM az alapértelmezés.
        ''' </summary>
        Private Function TempoterkepEpitese(tempoValtasok As List(Of TempoValtas)) As List(Of TempoValtas)
            Dim terkep As List(Of TempoValtas) = tempoValtasok.OrderBy(Function(tv) tv.Tick).ToList()
            If terkep.Count = 0 OrElse terkep(0).Tick > 0 Then
                terkep.Insert(0, New TempoValtas With {.Tick = 0, .Mikroszek = ALAP_TEMPO_MIKROSZEK})
            End If
            Return terkep
        End Function

        ''' <summary>
        ''' Egy tick-pozíció átváltása másodpercre. A tempóváltásokat is
        ''' figyelembe veszi: szakaszonként külön számol, mert egy tempóváltás
        ''' után egy tick már más ideig tart.
        ''' </summary>
        Private Function TickMasodpercre(celTick As Long, terkep As List(Of TempoValtas), eredmeny As MidiFajlAdat) As Double
            If celTick <= 0 Then Return 0

            ' SMPTE-időzítésnél a tempó nem játszik szerepet: a felbontás
            ' közvetlenül időalapú. (Ritka eset, de ne adjunk rá hibás értéket.)
            If eredmeny.SmpteE OrElse eredmeny.TickPerNegyed <= 0 Then Return 0

            Dim osszesMikroszek As Double = 0

            For i As Integer = 0 To terkep.Count - 1
                Dim szakaszKezdet As Long = terkep(i).Tick
                If szakaszKezdet >= celTick Then Exit For

                Dim szakaszVege As Long = celTick
                If i < terkep.Count - 1 AndAlso terkep(i + 1).Tick < celTick Then
                    szakaszVege = terkep(i + 1).Tick
                End If

                Dim tickek As Long = szakaszVege - szakaszKezdet
                osszesMikroszek += tickek * (terkep(i).Mikroszek / CDbl(eredmeny.TickPerNegyed))
            Next

            Return osszesMikroszek / 1000000.0
        End Function

        ' ============================================================
        ' Alacsony szintű bájt-olvasó segédfüggvények
        ' ============================================================

        ''' <summary>
        ''' Variable-length quantity (VLQ) beolvasása - a MIDI fájlok
        ''' delta-time és hossz-formátuma: 7 bites csoportok, az utolsó
        ''' KIVÉTELÉVEL 0x80-nal jelölve. Visszaadja az érték UTÁNI pozíciót.
        ''' </summary>
        Private Function VlqOlvasas(adat As Byte(), pos As Integer, hatar As Integer, ByRef ertek As Integer) As Integer
            Dim p As Integer = pos
            Dim v As Integer = 0
            Dim bajtokSzama As Integer = 0

            While p < hatar
                Dim b As Integer = adat(p)
                p += 1
                bajtokSzama += 1
                v = (v << 7) Or (b And &H7F)
                If (b And &H80) = 0 Then Exit While
                ' A szabvány szerint egy VLQ legfeljebb 4 bájt - ennél többnél
                ' sérült a fájl, ne fussunk ki a tartományból.
                If bajtokSzama >= 4 Then Exit While
            End While

            ertek = v
            Return p
        End Function

        Private Function ChunkAzonositoEgyezikE(adat As Byte(), offset As Integer, azonosito As String) As Boolean
            If offset + 4 > adat.Length Then Return False
            For i As Integer = 0 To 3
                If adat(offset + i) <> CByte(Asc(azonosito(i))) Then Return False
            Next
            Return True
        End Function

        Private Function OlvasBeInt32(adat As Byte(), offset As Integer) As Integer
            If offset + 4 > adat.Length Then Return -1
            Return (CInt(adat(offset)) << 24) Or (CInt(adat(offset + 1)) << 16) Or
                   (CInt(adat(offset + 2)) << 8) Or CInt(adat(offset + 3))
        End Function

        Private Function OlvasBeInt16(adat As Byte(), offset As Integer) As Integer
            If offset + 2 > adat.Length Then Return 0
            Return (CInt(adat(offset)) << 8) Or CInt(adat(offset + 1))
        End Function

    End Module

End Namespace

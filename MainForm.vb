Imports MidiBill3.Modellek
Imports System.Linq

Public Class MainForm

    Private WithEvents midiMotor As New MidiEngine()

    Private hangszerKeretek(3) As GroupBox
    Private hangszerPanelek(3) As HangszerPanel
    Private csatornaAllapotok(15) As CsatornaBeallitas
    Private aktivCsoportIndex As Integer = 0

    Private Const DOB_CSATORNA_INDEX As Integer = 9

    Private inicializalasKesz As Boolean = False
    Private aktualisFajlNev As String = ""
    Private utolsoBetoltesMappa As String = ""
    Private utolsoMentesMappa As String = ""
    Private utolsoImportMappa As String = ""
    Private utolsoHangszerlistaMappa As String = ""
    Private utolsoSoundFontMappa As String = ""
    Private utolsoPresetKonvertalasMappa As String = ""
    Private utolsoMb3KonvertalasMappa As String = ""
    Private utolsoMidiFajlModositasMappa As String = ""
    Private aktualisSplitPont As Integer = 60
    Private splitTanulasAktiv As Boolean = False
    Private mentesKihagyasa As Boolean = False
    ' True-ra vált a FormClosing legelején: ettől kezdve a MIDI callback-szálról
    ' érkező üzeneteket eldobjuk, hogy a lezárás alatt már ne próbáljunk a UI
    ' szálhoz nyúlni (lásd midiMotor_MidiUzenetErkezett és MainForm_FormClosing).
    Private zarasFolyamatban As Boolean = False
    ' A jelenleg aktív hangszerlista fájlneve (a gyári GM.lst, vagy egy

    ' fájlnév az "SF adatok" mappából - lásd Modellek\Utvonalak.vb).
    Private aktualisHangszerListaFajl As String = "GM.lst"
    Private aktualisTranszponalas As Integer = 0
    Private Const TRANSZPONALAS_MIN As Integer = -24
    Private Const TRANSZPONALAS_MAX As Integer = 24

    ' A MIDI üzenetek kijelzésének korlátozása: a lblMidiUzenet ténylegesen
    ' csak a midiKijelzoIdozito ütemében (max. 5x/mp, lásd Interval lent)
    ' frissül, függetlenül
    ' attól, milyen sűrűn érkeznek a bejövő MIDI-üzenetek - ez elkerüli a
    ' felesleges, követhetetlenül gyors UI-frissítést nagyon sűrű
    ' billentyűjáték (pl. gyors arpeggio) esetén. A tényleges szöveggé
    ' alakítást (MidiUzenetSzoveg - string-összefűzés + Hangnevek/
    ' HangszerLista-keresés) SZÁNDÉKOSAN csak a Tick-ben végezzük el, nem
    ' minden beérkező üzenetnél: csak a legutóbbi üzenet 3 nyers Integerét
    ' tároljuk el itt, ami lényegesen olcsóbb.
    Private utolsoMidiUzenetStatusz As Integer = 0
    Private utolsoMidiAdat1 As Integer = 0
    Private utolsoMidiAdat2 As Integer = 0
    Private midiSzovegValtozott As Boolean = False
    ' 200 ms (5 Hz): kényelmesen olvasható tempó egy rövid szöveges
    ' feliratnál, mégis élőnek hat - 100 ms-nál (10 Hz) a szöveg
    ' gyakorlatilag csak villog, nem olvasható el.
    Private WithEvents midiKijelzoIdozito As New Timer With {.Interval = 200}

    ' Az Alap hangerő és a Reverb csúszka MIDI-küldésének korlátozása:
    ' a TrackBar ValueChanged eseménye NEM csak a húzás VÉGÉN tüzel,
    ' hanem minden egyes pixelnyi elmozdulásnál - enélkül egyetlen
    ' egérhúzás is másodpercenként több tucatszor küldene ki 16-16 MIDI
    ' Controller Change üzenetet (mind a 16 csatornára). A felirat
    ' (grpVolume.Text/grpReverb.Text) frissítése olcsó, ezért AZONNAL
    ' megtörténik minden változásnál - csak a tényleges MIDI-küldést
    ' korlátozza ez az időzítő, ugyanazt a mintát követve, mint a
    ' midiKijelzoIdozito a kijelzésnél. 50 ms (20 Hz): nincs technikai
    ' korlát, ami lassabb ütemre kényszerítene (16 üzenet kiküldése egy
    ' valódi MIDI porton is csak kb. 15-16 ms), és így a leggyorsabb
    ' csúszkahúzás is simának, élőnek érződik.
    Private WithEvents csuszkaMidiIdozito As New Timer With {.Interval = 50}
    Private alapHangeroKuldendo As Boolean = False
    Private reverbKuldendo As Boolean = False

    ' A Kezdet.json a FELHASZNÁLÓI mappában (%AppData%\MidiBill3) van, nem a
    ' program mappájában - lásd a Modellek\Utvonalak.vb részletes magyarázatát.
    Private ReadOnly kezdetFile As String = Utvonalak.KezdetFajl

    ' A BEJÖVŐ üzenetek feldolgozásához kellenek (pedál-inverzió, split pont
    ' tanulás, "Extra MIDI üzenetek" indítása) - ezek NEM a csatorna-állapot
    ' részei, ezért maradnak itt.
    Private Const CONTROLLER_CHANGE As Integer = &HB0
    Private Const DAMPER_PEDAL As Integer = 64
    Private Const NOTE_ON As Integer = &H90

    ' A csatorna-állapotot leíró CC-számokat (hangerő, tér, vibrato, chorus,
    ' reverb) SZÁNDÉKOSAN nem itt deklaráljuk: azok a
    ' Modellek\CsatornaUzenetek modulban vannak, egyetlen közös helyen, és
    ' innen CsatornaUzenetek.<NÉV> alakban hivatkozunk rájuk.

    Private Sub MainForm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' MINDEN más előtt: a felhasználói mappa (%AppData%\MidiBill3)
        ' létrehozása, és a régi helyen maradt Kezdet.json egyszeri
        ' átköltöztetése - e nélkül a HangszerListaBetoltese() már egy
        ' nem létező mappában keresne.
        Utvonalak.Elokeszites()

        HangszerListaBetoltese()
        HangszerPanelekLetrehozasa()
        CsatornaAllapotokInicializalasa()
        SplitPontFeltoltese()
        MidiEszkozokFeltoltese()

        inicializalasKesz = True
        CsatornaPanelekFrissitese()

        KezdetBeallitasokBetoltese()

        midiKijelzoIdozito.Start()
        csuszkaMidiIdozito.Start()
    End Sub

    ''' <summary>
    ''' Kilépéskori takarítás. FONTOS a sorrend: ELŐSZÖR a zarasFolyamatban
    ''' jelzőt állítjuk be és zárjuk le a MIDI BEMENETET, és csak utána jön a
    ''' hangok leállítása / mentés / Dispose - így a MIDI callback-szál már
    ''' nem próbál üzenetet átadni a UI szálnak, amíg az a lezárással
    ''' foglalkozik (lásd a midiMotor_MidiUzenetErkezett megjegyzését).
    '''
    ''' FIGYELEM a jövőbeli fejlesztéshez: ha ide később bekerül a kilépés
    ''' MEGSZAKÍTÁSA (pl. a tervezett MIDI felvevő ablak "van aktív felvétel,
    ''' leállítsuk?" kérdése e.Cancel = True esetén), akkor annak az
    ''' ellenőrzésnek az EGÉSZ alábbi takarítás ELÉ kell kerülnie - különben a
    ''' megszakított kilépés után a MIDI bemenet lezárva maradna.
    ''' </summary>
    Private Sub MainForm_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        zarasFolyamatban = True
        midiMotor.BemenetBezaras()
        midiKijelzoIdozito.Stop()
        csuszkaMidiIdozito.Stop()

        For c As Integer = 1 To 16
            midiMotor.HangokKikapcsolasa(c, True)
        Next

        If Not mentesKihagyasa Then
            Try
                AktualisBeallitasokOsszegyujtese().Mentes(kezdetFile)
            Catch ex As Exception
            End Try
        End If

        midiMotor.Dispose()
    End Sub

    ''' <summary>A 4 fizikai HangszerPanel és keret létrehozása - a végleges elrendezéshez igazítva</summary>
    Private Sub HangszerPanelekLetrehozasa()
        Dim xKoordinatak() As Integer = {25, 337}
        Dim yKoordinatak() As Integer = {53, 469}

        For i As Integer = 0 To 3
            Dim oszlop As Integer = i Mod 2
            Dim sor As Integer = i \ 2

            Dim keret As New GroupBox()
            keret.Location = New Point(xKoordinatak(oszlop), yKoordinatak(sor))
            keret.Size = New Size(300, 408)

            Dim panel As New HangszerPanel()
            panel.Location = New Point(10, 20)
            keret.Controls.Add(panel)

            Me.Controls.Add(keret)

            hangszerKeretek(i) = keret
            hangszerPanelek(i) = panel

            AddHandler panel.HangszerValtozott, AddressOf HangszerPanel_HangszerValtozott
            AddHandler panel.HangeroValtozott, AddressOf HangszerPanel_HangeroValtozott
            AddHandler panel.TerValtozott, AddressOf HangszerPanel_TerValtozott
            AddHandler panel.VibratoValtozott, AddressOf HangszerPanel_VibratoValtozott
            AddHandler panel.AktivValtozott, AddressOf HangszerPanel_AktivValtozott
            AddHandler panel.AlulValtozott, AddressOf HangszerPanel_AlulValtozott
            AddHandler panel.FelulValtozott, AddressOf HangszerPanel_FelulValtozott
            AddHandler panel.OktavValtozott, AddressOf HangszerPanel_OktavValtozott
            AddHandler panel.ChorusValtozott, AddressOf HangszerPanel_ChorusValtozott
        Next
    End Sub

    Private Sub CsatornaAllapotokInicializalasa()
        For i As Integer = 0 To 15
            csatornaAllapotok(i) = New CsatornaBeallitas()
        Next
        csatornaAllapotok(0).Aktiv = True
    End Sub

    Private Sub SplitPontFeltoltese()
        cmbSplitPont.Items.Clear()
        For szam As Integer = 0 To 127
            cmbSplitPont.Items.Add(Hangnevek.HangNev(szam) & " (" & szam & ")")
        Next
        cmbSplitPont.SelectedIndex = 60
    End Sub

    ''' <summary>
    ''' A GYÁRI hangszerlista (GM.lst/GM.drm) betöltése induláskor - ennek
    ''' A HangszerPanelekLetrehozasa ELŐTT kell lefutnia, mert a HangszerPanel
    ''' konstruktora azonnal a HangszerLista.Nevek alapján tölti fel a saját
    ''' cmbHangszer legördülőjét.
    ''' </summary>
    Private Sub HangszerListaBetoltese()
        Dim lstFajlNev As String = "GM.lst"

        ' FONTOS: a nyilvántartott lista-nevet is át kell állítani, nem csak
        ' a tényleges listát betölteni. Ezt a metódust ugyanis nem csak
        ' induláskor hívjuk, hanem az "Alaphelyzetbe állítás" is - enélkül
        ' ott az aktualisHangszerListaFajl a KORÁBBI (pl. SoundFontból
        ' importált) lista nevén maradt volna, aminek két következménye lett:
        ' (1) az ablak fejléce továbbra is a régi lista nevét mutatta,
        ' (2) ami sokkal komolyabb: kilépéskor ez a régi név mentődött a
        '     Kezdet.json-ba, így a KÖVETKEZŐ INDULÁSNÁL a program újra a
        '     régi listát töltötte be - vagyis az alaphelyzetbe állítás nem
        '     élte túl az újraindítást.
        ' Induláskor ez az értékadás nincs hatással (a mező amúgy is
        ' "GM.lst"-tel indul).
        aktualisHangszerListaFajl = lstFajlNev

        Dim utvonal As String = Utvonalak.HangszerListaOlvasasra(lstFajlNev)
        If Not HangszerLista.Betolt(utvonal) Then
            MsgBox("Nem sikerült betölteni a hangszerlistát innen:" & vbCrLf & utvonal & vbCrLf & vbCrLf &
                   "A beépített General MIDI lista kerül alkalmazásra.",
                   MsgBoxStyle.Exclamation, "MidiBill")
        End If

        Dim dobFajlNev As String = IO.Path.GetFileNameWithoutExtension(lstFajlNev) & ".drm"
        Dim dobUtvonal As String = Utvonalak.HangszerListaOlvasasra(dobFajlNev)
        If Not HangszerLista.BetoltDobKeszletet(dobUtvonal) Then
            MsgBox("Nem sikerült betölteni a dobkészlet-listát innen:" & vbCrLf & dobUtvonal & vbCrLf & vbCrLf &
                   "A beépített alapértelmezett dobkészlet-lista kerül alkalmazásra.",
                   MsgBoxStyle.Exclamation, "MidiBill")
        End If
    End Sub

    Private Sub MidiEszkozokFeltoltese()
        cmbMidiIn.Items.Clear()
        'For Each nev As String In New List(Of String)() ' TESZT: üres bemeneti lista szimulálása
        For Each nev As String In MidiEngine.BemenetiEszkozok()
            cmbMidiIn.Items.Add(nev)
        Next

        cmbMidiOut.Items.Clear()
        'For Each nev As String In New List(Of String)() ' TESZT: üres kimeneti lista szimulálása
        For Each nev As String In MidiEngine.KimenetiEszkozok()
            cmbMidiOut.Items.Add(nev)
        Next

        If cmbMidiIn.Items.Count = 0 Then
            MsgBox("Nem található csatlakoztatott MIDI bemeneti eszköz." & vbCrLf &
               "Csatlakoztass egy MIDI billentyűzetet, majd a ""MIDI eszközök frissítése"" gombbal töltsd be.",
               MsgBoxStyle.Exclamation, "MidiBill")
        Else
            cmbMidiIn.SelectedIndex = 0
        End If

        If cmbMidiOut.Items.Count = 0 Then
            MsgBox("Nem található csatlakoztatott MIDI kimeneti eszköz." & vbCrLf &
               "MIDI kimenet nélkül a program nem tud hangot megszólaltatni, ezért most kilép." & vbCrLf &
               "Csatlakoztass egy MIDI kimeneti eszközt, majd indítsd el újra a programot.",
               MsgBoxStyle.Critical, "MidiBill")
            Environment.Exit(1)
        Else
            cmbMidiOut.SelectedIndex = 0
        End If
    End Sub

    Private Sub btnMidiFrissites_Click(sender As Object, e As EventArgs) Handles btnMidiFrissites.Click
        MidiEszkozokUjratoltese()
    End Sub

    ''' <summary>
    ''' Újra lekérdezi a Windows-tól az aktuálisan csatlakoztatott MIDI eszközöket,
    ''' és frissíti a legördülő listákat - a "MIDI eszközök frissítése" gomb hívja.
    ''' Amennyire lehet, megtartja a jelenleg kiválasztott eszközt (ha az még
    ''' mindig szerepel a listában), hogy egy új eszköz bedugása ne zavarja meg
    ''' a már beállított MIDI In/Out kiválasztást.
    ''' </summary>
    Private Sub MidiEszkozokUjratoltese()
        Dim regiBemenet As String = If(cmbMidiIn.SelectedItem IsNot Nothing, cmbMidiIn.SelectedItem.ToString(), "")
        Dim regiKimenet As String = If(cmbMidiOut.SelectedItem IsNot Nothing, cmbMidiOut.SelectedItem.ToString(), "")

        cmbMidiIn.Items.Clear()
        For Each nev As String In MidiEngine.BemenetiEszkozok()
            cmbMidiIn.Items.Add(nev)
        Next

        cmbMidiOut.Items.Clear()
        For Each nev As String In MidiEngine.KimenetiEszkozok()
            cmbMidiOut.Items.Add(nev)
        Next

        If cmbMidiIn.Items.Count = 0 Then
            MsgBox("MIDI bemenet nem található.", MsgBoxStyle.Exclamation, "MidiBill")
            Return
        End If

        If cmbMidiOut.Items.Count = 0 Then
            MsgBox("MIDI kimenet nem található.", MsgBoxStyle.Exclamation, "MidiBill")
            Return
        End If

        Dim ujBemenetIndex As Integer = cmbMidiIn.Items.IndexOf(regiBemenet)
        cmbMidiIn.SelectedIndex = If(ujBemenetIndex >= 0, ujBemenetIndex, 0)

        Dim ujKimenetIndex As Integer = cmbMidiOut.Items.IndexOf(regiKimenet)
        cmbMidiOut.SelectedIndex = If(ujKimenetIndex >= 0, ujKimenetIndex, 0)

        MsgBox("MIDI In/Out eszközlista sikeresen frissítve.", MsgBoxStyle.Information, "MidiBill")
    End Sub

    ''' <summary>
    ''' A Kezdet.json beolvasása induláskor. FONTOS: ha a fájl NEM létezik
    ''' (első indítás friss telepítés/build után), vagy a beolvasása hibára
    ''' fut, akkor is ki KELL küldeni az induló állapotot az
    ''' OsszesCsatornaUjrakuldese()-vel - e nélkül a MidiEngine belső
    ''' csatorna-állapotai (Aktiv/Alul/Felul/Oktav) végig az alapértelmezett
    ''' False/0 értéken maradnának, mivel a csatornaAllapotok tömböt és a
    ''' panelek property-jeit sem az inicializálás, sem a
    ''' CsatornaPanelekFrissitese() nem szinkronizálja a motorba (a panel
    ''' property-k szándékosan nem tüzelnek eseményt betöltéskor). Ilyenkor a
    ''' MidiEngine MINDEN csatornát inaktívnak látna, és a program némán
    ''' eldobná a lejátszott hangokat.
    ''' </summary>
    Private Sub KezdetBeallitasokBetoltese()
        If Not IO.File.Exists(kezdetFile) Then
            OsszesCsatornaUjrakuldese()
            CimsorFrissitese(kezdetFile)
            Return
        End If

        Try
            Dim b As Beallitasok = Beallitasok.Betoltes(kezdetFile)
            BeallitasokAlkalmazasa(b, True)   ' ez a végén maga is újraküld mindent
            CimsorFrissitese(kezdetFile)
        Catch ex As Exception
            ' A betöltés félbeszakadt - a beállítások részben alkalmazódhattak,
            ' ezért itt is kiküldjük a jelenlegi (rész-)állapotot, hogy a
            ' MidiEngine és a szintetizátor biztosan szinkronban legyen a UI-jal.
            OsszesCsatornaUjrakuldese()
            MsgBox("Nem sikerült betölteni a korábban elmentett beállításokat." & vbCrLf &
                   "Hiba: " & ex.Message, MsgBoxStyle.Exclamation, "MidiBill")
        End Try
    End Sub

    ' ============================================================
    ' MIDI eszközök megnyitása/váltása
    ' ============================================================

    Private Sub cmbMidiIn_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbMidiIn.SelectedIndexChanged
        If cmbMidiIn.SelectedIndex < 0 Then Return

        Dim frissLista As List(Of String) = MidiEngine.BemenetiEszkozok()
        If Not EszkozListaEgyezikE(cmbMidiIn, frissLista) Then
            MsgBox("A MIDI bemeneti eszközök listája időközben megváltozott (pl. egy" & vbCrLf &
               "eszközt kihúztak vagy csatlakoztattak), ezért frissítem a MIDI In/Out listát." & vbCrLf &
               "A korábbi kiválasztás visszaáll, ha az eszköz még elérhető, " &
               "egyébként az első elérhető eszköz kerül kiválasztásra.",
               MsgBoxStyle.Information, "MidiBill")
            MidiEszkozokUjratoltese()
            ActiveControl = Nothing
            Return
        End If

        Try
            midiMotor.BemenetMegnyitas(cmbMidiIn.SelectedIndex)
        Catch ex As Exception
            MsgBox("Nem sikerült megnyitni a kiválasztott MIDI bemeneti eszközt." & vbCrLf &
               "Lehet, hogy időközben eltávolították, vagy más program használja." & vbCrLf & vbCrLf &
               "Hiba: " & ex.Message & vbCrLf & vbCrLf &
               "Frissítem a MIDI eszközlistát.", MsgBoxStyle.Exclamation, "MidiBill")
            MidiEszkozokUjratoltese()
        End Try

        ActiveControl = Nothing
    End Sub

    Private Sub cmbMidiOut_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbMidiOut.SelectedIndexChanged
        If cmbMidiOut.SelectedIndex < 0 Then Return

        Dim frissLista As List(Of String) = MidiEngine.KimenetiEszkozok()
        If Not EszkozListaEgyezikE(cmbMidiOut, frissLista) Then
            MsgBox("A MIDI kimeneti eszközök listája időközben megváltozott (pl. egy" & vbCrLf &
               "eszközt kihúztak vagy csatlakoztattak), ezért frissítem a MIDI In/Out listát." & vbCrLf &
               "A korábbi kiválasztás visszaáll, ha az eszköz még elérhető, " &
               "egyébként az első elérhető eszköz kerül kiválasztásra.",
               MsgBoxStyle.Information, "MidiBill")
            MidiEszkozokUjratoltese()
            ActiveControl = Nothing
            Return
        End If

        Try
            midiMotor.KimenetMegnyitas(cmbMidiOut.SelectedIndex)
            OsszesCsatornaUjrakuldese()
        Catch ex As Exception
            MsgBox("Nem sikerült megnyitni a kiválasztott MIDI kimeneti eszközt." & vbCrLf &
               "Lehet, hogy időközben eltávolították, vagy más program használja." & vbCrLf & vbCrLf &
               "Hiba: " & ex.Message & vbCrLf & vbCrLf &
               "Frissítem a MIDI eszközlistát.", MsgBoxStyle.Exclamation, "MidiBill")
            MidiEszkozokUjratoltese()
        End Try

        ActiveControl = Nothing
    End Sub

    ''' <summary>
    ''' Ellenőrzi, hogy a ComboBox jelenlegi tartalma (sorrendben) pontosan
    ''' megegyezik-e a Windows-tól frissen lekérdezett eszközlistával. Ha nem,
    ''' az azt jelenti, hogy időközben eszköz csatlakozott/leválasztódott, és a
    ''' jelenlegi kiválasztás (index) már nem biztos, hogy a várt eszközre mutat.
    ''' </summary>
    Private Function EszkozListaEgyezikE(cmb As ComboBox, frissLista As List(Of String)) As Boolean
        If cmb.Items.Count <> frissLista.Count Then Return False
        For i As Integer = 0 To cmb.Items.Count - 1
            If cmb.Items(i).ToString() <> frissLista(i) Then Return False
        Next
        Return True
    End Function

    Private Sub OsszesCsatornaUjrakuldese()
        If Not inicializalasKesz Then Return

        midiMotor.SplitPont = aktualisSplitPont
        midiMotor.Transpozicio = aktualisTranszponalas

        For i As Integer = 0 To 15
            Dim csatornaSzam As Integer = i + 1
            Dim adat As CsatornaBeallitas = csatornaAllapotok(i)
            midiMotor.CsatornaAktiv(i) = adat.Aktiv
            midiMotor.CsatornaAlul(i) = adat.Alul
            midiMotor.CsatornaFelul(i) = adat.Felul
            midiMotor.CsatornaOktav(i) = adat.Oktav
            midiMotor.ProgramValtas(csatornaSzam, adat.Hangszer)
            CsatornaHangeroKuldes(csatornaSzam, adat.Hangero)
            midiMotor.ControllerValtas(csatornaSzam, CsatornaUzenetek.PAN, adat.Ter + 64)
            midiMotor.ControllerValtas(csatornaSzam, CsatornaUzenetek.MOD_WHEEL, adat.Vibrato)
            midiMotor.ControllerValtas(csatornaSzam, CsatornaUzenetek.CHORUS, adat.Chorus)
        Next

        ReverbKuldes(trkReverb.Value)
    End Sub

    ' ============================================================
    ' Csatornacsoport váltás (1-4 / 5-8 / 9-12 / 13-16)
    ' ============================================================

    Private Sub radCsoport1_CheckedChanged(sender As Object, e As EventArgs) Handles radCsoport1.CheckedChanged
        If radCsoport1.Checked Then CsoportValtas(0)
    End Sub

    Private Sub radCsoport2_CheckedChanged(sender As Object, e As EventArgs) Handles radCsoport2.CheckedChanged
        If radCsoport2.Checked Then CsoportValtas(1)
    End Sub

    Private Sub radCsoport3_CheckedChanged(sender As Object, e As EventArgs) Handles radCsoport3.CheckedChanged
        If radCsoport3.Checked Then CsoportValtas(2)
    End Sub

    Private Sub radCsoport4_CheckedChanged(sender As Object, e As EventArgs) Handles radCsoport4.CheckedChanged
        If radCsoport4.Checked Then CsoportValtas(3)
    End Sub

    Private Sub CsoportValtas(ujCsoportIndex As Integer)
        If Not inicializalasKesz Then Return
        aktivCsoportIndex = ujCsoportIndex
        CsatornaPanelekFrissitese()
    End Sub
    ''' <summary>A csatorna-keret feliratának összeállítása - a 10. (dob) csatornánál "(dob)" jelzéssel kiegészítve</summary>
    Private Function CsatornaFelirat(csatornaIndex As Integer, aktiv As Boolean) As String
        Dim alapNev As String = (csatornaIndex + 1).ToString() & ". csatorna"
        If csatornaIndex = DOB_CSATORNA_INDEX Then
            Return If(aktiv, alapNev & " (dob): Aktív", alapNev & " (dob)")
        Else
            Return If(aktiv, alapNev & ": Aktív", alapNev)
        End If
    End Function

    Private Sub CsatornaPanelekFrissitese()
        For i As Integer = 0 To 3
            Dim csatornaIndex As Integer = aktivCsoportIndex * 4 + i
            Dim csatornaSzam As Integer = csatornaIndex + 1
            Dim adat As CsatornaBeallitas = csatornaAllapotok(csatornaIndex)
            Dim panel As HangszerPanel = hangszerPanelek(i)
            Dim keret As GroupBox = hangszerKeretek(i)

            Dim dobCsatorna As Boolean = (csatornaIndex = DOB_CSATORNA_INDEX)
            panel.HangszerListaForrasBeallitasa(If(dobCsatorna, HangszerLista.DobNevek, Nothing))

            panel.Hangszer = adat.Hangszer
            panel.Hangero = adat.Hangero
            panel.Ter = adat.Ter
            panel.Vibrato = adat.Vibrato
            panel.Aktiv = adat.Aktiv
            panel.Alul = adat.Alul
            panel.Felul = adat.Felul
            panel.Oktav = adat.Oktav
            panel.Chorus = adat.Chorus

            keret.Text = CsatornaFelirat(csatornaIndex, adat.Aktiv)
        Next
    End Sub

    ' ============================================================
    ' HangszerPanel események -> MIDI küldés
    ' ============================================================

    Private Function CsatornaIndexTol(sender As Object) As Integer
        Dim panel As HangszerPanel = TryCast(sender, HangszerPanel)
        If panel Is Nothing Then Return -1
        Dim panelPozicio As Integer = Array.IndexOf(hangszerPanelek, panel)
        If panelPozicio < 0 Then Return -1
        Return aktivCsoportIndex * 4 + panelPozicio
    End Function

    Private Sub HangszerPanel_HangszerValtozott(sender As Object, patchIndex As Integer)
        Dim idx As Integer = CsatornaIndexTol(sender)
        If idx < 0 Then Return
        csatornaAllapotok(idx).Hangszer = patchIndex
        midiMotor.ProgramValtas(idx + 1, patchIndex)
    End Sub

    Private Sub HangszerPanel_HangeroValtozott(sender As Object, ertek As Integer)
        Dim idx As Integer = CsatornaIndexTol(sender)
        If idx < 0 Then Return
        csatornaAllapotok(idx).Hangero = ertek
        CsatornaHangeroKuldes(idx + 1, ertek)
    End Sub

    Private Sub HangszerPanel_TerValtozott(sender As Object, ertek As Integer)
        Dim idx As Integer = CsatornaIndexTol(sender)
        If idx < 0 Then Return
        csatornaAllapotok(idx).Ter = ertek
        midiMotor.ControllerValtas(idx + 1, CsatornaUzenetek.PAN, ertek + 64)
    End Sub

    Private Sub HangszerPanel_VibratoValtozott(sender As Object, ertek As Integer)
        Dim idx As Integer = CsatornaIndexTol(sender)
        If idx < 0 Then Return
        csatornaAllapotok(idx).Vibrato = ertek
        midiMotor.ControllerValtas(idx + 1, CsatornaUzenetek.MOD_WHEEL, ertek)
    End Sub

    Private Sub HangszerPanel_AktivValtozott(sender As Object, aktiv As Boolean)
        Dim idx As Integer = CsatornaIndexTol(sender)
        If idx < 0 Then Return

        csatornaAllapotok(idx).Aktiv = aktiv

        Dim panel As HangszerPanel = CType(sender, HangszerPanel)
        Dim panelPozicio As Integer = Array.IndexOf(hangszerPanelek, panel)
        If panelPozicio >= 0 Then
            hangszerKeretek(panelPozicio).Text = CsatornaFelirat(idx, aktiv)
        End If

        Dim csatornaSzam As Integer = idx + 1
        midiMotor.CsatornaAktiv(idx) = aktiv

        If aktiv Then
            ' A csatorna teljes állapotát (hangszín, hangerő, tér, vibrato,
            ' chorus, reverb) ÚJRA kiküldjük az aktiválásnál - MIELŐTT a
            ' lenyomva tartott hangok megszólalnának rajta.
            '
            ' ÉLŐBEN ez látszólag felesleges, mert a szintetizátor már
            ' induláskor megkapta MIND a 16 csatorna beállítását (lásd
            ' OsszesCsatornaUjrakuldese, ami szándékosan nem szűr
            ' aktivitásra) - ezért működött eddig is helyesen a hang.
            ' A MIDI FELVÉTELBEN viszont csak az szerepel, ami a felvétel
            ' INDULÁSA UTÁN ténylegesen kiment: egy menet közben aktivált
            ' csatorna hangjai bekerültek a felvételbe, a hangszíne viszont
            ' nem, így egy külső lejátszóban az a csatorna alapértelmezett
            ' zongorával szólalt meg. Ez a küldés zárja be ezt a rést -
            ' és élőben is helyesebb, mert az aktiválás így minden esetben
            ' érvényesíti a csatorna beállításait.
            CsatornaAllapotKuldese(idx)
            midiMotor.LenyomottHangokMegszolaltatasa(csatornaSzam)
        Else
            midiMotor.HangokKikapcsolasa(csatornaSzam)
        End If
    End Sub

    ''' <summary>
    ''' EGY csatorna teljes MIDI-állapotának kiküldése. Az üzenetsorozatot a
    ''' közös Modellek\CsatornaUzenetek modul állítja elő - ugyanaz, amit az
    ''' "Extra MIDI üzenetek" funkció mind az aktív csatornákra kiküld, és
    ''' amit a felvevő a mentett fájl elejére ír.
    ''' </summary>
    Private Sub CsatornaAllapotKuldese(csatornaIndex As Integer)
        Dim uzenetek As List(Of MidiUzenet) = CsatornaUzenetek.CsatornaAllapotUzenetei(
            csatornaIndex, csatornaAllapotok(csatornaIndex), trkAlapHangero.Value, trkReverb.Value)

        For Each uzenet As MidiUzenet In uzenetek
            midiMotor.KozvetlenUzenetKuldese(uzenet.StatuszBajt, uzenet.Adat1, uzenet.Adat2)
        Next
    End Sub

    Private Sub HangszerPanel_AlulValtozott(sender As Object, aktiv As Boolean)
        Dim idx As Integer = CsatornaIndexTol(sender)
        If idx < 0 Then Return
        csatornaAllapotok(idx).Alul = aktiv
        midiMotor.AlulValtas(idx + 1, aktiv)
    End Sub

    Private Sub HangszerPanel_FelulValtozott(sender As Object, aktiv As Boolean)
        Dim idx As Integer = CsatornaIndexTol(sender)
        If idx < 0 Then Return
        csatornaAllapotok(idx).Felul = aktiv
        midiMotor.FelulValtas(idx + 1, aktiv)
    End Sub

    ''' <summary>Csatornánkénti oktáv-eltolás változott - a MidiEngine azonnali, retroaktív hatással alkalmazza</summary>
    Private Sub HangszerPanel_OktavValtozott(sender As Object, ertek As Integer)
        Dim idx As Integer = CsatornaIndexTol(sender)
        If idx < 0 Then Return
        csatornaAllapotok(idx).Oktav = ertek
        midiMotor.OktavValtas(idx + 1, ertek)
    End Sub

    Private Sub HangszerPanel_ChorusValtozott(sender As Object, ertek As Integer)
        Dim idx As Integer = CsatornaIndexTol(sender)
        If idx < 0 Then Return
        csatornaAllapotok(idx).Chorus = ertek
        midiMotor.ControllerValtas(idx + 1, CsatornaUzenetek.CHORUS, ertek)
    End Sub

    Private Sub CsatornaHangeroKuldes(csatornaSzam As Integer, csatornaErtek As Integer)
        Dim vegsoErtek As Integer = CInt(csatornaErtek * (trkAlapHangero.Value / 100.0))
        midiMotor.ControllerValtas(csatornaSzam, CsatornaUzenetek.MAIN_VOLUME, vegsoErtek)
    End Sub

    ' ============================================================
    ' Globális hangerő
    ' ============================================================

    Private Sub trkAlapHangero_ValueChanged(sender As Object, e As EventArgs) Handles trkAlapHangero.ValueChanged
        If Not inicializalasKesz Then Return

        grpVolume.Text = "Alap hangerő: " & trkAlapHangero.Value & "%"
        alapHangeroKuldendo = True
    End Sub

    Private Sub trkReverb_ValueChanged(sender As Object, e As EventArgs) Handles trkReverb.ValueChanged
        If Not inicializalasKesz Then Return

        grpReverb.Text = "Visszhang: " & trkReverb.Value & "%"
        reverbKuldendo = True
    End Sub

    ''' <summary>
    ''' Az Alap hangerő és a Reverb csúszka húzása közben jelzett, még
    ''' el nem küldött MIDI-állapotot küldi ki ténylegesen - max. 20x/mp
    ''' (lásd csuszkaMidiIdozito.Interval), a csúszka aktuális értékét
    ''' olvasva ki minden alkalommal (nem a jelzés pillanatában rögzített
    ''' értéket), így mindig a legfrissebb állapot megy ki.
    ''' </summary>
    Private Sub csuszkaMidiIdozito_Tick(sender As Object, e As EventArgs) Handles csuszkaMidiIdozito.Tick
        If alapHangeroKuldendo Then
            alapHangeroKuldendo = False
            For i As Integer = 0 To 15
                CsatornaHangeroKuldes(i + 1, csatornaAllapotok(i).Hangero)
            Next
        End If

        If reverbKuldendo Then
            reverbKuldendo = False
            ReverbKuldes(trkReverb.Value)
        End If
    End Sub

    ''' <summary>
    ''' A Reverb (0-100%) átszámítása MIDI CC91 értékre (0-127, a teljes
    ''' MIDI-tartomány kihasználásához), és kiküldése MIND a 16 csatornára -
    ''' ez egy globális, nem csatornánkénti beállítás, ezért az aktiváltságtól
    ''' függetlenül mindig minden csatornára megy, ugyanúgy, ahogy a
    ''' Hangszer/Hangerő/Tér/Vibrato is (lásd OsszesCsatornaUjrakuldese).
    ''' </summary>
    Private Sub ReverbKuldes(szazalek As Integer)
        Dim midiErtek As Integer = szazalek * 127 \ 100
        For c As Integer = 1 To 16
            midiMotor.ControllerValtas(c, CsatornaUzenetek.REVERB, midiErtek)
        Next
    End Sub

    ' ============================================================
    ' Split pont
    ' ============================================================

    Private Sub cmbSplitPont_SelectedIndexChanged(sender As Object, e As EventArgs) Handles cmbSplitPont.SelectedIndexChanged
        If Not inicializalasKesz Then Return
        If cmbSplitPont.SelectedIndex < 0 Then Return

        aktualisSplitPont = cmbSplitPont.SelectedIndex
        midiMotor.SplitPont = aktualisSplitPont
        grpSplit.Text = "Split pont: " & Hangnevek.HangNev(aktualisSplitPont)
        lblSplitInfo.Text = "Beállítva: " & Hangnevek.HangNev(aktualisSplitPont)

        ActiveControl = Nothing
    End Sub

    Private Sub btnSplitTanulas_Click(sender As Object, e As EventArgs) Handles btnSplitTanulas.Click
        splitTanulasAktiv = True
        lblSplitInfo.Text = "Várakozás billentyűre..."
    End Sub

    Private Sub SplitPontBeallitasaTanulassal(hang As Integer)
        splitTanulasAktiv = False
        cmbSplitPont.SelectedIndex = hang
        lblSplitInfo.Text = "Beállítva: " & Hangnevek.HangNev(hang)
    End Sub

    ' ============================================================
    ' Globális transzponálás
    ' ============================================================

    Private Function ElojelesSzoveg(ertek As Integer) As String
        Return If(ertek > 0, "+" & ertek, ertek.ToString())
    End Function

    Private Sub TranszponalasFrissites(ujErtek As Integer)
        aktualisTranszponalas = Korlatoz(ujErtek, TRANSZPONALAS_MIN, TRANSZPONALAS_MAX)
        midiMotor.Transpozicio = aktualisTranszponalas
        grpTranspose.Text = "Transzponálás: " & ElojelesSzoveg(aktualisTranszponalas)
    End Sub

    Private Sub btnFelhangFel_Click(sender As Object, e As EventArgs) Handles btnFelhangFel.Click
        TranszponalasFrissites(aktualisTranszponalas + 1)
    End Sub

    Private Sub btnFelhangLe_Click(sender As Object, e As EventArgs) Handles btnFelhangLe.Click
        TranszponalasFrissites(aktualisTranszponalas - 1)
    End Sub

    Private Sub btnOktavFel_Click(sender As Object, e As EventArgs) Handles btnOktavFel.Click
        TranszponalasFrissites(aktualisTranszponalas + 12)
    End Sub

    Private Sub btnOktavLe_Click(sender As Object, e As EventArgs) Handles btnOktavLe.Click
        TranszponalasFrissites(aktualisTranszponalas - 12)
    End Sub

    ' ============================================================
    ' Bejövő MIDI üzenetek fogadása (NAudio háttérszáláról!)
    ' ============================================================

    ''' <summary>
    ''' A NAudio callback-száláról érkező MIDI üzenet átadása a UI szálnak.
    ''' SZÁNDÉKOSAN BeginInvoke (aszinkron), NEM Invoke: az Invoke blokkolná a
    ''' MIDI callback-szálat addig, amíg a UI szál fel nem dolgozza az üzenetet.
    ''' Ez két bajt okozna: (1) minden egyes leütés a UI szál terheltségének
    ''' megfelelő késleltetést kapna, (2) kilépéskor kölcsönös várakozás
    ''' (deadlock) alakulhatna ki, mert a UI szál a FormClosing-ban a MIDI
    ''' bemenet lezárására vár, miközben a callback-szál a UI szálra várna.
    ''' A BeginInvoke sorrendtartó (a Windows üzenetsor FIFO), tehát a hangok
    ''' sorrendje továbbra is garantált.
    '''
    ''' Az idobelyegMs paramétert (a jövőbeli MIDI-felvétel funkció
    ''' ELŐKÉSZÍTÉSE) a MidiEngine már a callback-szálon, az UI-ra való
    ''' átadás ELŐTT rögzítette (lásd MidiEngine.Bemenet_UzenetErkezett) - itt
    ''' SZÁNDÉKOSAN a BeginInvoke lambda ÁLTAL ELCSÍPETT (captured) helyi
    ''' paraméterként adjuk tovább, NEM egy megosztott (shared) mezőn
    ''' keresztül: mivel a callback-szál a UI szál feldolgozásától
    ''' függetlenül, gyorsabban is küldhet újabb üzeneteket, egy megosztott
    ''' mező tartalmát egy KÉSŐBBI üzenet felülírhatná, mire a KORÁBBAN
    ''' sorba állított BeginInvoke lambda lefutna a UI szálon - ez hibás
    ''' (a valóságosnál későbbi) időbélyeget eredményezne.
    ''' </summary>
    Private Sub midiMotor_MidiUzenetErkezett(uzenet As Integer, adat1 As Integer, adat2 As Integer, idobelyegMs As Long) Handles midiMotor.MidiUzenetErkezett
        If zarasFolyamatban Then Return

        Try
            If InvokeRequired Then
                BeginInvoke(Sub() MidiUzenetFeldolgozasa(uzenet, adat1, adat2, idobelyegMs))
            Else
                MidiUzenetFeldolgozasa(uzenet, adat1, adat2, idobelyegMs)
            End If
        Catch ex As Exception
            ' Az ablak épp bezáródik (megszűnt ablak-leíró) - ilyenkor a még
            ' "úton lévő" MIDI üzeneteket egyszerűen eldobjuk, ez nem hiba.
        End Try
    End Sub

    ''' <summary>
    ''' A bejövő MIDI üzenet feldolgozása (pedál-inverzió, split pont tanulás,
    ''' "Extra MIDI üzenetek"), majd továbbadása a MidiEngine-nek.
    '''
    ''' Az idobelyegMs (a NAudio callback-szálán, MÉG a UI-ra való átadás
    ''' előtt rögzített, jittermentes időpont) a UzenetKuldes-en keresztül
    ''' végig át van vezetve a MidiEngine kimeneti pontjáig - így a MIDI
    ''' felvevő a hangokat a TÉNYLEGES beérkezési idejükkel rögzíti, nem a
    ''' UI szál terheltségétől függő, késleltetett idővel.
    ''' </summary>
    Private Sub MidiUzenetFeldolgozasa(uzenet As Integer, adat1 As Integer, adat2 As Integer, idobelyegMs As Long)
        Dim vegsoAdat2 As Integer = adat2

        ' FONTOS: az üzenet TÍPUSÁT mindig a státuszbájt FELSŐ négy bitjéből
        ' kell meghatározni - az alsó négy bit a KÜLDŐ MIDI csatorna sorszáma.
        ' Enélkül az alábbi vizsgálatok csak akkor teljesülnének, ha a
        ' billentyűzet történetesen az 1. csatornán ad (&H90 / &HB0); egy 2.
        ' csatornára állított hangszernél (&H91 / &HB1) a pedál-inverzió, a
        ' split pont tanulás és az Extra MIDI üzenetek is némán kimaradna.
        Dim uzenetTipus As Integer = uzenet And &HF0

        If uzenetTipus = CONTROLLER_CHANGE AndAlso adat1 = DAMPER_PEDAL AndAlso chkPedal.Checked Then
            vegsoAdat2 = 127 - adat2
        End If

        utolsoMidiUzenetStatusz = uzenet
        utolsoMidiAdat1 = adat1
        utolsoMidiAdat2 = vegsoAdat2
        midiSzovegValtozott = True

        If splitTanulasAktiv AndAlso uzenetTipus = NOTE_ON AndAlso vegsoAdat2 > 0 Then
            SplitPontBeallitasaTanulassal(adat1)
            Return
        End If

        If chkMidiBeallitoUzenetek.Checked AndAlso uzenetTipus = NOTE_ON AndAlso vegsoAdat2 > 0 Then
            AktivCsatornakAllapotanakKuldese()
            chkMidiBeallitoUzenetek.Checked = False
            lblExtraMidiAllapot.Text = "MIDI üzenetek kiküldve."
        End If

        midiMotor.UzenetKuldes(uzenet, adat1, vegsoAdat2, idobelyegMs)
    End Sub

    Private Sub chkMidiBeallitoUzenetek_CheckedChanged(sender As Object, e As EventArgs) Handles chkMidiBeallitoUzenetek.CheckedChanged
        If chkMidiBeallitoUzenetek.Checked Then
            lblExtraMidiAllapot.Text = "Várakozás billentyűre..."
        Else
            lblExtraMidiAllapot.Text = "Ha bekapcsolt, az aktív csatorna beállítások kiküldésre kerülnek"
        End If
    End Sub

    ''' <summary>
    ''' Az AKTÍV csatornák teljes MIDI-állapotát (hangszín, hangerő, tér,
    ''' vibrato, chorus, reverb) újraküldi - a "MIDI beállító üzenetek
    ''' küldése" checkbox bepipálása után, a következő valódi Note On
    ''' előtt fut le, EGYETLEN egyszer (utána a checkbox automatikusan
    ''' visszakapcsol). Célja, hogy egy külső MIDI-felvevő program is
    ''' megkapja a teljes csatorna-beállítást (pl. hangszín), nem csak a
    ''' ténylegesen lejátszott hangokat. Az inaktív csatornákra
    ''' szándékosan nem küld semmit.
    ''' </summary>
    Private Sub AktivCsatornakAllapotanakKuldese()
        For Each uzenet As MidiUzenet In AktivCsatornakAllapotUzenetei()
            midiMotor.KozvetlenUzenetKuldese(uzenet.StatuszBajt, uzenet.Adat1, uzenet.Adat2)
        Next
    End Sub

    ''' <summary>
    ''' Az AKTÍV csatornák teljes MIDI-állapota kész üzenetsorozatként. Két
    ''' helyen használjuk: az "Extra MIDI üzenetek" funkció ÉLŐBEN kiküldi
    ''' (AktivCsatornakAllapotanakKuldese), a MIDI felvevő pedig a felvétel
    ''' indulásakor pillanatképként elteszi, hogy a mentett .mid fájl elejére
    ''' írhassa - így a felvétel egy külső lejátszóban is a helyes
    ''' hangszínekkel, hangerővel és effektekkel szólal meg.
    '''
    ''' Az üzenetsorozatot a KÖZÖS Modellek\CsatornaUzenetek modul állítja
    ''' elő, ugyanaz, amit a "MIDI fájlok módosítása" segédprogram is a
    ''' fájlokba ír - egyetlen helyen karbantartva.
    ''' </summary>
    Private Function AktivCsatornakAllapotUzenetei() As List(Of MidiUzenet)
        Return CsatornaUzenetek.AktivCsatornakAllapotUzenetei(
            csatornaAllapotok, trkAlapHangero.Value, trkReverb.Value)
    End Function

    ''' <summary>
    ''' A MIDI-üzenet kijelzés tényleges frissítése, max. 5x/mp ütemben
    ''' (a midiKijelzoIdozito Interval=200 beállítása szerint) - csak akkor
    ''' ír, ha ténylegesen érkezett új üzenet az utolsó tikk óta.
    ''' </summary>
    Private Sub midiKijelzoIdozito_Tick(sender As Object, e As EventArgs) Handles midiKijelzoIdozito.Tick
        If midiSzovegValtozott Then
            lblMidiUzenet.Text = "MIDI üzenetek: " & MidiUzenetSzoveg(utolsoMidiUzenetStatusz, utolsoMidiAdat1, utolsoMidiAdat2)
            midiSzovegValtozott = False
        End If
    End Sub

    ' ============================================================
    ' Kimeneti hiba jelzése
    ' ============================================================

    Private Sub midiMotor_KimenetiHiba(hibaUzenet As String) Handles midiMotor.KimenetiHiba
        If InvokeRequired Then
            Invoke(Sub() MsgBox(hibaUzenet, MsgBoxStyle.Exclamation, "MidiBill"))
        Else
            MsgBox(hibaUzenet, MsgBoxStyle.Exclamation, "MidiBill")
        End If
    End Sub

    ' ============================================================
    ' Pánikgomb
    ' ============================================================

    Private Sub btnReset_Click(sender As Object, e As EventArgs) Handles btnReset.Click
        For c As Integer = 1 To 16
            midiMotor.HangokKikapcsolasa(c, True)
        Next
    End Sub

    ' ============================================================
    ' Kilépés
    ' ============================================================

    Private Sub btnKilepes_Click(sender As Object, e As EventArgs) Handles btnKilepes.Click
        Close()
    End Sub

    Private Sub mnuKilepes_Click(sender As Object, e As EventArgs) Handles mnuKilepes.Click
        Close()
    End Sub

    Private Sub mnuKilepesMentesNelkul_Click(sender As Object, e As EventArgs) Handles mnuKilepesMentesNelkul.Click
        Dim valasz As MsgBoxResult = MsgBox("Biztosan mentés nélkül szeretnél kilépni?" & vbCrLf &
                                             "A jelenlegi beállítások elvesznek, legközelebb a korábban elmentett állapot töltődik be.",
                                             MsgBoxStyle.YesNo Or MsgBoxStyle.Question, "MidiBill - Kilépés mentés nélkül")
        If valasz = MsgBoxResult.Yes Then
            mentesKihagyasa = True
            Close()
        End If
    End Sub

    ' ============================================================
    ' Névjegy
    ' ============================================================

    Private Sub mnuNevjegy_Click(sender As Object, e As EventArgs) Handles mnuNevjegy.Click
        MsgBox("MidiBill3 2026, " & "Verzió: " & AppInfo.AppVerzio() & vbCrLf & vbCrLf &
           "MIDI billentyűzet megszólaltatása 16 csatornán." & vbCrLf &
           "(A 10. csatorna dedikált dobkészlet-listát használ)" & vbCrLf &
           ".NET 10 / WinForms port a VB6-os eredetiből.",
           MsgBoxStyle.Information, "Névjegy - MidiBill3")
    End Sub

    ' ============================================================
    ' Fájl mentés / betöltés (JSON) + régi .mb3 importálás
    ' ============================================================

    Private Function AktualisBeallitasokOsszegyujtese() As Beallitasok
        Dim b As New Beallitasok With {
            .Verzio = AppInfo.AppVerzio()
        }

        For i As Integer = 0 To 15
            Dim cs As New CsatornaBeallitas()

            Dim forras As CsatornaBeallitas = csatornaAllapotok(i)
            cs.Hangszer = forras.Hangszer
            cs.Hangero = forras.Hangero
            cs.Ter = forras.Ter
            cs.Vibrato = forras.Vibrato
            cs.Chorus = forras.Chorus
            cs.Aktiv = forras.Aktiv
            cs.Alul = forras.Alul
            cs.Felul = forras.Felul
            cs.Oktav = forras.Oktav
            b.Csatornak.Add(cs)
        Next

        b.AlapHangero = trkAlapHangero.Value
        b.MidiBemenetNev = If(cmbMidiIn.SelectedItem IsNot Nothing, cmbMidiIn.SelectedItem.ToString(), "")
        b.MidiKimenetNev = If(cmbMidiOut.SelectedItem IsNot Nothing, cmbMidiOut.SelectedItem.ToString(), "")
        b.PedalInvertalva = chkPedal.Checked
        b.SplitPont = aktualisSplitPont
        b.Transzponalas = aktualisTranszponalas
        b.Reverb = trkReverb.Value
        b.HangszerListaFajl = aktualisHangszerListaFajl

        Return b
    End Function

    ''' <summary>
    ''' Egy betöltött/importált Beallitasok alkalmazása a UI-ra és a MIDI-re.
    ''' frissitsdEszkozoketIs: JSON-betöltésnél True (van eszköznév-adat),
    ''' .mb3 importnál False (a régi fájl eszközindexei nem megbízhatóak).
    ''' A folyamat végén figyelmeztet, ha a fájl hibás/hiányzó MIDI eszköznevet
    ''' vagy tartományon kívüli, korrigált értéket tartalmazott.
    ''' </summary>
    Private Sub BeallitasokAlkalmazasa(b As Beallitasok, frissitsdEszkozoketIs As Boolean)
        Dim ertekKorrigalva As Boolean = False

        For i As Integer = 0 To 15
            Dim adat As New CsatornaBeallitas()

            Dim cs As CsatornaBeallitas = b.Csatornak(i)

            Dim ujHangszer As Integer = Korlatoz(cs.Hangszer, 0, 16383)
            Dim ujHangero As Integer = Korlatoz(cs.Hangero, 0, 127)
            Dim ujTer As Integer = Korlatoz(cs.Ter, -64, 63)
            Dim ujVibrato As Integer = Korlatoz(cs.Vibrato, 0, 127)
            Dim ujChorus As Integer = Korlatoz(cs.Chorus, 0, 127)
            Dim ujOktav As Integer = Korlatoz(cs.Oktav, -2, 2)

            If ujHangszer <> cs.Hangszer OrElse ujHangero <> cs.Hangero OrElse
                ujTer <> cs.Ter OrElse ujVibrato <> cs.Vibrato OrElse
                ujChorus <> cs.Chorus OrElse ujOktav <> cs.Oktav Then
                ertekKorrigalva = True
            End If

            adat.Hangszer = ujHangszer
            adat.Hangero = ujHangero
            adat.Ter = ujTer
            adat.Vibrato = ujVibrato
            adat.Chorus = ujChorus
            adat.Aktiv = cs.Aktiv
            adat.Alul = cs.Alul
            adat.Felul = cs.Felul
            adat.Oktav = ujOktav
            csatornaAllapotok(i) = adat
        Next

        Dim ujAlapHangero As Integer = Korlatoz(b.AlapHangero, 0, 100)
        If ujAlapHangero <> b.AlapHangero Then ertekKorrigalva = True
        trkAlapHangero.Value = ujAlapHangero

        chkPedal.Checked = b.PedalInvertalva

        splitTanulasAktiv = False
        lblSplitInfo.Text = ""

        Dim ujSplitPont As Integer = Korlatoz(b.SplitPont, 0, 127)
        If ujSplitPont <> b.SplitPont Then ertekKorrigalva = True
        cmbSplitPont.SelectedIndex = ujSplitPont

        Dim ujTranszponalas As Integer = Korlatoz(b.Transzponalas, TRANSZPONALAS_MIN, TRANSZPONALAS_MAX)
        If ujTranszponalas <> b.Transzponalas Then ertekKorrigalva = True
        TranszponalasFrissites(ujTranszponalas)

        Dim ujReverb As Integer = Korlatoz(b.Reverb, 0, 100)
        If ujReverb <> b.Reverb Then ertekKorrigalva = True
        trkReverb.Value = ujReverb
        grpReverb.Text = "Visszhang: " & ujReverb & "%"

        Dim eszkozHianyzik As Boolean = False

        If frissitsdEszkozoketIs Then
            Dim beIdx As Integer = cmbMidiIn.Items.IndexOf(b.MidiBemenetNev)
            If beIdx >= 0 Then
                cmbMidiIn.SelectedIndex = beIdx
            ElseIf Not String.IsNullOrEmpty(b.MidiBemenetNev) Then
                eszkozHianyzik = True
            End If

            Dim kiIdx As Integer = cmbMidiOut.Items.IndexOf(b.MidiKimenetNev)
            If kiIdx >= 0 Then
                cmbMidiOut.SelectedIndex = kiIdx
            ElseIf Not String.IsNullOrEmpty(b.MidiKimenetNev) Then
                eszkozHianyzik = True
            End If
        End If

        If Not String.IsNullOrWhiteSpace(b.HangszerListaFajl) Then
            HangszerListaAlkalmazasa(b.HangszerListaFajl)
        End If

        ' A hangszerlista-váltás UTÁN ellenőrizzük mind a 16 csatorna
        ' Hangszer-értékét: ha egy tárolt patch-szám nem szerepel az
        ' AKTUÁLIS listában (pl. kézzel szerkesztett, ellentmondásos
        ' preset esetén), az első elérhető hangszerre állítjuk - így a
        ' MIDI-kimenet MINDIG szinkronban lesz azzal, amit a panelek
        ' ténylegesen mutatnak majd, nem csak a jelenleg látható csoportnál.
        If HangszerekErvenyesitese() Then ertekKorrigalva = True

        CsatornaPanelekFrissitese()
        OsszesCsatornaUjrakuldese()

        If eszkozHianyzik Then
            MsgBox("A betölteni kívánt json fájl nem létező MIDI eszköznevet tartalmazott, amit a program figyelmen kívül hagyott.",
               MsgBoxStyle.Exclamation, "MidiBill")
        End If

        If ertekKorrigalva Then
            MsgBox("A betölteni kívánt json fájl hibás adatokat tartalmazott, amiket a program figyelmen kívül hagyott.",
               MsgBoxStyle.Exclamation, "MidiBill")
        End If
    End Sub

    ''' <summary>
    ''' Ellenőrzi mind a 16 csatorna Hangszer-értékét az AKTUÁLIS
    ''' hangszerlista alapján - ha egy tárolt patch-szám nem szerepel
    ''' benne (pl. listaváltás vagy kézzel szerkesztett preset miatt), az
    ''' első elérhető hangszerre állítja. Ezt hívja mind a preset-betöltés
    ''' (BeallitasokAlkalmazasa), mind a menüből indított hangszerlista-
    ''' váltás (mnuHangszerlistaBetoltese_Click) - egy helyen tartva a logikát.
    ''' </summary>
    ''' <returns>True, ha legalább egy csatornán korrigálni kellett</returns>
    Private Function HangszerekErvenyesitese() As Boolean
        Dim alapertelmezettHangszer As Integer = HangszerLista.Nevek.Keys.OrderBy(Function(k) k).FirstOrDefault()
        Dim alapertelmezettDobHangszer As Integer = HangszerLista.DobNevek.Keys.OrderBy(Function(k) k).FirstOrDefault()
        Dim korrigalva As Boolean = False

        For i As Integer = 0 To 15
            Dim dobCsatorna As Boolean = (i = DOB_CSATORNA_INDEX)
            Dim aktivLista As Dictionary(Of Integer, String) = If(dobCsatorna, HangszerLista.DobNevek, HangszerLista.Nevek)

            If Not aktivLista.ContainsKey(csatornaAllapotok(i).Hangszer) Then
                csatornaAllapotok(i).Hangszer = If(dobCsatorna, alapertelmezettDobHangszer, alapertelmezettHangszer)
                korrigalva = True
            End If
        Next

        Return korrigalva
    End Function

    Private Function Korlatoz(ertek As Integer, min As Integer, max As Integer) As Integer
        Return Math.Max(min, Math.Min(max, ertek))
    End Function

    ''' <summary>
    ''' Megpróbálja betölteni a megadott hangszerlista fájlt, és sikeres
    ''' esetben minden fizikai HangszerPanel legördülőjét frissíti.
    ''' Sikertelenség esetén figyelmeztet, de a JELENLEG AKTÍV lista marad
    ''' érvényben (nincs "csendes" visszaesés). A fájl helyét az
    ''' Utvonalak.HangszerListaOlvasasra dönti el: a gyári GM.lst/GM.drm a
    ''' program mappájából, minden más az "SF adatok" mappából jön.
    ''' </summary>
    Private Function HangszerListaAlkalmazasa(fajlNev As String) As Boolean
        Dim utvonal As String = Utvonalak.HangszerListaOlvasasra(fajlNev)

        If HangszerLista.Betolt(utvonal) Then
            aktualisHangszerListaFajl = fajlNev

            ' Ha van azonos nevű .drm fájl (pl. Teszt.lst mellett Teszt.drm),
            ' azt is betöltjük - ha nincs, csendben a jelenlegi dob-lista
            ' marad érvényben (ez NEM hiba, csak egy opcionális párosítás).
            Dim dobFajlNev As String = IO.Path.GetFileNameWithoutExtension(fajlNev) & ".drm"
            Dim dobUtvonal As String = Utvonalak.HangszerListaOlvasasra(dobFajlNev)
            HangszerLista.BetoltDobKeszletet(dobUtvonal) ' visszatérési értéke szándékosan figyelmen kívül hagyva

            For Each panel As HangszerPanel In hangszerPanelek
                panel.HangszerListaFrissitese()
            Next
            Return True
        Else
            MsgBox("A(z) """ & fajlNev & """ hangszerlista fájl nem található vagy hibás itt:" & vbCrLf &
                   IO.Path.GetDirectoryName(utvonal) & vbCrLf &
                   "A megadott hangszerlista betöltése nem lehetséges - a jelenleg aktív lista marad érvényben.",
                   MsgBoxStyle.Exclamation, "MidiBill")
            Return False
        End If
    End Function

    ''' <summary>
    ''' A nyers MIDI üzenetet (státuszbájt + 2 adatbájt) tömör, olvasható
    ''' szöveggé alakítja a lblMidiUzenet kijelzőhöz - pl. "NoteOn C4 v90",
    ''' "CC64 127", "PC Acoustic Grand Piano". A hangmagassághoz kötött
    ''' üzeneteknél a Hangnevek.HangNev()-et, Program Change-nél a
    ''' HangszerLista.Nev()-et használja fel, amiket már a Split
    ''' pont/hangszerválasztó is használ máshol.
    ''' </summary>
    Private Function MidiUzenetSzoveg(uzenet As Integer, adat1 As Integer, adat2 As Integer) As String
        Dim tipus As Integer = uzenet And &HF0

        Select Case tipus
            Case &H90
                If adat2 > 0 Then
                    Return "NoteOn " & Hangnevek.HangNev(adat1) & " v" & adat2
                Else
                    Return "NoteOff " & Hangnevek.HangNev(adat1)
                End If
            Case &H80
                Return "NoteOff " & Hangnevek.HangNev(adat1)
            Case &HA0
                Return "PolyPress " & Hangnevek.HangNev(adat1) & " " & adat2
            Case &HB0
                Return "CC" & adat1 & " " & adat2
            Case &HC0
                Return "PC " & HangszerLista.Nev(adat1)
            Case &HD0
                Return "ChanPress " & adat1
            Case &HE0
                Return "PitchBend " & (adat1 + adat2 * 128)
            Case Else
                Return "Sys " & uzenet.ToString("X2") & " " & adat1 & " " & adat2
        End Select
    End Function

    Private Sub CimsorFrissitese(teljesUtvonal As String)
        aktualisFajlNev = IO.Path.GetFileName(teljesUtvonal)
        CimSorUjrarajzolas()
    End Sub

    ''' <summary>A címsor tényleges kirajzolása a jelenlegi aktualisFajlNev és aktualisHangszerListaFajl alapján</summary>
    Private Sub CimSorUjrarajzolas()
        Me.Text = "MIDI billentyűzet megszólaltatása 16 csatornán : " & aktualisFajlNev &
                  ", " & aktualisHangszerListaFajl
    End Sub

    Private Sub DialogusMappaBeallitasa(dlg As FileDialog, ByRef megjegyzettMappa As String, Optional alapMappa As String = Nothing)
        ' Alapértelmezésben a Presetek mappa nyílik meg - ide mentődnek a
        ' preset (.json) fájlok. NEM a program mappája, ami telepítés után a
        ' Program Files alatt írásvédett lenne.
        Dim tenylegesAlapMappa As String = If(alapMappa, Utvonalak.PresetekMappa)
        dlg.InitialDirectory = If(String.IsNullOrEmpty(megjegyzettMappa), tenylegesAlapMappa, megjegyzettMappa)
    End Sub

    Private Sub DialogusAllapotMentese(dlg As FileDialog, teljesUtvonal As String, ByRef megjegyzettMappa As String)
        dlg.FileName = IO.Path.GetFileName(teljesUtvonal)
        megjegyzettMappa = IO.Path.GetDirectoryName(teljesUtvonal)
    End Sub

    Private Sub mnuBetolt_Click(sender As Object, e As EventArgs) Handles mnuBetolt.Click
        DialogusMappaBeallitasa(dlgMegnyitas, utolsoBetoltesMappa)

        If dlgMegnyitas.ShowDialog() = DialogResult.OK Then
            Try
                Dim b As Beallitasok = Beallitasok.Betoltes(dlgMegnyitas.FileName)
                BeallitasokAlkalmazasa(b, True)
                CimsorFrissitese(dlgMegnyitas.FileName)
                DialogusAllapotMentese(dlgMegnyitas, dlgMegnyitas.FileName, utolsoBetoltesMappa)
            Catch ex As Exception
                MsgBox("Nem sikerült betölteni a(z) """ & dlgMegnyitas.FileName & """ fájlt." & vbCrLf &
                       "Lehet, hogy sérült, vagy nem MidiBill preset fájl." & vbCrLf & vbCrLf &
                       "Hiba: " & ex.Message, MsgBoxStyle.Exclamation, "MidiBill")
            End Try
        End If
    End Sub

    Private Sub mnuMent_Click(sender As Object, e As EventArgs) Handles mnuMent.Click
        DialogusMappaBeallitasa(dlgMentes, utolsoMentesMappa)
        dlgMentes.FileName = IO.Path.GetFileNameWithoutExtension(aktualisFajlNev)

        If dlgMentes.ShowDialog() = DialogResult.OK Then
            Try
                AktualisBeallitasokOsszegyujtese().Mentes(dlgMentes.FileName)
                CimsorFrissitese(dlgMentes.FileName)
                DialogusAllapotMentese(dlgMentes, dlgMentes.FileName, utolsoMentesMappa)
            Catch ex As Exception
                MsgBox("Nem sikerült elmenteni a beállításokat ide:" & vbCrLf & dlgMentes.FileName & vbCrLf & vbCrLf &
                       "Hiba: " & ex.Message, MsgBoxStyle.Exclamation, "MidiBill")
            End Try
        End If
    End Sub

    Private Sub mnuImportMb3_Click(sender As Object, e As EventArgs) Handles mnuImportMb3.Click
        DialogusMappaBeallitasa(dlgImportMb3, utolsoImportMappa)

        If dlgImportMb3.ShowDialog() = DialogResult.OK Then
            Try
                Dim b As Beallitasok = Beallitasok.ImportMb3(dlgImportMb3.FileName)
                BeallitasokAlkalmazasa(b, False)
                CimsorFrissitese(dlgImportMb3.FileName)
                DialogusAllapotMentese(dlgImportMb3, dlgImportMb3.FileName, utolsoImportMappa)

                MsgBox("A régi preset sikeresen importálva (1-4. csatornára)." & vbCrLf &
                       "A MIDI be-/kimeneti eszköz kiválasztása változatlan maradt - " &
                       "a régi fájl eszközindexei a mai gépen már nem lennének megbízhatóak.",
                       MsgBoxStyle.Information, "MidiBill")
            Catch ex As Exception
                MsgBox("Nem sikerült importálni a(z) """ & dlgImportMb3.FileName & """ fájlt." & vbCrLf &
                       "Lehet, hogy sérült, vagy nem régi MidiBill .mb3 fájl." & vbCrLf & vbCrLf &
                       "Hiba: " & ex.Message, MsgBoxStyle.Exclamation, "MidiBill")
            End Try
        End If
    End Sub

    ''' <summary>
    ''' Ellenőrzi, hogy a megadott fájlnév ütközik-e a GYÁRI, védett
    ''' hangszerlistákkal (GM.lst / GM.drm). Ha igen, figyelmeztet, és True-t
    ''' ad vissza - a hívónak ilyenkor meg kell szakítania a műveletet.
    ''' Ezek a fájlok a program mappájában laknak, és mindig onnan olvasódnak;
    ''' egy azonos nevű felhasználói fájl sosem kerülne felhasználásra, ezért
    ''' inkább létre sem hagyjuk jönni.
    ''' </summary>
    Private Function GyariNevUtkozes(fajlNev As String) As Boolean
        If Not Utvonalak.GyariListaE(fajlNev) Then Return False

        MsgBox("A(z) """ & IO.Path.GetFileName(fajlNev) & """ a program GYÁRI hangszerlistája," & vbCrLf &
               "amit nem lehet felülírni vagy saját változattal helyettesíteni." & vbCrLf & vbCrLf &
               "Nevezd át a fájlt (SoundFont importálásánál magát a .sf2 fájlt), és próbáld újra.",
               MsgBoxStyle.Exclamation, "MidiBill")
        Return True
    End Function

    Private Sub mnuHangszerlistaBetoltese_Click(sender As Object, e As EventArgs) Handles mnuHangszerlistaBetoltese.Click
        DialogusMappaBeallitasa(dlgHangszerlista, utolsoHangszerlistaMappa, Utvonalak.SfAdatokMappa)

        If dlgHangszerlista.ShowDialog() = DialogResult.OK Then
            Dim forrasUtvonal As String = dlgHangszerlista.FileName
            Dim fajlNev As String = IO.Path.GetFileName(forrasUtvonal)

            ' Ha a kiválasztott fájl MÁR a végleges helyén van (gyári lista a
            ' program mappájában, vagy sajátlista az "SF adatok" mappában),
            ' nincs mit másolni - a HangszerListaAlkalmazasa a fájlNÉV alapján
            ' úgyis megtalálja. Egyébként az "SF adatok" mappába másoljuk,
            ' sosem a program mappájába (az telepítés után írásvédett lenne).
            If Not Utvonalak.MarAHelyenVanE(forrasUtvonal) Then
                If GyariNevUtkozes(fajlNev) Then Return

                Dim celUtvonal As String = Utvonalak.HangszerListaIrasra(fajlNev)
                Dim felulirodik As Boolean = IO.File.Exists(celUtvonal)
                Dim uzenet As String

                If felulirodik Then
                    uzenet = "A(z) """ & fajlNev & """ fájl bekerül ide:" & vbCrLf &
                             Utvonalak.SfAdatokMappa & vbCrLf & vbCrLf &
                             "Ott már létezik egy azonos nevű fájl, ami FELÜLÍRÓDIK." & vbCrLf & vbCrLf &
                             "Folytatod?"
                Else
                    uzenet = "A(z) """ & fajlNev & """ fájl bekerül (átmásolódik) ide:" & vbCrLf &
                             Utvonalak.SfAdatokMappa & vbCrLf & vbCrLf &
                             "Folytatod?"
                End If

                Dim valasz As MsgBoxResult = MsgBox(uzenet, MsgBoxStyle.YesNo Or MsgBoxStyle.Question, "MidiBill")
                If valasz = MsgBoxResult.No Then Return

                Try
                    IO.File.Copy(forrasUtvonal, celUtvonal, True)
                Catch ex As Exception
                    MsgBox("Nem sikerült átmásolni a fájlt ide:" & vbCrLf & Utvonalak.SfAdatokMappa & vbCrLf & vbCrLf &
                           "Hiba: " & ex.Message, MsgBoxStyle.Exclamation, "MidiBill")
                    Return
                End Try
            End If

            If HangszerListaAlkalmazasa(fajlNev) Then
                ' A lista sikeresen váltott - ellenőrizzük mind a 16
                ' csatornát, hátha valamelyik olyan hangszert használt,
                ' ami nem szerepel az új listában, majd frissítjük a
                ' paneleket és ÚJRAKÜLDJÜK a MIDI-t minden csatornára -
                ' e nélkül a szintetizátor a régi hangszereket tartaná
                ' megszólaltatva.
                Dim korrigalva As Boolean = HangszerekErvenyesitese()
                CsatornaPanelekFrissitese()
                OsszesCsatornaUjrakuldese()
                CimSorUjrarajzolas()

                If korrigalva Then
                    MsgBox("Egy vagy több csatorna olyan hangszert használt, ami nem szerepel az új hangszerlistában." & vbCrLf &
                           "Ezek automatikusan az első elérhető hangszerre álltak.",
                           MsgBoxStyle.Exclamation, "MidiBill")
                End If
            End If

            DialogusAllapotMentese(dlgHangszerlista, forrasUtvonal, utolsoHangszerlistaMappa)
        End If
    End Sub

    Private Sub mnuSoundFontImportalasa_Click(sender As Object, e As EventArgs) Handles mnuSoundFontImportalasa.Click
        DialogusMappaBeallitasa(dlgSoundFont, utolsoSoundFontMappa, Utvonalak.SfAdatokMappa)

        If dlgSoundFont.ShowDialog() = DialogResult.OK Then
            Dim sf2Utvonal As String = dlgSoundFont.FileName
            Dim sf2Nev As String = IO.Path.GetFileName(sf2Utvonal)
            Dim alapNev As String = IO.Path.GetFileNameWithoutExtension(sf2Utvonal)
            Dim ujLstNev As String = alapNev & ".lst"
            Dim ujDrmNev As String = alapNev & ".drm"

            ' Egy "GM.sf2" nevű SoundFont GM.lst/GM.drm néven generálna listát,
            ' ami ütközne a gyári, védett listákkal - ezt még a beolvasás előtt
            ' elkapjuk, hogy ne dolgozzunk feleslegesen.
            If GyariNevUtkozes(ujLstNev) Then Return

            ' A generált listák MINDIG az "SF adatok" mappába kerülnek
            Dim lstUtvonal As String = Utvonalak.HangszerListaIrasra(ujLstNev)
            Dim drmUtvonal As String = Utvonalak.HangszerListaIrasra(ujDrmNev)

            Dim eredmeny As (Nevek As Dictionary(Of Integer, String), Dobok As Dictionary(Of Integer, String), Kihagyottak As List(Of String))
            Try
                eredmeny = SoundFontOlvaso.Beolvas(sf2Utvonal)
            Catch ex As Exception
                MsgBox("A(z) """ & sf2Nev & """ SoundFont fájl tartalmát nem sikerült beolvasni." & vbCrLf & vbCrLf &
                       "Hiba: " & ex.Message, MsgBoxStyle.Exclamation, "MidiBill")
                Return
            End Try

            If eredmeny.Nevek.Count = 0 AndAlso eredmeny.Dobok.Count = 0 Then
                MsgBox("A(z) """ & sf2Nev & """ SoundFont fájlban nem található egyetlen felhasználható preset sem.",
                       MsgBoxStyle.Exclamation, "MidiBill")
                Return
            End If

            Dim lstLetezik As Boolean = IO.File.Exists(lstUtvonal)
            Dim drmLetezik As Boolean = eredmeny.Dobok.Count > 0 AndAlso IO.File.Exists(drmUtvonal)
            If lstLetezik OrElse drmLetezik Then
                Dim uzenet As String = "A(z) """ & ujLstNev & """" & If(drmLetezik, " és """ & ujDrmNev & """", "") &
                                        " már létezik itt:" & vbCrLf &
                                        Utvonalak.SfAdatokMappa & vbCrLf & vbCrLf &
                                        "Felülírjuk az újonnan importált SoundFont adataival?"
                If MsgBox(uzenet, MsgBoxStyle.YesNo Or MsgBoxStyle.Question, "MidiBill") = MsgBoxResult.No Then Return
            End If

            If eredmeny.Nevek.Count > 0 Then
                HangszerLista.Ment(lstUtvonal, eredmeny.Nevek)
            End If
            If eredmeny.Dobok.Count > 0 Then
                HangszerLista.Ment(drmUtvonal, eredmeny.Dobok)
            End If

            If HangszerListaAlkalmazasa(ujLstNev) Then
                Dim korrigalva As Boolean = HangszerekErvenyesitese()
                CsatornaPanelekFrissitese()
                OsszesCsatornaUjrakuldese()
                CimSorUjrarajzolas()

                Dim osszegzes As String = """" & sf2Nev & """ SoundFont fájl adatai sikeresen beolvasva." & vbCrLf &
                                           eredmeny.Nevek.Count & " hangszer és " & eredmeny.Dobok.Count & " dobkészlet betöltve" &
                                           If(eredmeny.Kihagyottak.Count > 0,
                                              ", " & eredmeny.Kihagyottak.Count & " érvénytelen bejegyzés kihagyva.",
                                              ".")

                MsgBox(osszegzes, MsgBoxStyle.Information, "MidiBill")

                If korrigalva Then
                    MsgBox("Egy vagy több csatorna olyan hangszert használt, ami nem szerepel az új hangszerlistában." & vbCrLf &
                           "Ezek automatikusan az első elérhető hangszerre álltak.",
                           MsgBoxStyle.Exclamation, "MidiBill")
                End If
            End If

            DialogusAllapotMentese(dlgSoundFont, sf2Utvonal, utolsoSoundFontMappa)
        End If
    End Sub

    ''' <summary>
    ''' A konvertáló segédprogramok kimeneti mappájának előkészítése:
    ''' elsődlegesen a forrásfájlok mellé, egy "Konvertalva" almappába. Ha ott
    ''' nem lehet létrehozni (pl. a forrásfájlok a telepítési mappában vannak,
    ''' ami a Program Files alatt írásvédett), a felhasználói mappába esik
    ''' vissza - így a művelet sosem szakad meg kezeletlen hibával.
    ''' </summary>
    Private Function KonvertalasKimenetiMappa(forrasMappa As String) As String
        Dim elsodlegesMappa As String = IO.Path.Combine(forrasMappa, "Konvertalva")
        Try
            IO.Directory.CreateDirectory(elsodlegesMappa)
            Return elsodlegesMappa
        Catch ex As Exception
            Dim tartalekMappa As String = IO.Path.Combine(Utvonalak.FelhasznaloiMappa, "Konvertalva")
            IO.Directory.CreateDirectory(tartalekMappa)
            Return tartalekMappa
        End Try
    End Function

    Private Sub mnuPresetFajlokFrissitese_Click(sender As Object, e As EventArgs) Handles mnuPresetFajlokFrissitese.Click
        DialogusMappaBeallitasa(dlgPresetKonvertalas, utolsoPresetKonvertalasMappa, Utvonalak.PresetekMappa)

        If dlgPresetKonvertalas.ShowDialog() = DialogResult.OK Then
            Dim fajlok As String() = dlgPresetKonvertalas.FileNames
            Dim forrasMappa As String = IO.Path.GetDirectoryName(fajlok(0))
            Dim kimenoMappa As String = KonvertalasKimenetiMappa(forrasMappa)

            Dim sikeres As Integer = 0
            Dim hibaUzenetek As New List(Of String)

            For Each fajl As String In fajlok
                Dim nev As String = IO.Path.GetFileName(fajl)
                Try
                    Dim b As Beallitasok = Beallitasok.Betoltes(fajl)
                    b.Verzio = AppInfo.AppVerzio()
                    b.Mentes(IO.Path.Combine(kimenoMappa, nev))
                    sikeres += 1
                Catch ex As Exception
                    hibaUzenetek.Add(nev & ": " & ex.Message)
                End Try
            Next

            Dim osszegzes As String = "Kész. Sikeres: " & sikeres & ", hibás: " & hibaUzenetek.Count & "." & vbCrLf &
                                       "A konvertált fájlok itt találhatók:" & vbCrLf & kimenoMappa
            If hibaUzenetek.Count > 0 Then
                osszegzes &= vbCrLf & vbCrLf & "Hibák:" & vbCrLf & String.Join(vbCrLf, hibaUzenetek)
            End If

            MsgBox(osszegzes, MsgBoxStyle.Information, "MidiBill - Preset fájlok frissítése")

            DialogusAllapotMentese(dlgPresetKonvertalas, fajlok(0), utolsoPresetKonvertalasMappa)
        End If
    End Sub

    Private Sub mnuMb3FajlokKonvertalasa_Click(sender As Object, e As EventArgs) Handles mnuMb3FajlokKonvertalasa.Click
        DialogusMappaBeallitasa(dlgMb3Konvertalas, utolsoMb3KonvertalasMappa, Utvonalak.PresetekMappa)

        If dlgMb3Konvertalas.ShowDialog() = DialogResult.OK Then
            Dim fajlok As String() = dlgMb3Konvertalas.FileNames

            Dim alapBemenet As String = If(cmbMidiIn.SelectedItem IsNot Nothing, cmbMidiIn.SelectedItem.ToString(), "")
            Dim alapKimenet As String = If(cmbMidiOut.SelectedItem IsNot Nothing, cmbMidiOut.SelectedItem.ToString(), "")

            Dim midiBe As String = InputBox(
                "Add meg a MIDI bemeneti eszköz nevét, amit a konvertált fájlokba be szeretnél állítani." & vbCrLf &
                "(A jelenleg kiválasztott eszköz van alapértelmezettként megadva.)",
                "MidiBill - MIDI bemenet", alapBemenet)
            Dim midiKi As String = InputBox(
                "Add meg a MIDI kimeneti eszköz nevét, amit a konvertált fájlokba be szeretnél állítani." & vbCrLf &
                "(A jelenleg kiválasztott eszköz van alapértelmezettként megadva.)",
                "MidiBill - MIDI kimenet", alapKimenet)

            Dim forrasMappa As String = IO.Path.GetDirectoryName(fajlok(0))
            Dim kimenoMappa As String = KonvertalasKimenetiMappa(forrasMappa)

            Dim sikeres As Integer = 0
            Dim hibaUzenetek As New List(Of String)

            For Each fajl As String In fajlok
                Dim nev As String = IO.Path.GetFileName(fajl)
                Try
                    Dim b As Beallitasok = Beallitasok.ImportMb3(fajl)
                    b.Verzio = AppInfo.AppVerzio()
                    b.MidiBemenetNev = midiBe
                    b.MidiKimenetNev = midiKi
                    Dim ujNev As String = IO.Path.GetFileNameWithoutExtension(nev) & ".json"
                    b.Mentes(IO.Path.Combine(kimenoMappa, ujNev))
                    sikeres += 1
                Catch ex As Exception
                    hibaUzenetek.Add(nev & ": " & ex.Message)
                End Try
            Next

            Dim osszegzes As String = "Kész. Sikeres: " & sikeres & ", hibás: " & hibaUzenetek.Count & "." & vbCrLf &
                                       "A konvertált fájlok itt találhatók:" & vbCrLf & kimenoMappa
            If hibaUzenetek.Count > 0 Then
                osszegzes &= vbCrLf & vbCrLf & "Hibák:" & vbCrLf & String.Join(vbCrLf, hibaUzenetek)
            End If

            MsgBox(osszegzes, MsgBoxStyle.Information, "MidiBill - Régi .mb3 fájlok konvertálása")

            DialogusAllapotMentese(dlgMb3Konvertalas, fajlok(0), utolsoMb3KonvertalasMappa)
        End If
    End Sub

    ''' <summary>
    ''' Meglévő .mid fájlok elejére beleírja az AKTÍV csatornák jelenlegi
    ''' MIDI-állapotát (hangszín, hangerő, tér, vibrato, chorus, reverb) -
    ''' lásd Modellek/MidiFajlSzerkeszto.vb. A bemeneti fájlokat nem írja
    ''' felül: a módosított másolatok a KonvertalasKimenetiMappa() szerinti
    ''' almappába kerülnek. JELENLEG CSAK format 1 MIDI fájlokat támogat -
    ''' format 0 esetén az adott fájl hibaként jelenik meg az összegzésben,
    ''' a többi fájl feldolgozása viszont folytatódik.
    ''' </summary>
    Private Sub mnuMidiFajlModositasa_Click(sender As Object, e As EventArgs) Handles mnuMidiFajlModositasa.Click
        DialogusMappaBeallitasa(dlgMidiFajlModositas, utolsoMidiFajlModositasMappa)

        If dlgMidiFajlModositas.ShowDialog() = DialogResult.OK Then
            Dim fajlok As String() = dlgMidiFajlModositas.FileNames
            Dim forrasMappa As String = IO.Path.GetDirectoryName(fajlok(0))
            Dim kimenoMappa As String = KonvertalasKimenetiMappa(forrasMappa)

            Dim sikeres As Integer = 0
            Dim hibaUzenetek As New List(Of String)

            For Each fajl As String In fajlok
                Dim nev As String = IO.Path.GetFileName(fajl)
                Try
                    Dim kimenetiUtvonal As String = IO.Path.Combine(kimenoMappa, nev)
                    MidiFajlSzerkeszto.BeallitasokBeszurasa(fajl, kimenetiUtvonal, csatornaAllapotok, trkAlapHangero.Value, trkReverb.Value)
                    sikeres += 1
                Catch ex As Exception
                    hibaUzenetek.Add(nev & ": " & ex.Message)
                End Try
            Next

            Dim osszegzes As String = "Kész. Sikeres: " & sikeres & ", hibás: " & hibaUzenetek.Count & "." & vbCrLf &
                                       "A módosított fájlok itt találhatók:" & vbCrLf & kimenoMappa
            If hibaUzenetek.Count > 0 Then
                osszegzes &= vbCrLf & vbCrLf & "Hibák:" & vbCrLf & String.Join(vbCrLf, hibaUzenetek)
            End If

            MsgBox(osszegzes, MsgBoxStyle.Information, "MidiBill - MIDI fájl módosítása")

            DialogusAllapotMentese(dlgMidiFajlModositas, fajlok(0), utolsoMidiFajlModositasMappa)
        End If
    End Sub

    ' ============================================================
    ' Segédprogramok: MIDI fájl -> preset, illetve olvasható jelentés
    ' ============================================================
    '
    ' Mindkettő a KÖZÖS Modellek\MidiFajlOlvaso beolvasóra épül, és ugyanúgy
    ' viselkedik: több fájl választható, a kimenet a forrásfájl MELLÉ, azonos
    ' néven készül, a felülírásra egyszer kérdezünk rá az egész kötegre, a
    ' végén pedig összegzés jön a hibákkal együtt.

    Private utolsoPresetMidibolMappa As String = ""
    Private utolsoMidiBeallitasMentesMappa As String = ""

    ''' <summary>
    ''' Preset (.json) készítése MIDI fájl(ok)ból - a "MIDI fájlok módosítása"
    ''' segédprogram FORDÍTOTTJA: nem beleírja, hanem KIOLVASSA a
    ''' csatorna-beállításokat. A leképezés szabályait lásd
    ''' Modellek\MidiPresetKeszito.vb.
    ''' </summary>
    Private Sub mnuPresetMidibol_Click(sender As Object, e As EventArgs) Handles mnuPresetMidibol.Click
        DialogusMappaBeallitasa(dlgPresetMidibol, utolsoPresetMidibolMappa)
        If dlgPresetMidibol.ShowDialog() <> DialogResult.OK Then Return

        Dim fajlok As String() = dlgPresetMidibol.FileNames

        Dim hangszerListaFajl As String = HangszerlistaValasztasa()
        If hangszerListaFajl Is Nothing Then Return   ' a felhasználó megszakította

        If Not FelulirasEngedelyezese(fajlok, ".json", "preset") Then Return

        Dim sikeres As Integer = 0
        Dim hibaUzenetek As New List(Of String)

        For Each fajl As String In fajlok
            Dim nev As String = IO.Path.GetFileName(fajl)
            Try
                Dim fajlAdat As MidiFajlAdat = MidiFajlOlvaso.Beolvas(fajl)
                Dim preset As Beallitasok = MidiPresetKeszito.PresetKeszitese(fajlAdat, hangszerListaFajl, AppInfo.AppVerzio())
                preset.Mentes(KimenetiUtvonal(fajl, ".json"))
                sikeres += 1
            Catch ex As Exception
                hibaUzenetek.Add(nev & ": " & ex.Message)
            End Try
        Next

        OsszegzesMegjelenitese(fajlok, sikeres, hibaUzenetek, ".json",
                               "MidiBill - Preset készítése MIDI fájlból")

        DialogusAllapotMentese(dlgPresetMidibol, fajlok(0), utolsoPresetMidibolMappa)
    End Sub

    ''' <summary>
    ''' MIDI fájl(ok) kezdeti beállításainak mentése EMBERI OLVASÁSRA szánt
    ''' szöveges (.txt) jelentésként - lásd Modellek\MidiJelentes.vb.
    ''' </summary>
    Private Sub mnuMidiBeallitasokMentese_Click(sender As Object, e As EventArgs) Handles mnuMidiBeallitasokMentese.Click
        DialogusMappaBeallitasa(dlgMidiBeallitasokMentese, utolsoMidiBeallitasMentesMappa)
        If dlgMidiBeallitasokMentese.ShowDialog() <> DialogResult.OK Then Return

        Dim fajlok As String() = dlgMidiBeallitasokMentese.FileNames

        Dim hangszerListaFajl As String = HangszerlistaValasztasa()
        If hangszerListaFajl Is Nothing Then Return

        If Not FelulirasEngedelyezese(fajlok, ".txt", "szöveges") Then Return

        Dim sikeres As Integer = 0
        Dim hibaUzenetek As New List(Of String)

        For Each fajl As String In fajlok
            Dim nev As String = IO.Path.GetFileName(fajl)
            Try
                Dim fajlAdat As MidiFajlAdat = MidiFajlOlvaso.Beolvas(fajl)
                Dim jelentes As String = MidiJelentes.JelentesKeszitese(fajlAdat, nev, hangszerListaFajl)

                ' UTF-8 BOM-mal, hogy a Jegyzettömb is helyesen jelenítse meg
                ' az ékezetes betűket.
                IO.File.WriteAllText(KimenetiUtvonal(fajl, ".txt"), jelentes, New Text.UTF8Encoding(True))
                sikeres += 1
            Catch ex As Exception
                hibaUzenetek.Add(nev & ": " & ex.Message)
            End Try
        Next

        OsszegzesMegjelenitese(fajlok, sikeres, hibaUzenetek, ".txt",
                               "MidiBill - MIDI fájl beállításainak mentése")

        DialogusAllapotMentese(dlgMidiBeallitasokMentese, fajlok(0), utolsoMidiBeallitasMentesMappa)
    End Sub

    ''' <summary>
    ''' A hangszernevek feloldásához használt lista kiválasztása. A gyári
    ''' GM.lst az alapértelmezett; ha a felhasználó mást szeretne, egy
    ''' fájlválasztó nyílik az "SF adatok" mappára.
    ''' </summary>
    ''' <returns>A választott lista FÁJLNEVE, vagy Nothing, ha a felhasználó megszakította a műveletet.</returns>
    Private Function HangszerlistaValasztasa() As String
        Dim valasz As MsgBoxResult = MsgBox(
            "A hangszernevek feloldásához a gyári ""GM.lst"" listát használjuk?" & vbCrLf & vbCrLf &
            "Igen  - a gyári General MIDI lista" & vbCrLf &
            "Nem   - másik hangszerlista választása az ""SF adatok"" mappából" & vbCrLf &
            "Mégse - a művelet megszakítása",
            MsgBoxStyle.YesNoCancel Or MsgBoxStyle.Question, "MidiBill - hangszerlista")

        Select Case valasz
            Case MsgBoxResult.Yes
                Return "GM.lst"
            Case MsgBoxResult.Cancel
                Return Nothing
        End Select

        dlgHangszerlistaValasztas.InitialDirectory = Utvonalak.SfAdatokMappa
        If dlgHangszerlistaValasztas.ShowDialog() <> DialogResult.OK Then Return Nothing

        Return IO.Path.GetFileName(dlgHangszerlistaValasztas.FileName)
    End Function

    ''' <summary>
    ''' A kimeneti fájl útvonala: a FORRÁSFÁJL MELLÉ, azonos alapnévvel, csak
    ''' más kiterjesztéssel (a felhasználó kifejezett kérése).
    ''' </summary>
    Private Function KimenetiUtvonal(forrasUtvonal As String, kiterjesztes As String) As String
        Return IO.Path.Combine(IO.Path.GetDirectoryName(forrasUtvonal),
                               IO.Path.GetFileNameWithoutExtension(forrasUtvonal) & kiterjesztes)
    End Function

    ''' <summary>
    ''' Ha a kötegben van olyan fájl, aminek a kimenete MÁR LÉTEZIK, egyszer
    ''' rákérdez a felülírásra - az egész kötegre vonatkozóan, nem fájlonként.
    ''' </summary>
    ''' <returns>True, ha folytatható a művelet.</returns>
    Private Function FelulirasEngedelyezese(fajlok As String(), kiterjesztes As String, megnevezes As String) As Boolean
        Dim letezok As New List(Of String)
        For Each fajl As String In fajlok
            Dim celUtvonal As String = KimenetiUtvonal(fajl, kiterjesztes)
            If IO.File.Exists(celUtvonal) Then letezok.Add(IO.Path.GetFileName(celUtvonal))
        Next

        If letezok.Count = 0 Then Return True

        Dim lista As String = String.Join(vbCrLf, letezok.Take(10))
        If letezok.Count > 10 Then lista &= vbCrLf & "... és még " & (letezok.Count - 10) & " fájl"

        Dim valasz As MsgBoxResult = MsgBox(
            letezok.Count & " " & megnevezes & " fájl MÁR LÉTEZIK a forrásfájlok mellett:" & vbCrLf & vbCrLf &
            lista & vbCrLf & vbCrLf & "Felülírjuk őket?",
            MsgBoxStyle.YesNo Or MsgBoxStyle.Question, "MidiBill")

        Return (valasz = MsgBoxResult.Yes)
    End Function

    ''' <summary>A két új segédprogram közös, összegző üzenetablaka.</summary>
    Private Sub OsszegzesMegjelenitese(fajlok As String(), sikeres As Integer, hibaUzenetek As List(Of String),
                                       kiterjesztes As String, cim As String)
        Dim osszegzes As String = "Kész. Sikeres: " & sikeres & ", hibás: " & hibaUzenetek.Count & "." & vbCrLf &
                                  "A(z) " & kiterjesztes & " fájlok a forrásfájlok mellé kerültek:" & vbCrLf &
                                  IO.Path.GetDirectoryName(fajlok(0))

        If hibaUzenetek.Count > 0 Then
            osszegzes &= vbCrLf & vbCrLf & "Hibák:" & vbCrLf & String.Join(vbCrLf, hibaUzenetek)
        End If

        MsgBox(osszegzes, MsgBoxStyle.Information, cim)
    End Sub

    Private Sub mnuAlaphelyzetbeAllitas_Click(sender As Object, e As EventArgs) Handles mnuAlaphelyzetbeAllitas.Click
        Dim valasz As MsgBoxResult = MsgBox("Minden jelenlegi beállítás törlődni fog! Folytatja?",
                                             MsgBoxStyle.YesNo Or MsgBoxStyle.Question, "MidiBill")
        If valasz = MsgBoxResult.No Then Return

        AlaphelyzetbeAllitas()
    End Sub

    ''' <summary>
    ''' Minden beállítást alaphelyzetbe állít - PONTOSAN úgy, mintha a program
    ''' a Kezdet.json beolvasása NÉLKÜL indulna: a hangszerlista visszaáll
    ''' GM.lst/GM.drm-re, a csatornák/Split pont/Transzponálás/Reverb/Pedál a
    ''' beépített alapértékekre. A MIDI In/Out kiválasztás SZÁNDÉKOSAN
    ''' változatlan marad (ez nem "beállítás" a felhasználó szemszögéből,
    ''' hanem eszközkapcsolat). NEM törli a Kezdet.json fájlt a lemezről -
    ''' normál kilépéskor a mostani (alaphelyzetbe állított) állapot
    ''' felülírja majd, ha a felhasználó nem a "Kilépés mentés nélkül"
    ''' opciót választja.
    ''' </summary>
    Private Sub AlaphelyzetbeAllitas()
        HangszerListaBetoltese()

        CsatornaAllapotokInicializalasa()

        trkAlapHangero.Value = 70

        splitTanulasAktiv = False
        lblSplitInfo.Text = ""
        cmbSplitPont.SelectedIndex = 60

        TranszponalasFrissites(0)

        trkReverb.Value = 0

        chkPedal.Checked = False
        chkMidiBeallitoUzenetek.Checked = False

        CsatornaPanelekFrissitese()
        OsszesCsatornaUjrakuldese()

        CimsorFrissitese(kezdetFile)
    End Sub

    ''' <summary>
    ''' Egy ELKÉSZÜLT felvétel mellé legyártja a "MIDI fájl beállításainak
    ''' mentése olvasható formában" jelentést, azonos alapnévvel, .txt
    ''' kiterjesztéssel, a .mid MELLÉ.
    '''
    ''' MIÉRT ITT, ÉS NEM A MidiFelvevo-ben? A felvevő szándékosan nem tud
    ''' semmit a hangszerlistákról és a preset-ekről - ő egy tiszta SMF-író.
    ''' Ez a rutin köti össze a kettőt, és a felvétel 3. fázisának KÉZI
    ''' mentése és a 4. fázis AUTOMATIKUS mentése is ezt fogja hívni.
    '''
    ''' A hangszerlistát és a preset nevét KÉRDÉS NÉLKÜL az éppen betöltöttből
    ''' veszi (a felhasználó kérése): a felvétel pontosan azokkal készült,
    ''' tehát azok adják a leghűbb képet - a menüpont felugró kérdése itt
    ''' csak zavaró lenne.
    ''' </summary>
    ''' <returns>
    ''' Üres sztring, ha elkészült a jelentés; egyébként a hiba szövege. A
    ''' hívó dolga megjeleníteni - de a felvétel MENTÉSÉT ez sosem
    ''' veszélyeztetheti: a .mid ilyenkor is megvan.
    ''' </returns>
    Private Function FelvetelJelentesenekKeszitese(midiUtvonal As String) As String
        Try
            Dim fajlAdat As MidiFajlAdat = MidiFajlOlvaso.Beolvas(midiUtvonal)
            Dim jelentes As String = MidiJelentes.JelentesKeszitese(
                fajlAdat, IO.Path.GetFileName(midiUtvonal), aktualisHangszerListaFajl, aktualisFajlNev)

            ' UTF-8 BOM-mal, hogy a Jegyzettömb is helyesen jelenítse meg
            ' az ékezetes betűket - ugyanúgy, mint a menüpontnál.
            IO.File.WriteAllText(KimenetiUtvonal(midiUtvonal, ".txt"), jelentes, New Text.UTF8Encoding(True))
            Return ""
        Catch ex As Exception
            Return ex.Message
        End Try
    End Function

    ' ================================================================
    ' ================================================================
    '  IDEIGLENES TESZT-KÓD - a 3. fázisban TELJES EGÉSZÉBEN TÖRLENDŐ!
    ' ================================================================
    '
    '  Ez a blokk kizárólag azért van itt, hogy a MIDI felvevő motorját
    '  (Modellek\MidiFelvevo.vb) VALÓDI hardveren, még a felvevő ablak
    '  megépítése ELŐTT ki lehessen próbálni - így ha az időzítéssel vagy
    '  a mentett fájllal baj van, az azelőtt kiderül, hogy a teljes
    '  felületet ráépítenénk.
    '
    '  Működés: a btnMidiFelvetel gomb ide-oda kapcsol.
    '    1. kattintás -> a felvevő FELFEGYVERZŐDIK (még nem rögzít semmit,
    '       az első ténylegesen kiküldött MIDI üzenetre vár)
    '    2. kattintás -> lezárás + mentés a Felvetelek mappába, dátum
    '       szerinti almappába, majd egy összegző üzenetablak
    '
    '  A 3. fázisban ennek a gombnak az lesz a dolga, hogy megnyissa a
    '  MidiFelvevoForm ablakot - AKKOR ez az EGÉSZ blokk (a lenti három
    '  mezővel és három metódussal együtt) törölhető.
    ' ================================================================

    Private ReadOnly midiFelvevoTeszt As New MidiFelvevo()
    Private utolsoJelzettFelvevoAllapot As FelvevoAllapot = FelvevoAllapot.Alap
    Private ReadOnly felvetelGombEredetiSzovege As String = "MIDI felvétel indítása"

    ''' <summary>IDEIGLENES: a motor kimeneti csapjáról továbbítja az üzeneteket a felvevőnek.</summary>
    Private Sub midiMotor_UzenetKikuldve(statuszBajt As Integer, adat1 As Integer, adat2 As Integer,
                                         idobelyegMs As Long, lejatszasBol As Boolean, metronomBol As Boolean) _
                                         Handles midiMotor.UzenetKikuldve

        midiFelvevoTeszt.UzenetRogzitese(statuszBajt, adat1, adat2, idobelyegMs, lejatszasBol, metronomBol)

        ' Csak ÁLLAPOTVÁLTÁSKOR nyúlunk a felülethez (tipikusan egyszer, az
        ' első leütésnél: Felfegyverezve -> Rogzit), nem minden üzenetnél.
        If midiFelvevoTeszt.Allapot <> utolsoJelzettFelvevoAllapot Then
            utolsoJelzettFelvevoAllapot = midiFelvevoTeszt.Allapot
            FelvetelGombFrissiteseTeszt()
        End If
    End Sub

    ''' <summary>IDEIGLENES: a gomb felirata és színe a felvevő állapota szerint.</summary>
    Private Sub FelvetelGombFrissiteseTeszt()
        Select Case midiFelvevoTeszt.Allapot
            Case FelvevoAllapot.Felfegyverezve
                btnMidiFelvetel.Text = "Várakozás billentyűre..."
                btnMidiFelvetel.UseVisualStyleBackColor = False
                btnMidiFelvetel.BackColor = Color.Gold
            Case FelvevoAllapot.Rogzit
                btnMidiFelvetel.Text = "● FELVÉTEL - leállítás"
                btnMidiFelvetel.UseVisualStyleBackColor = False
                btnMidiFelvetel.BackColor = Color.IndianRed
            Case Else
                btnMidiFelvetel.Text = felvetelGombEredetiSzovege
                btnMidiFelvetel.UseVisualStyleBackColor = True
        End Select
    End Sub

    ''' <summary>IDEIGLENES: felfegyverzés, illetve lezárás + mentés.</summary>
    Private Sub btnMidiFelvetel_Click(sender As Object, e As EventArgs) Handles btnMidiFelvetel.Click
        If midiFelvevoTeszt.Allapot = FelvevoAllapot.Alap Then
            ' A felvevő EZZEL kéri majd le a csatorna-állapotot, pontosan
            ' a rögzítés indulásának pillanatában (nem most, felfegyverzéskor).
            midiFelvevoTeszt.Felfegyverzes(AddressOf AktivCsatornakAllapotUzenetei)
            utolsoJelzettFelvevoAllapot = midiFelvevoTeszt.Allapot
            FelvetelGombFrissiteseTeszt()
            Return
        End If

        ' A számlálókat a lezárás ELŐTT kell kiolvasni: a LezarasEsMentes a
        ' végén kiüríti a puffert, utána már 0-t adna vissza.
        Dim esemenySzam As Integer = midiFelvevoTeszt.EsemenySzam
        Dim hosszMasodperc As Double = midiFelvevoTeszt.RogzitettHosszMs / 1000.0

        Dim utvonal As String = Utvonalak.UjFelvetelUtvonala(Utvonalak.ALAPERTELMEZETT_FELVETEL_NEV, True)

        Try
            If midiFelvevoTeszt.LezarasEsMentes(utvonal) Then
                ' A .mid megvan - a jelentés már csak "ráadás": ha nem sikerül,
                ' azt csak jelezzük, a felvételt nem érinti. (A 3. fázisban
                ' ezt a felvevő ablak jelölőnégyzete fogja engedélyezni.)
                Dim jelentesHiba As String = FelvetelJelentesenekKeszitese(utvonal)

                MsgBox("A felvétel elmentve:" & vbCrLf & utvonal & vbCrLf & vbCrLf &
                       "Rögzített üzenetek: " & esemenySzam & vbCrLf &
                       "Hossz: " & hosszMasodperc.ToString("0.0") & " másodperc" & vbCrLf &
                       If(jelentesHiba.Length = 0,
                          "Jelentés: elkészült (.txt a felvétel mellett)",
                          "A jelentés nem készült el: " & jelentesHiba),
                       MsgBoxStyle.Information, "MidiBill - teszt felvétel")
            Else
                MsgBox("Nem rögzült egyetlen üzenet sem, ezért nem készült fájl." & vbCrLf &
                       "(A felvétel az első lejátszott hangnál indul el.)",
                       MsgBoxStyle.Information, "MidiBill - teszt felvétel")
            End If
        Catch ex As Exception
            MsgBox("Nem sikerült elmenteni a felvételt ide:" & vbCrLf & utvonal & vbCrLf & vbCrLf &
                   "Hiba: " & ex.Message, MsgBoxStyle.Exclamation, "MidiBill - teszt felvétel")
        End Try

        utolsoJelzettFelvevoAllapot = midiFelvevoTeszt.Allapot
        FelvetelGombFrissiteseTeszt()
    End Sub

    ' ================================================================
    '  IDEIGLENES TESZT-KÓD VÉGE
    ' ================================================================

End Class
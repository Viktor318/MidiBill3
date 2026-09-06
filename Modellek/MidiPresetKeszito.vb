Namespace Modellek

    ''' <summary>
    ''' Egy beolvasott .mid fájl tartalmának átalakítása MidiBill preseté
    ''' (Beallitasok objektummá) - a "Preset készítése MIDI fájlból"
    ''' segédprogram logikája.
    '''
    ''' Ez a MidiFajlSzerkeszto FORDÍTOTTJA: az nem beleírja, hanem ez
    ''' KIOLVASSA a csatorna-beállításokat egy MIDI fájlból.
    '''
    ''' A leképezés szabályait a felhasználóval egyeztettük (2026-09-05) -
    ''' a nem magától értetődő döntéseket az egyes lépéseknél magyarázzuk.
    ''' </summary>
    Public Module MidiPresetKeszito

        ''' <summary>
        ''' A preset ALAP HANGERŐ értéke, amit a MIDI fájlból készült
        ''' preseteknél használunk.
        '''
        ''' MIÉRT 100 ÉS NEM A SZOKÁSOS 70? A program íráskor nem a csatorna
        ''' nyers hangerejét küldi ki, hanem a "csatorna hangerő × alap hangerő
        ''' / 100" szorzatot. Visszaolvasáskor viszont csak a SZORZATOT látjuk,
        ''' a két tényezőt nem lehet szétválasztani. Ha az alap hangerő 100%,
        ''' akkor a csatorna hangereje pontosan a fájlban talált CC7-érték
        ''' lesz, és a preset visszatöltve UGYANAZT a hangerőt adja, mint ami a
        ''' fájlban van. A 70%-os alapértékkel a preset halkabban szólna, mint
        ''' az eredeti fájl.
        ''' </summary>
        Public Const ALAP_HANGERO As Integer = 100

        ''' <summary>
        ''' Presetet készít egy beolvasott MIDI fájl tartalmából.
        ''' </summary>
        ''' <param name="fajlAdat">A MidiFajlOlvaso.Beolvas() eredménye.</param>
        ''' <param name="hangszerListaFajl">A presetbe írandó hangszerlista fájlneve (pl. "GM.lst").</param>
        ''' <param name="verzio">A program verziószáma (tájékoztató mező a presetben).</param>
        Public Function PresetKeszitese(fajlAdat As MidiFajlAdat, hangszerListaFajl As String, verzio As String) As Beallitasok
            Dim b As New Beallitasok With {
                .Verzio = verzio,
                .HangszerListaFajl = hangszerListaFajl,
                .AlapHangero = ALAP_HANGERO,
                .Reverb = ReverbMeghatarozasa(fajlAdat)
            }

            ' A MIDI fájl ezeket az adatokat egyáltalán nem tartalmazza (a
            ' program játék közbeni logikájához tartoznak, nem MIDI-üzenetek),
            ' ezért az alapértékükön maradnak: split pont C4, nincs
            ' transzponálás, a pedál nem invertált. A MIDI be-/kimenet nevét
            ' szándékosan üresen hagyjuk - ugyanúgy, mint a régi .mb3 importnál
            ' -, hogy a preset betöltése ne írja felül a felhasználó aktuális
            ' eszközválasztását.
            b.SplitPont = 60
            b.Transzponalas = 0
            b.PedalInvertalva = False
            b.MidiBemenetNev = ""
            b.MidiKimenetNev = ""

            For csatornaIndex As Integer = 0 To 15
                b.Csatornak.Add(CsatornaKeszitese(fajlAdat.Csatornak(csatornaIndex)))
            Next

            Return b
        End Function

        ''' <summary>
        ''' Egy csatorna beállításainak leképezése.
        '''
        ''' AKTÍV az a csatorna, amelyen a fájlban TÉNYLEGESEN MEGSZÓLAL hang
        ''' (Note On, nullánál nagyobb sebességgel). Szándékosan szigorúbb ez a
        ''' szabály, mint a "MIDI fájlok módosítása" segédprogramban, ahol
        ''' bármilyen csatorna-üzenet elég: ott a tág definíció ártalmatlan
        ''' (legfeljebb fölösleges beállítás kerül a fájlba), itt viszont egy
        ''' kósza vezérlő-üzenet miatt aktívvá tett csatorna ténylegesen
        ''' megváltoztatná a játékot - minden leütés kimenne egy olyan
        ''' csatornára is, amelyen a fájlban egyetlen hang sem szól.
        '''
        ''' A NEM AKTÍV csatornák MINDEN mezője alapértéken marad (akkor is, ha
        ''' a fájl tartalmazna rájuk vezérlő-adatot) - ez a felhasználó
        ''' kifejezett kérése volt.
        ''' </summary>
        Private Function CsatornaKeszitese(csatornaAdat As MidiCsatornaAdat) As CsatornaBeallitas
            Dim cs As New CsatornaBeallitas()

            If csatornaAdat.HangokSzama = 0 Then Return cs   ' inaktív: végig alapérték

            cs.Aktiv = True

            ' Hangszín: a fájl ELSŐ hangszerváltása. Ha a fájl egyáltalán nem
            ' tartalmaz Program Change üzenetet erre a csatornára, marad az
            ' alapérték (0), ami a General MIDI szerint a zongora.
            If csatornaAdat.KezdoHangszer >= 0 Then
                cs.Hangszer = csatornaAdat.KezdoHangszer
            End If

            ' A vezérlők kezdőértéke, hiányzó adatnál a CsatornaBeallitas
            ' alapértékével (hangerő 127, a többi 0).
            cs.Hangero = csatornaAdat.VezerloErtek(CsatornaUzenetek.MAIN_VOLUME, cs.Hangero)
            cs.Vibrato = csatornaAdat.VezerloErtek(CsatornaUzenetek.MOD_WHEEL, cs.Vibrato)
            cs.Chorus = csatornaAdat.VezerloErtek(CsatornaUzenetek.CHORUS, cs.Chorus)

            ' A MIDI a panorámát 0-127 tartományban tárolja (64 = középen), a
            ' preset viszont -64..+63 formában.
            If csatornaAdat.VanVezerlo(CsatornaUzenetek.PAN) Then
                cs.Ter = csatornaAdat.VezerloErtek(CsatornaUzenetek.PAN, 64) - 64
            End If

            Return cs
        End Function

        ''' <summary>
        ''' A globális Reverb meghatározása. A MIDI-ben a visszhang
        ''' CSATORNÁNKÉNTI (CC91), a presetben viszont EGYETLEN, globális érték
        ''' - a felhasználó döntése szerint a LEGNAGYOBB talált érték nyer, így
        ''' biztosan nem vész el a fájlban meglévő legerősebb visszhang.
        '''
        ''' Az összes csatornát figyelembe vesszük (nem csak a hangot játszókat),
        ''' hiszen ez a beállítás amúgy is minden csatornára egységesen megy ki.
        ''' Az átszámítás 0-127-ből 0-100%-ba kerekítéssel történik.
        ''' </summary>
        Private Function ReverbMeghatarozasa(fajlAdat As MidiFajlAdat) As Integer
            Dim legnagyobb As Integer = -1

            For csatornaIndex As Integer = 0 To 15
                Dim csatornaAdat As MidiCsatornaAdat = fajlAdat.Csatornak(csatornaIndex)
                If Not csatornaAdat.VanVezerlo(CsatornaUzenetek.REVERB) Then Continue For

                Dim ertek As Integer = csatornaAdat.VezerloErtek(CsatornaUzenetek.REVERB, 0)
                If ertek > legnagyobb Then legnagyobb = ertek
            Next

            If legnagyobb < 0 Then Return 0   ' a fájl egyáltalán nem tartalmaz visszhang-beállítást

            Return CInt(Math.Round(legnagyobb * 100.0 / 127.0))
        End Function

    End Module

End Namespace

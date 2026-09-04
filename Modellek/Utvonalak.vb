Imports System.IO

Namespace Modellek

    ''' <summary>
    ''' A program által használt összes fájl-útvonal EGYETLEN központi helyen.
    '''
    ''' Miért kell ez? Korábban minden felhasználói adat (Kezdet.json,
    ''' HibaNaplo.txt, a hangszerlisták és a preset fájlok) a program saját
    ''' mappájába (Application.StartupPath), azon belül egy közös "Adatok"
    ''' mappába került. Ez fejlesztés közben működik, mert a bin\Debug mappa
    ''' írható - DE amint a program telepítve lesz (Inno Setup) a
    ''' C:\Program Files alá, ott egy normál jogosultságú felhasználó NEM ír.
    ''' Az írások ilyenkor CSENDBEN elhasalnának (a hibakezelő Catch ágak még
    ''' a hibaüzenetet is elnyelnék), és a felhasználó azt tapasztalná, hogy a
    ''' beállításai nem maradnak meg.
    '''
    ''' Az új, tiszta szétválasztás:
    '''
    ''' GYÁRI TARTALOM (a program mappájában, CSAK OLVASHATÓ) - ezt szállítja
    ''' a telepítő, és a program soha nem ír bele:
    '''   - GM.lst, GM.drm ... a beépített General MIDI hangszer- és
    '''     dobkészlet-lista, közvetlenül a program mappájában (NINCS többé
    '''     "Adatok" almappa)
    '''
    ''' FELHASZNÁLÓI TARTALOM (%AppData%\MidiBill3, ÍRHATÓ) - minden írás ide
    ''' megy:
    '''   - Kezdet.json ............ az induló/kilépéskori beállítások
    '''   - HibaNaplo.txt .......... a globális hibanapló
    '''   - Presetek\ .............. a mentett preset fájlok (.json)
    '''   - SF adatok\ ............. a SoundFont-ból importált vagy kézzel
    '''                              betöltött hangszerlisták (.lst / .drm)
    '''   - Felvetelek\ ............ a MIDI felvevő által mentett .mid
    '''                              fájlok (a felhasználó beállításától
    '''                              függően dátum szerinti almappákba
    '''                              rendezve)
    '''
    ''' A GM.lst és GM.drm KÜLÖNLEGES: ezek mindig a gyári mappából
    ''' olvasódnak, akkor is, ha a felhasználói mappában véletlenül lenne
    ''' ilyen nevű fájl - és a program meg is akadályozza, hogy ilyen néven
    ''' jöjjön létre felhasználói fájl (lásd GyariListaE).
    ''' </summary>
    Public Module Utvonalak

        Private Const KEZDET_FAJL_NEV As String = "Kezdet.json"
        Private Const HIBANAPLO_FAJL_NEV As String = "HibaNaplo.txt"
        Private Const PRESETEK_MAPPA_NEV As String = "Presetek"
        Private Const SF_ADATOK_MAPPA_NEV As String = "SF adatok"
        Private Const FELVETELEK_MAPPA_NEV As String = "Felvetelek"

        ''' <summary>
        ''' A mentett felvételek alapértelmezett fájlneve - ez az érték kerül
        ''' induláskor a felvevő ablak "Mentett fájl neve" mezőjébe, és erre
        ''' esünk vissza akkor is, ha a felhasználó üresen hagyja a mezőt.
        ''' </summary>
        Public Const ALAPERTELMEZETT_FELVETEL_NEV As String = "Felvetel"

        ' A régi (2026-09-03 előtti) közös mappa neve - már csak az egyszeri
        ' átköltöztetéshez kell, lásd Elokeszites().
        Private Const REGI_ADATOK_MAPPA_NEV As String = "Adatok"

        ''' <summary>
        ''' A GYÁRI, védett hangszerlista-fájlnevek. Ezek mindig a program
        ''' mappájából olvasódnak, és a felhasználó nem hozhat létre ilyen
        ''' nevű saját fájlt (sem hangszerlista-betöltéssel, sem
        ''' SoundFont-importtal) - így nem keletkezhet olyan árva fájl, amit a
        ''' program soha nem használna fel.
        ''' </summary>
        Private ReadOnly gyariListaNevek() As String = {"GM.lst", "GM.drm"}

        ''' <summary>A program saját (telepítési) mappája - CSAK OLVASHATÓNAK tekintjük</summary>
        Public ReadOnly Property ProgramMappa As String
            Get
                Return System.Windows.Forms.Application.StartupPath
            End Get
        End Property

        ''' <summary>%AppData%\MidiBill3 - minden írható felhasználói adat helye</summary>
        Public ReadOnly Property FelhasznaloiMappa As String
            Get
                Return Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "MidiBill3")
            End Get
        End Property

        ''' <summary>A mentett preset fájlok (.json) mappája</summary>
        Public ReadOnly Property PresetekMappa As String
            Get
                Return Path.Combine(FelhasznaloiMappa, PRESETEK_MAPPA_NEV)
            End Get
        End Property

        ''' <summary>Az importált hangszer-/dobkészlet-listák (.lst / .drm) mappája</summary>
        Public ReadOnly Property SfAdatokMappa As String
            Get
                Return Path.Combine(FelhasznaloiMappa, SF_ADATOK_MAPPA_NEV)
            End Get
        End Property

        ''' <summary>A MIDI felvevő által mentett .mid fájlok gyökérmappája</summary>
        Public ReadOnly Property FelvetelekMappa As String
            Get
                Return Path.Combine(FelhasznaloiMappa, FELVETELEK_MAPPA_NEV)
            End Get
        End Property

        ''' <summary>A Kezdet.json teljes útvonala (mindig a felhasználói mappában)</summary>
        Public ReadOnly Property KezdetFajl As String
            Get
                Return Path.Combine(FelhasznaloiMappa, KEZDET_FAJL_NEV)
            End Get
        End Property

        ''' <summary>A HibaNaplo.txt teljes útvonala (a felhasználói mappában)</summary>
        Public ReadOnly Property HibaNaploFajl As String
            Get
                Return Path.Combine(FelhasznaloiMappa, HIBANAPLO_FAJL_NEV)
            End Get
        End Property

        ''' <summary>
        ''' Egy ÚJ felvétel teljes mentési útvonalát állítja elő, és a
        ''' szükséges mappá(ka)t is létrehozza. A mentés SZÁNDÉKOSAN nem kér
        ''' útvonalat a felhasználótól (nincs SaveFileDialog) - a felvevő
        ''' ablakban csak a fájlnév ALAPJÁT és az almappázás módját lehet
        ''' megadni, a többit ez a függvény intézi.
        '''
        ''' A névképzés a datumSzerintiAlmappa kapcsolótól függ:
        ''' - True (a felvevő ablak alapértelmezése): a fájl a dátum szerinti
        '''   almappába kerül, és a névhez CSAK az időpont fűződik hozzá,
        '''   mert a dátum már az almappa nevében szerepel -
        '''   pl. "Felvetelek\2026-08-30\Felvetel_143025.mid"
        ''' - False: a fájl közvetlenül a Felvetelek mappába kerül, laposan,
        '''   és a névhez a TELJES dátum és időpont hozzáfűződik -
        '''   pl. "Felvetelek\Felvetel_2026-08-30_143025.mid"
        '''
        ''' Az időbélyeg MÁSODPERC-pontos: az Automatikus felvétel mód rövid
        ''' szünet-küszöbök mellett percenként több szegmenst is menthet,
        ''' másodperc nélkül ezek felülírnák egymást.
        ''' </summary>
        ''' <param name="alapNev">A felhasználó által megadott fájlnév-alap (a fájlnévben tiltott karakterek automatikusan aláhúzásra cserélődnek; üres név esetén az ALAPERTELMEZETT_FELVETEL_NEV lép életbe).</param>
        ''' <param name="datumSzerintiAlmappa">Igaz esetén dátum szerinti almappába rendez, lásd fent.</param>
        Public Function UjFelvetelUtvonala(alapNev As String, datumSzerintiAlmappa As Boolean) As String
            Dim most As DateTime = DateTime.Now
            Dim tisztaAlapNev As String = FajlnevAlapTisztitasa(alapNev)

            Dim celMappa As String
            Dim fajlNevAlap As String

            If datumSzerintiAlmappa Then
                celMappa = Path.Combine(FelvetelekMappa, most.ToString("yyyy-MM-dd", Globalization.CultureInfo.InvariantCulture))
                fajlNevAlap = tisztaAlapNev & "_" & most.ToString("HHmmss", Globalization.CultureInfo.InvariantCulture)
            Else
                celMappa = FelvetelekMappa
                fajlNevAlap = tisztaAlapNev & "_" & most.ToString("yyyy-MM-dd_HHmmss", Globalization.CultureInfo.InvariantCulture)
            End If

            Directory.CreateDirectory(celMappa)

            Return UtkozesmentesUtvonal(celMappa, fajlNevAlap, ".mid")
        End Function

        ''' <summary>
        ''' A felhasználó által beírt fájlnév-alapot teszi biztonságossá: a
        ''' Windows-on tiltott fájlnév-karaktereket (\ / : * ? " &lt; &gt; | stb.)
        ''' aláhúzásra cseréli, és üres/csak whitespace név esetén az
        ''' alapértelmezett névre esik vissza. Enélkül egy ártatlanul beírt
        ''' "Próba: 1. rész" névtől a mentés kezeletlen kivétellel elszállna.
        ''' </summary>
        Private Function FajlnevAlapTisztitasa(alapNev As String) As String
            Dim tiszta As String = If(alapNev, "").Trim()

            For Each tiltottKarakter As Char In Path.GetInvalidFileNameChars()
                tiszta = tiszta.Replace(tiltottKarakter, "_"c)
            Next

            If tiszta.Length = 0 Then Return ALAPERTELMEZETT_FELVETEL_NEV
            Return tiszta
        End Function

        ''' <summary>
        ''' Ütközésmentes fájlútvonalat ad vissza: ha a kívánt név már
        ''' létezik, "_2", "_3" ... utótaggal próbálkozik tovább. A
        ''' másodperc-pontos időbélyeg mellett ez ritkán fordulhat elő (két
        ''' mentés ugyanabban a másodpercben), de így SOHA nem írunk felül
        ''' egy korábbi felvételt.
        ''' </summary>
        Private Function UtkozesmentesUtvonal(mappa As String, fajlNevAlap As String, kiterjesztes As String) As String
            Dim utvonal As String = Path.Combine(mappa, fajlNevAlap & kiterjesztes)
            Dim sorszam As Integer = 2

            While File.Exists(utvonal)
                utvonal = Path.Combine(mappa, fajlNevAlap & "_" & sorszam.ToString(Globalization.CultureInfo.InvariantCulture) & kiterjesztes)
                sorszam += 1
            End While

            Return utvonal
        End Function

        ''' <summary>
        ''' Igaz, ha a megadott fájlnév a GYÁRI, védett listák egyike
        ''' (GM.lst / GM.drm). A hívó teljes útvonalat is átadhat, csak a
        ''' fájlnév részt vizsgáljuk.
        ''' </summary>
        Public Function GyariListaE(fajlNev As String) As Boolean
            If String.IsNullOrEmpty(fajlNev) Then Return False
            Dim csakNev As String = Path.GetFileName(fajlNev)
            For Each gyariNev As String In gyariListaNevek
                If String.Equals(csakNev, gyariNev, StringComparison.OrdinalIgnoreCase) Then Return True
            Next
            Return False
        End Function

        ''' <summary>
        ''' Egy hangszerlista (.lst / .drm) OLVASÁSI útvonala: a gyári listák
        ''' (GM.lst / GM.drm) MINDIG a program mappájából jönnek, minden más a
        ''' felhasználói "SF adatok" mappából.
        ''' </summary>
        Public Function HangszerListaOlvasasra(fajlNev As String) As String
            If GyariListaE(fajlNev) Then Return Path.Combine(ProgramMappa, fajlNev)
            Return Path.Combine(SfAdatokMappa, fajlNev)
        End Function

        ''' <summary>
        ''' Egy hangszerlista ÍRÁSI útvonala - ez MINDIG a felhasználói
        ''' "SF adatok" mappa. A hívónak előbb ellenőriznie kell a
        ''' GyariListaE()-vel, hogy nem gyári névvel próbál-e írni!
        ''' </summary>
        Public Function HangszerListaIrasra(fajlNev As String) As String
            Directory.CreateDirectory(SfAdatokMappa)
            Return Path.Combine(SfAdatokMappa, fajlNev)
        End Function

        ''' <summary>
        ''' Igaz, ha a megadott fájl MÁR a végleges helyén van: a gyári listák
        ''' a program mappájában, minden más hangszerlista az "SF adatok"
        ''' mappában. Ilyenkor a hangszerlista-betöltésnél nincs mit másolni.
        ''' </summary>
        Public Function MarAHelyenVanE(teljesUtvonal As String) As Boolean
            Dim mappa As String = Path.GetDirectoryName(teljesUtvonal)
            If String.IsNullOrEmpty(mappa) Then Return False

            If GyariListaE(teljesUtvonal) Then
                Return MappakEgyeznekE(mappa, ProgramMappa)
            End If
            Return MappakEgyeznekE(mappa, SfAdatokMappa)
        End Function

        Private Function MappakEgyeznekE(egyik As String, masik As String) As Boolean
            Return String.Equals(
                egyik.TrimEnd(Path.DirectorySeparatorChar),
                masik.TrimEnd(Path.DirectorySeparatorChar),
                StringComparison.OrdinalIgnoreCase)
        End Function

        ''' <summary>
        ''' A felhasználói mappák létrehozása és a RÉGI szerkezetből való
        ''' egyszeri átköltöztetés. A MainForm_Load legelején kell hívni,
        ''' MINDEN más előtt.
        '''
        ''' Két dolgot költöztet, mindkettőt csak akkor, ha az új helyen még
        ''' nincs meg (tehát ismételt indításnál már nem csinál semmit, és
        ''' sosem ír felül újabb felhasználói adatot):
        ''' 1. a program mappájában maradt Kezdet.json-t,
        ''' 2. a régi, közös "Adatok" mappa tartalmát: a .json fájlokat a
        '''    Presetek, a .lst/.drm fájlokat az "SF adatok" mappába (a gyári
        '''    GM.lst/GM.drm kihagyásával, mert azok a program mappájában
        '''    maradnak; és minden más kiterjesztés kihagyásával, pl. a
        '''    fejlesztéshez használt .sf2 tesztfájl).
        '''
        ''' A régi Adatok mappát SZÁNDÉKOSAN nem törli - maradjon biztonsági
        ''' másolatnak, amíg a felhasználó meg nem győződik róla, hogy minden
        ''' rendben átjött.
        ''' </summary>
        Public Sub Elokeszites()
            Try
                Directory.CreateDirectory(FelhasznaloiMappa)
                Directory.CreateDirectory(PresetekMappa)
                Directory.CreateDirectory(SfAdatokMappa)
                ' A Felvetelek mappát is már induláskor létrehozzuk (nem csak
                ' az első mentésnél), hogy a lejátszás fájlválasztó dialógusa
                ' egy létező mappára tudjon nyílni, és a felhasználó akkor is
                ' megtalálja a helyét, ha még egyetlen felvétele sincs.
                Directory.CreateDirectory(FelvetelekMappa)

                Dim regiKezdetFajl As String = Path.Combine(ProgramMappa, KEZDET_FAJL_NEV)
                If File.Exists(regiKezdetFajl) AndAlso Not File.Exists(KezdetFajl) Then
                    File.Copy(regiKezdetFajl, KezdetFajl)
                End If

                RegiAdatokMappaAtkoltoztetese()
            Catch ex As Exception
                ' Ha a felhasználói mappa nem hozható létre (nagyon szokatlan),
                ' a program ettől még elindul - a mentések a saját hibakezelésük
                ' szerint fognak elhasalni, nem itt akad meg az indulás.
            End Try
        End Sub

        ''' <summary>A régi, közös "Adatok" mappa tartalmának szétosztása az új helyekre</summary>
        Private Sub RegiAdatokMappaAtkoltoztetese()
            Dim regiAdatokMappa As String = Path.Combine(ProgramMappa, REGI_ADATOK_MAPPA_NEV)
            If Not Directory.Exists(regiAdatokMappa) Then Return

            For Each forrasUtvonal As String In Directory.GetFiles(regiAdatokMappa)
                Dim fajlNev As String = Path.GetFileName(forrasUtvonal)
                Dim kiterjesztes As String = Path.GetExtension(fajlNev).ToLowerInvariant()
                Dim celUtvonal As String = ""

                If kiterjesztes = ".json" Then
                    celUtvonal = Path.Combine(PresetekMappa, fajlNev)
                ElseIf kiterjesztes = ".lst" OrElse kiterjesztes = ".drm" Then
                    If GyariListaE(fajlNev) Then Continue For
                    celUtvonal = Path.Combine(SfAdatokMappa, fajlNev)
                Else
                    Continue For
                End If

                Try
                    If Not File.Exists(celUtvonal) Then File.Copy(forrasUtvonal, celUtvonal)
                Catch ex As Exception
                    ' Egyetlen fájl másolási hibája ne akassza meg a többit
                End Try
            Next
        End Sub

    End Module

End Namespace

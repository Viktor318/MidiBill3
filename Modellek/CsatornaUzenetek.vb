Namespace Modellek

    ''' <summary>
    ''' Egyetlen, kész MIDI csatorna-üzenet (státuszbájt + legfeljebb két
    ''' adatbájt). Azért kell külön típus, hogy a csatorna-állapot
    ''' üzenetsorozatát egyszer lehessen előállítani, és utána a hívó
    ''' döntse el, mit kezd vele: KIKÜLDI egy MIDI eszközre (MainForm),
    ''' vagy BEÍRJA egy .mid fájlba (MidiFajlSzerkeszto, MidiFelvevo).
    ''' </summary>
    Public Structure MidiUzenet

        Public ReadOnly StatuszBajt As Integer
        Public ReadOnly Adat1 As Integer
        Public ReadOnly Adat2 As Integer

        ''' <summary>Hány adatbájtja van? A Program Change egybájtos, a Controller Change kétbájtos.</summary>
        Public ReadOnly AdatbajtokSzama As Integer

        Public Sub New(statuszBajt As Integer, adat1 As Integer, adat2 As Integer, adatbajtokSzama As Integer)
            Me.StatuszBajt = statuszBajt
            Me.Adat1 = adat1
            Me.Adat2 = adat2
            Me.AdatbajtokSzama = adatbajtokSzama
        End Sub

        ''' <summary>A MIDI csatorna sorszáma 0-tól (a státuszbájt alsó négy bitje).</summary>
        Public ReadOnly Property Csatorna As Integer
            Get
                Return StatuszBajt And &HF
            End Get
        End Property

    End Structure

    ''' <summary>
    ''' Egy csatorna teljes MIDI-állapotát leíró üzenetsorozat előállítása -
    ''' EGYETLEN, közös helyen.
    '''
    ''' MIÉRT KELL EZ? Ugyanezt az üzenetsorozatot HÁROM helyen használjuk:
    '''   1. MainForm.AktivCsatornakAllapotanakKuldese - élő kiküldés a
    '''      szintetizátorra ("Extra MIDI üzenetek" funkció),
    '''   2. MidiFajlSzerkeszto - meglévő .mid fájlba beszúrás
    '''      ("MIDI fájlok módosítása" segédprogram),
    '''   3. MidiFelvevo - az új felvétel elejére írás.
    ''' Korábban ez a logika két helyen, egymástól függetlenül létezett -
    ''' ha bővül (pl. egy új CC), így már csak EGY helyen kell módosítani,
    ''' és nem fordulhat elő, hogy az élőben kiküldött állapot eltér attól,
    ''' amit a fájlokba írunk.
    ''' </summary>
    Public Module CsatornaUzenetek

        ' A csatorna-állapotot leíró MIDI Controller Change számok. SZÁNDÉKOSAN
        ' itt, egy helyen vannak - korábban a MainForm és a MidiFajlSzerkeszto
        ' is külön-külön deklarálta ugyanezeket.
        Public Const BANK_SELECT_MSB As Integer = 0
        Public Const MOD_WHEEL As Integer = 1          ' "Vibrato" a felületen
        Public Const MAIN_VOLUME As Integer = 7
        Public Const PAN As Integer = 10               ' "Tér" a felületen
        Public Const BANK_SELECT_LSB As Integer = 32
        Public Const REVERB As Integer = 91            ' "Visszhang" a felületen
        Public Const CHORUS As Integer = 93

        Private Const CONTROLLER_CHANGE_STATUS As Integer = &HB0
        Private Const PROGRAM_CHANGE_STATUS As Integer = &HC0

        ''' <summary>
        ''' Egy csatorna teljes állapotát leíró 8 MIDI üzenet, MINDIG ebben a
        ''' sorrendben: Bank Select MSB (CC0) + LSB (CC32), Program Change,
        ''' hangerő (CC7), tér (CC10), vibrato (CC1), chorus (CC93), reverb
        ''' (CC91).
        '''
        ''' A Bank Select párost MINDIG kiküldjük a Program Change előtt, még
        ''' sima GM hangszernél is (ahol mindkét bájt 0) - egyszerű,
        ''' konzisztens viselkedés, és ez teszi lehetővé a General MIDI-n
        ''' túli, Bank Select-tel elérhető hangszerek (Roland GS, Yamaha XG,
        ''' General MIDI 2, egyedi SoundFont-bankok) használatát. A kulcs
        ''' szétbontását lásd Modellek\Hangszerkulcs.vb.
        ''' </summary>
        ''' <param name="csatornaIndex">A csatorna sorszáma 0-tól (0 = 1. csatorna).</param>
        ''' <param name="adat">A csatorna beállításai.</param>
        ''' <param name="alapHangeroSzazalek">A globális "Alap hangerő" csúszka értéke (0-100%), ami a csatorna saját hangerejét szorozza.</param>
        ''' <param name="reverbSzazalek">A globális "Visszhang" csúszka értéke (0-100%), ami 0-127-re számítódik át.</param>
        Public Function CsatornaAllapotUzenetei(csatornaIndex As Integer, adat As CsatornaBeallitas,
                                                alapHangeroSzazalek As Integer, reverbSzazalek As Integer) As List(Of MidiUzenet)

            Dim ccStatusz As Integer = CONTROLLER_CHANGE_STATUS Or csatornaIndex
            Dim pcStatusz As Integer = PROGRAM_CHANGE_STATUS Or csatornaIndex

            ' A kulcs szétbontása egy helyen történik - lásd Modellek\Hangszerkulcs.vb.
            Dim bankMsb As Integer = Hangszerkulcs.BankMsb(adat.Hangszer)
            Dim bankLsb As Integer = Hangszerkulcs.BankLsb(adat.Hangszer)
            Dim program As Integer = Hangszerkulcs.ProgramSzam(adat.Hangszer)
            Dim hangeroVegso As Integer = CInt(adat.Hangero * (alapHangeroSzazalek / 100.0))
            Dim reverbErtek As Integer = reverbSzazalek * 127 \ 100

            Return New List(Of MidiUzenet) From {
                New MidiUzenet(ccStatusz, BANK_SELECT_MSB, Adatbajt(bankMsb), 2),
                New MidiUzenet(ccStatusz, BANK_SELECT_LSB, Adatbajt(bankLsb), 2),
                New MidiUzenet(pcStatusz, Adatbajt(program), 0, 1),
                New MidiUzenet(ccStatusz, MAIN_VOLUME, Adatbajt(hangeroVegso), 2),
                New MidiUzenet(ccStatusz, PAN, Adatbajt(adat.Ter + 64), 2),
                New MidiUzenet(ccStatusz, MOD_WHEEL, Adatbajt(adat.Vibrato), 2),
                New MidiUzenet(ccStatusz, CHORUS, Adatbajt(adat.Chorus), 2),
                New MidiUzenet(ccStatusz, REVERB, Adatbajt(reverbErtek), 2)
            }
        End Function

        ''' <summary>
        ''' MINDEN aktív csatorna állapot-üzenetei egyetlen listában, a
        ''' csatornák sorrendjében. Az inaktív csatornákat kihagyja - egy
        ''' olyan csatornát nincs értelme beállítani, amin nem szól semmi.
        ''' </summary>
        ''' <param name="csatornak">A 16 csatorna beállításai (index 0 = 1. csatorna).</param>
        Public Function AktivCsatornakAllapotUzenetei(csatornak As IList(Of CsatornaBeallitas),
                                                      alapHangeroSzazalek As Integer, reverbSzazalek As Integer) As List(Of MidiUzenet)

            Dim eredmeny As New List(Of MidiUzenet)
            Dim csatornaSzam As Integer = Math.Min(16, csatornak.Count)

            For c As Integer = 0 To csatornaSzam - 1
                Dim adat As CsatornaBeallitas = csatornak(c)
                If adat Is Nothing OrElse Not adat.Aktiv Then Continue For
                eredmeny.AddRange(CsatornaAllapotUzenetei(c, adat, alapHangeroSzazalek, reverbSzazalek))
            Next

            Return eredmeny
        End Function

        ''' <summary>
        ''' Egy MIDI adatbájt (0-127) tartományra szorítása - a MIDI szabvány
        ''' szerint az adatbájtok felső bitje mindig 0. Enélkül egy sérült
        ''' preset tartományon kívüli értéke érvénytelen MIDI üzenetet
        ''' eredményezne (fájlba írásnál pedig a CByte konverzió kivételt
        ''' dobna).
        ''' </summary>
        Public Function Adatbajt(ertek As Integer) As Integer
            Return Math.Max(0, Math.Min(127, ertek))
        End Function

    End Module

End Namespace

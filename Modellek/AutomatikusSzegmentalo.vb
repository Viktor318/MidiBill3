Namespace Modellek

    ''' <summary>Az Automatikus felvétel mód döntése egy szegmensről.</summary>
    Public Enum SzegmensDontes
        ''' <summary>Nincs teendő: valami szól, vagy a csend még rövidebb a szünet-küszöbnél.</summary>
        Semmi = 0
        ''' <summary>A szünet-küszöb letelt, de a játék rövidebb volt a játékidő-küszöbnél: nem érdemel fájlt.</summary>
        Eldobas = 1
        ''' <summary>A szünet-küszöb letelt, és a játék elérte a játékidő-küszöböt: mentés.</summary>
        Mentes = 2
    End Enum

    ''' <summary>
    ''' Az Automatikus felvétel mód (a felvétel 4. fázisa) DÖNTÉSE, felület
    ''' nélkül - a felvevő ablak csak végrehajtja (mentés / eldobás, utána
    ''' azonnali újra-felfegyverzés), így a szabály önmagában tesztelhető.
    '''
    ''' A SZABÁLY (a felhasználóval egyeztetve, 2026-09-24/25):
    ''' - A szünet NEM az utolsó leütéstől számít, hanem attól a pillanattól,
    '''   amikor már SEMMI nem szól: minden billentyű fel van engedve ÉS a
    '''   kitartó pedál is fent van (lásd MidiFelvevo.CsendKezdeteMs). Amíg
    '''   valami szól, nincs döntés.
    ''' - Ha a csend elérte a SZÜNET-küszöböt: a játék hossza (az első és az
    '''   utolsó rögzített esemény között, MidiFelvevo.RogzitettHosszMs) dönt -
    '''   ha elérte a JÁTÉKIDŐ-küszöböt, mentés; ha rövidebb, eldobás.
    ''' - A küszöbök "elérte" jelentésűek (>=): pontosan 5 000 ms csend 5 mp-es
    '''   szünet-küszöbnél már lezár.
    '''
    ''' MINDKÉT oldalnak ugyanarról az óráról kell olvasnia: a mostMs a
    ''' MidiEngine.ElteltIdoMs (az időzítőből), illetve egy beérkező üzenet
    ''' saját időbélyege (ami szintén erről az óráról származik).
    ''' </summary>
    Public Module AutomatikusSzegmentalo

        ''' <summary>A döntés a nyers értékekből (a tesztekhez és a felvevőt használó változathoz).</summary>
        Public Function Dontes(allapot As FelvevoAllapot, csendKezdeteMs As Long, rogzitettHosszMs As Long,
                               mostMs As Long, szunetKuszobMs As Long, jatekidoKuszobMs As Long) As SzegmensDontes
            If allapot <> FelvevoAllapot.Rogzit Then Return SzegmensDontes.Semmi
            If csendKezdeteMs < 0 Then Return SzegmensDontes.Semmi
            If mostMs - csendKezdeteMs < szunetKuszobMs Then Return SzegmensDontes.Semmi
            If rogzitettHosszMs >= jatekidoKuszobMs Then Return SzegmensDontes.Mentes
            Return SzegmensDontes.Eldobas
        End Function

        ''' <summary>A döntés egy futó felvevő aktuális állapota alapján.</summary>
        Public Function Dontes(felvevo As MidiFelvevo, mostMs As Long,
                               szunetKuszobMs As Long, jatekidoKuszobMs As Long) As SzegmensDontes
            Return Dontes(felvevo.Allapot, felvevo.CsendKezdeteMs, felvevo.RogzitettHosszMs,
                          mostMs, szunetKuszobMs, jatekidoKuszobMs)
        End Function

    End Module

End Namespace

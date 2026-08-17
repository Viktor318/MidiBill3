Namespace Modellek

    ''' <summary>
    ''' MIDI hangszám (0-127) -> emberi olvasható hangnév (pl. "C4") konverzió,
    ''' nemzetközi/angol konvencióval (C, C#, D... a legtöbb zenei szoftver ezt
    ''' használja, nem a magyar szolmizációt). A MIDI 60-as szám = C4 (középső C).
    ''' </summary>
    Public Module Hangnevek

        Private ReadOnly Nevek() As String = {"C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"}

        Public Function HangNev(szam As Integer) As String
            Dim oktav As Integer = (szam \ 12) - 1
            Dim index As Integer = szam Mod 12
            Return Nevek(index) & oktav.ToString()
        End Function

    End Module

End Namespace
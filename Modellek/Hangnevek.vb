Namespace Modellek

    ''' <summary>
    ''' MIDI hangszám (0-127) -> emberi olvasható hangnév (pl. "C4") konverzió,
    ''' nemzetközi/angol konvencióval (C, C#, D... a legtöbb zenei szoftver ezt
    ''' használja, nem a magyar szolmizációt). A MIDI 60-as szám = C4 (középső C).
    ''' </summary>
    Public Module Hangnevek

        Private ReadOnly Nevek() As String = {"C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"}

        ''' <summary>
        ''' MIDI hangszám -> hangnév. Az ÉRVÉNYES MIDI-tartományon (0-127)
        ''' kívüli bemenetre (pl. egy kézzel szerkesztett, hibás preset JSON
        ''' negatív SplitPont értékével) nem MIDI-hangnevet ad vissza, hanem
        ''' magát a számot - enélkül egy negatív szám a "Mod 12" művelet miatt
        ''' negatív indexet adna, ami kezeletlen IndexOutOfRangeException-t
        ''' dobna a Nevek tömbön.
        ''' </summary>
        Public Function HangNev(szam As Integer) As String
            If szam < 0 OrElse szam > 127 Then Return szam.ToString()
            Dim oktav As Integer = (szam \ 12) - 1
            Dim index As Integer = szam Mod 12
            Return Nevek(index) & oktav.ToString()
        End Function

    End Module

End Namespace
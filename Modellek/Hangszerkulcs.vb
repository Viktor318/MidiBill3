Namespace Modellek

    ''' <summary>
    ''' A hangszer-azonosító KULCS kezelése - EGYETLEN helyen.
    '''
    ''' MI EZ? A program egyetlen egész számmal azonosít egy hangszert, mert
    ''' így fér el egy Dictionary kulcsában (HangszerLista), egy JSON mezőben
    ''' (CsatornaBeallitas.Hangszer) és egy legördülő elem mögött
    ''' (HangszerPanel) is. Ez a szám HÁROM MIDI-értéket fog össze:
    '''
    '''   kulcs = BankSelectMSB * 16384 + BankSelectLSB * 128 + programszám
    '''
    ''' MIÉRT KELL MINDKÉT BANK-BÁJT? Mert a szabványok MÁSHOL tárolják a
    ''' hangszer-variációt:
    ''' - Roland GS: a variáció az MSB-ben (az LSB a hangkészlet-térkép),
    ''' - Yamaha XG: az MSB a kategória (0 = dallam, 64 = effekt,
    '''   126/127 = dob), a variáció az LSB-ben,
    ''' - General MIDI 2: MSB = 121 (dallam) / 120 (dob), variáció az LSB-ben.
    ''' Egyetlen bankszámmal ezek egy része elérhetetlen - lásd az
    ''' állapot-dokumentum "Következő lépések" 13. pontját.
    '''
    ''' VISSZAMENŐLEGES KOMPATIBILITÁS: a korábbi képlet
    ''' "bank * 128 + programszám" volt, ahol a "bank" a Bank Select LSB-t
    ''' jelentette. A fenti képletbe MSB = 0-t behelyettesítve PONTOSAN
    ''' ugyanaz a szám jön ki - ezért a meglévő preset (.json) és
    ''' hangszerlista (.lst) fájlok migrálás nélkül érvényesek maradnak.
    '''
    ''' A modul SZÁNDÉKOSAN nem ellenőrzi a tartományt: a nyers számokat úgy
    ''' fűzi össze és bontja szét, ahogy kapja. Az érvényesség eldöntése a
    ''' BEOLVASÓK dolga (lásd HangszerLista .lst-elemzése és a
    ''' SoundFontOlvaso), mert ott van értelmes válasz arra, mit tegyünk egy
    ''' hibás sorral.
    ''' </summary>
    Public Module Hangszerkulcs

        ''' <summary>
        ''' Egy MIDI adatbájt legnagyobb értéke: a Bank Select MSB, az LSB és
        ''' a programszám is 0-127 közé esik. A .lst-elemzés ezzel dönti el,
        ''' érvényes-e egy sor.
        ''' </summary>
        Public Const MAX_ADATBAJT As Integer = 127

        ''' <summary>Ennyivel szorozzuk a Bank Select LSB-t a kulcsban (= a programszámok darabszáma).</summary>
        Private Const LSB_SULY As Integer = 128

        ''' <summary>Ennyivel szorozzuk a Bank Select MSB-t a kulcsban (= 128 * 128).</summary>
        Private Const MSB_SULY As Integer = 16384

        ''' <summary>
        ''' A legnagyobb érvényes kulcs (MSB = LSB = program = 127). A
        ''' preset-betöltés tartomány-ellenőrzése ezt használja: a korábbi
        ''' 16383-as határ levágta volna az MSB-t tartalmazó kulcsokat.
        ''' </summary>
        Public Const MAX_KULCS As Integer = 127 * MSB_SULY + 127 * LSB_SULY + 127

        ''' <summary>A három MIDI-érték összefűzése egyetlen kulccsá.</summary>
        ''' <param name="bankMsb">Bank Select MSB (CC0), 0-127.</param>
        ''' <param name="bankLsb">Bank Select LSB (CC32), 0-127.</param>
        ''' <param name="program">Program Change értéke, 0-127.</param>
        Public Function Kulcs(bankMsb As Integer, bankLsb As Integer, program As Integer) As Integer
            Return bankMsb * MSB_SULY + bankLsb * LSB_SULY + program
        End Function

        ''' <summary>A kulcs Bank Select MSB (CC0) része.</summary>
        Public Function BankMsb(kulcs As Integer) As Integer
            Return kulcs \ MSB_SULY
        End Function

        ''' <summary>A kulcs Bank Select LSB (CC32) része.</summary>
        Public Function BankLsb(kulcs As Integer) As Integer
            Return (kulcs \ LSB_SULY) Mod LSB_SULY
        End Function

        ''' <summary>A kulcs programszám (Program Change) része.</summary>
        Public Function ProgramSzam(kulcs As Integer) As Integer
            Return kulcs Mod LSB_SULY
        End Function

        ''' <summary>
        ''' A bank RÖVID jelölése a hangszernév mögé, a legördülő listákhoz:
        ''' üres, ha nincs bank; " (B8)", ha csak LSB van; " (B121.1)", ha
        ''' MSB is. A rövid alak azért fontos, mert a legördülőben kevés a
        ''' hely - a jelentésben ennél bővebb szöveg áll (lásd MidiJelentes).
        ''' </summary>
        Public Function BankJeloles(kulcs As Integer) As String
            Dim msb As Integer = BankMsb(kulcs)
            Dim lsb As Integer = BankLsb(kulcs)

            If msb = 0 AndAlso lsb = 0 Then Return ""
            If msb = 0 Then Return " (B" & lsb & ")"
            Return " (B" & msb & "." & lsb & ")"
        End Function

    End Module

End Namespace

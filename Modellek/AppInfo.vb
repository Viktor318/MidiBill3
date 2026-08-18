Namespace Modellek

    ''' <summary>
    ''' A futó assembly verziószámát adja vissza "Major.Minor.Build" formában
    ''' (pl. "1.3.0"). Közös hely, hogy a fő program és a segédprogramok
    ''' (PresetKonvertalo, Mb3Konvertalo) is ugyanazt a logikát használják.
    ''' FONTOS: mindhárom projekt .vbproj fájljában a &lt;Version&gt; elemnek
    ''' szinkronban kell maradnia, különben a segédprogramok saját, eltérő
    ''' verziószámot írnának ki - verziólépéskor mindhárom .vbproj-t frissíteni kell!
    ''' </summary>
    Public Module AppInfo

        Public Function AppVerzio() As String
            Dim verzio As Version = Reflection.Assembly.GetExecutingAssembly().GetName().Version
            Return verzio.Major & "." & verzio.Minor & "." & verzio.Build
        End Function

    End Module

End Namespace
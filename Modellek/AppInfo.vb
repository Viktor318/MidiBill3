Namespace Modellek

    ''' <summary>
    ''' A futó assembly verziószámát adja vissza "Major.Minor.Build" formában
    ''' (pl. "1.3.0"). Közös hely, hogy a fő program és a segédprogramok
    ''' (PresetKonvertalo, Mb3Konvertalo) is ugyanazt a logikát használják.
    ''' A verziószám forrása a Directory.Build.props (megoldás gyökere) - onnan
    ''' örökli mindhárom projekt automatikusan, nincs szükség kézi szinkronizálásra.
    ''' </summary>
    Public Module AppInfo

        Public Function AppVerzio() As String
            Dim verzio As Version = Reflection.Assembly.GetExecutingAssembly().GetName().Version
            Return verzio.Major & "." & verzio.Minor & "." & verzio.Build
        End Function

    End Module

End Namespace
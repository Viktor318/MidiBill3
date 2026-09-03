Namespace Modellek

    ''' <summary>
    ''' A futó assembly verziószámát adja vissza "Major.Minor.Build" formában
    ''' (pl. "1.11.0"). A verziószám forrása a MidiBill3.vbproj &lt;Version&gt;
    ''' eleme - onnan generálja a .NET SDK build közben automatikusan az
    ''' assembly-verziót, nincs szükség kézi szinkronizálásra.
    ''' </summary>
    Public Module AppInfo

        Public Function AppVerzio() As String
            Dim verzio As Version = Reflection.Assembly.GetExecutingAssembly().GetName().Version
            Return verzio.Major & "." & verzio.Minor & "." & verzio.Build
        End Function

    End Module

End Namespace
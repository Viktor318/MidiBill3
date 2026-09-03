Namespace My

    Partial Friend Class MyApplication

        Private Sub MyApplication_UnhandledException(sender As Object, e As Microsoft.VisualBasic.ApplicationServices.UnhandledExceptionEventArgs) Handles Me.UnhandledException
            HibaNaplozasa(e.Exception, "UI szál (My.Application.UnhandledException)")
            e.ExitApplication = True
        End Sub

        Private Sub MyApplication_Startup(sender As Object, e As Microsoft.VisualBasic.ApplicationServices.StartupEventArgs) Handles Me.Startup
            ' Háttérszálakon (pl. NAudio) keletkező, egyébként végzetes kivételek naplózása
            AddHandler AppDomain.CurrentDomain.UnhandledException,
                Sub(s As Object, ex As UnhandledExceptionEventArgs)
                    HibaNaplozasa(TryCast(ex.ExceptionObject, Exception), "Háttérszál (AppDomain.UnhandledException)")
                End Sub
        End Sub

        Private Sub HibaNaplozasa(ex As Exception, forras As String)
            Try
                ' A napló a FELHASZNÁLÓI mappába (%AppData%\MidiBill3) kerül, nem a
                ' program mappájába: telepítés (Program Files) után oda nem lehetne
                ' írni, és ez a Catch ág még a hibaüzenetet is elnyelné - vagyis a
                ' hibanaplózás pont akkor mondana csődöt, amikor a legnagyobb
                ' szükség lenne rá. A mappát azért is hozzuk létre itt, mert ez a
                ' kód lefuthat a MainForm_Load (Utvonalak.Elokeszites) ELŐTT is.
                IO.Directory.CreateDirectory(Modellek.Utvonalak.FelhasznaloiMappa)
                Dim naploFajl As String = Modellek.Utvonalak.HibaNaploFajl
                Dim sorok As String =
                    "==================================================" & vbCrLf &
                    "Időpont: " & DateTime.Now.ToString("yyyy.MM.dd HH:mm:ss") & vbCrLf &
                    "Forrás: " & forras & vbCrLf &
                    "Típus: " & If(ex?.GetType().FullName, "(ismeretlen)") & vbCrLf &
                    "Üzenet: " & If(ex?.Message, "(nincs)") & vbCrLf &
                    "Stack trace:" & vbCrLf & If(ex?.StackTrace, "(nincs)") & vbCrLf

                IO.File.AppendAllText(naploFajl, sorok)

                MsgBox("Váratlan hiba történt, a program bezáródik." & vbCrLf &
                       "A részletek a HibaNaplo.txt fájlba kerültek." & vbCrLf & vbCrLf &
                       "Hiba: " & If(ex?.Message, "(ismeretlen)"),
                       MsgBoxStyle.Critical, "MidiBill - váratlan hiba")
            Catch
                ' Ha még a naplózás is elhasal, ne akadjon el ezen a program bezárása
            End Try
        End Sub

    End Class

End Namespace
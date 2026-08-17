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
                Dim naploFajl As String = IO.Path.Combine(System.Windows.Forms.Application.StartupPath, "HibaNaplo.txt")
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
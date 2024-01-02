Imports System.Linq
Imports System.Globalization
Imports System.Net
Imports System.Runtime.InteropServices

Namespace Networking.Address
    Public Module IPParser
        Public Function TryParse(ip As String, <Out> ByRef info As IPInfo) As Boolean


            Dim portNum As Integer = Nothing, ipObj As IPAddress = Nothing, ipValue As IPAddress = Nothing
            Try
                If String.IsNullOrWhiteSpace(ip) Then
                    info = Nothing
                    Return False
                End If

                If ip.Count(Function(c) c Is ":"c) > 1 Then
                    info = Nothing
                    Return False
                End If

                If ip.Contains(":") Then
                    Dim ipParts = ip.Split(":"c)

                    If ipParts.Length <> 2 OrElse Not Integer.TryParse(ipParts(1), NumberStyles.Any, CultureInfo.InvariantCulture, portNum) Then
                        info = Nothing
                        Return False
                    End If

                    If Equals(ipParts(0).ToLower(), "localhost") Then ipParts(0) = "127.0.0.1"

                    If Not IPAddress.TryParse(ip, ipObj) Then
                        info = Nothing
                        Return False
                    End If

                    info = New IPInfo([GetType](ipObj), portNum, ipObj)
                    Return True
                ElseIf Equals(ip.ToLower(), "localhost") Then
                    ip = "127.0.0.1"
                End If

                If Not IPAddress.TryParse(ip, ipValue) Then
                    info = Nothing
                    Return False
                End If

                info = New IPInfo([GetType](ipValue), 0, ipValue)
                Return True
            Catch
                info = Nothing
                Return False
            End Try
        End Function

        Private Function [GetType](address As IPAddress) As IPType
            Dim ipStr = address.ToString()

            If Equals(ipStr, IPAddress.Any.ToString()) OrElse Equals(ipStr, IPAddress.Loopback.ToString()) Then Return IPType.Local

            Return IPType.Remote
        End Function
    End Module
End Namespace

Imports System.Net

Namespace Networking.Address
    Public Structure IPInfo
        Public ReadOnly type As IPType

        Public ReadOnly raw As String

        Public ReadOnly port As Integer

        Public ReadOnly isLocal As Boolean
        Public ReadOnly isRemote As Boolean

        Public ReadOnly address As IPAddress
        Public ReadOnly endPoint As IPEndPoint

        Public Sub New(type As IPType, port As Integer, address As IPAddress)
            Me.type = type
            Me.port = port
            Me.address = address

            isLocal = TypeOf type Is IPType.Local
            isRemote = TypeOf type Is IPType.Remote

            raw = address.ToString()
            endPoint = New IPEndPoint(address, port)
        End Sub

        Public Overrides Function ToString() As String
            Return endPoint.ToString()
        End Function

        Public Overrides Function GetHashCode() As Integer
            Return endPoint.GetHashCode()
        End Function
    End Structure
End Namespace

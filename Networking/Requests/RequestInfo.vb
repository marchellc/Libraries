Imports Networking.Data

Namespace Networking.Requests
    Public Class RequestInfo
        Implements IMessage
        Public Sub New()
        End Sub

        Public id As String

        Public sentAt As Date
        Public receivedAt As Date

        Public value As Object

        Public response As ResponseInfo
        Public manager As RequestManager

        Public isResponded As Boolean
        Public isTimedOut As Boolean

        Public Sub Deserialize(reader As Reader) Implements IDeserialize.Deserialize
            id = reader.ReadCleanString()

            sentAt = reader.ReadDate()
            receivedAt = Date.Now

            value = reader.ReadAnonymous()
        End Sub

        Public Sub Serialize(writer As Writer) Implements ISerialize.Serialize
            writer.WriteString(id)
            writer.WriteDate(Date.Now)
            writer.WriteAnonymous(value)
        End Sub
    End Class
End Namespace

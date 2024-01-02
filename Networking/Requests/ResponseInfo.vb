Imports Networking.Data

Namespace Networking.Requests
    Public Class ResponseInfo
        Implements IMessage
        Public Sub New()
        End Sub

        Public request As RequestInfo
        Public manager As RequestManager

        Public sentAt As Date
        Public receivedAt As Date

        Public response As Object
        Public isSuccess As Boolean
        Public id As String

        Public Sub Deserialize(reader As Reader) Implements IDeserialize.Deserialize
            sentAt = reader.ReadDate()
            receivedAt = Date.Now

            id = reader.ReadCleanString()

            isSuccess = reader.ReadBool()

            response = reader.ReadAnonymous()
        End Sub

        Public Sub Serialize(writer As Writer) Implements ISerialize.Serialize
            writer.WriteDate(Date.Now)
            writer.WriteString(id)
            writer.WriteBool(isSuccess)
            writer.WriteAnonymous(response)
        End Sub
    End Class
End Namespace

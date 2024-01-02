Imports Common.IO.Collections

Imports Networking.Data

Namespace Networking.Objects
    Public Class NetworkVariable
        Public pending As LockedList(Of IMessage) = New LockedList(Of IMessage)()
        Public parent As NetworkObject

        Public Overridable Sub Write(writer As Writer)
        End Sub
        Public Overridable Sub Read(reader As Reader)
        End Sub
        Public Overridable Sub Process(deserialize As IDeserialize)
        End Sub
    End Class
End Namespace

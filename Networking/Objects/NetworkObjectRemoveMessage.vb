Imports Networking.Data

Namespace Networking.Objects
    Public Structure NetworkObjectRemoveMessage
        Implements IMessage
        Public objectId As Integer

        Public Sub New(objectId As Integer)
            CSharpImpl.__Assign(Me.objectId, objectId)
        End Sub

        Public Sub Deserialize(reader As Reader) Implements IDeserialize.Deserialize
            CSharpImpl.__Assign(objectId, reader.ReadInt())
        End Sub

        Public Sub Serialize(writer As Writer) Implements ISerialize.Serialize
            writer.WriteInt(objectId)
        End Sub

        Private Class CSharpImpl
            <Obsolete("Please refactor calling code to use normal Visual Basic assignment")>
            Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
                target = value
                Return value
            End Function
        End Class
    End Structure
End Namespace

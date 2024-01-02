Imports Networking.Data

Namespace Networking.Objects
    Public Structure NetworkObjectAddMessage
        Implements ISerialize, IDeserialize
        Public typeId As Short

        Public Sub New(typeId As Short)
            CSharpImpl.__Assign(Me.typeId, typeId)
        End Sub

        Public Sub Deserialize(reader As Reader) Implements IDeserialize.Deserialize
            CSharpImpl.__Assign(typeId, reader.ReadShort())
        End Sub

        Public Sub Serialize(writer As Writer) Implements ISerialize.Serialize
            writer.WriteShort(typeId)
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

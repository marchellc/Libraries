Imports Networking.Data

Namespace Networking.Objects
    Public Structure NetworkRpcMessage
        Implements IMessage
        Public objectId As Integer
        Public functionHash As UShort

        Public args As Object()

        Public Sub New(objectId As Integer, functionHash As UShort, args As Object())
            Me.objectId = objectId
            Me.functionHash = functionHash
            Me.args = args
        End Sub

        Public Sub Deserialize(reader As Reader) Implements IDeserialize.Deserialize
            objectId = reader.ReadInt()
            functionHash = reader.ReadUShort()
            args = reader.ReadAnonymousArray()
        End Sub

        Public Sub Serialize(writer As Writer) Implements ISerialize.Serialize
            writer.WriteInt(objectId)
            writer.WriteUShort(functionHash)
            writer.WriteAnonymousArray(args)
        End Sub
    End Structure
End Namespace

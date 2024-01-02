Imports Common.Extensions

Imports Networking.Data

Namespace Networking.Objects
    Public Structure NetworkVariableSyncMessage
        Implements IMessage
        Public objectId As Integer

        Public hash As UShort

        Public msg As IMessage

        Public Sub New(objectId As Integer, hash As UShort, msg As IMessage)
            Me.objectId = objectId
            Me.hash = hash
            Me.msg = msg
        End Sub

        Public Sub Deserialize(reader As Reader) Implements IDeserialize.Deserialize
            objectId = reader.ReadInt()
            hash = reader.ReadUShort()

            msg = TryCast(reader.ReadType().Construct(), IMessage)
            msg.Deserialize(reader)
        End Sub

        Public Sub Serialize(writer As Writer) Implements ISerialize.Serialize
            writer.WriteInt(objectId)
            writer.WriteUShort(hash)

            msg.Serialize(writer)
        End Sub
    End Structure
End Namespace

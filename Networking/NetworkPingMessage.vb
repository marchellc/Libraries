Imports Networking.Data

Namespace Networking
    Public Structure NetworkPingMessage
        Implements IMessage
        Public isServer As Boolean

        Public sent As Date
        Public recv As Date

        Public Sub New(isServer As Boolean, sent As Date, recv As Date)
            Me.isServer = isServer
            Me.sent = sent
            Me.recv = recv
        End Sub

        Public Sub Deserialize(reader As Reader) Implements IDeserialize.Deserialize
            isServer = reader.ReadBool()

            sent = reader.ReadDate()

            If isServer Then
                recv = reader.ReadDate()
            Else
                recv = Date.Now
            End If
        End Sub

        Public Sub Serialize(writer As Writer) Implements ISerialize.Serialize
            writer.WriteBool(isServer)
            writer.WriteDate(sent)

            If Not isServer Then
                writer.WriteDate(sent)
                writer.WriteDate(recv)
            Else
                writer.WriteDate(sent)
            End If
        End Sub
    End Structure
End Namespace

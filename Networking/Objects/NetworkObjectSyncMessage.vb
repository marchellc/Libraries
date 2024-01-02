Imports Networking.Data

Imports System
Imports System.Collections.Generic

Namespace Networking.Objects
    Public Structure NetworkObjectSyncMessage
        Implements ISerialize, IDeserialize
        Public syncTypes As Dictionary(Of Short, Type)

        Public Sub New(syncTypes As Dictionary(Of Short, Type))
            Me.syncTypes = syncTypes
        End Sub

        Public Sub Deserialize(reader As Reader) Implements IDeserialize.Deserialize
            syncTypes = reader.ReadDictionary(Of Short, Type)()
        End Sub

        Public Sub Serialize(writer As Writer) Implements ISerialize.Serialize
            writer.WriteDictionary(syncTypes)
        End Sub
    End Structure
End Namespace

Imports Networking.Data

Namespace Networking.Objects
    Public Class NetworkField(Of T)
        Inherits NetworkVariable
        Private valueField As T

        Public Property Value As T
            Get
                Return valueField
            End Get
            Set(value As T)
                valueField = value
                pending.Add(New NetworkFieldUpdateMessage(value))
            End Set
        End Property
    End Class

    Public Structure NetworkFieldUpdateMessage
        Implements IMessage
        Public value As Object

        Public Sub New(value As Object)
            Me.value = value
        End Sub

        Public Sub Deserialize(reader As Reader) Implements IDeserialize.Deserialize
            value = reader.ReadAnonymous()
        End Sub

        Public Sub Serialize(writer As Writer) Implements ISerialize.Serialize
            writer.WriteAnonymous(value)
        End Sub
    End Structure
End Namespace

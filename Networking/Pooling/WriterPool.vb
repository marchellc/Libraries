Imports Common.Pooling
Imports Common.Pooling.Buffers

Imports Networking.Data

Namespace Networking.Pooling
    Public Class WriterPool
        Implements IPool(Of Writer)
        Public Property Options As PoolOptions Implements IPool(Of Writer).Options
        Public Property Buffer As IPoolBuffer(Of Writer) Implements IPool(Of Writer).Buffer

        Public Sub New()
            Options = PoolOptions.NewOnMissing
            Buffer = New BasicBuffer(Of Writer)(Me, Function() New Writer())
        End Sub

        Public Function [Next]() As Writer Implements IPool(Of Writer).Next
            Dim writer = Buffer.Get()

            writer.pool = Me
            writer.FromPool()

            Return writer
        End Function

        Public Sub [Return](obj As Writer) Implements IPool(Of Writer).Return
            obj.ToPool()
            obj.pool = Me

            Buffer.Add(obj)
        End Sub

        Public Sub Clear() Implements IPool(Of Writer).Clear
            Buffer.Clear()
        End Sub

        Public Sub Initialize(initialSize As Integer) Implements IPool(Of Writer).Initialize
            For i = 0 To initialSize - 1
                Buffer.AddNew()
            Next
        End Sub
    End Class
End Namespace

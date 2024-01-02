Imports Common.Pooling
Imports Common.Pooling.Buffers

Imports Networking.Data

Imports System

Namespace Networking.Pooling
    Public Class ReaderPool
        Implements IPool(Of Reader)
        Public Property Options As PoolOptions Implements IPool(Of Reader).Options
        Public Property Buffer As IPoolBuffer(Of Reader) Implements IPool(Of Reader).Buffer

        Public Sub New()
            Options = PoolOptions.NewOnMissing
            Buffer = New BasicBuffer(Of Reader)(Me, Function() New Reader())
        End Sub

        Public Function [Next](data As Byte()) As Reader
            Dim reader = Buffer.Get()

            reader.pool = Me
            reader.FromPool(data)

            Return reader
        End Function

        Public Sub [Return](obj As Reader) Implements IPool(Of Reader).Return
            obj.ToPool()
            obj.pool = Me

            Buffer.Add(obj)
        End Sub

        Public Sub Initialize(initialSize As Integer) Implements IPool(Of Reader).Initialize
            For i = 0 To initialSize - 1
                Buffer.AddNew()
            Next
        End Sub

        Public Sub Clear() Implements IPool(Of Reader).Clear
            Buffer.Clear()
        End Sub

        Public Function [Next]() As Reader Implements IPool(Of Reader).Next
            Return CSharpImpl.__Throw(Of Object)(New InvalidOperationException($"You must use the method with the byte[] overload to get a Reader"))
        End Function

        Private Class CSharpImpl
            <Obsolete("Please refactor calling code to use normal throw statements")>
            Shared Function __Throw(Of T)(ByVal e As Exception) As T
                Throw e
            End Function
        End Class
    End Class
End Namespace

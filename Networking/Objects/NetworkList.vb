Imports Common.IO.Collections
Imports Common.Utilities

Imports Networking.Data
Imports Networking.Utilities

Imports System.Collections
Imports System.Collections.Generic

Namespace Networking.Objects
    Public Class NetworkList(Of T)
        Inherits NetworkVariable
        Implements IList(Of T)
        Private ReadOnly list As LockedList(Of T)

        Public Sub New()
            list = New LockedList(Of T)()

            ' make sure that we have a writer & reader
            GetType(T).GetWriter()
            GetType(T).GetReader()
        End Sub

        Default Public Property Item(index As Integer) As T Implements IList(Of T).Item
            Get
                Return list(index)
            End Get
            Set(value As T)
                Insert(index, value)
            End Set
        End Property

        Public ReadOnly Property Count As Integer Implements ICollection(Of T).Count
            Get
                Return list.Count
            End Get
        End Property

        Public ReadOnly Property IsReadOnly As Boolean Implements ICollection(Of T).IsReadOnly
            Get
                Return list.IsReadOnly
            End Get
        End Property

        Public Sub Add(item As T) Implements ICollection(Of T).Add
            list.Add(item)

            Dim writer = parent.net.GetWriter()

            writer.Write(item)

            pending.Add(New NetworkListUpdateMessage(NetworkListUpdateMessage.ListOp.Add, writer))
        End Sub

        Public Sub Clear() Implements ICollection(Of T).Clear
            list.Clear()
            pending.Add(New NetworkListUpdateMessage(NetworkListUpdateMessage.ListOp.Clear, Nothing))
        End Sub

        Public Sub Insert(index As Integer, item As T) Implements IList(Of T).Insert
            If index.IsValidIndex(list.Count) Then
                list(index) = item

                Dim writer = parent.net.GetWriter()

                writer.WriteInt(index)
                writer.Write(item)

                pending.Add(New NetworkListUpdateMessage(NetworkListUpdateMessage.ListOp.Set, writer))
            End If
        End Sub

        Public Function Remove(item As T) As Boolean Implements ICollection(Of T).Remove
            Dim index = IndexOf(item)

            If index = -1 Then Return False

            If list.Remove(item) Then
                Dim writer = parent.net.GetWriter()

                writer.WriteInt(index)

                pending.Add(New NetworkListUpdateMessage(NetworkListUpdateMessage.ListOp.Remove, writer))

                Return True
            End If

            Return False
        End Function

        Public Sub RemoveAt(index As Integer) Implements IList(Of T).RemoveAt
            If index.IsValidIndex(list.Count) Then
                list.RemoveAt(index)

                Dim writer = parent.net.GetWriter()

                writer.WriteInt(index)

                pending.Add(New NetworkListUpdateMessage(NetworkListUpdateMessage.ListOp.Remove, writer))
            End If
        End Sub

        Public Function Contains(item As T) As Boolean Implements ICollection(Of T).Contains
            Return list.Contains(item)
        End Function

        Public Function IndexOf(item As T) As Integer Implements IList(Of T).IndexOf
            Return list.IndexOf(item)
        End Function

        Public Sub CopyTo(array As T(), arrayIndex As Integer) Implements ICollection(Of T).CopyTo
            list.CopyTo(array, arrayIndex)
        End Sub

        Public Function GetEnumerator() As IEnumerator(Of T) Implements IEnumerable(Of T).GetEnumerator
            Return list.GetEnumerator()
        End Function

        Private Function GetEnumerator1() As IEnumerator Implements IEnumerable.GetEnumerator
            Return list.GetEnumerator()
        End Function

                ''' Cannot convert MethodDeclarationSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: node
''' Actual value was not Networking.Objects.NetworkListUpdateMessage updateMsg.
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertToVariableDeclaratorOrNull(IsPatternExpressionSyntax node)
'''    at System.Linq.Enumerable.WhereSelectListIterator`2.MoveNext()
'''    at System.Linq.Enumerable.WhereEnumerableIterator`1.MoveNext()
'''    at System.Linq.Enumerable.<ConcatIterator>d__59`1.MoveNext()
'''    at System.Linq.Buffer`1..ctor(IEnumerable`1 source)
'''    at System.Linq.Enumerable.ToArray[TSource](IEnumerable`1 source)
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertToDeclarationStatement(List`1 des, List`1 isPatternExpressions)
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.InsertRequiredDeclarations(SyntaxList`1 convertedStatements, CSharpSyntaxNode originaNode)
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertStatement(StatementSyntax statement, CSharpSyntaxVisitor`1 methodBodyVisitor)
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.<>c__DisplayClass10_0.<ConvertStatements>b__0(StatementSyntax s)
'''    at System.Linq.Enumerable.<SelectManyIterator>d__17`2.MoveNext()
'''    at Microsoft.CodeAnalysis.SyntaxList`1.CreateNode(IEnumerable`1 nodes)
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertStatements(SyntaxList`1 statements, MethodBodyExecutableStatementVisitor iteratorState)
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertBody(BlockSyntax body, ArrowExpressionClauseSyntax expressionBody, Boolean hasReturnType, MethodBodyExecutableStatementVisitor iteratorState)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitMethodDeclaration(MethodDeclarationSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingVisitorWrapper`1.Accept(SyntaxNode csNode, Boolean addSourceMapping)
''' 
''' Input:
''' 
'''         public override void Process(Networking.Data.IDeserialize deserialize)
'''         {
'''             if (deserialize is not Networking.Objects.NetworkListUpdateMessage updateMsg)
'''                 return;
''' 
'''             switch (updateMsg.op)
'''             {
'''                 case Networking.Objects.NetworkListUpdateMessage.ListOp.Set:
'''                     {
'''                         var index = updateMsg.reader.ReadInt();
'''                         var value = updateMsg.reader.Read<T>();
''' 
'''                         this.list[index] = value;
''' 
'''                         break;
'''                     }
''' 
'''                 case Networking.Objects.NetworkListUpdateMessage.ListOp.Clear:
'''                     {
'''                         this.list.Clear();
'''                         break;
'''                     }
''' 
'''                 case Networking.Objects.NetworkListUpdateMessage.ListOp.Remove:
'''                     {
'''                         var index = updateMsg.reader.ReadInt();
''' 
'''                         this.list.RemoveAt(index);
''' 
'''                         break;
'''                     }
''' 
'''                 case Networking.Objects.NetworkListUpdateMessage.ListOp.Add:
'''                     {
'''                         var value = updateMsg.reader.Read<T>();
''' 
'''                         this.list.Add(value);
''' 
'''                         break;
'''                     }
'''             }
''' 
'''             updateMsg.FinishReader();
'''         }
''' 
''' 
    End Class

    Public Structure NetworkListUpdateMessage
        Implements IMessage
        Public Enum ListOp
            Remove
            Clear
            Add
            [Set]
        End Enum

        Public op As ListOp

        Public reader As Reader
        Public writer As Writer

        Public Sub New(op As ListOp, writer As Writer)
            Me.op = op
            Me.writer = writer
        End Sub

        Public Sub Deserialize(reader As Reader) Implements IDeserialize.Deserialize
            op = CType(reader.ReadByte(), ListOp)
            Me.reader = reader.ReadReader()
        End Sub

        Public Sub Serialize(writer As Writer) Implements ISerialize.Serialize
            writer.WriteByte(op)

            If Me.writer IsNot Nothing Then
                writer.WriteWriter(Me.writer)

                Me.writer.Clear()

                If Me.writer.pool IsNot Nothing Then Me.writer.Return()

                Me.writer = Nothing
            End If
        End Sub

        Public Sub FinishReader()
            If reader IsNot Nothing Then
                reader.Clear()

                If reader.pool IsNot Nothing Then reader.Return()

                reader = Nothing
            End If
        End Sub
    End Structure
End Namespace

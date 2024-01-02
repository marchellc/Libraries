Imports Common.Pooling
Imports Common.Extensions
Imports Common.Pooling.Pools

Imports System.Collections.Generic
Imports System.Text
Imports System.Linq
Imports System

Imports Networking.Utilities
Imports Networking.Pooling

Namespace Networking.Data
    Public Class Writer
        Friend pool As WriterPool

        Private bufferField As List(Of Byte)
        Private encodingField As Encoding
        Private charBufferField As Char()

        Public ReadOnly Property IsEmpty As Boolean
            Get
                Return bufferField Is Nothing OrElse bufferField.Count <= 0
            End Get
        End Property
        Public ReadOnly Property IsFull As Boolean
            Get
                Return bufferField IsNot Nothing AndAlso bufferField.Count >= bufferField.Capacity
            End Get
        End Property

        Public ReadOnly Property Size As Integer
            Get
                Return bufferField.Count
            End Get
        End Property
        Public ReadOnly Property CharSize As Integer
            Get
                Return charBufferField.Length
            End Get
        End Property

        Public ReadOnly Property Buffer As Byte()
            Get
                Return bufferField.ToArray()
            End Get
        End Property
        Public ReadOnly Property CharBuffer As Char()
            Get
                Return charBufferField
            End Get
        End Property

        Public Property Encoding As Encoding
            Get
                Return encodingField
            End Get
            Set(value As Encoding)
                encodingField = value
            End Set
        End Property

        Public Event OnPooled As Action
        Public Event OnUnpooled As Action

        Public Sub New()
            Me.New(Encoding.Default)
        End Sub

        Public Sub New(encoding As Encoding)
            encodingField = encoding
            charBufferField = New Char(0) {}
            bufferField = ListPool(Of Byte).Shared.Next()
        End Sub

        Friend Sub ToPool()
            bufferField?.[Return]()
            bufferField = Nothing

            OnPooledEvent.Call()
        End Sub

        Friend Sub FromPool()
            bufferField = ListPool(Of Byte).Shared.Next()
                        ''' Cannot convert AssignmentExpressionSyntax, System.NotSupportedException: CoalesceAssignmentExpression is not supported!
'''    at ICSharpCode.CodeConverter.VB.SyntaxKindExtensions.ConvertToken(SyntaxKind t, TokenContext context)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.MakeAssignmentStatement(AssignmentExpressionSyntax node, ExpressionSyntax left, ExpressionSyntax right)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingVisitorWrapper`1.Accept(SyntaxNode csNode, Boolean addSourceMapping)
''' 
''' Input:
'''             this.encodingField ??= System.Text.Encoding.Default
''' 

            OnUnpooledEvent.Call()
        End Sub

        Public Sub WriteBool(value As Boolean)
            bufferField.Add(If(value, CByte(1), CByte(0)))
        End Sub

        Public Sub WriteByte(value As Byte)
            bufferField.Add(value)
        End Sub

        Public Sub WriteSByte(value As SByte)
            bufferField.Add(value)
        End Sub

        Public Sub WriteBytes(bytes As IEnumerable(Of Byte))
            If bytes Is Nothing Then Throw New ArgumentNullException(NameOf(bytes))

            Dim size = bytes.Count()

            If size <= 0 Then
                WriteInt(0)
                Return
            End If

            WriteInt(size)

            For Each b In bytes
                WriteByte(b)
            Next
        End Sub

        Public Sub WriteShort(value As Short)
            bufferField.Add(value)
            bufferField.Add(value >> 8)
        End Sub

        Public Sub WriteUShort(value As UShort)
            bufferField.Add(value)
            bufferField.Add(value >> 8)
        End Sub

        Public Sub WriteInt(value As Integer)
            bufferField.Add(value)
            bufferField.Add(value >> 8)
            bufferField.Add(value >> 16)
            bufferField.Add(value >> 24)
        End Sub

        Public Sub WriteUInt(value As UInteger)
            bufferField.Add(value)
            bufferField.Add(value >> 8)
            bufferField.Add(value >> 16)
            bufferField.Add(value >> 24)
        End Sub

        Public Sub WriteLong(value As Long)
            bufferField.Add(value)
            bufferField.Add(value >> 8)
            bufferField.Add(value >> 16)
            bufferField.Add(value >> 24)
            bufferField.Add(value >> 32)
            bufferField.Add(value >> 40)
            bufferField.Add(value >> 48)
            bufferField.Add(value >> 56)
        End Sub

        Public Sub WriteULong(value As ULong)
            bufferField.Add(value)
            bufferField.Add(value >> 8)
            bufferField.Add(value >> 16)
            bufferField.Add(value >> 24)
            bufferField.Add(value >> 32)
            bufferField.Add(value >> 40)
            bufferField.Add(value >> 48)
            bufferField.Add(value >> 56)
        End Sub

                ''' Cannot convert MethodDeclarationSyntax, System.NotSupportedException: UnsafeKeyword is not supported!
'''    at ICSharpCode.CodeConverter.VB.SyntaxKindExtensions.ConvertToken(SyntaxKind t, TokenContext context)
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertModifier(SyntaxToken m, TokenContext context)
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.<>c__DisplayClass33_0.<ConvertModifiersCore>b__3(SyntaxToken x)
'''    at System.Linq.Enumerable.WhereSelectEnumerableIterator`2.MoveNext()
'''    at System.Linq.Enumerable.WhereSelectEnumerableIterator`2.MoveNext()
'''    at System.Collections.Generic.List`1..ctor(IEnumerable`1 collection)
'''    at System.Linq.Enumerable.ToList[TSource](IEnumerable`1 source)
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertModifiersCore(IReadOnlyCollection`1 modifiers, TokenContext context, Boolean isConstructor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitMethodDeclaration(MethodDeclarationSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingVisitorWrapper`1.Accept(SyntaxNode csNode, Boolean addSourceMapping)
''' 
''' Input:
''' 
'''         public unsafe void WriteFloat(float value)
'''         {
'''             var tmpValue = *(uint*)&value;
''' 
'''             this.bufferField.Add((byte)tmpValue);
'''             this.bufferField.Add((byte)(tmpValue >> 8));
'''             this.bufferField.Add((byte)(tmpValue >> 16));
'''             this.bufferField.Add((byte)(tmpValue >> 24));
'''         }
''' 
''' 
                ''' Cannot convert MethodDeclarationSyntax, System.NotSupportedException: UnsafeKeyword is not supported!
'''    at ICSharpCode.CodeConverter.VB.SyntaxKindExtensions.ConvertToken(SyntaxKind t, TokenContext context)
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertModifier(SyntaxToken m, TokenContext context)
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.<>c__DisplayClass33_0.<ConvertModifiersCore>b__3(SyntaxToken x)
'''    at System.Linq.Enumerable.WhereSelectEnumerableIterator`2.MoveNext()
'''    at System.Linq.Enumerable.WhereSelectEnumerableIterator`2.MoveNext()
'''    at System.Collections.Generic.List`1..ctor(IEnumerable`1 collection)
'''    at System.Linq.Enumerable.ToList[TSource](IEnumerable`1 source)
'''    at ICSharpCode.CodeConverter.VB.CommonConversions.ConvertModifiersCore(IReadOnlyCollection`1 modifiers, TokenContext context, Boolean isConstructor)
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitMethodDeclaration(MethodDeclarationSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingVisitorWrapper`1.Accept(SyntaxNode csNode, Boolean addSourceMapping)
''' 
''' Input:
''' 
'''         public unsafe void WriteDouble(double value)
'''         {
'''             var tmpValue = *(ulong*)&value;
''' 
'''             this.bufferField.Add((byte)tmpValue);
'''             this.bufferField.Add((byte)(tmpValue >> 8));
'''             this.bufferField.Add((byte)(tmpValue >> 16));
'''             this.bufferField.Add((byte)(tmpValue >> 24));
'''             this.bufferField.Add((byte)(tmpValue >> 32));
'''             this.bufferField.Add((byte)(tmpValue >> 40));
'''             this.bufferField.Add((byte)(tmpValue >> 48));
'''             this.bufferField.Add((byte)(tmpValue >> 56));
'''         }
''' 
''' 

        Public Sub WriteChar(value As Char)
            charBufferField(0) = value
            WriteBytes(encodingField.GetBytes(charBufferField, 0, charBufferField.Length))
        End Sub

        Public Sub WriteString(value As String)
            If value Is Nothing Then
                WriteByte(0)
                Return
            ElseIf String.IsNullOrWhiteSpace(value) Then
                WriteByte(1)
                Return
            Else
                WriteByte(2)
                WriteBytes(encodingField.GetBytes(value))
            End If
        End Sub

        Public Sub Write7BitEncodedInt(value As Integer)
            Dim tempValue = CUInt(value)

            While tempValue > &H80
                bufferField.Add(tempValue Or &H80)
                tempValue >>= 7
            End While

            bufferField.Add(tempValue)
        End Sub

        Public Sub WriteType(type As Type)
            WriteString(type.AssemblyQualifiedName)
        End Sub

        Public Sub WriteTime(span As TimeSpan)
            WriteLong(span.Ticks)
        End Sub

        Public Sub WriteDate([date] As Date)
            WriteShort([date].Year)

            WriteByte([date].Month)
            WriteByte([date].Day)
            WriteByte([date].Hour)
            WriteByte([date].Second)
            WriteByte([date].Millisecond)
        End Sub

        Public Sub WriteVersion(version As Version)
            WriteInt(version.Major)
            WriteInt(version.Minor)
            WriteInt(version.Build)
            WriteInt(version.Revision)
        End Sub

        Public Sub WriteWriter(writer As Writer)
            WriteBytes(writer.Buffer)
        End Sub

        Public Sub WriteAnonymous(obj As Object)

            Dim msg As IMessage = Nothing
            If obj Is Nothing Then
                WriteBool(True)
                Return
            Else


                WriteBool(False)
                WriteType(obj.GetType())

                If CSharpImpl.__Assign(msg, TryCast(obj, IMessage)) IsNot Nothing Then
                    WriteBool(True)
                    msg.Serialize(Me)
                Else
                    WriteBool(False)
                    Dim writer = GetWriter(obj.GetType())
                    writer(Me, obj)
                End If
            End If
        End Sub

        Public Sub WriteAnonymousArray(ParamArray objects As Object())
            WriteInt(objects.Length)

            For i = 0 To objects.Length - 1
                WriteAnonymous(objects(i))
            Next
        End Sub

        Public Sub Write(Of T)(value As T)
            Dim serialize As ISerialize = Nothing

            If value IsNot Nothing AndAlso CSharpImpl.__Assign(serialize, TryCast(value, ISerialize)) IsNot Nothing Then
                WriteByte(0)
                serialize.Serialize(Me)
                Return
            End If

            Dim writer = GetType(T).GetWriter()

            If value Is Nothing Then
                WriteByte(1)
                Return
            Else
                WriteByte(2)
                writer(Me, value)
            End If
        End Sub

        Public Sub WriteList(Of T)(items As IEnumerable(Of T))
            Dim writer = GetType(T).GetWriter()

            If items IsNot Nothing Then
                WriteByte(1)

                Dim size = items.Count()

                WriteInt(size)

                If size <= 0 Then Return

                For Each item In items
                    writer(Me, item)
                Next
            Else
                WriteByte(0)
                Return
            End If
        End Sub

        Public Sub WriteDictionary(Of TKey, TValue)(dict As IDictionary(Of TKey, TValue))
            Dim keyWriter = GetType(TKey).GetWriter
            Dim valueWriter = GetType(TValue).GetWriter

            If dict IsNot Nothing Then
                WriteByte(1)
                WriteInt(dict.Count)

                If dict.Count <= 0 Then Return

                For Each pair In dict
                    keyWriter(Me, pair.Key)
                    valueWriter(Me, pair.Value)
                Next
            Else
                WriteByte(0)
                Return
            End If
        End Sub

        Public Sub Clear()
            bufferField.Clear()
        End Sub

        Public Sub Take(target As Byte(), start As Integer, size As Integer)
            If size > Me.Size Then Throw New ArgumentOutOfRangeException("size")

            If size > target.Length Then Throw New ArgumentOutOfRangeException("size")

            For i = start To size - 1
                target(i) = bufferField.First()
                bufferField.RemoveAt(0)
            Next
        End Sub

        Public Function Take(size As Integer) As Byte()
            If size > Me.Size Then Throw New ArgumentOutOfRangeException("size")

            Dim array = New Byte(size - 1) {}

            Take(array, 0, size)

            Return array
        End Function

        Public Sub [Return]()
            If pool Is Nothing Then Throw New InvalidOperationException($"Cannot return to pool")

            pool.Return(Me)
        End Sub

        Private Class CSharpImpl
            <Obsolete("Please refactor calling code to use normal Visual Basic assignment")>
            Shared Function __Assign(Of T)(ByRef target As T, value As T) As T
                target = value
                Return value
            End Function
        End Class
    End Class
End Namespace

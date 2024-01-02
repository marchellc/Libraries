Imports Common.Extensions
Imports Common.Pooling.Pools

Imports Networking.Utilities
Imports Networking.Pooling

Imports System
Imports System.Text
Imports System.Collections.Generic

Namespace Networking.Data
    Public Class Reader
        Public Const BYTE_SIZE As Byte = 1
        Public Const SBYTE_SIZE As Byte = 1

        Public Const INT_16_SIZE As Byte = 2
        Public Const INT_32_SIZE As Byte = 4
        Public Const INT_64_SIZE As Byte = 8

        Public Const SINGLE_SIZE As Byte = 4
        Public Const DOUBLE_SIZE As Byte = 8

        Friend pool As ReaderPool

        Private encodingField As Encoding
        Private bufferField As List(Of Byte)
        Private dataField As Byte()
        Private offsetField As Integer

        Public ReadOnly Property Data As Byte()
            Get
                Return dataField
            End Get
        End Property
        Public ReadOnly Property Buffer As Byte()
            Get
                Return bufferField.ToArray()
            End Get
        End Property

        Public Property Offset As Integer
            Get
                Return offsetField
            End Get
            Set(value As Integer)
                offsetField = value
            End Set
        End Property

        Public ReadOnly Property Size As Integer
            Get
                Return dataField.Length
            End Get
        End Property
        Public ReadOnly Property BufferSize As Integer
            Get
                Return bufferField.Count
            End Get
        End Property

        Public ReadOnly Property IsEmpty As Boolean
            Get
                Return dataField Is Nothing OrElse dataField.Length <= 0
            End Get
        End Property
        Public ReadOnly Property IsEnd As Boolean
            Get
                Return offsetField >= dataField.Length
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

        Public Event OnMoved As Action(Of Integer, Integer)

        Public Event OnPooled As Action
        Public Event OnUnpooled As Action

        Public Sub New()
            Me.New(Encoding.Default)
        End Sub

        Public Sub New(encoding As Encoding)
            encodingField = encoding
        End Sub

        Friend Sub ToPool()
            ListPool(Of Byte).Shared.Return(bufferField)

            dataField = Nothing
            bufferField = Nothing

            offsetField = 0

            OnPooledEvent.Call()
        End Sub

        Friend Sub FromPool(data As Byte())
            bufferField = ListPool(Of Byte).Shared.Next()
            dataField = data
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

        Public Function ReadByte() As Byte
            Move(BYTE_SIZE)
            Return bufferField(0)
        End Function

        Public Function ReadBytes() As Byte()
            Dim size = ReadInt()

            If size <= 0 Then Return Array.Empty(Of Byte)()

            Return ReadBytes(size)
        End Function

        Public Function ReadBytes(size As Integer) As Byte()
            If size < 0 Then Throw New ArgumentOutOfRangeException("size")

            Move(size)
            Return bufferField.ToArray()
        End Function

        Public Sub ReadBytes(buffer As Byte(), offset As Integer, count As Integer)
            Move(count)

            If buffer Is Nothing Then
                Throw New ArgumentNullException("buffer")
            ElseIf offset < 0 Then
                Throw New ArgumentOutOfRangeException("offset")
            ElseIf offset > count Then
                Throw New ArgumentOutOfRangeException("offset")
            ElseIf count < 0 Then
                Throw New ArgumentOutOfRangeException("count")
            End If

            For i = offset To count - 1
                buffer(i) = bufferField(i - offset)
            Next
        End Sub

        Public Sub ReadBytes(buffer As IList(Of Byte), offset As Integer, count As Integer)
            Move(count)

            If buffer Is Nothing Then
                Throw New ArgumentNullException("buffer")
            ElseIf offset < 0 Then
                Throw New ArgumentOutOfRangeException("offset")
            ElseIf offset > count Then
                Throw New ArgumentOutOfRangeException("offset")
            ElseIf count < 0 Then
                Throw New ArgumentOutOfRangeException("count")
            End If

            For i = offset To count - 1
                buffer(i) = bufferField(i - offset)
            Next
        End Sub

        Public Function ReadSByte() As SByte
            Move(SBYTE_SIZE)
            Return bufferField(0)
        End Function

        Public Function ReadShort() As Short
            Move(INT_16_SIZE)
            Return bufferField(0) Or bufferField(1) << 8
        End Function

        Public Function ReadUShort() As UShort
            Move(INT_16_SIZE)
            Return bufferField(0) Or bufferField(1) << 8
        End Function

        Public Function ReadInt() As Integer
            Move(INT_32_SIZE)
            Return bufferField(0) Or bufferField(1) << 8 Or bufferField(2) << 16 Or bufferField(3) << 24
        End Function

        Public Function ReadUInt() As UInteger
            Move(INT_32_SIZE)
            Return bufferField(0) Or bufferField(1) << 8 Or bufferField(2) << 16 Or bufferField(3) << 24
        End Function

        Public Function ReadLong() As Long
            Move(INT_64_SIZE)

            Dim lo = CUInt(bufferField(0) Or bufferField(1) << 8 Or bufferField(2) << 16 Or bufferField(3) << 24)
            Dim hi = CUInt(bufferField(4) Or bufferField(5) << 8 Or bufferField(6) << 16 Or bufferField(7) << 24)

            Return CULng(hi) << 32 Or lo
        End Function

        Public Function ReadULong() As ULong
            Move(INT_64_SIZE)

            Dim lo = CUInt(bufferField(0) Or bufferField(1) << 8 Or bufferField(2) << 16 Or bufferField(3) << 24)
            Dim hi = CUInt(bufferField(4) Or bufferField(5) << 8 Or bufferField(6) << 16 Or bufferField(7) << 24)

            Return CULng(hi) << 32 Or lo
        End Function

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
'''         public unsafe float ReadFloat()
'''         {
'''             this.Move(Networking.Data.Reader.SINGLE_SIZE);
''' 
'''             var buff = (uint)(this.bufferField[0] | this.bufferField[1] << 8 | this.bufferField[2] << 16 | this.bufferField[3] << 24);
''' 
'''             return *(float*)&buff;
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
'''         public unsafe double ReadDouble()
'''         {
'''             this.Move(Networking.Data.Reader.DOUBLE_SIZE);
''' 
'''             var lo = (uint)(this.bufferField[0] | this.bufferField[1] << 8 | this.bufferField[2] << 16 | this.bufferField[3] << 24);
'''             var hi = (uint)(this.bufferField[4] | this.bufferField[5] << 8 | this.bufferField[6] << 16 | this.bufferField[7] << 24);
'''             var buff = ((ulong)hi) << 32 | lo;
''' 
'''             return *(double*)&buff;
'''         }
''' 
''' 

        Public Function ReadBool() As Boolean
            Move(BYTE_SIZE)
            Return bufferField(0) <> 0
        End Function

        Public Function ReadChar() As Char
            Dim charBytes = ReadBytes()
            Dim charValue = encodingField.GetChars(charBytes, 0, charBytes.Length)

            Return charValue(0)
        End Function

        Public Function ReadCleanString() As String
            Dim stringId = ReadByte()

            If stringId = 0 Then
                Return Nothing
            ElseIf stringId = 1 Then
                Return String.Empty
            Else
                Dim stringSize = ReadInt()
                Dim stringBytes = ReadBytes(stringSize)
                Dim stringValue = encodingField.GetString(stringBytes)

                Return stringValue
            End If
        End Function

        Public Function ReadTime() As TimeSpan
            Return New TimeSpan(ReadLong())
        End Function

        Public Function ReadDate() As Date
            Return New DateTime(ReadShort(), ReadByte(), ReadByte(), ReadByte(), ReadByte(), ReadByte())
        End Function

        Public Function ReadString() As NetworkString
            Dim stringId = ReadByte()

            If stringId = 0 Then
                Return New NetworkString(True, True, Nothing)
            ElseIf stringId = 1 Then
                Return New NetworkString(True, False, String.Empty)
            Else
                Dim stringSize = ReadInt()
                Dim stringBytes = ReadBytes(stringSize)
                Dim stringValue = encodingField.GetString(stringBytes)

                Return New NetworkString(False, False, stringValue)
            End If
        End Function

        Public Function ReadVersion() As Version
            Return New Version(ReadInt(), ReadInt(), ReadInt(), ReadInt())
        End Function

        Public Function ReadType() As Type
            Dim typeName = ReadCleanString()
            Dim typeValue = Type.GetType(typeName)

            Return typeValue
        End Function

        Public Function ReadAnonymous() As Object
            Dim isNull = ReadBool()

            If isNull Then Return Nothing

            Dim type = ReadType()
            Dim isMessage = ReadBool()

            If isMessage Then
                Dim message = TryCast(type.Construct(), IMessage)

                message.Deserialize(Me)

                Return message
            End If

            Dim reader = type.GetReader()

            Return reader(Me)
        End Function

        Public Function ReadAnonymousArray() As Object()
            Dim size = ReadInt()
            Dim array = New Object(size - 1) {}

            For i = 0 To size - 1
                array(i) = ReadAnonymous()
            Next

            Return array
        End Function

                ''' Cannot convert MethodDeclarationSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: node
''' Actual value was not Networking.Data.IDeserialize deserialize.
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
'''         public T Read<T>()
'''         {
'''             var reader = Networking.Utilities.TypeLoader.GetReader(typeof(T));
'''             var isNull = this.ReadByte();
''' 
'''             switch (isNull)
'''             {
'''                 case 0:
'''                     {
'''                         var item = typeof(T).Construct();
''' 
'''                         if (item is null || item is not Networking.Data.IDeserialize deserialize)
'''                             throw new System.Exception($"Invalid data signature");
''' 
'''                         deserialize.Deserialize(this);
''' 
'''                         if (deserialize is not T tDeserialize)
'''                             throw new System.InvalidCastException($"Cannot cast {deserialize.GetType().FullName} to {typeof(T).FullName}");
''' 
'''                         return tDeserialize;
'''                     }
''' 
'''                 case 1:
'''                     return default;
''' 
'''                 case 2:
'''                     {
'''                         var item = reader(this);
''' 
'''                         if (item is null || item is not T tItem)
'''                             throw new System.Exception($"Invalid data");
''' 
'''                         return tItem;
'''                     }
''' 
'''                 default:
'''                     throw new System.Exception($"Unknown data");
'''             }
'''         }
''' 
''' 
                ''' Cannot convert MethodDeclarationSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: node
''' Actual value was not T tItem.
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
'''         public System.Collections.Generic.List<T> ReadList<T>()
'''         {
'''             var reader = Networking.Utilities.TypeLoader.GetReader(typeof(T));
'''             var isNull = this.ReadByte();
''' 
'''             switch (isNull)
'''             {
'''                 case 0:
'''                     return null;
''' 
'''                 case 1:
'''                     {
'''                         var size = this.ReadInt();
''' 
'''                         if (size <= 0)
'''                             return new System.Collections.Generic.List<T>();
''' 
'''                         var list = new System.Collections.Generic.List<T>();
''' 
'''                         for (int i = 0; i < size; i++)
'''                         {
'''                             var item = reader(this);
''' 
'''                             if (item is null || item is not T tItem)
'''                                 continue;
''' 
'''                             list.Add(tItem);
'''                         }
''' 
'''                         return list;
'''                     }
''' 
'''                 default:
'''                     throw new System.Exception($"Invalid data");
'''             }
'''         }
''' 
''' 
                ''' Cannot convert MethodDeclarationSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: node
''' Actual value was not TKey tKeyItem.
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
'''         public System.Collections.Generic.Dictionary<TKey, TValue> ReadDictionary<TKey, TValue>()
'''         {
'''             var keyReader = Networking.Utilities.TypeLoader.GetReader(typeof(TKey));
'''             var valueReader = Networking.Utilities.TypeLoader.GetReader(typeof(TValue));
'''             var isNull = this.ReadByte();
''' 
'''             switch (isNull)
'''             {
'''                 case 0:
'''                     return null;
''' 
'''                 case 1:
'''                     {
'''                         var size = this.ReadInt();
''' 
'''                         if (size <= 0)
'''                             return new System.Collections.Generic.Dictionary<TKey, TValue>();
''' 
'''                         var dict = new System.Collections.Generic.Dictionary<TKey, TValue>(size);
''' 
'''                         for (int i = 0; i < size; i++)
'''                         {
'''                             var keyItem = keyReader(this);
'''                             var valueItem = valueReader(this);
''' 
'''                             if (keyItem is null || keyItem is not TKey tKeyItem)
'''                                 continue;
''' 
'''                             if (valueItem is null || valueItem is not TValue tValueItem)
'''                                 continue;
''' 
'''                             dict[tKeyItem] = tValueItem;
'''                         }
''' 
'''                         return dict;
'''                     }
''' 
'''                 default:
'''                     throw new System.Exception($"Invalid data");
'''             }
'''         }
''' 
''' 

        Public Function ReadReader() As Reader
            Dim bytes = ReadBytes()

            If pool IsNot Nothing Then
                Return pool.Next(bytes)
            Else
                Dim reader = New Reader()
                reader.FromPool(bytes)
                Return reader
            End If
        End Function

                ''' Cannot convert MethodDeclarationSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: node
''' Actual value was not Networking.Data.IDeserialize deserialize.
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
'''         public System.Collections.Generic.List<Networking.Data.IDeserialize> ReadToEnd()
'''         {
'''             var size = this.ReadInt();
'''             var list = new System.Collections.Generic.List<Networking.Data.IDeserialize>();
''' 
'''             if (size <= 0)
'''                 return list;
''' 
'''             list.Capacity = size;
''' 
'''             for (int i = 0; i < size; i++)
'''             {
'''                 var type = this.ReadType();
'''                 var message = @type.Construct();
''' 
'''                 if (message is null || message is not Networking.Data.IDeserialize deserialize)
'''                     continue;
''' 
'''                 deserialize.Deserialize(this);
''' 
'''                 list.Add(deserialize);
'''             }
''' 
'''             return list;
'''         }
''' 
''' 

        Public Function Read7BitEncodedInt() As Integer
            Dim count = 0
            Dim shift = 0

            Dim val As Byte

            Do
                If shift = 5 * 7 Then Throw New FormatException("Incorrect 7-bit int32 format")

                val = ReadByte()
                count = count Or (val And &H7F) << shift
                shift += 7
            Loop While (val And &H80) <> 0

            Return count
        End Function

        Public Sub ClearBuffer()
            bufferField.Clear()
        End Sub

        Public Sub Clear()
            bufferField.Clear()
            Array.Clear(dataField, 0, dataField.Length)
        End Sub

        Public Sub Reset()
            offsetField = 0
            bufferField.Clear()
        End Sub

        Public Sub Reset(newOffset As Integer)
            offsetField = newOffset
            bufferField.Clear()
        End Sub

        Public Sub [Return]()
            If pool Is Nothing Then Throw New InvalidOperationException($"Cannot return to an empty pool")

            pool.Return(Me)
        End Sub

        Private Sub Move(count As Integer)
            If offsetField >= dataField.Length OrElse offsetField + count > dataField.Length Then Throw New InvalidOperationException($"Cannot move offset by '{count}' (reached the end)")

            bufferField.Clear()

            Dim current = offsetField

            For i = 0 To count - 1
                bufferField.Add(dataField(Math.Min(Threading.Interlocked.Increment(offsetField), offsetField - 1)))
            Next

            OnMovedEvent.[Call](current, offsetField)
        End Sub
    End Class
End Namespace

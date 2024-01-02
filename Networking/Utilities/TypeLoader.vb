Imports Common.Extensions
Imports Common.Logging
Imports Common.IO.Collections

Imports Networking.Data

Imports System
Imports System.Reflection
Imports System.Collections.Generic
Imports System.Linq
Imports System.Runtime.CompilerServices

Namespace Networking.Utilities
    Public Module TypeLoader
        Public ReadOnly writers As LockedDictionary(Of Type, Action(Of Writer, Object))
        Public ReadOnly readers As LockedDictionary(Of Type, Func(Of Reader, Object))

        Public ReadOnly log As LogOutput

        Sub New()
            log = New LogOutput("Type Loader")
            Call log.Setup()

            log.Info($"Initializing types.")

            writers = New LockedDictionary(Of Type, Action(Of Writer, Object))()
            readers = New LockedDictionary(Of Type, Func(Of Reader, Object))()

            Dim assemblies = New List(Of Assembly)(AppDomain.CurrentDomain.GetAssemblies())
            Dim currAssembly = Assembly.GetExecutingAssembly()

            Dim readerType = GetType(Reader)
            Dim writerType = GetType(Writer)

            If Not assemblies.Contains(currAssembly) Then assemblies.Add(currAssembly)

            Dim joinedMethods = readerType.GetAllMethods().Union(writerType.GetAllMethods())

            Call log.Trace($"Found {joinedMethods.Count()} joined methods")

            For Each method In joinedMethods
                If method.IsStatic Then Continue For
                                ''' Cannot convert IfStatementSyntax, System.InvalidCastException: Unable to cast object of type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.EmptyStatementSyntax' to type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax'.
'''    at ICSharpCode.CodeConverter.VB.MethodBodyExecutableStatementVisitor.CollectElseBlocks(IfStatementSyntax node, List`1 elseIfBlocks, ElseBlockSyntax& elseBlock)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyExecutableStatementVisitor.VisitIfStatement(IfStatementSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.DefaultVisit(SyntaxNode node)
''' 
''' Input:
''' 
'''                 if (@method.Name.StartsWith("Read") && @method.ReturnType != typeof(void) && Common.Extensions.MethodExtensions.Parameters(@method).Length == 0)
'''                 {
'''                     var readType = @method.ReturnType;
''' 
'''                     if (Networking.Utilities.TypeLoader.readers.ContainsKey(readType))
'''                         continue;
''' 
'''                     Networking.Utilities.TypeLoader.readers[readType] = reader => @method.Call(reader);
''' 
'''                     Networking.Utilities.TypeLoader.log.Trace($"Cached default reader: {readType.FullName} ({@method.ToName()})");
'''                 }
'''                 else if (@method.Name.StartsWith("Write") && @method.ReturnType == typeof(void))
'''                 {
'''                     var methodParams = @method.Parameters();
''' 
'''                     if (methodParams.Length != 1)
'''                         continue;
''' 
'''                     var writeType = methodParams[(System.Int32)(0)].ParameterType;
''' 
'''                     if (Networking.Utilities.TypeLoader.writers.ContainsKey(writeType))
'''                         continue;
''' 
'''                     Networking.Utilities.TypeLoader.writers[writeType] = (writer, value) => @method.Call(writer, value);
''' 
'''                     Networking.Utilities.TypeLoader.log.Trace($"Cached default writer: {writeType.FullName} ({@method.ToName()})");
'''                 }
''' 
''' 
            Next

            For Each assembly In assemblies
                For Each type In assembly.GetTypes()
                    For Each method In type.GetAllMethods()
                        If Not method.IsStatic Then Continue For

                        Dim methodParams = method.Parameters()
                                                ''' Cannot convert IfStatementSyntax, System.InvalidCastException: Unable to cast object of type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.EmptyStatementSyntax' to type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax'.
'''    at ICSharpCode.CodeConverter.VB.MethodBodyExecutableStatementVisitor.CollectElseBlocks(IfStatementSyntax node, List`1 elseIfBlocks, ElseBlockSyntax& elseBlock)
'''    at ICSharpCode.CodeConverter.VB.MethodBodyExecutableStatementVisitor.VisitIfStatement(IfStatementSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.DefaultVisit(SyntaxNode node)
''' 
''' Input:
''' 
'''                         if (@method.ReturnType != typeof(void) && methodParams.Length == 2 && methodParams[(System.Int32)(0)].ParameterType == readerType)
'''                         {
'''                             if (Networking.Utilities.TypeLoader.readers.ContainsKey(@method.ReturnType))
'''                                 continue;
''' 
'''                             Networking.Utilities.TypeLoader.readers[@method.ReturnType] = reader => @method.Call(null, reader);
''' 
'''                             Networking.Utilities.TypeLoader.log.Trace($"Cached custom reader: {@method.ReturnType.FullName} ({@method.ToName()})");
'''                         }
'''                         else if (@method.ReturnType == typeof(void) && methodParams.Length == 1 && methodParams[(System.Int32)(0)].ParameterType == typeof(Networking.Data.Writer))
'''                         {
'''                             if (Networking.Utilities.TypeLoader.writers.ContainsKey(methodParams[(System.Int32)(0)].ParameterType))
'''                                 continue;
''' 
'''                             Networking.Utilities.TypeLoader.writers[methodParams[(System.Int32)(0)].ParameterType] = (writer, value) => @method.Call(null, writer, value);
''' 
'''                             Networking.Utilities.TypeLoader.log.Trace($"Cached custom writer: {methodParams[(System.Int32)(0)].ParameterType.FullName} ({@method.ToName()})");
'''                         }
''' 
''' 
                    Next
                Next
            Next

            log.Info($"Type Loader has finished.
" & $"WRITERS: {writers.Count}
" & $"READERS: {readers.Count}")
        End Sub

        <Extension()>
        Public Function GetWriter(Me type As Type) As Action(Of Writer, Object)
            Dim writer As Action(Of Writer, Object) = Nothing
            If writers.TryGetValue(type, writer) Then Return writer

            Throw New Exception($"No writers for type {type.FullName}")
        End Function

        <Extension()>
        Public Function GetReader(Me type As Type) As Func(Of Reader, Object)
            Dim reader As Func(Of Reader, Object) = Nothing
            If readers.TryGetValue(type, reader) Then Return reader

            Throw New Exception($"No readers for type {type.FullName}")
        End Function
    End Module
End Namespace

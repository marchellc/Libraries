Imports Common.Extensions
Imports Common.IO.Collections

Imports Networking.Features
Imports Networking.Utilities

Imports System
Imports System.Linq
Imports System.Collections.Generic
Imports System.Reflection

Namespace Networking.Objects
    Public Class NetworkManager
        Inherits NetworkFeature
        Public objects As LockedDictionary(Of Integer, NetworkObject)

        Public objectTypes As LockedDictionary(Of Short, Type)

        Public netProperties As LockedDictionary(Of UShort, PropertyInfo)
        Public netFields As LockedDictionary(Of UShort, FieldInfo)
        Public netMethods As LockedDictionary(Of UShort, MethodInfo)

        Public objectId As Integer
        Public objectTypeId As Short

        Public Overrides Sub Start()
            objects = New LockedDictionary(Of Integer, NetworkObject)()
            objectTypes = New LockedDictionary(Of Short, Type)()

            netProperties = New LockedDictionary(Of UShort, PropertyInfo)()
            netFields = New LockedDictionary(Of UShort, FieldInfo)()

            objectId = 0
            objectTypeId = 0

            If net.isServer Then
                LoadAndSendTypes()
                Call Listen(New Action(Of NetworkCmdMessage)(AddressOf OnCmd))
            Else
                Call Listen(New Action(Of NetworkObjectSyncMessage)(AddressOf OnSync))
                Call Listen(New Action(Of NetworkRpcMessage)(AddressOf OnRpc))
            End If

            Call Listen(Of NetworkObjectAddMessage)(New Action(Of NetworkObjectAddMessage)(AddressOf Me.OnCreated))
            Call Listen(New Action(Of NetworkObjectRemoveMessage)(AddressOf OnDestroyed))
            Call Listen(New Action(Of NetworkVariableSyncMessage)(AddressOf OnVarSync))

            log.Info($"Object networking initialized.")
        End Sub

        Public Overrides Sub [Stop]()
            Call Remove(Of NetworkObjectSyncMessage)()
            Call Remove(Of NetworkObjectAddMessage)()
            Call Remove(Of NetworkObjectRemoveMessage)()
            Call Remove(Of NetworkVariableSyncMessage)()
            Call Remove(Of NetworkRpcMessage)()
            Call Remove(Of NetworkCmdMessage)()

            objects.Clear()
            objects = Nothing

            objectTypes.Clear()
            objectTypes = Nothing

            objectId = 0
            objectTypeId = 0

            log.Info($"Object networking unloaded.")
        End Sub

                ''' Cannot convert MethodDeclarationSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: node
''' Actual value was not tT t.
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
'''         public tT Instantiate<tT>() where tT : Networking.Objects.NetworkObject
'''         {
'''             var netObjInstance = this.Instantiate(typeof(tT));
''' 
'''             if (netObjInstance is null)
'''                 throw new System.Exception($"Failed to instantiate type '{typeof(tT).FullName}'");
''' 
'''             if (netObjInstance is not tT t)
'''                 throw new System.Exception($"Instantiated type cannot be cast to '{typeof(tT).FullName}'");
''' 
'''             return t;
'''         }
''' 
''' 
                ''' Cannot convert MethodDeclarationSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: node
''' Actual value was not tT t.
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
'''         public tT Get<tT>(int objectId)
'''         {
'''             if (!this.objects.TryGetValue(objectId, out var netObj))
'''                 throw new System.InvalidOperationException($"No network object with ID {objectId}");
''' 
'''             if (netObj is not tT t)
'''                 throw new System.InvalidOperationException($"Cannot cast object {netObj.GetType().FullName} to {typeof(tT).FullName}");
''' 
'''             return t;
'''         }
''' 
''' 

        Public Function [Get](objectId As Integer) As NetworkObject
            Dim netObj As NetworkObject = Nothing
            If Not objects.TryGetValue(objectId, netObj) Then Throw New InvalidOperationException($"No network object with ID {objectId}")

            Return netObj
        End Function

                ''' Cannot convert MethodDeclarationSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: node
''' Actual value was not Networking.Objects.NetworkObject netObjInstance.
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
'''         public Networking.Objects.NetworkObject Instantiate(System.Type type)
'''         {
'''             if (this.objectTypes.Count <= 0)
'''                 throw new System.InvalidOperationException($"Types were not synchronized.");
''' 
'''             if (!this.objectTypes.TryGetKey(@type, out var typeId))
'''                 throw new System.InvalidOperationException($"Cannot instantiate a non network object type");
''' 
'''             var netObj = @type.Construct([this.objectId++, this]);
''' 
'''             if (netObj is null || netObj is not Networking.Objects.NetworkObject netObjInstance)
'''                 throw new System.Exception($"Failed to instantiate type '{@type.FullName}'");
''' 
'''             netObjInstance.StartInternal();
'''             netObjInstance.OnStart();
''' 
'''             this.objects[netObjInstance.id] = netObjInstance;
''' 
'''             base.net.Send(new Networking.Objects.NetworkObjectAddMessage(typeId));
''' 
'''             netObjInstance.isReady = true;
'''             netObjInstance.isDestroyed = false;
''' 
'''             base.log.Trace($"Instantiated network type {@type.FullName} at {netObjInstance.id}");
''' 
'''             return netObjInstance;
'''         }
''' 
''' 

        Public Sub Destroy(netObject As NetworkObject)
            If netObject Is Nothing Then Throw New ArgumentNullException(NameOf(netObject))

            If netObject.id <= 0 OrElse Not objects.ContainsKey(netObject.id) Then Throw New InvalidOperationException($"This network object was not spawned by this manager.")

            If netObject.isDestroyed Then Throw New InvalidOperationException($"Object is already destroyed")

            netObject.OnStop()
            netObject.StopInternal()

            objects.Remove(netObject.id)

            net.Send(New NetworkObjectRemoveMessage(netObject.id))

            netObject.isDestroyed = True
            netObject.isReady = False

            log.Trace($"Destroyed network type {netObject.GetType().FullName} at {netObject.id}")
        End Sub

        Private Sub OnCmd(cmdMsg As NetworkCmdMessage)
            If net.isClient Then Return

            Dim netObj As NetworkObject = Nothing
            If Not objects.TryGetValue(cmdMsg.objectId, netObj) Then Return

            Dim netMethod As MethodInfo = Nothing
            If Not netMethods.TryGetValue(cmdMsg.functionHash, netMethod) Then Return

            log.Trace($"Received cmdMsg: {cmdMsg.objectId} {cmdMsg.functionHash} ({netMethod.ToName()})")

            netMethod.Call(netObj, cmdMsg.args)
        End Sub

        Private Sub OnRpc(rpcMsg As NetworkRpcMessage)
            If net.isServer Then Return

            Dim netObj As NetworkObject = Nothing
            If Not objects.TryGetValue(rpcMsg.objectId, netObj) Then Return

            Dim netMethod As MethodInfo = Nothing
            If Not netMethods.TryGetValue(rpcMsg.functionHash, netMethod) Then Return

            log.Trace($"Received rpcMsg: {rpcMsg.objectId} {rpcMsg.functionHash} ({netMethod.ToName()})")

            netMethod.Call(netObj, rpcMsg.args)
        End Sub

        Private Sub OnVarSync(syncMsg As NetworkVariableSyncMessage)
            Dim netObj As NetworkObject = Nothing
            If Not objects.TryGetValue(syncMsg.objectId, netObj) Then Return

            log.Trace($"Received syncMsg: {syncMsg.objectId} {syncMsg.hash} ({syncMsg.msg.GetType().FullName})")

            netObj.ProcessVarSync(syncMsg)
        End Sub

        Private Sub OnDestroyed(destroyMsg As NetworkObjectRemoveMessage)
            Dim netObj As NetworkObject = Nothing
            If Not objects.TryGetValue(destroyMsg.objectId, netObj) Then Return

            netObj.OnStop()
            netObj.StopInternal()

            objects.Remove(destroyMsg.objectId)

            netObj.isDestroyed = True
            netObj.isReady = False

            log.Trace($"Received DESTROY: {destroyMsg.objectId}")
        End Sub

                ''' Cannot convert MethodDeclarationSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: node
''' Actual value was not Networking.Objects.NetworkObject netObjInstance.
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
'''         private void OnCreated(Networking.Objects.NetworkObjectAddMessage addMsg)
'''         {
'''             if (!this.objectTypes.TryGetValue(addMsg.typeId, out var type))
'''                 return;
''' 
'''             var netObj = @type.Construct([this.objectId++, this]);
''' 
'''             if (netObj is null || netObj is not Networking.Objects.NetworkObject netObjInstance)
'''                 throw new System.Exception($"Failed to instantiate type '{@type.FullName}'");
''' 
'''             netObjInstance.StartInternal();
'''             netObjInstance.OnStart();
''' 
'''             this.objects[netObjInstance.id] = netObjInstance;
''' 
'''             netObjInstance.isReady = true;
'''             netObjInstance.isDestroyed = false;
''' 
'''             base.log.Trace($"Received ADD: {addMsg.typeId} ({@type.FullName})");
'''         }
''' 
''' 

        Private Sub OnSync(syncMsg As NetworkObjectSyncMessage)
            If net.isServer Then Return

            If syncMsg.syncTypes Is Nothing OrElse syncMsg.syncTypes.Count <= 0 Then Return

            For Each syncType In syncMsg.syncTypes
                objectTypes(syncType.Key) = syncType.Value

                log.Trace($"Cached sync type {syncType.Value.FullName} ({syncType.Key})")

                For Each [property] In syncType.Value.GetAllProperties()
                    If [property].Name.StartsWith("Network") AndAlso [property].GetMethod IsNot Nothing AndAlso [property].SetMethod IsNot Nothing AndAlso Not [property].GetMethod.IsStatic AndAlso Not [property].SetMethod.IsStatic AndAlso [property].PropertyType.GetWriter() IsNot Nothing AndAlso [property].PropertyType.GetReader() IsNot Nothing Then
                        netProperties([property].GetPropertyHash()) = [property]
                        log.Trace($"Cached network property: {[property].ToName()} ({[property].GetPropertyHash()})")
                    End If
                Next

                For Each field In syncType.Value.GetAllFields()
                    If Not field.IsStatic AndAlso field.Name.StartsWith("network") AndAlso field.FieldType.InheritsType(Of NetworkVariable)() AndAlso Not field.IsInitOnly Then
                        netFields(field.GetFieldHash()) = field
                        log.Trace($"Cached network field: {field.ToName()} ({field.GetFieldHash()})")
                    End If
                Next

                For Each method In syncType.Value.GetAllMethods()
                                        ''' Cannot convert IfStatementSyntax, System.InvalidCastException: Unable to cast object of type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.EmptyStatementSyntax' to type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax'.
'''    at ICSharpCode.CodeConverter.VB.MethodBodyExecutableStatementVisitor.VisitIfStatement(IfStatementSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.DefaultVisit(SyntaxNode node)
''' 
''' Input:
'''                     if (!@method.IsStatic && (@method.Name.StartsWith("Cmd") || @method.Name.StartsWith("Rpc")) && @method.ReturnType == typeof(void))
'''                     {
'''                         this.netMethods[@method.GetMethodHash()] = @method;
''' 
'''                         base.log.Trace($"Cached network method: {@method.ToName()} ({@method.GetMethodHash()})");
''' 
'''                         var methodField = @method.DeclaringType.Field($"{@method.Name}Hash");
''' 
'''                         if (methodField != null && !methodField.IsInitOnly && methodField.IsStatic && methodField.FieldType == typeof(ushort))
'''                         {
'''                             methodField.SetValueFast(@method.GetMethodHash());
'''                             base.log.Trace($"Set value to network method field {methodField.ToName()} ({@method.GetMethodHash()})");
'''                         }
'''                     }
''' 
''' 
                Next
            Next
        End Sub

        Private Sub LoadAndSendTypes()
            If net.isClient Then Throw New InvalidOperationException($"The client cannot send their types.")

            If objectTypes.Count > 0 Then Throw New InvalidOperationException($"Types have already been loaded")

            log.Trace($"Loading sync types")

            For Each assembly In AppDomain.CurrentDomain.GetAssemblies()
                For Each type In assembly.GetTypes()
                    If type.InheritsType(Of NetworkObject)() AndAlso type.GetAllConstructors().Any(Function(c) c.Parameters().Length = 0 AndAlso Not type.ContainsGenericParameters) Then
                        objectTypes(Math.Min(Threading.Interlocked.Increment(objectTypeId), objectTypeId - 1)) = type

                        log.Trace($"Cached sync type: {type.FullName}")

                        For Each [property] In type.GetAllProperties()
                            If [property].Name.StartsWith("Network") AndAlso [property].GetMethod IsNot Nothing AndAlso [property].SetMethod IsNot Nothing AndAlso Not [property].GetMethod.IsStatic AndAlso Not [property].SetMethod.IsStatic AndAlso [property].PropertyType.GetWriter() IsNot Nothing AndAlso [property].PropertyType.GetReader() IsNot Nothing Then
                                netProperties([property].GetPropertyHash()) = [property]
                                log.Trace($"Cached network property: {[property].ToName()} ({[property].GetPropertyHash()})")
                            End If
                        Next

                        For Each field In type.GetAllFields()
                            If Not field.IsStatic AndAlso field.Name.StartsWith("network") AndAlso field.FieldType.InheritsType(Of NetworkVariable)() AndAlso Not field.IsInitOnly Then
                                netFields(field.GetFieldHash()) = field
                                log.Trace($"Cached network field: {field.ToName()} ({field.GetFieldHash()})")
                            End If
                        Next

                        For Each method In type.GetAllMethods()
                                                        ''' Cannot convert IfStatementSyntax, System.InvalidCastException: Unable to cast object of type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.EmptyStatementSyntax' to type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax'.
'''    at ICSharpCode.CodeConverter.VB.MethodBodyExecutableStatementVisitor.VisitIfStatement(IfStatementSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingMethodBodyVisitor.DefaultVisit(SyntaxNode node)
''' 
''' Input:
'''                             if (!@method.IsStatic && (@method.Name.StartsWith("Cmd") || @method.Name.StartsWith("Rpc")) && @method.ReturnType == typeof(void))
'''                             {
'''                                 this.netMethods[@method.GetMethodHash()] = @method;
''' 
'''                                 base.log.Trace($"Cached network method: {@method.ToName()} ({@method.GetMethodHash()})");
''' 
'''                                 var methodField = @method.DeclaringType.Field($"{@method.Name}Hash");
''' 
'''                                 if (methodField != null && !methodField.IsInitOnly && methodField.IsStatic && methodField.FieldType == typeof(ushort))
'''                                 {
'''                                     methodField.SetValueFast(@method.GetMethodHash());
'''                                     base.log.Trace($"Set value to network method field {methodField.ToName()} ({@method.GetMethodHash()})");
'''                                 }
'''                             }
''' 
''' 
                        Next
                    End If
                Next
            Next

            net.Send(New NetworkObjectSyncMessage(New Dictionary(Of Short, Type)(objectTypes)))

            log.Trace($"Sent network sync type message")
        End Sub
    End Class
End Namespace

Imports Common.Extensions
Imports Common.IO.Collections
Imports Common.Logging

Imports Networking.Data
Imports Networking.Features
Imports Networking.Pooling

Imports System
Imports System.Net
Imports System.Threading
Imports System.Collections.Concurrent

Namespace Networking.Server
    Public Class NetworkConnection
        Private handshakeSentAt As Date

        Private timer As Timer
        Private ping As Timer
        Private handshakeTimer As Timer

        Private inDataQueue As ConcurrentQueue(Of Byte())
        Private outDataQueue As ConcurrentQueue(Of Byte())

        Private features As LockedDictionary(Of Type, NetworkFeature)

        Public log As LogOutput
        Public writers As WriterPool
        Public readers As ReaderPool
        Public funcs As NetworkFunctions
        Public server As NetworkServer

        Public isNoDelay As Boolean = True

        Public isAuthed As Boolean
        Public isAuthReceived As Boolean

        Public maxOutput As Integer = 10
        Public maxInput As Integer = 10

        Public latency As Double = 0

        Public maxLatency As Double = 0
        Public minLatency As Double = 0
        Public avgLatency As Double = 0

        Public status As NetworkConnectionStatus = NetworkConnectionStatus.Disconnected
        Public handshake As NetworkHandshakeResult = NetworkHandshakeResult.TimedOut

        Public ReadOnly id As Integer
        Public ReadOnly remote As IPEndPoint

        Public Event OnAuthorized As Action
        Public Event OnPinged As Action
        Public Event OnMessage As Action(Of Type, Object)
                ''' Cannot convert ConstructorDeclarationSyntax, System.ArgumentOutOfRangeException: Exception of type 'System.ArgumentOutOfRangeException' was thrown.
''' Parameter name: node
''' Actual value was not Networking.Features.NetworkFeature netFeature.
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
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitConstructorDeclaration(ConstructorDeclarationSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingVisitorWrapper`1.Accept(SyntaxNode csNode, Boolean addSourceMapping)
''' 
''' Input:
''' 
'''         public NetworkConnection(int id, Networking.Server.NetworkServer server)
'''         {
'''             this.id = id;
'''             this.server = server;
'''             this.remote = server.server.GetClientEndPoint(id);
''' 
'''             this.log = new Common.Logging.LogOutput($"Network Connection ({this.remote})");
'''             this.log.Setup();
''' 
'''             this.writers = new Networking.Pooling.WriterPool();
'''             this.writers.Initialize(20);
''' 
'''             this.readers = new Networking.Pooling.ReaderPool();
'''             this.readers.Initialize(20);
''' 
'''             this.funcs = new Networking.Features.NetworkFunctions(
'''                 () => { return this.writers.Next(); },
''' 
'''                 netData => { return this.readers.Next(netData); },
'''                 netWriter => { this.Send(netWriter); },
''' 
'''                 false);
''' 
'''             this.status = Networking.NetworkConnectionStatus.Connected;
''' 
'''             this.log.Info($"Connection initialized on {this.remote}");
''' 
'''             this.timer = new System.Threading.Timer(_ => this.UpdateDataQueue(), null, 100, 100);
'''             this.ping = new System.Threading.Timer(_ => this.UpdatePing(), null, 100, 500);
''' 
'''             foreach (var type in server.requestFeatures)
'''             {
'''                 this.log.Trace($"Constructing feature: {@type.FullName}");
''' 
'''                 var feature = @type.Construct();
''' 
'''                 if (feature is null || feature is not Networking.Features.NetworkFeature netFeature)
'''                     continue;
''' 
'''                 this.features[@type] = netFeature;
''' 
'''                 netFeature.net = this.funcs;
''' 
'''                 this.log.Trace($"Constructed feature: {@type.FullName}");
'''             }
''' 
'''             this.Send(writer =>
'''             {
'''                 writer.WriteVersion(Networking.Server.NetworkServer.version);
'''                 writer.WriteDate(System.DateTime.Now.ToLocalTime());
'''             });
''' 
'''             this.log.Trace($"Sent handshake");
''' 
'''             this.handshakeSentAt = System.DateTime.Now;
'''             this.handshakeTimer = new System.Threading.Timer(_ => this.UpdateHandshake(), null, 0, 200);
'''         }
''' 
''' 

        Public Function [Get](Of T As NetworkFeature)() As T
            Dim feature As NetworkFeature = Nothing, netFeature As T = Nothing
            If features.TryGetValue(GetType(T), feature) AndAlso CSharpImpl.__Assign(netFeature, TryCast(feature, T)) IsNot Nothing Then Return netFeature

            Return Nothing
        End Function

        Public Sub Add(Of T As NetworkFeature)()
            If isAuthed Then
                If features.ContainsKey(GetType(T)) Then Return

                log.Trace($"Adding feature {GetType(T).FullName}")

                Dim feature = Activator.CreateInstance(Of T)()

                If feature Is Nothing Then Return

                feature.net = funcs
                feature.InternalStart()
                                ''' Cannot convert AssignmentExpressionSyntax, System.InvalidCastException: Unable to cast object of type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.EmptyStatementSyntax' to type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax'.
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingVisitorWrapper`1.Accept(SyntaxNode csNode, Boolean addSourceMapping)
''' 
''' Input:
'''                 feature.log!.Name += $" ({this.remote})"
''' 

                features(GetType(T)) = feature

                log.Trace($"Added feature {GetType(T).FullName}")
            End If
        End Sub

        Public Sub Remove(Of T As NetworkFeature)()
            Dim feature As NetworkFeature = Nothing
            If isAuthed Then
                If features.TryGetValue(GetType(T), feature) Then feature.InternalStop()

                features.Remove(GetType(T))
            End If
        End Sub

        Public Sub Send(writerFunction As Action(Of Writer))
            Dim writer = writers.Next()
            writerFunction.[Call](writer)
            Send(writer)
        End Sub

        Public Sub Send(writer As Writer)
            If isNoDelay Then
                server.server.Send(id, writer.Buffer.ToSegment())
                log.Trace($"Sent writer of {writer.Buffer.Length} bytes")
            Else
                inDataQueue.Enqueue(writer.Buffer)
                log.Trace($"Queued writer buffer of {writer.Buffer.Length} bytes")
            End If

            writer.Return()
        End Sub

        Public Sub Disconnect()
            server.server.Disconnect(id)
        End Sub

        Friend Sub InternalReceive(data As Byte())
            Dim reader = readers.Next(data)

            log.Trace($"Receiving {data.Length} / {reader.Size} bytes")
            Dim pingMsg As NetworkPingMessage = Nothing

            If Not isAuthed Then
                log.Trace($"Reading handshake result (not authed yet)")
                handshake = CType(reader.ReadByte(), NetworkHandshakeResult)
                log.Trace($"Result: {handshake}")
                readers.Return(reader)
                Return
            Else
                log.Trace($"Reading messages")

                Dim messages = reader.ReadAnonymousArray()

                log.Trace($"Read {messages.Length} messages")

                For i = 0 To messages.Length - 1

                    If CSharpImpl.__Assign(pingMsg, TryCast(messages(i), NetworkPingMessage)) IsNot Nothing Then
                        ProcessPing(pingMsg)
                        Continue For
                    End If

                    OnMessageEvent.[Call](messages(i).GetType(), messages(i))

                    For Each feature In features.Values
                        If feature.isRunning AndAlso feature.HasListener(messages(i).GetType()) Then
                            log.Trace($"Feature {feature.GetType().FullName} is processing message {messages(i).GetType().FullName}")
                            feature.Receive(messages(i))
                            Exit For
                        End If
                    Next
                Next

                log.Trace($"Processing finished")

                readers.Return(reader)
                Return
            End If
        End Sub

        Friend Sub Receive(data As Byte())
            If isNoDelay Then
                log.Trace($"Receiving {data.Length} bytes")
                InternalReceive(data)
                Return
            End If

            inDataQueue.Enqueue(data)
            log.Trace($"Queued {data.Length} bytes")
        End Sub

        Friend Sub [Stop]()
            timer?.Dispose()
            timer = Nothing

            handshakeTimer?.Dispose()
            handshakeTimer = Nothing

            ping?.Dispose()
            ping = Nothing

            outDataQueue?.Clear()
            inDataQueue?.Clear()

            outDataQueue = Nothing
            inDataQueue = Nothing

            If features IsNot Nothing Then
                For Each feature In features.Values
                    feature.InternalStop()
                Next
            End If

            features?.Clear()
            features = Nothing

            writers.Clear()
            writers = Nothing

            readers.Clear()
            readers = Nothing

            funcs = Nothing
            server = Nothing

            status = NetworkConnectionStatus.Disconnected
            handshake = NetworkHandshakeResult.TimedOut

            log?.Dispose()
            log = Nothing

            isAuthed = False
        End Sub

        Private Sub ProcessPing(pingMsg As NetworkPingMessage)
            If pingMsg.isServer Then Return

            log.Trace($"Processing ping message")

            latency = (pingMsg.recv - pingMsg.sent).TotalMilliseconds

            If latency > maxLatency Then maxLatency = latency

            If minLatency = 0 OrElse latency < minLatency Then minLatency = latency

            avgLatency = (minLatency + maxLatency) / 2

            OnPingedEvent.Call()

            log.Trace($"Processed: {latency} ms")
        End Sub

        Private Sub UpdateHandshake()
            If (Date.Now - handshakeSentAt).TotalSeconds >= 15 Then
                log.Warn($"Handshake has timed out")

                isAuthReceived = True
                handshake = NetworkHandshakeResult.TimedOut
                Return
            End If

            If Not isAuthReceived Then Return

            If handshake <> NetworkHandshakeResult.Confirmed Then
                log.Warn($"Handshake has not been confirmed, disconnecting")

                Disconnect()
                Return
            End If

            log.Info($"Handshake completed, initializing")

            isAuthed = True
            isAuthReceived = True

            handshakeTimer.Dispose()
            handshakeTimer = Nothing

            For Each feature In features.Values
                feature.InternalStart()
                                ''' Cannot convert AssignmentExpressionSyntax, System.InvalidCastException: Unable to cast object of type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.EmptyStatementSyntax' to type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.ExpressionSyntax'.
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitAssignmentExpression(AssignmentExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingVisitorWrapper`1.Accept(SyntaxNode csNode, Boolean addSourceMapping)
''' 
''' Input:
'''                 feature.log!.Name += $" ({this.remote})"
''' 
            Next

            OnAuthorizedEvent.Call()
        End Sub

        Private Sub UpdatePing()
            Send(Sub(writer) writer.WriteAnonymousArray(New NetworkPingMessage(True, Date.Now, Date.MinValue)))
            OnPingedEvent.Call()
            log.Trace($"Sent a ping message to the client")
        End Sub

        Private Sub UpdateDataQueue()
            If isNoDelay Then
                timer?.Dispose()
                timer = Nothing

                Return
            End If

            Dim outProcessed = 0

            Dim outData As Byte() = Nothing

            While outDataQueue.TryDequeue(outData) AndAlso outProcessed <= maxOutput
                server.server.Send(id, outData.ToSegment())
                outProcessed += 1
            End While

            Dim inProcessed = 0

            Dim inData As Byte() = Nothing

            While inDataQueue.TryDequeue(inData) AndAlso inProcessed <= maxInput
                InternalReceive(inData)
                inProcessed += 1
            End While
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

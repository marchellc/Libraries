Imports Common.Logging
Imports Common.Extensions
Imports Common.IO.Collections

Imports Networking.Address
Imports Networking.Data
Imports Networking.Pooling
Imports Networking.Features

Imports System
Imports System.Net
Imports System.Threading
Imports System.Collections.Concurrent
Imports System.Threading.Tasks

Namespace Networking.Client
    Public Class NetworkClient
        Private client As Telepathy.Client

        Private funcs As NetworkFunctions

        Private timer As Timer

        Private inDataQueue As ConcurrentQueue(Of Byte())
        Private outDataQueue As ConcurrentQueue(Of Byte())

        Private features As LockedDictionary(Of Type, NetworkFeature)

        Private processLock As Object = New Object()

        Public maxOutput As Integer = 10
        Public maxInput As Integer = 10
        Public maxReconnections As Integer = 15

        Public reconnectionReset As Integer = 1500
        Public reconnectionResetMultiplier As Integer = 2

        Public latency As Double = 0

        Public maxLatency As Double = 0
        Public minLatency As Double = 0
        Public avgLatency As Double = 0

        Public reconnectionTimeout As Integer = 1500
        Public reconnectionFailed As Integer = 0
        Public reconnectionResets As Integer = 0

        Public isRunning As Boolean
        Public isDisconnecting As Boolean
        Public isNoDelay As Boolean = True

        Public wasEverConnected As Boolean

        Public status As NetworkConnectionStatus = NetworkConnectionStatus.Disconnected
        Public handshake As NetworkHandshakeResult = NetworkHandshakeResult.TimedOut

        Public target As IPInfo

        Public log As LogOutput

        Public ReadOnly writers As WriterPool
        Public ReadOnly readers As ReaderPool

        Public Event OnDisconnected As Action
        Public Event OnConnected As Action
        Public Event OnAuthorized As Action
        Public Event OnPinged As Action

        Public Event OnData As Action(Of ArraySegment(Of Byte))
        Public Event OnMessage As Action(Of Type, Object)

        Public Shared ReadOnly version As Version
        Public Shared ReadOnly instance As NetworkClient

        Shared Sub New()
            version = New Version(1, 0, 0, 0)
            instance = New NetworkClient()
        End Sub

        Public Sub New()
            log = New LogOutput("Network Client")
            log.Setup()

            writers = New WriterPool()
            readers = New ReaderPool()

            writers.Initialize(20)
            readers.Initialize(20)

            inDataQueue = New ConcurrentQueue(Of Byte())()
            outDataQueue = New ConcurrentQueue(Of Byte())()

            target = New IPInfo(IPType.Remote, 8000, IPAddress.Loopback)



            funcs = New NetworkFunctions(Function() writers.Next(), Function(netData) readers.Next(netData), Sub(netWriter) Send(netWriter), True)
        End Sub

        Public Function [Get](Of T As {NetworkFeature, New})() As T
            Dim feature As NetworkFeature = Nothing
            If features.TryGetValue(GetType(T), feature) Then Return feature

            Call Add(Of T)()

            Return features(GetType(T))
        End Function

        Public Sub Add(Of T As {NetworkFeature, New})()
            If features.ContainsKey(GetType(T)) Then Return

            Dim feature = New T()

            feature.net = funcs

            If TypeOf status Is NetworkConnectionStatus.Connected Then feature.InternalStart()

            features(GetType(T)) = feature
        End Sub

        Public Sub Remove(Of T As {NetworkFeature, New})()
            Dim feature As NetworkFeature = Nothing
            If Not features.TryGetValue(GetType(T), feature) Then Return

            feature.InternalStop()

            features.Remove(GetType(T))
        End Sub

        Public Sub Send(writer As Action(Of Writer))
            funcs.Send(writer)
        End Sub

        Public Sub Send(writer As Writer)
            If writer Is Nothing Then Throw New ArgumentNullException(NameOf(writer))

            outDataQueue.Enqueue(writer.Buffer)

            If writer.pool IsNot Nothing Then writer.Return()
        End Sub

        Public Sub Send(data As Byte())
            If data Is Nothing Then Throw New ArgumentNullException(NameOf(data))

            If data.Length <= 0 Then Throw New ArgumentOutOfRangeException(NameOf(data))

            outDataQueue.Enqueue(data)

            log.Trace($"Enqueued {data.Length} bytes in send queue")
        End Sub

        Public Sub Connect(endPoint As IPEndPoint)
            If isRunning Then Disconnect()

            If endPoint IsNot Nothing AndAlso target.endPoint IsNot Nothing AndAlso target.endPoint IsNot endPoint Then
                target = New IPInfo(IPType.Remote, endPoint.Port, endPoint.Address)
            ElseIf target.endPoint Is Nothing AndAlso endPoint IsNot Nothing Then
                target = New IPInfo(IPType.Remote, endPoint.Port, endPoint.Address)
            End If

            log.Info($"Client connecting to {target} ..")

            outDataQueue.Clear()
            inDataQueue.Clear()

            client = New Telepathy.Client(Integer.MaxValue - 10)

            client.OnConnected = AddressOf HandleConnect
            client.OnDisconnected = AddressOf HandleDisconnect
            client.OnData = AddressOf HandleData

            client.NoDelay = isNoDelay

            status = NetworkConnectionStatus.Connecting

            isRunning = True
            isDisconnecting = False

            client.Connect(target.address.ToString(), target.port, New Action(AddressOf OnConnectionSuccess), New Action(AddressOf OnConnectionFail))
        End Sub

        Public Sub Disconnect()
            If client Is Nothing OrElse Not client.Connected Then Throw New InvalidOperationException($"Cannot disconnect; socket is not connected")

            If isDisconnecting Then Throw New InvalidOperationException($"Client is already disconnecting")

            isDisconnecting = True

            client.Disconnect()
        End Sub

        Private Sub HandleConnect()
            wasEverConnected = True
            status = NetworkConnectionStatus.Connected
            timer = New Timer(Sub(__) UpdateDataQueue(), Nothing, 0, 150)

            OnConnectedEvent.Call()
        End Sub

        Private Sub HandleDisconnect()
            isDisconnecting = False

            timer?.Dispose()
            timer = Nothing

            outDataQueue.Clear()
            inDataQueue.Clear()

            status = NetworkConnectionStatus.Disconnected

            isRunning = False

            OnDisconnectedEvent.Call()

            For Each feature In features.Values
                If Not feature.isRunning Then Continue For

                feature.InternalStop()
            Next
        End Sub

        Private Sub HandleData(input As ArraySegment(Of Byte))
            If Not isRunning Then Throw New InvalidOperationException($"The client needs to be running to process data")

            inDataQueue.Enqueue(input.ToArray())

            OnDataEvent.[Call](input)
        End Sub

        Private Sub OnConnectionSuccess()
        End Sub

        Private Sub OnConnectionFail()
            status = NetworkConnectionStatus.Disconnected

            ' this is a reset
            If reconnectionFailed >= maxReconnections Then
                reconnectionResets += 1
                reconnectionFailed = 0
                reconnectionTimeout *= reconnectionResetMultiplier

                log.Warn($"Max reconnection limit reached, retrying in {reconnectionTimeout} ms ..")

                Call Task.Run(Async Function()
                                  Await Task.Delay(reconnectionTimeout)

                                  reconnectionFailed = 0
                                  reconnectionTimeout = reconnectionResets * (reconnectionTimeout * reconnectionResetMultiplier)

                                  OnConnectionFail()
                              End Function)
            Else
                log.Warn($"Attempting to reconnect .. ({reconnectionFailed}/{maxReconnections})")

                reconnectionFailed += 1

                Call Task.Run(Async Function()
                                  Await Task.Delay(reconnectionTimeout)

                                  status = NetworkConnectionStatus.Connecting
                                  client.Connect(target.address.ToString(), target.port, New Action(AddressOf OnConnectionSuccess), New Action(AddressOf OnConnectionFail))
                              End Function)
            End If
        End Sub

        Private Sub InternalStart()
            log.Info($"Client has authorized, initializing ..")

            OnAuthorizedEvent.Call()

            For Each feature In features.Values
                feature.net = funcs

                If feature.isRunning Then Continue For

                feature.InternalStart()
            Next
        End Sub

        Private Sub InternalSend(data As Byte())
            If client Is Nothing OrElse Not client.Connected Then Throw New InvalidOperationException($"Cannot send data over an unconnected socket")

            client.Send(data.ToSegment())
        End Sub

        Private Sub InternalReceive(data As Byte())
            Dim reader = readers.Next(data)

            log.Trace($"Received {data.Length} bytes from the server")
            Dim pingMsg As NetworkPingMessage = Nothing

            If TypeOf handshake Is NetworkHandshakeResult.TimedOut Then
                log.Trace($"Client is not authorized, processing handshake")

                Dim serverVersion = reader.ReadVersion()
                Dim serverTime = reader.ReadDate()

                log.Trace($"Server version: {serverVersion}; server timestamp: {serverTime}")

                If serverVersion IsNot version OrElse serverTime.Hour <> Date.Now.ToLocalTime().Hour Then
                    log.Warn($"Version or timestamp mismatch, rejecting handshake.")

                    Send(Sub(writer) writer.WriteByte(NetworkHandshakeResult.Rejected))

                    readers.Return(reader)
                    Return
                End If

                Send(Sub(writer) writer.WriteByte(NetworkHandshakeResult.Confirmed))

                log.Trace($"Server handshake accepted")

                InternalStart()

                readers.Return(reader)
                Return
            Else
                Dim messages = reader.ReadAnonymousArray()

                log.Trace($"Received {messages.Length} messages")

                For i = 0 To messages.Length - 1

                    If CSharpImpl.__Assign(pingMsg, TryCast(messages(i), NetworkPingMessage)) IsNot Nothing Then
                        ProcessPing(pingMsg)
                        Continue For
                    End If

                    OnMessageEvent.[Call](messages(i).GetType(), messages(i))

                    For Each feature In features.Values
                        If feature.isRunning AndAlso feature.HasListener(messages(i).GetType()) Then
                            feature.Receive(messages(i))
                            Exit For
                        End If
                    Next
                Next

                readers.Return(reader)
                Return
            End If
        End Sub

        Private Sub ProcessPing(pingMsg As NetworkPingMessage)
            If Not pingMsg.isServer Then Return

            latency = (pingMsg.recv - pingMsg.sent).TotalMilliseconds

            If latency > maxLatency Then maxLatency = latency

            If minLatency = 0 OrElse latency < minLatency Then minLatency = latency

            avgLatency = (minLatency + maxLatency) / 2

            Send(Sub(writer)
                     pingMsg.isServer = False
                     writer.WriteAnonymousArray(pingMsg)
                 End Sub)

            OnPingedEvent.Call()
        End Sub

        Private Sub UpdateDataQueue()


            Dim outData As Byte() = Nothing, inData As Byte() = Nothing
            SyncLock processLock
                If status <> NetworkConnectionStatus.Connected OrElse outDataQueue Is Nothing OrElse inDataQueue Is Nothing OrElse Not isRunning OrElse isNoDelay Then Return

                client.Tick(100)

                Dim outProcessed = 0
                Dim inProcessed = 0
                While outDataQueue.TryDequeue(outData) AndAlso outProcessed <= maxOutput
                    InternalSend(outData)
                    outProcessed += 1
                End While
                While inDataQueue.TryDequeue(inData) AndAlso inProcessed <= maxInput
                    InternalReceive(inData)
                    inProcessed += 1
                End While
            End SyncLock
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

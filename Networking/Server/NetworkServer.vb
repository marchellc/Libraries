Imports Common.Extensions
Imports Common.IO.Collections
Imports Common.Logging
Imports Common.Utilities
Imports Networking.Features

Imports System

Namespace Networking.Server
    Public Class NetworkServer
        Public log As LogOutput
        Public server As Telepathy.Server

        Public connections As LockedDictionary(Of Integer, NetworkConnection)
        Public requestFeatures As LockedList(Of Type)

        Public port As Integer = 8000

        Public isRunning As Boolean
        Public isNoDelay As Boolean = True

        Public Event OnStarted As Action
        Public Event OnStopped As Action
        Public Event OnConnected As Action(Of NetworkConnection)
        Public Event OnDisconnected As Action(Of NetworkConnection)
        Public Event OnData As Action(Of NetworkConnection, Byte())

        Public Shared ReadOnly version As Version
        Public Shared ReadOnly instance As NetworkServer

        Shared Sub New()
            version = New Version(1, 0, 0, 0)
            instance = New NetworkServer()
        End Sub

        Public Sub New(Optional port As Integer = 8000)
            log = New LogOutput($"Network Server ({port})")
            log.Setup()

            Me.port = port

            connections = New LockedDictionary(Of Integer, NetworkConnection)()
            requestFeatures = New LockedList(Of Type)()
        End Sub

        Public Sub Start()
            If isRunning Then [Stop]()

            log.Info($"Starting the server ..")

            server = New Telepathy.Server(Integer.MaxValue - 10)
            server.NoDelay = isNoDelay

            server.OnConnected = AddressOf OnClientConnected
            server.OnDisconnected = AddressOf OnClientDisconnected
            server.OnData = AddressOf OnClientData

            CodeUtils.WhileTrue(Function() isRunning, Sub() server.Tick(100), 100)

            server.Start(port)

            isRunning = True

            OnStartedEvent.Call()

            log.Info($"Server started.")
        End Sub

        Public Sub [Stop]()
            If Not isRunning Then Throw New InvalidOperationException($"The server is not running!")

            log.Info($"Stopping the server ..")

            server.Stop()

            server.OnConnected = Nothing
            server.OnDisconnected = Nothing
            server.OnData = Nothing

            server.NoDelay = False

            server = Nothing

            isRunning = False

            OnStoppedEvent.Call()

            log.Info($"Server stopped.")
        End Sub

        Public Sub Add(Of T As NetworkFeature)()
            If requestFeatures.Contains(GetType(T)) Then Return

            requestFeatures.Add(GetType(T))

            log.Trace($"Added feature: {GetType(T).FullName}")
        End Sub

        Public Sub Remove(Of T As NetworkFeature)()
            If requestFeatures.Remove(GetType(T)) Then log.Trace($"Removed feature: {GetType(T).FullName}")
        End Sub

        Private Sub OnClientData(connId As Integer, data As ArraySegment(Of Byte))
            Dim connection As NetworkConnection = Nothing
            If Not connections.TryGetValue(connId, connection) Then Return

            log.Trace($"Received client data connId={connId}")

            Dim array = data.ToArray()

            connection.Receive(array)

            OnDataEvent.[Call](connection, array)
        End Sub

        Private Sub OnClientConnected(connId As Integer)
            Dim connection = New NetworkConnection(connId, Me)

            connections(connId) = connection

            OnConnectedEvent.[Call](connection)

            log.Info($"Client connected from {connection.remote} connId={connId}")
        End Sub

        Private Sub OnClientDisconnected(connId As Integer)
            Dim connection As NetworkConnection = Nothing
            If Not connections.TryGetValue(connId, connection) Then Return

            OnDisconnectedEvent.[Call](connection)

            log.Info($"Client disconnected from {connection.remote} connId={connId}")

            connection.Stop()

            connections.Remove(connId)
        End Sub
    End Class
End Namespace

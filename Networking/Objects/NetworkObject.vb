Imports Common.Extensions
Imports Common.IO.Collections

Imports Networking.Features

Imports System
Imports System.Threading

Namespace Networking.Objects
    Public Class NetworkObject
        Private netFields As LockedDictionary(Of UShort, NetworkVariable)
        Private netTimer As Timer
        Private thisType As Type

        Public isDestroyed As Boolean
        Public isReady As Boolean

        Public ReadOnly id As Integer

        Public ReadOnly manager As NetworkManager
        Public ReadOnly net As NetworkFunctions

        Public Sub New(id As Integer, manager As NetworkManager)
            Me.id = id
            Me.manager = manager
            net = manager.net
            thisType = [GetType]()
        End Sub

        Public Overridable Sub OnStart()
        End Sub
        Public Overridable Sub OnStop()
        End Sub

        Public Sub Destroy()
            manager.Destroy(Me)
        End Sub

        Public Sub SendRpc(functionHash As UShort, ParamArray args As Object())
            net.Send(New NetworkRpcMessage(id, functionHash, args))
        End Sub

        Public Sub SendCmd(functionHash As UShort, ParamArray args As Object())
            net.Send(New NetworkCmdMessage(id, functionHash, args))
        End Sub

        Friend Sub StartInternal()
            For Each fieldPair In manager.netFields
                If fieldPair.Value.DeclaringType IsNot thisType Then Continue For

                Dim fieldNetVar = TryCast(fieldPair.Value.FieldType.Construct(), NetworkVariable)

                fieldNetVar.parent = Me

                fieldPair.Value.SetValueFast(Me, fieldNetVar)

                netFields(fieldPair.Key) = fieldNetVar
            Next

            netTimer = New Timer(Sub(__) UpdateNetFields(), Nothing, 100, 100)
        End Sub

        Friend Sub StopInternal()
            netTimer?.Dispose()
            netTimer = Nothing

            netFields?.Clear()
            netFields = Nothing
        End Sub

        Friend Sub ProcessVarSync(syncMsg As NetworkVariableSyncMessage)
            Dim field As FieldInfo = Nothing, netVar As NetworkVariable = Nothing
            If Not manager.netFields.TryGetValue(syncMsg.hash, field) OrElse field.DeclaringType IsNot thisType OrElse Not netFields.TryGetValue(syncMsg.hash, netVar) Then Return

            netVar.Process(syncMsg.msg)
        End Sub

        Private Sub UpdateNetFields()
            For Each netField In netFields
                While netField.Value.pending.Count > 0
                    Dim msg = netField.Value.pending(0)

                    netField.Value.pending.RemoveAt(0)

                    net.Send(New NetworkVariableSyncMessage(id, netField.Key, msg))
                End While
            Next
        End Sub
    End Class
End Namespace

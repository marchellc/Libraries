Imports Common.Extensions
Imports Common.Logging

Imports Networking.Data

Imports System
Imports System.Collections.Generic

Namespace Networking.Features
    Public Class NetworkFeature
        Private listeners As Dictionary(Of Type, Action(Of Object)) = New Dictionary(Of Type, Action(Of Object))()

        Public isRunning As Boolean

        Public log As LogOutput
        Public net As NetworkFunctions

        Public Overridable Function Receive(reader As Reader) As Boolean
            Return False
        End Function

        Public Overridable Sub Start()
        End Sub
        Public Overridable Sub [Stop]()
        End Sub
        Public Overridable Sub SetupLog(log As LogOutput)
        End Sub

        Public Sub Listen(Of TMessage)(listener As Action(Of TMessage))
            CSharpImpl.__Assign(listeners(GetType(TMessage)), Sub(msg) listener.Call(msg))
        End Sub

        Public Sub Remove(Of TMessage)()
            listeners.Remove(GetType(TMessage))
        End Sub

        Friend Sub InternalStart()
            If isRunning Then Throw New InvalidOperationException($"This feature is already running")

            isRunning = True

            log = New LogOutput([GetType]().Name.SpaceByUpperCase())

            SetupLog(log)

            Start()
        End Sub

        Friend Sub InternalStop()
            If Not isRunning Then Throw New InvalidOperationException($"This feature is not running")

            isRunning = False

            [Stop]()

            log.Dispose()
            log = Nothing

            listeners.Clear()
        End Sub

        Friend Function HasListener(type As Type) As Boolean
            Return listeners.ContainsKey(type)
        End Function

        Friend Sub Receive(msg As Object)
            Dim msgType = msg.GetType()

            Dim listener As Action(Of Object) = Nothing
            If Not listeners.TryGetValue(msgType, listener) Then Return

            listener.[Call](msg)
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

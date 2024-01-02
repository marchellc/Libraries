Imports Common.Extensions

Imports Networking.Data

Imports System

Namespace Networking.Features
    Public Class NetworkFunctions
        Private getWriterField As Func(Of Writer)
        Private getReaderField As Func(Of Byte(), Reader)
        Private sendWriter As Action(Of Writer)

        Public isClient As Boolean
        Public isServer As Boolean



        Public Sub New(getWriter As Func(Of Writer), getReader As Func(Of Byte(), Reader), sendWriter As Action(Of Writer), isClient As Boolean)

            If getWriter Is Nothing Then Throw New ArgumentNullException(NameOf(getWriter))

            If getReader Is Nothing Then Throw New ArgumentNullException(NameOf(getReader))

            If sendWriter Is Nothing Then Throw New ArgumentNullException(NameOf(sendWriter))

            getWriterField = getWriter
            getReaderField = getReader
            Me.sendWriter = sendWriter
            Me.isClient = isClient
            isServer = Not isClient
        End Sub

        Public Function GetWriter() As Writer
            Return getWriterField()
        End Function

        Public Function GetReader(data As Byte()) As Reader
            Return getReaderField(data)
        End Function

        Public Sub Send(writer As Writer)
            sendWriter(writer)
        End Sub

        Public Sub Send(writer As Action(Of Writer))
            Dim net = GetWriter()

            If net Is Nothing Then Return

            writer.[Call](net)

            Send(net)
        End Sub

        Public Sub Send(ParamArray messages As Object())
            Send(Sub(writer) writer.WriteAnonymousArray(messages))
        End Sub
    End Class
End Namespace

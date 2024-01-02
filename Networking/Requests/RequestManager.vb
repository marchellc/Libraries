Imports Common.IO.Collections
Imports Common.Utilities

Imports Networking.Objects

Imports System
Imports System.Linq
Imports System.Reflection
Imports System.Threading

Namespace Networking.Requests
    Public Class RequestManager
        Inherits NetworkObject
        Private waitingRequests As LockedDictionary(Of String, Tuple(Of RequestInfo, Tuple(Of MethodInfo, Object)))
        Private requestHandlers As LockedDictionary(Of Type, Tuple(Of MethodInfo, Object))
        Private timer As Timer

        Private CmdGetResponseHash As UShort
        Private CmdSetResponseHash As UShort

        Private RpcGetResponseHash As UShort
        Private RpcSetResponseHash As UShort

        Public Sub New(id As Integer, manager As NetworkManager)
            MyBase.New(id, manager)
        End Sub

        Public Overrides Sub OnStart()
            waitingRequests = New LockedDictionary(Of String, Tuple(Of RequestInfo, Tuple(Of MethodInfo, Object)))()
            timer = New Timer(Sub(__) Update(), Nothing, 100, 250)
        End Sub

        Public Overrides Sub OnStop()
            waitingRequests.Clear()
            waitingRequests = Nothing

            timer?.Dispose()
            timer = Nothing
        End Sub

        Public Sub Request(Of T)(pRequest As Object, responseHandler As Action(Of ResponseInfo, T))
            Dim requestId = Generator.Instance.GetString(10, True)
            Dim requestInfo = New RequestInfo With {
    .id = requestId,

    .isResponded = False,
    .isTimedOut = False,

    .manager = Me,

    .value = pRequest,

    .response = Nothing
}

            waitingRequests(requestId) = New Tuple(Of RequestInfo, Tuple(Of MethodInfo, Object))(requestInfo, New Tuple(Of MethodInfo, Object)(responseHandler.GetMethodInfo(), responseHandler.Target))

            If net.isServer Then
                CallRpcGetResponse(requestInfo)
            Else
                CallCmdGetResponse(requestInfo)
            End If
        End Sub

        Public Sub Respond(request As RequestInfo, response As Object, isSuccess As Boolean)
            Dim responseInfo = New ResponseInfo With {
    .id = request.id,
    .isSuccess = isSuccess,
    .manager = Me,
    .request = request,
    .response = response
}

            request.isResponded = True
            request.isTimedOut = False

            If net.isServer Then
                CallRpcSetResponse(request, responseInfo)
            Else
                CallCmdSetResponse(request, responseInfo)
            End If
        End Sub

        Public Sub CallCmdGetResponse(request As RequestInfo)
            SendCmd(CmdGetResponseHash, request)
        End Sub

        Public Sub CallCmdSetResponse(request As RequestInfo, response As ResponseInfo)
            SendCmd(CmdSetResponseHash, request, response)
        End Sub

        Public Sub CallRpcGetResponse(request As RequestInfo)
            SendRpc(RpcGetResponseHash, request)
        End Sub

        Public Sub CallRpcSetResponse(request As RequestInfo, response As ResponseInfo)
            SendRpc(RpcSetResponseHash, request, response)
        End Sub

        Public Sub RpcSetResponse(request As RequestInfo, response As ResponseInfo)
            Dim handler As Tuple(Of RequestInfo, Tuple(Of MethodInfo, Object)) = Nothing
            If Not waitingRequests.TryGetValue(request.id, handler) Then Return

            handler.Item1.isResponded = True
            handler.Item1.isTimedOut = False
            handler.Item1.manager = Me

            handler.Item1.receivedAt = Date.Now

                        ''' Cannot convert InvocationExpressionSyntax, System.InvalidCastException: Unable to cast object of type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.EmptyStatementSyntax' to type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentListSyntax'.
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitInvocationExpression(InvocationExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingVisitorWrapper`1.Accept(SyntaxNode csNode, Boolean addSourceMapping)
''' 
''' Input:
''' 
'''             handler.Item2.Item1.Call(handler.Item2.Item1, [response, response.response])
''' 
        End Sub

        Public Sub RpcGetResponse(request As RequestInfo)
            Dim handler As Tuple(Of MethodInfo, Object) = Nothing
            If request.value Is Nothing OrElse Not requestHandlers.TryGetValue(request.value.GetType(), handler) Then Return

            request.manager = Me
            request.isResponded = False
            request.isTimedOut = False
            request.receivedAt = Date.Now

                        ''' Cannot convert InvocationExpressionSyntax, System.InvalidCastException: Unable to cast object of type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.EmptyStatementSyntax' to type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentListSyntax'.
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitInvocationExpression(InvocationExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingVisitorWrapper`1.Accept(SyntaxNode csNode, Boolean addSourceMapping)
''' 
''' Input:
''' 
'''             handler.Item1.Call(handler.Item2, [request, request.value])
''' 
        End Sub

        Public Sub CmdSetResponse(request As RequestInfo, response As ResponseInfo)
            Dim handler As Tuple(Of RequestInfo, Tuple(Of MethodInfo, Object)) = Nothing
            If Not waitingRequests.TryGetValue(request.id, handler) Then Return

            handler.Item1.isResponded = True
            handler.Item1.isTimedOut = False
            handler.Item1.manager = Me

            handler.Item1.receivedAt = Date.Now

                        ''' Cannot convert InvocationExpressionSyntax, System.InvalidCastException: Unable to cast object of type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.EmptyStatementSyntax' to type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentListSyntax'.
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitInvocationExpression(InvocationExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingVisitorWrapper`1.Accept(SyntaxNode csNode, Boolean addSourceMapping)
''' 
''' Input:
''' 
'''             handler.Item2.Item1.Call(handler.Item2.Item1, [response, response.response])
''' 
        End Sub

        Public Sub CmdGetResponse(request As RequestInfo)
            Dim handler As Tuple(Of MethodInfo, Object) = Nothing
            If request.value Is Nothing OrElse Not requestHandlers.TryGetValue(request.value.GetType(), handler) Then Return

            request.manager = Me
            request.isTimedOut = False
            request.isResponded = False
            request.receivedAt = Date.Now

                        ''' Cannot convert InvocationExpressionSyntax, System.InvalidCastException: Unable to cast object of type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.EmptyStatementSyntax' to type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentListSyntax'.
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitInvocationExpression(InvocationExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingVisitorWrapper`1.Accept(SyntaxNode csNode, Boolean addSourceMapping)
''' 
''' Input:
''' 
'''             handler.Item1.Call(handler.Item2, [request, request.value])
''' 
        End Sub

        Private Sub Update()
            For Each pair In waitingRequests
                If (Date.Now - pair.Value.Item1.sentAt).TotalSeconds >= 10 Then
                    pair.Value.Item1.isTimedOut = True
                    Continue For
                End If
            Next

            Dim timedOut = waitingRequests.Where(Function(pair) pair.Value.Item1.isTimedOut)

            For Each req In timedOut
                req.Value.Item1.isResponded = False
                req.Value.Item1.isTimedOut = True
                req.Value.Item1.manager = Me
                req.Value.Item1.response = Nothing

                                ''' Cannot convert InvocationExpressionSyntax, System.InvalidCastException: Unable to cast object of type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.EmptyStatementSyntax' to type 'Microsoft.CodeAnalysis.VisualBasic.Syntax.ArgumentListSyntax'.
'''    at ICSharpCode.CodeConverter.VB.NodesVisitor.VisitInvocationExpression(InvocationExpressionSyntax node)
'''    at Microsoft.CodeAnalysis.CSharp.CSharpSyntaxVisitor`1.Visit(SyntaxNode node)
'''    at ICSharpCode.CodeConverter.VB.CommentConvertingVisitorWrapper`1.Accept(SyntaxNode csNode, Boolean addSourceMapping)
''' 
''' Input:
'''                  
'''                 req.Value.Item2.Item1.Call(req.Value.Item2.Item2, [req.Value.Item1, null])
''' 

                waitingRequests.Remove(req.Key)
            Next
        End Sub
    End Class
End Namespace

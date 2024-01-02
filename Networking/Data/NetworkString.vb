Namespace Networking.Data
    Public Structure NetworkString
        Public ReadOnly isEmpty As Boolean
        Public ReadOnly isNull As Boolean
        Public ReadOnly isNullOrEmpty As Boolean

        Public ReadOnly value As String

        Public Sub New(isEmpty As Boolean, isNull As Boolean, value As String)
            Me.isEmpty = isEmpty
            Me.isNull = isNull
            isNullOrEmpty = isEmpty OrElse isNull
            Me.value = value
        End Sub

        Public Function GetValue(Optional defaultValue As String = "") As String
            If isNullOrEmpty Then Return defaultValue

            Return value
        End Function
    End Structure
End Namespace

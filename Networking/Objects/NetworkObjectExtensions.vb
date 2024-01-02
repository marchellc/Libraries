Imports Common.Extensions

Imports System.Reflection
Imports System.Runtime.CompilerServices

Namespace Networking.Objects
    Public Module NetworkObjectExtensions
        <Extension()>
        Public Function GetPropertyHash(Me [property] As PropertyInfo) As UShort
            Return CUShort((([property].DeclaringType.Name & "+" & [property].Name).GetStableHashCode() And &HFFFF))
        End Function

        <Extension()>
        Public Function GetFieldHash(Me field As FieldInfo) As UShort
            Return CUShort(((field.DeclaringType.Name & "+" & field.Name).GetStableHashCode() And &HFFFF))
        End Function

        <Extension()>
        Public Function GetMethodHash(Me method As MethodInfo) As UShort
            Return CUShort(((method.DeclaringType.Name & "+" & method.Name).GetStableHashCode() And &HFFFF))
        End Function
    End Module
End Namespace

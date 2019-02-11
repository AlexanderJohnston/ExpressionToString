﻿Public Class Lambda
    <Fact> Public Sub NoParametersVoidReturn()
        BuildAssert(
            Sub() Console.WriteLine(),
            "() => Console.WriteLine()",
            "Sub() Console.WriteLine"
        )
    End Sub

    <Fact> Public Sub OneParameterVoidReturn()
        BuildAssert(Of String)(
            Sub(s) Console.WriteLine(s),
            "(string s) => Console.WriteLine(s)",
            "Sub(s As String) Console.WriteLine(s)"
        )
    End Sub

    <Fact(Skip:="Needs MethodCall to check for String.Concat")> Public Sub TwoParametersVoidReturn()
        BuildAssert(Of String, String)(
            Sub(s1, s2) Console.WriteLine(s1 + s2),
            "(string s1, string s2) => Console.WriteLine(s1 + s2)",
            "Sub(s1 As String, s2 As String) Console.WriteLine(s1 + s2)"
        )
    End Sub

    <Fact> Public Sub NoParametersNonVoidReturn()
        BuildAssert(
            Function() "abcd",
            "() => ""abcd""",
            "Function() ""abcd"""
        )
    End Sub

    <Fact> Public Sub OneParameterNonVoidReturn()
        BuildAssert(Of String, String)(
            Function(s) s,
            "(string s) => s",
            "Function(s As String) s"
        )
    End Sub

    <Fact(Skip:="Needs MethodCall to check for String.Concat")> Public Sub TwoParametersNonVoidReturn()
        BuildAssert(Of String, String, String)(
            Function(s1, s2) s1 + s2,
            "(string s1, string s2) => s1 + s2",
            "Function(s1 As String, s2 As String) s1 + s2"
        )
    End Sub
End Class

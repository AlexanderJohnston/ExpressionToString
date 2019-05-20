﻿Partial Public Class VBCompilerGeneratedBase
    <Fact> <Trait("Category", Member)>
    Sub InstanceMember()
        Dim s = ""
        RunTest(
            Function() s.Length,
            "() => s.Length",
            "Function() s.Length"
        )
    End Sub

    <Fact> <Trait("Category", Member)>
    Sub ClosedVariable()
        Dim s = ""
        RunTest(
            Function() s,
            "() => s",
            "Function() s"
        )
    End Sub

    <Fact> <Trait("Category", Member)>
    Sub StaticMember()
        RunTest(
            Function() String.Empty,
            "() => string.Empty",
            "Function() String.Empty"
        )
    End Sub

    <Fact(Skip:="Test for nested scope")> <Trait("Category", Member)>
    Sub NestedClosureScope()
        Assert.False(True)
    End Sub
End Class

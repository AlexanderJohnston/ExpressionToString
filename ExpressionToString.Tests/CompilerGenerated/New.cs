﻿using System;
using System.Collections.Generic;
using Xunit;
using static ExpressionToString.Tests.Globals;
using static ExpressionToString.Tests.Runners;

namespace ExpressionToString.Tests {
    class Foo {
        public string Bar { get; set; }
        public string Baz { get; set; }
        public Foo() { }
        public Foo(string baz) { }
    }

    class Wrapper : List<string> {
        public void Add(string s1, string s2) => throw new NotImplementedException();
    }

    // class used for MemberMemberBinding and ListBinding
    class Node {
        public NodeData Data { get; set; } = new NodeData();
        public IList<Node> Children { get; set; } = new List<Node>() { null };
    }

    class NodeData {
        public string Name { get; set; }
    }

    [Trait("Source", CSharpCompiler)]
    public class New {
        [Fact]
        public void NamedType() => RunTest(
            () => new Random(),
            "() => new Random()",
            "Function() New Random"
        );

        [Fact]
        public void NamedTypeWithInitializer() => RunTest(
            () => new Foo { Bar = "abcd" },
            @"() => new Foo() {
    Bar = ""abcd""
}",
            @"Function() New Foo With {
    .Bar = ""abcd""
}"
        );

        [Fact]
        public void NamedTypeWithInitializers() => RunTest(
            () => new Foo { Bar = "abcd", Baz = "efgh" },
            @"() => new Foo() {
    Bar = ""abcd"",
    Baz = ""efgh""
}",
            @"Function() New Foo With {
    .Bar = ""abcd"",
    .Baz = ""efgh""
}"
        );

        [Fact]
        public void NamedTypeConstructorParameters() => RunTest(
            () => new Foo("ijkl"),
            @"() => new Foo(""ijkl"")",
            @"Function() New Foo(""ijkl"")"
        );

        [Fact]
        public void NamedTypeConstructorParametersWithInitializers() => RunTest(
            () => new Foo("ijkl") { Bar = "abcd", Baz = "efgh" },
            @"() => new Foo(""ijkl"") {
    Bar = ""abcd"",
    Baz = ""efgh""
}",
            @"Function() New Foo(""ijkl"") With {
    .Bar = ""abcd"",
    .Baz = ""efgh""
}"
        );

        [Fact]
        public void AnonymousType() => RunTest(
            () => new { Bar = "abcd", Baz = "efgh" },
            @"() => new {
    Bar = ""abcd"",
    Baz = ""efgh""
}",
            @"Function() New With {
    .Bar = ""abcd"",
    .Baz = ""efgh""
}"
        );

        [Fact]
        public void AnonymousTypeFromVariables() {
            var Bar = "abcd";
            var Baz = "efgh";
            RunTest(
                () => new { Bar, Baz },
                @"() => new {
    Bar,
    Baz
}",
                @"Function() New With {
    Bar,
    Baz
}"
            );
        }

        [Fact]
        public void CollectionTypeWithInitializer() => RunTest(
            () => new List<string> { "abcd", "efgh" },
            @"() => new List<string>() {
    ""abcd"",
    ""efgh""
}",
            @"Function() New List(Of String) From {
    ""abcd"",
    ""efgh""
}"
        );

        [Fact]
        public void CollectionTypeWithMultipleElementsInitializers() => RunTest(
            () => new Wrapper { { "ab", "cd" }, { "ef", "gh" } },
            @"() => new Wrapper() {
    {
        ""ab"",
        ""cd""
    },
    {
        ""ef"",
        ""gh""
    }
}",
            @"Function() New Wrapper From {
    {
        ""ab"",
        ""cd""
    },
    {
        ""ef"",
        ""gh""
    }
}"
        );

        [Fact]
        public void CollectionTypeWithSingleOrMultipleElementsInitializers() => RunTest(
            () => new Wrapper { { "ab", "cd" }, "ef" },
            @"() => new Wrapper() {
    {
        ""ab"",
        ""cd""
    },
    ""ef""
}",
            @"Function() New Wrapper From {
    {
        ""ab"",
        ""cd""
    },
    ""ef""
}"
        );

        [Fact]
        public void MemberMemberBinding() => RunTest(
            () => new Node { Data = { Name = "abcd" } },
            @"() => new Node() {
    Data = {
        Name = ""abcd""
    }
}",
            @"Function() New Node With {
    .Data = With {
        .Name = ""abcd""
    }
}"
        );

        [Fact]
        public void ListBinding() => RunTest(
            () => new Node { Children = { new Node(), new Node() } },
            @"() => new Node() {
    Children = {
        new Node(),
        new Node()
    }
}",
            @"Function() New Node With {
    .Children = From {
        New Node,
        New Node
    }
}"
        );
    }
}

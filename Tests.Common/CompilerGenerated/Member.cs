﻿using Xunit;
using static ExpressionToString.Tests.Categories;

namespace ExpressionToString.Tests {
    public partial class CompilerGeneratedBase {
        [Fact]
        [Trait("Category", Member)]
        public void InstanceMember() {
            var s = "";
            RunTest(
                () => s.Length,
                "() => s.Length",
                "Function() s.Length", 
                @"Lambda(
    MakeMemberAccess(s,
        typeof(string).GetProperty(""Length"")
    )
)"
            );
        }

        [Fact]
        [Trait("Category", Member)]
        public void ClosedVariable() {
            var s = "";
            RunTest(
                () => s,
                "() => s",
                "Function() s", 
                "Lambda(s)"
            );
        }

        [Fact]
        [Trait("Category", Member)]
        public void StaticMember() => RunTest(
            () => string.Empty,
            "() => string.Empty",
            "Function() String.Empty", 
            @"Lambda(
    MakeMemberAccess(null,
        typeof(string).GetField(""Empty"")
    )
)"
        );

        [Fact(Skip ="Test for nested closure scopes")]
        [Trait("Category", Member)]
        public void NestedClosedVariable() {
            Assert.False(true);
        }
    }
}

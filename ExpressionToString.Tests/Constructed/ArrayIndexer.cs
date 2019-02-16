﻿using ExpressionToString.Util;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Xunit;
using static ExpressionToString.Tests.Runners;
using static System.Linq.Expressions.Expression;

namespace ExpressionToString.Tests.Constructed {
    public class ArrayIndexer {
        ParameterExpression arr = Parameter(typeof(string[]), "arr");
        ParameterExpression arr2D = Parameter(typeof(string[,]), "arr");
        ParameterExpression lst = Parameter(typeof(List<string>), "lst");
        PropertyInfo listIndexer = typeof(List<string>).GetIndexers(true).Single();

        [Fact]
        public void MakeArrayIndex() => BuildAssert(
            ArrayIndex(arr, Constant(0)),
            "arr[0]",
            "arr(0)"
        );

        [Fact]
        public void MakeArrayMultipleIndex() => BuildAssert(
            ArrayIndex(arr2D, Constant(0), Constant(1)),
            "arr[0, 1]",
            "arr(0, 1)"
        );

        [Fact]
        public void MakeArrayAccess() => BuildAssert(
            ArrayAccess(arr, Constant(0)),
            "arr[0]",
            "arr(0)"
        );

        [Fact]
        public void InstanceIndexer() => BuildAssert(
            MakeIndex(
                lst, listIndexer, new[] { Constant(0) as Expression }
            ),
            "lst[0]",
            "lst(0)"
        );

        [Fact]
        public void PropertyIndexer() => BuildAssert(
            Property(lst, listIndexer, Constant(0)),
            "lst[0]",
            "lst(0)"
        );
    }
}

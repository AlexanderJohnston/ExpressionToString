﻿using System;
using Xunit;
using static ExpressionToString.Tests.Globals;
using static ExpressionToString.Tests.Runners;
using static System.Linq.Expressions.Expression;

namespace ExpressionToString.Tests.Constructed {
    [Trait("Source", FactoryMethods)]
    public class MakeUnary {
        [Fact]
        public void ConstructArrayLength() => BuildAssert(ArrayLength(arr), "arr.Length", "arr.Length");

        [Fact]
        public void ConstructConvert() => BuildAssert(Convert(arr, typeof(object)), "(object)arr", "CObj(arr)");

        [Fact]
        public void ConstructConvertChecked() => BuildAssert(ConvertChecked(arr, typeof(object)), "(object)arr", "CObj(arr)");

        [Fact]
        public void ConstructNegate() => BuildAssert(Negate(i), "-i", "-i");

        [Fact]
        public void ConstructBitwiseNot() => BuildAssert(Not(i), "~i", "Not i");

        [Fact]
        public void ConstructLogicalNot() => BuildAssert(Not(b1), "!b1", "Not b1");

        [Fact]
        public void ConstructTypeAs() => BuildAssert(TypeAs(arr, typeof(object)), "arr as object", "TryCast(arr, Object)");

        [Fact]
        public void ConstructPostDecrementAssign() => BuildAssert(PostDecrementAssign(i), "i--", "(i -= 1 : i + 1)");

        [Fact]
        public void ConstructPostIncrementAssign() => BuildAssert(PostIncrementAssign(i), "i++", "(i += 1 : i - 1)");

        [Fact]
        public void ConstructPreDecrementAssign() => BuildAssert(PreDecrementAssign(i), "--i", "(i -= 1 : i)");

        [Fact]
        public void ConstructPreIncrementAssign() => BuildAssert(PreIncrementAssign(i), "++i", "(i += 1 : i)");

        [Fact]
        public void ConstructIsTrue() => BuildAssert(IsTrue(b1), "b1", "b1");

        [Fact]
        public void ConstructIsFalse() => BuildAssert(IsFalse(b1), "!b1", "Not b1");

        [Fact]
        public void ConstructIncrement() => BuildAssert(Increment(i), "i += 1", "i += 1");

        [Fact]
        public void ConstructDecrement() => BuildAssert(Decrement(i), "i -= 1", "i -= 1");

        [Fact]
        public void ConstructThrow() => BuildAssert(Throw(Constant(new Random())), "throw #Random", "Throw #Random");

        [Fact]
        public void ConstructRethrow() => BuildAssert(Rethrow(), "throw", "Throw");
    }
}

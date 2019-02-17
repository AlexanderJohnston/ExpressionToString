﻿using ExpressionToString.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using static ExpressionToString.FormatterNames;
using static ExpressionToString.Util.Functions;
using static System.Linq.Enumerable;
using static System.Linq.Expressions.ExpressionType;

namespace ExpressionToString {
    public class CSharpCodeWriter : CodeWriter {
        public CSharpCodeWriter(object o) : base(o) { }
        public CSharpCodeWriter(object o, out Dictionary<object, List<(int start, int length)>> visitedObjects) : base(o, out visitedObjects) { }

        // TODO handle order of operations -- https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/operators/

        private static readonly Dictionary<ExpressionType, string> simpleBinaryOperators = new Dictionary<ExpressionType, string>() {
            [Add] = "+",
            [AddChecked] = "+",
            [Divide] = "/",
            [Modulo] = "%",
            [Multiply] = "*",
            [MultiplyChecked] = "*",
            [Subtract] = "-",
            [SubtractChecked] = "-",
            [And] = "&",
            [Or] = "|",
            [ExclusiveOr] = "^",
            [AndAlso] = "&&",
            [OrElse] = "||",
            [Equal] = "==",
            [NotEqual] = "!=",
            [GreaterThanOrEqual] = ">=",
            [GreaterThan] = ">",
            [LessThan] = "<",
            [LessThanOrEqual] = "<=",
            [Coalesce] = "??",
            [LeftShift] = "<<",
            [RightShift] = ">>"
        };

        protected override void WriteBinary(BinaryExpression expr) {
            if (simpleBinaryOperators.TryGetValue(expr.NodeType, out var @operator)) {
                Write(expr.Left);
                $" {@operator} ".AppendTo(sb);
                Write(expr.Right);
                return;
            }

            switch (expr.NodeType) {
                case ArrayIndex:
                    Write(expr.Left);
                    "[".AppendTo(sb);
                    Write(expr.Right);
                    "]".AppendTo(sb);
                    return;
                case Power:
                    "Math.Pow(".AppendTo(sb);
                    Write(expr.Left);
                    ", ".AppendTo(sb);
                    Write(expr.Right);
                    ")".AppendTo(sb);
                    return;
            }

            throw new NotImplementedException();
        }

        protected override void WriteUnary(UnaryExpression expr) {
            switch (expr.NodeType) {
                case ArrayLength:
                    Write(expr.Operand);
                    ".Length".AppendTo(sb);
                    break;
                case ExpressionType.Convert:
                case ConvertChecked:
                    "(".AppendTo(sb);
                    expr.Type.FriendlyName(CSharp).AppendTo(sb);
                    ")".AppendTo(sb);
                    Write(expr.Operand);
                    break;
                case Negate:
                case NegateChecked:
                    "-".AppendTo(sb);
                    Write(expr.Operand);
                    break;
                case Not:
                    if (expr.Type == typeof(bool)) {
                        "!".AppendTo(sb);
                    } else {
                        "~".AppendTo(sb);
                    }
                    Write(expr.Operand);
                    break;
                case TypeAs:
                    Write(expr.Operand);
                    $" as {expr.Type.FriendlyName(CSharp)}".AppendTo(sb);
                    break;
                default:
                    throw new NotImplementedException($"NodeType: {expr.NodeType}, Expression object type: {expr.GetType().Name}");
            }
        }

        protected override void WriteLambda(LambdaExpression expr) {
            "(".AppendTo(sb);
            // we can't use WriteList here, because we have to call WriteParameterDeclaration
            expr.Parameters.ForEach((prm, index) => {
                if (index > 0) { ", ".AppendTo(sb); }
                Write(prm, true);
            });
            ") => ".AppendTo(sb);
            Write(expr.Body);
        }

        protected override void WriteParameterDeclarationImpl(ParameterExpression prm) =>
            $"{prm.Type.FriendlyName(CSharp)} {prm.Name}".AppendTo(sb);

        protected override void WriteParameter(ParameterExpression expr) => expr.Name.AppendTo(sb);

        protected override void WriteConstant(ConstantExpression expr) =>
            RenderLiteral(expr.Value, CSharp).AppendTo(sb);

        protected override void WriteMemberAccess(MemberExpression expr) {
            switch (expr.Expression) {
                case ConstantExpression cexpr when cexpr.Type.IsClosureClass():
                    // closed over variable from outer scope
                    expr.Member.Name.Replace("$VB$Local_", "").AppendTo(sb);
                    return;
                case null:
                    // static member
                    $"{expr.Member.DeclaringType.FriendlyName(CSharp)}.{expr.Member.Name}".AppendTo(sb);
                    return;
                default:
                    Write(expr.Expression);
                    $".{expr.Member.Name}".AppendTo(sb);
                    return;
            }
        }

        protected override void WriteNew(NewExpression expr) {
            "new ".AppendTo(sb);
            if (expr.Type.IsAnonymous()) {
                "{ ".AppendTo(sb);
                expr.Constructor.GetParameters().Select(x => x.Name).Zip(expr.Arguments).ForEachT((name, arg, index) => {
                    if (index > 0) { ", ".AppendTo(sb); }
                    if (!(arg is MemberExpression mexpr && mexpr.Member.Name.Replace("$VB$Local_", "") == name)) {
                        $"{name} = ".AppendTo(sb);
                    }
                    Write(arg);
                });
                " }".AppendTo(sb);
            } else {
                expr.Type.FriendlyName(CSharp).AppendTo(sb);
                "(".AppendTo(sb);
                WriteList(expr.Arguments);
                ")".AppendTo(sb);
            }
        }

        static HashSet<MethodInfo> stringConcats = typeof(string)
            .GetMethods()
            .Where(x => x.Name == "Concat" && x.GetParameters().All(
                y => y.ParameterType.In(typeof(string), typeof(string[])))
            ).ToHashSet();

        protected override void WriteCall(MethodCallExpression expr) {
            if (expr.Method.In(stringConcats)) {
                var firstArg = expr.Arguments[0];
                IEnumerable<Expression> argsToWrite = null;
                if (firstArg is NewArrayExpression newArray && firstArg.NodeType == NewArrayInit) {
                    argsToWrite = newArray.Expressions;
                } else if (expr.Arguments.All(x => x.Type == typeof(string))) {
                    argsToWrite = expr.Arguments;
                }
                if (argsToWrite != null) {
                    WriteList(argsToWrite, " + ");
                    return;
                }
            }

            bool isIndexer = false;
            if ((expr.Object?.Type.IsArray ?? false) && expr.Method.Name == "Get") {
                isIndexer = true;
            } else {
                var indexerMethods = expr.Method.ReflectedType.GetIndexers(true).SelectMany(x => new[] { x.GetMethod, x.SetMethod }).ToList();
                isIndexer = expr.Method.In(indexerMethods);
            }
            if (isIndexer) {
                Write(expr.Object);
                "[".AppendTo(sb);
                WriteList(expr.Arguments);
                "]".AppendTo(sb);
                return;
            }

            Expression instance = null;
            IEnumerable<Expression> arguments = expr.Arguments;

            if (expr.Object != null) {
                // instance method
                instance = expr.Object;
            } else if (expr.Method.HasAttribute<ExtensionAttribute>()) {
                // extension method
                instance = expr.Arguments[0];
                arguments = expr.Arguments.Skip(1);
            }

            if (instance == null) {
                expr.Method.ReflectedType.FriendlyName(CSharp).AppendTo(sb);
            } else {
                Write(instance);
            }

            $".{expr.Method.Name}(".AppendTo(sb);
            WriteList(arguments);
            ")".AppendTo(sb);
        }

        protected override void WriteBinding(MemberBinding binding) {
            switch (binding) {
                case MemberAssignment assignmentBinding:
                    binding.Member.Name.AppendTo(sb);
                    " = ".AppendTo(sb);
                    Write(assignmentBinding.Expression);
                    break;
                case MemberListBinding listBinding:
                    throw new NotImplementedException();
                case MemberMemberBinding memberBinding:
                    throw new NotImplementedException();
                default:
                    throw new NotImplementedException();
            }
        }

        protected override void WriteMemberInit(MemberInitExpression expr) {
            Write(expr.NewExpression);
            if (expr.Bindings.Any()) {
                " { ".AppendTo(sb);
                WriteList(expr.Bindings);
                " }".AppendTo(sb);
            }
        }

        protected override void WriteListInit(ListInitExpression expr) {
            Write(expr.NewExpression);
            " { ".AppendTo(sb);
            WriteList(expr.Initializers);
            " }".AppendTo(sb);
        }

        protected override void WriteElementInit(ElementInit elementInit) {
            var args = elementInit.Arguments;
            switch (args.Count) {
                case 0:
                    throw new NotImplementedException();
                case 1:
                    Write(args.First());
                    break;
                default:
                    "{ ".AppendTo(sb);
                    WriteList(args);
                    " }".AppendTo(sb);
                    break;
            }
        }

        protected override void WriteNewArray(NewArrayExpression expr) {
            switch (expr.NodeType) {
                case NewArrayInit:
                    var elementType = expr.Type.GetElementType();
                    "new ".AppendTo(sb);
                    if (elementType.IsArray || expr.Expressions.None() || expr.Expressions.Any(x => x.Type != elementType)) {
                        expr.Type.FriendlyName(CSharp).AppendTo(sb);
                    } else {
                        "[]".AppendTo(sb);
                    }
                    " { ".AppendTo(sb);
                    WriteList(expr.Expressions);
                    " }".AppendTo(sb);
                    break;
                case NewArrayBounds:
                    (string left, string right) specifierChars = ("[", "]");
                    var nestedArrayTypes = expr.Type.NestedArrayTypes().ToList();
                    $"new {nestedArrayTypes.Last().root.FriendlyName(CSharp)}".AppendTo(sb);
                    nestedArrayTypes.ForEachT((current, _, index) => {
                        specifierChars.left.AppendTo(sb);
                        if (index == 0) {
                            WriteList(expr.Expressions);
                        } else {
                            Repeat("", current.GetArrayRank()).Joined().AppendTo(sb);
                        }
                        specifierChars.right.AppendTo(sb);
                    });
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        protected override void WriteConditional(ConditionalExpression expr) {
            Write(expr.Test);
            " ? ".AppendTo(sb);
            Write(expr.IfTrue);
            " : ".AppendTo(sb);
            Write(expr.IfFalse);
        }

        protected override void WriteDefault(DefaultExpression expr) =>
            $"default({expr.Type.FriendlyName(CSharp)})".AppendTo(sb);

        protected override void WriteTypeBinary(TypeBinaryExpression expr) {
            Write(expr.Expression);
            $" is {expr.TypeOperand.FriendlyName(CSharp)}".AppendTo(sb);
        }

        protected override void WriteInvocation(InvocationExpression expr) {
            if (expr.Expression is LambdaExpression) { "(".AppendTo(sb); }
            Write(expr.Expression);
            if (expr.Expression is LambdaExpression) { ")".AppendTo(sb); }
            "(".AppendTo(sb);
            WriteList(expr.Arguments);
            ")".AppendTo(sb);
        }

        protected override void WriteIndex(IndexExpression expr) {
            Write(expr.Object);
            "[".AppendTo(sb);
            WriteList(expr.Arguments);
            "]".AppendTo(sb);
        }
    }
}

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
using static ExpressionToString.Globals;

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
                Write($" {@operator} ");
                Write(expr.Right);
                return;
            }

            switch (expr.NodeType) {
                case ArrayIndex:
                    Write(expr.Left);
                    Write("[");
                    Write(expr.Right);
                    Write("]");
                    return;
                case Power:
                    Write("Math.Pow(");
                    Write(expr.Left);
                    Write(", ");
                    Write(expr.Right);
                    Write(")");
                    return;
            }

            throw new NotImplementedException();
        }

        protected override void WriteUnary(UnaryExpression expr) {
            switch (expr.NodeType) {
                case ArrayLength:
                    Write(expr.Operand);
                    Write(".Length");
                    break;
                case ExpressionType.Convert:
                case ConvertChecked:
                    Write($"({expr.Type.FriendlyName(CSharp)})");
                    Write(expr.Operand);
                    break;
                case Negate:
                case NegateChecked:
                    Write("-");
                    Write(expr.Operand);
                    break;
                case Not:
                    if (expr.Type == typeof(bool)) {
                        Write("!");
                    } else {
                        Write("~");
                    }
                    Write(expr.Operand);
                    break;
                case TypeAs:
                    Write(expr.Operand);
                    Write($" as {expr.Type.FriendlyName(CSharp)}");
                    break;
                default:
                    throw new NotImplementedException($"NodeType: {expr.NodeType}, Expression object type: {expr.GetType().Name}");
            }
        }

        protected override void WriteLambda(LambdaExpression expr) {
            Write("(");
            // we can't use WriteList here, because we have to call WriteParameterDeclaration
            expr.Parameters.ForEach((prm, index) => {
                if (index > 0) { Write(", "); }
                Write(prm, true);
            });
            Write(") => ");
            Write(expr.Body);
        }

        protected override void WriteParameterDeclarationImpl(ParameterExpression prm) =>
            Write($"{prm.Type.FriendlyName(CSharp)} {prm.Name}");

        protected override void WriteParameter(ParameterExpression expr) => Write(expr.Name);

        protected override void WriteConstant(ConstantExpression expr) =>
            Write(RenderLiteral(expr.Value, CSharp));

        protected override void WriteMemberAccess(MemberExpression expr) {
            switch (expr.Expression) {
                case ConstantExpression cexpr when cexpr.Type.IsClosureClass():
                case MemberExpression mexpr when mexpr.Type.IsClosureClass():
                    // closed over variable from outer scope
                    Write(expr.Member.Name.Replace("$VB$Local_", ""));
                    return;
                case null:
                    // static member
                    Write($"{expr.Member.DeclaringType.FriendlyName(CSharp)}.{expr.Member.Name}");
                    return;
                default:
                    Write(expr.Expression);
                    Write($".{expr.Member.Name}");
                    return;
            }
        }

        protected override void WriteNew(NewExpression expr) {
            Write("new ");
            if (expr.Type.IsAnonymous()) {
                Write("{ ");
                expr.Constructor.GetParameters().Select(x => x.Name).Zip(expr.Arguments).ForEachT((name, arg, index) => {
                    if (index > 0) { Write(", "); }
                    if (!(arg is MemberExpression mexpr && mexpr.Member.Name.Replace("$VB$Local_", "") == name)) {
                        Write($"{name} = ");
                    }
                    Write(arg);
                });
                Write(" }");
            } else {
                Write(expr.Type.FriendlyName(CSharp));
                Write("(");
                WriteList(expr.Arguments);
                Write(")");
            }
        }

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
                Write("[");
                WriteList(expr.Arguments);
                Write("]");
                return;
            }

            if (expr.Method.In(stringFormats) && expr.Arguments[0] is ConstantExpression cexpr && cexpr.Value is string format) {
                var parts = ParseFormatString(format);
                Write("$\"");
                foreach (var (literal, index, alignment, itemFormat) in parts) {
                    Write(literal.Replace("{", "{{").Replace("}", "}}"));
                    if (index == null) { break; }
                    Write("{");
                    Write(expr.Arguments[index.Value + 1]);
                    if (alignment != null) { Write($", {alignment}"); }
                    if (itemFormat != null) { Write($":{itemFormat}"); }
                    Write("}");
                }
                Write("\"");
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
                Write(expr.Method.ReflectedType.FriendlyName(CSharp));
            } else {
                Write(instance);
            }

            Write($".{expr.Method.Name}(");
            WriteList(arguments);
            Write(")");
        }

        protected override void WriteBinding(MemberBinding binding) {
            switch (binding) {
                case MemberAssignment assignmentBinding:
                    Write(binding.Member.Name);
                    Write(" = ");
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
                Write(" { ");
                WriteList(expr.Bindings);
                Write(" }");
            }
        }

        protected override void WriteListInit(ListInitExpression expr) {
            Write(expr.NewExpression);
            Write(" { ");
            WriteList(expr.Initializers);
            Write(" }");
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
                    Write("{ ");
                    WriteList(args);
                    Write(" }");
                    break;
            }
        }

        protected override void WriteNewArray(NewArrayExpression expr) {
            switch (expr.NodeType) {
                case NewArrayInit:
                    var elementType = expr.Type.GetElementType();
                    Write("new ");
                    if (elementType.IsArray || expr.Expressions.None() || expr.Expressions.Any(x => x.Type != elementType)) {
                        Write(expr.Type.FriendlyName(CSharp));
                    } else {
                        Write("[]");
                    }
                    Write(" { ");
                    WriteList(expr.Expressions);
                    Write(" }");
                    break;
                case NewArrayBounds:
                    (string left, string right) specifierChars = ("[", "]");
                    var nestedArrayTypes = expr.Type.NestedArrayTypes().ToList();
                    Write($"new {nestedArrayTypes.Last().root.FriendlyName(CSharp)}");
                    nestedArrayTypes.ForEachT((current, _, index) => {
                        Write(specifierChars.left);
                        if (index == 0) {
                            WriteList(expr.Expressions);
                        } else {
                            Write(Repeat("", current.GetArrayRank()).Joined());
                        }
                        Write(specifierChars.right);
                    });
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        protected override void WriteConditional(ConditionalExpression expr) {
            if (expr.Type == typeof(void)) { // if block, or if..else block
                Write("if (");
                Write(expr.Test); // what happens if this is a BlockExpression?
                Write(")");
                WriteBlock(expr.IfTrue);
                if (expr.IfFalse != null) {
                    Write("else ");
                    WriteBlock(expr.IfFalse);
                }
            } else {
                Write(expr.Test);
                Write(" ? ");
                Write(expr.IfTrue);
                Write(" : ");
                Write(expr.IfFalse);
            }
        }

        protected override void WriteDefault(DefaultExpression expr) =>
            Write($"default({expr.Type.FriendlyName(CSharp)})");

        protected override void WriteTypeBinary(TypeBinaryExpression expr) {
            Write(expr.Expression);
            var typeName = expr.TypeOperand.FriendlyName(CSharp);
            switch (expr.NodeType) {
                case TypeIs:
                    Write($" is {typeName}");
                    break;
                case TypeEqual:
                    Write($".GetType() == typeof({typeName})");
                    break;
            }
        }

        protected override void WriteInvocation(InvocationExpression expr) {
            if (expr.Expression is LambdaExpression) { Write("("); }
            Write(expr.Expression);
            if (expr.Expression is LambdaExpression) { Write(")"); }
            Write("(");
            WriteList(expr.Arguments);
            Write(")");
        }

        protected override void WriteIndex(IndexExpression expr) {
            Write(expr.Object);
            Write("[");
            WriteList(expr.Arguments);
            Write("]");
        }

        protected override void WriteBlock(BlockExpression expr) {
            Write("{");
            Indent();
            foreach (var subexpr in expr.Expressions) {
                WriteEOL();
                Write(subexpr);
            }
            // we can ignore expr.Result, because it is the same as expr.Expressions.Last()
            WriteEOL(true);
            Write("}");
        }

        private void WriteBlock(Expression expr) {
            if (expr is BlockExpression bexpr) {
                WriteBlock(bexpr);
                return;
            }
            Indent();
            WriteEOL();
            Write(expr);
            WriteEOL(true);
        }
    }
}
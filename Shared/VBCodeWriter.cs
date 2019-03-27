﻿using ExpressionToString.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using static ExpressionToString.FormatterNames;
using static ExpressionToString.Globals;
using static ExpressionToString.Util.Functions;
using static System.Linq.Enumerable;
using static System.Linq.Expressions.ExpressionType;

namespace ExpressionToString {
    public class VBCodeWriter : CodeWriter {
        public VBCodeWriter(object o) : base(o) { }
        public VBCodeWriter(object o, out Dictionary<object, List<(int start, int length)>> visitedObjects) : base(o, out visitedObjects) { }

        private static readonly Dictionary<ExpressionType, string> simpleBinaryOperators = new Dictionary<ExpressionType, string>() {
            [Add] = "+",
            [AddChecked] = "+",
            [Divide] = "/",
            [Modulo] = "Mod",
            [Multiply] = "*",
            [MultiplyChecked] = "*",
            [Subtract] = "-",
            [SubtractChecked] = "-",
            [And] = "And",
            [Or] = "Or",
            [ExclusiveOr] = "Xor",
            [AndAlso] = "AndAlso",
            [OrElse] = "OrElse",
            [GreaterThanOrEqual] = ">=",
            [GreaterThan] = ">",
            [LessThan] = "<",
            [LessThanOrEqual] = "<=",
            [LeftShift] = "<<",
            [RightShift] = ">>",
            [Power] = "^",
            [Assign] = "=",
            [AddAssign] = "+=",
            [AddAssignChecked] = "+=",
            [DivideAssign] = "/=",
            [LeftShiftAssign] = "<<=",
            [MultiplyAssign] = "*=",
            [MultiplyAssignChecked] = "*=",
            [PowerAssign] = "^=",
            [RightShiftAssign] = ">>=",
            [SubtractAssign] = "-=",
            [SubtractAssignChecked] = "-="
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
                    Write("(");
                    Write(expr.Right);
                    Write(")");
                    return;
                case Coalesce:
                    Write("If(");
                    Write(expr.Left);
                    Write(", ");
                    Write(expr.Right);
                    Write(")");
                    return;
                case OrAssign:
                case AndAssign:
                case ExclusiveOrAssign:
                case ModuloAssign:
                    var op = (ExpressionType)Enum.Parse(typeof(ExpressionType), expr.NodeType.ToString().Replace("Assign", ""));
                    Write(expr.Left);
                    Write(" = ");
                    Write(expr.Left);
                    Write($" {simpleBinaryOperators[op]} ");
                    Write(expr.Right);
                    return;
                case Equal:
                    Write(expr.Left);
                    Write(expr.IsReferenceComparison() ?
                        " Is " :
                        " = "
                    );
                    Write(expr.Right);
                    return;
                case NotEqual:
                    Write(expr.Left);
                    Write(expr.IsReferenceComparison() ?
                        " IsNot " :
                        " <> "
                    );
                    Write(expr.Right);
                    return;
            }

            throw new NotImplementedException();
        }

        private static Dictionary<Type, string> conversionFunctions = new Dictionary<Type, string>() {
            {typeof(bool), "CBool"},
            {typeof(byte), "CByte"},
            {typeof(char), "CChar"},
            {typeof(DateTime), "CDate"},
            {typeof(double), "CDbl"},
            {typeof(decimal), "CDec"},
            {typeof(int), "CInt"},
            {typeof(long), "CLng"},
            {typeof(object), "CObj"},
            {typeof(sbyte), "CSByte"},
            {typeof(short), "CShort"},
            {typeof(float), "CSng"},
            {typeof(string), "CStr"},
            {typeof(uint), "CUInt"},
            {typeof(ulong), "CULng"},
            {typeof(ushort), "CUShort" }
        };

        protected override void WriteUnary(UnaryExpression expr) {
            switch (expr.NodeType) {
                case ArrayLength:
                    Write(expr.Operand);
                    Write(".Length");
                    break;
                case ExpressionType.Convert:
                case ConvertChecked:
                    if (conversionFunctions.TryGetValue(expr.Type, out var conversionFunction)) {
                        Write(conversionFunction);
                        Write("(");
                        Write(expr.Operand);
                        Write(")");
                    } else {
                        Write("CType(");
                        Write(expr.Operand);
                        Write($", {expr.Type.FriendlyName(VisualBasic)})");
                    }
                    break;
                case Negate:
                case NegateChecked:
                    Write("-");
                    Write(expr.Operand);
                    break;
                case Not:
                    Write("Not ");
                    Write(expr.Operand);
                    break;
                case TypeAs:
                    Write("TryCast(");
                    Write(expr.Operand);
                    Write($", {expr.Type.FriendlyName(VisualBasic)})");
                    break;

                case PreIncrementAssign:
                    Write("(");
                    Write(expr.Operand);
                    Write(" += 1 : ");
                    Write(expr.Operand);
                    Write(")");
                    return;
                case PostIncrementAssign:
                    Write("(");
                    Write(expr.Operand);
                    Write(" += 1 : ");
                    Write(expr.Operand);
                    Write(" - 1)");
                    return;
                case PreDecrementAssign:
                    Write("(");
                    Write(expr.Operand);
                    Write(" -= 1 : ");
                    Write(expr.Operand);
                    Write(")");
                    return;
                case PostDecrementAssign:
                    Write("(");
                    Write(expr.Operand);
                    Write(" -= 1 : ");
                    Write(expr.Operand);
                    Write(" + 1)");
                    return;

                case IsTrue:
                    Write(expr.Operand);
                    break;
                case IsFalse:
                    Write("Not ");
                    Write(expr.Operand);
                    break;

                case Increment:
                    Write(expr.Operand);
                    Write(" += 1");
                    break;
                case Decrement:
                    Write(expr.Operand);
                    Write(" -= 1");
                    break;

                default:
                    throw new NotImplementedException($"NodeType: {expr.NodeType}, Expression object type: {expr.GetType().Name}");
            }
        }

        protected override void WriteLambda(LambdaExpression expr) {
            if (expr.ReturnType == typeof(void)) {
                Write("Sub");
            } else {
                Write("Function");
            }
            Write("(");
            expr.Parameters.ForEach((prm, index) => {
                if (index > 0) { Write(", "); }
                Write(prm, true);
            });
            Write(") ");
            Write(expr.Body);
        }

        protected override void WriteParameterDeclarationImpl(ParameterExpression prm) =>
            Write($"{prm.Name} As {prm.Type.FriendlyName(VisualBasic)}");

        protected override void WriteParameter(ParameterExpression expr) => Write(expr.Name);

        protected override void WriteConstant(ConstantExpression expr) =>
            Write(RenderLiteral(expr.Value, VisualBasic));

        protected override void WriteMemberAccess(MemberExpression expr) {
            switch (expr.Expression) {
                case ConstantExpression cexpr when cexpr.Type.IsClosureClass():
                case MemberExpression mexpr when mexpr.Type.IsClosureClass():
                    // closed over variable from outer scope
                    Write(expr.Member.Name.Replace("$VB$Local_", ""));
                    return;
                case null:
                    // static member
                    Write($"{expr.Member.DeclaringType.FriendlyName(VisualBasic)}.{expr.Member.Name}");
                    return;
                default:
                    Write(expr.Expression);
                    Write($".{expr.Member.Name}");
                    return;
            }
        }

        protected override void WriteNew(NewExpression expr) {
            Write("New ");
            if (expr.Type.IsAnonymous()) {
                Write("With {");
                expr.Constructor.GetParameters().Select(x => x.Name).Zip(expr.Arguments).ForEachT((name, arg, index) => {
                    if (index > 0) { Write(", "); }
                    if (!(arg is MemberExpression mexpr && mexpr.Member.Name.Replace("$VB$Local_", "") == name)) {
                        Write($".{name} = ");
                    }
                    Write(arg);
                });
                Write("}");
            } else {
                Write(expr.Type.FriendlyName(VisualBasic));
                if (expr.Arguments.Any()) {
                    Write("(");
                    WriteList(expr.Arguments);
                    Write(")");
                }
            }
        }

        static readonly MethodInfo power = typeof(Math).GetMethod("Pow");

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
                Write("(");
                WriteList(expr.Arguments);
                Write(")");
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

            if (expr.Method == power) {
                Write(expr.Arguments[0]);
                Write(" ^ ");
                Write(expr.Arguments[1]);
                return;
            }

            // Microsoft.VisualBasic.CompilerServices is not available to .NET Standard
            if (expr.Method.DeclaringType.FullName == "Microsoft.VisualBasic.CompilerServices.LikeOperator") {
                Write(expr.Arguments[0]);
                Write(" Like ");
                Write(expr.Arguments[1]);
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
                Write(expr.Method.ReflectedType.FriendlyName(VisualBasic));
            } else {
                Write(instance);
            }

            Write($".{expr.Method.Name}");
            if (arguments.Any()) {
                Write("(");
                WriteList(arguments);
                Write(")");
            }
        }

        protected override void WriteBinding(MemberBinding binding) {
            switch (binding) {
                case MemberAssignment assignmentBinding:
                    Write(".");
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
                Write(" With {");
                WriteList(expr.Bindings);
                Write("}");
            }
        }

        protected override void WriteListInit(ListInitExpression expr) {
            Write(expr.NewExpression);
            Write(" From {");
            expr.Initializers.ForEach((init, index) => {
                if (index > 0) { Write(", "); }
                Write(init);
            });
            Write("}");
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
                    Write("{");
                    WriteList(args);
                    Write("}");
                    break;
            }
        }

        protected override void WriteNewArray(NewArrayExpression expr) {
            switch (expr.NodeType) {
                case NewArrayInit:
                    var elementType = expr.Type.GetElementType();
                    if (expr.Expressions.None() || expr.Expressions.Any(x => x.Type != elementType)) {
                        Write($"New {expr.Type.FriendlyName(VisualBasic)} ");
                    }
                    Write("{ ");
                    expr.Expressions.ForEach((arg, index) => {
                        if (index > 0) { Write(", "); }
                        if (arg.NodeType == NewArrayInit) { Write("("); }
                        Write(arg);
                        if (arg.NodeType == NewArrayInit) { Write(")"); }
                    });
                    Write(" }");
                    break;
                case NewArrayBounds:
                    (string left, string right) specifierChars = ("(", ")");
                    var nestedArrayTypes = expr.Type.NestedArrayTypes().ToList();
                    Write($"New {nestedArrayTypes.Last().root.FriendlyName(VisualBasic)}");
                    nestedArrayTypes.ForEachT((current, _, arrayTypeIndex) => {
                        Write(specifierChars.left);
                        if (arrayTypeIndex == 0) {
                            expr.Expressions.ForEach((x, index) => {
                                if (index > 0) { Write(", "); }
                                // because in VB.NET the upper bound of an array is specified, not the numbe of items
                                if (x is ConstantExpression cexpr) {
                                    string newValue = (((dynamic)cexpr.Value) - 1).ToString();
                                    Write(newValue);
                                } else {
                                    Write(Expression.SubtractChecked(x, Expression.Constant(1)));
                                }
                            });
                        } else {
                            Write(Repeat("", current.GetArrayRank()).Joined());
                        }
                        Write(specifierChars.right);
                    });
                    Write(" {}");
                    break;
                default:
                    throw new NotImplementedException();
            }
        }

        protected override void WriteConditional(ConditionalExpression expr) {
            if (expr.Type == typeof(void)) {
                Write("If");
                if (IsBlockSyntax(expr.Test)) {
                    Indent();
                    WriteEOL();
                    Write(expr.Test);
                    WriteEOL(true);
                } else {
                    Write(" ");
                    Write(expr.Test);
                    Write(" ");
                }
                Write("Then");
                Indent();
                WriteEOL();
                Write(expr.IfTrue);
                Dedent();
                WriteEOL();
                if (!(expr.IfFalse is DefaultExpression) && expr.Type == typeof(void)) {
                    Write("Else"); // TODO handle ElseIf -- ifFalse is a ConditionalExpression with typeof(void)
                    Indent();
                    WriteEOL();
                    Write(expr.IfFalse);
                    Dedent();
                    WriteEOL();
                }
                Write("End If");
            } else {
                Write("If(");
                if (IsBlockSyntax(expr.Test)) {
                    Write("(");
                    Indent();
                    WriteEOL();
                }
                Write(expr.Test);
                if (IsBlockSyntax(expr.Test)) {
                    WriteEOL(true);
                    Write(")");
                }
                Write(", ");
                Write(expr.IfTrue);
                Write(", ");
                Write(expr.IfFalse);
                Write(")");
            }
        }

        protected override void WriteDefault(DefaultExpression expr) =>
            Write($"CType(Nothing, {expr.Type.FriendlyName(VisualBasic)})");

        protected override void WriteTypeBinary(TypeBinaryExpression expr) {
            switch (expr.NodeType) {
                case TypeIs:
                    Write("TypeOf ");
                    Write(expr.Expression);
                    Write($" Is {expr.TypeOperand.FriendlyName(VisualBasic)}");
                    break;
                case TypeEqual:
                    Write(expr.Expression);
                    Write($".GetType = GetType({expr.TypeOperand.FriendlyName(VisualBasic)})");
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
            Write("(");
            WriteList(expr.Arguments);
            Write(")");
        }

        protected override void WriteBlock(BlockExpression expr) {
            var explicitBlock = expr.Variables.Any();
            if (explicitBlock) {
                Write("Block");
                Indent();
                WriteEOL();
                expr.Variables.ForEach((v, index) => {
                    if (index > 0) { WriteEOL(); }
                    Write("Dim ");
                    Write(v, true);
                });
            }
            expr.Expressions.ForEach((subexpr, index) => {
                if (index > 0 || expr.Variables.Any()) { WriteEOL(); }
                Write(subexpr);
            });
            if (explicitBlock) {
                WriteEOL(true);
                Write("End Block");
            }
        }

        private void WriteStatement(Expression expr) {
            WriteEOL();
            Write(expr);
        }

        private bool IsBlockSyntax(Expression expr) {
            switch (expr) {
                case ConditionalExpression cexpr:
                    return cexpr.Type == typeof(void);
                case BlockExpression bexpr:
                    return true;
            }
            return false;
        }

        protected override void WriteSwitch(SwitchExpression expr) => throw new NotImplementedException();

        protected override void WriteSwitchCase(SwitchCase switchCase) {
            Write("Case ");
            WriteList(switchCase.TestValues);
            Indent();
            
            WriteEOL();
        }
    }
}

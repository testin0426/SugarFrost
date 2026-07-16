using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using JetBrains.Annotations;
using Naninovel.Expression;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Allows parsing and evaluating script expressions.
    /// </summary>
    public static class ExpressionEvaluator
    {
        /// <summary>
        /// Context of a script expression evaluation process.
        /// </summary>
        public class EvaluationContext
        {
            /// <summary>
            /// The expression being evaluated.
            /// </summary>
            public IExpression Expression { get; }
            /// <summary>
            /// Information about the parameter which value contains the expression being evaluated.
            /// Null when the expression is not associated with a parameter.
            /// </summary>
            [CanBeNull] public ParameterContext Parameter { get; }

            public EvaluationContext (IExpression expression, ParameterContext parameter = null)
            {
                Expression = expression;
                Parameter = parameter;
            }
        }

        /// <summary>
        /// Information about the parameter which value contains the expression being evaluated.
        /// </summary>
        public class ParameterContext
        {
            /// <summary>
            /// Command parameter containing the expression being evaluated.
            /// </summary>
            public ICommandParameter Parameter { get; }
            /// <summary>
            /// Raw parameter value index containing the expression being evaluated.
            /// </summary>
            public int PartIndex { get; }

            public ParameterContext (ICommandParameter parameter, int partIndex)
            {
                Parameter = parameter;
                PartIndex = partIndex;
            }
        }

        /// <summary>
        /// Optional preferences of the expression evaluation.
        /// </summary>
        public struct EvaluateOptions
        {
            /// <summary>
            /// Invoked on error when parsing or evaluating the expression.
            /// </summary>
            [CanBeNull] public Action<string> OnError;
            /// <summary>
            /// Associates the expression evaluation with a command parameter. 
            /// </summary>
            [CanBeNull] public ParameterContext Parameter;
        }

        /// <summary>
        /// Context associated with the current expression evaluation process, if any.
        /// </summary>
        /// <remarks>
        /// This is an ambient property provided by the <see cref="ExpressionEvaluator"/> while
        /// the evaluation is performed. Null when accessed outside of the evaluation process.
        /// </remarks>
        [CanBeNull] public static EvaluationContext Context => ctx.Value;

        private static readonly List<ParseDiagnostic> errors = new();
        private static readonly AsyncLocal<EvaluationContext> ctx = new();
        private static readonly ILookup<string, ExpressionFunction> fnByName;
        private static readonly Parser parser;
        private static readonly Evaluator evaluator;

        static ExpressionEvaluator ()
        {
            parser = new(new() {
                Symbols = Compiler.Symbols,
                HandleDiagnostic = errors.Add
            });
            evaluator = new(new() {
                ResolveVariable = ResolveVariable,
                ResolveFunction = ResolveFunction
            });
            fnByName = ExpressionFunctions.Resolve()
                .ToLookup(fn => fn.Id, comparer: StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Parses specified expression text and evaluates the result of the expected type.
        /// Returns default value of the result type when the operation fails.
        /// </summary>
        public static TResult Evaluate<TResult> (string text, EvaluateOptions options = default)
        {
            return Evaluate(text, options).GetValue<TResult>();
        }

        /// <summary>
        /// Attempts to parse specified expression text and evaluate the result of the expected type.
        /// Returns false when the operation fails and vice versa.
        /// </summary>
        public static bool TryEvaluate<TResult> (string text, out TResult result, EvaluateOptions options = default)
        {
            result = default;
            try { return (result = Evaluate<TResult>(text, options)) != null; }
            catch { return false; }
        }

        /// <summary>
        /// Parses specified expression text and evaluates the result.
        /// Returns null when the operation fails.
        /// </summary>
        public static IOperand Evaluate (string text, EvaluateOptions options = default)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                options.OnError?.Invoke("Expression is missing.");
                return default;
            }

            errors.Clear();

            if (!parser.TryParse(text, out var exp))
            {
                options.OnError?.Invoke($"Failed to parse '{text}' expression: {errors.FirstOrDefault()}");
                return default;
            }

            return Evaluate(exp, options);
        }

        /// <summary>
        /// Evaluates specified expression.
        /// Returns null when the operation fails.
        /// </summary>
        public static IOperand Evaluate (IExpression exp, EvaluateOptions options = default)
        {
            errors.Clear();

            try
            {
                ctx.Value = new(exp, options.Parameter);
                return evaluator.Evaluate(exp);
            }
            catch (Expression.Error err)
            {
                options.OnError?.Invoke($"Failed to evaluate expression: {err.Message}");
                return default;
            }
            finally { ctx.Value = null; }
        }

        /// <summary>
        /// Parses specified assignments expression text and adds the invidividual parsed assignments to the specified collection.
        /// </summary>
        public static void ParseAssignments (string text, IList<Assignment> assignments, Action<string> onError = null)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                onError?.Invoke("Expression is missing.");
                return;
            }

            errors.Clear();

            if (!parser.TryParseAssignments(text, assignments))
            {
                onError?.Invoke($"Failed to parse '{text}' assignment expression: {errors.FirstOrDefault()}");
                return;
            }
        }

        private static IOperand ResolveVariable (string name)
        {
            if (name.StartsWith(Compiler.ScriptConstantPrefix, StringComparison.OrdinalIgnoreCase))
            {
                var docs = Engine.GetServiceOrErr<ITextManager>();
                var managedTextValue = docs.GetRecordValue(name, ManagedTextPaths.ScriptConstants);
                if (string.IsNullOrEmpty(managedTextValue))
                {
                    Engine.Warn($"Missing '{name}' script constant. Make sure associated record exists in '{ManagedTextPaths.ScriptConstants}' managed text document.");
                    managedTextValue = $"{{{name}}}";
                }
                return new Expression.String(managedTextValue);
            }

            var vars = Engine.GetServiceOrErr<ICustomVariableManager>();
            if (!vars.VariableExists(name))
            {
                Engine.Warn($"Custom variable '{name}' is not initialized, but its value is requested in a script expression. " +
                            "Make sure to initialize variables with '@set' command or via 'Custom Variables' configuration menu before using them.");
                return new Expression.String("");
            }

            var value = vars.GetVariableValue(name);
            if (value.Type == CustomVariableValueType.String) return new Expression.String(value.String);
            if (value.Type == CustomVariableValueType.Boolean) return new Expression.Boolean(value.Boolean);
            return new Numeric(value.Number);
        }

        private static IOperand ResolveFunction (string name, IReadOnlyList<IOperand> args)
        {
            var methodArgs = args.Select(p => p.GetValue()).ToArray();
            var value = InvokeMethod(name, methodArgs, true) ??
                        InvokeMethod(name, methodArgs, false) ??
                        InvokeDefaultSelect(name, methodArgs) ??
                        throw new Error($"Requested '{name}' expression function is not found.");
            return ValueToOperand(value);
        }

        private static object InvokeDefaultSelect (string name, object[] args)
        {
            if (!name.EqualsOrdinal("select")) return null;
            var vars = Engine.GetServiceOrErr<ICustomVariableManager>();
            if (vars.TryGetVariableValue<int>("selector", out var idx))
                return args[Mathf.Min(idx, args.Length - 1)];
            return args[UnityEngine.Random.Range(0, args.Length)];
        }

        private static object InvokeMethod (string name, object[] args, bool strictType)
        {
            if (!fnByName.Contains(name)) return null;

            foreach (var fn in fnByName[name])
            {
                var argsInfo = fn.Method.GetParameters();

                // Handle functions with single 'params' argument.
                if (argsInfo.Length == 1 && argsInfo[0].IsDefined(typeof(ParamArrayAttribute)) &&
                    args.All(p => IsCompatible(p, argsInfo[0].ParameterType.GetElementType())))
                {
                    var elementType = argsInfo[0].ParameterType.GetElementType();
                    for (int i = 0; i < args.Length; i++)
                        args[i] = ConvertValue(args[i], elementType);
                    var elements = Array.CreateInstance(elementType, args.Length);
                    Array.Copy(args, elements, args.Length);
                    return fn.Method.Invoke(null, new object[] { elements });
                }

                // Check argument count equality.
                if (argsInfo.Length != args.Length) continue;

                // Check argument type and order equality.
                var paramTypeCheckPassed = true;
                for (int i = 0; i < argsInfo.Length; i++)
                    if (!IsCompatible(args[i], argsInfo[i].ParameterType))
                    {
                        paramTypeCheckPassed = false;
                        break;
                    }
                    else args[i] = ConvertValue(args[i], argsInfo[i].ParameterType);
                if (!paramTypeCheckPassed) continue;

                return fn.Method.Invoke(null, args);
            }

            return null;

            bool IsCompatible (object actual, Type expected)
            {
                if (strictType)
                {
                    var actualType = actual.GetType();
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    if (actualType == typeof(float) && (float)actual == Convert.ToInt32(actual))
                        actualType = typeof(int);
                    return actualType == expected;
                }
                try { return Convert.ChangeType(actual, expected).GetType() == expected; }
                catch { return false; }
            }

            object ConvertValue (object actual, Type expected)
            {
                if (actual.GetType() == expected) return actual;
                return Convert.ChangeType(actual, expected);
            }
        }

        private static IOperand ValueToOperand (object value)
        {
            if (value is string str) return new Expression.String(str);
            if (value is bool boolean) return new Expression.Boolean(boolean);
            return new Numeric(Convert.ToSingle(value));
        }
    }
}

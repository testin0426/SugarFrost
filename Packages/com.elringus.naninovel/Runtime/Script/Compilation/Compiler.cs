using System;
using System.Collections.Generic;
using System.Linq;
using Naninovel.Syntax;

namespace Naninovel
{
    /// <summary>
    /// Project-specific NaniScript compiler infrastructure.
    /// </summary>
    public static class Compiler
    {
        /// <inheritdoc cref="Syntax.ISymbols"/>
        public static ISymbols Symbols { get; }
        /// <inheritdoc cref="Naninovel.ScriptCompiler"/>
        public static IScriptCompiler ScriptCompiler { get; }
        /// <inheritdoc cref="Naninovel.ScriptFormatter"/>
        public static ScriptFormatter ScriptFormatter { get; }
        /// <inheritdoc cref="Syntax.ScriptFormatter"/>
        public static Syntax.ScriptFormatter SyntaxFormatter { get; }
        /// <inheritdoc cref="Syntax.ScriptParser"/>
        public static ScriptParser SyntaxParser { get; }
        /// <inheritdoc cref="Syntax.NamedValueParser"/>
        public static NamedValueParser NamedValueParser { get; }
        /// <inheritdoc cref="Syntax.ListValueParser"/>
        public static ListValueParser ListValueParser { get; }

        /// <summary>
        /// Prefix to identify global script variables.
        /// </summary>
        public static string GlobalVariablePrefix { get; }
        /// <summary>
        /// Prefix to identify script constants pulled from managed text.
        /// </summary>
        public static string ScriptConstantPrefix { get; }
        /// <summary>
        /// Locale-specific aliases for commands and their parameters, mapped by implementation type name.
        /// </summary>
        public static Dictionary<string, CommandLocalization> Commands { get; }
        /// <summary>
        /// Locale-specific aliases for expression functions, mapped by associated C# method name.
        /// </summary>
        public static Dictionary<string, FunctionLocalization> Functions { get; }
        /// <summary>
        /// Locale-specific aliases for constants baked by C# enums, mapped by associated C# enum type name.
        /// </summary>
        public static Dictionary<string, ConstantLocalization> Constants { get; }

        private static readonly ErrorCollector errCollector = new();

        static Compiler ()
        {
            var cfg = Configuration.GetOrDefault<ScriptsConfiguration>();
            Symbols = cfg.CompilerLocalization.GetSymbols();
            GlobalVariablePrefix = cfg.CompilerLocalization.GlobalVariablePrefix;
            ScriptConstantPrefix = cfg.CompilerLocalization.ScriptConstantPrefix;
            Commands = cfg.CompilerLocalization.Commands?.ToDictionary(kv => kv.Id) ?? new();
            Functions = cfg.CompilerLocalization.Functions?.ToDictionary(kv => kv.MethodName) ?? new();
            Constants = cfg.CompilerLocalization.Constants?.ToDictionary(kv => kv.TypeName) ?? new();
            ScriptFormatter = new(Symbols);
            SyntaxFormatter = new(Symbols);
            NamedValueParser = new(Symbols);
            ListValueParser = new(Symbols);
            SyntaxParser = new(new() { Symbols = Symbols, Handlers = new() { ErrorHandler = errCollector } });
            ScriptCompiler = CreateScriptCompiler(cfg.ScriptCompiler);
        }

        /// <summary>
        /// Transforms specified source scenario text into <see cref="Script"/>.
        /// </summary>
        /// <param name="path">Unique (project-wide) local resource path of the script.</param>
        /// <param name="text">The source scenario text to compile.</param>
        /// <param name="file">Optional location of the script file to be used in error logs.</param>
        public static Script CompileScript (string path, string text, string file = null)
        {
            var logger = CompileErrorLogger.GetFor(file ?? path);
            var script = ScriptCompiler.CompileScript(path, text, new(logger));
            CompileErrorLogger.Return(logger);
            return script;
        }

        /// <summary>
        /// Transforms specified source command body text into <see cref="Command"/>.
        /// </summary>
        /// <param name="text">The source text of the command body (without the leading '@').</param>
        /// <param name="spot">Optional playback spot to associate with the command.</param>
        public static Command CompileCommand (string text, PlaybackSpot spot = default)
        {
            var name = string.IsNullOrEmpty(spot.ScriptPath) ? text : spot.ScriptPath;
            var logger = CompileErrorLogger.GetFor(name);
            var cmd = ScriptCompiler.CompileCommand(text, spot, new(logger));
            CompileErrorLogger.Return(logger);
            return cmd;
        }

        /// <summary>
        /// Parses specified source scenario line text into a line syntax.
        /// </summary>
        /// <param name="lineText">The source scenario line text to parse.</param>
        /// <param name="errors">An optional error handler.</param>
        public static IScriptLine ParseLine (string lineText, IErrorHandler errors = null)
        {
            errCollector.Clear();
            var line = SyntaxParser.ParseLine(lineText);
            if (errors != null)
                foreach (var err in errCollector)
                    errors.HandleError(err);
            return line;
        }

        private static IScriptCompiler CreateScriptCompiler (string typeName)
        {
            var type = Type.GetType(typeName);
            if (type is null) throw new Error($"Failed to create script compiler from '{typeName}': Failed to resolve type.");
            var compiler = Activator.CreateInstance(type) as IScriptCompiler;
            if (compiler == null)
                throw new Error($"Failed to create script compiler from '{typeName}': " +
                                $"Type doesn't implement '{nameof(IScriptCompiler)}' interface.");
            return compiler;
        }
    }
}

using System.Linq;
using Naninovel.Syntax;

namespace Naninovel
{
    /// <inheritdoc cref="IScriptCompiler"/>
    public class ScriptCompiler : IScriptCompiler
    {
        protected virtual ScriptParser ScriptParser { get; }
        protected virtual CommentLineCompiler CommentLineCompiler { get; }
        protected virtual LabelLineCompiler LabelLineCompiler { get; }
        protected virtual CommandLineCompiler CommandLineCompiler { get; }
        protected virtual GenericLineCompiler GenericLineCompiler { get; }
        protected virtual CommandCompiler CommandCompiler { get; }

        private readonly CompileErrorHandler errHandler = new();
        private readonly TextMapper textMapper = new();

        public ScriptCompiler ()
        {
            ScriptParser = new(new() {
                Symbols = Compiler.Symbols,
                Handlers = new() {
                    ErrorHandler = errHandler,
                    TextIdentifier = textMapper
                }
            });
            CommentLineCompiler = new();
            LabelLineCompiler = new();
            CommandLineCompiler = new(textMapper, errHandler);
            GenericLineCompiler = new(textMapper, errHandler);
            CommandCompiler = new(textMapper, errHandler, false);
        }

        public virtual Script CompileScript (string path, string text, CompileOptions options = default)
        {
            Reset(options);
            var textLines = ScriptParser.SplitText(text);
            var lines = new ScriptLine[textLines.Length];
            for (int i = 0; i < textLines.Length; i++)
                lines[i] = CompileLine(i, path, textLines[i]);
            return Script.Create(path, lines, CreateTextMap());
        }

        public Command CompileCommand (string text, PlaybackSpot spot = default, CompileOptions options = default)
        {
            Reset(options);
            errHandler.LineIndex = 0;
            var syntax = ScriptParser.ParseCommand(text);
            return CommandCompiler.Compile(syntax, spot, 0);
        }

        protected virtual ScriptLine CompileLine (int lineIndex, string path, string lineText)
        {
            errHandler.LineIndex = lineIndex;
            switch (ScriptParser.ParseLine(lineText))
            {
                case Syntax.CommentLine comment: return CompileCommentLine(new(path, lineText, lineIndex, comment));
                case Syntax.LabelLine label: return CompileLabelLine(new(path, lineText, lineIndex, label));
                case Syntax.CommandLine command: return CompileCommandLine(new(path, lineText, lineIndex, command));
                case Syntax.GenericLine generic:
                    if (string.IsNullOrWhiteSpace(lineText)) return new EmptyLine(lineIndex, generic.Indent);
                    return CompileGenericLine(new(path, lineText, lineIndex, generic));
                default: throw new Error($"Unknown line type: {lineText}");
            }
        }

        protected virtual CommentLine CompileCommentLine (LineCompileArgs<Syntax.CommentLine> args)
        {
            return CommentLineCompiler.Compile(args);
        }

        protected virtual LabelLine CompileLabelLine (LineCompileArgs<Syntax.LabelLine> args)
        {
            return LabelLineCompiler.Compile(args);
        }

        protected virtual CommandLine CompileCommandLine (LineCompileArgs<Syntax.CommandLine> args)
        {
            return CommandLineCompiler.Compile(args);
        }

        protected virtual GenericLine CompileGenericLine (LineCompileArgs<Syntax.GenericLine> args)
        {
            return GenericLineCompiler.Compile(args);
        }

        protected virtual ScriptTextMap CreateTextMap ()
        {
            return new(textMapper.Map.ToDictionary(kv => kv.Key, kv => kv.Value));
        }

        protected virtual void Reset (CompileOptions options)
        {
            errHandler.Errors = options.Errors;
            textMapper.Clear();
        }
    }
}

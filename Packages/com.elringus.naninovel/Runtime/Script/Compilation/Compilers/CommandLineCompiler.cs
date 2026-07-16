using Naninovel.Syntax;

namespace Naninovel
{
    public class CommandLineCompiler : ScriptLineCompiler<CommandLine, Syntax.CommandLine>
    {
        protected virtual CommandCompiler CommandCompiler { get; }

        public CommandLineCompiler (ITextIdentifier identifier, IErrorHandler errorHandler = null)
        {
            CommandCompiler = new(identifier, errorHandler);
        }

        protected override CommandLine Compile (Syntax.CommandLine stx)
        {
            var spot = new PlaybackSpot(ScriptPath, LineIndex, 0);
            var command = CommandCompiler.Compile(stx.Command, spot, stx.Indent);
            return new(command, LineIndex, stx.Indent, LineHash);
        }
    }
}


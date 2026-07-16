using Naninovel.Syntax;

namespace Naninovel
{
    public abstract class ScriptLineCompiler<TResult, TSyntax>
        where TResult : ScriptLine
        where TSyntax : IScriptLine
    {
        protected virtual string ScriptPath { get; private set; }
        protected virtual int LineIndex { get; private set; }
        protected virtual string LineHash { get; private set; }

        /// <summary>
        /// Produces a persistent hash code from the specified script line text (trimmed).
        /// </summary>
        public static string GetHash (string lineText)
        {
            return CryptoUtils.PersistentHexCode(lineText.TrimFull());
        }

        public virtual TResult Compile (LineCompileArgs<TSyntax> args)
        {
            ScriptPath = args.ScriptPath;
            LineIndex = args.LineIndex;
            LineHash = GetHash(args.LineText);
            return Compile(args.LineSyntax);
        }

        protected abstract TResult Compile (TSyntax stx);
    }
}

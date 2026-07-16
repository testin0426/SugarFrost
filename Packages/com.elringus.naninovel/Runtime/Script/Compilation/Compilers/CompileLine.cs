using System.Collections.Generic;
using Naninovel.Syntax;

namespace Naninovel
{
    public delegate TSyntax CompileLine<TSyntax> (
        string lineText,
        IReadOnlyList<Token> tokens)
        where TSyntax : IScriptLine;
}

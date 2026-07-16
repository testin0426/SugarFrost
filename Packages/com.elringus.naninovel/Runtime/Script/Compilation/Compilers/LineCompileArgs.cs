using System;
using System.Collections.Generic;
using Naninovel.Syntax;

namespace Naninovel
{
    /// <summary>
    /// Arguments for line syntax compilation.
    /// </summary>
    public readonly struct LineCompileArgs<TSyntax> : IEquatable<LineCompileArgs<TSyntax>> where TSyntax : IScriptLine
    {
        public readonly string ScriptPath;
        public readonly string LineText;
        public readonly int LineIndex;
        public readonly TSyntax LineSyntax;

        public LineCompileArgs (string scriptPath, string lineText, int lineIndex, TSyntax lineSyntax)
        {
            ScriptPath = scriptPath;
            LineText = lineText;
            LineIndex = lineIndex;
            LineSyntax = lineSyntax;
        }

        public bool Equals (LineCompileArgs<TSyntax> other)
        {
            return ScriptPath == other.ScriptPath &&
                   LineText == other.LineText &&
                   LineIndex == other.LineIndex &&
                   EqualityComparer<TSyntax>.Default.Equals(LineSyntax, other.LineSyntax);
        }

        public override bool Equals (object obj)
        {
            return obj is LineCompileArgs<TSyntax> other && Equals(other);
        }

        public override int GetHashCode ()
        {
            unchecked
            {
                var hashCode = (ScriptPath != null ? ScriptPath.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (LineText != null ? LineText.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ LineIndex;
                hashCode = (hashCode * 397) ^ EqualityComparer<TSyntax>.Default.GetHashCode(LineSyntax);
                return hashCode;
            }
        }
    }
}

using System;

namespace Naninovel
{
    [Serializable]
    public class EmptyLine : ScriptLine
    {
        public EmptyLine (int lineIndex, int indent)
            : base(lineIndex, indent, string.Empty) { }
    }
}

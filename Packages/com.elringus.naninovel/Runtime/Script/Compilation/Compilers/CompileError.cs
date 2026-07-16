using System;

namespace Naninovel
{
    /// <summary>
    /// Represent a <see cref="ScriptLine"/> compilation error.
    /// </summary>
    public readonly struct CompileError : IEquatable<CompileError>
    {
        /// <summary>
        /// Index of the line in the scenario script.
        /// </summary>
        public readonly int LineIndex;
        /// <summary>
        /// Number of the line in the scenario script (index + 1).
        /// </summary>
        public int LineNumber => LineIndex + 1;
        /// <summary>
        /// Description of the compilation error.
        /// </summary>
        public readonly string Description;

        public CompileError (int lineIndex, string description)
        {
            LineIndex = lineIndex;
            Description = description ?? string.Empty;
        }

        public string ToString (string scriptPathOrName)
        {
            var description = Description == string.Empty ? "." : $": {Description}";
            var link = StringUtils.BuildAssetLink(scriptPathOrName, LineIndex);
            return $"Error compiling {link} script at line #{LineNumber}{description}";
        }

        public override int GetHashCode ()
        {
            unchecked
            {
                return (LineIndex * 397) ^ Description.GetHashCode();
            }
        }

        public bool Equals (CompileError other) => LineIndex == other.LineIndex && Description == other.Description;
        public override bool Equals (object obj) => obj is CompileError other && Equals(other);
        public static bool operator == (CompileError left, CompileError right) => left.Equals(right);
        public static bool operator != (CompileError left, CompileError right) => !left.Equals(right);
    }
}

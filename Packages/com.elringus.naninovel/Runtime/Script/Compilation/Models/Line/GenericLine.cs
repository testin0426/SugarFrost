using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// A <see cref="Script"/> line representing text to print.
    /// </summary>
    [Serializable]
    public class GenericLine : ScriptLine
    {
        /// <summary>
        /// A list of <see cref="Command"/> contained by this line.
        /// </summary>
        public IReadOnlyList<Command> InlinedCommands => inlinedCommands;

        [SerializeReference] private List<Command> inlinedCommands;

        public GenericLine (IEnumerable<Command> inlinedCommands, int lineIndex, int indent, string lineHash)
            : base(lineIndex, indent, lineHash)
        {
            this.inlinedCommands = inlinedCommands.ToList();
        }
    }
}

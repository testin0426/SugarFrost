using System.Collections.Generic;

namespace Naninovel
{
    /// <summary>
    /// Configures <see cref="IScriptCompiler.CompileScript"/>.
    /// </summary>
    public readonly struct CompileOptions
    {
        /// <summary>
        /// When assigned and error occurs while compiling, will add the error to the collection.
        /// </summary>
        public readonly ICollection<CompileError> Errors;

        public CompileOptions (ICollection<CompileError> errors)
        {
            Errors = errors;
        }
    }
}

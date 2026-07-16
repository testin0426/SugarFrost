using System.Collections.Generic;
using Naninovel.Syntax;

namespace Naninovel
{
    public class CompileErrorHandler : IErrorHandler
    {
        public ICollection<CompileError> Errors { get; set; }
        public int LineIndex { get; set; }

        public void HandleError (ParseError error)
        {
            Errors?.Add(new(LineIndex, error.Message));
        }
    }
}

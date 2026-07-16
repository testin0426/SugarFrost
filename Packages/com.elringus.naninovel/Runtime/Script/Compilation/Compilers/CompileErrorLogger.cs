using System.Collections;
using System.Collections.Generic;

namespace Naninovel
{
    /// <summary>
    /// Can be specified to the script compiler instead of error collection to log the errors.
    /// </summary>
    public class CompileErrorLogger : ICollection<CompileError>
    {
        public int Count { get; } = 0;
        public bool IsReadOnly => false;

        private static readonly Stack<CompileErrorLogger> pool = new();

        private readonly IEnumerator<CompileError> enumerator = new List<CompileError>.Enumerator();
        private string scriptPathOrName;

        private CompileErrorLogger () { }

        public static CompileErrorLogger GetFor (string scriptPathOrName)
        {
            var logger = pool.Count > 0 ? pool.Pop() : new();
            logger.scriptPathOrName = scriptPathOrName;
            return logger;
        }

        public static void Return (CompileErrorLogger logger)
        {
            pool.Push(logger);
        }

        public void Add (CompileError item) => Engine.Err(item.ToString(scriptPathOrName));
        public bool Remove (CompileError item) => true;
        public void Clear () { }
        public bool Contains (CompileError item) => false;
        public void CopyTo (CompileError[] array, int arrayIndex) { }
        public IEnumerator<CompileError> GetEnumerator () => enumerator;
        IEnumerator IEnumerable.GetEnumerator () => GetEnumerator();
    }
}

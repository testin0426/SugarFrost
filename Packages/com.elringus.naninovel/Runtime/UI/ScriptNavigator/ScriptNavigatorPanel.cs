using System.Collections.Generic;
using UnityEngine;

namespace Naninovel.UI
{
    public class ScriptNavigatorPanel : NavigatorPanel, IScriptNavigatorUI
    {
        protected override Awaitable LocateAllScriptPaths (ICollection<string> paths)
        {
            ScriptManager.ScriptLoader.CollectPaths(paths);
            return Async.Completed;
        }
    }
}

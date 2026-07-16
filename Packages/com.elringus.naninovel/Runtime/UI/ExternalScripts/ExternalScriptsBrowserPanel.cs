using System.Collections.Generic;
using UnityEngine;

namespace Naninovel.UI
{
    public class ExternalScriptsBrowserPanel : NavigatorPanel, IExternalScriptsUI
    {
        protected override Awaitable LocateAllScriptPaths (ICollection<string> paths)
        {
            ScriptManager.ExternalScriptLoader.CollectPaths(paths);
            return Async.Completed;
        }
    }
}

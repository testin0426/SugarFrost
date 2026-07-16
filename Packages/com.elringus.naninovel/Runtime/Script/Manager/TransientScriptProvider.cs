using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Naninovel
{
    /// <summary>
    /// Manages <see cref="Script"/> resources added with <see cref="IScriptManager.AddTransientScript"/>.
    /// </summary>
    public class TransientScriptProvider : ResourceProvider
    {
        [Serializable]
        public class State
        {
            [CanBeNull] public string[] Paths;
            [CanBeNull] public string[] Texts;
        }

        protected virtual Dictionary<string, string> TextByPath { get; } = new();
        protected virtual Dictionary<string, Script> ScriptByPath { get; } = new();
        protected virtual IScriptManager Manager { get; }

        public TransientScriptProvider (IScriptManager manager)
        {
            Manager = manager;
        }

        public virtual void SetScriptText (string localPath, string text)
        {
            var fullPath = Manager.ScriptLoader.GetFullPath(localPath);
            Paths.Add(fullPath);
            TextByPath[fullPath] = text;
        }

        public virtual bool ScriptExists (string localPath)
        {
            var fullPath = Manager.ScriptLoader.GetFullPath(localPath);
            return TextByPath.ContainsKey(fullPath);
        }

        public virtual void RemoveScript (string localPath)
        {
            var fullPath = Manager.ScriptLoader.GetFullPath(localPath);
            Paths.Remove(fullPath);
            TextByPath.Remove(fullPath);
        }

        [CanBeNull]
        public virtual State Serialize ()
        {
            if (TextByPath.Count == 0) return null;
            return new() {
                Paths = TextByPath.Keys.ToArray(),
                Texts = TextByPath.Values.ToArray()
            };
        }

        public virtual void Deserialize ([CanBeNull] State state)
        {
            Paths.Clear();
            TextByPath.Clear();
            if (state?.Paths != null && state.Texts != null)
                for (int i = 0; i < state.Paths.Length; i++)
                {
                    Paths.Add(state.Paths[i]);
                    TextByPath[state.Paths[i]] = state.Texts[i];
                }
        }

        protected override Awaitable<Object> LoadObject (string fullPath)
        {
            if (ScriptByPath.TryGetValue(fullPath, out var script))
                return Async.Result<Object>(script);
            if (!TextByPath.TryGetValue(fullPath, out var text))
                throw new Error($"Failed to load '{fullPath}' transient script: missing script text.");
            var localPath = Manager.ScriptLoader.GetLocalPath(fullPath);
            script = ScriptByPath[fullPath] = Compiler.CompileScript(localPath, text);
            return Async.Result<Object>(script);
        }

        protected override void DisposeResource (Resource resource)
        {
            var localPath = Manager.ScriptLoader.GetLocalPath(resource);
            if (Manager.HasTransientScript(localPath))
                Manager.RemoveTransientScript(localPath);
        }
    }
}

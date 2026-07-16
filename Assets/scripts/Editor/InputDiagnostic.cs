#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Naninovel;
using Naninovel.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Game.Debug
{
    public static class InputDiagnostic
    {
        [MenuItem("Naninovel/Debug/Dump Input State")]
        public static void DumpInputState ()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== INPUT DIAGNOSTIC ===");

            // 1. EventSystem
            var es = EventSystem.current;
            if (!es) sb.AppendLine("[CRITICAL] No EventSystem.current!");
            else      sb.AppendLine($"EventSystem: '{es.name}'  enabled={es.isActiveAndEnabled}");

            // All EventSystems in scene (duplicates disable each other)
            var allES = Object.FindObjectsByType<EventSystem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            sb.AppendLine($"Total EventSystem objects in scene: {allES.Length}");
            foreach (var e in allES)
                sb.AppendLine($"  {e.name}  active={e.gameObject.activeInHierarchy}  enabled={e.enabled}");

            if (!Engine.Initialized)
            {
                sb.AppendLine("[CRITICAL] Engine not initialized.");
                sb.AppendLine("========================");
                UnityEngine.Debug.Log(sb.ToString());
                return;
            }

            // 2. InputManager basics
            var input = Engine.GetService<IInputManager>();
            if (input == null) { sb.AppendLine("[CRITICAL] IInputManager is null."); }
            else
            {
                sb.AppendLine($"\nInputManager.Enabled = {input.Enabled}");
                sb.AppendLine($"InputManager.Muted   = {input.Muted}");
                sb.AppendLine($"IsMuted(Continue)    = {input.IsMuted(Inputs.Continue)}");
                sb.AppendLine($"IsMuted(Skip)        = {input.IsMuted(Inputs.Skip)}");
            }

            // 3. Active muters via reflection
            var concreteInput = input as InputManager;
            if (concreteInput != null)
            {
                var regProp = typeof(InputManager).GetProperty("MuteRegistry",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                var registry = regProp?.GetValue(concreteInput) as InputMuteRegistry;
                if (registry != null)
                {
                    var dictProp = typeof(InputMuteRegistry).GetProperty("AllowedIdsByMuter",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                    var dict = dictProp?.GetValue(registry)
                        as Dictionary<object, HashSet<string>>;
                    if (dict != null)
                    {
                        sb.AppendLine($"\n--- Active Muters ({dict.Count}) ---");
                        foreach (var kv in dict)
                        {
                            var allowed = kv.Value == null ? "<BLOCKS ALL>" : $"[{string.Join(", ", kv.Value)}]";
                            sb.AppendLine($"  [{kv.Key?.GetType().Name}] '{kv.Key}' → {allowed}");
                        }
                    }
                    else sb.AppendLine("(could not read muter dict)");
                }
            }

            // 4. Scene-object TitleMenus (not UIManager-managed)
            var titleMenus = Object.FindObjectsByType<TitleMenu>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            sb.AppendLine($"\n--- TitleMenu objects ({titleMenus.Length}) ---");
            foreach (var tm in titleMenus)
                sb.AppendLine($"  '{tm.name}' visible={tm.Visible} blockInput={tm.BlockInputWhenVisible} go.active={tm.gameObject.activeInHierarchy}");

            // 5. UIManager managed UIs that currently block input
            var uis = Engine.GetService<IUIManager>() as UIManager;
            if (uis != null)
            {
                var muiProp = typeof(UIManager).GetProperty("ManagedUIs",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                var managed = muiProp?.GetValue(uis) as System.Collections.IEnumerable;
                if (managed != null)
                {
                    sb.AppendLine("\n--- Managed UIs blocking input ---");
                    bool anyBlocking = false;
                    foreach (var item in managed)
                    {
                        var uiComp = item.GetType().GetProperty("UIComponent")?.GetValue(item) as CustomUI;
                        if (uiComp != null && uiComp.BlockInputWhenVisible && uiComp.Visible)
                        {
                            sb.AppendLine($"  BLOCKING: '{uiComp.name}'");
                            anyBlocking = true;
                        }
                    }
                    if (!anyBlocking) sb.AppendLine("  (none)");
                }

                var ctp = uis.GetUI<IClickThroughPanel>();
                var ctpUI = ctp as CustomUI;
                sb.AppendLine($"\nClickThroughPanel visible = {ctpUI?.Visible}  blockInput={ctpUI?.BlockInputWhenVisible}");
            }

            sb.AppendLine("========================");
            UnityEngine.Debug.Log(sb.ToString());
        }
    }
}
#endif

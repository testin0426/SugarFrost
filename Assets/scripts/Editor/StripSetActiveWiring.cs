#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Editor
{
    public static class StripSetActiveWiring
    {
        [MenuItem("Naninovel/Debug/Strip SetActive wiring from GenericActorBehaviour prefabs")]
        public static void Run()
        {
            var guids = AssetDatabase.FindAssets("t:Prefab");
            int stripped = 0;
            var modified = new List<string>();

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;

                bool prefabDirty = false;
                foreach (var comp in prefab.GetComponentsInChildren<Component>(true))
                {
                    if (comp == null) continue;
                    var so = new SerializedObject(comp);
                    var eventProp = so.FindProperty("onVisibilityChanged");
                    if (eventProp == null) continue;

                    var calls = eventProp.FindPropertyRelative("m_PersistentCalls.m_Calls");
                    if (calls == null) continue;

                    bool compDirty = false;
                    for (int i = calls.arraySize - 1; i >= 0; i--)
                    {
                        var method = calls.GetArrayElementAtIndex(i).FindPropertyRelative("m_MethodName");
                        if (method != null && method.stringValue == "SetActive")
                        {
                            UnityEngine.Debug.Log($"[StripSetActiveWiring] Removing SetActive[{i}] from '{comp.name}' in {path}");
                            calls.DeleteArrayElementAtIndex(i);
                            compDirty = true;
                        }
                    }

                    if (compDirty)
                    {
                        so.ApplyModifiedPropertiesWithoutUndo();
                        prefabDirty = true;
                    }
                }

                if (prefabDirty)
                {
                    PrefabUtility.SavePrefabAsset(prefab);
                    modified.Add(path);
                    stripped++;
                }
            }

            AssetDatabase.SaveAssets();
            UnityEngine.Debug.Log($"[StripSetActiveWiring] Done. Modified {stripped} prefab(s):\n{string.Join("\n", modified)}");
        }
    }
}
#endif

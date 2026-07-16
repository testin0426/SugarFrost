using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public class AutoImportFBXAnim : AssetPostprocessor
{
    void OnPreprocessModel()
    {
        ModelImporter importer = (ModelImporter)assetImporter;
        string fileName = Path.GetFileNameWithoutExtension(assetPath);
        string folderPath = Path.GetDirectoryName(assetPath);

        if (!fileName.Contains("_")) return;

        string[] parts = fileName.Split('_');
        string baseName = parts[0]; 
        string actionName = parts[1]; // This will be "FuEr"

        string sourceFbxPath = $"{folderPath}/{baseName}.fbx".Replace("\\", "/");
        if (!File.Exists(sourceFbxPath)) return;

        // 1. Set Rig Configuration
        importer.animationType = ModelImporterAnimationType.Generic;
        
        Avatar sourceAvatar = AssetDatabase.LoadAssetAtPath<Avatar>(sourceFbxPath);
        if (sourceAvatar != null)
        {
            importer.sourceAvatar = sourceAvatar;
        }

        // 2. Force Clip Renaming (Simulating the "Apply" button)
       ModelImporterClipAnimation[] clips = importer.clipAnimations?.Length > 0
        ? importer.clipAnimations       // 已有配置
        : importer.defaultClipAnimations; 

        TakeInfo[] takes = importer.importedTakeInfos;
        
        if (clips != null && clips.Length > 0 && takes != null && takes.Length > 0)
        {
            clips[0].name = actionName;
            clips[0].takeName = takes[0].name; // ← 关键！每次都同步实际take名
            importer.clipAnimations = clips;
        }
        else if (takes != null && takes.Length > 0)
        {
            var fallbackClip = new ModelImporterClipAnimation
            {
                name = actionName,
                takeName = takes[0].name
            };
            importer.clipAnimations = new[] { fallbackClip };
        }
    }

    static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
    {
        foreach (string assetPath in importedAssets)
        {
            if (!assetPath.EndsWith(".fbx", System.StringComparison.OrdinalIgnoreCase)) continue;

            string fileName = Path.GetFileNameWithoutExtension(assetPath);
            if (!fileName.Contains("_")) continue;

            string[] parts = fileName.Split('_');
            string baseName = parts[0];
            string actionName = string.Join("_", parts, 1, parts.Length - 1);
            string folderPath = Path.GetDirectoryName(assetPath);

            string controllerPath = $"{folderPath}/{baseName}.controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            
            if (controller == null) continue;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            AnimationClip importedClip = null;
            
            // Search specifically for the clip we renamed in the Preprocess step
            foreach (Object asset in assets)
            {
                if (asset is AnimationClip && asset.name == actionName)
                {
                    importedClip = asset as AnimationClip;
                    break;
                }
            }

            if (importedClip != null)
            {
                // Pass actionName explicitly to guarantee the Trigger and State are named correctly
                SetupAnimatorController(controller, importedClip, actionName);
            }
            else
            {
                Debug.LogWarning($"[Pipeline] Could not locate the renamed clip '{actionName}' in {assetPath}. Unity may require an editor refresh.");
            }
        }
    }

    static void SetupAnimatorController(AnimatorController controller, AnimationClip clip, string targetName)
    {
        // Add Trigger using targetName
        if (!controller.parameters.Any(p => p.name == targetName && p.type == AnimatorControllerParameterType.Trigger))
        {
            controller.AddParameter(targetName, AnimatorControllerParameterType.Trigger);
        }

        int layerIndex = -1;
        for (int i = 0; i < controller.layers.Length; i++)
        {
            if (controller.layers[i].name == "Body")
            {
                layerIndex = i;
                break;
            }
        }

        if (layerIndex == -1)
        {
            controller.AddLayer("Body");
            layerIndex = controller.layers.Length - 1;
            AnimatorControllerLayer[] layers = controller.layers;
            layers[layerIndex].defaultWeight = 1f;
            controller.layers = layers;
        }

        AnimatorStateMachine stateMachine = controller.layers[layerIndex].stateMachine;

        // Create State
        AnimatorState state = null;
        foreach (var childState in stateMachine.states)
        {
            if (childState.state.name == targetName)
            {
                state = childState.state;
                break;
            }
        }

        if (state == null)
        {
            state = stateMachine.AddState(targetName);
        }
        state.motion = clip;

        // Create AnyState Transition
        bool transitionExists = false;
        foreach (var t in stateMachine.anyStateTransitions)
        {
            if (t.destinationState == state)
            {
                transitionExists = true;
                break;
            }
        }

        if (!transitionExists)
        {
            AnimatorStateTransition transition = stateMachine.AddAnyStateTransition(state);
            transition.AddCondition(AnimatorConditionMode.If, 0, targetName);
            transition.hasExitTime = false;
            transition.duration = 0.1f;
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log($"[Pipeline] Successfully processed clip '{clip.name}' and assigned to Trigger/State '{targetName}'.");
    }
}
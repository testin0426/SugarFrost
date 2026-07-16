using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

[RequireComponent(typeof(SkinnedMeshRenderer))]
public class BlendShapePoseBaker : MonoBehaviour
{
    [Tooltip("The root Animator of the character.")]
    public Animator rootAnimator;

    [Tooltip("Add all the blendshape names you want to generate individual clips for.")]
    public List<string> targetBlendShapes = new List<string>();

    [Tooltip("The folder where animations will be saved. Must start with 'Assets/' and end with '/'.")]
    public string saveFolder = "Assets/Animations/Faces/";

    #if UNITY_EDITOR
    [ContextMenu("Bake Poses and Setup Animator")]
    public void BakeAndSetup()
    {
        SkinnedMeshRenderer smr = GetComponent<SkinnedMeshRenderer>();
        if (smr == null || smr.sharedMesh == null)
        {
            Debug.LogError("Missing SkinnedMeshRenderer or Mesh data.");
            return;
        }

        if (!saveFolder.StartsWith("Assets/") || !saveFolder.EndsWith("/"))
        {
            Debug.LogError("Save folder path must start with 'Assets/' and end with '/'. Example: 'Assets/Animations/'");
            return;
        }

        if (!AssetDatabase.IsValidFolder(saveFolder.TrimEnd('/')))
        {
            Debug.LogError($"Directory '{saveFolder}' does not exist. Please create the folder in your Project window first.");
            return;
        }

        if (rootAnimator == null)
        {
            rootAnimator = GetComponentInParent<Animator>();
            if (rootAnimator == null)
            {
                Debug.LogError("Animator not found in parent hierarchy.");
                return;
            }
        }

        AnimatorController controller = rootAnimator.runtimeAnimatorController as AnimatorController;
        if (controller == null)
        {
            Debug.LogError("Animator does not have a valid AnimatorController assigned.");
            return;
        }

        Mesh mesh = smr.sharedMesh;
        string relativePath = AnimationUtility.CalculateTransformPath(transform, rootAnimator.transform);

        // Find or Create the "Face" Layer
        int faceLayerIndex = -1;
        for (int i = 0; i < controller.layers.Length; i++)
        {
            if (controller.layers[i].name == "Face")
            {
                faceLayerIndex = i;
                break;
            }
        }

        if (faceLayerIndex == -1)
        {
            controller.AddLayer("Face");
            faceLayerIndex = controller.layers.Length - 1;
            AnimatorControllerLayer[] layers = controller.layers;
            layers[faceLayerIndex].defaultWeight = 1f;
            controller.layers = layers;
        }

        AnimatorStateMachine faceStateMachine = controller.layers[faceLayerIndex].stateMachine;

        // Collect all existing .anim clips in the save folder
        string[] existingGuids = AssetDatabase.FindAssets("t:AnimationClip", new[] { saveFolder.TrimEnd('/') });
        List<AnimationClip> existingClips = new List<AnimationClip>();
        HashSet<string> existingClipNames = new HashSet<string>();
        foreach (string guid in existingGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AnimationClip existClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (existClip != null)
            {
                existingClips.Add(existClip);
                existingClipNames.Add(existClip.name);
            }
        }

        // Collect all blendshape property names that already exist across all existing clips
        HashSet<string> allExistingProperties = new HashSet<string>();
        foreach (var existClip in existingClips)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(existClip))
            {
                if (binding.propertyName.StartsWith("blendShape."))
                    allExistingProperties.Add(binding.propertyName);
            }
        }

        List<AnimationClip> newlyCreatedClips = new List<AnimationClip>();

        // Process each target blendshape
        foreach (string targetShape in targetBlendShapes)
        {
            AnimationClip clip = new AnimationClip { frameRate = 30 };

            for (int i = 0; i < mesh.blendShapeCount; i++)
            {
                string shapeName = mesh.GetBlendShapeName(i);
                float weightValue = (shapeName == targetShape) ? 100f : 0f;

                EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
                    relativePath,
                    typeof(SkinnedMeshRenderer),
                    "blendShape." + shapeName
                );

                AnimationCurve curve = new AnimationCurve(new Keyframe(0f, weightValue));
                AnimationUtility.SetEditorCurve(clip, binding, curve);
            }

            string fullPath = saveFolder + targetShape + ".anim";
            AssetDatabase.CreateAsset(clip, fullPath);
            newlyCreatedClips.Add(clip);

            // Setup Animator Controller
            if (!controller.parameters.Any(p => p.name == targetShape && p.type == AnimatorControllerParameterType.Trigger))
            {
                controller.AddParameter(targetShape, AnimatorControllerParameterType.Trigger);
            }

            AnimatorState state = null;
            foreach (var childState in faceStateMachine.states)
            {
                if (childState.state.name == targetShape)
                {
                    state = childState.state;
                    break;
                }
            }

            if (state == null)
            {
                state = faceStateMachine.AddState(targetShape);
            }

            state.motion = clip;

            bool transitionExists = false;
            foreach (var t in faceStateMachine.anyStateTransitions)
            {
                if (t.destinationState == state)
                {
                    transitionExists = true;
                    break;
                }
            }

            if (!transitionExists)
            {
                AnimatorStateTransition transition = faceStateMachine.AddAnyStateTransition(state);
                transition.AddCondition(AnimatorConditionMode.If, 0, targetShape);
                transition.hasExitTime = false;
                transition.duration = 0.0f;
            }
        }

        // --- Sync blendshape properties across all clips ---
        // Gather the property names from newly created clips
        HashSet<string> newProperties = new HashSet<string>();
        foreach (var newClip in newlyCreatedClips)
        {
            foreach (var binding in AnimationUtility.GetCurveBindings(newClip))
            {
                if (binding.propertyName.StartsWith("blendShape."))
                    newProperties.Add(binding.propertyName);
            }
        }

        // Step 1: Patch existing clips — add missing new blendshape properties with value 0
        foreach (var existClip in existingClips)
        {
            var existBindings = AnimationUtility.GetCurveBindings(existClip);
            HashSet<string> existProps = new HashSet<string>();
            foreach (var b in existBindings)
            {
                if (b.propertyName.StartsWith("blendShape."))
                    existProps.Add(b.propertyName);
            }

            bool modified = false;
            foreach (string prop in newProperties)
            {
                if (!existProps.Contains(prop))
                {
                    EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
                        relativePath,
                        typeof(SkinnedMeshRenderer),
                        prop
                    );
                    AnimationCurve zeroCurve = new AnimationCurve(new Keyframe(0f, 0f));
                    AnimationUtility.SetEditorCurve(existClip, binding, zeroCurve);
                    modified = true;
                }
            }

            if (modified)
            {
                EditorUtility.SetDirty(existClip);
            }
        }

        // Step 2: Patch new clips — add missing properties that existing clips had
        foreach (var newClip in newlyCreatedClips)
        {
            var newBindings = AnimationUtility.GetCurveBindings(newClip);
            HashSet<string> newClipProps = new HashSet<string>();
            foreach (var b in newBindings)
            {
                if (b.propertyName.StartsWith("blendShape."))
                    newClipProps.Add(b.propertyName);
            }

            bool modified = false;
            foreach (string prop in allExistingProperties)
            {
                if (!newClipProps.Contains(prop))
                {
                    EditorCurveBinding binding = EditorCurveBinding.FloatCurve(
                        relativePath,
                        typeof(SkinnedMeshRenderer),
                        prop
                    );
                    AnimationCurve zeroCurve = new AnimationCurve(new Keyframe(0f, 0f));
                    AnimationUtility.SetEditorCurve(newClip, binding, zeroCurve);
                    modified = true;
                }
            }

            if (modified)
            {
                EditorUtility.SetDirty(newClip);
            }
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log($"Successfully baked {targetBlendShapes.Count} clips to '{saveFolder}' and synced blendshape properties across {existingClips.Count} existing clips.");
    }
    #endif
}

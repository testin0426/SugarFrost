using Naninovel.UI;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEngine;

namespace Naninovel
{
    public static class AssetMenuItems
    {
        private class DoCopyAsset : EndNameEditAction
        {
            public override void Action (int instanceId, string targetPath, string sourcePath)
            {
                AssetDatabase.CopyAsset(sourcePath, targetPath);
                var newAsset = AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
                ProjectWindowUtil.ShowCreatedAsset(newAsset);
            }
        }

        public const string DefaultScriptContent = "\n";
        public const string DefaultManagedTextContent = "Item1Path: Item 1 Value\nItem2Path: Item 2 Value\n";

        private static void CreateResourceCopy (string resourcePath, string copyName)
        {
            var assetPath = PathUtils.Combine(PackagePath.RuntimeResourcesPath, resourcePath);
            CreateAssetCopy(assetPath, copyName);
        }

        private static void CreatePrefabCopy (string prefabPath, string copyName)
        {
            var assetPath = PathUtils.Combine(PackagePath.PrefabsPath, $"{prefabPath}.prefab");
            CreateAssetCopy(assetPath, copyName + ".prefab");
        }

        private static void CreateAssetCopy (string assetPath, string copyPath)
        {
            var endAction = ScriptableObject.CreateInstance<DoCopyAsset>();
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, endAction, copyPath, null, assetPath);
        }

        [MenuItem("Assets/Create/Naninovel/Scenario Script", priority = -4)]
        private static void CreateScript () => ProjectWindowUtil.CreateAssetWithContent("NewScript.nani", DefaultScriptContent);

        [MenuItem("Assets/Create/Naninovel/Managed Text", priority = -3)]
        private static void CreateManagedText () => ProjectWindowUtil.CreateAssetWithContent("NewManagedText.txt", DefaultManagedTextContent);

        [MenuItem("Assets/Create/Naninovel/Custom UI", priority = -2)]
        private static void CreateCustomUI () => CreatePrefabCopy("Templates/CustomUI", "NewCustomUI");

        [MenuItem("Assets/Create/Naninovel/Input/Controls")]
        private static void CreateInputControls () => CreateResourceCopy("Input/DefaultControls.inputactions", "NewControls.inputactions");
        [MenuItem("Assets/Create/Naninovel/Input/Event System")]
        private static void CreateInputEventSystem () => CreateResourceCopy("Input/DefaultEventSystem.prefab", "NewEventSystem.prefab");

        [MenuItem("Assets/Create/Naninovel/Character/Generic", priority = 100)]
        private static void CreateCharacterGeneric () => CreatePrefabCopy("Templates/GenericCharacter", "NewGenericCharacter");
        [MenuItem("Assets/Create/Naninovel/Character/Layered", priority = 100)]
        private static void CreateCharacterLayered () => CreatePrefabCopy("Templates/LayeredCharacter", "NewLayeredCharacter");

        [MenuItem("Assets/Create/Naninovel/Background/Generic", priority = 101)]
        private static void CreateBackgroundGeneric () => CreatePrefabCopy("Templates/GenericBackground", "NewGenericBackground");
        [MenuItem("Assets/Create/Naninovel/Background/Layered", priority = 101)]
        private static void CreateBackgroundLayered () => CreatePrefabCopy("Templates/LayeredBackground", "NewLayeredBackground");

        [MenuItem("Assets/Create/Naninovel/Text Printer/Dialogue", priority = 102)]
        private static void CreatePrinterDialogue () => CreatePrefabCopy("TextPrinters/Dialogue", "NewDialoguePrinter");
        [MenuItem("Assets/Create/Naninovel/Text Printer/Fullscreen", priority = 102)]
        private static void CreatePrinterFullscreen () => CreatePrefabCopy("TextPrinters/Fullscreen", "NewFullscreenPrinter");
        [MenuItem("Assets/Create/Naninovel/Text Printer/Wide", priority = 102)]
        private static void CreatePrinterWide () => CreatePrefabCopy("TextPrinters/Wide", "NewWidePrinter");
        [MenuItem("Assets/Create/Naninovel/Text Printer/Chat", priority = 102)]
        private static void CreatePrinterChat () => CreatePrefabCopy("TextPrinters/Chat", "NewChatPrinter");
        [MenuItem("Assets/Create/Naninovel/Text Printer/Bubble", priority = 102)]
        private static void CreatePrinterBubble () => CreatePrefabCopy("TextPrinters/Bubble", "NewBubblePrinter");
        [MenuItem("Assets/Create/Naninovel/Text Printer/Scene Bubble", priority = 102)]
        private static void CreatePrinterSceneBubble () => CreatePrefabCopy("TextPrinters/SceneBubble", "NewSceneBubblePrinter");

        [MenuItem("Assets/Create/Naninovel/Choice Handler/Button List", priority = 103)]
        private static void CreateChoiceButtonList () => CreatePrefabCopy("ChoiceHandlers/ButtonList", "NewButtonListChoiceHandler");
        [MenuItem("Assets/Create/Naninovel/Choice Handler/Scene List", priority = 103)]
        private static void CreateChoiceSceneList () => CreatePrefabCopy("ChoiceHandlers/SceneList", "NewSceneListChoiceHandler");
        [MenuItem("Assets/Create/Naninovel/Choice Handler/Button Area", priority = 103)]
        private static void CreateChoiceButtonArea () => CreatePrefabCopy("ChoiceHandlers/ButtonArea", "NewButtonAreaChoiceHandler");
        [MenuItem("Assets/Create/Naninovel/Choice Handler/Chat Reply", priority = 103)]
        private static void CreateChoiceChatReply () => CreatePrefabCopy("ChoiceHandlers/ChatReply", "NewChatReplyChoiceHandler");
        [MenuItem("Assets/Create/Naninovel/Choice Handler/Choice Button/Choice Button", priority = 103)]
        private static void CreateChoiceButton () => CreatePrefabCopy("ChoiceHandlers/ChoiceButton", "NewChoiceButton");
        [MenuItem("Assets/Create/Naninovel/Choice Handler/Choice Button/Scene List Button", priority = 103)]
        private static void CreateChoiceSceneListButton () => CreatePrefabCopy("ChoiceHandlers/SceneListButton", "NewSceneListChoiceButton");

        [MenuItem("Assets/Create/Naninovel/Default UI/BacklogUI")]
        private static void CreateBacklogUI () => CreatePrefabCopy("DefaultUI/BacklogUI", "NewBacklogUI");
        [MenuItem("Assets/Create/Naninovel/Default UI/CGGalleryUI")]
        private static void CreateCGGalleryUI () => CreatePrefabCopy("DefaultUI/CGGalleryUI", "NewCGGalleryUI");
        [MenuItem("Assets/Create/Naninovel/Default UI/ConfirmationUI")]
        private static void CreateConfirmationUI () => CreatePrefabCopy("DefaultUI/ConfirmationUI", "NewConfirmationUI");
        [MenuItem("Assets/Create/Naninovel/Default UI/ExternalScriptsUI")]
        private static void CreateExternalScriptsUI () => CreatePrefabCopy("DefaultUI/ExternalScriptsUI", "NewExternalScriptsUI");
        [MenuItem("Assets/Create/Naninovel/Default UI/LoadingUI")]
        private static void CreateLoadingUI () => CreatePrefabCopy("DefaultUI/LoadingUI", "NewLoadingUI");
        [MenuItem("Assets/Create/Naninovel/Default UI/MovieUI")]
        private static void CreateMovieUI () => CreatePrefabCopy("DefaultUI/MovieUI", "NewMovieUI");
        [MenuItem("Assets/Create/Naninovel/Default UI/PauseUI")]
        private static void CreatePauseUI () => CreatePrefabCopy("DefaultUI/PauseUI", "NewPauseUI");
        [MenuItem("Assets/Create/Naninovel/Default UI/RollbackUI")]
        private static void CreateRollbackUI () => CreatePrefabCopy("DefaultUI/RollbackUI", "NewRollbackUI");
        [MenuItem("Assets/Create/Naninovel/Default UI/SaveLoadUI")]
        private static void CreateSaveLoadUI () => CreatePrefabCopy("DefaultUI/SaveLoadUI", "NewSaveLoadUI");
        [MenuItem("Assets/Create/Naninovel/Default UI/SettingsUI")]
        private static void CreateSettingsUI () => CreatePrefabCopy("DefaultUI/SettingsUI", "NewSettingsUI");
        [MenuItem("Assets/Create/Naninovel/Default UI/TipsUI")]
        private static void CreateTipsUI () => CreatePrefabCopy("DefaultUI/TipsUI", "NewTipsUI");
        [MenuItem("Assets/Create/Naninovel/Default UI/TitleUI")]
        private static void CreateTitleUI () => CreatePrefabCopy("DefaultUI/TitleUI", "NewTitleUI");
        [MenuItem("Assets/Create/Naninovel/Default UI/ToastUI")]
        private static void CreateToastUI () => CreatePrefabCopy("DefaultUI/ToastUI", "NewToastUI");
        [MenuItem("Assets/Create/Naninovel/Default UI/VariableInputUI")]
        private static void CreateVariableInputUI () => CreatePrefabCopy("DefaultUI/VariableInputUI", "NewVariableInputUI");
        [MenuItem("Assets/Create/Naninovel/Default UI/ScriptNavigatorUI")]
        private static void CreateScriptNavigatorUI () => CreatePrefabCopy("DefaultUI/ScriptNavigatorUI", "NewScriptNavigatorUI");

        [MenuItem("GameObject/Naninovel/Dialogue", false, 10)]
        private static void CreateDialogue ()
        {
            var path = PathUtils.Combine(PackagePath.PrefabsPath, "Dialogue/Dialogue.prefab");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var obj = Selection.activeTransform ? Object.Instantiate(prefab, Selection.activeTransform) : Object.Instantiate(prefab);
            obj.name = prefab.name;
            Undo.RegisterCreatedObjectUndo(obj, "Create Naninovel Dialogue");
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/Naninovel/Character", false, 100)]
        private static void CreateSceneCharacter ()
        {
            var obj = new GameObject("Character", typeof(TransientCharacter));
            if (Selection.activeTransform) obj.transform.SetParent(Selection.activeTransform, false);
            Undo.RegisterCreatedObjectUndo(obj, "Create Naninovel Character");
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/Naninovel/Background", false, 100)]
        private static void CreateSceneBackground ()
        {
            var obj = new GameObject("Background", typeof(TransientBackground));
            if (Selection.activeTransform) obj.transform.SetParent(Selection.activeTransform, false);
            Undo.RegisterCreatedObjectUndo(obj, "Create Naninovel Background");
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/Naninovel/Text Printer", false, 100)]
        private static void CreateSceneTextPrinter ()
        {
            var obj = new GameObject("Printer");
            obj.transform.localScale = Vector3.one * .005f;
            if (Selection.activeTransform) obj.transform.SetParent(Selection.activeTransform, false);
            var printer = obj.AddComponent<TransientUITextPrinter>();
            var path = PathUtils.Combine(PackagePath.PrefabsPath, "TextPrinters/SceneBubble.prefab");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var ui = Object.Instantiate(prefab, obj.transform);
            ui.name = prefab.name;
            typeof(TransientUITextPrinter).GetProperty(nameof(TransientUITextPrinter.PrinterPanel))!
                .SetValue(printer, ui.GetComponent<UITextPrinterPanel>());
            Undo.RegisterCreatedObjectUndo(obj, "Create Naninovel Text Printer");
            Selection.activeGameObject = obj;
        }

        [MenuItem("GameObject/Naninovel/Choice Handler", false, 100)]
        private static void CreateSceneChoiceHandler ()
        {
            var obj = new GameObject("ChoiceHandler");
            obj.transform.localScale = Vector3.one * .005f;
            if (Selection.activeTransform) obj.transform.SetParent(Selection.activeTransform, false);
            var printer = obj.AddComponent<TransientUIChoiceHandler>();
            var path = PathUtils.Combine(PackagePath.PrefabsPath, "ChoiceHandlers/SceneList.prefab");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var ui = Object.Instantiate(prefab, obj.transform);
            ui.name = prefab.name;
            typeof(TransientUIChoiceHandler).GetProperty(nameof(TransientUIChoiceHandler.ChoiceHandlerPanel))!
                .SetValue(printer, ui.GetComponent<ChoiceHandlerPanel>());
            Undo.RegisterCreatedObjectUndo(obj, "Create Naninovel Choice Handler");
            Selection.activeGameObject = obj;
        }
    }
}

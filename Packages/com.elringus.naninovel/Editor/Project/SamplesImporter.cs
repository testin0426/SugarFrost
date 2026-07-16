using System.IO;
using UnityEditor;

namespace Naninovel
{
    /// <summary>
    /// Watches for Samples~ UPM import and offers a setup.
    /// </summary>
    public sealed class SamplesImporter : AssetPostprocessor
    {
        private void OnPreprocessAsset ()
        {
            if (!assetImporter.importSettingsMissing) return; // only trigger on initial import
            if (!assetPath.StartsWithOrdinal("Assets/Samples/Naninovel/")) return;
            if (assetPath.EndsWithOrdinal("/Visual Novel"))
                EditorApplication.delayCall += () => SetupVisualNovel(assetPath);
            else if (assetPath.EndsWithOrdinal("/Dialogue System"))
                EditorApplication.delayCall += () => SetupDialogueSystem(assetPath);
        }

        private static void SetupVisualNovel (string sampleRoot)
        {
            if (!EditorUtility.DisplayDialog("Set Up Visual Novel Sample?",
                    $"Proceeding will import sample scenario files into '{PackagePath.ScenarioRoot}' (OVERWRITING EXISTING FILES) and override 'Scripts Configuration'. " +
                    "Note that the sample setup assumes the default Naninovel configuration and UIs (Minimal Mode is not supported).\n\n" +
                    "After the setup is complete, run the sample from the scene located in the 'Samples/Naninovel/.../Visual Novel' directory.",
                    "Proceed", "Cancel"))
            {
                Directory.Delete(sampleRoot, true);
                File.Delete($"{sampleRoot}.meta");
                AssetDatabase.Refresh();
                return;
            }

            var bgs = ConfigurationSettings.LoadOrDefaultAndSave<BackgroundsConfiguration>();
            var mainBg = bgs.GetMetadataOrDefault(BackgroundsConfiguration.MainActorId);
            mainBg.Implementation = typeof(PlaceholderBackground).AssemblyQualifiedName;
            BackgroundMetadataEditor.EnsureDefaultPlaceholderAppearanceAdded(mainBg);
            EditorUtility.SetDirty(bgs);

            var chars = ConfigurationSettings.LoadOrDefaultAndSave<CharactersConfiguration>();
            foreach (var guid in AssetDatabase.FindAssets("t:Naninovel.CharacterRecord"))
                if (AssetDatabase.LoadAssetAtPath<CharacterRecord>(AssetDatabase.GUIDToAssetPath(guid)) is { } rec)
                    chars.Metadata[rec.ActorId] = rec.Metadata;
            EditorUtility.SetDirty(chars);

            ScaffoldScripts(sampleRoot);
            PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            // When Naninovel is initially imported, it scaffolds Entry and Title scrips.
            // If we then override them with the sample files, story editor breaks, so force-reinit it.
            StoryEditor.StoryEditor.Deinitialize();
        }

        private static void SetupDialogueSystem (string sampleRoot)
        {
            if (!EditorUtility.DisplayDialog("Set Up Dialogue System Sample?",
                    $"Proceeding will install the required UPM packages, import sample scenario files into '{PackagePath.ScenarioRoot}' (OVERWRITING EXISTING FILES) " +
                    "and enable 'Minimal Mode', which will modify Naninovel configurations, remove most built-in UIs, " +
                    "and disable certain features to optimize the engine for use as a drop-in dialogue system.\n\n" +
                    "After the setup is complete, run the sample from the scene located in the 'Samples/Naninovel/.../Dialogue System' directory.",
                    "Proceed", "Cancel"))
            {
                Directory.Delete(sampleRoot, true);
                File.Delete($"{sampleRoot}.meta");
                AssetDatabase.Refresh();
                return;
            }

            // Otherwise, the story editor breaks after importing the dialogue samples in a new project.
            StoryEditor.Window.CloseAll();

            ScaffoldScripts(sampleRoot);
            SetupMinimalMode.Execute();
            PlayerSettings.runInBackground = true;
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

            PackageInstaller.Install(new[] {
                "com.unity.modules.physics",
                "com.unity.inputsystem",
                "com.unity.timeline",
                "com.unity.cinemachine"
            }, StoryEditor.Window.DockWithInspector);
        }

        private static void ScaffoldScripts (string sampleRoot)
        {
            var resolver = new ScriptPathResolver { RootUri = PackagePath.ScenarioRoot };
            var sampleDir = $"{sampleRoot}/Scenario~";
            IOUtils.CopyDirectory(sampleDir, PackagePath.ScenarioRoot);
            Directory.Delete(sampleDir, true);
            foreach (var filePath in Directory.GetFiles(PackagePath.ScenarioRoot, "*.nani", SearchOption.AllDirectories))
            {
                var assetPath = filePath.StartsWithOrdinal("Assets") ?
                    PathUtils.FormatPath(filePath) : PathUtils.AbsoluteToAssetPath(filePath);
                AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                if (AssetDatabase.AssetPathToGUID(assetPath) is { Length: > 0 } guid)
                    Assets.Register(new(guid, ScriptsConfiguration.DefaultPathPrefix, resolver.Resolve(assetPath)));
                else Engine.Err($"Failed to import '{assetPath}' sample script. Import the script manually (right-click -> 'Reimport').");
            }
        }
    }
}

using System;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Naninovel
{
    [CustomEditor(typeof(EngineVersion))]
    public class EngineVersionEditor : Editor
    {
        protected static string GitHubProjectPath => PlayerPrefs.GetString(nameof(GitHubProjectPath), string.Empty);

        private const string packageTextTemplate = @"{
    ""name"": ""com.elringus.naninovel"",
    ""version"": ""{PACKAGE_VERSION}"",
    ""displayName"": ""Naninovel"",
    ""description"": ""Create visual novels, branching dialogues, and interactive cutscenes with an all-in-one suite of writer-friendly storytelling tools."",
    ""unity"": ""6000.0"",
    ""author"": { ""name"": ""Elringus"", ""url"": ""https://naninovel.com"" },
    ""licensesUrl"": ""https://naninovel.com/eula"",
    ""documentationUrl"": ""https://naninovel.com/guide"",
    ""changelogUrl"": ""https://naninovel.com/releases/{GIT_VERSION}"",
    ""keywords"": [
        ""visual novel engine"",
        ""VN engine"",
        ""dialogue system"",
        ""cutscene system"",
        ""interactive fiction"",
        ""branching narrative"",
        ""conversation"",
        ""speech bubble"",
        ""dating sim"",
        ""text adventure"",
        ""RPG dialogue"",
        ""story game"",
        ""choice"",
        ""cinematic"",
        ""anime""
    ],
    ""dependencies"": {
        ""com.unity.modules.audio"": ""1.0.0"",
        ""com.unity.modules.video"": ""1.0.0"",
        ""com.unity.modules.imgui"": ""1.0.0"",
        ""com.unity.modules.imageconversion"": ""1.0.0"",
        ""com.unity.modules.uielements"": ""1.0.0"",
        ""com.unity.modules.particlesystem"": ""1.0.0"",
        ""com.unity.ugui"": ""2.0.0"",
        ""com.unity.nuget.newtonsoft-json"": ""3.2.2""
    },
    ""samples"": [
        {
            ""displayName"": ""Visual Novel"",
            ""description"": ""A visual novel scenario template with multiple routes"",
            ""path"": ""Samples~/VisualNovel""
        },
        {
            ""displayName"": ""Dialogue System"",
            ""description"": ""An example on using Naninovel as a drop-in dialogue (cutscene) system"",
            ""path"": ""Samples~/DialogueSystem""
        }
    ]
}
";

        public override void OnInspectorGUI ()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Update", GUIStyles.NavigationButton))
                Update();
        }

        public static void Update ()
        {
            var asset = EngineVersion.LoadFromResources();
            using var serializedObj = new SerializedObject(asset);
            serializedObj.Update();
            var engineVersionProperty = serializedObj.FindProperty("engineVersion");
            var buildDateProperty = serializedObj.FindProperty("buildDate");
            buildDateProperty.stringValue = $"{DateTime.Now:yyyy-MM-dd}";
            serializedObj.ApplyModifiedProperties();

            var gitVersion = engineVersionProperty.stringValue;
            var packageText = packageTextTemplate
                .Replace("{GIT_VERSION}", gitVersion)
                .Replace("{PACKAGE_VERSION}", asset.BuildVersionTag());
            var packagePath = PathUtils.Combine(PackagePath.PackageRootPath, "package.json");
            File.WriteAllText(packagePath, packageText);

            EditorUtility.SetDirty(asset);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        private static DateTime ParseBuildDate (string buildDate)
        {
            var parsed = DateTime.TryParseExact(buildDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result);
            return parsed ? result : DateTime.MinValue;
        }
    }
}

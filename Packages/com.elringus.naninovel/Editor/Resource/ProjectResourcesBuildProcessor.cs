using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace Naninovel
{
    public class ProjectResourcesBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public static string TempFolderPath = "Assets/TEMP_NANINOVEL/Resources";

        public int callbackOrder => 100;

        private static string assetPath => $"{TempFolderPath}/{ProjectResources.AssetPath}.asset";

        public void OnPreprocessBuild (BuildReport report)
        {
            var asset = ProjectResources.Collect();
            EditorUtils.CreateFolderAsset(assetPath.GetBeforeLast("/"));
            AssetDatabase.CreateAsset(asset, assetPath);
            AssetDatabase.SaveAssets();
        }

        public void OnPostprocessBuild (BuildReport report)
        {
            AssetDatabase.DeleteAsset(TempFolderPath.GetBeforeLast("/"));
            AssetDatabase.SaveAssets();
        }
    }
}

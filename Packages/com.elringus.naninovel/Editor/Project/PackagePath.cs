using System.IO;
using System.Linq;
using JetBrains.Annotations;
using UnityEngine;

namespace Naninovel
{
    /// <summary>
    /// Provides paths to various package-related directories and resources; all paths are relative to project root.
    /// </summary>
    public static class PackagePath
    {
        public static string PackageRootPath => GetPackageRootPath();
        public static string EditorResourcesPath => PathUtils.Combine(PackageRootPath, "Editor/Resources/Naninovel");
        public static string RuntimeResourcesPath => PathUtils.Combine(PackageRootPath, "Resources/Naninovel");
        public static string PrefabsPath => PathUtils.Combine(PackageRootPath, "Prefabs");
        public static string GeneratedDataPath => GetGeneratedDataPath();
        public static string GeneratedResourcesPath => PathUtils.Combine(GeneratedDataPath, "Resources/Naninovel");
        public static string EditorDataPath => cachedEditorDataPath ??= Ensure(PathUtils.Combine(GeneratedDataPath, ".nani"));
        public static string TransientDataPath => cachedTransientDataPath ??= Ensure(PathUtils.Combine(EditorDataPath, "Transient"));
        public static string TransientAssetPath => cachedTransientAssetPath ??= Ensure(PathUtils.Combine(GeneratedDataPath, "Transient"));
        public static string ScenarioRoot => GetScenarioRoot();

        private static string cachedDataPath;
        private static string cachedEditorDataPath;
        private static string cachedTransientDataPath;
        private static string cachedTransientAssetPath;
        private static string cachedPackagePath;
        private static string cachedScenarioPath;

        /// <summary>
        /// Resets the cached paths and resolves them again.
        /// </summary>
        public static void Refresh ()
        {
            cachedDataPath = null;
            cachedEditorDataPath = null;
            cachedTransientDataPath = null;
            cachedTransientAssetPath = null;
            cachedPackagePath = null;
            cachedScenarioPath = null;
            _ = PackageRootPath;
            _ = GeneratedDataPath;
            _ = TransientDataPath;
            _ = TransientAssetPath;
            _ = ScenarioRoot;
        }

        private static string GetPackageRootPath ()
        {
            const string beacon = "Elringus.Naninovel.Editor.asmdef";
            if (string.IsNullOrEmpty(cachedPackagePath) || !Directory.Exists(cachedPackagePath))
                cachedPackagePath = FindInPackages() ?? FindInAssets();
            return cachedPackagePath ?? throw new Error("Failed to locate Naninovel package directory.");

            [CanBeNull]
            static string FindInPackages ()
            {
                // Even when package is installed as immutable (eg, local or git) and only physically
                // exists under Library/PackageCache/…, Unity will still symlink it to Packages/….
                const string dir = "Packages/com.elringus.naninovel";
                return Directory.Exists(dir) ? dir : null;
            }

            [CanBeNull]
            static string FindInAssets ()
            {
                var options = new EnumerationOptions {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = true,
                    ReturnSpecialDirectories = false,
                    AttributesToSkip = FileAttributes.System
                };
                foreach (var path in Directory.EnumerateFiles(Application.dataPath, beacon, options))
                    return PathUtils.AbsoluteToAssetPath(Path.GetDirectoryName(Path.GetDirectoryName(path)));
                return null;
            }
        }

        private static string GetGeneratedDataPath ()
        {
            const string beacon = ".naninovel.unity.data";
            if (string.IsNullOrEmpty(cachedDataPath) || !Directory.Exists(cachedDataPath))
                cachedDataPath = FindInAssets();
            if (!string.IsNullOrEmpty(cachedDataPath)) return cachedDataPath;
            const string defaultDir = "Assets/NaninovelData";
            const string defaultFile = defaultDir + "/" + beacon;
            Directory.CreateDirectory(defaultDir);
            File.WriteAllText(defaultFile, "");
            return cachedDataPath = defaultDir;

            [CanBeNull]
            static string FindInAssets ()
            {
                var options = new EnumerationOptions {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = true,
                    ReturnSpecialDirectories = false,
                    AttributesToSkip = FileAttributes.System
                };
                foreach (var path in Directory.EnumerateFiles(Application.dataPath, beacon, options))
                    return PathUtils.AbsoluteToAssetPath(Path.GetDirectoryName(path));
                return null;
            }
        }

        private static string GetScenarioRoot ()
        {
            if (string.IsNullOrEmpty(cachedScenarioPath) || !Directory.Exists(cachedScenarioPath))
                cachedScenarioPath = FindInAssets();
            if (!string.IsNullOrEmpty(cachedScenarioPath)) return cachedScenarioPath;
            const string defaultDir = "Assets/Scenario";
            Directory.CreateDirectory(defaultDir);
            return cachedScenarioPath = defaultDir;

            [CanBeNull]
            static string FindInAssets ()
            {
                var options = new EnumerationOptions {
                    IgnoreInaccessible = true,
                    RecurseSubdirectories = true,
                    ReturnSpecialDirectories = false,
                    AttributesToSkip = FileAttributes.System
                };
                var files = Directory.EnumerateFiles(Application.dataPath, "*.nani", options).Where(p => !p.Contains("Scenario~"));
                return ScenarioRootResolver.Resolve(files) is { } root ? PathUtils.AbsoluteToAssetPath(root) : null;
            }
        }

        private static string Ensure (string dir)
        {
            Directory.CreateDirectory(dir);
            return dir;
        }
    }
}

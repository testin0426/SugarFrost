using System;
using System.IO;
using System.Text;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Naninovel
{
    [ScriptedImporter(version: 65, ext: "nani")]
    public class ScriptImporter : ScriptedImporter
    {
        public override void OnImportAsset (AssetImportContext ctx)
        {
            try
            {
                var cfg = Configuration.GetOrDefault<ScriptsConfiguration>();
                var assetPath = ctx.assetPath;
                var assetBytes = File.ReadAllBytes(assetPath);
                var scriptText = Encoding.UTF8.GetString(assetBytes, 0, assetBytes.Length);
                PurgeBom(assetPath, scriptText);

                var root = BuildProcessor.Building
                    ? Path.Combine(ResourcesBuilder.TempResourcesPath, "Naninovel", cfg.Loader.PathPrefix)
                    : PackagePath.ScenarioRoot;
                var pathResolver = new ScriptPathResolver { RootUri = root };
                var scriptPath = pathResolver.Resolve(assetPath);
                var script = Compiler.CompileScript(scriptPath, scriptText, assetPath);
                script.name = Path.GetFileNameWithoutExtension(assetPath);
                script.hideFlags = HideFlags.NotEditable;
                ctx.AddObjectToAsset("naniscript", script);
                ctx.SetMainObject(script);
            }
            catch (Exception e) { ctx.LogImportError($"Failed to import scenario script: {e}"); }
        }

        // Unity auto adding BOM when creating script assets: https://git.io/fjVgY
        private static void PurgeBom (string assetPath, string contents)
        {
            if (contents.Length > 0 && contents[0] == '\uFEFF')
                File.WriteAllText(assetPath, contents[1..]);
        }
    }
}

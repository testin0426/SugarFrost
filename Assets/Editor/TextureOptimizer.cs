using UnityEditor;
using UnityEngine;

// One-time helper: bulk-fixes texture import settings to use GPU block
// compression (BC7/DXT) instead of shipping uncompressed RGBA32.
// Select one or more folders (or textures) in the Project window, then run
// the menu item. Normal maps and large textures are handled sensibly.
public static class TextureOptimizer
{
    [MenuItem("Tools/Optimize Selected Textures")]
    private static void OptimizeSelected()
    {
        var guids = Selection.assetGUIDs;
        int changed = 0;

        var texGuids = AssetDatabase.FindAssets("t:Texture2D",
            System.Array.ConvertAll(guids, AssetDatabase.GUIDToAssetPath));

        try
        {
            for (int i = 0; i < texGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(texGuids[i]);
                EditorUtility.DisplayProgressBar("Optimizing textures",
                    path, (float)i / texGuids.Length);

                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                bool dirty = false;

                if (importer.textureCompression != TextureImporterCompression.Compressed)
                {
                    importer.textureCompression = TextureImporterCompression.Compressed;
                    dirty = true;
                }

                // Cap source resolution. Backgrounds/CGs rarely need >2048.
                if (importer.maxTextureSize > 2048)
                {
                    importer.maxTextureSize = 2048;
                    dirty = true;
                }

                // Crunch gives extra on-disk shrink for albedo/color maps.
                // Skip it for normal maps (hurts quality there).
                bool isNormal = importer.textureType == TextureImporterType.NormalMap;
                if (!isNormal && !importer.crunchedCompression)
                {
                    importer.crunchedCompression = true;
                    importer.compressionQuality = 50;
                    dirty = true;
                }

                if (dirty)
                {
                    EditorUtility.SetDirty(importer);
                    importer.SaveAndReimport();
                    changed++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        Debug.Log($"TextureOptimizer: reimported {changed} of {texGuids.Length} textures.");
    }
}

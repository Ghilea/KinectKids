using System.IO;
using UnityEditor;
using UnityEngine;

namespace KinectKids3D.Editor
{
    /// <summary>
    /// Forces every PNG under the Greve Gast chase Resources folder to import as
    /// a single Sprite with the project's standard settings (280 px/unit, custom
    /// bottom-centre pivot, alpha transparency, no mipmaps, uncompressed).
    ///
    /// This is the reliable, editor-driven way to guarantee
    /// <c>Resources.Load&lt;Sprite&gt;</c> returns the cut chase art. Hand-authored
    /// .meta files are not enough on their own, so the platform build calls this.
    /// </summary>
    public static class GreveChaseSpriteImporter
    {
        private const string ChaseResources =
            "Assets/KinectKids/Games/GreveGast/Resources/GreveChase";

        [MenuItem("KinectKids/Greve Gast/Importera chase-sprites")]
        public static void ImportAll()
        {
            if (!AssetDatabase.IsValidFolder(ChaseResources))
            {
                Debug.LogWarning("Chase-sprites saknas: " + ChaseResources);
                return;
            }

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ChaseResources });
            int configured = 0;
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase)) continue;

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                // Already configured? Skip to avoid needless reimports each build.
                if (importer.textureType == TextureImporterType.Sprite
                    && importer.spriteImportMode == SpriteImportMode.Single
                    && Mathf.Approximately(importer.spritePixelsPerUnit, 280f)
                    && !importer.mipmapEnabled)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 280f;

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(0.5f, 0f);
                importer.SetTextureSettings(settings);

                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.maxTextureSize = 2048;

                importer.SaveAndReimport();
                configured++;
            }

            Debug.Log($"Chase-sprites: {configured} omkonfigurerade som Sprite (av {guids.Length}).");
        }
    }
}

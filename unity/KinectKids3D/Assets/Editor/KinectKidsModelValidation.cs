using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KinectKids3D.Editor
{
    internal static class KinectKidsModelValidation
    {
        [MenuItem("KinectKids/Validera importerade 3D-modeller")]
        public static void Validate()
        {
            ValidateModel("Models/Quaternius/Bat", true, false);
            ValidateModel("Models/Quaternius/Ghost", true, true);
            ValidateModel("Models/Quaternius/Spider", true, false);
            ValidateModel("Models/KayKit/torch_mounted", false, true);
            ValidateModel("Models/KayKit/wall_doorway", false, true);
            ValidateModel("Models/QuaterniusKnight/KnightCharacter", true, false);
            ValidateModel("Models/CuteMonsters/Cthulhu", true, true);
            ValidateModel("Models/CuteMonsters/Demon", true, true);
            ValidateModel("Models/CuteMonsters/Ghost", true, true);
            ValidateModel("Models/CuteMonsters/Skull", true, true);
            ValidateModel("Models/KenneyGraveyard/character-ghost", true, true);
            ValidateModel("Models/KenneyGraveyard/character-skeleton", true, true);
            ValidateModel("Models/KenneyGraveyard/character-vampire", true, true);
            ValidateModel("Models/KenneyGraveyard/character-zombie", true, true);
            ValidateModel("Models/KenneyGraveyard/crypt-door", false, true);
            ValidateModel("Models/KenneyGraveyard/gravestone-broken", false, true);
            Debug.Log("Alla importerade 3D-modeller och animationer är redo.");
        }

        public static void ValidateBatch()
        {
            try
            {
                Validate();
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorApplication.Exit(1);
            }
        }

        private static void ValidateModel(string resourcePath, bool needsAnimation, bool needsTexture)
        {
            GameObject model = Resources.Load<GameObject>(resourcePath);
            if (model == null) throw new InvalidOperationException("Modellen saknas: " + resourcePath);
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                throw new InvalidOperationException("Modellen har ingen synlig mesh: " + resourcePath);
            Material[] materials = renderers.SelectMany(renderer => renderer.sharedMaterials).ToArray();
            if (materials.Any(material => material == null))
                throw new InvalidOperationException("Modellen saknar material: " + resourcePath);
            Texture2D externalTexture = null;
            if (resourcePath.StartsWith("Models/CuteMonsters/", StringComparison.Ordinal))
            {
                string modelName = resourcePath.Substring(resourcePath.LastIndexOf('/') + 1);
                externalTexture = Resources.Load<Texture2D>(
                    "Models/CuteMonsters/Textures/" + modelName + "_Texture");
            }
            else if (resourcePath.StartsWith("Models/KenneyGraveyard/", StringComparison.Ordinal))
                externalTexture = Resources.Load<Texture2D>("Models/KenneyGraveyard/Textures/colormap");
            if (needsTexture && !materials.Any(material => material.mainTexture != null) && externalTexture == null)
                throw new InvalidOperationException("Modellen saknar sin textur: " + resourcePath);

            AnimationClip[] clips = Resources.LoadAll<AnimationClip>(resourcePath)
                .Where(clip => !clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            if (needsAnimation && clips.Length == 0)
                throw new InvalidOperationException("Modellen saknar animationer: " + resourcePath);

            Debug.Log(resourcePath + " | animationer: " + clips.Length + " | " +
                string.Join(", ", clips.Select(clip => clip.name)));
        }
    }
}

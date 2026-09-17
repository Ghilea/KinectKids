using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KinectKids3D.Editor
{
    public static class KinectKidsAudioValidation
    {
        private static readonly string[] RequiredClips =
        {
            "Audio/SFX/Ambience/ambient_horror",
            "Audio/SFX/Creatures/bat_wings",
            "Audio/SFX/Doors/door_creak_open",
            "Audio/SFX/Doors/door_open",
            "Audio/SFX/Doors/grind_stone",
            "Audio/SFX/Environment/chain_rattle",
            "Audio/SFX/Environment/metal_clank",
            "Audio/SFX/Environment/success_bell",
            "Audio/SFX/Impacts/horror_bass_01",
            "Audio/SFX/Impacts/horror_high_01"
        };

        [MenuItem("KinectKids/Validera ljudbibliotek")]
        public static void Validate()
        {
            string[] missing = RequiredClips
                .Where(path => Resources.Load<AudioClip>(path) == null)
                .ToArray();
            int magicCount = Resources.LoadAll<AudioClip>("Audio/SFX/Magic").Length;
            int ghostCount = Resources.LoadAll<AudioClip>("Audio/SFX/Creatures")
                .Count(clip => clip.name.StartsWith("ghost_moan_", StringComparison.OrdinalIgnoreCase));
            int hitCount = Resources.LoadAll<AudioClip>("Audio/SFX/Impacts")
                .Count(clip => clip.name.StartsWith("hit_", StringComparison.OrdinalIgnoreCase));

            if (missing.Length > 0 || magicCount < 5 || ghostCount < 5 || hitCount < 4)
                throw new InvalidOperationException("Ljudvalidering misslyckades. Saknas: "
                    + string.Join(", ", missing) + $" | magi={magicCount}, spöken={ghostCount}, träffar={hitCount}");

            Debug.Log($"Ljudvalidering OK: {magicCount} magiljud, {ghostCount} spökröster, "
                + $"{hitCount} träffljud och alla obligatoriska miljöljud hittades.");
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
    }
}

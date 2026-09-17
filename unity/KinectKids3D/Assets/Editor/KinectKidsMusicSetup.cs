using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace KinectKids3D.Editor
{
    [InitializeOnLoad]
    internal static class KinectKidsMusicSetup
    {
        private const long ExpectedBytes = 1945137;
        private const string AssetPath = "Assets/Resources/Audio/RideMusic.ogg";
        private static readonly string[] SupportedExtensions = { ".ogg", ".mp3", ".wav", ".aiff", ".aif" };

        static KinectKidsMusicSetup()
        {
            EditorApplication.delayCall += EnsureMusicAsset;
        }

        internal static void EnsureMusicAsset()
        {
            // En ForceUpdate medan Play-läget startar kopplar loss det AudioClip som
            // redan spelas. Installera därför bara musik utanför Play-läget.
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (InstallUserMusic()) return;
            string target = Path.Combine(Application.dataPath, "Resources", "Audio", "RideMusic.ogg");
            if (File.Exists(target) && new FileInfo(target).Length == ExpectedBytes) return;

            string sourceFolder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "MusicSource"));
            string[] parts = Directory.Exists(sourceFolder)
                ? Directory.GetFiles(sourceFolder, "RideMusic.base64.part*").OrderBy(path => path).ToArray()
                : new string[0];
            if (parts.Length == 0)
            {
                Debug.LogError("Musikkällan saknas i MusicSource. Procedurmusiken används som reserv.");
                return;
            }

            try
            {
                string encoded = string.Concat(parts.Select(File.ReadAllText));
                byte[] bytes = Convert.FromBase64String(encoded);
                if (bytes.LongLength != ExpectedBytes)
                    throw new InvalidDataException("Musikkällan har fel storlek: " + bytes.LongLength);
                Directory.CreateDirectory(Path.GetDirectoryName(target));
                File.WriteAllBytes(target, bytes);
                AssetDatabase.ImportAsset(AssetPath, ImportAssetOptions.ForceUpdate);
                Debug.Log("Spökjaktens licensierade musik har installerats: " + AssetPath);
            }
            catch (Exception exception)
            {
                Debug.LogError("Musiken kunde inte installeras: " + exception.Message);
            }
        }

        private static bool InstallUserMusic()
        {
            string source = Directory.GetFiles(Application.dataPath, "*", SearchOption.TopDirectoryOnly)
                .Where(path => SupportedExtensions.Contains(Path.GetExtension(path).ToLowerInvariant()))
                .OrderByDescending(File.GetLastWriteTimeUtc)
                .FirstOrDefault();
            if (string.IsNullOrEmpty(source)) return false;

            try
            {
                string audioFolder = Path.Combine(Application.dataPath, "Resources", "Audio");
                Directory.CreateDirectory(audioFolder);
                string extension = Path.GetExtension(source).ToLowerInvariant();
                string target = Path.Combine(audioFolder, "CustomRideMusic" + extension);
                foreach (string existing in Directory.GetFiles(audioFolder, "CustomRideMusic.*"))
                    if (!string.Equals(existing, target, StringComparison.OrdinalIgnoreCase)) File.Delete(existing);

                bool needsCopy = !File.Exists(target)
                    || !File.ReadAllBytes(source).SequenceEqual(File.ReadAllBytes(target));
                string assetPath = "Assets/Resources/Audio/CustomRideMusic" + extension;
                if (needsCopy)
                {
                    File.Copy(source, target, true);
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
                    Debug.Log("Ny musik installerades i Spökjakten: " + Path.GetFileName(source));
                }
                else if (AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath) == null)
                {
                    AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
                }
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogError("Den egna musikfilen kunde inte installeras: " + exception.Message);
                return false;
            }
        }
    }
}

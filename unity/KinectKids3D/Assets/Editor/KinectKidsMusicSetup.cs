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

        static KinectKidsMusicSetup()
        {
            EditorApplication.delayCall += EnsureMusicAsset;
        }

        internal static void EnsureMusicAsset()
        {
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
    }
}

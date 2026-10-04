// The tests exercise the production scheduling code without starting Unity.
namespace UnityEngine
{
    public class ScriptableObject { }
    public class CreateAssetMenuAttribute : System.Attribute
    {
        public string menuName;
        public string fileName;
    }
    public class AudioClip
    {
        public int frequency = 48000;
        public int samples = 480000;
        public float length => (float)samples / frequency;
    }
    public class AudioSource
    {
        public AudioClip clip;
        public bool loop;
        public bool isPlaying;
        public int timeSamples;
        public void Play() => isPlaying = true;
        public void Pause() => isPlaying = false;
        public void UnPause() => isPlaying = true;
        public void Stop() { isPlaying = false; timeSamples = 0; }
    }
    public static class Mathf
    {
        public static int Clamp(int value, int min, int max) => System.Math.Clamp(value, min, max);
        public static float Clamp(float value, float min, float max) => System.Math.Clamp(value, min, max);
    }
}

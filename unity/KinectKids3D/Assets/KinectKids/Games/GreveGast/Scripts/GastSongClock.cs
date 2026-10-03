using UnityEngine;

namespace KinectKids.Games.GreveGast
{
    /// <summary>Song position follows the playing clip's sample cursor, never accumulated frame delta.</summary>
    public sealed class GastSongClock
    {
        private readonly AudioSource source;
        private readonly AudioClip clip;
        private float heldTime;
        private bool running;
        public bool IsRunning => running && source != null && source.isPlaying;
        public float TimeSeconds
        {
            get
            {
                if (source != null && source.clip != null && source.isPlaying)
                    return (float)source.timeSamples / source.clip.frequency;
                return heldTime;
            }
        }

        public GastSongClock(AudioSource audioSource, AudioClip audioClip)
        { source = audioSource; clip = audioClip; }

        public void PlayFrom(float seconds)
        {
            if (source == null || clip == null) return;
            source.clip = clip;
            source.loop = false;
            Seek(seconds);
            source.Play();
            running = true;
        }

        public void Pause()
        {
            if (source == null) return;
            heldTime = TimeSeconds;
            source.Pause();
            running = false;
        }

        public void Resume()
        {
            if (source == null || clip == null) return;
            source.UnPause();
            running = true;
        }

        public void Seek(float seconds)
        {
            heldTime = Mathf.Clamp(seconds, 0f, clip != null ? clip.length : 0f);
            if (source != null && clip != null) source.timeSamples = Mathf.Clamp((int)(heldTime * clip.frequency), 0, clip.samples - 1);
        }

        public void Stop() { if (source != null) source.Stop(); running = false; heldTime = 0f; }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace KinectKids3D
{
    [Serializable] public sealed class GreveGastSection { public string name; public float start; }

    [Serializable]
    public sealed class GreveGastCue
    {
        public string id;
        public float time;
        public float warning;
        public float duration = 1f;
        public string kind;
        public string action;
        public int lane;
    }

    [Serializable]
    public sealed class GreveGastTimelineData
    {
        public string title;
        public float songLength;
        public float gameplayStart = 21.00f;
        public GreveGastSection[] sections = Array.Empty<GreveGastSection>();
        public GreveGastCue[] cues = Array.Empty<GreveGastCue>();
    }

    public sealed class GreveGastSongController : MonoBehaviour
    {
        public event Action<GreveGastCue> Warning;
        public event Action<GreveGastCue> Cue;
        public event Action<GreveGastSection> SectionChanged;

        private AudioSource source;
        private GreveGastTimelineData timeline;
        private readonly HashSet<string> warned = new HashSet<string>();
        private readonly HashSet<string> fired = new HashSet<string>();
        private int sectionIndex = -1;
        private bool begun;

        public AudioSource Source => source;
        public GreveGastTimelineData Timeline => timeline;
        public float SongTime => source != null ? source.time : 0f;
        public bool IsPlaying => source != null && source.isPlaying;
        public float GameplayStartTime => timeline != null ? timeline.gameplayStart : 21.00f;

        public bool Begin()
        {
            TextAsset json = Resources.Load<TextAsset>("GreveGast/GreveGastTimeline");
            AudioClip clip = Resources.Load<AudioClip>("Audio/Music/GreveGastsJakt");
            if (json == null || clip == null)
            {
                Debug.LogError("Greve Gasts tidslinje eller musik saknas i Resources.");
                return false;
            }

            timeline = JsonUtility.FromJson<GreveGastTimelineData>(json.text);
            source = gameObject.AddComponent<AudioSource>();
            source.clip = clip;
            source.loop = false;
            source.playOnAwake = false;
            source.volume = 0.82f;
            source.spatialBlend = 0f;
            source.PlayScheduled(AudioSettings.dspTime + 0.18d);
            begun = true;
            Debug.Log("Greve Gasts musik laddad: " + clip.name +
                      " | langd=" + clip.length.ToString("0.00") +
                      " | cues=" + timeline.cues.Length);
            return true;
        }

        private void Update()
        {
            if (!begun || timeline == null) return;
            float now = SongTime;
            for (int index = 0; index < timeline.sections.Length; index++)
            {
                if (timeline.sections[index].start <= now && index > sectionIndex)
                {
                    sectionIndex = index;
                    SectionChanged?.Invoke(timeline.sections[index]);
                }
            }

            foreach (GreveGastCue cue in timeline.cues)
            {
                // Every lyric cue owns its reaction time. Fast passages can use
                // a short warning while child mode can keep a longer one.
                float warningLead = Mathf.Max(0f, cue.warning);
                if (!warned.Contains(cue.id) && now >= cue.time - warningLead)
                {
                    warned.Add(cue.id);
                    Warning?.Invoke(cue);
                }
                if (!fired.Contains(cue.id) && now >= cue.time)
                {
                    fired.Add(cue.id);
                    Cue?.Invoke(cue);
                }
            }
        }

        public void TogglePause()
        {
            if (source == null) return;
            if (source.isPlaying) source.Pause(); else source.UnPause();
        }

        public void Seek(float seconds)
        {
            if (source == null || source.clip == null) return;
            bool resume = source.isPlaying;
            source.time = Mathf.Clamp(seconds, 0f, source.clip.length - 0.05f);
            warned.Clear();
            fired.Clear();
            sectionIndex = -1;
            if (resume) source.Play();
        }

        public GreveGastCue NextCue()
        {
            if (timeline == null) return null;
            foreach (GreveGastCue cue in timeline.cues)
                if (cue.time > SongTime + 0.05f) return cue;
            return null;
        }
    }
}

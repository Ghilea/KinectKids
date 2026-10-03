using System;
using System.Collections.Generic;
using UnityEngine;

namespace KinectKids.Games.GreveGast
{
    /// <summary>Frame-resilient, once-per-pass cue dispatch. Untimed cues are never scheduled.</summary>
    public sealed class GastCueTimeline
    {
        private readonly GastSongAsset song;
        private readonly HashSet<string> fired = new HashSet<string>();
        private readonly List<KeyValuePair<float, string>> due = new List<KeyValuePair<float, string>>(32);
        public event Action<GastSongCue, string> Cue;

        public GastCueTimeline(GastSongAsset asset) { song = asset; }

        public void Tick(float previousTime, float currentTime)
        {
            if (song == null || song.cues == null || currentTime < previousTime) return;
            due.Clear();
            for (int i = 0; i < song.cues.Count; i++)
            {
                GastSongCue cue = song.cues[i];
                if (cue == null || !cue.hasVocalTime || cue.timingStatus == GastTimingStatus.Untimed || string.IsNullOrEmpty(cue.id)) continue;
                float vocal = cue.EffectiveTime(song);
                Add(due, cue, "gesture", vocal - cue.gestureLeadSeconds, previousTime, currentTime);
                if (cue.gameplayCommand)
                {
                    Add(due, cue, "warning", vocal - cue.warningLeadSeconds, previousTime, currentTime);
                    Add(due, cue, "window-open", vocal + cue.impactOffsetSeconds - cue.windowBeforeSeconds, previousTime, currentTime);
                    Add(due, cue, "impact", vocal + cue.impactOffsetSeconds, previousTime, currentTime);
                    Add(due, cue, "result", vocal + cue.impactOffsetSeconds + cue.windowAfterSeconds, previousTime, currentTime);
                }
            }
            due.Sort((a, b) =>
            {
                int timeOrder = a.Key.CompareTo(b.Key);
                return timeOrder != 0 ? timeOrder : string.CompareOrdinal(a.Value, b.Value);
            });
            for (int i = 0; i < due.Count; i++)
            {
                int split = due[i].Value.LastIndexOf('|');
                string cueId = due[i].Value.Substring(0, split);
                string phase = due[i].Value.Substring(split + 1);
                if (!fired.Add(due[i].Value)) continue;
                GastSongCue cue = Find(cueId);
                if (cue != null) Cue?.Invoke(cue, phase);
            }
            due.Clear();
        }

        private GastSongCue Find(string id)
        { for (int i = 0; i < song.cues.Count; i++) if (song.cues[i] != null && song.cues[i].id == id) return song.cues[i]; return null; }

        private void Add(List<KeyValuePair<float, string>> list, GastSongCue cue, string phase, float time, float from, float to)
        {
            string eventId = cue.id + "|" + phase;
            if (time >= from && time <= to && !fired.Contains(eventId)) list.Add(new KeyValuePair<float, string>(time, eventId));
        }

        public void Reset() { fired.Clear(); }
        public void RestoreAt(float songTime)
        {
            fired.Clear();
            if (song == null || song.cues == null) return;
            for (int i = 0; i < song.cues.Count; i++)
            {
                GastSongCue cue = song.cues[i];
                if (cue == null || !cue.hasVocalTime || cue.timingStatus == GastTimingStatus.Untimed) continue;
                float t = cue.EffectiveTime(song);
                if (t - cue.gestureLeadSeconds < songTime) fired.Add(cue.id + "|gesture");
                if (cue.gameplayCommand)
                {
                    if (t - cue.warningLeadSeconds < songTime) fired.Add(cue.id + "|warning");
                    if (t + cue.impactOffsetSeconds - cue.windowBeforeSeconds < songTime) fired.Add(cue.id + "|window-open");
                    if (t + cue.impactOffsetSeconds < songTime) fired.Add(cue.id + "|impact");
                    if (t + cue.impactOffsetSeconds + cue.windowAfterSeconds < songTime) fired.Add(cue.id + "|result");
                }
            }
        }
    }
}

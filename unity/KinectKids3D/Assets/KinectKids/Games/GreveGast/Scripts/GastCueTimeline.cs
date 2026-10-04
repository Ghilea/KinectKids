using System;
using System.Collections.Generic;
using UnityEngine;

namespace KinectKids.Games.GreveGast
{
    /// <summary>Frame-resilient, once-per-pass cue dispatch. Untimed cues are never scheduled.</summary>
    public sealed class GastCueTimeline
    {
        private readonly GastSongAsset song;
        private int scheduleRevision;
        private readonly HashSet<string> fired = new HashSet<string>();
        private readonly List<KeyValuePair<float, string>> due = new List<KeyValuePair<float, string>>(32);
        public event Action<GastSongCue, string> Cue;

        public GastCueTimeline(GastSongAsset asset)
        { song = asset; scheduleRevision = CalculateRevision(); }

        /// <summary>Re-arm edited future cues; the runner must rebuild existing obstacles.</summary>
        public bool RefreshAt(float songTime)
        {
            if (scheduleRevision == CalculateRevision()) return false;
            RestoreAt(songTime);
            return true;
        }

        private int CalculateRevision()
        {
            unchecked
            {
                int hash = 17;
                if (song == null || song.cues == null) return hash;
                hash = hash * 31 + song.cues.Count;
                for (int i = 0; i < song.cues.Count; i++)
                {
                    GastSongCue cue = song.cues[i];
                    if (cue == null) { hash *= 31; continue; }
                    hash = hash * 31 + (cue.id != null ? cue.id.GetHashCode() : 0);
                    hash = hash * 31 + cue.EffectiveTime(song).GetHashCode();
                    hash = hash * 31 + cue.hasVocalTime.GetHashCode();
                    hash = hash * 31 + (int)cue.timingStatus;
                    hash = hash * 31 + cue.gameplayCommand.GetHashCode();
                    hash = hash * 31 + cue.gestureLeadSeconds.GetHashCode();
                    hash = hash * 31 + cue.warningLeadSeconds.GetHashCode();
                    hash = hash * 31 + cue.impactOffsetSeconds.GetHashCode();
                    hash = hash * 31 + cue.windowBeforeSeconds.GetHashCode();
                    hash = hash * 31 + cue.windowAfterSeconds.GetHashCode();
                    hash = hash * 31 + (int)cue.action;
                    hash = hash * 31 + (int)cue.command;
                    hash = hash * 31 + cue.reactionDurationSeconds.GetHashCode();
                }
                return hash;
            }
        }

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

        public void Reset() { fired.Clear(); scheduleRevision = CalculateRevision(); }
        public void RestoreAt(float songTime)
        {
            scheduleRevision = CalculateRevision();
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

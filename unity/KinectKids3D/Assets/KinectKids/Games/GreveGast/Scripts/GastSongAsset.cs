using System;
using System.Collections.Generic;
using UnityEngine;

namespace KinectKids.Games.GreveGast
{
    public enum GastTimingStatus { Untimed, Estimated, VerifiedByListening }
    public enum GastGameplayCommand { None, Jump, Slide, LaneLeft, LaneRight, RunStart }
    public enum GastCueAction { None, PortraitWake, LeanOut, ChaseLaunch, ChaseHover, Sing, CommandJump, CommandSlide, CommandLaneLeft, CommandLaneRight, Success, Fail, Environment }

    [CreateAssetMenu(menuName = "KinectKids/Greve Gast/Song", fileName = "GastSong")]
    public sealed class GastSongAsset : ScriptableObject
    {
        public AudioClip master;
        public float globalOffsetSeconds;
        public List<GastSongSection> sections = new List<GastSongSection>();
        public List<GastSongCue> cues = new List<GastSongCue>();
    }

    [Serializable]
    public sealed class GastSongSection
    {
        public string id;
        public string title;
        public bool hasStartTime;
        public float startSeconds;
        public GastTimingStatus startTimingStatus;
        public bool hasEndTime;
        public float endSeconds;
        public GastTimingStatus endTimingStatus;
        public float offsetSeconds;
    }

    [Serializable]
    public sealed class GastSongCue
    {
        public string id;
        public string sectionId;
        public string lyric;
        public bool gameplayCommand;
        public bool hasVocalTime;
        public float vocalTimeSeconds;
        public GastTimingStatus timingStatus;
        public float cueOffsetSeconds;
        public float gestureLeadSeconds = 0.45f;
        public float warningLeadSeconds = 1.2f;
        public float impactOffsetSeconds = 0.8f;
        public float windowBeforeSeconds = 0.35f;
        public float windowAfterSeconds = 0.35f;
        public GastCueAction action;
        public GastGameplayCommand command;
        public float reactionDurationSeconds = 0.8f;

        public float EffectiveTime(GastSongAsset song)
        {
            float sectionOffset = 0f;
            if (song != null && song.sections != null)
                for (int i = 0; i < song.sections.Count; i++)
                    if (song.sections[i] != null && song.sections[i].id == sectionId)
                    { sectionOffset = song.sections[i].offsetSeconds; break; }
            return vocalTimeSeconds + (song != null ? song.globalOffsetSeconds : 0f) + sectionOffset + cueOffsetSeconds;
        }
    }
}

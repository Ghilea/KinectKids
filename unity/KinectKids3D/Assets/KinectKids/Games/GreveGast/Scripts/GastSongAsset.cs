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

    /// <summary>Keep timed jump and duck responses when a following action interrupts the animation.</summary>
    public sealed class GastDodgeHistory
    {
        private float jumpTime = -1f;
        private float duckTime = -1f;

        public void Record(GastGameplayCommand command, float songTime)
        {
            if (command == GastGameplayCommand.Jump) jumpTime = songTime;
            else if (command == GastGameplayCommand.Slide) duckTime = songTime;
        }

        public bool InWindow(GastGameplayCommand command, float open, float close, float now)
        {
            float time = command == GastGameplayCommand.Jump ? jumpTime :
                command == GastGameplayCommand.Slide ? duckTime : -1f;
            return time >= 0f && time >= open && time <= close && time <= now;
        }

        public void Reset() { jumpTime = -1f; duckTime = -1f; }
    }

    /// <summary>Lane masks use bit 0 = left, bit 1 = centre, bit 2 = right.</summary>
    public static class GastRunnerRules
    {
        public static int PlayerLane(float playerX, float laneSpacing)
        {
            if (laneSpacing <= 0f) return 1;
            return System.Math.Max(0, System.Math.Min(2, (int)System.Math.Round(playerX / laneSpacing) + 1));
        }

        public static int NextLane(int currentLane, int direction, bool songSideCommand)
        {
            if (direction == 0) return currentLane;
            if (songSideCommand) return direction < 0 ? 0 : 2;
            return System.Math.Max(0, System.Math.Min(2, currentLane + direction));
        }

        public static int BlockedLanes(GastGameplayCommand command)
        {
            switch (command)
            {
                case GastGameplayCommand.Jump:
                case GastGameplayCommand.Slide: return 7;
                case GastGameplayCommand.LaneLeft: return 6;
                case GastGameplayCommand.LaneRight: return 3;
                default: return 0;
            }
        }

        public static bool SongResponse(GastGameplayCommand command, int lane,
            bool jumpInWindow, bool duckInWindow)
        {
            switch (command)
            {
                case GastGameplayCommand.Jump: return jumpInWindow;
                case GastGameplayCommand.Slide: return duckInWindow;
                case GastGameplayCommand.LaneLeft: return lane == 0;
                case GastGameplayCommand.LaneRight: return lane == 2;
                default: return false;
            }
        }

        public static bool CanSpawnRandom(GastSongAsset song, float now, float travelSeconds, float recoverySeconds)
        {
            if (song == null || song.cues == null) return true;
            float finish = now + travelSeconds + recoverySeconds;
            foreach (GastSongCue cue in song.cues)
            {
                if (cue == null || !cue.gameplayCommand || !cue.hasVocalTime ||
                    cue.timingStatus == GastTimingStatus.Untimed) continue;
                float vocal = cue.EffectiveTime(song);
                float start = vocal - cue.warningLeadSeconds;
                float end = vocal + cue.impactOffsetSeconds + cue.windowAfterSeconds + recoverySeconds;
                if (now <= end && finish >= start) return false;
            }
            return true;
        }
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
        public float impactOffsetSeconds;
        public float windowBeforeSeconds = 0.35f;
        public float windowAfterSeconds = 0.5f;
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

using System;
using System.Collections.Generic;
using UnityEngine;

namespace KinectKids.Scene25D.Sections
{
    /// <summary>
    /// A musical section of the song (intro, verse, bridge…) mapped to a room and
    /// an intensity. This is the DATA that ties the environment to the music: the
    /// runner changes rooms and difficulty in step with the track (new-way.md:
    /// "övergångar mellan olika rum enligt låtens struktur").
    /// </summary>
    [Serializable]
    public struct SongSection
    {
        [Tooltip("Label for debugging (\"Intro\", \"Vers 1\", \"Refräng\", \"Brygga\"…).")]
        public string label;

        [Tooltip("Song time in seconds when this section STARTS.")]
        public float startSeconds;

        [Tooltip("Which room the runner should be in during this section.")]
        public RoomKind room;

        [Tooltip("0..1 intensity: scales world speed and hazard frequency for this section.")]
        public float intensity;

        public static SongSection At(float startSeconds, string label, RoomKind room, float intensity = 0.5f)
            => new SongSection
            {
                startSeconds = startSeconds,
                label = label,
                room = room,
                intensity = Mathf.Clamp01(intensity),
            };
    }

    /// <summary>
    /// The full ordered structure of a song as a list of <see cref="SongSection"/>.
    /// Frozen data — a designer authors it once per track and the driver walks it.
    /// Keeping it separate from the driver means the same driver runs any song.
    /// </summary>
    [Serializable]
    public sealed class SongStructure
    {
        [Tooltip("Estimated beats per minute, used to quantise transitions to beats.")]
        public float bpm = 120f;

        [Tooltip("Ordered sections; the first should start at 0.")]
        public List<SongSection> sections = new List<SongSection>();

        public SongStructure() { }
        public SongStructure(float bpm) { this.bpm = bpm; }

        public SongStructure Add(float startSeconds, string label, RoomKind room, float intensity = 0.5f)
        {
            sections.Add(SongSection.At(startSeconds, label, room, intensity));
            return this;
        }

        public float SecondsPerBeat => bpm > 1f ? 60f / bpm : 0.5f;

        /// <summary>Index of the section active at a given song time (or -1 before the first).</summary>
        public int SectionIndexAt(float songTime)
        {
            int found = -1;
            for (int i = 0; i < sections.Count; i++)
            {
                if (songTime >= sections[i].startSeconds) found = i;
                else break;
            }
            return found;
        }
    }

    /// <summary>
    /// Drives a <see cref="SectionStreamer"/> from a <see cref="SongStructure"/>
    /// and a song clock: it watches the song time, and when the active section
    /// changes it tells the streamer to enter the new room (quantised to the next
    /// beat so the change lands musically). It also exposes the current intensity
    /// so a scene director can scale speed and hazard frequency.
    ///
    /// The clock is injected (a delegate returning song seconds) so this works
    /// with an AudioSource, an unscaled timer, or a test — no hard audio coupling.
    /// </summary>
    public sealed class SongStructureDriver : MonoBehaviour
    {
        private SectionStreamer streamer;
        private SongStructure structure;
        private Func<float> songTime;
        private Func<RoomKind, SectionDefinition> roomFactory;

        private int currentSectionIndex = -1;
        private int pendingSectionIndex = -1;
        private float pendingBeatTime;

        /// <summary>0..1 intensity of the current section; scene director scales gameplay by it.</summary>
        public float CurrentIntensity { get; private set; } = 0.5f;

        /// <summary>The section label currently playing (for debug HUD).</summary>
        public string CurrentLabel { get; private set; } = "";

        /// <summary>Fires when the runner actually enters a new room.</summary>
        public event Action<SongSection> SectionChanged;

        /// <summary>
        /// Wire the driver. <paramref name="roomFactory"/> turns a RoomKind into a
        /// fresh SectionDefinition (usually <c>RoomCatalog.Build</c>).
        /// </summary>
        public void Init(SectionStreamer sectionStreamer, SongStructure songStructure,
            Func<float> songTimeSource, Func<RoomKind, SectionDefinition> roomFactory)
        {
            streamer = sectionStreamer;
            structure = songStructure;
            songTime = songTimeSource;
            this.roomFactory = roomFactory;

            // Enter the very first section immediately (no beat wait for the opener).
            if (structure != null && structure.sections.Count > 0)
            {
                currentSectionIndex = 0;
                ApplySection(0, snap: true);
            }
        }

        private void Update()
        {
            if (structure == null || songTime == null || streamer == null) return;

            float t = songTime();
            int idx = structure.SectionIndexAt(t);

            // A new section became active: schedule the room change on the next beat.
            if (idx >= 0 && idx != currentSectionIndex && idx != pendingSectionIndex)
            {
                pendingSectionIndex = idx;
                pendingBeatTime = NextBeat(t);
            }

            // When the scheduled beat arrives (and the streamer is free), switch.
            if (pendingSectionIndex >= 0 && t >= pendingBeatTime && !streamer.IsTransitioning)
            {
                currentSectionIndex = pendingSectionIndex;
                pendingSectionIndex = -1;
                ApplySection(currentSectionIndex, snap: false);
            }
        }

        private float NextBeat(float songTimeNow)
        {
            float spb = structure.SecondsPerBeat;
            if (spb <= 0.001f) return songTimeNow;
            float beatsElapsed = Mathf.Floor(songTimeNow / spb);
            return (beatsElapsed + 1f) * spb;
        }

        private void ApplySection(int index, bool snap)
        {
            if (index < 0 || index >= structure.sections.Count) return;
            SongSection section = structure.sections[index];
            CurrentIntensity = section.intensity;
            CurrentLabel = section.label;

            SectionDefinition def = roomFactory != null ? roomFactory(section.room) : null;
            if (def != null)
            {
                if (snap) def.transitionSeconds = 0.01f;
                streamer.EnterRoom(def);
            }
            SectionChanged?.Invoke(section);
        }
    }
}

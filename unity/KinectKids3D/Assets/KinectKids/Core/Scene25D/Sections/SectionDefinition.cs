using System;
using System.Collections.Generic;
using UnityEngine;

namespace KinectKids.Scene25D.Sections
{
    /// <summary>
    /// The named rooms the runner streams through. Each is a distinct space with
    /// its own theme and module set, but all are built from the SAME reusable
    /// module kinds (new-way.md: "Hellre 20 bra återanvändbara delar ... än 100
    /// unika objekt"). Add a room by adding an enum value + a
    /// <see cref="SectionDefinition"/>; nothing else in the engine changes.
    /// </summary>
    public enum RoomKind
    {
        Corridor,
        GreatHall,
        Kitchen,
        Cellar,
        Attic,
        Custom,
    }

    /// <summary>
    /// A single reusable "prop slot" placed at a fixed depth along the room, on a
    /// side. The streamer positions it in perspective and streams it toward the
    /// camera as the world moves. Multiple slots let a room author a distinctive
    /// silhouette (e.g. long banquet table in the hall, hanging pots in the
    /// kitchen) purely as data.
    /// </summary>
    [Serializable]
    public struct PropPlacement
    {
        [Tooltip("The module (prop/arch/overhead/framing) to place.")]
        public EnvironmentModule module;

        [Tooltip("Depth along the room, 0 = near camera, 1 = far at the vanishing point.")]
        public float depth01;

        public static PropPlacement At(EnvironmentModule module, float depth01)
            => new PropPlacement { module = module, depth01 = Mathf.Clamp01(depth01) };
    }

    /// <summary>
    /// A section / room described entirely as DATA. It says which floor + wall
    /// modules tile the corridor, which fixed props decorate it, which hazards it
    /// allows, its theme tint and how long it lasts — but contains no art and no
    /// hard gameplay rules. A <c>SectionStreamer</c> turns it into live objects;
    /// a <c>SongStructureDriver</c> decides when to enter/leave it.
    ///
    /// Rooms are built with the fluent helpers so a designer reads the layout at
    /// a glance and can reorder / retint without touching engine code.
    /// </summary>
    [Serializable]
    public sealed class SectionDefinition
    {
        [Tooltip("Which room this is (also selects sensible defaults).")]
        public RoomKind room = RoomKind.Corridor;

        [Tooltip("Human-readable name shown in debug / editor.")]
        public string displayName = "Corridor";

        [Tooltip("Signature colour the dark-storybook theme is tinted toward.")]
        public Color signature = new Color(0.18f, 0.16f, 0.30f);

        [Tooltip("How long the room lasts, measured in world travel units " +
                 "(ParallaxController.ForwardTravel). The streamer advances through it.")]
        public float lengthTravel = 60f;

        [Tooltip("Seconds to cross-fade the theme/lighting when entering this room.")]
        public float transitionSeconds = 1.2f;

        [Tooltip("Repeating floor tile modules, cycled as the lane streams. " +
                 "At least one required; alternated for variation.")]
        public List<EnvironmentModule> floorTiles = new List<EnvironmentModule>();

        [Tooltip("Repeating left-wall section modules, placed at receding depths.")]
        public List<EnvironmentModule> leftWalls = new List<EnvironmentModule>();

        [Tooltip("Repeating right-wall section modules, placed at receding depths.")]
        public List<EnvironmentModule> rightWalls = new List<EnvironmentModule>();

        [Tooltip("Fixed decorative props / arches / overheads / framing placed at authored depths.")]
        public List<PropPlacement> props = new List<PropPlacement>();

        [Tooltip("Ambient FX modules (fog bands, sparks, steam) that live for the whole room.")]
        public List<EnvironmentModule> effects = new List<EnvironmentModule>();

        [Tooltip("Which hazard kinds may spawn in this room. Empty = no hazards (calm room).")]
        public List<HazardKind> allowedHazards = new List<HazardKind>();

        [Tooltip("Seconds between hazard spawns in this room (higher = calmer).")]
        public float hazardInterval = 2.4f;

        // ---- Fluent authoring helpers (keep room definitions readable) ----

        public SectionDefinition Named(string name) { displayName = name; return this; }
        public SectionDefinition Tinted(Color c) { signature = c; return this; }
        public SectionDefinition Length(float travel) { lengthTravel = travel; return this; }
        public SectionDefinition Transition(float seconds) { transitionSeconds = seconds; return this; }

        public SectionDefinition Floor(params EnvironmentModule[] tiles)
        {
            floorTiles.AddRange(tiles);
            return this;
        }

        public SectionDefinition Walls(EnvironmentModule left, EnvironmentModule right)
        {
            leftWalls.Add(left);
            rightWalls.Add(right);
            return this;
        }

        public SectionDefinition Prop(EnvironmentModule module, float depth01)
        {
            props.Add(PropPlacement.At(module, depth01));
            return this;
        }

        public SectionDefinition Effect(params EnvironmentModule[] fx)
        {
            effects.AddRange(fx);
            return this;
        }

        public SectionDefinition Hazards(float interval, params HazardKind[] kinds)
        {
            hazardInterval = interval;
            allowedHazards.Clear();
            allowedHazards.AddRange(kinds);
            return this;
        }

        /// <summary>Whether this room spawns hazards at all.</summary>
        public bool HasHazards => allowedHazards != null && allowedHazards.Count > 0;

        /// <summary>Pick a hazard kind for this room (round-robin friendly index).</summary>
        public HazardKind HazardAt(int index)
        {
            if (!HasHazards) return HazardKind.LowBeam;
            return allowedHazards[((index % allowedHazards.Count) + allowedHazards.Count) % allowedHazards.Count];
        }

        /// <summary>A brand-new definition for a room, ready to be filled fluently.</summary>
        public static SectionDefinition Create(RoomKind room, string name, Color signature)
        {
            return new SectionDefinition
            {
                room = room,
                displayName = name,
                signature = signature,
                floorTiles = new List<EnvironmentModule>(),
                leftWalls = new List<EnvironmentModule>(),
                rightWalls = new List<EnvironmentModule>(),
                props = new List<PropPlacement>(),
                effects = new List<EnvironmentModule>(),
                allowedHazards = new List<HazardKind>(),
            };
        }
    }
}

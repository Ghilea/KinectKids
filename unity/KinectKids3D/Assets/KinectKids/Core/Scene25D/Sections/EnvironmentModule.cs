using System;
using UnityEngine;

namespace KinectKids.Scene25D.Sections
{
    /// <summary>
    /// The role a module plays in a room. Kept art-agnostic: a module is a
    /// building BLOCK, not a specific picture. The same kind can be filled by a
    /// placeholder silhouette now and a final PNG later without touching any
    /// section, streaming or gameplay code (new-way.md: "Vi ska kunna byta en
    /// sprite eller bakgrund utan att behöva ändra spelregler").
    /// </summary>
    public enum ModuleKind
    {
        /// <summary>A run-lane floor tile that streams toward the camera.</summary>
        FloorSegment,
        /// <summary>A left wall section that recedes toward the vanishing point.</summary>
        WallLeft,
        /// <summary>A right wall section that recedes toward the vanishing point.</summary>
        WallRight,
        /// <summary>An arch / portal / doorway spanning the lane (a room threshold).</summary>
        Arch,
        /// <summary>A decorative prop (statue, banner, barrel, painting, lantern…).</summary>
        Prop,
        /// <summary>A ceiling / overhead piece (beams, chandelier, hanging pots).</summary>
        Overhead,
        /// <summary>A gameplay obstacle the player must dodge (maps to a HazardKind).</summary>
        Hazard,
        /// <summary>A pure visual effect layer (fog band, sparks, glow, steam).</summary>
        Fx,
        /// <summary>A framing element close to the camera (foreground pillar, rubble).</summary>
        Framing,
    }

    /// <summary>
    /// Which side of the lane a module sits on. Lets one definition describe both
    /// walls or alternate props left/right without duplicating data.
    /// </summary>
    public enum ModuleSide
    {
        Center,
        Left,
        Right,
        BothSides,
    }

    /// <summary>
    /// A single reusable environment building block, described as DATA. It names
    /// the visual it wants (a sprite key resolved through a swappable resolver)
    /// and how big / where it sits, but owns no art and no gameplay rules.
    ///
    /// A <see cref="SectionDefinition"/> lists these; a <c>SectionStreamer</c>
    /// instantiates them in perspective. Because everything is a key + geometry,
    /// the art pipeline can replace a placeholder with a final PNG by mapping the
    /// same key, and gameplay never notices.
    /// </summary>
    [Serializable]
    public struct EnvironmentModule
    {
        [Tooltip("What role this block plays in the room.")]
        public ModuleKind kind;

        [Tooltip("Which side of the lane it sits on.")]
        public ModuleSide side;

        [Tooltip("Sprite key resolved through the section's sprite resolver " +
                 "(e.g. \"env_2\", \"prop_cauldron\"). Empty = use a themed placeholder.")]
        public string spriteKey;

        [Tooltip("Rendered world height in units at the NEAR plane (perspective scales it down with depth).")]
        public float worldHeight;

        [Tooltip("Extra horizontal offset from its default lane position, in near-plane units.")]
        public float lateralOffset;

        [Tooltip("Scene band this block draws in. Defaults per-kind if left as Actors placeholder.")]
        public SceneBand band;

        [Tooltip("For Hazard modules: which dodge the player must perform.")]
        public HazardKind hazardKind;

        [Tooltip("Optional tint multiplier applied on top of the theme (white = untinted).")]
        public Color tint;

        [Tooltip("Relative spawn weight when a room picks modules at random. 0 or 1 = default.")]
        public float weight;

        /// <summary>A default-tinted, unit-weighted module of a kind.</summary>
        public static EnvironmentModule Of(ModuleKind kind, string spriteKey = "",
            float worldHeight = 3f, ModuleSide side = ModuleSide.Center)
        {
            return new EnvironmentModule
            {
                kind = kind,
                side = side,
                spriteKey = spriteKey,
                worldHeight = worldHeight,
                lateralOffset = 0f,
                band = DefaultBand(kind, side),
                hazardKind = HazardKind.LowBeam,
                tint = Color.white,
                weight = 1f,
            };
        }

        /// <summary>A hazard module (obstacle) of a given dodge kind.</summary>
        public static EnvironmentModule HazardOf(HazardKind hazardKind, string spriteKey = "")
        {
            EnvironmentModule m = Of(ModuleKind.Hazard, spriteKey);
            m.hazardKind = hazardKind;
            m.band = SceneBand.Hazards;
            return m;
        }

        /// <summary>Fluent tint setter.</summary>
        public EnvironmentModule WithTint(Color c) { tint = c; return this; }
        /// <summary>Fluent lateral-offset setter.</summary>
        public EnvironmentModule WithOffset(float x) { lateralOffset = x; return this; }
        /// <summary>Fluent weight setter for random room fills.</summary>
        public EnvironmentModule WithWeight(float w) { weight = w; return this; }

        /// <summary>Sensible default band for a kind so callers rarely set it.</summary>
        public static SceneBand DefaultBand(ModuleKind kind, ModuleSide side)
        {
            switch (kind)
            {
                case ModuleKind.FloorSegment: return SceneBand.Floor;
                case ModuleKind.WallLeft: return SceneBand.LeftEnvironment;
                case ModuleKind.WallRight: return SceneBand.RightEnvironment;
                case ModuleKind.Arch: return SceneBand.Background;
                case ModuleKind.Overhead: return SceneBand.MidBackground;
                case ModuleKind.Prop:
                    return side == ModuleSide.Right ? SceneBand.RightEnvironment : SceneBand.LeftEnvironment;
                case ModuleKind.Hazard: return SceneBand.Hazards;
                case ModuleKind.Fx: return SceneBand.Fog;
                case ModuleKind.Framing: return SceneBand.Foreground;
                default: return SceneBand.Background;
            }
        }
    }
}

using UnityEngine;

namespace KinectKids.Scene25D.Sections
{
    /// <summary>
    /// Concrete, hand-authored rooms built from the reusable module kinds, plus a
    /// sample <see cref="SongStructure"/> that chains them. This is pure DATA
    /// (new-way.md: "Hellre 20 bra återanvändbara delar ... än 100 unika
    /// objekt") — the same corridor floor tile appears in every room, walls and
    /// props are reused with different tints, and only the arrangement differs.
    ///
    /// Sprite keys reuse the existing chase environment cuts (env_0..env_19) so
    /// that when <c>GreveChaseSprites.Environment</c> is used as the
    /// <see cref="ModuleLibrary.Resolver"/>, real art fills the rooms; otherwise
    /// themed placeholders render. Either way the layout is identical.
    /// </summary>
    public static class RoomCatalog
    {
        // Reused environment cuts (see docs/asset-sources/environment_sheet.png).
        private const string FloorClean = "env_0";
        private const string FloorCracked = "env_1";
        private const string WallLeft = "env_2";
        private const string WallRight = "env_3";
        private const string Arch = "env_4";
        private const string Door = "env_5";
        private const string Window = "env_7";
        private const string Portrait = "env_8";
        private const string Rocks = "env_9";
        private const string Torch = "env_10";
        private const string Banner = "env_11";
        private const string Statue = "env_12";
        private const string Pillar = "env_13";
        private const string FogThin = "env_16";
        private const string FogThick = "env_17";

        /// <summary>Build a fresh definition for a room kind (new instance each call).</summary>
        public static SectionDefinition Build(RoomKind room)
        {
            switch (room)
            {
                case RoomKind.GreatHall: return GreatHall();
                case RoomKind.Kitchen: return Kitchen();
                case RoomKind.Cellar: return Cellar();
                case RoomKind.Attic: return Attic();
                case RoomKind.Corridor:
                default: return Corridor();
            }
        }

        /// <summary>
        /// CORRIDOR — the default tight, torch-lit stone hallway. Alternating
        /// clean/cracked floor, receding torch-lit walls, occasional arch and
        /// portrait, thin drifting fog. The staple "running" room.
        /// </summary>
        public static SectionDefinition Corridor()
        {
            var def = SectionDefinition.Create(RoomKind.Corridor, "Korridor",
                    new Color(0.18f, 0.16f, 0.30f))
                .Length(55f)
                .Transition(1.0f)
                .Floor(
                    EnvironmentModule.Of(ModuleKind.FloorSegment, FloorClean, 1f),
                    EnvironmentModule.Of(ModuleKind.FloorSegment, FloorCracked, 1f))
                .Walls(
                    EnvironmentModule.Of(ModuleKind.WallLeft, WallLeft, 11f, ModuleSide.Left),
                    EnvironmentModule.Of(ModuleKind.WallRight, WallRight, 11f, ModuleSide.Right))
                .Prop(EnvironmentModule.Of(ModuleKind.Arch, Arch, 4.2f), 0.55f)
                .Prop(EnvironmentModule.Of(ModuleKind.Prop, Portrait, 3.4f, ModuleSide.Left), 0.42f)
                .Prop(EnvironmentModule.Of(ModuleKind.Prop, Statue, 4.0f, ModuleSide.Left).WithOffset(-0.4f), 0.30f)
                .Prop(EnvironmentModule.Of(ModuleKind.Prop, Banner, 3.0f, ModuleSide.Right), 0.36f)
                .Prop(EnvironmentModule.Of(ModuleKind.Framing, Pillar, 10.5f, ModuleSide.Left), 0.02f)
                .Prop(EnvironmentModule.Of(ModuleKind.Framing, Pillar, 8.5f, ModuleSide.Right), 0.05f)
                .Effect(
                    EnvironmentModule.Of(ModuleKind.Fx, FogThin, 2.2f))
                .Hazards(2.4f, HazardKind.LowBeam, HazardKind.Falling, HazardKind.Side, HazardKind.Jump);
            return def;
        }

        /// <summary>
        /// GREAT HALL — a grand, wide, high room. Wider perspective (set by the
        /// director via PerspectiveModel.Wide), banners and statues line a long
        /// nave, arches march into the distance, a moonlit window sits deep back.
        /// Calmer hazards, more spectacle.
        /// </summary>
        public static SectionDefinition GreatHall()
        {
            var def = SectionDefinition.Create(RoomKind.GreatHall, "Stora salen",
                    new Color(0.24f, 0.18f, 0.42f))
                .Length(60f)
                .Transition(1.4f)
                .Floor(
                    EnvironmentModule.Of(ModuleKind.FloorSegment, FloorClean, 1f)
                        .WithTint(new Color(0.5f, 0.45f, 0.6f)))
                .Walls(
                    EnvironmentModule.Of(ModuleKind.WallLeft, WallLeft, 13f, ModuleSide.Left),
                    EnvironmentModule.Of(ModuleKind.WallRight, WallRight, 13f, ModuleSide.Right))
                .Prop(EnvironmentModule.Of(ModuleKind.Prop, Window, 3.6f, ModuleSide.Left).WithOffset(-1.2f), 0.86f)
                .Prop(EnvironmentModule.Of(ModuleKind.Arch, Arch, 5.0f), 0.72f)
                .Prop(EnvironmentModule.Of(ModuleKind.Arch, Arch, 5.0f), 0.5f)
                .Prop(EnvironmentModule.Of(ModuleKind.Prop, Banner, 3.4f, ModuleSide.Left), 0.62f)
                .Prop(EnvironmentModule.Of(ModuleKind.Prop, Banner, 3.4f, ModuleSide.Right), 0.48f)
                .Prop(EnvironmentModule.Of(ModuleKind.Prop, Statue, 4.6f, ModuleSide.Left), 0.34f)
                .Prop(EnvironmentModule.Of(ModuleKind.Prop, Statue, 4.6f, ModuleSide.Right), 0.22f)
                .Prop(EnvironmentModule.Of(ModuleKind.Framing, Pillar, 12f, ModuleSide.Left), 0.02f)
                .Prop(EnvironmentModule.Of(ModuleKind.Framing, Pillar, 12f, ModuleSide.Right), 0.03f)
                .Effect(
                    EnvironmentModule.Of(ModuleKind.Fx, FogThick, 2.6f)
                        .WithTint(new Color(0.82f, 0.87f, 1f, 0.32f)))
                .Hazards(3.0f, HazardKind.LowBeam, HazardKind.Side);
            return def;
        }

        /// <summary>
        /// KITCHEN — a cramped, warm, cluttered room. Tight perspective, lots of
        /// low props (barrels/rocks stand in as crates and pots), warm accent
        /// lighting, low overhead beams to duck under. Busy, frantic hazards.
        /// </summary>
        public static SectionDefinition Kitchen()
        {
            Color warm = new Color(0.42f, 0.26f, 0.16f);
            var def = SectionDefinition.Create(RoomKind.Kitchen, "Köket",
                    new Color(0.40f, 0.24f, 0.16f))
                .Length(48f)
                .Transition(0.9f)
                .Floor(
                    EnvironmentModule.Of(ModuleKind.FloorSegment, FloorCracked, 1f)
                        .WithTint(new Color(0.5f, 0.4f, 0.32f)),
                    EnvironmentModule.Of(ModuleKind.FloorSegment, FloorClean, 1f)
                        .WithTint(new Color(0.55f, 0.44f, 0.34f)))
                .Walls(
                    EnvironmentModule.Of(ModuleKind.WallLeft, WallLeft, 9f, ModuleSide.Left)
                        .WithTint(warm),
                    EnvironmentModule.Of(ModuleKind.WallRight, WallRight, 9f, ModuleSide.Right)
                        .WithTint(warm))
                // Low props stand in as kitchen clutter (crates, sacks, pots).
                .Prop(EnvironmentModule.Of(ModuleKind.Prop, Rocks, 2.0f, ModuleSide.Left).WithOffset(-0.3f), 0.52f)
                .Prop(EnvironmentModule.Of(ModuleKind.Prop, Rocks, 1.7f, ModuleSide.Right).WithOffset(0.3f), 0.44f)
                .Prop(EnvironmentModule.Of(ModuleKind.Prop, Rocks, 2.2f, ModuleSide.Left), 0.30f)
                // Overhead beams / hanging pots to duck under.
                .Prop(EnvironmentModule.Of(ModuleKind.Overhead, Banner, 2.4f), 0.60f)
                .Prop(EnvironmentModule.Of(ModuleKind.Overhead, Banner, 2.4f), 0.40f)
                .Prop(EnvironmentModule.Of(ModuleKind.Prop, Torch, 1.4f, ModuleSide.Right)
                    .WithTint(PlaceholderArt.LanternOrange), 0.66f)
                .Prop(EnvironmentModule.Of(ModuleKind.Framing, Pillar, 8f, ModuleSide.Left), 0.03f)
                .Effect(
                    EnvironmentModule.Of(ModuleKind.Fx, FogThin, 1.8f)
                        .WithTint(new Color(1f, 0.7f, 0.4f, 0.25f)))
                .Hazards(1.9f, HazardKind.LowBeam, HazardKind.Jump, HazardKind.Side, HazardKind.Falling);
            return def;
        }

        /// <summary>
        /// CELLAR — a dark, cold, low dungeon. Tight perspective, cracked floor,
        /// heavy cold-blue tint, doors and rubble, thick low fog. Sparse but
        /// nasty hazards (jump the gaps, dodge falling debris).
        /// </summary>
        public static SectionDefinition Cellar()
        {
            Color cold = new Color(0.12f, 0.16f, 0.28f);
            var def = SectionDefinition.Create(RoomKind.Cellar, "Källaren",
                    new Color(0.10f, 0.14f, 0.26f))
                .Length(50f)
                .Transition(1.1f)
                .Floor(
                    EnvironmentModule.Of(ModuleKind.FloorSegment, FloorCracked, 1f)
                        .WithTint(new Color(0.32f, 0.34f, 0.42f)))
                .Walls(
                    EnvironmentModule.Of(ModuleKind.WallLeft, WallLeft, 9f, ModuleSide.Left).WithTint(cold),
                    EnvironmentModule.Of(ModuleKind.WallRight, WallRight, 9f, ModuleSide.Right).WithTint(cold))
                .Prop(EnvironmentModule.Of(ModuleKind.Arch, Door, 4.0f), 0.7f)
                .Prop(EnvironmentModule.Of(ModuleKind.Prop, Rocks, 2.2f, ModuleSide.Left), 0.5f)
                .Prop(EnvironmentModule.Of(ModuleKind.Prop, Rocks, 1.9f, ModuleSide.Right), 0.38f)
                .Prop(EnvironmentModule.Of(ModuleKind.Prop, Torch, 1.4f, ModuleSide.Left)
                    .WithTint(PlaceholderArt.LanternOrange), 0.6f)
                .Prop(EnvironmentModule.Of(ModuleKind.Framing, Pillar, 8f, ModuleSide.Right), 0.03f)
                .Effect(
                    EnvironmentModule.Of(ModuleKind.Fx, FogThick, 2.6f)
                        .WithTint(new Color(0.5f, 0.6f, 0.85f, 0.35f)))
                .Hazards(2.1f, HazardKind.Jump, HazardKind.Falling, HazardKind.Side);
            return def;
        }

        /// <summary>
        /// ATTIC — a cramped, dusty roof space. Tight, low perspective, warm dusty
        /// light, low overhead beams everywhere (duck!), leaning props. Frantic,
        /// beam-heavy hazards.
        /// </summary>
        public static SectionDefinition Attic()
        {
            Color dusty = new Color(0.34f, 0.28f, 0.20f);
            var def = SectionDefinition.Create(RoomKind.Attic, "Vinden",
                    new Color(0.32f, 0.26f, 0.18f))
                .Length(46f)
                .Transition(0.9f)
                .Floor(
                    EnvironmentModule.Of(ModuleKind.FloorSegment, FloorClean, 1f)
                        .WithTint(new Color(0.5f, 0.42f, 0.30f)),
                    EnvironmentModule.Of(ModuleKind.FloorSegment, FloorCracked, 1f)
                        .WithTint(new Color(0.46f, 0.38f, 0.28f)))
                .Walls(
                    EnvironmentModule.Of(ModuleKind.WallLeft, WallLeft, 8f, ModuleSide.Left).WithTint(dusty),
                    EnvironmentModule.Of(ModuleKind.WallRight, WallRight, 8f, ModuleSide.Right).WithTint(dusty))
                // Lots of low overhead beams to duck under.
                .Prop(EnvironmentModule.Of(ModuleKind.Overhead, Banner, 2.6f), 0.66f)
                .Prop(EnvironmentModule.Of(ModuleKind.Overhead, Banner, 2.6f), 0.48f)
                .Prop(EnvironmentModule.Of(ModuleKind.Overhead, Banner, 2.6f), 0.32f)
                .Prop(EnvironmentModule.Of(ModuleKind.Prop, Rocks, 1.8f, ModuleSide.Left), 0.44f)
                .Prop(EnvironmentModule.Of(ModuleKind.Prop, Portrait, 2.6f, ModuleSide.Right), 0.55f)
                .Prop(EnvironmentModule.Of(ModuleKind.Framing, Pillar, 7f, ModuleSide.Left), 0.03f)
                .Effect(
                    EnvironmentModule.Of(ModuleKind.Fx, FogThin, 1.6f)
                        .WithTint(new Color(1f, 0.78f, 0.5f, 0.22f)))
                .Hazards(1.8f, HazardKind.LowBeam, HazardKind.Jump, HazardKind.LowBeam, HazardKind.Side);
            return def;
        }

        /// <summary>The perspective model that best suits each room's feel.</summary>
        public static PerspectiveModel PerspectiveFor(RoomKind room)
        {
            switch (room)
            {
                case RoomKind.GreatHall: return PerspectiveModel.Wide;
                case RoomKind.Kitchen: return PerspectiveModel.Tight;
                case RoomKind.Cellar: return PerspectiveModel.Tight;
                case RoomKind.Attic: return PerspectiveModel.Tight;
                default: return PerspectiveModel.Default;
            }
        }

        /// <summary>
        /// A sample song structure that chains the three rooms in step with a
        /// typical verse/chorus/bridge arc. Times are illustrative; retune to a
        /// real track's markers. Intensity rises into choruses.
        /// </summary>
        public static SongStructure SampleSong(float bpm = 124f)
        {
            return new SongStructure(bpm)
                .Add(0f, "Intro", RoomKind.Corridor, 0.35f)
                .Add(16f, "Vers 1", RoomKind.Corridor, 0.5f)
                .Add(32f, "Refräng 1", RoomKind.GreatHall, 0.75f)
                .Add(52f, "Vers 2", RoomKind.Kitchen, 0.6f)
                .Add(68f, "Stick", RoomKind.Cellar, 0.7f)
                .Add(84f, "Refräng 2", RoomKind.GreatHall, 0.85f)
                .Add(104f, "Brygga", RoomKind.Attic, 0.6f)
                .Add(120f, "Slutrefräng", RoomKind.GreatHall, 1.0f);
        }
    }
}

using System;
using UnityEngine;

namespace KinectKids.Scene25D.Sections
{
    /// <summary>
    /// Resolves a module's <see cref="EnvironmentModule.spriteKey"/> to an actual
    /// <see cref="Sprite"/>, with a themed placeholder fallback so a room always
    /// renders even before final art exists.
    ///
    /// This is the ONE seam between the modular environment and the art pipeline:
    /// point <see cref="Resolver"/> at <c>GreveChaseSprites.Environment</c> (or any
    /// Resources lookup) and every module that names a matching key gets the real
    /// PNG; unmatched keys fall back to a placeholder shape picked from the
    /// module's kind. No section, streaming or gameplay code changes when art is
    /// swapped in (new-way.md asset-strategy + "Återanvändning").
    /// </summary>
    public sealed class ModuleLibrary
    {
        /// <summary>
        /// Optional real-art resolver: key → Sprite (or null if missing). Typically
        /// <c>GreveChaseSprites.Environment</c>. When null, everything is placeholder.
        /// </summary>
        public Func<string, Sprite> Resolver { get; set; }

        public ModuleLibrary(Func<string, Sprite> resolver = null)
        {
            Resolver = resolver;
        }

        /// <summary>True if any real art is wired up (used to pick art vs placeholder layouts).</summary>
        public bool HasArt(string sampleKey)
        {
            return Resolver != null && !string.IsNullOrEmpty(sampleKey) && Resolver(sampleKey) != null;
        }

        /// <summary>
        /// Resolve the sprite for a module: real art if the key maps, otherwise a
        /// placeholder silhouette chosen by the module's kind.
        /// </summary>
        public Sprite Resolve(in EnvironmentModule module)
        {
            if (Resolver != null && !string.IsNullOrEmpty(module.spriteKey))
            {
                Sprite real = Resolver(module.spriteKey);
                if (real != null) return real;
            }
            return Placeholder(module.kind);
        }

        /// <summary>The placeholder silhouette that best represents a module kind.</summary>
        public static Sprite Placeholder(ModuleKind kind)
        {
            switch (kind)
            {
                case ModuleKind.FloorSegment: return PlaceholderArt.SolidBlock();
                case ModuleKind.WallLeft:
                case ModuleKind.WallRight: return PlaceholderArt.SolidBlock();
                case ModuleKind.Arch: return PlaceholderArt.SolidBlock();
                case ModuleKind.Overhead: return PlaceholderArt.Beam();
                case ModuleKind.Prop: return PlaceholderArt.Capsule();
                case ModuleKind.Hazard: return PlaceholderArt.SolidBlock();
                case ModuleKind.Fx: return PlaceholderArt.Glow();
                case ModuleKind.Framing: return PlaceholderArt.SolidBlock();
                default: return PlaceholderArt.SolidBlock();
            }
        }

        /// <summary>
        /// A reasonable placeholder tint for a kind within a room theme, used when
        /// the module itself carries no explicit tint (tint == white). Keeps the
        /// dark-storybook palette consistent across rooms.
        /// </summary>
        public static Color PlaceholderTint(ModuleKind kind, in Scene25DBackdrop.Theme theme)
        {
            switch (kind)
            {
                case ModuleKind.FloorSegment: return theme.floor;
                case ModuleKind.WallLeft:
                case ModuleKind.WallRight: return theme.walls;
                case ModuleKind.Arch: return theme.structures;
                case ModuleKind.Overhead: return theme.structures;
                case ModuleKind.Prop: return theme.accent;
                case ModuleKind.Hazard: return PlaceholderArt.Shadow;
                case ModuleKind.Fx: return theme.fog;
                case ModuleKind.Framing: return PlaceholderArt.Shadow;
                default: return theme.structures;
            }
        }
    }
}

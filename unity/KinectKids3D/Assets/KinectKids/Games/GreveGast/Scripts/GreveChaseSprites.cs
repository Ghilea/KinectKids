using System.Collections.Generic;
using UnityEngine;
using KinectKids.Scene25D;

namespace KinectKids.Games.GreveGast
{
    /// <summary>
    /// Loads the real chase sprites cut from the asset sheets, living under
    /// <c>Resources/GreveChase/...</c>. Everything falls back to
    /// <see cref="PlaceholderArt"/> when a sprite is missing, so the scene still
    /// runs if art is absent — keeping gameplay fully decoupled from assets.
    /// </summary>
    public static class GreveChaseSprites
    {
        private const string Root = "GreveChase/";
        private static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Player(string pose) => Load("Player/player_" + pose);
        public static Sprite Greve(string pose) => Load("GreveGast/greve_" + pose);
        public static Sprite Environment(string name) => Load("Environment/" + name);
        public static Sprite Hazard(string name) => Load("Hazards/" + name);

        public static bool HasPlayerArt => Player("idle") != null;
        public static bool HasGreveArt => Greve("idle") != null;

        public static Sprite Load(string relativePath)
        {
            if (cache.TryGetValue(relativePath, out Sprite cached)) return cached;
            Sprite sprite = Resources.Load<Sprite>(Root + relativePath);
            cache[relativePath] = sprite;
            return sprite;
        }
    }

    /// <summary>
    /// A single-sprite character that swaps between named key-pose sprites
    /// (new-way.md: frame/pose-based animation where it fits better than a
    /// puppet). Used for the player and Greve Gast once real art is present.
    /// </summary>
    public sealed class PoseSpriteCharacter : MonoBehaviour
    {
        private SpriteRenderer sr;
        private System.Func<string, Sprite> resolver;
        private readonly Dictionary<string, Sprite> poses = new Dictionary<string, Sprite>();
        private string current;

        public SpriteRenderer Renderer => sr;

        public void Init(System.Func<string, Sprite> spriteResolver, SceneBand band, float depth01)
        {
            resolver = spriteResolver;
            sr = gameObject.GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            LayerSorting.Apply(sr, band, depth01);
        }

        public void SetPose(string pose)
        {
            if (pose == current) return;
            current = pose;
            if (!poses.TryGetValue(pose, out Sprite sprite))
            {
                sprite = resolver != null ? resolver(pose) : null;
                poses[pose] = sprite;
            }
            if (sprite != null) sr.sprite = sprite;
        }
    }
}

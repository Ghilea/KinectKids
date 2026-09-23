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
        private readonly Dictionary<string, Sprite[]> animations = new Dictionary<string, Sprite[]>();
        private string current;

        // Frame-animation state for the active pose.
        private Sprite[] activeFrames;
        private float activeFps;
        private float frameTimer;
        private int frameIndex;

        // Optional running "bob" so a single-frame pose still reads as alive.
        private bool bob;
        private float bobTime;
        private float bobAmplitude;
        private float bobSpeed;
        private Vector3 bobBaseLocalPos;

        public SpriteRenderer Renderer => sr;

        public void Init(System.Func<string, Sprite> spriteResolver, SceneBand band, float depth01)
        {
            resolver = spriteResolver;
            sr = gameObject.GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            LayerSorting.Apply(sr, band, depth01);
        }

        /// <summary>Show a single static pose sprite (resolved as "player_&lt;pose&gt;" etc.).</summary>
        public void SetPose(string pose)
        {
            if (pose == current) return;
            current = pose;
            activeFrames = null; // leaving any running animation
            if (!poses.TryGetValue(pose, out Sprite sprite))
            {
                sprite = resolver != null ? resolver(pose) : null;
                poses[pose] = sprite;
            }
            if (sprite != null) sr.sprite = sprite;
        }

        /// <summary>
        /// Play a looping frame animation built from several pose names (e.g.
        /// run_far / run_mid / run_near). Frames that fail to resolve are skipped;
        /// if none resolve the call is a no-op so the scene still runs.
        /// </summary>
        public void PlayAnimation(string animKey, string[] frameNames, float framesPerSecond)
        {
            if (animKey == current) return;
            current = animKey;

            if (!animations.TryGetValue(animKey, out Sprite[] frames))
            {
                var resolved = new List<Sprite>();
                if (frameNames != null)
                {
                    for (int i = 0; i < frameNames.Length; i++)
                    {
                        Sprite s = resolver != null ? resolver(frameNames[i]) : null;
                        if (s != null) resolved.Add(s);
                    }
                }
                frames = resolved.ToArray();
                animations[animKey] = frames;
            }

            if (frames.Length == 0) return;

            activeFrames = frames;
            activeFps = Mathf.Max(0.01f, framesPerSecond);
            frameTimer = 0f;
            frameIndex = 0;
            sr.sprite = frames[0];
        }

        /// <summary>Enable a subtle vertical bob (used while running) for extra life.</summary>
        public void SetBob(bool enabled, float amplitude = 0.06f, float speed = 9f)
        {
            if (enabled && !bob)
            {
                bobBaseLocalPos = transform.localPosition;
            }
            else if (!enabled && bob)
            {
                transform.localPosition = bobBaseLocalPos;
            }
            bob = enabled;
            bobAmplitude = amplitude;
            bobSpeed = speed;
        }

        private void Update()
        {
            if (activeFrames != null && activeFrames.Length > 1)
            {
                frameTimer += Time.deltaTime;
                float frameLength = 1f / activeFps;
                while (frameTimer >= frameLength)
                {
                    frameTimer -= frameLength;
                    frameIndex = (frameIndex + 1) % activeFrames.Length;
                    sr.sprite = activeFrames[frameIndex];
                }
            }

            if (bob)
            {
                bobTime += Time.deltaTime * bobSpeed;
                // Re-anchor to the current base each frame in case gameplay moved us.
                Vector3 p = transform.localPosition;
                float offset = Mathf.Abs(Mathf.Sin(bobTime)) * bobAmplitude;
                // Only override the Y component contributed by the bob.
                p.y = (bobBaseLocalPos.y) + offset;
                bobBaseLocalPos.x = p.x; // track lateral movement driven elsewhere
                bobBaseLocalPos.z = p.z;
                transform.localPosition = p;
            }
        }
    }
}

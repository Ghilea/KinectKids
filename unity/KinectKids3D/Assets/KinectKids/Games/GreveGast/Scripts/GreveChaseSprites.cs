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

        /// <summary>
        /// Loads a numbered animation exported as prefix_1, prefix_2, ... .
        /// Missing tail frames end the sequence, which makes it safe to add more
        /// authored frames without changing gameplay code.
        /// </summary>
        public static Sprite[] PlayerAnimation(string prefix, int maximumFrames = 16)
            => LoadSequence("Player/player_" + prefix + "_", maximumFrames);

        public static Sprite[] GreveAnimation(string prefix, int maximumFrames = 16)
            => LoadSequence("GreveGast/greve_" + prefix + "_", maximumFrames);

        public static bool HasPlayerArt => Player("idle") != null;
        public static bool HasGreveArt => Greve("idle") != null;

        public static Sprite Load(string relativePath)
        {
            if (cache.TryGetValue(relativePath, out Sprite cached)) return cached;
            Sprite sprite = Resources.Load<Sprite>(Root + relativePath);
            cache[relativePath] = sprite;
            return sprite;
        }

        private static Sprite[] LoadSequence(string relativePrefix, int maximumFrames)
        {
            var frames = new List<Sprite>();
            for (int i = 1; i <= maximumFrames; i++)
            {
                Sprite frame = Load(relativePrefix + i);
                if (frame == null) break;
                frames.Add(frame);
            }
            return frames.ToArray();
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

        // Procedural run cycle: a single run sprite is animated by alternating a
        // horizontal flip (arms/legs visually swap sides), a vertical bob and a
        // slight tilt — so it reads as taking steps instead of zooming.
        private bool running;
        private float runTime;
        private float runCadence = 8f;      // steps per second feel
        private float runBob = 0.10f;       // vertical bounce (local units)
        private float runTilt = 6f;         // degrees of body sway
        private Vector3 runBaseLocalPos;
        private float baseScaleX;           // remembered so flipping keeps size

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
        /// Play a looping frame animation built from several pose names. Frames
        /// that fail to resolve are skipped; if none resolve the call is a no-op
        /// so the scene still runs. NOTE: for the player run we do NOT use this
        /// with the far/mid/near distance images (that just zooms); use
        /// <see cref="PlayRunCycle"/> instead.
        /// </summary>
        public void PlayAnimation(string animKey, string[] frameNames, float framesPerSecond)
        {
            if (animKey == current) return;
            current = animKey;
            running = false;

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

        // Cached result of the real-frame probe: -1 = unknown, 0 = none, 1 = yes.
        private int hasRealRunFrames = -1;
        private string[] realRunFrameNames;

        /// <summary>
        /// Best run animation available: if the project ships REAL run-cycle
        /// frames (run_1, run_2, ... — proper feet-swapping poses) they play as a
        /// true frame animation. Otherwise it falls back to the procedural run
        /// cycle so the game always animates. Dropping in the real frames later
        /// needs no code change.
        /// </summary>
        public void PlaySmartRun(string fallbackPose, float cadence = 8f)
        {
            if (hasRealRunFrames < 0) ProbeRealRunFrames();

            if (hasRealRunFrames == 1)
            {
                StopRunCycle(); // no procedural mirroring when we have real frames
                PlayAnimation("run", realRunFrameNames, Mathf.Max(6f, cadence * 1.4f));
            }
            else
            {
                PlayRunCycle(fallbackPose, cadence);
            }
        }

        /// <summary>Detect a contiguous real run-cycle frame set run_1, run_2, ...</summary>
        private void ProbeRealRunFrames()
        {
            var found = new List<string>();
            for (int i = 1; i <= 12; i++)
            {
                string name = "run_" + i;
                Sprite s = resolver != null ? resolver(name) : null;
                if (s == null) break; // frames must be contiguous from 1
                found.Add(name);
                poses[name] = s;      // cache so PlayAnimation reuses it
            }
            if (found.Count >= 2)
            {
                realRunFrameNames = found.ToArray();
                hasRealRunFrames = 1;
            }
            else
            {
                hasRealRunFrames = 0;
            }
        }

        /// <summary>
        /// Play a procedural front-facing run cycle from a single run sprite:
        /// the sprite bobs, sways and mirrors left/right on each step so the feet
        /// and arms appear to swap — a real running motion rather than a zoom.
        /// </summary>
        public void PlayRunCycle(string runPose, float cadence = 8f)
        {
            if (current != runPose)
            {
                current = runPose;
                activeFrames = null;
                if (!poses.TryGetValue(runPose, out Sprite sprite))
                {
                    sprite = resolver != null ? resolver(runPose) : null;
                    poses[runPose] = sprite;
                }
                if (sprite != null) sr.sprite = sprite;
            }

            if (!running)
            {
                running = true;
                runTime = 0f;
                runBaseLocalPos = transform.localPosition;
                baseScaleX = Mathf.Abs(transform.localScale.x);
            }
            runCadence = cadence;
        }

        /// <summary>Stop the run cycle and restore upright, unflipped transform.</summary>
        public void StopRunCycle()
        {
            if (!running) return;
            running = false;
            Vector3 p = transform.localPosition;
            p.y = runBaseLocalPos.y;
            transform.localPosition = p;
            transform.localRotation = Quaternion.identity;
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x);
            transform.localScale = s;
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

            if (running)
            {
                runTime += Time.deltaTime * runCadence;

                // Vertical bounce, twice per stride (both feet plant per cycle).
                Vector3 p = transform.localPosition;
                float bounce = Mathf.Abs(Mathf.Sin(runTime * Mathf.PI)) * runBob;
                p.y = runBaseLocalPos.y + bounce;
                // Keep tracking lateral movement driven by gameplay.
                runBaseLocalPos.x = p.x;
                runBaseLocalPos.z = p.z;
                transform.localPosition = p;

                // Body sway (slight tilt) in sync with the stride.
                float sway = Mathf.Sin(runTime * Mathf.PI) * runTilt;
                transform.localRotation = Quaternion.Euler(0f, 0f, sway);

                // Mirror the sprite each step so arms/legs swap sides — this is
                // what makes the feet look like they change place.
                bool mirror = Mathf.Sin(runTime * Mathf.PI) < 0f;
                Vector3 s = transform.localScale;
                float mag = baseScaleX > 0.0001f ? baseScaleX : Mathf.Abs(s.x);
                s.x = mirror ? -mag : mag;
                transform.localScale = s;
            }
        }
    }
}

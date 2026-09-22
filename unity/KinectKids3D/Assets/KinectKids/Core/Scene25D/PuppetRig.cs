using System.Collections.Generic;
using UnityEngine;

namespace KinectKids.Scene25D
{
    /// <summary>
    /// A single body part in a 2.5D puppet. Each part is one sprite layer with a
    /// pivot; poses drive its local position/rotation/scale. Swapping the sprite
    /// (placeholder -> final art) needs no code changes.
    /// </summary>
    public sealed class PuppetPart
    {
        public string Name;
        public Transform Transform;
        public SpriteRenderer Renderer;
        public Vector3 RestPosition;
        public Quaternion RestRotation;
        public Vector3 RestScale;
    }

    /// <summary>How one part should be offset from rest for a given pose.</summary>
    [System.Serializable]
    public struct PosePart
    {
        public string part;
        public Vector2 offset;
        public float rotation;
        public float scale;
    }

    /// <summary>A named, pose-based key (new-way.md: prioritise clear key poses).</summary>
    [System.Serializable]
    public sealed class PuppetPose
    {
        public string name;
        public List<PosePart> parts = new List<PosePart>();

        public PuppetPose(string name) { this.name = name; }

        public PuppetPose Set(string part, Vector2 offset, float rotation = 0f, float scale = 1f)
        {
            parts.Add(new PosePart { part = part, offset = offset, rotation = rotation, scale = scale });
            return this;
        }
    }

    /// <summary>
    /// Layered puppet controller. Parts are registered once (from placeholder
    /// silhouettes or authored sprite layers), then gameplay blends between
    /// named key poses. This is the character-animation half of the pipeline for
    /// both the player and Greve Gast.
    /// </summary>
    public sealed class PuppetRig : MonoBehaviour
    {
        private readonly Dictionary<string, PuppetPart> parts = new Dictionary<string, PuppetPart>();
        private readonly Dictionary<string, PuppetPose> poses = new Dictionary<string, PuppetPose>();

        private PuppetPose currentPose;
        private PuppetPose targetPose;
        private float blend = 1f;
        private float blendSpeed = 8f;

        [Tooltip("Ambient bob amplitude to keep idle characters alive.")]
        public float idleBob = 0.04f;
        public float idleBobSpeed = 2.2f;

        private float bobPhase;

        public IReadOnlyDictionary<string, PuppetPart> Parts => parts;

        public PuppetPart AddPart(string name, SpriteRenderer renderer)
        {
            var part = new PuppetPart
            {
                Name = name,
                Transform = renderer.transform,
                Renderer = renderer,
                RestPosition = renderer.transform.localPosition,
                RestRotation = renderer.transform.localRotation,
                RestScale = renderer.transform.localScale
            };
            parts[name] = part;
            return part;
        }

        public void AddPose(PuppetPose pose)
        {
            if (pose != null) poses[pose.name] = pose;
        }

        public void SetPose(string name, bool immediate = false)
        {
            if (!poses.TryGetValue(name, out PuppetPose pose)) return;
            targetPose = pose;
            if (currentPose == null || immediate)
            {
                currentPose = pose;
                blend = 1f;
            }
            else
            {
                blend = 0f;
            }
        }

        public void SetBlendSpeed(float speed) => blendSpeed = Mathf.Max(0.5f, speed);

        private void Update()
        {
            float dt = Time.deltaTime;
            bobPhase += dt * idleBobSpeed;

            if (targetPose != null && blend < 1f)
            {
                blend = Mathf.Clamp01(blend + dt * blendSpeed);
                if (blend >= 1f) currentPose = targetPose;
            }

            ApplyPose();
        }

        private void ApplyPose()
        {
            // Start from rest, then apply blended current/target contributions.
            foreach (KeyValuePair<string, PuppetPart> kv in parts)
            {
                PuppetPart part = kv.Value;
                if (part.Transform == null) continue;

                Vector3 pos = part.RestPosition;
                float rot = 0f;
                float scl = 1f;

                if (currentPose != null) Accumulate(currentPose, part.Name, 1f - (targetPose != null ? blend : 0f), ref pos, ref rot, ref scl);
                if (targetPose != null && targetPose != currentPose) Accumulate(targetPose, part.Name, blend, ref pos, ref rot, ref scl);

                // Idle bob adds subtle life to the whole rig.
                pos.y += Mathf.Sin(bobPhase) * idleBob;

                part.Transform.localPosition = pos;
                part.Transform.localRotation = part.RestRotation * Quaternion.Euler(0, 0, rot);
                part.Transform.localScale = part.RestScale * scl;
            }
        }

        private static void Accumulate(PuppetPose pose, string partName, float weight,
            ref Vector3 pos, ref float rot, ref float scl)
        {
            if (weight <= 0f) return;
            for (int i = 0; i < pose.parts.Count; i++)
            {
                if (pose.parts[i].part != partName) continue;
                PosePart p = pose.parts[i];
                pos += (Vector3)p.offset * weight;
                rot += p.rotation * weight;
                scl = Mathf.Lerp(scl, p.scale, weight);
                return;
            }
        }
    }
}

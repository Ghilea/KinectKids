using System.Collections.Generic;
using UnityEngine;

namespace KinectKids.Scene25D
{
    /// <summary>
    /// A locked / directed camera for 2.5D scenes (new-way.md: cameras are
    /// locked or scripted, never free third-person). It supports:
    ///   * a resting framing,
    ///   * smooth blends between authored shots,
    ///   * light procedural motion (sway, shake) for life and impact feedback.
    ///
    /// The player never controls it directly. Gameplay requests shots by name or
    /// index; everything else is handled here so scenes stay cinematic.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class ScriptedCamera : MonoBehaviour
    {
        [System.Serializable]
        public struct Shot
        {
            public string name;
            public Vector3 position;
            public Vector3 eulerAngles;
            public float orthographicSize;
            public float blendSeconds;
        }

        [Tooltip("Authored camera shots. Index 0 is the default resting shot.")]
        public List<Shot> shots = new List<Shot>();

        [Tooltip("Idle sway amplitude (world units) to keep locked shots alive.")]
        public Vector2 swayAmplitude = new Vector2(0.05f, 0.03f);

        [Tooltip("Idle sway speed.")]
        public Vector2 swaySpeed = new Vector2(0.6f, 0.9f);

        private Camera cam;
        private Shot current;
        private Shot from;
        private Shot target;
        private float blendTime;
        private float blendDuration;
        private bool blending;

        private float shakeAmount;
        private float shakeDecay = 4f;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            cam.orthographic = true;
            if (shots.Count == 0)
            {
                shots.Add(new Shot
                {
                    name = "Default",
                    position = transform.position,
                    eulerAngles = transform.eulerAngles,
                    orthographicSize = cam.orthographicSize,
                    blendSeconds = 0.6f
                });
            }
            current = shots[0];
            target = current;
            ApplyImmediate(current);
        }

        public void CutTo(int index) => GoTo(index, true);
        public void BlendTo(int index) => GoTo(index, false);

        public void BlendTo(string shotName)
        {
            for (int i = 0; i < shots.Count; i++)
                if (shots[i].name == shotName) { GoTo(i, false); return; }
        }

        private void GoTo(int index, bool immediate)
        {
            if (index < 0 || index >= shots.Count) return;
            target = shots[index];
            if (immediate || target.blendSeconds <= 0.001f)
            {
                current = target;
                blending = false;
                return;
            }
            from = current;
            blendTime = 0f;
            blendDuration = target.blendSeconds;
            blending = true;
        }

        /// <summary>Add a one-shot camera shake, e.g. when the player is hit.</summary>
        public void Shake(float amount)
        {
            shakeAmount = Mathf.Max(shakeAmount, amount);
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;

            if (blending)
            {
                blendTime += dt;
                float t = Mathf.Clamp01(blendTime / blendDuration);
                float e = t * t * (3f - 2f * t);
                current.position = Vector3.Lerp(from.position, target.position, e);
                current.eulerAngles = Vector3.Lerp(from.eulerAngles, target.eulerAngles, e);
                current.orthographicSize = Mathf.Lerp(from.orthographicSize, target.orthographicSize, e);
                if (t >= 1f) { current = target; blending = false; }
            }

            // Idle sway keeps a locked shot from feeling dead.
            float sx = Mathf.Sin(Time.time * swaySpeed.x) * swayAmplitude.x;
            float sy = Mathf.Cos(Time.time * swaySpeed.y) * swayAmplitude.y;

            // Impact shake.
            Vector3 shake = Vector3.zero;
            if (shakeAmount > 0.0001f)
            {
                shake = new Vector3(
                    (Random.value - 0.5f) * 2f,
                    (Random.value - 0.5f) * 2f, 0f) * shakeAmount;
                shakeAmount = Mathf.MoveTowards(shakeAmount, 0f, shakeDecay * dt);
            }

            transform.position = current.position + new Vector3(sx, sy, 0f) + shake;
            transform.eulerAngles = current.eulerAngles;
            if (cam != null) cam.orthographicSize = current.orthographicSize;
        }

        private void ApplyImmediate(Shot shot)
        {
            transform.position = shot.position;
            transform.eulerAngles = shot.eulerAngles;
            if (cam != null && shot.orthographicSize > 0f) cam.orthographicSize = shot.orthographicSize;
        }
    }
}

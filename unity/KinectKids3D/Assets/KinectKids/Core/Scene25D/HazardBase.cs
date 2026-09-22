using System;
using UnityEngine;

namespace KinectKids.Scene25D
{
    /// <summary>
    /// Base class for a modular 2.5D chase hazard. A hazard travels from the far
    /// plane toward the camera along the depth axis (using
    /// <see cref="CameraDepthScaler"/>) and, at a resolve point, checks whether
    /// the player performed the required <see cref="DodgeAction"/>.
    ///
    /// Gameplay rules (what dodge is required, timing, hit/avoid) live here and
    /// are fully decoupled from visuals: the sprite is a placeholder now and a
    /// final PNG later, with no rule changes. New hazard kinds only override the
    /// small hooks below.
    /// </summary>
    public abstract class HazardBase : MonoBehaviour
    {
        [Tooltip("Seconds for the hazard to travel from spawn (far) to the player (near).")]
        public float travelSeconds = 2.2f;

        [Tooltip("Depth at which the outcome is decided (1 = right at the player).")]
        public float resolveDepth = 0.92f;

        [Tooltip("The dodge the player must perform to avoid this hazard.")]
        public DodgeAction requiredAction = DodgeAction.Duck;

        protected CameraDepthScaler scaler;
        protected IDodgeSource dodgeSource;

        private float t;
        private bool resolved;
        private HazardResult result = HazardResult.Pending;

        public HazardResult Result => result;
        public bool Finished { get; private set; }

        /// <summary>
        /// Optionally replace the hazard's placeholder sprite with real art.
        /// Behaviour (required dodge, timing) is unchanged; only the visual swaps.
        /// </summary>
        public void OverrideSprite(Sprite sprite)
        {
            if (sprite == null) return;
            SpriteRenderer sr = GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null) sr.sprite = sprite;
        }

        /// <summary>Fired once when the hazard is resolved. bool = avoided.</summary>
        public event Action<HazardBase, bool> Resolved;

        public void Init(IDodgeSource source)
        {
            dodgeSource = source;
            scaler = GetComponent<CameraDepthScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CameraDepthScaler>();
            scaler.band = SceneBand.Hazards;
            OnInit();
        }

        protected virtual void OnInit() { }

        protected virtual void Update()
        {
            if (Finished) return;

            t += Time.deltaTime / Mathf.Max(0.1f, travelSeconds);
            float depth = Mathf.Clamp01(t);
            if (scaler != null) scaler.Apply(depth);
            OnDepth(depth);

            if (!resolved && depth >= resolveDepth)
            {
                resolved = true;
                bool avoided = EvaluateAvoided();
                result = avoided ? HazardResult.Avoided : HazardResult.Hit;
                OnResolved(avoided);
                Resolved?.Invoke(this, avoided);
            }

            if (depth >= 1f)
            {
                Finished = true;
                OnDespawn();
                Destroy(gameObject, 0.15f);
            }
        }

        /// <summary>Default rule: player must currently match the required action.</summary>
        protected virtual bool EvaluateAvoided()
        {
            if (dodgeSource == null) return true;
            return dodgeSource.CurrentDodge == requiredAction;
        }

        protected virtual void OnDepth(float depth01) { }
        protected virtual void OnResolved(bool avoided) { }
        protected virtual void OnDespawn() { }
    }
}

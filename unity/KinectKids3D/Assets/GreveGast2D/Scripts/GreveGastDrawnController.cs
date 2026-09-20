using UnityEngine;

namespace GreveGast2D
{
    /// <summary>
    /// Stable animation API used by chase and timeline code. Callers do not need
    /// to know whether Greve Gast is animated with sprites, clips or crossfades.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public sealed class GreveGastDrawnController : MonoBehaviour
    {
        private static readonly int Running = Animator.StringToHash("Running");
        private static readonly int Reach = Animator.StringToHash("Reach");
        private static readonly int Stumble = Animator.StringToHash("Stumble");
        private static readonly int Laugh = Animator.StringToHash("Laugh");
        private static readonly int Dance = Animator.StringToHash("Dance");
        private static readonly int Catch = Animator.StringToHash("Catch");
        private static readonly int Surprise = Animator.StringToHash("Surprise");

        private Animator animator;

        public float ChaseDistance { get; private set; }
        public bool IsRunning { get; private set; }

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void SetRunning(bool running)
        {
            IsRunning = running;
            if (animator != null) animator.SetBool(Running, running);
        }

        public void SetChaseDistance(float normalizedDistance)
        {
            ChaseDistance = Mathf.Clamp01(normalizedDistance);
        }

        public void SetVisible(bool visible)
        {
            foreach (SpriteRenderer spriteRenderer in GetComponentsInChildren<SpriteRenderer>(true))
                spriteRenderer.enabled = visible && spriteRenderer.gameObject.name != "SpriteB";
        }

        public void PlayReach() => Trigger(Reach);
        public void PlayStumble() => Trigger(Stumble);
        public void PlayLaugh() => Trigger(Laugh);
        public void PlayDance() => Trigger(Dance);
        public void PlayCatch() => Trigger(Catch);
        public void PlaySurprise() => Trigger(Surprise);

        private void Trigger(int parameter)
        {
            if (animator != null) animator.SetTrigger(parameter);
        }
    }
}

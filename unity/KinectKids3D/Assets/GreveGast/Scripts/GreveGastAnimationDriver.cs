using UnityEngine;

namespace GreveGast
{
    [RequireComponent(typeof(Animator))]
    public class GreveGastAnimationDriver : MonoBehaviour
    {
        [Header("Development")]
        [SerializeField] private bool enableDebugKeys = false;

        private Animator animator;

        private static readonly int Running = Animator.StringToHash("Running");
        private static readonly int Reach = Animator.StringToHash("Reach");
        private static readonly int Stumble = Animator.StringToHash("Stumble");
        private static readonly int Dance = Animator.StringToHash("Dance");
        private static readonly int Catch = Animator.StringToHash("Catch");

        private void Awake()
        {
            animator = GetComponent<Animator>();
        }

        public void SetRun(bool value)
        {
            if (animator != null)
                animator.SetBool(Running, value);
        }

        public void PlayReach()
        {
            if (animator != null)
                animator.SetTrigger(Reach);
        }

        public void PlayStumble()
        {
            if (animator != null)
                animator.SetTrigger(Stumble);
        }

        public void PlayDance()
        {
            if (animator != null)
                animator.SetTrigger(Dance);
        }

        public void PlayCatch()
        {
            if (animator != null)
                animator.SetTrigger(Catch);
        }

        public void PlayState(string stateName, float normalizedTime = 0f)
        {
            if (animator != null && !string.IsNullOrWhiteSpace(stateName))
                animator.Play(stateName, 0, Mathf.Clamp01(normalizedTime));
        }

        private void Update()
        {
            if (!enableDebugKeys || animator == null)
                return;

            SetRun(Input.GetKey(KeyCode.R));

            if (Input.GetKeyDown(KeyCode.Alpha1)) PlayReach();
            if (Input.GetKeyDown(KeyCode.Alpha2)) PlayStumble();
            if (Input.GetKeyDown(KeyCode.Alpha3)) PlayDance();
            if (Input.GetKeyDown(KeyCode.Alpha4)) PlayCatch();
        }
    }
}

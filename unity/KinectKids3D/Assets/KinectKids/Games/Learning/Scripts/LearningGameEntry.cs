using UnityEngine;
using KinectKids.Games.Learning;

namespace KinectKids3D.Platform
{
    public sealed class LearningGameEntry : MonoBehaviour
    {
        [SerializeField] private LearningGameMode mode;
        [Tooltip("Use the old flat OnGUI learning game instead of the 2.5D version.")]
        public bool useLegacyFlat = false;

        public void Configure(LearningGameMode value) => mode = value;

        private void Awake()
        {
            if (useLegacyFlat)
            {
                LearningChoiceGameUnity legacy = gameObject.AddComponent<LearningChoiceGameUnity>();
                legacy.Configure(mode);
                return;
            }

            Learning25DGame game = gameObject.AddComponent<Learning25DGame>();
            game.Configure(mode);
        }
    }
}

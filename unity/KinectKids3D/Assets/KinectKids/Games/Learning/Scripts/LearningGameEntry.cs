using UnityEngine;

namespace KinectKids3D.Platform
{
    public sealed class LearningGameEntry : MonoBehaviour
    {
        [SerializeField] private LearningGameMode mode;

        public void Configure(LearningGameMode value) => mode = value;

        private void Awake()
        {
            LearningChoiceGameUnity game = gameObject.AddComponent<LearningChoiceGameUnity>();
            game.Configure(mode);
        }
    }
}

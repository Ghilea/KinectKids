using UnityEngine;

namespace KinectKids3D.Platform
{
    public sealed class GreveGastSceneEntry : MonoBehaviour
    {
        private void Awake()
        {
            if (FindFirstObjectByType<GreveGastStyleCGame>() == null)
            {
                GameObject game = new GameObject("Greve Gast - Style C+ vertical slice");
                game.AddComponent<GreveGastStyleCGame>();
            }
        }
    }
}

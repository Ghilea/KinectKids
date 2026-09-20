using UnityEngine;

namespace KinectKids3D.Platform
{
    public sealed class GreveGastSceneEntry : MonoBehaviour
    {
        private void Awake()
        {
            if (FindFirstObjectByType<GreveGastGame>() == null)
            {
                GameObject game = new GameObject("Greve Gasts teckningsjakt");
                game.AddComponent<GreveGastGame>();
            }
        }
    }
}

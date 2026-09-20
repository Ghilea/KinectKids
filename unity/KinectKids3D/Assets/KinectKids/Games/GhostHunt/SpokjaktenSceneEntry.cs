using UnityEngine;

namespace KinectKids3D.Platform
{
    public sealed class SpokjaktenSceneEntry : MonoBehaviour
    {
        private void Awake()
        {
            if (FindFirstObjectByType<SpokjaktenGame>() == null)
            {
                GameObject game = new GameObject("Spökjakten");
                game.AddComponent<SpokjaktenGame>();
            }
        }
    }
}

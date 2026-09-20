using UnityEngine;

namespace KinectKids3D.Platform
{
    [CreateAssetMenu(menuName = "KinectKids/Game Registry", fileName = "GameRegistry")]
    public sealed class GameRegistry : ScriptableObject
    {
        public GameDefinition[] games;
    }
}

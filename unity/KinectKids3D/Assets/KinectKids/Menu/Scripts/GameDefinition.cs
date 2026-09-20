using UnityEngine;

namespace KinectKids3D.Platform
{
    [CreateAssetMenu(menuName = "KinectKids/Game Definition", fileName = "GameDefinition")]
    public sealed class GameDefinition : ScriptableObject
    {
        public string displayName;
        [TextArea] public string description;
        public string sceneName;
        public Color cardColor = new Color(0.65f, 0.24f, 0.72f);
        public Sprite preview;
        public bool isAvailable = true;
    }

}

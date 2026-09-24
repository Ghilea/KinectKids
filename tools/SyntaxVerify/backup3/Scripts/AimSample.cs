using UnityEngine;

namespace KinectKids3D
{
    public struct AimSample
    {
        public int PlayerIndex;
        public int HandId;
        public Vector2 Position;
        public bool Fire;
        public float GestureProgress;

        public AimSample(int playerIndex, int handId, Vector2 position, bool fire, float gestureProgress)
        {
            PlayerIndex = playerIndex;
            HandId = handId;
            Position = position;
            Fire = fire;
            GestureProgress = gestureProgress;
        }
    }
}

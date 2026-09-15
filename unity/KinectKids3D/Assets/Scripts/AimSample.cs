using UnityEngine;

namespace KinectKids3D
{
    public struct AimSample
    {
        public int PlayerIndex;
        public int HandId;
        public Vector2 Position;

        public AimSample(int playerIndex, int handId, Vector2 position)
        {
            PlayerIndex = playerIndex;
            HandId = handId;
            Position = position;
        }
    }
}

namespace KinectKids3D
{
    public struct PlayerPose
    {
        public int PlayerIndex;
        public long TrackingId;
        public float CenterX;
        public float HeadY;
        public float ShoulderY;

        public PlayerPose(int playerIndex, long trackingId, float centerX, float headY, float shoulderY = 0f)
        {
            PlayerIndex = playerIndex;
            TrackingId = trackingId;
            CenterX = centerX;
            HeadY = headY;
            ShoulderY = shoulderY;
        }
    }
}

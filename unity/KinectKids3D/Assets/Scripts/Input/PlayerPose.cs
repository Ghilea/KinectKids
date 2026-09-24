namespace KinectKids3D
{
    public struct PlayerPose
    {
        public int PlayerIndex;
        public long TrackingId;
        public float CenterX;
        public float HeadY;

        public PlayerPose(int playerIndex, long trackingId, float centerX, float headY)
        {
            PlayerIndex = playerIndex;
            TrackingId = trackingId;
            CenterX = centerX;
            HeadY = headY;
        }
    }
}

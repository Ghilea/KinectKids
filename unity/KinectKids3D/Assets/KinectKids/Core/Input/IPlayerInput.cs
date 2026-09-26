using UnityEngine;

namespace KinectKids3D.Platform
{
    public struct PlayerInputFrame
    {
        public bool IsTracked;
        public bool PlayerChanged;
        public float CenterX;
        public float HeadY;
        public int BodySide;
        public Vector2 RightHand;
        public Vector2 LeftHand;
        public bool Jump;
        public bool Duck;
        public bool MoveLeft;
        public bool MoveRight;
        public bool Run;
        public bool KinectRun;
        public bool Action;
    }

    public interface IPlayerInput
    {
        PlayerInputFrame Frame { get; }
        bool KinectConnected { get; }
        string Status { get; }
        void Tick();
    }
}

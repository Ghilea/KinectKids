using System.Collections.Generic;
using UnityEngine;

namespace KinectKids3D
{
    public enum GreveGastAction { None, Run, Jump, Duck, Left, Right }

    public sealed class GreveGastBodyInput
    {
        private KinectAutoAimProvider provider;
        private readonly Platform.KinectBodyGestureTracker gestures = new Platform.KinectBodyGestureTracker();
        private float runEnergy;

        public bool KinectConnected => Platform.KinectKidsInputManager.Instance != null
            ? Platform.KinectKidsInputManager.Instance.KinectConnected
            : provider != null && provider.KinectConnected;
        public string Status => Platform.KinectKidsInputManager.Instance != null
            ? Platform.KinectKidsInputManager.Instance.Status
            : provider != null ? provider.Status : "Kinect startar";
        public float HeightDelta { get; private set; }
        public float HorizontalDelta { get; private set; }
        public int BodySide { get; private set; }
        public bool PlayerChanged { get; private set; }
        public bool PlayerTracked { get; private set; }
        public float RunEnergy => runEnergy;
        public GreveGastAction Current { get; private set; }

        public void Start()
        {
            if (Platform.KinectKidsInputManager.Instance != null) return;
            provider = new KinectAutoAimProvider();
            provider.Start();
        }

        public void Update()
        {
            if (Platform.KinectKidsInputManager.Instance != null)
            {
                Platform.PlayerInputFrame shared = Platform.KinectKidsInputManager.Instance.Frame;
                HeightDelta = shared.HeadY;
                HorizontalDelta = shared.CenterX;
                BodySide = shared.BodySide;
                PlayerChanged = shared.PlayerChanged;
                PlayerTracked = shared.IsTracked;
                runEnergy = shared.KinectRun ? 1f : 0f;
                Current = GreveGastAction.None;
                if (shared.Run) Current = GreveGastAction.Run;
                if (shared.MoveLeft) Current = GreveGastAction.Left;
                if (shared.MoveRight) Current = GreveGastAction.Right;
                if (shared.Duck) Current = GreveGastAction.Duck;
                if (shared.Jump) Current = GreveGastAction.Jump;
                return;
            }
            if (provider == null) return;
            IReadOnlyList<PlayerPose> poses = provider.GetPlayerPoses();
            Platform.KinectBodyGestureTracker.Motion motion = gestures.Update(provider.KinectConnected && poses.Count > 0,
                poses.Count > 0 ? poses[0] : default(PlayerPose), Time.unscaledDeltaTime, Time.unscaledTime);
            HeightDelta = motion.HeadDelta;
            HorizontalDelta = motion.CenterDelta;
            BodySide = motion.Side;
            PlayerChanged = motion.PlayerChanged;
            PlayerTracked = motion.Tracked;
            runEnergy = motion.Run ? 1f : 0f;

            Current = GreveGastAction.None;
            if (Input.GetKey(KeyCode.W) || motion.Run) Current = GreveGastAction.Run;
            if (motion.Side < 0 || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) Current = GreveGastAction.Left;
            if (motion.Side > 0 || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) Current = GreveGastAction.Right;
            if (motion.Duck || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) Current = GreveGastAction.Duck;
            if (motion.Jump || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.UpArrow)) Current = GreveGastAction.Jump;
        }

        public void Dispose()
        {
            if (provider != null) provider.Dispose();
            provider = null;
        }
    }
}

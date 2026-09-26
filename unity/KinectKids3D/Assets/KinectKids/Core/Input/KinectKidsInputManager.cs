using System.Collections.Generic;
using UnityEngine;

namespace KinectKids3D.Platform
{
    [DefaultExecutionOrder(-100)]
    public sealed class KinectKidsInputManager : MonoBehaviour, IPlayerInput
    {
        private KinectAutoAimProvider provider;
        private PlayerInputFrame frame;
        private readonly KinectBodyGestureTracker gestures = new KinectBodyGestureTracker();
        private readonly List<AimSample> latestAimSamples = new List<AimSample>(4);
        private readonly List<PlayerPose> latestPlayerPoses = new List<PlayerPose>(2);

        public static KinectKidsInputManager Instance { get; private set; }
        public PlayerInputFrame Frame => frame;
        public bool KinectConnected => provider != null && provider.KinectConnected;
        public bool PlayerDetected => KinectConnected && latestPlayerPoses.Count > 0;
        public string Status => provider != null ? provider.Status : "Kinect startar";
        public IReadOnlyList<AimSample> AimSamples => latestAimSamples;
        public IReadOnlyList<PlayerPose> PlayerPoses => latestPlayerPoses;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            provider = new KinectAutoAimProvider();
            provider.Start();
        }

        private void Update() => Tick();

        public void RetryKinect()
        {
            if (provider == null) return;
            gestures.Reset();
            frame = new PlayerInputFrame();
            latestPlayerPoses.Clear();
            latestAimSamples.Clear();
            provider.RetryNow();
        }

        public void Tick()
        {
            if (provider == null) return;
            IReadOnlyList<PlayerPose> poses = provider.GetPlayerPoses();
            IReadOnlyList<AimSample> aims = provider.GetAimSamples();
            latestPlayerPoses.Clear();
            for (int i = 0; i < poses.Count; i++) latestPlayerPoses.Add(poses[i]);
            latestAimSamples.Clear();
            for (int i = 0; i < aims.Count; i++) latestAimSamples.Add(aims[i]);
            bool tracked = KinectConnected && poses.Count > 0;
            KinectBodyGestureTracker.Motion motion = gestures.Update(tracked,
                tracked ? poses[0] : default(PlayerPose), Time.unscaledDeltaTime, Time.unscaledTime);

            Vector2 right = motion.PlayerChanged ? Vector2.zero : frame.RightHand;
            Vector2 left = motion.PlayerChanged ? Vector2.zero : frame.LeftHand;
            bool action = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
            for (int i = 0; i < aims.Count; i++)
            {
                AimSample aim = aims[i];
                if (aim.PlayerIndex != 0) continue;
                if ((aim.HandId & 1) == 1) { right = aim.Position; action |= aim.Fire; }
                else left = aim.Position;
            }

            frame = new PlayerInputFrame
            {
                IsTracked = motion.Tracked,
                PlayerChanged = motion.PlayerChanged,
                CenterX = motion.CenterDelta,
                HeadY = motion.HeadDelta,
                BodySide = motion.Side,
                RightHand = right,
                LeftHand = left,
                Jump = motion.Jump || Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.UpArrow),
                Duck = motion.Duck || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow),
                MoveLeft = motion.Side < 0 || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow),
                MoveRight = motion.Side > 0 || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow),
                KinectRun = motion.Run,
                Run = motion.Run || Input.GetKey(KeyCode.W) ||
                    Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
                Action = action
            };
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (provider != null) provider.Dispose();
        }
    }
}

using System.Collections.Generic;
using UnityEngine;

namespace KinectKids3D.Platform
{
    public sealed class KinectKidsInputManager : MonoBehaviour, IPlayerInput
    {
        private KinectAutoAimProvider provider;
        private PlayerInputFrame frame;
        private float baselineHead = 1.7f;
        private float baselineCenter;
        private float filteredHead = 1.7f;
        private float filteredCenter;
        private float previousHead = 1.7f;
        private float calibrationTime;
        private float runEnergy;
        private readonly List<AimSample> latestAimSamples = new List<AimSample>(4);
        private readonly List<PlayerPose> latestPlayerPoses = new List<PlayerPose>(2);

        public static KinectKidsInputManager Instance { get; private set; }
        public PlayerInputFrame Frame => frame;
        public bool KinectConnected => provider != null && provider.KinectConnected;
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

        public void Tick()
        {
            if (provider == null) return;
            IReadOnlyList<PlayerPose> poses = provider.GetPlayerPoses();
            IReadOnlyList<AimSample> aims = provider.GetAimSamples();
            latestPlayerPoses.Clear();
            for (int i = 0; i < poses.Count; i++) latestPlayerPoses.Add(poses[i]);
            latestAimSamples.Clear();
            for (int i = 0; i < aims.Count; i++) latestAimSamples.Add(aims[i]);
            bool tracked = poses.Count > 0;
            if (tracked)
            {
                PlayerPose pose = poses[0];
                float blend = 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime);
                filteredHead = Mathf.Lerp(filteredHead, pose.HeadY, blend);
                filteredCenter = Mathf.Lerp(filteredCenter, pose.CenterX, blend);
                if (calibrationTime <= 1.6f)
                {
                    calibrationTime += Time.unscaledDeltaTime;
                    baselineHead = Mathf.Lerp(baselineHead, filteredHead, 0.07f);
                    baselineCenter = Mathf.Lerp(baselineCenter, filteredCenter, 0.07f);
                }
            }

            Vector2 right = frame.RightHand;
            Vector2 left = frame.LeftHand;
            bool action = Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0);
            for (int i = 0; i < aims.Count; i++)
            {
                AimSample aim = aims[i];
                if (aim.PlayerIndex != 0) continue;
                if ((aim.HandId & 1) == 1) { right = aim.Position; action |= aim.Fire; }
                else left = aim.Position;
            }

            float velocity = Mathf.Abs(filteredHead - previousHead) / Mathf.Max(0.005f, Time.unscaledDeltaTime);
            previousHead = filteredHead;
            runEnergy = Mathf.Lerp(runEnergy, Mathf.Clamp01((velocity - 0.05f) * 3.2f),
                1f - Mathf.Exp(-7f * Time.unscaledDeltaTime));
            float heightDelta = filteredHead - baselineHead;
            float horizontalDelta = filteredCenter - baselineCenter;
            frame = new PlayerInputFrame
            {
                IsTracked = tracked && KinectConnected,
                CenterX = horizontalDelta,
                HeadY = heightDelta,
                RightHand = right,
                LeftHand = left,
                Jump = heightDelta > 0.14f || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow),
                Duck = heightDelta < -0.19f || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow),
                MoveLeft = horizontalDelta < -0.16f || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow),
                MoveRight = horizontalDelta > 0.16f || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow),
                Run = runEnergy > 0.44f || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
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

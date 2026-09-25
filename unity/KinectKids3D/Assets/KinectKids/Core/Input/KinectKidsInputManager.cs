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
        private float baselineBody = 1.35f;
        private float filteredBody = 1.35f;
        private float filteredCenter;
        private float previousHead = 1.7f;
        private float previousBody = 1.35f;
        private float calibrationTime;
        private float runEnergy;
        private float jumpUntil;
        private float duckUntil;
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
            calibrationTime = 0f;
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
            bool tracked = poses.Count > 0;
            if (tracked)
            {
                PlayerPose pose = poses[0];
                float blend = 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime);
                filteredHead = Mathf.Lerp(filteredHead, pose.HeadY, blend);
                float bodyY = Mathf.Abs(pose.ShoulderY) > 0.001f ? pose.ShoulderY : pose.HeadY - 0.35f;
                filteredBody = Mathf.Lerp(filteredBody, bodyY, blend);
                filteredCenter = Mathf.Lerp(filteredCenter, pose.CenterX, blend);
                if (calibrationTime <= 1.6f)
                {
                    calibrationTime += Time.unscaledDeltaTime;
                    baselineHead = Mathf.Lerp(baselineHead, filteredHead, 0.07f);
                    baselineBody = Mathf.Lerp(baselineBody, filteredBody, 0.07f);
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

            float bodyVelocity = (filteredBody - previousBody) / Mathf.Max(0.005f, Time.unscaledDeltaTime);
            float velocity = Mathf.Abs(bodyVelocity);
            previousHead = filteredHead;
            previousBody = filteredBody;
            runEnergy = Mathf.Lerp(runEnergy, Mathf.Clamp01((velocity - 0.08f) * 2.6f),
                1f - Mathf.Exp(-7f * Time.unscaledDeltaTime));
            float heightDelta = filteredHead - baselineHead;
            float bodyDelta = filteredBody - baselineBody;
            float horizontalDelta = filteredCenter - baselineCenter;
            if (bodyDelta > 0.10f && bodyVelocity > 0.05f) jumpUntil = Time.unscaledTime + 0.34f;
            if (bodyDelta < -0.10f && bodyVelocity < -0.05f) duckUntil = Time.unscaledTime + 0.42f;
            frame = new PlayerInputFrame
            {
                IsTracked = tracked && KinectConnected,
                CenterX = horizontalDelta,
                HeadY = heightDelta,
                RightHand = right,
                LeftHand = left,
                Jump = Time.unscaledTime < jumpUntil || Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow),
                Duck = Time.unscaledTime < duckUntil || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow),
                MoveLeft = horizontalDelta < -0.16f || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow),
                MoveRight = horizontalDelta > 0.16f || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow),
                Run = runEnergy > 0.24f || Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift),
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

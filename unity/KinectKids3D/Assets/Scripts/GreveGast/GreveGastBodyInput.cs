using System.Collections.Generic;
using UnityEngine;

namespace KinectKids3D
{
    public enum GreveGastAction { None, Run, Jump, Duck, Left, Right }

    public sealed class GreveGastBodyInput
    {
        private readonly KinectAutoAimProvider provider = new KinectAutoAimProvider();
        private float baselineHead = 1.7f;
        private float baselineCenter;
        private float filteredHead = 1.7f;
        private float filteredCenter;
        private float previousHead = 1.7f;
        private float calibrationTime;
        private float runEnergy;
        private bool initialized;

        public bool KinectConnected => provider.KinectConnected;
        public string Status => provider.Status;
        public float HeightDelta { get; private set; }
        public float HorizontalDelta { get; private set; }
        public float RunEnergy => runEnergy;
        public GreveGastAction Current { get; private set; }

        public void Start() => provider.Start();

        public void Update()
        {
            IReadOnlyList<PlayerPose> poses = provider.GetPlayerPoses();
            if (poses.Count > 0)
            {
                PlayerPose pose = poses[0];
                if (!initialized)
                {
                    initialized = true;
                    filteredHead = previousHead = baselineHead = pose.HeadY;
                    filteredCenter = baselineCenter = pose.CenterX;
                }
                float blend = 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime);
                filteredHead = Mathf.Lerp(filteredHead, pose.HeadY, blend);
                filteredCenter = Mathf.Lerp(filteredCenter, pose.CenterX, blend);
                if (calibrationTime < 1.6f)
                {
                    calibrationTime += Time.unscaledDeltaTime;
                    baselineHead = Mathf.Lerp(baselineHead, filteredHead, 0.07f);
                    baselineCenter = Mathf.Lerp(baselineCenter, filteredCenter, 0.07f);
                }
            }

            HeightDelta = filteredHead - baselineHead;
            HorizontalDelta = filteredCenter - baselineCenter;
            float velocity = Mathf.Abs(filteredHead - previousHead) / Mathf.Max(0.005f, Time.unscaledDeltaTime);
            previousHead = filteredHead;
            runEnergy = Mathf.Lerp(runEnergy, Mathf.Clamp01((velocity - 0.05f) * 3.2f),
                1f - Mathf.Exp(-7f * Time.unscaledDeltaTime));

            Current = GreveGastAction.None;
            if (Input.GetKey(KeyCode.W) || runEnergy > 0.44f) Current = GreveGastAction.Run;
            if (HeightDelta > 0.14f || Input.GetKey(KeyCode.Space)) Current = GreveGastAction.Jump;
            if (HeightDelta < -0.19f || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) Current = GreveGastAction.Duck;
            if (HorizontalDelta < -0.16f || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) Current = GreveGastAction.Left;
            if (HorizontalDelta > 0.16f || Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) Current = GreveGastAction.Right;
        }

        public void Dispose() => provider.Dispose();
    }
}

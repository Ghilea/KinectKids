using System;
using System.Collections.Generic;
using UnityEngine;

namespace KinectKids3D
{
    /// <summary>
    /// Startar Kinect utan att frysa huvudtråden. Musen fungerar medan bryggan
    /// startar eller saknar sensor, och byts automatiskt bort när Kinect är redo.
    /// </summary>
    public sealed class KinectAutoAimProvider : IAimProvider
    {
        private readonly MouseAimProvider mouse = new MouseAimProvider();
        private KinectBridgeAimProvider bridge;
        private float nextRetryAt;
        private bool disposed;

        public bool IsAvailable => !disposed;
        public bool KinectConnected => bridge != null && bridge.IsAvailable;
        public string Status
        {
            get
            {
                if (bridge == null) return "Kinect-bryggan väntar på återanslutning";
                return bridge.Status;
            }
        }

        public void Start()
        {
            StartBridge();
        }

        public void Tick()
        {
            if (disposed || KinectConnected || bridge == null || !bridge.HasFailed) return;
            if (Time.realtimeSinceStartup < nextRetryAt) return;
            StartBridge();
        }

        public void RetryNow()
        {
            if (disposed || KinectConnected) return;
            nextRetryAt = 0f;
            StartBridge();
        }

        private void StartBridge()
        {
            if (disposed) return;
            if (bridge != null) bridge.Dispose();
            bridge = new KinectBridgeAimProvider();
            bool launched = bridge.TryStart();
            nextRetryAt = Time.realtimeSinceStartup + (launched ? 5f : 7f);
        }

        public IReadOnlyList<AimSample> GetAimSamples()
        {
            Tick();
            return KinectConnected ? bridge.GetAimSamples() : mouse.GetAimSamples();
        }

        public IReadOnlyList<PlayerPose> GetPlayerPoses()
        {
            Tick();
            return KinectConnected ? bridge.GetPlayerPoses() : mouse.GetPlayerPoses();
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            if (bridge != null) bridge.Dispose();
            bridge = null;
            mouse.Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace KinectKids3D
{
    /// <summary>
    /// Gemensam bas för Kinect-baserade <see cref="IAimProvider"/>-implementationer.
    /// Håller den trådsäkra bufferten av sikten och spelarposer samt statustext –
    /// logik som är identisk mellan bridge- (pipe) och V1- (reflection) varianterna.
    ///
    /// Själva inläsningen (transport, gestigenkänning, uppstart/avstängning) ligger
    /// kvar i de konkreta klasserna eftersom den skiljer sig helt åt.
    /// </summary>
    public abstract class KinectAimProviderBase : IAimProvider
    {
        protected readonly object sync = new object();
        protected readonly List<AimSample> latest = new List<AimSample>(4);
        protected readonly List<PlayerPose> latestPoses = new List<PlayerPose>(2);
        protected volatile string status = string.Empty;
        private volatile bool available;

        public bool IsAvailable { get => available; protected set => available = value; }
        public string Status => status;

        public IReadOnlyList<AimSample> GetAimSamples()
        {
            lock (sync) return latest.ToArray();
        }

        public IReadOnlyList<PlayerPose> GetPlayerPoses()
        {
            lock (sync) return latestPoses.ToArray();
        }

        public abstract void Dispose();

        /// <summary>
        /// Per-hand gesttillstånd (rörelseutjämning, låst sikte, avkylning).
        /// Delad datastruktur mellan providers.
        /// </summary>
        protected sealed class HandGestureState
        {
            public bool Initialized;
            public bool PunchReady;
            public float LastDepth;
            public float SmoothedForwardSpeed;
            public Vector2 LastAim;
            public Vector2 LockedAim;
            public DateTime LastAt;
            public DateTime CooldownUntil;
            public DateTime AimLockedUntil;
        }
    }
}

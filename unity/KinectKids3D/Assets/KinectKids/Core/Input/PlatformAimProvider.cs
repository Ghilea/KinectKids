using System.Collections.Generic;

namespace KinectKids3D.Platform
{
    /// <summary>
    /// Compatibility adapter for migrated games that still consume IAimProvider.
    /// It never owns or restarts the Kinect bridge.
    /// </summary>
    public sealed class PlatformAimProvider : IAimProvider
    {
        private KinectKidsInputManager Input => KinectKidsInputManager.Instance;
        public bool IsAvailable => Input != null;
        public string Status => Input != null ? Input.Status : "Gemensam input saknas";
        public IReadOnlyList<AimSample> GetAimSamples() => Input != null ? Input.AimSamples : EmptyAim;
        public IReadOnlyList<PlayerPose> GetPlayerPoses() => Input != null ? Input.PlayerPoses : EmptyPoses;
        public void Dispose() { }

        private static readonly AimSample[] EmptyAim = new AimSample[0];
        private static readonly PlayerPose[] EmptyPoses = new PlayerPose[0];
    }
}

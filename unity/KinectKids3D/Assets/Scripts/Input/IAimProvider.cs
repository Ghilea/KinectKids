using System;
using System.Collections.Generic;

namespace KinectKids3D
{
    public interface IAimProvider : IDisposable
    {
        bool IsAvailable { get; }
        string Status { get; }
        IReadOnlyList<AimSample> GetAimSamples();
        IReadOnlyList<PlayerPose> GetPlayerPoses();
    }
}

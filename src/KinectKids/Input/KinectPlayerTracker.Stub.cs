using System;
using System.Collections.Generic;
using KinectKids.Models;

namespace KinectKids.Input
{
    // Används bara av GitHub Actions för att kompilera och kontrollera hela
    // WPF-programmet när den gamla Kinect SDK:n inte finns på byggservern.
    // Vanliga Windows-byggen använder KinectPlayerTracker.cs i stället.
    public sealed class KinectPlayerTracker : IPlayerTracker
    {
        public event EventHandler<IReadOnlyList<TrackedPlayer>> PlayersChanged;
        public event EventHandler<string> StatusChanged;

        public bool IsConnected => false;
        public string Status => "Kinect SDK-simulator";

        public void Start()
        {
            StatusChanged?.Invoke(this, Status);
            PlayersChanged?.Invoke(this, Array.Empty<TrackedPlayer>());
        }

        public void Stop()
        {
        }

        public void Dispose()
        {
        }
    }
}

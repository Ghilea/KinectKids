using System;
using System.Collections.Generic;
using KinectKids.Models;

namespace KinectKids.Input
{
    public interface IPlayerTracker : IDisposable
    {
        event EventHandler<IReadOnlyList<TrackedPlayer>> PlayersChanged;
        event EventHandler<string> StatusChanged;
        bool IsConnected { get; }
        string Status { get; }
        void Start();
        void Stop();
    }
}

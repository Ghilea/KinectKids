using System;
using System.Collections.Generic;
using System.Windows;
using KinectKids.Models;

namespace KinectKids.Input
{
    /// <summary>
    /// Kinect är alltid förstahandsval. Musen aktiveras automatiskt som reserv
    /// när sensorn inte kan öppnas och försvinner igen när Kinect återansluts.
    /// </summary>
    public sealed class AutomaticPlayerTracker : IPlayerTracker
    {
        private readonly KinectPlayerTracker kinect = new KinectPlayerTracker();
        private readonly MousePlayerTracker mouse;

        public AutomaticPlayerTracker(FrameworkElement surface)
        {
            mouse = new MousePlayerTracker(surface);
        }

        public event EventHandler<IReadOnlyList<TrackedPlayer>> PlayersChanged;
        public event EventHandler<string> StatusChanged;

        public bool IsConnected => kinect.IsConnected;
        public string Status => kinect.IsConnected
            ? kinect.Status
            : kinect.Status + " – musreserv aktiv";

        public void Start()
        {
            kinect.PlayersChanged += OnKinectPlayersChanged;
            kinect.StatusChanged += OnKinectStatusChanged;
            mouse.PlayersChanged += OnMousePlayersChanged;
            kinect.Start();
            mouse.Start();
            StatusChanged?.Invoke(this, Status);
        }

        public void Stop()
        {
            kinect.Stop();
            mouse.Stop();
            kinect.PlayersChanged -= OnKinectPlayersChanged;
            kinect.StatusChanged -= OnKinectStatusChanged;
            mouse.PlayersChanged -= OnMousePlayersChanged;
        }

        private void OnKinectPlayersChanged(object sender, IReadOnlyList<TrackedPlayer> current)
        {
            if (kinect.IsConnected) PlayersChanged?.Invoke(this, current);
        }

        private void OnMousePlayersChanged(object sender, IReadOnlyList<TrackedPlayer> current)
        {
            if (!kinect.IsConnected) PlayersChanged?.Invoke(this, current);
        }

        private void OnKinectStatusChanged(object sender, string status)
        {
            StatusChanged?.Invoke(this, Status);
        }

        public void Dispose() => Stop();
    }
}

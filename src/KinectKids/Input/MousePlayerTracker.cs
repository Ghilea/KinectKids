using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using KinectKids.Models;

namespace KinectKids.Input
{
    public sealed class MousePlayerTracker : IPlayerTracker
    {
        private readonly FrameworkElement surface;
        private readonly List<TrackedPlayer> currentPlayers = new List<TrackedPlayer>();

        public MousePlayerTracker(FrameworkElement surface)
        {
            this.surface = surface;
        }

        public event EventHandler<IReadOnlyList<TrackedPlayer>> PlayersChanged;
        public event EventHandler<string> StatusChanged;
        public bool IsConnected => true;
        public string Status => "Testläge med mus";

        public void Start()
        {
            surface.MouseMove += OnMouseMove;
            surface.MouseLeave += OnMouseLeave;
            StatusChanged?.Invoke(this, Status);
        }

        public void Stop()
        {
            surface.MouseMove -= OnMouseMove;
            surface.MouseLeave -= OnMouseLeave;
            currentPlayers.Clear();
            PlayersChanged?.Invoke(this, currentPlayers);
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            var point = e.GetPosition(surface);
            double width = Math.Max(1, surface.ActualWidth);
            double height = Math.Max(1, surface.ActualHeight);
            var normalized = new Point(point.X / width, point.Y / height);
            currentPlayers.Clear();
            currentPlayers.Add(new TrackedPlayer(-1, normalized, normalized, new Point(0.5, 0.15), new Point(0.5, 0.6), true));
            PlayersChanged?.Invoke(this, currentPlayers);
        }

        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            currentPlayers.Clear();
            PlayersChanged?.Invoke(this, currentPlayers);
        }

        public void Dispose() => Stop();
    }
}

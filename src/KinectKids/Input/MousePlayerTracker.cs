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
        public event EventHandler<ColorFrameEventArgs> ColorFrameReady { add { } remove { } }
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
            var joints = new Dictionary<BodyJoint, Point>
            {
                [BodyJoint.Head] = new Point(normalized.X, 0.16),
                [BodyJoint.ShoulderCenter] = new Point(normalized.X, 0.28),
                [BodyJoint.ShoulderLeft] = new Point(normalized.X - 0.08, 0.3),
                [BodyJoint.ShoulderRight] = new Point(normalized.X + 0.08, 0.3),
                [BodyJoint.ElbowLeft] = new Point(normalized.X - 0.12, 0.4),
                [BodyJoint.ElbowRight] = new Point(normalized.X + 0.12, 0.4),
                [BodyJoint.WristLeft] = normalized,
                [BodyJoint.WristRight] = normalized,
                [BodyJoint.HandLeft] = normalized,
                [BodyJoint.HandRight] = normalized,
                [BodyJoint.Spine] = new Point(normalized.X, 0.45),
                [BodyJoint.HipCenter] = new Point(normalized.X, 0.58),
                [BodyJoint.HipLeft] = new Point(normalized.X - 0.05, 0.59),
                [BodyJoint.HipRight] = new Point(normalized.X + 0.05, 0.59),
                [BodyJoint.KneeLeft] = new Point(normalized.X - 0.05, 0.74),
                [BodyJoint.KneeRight] = new Point(normalized.X + 0.05, 0.74),
                [BodyJoint.AnkleLeft] = new Point(normalized.X - 0.05, 0.9),
                [BodyJoint.AnkleRight] = new Point(normalized.X + 0.05, 0.9),
                [BodyJoint.FootLeft] = new Point(normalized.X - 0.07, 0.94),
                [BodyJoint.FootRight] = new Point(normalized.X + 0.07, 0.94)
            };
            currentPlayers.Clear();
            currentPlayers.Add(new TrackedPlayer(-1, joints, true));
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

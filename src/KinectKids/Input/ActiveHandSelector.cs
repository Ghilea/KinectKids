using System;
using System.Collections.Generic;
using System.Windows;
using KinectKids.Models;

namespace KinectKids.Input
{
    /// <summary>
    /// Väljer automatiskt den hand som spelaren faktiskt använder. Valet ligger
    /// kvar tills den andra handen rör sig tydligt mer, vilket hindrar fladdrande
    /// sikten och kan återanvändas av alla nuvarande och framtida spel.
    /// </summary>
    public sealed class ActiveHandSelector
    {
        private readonly Dictionary<long, HandState> states = new Dictionary<long, HandState>();

        public ActiveHandSelection Select(TrackedPlayer player, DateTime now)
        {
            HandState state;
            if (!states.TryGetValue(player.TrackingId, out state))
            {
                state = new HandState { RightActive = IsTracked(player.RightHand), LastSwitch = now };
                states[player.TrackingId] = state;
            }

            bool leftTracked = IsTracked(player.LeftHand);
            bool rightTracked = IsTracked(player.RightHand);
            double leftDelta = leftTracked && state.HasLeft ? Distance(player.LeftHand, state.Left) : 0;
            double rightDelta = rightTracked && state.HasRight ? Distance(player.RightHand, state.Right) : 0;
            double leftVertical = leftTracked && state.HasLeft ? player.LeftHand.Y - state.Left.Y : 0;
            double rightVertical = rightTracked && state.HasRight ? player.RightHand.Y - state.Right.Y : 0;

            state.LeftActivity = state.LeftActivity * 0.82 + Math.Min(0.18, leftDelta);
            state.RightActivity = state.RightActivity * 0.82 + Math.Min(0.18, rightDelta);
            if (!leftTracked) state.RightActive = true;
            else if (!rightTracked) state.RightActive = false;
            else if ((now - state.LastSwitch).TotalMilliseconds >= 520)
            {
                double active = state.RightActive ? state.RightActivity : state.LeftActivity;
                double other = state.RightActive ? state.LeftActivity : state.RightActivity;
                if (other > active * 1.32 + 0.012)
                {
                    state.RightActive = !state.RightActive;
                    state.LastSwitch = now;
                }
            }

            state.Left = player.LeftHand;
            state.Right = player.RightHand;
            state.HasLeft = leftTracked;
            state.HasRight = rightTracked;
            return new ActiveHandSelection
            {
                Position = state.RightActive ? player.RightHand : player.LeftHand,
                IsRightHand = state.RightActive,
                VerticalDelta = state.RightActive ? rightVertical : leftVertical
            };
        }

        public void Reset() => states.Clear();

        private static bool IsTracked(Point point) =>
            point.X >= 0 && point.X <= 1 && point.Y >= 0 && point.Y <= 1;

        private static double Distance(Point first, Point second)
        {
            double x = first.X - second.X;
            double y = first.Y - second.Y;
            return Math.Sqrt(x * x + y * y);
        }

        private sealed class HandState
        {
            public Point Left;
            public Point Right;
            public bool HasLeft;
            public bool HasRight;
            public bool RightActive;
            public double LeftActivity;
            public double RightActivity;
            public DateTime LastSwitch;
        }
    }

    public struct ActiveHandSelection
    {
        public Point Position { get; set; }
        public bool IsRightHand { get; set; }
        public double VerticalDelta { get; set; }
    }
}

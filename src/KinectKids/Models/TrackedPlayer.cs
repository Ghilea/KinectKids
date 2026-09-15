using System.Windows;

namespace KinectKids.Models
{
    public sealed class TrackedPlayer
    {
        public TrackedPlayer(long trackingId, Point leftHand, Point rightHand, Point head, Point hip, bool isReady)
        {
            TrackingId = trackingId;
            LeftHand = leftHand;
            RightHand = rightHand;
            Head = head;
            Hip = hip;
            IsReady = isReady;
        }

        public long TrackingId { get; }
        public Point LeftHand { get; }
        public Point RightHand { get; }
        public Point Head { get; }
        public Point Hip { get; }
        public bool IsReady { get; }
    }
}

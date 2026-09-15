using System.Collections.Generic;
using System.Windows;

namespace KinectKids.Models
{
    public sealed class TrackedPlayer
    {
        public TrackedPlayer(long trackingId, IReadOnlyDictionary<BodyJoint, Point> joints, bool isReady)
        {
            TrackingId = trackingId;
            Joints = joints;
            IsReady = isReady;
        }

        public long TrackingId { get; }
        public IReadOnlyDictionary<BodyJoint, Point> Joints { get; }
        public Point LeftHand => Joint(BodyJoint.HandLeft);
        public Point RightHand => Joint(BodyJoint.HandRight);
        public Point Head => Joint(BodyJoint.Head);
        public Point Hip => Joint(BodyJoint.HipCenter);
        public bool IsReady { get; }

        public Point Joint(BodyJoint joint)
        {
            Point point;
            return Joints != null && Joints.TryGetValue(joint, out point) ? point : new Point(-1, -1);
        }
    }
}

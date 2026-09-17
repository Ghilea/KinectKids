// En minimal CI-simulator som gör att den riktiga bryggkoden kan kompileras
// utan att Kinect SDK 1.8 installeras på GitHubs byggdator.
using System;
using System.Collections.Generic;

namespace Microsoft.Kinect
{
    public enum KinectStatus { Connected, Disconnected }
    public enum DepthImageFormat { Resolution320x240Fps30 }
    public enum SkeletonTrackingState { NotTracked, PositionOnly, Tracked }
    public enum JointTrackingState { NotTracked, Inferred, Tracked }
    public enum JointType { ShoulderCenter, Head, HandLeft, HandRight }

    public struct SkeletonPoint
    {
        public float X;
        public float Y;
        public float Z;
    }

    public struct DepthImagePoint
    {
        public int X;
        public int Y;
    }

    public struct Joint
    {
        public JointTrackingState TrackingState { get; set; }
        public SkeletonPoint Position { get; set; }
    }

    public sealed class JointCollection
    {
        public Joint this[JointType type] => new Joint();
    }

    public sealed class Skeleton
    {
        public SkeletonTrackingState TrackingState { get; set; }
        public SkeletonPoint Position { get; set; }
        public int TrackingId { get; set; }
        public JointCollection Joints { get; } = new JointCollection();
    }

    public sealed class SkeletonFrame : IDisposable
    {
        public int SkeletonArrayLength => 0;
        public void CopySkeletonDataTo(Skeleton[] skeletons) { }
        public void Dispose() { }
    }

    public sealed class SkeletonFrameReadyEventArgs : EventArgs
    {
        public SkeletonFrame OpenSkeletonFrame() => null;
    }

    public sealed class DepthImageStream
    {
        public void Enable(DepthImageFormat format) { }
    }

    public sealed class SkeletonStream
    {
        public void Enable() { }
    }

    public sealed class CoordinateMapper
    {
        public DepthImagePoint MapSkeletonPointToDepthPoint(SkeletonPoint point, DepthImageFormat format)
        {
            return new DepthImagePoint();
        }
    }

    public sealed class KinectSensor
    {
        public static IList<KinectSensor> KinectSensors { get; } = new List<KinectSensor>();
        public KinectStatus Status { get; set; }
        public DepthImageStream DepthStream { get; } = new DepthImageStream();
        public SkeletonStream SkeletonStream { get; } = new SkeletonStream();
        public CoordinateMapper CoordinateMapper { get; } = new CoordinateMapper();
        public event EventHandler<SkeletonFrameReadyEventArgs> SkeletonFrameReady;
        public void Start() { }
        public void Stop() { }
    }
}

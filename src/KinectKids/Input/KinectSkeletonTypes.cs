using System;
using Microsoft.Kinect;

namespace KinectKids.Input
{
    /// <summary>
    /// Skeleton frame reader for gesture detection.
    /// </summary>
    public sealed class SkeletonFrameReader : IDisposable
    {
        private readonly object _lock = new object();
        private EventHandler<SkeletonFrameArrivedEventArgs> _frameArrived;

        public event EventHandler<SkeletonFrameArrivedEventArgs> FrameArrived
        {
            add => this._lock.EnterLock(ref _frameArrived, val => val += value);
            remove => this._lock.ExitLock(ref _frameArrived, val => val -= value);
        }

        public void Dispose()
        {
            this._lock.ExitLock(ref _frameArrived, _ => {});
        }

        private ref EventHandler<SkeletonFrameArrivedEventArgs> FrameArrivedLock()
        {
            return ref this._lock.EnterLock(ref _frameArrived);
        }
    }

    /// <summary>
    /// Stub SkeletonSensor for when Kinect SDK is available but sensor needs abstraction.
    /// </summary>
    public sealed class SkeletonSensor : IDisposable
    {
        public bool IsConnected => true;

        public void Open() { }

        public SkeletonFrameReader OpenSkeletonFrameReader()
        {
            return new SkeletonFrameReader();
        }

        public event EventHandler<StatusChangedEventArgs> Disconnected = delegate { };

        public void Dispose() { }
    }

    /// <summary>
    /// Stub HandStatus for gesture detection.
    /// </summary>
    public sealed class HandStatus : IDisposable
    {
        public bool IsTracking => true;
        public System.Collections.Generic.Dictionary<BodyJointType, GesturePoint> TrackingJoints = new System.Collections.Generic.Dictionary<BodyJointType, GesturePoint>();

        public void Dispose() { }
    }

    /// <summary>
    /// Stub Skeleton for gesture processing.
    /// </summary>
    public sealed class Skeleton : IDisposable
    {
        public HandStatus Left => new HandStatus();
        public HandStatus Right => new HandStatus();
        public System.Collections.Generic.Dictionary<int, GesturePoint> Joints = new System.Collections.Generic.Dictionary<int, GesturePoint>();

        public void Dispose() { }
    }

    /// <summary>
    /// Stub SkeletonFrame for frame processing.
    /// </summary>
    public sealed class SkeletonFrame : IDisposable
    {
        public int SkeletonArrayLength => 0;
        public System.Collections.Generic.Dictionary<int, HandStatus> GetSkeletonArrayElement(int index) => null;

        public void Dispose() { }
    }

    /// <summary>
    /// Stub StatusChangedEventArgs for sensor events.
    /// </summary>
    public class StatusChangedEventArgs : EventArgs
    {
        public KinectStatus Status { get; set; }
        public object Sensor { get; set; }
    }

    /// <summary>
    /// Stub SkeletonFrameArrivedEventArgs for frame events.
    /// </summary>
    public struct SkeletonFrameArrivedEventArgs : IDisposable
    {
        public System.Collections.Generic.Dictionary<int, GesturePoint> Frame = new System.Collections.Generic.Dictionary<int, GesturePoint>();

        public void Dispose()
        {
            // Frame data is immutable
        }
    }

    /// <summary>
    /// Stub HandStatus mapping to BodyJointType joints.
    /// </summary>
    public enum BodyJointType
    {
        Head,
        ShoulderCenter,
        ShoulderLeft,
        ShoulderRight,
        ElbowLeft,
        ElbowRight,
        WristLeft,
        WristRight,
        HandLeft,
        HandRight,
        Spine,
        HipCenter,
        HipLeft,
        HipRight,
        KneeLeft,
        KneeRight,
        AnkleLeft,
        AnkleRight,
        FootLeft,
        FootRight
    }

    public enum GestureType
    {
        None,
        HandsUp,
        HandsPlus,
        HandsDown,
        LeftSwipe,
        RightSwipe,
        Tap
    }

    /// <summary>
    /// Stub Point structure for gesture events (not System.Windows.Point).
    /// </summary>
    public struct GesturePoint
    {
        public double X;
        public double Y;

        public GesturePoint(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static GesturePoint Origin => new GesturePoint(0, 0);
        public static GesturePoint NegativeOne => new GesturePoint(-1, -1);
    }
}

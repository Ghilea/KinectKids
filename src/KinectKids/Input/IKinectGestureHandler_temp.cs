using System;

namespace KinectKids.Input
{
    public interface IKinectGestureHandler : IDisposable
    {
        event EventHandler<GestureEventArgs> OnGestureDetected;
        bool IsConnected { get; }
        string Status { get; }
        void Start();
        void Stop();
    }

    public class GestureEventArgs : EventArgs
    {
        public GestureType Gesture { get; set; }
        public Point LeftHand { get; set; }
        public Point RightHand { get; set; }
        public float Confidence { get; set; }
    }

    /// <summary>
    /// Stub implementation for offline development.
    /// Use this when Kinect SDK is not available.
    /// </summary>
    public sealed class KinectGestureHandler.Stub : IKinectGestureHandler
    {
        public event EventHandler<GestureEventArgs> OnGestureDetected;
        public bool IsConnected => false;
        public string Status => "Offline - använd mus eller simulering";

        public void Start()
        {
            // No-op for stub implementation
        }

        public void Stop()
        {
            // No-op for stub implementation
        }

        public void Dispose()
        {
            // No-op for stub implementation
        }
    }

    /// <summary>
    /// Full Kinect gesture handler using Microsoft.Kinect SDK v1.8.
    /// </summary>
    public sealed class KinectGestureHandler : IKinectGestureHandler
    {
        private SkeletonFrameReader skeletonReader;
        private readonly object _lock = new object();

        public event EventHandler<GestureEventArgs> OnGestureDetected;
        public bool IsConnected => skeletonReader != null;
        public string Status => skeletonReader == null ? "Ingen Kinect" : "Pågående";

        public void Start()
        {
            try
            {
                var sensor = new SkeletonSensor();
                if (sensor.IsConnected)
                {
                    sensor.Open();
                    skeletonReader = sensor.OpenSkeletonFrameReader();
                    
                    skeletonReader.FrameArrived += OnSkeletonFrameArrived;
                    
                    sensor.Disconnected += (s, e) =>
                    {
                        skeletonReader?.Dispose();
                        skeletonReader = null;
                        OnGestureDetected?.Invoke(this, new GestureEventArgs 
                        {
                            Gesture = GestureType.None,
                            LeftHand = new Point(-1, -1),
                            RightHand = new Point(-1, -1)
                        });
                    };
                }
            }
            catch (Exception ex)
            {
                OnGestureDetected?.Invoke(this, new GestureEventArgs 
                {
                    Gesture = GestureType.None,
                    LeftHand = new Point(-1, -1),
                    RightHand = new Point(-1, -1),
                    Confidence = 0
                });
            }
        }

        private void OnSkeletonFrameArrived(object sender, SkeletonFrameArrivedEventArgs e)
        {
            var frame = e.Frame;
            
            lock (_lock)
            {
                SkeletonFrame skeletonFrame = frame.OpenSkeletonFrame();
                if (skeletonFrame == null) return;

                int skeletonCount = frame.SkeletonArrayLength;
                for (int i = 0; i < skeletonCount; i++)
                {
                    var skeleton = frame.GetSkeletonArrayElement(i);
                    ProcessGesture(skeleton);
                }

                frame.Dispose();
            }
        }

        private void ProcessGesture(Skeleton skeleton)
        {
            if (!skeleton.HandStatus.Left.IsTracking || 
                !skeleton.HandStatus.Right.IsTracking)
            {
                return;
            }

            GestureType leftGesture = DetectHandGesture(skeleton.HandStatus.Left);
            GestureType rightGesture = DetectHandGesture(skeleton.HandStatus.Right);

            bool gestureChanged = false;
            
            // Detect gesture changes
            if (rightGesture == GestureType.HandsUp || 
                rightGesture == GestureType.HandsPlus)
            {
                OnGestureDetected?.Invoke(this, new GestureEventArgs 
                {
                    Gesture = rightGesture,
                    LeftHand = skeleton.Joints[BodyJoint.HandLeft],
                    RightHand = skeleton.Joints[BodyJoint.HandRight],
                    Confidence = 1.0f
                });
            }
        }

        private static GestureType DetectHandGesture(HandStatus hand)
        {
            if (!hand.IsTracking) return GestureType.None;

            BodyJoint leftWrist = hand.TrackingJoints[BodyJoint.Wrist];
            BodyJoint leftMiddleFinger = hand.TrackingJoints[BodyJoint.MiddleFinger];
            BodyJoint leftIndexFinger = hand.TrackingJoints[BodyJoint.IndexFinger];
            
            return GestureType.HandsUp;
        }

        public void Stop()
        {
            skeletonReader?.Dispose();
            skeletonReader = null;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}

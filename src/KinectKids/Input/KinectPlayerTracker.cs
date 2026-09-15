using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Microsoft.Kinect;
using KinectKids.Models;

namespace KinectKids.Input
{
    public sealed class KinectPlayerTracker : IPlayerTracker
    {
        private KinectSensor sensor;
        private Skeleton[] skeletons;
        private readonly Dictionary<long, CandidateState> candidates = new Dictionary<long, CandidateState>();
        private string status = "Letar efter Kinect…";

        public event EventHandler<IReadOnlyList<TrackedPlayer>> PlayersChanged;
        public event EventHandler<string> StatusChanged;

        public bool IsConnected => sensor != null && sensor.Status == KinectStatus.Connected;
        public string Status => status;

        public void Start()
        {
            KinectSensor.KinectSensors.StatusChanged += OnSensorStatusChanged;
            TryConnect();
        }

        public void Stop()
        {
            KinectSensor.KinectSensors.StatusChanged -= OnSensorStatusChanged;
            Disconnect();
        }

        private void TryConnect()
        {
            if (IsConnected) return;
            var available = KinectSensor.KinectSensors.FirstOrDefault(item => item.Status == KinectStatus.Connected);
            if (available == null)
            {
                SetStatus("Ingen Kinect hittades");
                return;
            }

            try
            {
                sensor = available;
                sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
                sensor.SkeletonStream.Enable(new TransformSmoothParameters
                {
                    Smoothing = 0.55f,
                    Correction = 0.35f,
                    Prediction = 0.25f,
                    JitterRadius = 0.08f,
                    MaxDeviationRadius = 0.12f
                });
                skeletons = new Skeleton[sensor.SkeletonStream.FrameSkeletonArrayLength];
                sensor.SkeletonFrameReady += OnSkeletonFrameReady;
                sensor.Start();
                SetStatus("Kinect ansluten");
            }
            catch (Exception ex)
            {
                Disconnect();
                SetStatus(FriendlyError(ex));
            }
        }

        private void Disconnect()
        {
            if (sensor == null) return;
            try
            {
                sensor.SkeletonFrameReady -= OnSkeletonFrameReady;
                if (sensor.IsRunning) sensor.Stop();
            }
            catch (InvalidOperationException)
            {
                // Sensorn kan redan ha kopplats ur fysiskt.
            }
            sensor = null;
            skeletons = null;
            candidates.Clear();
        }

        private void OnSensorStatusChanged(object sender, StatusChangedEventArgs e)
        {
            if (e.Status == KinectStatus.Connected)
            {
                TryConnect();
            }
            else if (sensor == e.Sensor)
            {
                Disconnect();
                SetStatus(StatusText(e.Status));
                PlayersChanged?.Invoke(this, Array.Empty<TrackedPlayer>());
            }
        }

        private void OnSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            using (var frame = e.OpenSkeletonFrame())
            {
                if (frame == null || skeletons == null) return;
                frame.CopySkeletonDataTo(skeletons);

                DateTime now = DateTime.UtcNow;
                var raw = skeletons
                    .Where(item => item.TrackingState == SkeletonTrackingState.Tracked && IsPlausibleBody(item))
                    .ToList();

                foreach (var skeleton in raw)
                {
                    CandidateState state;
                    if (!candidates.TryGetValue(skeleton.TrackingId, out state))
                    {
                        state = new CandidateState { FirstSeen = now };
                        candidates[skeleton.TrackingId] = state;
                    }
                    state.LastSeen = now;
                }

                foreach (var expired in candidates.Where(item => now - item.Value.LastSeen > TimeSpan.FromSeconds(1)).Select(item => item.Key).ToArray())
                    candidates.Remove(expired);

                // En första kropp visas snabbt. En andra måste vara stabil längre och
                // stå tydligt åtskild, så möbler/reflektioner inte blir spelare två.
                var primarySkeleton = raw
                    .Where(item => now - candidates[item.TrackingId].FirstSeen >= TimeSpan.FromMilliseconds(350))
                    .OrderBy(item => item.Position.Z)
                    .ThenBy(item => candidates[item.TrackingId].FirstSeen)
                    .FirstOrDefault();

                var tracked = new List<TrackedPlayer>();
                if (primarySkeleton != null)
                {
                    tracked.Add(ToPlayer(primarySkeleton));
                    var secondarySkeleton = raw
                        .Where(item => item.TrackingId != primarySkeleton.TrackingId)
                        .Where(item => now - candidates[item.TrackingId].FirstSeen >= TimeSpan.FromMilliseconds(2200))
                        .Where(HasTrackedUpperBody)
                        .Where(item => Math.Abs(item.Position.X - primarySkeleton.Position.X) >= 0.38f)
                        .Where(item => Math.Abs(item.Position.Z - primarySkeleton.Position.Z) <= 0.85f)
                        .OrderBy(item => item.Position.Z)
                        .FirstOrDefault();
                    if (secondarySkeleton != null) tracked.Add(ToPlayer(secondarySkeleton));
                }

                tracked = tracked.OrderBy(item => item.Hip.X).ToList();

                PlayersChanged?.Invoke(this, tracked);
            }
        }

        private TrackedPlayer ToPlayer(Skeleton skeleton)
        {
            var joints = new Dictionary<BodyJoint, Point>
            {
                [BodyJoint.Head] = Map(skeleton.Joints[JointType.Head]),
                [BodyJoint.ShoulderCenter] = Map(skeleton.Joints[JointType.ShoulderCenter]),
                [BodyJoint.ShoulderLeft] = Map(skeleton.Joints[JointType.ShoulderLeft]),
                [BodyJoint.ShoulderRight] = Map(skeleton.Joints[JointType.ShoulderRight]),
                [BodyJoint.ElbowLeft] = Map(skeleton.Joints[JointType.ElbowLeft]),
                [BodyJoint.ElbowRight] = Map(skeleton.Joints[JointType.ElbowRight]),
                [BodyJoint.WristLeft] = Map(skeleton.Joints[JointType.WristLeft]),
                [BodyJoint.WristRight] = Map(skeleton.Joints[JointType.WristRight]),
                [BodyJoint.HandLeft] = Map(skeleton.Joints[JointType.HandLeft]),
                [BodyJoint.HandRight] = Map(skeleton.Joints[JointType.HandRight]),
                [BodyJoint.Spine] = Map(skeleton.Joints[JointType.Spine]),
                [BodyJoint.HipCenter] = Map(skeleton.Joints[JointType.HipCenter]),
                [BodyJoint.HipLeft] = Map(skeleton.Joints[JointType.HipLeft]),
                [BodyJoint.HipRight] = Map(skeleton.Joints[JointType.HipRight]),
                [BodyJoint.KneeLeft] = Map(skeleton.Joints[JointType.KneeLeft]),
                [BodyJoint.KneeRight] = Map(skeleton.Joints[JointType.KneeRight]),
                [BodyJoint.AnkleLeft] = Map(skeleton.Joints[JointType.AnkleLeft]),
                [BodyJoint.AnkleRight] = Map(skeleton.Joints[JointType.AnkleRight]),
                [BodyJoint.FootLeft] = Map(skeleton.Joints[JointType.FootLeft]),
                [BodyJoint.FootRight] = Map(skeleton.Joints[JointType.FootRight])
            };
            bool ready = skeleton.Joints[JointType.HandLeft].TrackingState != JointTrackingState.NotTracked
                         && skeleton.Joints[JointType.HandRight].TrackingState != JointTrackingState.NotTracked;
            return new TrackedPlayer(skeleton.TrackingId, joints, ready);
        }

        private static bool IsPlausibleBody(Skeleton skeleton)
        {
            var head = skeleton.Joints[JointType.Head];
            var hip = skeleton.Joints[JointType.HipCenter];
            if (head.TrackingState == JointTrackingState.NotTracked || hip.TrackingState == JointTrackingState.NotTracked)
                return false;
            if (skeleton.Position.Z < 0.8f || skeleton.Position.Z > 4.5f)
                return false;
            return head.Position.Y - hip.Position.Y >= 0.25f;
        }

        private static bool HasTrackedUpperBody(Skeleton skeleton)
        {
            return skeleton.Joints[JointType.ShoulderLeft].TrackingState == JointTrackingState.Tracked
                   && skeleton.Joints[JointType.ShoulderRight].TrackingState == JointTrackingState.Tracked
                   && skeleton.Joints[JointType.HandLeft].TrackingState != JointTrackingState.NotTracked
                   && skeleton.Joints[JointType.HandRight].TrackingState != JointTrackingState.NotTracked;
        }

        private Point Map(Joint joint)
        {
            if (sensor == null || joint.TrackingState == JointTrackingState.NotTracked) return new Point(-1, -1);
            var depth = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(joint.Position, DepthImageFormat.Resolution320x240Fps30);
            return new Point(Clamp(depth.X / 320.0), Clamp(depth.Y / 240.0));
        }

        private static double Clamp(double value) => Math.Max(0, Math.Min(1, value));

        private static string FriendlyError(Exception exception)
        {
            string message = exception.Message ?? string.Empty;
            if (message.IndexOf("bandwidth", StringComparison.OrdinalIgnoreCase) >= 0)
                return "För lite USB-bandbredd – prova en annan USB 2.0-port";
            return "Kinect kunde inte startas: " + message;
        }

        private static string StatusText(KinectStatus value)
        {
            switch (value)
            {
                case KinectStatus.Disconnected: return "Kinect urkopplad";
                case KinectStatus.InsufficientBandwidth: return "För lite USB-bandbredd – byt USB-port";
                case KinectStatus.NotPowered: return "Kinect saknar ström";
                case KinectStatus.NotReady: return "Kinect är inte redo";
                default: return "Kinect-status: " + value;
            }
        }

        private void SetStatus(string value)
        {
            status = value;
            StatusChanged?.Invoke(this, value);
        }

        public void Dispose() => Stop();

        private sealed class CandidateState
        {
            public DateTime FirstSeen { get; set; }
            public DateTime LastSeen { get; set; }
        }
    }
}

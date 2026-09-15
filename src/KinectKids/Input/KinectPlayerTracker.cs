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

                var tracked = skeletons
                    .Where(item => item.TrackingState == SkeletonTrackingState.Tracked)
                    .Take(2)
                    .Select(ToPlayer)
                    .OrderBy(item => item.Hip.X)
                    .ToList();

                PlayersChanged?.Invoke(this, tracked);
            }
        }

        private TrackedPlayer ToPlayer(Skeleton skeleton)
        {
            var left = Map(skeleton.Joints[JointType.HandLeft]);
            var right = Map(skeleton.Joints[JointType.HandRight]);
            var head = Map(skeleton.Joints[JointType.Head]);
            var hip = Map(skeleton.Joints[JointType.HipCenter]);
            bool ready = skeleton.Joints[JointType.HandLeft].TrackingState != JointTrackingState.NotTracked
                         && skeleton.Joints[JointType.HandRight].TrackingState != JointTrackingState.NotTracked;
            return new TrackedPlayer(skeleton.TrackingId, left, right, head, hip, ready);
        }

        private Point Map(Joint joint)
        {
            if (sensor == null || joint.TrackingState == JointTrackingState.NotTracked) return new Point(-1, -1);
            var depth = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(joint.Position, DepthImageFormat.Resolution320x240Fps30);
            // Spegelvänd X-led ger den naturliga "spegelkänslan" barn förväntar sig.
            return new Point(1 - Clamp(depth.X / 320.0), Clamp(depth.Y / 240.0));
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
    }
}

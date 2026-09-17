using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Kinect;

namespace KinectKids.Bridge
{
    internal sealed class Program : IDisposable
    {
        private readonly Dictionary<int, DateTime> firstSeen = new Dictionary<int, DateTime>();
        private readonly object sendSync = new object();
        private KinectSensor sensor;
        private Process parent;
        private NamedPipeClientStream pipe;
        private StreamWriter writer;
        private bool stopping;

        private static int Main(string[] args)
        {
            using (var program = new Program())
            {
                try
                {
                    program.Run(args);
                    return 0;
                }
                catch (Exception exception)
                {
                    program.TrySendStatus("ERROR", DeepestMessage(exception));
                    Console.Error.WriteLine(exception);
                    return 1;
                }
            }
        }

        private void Run(string[] args)
        {
            int parentId = ReadIntArgument(args, "--parent");
            if (parentId > 0)
            {
                try { parent = Process.GetProcessById(parentId); }
                catch { parent = null; }
            }

            string pipeName = ReadArgument(args, "--pipe");
            if (string.IsNullOrWhiteSpace(pipeName)) pipeName = "KinectKidsV1";
            pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
            pipe.Connect(10000);
            writer = new StreamWriter(pipe, new UTF8Encoding(false)) { AutoFlush = true };
            SendStatus("INFO", "Kinect-bryggan är startad");
            SendStatus("INFO", "Letar efter ansluten Kinect 360…");
            sensor = KinectSensor.KinectSensors.FirstOrDefault(item => item.Status == KinectStatus.Connected);
            if (sensor == null) throw new InvalidOperationException("Ingen ansluten Kinect 360 hittades.");

            SendStatus("INFO", "Kinect hittad – startar djup- och skelettström…");
            sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
            sensor.SkeletonStream.Enable();
            sensor.SkeletonFrameReady += OnSkeletonFrameReady;
            sensor.Start();
            SendStatus("READY", "Kinect 360 ansluten via 32-bitarsbryggan");

            while (!stopping)
            {
                if (parent != null && parent.HasExited) break;
                Thread.Sleep(200);
            }
        }

        private void OnSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs eventArgs)
        {
            try
            {
                using (SkeletonFrame frame = eventArgs.OpenSkeletonFrame())
                {
                    if (frame == null) return;
                    var skeletons = new Skeleton[frame.SkeletonArrayLength];
                    frame.CopySkeletonDataTo(skeletons);
                    DateTime now = DateTime.UtcNow;
                    List<Skeleton> tracked = skeletons
                        .Where(item => item.TrackingState == SkeletonTrackingState.Tracked)
                        .Where(item => item.Position.Z >= 0.8f && item.Position.Z <= 4.5f)
                        .OrderBy(item => item.Position.Z)
                        .ToList();

                    foreach (Skeleton body in tracked)
                        if (!firstSeen.ContainsKey(body.TrackingId)) firstSeen[body.TrackingId] = now;
                    foreach (int expired in firstSeen.Keys.Where(id => tracked.All(body => body.TrackingId != id)).ToArray())
                        firstSeen.Remove(expired);

                    var accepted = new List<Skeleton>();
                    Skeleton primary = tracked.FirstOrDefault();
                    if (primary != null) accepted.Add(primary);
                    if (primary != null)
                    {
                        Skeleton second = tracked.Skip(1).FirstOrDefault(body =>
                            now - firstSeen[body.TrackingId] >= TimeSpan.FromSeconds(2.2)
                            && Math.Abs(body.Position.X - primary.Position.X) >= 0.38f
                            && Math.Abs(body.Position.Z - primary.Position.Z) <= 0.85f);
                        if (second != null) accepted.Add(second);
                    }

                    accepted = accepted.OrderBy(body => body.Position.X).ToList();
                    var packet = new StringBuilder("F|1");
                    for (int player = 0; player < accepted.Count; player++) AppendPlayer(packet, accepted[player], player);
                    Send(packet.ToString());
                }
            }
            catch (Exception exception)
            {
                SendStatus("WARN", DeepestMessage(exception));
            }
        }

        private void AppendPlayer(StringBuilder packet, Skeleton body, int player)
        {
            Joint shoulder = body.Joints[JointType.ShoulderCenter];
            Joint head = body.Joints[JointType.Head];
            Joint left = body.Joints[JointType.HandLeft];
            Joint right = body.Joints[JointType.HandRight];
            if (left.TrackingState == JointTrackingState.NotTracked
                || right.TrackingState == JointTrackingState.NotTracked) return;

            DepthImagePoint leftDepth = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(
                left.Position, DepthImageFormat.Resolution320x240Fps30);
            DepthImagePoint rightDepth = sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(
                right.Position, DepthImageFormat.Resolution320x240Fps30);
            packet.Append('~').Append("P|").Append(player).Append('|').Append(body.TrackingId)
                .Append('|').Append(F(shoulder.Position.X)).Append('|').Append(F(head.Position.Y))
                .Append('|').Append(F(shoulder.Position.Y)).Append('|').Append(F(shoulder.Position.Z))
                .Append('|').Append(F(leftDepth.X / 320f)).Append('|').Append(F(leftDepth.Y / 240f))
                .Append('|').Append(F(left.Position.Y)).Append('|').Append(F(left.Position.Z))
                .Append('|').Append(F(rightDepth.X / 320f)).Append('|').Append(F(rightDepth.Y / 240f))
                .Append('|').Append(F(right.Position.Y)).Append('|').Append(F(right.Position.Z));
        }

        private static string F(float value)
        {
            return value.ToString("0.0000", CultureInfo.InvariantCulture);
        }

        private void SendStatus(string kind, string message)
        {
            Send("S|" + kind + "|" + (message ?? string.Empty).Replace('\n', ' ').Replace('\r', ' '));
        }

        private void TrySendStatus(string kind, string message)
        {
            try { if (writer != null) SendStatus(kind, message); }
            catch { }
        }

        private void Send(string message)
        {
            lock (sendSync)
            {
                if (writer == null) throw new IOException("Den lokala Kinect-pipen är inte ansluten.");
                writer.WriteLine(message);
            }
        }

        private static string ReadArgument(string[] args, string name)
        {
            for (int i = 0; i + 1 < args.Length; i++)
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase)) return args[i + 1];
            return null;
        }

        private static int ReadIntArgument(string[] args, string name)
        {
            int value;
            return int.TryParse(ReadArgument(args, name), out value) ? value : 0;
        }

        private static string DeepestMessage(Exception exception)
        {
            while (exception.InnerException != null) exception = exception.InnerException;
            return exception.Message;
        }

        public void Dispose()
        {
            stopping = true;
            if (sensor != null)
            {
                try
                {
                    sensor.SkeletonFrameReady -= OnSkeletonFrameReady;
                    sensor.Stop();
                }
                catch { }
                sensor = null;
            }
            if (writer != null) writer.Dispose();
            writer = null;
            if (pipe != null) pipe.Dispose();
            pipe = null;
            if (parent != null) parent.Dispose();
        }
    }
}

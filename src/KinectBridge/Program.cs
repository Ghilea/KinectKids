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
        private EventWaitHandle stopEvent;
        private bool stopping;
        private int startupFinished;
        private int primaryTrackingId;

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
            string stopEventName = ReadArgument(args, "--stop-event");
            if (!string.IsNullOrEmpty(stopEventName))
                stopEvent = EventWaitHandle.OpenExisting(stopEventName);
            pipe = new NamedPipeClientStream(".", pipeName, PipeDirection.Out);
            pipe.Connect(10000);
            writer = new StreamWriter(pipe, new UTF8Encoding(false)) { AutoFlush = true };
            SendStatus("INFO", "Kinect-bryggan är startad");
            SendStatus("INFO", "Letar efter ansluten Kinect 360…");
            sensor = KinectSensor.KinectSensors.FirstOrDefault(item => item.Status == KinectStatus.Connected);
            if (sensor == null) throw new InvalidOperationException("Ingen ansluten Kinect 360 hittades.");

            SendStatus("INFO", "Kinect hittad – startar djup- och skelettström…");
            StartStartupWatchdog();
            SendStatus("INFO", "Aktiverar Kinect-djupström…");
            sensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
            SendStatus("INFO", "Djupström klar - aktiverar skelettström…");
            // Kinect v1-data blir mycket orolig när en hand förs ut från
            // kroppen. SDK-filtreringen tar bort led-jitter innan koordinaterna
            // skickas till Unity, men behåller tillräckligt snabb respons för kast.
            sensor.SkeletonStream.Enable(new TransformSmoothParameters
            {
                Smoothing = 0.62f,
                Correction = 0.28f,
                Prediction = 0.22f,
                JitterRadius = 0.075f,
                MaxDeviationRadius = 0.18f
            });
            sensor.SkeletonFrameReady += OnSkeletonFrameReady;
            SendStatus("INFO", "Skelettström klar - startar sensorn…");
            try
            {
                sensor.Start();
            }
            catch (Exception)
            {
                Interlocked.Exchange(ref startupFinished, 1);
                throw new InvalidOperationException(
                    "Kinect kunde inte starta. Stäng Kinect Explorer, Kinect Studio och andra Kinect-program, "
                    + "koppla ur Kinect USB och ström en kort stund, anslut igen och försök på nytt.");
            }
            Interlocked.Exchange(ref startupFinished, 1);
            SendStatus("READY", "Kinect 360 ansluten via 32-bitarsbryggan");

            while (!stopping)
            {
                if (parent != null && parent.HasExited) break;
                if (stopEvent != null && stopEvent.WaitOne(200)) break;
                if (stopEvent == null) Thread.Sleep(200);
            }
        }

        private void StartStartupWatchdog()
        {
            var watchdog = new Thread(() =>
            {
                Thread.Sleep(15000);
                if (Interlocked.CompareExchange(ref startupFinished, 0, 0) != 0 || stopping) return;
                TrySendStatus("ERROR",
                    "Kinect hittades men sensorn svarade inte inom 15 sekunder. "
                    + "Kontrollera USB-anslutningen och att inget annat Kinect-program är öppet.");
                Environment.Exit(2);
            })
            {
                IsBackground = true,
                Name = "KinectKids startup watchdog"
            };
            watchdog.Start();
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
                        .Where(item => item.Position.Z >= 0.55f && item.Position.Z <= 5.5f)
                        .OrderBy(item => item.Position.Z)
                        .ToList();
                    int positionOnlyCount = skeletons.Count(item =>
                        item.TrackingState == SkeletonTrackingState.PositionOnly
                        && item.Position.Z >= 0.55f && item.Position.Z <= 5.5f);

                    foreach (Skeleton body in tracked)
                        if (!firstSeen.ContainsKey(body.TrackingId)) firstSeen[body.TrackingId] = now;
                    foreach (int expired in firstSeen.Keys.Where(id => tracked.All(body => body.TrackingId != id)).ToArray())
                        firstSeen.Remove(expired);

                    var accepted = new List<Skeleton>();
                    // Keep the same child as player 0 while their tracking ID is
                    // present. Sorting by X made a second person steal the menu
                    // cursor and chase controls whenever they crossed sides.
                    Skeleton primary = tracked.FirstOrDefault(item => item.TrackingId == primaryTrackingId)
                        ?? tracked.FirstOrDefault();
                    primaryTrackingId = primary != null ? primary.TrackingId : 0;
                    if (primary != null) accepted.Add(primary);
                    if (primary != null)
                    {
                        Skeleton second = tracked.Skip(1).FirstOrDefault(body =>
                            now - firstSeen[body.TrackingId] >= TimeSpan.FromSeconds(2.2)
                            && Math.Abs(body.Position.X - primary.Position.X) >= 0.38f
                            && Math.Abs(body.Position.Z - primary.Position.Z) <= 0.85f);
                        if (second != null) accepted.Add(second);
                    }

                    var packet = new StringBuilder("F|1|").Append(tracked.Count).Append('|').Append(positionOnlyCount);
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
            if (shoulder.TrackingState == JointTrackingState.NotTracked)
                shoulder = body.Joints[JointType.Spine];
            if (head.TrackingState == JointTrackingState.NotTracked)
                head = shoulder;
            // En kort tappad hand får inte radera hela spelaren. Handleden är
            // ofta fortfarande spårad när handen ligger nära bildkanten.
            if (left.TrackingState == JointTrackingState.NotTracked)
                left = body.Joints[JointType.WristLeft];
            if (right.TrackingState == JointTrackingState.NotTracked)
                right = body.Joints[JointType.WristRight];
            // Body gestures must continue working even when one hand leaves the
            // sensor volume. Use the shoulder as a neutral aim fallback.
            if (left.TrackingState == JointTrackingState.NotTracked) left = shoulder;
            if (right.TrackingState == JointTrackingState.NotTracked) right = shoulder;

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
            if (stopEvent != null) stopEvent.Dispose();
        }
    }
}

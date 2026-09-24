using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using UnityEngine;

namespace KinectKids3D
{
    /// <summary>
    /// Tar emot enbart ledpositioner från KinectBridge.exe via en lokal Windows-pipe.
    /// Bryggan är x86 så att Kinect SDK 1.8 fungerar även när Unity körs som 64-bitarsprogram.
    /// </summary>
    public sealed class KinectBridgeAimProvider : KinectAimProviderBase
    {
        private readonly Dictionary<int, HandGestureState> gestures = new Dictionary<int, HandGestureState>();
        private readonly ManualResetEvent ready = new ManualResetEvent(false);
        private NamedPipeServerStream pipe;
        private StreamReader reader;
        private Thread receiveThread;
        private Process bridgeProcess;
        private volatile bool stopping;
        private bool disposed;
        private DateTime lastTrackedFrameAt = DateTime.MinValue;

        public bool HasFailed { get; private set; }

        public KinectBridgeAimProvider()
        {
            status = "Startar Kinect-bryggan…";
        }

        public bool TryStart()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor
                && Application.platform != RuntimePlatform.WindowsPlayer)
            {
                status = "Kinect 360 stöds bara på Windows";
                HasFailed = true;
                return false;
            }

            try
            {
                string executable = EnsureBridgeExecutable();
                if (string.IsNullOrEmpty(executable))
                {
                    HasFailed = true;
                    return false;
                }

                string pipeName = "KinectKidsV1-" + Process.GetCurrentProcess().Id;
                pipe = new NamedPipeServerStream(pipeName, PipeDirection.In, 1,
                    PipeTransmissionMode.Byte, PipeOptions.Asynchronous);
                receiveThread = new Thread(ReceiveLoop) { IsBackground = true, Name = "KinectKids Kinect bridge" };
                receiveThread.Start();

                var start = new ProcessStartInfo
                {
                    FileName = executable,
                    Arguments = "--parent " + Process.GetCurrentProcess().Id + " --pipe " + pipeName,
                    WorkingDirectory = Path.GetDirectoryName(executable),
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WindowStyle = ProcessWindowStyle.Hidden
                };
                bridgeProcess = Process.Start(start);
                if (bridgeProcess == null) throw new InvalidOperationException("KinectBridge.exe kunde inte startas.");
                bridgeProcess.EnableRaisingEvents = true;
                bridgeProcess.Exited += OnBridgeExited;
                status = "Kinect-bryggan startar i bakgrunden…";
                HasFailed = false;
                return true;
            }
            catch (Exception exception)
            {
                status = "Kinect-bryggan kunde inte starta: " + DeepestMessage(exception);
                HasFailed = true;
                Dispose();
                return false;
            }
        }

        private void OnBridgeExited(object sender, EventArgs eventArgs)
        {
            if (stopping) return;
            IsAvailable = false;
            HasFailed = true;
            try
            {
                string error = bridgeProcess != null ? bridgeProcess.StandardError.ReadToEnd() : string.Empty;
                int code = bridgeProcess != null ? bridgeProcess.ExitCode : -1;
                if (string.IsNullOrWhiteSpace(error))
                    status = "Kinect-bryggan stängdes (kod " + code + ")";
                else
                    status = FriendlyStatus(Compact(error));
            }
            catch (Exception exception)
            {
                status = "Kinect-bryggan stängdes: " + DeepestMessage(exception);
            }
            ready.Set();
        }

        private void ReceiveLoop()
        {
            try
            {
                pipe.WaitForConnection();
                reader = new StreamReader(pipe);
                while (!stopping)
                {
                    string packet = reader.ReadLine();
                    if (packet == null) break;
                    ParsePacket(packet);
                }
            }
            catch (IOException)
            {
                if (!stopping)
                {
                    status = "Kontakten med Kinect-bryggan bröts";
                    IsAvailable = false;
                    HasFailed = true;
                }
            }
            catch (ObjectDisposedException) { }
            catch (Exception exception)
            {
                if (!stopping)
                {
                    status = "Kinect-bryggan gav fel: " + DeepestMessage(exception);
                    IsAvailable = false;
                    HasFailed = true;
                }
            }
        }

        private void ParsePacket(string packet)
        {
            string[] lines = packet.Split(new[] { '~' }, StringSplitOptions.RemoveEmptyEntries);
            if (lines.Length == 0) return;
            if (lines[0].StartsWith("S|", StringComparison.Ordinal))
            {
                string[] statusParts = lines[0].Split(new[] { '|' }, 3);
                if (statusParts.Length >= 3)
                {
                    status = FriendlyStatus(statusParts[2]);
                    if (statusParts[1] == "READY")
                    {
                        IsAvailable = true;
                        HasFailed = false;
                        ready.Set();
                    }
                    else if (statusParts[1] == "ERROR")
                    {
                        IsAvailable = false;
                        HasFailed = true;
                        ready.Set();
                    }
                }
                return;
            }
            if (!lines[0].StartsWith("F|", StringComparison.Ordinal)) return;

            var samples = new List<AimSample>(4);
            var poses = new List<PlayerPose>(2);
            DateTime now = DateTime.UtcNow;
            for (int i = 1; i < lines.Length; i++)
            {
                string[] values = lines[i].Split('|');
                if (values.Length != 15 || values[0] != "P") continue;
                int player = ParseInt(values[1]);
                long trackingId = ParseLong(values[2]);
                poses.Add(new PlayerPose(player, trackingId, ParseFloat(values[3]), ParseFloat(values[4])));
                AddHand(samples, player, player * 2, ParseFloat(values[7]), ParseFloat(values[8]),
                    ParseFloat(values[9]), ParseFloat(values[10]), ParseFloat(values[5]), ParseFloat(values[6]), now);
                AddHand(samples, player, player * 2 + 1, ParseFloat(values[11]), ParseFloat(values[12]),
                    ParseFloat(values[13]), ParseFloat(values[14]), ParseFloat(values[5]), ParseFloat(values[6]), now);
            }

            lock (sync)
            {
                // Behåll den senaste stabila posen under mycket korta bortfall.
                // Kinect 360 kan missa enstaka bildrutor när handen är långt
                // ut från kroppen; UI och sikte ska inte blinka bort för det.
                if (poses.Count == 0 && latestPoses.Count > 0
                    && now - lastTrackedFrameAt < TimeSpan.FromMilliseconds(420))
                    return;
                latest.Clear();
                latest.AddRange(samples);
                latestPoses.Clear();
                latestPoses.AddRange(poses);
                if (poses.Count > 0) lastTrackedFrameAt = now;
            }
            IsAvailable = true;
            HasFailed = false;
            status = "Kinect 360 ansluten via säker 32-bitarsbrygga";
            ready.Set();
        }

        private void AddHand(List<AimSample> output, int player, int handId, float x, float y,
            float handY, float handZ, float shoulderY, float shoulderZ, DateTime now)
        {
            HandGestureState gesture;
            if (!gestures.TryGetValue(handId, out gesture))
            {
                gesture = new HandGestureState();
                gestures[handId] = gesture;
            }

            Vector2 rawAim = new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y));
            bool fire = false;
            float forwardSpeed = 0f;
            if (gesture.Initialized)
            {
                float dt = Mathf.Clamp((float)(now - gesture.LastAt).TotalSeconds, 0.001f, 0.10f);
                float depthStep = gesture.LastDepth - handZ;
                float instantForwardSpeed = depthStep / dt;
                float velocityBlend = 1f - Mathf.Exp(-18f * dt);
                gesture.SmoothedForwardSpeed = Mathf.Lerp(
                    gesture.SmoothedForwardSpeed, instantForwardSpeed, velocityBlend);
                forwardSpeed = gesture.SmoothedForwardSpeed;

                // Ett nytt kast laddas genom att handen bromsar in, inte genom
                // att barnet först måste dra tillbaka den intill kroppen.
                if (!gesture.PunchReady && now >= gesture.CooldownUntil
                    && forwardSpeed < 0.12f)
                    gesture.PunchReady = true;

                if (gesture.PunchReady && now >= gesture.CooldownUntil
                    && forwardSpeed > 0.45f && depthStep > 0.012f)
                {
                    fire = true;
                    gesture.PunchReady = false;
                    gesture.CooldownUntil = now.AddMilliseconds(300);
                    gesture.LockedAim = gesture.LastAim;
                    gesture.AimLockedUntil = now.AddMilliseconds(260);
                }
            }
            else
            {
                gesture.PunchReady = true;
                gesture.LastAim = rawAim;
            }

            gesture.Initialized = true;
            gesture.LastDepth = handZ;
            gesture.LastAt = now;
            Vector2 displayedAim = now < gesture.AimLockedUntil ? gesture.LockedAim : rawAim;
            if (now >= gesture.AimLockedUntil) gesture.LastAim = rawAim;
            float progress = Mathf.Clamp01(Mathf.Max(0f, forwardSpeed) / 0.45f);
            output.Add(new AimSample(player, handId,
                displayedAim, fire, progress));
        }

        private string EnsureBridgeExecutable()
        {
            string packaged = Path.Combine(Application.streamingAssetsPath, "KinectBridge", "KinectBridge.exe");
            if (File.Exists(packaged)) return packaged;

            string root = FindProjectRoot();
            if (root == null)
            {
                status = "KinectBridge.exe saknas i programmet";
                return null;
            }
            string executable = Path.Combine(root, "src", "KinectBridge", "bin", "Release", "KinectBridge.exe");
            string bridgeSource = Path.Combine(root, "src", "KinectBridge", "Program.cs");
            if (File.Exists(executable)
                && (!File.Exists(bridgeSource)
                    || File.GetLastWriteTimeUtc(executable) >= File.GetLastWriteTimeUtc(bridgeSource)))
                return executable;

            string script = Path.Combine(root, "scripts", "Build-KinectBridge.ps1");
            if (!File.Exists(script))
            {
                status = "Byggskriptet för Kinect-bryggan saknas";
                return null;
            }
            status = "Bygger Kinect-bryggan första gången…";
            using (Process build = Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -ExecutionPolicy Bypass -File \"" + script + "\"",
                WorkingDirectory = root,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            }))
            {
                if (build == null || !build.WaitForExit(120000) || build.ExitCode != 0)
                {
                    status = "Kinect-bryggan kunde inte byggas – kör scripts\\Build-KinectBridge.ps1";
                    return null;
                }
            }
            if (!File.Exists(executable))
            {
                status = "KinectBridge.exe skapades inte";
                return null;
            }
            return executable;
        }

        private static string FindProjectRoot()
        {
            DirectoryInfo directory = new DirectoryInfo(Application.dataPath);
            for (int i = 0; i < 8 && directory != null; i++, directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "KinectKids.sln"))) return directory.FullName;
            }
            return null;
        }

        private static float ParseFloat(string value)
        {
            return float.Parse(value, NumberStyles.Float, CultureInfo.InvariantCulture);
        }

        private static int ParseInt(string value) => int.Parse(value, CultureInfo.InvariantCulture);
        private static long ParseLong(string value) => long.Parse(value, CultureInfo.InvariantCulture);

        private static string DeepestMessage(Exception exception)
        {
            while (exception.InnerException != null) exception = exception.InnerException;
            string message = exception.Message;
            if (message.IndexOf("HRESULT", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("E_FAIL", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Kinect kunde inte öppnas – sensorn används redan eller USB-anslutningen behöver startas om";
            return message;
        }

        private static string FriendlyStatus(string message)
        {
            if (string.IsNullOrEmpty(message)) return message;
            if (message.IndexOf("HRESULT", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("E_FAIL", StringComparison.OrdinalIgnoreCase) >= 0
                || message.IndexOf("COM-komponent", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Kinect kunde inte öppnas – sensorn används redan eller USB-anslutningen behöver startas om";
            return message;
        }

        private static string Compact(string value)
        {
            string compact = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
            while (compact.Contains("  ")) compact = compact.Replace("  ", " ");
            return compact.Length <= 260 ? compact : compact.Substring(0, 257) + "…";
        }

        public override void Dispose()
        {
            if (disposed) return;
            disposed = true;
            stopping = true;
            IsAvailable = false;
            if (reader != null)
            {
                reader.Dispose();
                reader = null;
            }
            if (pipe != null)
            {
                pipe.Dispose();
                pipe = null;
            }
            if (receiveThread != null && receiveThread.IsAlive) receiveThread.Join(500);
            receiveThread = null;
            if (bridgeProcess != null)
            {
                try { if (!bridgeProcess.HasExited) bridgeProcess.Kill(); }
                catch { }
                bridgeProcess.Dispose();
                bridgeProcess = null;
            }
            ready.Dispose();
            lock (sync)
            {
                latest.Clear();
                latestPoses.Clear();
            }
        }
    }
}

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
    public sealed class KinectBridgeAimProvider : IAimProvider
    {
        private const int StartupTimeoutSeconds = 30;
        private readonly object sync = new object();
        private readonly List<AimSample> latest = new List<AimSample>(4);
        private readonly List<PlayerPose> latestPoses = new List<PlayerPose>(2);
        private readonly Dictionary<int, HandGestureState> gestures = new Dictionary<int, HandGestureState>();
        private readonly ManualResetEvent ready = new ManualResetEvent(false);
        private NamedPipeServerStream pipe;
        private StreamReader reader;
        private Thread receiveThread;
        private Process bridgeProcess;
        private volatile bool stopping;
        private bool disposed;
        private DateTime lastTrackedFrameAt = DateTime.MinValue;
        private string status = "Startar Kinect-bryggan…";

        public bool IsAvailable { get; private set; }
        public string Status => status;

        public bool TryStart()
        {
            if (Application.platform != RuntimePlatform.WindowsEditor
                && Application.platform != RuntimePlatform.WindowsPlayer)
            {
                status = "Kinect 360 stöds bara på Windows";
                return false;
            }

            try
            {
                string executable = EnsureBridgeExecutable();
                if (string.IsNullOrEmpty(executable)) return false;

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

                DateTime deadline = DateTime.UtcNow.AddSeconds(StartupTimeoutSeconds);
                while (!ready.WaitOne(250) && DateTime.UtcNow < deadline)
                {
                    if (bridgeProcess.HasExited)
                    {
                        string error = bridgeProcess.StandardError.ReadToEnd();
                        status = "Kinect-bryggan stängdes (kod " + bridgeProcess.ExitCode + ")";
                        if (!string.IsNullOrWhiteSpace(error)) status += ": " + FriendlyStatus(Compact(error));
                        Dispose();
                        return false;
                    }
                }
                if (!ready.WaitOne(0))
                {
                    status += " – start tog över 30 sekunder; koppla ur sensorn och anslut igen";
                    Dispose();
                    return false;
                }

                if (!IsAvailable)
                {
                    Dispose();
                    return false;
                }
                return true;
            }
            catch (Exception exception)
            {
                status = "Kinect-bryggan kunde inte starta: " + DeepestMessage(exception);
                Dispose();
                return false;
            }
        }

        public IReadOnlyList<AimSample> GetAimSamples()
        {
            lock (sync) return latest.ToArray();
        }

        public IReadOnlyList<PlayerPose> GetPlayerPoses()
        {
            lock (sync) return latestPoses.ToArray();
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
                if (!stopping) status = "Kontakten med Kinect-bryggan bröts";
            }
            catch (ObjectDisposedException) { }
            catch (Exception exception)
            {
                if (!stopping) status = "Kinect-bryggan gav fel: " + DeepestMessage(exception);
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
                        ready.Set();
                    }
                    else if (statusParts[1] == "ERROR")
                    {
                        IsAvailable = false;
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

            bool fire = false;
            if (gesture.Initialized)
            {
                float dt = Mathf.Max(0.001f, (float)(now - gesture.LastAt).TotalSeconds);
                float forwardSpeed = (gesture.LastDepth - handZ) / dt;
                bool handIsReady = handY > shoulderY - 0.42f && handZ > shoulderZ - 0.16f;
                if (handIsReady) gesture.Armed = true;
                if (gesture.Armed && now >= gesture.CooldownUntil
                    && forwardSpeed > 0.42f && handZ < shoulderZ - 0.10f)
                {
                    fire = true;
                    gesture.Armed = false;
                    gesture.CooldownUntil = now.AddMilliseconds(360);
                }
            }

            gesture.Initialized = true;
            gesture.LastDepth = handZ;
            gesture.LastAt = now;
            float progress = gesture.Armed ? Mathf.Clamp01((shoulderZ - handZ + 0.16f) / 0.28f) : 0f;
            output.Add(new AimSample(player, handId,
                new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y)), fire, progress));
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

        public void Dispose()
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

        private sealed class HandGestureState
        {
            public bool Initialized;
            public bool Armed;
            public float LastDepth;
            public DateTime LastAt;
            public DateTime CooldownUntil;
        }
    }
}

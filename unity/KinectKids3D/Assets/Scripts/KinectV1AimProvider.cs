using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using UnityEngine;

namespace KinectKids3D
{
    /// <summary>
    /// Läser Kinect SDK 1.8 med reflection. Unity-projektet kan därför öppnas
    /// och köras med mus även när Kinect SDK inte är installerat.
    /// </summary>
    public sealed class KinectV1AimProvider : IAimProvider
    {
        private readonly object sync = new object();
        private readonly List<AimSample> latest = new List<AimSample>(4);
        private readonly List<PlayerPose> latestPoses = new List<PlayerPose>(2);
        private readonly Dictionary<long, DateTime> firstSeen = new Dictionary<long, DateTime>();
        private readonly Dictionary<int, HandGestureState> handGestures = new Dictionary<int, HandGestureState>();
        private Assembly assembly;
        private object sensor;
        private object coordinateMapper;
        private object depthFormat;
        private MethodInfo mapToDepth;
        private EventInfo skeletonEvent;
        private Delegate skeletonHandler;
        private Type skeletonType;
        private Type jointType;
        private string status = "Letar efter Kinect 360…";

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
                string dllPath = FindKinectAssembly();
                if (dllPath == null)
                {
                    status = "Kinect SDK 1.8 saknas – musläge används";
                    return false;
                }

                assembly = Assembly.LoadFrom(dllPath);
                Type sensorType = assembly.GetType("Microsoft.Kinect.KinectSensor", true);
                IEnumerable sensors = (IEnumerable)sensorType.GetProperty("KinectSensors").GetValue(null, null);
                sensor = sensors.Cast<object>().FirstOrDefault(item =>
                    string.Equals(GetProperty(item, "Status").ToString(), "Connected", StringComparison.OrdinalIgnoreCase));
                if (sensor == null)
                {
                    status = "Ingen ansluten Kinect 360 – musläge används";
                    return false;
                }

                Type depthFormatType = assembly.GetType("Microsoft.Kinect.DepthImageFormat", true);
                depthFormat = Enum.Parse(depthFormatType, "Resolution320x240Fps30");
                object depthStream = GetProperty(sensor, "DepthStream");
                depthStream.GetType().GetMethod("Enable", new[] { depthFormatType }).Invoke(depthStream, new[] { depthFormat });

                object skeletonStream = GetProperty(sensor, "SkeletonStream");
                skeletonStream.GetType().GetMethod("Enable", Type.EmptyTypes).Invoke(skeletonStream, null);
                skeletonType = assembly.GetType("Microsoft.Kinect.Skeleton", true);
                jointType = assembly.GetType("Microsoft.Kinect.JointType", true);
                coordinateMapper = GetProperty(sensor, "CoordinateMapper");
                Type skeletonPointType = assembly.GetType("Microsoft.Kinect.SkeletonPoint", true);
                mapToDepth = coordinateMapper.GetType().GetMethod(
                    "MapSkeletonPointToDepthPoint", new[] { skeletonPointType, depthFormatType });

                skeletonEvent = sensor.GetType().GetEvent("SkeletonFrameReady");
                skeletonHandler = CreateEventHandler(skeletonEvent.EventHandlerType);
                skeletonEvent.AddEventHandler(sensor, skeletonHandler);
                sensor.GetType().GetMethod("Start", Type.EmptyTypes).Invoke(sensor, null);
                IsAvailable = true;
                status = "Kinect 360 ansluten – händerna styr siktet";
                return true;
            }
            catch (Exception exception)
            {
                status = FriendlyError(Unwrap(exception));
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

        private void OnSkeletonFrame(object sender, object eventArgs)
        {
            object frame = null;
            try
            {
                frame = eventArgs.GetType().GetMethod("OpenSkeletonFrame").Invoke(eventArgs, null);
                if (frame == null) return;
                int length = (int)GetProperty(frame, "SkeletonArrayLength");
                Array skeletons = Array.CreateInstance(skeletonType, length);
                frame.GetType().GetMethod("CopySkeletonDataTo").Invoke(frame, new object[] { skeletons });
                DateTime now = DateTime.UtcNow;

                var tracked = skeletons.Cast<object>()
                    .Where(item => GetProperty(item, "TrackingState").ToString() == "Tracked")
                    .Where(IsPlausible)
                    .OrderBy(item => ReadFloat(GetProperty(item, "Position"), "Z"))
                    .ToList();

                foreach (object body in tracked)
                {
                    long id = Convert.ToInt64(GetProperty(body, "TrackingId"));
                    if (!firstSeen.ContainsKey(id)) firstSeen[id] = now;
                }
                foreach (long expired in firstSeen.Keys.Where(id => tracked.All(body => Convert.ToInt64(GetProperty(body, "TrackingId")) != id)).ToArray())
                    firstSeen.Remove(expired);

                var accepted = new List<object>();
                object primary = tracked.FirstOrDefault();
                if (primary != null) accepted.Add(primary);
                if (primary != null)
                {
                    object primaryPosition = GetProperty(primary, "Position");
                    float primaryX = ReadFloat(primaryPosition, "X");
                    float primaryZ = ReadFloat(primaryPosition, "Z");
                    object second = tracked.Skip(1).FirstOrDefault(body =>
                    {
                        long id = Convert.ToInt64(GetProperty(body, "TrackingId"));
                        object position = GetProperty(body, "Position");
                        return now - firstSeen[id] >= TimeSpan.FromSeconds(2.2)
                               && Mathf.Abs(ReadFloat(position, "X") - primaryX) >= 0.38f
                               && Mathf.Abs(ReadFloat(position, "Z") - primaryZ) <= 0.85f;
                    });
                    if (second != null) accepted.Add(second);
                }

                accepted = accepted.OrderBy(body => ReadFloat(GetProperty(body, "Position"), "X")).ToList();
                var next = new List<AimSample>(4);
                var nextPoses = new List<PlayerPose>(2);
                for (int player = 0; player < accepted.Count; player++)
                {
                    object body = accepted[player];
                    long trackingId = Convert.ToInt64(GetProperty(body, "TrackingId"));
                    object shoulder = GetJointPoint(body, "ShoulderCenter");
                    object head = GetJointPoint(body, "Head");
                    nextPoses.Add(new PlayerPose(player, trackingId,
                        ReadFloat(shoulder, "X"), ReadFloat(head, "Y")));
                    AddHand(next, body, player, player * 2, "HandLeft", shoulder, now);
                    AddHand(next, body, player, player * 2 + 1, "HandRight", shoulder, now);
                }
                lock (sync)
                {
                    latest.Clear();
                    latest.AddRange(next);
                    latestPoses.Clear();
                    latestPoses.AddRange(nextPoses);
                }
            }
            catch
            {
                // En tappad sensorbild är normal; nästa bild kommer cirka 33 ms senare.
            }
            finally
            {
                IDisposable disposable = frame as IDisposable;
                if (disposable != null) disposable.Dispose();
            }
        }

        private void AddHand(List<AimSample> output, object body, int player, int handId, string handName,
            object shoulderPoint, DateTime now)
        {
            object joint = GetJoint(body, handName);
            if (GetProperty(joint, "TrackingState").ToString() == "NotTracked") return;
            object point = GetProperty(joint, "Position");
            object depthPoint = mapToDepth.Invoke(coordinateMapper, new[] { point, depthFormat });
            float x = Convert.ToSingle(GetProperty(depthPoint, "X")) / 320f;
            float y = Convert.ToSingle(GetProperty(depthPoint, "Y")) / 240f;
            float handZ = ReadFloat(point, "Z");
            float handY = ReadFloat(point, "Y");
            float shoulderZ = ReadFloat(shoulderPoint, "Z");
            float shoulderY = ReadFloat(shoulderPoint, "Y");

            HandGestureState gesture;
            if (!handGestures.TryGetValue(handId, out gesture))
            {
                gesture = new HandGestureState();
                handGestures[handId] = gesture;
            }

            bool fire = false;
            if (gesture.Initialized)
            {
                float dt = Mathf.Max(0.001f, (float)(now - gesture.LastAt).TotalSeconds);
                float forwardSpeed = (gesture.LastDepth - handZ) / dt;
                bool handIsReady = handY > shoulderY - 0.42f && handZ > shoulderZ - 0.16f;
                if (handIsReady) gesture.Armed = true;
                if (gesture.Armed && now >= gesture.CooldownUntil
                    && forwardSpeed > 0.36f && handZ < shoulderZ - 0.085f)
                {
                    fire = true;
                    gesture.Armed = false;
                    gesture.CooldownUntil = now.AddMilliseconds(340);
                }
            }

            gesture.Initialized = true;
            gesture.LastDepth = handZ;
            gesture.LastAt = now;
            float progress = gesture.Armed
                ? Mathf.Clamp01((shoulderZ - handZ + 0.16f) / 0.28f)
                : 0f;
            output.Add(new AimSample(player, handId,
                new Vector2(Mathf.Clamp01(x), Mathf.Clamp01(y)), fire, progress));
        }

        private object GetJoint(object body, string jointName)
        {
            object joints = GetProperty(body, "Joints");
            object enumValue = Enum.Parse(jointType, jointName);
            PropertyInfo indexer = joints.GetType().GetProperty("Item");
            return indexer.GetValue(joints, new[] { enumValue });
        }

        private object GetJointPoint(object body, string jointName)
        {
            return GetProperty(GetJoint(body, jointName), "Position");
        }

        private bool IsPlausible(object body)
        {
            object position = GetProperty(body, "Position");
            float z = ReadFloat(position, "Z");
            return z >= 0.8f && z <= 4.5f;
        }

        private Delegate CreateEventHandler(Type handlerType)
        {
            MethodInfo invoke = handlerType.GetMethod("Invoke");
            ParameterInfo[] info = invoke.GetParameters();
            ParameterExpression sender = Expression.Parameter(info[0].ParameterType, "sender");
            ParameterExpression args = Expression.Parameter(info[1].ParameterType, "args");
            MethodInfo callback = GetType().GetMethod("OnSkeletonFrame", BindingFlags.NonPublic | BindingFlags.Instance);
            MethodCallExpression body = Expression.Call(
                Expression.Constant(this), callback,
                Expression.Convert(sender, typeof(object)),
                Expression.Convert(args, typeof(object)));
            return Expression.Lambda(handlerType, body, sender, args).Compile();
        }

        private static string FindKinectAssembly()
        {
            string[] candidates =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                    "Microsoft SDKs", "Kinect", "v1.8", "Assemblies", "Microsoft.Kinect.dll"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    "Microsoft SDKs", "Kinect", "v1.8", "Assemblies", "Microsoft.Kinect.dll")
            };
            return candidates.FirstOrDefault(File.Exists);
        }

        private static object GetProperty(object target, string name)
        {
            return target.GetType().GetProperty(name).GetValue(target, null);
        }

        private static float ReadFloat(object target, string name)
        {
            return Convert.ToSingle(GetProperty(target, name));
        }

        private static Exception Unwrap(Exception exception)
        {
            while ((exception is TargetInvocationException || exception is TypeInitializationException)
                   && exception.InnerException != null)
                exception = exception.InnerException;
            return exception;
        }

        private static string FriendlyError(Exception exception)
        {
            string message = exception.Message ?? string.Empty;
            if (message.IndexOf("bandwidth", StringComparison.OrdinalIgnoreCase) >= 0)
                return "För lite USB-bandbredd – prova en annan USB 2.0-port";
            return "Kinect kunde inte starta: " + message;
        }

        public void Dispose()
        {
            IsAvailable = false;
            if (sensor == null) return;
            try
            {
                if (skeletonEvent != null && skeletonHandler != null)
                    skeletonEvent.RemoveEventHandler(sensor, skeletonHandler);
                sensor.GetType().GetMethod("Stop", Type.EmptyTypes).Invoke(sensor, null);
            }
            catch
            {
            }
            sensor = null;
            handGestures.Clear();
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

using System.Collections.Generic;
using UnityEngine;

namespace KinectKids3D
{
    public sealed class MouseAimProvider : IAimProvider
    {
        private readonly List<AimSample> samples = new List<AimSample>(1);
        private readonly List<PlayerPose> poses = new List<PlayerPose>(1);

        public bool IsAvailable => true;
        public string Status => "Musläge (Kinect hittades inte)";

        public IReadOnlyList<AimSample> GetAimSamples()
        {
            samples.Clear();
            Vector3 mouse = Input.mousePosition;
            samples.Add(new AimSample(0, 0, new Vector2(
                Mathf.Clamp01(mouse.x / Mathf.Max(1, Screen.width)),
                Mathf.Clamp01(1f - mouse.y / Mathf.Max(1, Screen.height))),
                Input.GetMouseButtonDown(0), Input.GetMouseButton(0) ? 1f : 0f));
            return samples;
        }

        public IReadOnlyList<PlayerPose> GetPlayerPoses()
        {
            float x = 0f;
            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A)) x = -0.34f;
            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D)) x = 0.34f;
            float headY = Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S) ? 1.25f : 1.72f;
            poses.Clear();
            poses.Add(new PlayerPose(0, 1, x, headY));
            return poses;
        }

        public void Dispose()
        {
        }
    }
}

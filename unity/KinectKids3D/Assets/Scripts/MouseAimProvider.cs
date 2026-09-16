using System.Collections.Generic;
using UnityEngine;

namespace KinectKids3D
{
    public sealed class MouseAimProvider : IAimProvider
    {
        private readonly List<AimSample> samples = new List<AimSample>(1);

        public bool IsAvailable => true;
        public string Status => "Musläge (Kinect hittades inte)";

        public IReadOnlyList<AimSample> GetAimSamples()
        {
            samples.Clear();
            Vector3 mouse = Input.mousePosition;
            samples.Add(new AimSample(0, 0, new Vector2(
                Mathf.Clamp01(mouse.x / Mathf.Max(1, Screen.width)),
                Mathf.Clamp01(1f - mouse.y / Mathf.Max(1, Screen.height)))));
            return samples;
        }

        public void Dispose()
        {
        }
    }
}

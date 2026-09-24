using UnityEngine;

namespace KinectKids.Scene25D.Sections
{
    /// <summary>
    /// The shared perspective model for a 2.5D corridor: maps a normalised depth
    /// t in [0..1] (0 = at the camera / near / large, 1 = the vanishing point /
    /// far / tiny) to screen geometry. This is the SAME model
    /// <c>ChaseEnvironmentBuilder</c> uses, lifted into a reusable struct so the
    /// modular section streamer places floor tiles, walls and props on the exact
    /// same receding lines — every module converges on one vanishing point and
    /// the corridor holds together regardless of which room is streaming.
    ///
    /// Tuned for an orthographic camera at size 5 (visible y ≈ [-5,+5],
    /// x ≈ [-8.9,+8.9] @16:9). A room can override the numbers to feel wider
    /// (a great hall) or tighter (a cellar) without changing any placement code.
    /// </summary>
    public struct PerspectiveModel
    {
        public float floorLine;     // y at the camera (bottom of screen)
        public float horizon;       // y of the vanishing point
        public float nearHalfWidth; // half lane width at the camera
        public float farHalfWidth;  // half lane width at the horizon
        public float nearScale;     // module scale at the camera
        public float farScale;      // module scale at the horizon

        /// <summary>The default corridor model matching ChaseEnvironmentBuilder.</summary>
        public static PerspectiveModel Default => new PerspectiveModel
        {
            floorLine = -4.7f,
            horizon = 1.6f,
            nearHalfWidth = 4.2f,
            farHalfWidth = 0.55f,
            nearScale = 1.0f,
            farScale = 0.14f,
        };

        /// <summary>A wider, taller variant for a grand room (great hall).</summary>
        public static PerspectiveModel Wide
        {
            get
            {
                PerspectiveModel m = Default;
                m.nearHalfWidth = 5.4f;
                m.farHalfWidth = 0.8f;
                m.horizon = 1.9f;
                return m;
            }
        }

        /// <summary>A tighter, lower variant for a cramped room (kitchen / cellar).</summary>
        public static PerspectiveModel Tight
        {
            get
            {
                PerspectiveModel m = Default;
                m.nearHalfWidth = 3.4f;
                m.farHalfWidth = 0.45f;
                m.horizon = 1.2f;
                return m;
            }
        }

        /// <summary>Ease so distance compresses toward the horizon (real perspective).</summary>
        public static float Curve(float t)
        {
            t = Mathf.Clamp01(t);
            return t * t * (3f - 2f * t);
        }

        public float Y(float t) => Mathf.Lerp(floorLine, horizon, Curve(t));
        public float Scale(float t) => Mathf.Lerp(nearScale, farScale, Curve(t));
        public float HalfWidth(float t) => Mathf.Lerp(nearHalfWidth, farHalfWidth, Curve(t));

        /// <summary>Distance-shade a colour so far modules sink into the haze.</summary>
        public Color Shade(Color c, float t, float minShade = 0.6f)
        {
            float s = Mathf.Lerp(1f, minShade, Curve(t));
            return new Color(c.r * s, c.g * s, Mathf.Lerp(c.b, c.b * 0.85f, Curve(t)), c.a);
        }
    }
}

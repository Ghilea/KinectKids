using UnityEngine;

namespace KinectKids3D.Platform
{
    // All distances are metres in Kinect skeleton space. Head-to-shoulder
    // distance gives a useful scale for both small children and adults.
    public sealed class KinectBodyGestureTracker
    {
        public struct Motion
        {
            public bool Tracked;
            public bool PlayerChanged;
            public float CenterDelta;
            public float HeadDelta;
            public int Side;
            public bool Jump;
            public bool Duck;
            public bool Run;
        }

        private long trackingId;
        private bool hasPlayer;
        private float baselineHead;
        private float baselineShoulder;
        private float baselineCenter;
        private float filteredHead;
        private float filteredShoulder;
        private float filteredCenter;
        private float previousHead;
        private float previousShoulder;
        private int side;
        private int lastPulseDirection;
        private int alternatingPulses;
        private float lastPulseAt;
        private float runUntil;
        private float jumpUntil;
        private float duckUntil;
        private float lastGestureAt;

        public void Reset()
        {
            hasPlayer = false;
            trackingId = 0;
            side = 0;
            lastPulseDirection = 0;
            alternatingPulses = 0;
            runUntil = jumpUntil = duckUntil = 0f;
        }

        public Motion Update(bool tracked, PlayerPose pose, float deltaTime, float now)
        {
            if (!tracked)
            {
                Reset();
                return new Motion();
            }

            float shoulder = Mathf.Abs(pose.ShoulderY) > 0.001f
                ? pose.ShoulderY : pose.HeadY - 0.28f;
            if (!hasPlayer || pose.TrackingId != trackingId)
            {
                trackingId = pose.TrackingId;
                hasPlayer = true;
                baselineHead = filteredHead = previousHead = pose.HeadY;
                baselineShoulder = filteredShoulder = previousShoulder = shoulder;
                baselineCenter = filteredCenter = pose.CenterX;
                side = 0;
                lastPulseDirection = alternatingPulses = 0;
                lastPulseAt = now;
                // Allow an intentional movement shortly after acquisition,
                // while still ignoring the first settling frames.
                lastGestureAt = now - 0.30f;
                runUntil = jumpUntil = duckUntil = 0f;
                return new Motion { Tracked = true, PlayerChanged = true };
            }

            float dt = Mathf.Max(0.005f, deltaTime);
            float blend = 1f - Mathf.Exp(-12f * dt);
            filteredHead = Mathf.Lerp(filteredHead, pose.HeadY, blend);
            filteredShoulder = Mathf.Lerp(filteredShoulder, shoulder, blend);
            filteredCenter = Mathf.Lerp(filteredCenter, pose.CenterX, blend);

            float size = Mathf.Clamp(baselineHead - baselineShoulder, 0.16f, 0.50f);
            float centerDelta = filteredCenter - baselineCenter;
            float headDelta = filteredHead - baselineHead;
            float shoulderDelta = filteredShoulder - baselineShoulder;
            float headVelocity = (filteredHead - previousHead) / dt;
            float shoulderVelocity = (filteredShoulder - previousShoulder) / dt;
            previousHead = filteredHead;
            previousShoulder = filteredShoulder;

            float sideEnter = Mathf.Clamp(size * 0.38f, 0.065f, 0.13f);
            float sideExit = sideEnter * 0.58f;
            if (side == 0)
            {
                if (centerDelta < -sideEnter) side = -1;
                else if (centerDelta > sideEnter) side = 1;
            }
            else if (side < 0)
            {
                if (centerDelta > sideEnter) side = 1;
                else if (centerDelta > -sideExit) side = 0;
            }
            else if (centerDelta < -sideEnter) side = -1;
            else if (centerDelta < sideExit) side = 0;

            float jumpDistance = Mathf.Clamp(size * 0.31f, 0.055f, 0.10f);
            float duckDistance = Mathf.Clamp(size * 0.34f, 0.060f, 0.11f);
            bool gestureReady = now - lastGestureAt > 0.45f;
            if (gestureReady && shoulderDelta > jumpDistance && headDelta > jumpDistance * 0.65f
                && shoulderVelocity > 0.055f)
            {
                jumpUntil = now + 0.32f;
                lastGestureAt = now;
                alternatingPulses = 0;
            }
            else if (gestureReady && headDelta < -duckDistance
                && (shoulderDelta < -duckDistance * 0.45f || headDelta < -duckDistance * 1.7f)
                && headVelocity < -0.055f)
            {
                duckUntil = now + 0.42f;
                lastGestureAt = now;
                alternatingPulses = 0;
            }

            // Running in place has repeated up/down reversals. A single jump or
            // duck has only one reversal and cannot start a sprint on its own.
            float motionVelocity = shoulderVelocity * 0.60f + headVelocity * 0.40f;
            float pulseSpeed = Mathf.Clamp(size * 0.22f, 0.040f, 0.075f);
            int direction = motionVelocity > pulseSpeed ? 1 : motionVelocity < -pulseSpeed ? -1 : 0;
            if (direction != 0 && direction != lastPulseDirection
                && now - lastPulseAt >= 0.11f && now >= jumpUntil && now >= duckUntil)
            {
                alternatingPulses = now - lastPulseAt <= 0.85f
                    ? Mathf.Min(4, alternatingPulses + 1) : 1;
                lastPulseDirection = direction;
                lastPulseAt = now;
                if (alternatingPulses >= 3) runUntil = now + 0.75f;
            }
            if (now - lastPulseAt > 0.9f) alternatingPulses = 0;

            return new Motion
            {
                Tracked = true,
                CenterDelta = centerDelta,
                HeadDelta = headDelta,
                Side = side,
                Jump = now < jumpUntil,
                Duck = now < duckUntil,
                Run = now < runUntil
            };
        }
    }
}

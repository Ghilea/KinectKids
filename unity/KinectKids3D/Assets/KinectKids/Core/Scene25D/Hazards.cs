using UnityEngine;

namespace KinectKids.Scene25D
{
    /// <summary>A low beam the player must DUCK under. Placeholder = horizontal beam.</summary>
    public sealed class LowBeamHazard : HazardBase
    {
        private SpriteRenderer sr;

        protected override void OnInit()
        {
            requiredAction = DodgeAction.Duck;
            sr = PlaceholderArt.NewSpriteObject("Beam", PlaceholderArt.Beam(),
                PlaceholderArt.DeepPurple, transform, SceneBand.Hazards);
            sr.transform.localScale = new Vector3(4.5f, 0.6f, 1f);
        }

        protected override void OnDepth(float depth01)
        {
            // Beam hangs high; player ducks under it.
            transform.localPosition = new Vector3(transform.localPosition.x,
                Mathf.Lerp(1.6f, 0.2f, depth01), transform.localPosition.z);
        }

        protected override void OnResolved(bool avoided)
        {
            if (sr != null) sr.color = avoided ? PlaceholderArt.DeepPurple : new Color(0.9f, 0.2f, 0.2f);
        }
    }

    /// <summary>A falling object the player must dodge LEFT or RIGHT.</summary>
    public sealed class FallingObstacleHazard : HazardBase
    {
        private SpriteRenderer sr;
        private float side; // -1 falls on left, +1 on right

        protected override void OnInit()
        {
            side = Random.value < 0.5f ? -1f : 1f;
            // Player must move to the OPPOSITE side to avoid it.
            requiredAction = side < 0 ? DodgeAction.Right : DodgeAction.Left;
            sr = PlaceholderArt.NewSpriteObject("Falling", PlaceholderArt.SolidBlock(),
                PlaceholderArt.Shadow, transform, SceneBand.Hazards);
            sr.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
        }

        protected override void OnDepth(float depth01)
        {
            float x = side * Mathf.Lerp(0.2f, 2.4f, depth01);
            transform.localPosition = new Vector3(x, transform.localPosition.y, transform.localPosition.z);
            if (sr != null) sr.transform.localRotation = Quaternion.Euler(0, 0, depth01 * 220f * -side);
        }

        protected override bool EvaluateAvoided()
        {
            if (dodgeSource == null) return true;
            // Avoided if player moved to the safe side (matched required action)
            // or is already standing clear on that side.
            if (dodgeSource.CurrentDodge == requiredAction) return true;
            return requiredAction == DodgeAction.Left
                ? dodgeSource.LateralPosition < -0.3f
                : dodgeSource.LateralPosition > 0.3f;
        }

        protected override void OnResolved(bool avoided)
        {
            if (sr != null) sr.color = avoided ? PlaceholderArt.Shadow : new Color(0.9f, 0.2f, 0.2f);
        }
    }

    /// <summary>An object swinging in from the side; dodge to the opposite side.</summary>
    public sealed class SideObjectHazard : HazardBase
    {
        private SpriteRenderer sr;
        private float side;

        protected override void OnInit()
        {
            side = Random.value < 0.5f ? -1f : 1f;
            requiredAction = side < 0 ? DodgeAction.Right : DodgeAction.Left;
            sr = PlaceholderArt.NewSpriteObject("SideObject", PlaceholderArt.Beam(),
                new Color(0.35f, 0.25f, 0.15f), transform, SceneBand.Hazards);
            sr.transform.localScale = new Vector3(2.6f, 0.9f, 1f);
        }

        protected override void OnDepth(float depth01)
        {
            float x = side * Mathf.Lerp(3.0f, 0.6f, depth01);
            transform.localPosition = new Vector3(x, transform.localPosition.y, transform.localPosition.z);
            if (sr != null) sr.transform.localRotation = Quaternion.Euler(0, 0, side * -25f);
        }

        protected override bool EvaluateAvoided()
        {
            if (dodgeSource == null) return true;
            if (dodgeSource.CurrentDodge == requiredAction) return true;
            return requiredAction == DodgeAction.Left
                ? dodgeSource.LateralPosition < -0.3f
                : dodgeSource.LateralPosition > 0.3f;
        }

        protected override void OnResolved(bool avoided)
        {
            if (sr != null) sr.color = avoided ? new Color(0.35f, 0.25f, 0.15f) : new Color(0.9f, 0.2f, 0.2f);
        }
    }

    /// <summary>A hole/low object the player must JUMP over.</summary>
    public sealed class JumpHazard : HazardBase
    {
        private SpriteRenderer sr;

        protected override void OnInit()
        {
            requiredAction = DodgeAction.Jump;
            sr = PlaceholderArt.NewSpriteObject("JumpObstacle", PlaceholderArt.SolidBlock(),
                PlaceholderArt.Shadow, transform, SceneBand.Hazards);
            sr.transform.localScale = new Vector3(3.2f, 0.5f, 1f);
        }

        protected override void OnDepth(float depth01)
        {
            transform.localPosition = new Vector3(0f,
                Mathf.Lerp(0.4f, -1.9f, depth01), transform.localPosition.z);
        }

        protected override void OnResolved(bool avoided)
        {
            if (sr != null) sr.color = avoided ? PlaceholderArt.Shadow : new Color(0.9f, 0.2f, 0.2f);
        }
    }

    /// <summary>Data-driven spawn kinds so a scene script/timeline stays art-agnostic.</summary>
    public enum HazardKind
    {
        LowBeam,
        Falling,
        Side,
        Jump
    }

    /// <summary>Spawns hazards by kind. New kinds only need a case here.</summary>
    public static class HazardFactory
    {
        public static HazardBase Spawn(HazardKind kind, Transform parent, IDodgeSource source)
        {
            GameObject go = new GameObject("Hazard_" + kind);
            go.transform.SetParent(parent, false);
            HazardBase hazard;
            switch (kind)
            {
                case HazardKind.Falling: hazard = go.AddComponent<FallingObstacleHazard>(); break;
                case HazardKind.Side: hazard = go.AddComponent<SideObjectHazard>(); break;
                case HazardKind.Jump: hazard = go.AddComponent<JumpHazard>(); break;
                default: hazard = go.AddComponent<LowBeamHazard>(); break;
            }
            hazard.Init(source);
            return hazard;
        }
    }
}

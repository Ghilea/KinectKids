using UnityEngine;

namespace KinectKids.Scene25D
{
    /// <summary>
    /// The abstract dodge action a hazard demands. Kept independent of Kinect,
    /// keyboard or any specific input source so hazards stay reusable across
    /// games and input backends.
    /// </summary>
    public enum DodgeAction
    {
        None,
        Duck,
        Jump,
        Left,
        Right
    }

    /// <summary>
    /// Anything that can tell a hazard which dodge the player is currently doing.
    /// The chase director implements this on top of the platform Kinect input,
    /// but tests or other games can supply their own.
    /// </summary>
    public interface IDodgeSource
    {
        DodgeAction CurrentDodge { get; }
        /// <summary>Lateral position of the player, -1 (left) .. 1 (right).</summary>
        float LateralPosition { get; }
    }

    /// <summary>Outcome reported by a hazard when it reaches the player.</summary>
    public enum HazardResult
    {
        Pending,
        Avoided,
        Hit
    }
}

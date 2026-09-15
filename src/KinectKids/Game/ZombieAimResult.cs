using System.Windows;

namespace KinectKids.Game
{
    public sealed class ZombieAimResult
    {
        public bool IsAiming { get; set; }
        public double Progress { get; set; }
        public int Points { get; set; }
        public Point HitPoint { get; set; }
        public bool WasBossHit { get; set; }
    }
}

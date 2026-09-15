using System.Windows.Controls;

namespace KinectKids.Game
{
    internal sealed class ZombieTarget
    {
        public Grid Visual { get; set; }
        public Border HealthFill { get; set; }
        public double LaneX { get; set; }
        public double Age { get; set; }
        public double Lifetime { get; set; }
        public double Phase { get; set; }
        public int Health { get; set; }
        public int MaxHealth { get; set; }
        public bool IsBoss { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }
    }
}

using System.Windows.Media;
using System.Windows.Shapes;

namespace KinectKids.Game
{
    public class Balloon
    {
        public Ellipse Shape { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Radius { get; set; }
        public double Speed { get; set; }
        public int Points { get; set; }
        public Brush Color { get; set; }
    }

    /// <summary>
    /// Balong för matte-spel med text.
    /// </summary>
    public sealed class MathBalloon : Balloon
    {
        public System.Windows.Controls.TextBlock Label { get; set; }
        public int Answer { get; set; }
        public double BaseX { get; set; }
        public double SwayAmplitude { get; set; }
        public double SwayRate { get; set; }
        public double SwayPhase { get; set; }
    }
}

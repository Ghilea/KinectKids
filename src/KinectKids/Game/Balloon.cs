using System.Windows.Media;
using System.Windows.Shapes;

namespace KinectKids.Game
{
    public sealed class Balloon
    {
        public Ellipse Shape { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double Radius { get; set; }
        public double Speed { get; set; }
        public int Points { get; set; }
        public Brush Color { get; set; }
        public bool AnswerCorrect { get; set; }
    }

    /// <summary>
    /// Balong för matte-spel med text.
    /// </summary>
    public sealed class MathBalloon : Balloon
    {
        public string AnswerText { get; set; }
    }
}

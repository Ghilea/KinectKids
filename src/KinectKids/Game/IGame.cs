using System;
using System.Windows.Media;

namespace KinectKids.Game
{
    /// <summary>
    /// Gemensamt kontrakt för fristående spelmoduler. Kinect- och musinmatning
    /// hålls utanför modulen så att samma spel kan testas utan en sensor.
    /// </summary>
    public interface IGame
    {
        event EventHandler<GameEventArgs> GameEvent;

        string Id { get; }
        string DisplayName { get; }
        string Instruction { get; }
        int CurrentScore { get; }
        bool IsActive { get; }

        void Start();
        void Stop();
        void Reset();
        void Update(double elapsedSeconds);
        void Render(DrawingContext context);
        void OnKinectGesture(GestureType gesture);
    }

    public enum GestureType
    {
        None,
        HandsUp,
        ArmsOut,
        HandsTogether,
        Duck,
        Punch
    }

    public sealed class GameEventArgs : EventArgs
    {
        public string Message { get; set; }
        public int ScoreDelta { get; set; }
        public int PlayerIndex { get; set; }
        public bool RoundCompleted { get; set; }
    }
}

using System;
using System.Windows;

namespace KinectKids.Game
{
    public interface IGame
    {
        event EventHandler<GameEventArgs> OnGameEvent;
        void Start();
        void Stop();
        void Reset();
        void Update(double dt);
        void Draw(DrawingContext context);
        void OnKinectGesture(GestureType gesture);
        int CurrentScore { get; }
        bool IsActive { get; }
    }

    public enum GestureType
    {
        HandsUp,
        PointLeft,
        PointRight,
        HandsParallel,
        HandDuck,
        HandsPlus,
        Punch,
        None
    }

    public class GameEventArgs : EventArgs
    {
        public int ScoreDelta { get; set; }
        public string Message { get; set; }
    }
}

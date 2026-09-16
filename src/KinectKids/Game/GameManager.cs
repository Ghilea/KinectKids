using System;
using System.Windows.Media;

namespace KinectKids.Game
{
    /// <summary>
    /// Livscykel för en aktiv IGame-modul. Huvudfönstret kan stegvis flyttas
    /// över till denna utan att de redan fungerande spelen behöver skrivas om.
    /// </summary>
    public sealed class GameManager
    {
        private IGame currentGame;

        public event EventHandler<GameEventArgs> GameEvent;

        public IGame CurrentGame => currentGame;
        public bool IsActive => currentGame != null && currentGame.IsActive;
        public int CurrentScore => currentGame == null ? 0 : currentGame.CurrentScore;

        public void Select(IGame game)
        {
            if (ReferenceEquals(currentGame, game)) return;
            if (currentGame != null)
            {
                currentGame.GameEvent -= ForwardEvent;
                currentGame.Stop();
            }

            currentGame = game;
            if (currentGame != null)
                currentGame.GameEvent += ForwardEvent;
        }

        public void Start()
        {
            if (currentGame == null)
                throw new InvalidOperationException("Välj ett spel innan rundan startas.");
            currentGame.Start();
        }

        public void Stop()
        {
            if (currentGame != null) currentGame.Stop();
        }

        public void Reset()
        {
            if (currentGame != null) currentGame.Reset();
        }

        public void Update(double elapsedSeconds)
        {
            if (currentGame != null && currentGame.IsActive)
                currentGame.Update(elapsedSeconds);
        }

        public void Render(DrawingContext context)
        {
            if (currentGame != null)
                currentGame.Render(context);
        }

        public void HandleGesture(GestureType gesture)
        {
            if (currentGame != null && currentGame.IsActive)
                currentGame.OnKinectGesture(gesture);
        }

        private void ForwardEvent(object sender, GameEventArgs e)
        {
            GameEvent?.Invoke(this, e);
        }
    }
}

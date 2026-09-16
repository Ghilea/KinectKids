using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace KinectKids.Game
{
    public sealed class GameManager
    {
        private IGame _currentGame;
        private bool _gameActive;
        private DateTime _instructionHideAt;
        private string _calibrationMessage;

        public event EventHandler<GameEventArgs> OnGameManagerEvent;

        public IGame CurrentGame => _currentGame;
        public int CurrentScore => _currentGame?.CurrentScore ?? 0;
        public bool IsActive => _gameActive;

        public void SetActiveGame(IGame game)
        {
            if (_currentGame != null)
            {
                _currentGame.Stop();
            }

            _currentGame = game;
            OnGameManagerEvent?.Invoke(this, new GameEventArgs 
            {
                Message = $"Byter till spel: {game.GetType().Name}"
            });
        }

        public void Start()
        {
            if (_currentGame == null)
            {
                throw new InvalidOperationException("Inget spel s\u00E4tts.");
            }

            _gameActive = true;
            _instructionHideAt = DateTime.UtcNow.AddSeconds(10);
            _currentGame.Start();
        }

        public void Stop()
        {
            if (_currentGame != null)
            {
                _currentGame.Stop();
                _currentGame = null;
            }
            _gameActive = false;
        }

        public void Reset()
        {
            if (_currentGame != null)
            {
                _currentGame.Reset();
            }
        }

        public void Update(double dt)
        {
            if (_currentGame == null) return;
            
            _currentGame.Update(dt);
            OnGameManagerEvent?.Invoke(this, new GameEventArgs 
            {
                Message = $"Po\u00E5ng: {_currentGame.CurrentScore}"
            });
        }

        public void Draw(DrawingContext context)
        {
            if (_currentGame != null)
            {
                _currentGame.Draw(context);
            }
        }

        public void OnKinectGesture(GestureType gesture)
        {
            if (_currentGame != null && _gameActive)
            {
                _currentGame.OnKinectGesture(gesture);
            }
        }

        public string CalibrationMessage => _calibrationMessage;
        public void SetCalibrationMessage(string message)
        {
            _calibrationMessage = message;
        }

        public void HideInstruction()
        {
            DateTime now = DateTime.UtcNow;
            if (now >= _instructionHideAt && _currentGame != null)
            {
                OnGameManagerEvent?.Invoke(this, new GameEventArgs 
                { 
                    Message = "Instruktion d\u00F6ljd." 
                });
            }
        }
    }
}

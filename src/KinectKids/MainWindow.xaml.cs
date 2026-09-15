using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using KinectKids.Game;
using KinectKids.Input;
using KinectKids.Models;

namespace KinectKids
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer gameTimer;
        private readonly Stopwatch frameClock = new Stopwatch();
        private readonly Ellipse[] handCursors = new Ellipse[4];
        private readonly int[] scores = new int[2];
        private IPlayerTracker tracker;
        private GameEngine game;
        private IReadOnlyList<TrackedPlayer> players = Array.Empty<TrackedPlayer>();
        private DateTime roundEndsAt;
        private bool isPlaying;
        private bool isPaused;
        private TimeSpan pausedRemaining;
        private bool isFullscreen;

        public MainWindow()
        {
            InitializeComponent();
            game = new GameEngine(Playfield);
            CreateHandCursors();

            gameTimer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            gameTimer.Tick += OnGameTick;

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SwitchTracker(new KinectPlayerTracker());
        }

        private void SwitchTracker(IPlayerTracker next)
        {
            if (tracker != null)
            {
                tracker.PlayersChanged -= OnPlayersChanged;
                tracker.StatusChanged -= OnStatusChanged;
                tracker.Dispose();
            }

            tracker = next;
            tracker.PlayersChanged += OnPlayersChanged;
            tracker.StatusChanged += OnStatusChanged;
            InputModeButton.Content = tracker is MousePlayerTracker ? "Använd Kinect" : "Testa med mus";
            UpdateSensorStatus(tracker.Status, tracker.IsConnected);
            tracker.Start();
        }

        private void OnStatusChanged(object sender, string status)
        {
            Dispatcher.BeginInvoke(new Action(() => UpdateSensorStatus(status, tracker != null && tracker.IsConnected)));
        }

        private void OnPlayersChanged(object sender, IReadOnlyList<TrackedPlayer> updatedPlayers)
        {
            var snapshot = updatedPlayers?.ToArray() ?? Array.Empty<TrackedPlayer>();
            Dispatcher.BeginInvoke(new Action(() =>
            {
                players = snapshot;
                UpdateCalibration();
                UpdateHands();
            }));
        }

        private void UpdateSensorStatus(string text, bool connected)
        {
            SensorText.Text = text;
            bool mouse = tracker is MousePlayerTracker;
            SensorDot.Fill = connected ? BrushFrom("#50E3A4") : BrushFrom("#FFBE47");
            SensorBadge.Background = mouse ? BrushFrom("#335EAEFF") : connected ? BrushFrom("#3324D49A") : BrushFrom("#44FFBE47");
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            ShowCalibration();
        }

        private void MouseModeButton_Click(object sender, RoutedEventArgs e)
        {
            SwitchTracker(tracker is MousePlayerTracker
                ? (IPlayerTracker)new KinectPlayerTracker()
                : new MousePlayerTracker(this));
            ShowCalibration();
        }

        private void ShowCalibration()
        {
            StopRound();
            HomePanel.Visibility = Visibility.Collapsed;
            GamePanel.Visibility = Visibility.Collapsed;
            CalibrationPanel.Visibility = Visibility.Visible;
            UpdateCalibration();
        }

        private void UpdateCalibration()
        {
            if (CalibrationPanel.Visibility != Visibility.Visible) return;

            var ready = players.Where(item => item.IsReady).Take(2).ToArray();
            CalibrationStatus.Text = ready.Length == 0
                ? "Vi väntar tills Kinect ser minst en spelare…"
                : ready.Length == 1
                    ? "Spelare 1 är redo! En till kan hoppa in om ni vill."
                    : "Båda spelarna är redo!";

            ContinueButton.IsEnabled = ready.Length > 0;
            ContinueButton.Opacity = ready.Length > 0 ? 1 : 0.45;
            PositionReadyMarker(PlayerOneReady, ready.ElementAtOrDefault(0));
            PositionReadyMarker(PlayerTwoReady, ready.ElementAtOrDefault(1));
        }

        private void PositionReadyMarker(Ellipse marker, TrackedPlayer player)
        {
            if (player == null || CalibrationCanvas.ActualWidth <= 0)
            {
                marker.Visibility = Visibility.Collapsed;
                return;
            }

            marker.Visibility = Visibility.Visible;
            double x = player.Hip.X * CalibrationCanvas.ActualWidth - marker.Width / 2;
            double y = player.Hip.Y * CalibrationCanvas.ActualHeight - marker.Height / 2;
            Canvas.SetLeft(marker, x);
            Canvas.SetTop(marker, y);
        }

        private async void ContinueButton_Click(object sender, RoutedEventArgs e)
        {
            await BeginRoundAsync();
        }

        private async void ReplayButton_Click(object sender, RoutedEventArgs e)
        {
            await BeginRoundAsync();
        }

        private async Task BeginRoundAsync()
        {
            CalibrationPanel.Visibility = Visibility.Collapsed;
            HomePanel.Visibility = Visibility.Collapsed;
            GamePanel.Visibility = Visibility.Visible;
            PauseOverlay.Visibility = Visibility.Collapsed;
            ResultOverlay.Visibility = Visibility.Collapsed;
            PauseButton.Visibility = Visibility.Visible;
            game.Reset();
            scores[0] = 0;
            scores[1] = 0;
            UpdateScoreboard();
            UpdateHands();

            CountdownOverlay.Visibility = Visibility.Visible;
            for (int value = 3; value >= 1; value--)
            {
                CountdownText.Text = value.ToString();
                await Task.Delay(700);
                if (GamePanel.Visibility != Visibility.Visible) return;
            }
            CountdownText.Text = "KÖR!";
            await Task.Delay(500);
            if (GamePanel.Visibility != Visibility.Visible) return;
            CountdownOverlay.Visibility = Visibility.Collapsed;

            isPlaying = true;
            isPaused = false;
            roundEndsAt = DateTime.UtcNow.AddSeconds(60);
            frameClock.Restart();
            gameTimer.Start();
        }

        private void OnGameTick(object sender, EventArgs e)
        {
            if (!isPlaying || isPaused) return;
            double elapsed = Math.Min(0.05, frameClock.Elapsed.TotalSeconds);
            frameClock.Restart();
            game.Update(elapsed);

            var remaining = roundEndsAt - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                FinishRound();
                return;
            }
            TimeText.Text = Math.Ceiling(remaining.TotalSeconds).ToString("0");
        }

        private void UpdateHands()
        {
            if (GamePanel.Visibility != Visibility.Visible || Playfield.ActualWidth <= 0) return;
            for (int index = 0; index < handCursors.Length; index++) handCursors[index].Visibility = Visibility.Collapsed;

            var activePlayers = players.Where(item => item.IsReady).Take(2).ToArray();
            PlayerTwoScoreBox.Visibility = activePlayers.Length > 1 ? Visibility.Visible : Visibility.Collapsed;

            for (int playerIndex = 0; playerIndex < activePlayers.Length; playerIndex++)
            {
                ShowHand(playerIndex * 2, activePlayers[playerIndex].LeftHand, playerIndex);
                ShowHand(playerIndex * 2 + 1, activePlayers[playerIndex].RightHand, playerIndex);
            }
        }

        private void ShowHand(int cursorIndex, Point normalized, int playerIndex)
        {
            if (normalized.X < 0 || normalized.Y < 0) return;
            var cursor = handCursors[cursorIndex];
            double x = normalized.X * Playfield.ActualWidth;
            double y = normalized.Y * Playfield.ActualHeight;
            cursor.Visibility = Visibility.Visible;
            Canvas.SetLeft(cursor, x - cursor.Width / 2);
            Canvas.SetTop(cursor, y - cursor.Height / 2);

            if (isPlaying && !isPaused)
            {
                int points = game.PopAt(new Point(x, y), cursor.Width * 0.42);
                if (points > 0)
                {
                    scores[playerIndex] += points;
                    UpdateScoreboard();
                    Pulse(cursor);
                }
            }
        }

        private static void Pulse(Ellipse cursor)
        {
            cursor.RenderTransformOrigin = new Point(0.5, 0.5);
            var scale = new ScaleTransform(1.35, 1.35);
            cursor.RenderTransform = scale;
            var animation = new System.Windows.Media.Animation.DoubleAnimation(1.35, 1, TimeSpan.FromMilliseconds(160));
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }

        private void CreateHandCursors()
        {
            for (int index = 0; index < handCursors.Length; index++)
            {
                bool playerTwo = index >= 2;
                var cursor = new Ellipse
                {
                    Width = 58,
                    Height = 58,
                    Fill = playerTwo ? BrushFrom("#CFFF7694") : BrushFrom("#CF5EAEFF"),
                    Stroke = Brushes.White,
                    StrokeThickness = 5,
                    Visibility = Visibility.Collapsed,
                    IsHitTestVisible = false
                };
                handCursors[index] = cursor;
                Playfield.Children.Add(cursor);
            }
        }

        private void UpdateScoreboard()
        {
            PlayerOneScore.Text = scores[0].ToString();
            PlayerTwoScore.Text = scores[1].ToString();
        }

        private void FinishRound()
        {
            isPlaying = false;
            gameTimer.Stop();
            PauseButton.Visibility = Visibility.Collapsed;
            ResultOverlay.Visibility = Visibility.Visible;
            ResultText.Text = PlayerTwoScoreBox.Visibility == Visibility.Visible
                ? $"Spelare 1: {scores[0]}  •  Spelare 2: {scores[1]}"
                : $"Du fick {scores[0]} poäng";
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            if (!isPlaying) return;
            if (!isPaused)
            {
                isPaused = true;
                pausedRemaining = roundEndsAt - DateTime.UtcNow;
                PauseOverlay.Visibility = Visibility.Visible;
                PauseButton.Content = "Fortsätt";
            }
            else
            {
                isPaused = false;
                roundEndsAt = DateTime.UtcNow + pausedRemaining;
                frameClock.Restart();
                PauseOverlay.Visibility = Visibility.Collapsed;
                PauseButton.Content = "Paus";
            }
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            StopRound();
            CalibrationPanel.Visibility = Visibility.Collapsed;
            GamePanel.Visibility = Visibility.Collapsed;
            HomePanel.Visibility = Visibility.Visible;
        }

        private void StopRound()
        {
            isPlaying = false;
            isPaused = false;
            gameTimer.Stop();
            game.Reset();
            PauseOverlay.Visibility = Visibility.Collapsed;
            ResultOverlay.Visibility = Visibility.Collapsed;
            CountdownOverlay.Visibility = Visibility.Collapsed;
        }

        private void FullscreenButton_Click(object sender, RoutedEventArgs e) => ToggleFullscreen();

        private void ToggleFullscreen()
        {
            isFullscreen = !isFullscreen;
            WindowStyle = isFullscreen ? WindowStyle.None : WindowStyle.SingleBorderWindow;
            WindowState = isFullscreen ? WindowState.Maximized : WindowState.Normal;
            FullscreenButton.Content = isFullscreen ? "Fönster" : "Helskärm";
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11) ToggleFullscreen();
            if (e.Key == Key.Escape)
            {
                if (isFullscreen) ToggleFullscreen();
                else if (HomePanel.Visibility != Visibility.Visible) HomeButton_Click(sender, e);
            }
            if (e.Key == Key.Space && isPlaying) PauseButton_Click(sender, e);
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            gameTimer.Stop();
            tracker?.Dispose();
        }

        private static Brush BrushFrom(string color) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
    }
}

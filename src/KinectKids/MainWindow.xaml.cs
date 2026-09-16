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
using KinectKids.Services;

namespace KinectKids
{
    public partial class MainWindow : Window
    {
        private static readonly BodyJoint[][] Bones =
        {
            new[] { BodyJoint.Head, BodyJoint.ShoulderCenter },
            new[] { BodyJoint.ShoulderCenter, BodyJoint.ShoulderLeft },
            new[] { BodyJoint.ShoulderLeft, BodyJoint.ElbowLeft },
            new[] { BodyJoint.ElbowLeft, BodyJoint.WristLeft },
            new[] { BodyJoint.WristLeft, BodyJoint.HandLeft },
            new[] { BodyJoint.ShoulderCenter, BodyJoint.ShoulderRight },
            new[] { BodyJoint.ShoulderRight, BodyJoint.ElbowRight },
            new[] { BodyJoint.ElbowRight, BodyJoint.WristRight },
            new[] { BodyJoint.WristRight, BodyJoint.HandRight },
            new[] { BodyJoint.ShoulderCenter, BodyJoint.Spine },
            new[] { BodyJoint.Spine, BodyJoint.HipCenter },
            new[] { BodyJoint.HipCenter, BodyJoint.HipLeft },
            new[] { BodyJoint.HipLeft, BodyJoint.KneeLeft },
            new[] { BodyJoint.KneeLeft, BodyJoint.AnkleLeft },
            new[] { BodyJoint.AnkleLeft, BodyJoint.FootLeft },
            new[] { BodyJoint.HipCenter, BodyJoint.HipRight },
            new[] { BodyJoint.HipRight, BodyJoint.KneeRight },
            new[] { BodyJoint.KneeRight, BodyJoint.AnkleRight },
            new[] { BodyJoint.AnkleRight, BodyJoint.FootRight }
        };

        private readonly DispatcherTimer gameTimer;
        private readonly Stopwatch frameClock = new Stopwatch();
        private readonly Grid[] handCursors = new Grid[4];
        private readonly Border[] handAimProgress = new Border[4];
        private readonly int[] scores = new int[2];
        private IPlayerTracker tracker;
        private GameEngine game;
        private ZombieRailEngine zombieGame;
        private MathGame mathGame;
        private SimonSaysGame simonGame;
        private readonly GameManager moduleManager = new GameManager();
        private readonly ProgressionService progression = new ProgressionService();
        private IReadOnlyList<TrackedPlayer> players = Array.Empty<TrackedPlayer>();
        private GameMode selectedGame = GameMode.Balloons;
        private DateTime roundEndsAt;
        private bool isPlaying;
        private bool isPaused;
        private TimeSpan pausedRemaining;
        private bool isFullscreen;
        private DateTime instructionHidesAt;
        private Button dwellButton;
        private DateTime dwellStartedAt;
        private bool dwellTriggered;

        public MainWindow()
        {
            InitializeComponent();
            game = new GameEngine(Playfield);
            zombieGame = new ZombieRailEngine(Playfield);
            mathGame = new MathGame(Playfield);
            simonGame = new SimonSaysGame();
            moduleManager.GameEvent += OnModuleGameEvent;
            CreateHandCursors();
            UpdateProgressionUi();

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
                simonGame.SetPlayers(snapshot);
                RenderSkeletons(CalibrationSkeletonCanvas);
                RenderSkeletons(GameSkeletonCanvas);
                UpdateCalibration();
                UpdateHands();
                UpdateMenuControl();
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
            selectedGame = GameMode.Balloons;
            ShowCalibration();
        }

        private void MathStartButton_Click(object sender, RoutedEventArgs e)
        {
            selectedGame = GameMode.Math;
            ShowCalibration();
        }

        private void SimonStartButton_Click(object sender, RoutedEventArgs e)
        {
            selectedGame = GameMode.SimonSays;
            ShowCalibration();
        }

        private void ZombieStartButton_Click(object sender, RoutedEventArgs e)
        {
            selectedGame = GameMode.ZombieTrain;
            ShowCalibration();
        }

        private void SpookyAdventureButton_Click(object sender, RoutedEventArgs e)
        {
            if (!progression.IsGameUnlocked("SpookyAdventure3D"))
            {
                UpdateProgressionUi();
                return;
            }

            string executable = FindSpookyAdventureExecutable();
            if (executable == null)
            {
                MessageBox.Show(
                    "Spökjakten 3D är upplåst men behöver byggas i Unity först.\n\n" +
                    "Öppna unity\\KinectKids3D i Unity 6 och bygg Windows-versionen till " +
                    "unity\\KinectKids3D\\Build.",
                    "Spökjakten 3D",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            tracker?.Stop();
            Process.Start(new ProcessStartInfo(executable)
            {
                WorkingDirectory = System.IO.Path.GetDirectoryName(executable)
            });
            Close();
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
            switch (selectedGame)
            {
                case GameMode.Math:
                    CalibrationTitle.Text = "Gör er redo för Matematikbanan";
                    break;
                case GameMode.SimonSays:
                    CalibrationTitle.Text = "Gör er redo för Simon säger";
                    break;
                case GameMode.ZombieTrain:
                    CalibrationTitle.Text = "Gör er redo för Zombietåget";
                    break;
                default:
                    CalibrationTitle.Text = "Gör er redo för Ballongjakten";
                    break;
            }
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
            var first = ready.ElementAtOrDefault(0);
            var second = ready.ElementAtOrDefault(1);
            PositionHandMarker(PlayerOneLeftHand, first?.LeftHand);
            PositionHandMarker(PlayerOneRightHand, first?.RightHand);
            PositionHandMarker(PlayerTwoLeftHand, second?.LeftHand);
            PositionHandMarker(PlayerTwoRightHand, second?.RightHand);
        }

        private void PositionHandMarker(Ellipse marker, Point? hand)
        {
            if (!hand.HasValue || hand.Value.X < 0 || CalibrationCanvas.ActualWidth <= 0)
            {
                marker.Visibility = Visibility.Collapsed;
                return;
            }

            marker.Visibility = Visibility.Visible;
            double x = hand.Value.X * CalibrationCanvas.ActualWidth - marker.Width / 2;
            double y = hand.Value.Y * CalibrationCanvas.ActualHeight - marker.Height / 2;
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
            GameInstruction.Visibility = Visibility.Visible;
            game.Reset();
            zombieGame.Reset();
            mathGame.Reset();
            simonGame.Reset();
            IGame selectedModule = null;
            if (selectedGame == GameMode.Math) selectedModule = mathGame;
            if (selectedGame == GameMode.SimonSays) selectedModule = simonGame;
            moduleManager.Select(selectedModule);
            ConfigureGameMode();
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
            if (moduleManager.CurrentGame != null) moduleManager.Start();
            roundEndsAt = DateTime.UtcNow.AddSeconds(60);
            instructionHidesAt = DateTime.UtcNow.AddSeconds(selectedGame == GameMode.ZombieTrain ? 11 : 8);
            frameClock.Restart();
            gameTimer.Start();
        }

        private void ConfigureGameMode()
        {
            bool zombies = selectedGame == GameMode.ZombieTrain;
            ZombieBackdrop.Visibility = zombies ? Visibility.Visible : Visibility.Collapsed;
            BalloonBackdrop.Visibility = zombies ? Visibility.Collapsed : Visibility.Visible;
            BalloonBackdrop.Background = selectedGame == GameMode.Math
                ? BrushFrom("#173D63")
                : selectedGame == GameMode.SimonSays ? BrushFrom("#282B57") : BrushFrom("#102B46");
            Playfield.Background = Brushes.Transparent;
            GameSkeletonCanvas.Opacity = selectedGame == GameMode.SimonSays ? 0.9 : zombies ? 0.46 : 0.72;
            BossBanner.Visibility = Visibility.Collapsed;
            TimeText.Text = "60";
            switch (selectedGame)
            {
                case GameMode.Math:
                    GameInstructionText.Text = "Träffa ballongen med rätt svar!";
                    break;
                case GameMode.SimonSays:
                    GameInstructionText.Text = "Följ rörelsen som Simon visar!";
                    break;
                case GameMode.ZombieTrain:
                    GameInstructionText.Text = "Håll en handring på en zombie tills mätaren under handen fylls!";
                    break;
                default:
                    GameInstructionText.Text = "De färgade ringarna är dina händer – rör en ring in i en ballong!";
                    break;
            }
        }

        private void OnGameTick(object sender, EventArgs e)
        {
            if (!isPlaying || isPaused) return;
            double elapsed = Math.Min(0.05, frameClock.Elapsed.TotalSeconds);
            frameClock.Restart();
            if (selectedGame == GameMode.ZombieTrain)
            {
                zombieGame.Update(elapsed);
                UpdateRailBackground();
                BossBanner.Visibility = zombieGame.BossIsActive ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (selectedGame == GameMode.Math || selectedGame == GameMode.SimonSays)
            {
                moduleManager.Update(elapsed);
                if (moduleManager.CurrentGame != null)
                    GameInstructionText.Text = moduleManager.CurrentGame.Instruction;
            }
            else
            {
                game.Update(elapsed);
            }
            if (selectedGame != GameMode.Math
                && selectedGame != GameMode.SimonSays
                && DateTime.UtcNow >= instructionHidesAt)
                GameInstruction.Visibility = Visibility.Collapsed;

            var remaining = roundEndsAt - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero)
            {
                FinishRound();
                return;
            }
            TimeText.Text = Math.Ceiling(remaining.TotalSeconds).ToString("0");
        }

        private void UpdateRailBackground()
        {
            double progress = Math.Max(0, Math.Min(1, 1 - (roundEndsAt - DateTime.UtcNow).TotalSeconds / 60.0));
            double crossFade = Math.Max(0, Math.Min(1, (progress - 0.44) / 0.16));
            ZombieStationBackground.Opacity = 1 - crossFade;
            ZombieTunnelBackground.Opacity = crossFade;
            StationScale.ScaleX = StationScale.ScaleY = 1.04 + progress * 0.09;
            TunnelScale.ScaleX = TunnelScale.ScaleY = 1.02 + Math.Max(0, progress - 0.4) * 0.11;
            StationTranslate.X = Math.Sin(progress * Math.PI * 5) * 9;
            TunnelTranslate.X = Math.Sin(progress * Math.PI * 6 + 1.2) * 11;
        }

        private void UpdateHands()
        {
            if (GamePanel.Visibility != Visibility.Visible || Playfield.ActualWidth <= 0) return;
            for (int index = 0; index < handCursors.Length; index++)
            {
                handCursors[index].Visibility = Visibility.Collapsed;
                handAimProgress[index].Width = 0;
            }

            var activePlayers = players.Where(item => item.IsReady).Take(2).ToArray();
            PlayerTwoScoreBox.Visibility = activePlayers.Length > 1 ? Visibility.Visible : Visibility.Collapsed;

            for (int playerIndex = 0; playerIndex < activePlayers.Length; playerIndex++)
            {
                ShowHand(playerIndex * 2, activePlayers[playerIndex].LeftHand, playerIndex);
                ShowHand(playerIndex * 2 + 1, activePlayers[playerIndex].RightHand, playerIndex);
            }

            if (selectedGame == GameMode.ZombieTrain)
            {
                for (int index = activePlayers.Length * 2; index < handCursors.Length; index++)
                    zombieGame.ClearAim(index);
            }
        }

        private void ShowHand(int cursorIndex, Point normalized, int playerIndex)
        {
            if (normalized.X < 0 || normalized.Y < 0)
            {
                if (selectedGame == GameMode.ZombieTrain) zombieGame.ClearAim(cursorIndex);
                return;
            }
            var cursor = handCursors[cursorIndex];
            double x = normalized.X * Playfield.ActualWidth;
            double y = normalized.Y * Playfield.ActualHeight;
            cursor.Visibility = Visibility.Visible;
            Canvas.SetLeft(cursor, x - cursor.Width / 2);
            Canvas.SetTop(cursor, y - 31);

            if (isPlaying && !isPaused)
            {
                int points;
                if (selectedGame == GameMode.ZombieTrain)
                {
                    ZombieAimResult result = zombieGame.AimAt(new Point(x, y), cursorIndex, DateTime.UtcNow);
                    handAimProgress[cursorIndex].Width = 64 * result.Progress;
                    points = result.Points;
                }
                else if (selectedGame == GameMode.Math)
                {
                    handAimProgress[cursorIndex].Width = 0;
                    points = mathGame.PopAt(new Point(x, y), 29, playerIndex);
                }
                else if (selectedGame == GameMode.SimonSays)
                {
                    handAimProgress[cursorIndex].Width = 0;
                    points = 0;
                }
                else
                {
                    handAimProgress[cursorIndex].Width = 0;
                    points = game.PopAt(new Point(x, y), 29);
                }

                if (points > 0)
                {
                    scores[playerIndex] += points;
                    UpdateScoreboard();
                    Pulse(cursor);
                    ShowPopFeedback(new Point(x, y), points, playerIndex);
                }
            }
        }

        private void ShowPopFeedback(Point point, int points, int playerIndex)
        {
            var color = playerIndex == 0 ? BrushFrom("#FF5EAEFF") : BrushFrom("#FFFF7694");
            var ring = new Ellipse
            {
                Width = 70,
                Height = 70,
                Stroke = color,
                StrokeThickness = 8,
                Fill = Brushes.Transparent,
                IsHitTestVisible = false,
                RenderTransformOrigin = new Point(0.5, 0.5)
            };
            var score = new TextBlock
            {
                Text = "+" + points,
                Foreground = Brushes.White,
                FontSize = 27,
                FontWeight = FontWeights.Black,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(ring, point.X - 35);
            Canvas.SetTop(ring, point.Y - 35);
            Canvas.SetLeft(score, point.X + 27);
            Canvas.SetTop(score, point.Y - 42);
            Playfield.Children.Add(ring);
            Playfield.Children.Add(score);
            Panel.SetZIndex(ring, 1800);
            Panel.SetZIndex(score, 1801);

            var scale = new ScaleTransform(0.7, 0.7);
            ring.RenderTransform = scale;
            var grow = new System.Windows.Media.Animation.DoubleAnimation(0.7, 1.65, TimeSpan.FromMilliseconds(320));
            scale.BeginAnimation(ScaleTransform.ScaleXProperty, grow);
            scale.BeginAnimation(ScaleTransform.ScaleYProperty, grow);
            var fade = new System.Windows.Media.Animation.DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(420));
            fade.Completed += (sender, args) =>
            {
                Playfield.Children.Remove(ring);
                Playfield.Children.Remove(score);
            };
            ring.BeginAnimation(OpacityProperty, fade);
            score.BeginAnimation(OpacityProperty, fade);
        }

        private static void Pulse(FrameworkElement cursor)
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
                Brush playerColor = playerTwo ? BrushFrom("#FFFF7694") : BrushFrom("#FF5EAEFF");
                var hand = new Ellipse
                {
                    Width = 58,
                    Height = 58,
                    Fill = playerTwo ? BrushFrom("#CFFF7694") : BrushFrom("#CF5EAEFF"),
                    Stroke = Brushes.White,
                    StrokeThickness = 5
                };
                var center = new Ellipse
                {
                    Width = 12,
                    Height = 12,
                    Fill = Brushes.White,
                    Stroke = playerColor,
                    StrokeThickness = 3,
                    VerticalAlignment = VerticalAlignment.Top,
                    Margin = new Thickness(0, 23, 0, 0)
                };
                var fill = new Border
                {
                    Width = 0,
                    Height = 9,
                    Background = BrushFrom("#FFFFCF5A"),
                    CornerRadius = new CornerRadius(5),
                    HorizontalAlignment = HorizontalAlignment.Left
                };
                var track = new Border
                {
                    Width = 68,
                    Height = 13,
                    Background = BrushFrom("#AA14213D"),
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(7),
                    VerticalAlignment = VerticalAlignment.Bottom,
                    Child = fill
                };
                var cursor = new Grid
                {
                    Width = 78,
                    Height = 78,
                    Visibility = Visibility.Collapsed,
                    IsHitTestVisible = false
                };
                cursor.Children.Add(hand);
                cursor.Children.Add(center);
                cursor.Children.Add(track);
                handCursors[index] = cursor;
                handAimProgress[index] = fill;
                Panel.SetZIndex(cursor, 1500);
                Playfield.Children.Add(cursor);
            }
        }

        private void RenderSkeletons(Canvas canvas)
        {
            canvas.Children.Clear();
            if (!canvas.IsVisible || canvas.ActualWidth < 1 || canvas.ActualHeight < 1) return;

            var visiblePlayers = players.Where(item => item.IsReady).Take(2).ToArray();
            for (int playerIndex = 0; playerIndex < visiblePlayers.Length; playerIndex++)
            {
                var player = visiblePlayers[playerIndex];
                Brush color = playerIndex == 0 ? BrushFrom("#FF5EAEFF") : BrushFrom("#FFFF7694");

                foreach (var bone in Bones)
                {
                    Point start = player.Joint(bone[0]);
                    Point end = player.Joint(bone[1]);
                    if (!IsTracked(start) || !IsTracked(end)) continue;
                    canvas.Children.Add(new Line
                    {
                        X1 = start.X * canvas.ActualWidth,
                        Y1 = start.Y * canvas.ActualHeight,
                        X2 = end.X * canvas.ActualWidth,
                        Y2 = end.Y * canvas.ActualHeight,
                        Stroke = color,
                        StrokeThickness = 7,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap = PenLineCap.Round,
                        IsHitTestVisible = false
                    });
                }

                foreach (var point in player.Joints.Values.Where(IsTracked))
                {
                    var joint = new Ellipse
                    {
                        Width = 13,
                        Height = 13,
                        Fill = Brushes.White,
                        Stroke = color,
                        StrokeThickness = 3,
                        IsHitTestVisible = false
                    };
                    Canvas.SetLeft(joint, point.X * canvas.ActualWidth - joint.Width / 2);
                    Canvas.SetTop(joint, point.Y * canvas.ActualHeight - joint.Height / 2);
                    canvas.Children.Add(joint);
                }
            }
        }

        private void UpdateMenuControl()
        {
            bool menuVisible = HomePanel.Visibility == Visibility.Visible
                               || CalibrationPanel.Visibility == Visibility.Visible
                               || PauseOverlay.Visibility == Visibility.Visible
                               || ResultOverlay.Visibility == Visibility.Visible;
            var player = players.FirstOrDefault(item => item.IsReady);
            if (!menuVisible || KinectMenuCanvas.ActualWidth < 1 || player == null || tracker is MousePlayerTracker || !IsTracked(player.RightHand))
            {
                ResetMenuDwell();
                return;
            }

            Point hand = player.RightHand;
            double x = hand.X * KinectMenuCanvas.ActualWidth;
            double y = hand.Y * KinectMenuCanvas.ActualHeight;
            MenuHandIndicator.Visibility = Visibility.Visible;
            Canvas.SetLeft(MenuHandIndicator, x - MenuHandIndicator.Width / 2);
            Canvas.SetTop(MenuHandIndicator, y - 34);

            Button target = FindButtonAt(new Point(x, y));
            if (target != dwellButton)
            {
                dwellButton = target;
                dwellStartedAt = DateTime.UtcNow;
                dwellTriggered = false;
                MenuDwellProgress.Width = 0;
            }

            if (target == null) return;
            double progress = Math.Min(1, (DateTime.UtcNow - dwellStartedAt).TotalMilliseconds / 1100.0);
            MenuDwellProgress.Width = 72 * progress;
            if (progress >= 1 && !dwellTriggered)
            {
                dwellTriggered = true;
                target.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, target));
            }
        }

        private Button FindButtonAt(Point point)
        {
            foreach (var button in FindVisualChildren<Button>(this).Where(item => item.IsVisible && item.IsEnabled))
            {
                try
                {
                    Point topLeft = button.TransformToAncestor(this).Transform(new Point(0, 0));
                    var bounds = new Rect(topLeft, new Size(button.ActualWidth, button.ActualHeight));
                    if (bounds.Contains(point)) return button;
                }
                catch (InvalidOperationException)
                {
                    // Elementet hann döljas när en annan meny öppnades.
                }
            }
            return null;
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject parent) where T : DependencyObject
        {
            if (parent == null) yield break;
            for (int index = 0; index < VisualTreeHelper.GetChildrenCount(parent); index++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, index);
                var match = child as T;
                if (match != null) yield return match;
                foreach (var descendant in FindVisualChildren<T>(child)) yield return descendant;
            }
        }

        private void ResetMenuDwell()
        {
            MenuHandIndicator.Visibility = Visibility.Collapsed;
            MenuDwellProgress.Width = 0;
            dwellButton = null;
            dwellTriggered = false;
        }

        private static bool IsTracked(Point point) => point.X >= 0 && point.X <= 1 && point.Y >= 0 && point.Y <= 1;

        private void UpdateScoreboard()
        {
            PlayerOneScore.Text = scores[0].ToString();
            PlayerTwoScore.Text = scores[1].ToString();
        }

        private void OnModuleGameEvent(object sender, GameEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Message))
                GameInstructionText.Text = e.Message;

            if (selectedGame == GameMode.SimonSays && e.ScoreDelta > 0)
            {
                int playerIndex = Math.Max(0, Math.Min(1, e.PlayerIndex));
                scores[playerIndex] += e.ScoreDelta;
                UpdateScoreboard();
                ShowPopFeedback(new Point(Playfield.ActualWidth / 2, Playfield.ActualHeight / 2),
                    e.ScoreDelta, playerIndex);
            }
        }

        private void FinishRound()
        {
            isPlaying = false;
            gameTimer.Stop();
            moduleManager.Stop();
            PauseButton.Visibility = Visibility.Collapsed;
            ResultOverlay.Visibility = Visibility.Visible;
            string scoreText = PlayerTwoScoreBox.Visibility == Visibility.Visible
                ? $"Spelare 1: {scores[0]}  •  Spelare 2: {scores[1]}"
                : $"Du fick {scores[0]} poäng";

            if (selectedGame == GameMode.Math)
            {
                bool unlocked = progression.AddMathScore(0, scores[0]);
                if (PlayerTwoScoreBox.Visibility == Visibility.Visible)
                    unlocked = progression.AddMathScore(1, scores[1]) || unlocked;

                scoreText += unlocked
                    ? "\nSpökjakten 3D är nu upplåst!"
                    : "\n" + progression.GetRemainingForSpookyAdventure() +
                      " mattepoäng kvar till Spökjakten 3D.";
                UpdateProgressionUi();
            }

            ResultText.Text = scoreText;
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
            UpdateProgressionUi();
        }

        private void StopRound()
        {
            isPlaying = false;
            isPaused = false;
            gameTimer.Stop();
            game.Reset();
            zombieGame.Reset();
            moduleManager.Stop();
            mathGame.Reset();
            simonGame.Reset();
            PauseOverlay.Visibility = Visibility.Collapsed;
            ResultOverlay.Visibility = Visibility.Collapsed;
            CountdownOverlay.Visibility = Visibility.Collapsed;
            BossBanner.Visibility = Visibility.Collapsed;
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

        private void UpdateProgressionUi()
        {
            if (SpookyAdventureButton == null || ProgressText == null) return;
            bool unlocked = progression.IsGameUnlocked("SpookyAdventure3D");
            SpookyAdventureButton.IsEnabled = unlocked;
            SpookyAdventureButton.Opacity = unlocked ? 1 : 0.58;
            SpookyAdventureButton.Content = unlocked
                ? "Spökjakten 3D"
                : "Spökjakten 3D 🔒";
            ProgressText.Text = unlocked
                ? "Belöning upplåst: Spökjakten 3D är redo."
                : "Samla " + progression.GetRemainingForSpookyAdventure() +
                  " mattepoäng till för att låsa upp Spökjakten 3D.";
        }

        private static string FindSpookyAdventureExecutable()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
                System.IO.Path.Combine(baseDirectory, "KinectKids3D.exe"),
                System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDirectory, "..", "..", "..", "..",
                    "unity", "KinectKids3D", "Build", "KinectKids3D.exe"))
            };
            return candidates.FirstOrDefault(System.IO.File.Exists);
        }

        private enum GameMode
        {
            Balloons,
            ZombieTrain,
            Math,
            SimonSays
        }
    }
}

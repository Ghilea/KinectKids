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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
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
        private SwedishGame swedishGame;
        private ShapeGame shapeGame;
        private PatternGame patternGame;
        private SimonSaysGame simonGame;
        private readonly GameManager moduleManager = new GameManager();
        private readonly ProgressionService progression = new ProgressionService();
        private readonly ActiveHandSelector activeHandSelector = new ActiveHandSelector();
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
        private Button[] carouselButtons;
        private MenuGameDefinition[] menuGames;
        private int carouselIndex;
        private int announcedCarouselIndex = -1;
        private double carouselDragDistance;
        private bool carouselSwipeLatched;
        private DateTime lastCarouselMove;
        private readonly MediaPlayer menuMusic = new MediaPlayer();
        private readonly SpokenInstructionService spokenInstructions = new SpokenInstructionService();
        private bool menuMusicEnabled = true;
        private bool showCamera = true;
        private WriteableBitmap cameraBitmap;

        public MainWindow()
        {
            InitializeComponent();
            game = new GameEngine(Playfield);
            zombieGame = new ZombieRailEngine(Playfield);
            mathGame = new MathGame(Playfield);
            swedishGame = new SwedishGame(Playfield);
            shapeGame = new ShapeGame(Playfield);
            patternGame = new PatternGame(Playfield);
            simonGame = new SimonSaysGame();
            moduleManager.GameEvent += OnModuleGameEvent;
            CreateHandCursors();
            menuGames = new[]
            {
                new MenuGameDefinition(MathStartButton, "Matematikbanan",
                    "Räkna tillsammans och slå på ballongen med rätt svar.",
                    "MATEMATIK & LOGIK", "2 + 3", "#C52A7862", "menu_math"),
                new MenuGameDefinition(ShapeStartButton, "Formverkstan",
                    "Känn igen cirklar, trianglar, kvadrater och hur många hörn de har.",
                    "MATEMATIK & LOGIK", "● ▲ ■", "#C52A7862", "menu_shapes"),
                new MenuGameDefinition(PatternStartButton, "Mönsterjakten",
                    "Fortsätt serier med former, bokstäver, storlekar och tal. Nya mönster låses upp när det blir svårare.",
                    "MATEMATIK & LOGIK", "● ■ ● ?", "#C52A7862", "menu_patterns"),
                new MenuGameDefinition(SwedishStartButton, "Bokstavsjakten",
                    "Hitta begynnelsebokstäver och bokstäver som saknas i svenska ord.",
                    "SVENSKA", "Å Ä Ö", "#C56D294F", "menu_swedish"),
                new MenuGameDefinition(SimonStartButton, "Simon säger",
                    "Se rörelsen, lyssna på instruktionen och härma med hela kroppen.",
                    "RÖRELSE & LEK", "★", "#C5A34B20", "menu_simon"),
                new MenuGameDefinition(BalloonStartButton, "Ballongjakten",
                    "Rör den hand du vill använda och smäll så många färgglada ballonger du kan.",
                    "RÖRELSE & LEK", "● ● ●", "#C5A34B20", "menu_balloons"),
                new MenuGameDefinition(SpookyAdventureButton, "Spökjakten",
                    "Kliv ombord på spökvagnen, ducka, väj och bekämpa slottets monster.",
                    "ÄVENTYR", "☾", "#C51A102F", "menu_spooky"),
                new MenuGameDefinition(GreveGastButton, "Greve Gasts Jakt",
                    "Greve Gast har tagit över en levande teckning. Spring genom pappersvärlden och lura honom.",
                    "ÄVENTYR", "♬", "#C548176A", "menu_greve_gast")
            };
            carouselButtons = menuGames.Select(item => item.Button).ToArray();
            GameCarousel.SizeChanged += (sender, args) => UpdateCarousel(false);
            menuMusic.MediaEnded += (sender, args) =>
            {
                menuMusic.Position = TimeSpan.Zero;
                if (menuMusicEnabled && HomePanel.Visibility == Visibility.Visible) menuMusic.Play();
            };
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
            SwitchTracker(new AutomaticPlayerTracker(this));
            if (Environment.GetCommandLineArgs().Any(argument =>
                    string.Equals(argument, "--fullscreen", StringComparison.OrdinalIgnoreCase)))
                SetFullscreen(true);
            UpdateCarousel(false);
            StartMenuMusic();
        }

        private void SwitchTracker(IPlayerTracker next)
        {
            activeHandSelector.Reset();
            if (tracker != null)
            {
                tracker.PlayersChanged -= OnPlayersChanged;
                tracker.StatusChanged -= OnStatusChanged;
                tracker.ColorFrameReady -= OnColorFrameReady;
                tracker.Dispose();
            }

            tracker = next;
            tracker.PlayersChanged += OnPlayersChanged;
            tracker.StatusChanged += OnStatusChanged;
            tracker.ColorFrameReady += OnColorFrameReady;
            UpdateSensorStatus(tracker.Status, tracker.IsConnected);
            tracker.Start();
        }

        private void OnStatusChanged(object sender, string status)
        {
            Dispatcher.BeginInvoke(new Action(() => UpdateSensorStatus(status, tracker != null && tracker.IsConnected)));
        }

        private void OnColorFrameReady(object sender, ColorFrameEventArgs frame)
        {
            if (frame?.Pixels == null) return;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (cameraBitmap == null || cameraBitmap.PixelWidth != frame.Width || cameraBitmap.PixelHeight != frame.Height)
                {
                    cameraBitmap = new WriteableBitmap(frame.Width, frame.Height, 96, 96, PixelFormats.Bgr32, null);
                    PlayerCameraImage.Source = cameraBitmap;
                    CalibrationCameraImage.Source = cameraBitmap;
                }
                cameraBitmap.WritePixels(new Int32Rect(0, 0, frame.Width, frame.Height),
                    frame.Pixels, frame.Stride, 0);
                UpdatePlayerViewMode();
            }), DispatcherPriority.Render);
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
            SensorDot.Fill = connected ? BrushFrom("#50E3A4") : BrushFrom("#FFBE47");
            SensorBadge.Background = connected ? BrushFrom("#3324D49A") : BrushFrom("#335EAEFF");
            UpdatePlayerViewMode();
        }

        private void UpdatePlayerViewMode()
        {
            bool cameraVisible = showCamera && cameraBitmap != null && tracker != null && tracker.IsConnected;
            PlayerCameraImage.Visibility = cameraVisible ? Visibility.Visible : Visibility.Collapsed;
            CalibrationCameraImage.Visibility = cameraVisible ? Visibility.Visible : Visibility.Collapsed;
            GameSkeletonCanvas.Visibility = cameraVisible ? Visibility.Collapsed : Visibility.Visible;
            CalibrationSkeletonCanvas.Visibility = cameraVisible ? Visibility.Collapsed : Visibility.Visible;
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmCarouselSelection(sender)) return;
            selectedGame = GameMode.Balloons;
            await BeginRoundAsync();
        }

        private async void MathStartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmCarouselSelection(sender)) return;
            selectedGame = GameMode.Math;
            await BeginRoundAsync();
        }

        private async void SwedishStartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmCarouselSelection(sender)) return;
            selectedGame = GameMode.Swedish;
            await BeginRoundAsync();
        }

        private async void ShapeStartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmCarouselSelection(sender)) return;
            selectedGame = GameMode.Shapes;
            await BeginRoundAsync();
        }

        private async void PatternStartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmCarouselSelection(sender)) return;
            selectedGame = GameMode.Patterns;
            await BeginRoundAsync();
        }

        private async void SimonStartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmCarouselSelection(sender)) return;
            selectedGame = GameMode.SimonSays;
            await BeginRoundAsync();
        }

        private async void ZombieStartButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmCarouselSelection(sender)) return;
            selectedGame = GameMode.ZombieTrain;
            await BeginRoundAsync();
        }

        private void SpookyAdventureButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmCarouselSelection(sender)) return;
            string executable = FindSpookyAdventureExecutable();
            if (executable == null)
            {
                MessageBox.Show(
                    "Spökjakten finns i menyn men behöver byggas i Unity först.\n\n" +
                    "Öppna unity\\KinectKids3D i Unity 6 och bygg Windows-versionen till " +
                    "unity\\KinectKids3D\\Build.",
                    "Spökjakten",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            tracker?.Stop();
            Process.Start(new ProcessStartInfo(executable)
            {
                WorkingDirectory = System.IO.Path.GetDirectoryName(executable),
                Arguments = "--launcher \"" + Process.GetCurrentProcess().MainModule.FileName + "\"" +
                            (isFullscreen ? " --launcher-fullscreen 1" : string.Empty)
            });
            Close();
        }

        private void GreveGastButton_Click(object sender, RoutedEventArgs e)
        {
            if (!ConfirmCarouselSelection(sender)) return;
            string executable = FindGreveGastExecutable();
            if (executable == null)
            {
                MessageBox.Show(
                    "Greve Gasts Jakt behöver byggas i Unity först. Kör det gemensamma byggkommandot.",
                    "Greve Gasts Jakt", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            tracker?.Stop();
            Process.Start(new ProcessStartInfo(executable)
            {
                WorkingDirectory = System.IO.Path.GetDirectoryName(executable),
                Arguments = "--greve-gast --launcher \"" + Process.GetCurrentProcess().MainModule.FileName + "\"" +
                            (isFullscreen ? " --launcher-fullscreen 1" : string.Empty)
            });
            Close();
        }

        private void ShowCalibration()
        {
            menuMusic.Pause();
            StopRound();
            HomePanel.Visibility = Visibility.Collapsed;
            GamePanel.Visibility = Visibility.Collapsed;
            CalibrationPanel.Visibility = Visibility.Visible;
            switch (selectedGame)
            {
                case GameMode.Math:
                    CalibrationTitle.Text = "Gör er redo för Matematikbanan";
                    break;
                case GameMode.Swedish:
                    CalibrationTitle.Text = "Gör er redo för Bokstavsjakten";
                    break;
                case GameMode.Shapes:
                    CalibrationTitle.Text = "Gör er redo för Formverkstan";
                    break;
                case GameMode.Patterns:
                    CalibrationTitle.Text = "Gör er redo för Mönsterjakten";
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
            menuMusic.Pause();
            spokenInstructions.Stop();
            CalibrationPanel.Visibility = Visibility.Collapsed;
            HomePanel.Visibility = Visibility.Collapsed;
            GamePanel.Visibility = Visibility.Visible;
            ResultOverlay.Visibility = Visibility.Collapsed;
            SessionMenu.ShowButton();
            GameInstruction.Visibility = Visibility.Visible;
            game.Reset();
            zombieGame.Reset();
            mathGame.Reset();
            swedishGame.Reset();
            shapeGame.Reset();
            patternGame.Reset();
            simonGame.Reset();
            IGame selectedModule = null;
            if (selectedGame == GameMode.Math) selectedModule = mathGame;
            if (selectedGame == GameMode.Swedish) selectedModule = swedishGame;
            if (selectedGame == GameMode.Shapes) selectedModule = shapeGame;
            if (selectedGame == GameMode.Patterns) selectedModule = patternGame;
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
            else if (selectedGame == GameMode.Balloons)
                spokenInstructions.Speak("balloon_instruction");
            roundEndsAt = DateTime.UtcNow.AddSeconds(60);
            instructionHidesAt = DateTime.UtcNow.AddSeconds(selectedGame == GameMode.ZombieTrain ? 11 : 8);
            frameClock.Restart();
            gameTimer.Start();
        }

        private void ConfigureGameMode()
        {
            bool zombies = selectedGame == GameMode.ZombieTrain;
            BalloonBackdrop.Visibility = Visibility.Visible;
            string backgroundAsset = selectedGame == GameMode.Math ? "background-math.png"
                : selectedGame == GameMode.Swedish ? "background-swedish.png"
                : selectedGame == GameMode.Shapes ? "background-shapes.png"
                : selectedGame == GameMode.Patterns ? "background-patterns.png"
                : selectedGame == GameMode.SimonSays ? "background-simon.png"
                : selectedGame == GameMode.Balloons ? "background-balloons.png"
                : "learning-game-background.png";
            GameBackgroundImage.Source = new BitmapImage(new Uri(
                "pack://application:,,,/Assets/" + backgroundAsset, UriKind.Absolute));
            BalloonBackdrop.Background = selectedGame == GameMode.Math
                ? BrushFrom("#173D63")
                : selectedGame == GameMode.Swedish ? BrushFrom("#512847")
                : selectedGame == GameMode.Shapes ? BrushFrom("#594018")
                : selectedGame == GameMode.Patterns ? BrushFrom("#193F55")
                : selectedGame == GameMode.SimonSays ? BrushFrom("#282B57") : BrushFrom("#102B46");
            GameThemeTint.Background = selectedGame == GameMode.Math
                ? BrushFrom("#38285F9B")
                : selectedGame == GameMode.Swedish ? BrushFrom("#405F2859")
                : selectedGame == GameMode.Shapes ? BrushFrom("#3A8A6218")
                : selectedGame == GameMode.Patterns ? BrushFrom("#381D6D78")
                : selectedGame == GameMode.SimonSays ? BrushFrom("#42402B73")
                : selectedGame == GameMode.Balloons ? BrushFrom("#26357CA0")
                : BrushFrom("#6010213D");
            Playfield.Background = Brushes.Transparent;
            GameSkeletonCanvas.Opacity = selectedGame == GameMode.SimonSays ? 0.9 : zombies ? 0.46 : 0.72;
            BossBanner.Visibility = Visibility.Collapsed;
            TimeText.Text = "60";
            GameInstruction.Visibility = selectedGame == GameMode.Math || selectedGame == GameMode.Patterns
                ? Visibility.Visible
                : Visibility.Collapsed;
            switch (selectedGame)
            {
                case GameMode.Math:
                    GameInstructionText.Text = "Träffa ballongen med rätt svar!";
                    break;
                case GameMode.Swedish:
                    GameInstructionText.Text = "Slå på kortet med rätt bokstav!";
                    break;
                case GameMode.Shapes:
                    GameInstructionText.Text = "Slå på formen som efterfrågas!";
                    break;
                case GameMode.Patterns:
                    GameInstructionText.Text = "Titta på mönstret och välj vad som kommer sedan!";
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
                BossBanner.Visibility = zombieGame.BossIsActive ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (IsModuleGame(selectedGame))
            {
                moduleManager.Update(elapsed);
                UpdateVisualQuestion();
            }
            else
            {
                game.Update(elapsed);
            }
            if (!IsModuleGame(selectedGame)
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
                ActiveHandSelection selection = activeHandSelector.Select(activePlayers[playerIndex], DateTime.UtcNow);
                int cursorIndex = playerIndex * 2 + (selection.IsRightHand ? 1 : 0);
                ShowHand(cursorIndex, selection.Position, playerIndex);
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
                else if (selectedGame == GameMode.Swedish)
                {
                    handAimProgress[cursorIndex].Width = 0;
                    points = swedishGame.PopAt(new Point(x, y), 29, playerIndex);
                }
                else if (selectedGame == GameMode.Shapes)
                {
                    handAimProgress[cursorIndex].Width = 0;
                    points = shapeGame.PopAt(new Point(x, y), 29, playerIndex);
                }
                else if (selectedGame == GameMode.Patterns)
                {
                    handAimProgress[cursorIndex].Width = 0;
                    points = patternGame.PopAt(new Point(x, y), 29, playerIndex);
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

                if (points != 0)
                {
                    scores[playerIndex] = Math.Max(0, scores[playerIndex] + points);
                    UpdateScoreboard();
                    Pulse(cursor);
                    ShowPopFeedback(new Point(x, y), points, playerIndex);
                }
            }
        }

        private void ShowPopFeedback(Point point, int points, int playerIndex)
        {
            var color = points < 0
                ? BrushFrom("#FFFF4D65")
                : playerIndex == 0 ? BrushFrom("#FF5EAEFF") : BrushFrom("#FFFF7694");
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
                Text = points > 0 ? "+" + points : points.ToString(),
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
                Brush outfit = playerIndex == 0 ? BrushFrom("#DD246BB2") : BrushFrom("#DDBB3E66");

                foreach (var bone in Bones)
                {
                    Point start = player.Joint(bone[0]);
                    Point end = player.Joint(bone[1]);
                    if (!IsTracked(start) || !IsTracked(end)) continue;
                    AddAvatarLimb(canvas, start, end, outfit);
                }

                Point shoulderLeft = player.Joint(BodyJoint.ShoulderLeft);
                Point shoulderRight = player.Joint(BodyJoint.ShoulderRight);
                Point hipLeft = player.Joint(BodyJoint.HipLeft);
                Point hipRight = player.Joint(BodyJoint.HipRight);
                if (IsTracked(shoulderLeft) && IsTracked(shoulderRight) && IsTracked(hipLeft) && IsTracked(hipRight))
                {
                    var torso = new Polygon
                    {
                        Points = new PointCollection
                        {
                            ScalePoint(shoulderLeft, canvas), ScalePoint(shoulderRight, canvas),
                            ScalePoint(hipRight, canvas), ScalePoint(hipLeft, canvas)
                        },
                        Fill = outfit,
                        Stroke = color,
                        StrokeThickness = 4,
                        StrokeLineJoin = PenLineJoin.Round,
                        IsHitTestVisible = false
                    };
                    canvas.Children.Add(torso);
                }

                AddAvatarHead(canvas, player.Joint(BodyJoint.Head), color, playerIndex);
                AddAvatarHand(canvas, player.Joint(BodyJoint.HandLeft), color);
                AddAvatarHand(canvas, player.Joint(BodyJoint.HandRight), color);
            }
        }

        private static void AddAvatarLimb(Canvas canvas, Point start, Point end, Brush color)
        {
            Point a = ScalePoint(start, canvas);
            Point b = ScalePoint(end, canvas);
            canvas.Children.Add(new Line
            {
                X1 = a.X, Y1 = a.Y, X2 = b.X, Y2 = b.Y,
                Stroke = color,
                StrokeThickness = 17,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                IsHitTestVisible = false
            });
        }

        private static void AddAvatarHead(Canvas canvas, Point point, Brush color, int playerIndex)
        {
            if (!IsTracked(point)) return;
            Point center = ScalePoint(point, canvas);
            var head = new Ellipse
            {
                Width = 54, Height = 54,
                Fill = playerIndex == 0 ? BrushFrom("#FFFFD2AD") : BrushFrom("#FFB97A56"),
                Stroke = color, StrokeThickness = 5,
                IsHitTestVisible = false
            };
            Canvas.SetLeft(head, center.X - 27);
            Canvas.SetTop(head, center.Y - 27);
            canvas.Children.Add(head);

            AddFaceDot(canvas, center.X - 9, center.Y - 6);
            AddFaceDot(canvas, center.X + 9, center.Y - 6);
            var smileGeometry = new PathGeometry();
            var smileFigure = new PathFigure { StartPoint = new Point(center.X - 10, center.Y + 5) };
            smileFigure.Segments.Add(new QuadraticBezierSegment(
                new Point(center.X, center.Y + 15), new Point(center.X + 10, center.Y + 5), true));
            smileGeometry.Figures.Add(smileFigure);
            canvas.Children.Add(new System.Windows.Shapes.Path
            {
                Data = smileGeometry,
                Stroke = BrushFrom("#FF27364A"),
                StrokeThickness = 3,
                StrokeStartLineCap = PenLineCap.Round,
                StrokeEndLineCap = PenLineCap.Round,
                IsHitTestVisible = false
            });
        }

        private static void AddFaceDot(Canvas canvas, double x, double y)
        {
            var eye = new Ellipse
            {
                Width = 5, Height = 7, Fill = BrushFrom("#FF27364A"), IsHitTestVisible = false
            };
            Canvas.SetLeft(eye, x - eye.Width / 2);
            Canvas.SetTop(eye, y - eye.Height / 2);
            canvas.Children.Add(eye);
        }

        private static void AddAvatarHand(Canvas canvas, Point point, Brush color)
        {
            if (!IsTracked(point)) return;
            Point center = ScalePoint(point, canvas);
            var hand = new Ellipse
            {
                Width = 28, Height = 28, Fill = BrushFrom("#FFFFD2AD"),
                Stroke = color, StrokeThickness = 4, IsHitTestVisible = false
            };
            Canvas.SetLeft(hand, center.X - 14);
            Canvas.SetTop(hand, center.Y - 14);
            canvas.Children.Add(hand);
        }

        private static Point ScalePoint(Point point, Canvas canvas) =>
            new Point(point.X * canvas.ActualWidth, point.Y * canvas.ActualHeight);

        private bool ConfirmCarouselSelection(object sender)
        {
            if (HomePanel.Visibility != Visibility.Visible || !(sender is Button button)) return true;
            int index = Array.IndexOf(carouselButtons, button);
            if (index < 0 || index == carouselIndex) return true;
            carouselIndex = index;
            UpdateCarousel(true);
            ResetMenuDwell(false);
            return false;
        }

        private void CarouselUpButton_Click(object sender, RoutedEventArgs e) => MoveCarousel(-1);
        private void CarouselDownButton_Click(object sender, RoutedEventArgs e) => MoveCarousel(1);

        private void HomePanel_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            MoveCarousel(e.Delta > 0 ? -1 : 1);
            e.Handled = true;
        }

        private void MoveCarousel(int direction)
        {
            if (carouselButtons == null || carouselButtons.Length == 0) return;
            carouselIndex = (carouselIndex + direction + carouselButtons.Length) % carouselButtons.Length;
            UpdateCarousel(true);
            ResetMenuDwell(false);
        }

        private void UpdateCarousel(bool animate)
        {
            if (carouselButtons == null) return;
            double centerY = Math.Max(210, GameCarousel.ActualHeight / 2) - 34;
            int count = carouselButtons.Length;

            for (int index = 0; index < count; index++)
            {
                int offset = index - carouselIndex;
                if (offset > count / 2) offset -= count;
                if (offset < -count / 2) offset += count;
                Button button = carouselButtons[index];
                button.Background = BrushFrom(menuGames[index].Color);
                int distance = Math.Abs(offset);
                if (distance > 3)
                {
                    button.Visibility = Visibility.Collapsed;
                    continue;
                }

                button.Visibility = Visibility.Visible;
                double targetTop = centerY + offset * 88;
                double targetLeft = distance == 0 ? 108 : distance == 1 ? 66 : distance == 2 ? 26 : 2;
                double targetScale = distance == 0 ? 1.0 : distance == 1 ? 0.88 : distance == 2 ? 0.75 : 0.64;
                double targetOpacity = distance == 0 ? 1.0 : distance == 1 ? 0.72 : distance == 2 ? 0.42 : 0.22;
                Panel.SetZIndex(button, 10 - distance);
                button.Effect = distance == 0
                    ? new System.Windows.Media.Effects.DropShadowEffect
                    {
                        BlurRadius = 28,
                        ShadowDepth = 0,
                        Opacity = 0.72,
                        Color = Colors.White
                    }
                    : null;

                double oldTop = Canvas.GetTop(button);
                double oldLeft = Canvas.GetLeft(button);
                if (double.IsNaN(oldTop)) oldTop = targetTop;
                if (double.IsNaN(oldLeft)) oldLeft = targetLeft;
                Canvas.SetTop(button, targetTop);
                Canvas.SetLeft(button, targetLeft);
                button.RenderTransformOrigin = new Point(0.5, 0.5);
                var scale = button.RenderTransform as ScaleTransform;
                if (scale == null)
                {
                    scale = new ScaleTransform(targetScale, targetScale);
                    button.RenderTransform = scale;
                }

                if (animate)
                {
                    var ease = new CubicEase { EasingMode = EasingMode.EaseOut };
                    button.BeginAnimation(Canvas.TopProperty,
                        new DoubleAnimation(oldTop, targetTop, TimeSpan.FromMilliseconds(260)) { EasingFunction = ease });
                    button.BeginAnimation(Canvas.LeftProperty,
                        new DoubleAnimation(oldLeft, targetLeft, TimeSpan.FromMilliseconds(260)) { EasingFunction = ease });
                    scale.BeginAnimation(ScaleTransform.ScaleXProperty,
                        new DoubleAnimation(scale.ScaleX, targetScale, TimeSpan.FromMilliseconds(240)) { EasingFunction = ease });
                    scale.BeginAnimation(ScaleTransform.ScaleYProperty,
                        new DoubleAnimation(scale.ScaleY, targetScale, TimeSpan.FromMilliseconds(240)) { EasingFunction = ease });
                    button.BeginAnimation(OpacityProperty,
                        new DoubleAnimation(button.Opacity, targetOpacity, TimeSpan.FromMilliseconds(220)));
                }
                else
                {
                    scale.ScaleX = scale.ScaleY = targetScale;
                    button.Opacity = targetOpacity;
                }
            }

            MenuGameDefinition selected = menuGames[carouselIndex];
            PreviewTitle.Text = selected.Title;
            PreviewDescription.Text = selected.Description;
            PreviewCategory.Text = selected.Category + " • 1–2 SPELARE • NIVÅN ÖKAR";
            PreviewGlyph.Text = selected.Glyph;
            PreviewTint.Background = BrushFrom(selected.Color);
            if (animate)
                PreviewTitle.BeginAnimation(OpacityProperty,
                    new DoubleAnimation(0.25, 1, TimeSpan.FromMilliseconds(260)));
            AnnounceCarouselSelection();
        }

        private void AnnounceCarouselSelection()
        {
            if (!IsLoaded || HomePanel.Visibility != Visibility.Visible ||
                carouselIndex == announcedCarouselIndex) return;

            announcedCarouselIndex = carouselIndex;
            spokenInstructions.Speak(menuGames[carouselIndex].VoiceKey);
        }

        private Point SelectMenuHand(TrackedPlayer player, out double verticalMovement)
        {
            ActiveHandSelection selection = activeHandSelector.Select(player, DateTime.UtcNow);
            verticalMovement = selection.VerticalDelta;
            return selection.Position;
        }

        private void UpdateMenuControl()
        {
            bool menuVisible = HomePanel.Visibility == Visibility.Visible
                               || CalibrationPanel.Visibility == Visibility.Visible
                               || SessionMenu.IsOpen
                               || ResultOverlay.Visibility == Visibility.Visible;
            var player = players.FirstOrDefault(item => item.IsReady);
            if (!menuVisible || KinectMenuCanvas.ActualWidth < 1 || player == null)
            {
                ResetMenuDwell();
                return;
            }

            double verticalMovement = 0;
            Point hand = SelectMenuHand(player, out verticalMovement);
            if (!IsTracked(hand))
            {
                ResetMenuDwell();
                return;
            }
            double x = hand.X * KinectMenuCanvas.ActualWidth;
            double y = hand.Y * KinectMenuCanvas.ActualHeight;
            MenuHandIndicator.Visibility = Visibility.Visible;
            Canvas.SetLeft(MenuHandIndicator, x - MenuHandIndicator.Width / 2);
            Canvas.SetTop(MenuHandIndicator, y - 34);

            if (HomePanel.Visibility == Visibility.Visible && hand.X < 0.58)
            {
                double stableMovement = Math.Abs(verticalMovement) < 0.003
                    ? 0
                    : Math.Max(-0.035, Math.Min(0.035, verticalMovement));
                if (carouselSwipeLatched)
                {
                    if (stableMovement == 0)
                    {
                        carouselSwipeLatched = false;
                        carouselDragDistance = 0;
                    }
                }
                else
                {
                    carouselDragDistance += stableMovement;
                }

                if (!carouselSwipeLatched && Math.Abs(carouselDragDistance) >= 0.075 &&
                    (DateTime.UtcNow - lastCarouselMove).TotalMilliseconds >= 230)
                {
                    int direction = carouselDragDistance < 0 ? 1 : -1;
                    carouselDragDistance = 0;
                    carouselSwipeLatched = true;
                    lastCarouselMove = DateTime.UtcNow;
                    MoveCarousel(direction);
                    return;
                }
            }
            else
            {
                carouselDragDistance *= 0.35;
                carouselSwipeLatched = false;
            }

            Button target = FindButtonAt(new Point(x, y));
            if (HomePanel.Visibility == Visibility.Visible && target != null)
            {
                int carouselButtonIndex = Array.IndexOf(carouselButtons, target);
                if (carouselButtonIndex >= 0 && carouselButtonIndex != carouselIndex)
                    target = null;
            }
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

        private void ResetMenuDwell(bool hideIndicator = true)
        {
            if (hideIndicator) MenuHandIndicator.Visibility = Visibility.Collapsed;
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
            UpdateVisualQuestion();
            if (!string.IsNullOrWhiteSpace(e.VoiceKey))
            {
                if (e.ScoreDelta != 0) spokenInstructions.SpeakNow(e.VoiceKey);
                else spokenInstructions.Speak(e.VoiceKey);
            }

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
            spokenInstructions.Stop();
            SessionMenu.HideAll();
            ResultOverlay.Visibility = Visibility.Visible;
            string scoreText = PlayerTwoScoreBox.Visibility == Visibility.Visible
                ? $"Spelare 1: {scores[0]}  •  Spelare 2: {scores[1]}"
                : $"Du fick {scores[0]} poäng";

            if (selectedGame == GameMode.Math)
            {
                progression.AddMathScore(0, scores[0]);
                if (PlayerTwoScoreBox.Visibility == Visibility.Visible)
                    progression.AddMathScore(1, scores[1]);

                scoreText += "\nTotalt har ni samlat " + progression.GetCombinedMathScore() + " mattepoäng.";
                UpdateProgressionUi();
            }

            ResultText.Text = scoreText;
        }

        private void SessionMenu_Opened(object sender, EventArgs e)
        {
            if (!isPlaying || isPaused) return;
            isPaused = true;
            pausedRemaining = roundEndsAt - DateTime.UtcNow;
        }

        private void SessionMenu_ContinueRequested(object sender, EventArgs e)
        {
            if (!isPlaying) return;
            isPaused = false;
            roundEndsAt = DateTime.UtcNow + pausedRemaining;
            frameClock.Restart();
            SessionMenu.Close();
        }

        private async void SessionMenu_RestartRequested(object sender, EventArgs e)
        {
            SessionMenu.Close();
            await BeginRoundAsync();
        }

        private void SessionMenu_HomeRequested(object sender, EventArgs e)
        {
            HomeButton_Click(sender, new RoutedEventArgs());
        }

        private void HomeButton_Click(object sender, RoutedEventArgs e)
        {
            StopRound();
            CalibrationPanel.Visibility = Visibility.Collapsed;
            GamePanel.Visibility = Visibility.Collapsed;
            HomePanel.Visibility = Visibility.Visible;
            if (menuMusicEnabled) menuMusic.Play();
            announcedCarouselIndex = -1;
            UpdateCarousel(false);
            UpdateProgressionUi();
        }

        private void StartMenuMusic()
        {
            try
            {
                string musicPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "Assets", "menu-hella-bumps.mp3");
                if (!System.IO.File.Exists(musicPath))
                {
                    MenuMusicButton.Content = "♫ Musik saknas";
                    MenuMusicButton.IsEnabled = false;
                    return;
                }

                menuMusic.Volume = 0.22;
                menuMusic.Open(new Uri(musicPath, UriKind.Absolute));
                menuMusic.Play();
            }
            catch (InvalidOperationException)
            {
                MenuMusicButton.Content = "♫ Musik saknas";
                MenuMusicButton.IsEnabled = false;
            }
        }

        private void UpdateVisualQuestion()
        {
            if (selectedGame == GameMode.Math)
                GameInstructionText.Text = mathGame.VisualPrompt;
            else if (selectedGame == GameMode.Patterns)
                GameInstructionText.Text = patternGame.VisualPrompt;
        }

        private void MenuMusicButton_Click(object sender, RoutedEventArgs e)
        {
            menuMusicEnabled = !menuMusicEnabled;
            MenuMusicButton.Content = menuMusicEnabled ? "♫ Musik: på" : "♫ Musik: av";
            if (menuMusicEnabled && HomePanel.Visibility == Visibility.Visible) menuMusic.Play();
            else menuMusic.Pause();
        }

        private void CameraModeButton_Click(object sender, RoutedEventArgs e)
        {
            showCamera = !showCamera;
            CameraModeButton.Content = showCamera ? "Bild: kamera" : "Bild: figur";
            UpdatePlayerViewMode();
        }

        private void StopRound()
        {
            spokenInstructions.Stop();
            isPlaying = false;
            isPaused = false;
            gameTimer.Stop();
            game.Reset();
            zombieGame.Reset();
            moduleManager.Stop();
            mathGame.Reset();
            swedishGame.Reset();
            shapeGame.Reset();
            patternGame.Reset();
            simonGame.Reset();
            SessionMenu.HideAll();
            ResultOverlay.Visibility = Visibility.Collapsed;
            CountdownOverlay.Visibility = Visibility.Collapsed;
            BossBanner.Visibility = Visibility.Collapsed;
        }

        private void FullscreenButton_Click(object sender, RoutedEventArgs e) => ToggleFullscreen();

        private void ToggleFullscreen()
        {
            SetFullscreen(!isFullscreen);
        }

        private void SetFullscreen(bool enabled)
        {
            isFullscreen = enabled;
            WindowStyle = isFullscreen ? WindowStyle.None : WindowStyle.SingleBorderWindow;
            WindowState = isFullscreen ? WindowState.Maximized : WindowState.Normal;
            FullscreenButton.Content = isFullscreen ? "Fönster" : "Helskärm";
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11) ToggleFullscreen();
            if (HomePanel.Visibility == Visibility.Visible && e.Key == Key.Up)
            {
                MoveCarousel(-1);
                e.Handled = true;
            }
            if (HomePanel.Visibility == Visibility.Visible && e.Key == Key.Down)
            {
                MoveCarousel(1);
                e.Handled = true;
            }
            if (e.Key == Key.Escape)
            {
                if (isFullscreen) ToggleFullscreen();
                else if (GamePanel.Visibility == Visibility.Visible && isPlaying)
                {
                    if (SessionMenu.IsOpen) SessionMenu_ContinueRequested(sender, EventArgs.Empty);
                    else
                    {
                        SessionMenu.Open();
                        SessionMenu_Opened(sender, EventArgs.Empty);
                    }
                }
                else if (HomePanel.Visibility != Visibility.Visible) HomeButton_Click(sender, e);
            }
            if (e.Key == Key.Space && isPlaying)
            {
                if (SessionMenu.IsOpen) SessionMenu_ContinueRequested(sender, EventArgs.Empty);
                else
                {
                    SessionMenu.Open();
                    SessionMenu_Opened(sender, EventArgs.Empty);
                }
            }
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            gameTimer.Stop();
            menuMusic.Close();
            spokenInstructions.Dispose();
            tracker?.Dispose();
        }

        private static Brush BrushFrom(string color) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));

        private void UpdateProgressionUi()
        {
            if (SpookyAdventureButton == null || ProgressText == null) return;
            SpookyAdventureButton.IsEnabled = true;
            SpookyAdventureButton.Opacity = 1;
            SpookyAdventureButton.Content = "Spökjakten";
            ProgressText.Text = "Alla spel är öppna • Samlade mattepoäng: " +
                                progression.GetCombinedMathScore();
        }

        private static bool IsModuleGame(GameMode mode)
        {
            return mode == GameMode.Math
                   || mode == GameMode.Swedish
                   || mode == GameMode.Shapes
                   || mode == GameMode.Patterns
                   || mode == GameMode.SimonSays;
        }

        private static string FindSpookyAdventureExecutable()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
                System.IO.Path.Combine(baseDirectory, "KinectKids3D.exe"),
                System.IO.Path.Combine(baseDirectory, "Spokjakten3D", "Spokjakten3D.exe"),
                System.IO.Path.Combine(baseDirectory, "Spokjakten3D.exe"),
                System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDirectory, "..", "..", "..", "..",
                    "unity", "KinectKids3D", "Build", "Spokjakten3D", "Spokjakten3D.exe")),
                System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDirectory, "..", "..", "..", "..",
                    "unity", "KinectKids3D", "Build", "KinectKids3D.exe"))
            };
            return candidates.FirstOrDefault(System.IO.File.Exists);
        }

        private static string FindGreveGastExecutable()
        {
            string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string[] candidates =
            {
                System.IO.Path.Combine(baseDirectory, "GreveGastJakt", "GreveGastJakt.exe"),
                System.IO.Path.Combine(baseDirectory, "GreveGastJakt.exe"),
                System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDirectory, "..", "..", "..", "..",
                    "unity", "KinectKids3D", "Build", "GreveGastJakt", "GreveGastJakt.exe"))
            };
            return candidates.FirstOrDefault(System.IO.File.Exists);
        }

        private enum GameMode
        {
            Balloons,
            ZombieTrain,
            Math,
            Swedish,
            Shapes,
            Patterns,
            SimonSays
        }

        private sealed class MenuGameDefinition
        {
            public MenuGameDefinition(Button button, string title, string description,
                string category, string glyph, string color, string voiceKey)
            {
                Button = button;
                Title = title;
                Description = description;
                Category = category;
                Glyph = glyph;
                Color = color;
                VoiceKey = voiceKey;
            }

            public Button Button { get; }
            public string Title { get; }
            public string Description { get; }
            public string Category { get; }
            public string Glyph { get; }
            public string Color { get; }
            public string VoiceKey { get; }
        }
    }
}

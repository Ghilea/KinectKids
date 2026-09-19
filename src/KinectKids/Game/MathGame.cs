using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using KinectKids.Models;

namespace KinectKids.Game
{
    /// <summary>
    /// Pedagogisk Kinect-modul där barnet träffar ballongen med rätt svar.
    /// Frågan och alternativen skapas lokalt och inga sensordata sparas.
    /// </summary>
    public sealed class MathGame : IGame
    {
        private static readonly Brush[] BalloonColors =
        {
            BrushFrom(255, 91, 119),
            BrushFrom(255, 190, 71),
            BrushFrom(87, 213, 167),
            BrushFrom(94, 174, 255)
        };

        private readonly Canvas canvas;
        private readonly Random random = new Random();
        private readonly List<MathBalloon> balloons = new List<MathBalloon>();
        private readonly Queue<MathQuestion> missedQuestions = new Queue<MathQuestion>();
        private readonly Dictionary<int, Point> lockedHands = new Dictionary<int, Point>();
        private MathQuestion question;
        private double nextQuestionDelay;
        private int answeredQuestions;
        private int freshQuestionsBeforeRetry;
        private DateTime inputArmedAt;

        public MathGame(Canvas canvas)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        }

        public event EventHandler<GameEventArgs> GameEvent;

        public string Id => "MathGame";
        public string DisplayName => "Mattematiken i banan";
        public string Instruction => question == null
            ? "Gör er redo…"
            : "Träffa rätt svar: " + question.Expression + " = ?";
        public string VisualPrompt => question == null ? string.Empty : question.Expression + " = ?";
        public int CurrentScore { get; private set; }
        public bool IsActive { get; private set; }

        public void Start()
        {
            IsActive = true;
            if (question == null) CreateQuestion();
        }

        public void Stop()
        {
            IsActive = false;
        }

        public void Reset()
        {
            IsActive = false;
            CurrentScore = 0;
            answeredQuestions = 0;
            nextQuestionDelay = 0;
            freshQuestionsBeforeRetry = 0;
            question = null;
            missedQuestions.Clear();
            lockedHands.Clear();
            ClearBalloons();
        }

        public void Update(double elapsedSeconds)
        {
            if (!IsActive) return;

            if (question == null)
            {
                nextQuestionDelay -= elapsedSeconds;
                if (nextQuestionDelay <= 0) CreateQuestion();
                return;
            }

            double time = DateTime.UtcNow.TimeOfDay.TotalSeconds;
            for (int index = 0; index < balloons.Count; index++)
            {
                MathBalloon balloon = balloons[index];
                balloon.Y += balloon.Speed * elapsedSeconds;
                balloon.X = balloon.BaseX + Math.Sin(time * balloon.SwayRate + balloon.SwayPhase) * balloon.SwayAmplitude;
                Position(balloon);
            }

            MathBalloon correct = balloons.FirstOrDefault(item => item.Answer == question.CorrectAnswer);
            if (correct != null && correct.Y - correct.Radius > canvas.ActualHeight)
                MissQuestion();
        }

        public int PopAt(Point point, double hitRadius, int playerIndex)
        {
            if (!IsActive) return 0;
            Point lockedAt;
            if (lockedHands.TryGetValue(playerIndex, out lockedAt))
            {
                if (Distance(point.X, point.Y, lockedAt.X, lockedAt.Y) < 55) return 0;
                lockedHands.Remove(playerIndex);
            }
            if (question == null) return 0;
            if (DateTime.UtcNow < inputArmedAt) return 0;

            MathBalloon hit = balloons
                .OrderBy(item => Distance(point.X, point.Y, item.X, item.Y))
                .FirstOrDefault(item => Distance(point.X, point.Y, item.X, item.Y) <= item.Radius + hitRadius);
            if (hit == null) return 0;
            // Valet blir aktivt så snart hela ballongen syns, inte först halvvägs ned.
            if (hit.Y - hit.Radius < 4) return 0;
            lockedHands[playerIndex] = point;

            if (hit.Answer == question.CorrectAnswer)
            {
                int points = 10 + Math.Min(10, answeredQuestions);
                CurrentScore += points;
                answeredQuestions++;
                Raise("Rätt! " + question.Expression + " = " + question.CorrectAnswer, points, playerIndex, "correct");
                question = null;
                nextQuestionDelay = 2.5;
                ClearBalloons();
                return points;
            }

            CurrentScore = Math.Max(0, CurrentScore - 3);
            RemoveBalloon(hit);
            Raise("Fel svar. Tre poäng bort.", -3, playerIndex, "retry");
            return -3;
        }

        public void Render(DrawingContext context)
        {
            // WPF-versionen använder en Canvas så att samma element även kan
            // användas av kollisionsdetekteringen och musläget.
        }

        public void OnKinectGesture(GestureType gesture)
        {
            // Matematikmodulen styrs med handpositioner, inte diskreta gester.
        }

        private void CreateQuestion()
        {
            if (canvas.ActualWidth < 200 || canvas.ActualHeight < 200)
            {
                nextQuestionDelay = 0.2;
                return;
            }

            ClearBalloons();
            int level = 1 + answeredQuestions / 4;
            if (missedQuestions.Count > 0 && freshQuestionsBeforeRetry <= 0)
            {
                question = missedQuestions.Dequeue();
                freshQuestionsBeforeRetry = missedQuestions.Count > 0 ? random.Next(1, 3) : 0;
            }
            else
            {
                question = MathQuestion.Create(random, level);
                if (missedQuestions.Count > 0) freshQuestionsBeforeRetry--;
            }

            double[] lanes = { 0.16, 0.38, 0.62, 0.84 };
            lanes = lanes.OrderBy(item => random.Next()).ToArray();
            double baseSpeed = 78 + Math.Min(30, answeredQuestions * 1.5);
            for (int index = 0; index < question.Options.Count; index++)
            {
                double radius = Math.Max(48, Math.Min(67, canvas.ActualWidth / 18));
                double x = canvas.ActualWidth * lanes[index] + random.Next(-10, 11);
                x = Math.Max(radius + 8, Math.Min(canvas.ActualWidth - radius - 8, x));
                var ellipse = new Ellipse
                {
                    Width = radius * 2,
                    Height = radius * 2.16,
                    Fill = BalloonColors[index],
                    Stroke = Brushes.White,
                    StrokeThickness = 5,
                    IsHitTestVisible = false,
                    Effect = new DropShadowEffect
                    {
                        BlurRadius = 18,
                        ShadowDepth = 5,
                        Opacity = 0.3,
                        Color = Colors.Black
                    }
                };
                var label = new TextBlock
                {
                    Text = question.Options[index].ToString(),
                    Foreground = Brushes.White,
                    FontSize = 34,
                    FontWeight = FontWeights.Black,
                    IsHitTestVisible = false
                };
                var balloon = new MathBalloon
                {
                    Shape = ellipse,
                    Label = label,
                    Radius = radius,
                    X = x,
                    Y = -radius - random.Next(0, 70),
                    BaseX = x,
                    Speed = baseSpeed + random.NextDouble() * 8 - 4,
                    SwayAmplitude = random.Next(6, 19),
                    SwayRate = 1.0 + random.NextDouble() * 0.9,
                    SwayPhase = random.NextDouble() * Math.PI * 2,
                    Answer = question.Options[index],
                    Color = BalloonColors[index]
                };
                balloons.Add(balloon);
                canvas.Children.Insert(0, ellipse);
                canvas.Children.Add(label);
                Panel.SetZIndex(label, 2);
                Position(balloon);
            }

            inputArmedAt = DateTime.UtcNow.AddMilliseconds(650);
            Raise(Instruction, 0, 0, question.VoiceKey);
        }

        private void MissQuestion()
        {
            missedQuestions.Enqueue(question);
            if (missedQuestions.Count == 1) freshQuestionsBeforeRetry = random.Next(2, 4);
            Raise("Det rätta svaret missades och kommer tillbaka senare.", 0, 0, "missed_answer");
            question = null;
            nextQuestionDelay = 1.4;
            ClearBalloons();
        }

        private void RemoveBalloon(MathBalloon balloon)
        {
            balloons.Remove(balloon);
            canvas.Children.Remove(balloon.Shape);
            canvas.Children.Remove(balloon.Label);
        }

        private void ClearBalloons()
        {
            foreach (MathBalloon balloon in balloons)
            {
                canvas.Children.Remove(balloon.Shape);
                canvas.Children.Remove(balloon.Label);
            }
            balloons.Clear();
        }

        private static void Position(MathBalloon balloon)
        {
            Canvas.SetLeft(balloon.Shape, balloon.X - balloon.Radius);
            Canvas.SetTop(balloon.Shape, balloon.Y - balloon.Radius);
            balloon.Label.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(balloon.Label, balloon.X - balloon.Label.DesiredSize.Width / 2);
            Canvas.SetTop(balloon.Label, balloon.Y - balloon.Label.DesiredSize.Height / 2);
        }

        private void Raise(string message, int scoreDelta, int playerIndex, string voiceKey = null)
        {
            GameEvent?.Invoke(this, new GameEventArgs
            {
                Message = message,
                ScoreDelta = scoreDelta,
                PlayerIndex = playerIndex,
                VoiceKey = voiceKey
            });
        }

        private static double Distance(double x1, double y1, double x2, double y2)
        {
            double dx = x1 - x2;
            double dy = y1 - y2;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        private static Brush BrushFrom(byte red, byte green, byte blue)
        {
            return new SolidColorBrush(Color.FromRgb(red, green, blue));
        }
    }
}

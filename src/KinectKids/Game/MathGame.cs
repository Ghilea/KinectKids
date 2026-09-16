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
        private MathQuestion question;
        private double nextQuestionDelay;
        private int answeredQuestions;

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
            question = null;
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
                balloon.Y = balloon.BaseY + Math.Sin(time * 1.7 + index * 0.9) * 10;
                Position(balloon);
            }
        }

        public int PopAt(Point point, double hitRadius, int playerIndex)
        {
            if (!IsActive || question == null) return 0;

            MathBalloon hit = balloons
                .OrderBy(item => Distance(point.X, point.Y, item.X, item.Y))
                .FirstOrDefault(item => Distance(point.X, point.Y, item.X, item.Y) <= item.Radius + hitRadius);
            if (hit == null) return 0;

            if (hit.Answer == question.CorrectAnswer)
            {
                int points = 10 + Math.Min(10, answeredQuestions);
                CurrentScore += points;
                answeredQuestions++;
                Raise("Rätt! " + question.Expression + " = " + question.CorrectAnswer, points, playerIndex);
                question = null;
                nextQuestionDelay = 0.55;
                ClearBalloons();
                return points;
            }

            hit.Shape.Opacity = 0.28;
            hit.Label.Opacity = 0.35;
            Raise("Nästan! Prova en annan ballong.", 0, playerIndex);
            return 0;
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
            question = MathQuestion.Create(random, level);

            double usableTop = Math.Max(190, canvas.ActualHeight * 0.31);
            double usableHeight = Math.Max(120, canvas.ActualHeight - usableTop - 80);
            for (int index = 0; index < question.Options.Count; index++)
            {
                double x = canvas.ActualWidth * (0.17 + 0.22 * index);
                double baseY = usableTop + usableHeight * (index % 2 == 0 ? 0.28 : 0.7);
                double radius = Math.Max(48, Math.Min(67, canvas.ActualWidth / 18));
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
                    Y = baseY,
                    BaseY = baseY,
                    Answer = question.Options[index],
                    Color = BalloonColors[index]
                };
                balloons.Add(balloon);
                canvas.Children.Insert(0, ellipse);
                canvas.Children.Add(label);
                Panel.SetZIndex(label, 2);
                Position(balloon);
            }

            Raise(Instruction, 0, 0);
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

        private void Raise(string message, int scoreDelta, int playerIndex)
        {
            GameEvent?.Invoke(this, new GameEventArgs
            {
                Message = message,
                ScoreDelta = scoreDelta,
                PlayerIndex = playerIndex
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

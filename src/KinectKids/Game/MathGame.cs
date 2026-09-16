using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Kinect;

namespace KinectKids.Game
{
    public sealed class MathGame : IGame
    {
        private static readonly Random Random = new Random();
        private const int SpawnCooldown = 0.5;
        private const int TimeoutSeconds = 8;

        public event EventHandler<GameEventArgs> OnGameEvent;
        public int CurrentScore { get; private set; }
        public bool IsActive => _gameActive;

        private bool _gameActive;
        private readonly Stack<MathBalloon> balloons = new Stack<MathBalloon>();
        private readonly Queue<MathQuestion> questionQueue = new Queue<MathQuestion>();
        private double spawnTimer;
        private DateTime timeoutAt;

        public void Start()
        {
            _gameActive = true;
            SpawnNewQuestion();
            OnGameEvent?.Invoke(this, new GameEventArgs 
            { 
                Message = "Mata ut svaret p\u00E5 fr\u00E5gan!" 
            });
        }

        public void Stop()
        {
            _gameActive = false;
            foreach (var balloon in balloons)
            {
                Canvas.SetZIndex(balloon.Shape, 1);
            }
        }

        public void Reset()
        {
            _gameActive = false;
            CurrentScore = 0;
            balloons.Clear();
            questionQueue.Clear();
            spawnTimer = 0;
            timeoutAt = DateTime.MinValue;
            OnGameEvent?.Invoke(this, new GameEventArgs { Message = "Redo!" });
        }

        public void Update(double dt)
        {
            spawnTimer += dt;
            if (spawnTimer >= SpawnCooldown && balloons.Count < 10)
            {
                spawnTimer = 0;
                SpawnQuestionBalloon();
            }

            DateTime now = DateTime.UtcNow;
            if (_gameActive && now > timeoutAt)
            {
                OnGameEvent?.Invoke(this, new GameEventArgs 
                { 
                    Message = "Tiden ute! F\u00F6rs\u00F6k igen." 
                });
                Reset();
            }

            foreach (var balloon in balloons.ToArray())
            {
                balloon.Y -= balloon.Speed * dt;
                if (balloon.Y + balloon.Radius < 0)
                {
                    Canvas.SetZIndex(balloon.Shape, 1);
                }
            }
        }

        public void Draw(DrawingContext context)
        {
            context.Clear();

            string instruction = _gameActive && questionQueue.Count > 0
                ? "Mata ut svaret p\u00E5 fr\u00E5gan!"
                : "Klicka Start f\u00F6r att spela";

            var textBlock = new TextBlock
            {
                Text = instruction,
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                IsHitTestVisible = false
            };

            context.PushTransform();
            context.TranslateTransform(0, 150);
            TextBlockGeometry geometry = new TextBlockGeometry(textBlock);
            context.DrawGeometry(brush: Brushes.White, geometry: geometry, pen: null);
            context.Pop();
        }

        public void OnKinectGesture(GestureType gesture)
        {
            if (_gameActive && questionQueue.Count > 0)
            {
                MathQuestion currentQuestion = questionQueue.Peek();
                DateTime now = DateTime.UtcNow;

                if (gesture == GestureType.HandsUp || gesture == GestureType.HandsPlus)
                {
                    timeoutAt = now + TimeSpan.FromSeconds(TimeoutSeconds);
                    OnGameEvent?.Invoke(this, new GameEventArgs 
                    { 
                        Message = "V\u00E4ntar p\u00E5 svar..." 
                    });
                }

                if (now >= timeoutAt)
                {
                    questionQueue.Dequeue();
                    SpawnNewQuestion();
                    OnGameEvent?.Invoke(this, new GameEventArgs 
                    { 
                        Message = "Inget svar mottaget!" 
                    });
                }
            }
        }

        private void SpawnQuestionBalloon()
        {
            MathQuestion question = GenerateMathQuestion();
            questionQueue.Enqueue(question);

            int selectedIndex = Random.Next(2, 5);
            var selectedColor = GetBalloonColor(selectedIndex + 1);

            double radius = Random.Next(40, 70);
            var balloon = new MathBalloon
            {
                Radius = radius,
                X = Random.NextDouble() * (canvas?.ActualWidth ?? 800) - radius,
                Y = canvas?.ActualHeight ?? 600 + radius,
                Speed = Random.Next(40, 90),
                AnswerText = GetOptionString(question.Options[selectedIndex]),
                CorrectAnswer = question.Options[selectedIndex],
                IsQuestionBalloon = true,
                Shape = new Ellipse
                {
                    Width = radius * 2,
                    Height = radius * 2.25,
                    Fill = selectedColor,
                    Stroke = Brushes.White,
                    StrokeThickness = 4,
                    Effect = new DropShadowEffect
                    {
                        BlurRadius = 12,
                        ShadowDepth = 3,
                        Opacity = 0.3,
                        Color = Colors.Black
                    },
                    IsHitTestVisible = false
                }
            };

            canvas?.Children.Insert(0, balloon.Shape);
            Canvas.SetLeft(balloon.Shape, balloon.X - balloon.Radius);
            Canvas.SetTop(balloon.Shape, balloon.Y - balloon.Radius);
            balloons.Push(balloon);

            if (questionQueue.Count == 1)
            {
                OnGameEvent?.Invoke(this, new GameEventArgs 
                { 
                    Message = $"Svara p\u00E5: {question.Expression} = ?" 
                });
            }
        }

        private void SpawnNewQuestion()
        {
            questionQueue.Enqueue(GenerateMathQuestion());
            OnGameEvent?.Invoke(this, new GameEventArgs 
            { 
                    Message = $"Nytt fr\u00E5ga: {question.Expression} = ?" 
            });
        }

        public int PopAt(Point point, double hitRadius)
        {
            if (!IsActive || questionQueue.Count == 0) return 0;

            MathBalloon hit = null;
            for (int i = balloons.Count - 1; i >= 0; i--)
            {
                var balloon = balloons[i];
                double distance = Distance(point.X, point.Y, balloon.X, balloon.Y);
                if (distance <= balloon.Radius + hitRadius)
                {
                    hit = balloon;
                    break;
                }
            }

            if (hit == null) return 0;

            MathQuestion question = questionQueue.Peek();
            string selectedAnswer = hit.AnswerText;
            int selectedIndex = question.Options.IndexOf(selectedAnswer);

            Color selectedColor = GetBalloonColor(selectedIndex + 1);

            List<string> allOptions = new List<string>();
            for (int i = 0; i < 4; i++)
            {
                int optionIndex = (i - 2) % 4;
                if (optionIndex < 0) optionIndex += 4;
                allOptions.Add(question.Options[optionIndex]);
            }

            var questionText = new TextBlock
            {
                Text = $"{question.Expression} = ?",
                FontSize = 24,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.White,
                Background = Brushes.Transparent
            };

            Canvas.SetZIndex(hit.Shape, 0);

            for (int i = 0; i < 4; i++)
            {
                double offsetX = ((i % 2) - 0.5) * 60;
                double offsetY = Math.Floor(i / 2.0) * 28 - 10;

                var textBlock = new TextBlock
                {
                    Text = allOptions[i],
                    FontSize = 24,
                    FontWeight = FontWeights.Medium,
                    Foreground = Brushes.White,
                    Background = GetBalloonColor(i + 1),
                    Margin = new Thickness(offsetX, offsetY, 0, 0)
                };

                Canvas.SetLeft(textBlock, hit.X - 30);
                Canvas.SetTop(textBlock, hit.Y - 50 + offsetY);

                canvas.Children.Add(textBlock);
            }

            Canvas.SetZIndex(questionText.Shape, 1);
            Canvas.SetLeft(questionText.Shape, questionText.X - 45);
            Canvas.SetTop(questionText.Shape, questionText.Y - 28);
            canvas.Children.Insert(0, questionText.Shape);

            return 1; // Return 1 for any click to avoid ballong issues
        }

        private MathQuestion GenerateMathQuestion()
        {
            string operation = "add";
            double a = Random.Next(2, 11);
            double b = Random.Next(2, 11);
            int answer;

            switch (operation)
            {
                case "add":
                    answer = (int)(a + b);
                    return new MathQuestion($"({a}+{b})")
                    {
                        Options = new List<string> { 
                            $"{answer}", 
                            $"{answer - 1}", 
                            $"{answer + 1}", 
                            $"{answer - 2}" 
                        }
                    };

                case "sub":
                    answer = (int)(a - b);
                    return new MathQuestion($"({a}-{b})")
                    {
                        Options = new List<string> { 
                            $"{answer}", 
                            $"{answer + 1}", 
                            $"{answer - 1}", 
                            $"{answer + 2}" 
                        }
                    };

                default:
                    answer = (int)(a * b);
                    return new MathQuestion($"({a}x{b})")
                    {
                        Options = new List<string> { 
                            $"{answer}", 
                            $"{answer - 1}", 
                            $"{answer + 1}", 
                            $"{answer - 3}" 
                        }
                    };
            }
        }

        private Color GetBalloonColor(int selectedIndex)
        {
            return new SolidColorBrush(Color.FromRgb(255, 91, 119))[(selectedIndex - 1) % 4];
        }

        private string GetOptionString(string option) => option;

        private double Distance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
        }

        private Canvas canvas => Application.Current?.FindResource("Playfield") as Canvas;
    }

    public class MathBalloon : Balloon
    {
        public string AnswerText { get; set; }
        public List<string> Options { get; set; }
        public bool IsQuestionBalloon { get; set; }

        public MathBalloon()
        {
            Options = new List<string>();
        }

        protected override void OnClick(Point point)
        {
            OnGameEvent?.Invoke(this, new GameEventArgs 
            { 
                Message = $"Svar: {AnswerText}" 
            });
        }
    }

    public class MathQuestion
    {
        public string Expression { get; set; }
        public List<string> Options { get; set; }
        public int Answer { get; private set; }

        public MathQuestion(string expression)
        {
            Expression = expression;
            Options = new List<string>();
        }

        public void GenerateOptions()
        {
            string operation = "add";
            string[] parts = Expression.Replace("(", "").Replace(")", "").Split('+', '-');
            int a = int.Parse(parts[0]);
            int b = int.Parse(parts[1]);
            int result;

            switch (operation)
            {
                case "add":
                    result = a + b;
                    break;
                case "sub":
                    result = a - b;
                    break;
                default:
                    result = a * b;
                    break;
            }

            Answer = result;

            int[] answers = { result, result - 1, result + 1 };
            Random random = new Random();
            
            for (int i = 0; i < 3; i++)
            {
                int wrong = random.Next(-5, 6);
                while (answers.Contains(result + wrong) || wrong == 0)
                {
                    wrong = random.Next(-5, 6);
                }
                answers.Add(result + wrong);
            }

            Array.Sort(answers);

            Options.Clear();
            foreach (int answer in answers)
            {
                Options.Add(answer.ToString());
            }

            int index = Array.IndexOf(answers, Answer);
            Options[index] = result.ToString();
        }
    }
}

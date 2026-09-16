using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace KinectKids.Game
{
    /// <summary>
    /// Matte-spel där barnen löser matematiska uttryck genom att fånga rätt färgkodade balonger.
    /// </summary>
    public sealed class MathGame : IGame
    {
        private readonly Canvas canvas;
        private readonly List<Balloon> balloons = new List<Balloon>();
        private string currentQuestion;
        private int correctAnswerValue;
        private int currentScore;
        private bool isWaitingForAnswer;

        public MathGame(Canvas canvas)
        {
            this.canvas = canvas;
            currentScore = 0;
            Reset();
        }

        /// <summary>
        /// Nuvarande poäng.
        /// </summary>
        public int CurrentScore => currentScore;

        /// <summary>
        /// Är spelet aktivt?
        /// </summary>
        public bool IsActive { get; private set; } = true;

        /// <summary>
        /// Återställer speltillståndet.
        /// </summary>
        public void Reset()
        {
            foreach (var balloon in balloons)
                canvas.Children.Remove(((MathBalloon)balloon).Shape);
            balloons.Clear();
            currentQuestion = null;
            isWaitingForAnswer = false;
        }

        /// <summary>
        /// Uppdaterar spellogiken.
        /// </summary>
        public void Update(double elapsedSeconds)
        {
            if (!IsActive || canvas.ActualWidth < 200 || canvas.ActualHeight < 150) return;

            if (currentQuestion != null && isWaitingForAnswer)
            {
                double timeRemaining = 8.0 - ((DateTime.Now - DateTime.MinValue).TotalSeconds);
                if (timeRemaining <= 0)
                {
                    EndTurn();
                }
            }
        }

        /// <summary>
        /// Hanterar spelarens input (att trycka på en punkt för att svara).
        /// </summary>
        public object HandleInput(Point point, int handId, DateTime now)
        {
            if (!IsActive || !isWaitingForAnswer) return null;

            // Kolla om någon balong hamnar under pekpunkten
            var hit = balloons.FirstOrDefault(b => 
                b.Y - b.Radius * 0.3 <= point.Y &&
                Math.Abs(b.X - point.X) < b.Radius);

            if (hit != null)
            {
                MathBalloon mathBalloon = hit as MathBalloon;
                if (mathBalloon != null && ParseAnswer(mathBalloon.AnswerText) == correctAnswerValue)
                {
                    currentScore += hit.Points;
                    Remove(hit);
                    
                    isWaitingForAnswer = false;
                    SpawnNewQuestion();
                    
                    return new MathInputResult 
                    { 
                        Success = true, 
                        Points = hit.Points,
                        Message = "Rätt svar! +10" 
                    };
                }
                else
                {
                    currentScore -= 5; // Straff för fel svar
                    Remove(hit);
                    
                    isWaitingForAnswer = false;
                    return new MathInputResult 
                    { 
                        Success = false, 
                        Points = -5,
                        Message = "Fel! Försök igen." 
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// Startar en ny frågerunda.
        /// </summary>
        private void SpawnNewQuestion()
        {
            currentQuestion = GenerateMathProblem();
            correctAnswerValue = ParseAnswer(currentQuestion);
            isWaitingForAnswer = true;

            string[] answers = GetAnswers(currentQuestion);
            Color[] colors = 
            {
                Colors.Red,    // Fel 1
                Colors.Green,  // Rätt
                Colors.Blue,   // Fel 2
                Colors.Yellow, // Fel 3
                Colors.Purple  // Alternativ
            };

            for (int i = 0; i < answers.Length; i++)
            {
                SpawnBalloon(answers[i], colors[i % colors.Length]);
            }
        }

        /// <summary>
        /// Genererar en ny matteuppgift.
        /// </summary>
        private string GenerateMathProblem()
        {
            int operation = new Random().Next(3); 
            
            switch (operation)
            {
                case 0: // Addition
                    int n1 = new Random().Next(1, 20);
                    int n2 = new Random().Next(1, 20);
                    return $"{n1} + {n2}";
                    
                case 1: // Subtraktion
                    int subN1 = new Random().Next(15, 30);
                    int subN2 = new Random().Next(5, 14);
                    return $"{subN1} - {subN2}";
                    
                case 2: // Multiplikation (enklare)
                    int mulN1 = new Random().Next(1, 10);
                    int mulN2 = new Random().Next(1, 9);
                    return $"{mulN1} × {mulN2}";
            }

            return "2 + 2";
        }

        /// <summary>
        /// Genererar svarsalternativ för en uppgift.
        /// </summary>
        private string[] GetAnswers(string question)
        {
            string[] answers = new string[5];
            int correctAnswer = ParseAnswer(question);
            
            // Rätt svar (index 1 för att visa på grönt)
            answers[1] = correctAnswer.ToString();
            
            // Felaktiga svar
            for (int i = 0; i < answers.Length && i != 1; i++)
            {
                int offset = new Random().Next(-5, 6);
                answers[i] = (correctAnswer + offset).ToString();
                
                if (i > 0)
                {
                    while (answers.Contains(answers[1]))
                    {
                        offset = new Random().Next(-10, 11);
                        answers[i] = (correctAnswer + offset).ToString();
                    }
                }
            }

            return answers;
        }

        /// <summary>
        /// Parser rätt svar från en fråga.
        /// </summary>
        private int ParseAnswer(string question)
        {
            try
            {
                string[] parts = question.Split(new[] { '+', '-', '×' }, StringSplitOptions.RemoveEmptyEntries);
                int n1 = int.Parse(parts[0]);
                int n2 = int.Parse(parts[1]);
                
                if (question.Contains('+')) return n1 + n2;
                if (question.Contains('-')) return n1 - n2;
                if (question.Contains('×')) return n1 * n2;
            }
            catch { }
            
            return 0;
        }

        /// <summary>
        /// Spawner en balong med ett specifikt svar.
        /// </summary>
        private void SpawnBalloon(string answer, Color color)
        {
            double radius = new Random().Next(35, 60);
            var shape = new Ellipse
            {
                Width = radius * 2,
                Height = radius * 2.25,
                Fill = new SolidColorBrush(color),
                Stroke = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255)),
                StrokeThickness = 6,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 14,
                    ShadowDepth = 4,
                    Opacity = 0.3
                },
                IsHitTestVisible = true
            };

            var balloon = new Balloon
            {
                Shape = shape,
                Radius = radius,
                X = radius + new Random().Next(100, Math.Max(200, canvas.ActualWidth - radius * 2)),
                Y = canvas.ActualHeight + radius + (new Random().Next(-50, 50)),
                Speed = random.Next(70, 140),
                Points = radius < 50 ? 10 : 15,
                Color = new SolidColorBrush(color)
            };

            balloons.Add(balloon);
            canvas.Children.Add(shape);
        }

        private void Remove(Balloon balloon)
        {
            balloons.Remove(balloon);
            Canvas.SetLeft(balloon.Shape, -100);
            Canvas.SetTop(balloon.Shape, -100);
        }

        private void EndTurn()
        {
            isWaitingForAnswer = false;
            SpawnNewQuestion();
        }

        private readonly Random random = new Random();
    }

    /// <summary>
    /// Resulter klass för matte-spel input.
    /// </summary>
    public class MathInputResult
    {
        public bool Success { get; set; }
        public int Points { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Balong med textattribut.
    /// </summary>
    public sealed class MathBalloon : Balloon
    {
        public string AnswerText { get; set; }
    }
}

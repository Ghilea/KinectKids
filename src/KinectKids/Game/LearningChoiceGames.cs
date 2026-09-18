using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using KinectKids.Models;

namespace KinectKids.Game
{
    /// <summary>
    /// Gemensam spelplan för korta pedagogiska valspel. Barnet slår på ett
    /// stort svarskort med valfri hand. Fel svar ger aldrig minuspoäng.
    /// </summary>
    public abstract class LearningChoiceGame : IGame
    {
        private static readonly Brush[] CardColors =
        {
            BrushFrom("#E94F64"), BrushFrom("#F3A83B"),
            BrushFrom("#28A982"), BrushFrom("#3C83D5")
        };

        private readonly Canvas canvas;
        private readonly Random random = new Random();
        private readonly List<ChoiceCard> cards = new List<ChoiceCard>();
        private LearningQuestion question;
        private double nextQuestionDelay;
        private int answeredQuestions;

        protected LearningChoiceGame(Canvas canvas)
        {
            this.canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        }

        public event EventHandler<GameEventArgs> GameEvent;

        public abstract string Id { get; }
        public abstract string DisplayName { get; }
        protected abstract string ReadyInstruction { get; }
        protected abstract LearningQuestion CreateLearningQuestion(Random source, int level);

        public string Instruction => question == null ? ReadyInstruction : question.Prompt;
        public int CurrentScore { get; private set; }
        public bool IsActive { get; private set; }

        public void Start()
        {
            IsActive = true;
            if (question == null) CreateQuestion();
        }

        public void Stop() => IsActive = false;

        public void Reset()
        {
            IsActive = false;
            CurrentScore = 0;
            answeredQuestions = 0;
            nextQuestionDelay = 0;
            question = null;
            ClearCards();
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
            for (int index = 0; index < cards.Count; index++)
            {
                ChoiceCard card = cards[index];
                card.Y = card.BaseY + Math.Sin(time * 1.45 + index * 0.8) * 7;
                Position(card);
            }
        }

        public int PopAt(Point point, double hitRadius, int playerIndex)
        {
            if (!IsActive || question == null) return 0;

            ChoiceCard hit = cards
                .Where(item => item.IsEnabled)
                .OrderBy(item => Distance(point, item))
                .FirstOrDefault(item => Distance(point, item) <= item.HitRadius + hitRadius);
            if (hit == null) return 0;

            if (string.Equals(hit.Answer, question.CorrectAnswer, StringComparison.Ordinal))
            {
                int points = 10 + Math.Min(10, answeredQuestions);
                CurrentScore += points;
                answeredQuestions++;
                Raise(question.SuccessMessage, points, playerIndex);
                question = null;
                nextQuestionDelay = 0.6;
                ClearCards();
                return points;
            }

            hit.IsEnabled = false;
            hit.Container.Opacity = 0.25;
            Raise("Bra försök! Prova ett annat svar.", 0, playerIndex);
            return 0;
        }

        public void Render(DrawingContext context) { }
        public void OnKinectGesture(GestureType gesture) { }

        private void CreateQuestion()
        {
            if (canvas.ActualWidth < 200 || canvas.ActualHeight < 200)
            {
                nextQuestionDelay = 0.2;
                return;
            }

            ClearCards();
            question = CreateLearningQuestion(random, 1 + answeredQuestions / 4);
            double width = Math.Max(145, Math.Min(205, canvas.ActualWidth * 0.18));
            double height = Math.Max(120, Math.Min(165, canvas.ActualHeight * 0.22));
            double top = Math.Max(210, canvas.ActualHeight * 0.35);
            double lower = Math.Min(canvas.ActualHeight - height / 2 - 45, top + height * 1.05);

            for (int index = 0; index < question.Options.Count; index++)
            {
                string answer = question.Options[index];
                var label = new TextBlock
                {
                    Text = answer,
                    Foreground = Brushes.White,
                    FontSize = answer.Length <= 2 ? 58 : answer.Length <= 7 ? 31 : 24,
                    FontWeight = FontWeights.Black,
                    TextAlignment = TextAlignment.Center,
                    TextWrapping = TextWrapping.Wrap,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    IsHitTestVisible = false
                };
                var border = new Border
                {
                    Width = width,
                    Height = height,
                    CornerRadius = new CornerRadius(28),
                    Background = CardColors[index],
                    BorderBrush = Brushes.White,
                    BorderThickness = new Thickness(5),
                    Child = label,
                    Padding = new Thickness(10),
                    IsHitTestVisible = false,
                    Effect = new DropShadowEffect
                    {
                        BlurRadius = 18,
                        ShadowDepth = 6,
                        Opacity = 0.35,
                        Color = Colors.Black
                    }
                };
                var card = new ChoiceCard
                {
                    Container = border,
                    Answer = answer,
                    X = canvas.ActualWidth * (0.17 + index * 0.22),
                    Y = index % 2 == 0 ? top : lower,
                    BaseY = index % 2 == 0 ? top : lower,
                    Width = width,
                    Height = height,
                    IsEnabled = true
                };
                cards.Add(card);
                canvas.Children.Insert(0, border);
                Position(card);
            }

            Raise(Instruction, 0, 0);
        }

        private void ClearCards()
        {
            foreach (ChoiceCard card in cards) canvas.Children.Remove(card.Container);
            cards.Clear();
        }

        private static void Position(ChoiceCard card)
        {
            Canvas.SetLeft(card.Container, card.X - card.Width / 2);
            Canvas.SetTop(card.Container, card.Y - card.Height / 2);
        }

        private static double Distance(Point point, ChoiceCard card)
        {
            double dx = point.X - card.X;
            double dy = point.Y - card.Y;
            return Math.Sqrt(dx * dx + dy * dy);
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

        private static Brush BrushFrom(string color) =>
            new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));

        private sealed class ChoiceCard
        {
            public Border Container { get; set; }
            public string Answer { get; set; }
            public double X { get; set; }
            public double Y { get; set; }
            public double BaseY { get; set; }
            public double Width { get; set; }
            public double Height { get; set; }
            public bool IsEnabled { get; set; }
            public double HitRadius => Math.Max(Width, Height) * 0.48;
        }
    }

    public sealed class SwedishGame : LearningChoiceGame
    {
        private static readonly string[][] WordGroups =
        {
            new[] { "SOL", "MÅNE", "BOLL", "HUS" },
            new[] { "KATT", "HUND", "FISK", "FÅGEL" },
            new[] { "GLASS", "BANAN", "ÄPPLE", "OST" },
            new[] { "BÅT", "BIL", "TÅG", "CYKEL" }
        };

        public SwedishGame(Canvas canvas) : base(canvas) { }
        public override string Id => "SwedishGame";
        public override string DisplayName => "Bokstavsjakten";
        protected override string ReadyInstruction => "Gör er redo för Bokstavsjakten!";

        protected override LearningQuestion CreateLearningQuestion(Random source, int level)
        {
            string[] group = WordGroups[source.Next(WordGroups.Length)];
            string word = group[source.Next(group.Length)];
            if (level <= 2 || source.NextDouble() < 0.55)
            {
                string correct = word.Substring(0, 1);
                string[] alphabet = { "A", "B", "D", "F", "H", "K", "M", "S", "T", "Å", "Ä", "Ö" };
                return MakeQuestion(source, "Vilken bokstav börjar " + word + " med?", correct,
                    alphabet.Where(item => item != correct), "Rätt! " + word + " börjar med " + correct + ".");
            }

            string shown = word.Substring(0, word.Length - 1) + " _";
            string finalLetter = word.Substring(word.Length - 1, 1);
            string[] endings = { "A", "E", "I", "L", "N", "R", "S", "T", "Å" };
            return MakeQuestion(source, "Vilken bokstav saknas?  " + shown, finalLetter,
                endings.Where(item => item != finalLetter), "Precis! Ordet är " + word + ".");
        }

        private static LearningQuestion MakeQuestion(Random source, string prompt, string correct,
            IEnumerable<string> distractors, string success)
        {
            var options = distractors.OrderBy(item => source.Next()).Take(3).Concat(new[] { correct })
                .OrderBy(item => source.Next()).ToArray();
            return new LearningQuestion(prompt, correct, options, success);
        }
    }

    public sealed class ShapeGame : LearningChoiceGame
    {
        private static readonly string[] Shapes = { "●", "▲", "■", "★" };
        private static readonly string[] Names = { "CIRKELN", "TRIANGELN", "KVADRATEN", "STJÄRNAN" };

        public ShapeGame(Canvas canvas) : base(canvas) { }
        public override string Id => "ShapeGame";
        public override string DisplayName => "Formverkstan";
        protected override string ReadyInstruction => "Gör er redo för Formverkstan!";

        protected override LearningQuestion CreateLearningQuestion(Random source, int level)
        {
            int index = source.Next(Shapes.Length);
            string extra = level >= 3 && index < 3
                ? index == 0 ? " Den har inga hörn." : index == 1 ? " Den har tre hörn." : " Den har fyra hörn."
                : string.Empty;
            return new LearningQuestion("Hitta " + Names[index].ToLowerInvariant() + "." + extra,
                Shapes[index], Shapes.OrderBy(item => source.Next()).ToArray(),
                "Ja! Det är " + Names[index].ToLowerInvariant() + ".");
        }
    }

    public sealed class PatternGame : LearningChoiceGame
    {
        private static readonly string[][] Patterns =
        {
            new[] { "●  ■  ●  ■  ?", "●" },
            new[] { "▲  ▲  ★  ▲  ▲  ?", "★" },
            new[] { "1   2   3   4   ?", "5" },
            new[] { "2   4   6   8   ?", "10" },
            new[] { "A   B   A   B   ?", "A" },
            new[] { "LITEN  STOR  LITEN  STOR  ?", "LITEN" }
        };

        public PatternGame(Canvas canvas) : base(canvas) { }
        public override string Id => "PatternGame";
        public override string DisplayName => "Mönsterjakten";
        protected override string ReadyInstruction => "Gör er redo för Mönsterjakten!";

        protected override LearningQuestion CreateLearningQuestion(Random source, int level)
        {
            int maximum = level < 2 ? 3 : Patterns.Length;
            string[] selected = Patterns[source.Next(maximum)];
            string[] pool = { "●", "■", "▲", "★", "A", "B", "3", "5", "9", "10", "LITEN", "STOR" };
            var options = pool.Where(item => item != selected[1]).OrderBy(item => source.Next()).Take(3)
                .Concat(new[] { selected[1] }).OrderBy(item => source.Next()).ToArray();
            return new LearningQuestion("Vad kommer sedan?  " + selected[0], selected[1], options,
                "Rätt! Du hittade mönstret.");
        }
    }
}


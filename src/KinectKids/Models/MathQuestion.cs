using System;

namespace KinectKids.Game
{
    /// <summary>
    /// Datastruktur för matematikfrågor i Matte-spel.
    /// </summary>
    public sealed class MathQuestion
    {
        /// <summary>
        /// Textuell representation av frågan (t.ex. "3+2").
        /// </summary>
        public string Expression { get; set; }

        /// <summary>
        /// Rätt svar till frågan (t.ex. 5 för "3+2").
        /// </summary>
        public int CorrectAnswer { get; set; }

        /// <summary>
        /// Möjliga svarsalternativ (inklusive felaktiga).
        /// </summary>
        public string[] Options { get; set; }

        /// <summary>
        /// Poängvärdet för denna fråga.
        /// </summary>
        public int Points { get; set; }

        /// <summary>
        /// Skapar en ny mattefråga med slumpade tal.
        /// </summary>
        /// <param name="min">Minsta tal (enklare nivåer).</param>
        /// <param name="max">Största tal.</param>
        /// <param name="operation">Mål för matematisk operation.</param>
        public MathQuestion(int min = 2, int max = 10, OperationType operation = OperationType.Addition)
        {
            Random rand = new Random();

            // Generera tal baserat på operation
            int n1, n2;
            switch (operation)
            {
                case OperationType.Addition:
                    n1 = rand.Next(min, max + 1);
                    n2 = rand.Next(min, max + 1);
                    Expression = $"{n1} + {n2}";
                    CorrectAnswer = n1 + n2;
                    Points = 10;
                    break;

                case OperationType.Subtraction:
                    // Se till att resultatet blir positivt
                    n1 = rand.Next(min, max + 1);
                    n2 = rand.Next(min, Math.Min(max - min + 1, n1)); // n2 <= n1 för positivt resultat
                    Expression = $"{n1} - {n2}";
                    CorrectAnswer = n1 - n2;
                    Points = 10;
                    break;

                case OperationType.Multiplication:
                    // Enklare multiplikation för barn
                    n1 = rand.Next(1, min + 1);
                    n2 = rand.Next(1, max + 1);
                    Expression = $"{n1} × {n2}";
                    CorrectAnswer = n1 * n2;
                    Points = 15; // Mer poäng för svårare operation
                    break;

                default:
                    n1 = rand.Next(min, max + 1);
                    n2 = rand.Next(min, max + 1);
                    Expression = $"{n1} + {n2}";
                    CorrectAnswer = n1 + n2;
                    Points = 10;
                    break;
            }

            // Generera svarsalternativ
            Options = GenerateOptions(CorrectAnswer, min - 5, max + 5);
        }

        /// <summary>
        /// Genererar slumpade svarsalternativ med ett korrekt svar och felaktiga.
        /// </summary>
        private string[] GenerateOptions(int correctAnswer, int minOffset, int maxOffset)
        {
            System.Collections.Generic.List<string> options = new System.Collections.Generic.List<string>();
            
            // Lägg till rätt svar
            options.Add(correctAnswer.ToString());

            // Skapa 3-4 felaktiga alternativ
            while (options.Count < 4)
            {
                int offset = new Random().Next(minOffset, maxOffset + 1);
                int wrongAnswer = correctAnswer + offset;
                
                string optionText = wrongAnswer.ToString();
                
                // Undvik duplicering
                if (!options.Contains(optionText))
                {
                    options.Add(optionText);
                }
            }

            return options.ToArray();
        }

        /// <summary>
        /// Returnerar rätt svarsalternativ som text.
        /// </summary>
        public string GetCorrectOptionText()
        {
            foreach (string option in Options)
            {
                if (int.TryParse(option, out int value) && value == CorrectAnswer)
                {
                    return option;
                }
            }
            return null;
        }

        /// <summary>
        /// Skapar en ny fråga med samma operation men nya tal.
        /// </summary>
        public MathQuestion GenerateNextSameType()
        {
            return new MathQuestion(
                this.Points <= 15 ? 
                    (this.Expression.Contains('×') ? 2 : 1) : 
                    (this.Expression.Contains('×') ? 3 : 2),
                this.Expression.Contains('+') ? 20 : 15,
                GetOperationType()
            );
        }

        /// <summary>
        /// Hämtar operationstyp från nuvarande fråga.
        /// </summary>
        private OperationType GetOperationType()
        {
            if (Expression.Contains('×')) return OperationType.Multiplication;
            if (Expression.Contains('-')) return OperationType.Subtraction;
            return OperationType.Addition;
        }

        /// <summary>
        /// Hämtar operationstyp baserat på Points (enklare = addition/subtraction).
        /// </summary>
        public OperationType GetOperationFromDifficulty()
        {
            if (Points > 15) return OperationType.Multiplication;
            if (Expression.Contains('-')) return OperationType.Subtraction;
            return OperationType.Addition;
        }
    }

    /// <summary>
    /// Enumeration för matematiska operationer.
    /// </summary>
    public enum OperationType
    {
        Addition,    // + 
        Subtraction, // -
        Multiplication // ×
    }
}

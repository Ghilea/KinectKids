using System;
using System.Collections.Generic;
using System.Linq;

namespace KinectKids.Models
{
    public enum MathOperation
    {
        Addition,
        Subtraction
    }

    public sealed class MathQuestion
    {
        private MathQuestion(string expression, int correctAnswer, IReadOnlyList<int> options, string voiceKey)
        {
            Expression = expression;
            CorrectAnswer = correctAnswer;
            Options = options;
            VoiceKey = voiceKey;
        }

        public string Expression { get; }
        public int CorrectAnswer { get; }
        public IReadOnlyList<int> Options { get; }
        public string VoiceKey { get; }

        public static MathQuestion Create(Random random, int level)
        {
            int safeLevel = Math.Max(1, Math.Min(5, level));
            MathOperation operation = safeLevel >= 2 && random.NextDouble() >= 0.58
                ? MathOperation.Subtraction
                : MathOperation.Addition;

            int maximum = 5 + safeLevel * 3;
            int first = random.Next(1, maximum + 1);
            int second = random.Next(1, maximum + 1);
            int answer;
            string expression;

            if (operation == MathOperation.Subtraction)
            {
                if (second > first)
                {
                    int swap = first;
                    first = second;
                    second = swap;
                }
                answer = first - second;
                expression = first + " − " + second;
            }
            else
            {
                answer = first + second;
                expression = first + " + " + second;
            }

            var options = new HashSet<int> { answer };
            while (options.Count < 4)
            {
                int spread = Math.Max(3, safeLevel + 2);
                int candidate = Math.Max(0, answer + random.Next(-spread, spread + 1));
                options.Add(candidate);
            }

            string voiceKey = "math|" + first + "|" +
                              (operation == MathOperation.Addition ? "plus" : "minus") + "|" + second;
            return new MathQuestion(expression, answer, options.OrderBy(item => random.Next()).ToArray(), voiceKey);
        }
    }
}

using System.Collections.Generic;

namespace KinectKids.Models
{
    public sealed class LearningQuestion
    {
        public LearningQuestion(string prompt, string correctAnswer,
            IReadOnlyList<string> options, string successMessage)
        {
            Prompt = prompt;
            CorrectAnswer = correctAnswer;
            Options = options;
            SuccessMessage = successMessage;
        }

        public string Prompt { get; }
        public string CorrectAnswer { get; }
        public IReadOnlyList<string> Options { get; }
        public string SuccessMessage { get; }
    }
}

using System.Collections.Generic;

namespace KinectKids.Models
{
    public sealed class LearningQuestion
    {
        public LearningQuestion(string prompt, string correctAnswer,
            IReadOnlyList<string> options, string successMessage, string voiceKey, string visualPrompt = null)
        {
            Prompt = prompt;
            CorrectAnswer = correctAnswer;
            Options = options;
            SuccessMessage = successMessage;
            VoiceKey = voiceKey;
            VisualPrompt = visualPrompt;
        }

        public string Prompt { get; }
        public string CorrectAnswer { get; }
        public IReadOnlyList<string> Options { get; }
        public string SuccessMessage { get; }
        public string VoiceKey { get; }
        public string VisualPrompt { get; }
    }
}

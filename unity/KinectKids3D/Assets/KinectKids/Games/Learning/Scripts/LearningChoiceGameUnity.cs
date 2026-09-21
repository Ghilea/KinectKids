using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace KinectKids3D.Platform
{
    public enum LearningGameMode { Math, Swedish, Shapes, Patterns }

    public sealed class LearningChoiceGameUnity : MonoBehaviour
    {
        private sealed class Question
        {
            public string Prompt;
            public string Visual;
            public string Correct;
            public string[] Options;
            public string Voice;
        }

        private sealed class FallingCard
        {
            public string Answer;
            public float BaseX;
            public float X;
            public float Y;
            public float Speed;
            public float Phase;
            public int Color;
        }

        private readonly System.Random random = new System.Random();
        private readonly List<FallingCard> cards = new List<FallingCard>();
        private readonly Queue<Question> missed = new Queue<Question>();
        private readonly Dictionary<int, Vector2> lockedHands = new Dictionary<int, Vector2>();
        private readonly Color[] cardColors =
        {
            new Color(0.92f, 0.25f, 0.37f), new Color(0.98f, 0.59f, 0.12f),
            new Color(0.10f, 0.66f, 0.48f), new Color(0.18f, 0.49f, 0.84f)
        };

        private LearningGameMode mode;
        private Question question;
        private AudioSource voice;
        private float nextQuestionAt;
        private float inputArmedAt;
        private float roundEndsAt;
        private float messageUntil;
        private string message = "";
        private int score;
        private int answered;
        private int questionsSinceMiss;
        private bool configured;
        private bool finished;
        private GUIStyle title;
        private GUIStyle prompt;
        private GUIStyle cardStyle;
        private GUIStyle hud;

        public void Configure(LearningGameMode value)
        {
            mode = value;
            configured = true;
        }

        private void Start()
        {
            if (!configured) mode = LearningGameMode.Math;
            Camera camera = Camera.main;
            if (camera == null)
            {
                camera = new GameObject("Pedagogisk kamera").AddComponent<Camera>();
                camera.tag = "MainCamera";
                camera.gameObject.AddComponent<AudioListener>();
            }
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor();
            voice = gameObject.AddComponent<AudioSource>();
            voice.spatialBlend = 0f;
            voice.playOnAwake = false;
            roundEndsAt = Time.unscaledTime + 90f;
            nextQuestionAt = Time.unscaledTime + 0.4f;
        }

        private void Update()
        {
            if (finished)
            {
                if (Input.GetKeyDown(KeyCode.R)) Restart();
                return;
            }
            if (Time.unscaledTime >= roundEndsAt)
            {
                finished = true;
                StopVoice();
                return;
            }
            if (question == null)
            {
                if (Time.unscaledTime >= nextQuestionAt && (voice == null || !voice.isPlaying)) CreateQuestion();
                return;
            }

            float delta = Time.deltaTime;
            for (int i = 0; i < cards.Count; i++)
            {
                FallingCard card = cards[i];
                card.Y += card.Speed * delta;
                card.X = card.BaseX + Mathf.Sin(Time.time * 1.35f + card.Phase) * 0.012f;
            }

            for (int i = 0; i < cards.Count; i++)
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))
                    || Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad1 + i)))
                { Choose(cards[i], -1, Vector2.zero); return; }

            ProcessHands();
            FallingCard correct = cards.FirstOrDefault(item => item.Answer == question.Correct);
            if (correct != null && correct.Y > 1.13f) MissQuestion();
        }

        private void ProcessHands()
        {
            KinectKidsInputManager input = KinectKidsInputManager.Instance;
            if (input == null) return;
            IReadOnlyList<AimSample> samples = input.AimSamples;
            bool kinect = input.KinectConnected;
            for (int i = 0; i < samples.Count; i++)
            {
                AimSample sample = samples[i];
                Vector2 point = sample.Position;
                if (lockedHands.TryGetValue(sample.HandId, out Vector2 locked))
                {
                    if (Vector2.Distance(point, locked) < 0.09f) continue;
                    lockedHands.Remove(sample.HandId);
                }
                if (Time.unscaledTime < inputArmedAt) continue;
                FallingCard hit = CardAt(point);
                if (hit == null) continue;
                if (!kinect && !sample.Fire) continue;
                Choose(hit, sample.HandId, point);
                return;
            }
        }

        private FallingCard CardAt(Vector2 point)
        {
            float width = 0.17f, height = 0.19f;
            return cards.Where(card => card.Y - height * 0.5f > 0.21f)
                .OrderBy(card => Vector2.Distance(point, new Vector2(card.X, card.Y)))
                .FirstOrDefault(card => Mathf.Abs(point.x - card.X) <= width * 0.55f
                    && Mathf.Abs(point.y - card.Y) <= height * 0.55f);
        }

        private void Choose(FallingCard card, int hand, Vector2 point)
        {
            if (hand >= 0) lockedHands[hand] = point;
            if (card.Answer == question.Correct)
            {
                score += 10 + Mathf.Min(10, answered);
                answered++;
                questionsSinceMiss++;
                message = "RÄTT!";
                messageUntil = Time.unscaledTime + 1.0f;
                Speak("correct");
                question = null;
                cards.Clear();
                nextQuestionAt = Time.unscaledTime + 1.15f;
            }
            else
            {
                score = Mathf.Max(0, score - 3);
                cards.Remove(card);
                message = "PROVA IGEN";
                messageUntil = Time.unscaledTime + 0.9f;
                Speak("retry");
            }
        }

        private void MissQuestion()
        {
            missed.Enqueue(question);
            questionsSinceMiss = 0;
            message = "SVARET KOMMER IGEN SENARE";
            messageUntil = Time.unscaledTime + 1.2f;
            Speak("missed_answer");
            question = null;
            cards.Clear();
            nextQuestionAt = Time.unscaledTime + 1.35f;
        }

        private void CreateQuestion()
        {
            bool retry = missed.Count > 0 && questionsSinceMiss >= 2;
            question = retry ? missed.Dequeue() : GenerateQuestion();
            if (retry) questionsSinceMiss = 0;
            else if (missed.Count > 0) questionsSinceMiss++;
            cards.Clear();
            float[] lanes = { 0.15f, 0.38f, 0.62f, 0.85f };
            lanes = lanes.OrderBy(_ => random.Next()).ToArray();
            for (int i = 0; i < question.Options.Length; i++)
            {
                cards.Add(new FallingCard
                {
                    Answer = question.Options[i], BaseX = lanes[i], X = lanes[i],
                    Y = -0.11f - i * 0.028f, Speed = 0.075f + Mathf.Min(0.035f, answered * 0.0018f),
                    Phase = (float)random.NextDouble() * 6.28f, Color = i
                });
            }
            inputArmedAt = Time.unscaledTime + 0.65f;
            Speak(question.Voice);
        }

        private Question GenerateQuestion()
        {
            int level = 1 + answered / 4;
            switch (mode)
            {
                case LearningGameMode.Swedish: return SwedishQuestion(level);
                case LearningGameMode.Shapes: return ShapeQuestion();
                case LearningGameMode.Patterns: return PatternQuestion(level);
                default: return MathQuestion(level);
            }
        }

        private Question MathQuestion(int level)
        {
            int max = 5 + Mathf.Min(5, level) * 3;
            int a = random.Next(1, max + 1), b = random.Next(1, max + 1);
            bool subtract = level >= 2 && random.NextDouble() > 0.58;
            if (subtract && b > a) { int swap = a; a = b; b = swap; }
            int answer = subtract ? a - b : a + b;
            HashSet<int> choices = new HashSet<int> { answer };
            while (choices.Count < 4) choices.Add(Math.Max(0, answer + random.Next(-4 - level, 5 + level)));
            return new Question
            {
                Prompt = "Vad blir?", Visual = a + (subtract ? " − " : " + ") + b + " = ?", Correct = answer.ToString(),
                Options = choices.Select(x => x.ToString()).OrderBy(_ => random.Next()).ToArray(),
                Voice = "math|" + a + "|" + (subtract ? "minus" : "plus") + "|" + b
            };
        }

        private Question SwedishQuestion(int level)
        {
            string[] words = { "SOL", "MÅNE", "BOLL", "HUS", "KATT", "HUND", "FISK", "FÅGEL", "GLASS", "BANAN", "ÄPPLE", "OST", "BÅT", "BIL", "TÅG", "CYKEL" };
            string word = words[random.Next(words.Length)];
            bool ending = level > 2 && random.NextDouble() > 0.5;
            string correct = ending ? word.Substring(word.Length - 1) : word.Substring(0, 1);
            string[] alphabet = { "A", "B", "D", "F", "H", "K", "M", "N", "S", "T", "Å", "Ä", "Ö" };
            string[] options = alphabet.Where(x => x != correct).OrderBy(_ => random.Next()).Take(3)
                .Concat(new[] { correct }).OrderBy(_ => random.Next()).ToArray();
            string key = WordKey(word);
            return new Question
            {
                Prompt = ending ? "Vilken bokstav saknas?" : "Vilken bokstav börjar ordet med?",
                Visual = ending ? word.Substring(0, word.Length - 1) + " _" : word,
                Correct = correct, Options = options, Voice = (ending ? "missing_" : "starts_") + key
            };
        }

        private Question ShapeQuestion()
        {
            string[] shapes = { "●", "▲", "■", "★" };
            string[] names = { "CIRKEL", "TRIANGEL", "KVADRAT", "STJÄRNA" };
            string[] keys = { "circle", "triangle", "square", "star" };
            int index = random.Next(shapes.Length);
            return new Question { Prompt = "Hitta formen", Visual = names[index], Correct = shapes[index],
                Options = shapes.OrderBy(_ => random.Next()).ToArray(), Voice = "shape_" + keys[index] };
        }

        private Question PatternQuestion(int level)
        {
            string[][] patterns =
            {
                new[] { "●  ■  ●  ■  ?", "●" }, new[] { "▲  ▲  ★  ▲  ▲  ?", "★" },
                new[] { "1  2  3  4  ?", "5" }, new[] { "2  4  6  8  ?", "10" },
                new[] { "A  B  A  B  ?", "A" }, new[] { "LITEN  STOR  LITEN  STOR  ?", "LITEN" },
                new[] { "■  ▲  ■  ▲  ?", "■" }, new[] { "1  3  5  7  ?", "9" },
                new[] { "A  A  B  A  A  ?", "B" }, new[] { "★  ●  ●  ★  ●  ●  ?", "★" },
                new[] { "10  8  6  4  ?", "2" }, new[] { "B  C  D  E  ?", "F" }
            };
            int available = 6;
            int index = random.Next(available);
            string answer = patterns[index][1];
            string[] pool = { "●", "■", "▲", "★", "A", "B", "F", "2", "5", "9", "10", "LITEN", "STOR" };
            string[] options = pool.Where(x => x != answer).OrderBy(_ => random.Next()).Take(3)
                .Concat(new[] { answer }).OrderBy(_ => random.Next()).ToArray();
            return new Question { Prompt = "Vad kommer sedan?", Visual = patterns[index][0], Correct = answer,
                Options = options, Voice = "pattern_" + (index % 6) };
        }

        private static string WordKey(string word) => word.ToLowerInvariant()
            .Replace("å", "a").Replace("ä", "a").Replace("ö", "o");

        private void Speak(string key)
        {
            StopVoice();
            StartCoroutine(SpeakRoutine(VoiceParts(key)));
        }

        private IEnumerator SpeakRoutine(IEnumerable<string> parts)
        {
            foreach (string part in parts)
            {
                AudioClip clip = Resources.Load<AudioClip>("Voice/" + part);
                if (clip == null) continue;
                voice.clip = clip;
                voice.Play();
                while (voice.isPlaying) yield return null;
                yield return new WaitForSecondsRealtime(0.06f);
            }
        }

        private static IEnumerable<string> VoiceParts(string key)
        {
            string[] parts = (key ?? "").Split('|');
            if (parts.Length == 4 && parts[0] == "math")
                return new[] { "prompt_math", "number_" + parts[1], parts[2], "number_" + parts[3] };
            return new[] { key };
        }

        private void StopVoice()
        {
            StopAllCoroutines();
            if (voice != null) voice.Stop();
        }

        private void Restart()
        {
            score = answered = questionsSinceMiss = 0;
            missed.Clear(); cards.Clear(); question = null; finished = false;
            roundEndsAt = Time.unscaledTime + 90f;
            nextQuestionAt = Time.unscaledTime + 0.3f;
        }

        private void OnGUI()
        {
            EnsureStyles();
            DrawBackground();

            if (finished) { DrawHeader(); DrawFinished(); DrawCursors(); return; }
            if (question != null)
            {
                float w = Screen.width * 0.17f, h = Screen.height * 0.19f;
                for (int i = 0; i < cards.Count; i++)
                {
                    FallingCard card = cards[i];
                    Rect rect = new Rect(card.X * Screen.width - w * 0.5f, card.Y * Screen.height - h * 0.5f, w, h);
                    Fill(new Rect(rect.x + 7, rect.y + 8, rect.width, rect.height), new Color(0.12f, 0.08f, 0.18f, 0.28f));
                    Fill(rect, cardColors[card.Color % cardColors.Length]);
                    GUI.Label(rect, card.Answer, cardStyle);
                    GUI.Label(new Rect(rect.x, rect.yMax - 30, rect.width, 28), (i + 1).ToString(), hud);
                }
            }
            DrawHeader();
            if (Time.unscaledTime < messageUntil)
                GUI.Label(new Rect(0, Screen.height * 0.46f, Screen.width, 90), message, title);
            DrawCursors();
        }

        private void DrawHeader()
        {
            Color background = BackgroundColor();
            Fill(new Rect(0, 0, Screen.width, 235f), new Color(background.r, background.g, background.b, 0.98f));
            Fill(new Rect(0, 229f, Screen.width, 6f), new Color(AccentColor().r, AccentColor().g, AccentColor().b, 0.38f));
            GUI.Label(new Rect(20, 12, Screen.width - 40, 70), Title(), title);
            GUI.Label(new Rect(30, 76, 280, 42), "POÄNG: " + score, hud);
            GUI.Label(new Rect(Screen.width - 310, 76, 280, 42), "TID: " + Mathf.CeilToInt(Mathf.Max(0, roundEndsAt - Time.unscaledTime)), hud);
            if (question == null) return;
            GUI.Label(new Rect(Screen.width * 0.16f, 88, Screen.width * 0.68f, 48), question.Prompt, prompt);
            GUI.Label(new Rect(Screen.width * 0.10f, 132, Screen.width * 0.80f, 92), question.Visual, title);
        }

        private void DrawBackground()
        {
            Fill(new Rect(0, 0, Screen.width, Screen.height), BackgroundColor());
            Color ink = AccentColor();
            for (int i = 0; i < 13; i++)
            {
                float x = (i * 173f + 40f) % Mathf.Max(1f, Screen.width - 100f);
                float y = Screen.height * (0.22f + (i % 5) * 0.16f);
                Fill(new Rect(x, y, 55f + (i % 4) * 24f, 6f), new Color(ink.r, ink.g, ink.b, 0.18f));
            }
        }

        private void DrawCursors()
        {
            KinectKidsInputManager input = KinectKidsInputManager.Instance;
            if (input == null) return;
            foreach (AimSample sample in input.AimSamples)
            {
                Vector2 p = new Vector2(sample.Position.x * Screen.width, sample.Position.y * Screen.height);
                Color color = sample.PlayerIndex == 0
                    ? ((sample.HandId & 1) == 0 ? new Color(0.10f, 0.72f, 0.92f, 0.85f) : new Color(0.96f, 0.30f, 0.52f, 0.85f))
                    : ((sample.HandId & 1) == 0 ? new Color(0.18f, 0.82f, 0.42f, 0.85f) : new Color(1f, 0.70f, 0.12f, 0.85f));
                Fill(new Rect(p.x - 18, p.y - 18, 36, 36), color);
            }
        }

        private void DrawFinished()
        {
            GUI.Label(new Rect(0, Screen.height * 0.25f, Screen.width, 90), "BRA JOBBAT!", title);
            GUI.Label(new Rect(0, Screen.height * 0.39f, Screen.width, 70), "DU FICK " + score + " POÄNG", title);
            if (GUI.Button(new Rect(Screen.width * 0.30f, Screen.height * 0.58f, Screen.width * 0.40f, 60), "SPELA IGEN (R)")) Restart();
            if (GUI.Button(new Rect(Screen.width * 0.30f, Screen.height * 0.68f, Screen.width * 0.40f, 60), "TILL SPELMENYN"))
                KinectKidsPlatformRoot.Instance.Scenes.LoadMenu();
        }

        private string Title() => mode == LearningGameMode.Math ? "MATEMATIKBANAN" : mode == LearningGameMode.Swedish ? "BOKSTAVSJAKTEN" : mode == LearningGameMode.Shapes ? "FORMVERKSTAN" : "MÖNSTERJAKTEN";
        private Color BackgroundColor() => mode == LearningGameMode.Math ? new Color(0.84f, 0.94f, 1f) : mode == LearningGameMode.Swedish ? new Color(1f, 0.90f, 0.83f) : mode == LearningGameMode.Shapes ? new Color(0.87f, 1f, 0.88f) : new Color(0.94f, 0.87f, 1f);
        private Color AccentColor() => mode == LearningGameMode.Math ? new Color(0.10f, 0.48f, 0.86f) : mode == LearningGameMode.Swedish ? new Color(0.92f, 0.35f, 0.18f) : mode == LearningGameMode.Shapes ? new Color(0.10f, 0.62f, 0.36f) : new Color(0.58f, 0.22f, 0.78f);

        private void EnsureStyles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Clamp(Screen.height / 16, 38, 70), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            prompt = new GUIStyle(title) { fontSize = Mathf.Clamp(Screen.height / 30, 24, 38) };
            cardStyle = new GUIStyle(title) { fontSize = Mathf.Clamp(Screen.height / 17, 38, 66), wordWrap = true };
            hud = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Clamp(Screen.height / 43, 18, 27), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            title.normal.textColor = prompt.normal.textColor = hud.normal.textColor = new Color(0.14f, 0.08f, 0.20f);
            cardStyle.normal.textColor = Color.white;
        }

        private static void Fill(Rect rect, Color color)
        {
            Color old = GUI.color; GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}

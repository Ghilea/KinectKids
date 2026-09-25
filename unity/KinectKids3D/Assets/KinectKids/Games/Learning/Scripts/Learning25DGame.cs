using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KinectKids.Scene25D;
using KinectKids3D;
using KinectKids3D.Platform;

namespace KinectKids.Games.Learning
{
    /// <summary>
    /// 2.5D version of the four learning games (Matematikbanan, Bokstavsjakten,
    /// Formverkstan, Mönsterjakten).
    ///
    /// The proven question generation, voice sequencing and missed-question queue
    /// are preserved exactly (same voice resource keys, same difficulty ramp).
    /// The only change is presentation: answers are shown as depth-scaled sprite
    /// cards floating toward the camera in a themed dark-storybook 2.5D scene,
    /// selected with the Kinect hand in world space. Gameplay stays fully
    /// decoupled from art, so final card/background sprites can replace the
    /// placeholders without touching rules.
    /// </summary>
    public sealed class Learning25DGame : MonoBehaviour
    {
        private sealed class Question
        {
            public string Prompt;
            public string Visual;
            public string Correct;
            public string[] Options;
            public string Voice;
        }

        private sealed class AnswerCard
        {
            public string Answer;
            public GameObject Go;
            public SpriteRenderer Panel;
            public CameraDepthScaler Scaler;
            public float Lane;
            public float Depth;
            public float Speed;
            public int ColorIndex;
        }

        private readonly System.Random random = new System.Random();
        private readonly List<AnswerCard> cards = new List<AnswerCard>();
        private readonly Queue<Question> missed = new Queue<Question>();
        private readonly Dictionary<int, Vector2> lockedHands = new Dictionary<int, Vector2>();
        private readonly Color[] cardColors =
        {
            new Color(0.92f, 0.25f, 0.37f), new Color(0.98f, 0.59f, 0.12f),
            new Color(0.10f, 0.66f, 0.48f), new Color(0.18f, 0.49f, 0.84f)
        };

        private LearningGameMode mode;
        private bool configured;
        private Question question;
        private AudioSource voice;
        private Camera cam;
        private ScriptedCamera scriptedCamera;
        private ParallaxController parallax;

        private float nextQuestionAt;
        private float inputArmedAt;
        private float roundEndsAt;
        private float messageUntil;
        private string message = "";
        private int score;
        private int answered;
        private int questionsSinceMiss;
        private bool finished;

        private GUIStyle title;
        private GUIStyle prompt;
        private GUIStyle hud;

        public void Configure(LearningGameMode value)
        {
            mode = value;
            configured = true;
        }

        private void Start()
        {
            if (!configured) mode = LearningGameMode.Math;
            if (KinectKidsInputManager.Instance == null)
                new GameObject("KinectKidsInputManager").AddComponent<KinectKidsInputManager>();

            var theme = Scene25DBackdrop.Theme.Tinted(AccentColor());
            cam = Scene25DBackdrop.BuildCamera(transform, theme.sky, out scriptedCamera);
            parallax = Scene25DBackdrop.Build(transform, theme);
            parallax.forwardSpeed = 0.6f; // gentle drift

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
                AnswerCard card = cards[i];
                card.Depth += card.Speed * delta;
                float x = card.Lane * Mathf.Lerp(1.0f, 3.6f, card.Depth);
                card.Go.transform.localPosition = new Vector3(x, card.Go.transform.localPosition.y, card.Go.transform.localPosition.z);
                card.Scaler.Apply(card.Depth);
            }

            // Keyboard 1-4 fallback.
            for (int i = 0; i < cards.Count; i++)
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))
                    || Input.GetKeyDown((KeyCode)((int)KeyCode.Keypad1 + i)))
                { Choose(cards[i], -1, Vector2.zero); return; }

            ProcessHands();

            AnswerCard correct = cards.FirstOrDefault(item => item.Answer == question.Correct);
            if (correct != null && correct.Depth >= 1f) MissQuestion();
        }

        private void ProcessHands()
        {
            KinectKidsInputManager input = KinectKidsInputManager.Instance;
            if (input == null || cam == null) return;
            bool kinect = input.KinectConnected;
            foreach (AimSample sample in input.AimSamples)
            {
                Vector2 point = sample.Position;
                if (lockedHands.TryGetValue(sample.HandId, out Vector2 locked))
                {
                    if (Vector2.Distance(point, locked) < 0.09f) continue;
                    lockedHands.Remove(sample.HandId);
                }
                if (Time.unscaledTime < inputArmedAt) continue;
                if (!kinect && !sample.Fire) continue;
                AnswerCard hit = CardAt(ScreenToScene(point));
                if (hit == null) continue;
                Choose(hit, sample.HandId, point);
                return;
            }
        }

        private Vector3 ScreenToScene(Vector2 normalized)
        {
            float h = cam.orthographicSize;
            float w = h * cam.aspect;
            float x = (normalized.x - 0.5f) * 2f * w + cam.transform.position.x;
            float y = (normalized.y - 0.5f) * 2f * h + cam.transform.position.y;
            return new Vector3(x, y, 0f);
        }

        private AnswerCard CardAt(Vector3 world)
        {
            AnswerCard best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < cards.Count; i++)
            {
                AnswerCard c = cards[i];
                if (c.Depth < 0.12f) continue;
                float radius = Mathf.Lerp(0.6f, 1.8f, c.Depth);
                float d = Vector2.Distance(new Vector2(world.x, world.y),
                    new Vector2(c.Go.transform.position.x, c.Go.transform.position.y));
                if (d <= radius && d < bestDist) { bestDist = d; best = c; }
            }
            return best;
        }

        private void Choose(AnswerCard card, int hand, Vector2 point)
        {
            if (hand >= 0) lockedHands[hand] = point;
            if (card.Answer == question.Correct)
            {
                score += 10 + Mathf.Min(10, answered);
                answered++;
                questionsSinceMiss++;
                Message("RÄTT!");
                Speak("correct");
                ClearCards();
                question = null;
                nextQuestionAt = Time.unscaledTime + 1.15f;
            }
            else
            {
                score = Mathf.Max(0, score - 3);
                RemoveCard(card);
                Message("PROVA IGEN");
                Speak("retry");
            }
        }

        private void MissQuestion()
        {
            missed.Enqueue(question);
            questionsSinceMiss = 0;
            Message("SVARET KOMMER IGEN SENARE");
            Speak("missed_answer");
            ClearCards();
            question = null;
            nextQuestionAt = Time.unscaledTime + 1.35f;
        }

        private void CreateQuestion()
        {
            bool retry = missed.Count > 0 && questionsSinceMiss >= 2;
            question = retry ? missed.Dequeue() : GenerateQuestion();
            if (retry) questionsSinceMiss = 0;
            else if (missed.Count > 0) questionsSinceMiss++;
            ClearCards();

            float[] lanes = { -0.75f, -0.25f, 0.25f, 0.75f };
            lanes = lanes.OrderBy(_ => random.Next()).ToArray();
            for (int i = 0; i < question.Options.Length; i++)
                cards.Add(CreateCard(question.Options[i], lanes[i], i));

            inputArmedAt = Time.unscaledTime + 0.65f;
            Speak(question.Voice);
        }

        private AnswerCard CreateCard(string answer, float lane, int index)
        {
            var go = new GameObject("Card_" + answer);
            go.transform.SetParent(transform, false);
            var scaler = go.AddComponent<CameraDepthScaler>();
            scaler.band = SceneBand.Actors;
            scaler.farScale = 0.4f;
            scaler.nearScale = 2.0f;
            scaler.farY = 1.6f;
            scaler.nearY = -1.8f;

            var panel = PlaceholderArt.NewSpriteObject("Panel", PlaceholderArt.SolidBlock(),
                cardColors[index % cardColors.Length], go.transform, SceneBand.Actors, 0.5f);
            panel.transform.localScale = new Vector3(1.4f, 1.0f, 1f);

            go.transform.localPosition = new Vector3(lane, 0, LayerSorting.BandZ(SceneBand.Actors));
            return new AnswerCard
            {
                Answer = answer, Go = go, Panel = panel, Scaler = scaler,
                Lane = lane, Depth = 0f,
                Speed = 0.14f + Mathf.Min(0.07f, answered * 0.0036f), ColorIndex = index
            };
        }

        private void RemoveCard(AnswerCard card)
        {
            Destroy(card.Go);
            cards.Remove(card);
        }

        private void ClearCards()
        {
            for (int i = 0; i < cards.Count; i++) if (cards[i].Go != null) Destroy(cards[i].Go);
            cards.Clear();
        }

        // --- question generation (preserved from LearningChoiceGameUnity) ---

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
                new[] { "A  B  A  B  ?", "A" }, new[] { "LITEN  STOR  LITEN  STOR  ?", "LITEN" }
            };
            int index = random.Next(patterns.Length);
            string answer = patterns[index][1];
            string[] pool = { "●", "■", "▲", "★", "A", "B", "F", "2", "5", "9", "10", "LITEN", "STOR" };
            string[] options = pool.Where(x => x != answer).OrderBy(_ => random.Next()).Take(3)
                .Concat(new[] { answer }).OrderBy(_ => random.Next()).ToArray();
            return new Question { Prompt = "Vad kommer sedan?", Visual = patterns[index][0], Correct = answer,
                Options = options, Voice = "pattern_" + (index % 6) };
        }

        private static string WordKey(string word) => word.ToLowerInvariant()
            .Replace("å", "a").Replace("ä", "a").Replace("ö", "o");

        // --- voice (preserved) ---

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

        private void Message(string text)
        {
            message = text;
            messageUntil = Time.unscaledTime + 1.0f;
        }

        private void Restart()
        {
            score = answered = questionsSinceMiss = 0;
            missed.Clear(); ClearCards(); question = null; finished = false;
            roundEndsAt = Time.unscaledTime + 90f;
            nextQuestionAt = Time.unscaledTime + 0.3f;
        }

        // --- HUD (labels only; the world is rendered by sprites) ---

        private void OnGUI()
        {
            EnsureStyles();
            DrawCardLabels();
            DrawCursors();

            Fill(new Rect(0, 0, Screen.width, 200), new Color(0.05f, 0.05f, 0.12f, 0.72f));
            GUI.Label(new Rect(20, 10, Screen.width - 40, 60), TitleText(), title);
            GUI.Label(new Rect(30, 70, 300, 36), "POÄNG: " + score, hud);
            GUI.Label(new Rect(Screen.width - 320, 70, 300, 36),
                "TID: " + Mathf.CeilToInt(Mathf.Max(0, roundEndsAt - Time.unscaledTime)), hud);
            if (question != null)
            {
                GUI.Label(new Rect(Screen.width * 0.16f, 96, Screen.width * 0.68f, 44), question.Prompt, prompt);
                GUI.Label(new Rect(Screen.width * 0.10f, 134, Screen.width * 0.80f, 64), question.Visual, title);
            }

            if (Time.unscaledTime < messageUntil)
                GUI.Label(new Rect(0, Screen.height * 0.72f, Screen.width, 70), message, title);

            if (finished)
            {
                Fill(new Rect(Screen.width * 0.2f, Screen.height * 0.3f, Screen.width * 0.6f, Screen.height * 0.36f),
                    new Color(0.05f, 0.05f, 0.12f, 0.92f));
                GUI.Label(new Rect(0, Screen.height * 0.34f, Screen.width, 80), "BRA JOBBAT! " + score + " POÄNG", title);
                if (GUI.Button(new Rect(Screen.width * 0.3f, Screen.height * 0.56f, Screen.width * 0.4f, 60), "SPELA IGEN (R)")) Restart();
                if (GUI.Button(new Rect(Screen.width * 0.3f, Screen.height * 0.66f, Screen.width * 0.4f, 60), "TILL SPELMENYN"))
                {
                    if (!KinectKidsStoryMode.ContinueAfter(StorySceneName()))
                        KinectKidsPlatformRoot.Instance.Scenes.LoadMenu();
                }
            }
        }

        private void DrawCardLabels()
        {
            if (cam == null) return;
            for (int i = 0; i < cards.Count; i++)
            {
                AnswerCard c = cards[i];
                Vector3 vp = cam.WorldToViewportPoint(c.Go.transform.position);
                if (vp.z < 0) continue;
                float scale = Mathf.Lerp(0.4f, 2.0f, c.Depth);
                float size = 60f * scale;
                Rect r = new Rect(vp.x * Screen.width - size, (1f - vp.y) * Screen.height - size * 0.5f, size * 2f, size);
                var style = new GUIStyle(title) { fontSize = Mathf.Clamp((int)(size * 0.9f), 20, 90) };
                style.normal.textColor = Color.white;
                GUI.Label(r, c.Answer, style);
            }
        }

        private void DrawCursors()
        {
            KinectKidsInputManager input = KinectKidsInputManager.Instance;
            if (input == null) return;
            foreach (AimSample sample in input.AimSamples)
            {
                Vector2 p = new Vector2(sample.Position.x * Screen.width, (1f - sample.Position.y) * Screen.height);
                Color color = (sample.HandId & 1) == 0
                    ? new Color(0.10f, 0.72f, 0.92f, 0.85f)
                    : new Color(0.96f, 0.30f, 0.52f, 0.85f);
                Fill(new Rect(p.x - 18, p.y - 18, 36, 36), color);
            }
        }

        private string TitleText() => mode == LearningGameMode.Math ? "MATEMATIKBANAN"
            : mode == LearningGameMode.Swedish ? "BOKSTAVSJAKTEN"
            : mode == LearningGameMode.Shapes ? "FORMVERKSTAN" : "MÖNSTERJAKTEN";

        private string StorySceneName() => mode == LearningGameMode.Math ? "Matematikbanan"
            : mode == LearningGameMode.Swedish ? "Bokstavsjakten"
            : mode == LearningGameMode.Shapes ? "Formverkstan" : "Monsterjakten";

        private Color AccentColor() => mode == LearningGameMode.Math ? new Color(0.10f, 0.48f, 0.86f)
            : mode == LearningGameMode.Swedish ? new Color(0.92f, 0.35f, 0.18f)
            : mode == LearningGameMode.Shapes ? new Color(0.10f, 0.62f, 0.36f) : new Color(0.58f, 0.22f, 0.78f);

        private void EnsureStyles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Clamp(Screen.height / 16, 38, 70), fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            prompt = new GUIStyle(title) { fontSize = Mathf.Clamp(Screen.height / 30, 24, 38) };
            hud = new GUIStyle(GUI.skin.label) { fontSize = Mathf.Clamp(Screen.height / 43, 18, 27), fontStyle = FontStyle.Bold };
            title.normal.textColor = prompt.normal.textColor = new Color(0.95f, 0.95f, 1f);
            hud.normal.textColor = new Color(0.9f, 0.9f, 1f);
        }

        private static void Fill(Rect rect, Color color)
        {
            Color old = GUI.color; GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}

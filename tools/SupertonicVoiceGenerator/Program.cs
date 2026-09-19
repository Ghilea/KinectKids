using SherpaOnnx;

if (args.Length < 2)
{
    Console.Error.WriteLine("Usage: SupertonicVoiceGenerator <model-directory> <output-directory> [speaker-id] [--all-prompts|--menu-prompts]");
    return 2;
}

string modelDirectory = Path.GetFullPath(args[0]);
string outputDirectory = Path.GetFullPath(args[1]);
Directory.CreateDirectory(outputDirectory);

var config = new OfflineTtsConfig();
config.Model.Supertonic.DurationPredictor = Path.Combine(modelDirectory, "duration_predictor.int8.onnx");
config.Model.Supertonic.TextEncoder = Path.Combine(modelDirectory, "text_encoder.int8.onnx");
config.Model.Supertonic.VectorEstimator = Path.Combine(modelDirectory, "vector_estimator.int8.onnx");
config.Model.Supertonic.Vocoder = Path.Combine(modelDirectory, "vocoder.int8.onnx");
config.Model.Supertonic.TtsJson = Path.Combine(modelDirectory, "tts.json");
config.Model.Supertonic.UnicodeIndexer = Path.Combine(modelDirectory, "unicode_indexer.bin");
config.Model.Supertonic.VoiceStyle = Path.Combine(modelDirectory, "voice.bin");
config.Model.NumThreads = Math.Max(2, Math.Min(6, Environment.ProcessorCount / 2));
config.Model.Provider = "cpu";

using var tts = new OfflineTts(config);
int firstSpeaker = args.Length >= 3 ? int.Parse(args[2]) : 0;
int lastSpeaker = args.Length >= 3 ? firstSpeaker : tts.NumSpeakers - 1;
const string sampleText = "Välkommen till Kinect Kids! Vilken bokstav börjar ordet äpple med?";
bool generateAllPrompts = args.Any(item => item == "--all-prompts");
bool generateMenuPrompts = args.Any(item => item == "--menu-prompts");

Console.WriteLine($"Supertonic-röster: {tts.NumSpeakers}");
for (int speaker = firstSpeaker; speaker <= lastSpeaker; speaker++)
{
    var generation = new OfflineTtsGenerationConfig
    {
        Sid = speaker,
        NumSteps = 8,
        Speed = 1.16f
    };
    generation.Extra["lang"] = "sv";
    if (generateAllPrompts || generateMenuPrompts)
    {
        var prompts = generateMenuPrompts ? CreateMenuPrompts() : CreatePrompts();
        foreach (var prompt in prompts)
            Generate(tts, generation, prompt.Value, Path.Combine(outputDirectory, prompt.Key + ".wav"));
    }
    else
    {
        Generate(tts, generation, sampleText,
            Path.Combine(outputDirectory, $"supertonic-svenska-rost-{speaker + 1:00}.wav"));
    }
}

return 0;

static void Generate(OfflineTts tts, OfflineTtsGenerationConfig generation, string text, string output)
{
    var audio = tts.GenerateWithConfig(text, generation, null!);
    if (!audio.SaveToWaveFile(output))
        throw new IOException("Kunde inte skriva " + output);
    Console.WriteLine(Path.GetFileName(output));
}

static IReadOnlyDictionary<string, string> CreatePrompts()
{
    var prompts = new Dictionary<string, string>(CreateMenuPrompts())
    {
        ["prompt_math"] = "Vad blir",
        ["plus"] = "plus",
        ["minus"] = "minus",
        ["shape_circle"] = "Hitta cirkeln!",
        ["shape_triangle"] = "Hitta triangeln!",
        ["shape_square"] = "Hitta kvadraten!",
        ["shape_star"] = "Hitta stjärnan!",
        ["pattern_0"] = "Cirkel, kvadrat, cirkel, kvadrat. Vad kommer sedan?",
        ["pattern_1"] = "Triangel, triangel, stjärna, triangel, triangel. Vad kommer sedan?",
        ["pattern_2"] = "Ett, två, tre, fyra. Vad kommer sedan?",
        ["pattern_3"] = "Två, fyra, sex, åtta. Vad kommer sedan?",
        ["pattern_4"] = "A, B, A, B. Vad kommer sedan?",
        ["pattern_5"] = "Liten, stor, liten, stor. Vad kommer sedan?",
        ["simon_hands_up"] = "Simon säger: händerna över huvudet!",
        ["simon_arms_out"] = "Simon säger: armarna rakt ut!",
        ["simon_hands_together"] = "Simon säger: händerna tillsammans!",
        ["simon_duck"] = "Simon säger: ducka!",
        ["simon_ready"] = "Gör dig redo!",
        ["correct"] = "Rätt! Bra jobbat.",
        ["retry"] = "Bra försök! Prova ett annat svar!",
        ["great_next"] = "Snyggt! Nästa rörelse!",
        ["new_movement"] = "Nästan! Vi tar en ny rörelse!",
        ["balloon_instruction"] = "Rör en handring in i ballongerna för att smälla dem!"
    };

    for (int number = 0; number <= 20; number++)
        prompts["number_" + number] = number.ToString();

    var words = new Dictionary<string, string>
    {
        ["sol"] = "sol", ["mane"] = "måne", ["boll"] = "boll", ["hus"] = "hus",
        ["katt"] = "katt", ["hund"] = "hund", ["fisk"] = "fisk", ["fagel"] = "fågel",
        ["glass"] = "glass", ["banan"] = "banan", ["apple"] = "äpple", ["ost"] = "ost",
        ["bat"] = "båt", ["bil"] = "bil", ["tag"] = "tåg", ["cykel"] = "cykel"
    };
    foreach (var word in words)
    {
        prompts["starts_" + word.Key] = "Vilken bokstav börjar ordet " + word.Value + " med?";
        prompts["missing_" + word.Key] = "Vilken bokstav saknas i ordet " + word.Value + "?";
    }
    return prompts;
}

static Dictionary<string, string> CreateMenuPrompts() => new Dictionary<string, string>
{
    ["menu_math"] = "Matematikbanan!",
    ["menu_swedish"] = "Bokstavsjakten!",
    ["menu_shapes"] = "Formverkstan!",
    ["menu_patterns"] = "Mönsterjakten!",
    ["menu_simon"] = "Simon säger!",
    ["menu_balloons"] = "Ballongjakten!",
    ["menu_spooky"] = "Spökjakten!"
};

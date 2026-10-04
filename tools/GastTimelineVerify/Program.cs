using KinectKids.Games.GreveGast;
using UnityEngine;

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine("PASS " + name);
}

var dodgeHistory = new GastDodgeHistory();
dodgeHistory.Record(GastGameplayCommand.Slide, 10f);
dodgeHistory.Record(GastGameplayCommand.LaneLeft, 10.1f);
Check(dodgeHistory.InWindow(GastGameplayCommand.Slide, 9.8f, 10.5f, 10.2f),
    "sidestep after duck preserves the timed duck response");
dodgeHistory.Record(GastGameplayCommand.Jump, 10.2f);
Check(dodgeHistory.InWindow(GastGameplayCommand.Slide, 9.8f, 10.5f, 10.3f) &&
    dodgeHistory.InWindow(GastGameplayCommand.Jump, 10.1f, 10.7f, 10.3f),
    "jump after duck records both responses independently");
Check(!dodgeHistory.InWindow(GastGameplayCommand.Slide, 10.1f, 10.7f, 10.3f),
    "old duck cannot satisfy a later obstacle outside its window");
Check(!dodgeHistory.InWindow(GastGameplayCommand.Jump, 10.1f, 10.7f, 10.15f),
    "future movement cannot satisfy an earlier song position");
dodgeHistory.Reset();
Check(!dodgeHistory.InWindow(GastGameplayCommand.Slide, 9.8f, 10.5f, 10.3f) &&
    !dodgeHistory.InWindow(GastGameplayCommand.Jump, 10.1f, 10.7f, 10.3f),
    "seek or player change clears previous movement responses");

var song = new GastSongAsset();
var jump = new GastSongCue { id = "jump", vocalTimeSeconds = 10f,
    hasVocalTime = true, timingStatus = GastTimingStatus.Estimated,
    gameplayCommand = true, command = GastGameplayCommand.Jump };
song.cues.Add(jump);
song.cues.Add(new GastSongCue { id = "untimed", gameplayCommand = true });
var timeline = new GastCueTimeline(song);
var events = new List<string>();
timeline.Cue += (cue, phase) => events.Add(cue.id + ":" + phase);
timeline.Tick(0f, 9f);
Check(events.SequenceEqual(new[] { "jump:warning" }), "warn before the sung command");
timeline.Tick(9f, 10f);
Check(events.SequenceEqual(new[] { "jump:warning", "jump:gesture", "jump:window-open", "jump:impact" }),
    "skipped frames dispatch phases in time order, impact at the command");
timeline.Tick(10f, 10.4f);
Check(!events.Contains("jump:result"), "late response window stays open after impact");
timeline.Tick(10.4f, 11f);
Check(events.Last() == "jump:result" && events.Count == 5, "result after the complete response window");
timeline.Tick(11f, 12f);
timeline.Tick(0f, 12f);
Check(events.Count == 5, "phases fire once and untimed commands do not fire");

events.Clear();
timeline.RestoreAt(9.8f);
timeline.Tick(9.8f, 11f);
Check(events.SequenceEqual(new[] { "jump:impact", "jump:result" }), "seek does not replay expired warnings");
events.Clear();
timeline.RestoreAt(0f);
timeline.Tick(0f, 11f);
Check(events.Count == 5, "rewind re-arms commands");

events.Clear();
song.globalOffsetSeconds = 1f;
jump.sectionId = "chorus";
jump.cueOffsetSeconds = 0.25f;
song.sections.Add(new GastSongSection { id = "chorus", offsetSeconds = 0.5f });
timeline.Reset();
timeline.Tick(0f, 10f);
Check(events.Count == 0, "global, section and cue offsets shift warnings together");
timeline.Tick(10f, 11.75f);
Check(events.Last() == "jump:impact", "offset impact follows the same clock");

var earlySong = new GastSongAsset();
earlySong.cues.Add(new GastSongCue { id = "early-jump", vocalTimeSeconds = 10f,
    hasVocalTime = true, timingStatus = GastTimingStatus.Estimated,
    gameplayCommand = true, impactOffsetSeconds = -0.2f });
var earlyTimeline = new GastCueTimeline(earlySong);
var earlyEvents = new List<string>();
earlyTimeline.Cue += (_, phase) => earlyEvents.Add(phase);
earlyTimeline.Tick(0f, 9.79f);
Check(!earlyEvents.Contains("impact"), "earlier encounter still waits for its adjusted time");
earlyTimeline.Tick(9.79f, 9.81f);
Check(earlyEvents.Last() == "impact", "negative impact offset advances encounter by 0.2 seconds");
earlyTimeline.Tick(9.81f, 10.29f);
Check(!earlyEvents.Contains("result"), "earlier encounter retains the late response window");
earlyTimeline.Tick(10.29f, 10.31f);
Check(earlyEvents.Last() == "result" && earlyEvents.Count == 5, "adjusted encounter resolves once after its window");

var clip = new AudioClip();
var source = new AudioSource();
var clock = new GastSongClock(source, clip);
clock.PlayFrom(0f);
source.isPlaying = false;
Check(clock.TimeSeconds == 0f, "streaming startup does not jump to the end of the song");
clock.PlayFrom(2f);
source.timeSamples = 144000;
Check(clock.TimeSeconds == 3f, "clock follows the audio sample cursor");
clock.Pause();
Check(clock.TimeSeconds == 3f && !clock.IsRunning, "pause preserves sample position");
clock.Resume();
source.timeSamples = 192000;
Check(clock.TimeSeconds == 4f, "resume continues sample position");
source.timeSamples = clip.samples - 240;
Check(clock.TimeSeconds > clip.length - 0.01f, "clock observes the final audio samples");
source.isPlaying = false;
source.timeSamples = 0;
Check(clock.TimeSeconds == clip.length, "clip end holds final time instead of rewinding");
clock.Stop();
Check(clock.TimeSeconds == 0f, "explicit stop resets the clock");

// Exercise the shipped command data, rather than a second hand-written schedule.
string assetPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
    "../../../../../unity/KinectKids3D/Assets/Resources/GastSong.asset"));
string assetText = File.ReadAllText(assetPath);
string introFields = System.Text.RegularExpressions.Regex.Match(assetText,
    @"(?ms)^  - id: intro_spring_01\r?\n(?<fields>.*?)(?=^  - id: |\z)").Groups["fields"].Value;
float IntroField(string name) => float.Parse(System.Text.RegularExpressions.Regex.Match(
    introFields, @"(?m)^    " + name + @": ([-\d.]+)").Groups[1].Value,
    System.Globalization.CultureInfo.InvariantCulture);
var introSong = new GastSongAsset();
introSong.cues.Add(new GastSongCue { id = "intro_spring_01", hasVocalTime = true,
    timingStatus = GastTimingStatus.Estimated, gameplayCommand = false,
    vocalTimeSeconds = IntroField("vocalTimeSeconds"), gestureLeadSeconds = IntroField("gestureLeadSeconds"),
    action = GastCueAction.ChaseLaunch, command = GastGameplayCommand.RunStart });
Check(introSong.cues[0].EffectiveTime(introSong) == 21f, "shipped SPRING marker is at 21 seconds");
var introTimeline = new GastCueTimeline(introSong);
var introEvents = new List<string>();
introTimeline.Cue += (_, phase) => introEvents.Add(phase);
introTimeline.Tick(0f, 20.99f);
Check(introEvents.Count == 0, "SPRING gesture does not interrupt idle early");
introTimeline.Tick(20.99f, 21f);
Check(introEvents.SequenceEqual(new[] { "gesture" }), "SPRING gesture starts on its sung marker");
introTimeline.Tick(21f, 22f);
Check(introEvents.Count == 1, "SPRING gesture fires once without gameplay hazards");
var authoredSong = new GastSongAsset();
foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(
    assetText, @"(?ms)^  - id: (command_[^\r\n]+)\r?\n(?<fields>.*?)(?=^  - id: |\z)"))
{
    string fields = match.Groups["fields"].Value;
    float Read(string name) => float.Parse(System.Text.RegularExpressions.Regex.Match(
        fields, @"(?m)^    " + name + @": ([-\d.]+)").Groups[1].Value,
        System.Globalization.CultureInfo.InvariantCulture);
    authoredSong.cues.Add(new GastSongCue { id = match.Groups[1].Value,
        hasVocalTime = true, timingStatus = GastTimingStatus.Estimated, gameplayCommand = true,
        vocalTimeSeconds = Read("vocalTimeSeconds"), warningLeadSeconds = Read("warningLeadSeconds"),
        gestureLeadSeconds = Read("gestureLeadSeconds"), impactOffsetSeconds = Read("impactOffsetSeconds"),
        windowBeforeSeconds = Read("windowBeforeSeconds"), windowAfterSeconds = Read("windowAfterSeconds") });
}
Check(authoredSong.cues.Count == 4, "shipped timeline contains all four command cues");
var authoredTimeline = new GastCueTimeline(authoredSong);
var authoredEvents = new List<string>();
authoredTimeline.Cue += (cue, phase) => authoredEvents.Add(cue.id + ":" + phase);
float playhead = 0f;
float previousImpact = 0f;
foreach (var cue in authoredSong.cues)
{
    float vocal = cue.vocalTimeSeconds;
    Check(vocal + cue.impactOffsetSeconds > previousImpact,
        cue.id + " keeps encounter order after timing edits");
    Check(vocal + cue.impactOffsetSeconds - cue.windowBeforeSeconds <= vocal,
        cue.id + " accepts movement on the sung word");
    authoredTimeline.Tick(playhead, vocal);
    Check(!authoredEvents.Contains(cue.id + ":impact"), cue.id + " has not passed on its sung word");
    playhead = vocal + cue.impactOffsetSeconds + 0.01f;
    authoredTimeline.Tick(vocal, playhead);
    Check(authoredEvents.Contains(cue.id + ":impact"), cue.id + " arrives after its sung word");
    previousImpact = vocal + cue.impactOffsetSeconds;
}
authoredTimeline.Tick(playhead, playhead + 1f);
Check(authoredEvents.Count(e => e.EndsWith(":result")) == 4,
    "each shipped command resolves exactly once");

string editorSourcePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
    "../../../../../unity/KinectKids3D/Assets/Editor/GastSongEditor.cs"));
string editorSource = File.ReadAllText(editorSourcePath);
var sourceTimes = System.Text.RegularExpressions.Regex.Matches(editorSource,
    "AddEstimatedCommand\\(asset,\\s*\"(?<id>command_[^\"]+)\"[^\\r\\n]*?,\\s*(?<time>\\d+(?:\\.\\d+)?)f,\\s*GastCueAction");
Check(sourceTimes.Count == 4, "command vocal times are defined once in GastSongEditor.cs");
foreach (System.Text.RegularExpressions.Match sourceTime in sourceTimes)
{
    string id = sourceTime.Groups["id"].Value;
    float time = float.Parse(sourceTime.Groups["time"].Value, System.Globalization.CultureInfo.InvariantCulture);
    Check(authoredSong.cues.Single(c => c.id == id).vocalTimeSeconds == time,
        id + " saved asset matches the authoritative source time");
}

foreach (var command in new[] { GastGameplayCommand.Slide, GastGameplayCommand.LaneLeft, GastGameplayCommand.LaneRight })
{
    var editedSong = new GastSongAsset();
    var editedCue = new GastSongCue { id = "edited", vocalTimeSeconds = 10f,
        hasVocalTime = true, timingStatus = GastTimingStatus.Estimated, gameplayCommand = true,
        command = command };
    editedSong.cues.Add(editedCue);
    var editedTimeline = new GastCueTimeline(editedSong);
    var editedEvents = new List<string>();
    editedTimeline.Cue += (_, phase) => editedEvents.Add(phase);
    editedTimeline.Tick(0f, 9.5f);
    Check(editedEvents.Contains("warning"), command + " already has a warned obstacle before editing");
    editedCue.vocalTimeSeconds += 5f;
    Check(editedTimeline.RefreshAt(9.5f), command + " five-second edit requests obstacle rebuild");
    Check(!editedTimeline.RefreshAt(9.5f), command + " unchanged data does not repeatedly rebuild");
    editedTimeline.Tick(9.5f, 10.5f);
    Check(!editedEvents.Contains("impact"), command + " old encounter is cancelled");
    editedTimeline.Tick(10.5f, 15.6f);
    Check(editedEvents.Count(e => e == "warning") == 2 && editedEvents.Count(e => e == "impact") == 1,
        command + " warning is re-armed and encounter moves five seconds later");
    editedCue.impactOffsetSeconds += 1f;
    Check(editedTimeline.RefreshAt(15.6f), command + " impact-only change also rebuilds");
}

for (int lane = 0; lane < 3; lane++)
{
    Check((GastRunnerRules.BlockedLanes(GastGameplayCommand.Jump) & (1 << lane)) != 0 &&
        (GastRunnerRules.BlockedLanes(GastGameplayCommand.Slide) & (1 << lane)) != 0,
        "jump and duck visibly block lane " + lane);
    Check(!GastRunnerRules.SongResponse(GastGameplayCommand.Jump, lane, false, true) &&
        !GastRunnerRules.SongResponse(GastGameplayCommand.Slide, lane, true, false),
        "changing lane cannot bypass jump or duck in lane " + lane);
    Check(GastRunnerRules.SongResponse(GastGameplayCommand.Jump, lane, true, false) &&
        GastRunnerRules.SongResponse(GastGameplayCommand.Slide, lane, false, true),
        "matching timed jump and duck succeed in lane " + lane);
    Check(GastRunnerRules.NextLane(lane, -1, true) == 0 && GastRunnerRules.NextLane(lane, 1, true) == 2,
        "one song-side gesture reaches the named safe lane from lane " + lane);
}
Check(GastRunnerRules.BlockedLanes(GastGameplayCommand.LaneLeft) == 6 &&
    GastRunnerRules.BlockedLanes(GastGameplayCommand.LaneRight) == 3, "side commands leave only the named edge lane open");
Check(GastRunnerRules.SongResponse(GastGameplayCommand.LaneLeft, 0, false, false) &&
    GastRunnerRules.SongResponse(GastGameplayCommand.LaneRight, 2, false, false),
    "already at the named edge can hold safely without an impossible extra step");
Check(!GastRunnerRules.SongResponse(GastGameplayCommand.LaneLeft, 1, false, false) &&
    !GastRunnerRules.SongResponse(GastGameplayCommand.LaneRight, 0, false, false),
    "wrong-side or middle lane cannot satisfy a side command");
Check(GastRunnerRules.NextLane(0, 1, false) == 1 && GastRunnerRules.NextLane(0, -1, false) == 0,
    "free movement retains single steps and clamps the corridor edges");

var guardedSong = new GastSongAsset();
guardedSong.cues.Add(new GastSongCue { id = "guard", vocalTimeSeconds = 10f,
    hasVocalTime = true, gameplayCommand = true, timingStatus = GastTimingStatus.Estimated,
    warningLeadSeconds = 0.65f, impactOffsetSeconds = 0.35f });
Check(GastRunnerRules.CanSpawnRandom(guardedSong, 3f, 1.8f, 0.8f), "random encounters fill quiet parts of the song");
Check(!GastRunnerRules.CanSpawnRandom(guardedSong, 8f, 1.8f, 0.8f),
    "random spawn is rejected when its travel and recovery overlap the next warning");
Check(!GastRunnerRules.CanSpawnRandom(guardedSong, 10f, 1.8f, 0.8f), "song encounter takes priority over random obstacles");
Check(GastRunnerRules.CanSpawnRandom(guardedSong, 12f, 1.8f, 0.8f), "random encounters resume after song recovery");
guardedSong.cues[0].cueOffsetSeconds = 5f;
Check(GastRunnerRules.CanSpawnRandom(guardedSong, 8f, 1.8f, 0.8f) &&
    !GastRunnerRules.CanSpawnRandom(guardedSong, 13f, 1.8f, 0.8f),
    "random protection follows edited song offsets");
guardedSong.cues[0].timingStatus = GastTimingStatus.Untimed;
Check(GastRunnerRules.CanSpawnRandom(guardedSong, 15f, 1.8f, 0.8f), "untimed cues do not suppress random play");

Check(GastRunnerRules.PlayerLane(0f, 4.2f) == 1, "random obstacle targets centre when the player is centred");
Check(GastRunnerRules.PlayerLane(-4.2f, 4.2f) == 0 && GastRunnerRules.PlayerLane(4.2f, 4.2f) == 2,
    "random obstacle targets either edge when the player is there");
Check(GastRunnerRules.PlayerLane(-1.7f, 4.2f) == 1 && GastRunnerRules.PlayerLane(1.7f, 4.2f) == 1,
    "small player offsets do not create unrelated side-lane obstacles");
Check(GastRunnerRules.PlayerLane(-100f, 4.2f) == 0 && GastRunnerRules.PlayerLane(100f, 4.2f) == 2,
    "random target remains inside the playable lanes");
guardedSong.cues[0].timingStatus = GastTimingStatus.Estimated;
guardedSong.cues[0].cueOffsetSeconds = 0f;
Check(!GastRunnerRules.CanSpawnRandom(guardedSong, 6f, 0.8f + 2.4f, 0.8f),
    "random protection includes both advance warning and visible travel");
Check(GastRunnerRules.CanSpawnRandom(guardedSong, 5f, 0.8f + 2.4f, 0.8f),
    "the longer random encounter still fits sufficiently early in a quiet gap");

using KinectKids.Games.GreveGast;
using UnityEngine;

static void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine("PASS " + name);
}

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

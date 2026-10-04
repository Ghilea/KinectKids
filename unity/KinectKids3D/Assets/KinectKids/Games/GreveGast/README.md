# Greve Gast module

The active platform scene now boots the **2.5D layered chase**
(`ChaseSceneDirector`) described in `new-way.md`. The player runs toward the
camera while Greve Gast floats behind and chases; the world is built from
parallax layers and modular hazards using placeholder art that final sprites
can replace without touching gameplay.

## Scripts (`Scripts/`)

| Script | Role |
|---|---|
| `GreveGastSceneEntry` | Platform scene entry. Boots `ChaseSceneDirector` by default; set `useLegacySlice = true` to boot the old slice instead. |
| `ChaseSceneEntry` | Standalone booter for the 2.5D chase (adds a `ChaseSceneDirector`). |
| `ChaseSceneDirector` | Assembles and runs the chase: parallax world, player + Greve Gast puppets, hazards, chase distance, scripted camera. Implements `IDodgeSource` on top of the shared Kinect input. |
| `ChaseEnvironmentBuilder` | Builds the layered environment (sky, background, side walls, floor, foreground, fog) from `KinectKids.Scene25D` parallax layers. |
| `GreveGastStyleCGame` | **Legacy** immediate-mode Style C+ slice, kept as a fallback. |

The reusable 2.5D systems live in `Assets/KinectKids/Core/Scene25D` – see the
README there for the full pipeline and how to swap placeholders for final art.

## Chase scope (first pass)

- player runs toward the camera; run to gain distance, stand still and Greve
  Gast closes in;
- Kinect / keyboard input maps to duck, jump and weave left/right;
- modular hazards (low beam = duck, falling / side object = weave, obstacle =
  jump) travel toward the camera and check the right dodge;
- chase meter, hit/avoid feedback, catch recovery and camera shake;
- common platform pause and return-to-menu flow.

## Authored chase animation

The active `CorridorRunnerDirector` loads numbered frames from
`Resources/GreveChase`: `player_run_*`, `player_jump_*`, `player_side_*` and
`player_duck_*`, `player_hit_*`, `player_recover_*`, `greve_sing_*`,
`greve_fly_*` and `greve_duck_react_*`. The player's run, jump, duck, lane
change, fall and recovery use real frame sequences; lane movement
and its animation finish on the same frame, and one side sequence is mirrored
for the opposite direction. Greve Gast sings at distance, visibly flies closer,
and performs a comic reaction while ducking gives him extra chase distance. He
then transitions back to the old sheet's `chase_near` and extended-hand `reach`
poses when close. His world-space size also grows with chase distance, while the
player jump combines its sprite sequence with a full vertical arc. Jump, duck
and lane gestures are one-shot commands: the full movement completes after the
Kinect pose or key first triggers it. The corridor continues through player
animations. Song obstacles use absolute audio positions independently of
world speed, so a duck, recovery or skipped frame cannot delay their arrival.
The newly cut `new_*` environment sprites supply corridor decorations, fog and
the illustrated kitchen vista. Missing sequences still fall back to the
earlier single-pose sprites.

## Song timeline (work in progress)

The active runner uses `Resources/Audio/Music/GreveGastsJakt.wav` as its
non-looping master. The editor creates `Assets/Resources/GastSong.asset` and
the `KinectKids/Greve Gast/Cue Editor` window edits sections, cue timing,
global/section/cue offsets, vocal marks and lead windows. Runtime cue position
is read from `AudioSource.timeSamples / clip.frequency`; only cues with a vocal
time and a non-`Untimed` status are dispatched. The 21.5 s SPRING mark is an
estimate from Dennis and is visibly labelled as such. The first refrain's
HOPPA, DUCKA, VÄNSTER and HÖGER cues use local word-alignment estimates
(89.435, 91.295, 91.792 and 92.686 s), all labelled Estimated and adjusted
by the user during playtesting. Two local Whisper
passes on the actual WAV placed the command group around 89–93 s; the former
legacy-plan times (64.3, 69.8, 75.3 and 80.8 s) were in the preceding verse.
These estimates still require an audible in-scene check before marking them
VerifiedByListening. They create a warned obstacle,
evaluate the matching jump/slide/screen-lane input once in its editable timing
window, and play a short Gast reaction. Existing keyboard and Kinect controls
operate the same runner and are used for cue grading.
Random obstacles fill gaps between song commands. Each timed command displays an
existing action icon: `haz_20` (jump), `haz_21` (duck), `haz_18` (screen left)
or `haz_19` (screen right). The HUD no longer lists movement instructions as
text. Obstacles reach the player at the vocal mark by default; an editable
impact offset can deliberately shift that point. The first four command
encounters currently use +0.35 s, so the obstacle arrives after the sung command
and leaves time to react. Their warning lead is 0.65 s. Closely spaced edited
commands can have overlapping warnings; the earliest encounter owns the prompt.
Matching input is latched
through the full response window, and the result is evaluated at its end.
These four cues accept input from 0.35 s before to 0.85 s after the vocal mark.
Their prompts advance shortly after impact, independently of the later grading.

Song jumps and ducks block all three lanes and require the matching timed
movement regardless of the player's lane. Side commands block the two lanes
opposite the named safe edge: left leaves only the left lane open, right only
the right lane. A player already at that edge can hold their position. While a
song-side command is active, a matching keyboard or Kinect direction moves to
that edge in one animation, even from the opposite edge; free movement still
uses ordinary one-lane steps. Side commands check actual position at impact,
so the next command cannot retroactively fail the previous one.

Random encounters choose a jump obstacle, overhead duck beam or side-step
obstacle in the player's actual lane at warning time. The lane remains fixed
after that, allowing the player to dodge without being followed. The spawn
interval is 4–6 s with only one unresolved random encounter at a time.
A movement icon appears over the threatened lane 0.8 s before the obstacle is
activated. The obstacle then enters from the camera-side floor edge, starting
behind the near clipping plane with 2.4 s travel to the player, rather than
popping into the middle of the view. Random duck beams are kept below the
camera's eye level so they also enter naturally from the bottom of the view.
Their advance warning, complete travel plus 0.8 s recovery must fit before the next authored
warning. Random spawning remains suspended through the song response window
and recovery, and stale random hazards are removed if edited song timings
bring them into a protected interval. Song cue times are unaffected.

Open **KinectKids → Greve Gast → Cue Editor** to edit the timeline. Use its
playhead to add voice marks, set a cue's status after checking the WAV, and
adjust the independent offsets. The editor displays the decoded master
waveform and can validate IDs and cue bounds. Sections 4–17 deliberately have
no fabricated boundaries. The former `Resources/GreveGast/GreveGastTimeline.json`
is not runtime data; its first-refrain timings are obsolete and no longer seed
the editable command cues.
The editor always opens the active `Assets/Resources/GastSong.asset` and shows
its path, effective vocal time, obstacle appearance time and player encounter
time. While playing, it displays the runner's actual asset and audio position.
The four command vocal times have one authoritative editing location:
`ApplyCommandTimes` in `GastSongEditor.cs`. After script compilation, those
times are synchronized into the existing asset and saved automatically; the
same method initializes a new asset. Existing command cues are updated by ID,
without duplicating them or resetting their other timing settings. Their vocal
time fields in the editor window are read-only and show the source location.
Other settings edited in the window are saved automatically. Runtime timing
edits clear stale obstacles and re-arm the revised
schedule at the current audio position, including when a warning already fired.

The active game's existing sprite sequences animate the chase, player jump,
slide, lane changes, singing, and duck reaction. The source artwork does not
provide a separately rigged face, mouth, arms, hat, and coat, so the remaining
fine-grained Gast gestures are still whole-sprite prototypes. The rest-of-song
cue pass, connected platform pause lifecycle, and visual in-Unity review remain
unfinished; don't mark the corresponding cues Verified until they have been
listened to and played in the scene.
Backward audio seeks clear stale obstacles and rebuild commands whose warning
windows are active at the new position. The audio clock holds the clip's final
position on natural completion instead of returning to zero.
Playback is audible from sample zero, including the opening greeting; the
intro crawl no longer mutes the first 3.2 seconds. A streamed clip waiting to
start holds its initial position rather than dispatching the song's cues early.
Jump spikes are lifted by their scaled height when vertically flipped around
their bottom pivot, keeping them above the floor. Warning travel starts beyond
the camera's floor visibility limit so the whole obstacle is in view.
A missed song obstacle only causes damage if its rendered bounds were visible
above the floor at least 0.35 s before impact. Missing art, off-screen placement
or late dispatch cannot produce an invisible hit.

## Controls

`W`, up arrow or Space = jump; `S` or down arrow = slide; `A`/`D` or arrows =
change lane. Hold `Shift` or run in place with Kinect to push Greve farther
back. Corridor and obstacle speed remain constant. Kinect maps head height to
jump/slide and body centre to lane changes.

Recurring wall doors, generated stair geometry, kitchen hearths and central
cellar portals are omitted from the streamed corridor until they receive
authored environment cues. Wall torches remain wall decorations.
The song-driven runner no longer runs the legacy elapsed-time room cycle.
It remains in the castle corridor until a real environment change is requested.
The room label follows the foreground segment around the camera, so a room
farther down the track cannot change the label prematurely. The stairwell
placeholder is labelled as a dark passage because it has no staircase.

The wide run-distance indicator is centered at the top of the screen. Its top
edge illuminates from the same Kinect run-energy or Shift state that drives
chase distance. No player portraits are drawn in this HUD. Its distance bar follows
`1 - chase`; Greve's marker moves away from the player as the gap grows, with a
green arrow while distance is actually gained. Running at maximum distance
still lights the input indicator without claiming additional gain.

`dotnet run --project tools/GastTimelineVerify` from the repository root checks
the production cue scheduler and audio clock with lightweight audio stubs:
phase order across skipped frames, single dispatch, untimed cues, offsets,
rewind, pause/resume and natural clip completion. Scene visuals and Kinect
gestures still require an in-Unity playthrough.

## Legacy assets

The earlier `GreveGastGame` (generated 3D under `Assets/GreveGast`) and the drawn
sprite set (`Assets/GreveGast2D`) remain in place as references and fallbacks.
Do not delete or move them until the 2.5D chase fully covers the song with
final art. The new pipeline is additive and does not depend on them.

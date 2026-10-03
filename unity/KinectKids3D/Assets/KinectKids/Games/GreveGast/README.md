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
Kinect pose or key first triggers it. Ducking freezes the streamed world while
Greve Gast advances. A failed dodge likewise freezes the world for authored
fall and get-up sequences before running resumes.
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
estimate from Dennis and is visibly labelled as such. Untimed HOPPA, DUCKA,
VÄNSTER and HÖGER markers are seeded for the first refrain and do not trigger
gameplay. Existing keyboard and Kinect controls continue to operate the
runner.

Open **KinectKids → Greve Gast → Cue Editor** to edit the timeline. Use its
playhead to add voice marks, set a cue's status after checking the WAV, and
adjust the independent offsets. The editor displays the decoded master
waveform and can validate IDs and cue bounds. Sections 4–17 deliberately have
no fabricated boundaries. The former `Resources/GreveGast/GreveGastTimeline.json`
is an older estimate-only plan; it is not authoritative runtime data.

The active game's existing sprite sequences animate the chase, player jump,
slide, lane changes, singing, and duck reaction. The source artwork does not
provide a separately rigged face, mouth, arms, hat, and coat, so the remaining
fine-grained Gast gestures are still whole-sprite prototypes. The full song
cue pass, authored cue-driven hazard placement/result grading, connected pause
and seek lifecycle, and visual in-Unity review remain unfinished; don't mark
the corresponding cues Verified until they have been listened to and played
in the scene.

## Controls

Same as the platform (`W`/up = jump, `S`/down = duck, `A`/`D` or arrows = weave,
`Shift` = run). Kinect maps head height to jump/duck and body centre to weave.

## Legacy assets

The earlier `GreveGastGame` (generated 3D under `Assets/GreveGast`) and the drawn
sprite set (`Assets/GreveGast2D`) remain in place as references and fallbacks.
Do not delete or move them until the 2.5D chase fully covers the song with
final art. The new pipeline is additive and does not depend on them.

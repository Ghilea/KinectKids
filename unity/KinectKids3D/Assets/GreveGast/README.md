# Greve Gast – Unity prototype character

This package creates an original, simple game-ready prototype of Greve Gast based on the supplied concept sheet.

## What it creates

After running the builder, Unity generates:

- `Assets/GreveGast/Generated/Prefabs/GreveGast.prefab`
- textured/material-based character pieces
- Animator Controller
- animation clips:
  - Idle
  - Run
  - Reach
  - Stumble
  - Dance
  - Catch
- `GreveGastAnimationDriver` runtime helper

The model deliberately uses simple Unity geometry so it is cheap, readable at a distance, easy to animate, and suitable for the Kinect chase prototype.

## Installation

1. Copy the folder `Assets/GreveGast` into the Unity project's `Assets` folder.
2. Open Unity and wait for script compilation/import.
3. Choose **Tools > Greve Gast > Build Character**.
4. Unity selects the generated prefab automatically.
5. Drag `Assets/GreveGast/Generated/Prefabs/GreveGast.prefab` into the chase scene.

## Runtime control

```csharp
GreveGastAnimationDriver gast;

// chasing
gast.SetRun(true);

// reaches toward player
gast.PlayReach();

// misses an obstacle / comedy beat
gast.PlayStumble();

// musical gag
gast.PlayDance();

// player loses / Gast catches up
gast.PlayCatch();
```

Animator parameters are also available directly:

- Bool `Running`
- Trigger `Reach`
- Trigger `Stumble`
- Trigger `Dance`
- Trigger `Catch`

## Suggested use in the chase timeline

- Normal chase sections: `Running = true`
- Lyrics such as "Jag kommer närmare": combine Run + move Greve Gast closer
- Comedic crash/vas/rustning moments: `Stumble`
- Short musical breaks: `Dance`
- Almost grabs player: `Reach`
- Player fails final chase: `Catch`

## Important

This is a **prototype model**, not a production sculpt. It is intentionally made from simple geometry.

The useful part is that the hierarchy and animation contract can remain stable. Later, a Blender/FBX model can replace the primitive mesh pieces while retaining the same gameplay integration and animation state names.

# Greve Gast: living mass test

Open `unity/KinectKids3D/Assets/KinectKids/Games/GreveGast/Scenes/GastLivingFogTest.unity` in Unity and press Play. The scene contains one wall, one floor edge and a `GastLivingFog` hierarchy. Drag the aggression slider or use the up/down arrow keys to compare slow and aggressive motion.

The same `GastLivingFog` hierarchy is also created by `CorridorRunnerDirector` behind Greve. In the corridor `RearMass` fills the space behind him with an opaque, breathing black silhouette, while `EdgeMass` and `FloorMass` spread along the walls and floor. `TendrilSpawner` manages 7–10 tendrils in the test scene and 12–16 in the corridor, including upper tendrils rooted in the black rear mass. Each `ProceduralTendril` tube has 13 control points, Catmull–Rom interpolation, growth, pulse and retraction. `Wisps/Smoke` adds continuous mesh ribbons above the body. `GastFogController` exposes aggression from 0 to 1. The corridor also has a separate low blue fog bank around and in front of the player.

Batch-rendered stills for quick review:

- [Aggression 0.25](aggression-025.png)
- [Aggression 0.95](aggression-095.png)
- [Integrated corridor](corridor-integrated.png)

The stills show shape and coverage. They do not demonstrate the animation; inspect that in Play mode in the test scene or corridor.

In corridor mode, tendril roots are restricted to the opaque lower wall body and the black floor banks beside the walls. The standalone test scene keeps its original root layout.

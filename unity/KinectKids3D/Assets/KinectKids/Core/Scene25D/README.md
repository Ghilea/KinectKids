# KinectKids 2.5D-pipeline (`KinectKids.Scene25D`)

Detta är den återanvändbara 2.5D-motorn som beskrivs i `new-way.md`. Den låter
oss bygga hela spelet som lager av 2D-bilder i ett djupsorterat, parallax-drivet
rum med låsta/regisserade kameror – i stället för fulla 3D-miljöer.

Grundprincipen: **om kameran aldrig ser baksidan av något bygger vi inte
baksidan.** Gameplay och grafik hålls isär så att en platshållare kan bytas mot
en färdig PNG utan att spelreglerna ändras.

## Delar

| Fil | Ansvar |
|---|---|
| `LayerSorting.cs` | `SceneBand`-enum (Sky…Ui) och deterministisk sprite-sortering. Alla lager refererar band i stället för magiska tal. |
| `ParallaxLayer.cs` | Ett djuplager. Scrollar/skalar utifrån ett delat "travel"-värde. Kan loopa och driva (dimma). |
| `ParallaxController.cs` | Driver alla `ParallaxLayer` från en enda `forwardSpeed` + `lateralTarget`. Gameplay sätter bara fart. |
| `CameraDepthScaler.cs` | Skalar/positionerar en aktör längs djupaxeln (0 = långt bort/liten, 1 = nära/stor) för perspektivkänsla. |
| `ScriptedCamera.cs` | Låst/regisserad ortografisk kamera med namngivna shots, mjuka blends, idle-sway och skak. Spelaren styr den aldrig. |
| `PlaceholderArt.cs` | Genererar enkla silhuett-sprites i mörk-sagobok-paletten i runtime. Ren platshållare tills riktig konst finns. |
| `PuppetRig.cs` + `PuppetPose` | Lagerbaserad karaktärsanimation. Registrera kroppsdelar en gång, blenda sedan mellan tydliga key poses. |
| `PuppetBuilder.cs` | Bygger färdiga platshållarpuppets för spelaren och Greve Gast enligt kroppsdelslistan i `new-way.md`. |
| `DodgeAction.cs` | `DodgeAction`-enum + `IDodgeSource`. Kopplar hinder från input-källan (Kinect/tangentbord) utan hårt beroende. |
| `HazardBase.cs` | Basklass för hinder som färdas mot kameran och prövar rätt väjning vid en resolve-punkt. |
| `Hazards.cs` | Konkreta hinder (LowBeam/ducka, Falling/väj, Side/väj, Jump/hoppa) + `HazardFactory`. |

## Så byter man platshållare mot riktig grafik

1. **Miljölager:** parenta en `SpriteRenderer` med den färdiga PNG:en under
   rätt `ParallaxLayer` och sätt band via `LayerSorting.Apply(...)`. Ta bort
   platshållar-spriten. Parallaxen fungerar oförändrat.
2. **Karaktärer:** byt varje `PuppetPart`s sprite mot ett färdigt kroppslager
   med samma partnamn (t.ex. `head`, `armLeft`). Poserna i `PuppetBuilder`
   fungerar vidare; justera bara pivots/offset vid behov.
3. **Hinder:** byt `SpriteRenderer.sprite` i respektive hazard-typ. Reglerna
   (`requiredAction`, timing) ligger kvar i koden och påverkas inte.

## Att bygga en ny 2.5D-scen

```csharp
var world = ChaseEnvironmentBuilder.Build(root);   // eller egen lageruppsättning
world.forwardSpeed = 6f;                            // gameplay driver bara farten
world.lateralTarget = playerLateral;                // -1..1

var player = PuppetBuilder.BuildPlayer(root);
player.SetPose("run");

var hazard = HazardFactory.Spawn(HazardKind.LowBeam, root, dodgeSource);
hazard.Resolved += (h, avoided) => { /* poäng/feedback */ };
```

## Verifiering

Skripten kompilerar rent (Roslyn mot en UnityEngine-stub, 0 fel). Full
Unity-verifiering körs via `scripts/Build.ps1` när Unity 6000.0.60f1 finns
installerat.

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

## Modulär, sektionsbaserad runner (`Sections/`)

Ovanpå kärnmotorn ligger ett datadrivet lager som delar världen i **sektioner /
rum** (korridor, stora salen, köket, källaren, vinden) byggda av **återanvändbara
moduler**. Rummen byts i takt med **låtens struktur**.

| Fil | Ansvar |
|---|---|
| `EnvironmentModule.cs` | `ModuleKind` (golvsegment, väggdel, valv, prop, overhead, hinder, FX, framing) + `EnvironmentModule`-data. Art-agnostiskt: en modul namnger en sprite-nyckel + geometri, aldrig baked grafik. |
| `ModuleLibrary.cs` | Den enda kopplingen till grafik: en `Resolver` (nyckel → Sprite) med platshållar-fallback. Byt sprite utan att röra spelregler. |
| `PerspectiveModel.cs` | Delad perspektivmodell (`Default`/`Wide`/`Tight`) så alla moduler konvergerar mot samma flyktpunkt. |
| `SectionDefinition.cs` | `RoomKind` + ett rum som ren data (golv/väggar/props/FX/tillåtna hinder/längd/tema) med flytande bygg-API. |
| `SectionStreamer.cs` | Strömmar rum mot kameran i perspektiv, återvinner lager, gör cross-fade-övergångar. Driver `ParallaxController`. |
| `SongStructureDriver.cs` | `SongStructure`/`SongSection` + driver som byter rum beat-kvantiserat och sätter intensitet. |
| `RoomCatalog.cs` | Konkreta rum (Korridor/Stora salen/Köket/Källaren/Vinden) + `SampleSong()` som kedjar ihop dem. |

Regidirektören `Games/GreveGast/Scripts/RunnerSceneDirector.cs` binder ihop allt
(kamera + parallax + streamer + låtdriver + puppets + hinder från aktivt rum).
Den lämnar `ChaseSceneDirector` orörd. Scenen `GreveGast.unity` bootar runnern via
`GreveGastSceneEntry` (`useRunner = 1`); sätt `useRunner = 0` för den enkla
korridoren eller `useLegacySlice = 1` för Style C+-slicen.

### Bygga en ny runner-scen

```csharp
var parallax = worldGo.AddComponent<ParallaxController>();
var library  = new ModuleLibrary(GreveChaseSprites.Environment); // seam till riktig art

var streamer = worldGo.AddComponent<SectionStreamer>();
streamer.Init(parallax, library, PerspectiveModel.Default);

var driver = worldGo.AddComponent<SongStructureDriver>();
driver.Init(streamer, RoomCatalog.SampleSong(), () => music.time, RoomCatalog.Build);
// gameplay sätter bara streamer.forwardSpeed / streamer.lateralTarget varje frame
```

### Lägga till ett rum

1. Lägg ett värde i `RoomKind`.
2. Skriv en byggmetod i `RoomCatalog` (`.Floor(...).Walls(...).Prop(...).Effect(...).Hazards(...)`).
3. Koppla in det i `RoomCatalog.Build` + `PerspectiveFor`, och referera det i en `SongStructure`.

Inga ändringar behövs i streamer, motor eller spelregler.

## Verifiering

Skripten kompilerar rent (Roslyn mot en UnityEngine-stub, 0 fel). Full
Unity-verifiering körs via `scripts/Build.ps1` när Unity 6000.0.60f1 finns
installerat.

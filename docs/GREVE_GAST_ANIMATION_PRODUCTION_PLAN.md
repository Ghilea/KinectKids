# Greve Gast — Animation Production Plan

## Syfte
Det här dokumentet definierar vilka animationer Greve Gast behöver, hur de ska produceras, hur spritesheets ska struktureras och i vilken ordning animationerna ska byggas och kopplas till spelets cue-system.

Greve Gast är en svävande spökgreve. Han ska inte springa som en människa. Hans rörelser ska vara teatrala, hotfulla och tydligt kopplade till gameplay.

---

# 1. Produktionsprincip

## En animation per bild
Varje animation ska ligga i en egen PNG/spritesheet.

Exempel:

- greve_chase_fast_loop.png
- greve_surge_forward.png
- greve_strafe_left.png
- greve_strafe_right.png
- greve_reach_center.png
- greve_reach_left.png
- greve_reach_right.png

Detta gör det enklare att:
- beskära frames
- byta ut en animation
- ändra timing
- testa animationer separat
- felsöka
- importera i Unity
- koppla animationer mot cues

---

# 2. Tekniska bildkrav

Alla spritesheets ska följa:

- PNG
- Transparent bakgrund
- En animation per bild
- Stora mellanrum mellan varje frame
- Ingen överlappning
- Ingen scenbakgrund
- Full karaktär synlig
- Extra marginal kring hatt, händer och dimma
- Samma skala inom samma animationsfamilj
- Samma kameravinkel
- Samma karaktärsdesign
- Samma ljusriktning

Undvik text, frame-nummer och dekor i själva produktionsbilden om de riskerar att störa auto-slicing.

---

# 3. Fas 1 — Kärnan i jakten

Det här ska byggas först.

## 3.1 Chase_Fast_Loop

### Syfte
Grevens standardanimation under den intensiva jakten.

### Rekommenderat antal frames
8

### Rörelse
- Svävar snabbt mot spelaren
- Kroppen rör sig lätt upp/ner
- Rock och dimma dras bakåt
- Händerna rör sig aggressivt
- Ansiktet ska vara hotfullt
- Loop ska fungera sömlöst

### Fil
`greve_chase_fast_loop.png`

### Frames
- greve_chase_fast_01
- greve_chase_fast_02
- greve_chase_fast_03
- greve_chase_fast_04
- greve_chase_fast_05
- greve_chase_fast_06
- greve_chase_fast_07
- greve_chase_fast_08

---

## 3.2 SurgeForward

### Syfte
Kort aggressiv acceleration där Greven kastar sig närmare spelaren.

### Rekommenderat antal frames
6–8

### Rörelse
- Startar från chase-position
- Kroppen lutar framåt
- Händer sträcks mot spelaren
- Dimkroppen sträcks bakåt
- Tydlig fartökning
- Sista frame kan återgå mot chase eller lämna plats för blend tillbaka

### Fil
`greve_surge_forward.png`

---

## 3.3 Strafe_Left

### Syfte
Greven följer spelaren åt vänster.

### Rekommenderat antal frames
6

### Rörelse
- Svävar i sidled
- Fortsätter möta spelaren/kameran
- Kroppen lutar lätt in i rörelsen
- Dimman släpar åt motsatt håll
- Hotfull chase-känsla ska bevaras

### Fil
`greve_strafe_left.png`

---

## 3.4 Strafe_Right

### Syfte
Greven följer spelaren åt höger.

### Rekommenderat antal frames
6

### Rörelse
Samma princip som Strafe_Left, speglad naturligt.

### Fil
`greve_strafe_right.png`

---

## 3.5 Reach_Center

### Syfte
Greven försöker greppa spelaren rakt framifrån.

### Rekommenderat antal frames
6

### Rörelse
- Start från chase-ready
- En eller båda händer går framåt
- Ansiktet blir mer intensivt
- Max extension nära slutet
- Tydlig recovery eller möjlighet att blend:a tillbaka

### Fil
`greve_reach_center.png`

---

## 3.6 Reach_Left

### Syfte
Greven försöker få tag i spelaren åt vänster.

### Rekommenderat antal frames
6

### Fil
`greve_reach_left.png`

---

## 3.7 Reach_Right

### Syfte
Greven försöker få tag i spelaren åt höger.

### Rekommenderat antal frames
6

### Fil
`greve_reach_right.png`

---

# 4. Fas 2 — Cue-animationer

Dessa ska användas tillsammans med cue-systemet.

## 4.1 Shout_Run
Cue: `SPRING!`

### Rekommenderat antal frames
4–6

### Rörelse
- Kraftig shout-pose
- Öppen mun
- Kommenderande handgest
- Kan gärna kännas som att Greven sätter igång jakten

Fil:
`greve_shout_run.png`

---

## 4.2 Shout_Jump
Cue: `HOPPA!`

### Rekommenderat antal frames
4–6

### Rörelse
- Tydlig uppåtriktad gest
- Stor, lättläst armrörelse
- Kommenderande ansiktsuttryck

Fil:
`greve_shout_jump.png`

---

## 4.3 Shout_Duck
Cue: `DUCKA!`

### Rekommenderat antal frames
4–6

### Rörelse
- Nedåtpressande gest
- Tydlig sänkning eller handrörelse nedåt
- Ska gå att läsa direkt även i periferin

Fil:
`greve_shout_duck.png`

---

## 4.4 Shout_Left
Cue: `VÄNSTER!`

### Rekommenderat antal frames
4–6

### Rörelse
- Tydlig directional gesture åt vänster
- Hela posen ska hjälpa riktningen

Fil:
`greve_shout_left.png`

---

## 4.5 Shout_Right
Cue: `HÖGER!`

### Rekommenderat antal frames
4–6

Fil:
`greve_shout_right.png`

---

# 5. Fas 3 — Intensitet och variation

## 5.1 Chase_Close_Loop

### Syfte
Greven är nära spelaren.

### Känsla
- Större i bild
- Mer aggressiva händer
- Mer intensivt ansikte
- Kan ha mer framåtlutad kropp

Fil:
`greve_chase_close_loop.png`

---

## 5.2 Chase_Far_Loop

### Syfte
Greven ligger längre bakom.

### Känsla
- Mindre aggressiv
- Mer kontrollerad svävning
- Mindre reach
- Fortfarande jakt

Fil:
`greve_chase_far_loop.png`

---

# 6. Fas 4 — Reaktioner och fail states

## 6.1 Hit_React

### Rekommenderat antal frames
5

### Rörelse
- Ryck tillbaka
- Armar går ut/upp
- Dimma blir kaotisk
- Kort och läsbar reaktion

Fil:
`greve_hit_react.png`

---

## 6.2 Recover

### Rekommenderat antal frames
5

### Rörelse
- Från störd pose
- Återtar kontroll
- Tillbaka till chase-ready

Fil:
`greve_recover.png`

---

## 6.3 Near_Catch

### Rekommenderat antal frames
6

### Rörelse
- Kommer extremt nära
- Båda händer fram
- Aggressivt ansikte
- Stor intensitet
- Ska ge känslan att spelaren nästan förlorar

Fil:
`greve_near_catch.png`

---

## 6.4 Catch

### Syfte
Greven får tag i spelaren / fail-state.

### Rekommenderat antal frames
6–8

### Viktigt
Barnvänligt. Hotfullt och dramatiskt men inte våldsamt eller skräckinjagande på ett olämpligt sätt.

Fil:
`greve_catch.png`

---

# 7. Rekommenderad state-arkitektur

Animationerna bör kopplas ungefär så här:

## Basstate
- Chase_Far
- Chase_Fast
- Chase_Close

## Movement override
- Strafe_Left
- Strafe_Right
- SurgeForward

## Attack override
- Reach_Left
- Reach_Center
- Reach_Right
- Near_Catch
- Catch

## Cue gesture
- Shout_Run
- Shout_Jump
- Shout_Duck
- Shout_Left
- Shout_Right

## Reaction
- Hit_React
- Recover

Cue-animationer bör helst kunna spelas utan att hela Grevens chase-logik bryts permanent.

---

# 8. Cue-systemkoppling

När cue-systemet triggar:

- `RUN` -> Shout_Run
- `JUMP` -> Shout_Jump
- `DUCK` -> Shout_Duck
- `LEFT` -> Shout_Left
- `RIGHT` -> Shout_Right

Animationstriggern ska vara separat från cue-data så att tidsjusteringar i låten inte kräver att sprite assets ändras.

Exempel:

Cue-data:
- timestamp
- cue type
- pre-warning
- gameplay window

Animation controller:
- tar emot cue type
- väljer rätt shout-animation
- återgår till chase-state efter animationen

---

# 9. Produktionsordning

## Paket A — bygg först
1. Chase_Fast_Loop
2. SurgeForward
3. Reach_Center

Detta etablerar:
- basrörelse
- acceleration
- attackkänsla

## Paket B
4. Strafe_Left
5. Strafe_Right
6. Reach_Left
7. Reach_Right

## Paket C
8. Shout_Run
9. Shout_Jump
10. Shout_Duck
11. Shout_Left
12. Shout_Right

## Paket D
13. Chase_Close_Loop
14. Chase_Far_Loop
15. Hit_React
16. Recover
17. Near_Catch
18. Catch

---

# 10. Masterprompt för Greve Gast

Använd samma stilbas i varje bildgenerering:

> Create a high-quality 2D game sprite sheet for Greve Gast from KinectKids: Spökjakten. He is a male ghost count, not a vampire. He has a tall top hat, glowing eyes, a sharp sinister grin, elegant ghostly clothing in deep purple, blue and dark red accents, long expressive hands, and a swirling smoky ghost lower body instead of legs. The style is a polished family-friendly spooky fantasy game style with cinematic lighting, expressive poses, clean silhouettes, rich detail, and strong readability. He should look threatening, theatrical, and mysterious, but still appropriate for children. Transparent background. Large spacing between every frame for easy cropping. Keep character scale consistent across frames. No background scene. Each frame should be clearly separated and fully visible.

---

# 11. Animationsprompts

## Chase_Fast_Loop
> Create the animation "Chase_Fast_Loop". Show 8 clearly separated frames. Greve Gast aggressively floats toward the camera/player. His spectral lower body swirls beneath him, his coat and ghost smoke trail backward, and his long arms move rhythmically in a threatening chase motion. The animation must loop smoothly from frame 8 back to frame 1.

## SurgeForward
> Create the animation "SurgeForward". Show 6 to 8 clearly separated frames. Greve Gast suddenly lunges toward the player with a powerful ghostly burst. His hands stretch forward, his upper body leans aggressively toward the camera and his spectral tail trails strongly backward.

## Strafe_Left
> Create the animation "Strafe_Left". Show 6 clearly separated frames. Greve Gast floats rapidly to his left while keeping his body and face oriented toward the player. His smoke tail and coat trail naturally in the opposite direction.

## Strafe_Right
> Create the animation "Strafe_Right". Show 6 clearly separated frames. Greve Gast floats rapidly to his right while maintaining a threatening forward-facing chase posture.

## Reach_Center
> Create the animation "Reach_Center". Show 6 clearly separated frames. Greve Gast reaches directly toward the player with both hands in a strong grabbing motion, starting from a chase-ready pose and ending at maximum reach.

## Reach_Left
> Create the animation "Reach_Left". Show 6 clearly separated frames. Greve Gast lunges and reaches clearly toward the player's left side.

## Reach_Right
> Create the animation "Reach_Right". Show 6 clearly separated frames. Greve Gast lunges and reaches clearly toward the player's right side.

## Shout_Run
> Create the animation "Shout_Run". Show 4 to 6 clearly separated frames. Greve Gast performs a dramatic command gesture for "SPRING!" with an open shouting mouth, intense eyes and a forceful forward hand gesture.

## Shout_Jump
> Create the animation "Shout_Jump". Show 4 to 6 clearly separated frames. Greve Gast performs a strong upward-emphasis gesture for "HOPPA!".

## Shout_Duck
> Create the animation "Shout_Duck". Show 4 to 6 clearly separated frames. Greve Gast performs a clear downward pressing gesture for "DUCKA!".

## Shout_Left
> Create the animation "Shout_Left". Show 4 to 6 clearly separated frames. Greve Gast performs a highly readable directional gesture toward the left for "VÄNSTER!".

## Shout_Right
> Create the animation "Shout_Right". Show 4 to 6 clearly separated frames. Greve Gast performs a highly readable directional gesture toward the right for "HÖGER!".

## Hit_React
> Create the animation "Hit_React". Show 5 clearly separated frames. Greve Gast recoils backward as if briefly disrupted, with hands pulling back and his ghost smoke becoming chaotic.

## Recover
> Create the animation "Recover". Show 5 clearly separated frames. Greve Gast regains control after a hit reaction and returns to a stable chase-ready pose.

## Near_Catch
> Create the animation "Near_Catch". Show 6 clearly separated frames. Greve Gast lunges extremely close to the player with both hands reaching forward and a fierce theatrical expression.

## Catch
> Create the animation "Catch". Show 6 to 8 clearly separated frames. Greve Gast reaches and catches the player in a dramatic but family-friendly fail-state without violence.

---

# 12. Definition of Done

En animation är inte klar förrän:

- Stilen matchar Greve Gast
- Alla frames har transparent bakgrund
- Alla frames är fullt synliga
- Det finns stora mellanrum mellan frames
- Ingen frame överlappar en annan
- Figuren håller konsekvent skala
- Rörelsen går att läsa tydligt
- Animationen fungerar i Unity
- Loopar loopar rent
- Cue-animationer triggar rätt
- Ingen sprite får synligt hopp i storlek eller position
- Händer, hatt och dimma kapas inte vid slice

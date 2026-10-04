# Greve Gast: inkopplade animationer

Originalbilderna finns i `Assets/greveGast`. Spelets förberedda animationsark finns i
`unity/KinectKids3D/Assets/KinectKids/Games/GreveGast/Resources/GreveChase/GreveAnimations`.
Vid byte av en originalbild kör `tools/Prepare-GreveGastAnimations.py` med Python
och paketen Pillow, NumPy och SciPy. Verktyget uppdaterar Unity-arken och behåller
deras `.meta`-filer. Originalen ska inte kopieras direkt till Unity-mappen.

Verktyget separerar figurerna efter alfakonturerna, tar bort fristående rester och
återställer mjuka kanter. Händer och dimma som korsar originalens rutgränser följer
med rätt figur. Ramarna får gemensam storlek per sekvens, centrerad figur, gemensam
botten och minst 16 pixlars fri kant. Högerropets spegelvända kolumnordning rättas.
Förhandsvisningar sparas i `artifacts/greve-animation-crops`.

`GreveChaseSprites.GreveAnimation` delar de förberedda arken i två rader, lästa från vänster
till höger i översta raden och sedan nedersta. Importen behåller originalstorlek,
transparens och lika stora bildrutor med gemensam bottenpivot. Ingen komprimering
eller mipmapping används.

| Situation | Sekvens | Bildrutor |
| --- | --- | --- |
| Tavlan före framträdandet | `intro_taunt` | 8 |
| Övergång från tavlan | `intro_to_idle` | 8 |
| Efter övergången och när spelaren lyckas | `idle_loop` | 8 |
| Vanlig jakt | `chase_fast_loop` | 8 |
| Spring, hoppa, ducka, vänster, höger | respektive `shout_*` | 6 vardera |
| Följa spelarens sidoförflyttning | `strafe_left`, `strafe_right` | 6 vardera |
| Slutligt utfall mot vänster/höger sida | `reach_left`, `reach_right` | 6 vardera |
| Missat hinder eller utfall i mitten | `surge_forward` | 8 |

Introduktionen följer ljudets klocka. Med SPRING-markören vid 21 sekunder börjar
taunt vid 12 sekunder, greven lämnar tavlan vid 14,5 och övergår till idle vid 16.
Vid 21 sekunder startar både jakten och `shout_run`, utan gestförsprång.
Starttiden definieras i `ApplyIntroTiming` i GastSongEditor och läses från samma
markör av spelet. Ändringar i markörens effektiva tid flyttar även framträdandet.
Hoppa-, ducka-, vänster- och högermarkörerna samt hinderinställningarna är oförändrade.
Rop använder tidslinjens gesture-händelser.
Utfallet fortsätter animeras under fångstsekvensens nedtoning.

`greve_reach_center.png` är ett referensark med ogenomskinlig bakgrund och
bildnummer. Originalet är kvar, men importeras inte till spelet. Den transparenta
`surge_forward` används för mitten tills ett transparent center-ark finns.

De 37 tidigare grevbilderna och deras meta-filer har tagits bort. Äldre posnamn
översätts till de nya sekvenserna. Menyn visar en utskuren ny grevbild, inte hela arket.
Den gamla tavlans bildrutor används inte längre; dess gemensamma miljöark behövs
fortfarande för äldre miljödelar. De små körspökena har tagits bort ur den aktiva jakten.

Grevens djup begränsas till framför tavlans vägg under framträdandet och början av
jakten. När väggen passerat återgår han mjukt till sitt normala jaktavstånd.
Slottet har mörkare blå grundljus, mörkare miljöbilder och golvdimma, dämpad
fönstervy samt svagt och långsamt varierande fackelljus. Spelaren, greven och
handlingsikonerna behåller sin läsbarhet.

Duckningen varar 0,45 sekunder och kan avbrytas direkt med sidoväjning eller hopp,
via Kinect eller tangentbord. En registrerad duckning och ett registrerat hopp
sparas var för sig, så att nästa rörelse inte raderar ett korrekt sångsvar.

Verifierat 2026-10-04: runtime och editor kompilerar mot projektets Unity-referenser.
En separat Unity 6000.0.60f1-kontroll laddade 14 sekvenser och skapade alla 94
sprites med rätt rutstorlek, radordning och pivot samt kontrollerade 16 äldre
posnamn. `tools/GastTimelineVerify` passerade sina 98 kontroller, inklusive
SPRING-gestens start vid 21 sekunder och snabba rörelser efter duckning. Full speltest
med Kinect har inte utförts.

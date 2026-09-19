# Greve Gasts Jakt – första tidskarta

**Ljudfil:** `Greve Gasts Jakt.wav`  
**Längd:** `06:12.00`

> Detta är en strukturell första passering från den faktiska WAV-filen. Stora musikövergångar är relativt säkra. Exakta ord som HOPPA, DUCKA, VÄNSTER och HÖGER ska läggas som redigerbara markörer och finjusteras med låten i Unity innan banan låses.

## 00:00.00–00:13.75 — Intro – viskad öppning
- **Säkerhet:** high
- **Spel:** Långsam etablering av slottet. Ingen aktiv jakt ännu.

## 00:13.75–00:31.88 — Första musikaliska uppbyggnaden/jaktstarten
- **Säkerhet:** high
- **Spel:** Greve Gast avslöjas. Första SPRING-signalen och framåtrörelsen börjar.

## 00:31.88–01:00.35 — Vers 1 – slottets korridorer
- **Säkerhet:** medium
- **Spel:** Enkla hinder, spöken och dörrar. Introducera springmekaniken.

## 01:00.35–01:28.26 — Första uppbyggnaden och refrängen
- **Säkerhet:** medium
- **Spel:** Första tydliga serien med HOPPA, DUCKA, VÄNSTER och HÖGER.

## 01:28.26–01:57.24 — Instrumental jakt 1
- **Säkerhet:** high
- **Spel:** Ren hindersekvens utan krav på att följa nya lyrics.

## 01:57.24–02:08.17 — Vers 2 – köket öppnar
- **Säkerhet:** medium
- **Spel:** Miljön övergår till slottsköket.

## 02:08.17–02:37.18 — Köksjakt, rörelsekommandon och vas-skämt
- **Säkerhet:** medium
- **Spel:** Flygande tallrikar, kastruller, bord, stol, vas och komiskt stopp.

## 02:37.18–03:20.76 — Stor jaktsektion / galleri och rustningar
- **Säkerhet:** medium-low
- **Spel:** Full orkester, tavelgalleri, rustningar och levande miljöhändelser.

## 03:20.76–03:36.18 — Övergång mot falsk säkerhet
- **Säkerhet:** high
- **Spel:** Sänk hastighet och intensitet. Greve Gast verkar tappa bort spelaren.

## 03:36.18–03:45.26 — Falsk säkerhet – lugn dialog
- **Säkerhet:** high
- **Spel:** Nästan inga hinder. Dämpat ljus och låg kamerarörelse.

## 03:45.26–03:50.90 — Spänningspaus före BU
- **Säkerhet:** high
- **Spel:** Kameran antyder något bakom spelaren. Förbered jumpscare.

## 03:50.90–04:14.14 — BU och återstartad jakt
- **Säkerhet:** high
- **Spel:** Greve Gast dyker upp. Snabb instrumental flykt.

## 04:14.14–04:30.14 — Vers 4 – källaren
- **Säkerhet:** medium
- **Spel:** Trappa, tunnor, rep, lågt tak och komisk spindel.

## 04:30.14–04:36.87 — Call and response
- **Säkerhet:** medium
- **Spel:** Tydliga rörelseord. Agenten ska skapa redigerbara cue-markörer här.

## 04:36.87–04:43.12 — Greve Gast kommer nära / dramatisk paus
- **Säkerhet:** high
- **Spel:** Chase meter pressas. Kort stopp före finalen.

## 04:43.12–05:25.24 — Finalrefräng och lång finaljakt
- **Säkerhet:** medium
- **Spel:** Hög intensitet, återkommande kommandon och fler kombinationshinder.

## 05:25.24–05:42.24 — Slutkorridoren och porten
- **Säkerhet:** medium
- **Spel:** Utgången syns. Greve Gast presenterar sista fällorna.

## 05:42.24–05:55.75 — Sista rörelsesekvensen
- **Säkerhet:** medium
- **Spel:** HOPPA, HOPPA, DUCKA, VÄNSTER, HÖGER, DUCKA, HÖGER, VÄNSTER.

## 05:55.75–06:06.85 — Nedräkning och sluthopp
- **Säkerhet:** medium
- **Spel:** Fem till ett. Stort hopp vid sista musikaliska slaget.

## 06:06.85–06:12.00 — Kort outro och slutackord
- **Säkerhet:** high
- **Spel:** Landning, Greve Gasts reaktion och övergång till resultatskärm.

## Krav till AI-agenten

Agenten ska använda `AudioSource.time` eller `AudioSettings.dspTime` som masterklocka.
Alla cue-tider ska ligga i ett redigerbart ScriptableObject/JSON-system, inte vara hårdkodade.
Agenten ska skapa ett tidslinje-debugfönster med play, pause, seek, nästa cue och knappar för att flytta markerad cue ±0,05/0,10 sekunder.
De exakta Kinect-kommandona ska verifieras genom uppspelning i Unity innan miljö och hinder låses.
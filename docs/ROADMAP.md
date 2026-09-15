# Förslag för nästa versioner

Version 1.0 fokuserar på en stabil Kinect-grund och ett färdigt Balloon Pop. Följande spel kan använda samma `IPlayerTracker` och normaliserade ledkoordinater:

## Version 1.1

- Simon säger: händer upp, sträck ut armarna, ducka och stå still
- valbar rundlängd på 30, 60 eller 90 sekunder
- lokala topplistor utan namn eller persondata
- valbar ljudnivå och egna poppljud

## Version 1.2

- Undvik hinder: luta, hoppa och ducka
- svårighetsgrad per spel
- förenklat vuxenläge för inställningar

## Tekniskt

- isolera spelvyn bakom ett gemensamt `IGame`-gränssnitt
- lägg till en inspelningsfri diagnostikvy för ledspårning
- paketera en signerad Windows-installation
- testa på flera USB 2.0-kontrollers och skärmupplösningar

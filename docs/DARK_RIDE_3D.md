# Designriktning: Spökjakten 3D

Målet är en egen, barnvänlig interaktiv dark ride inspirerad av känslan i klassiska spöktåg. Vi kopierar inte Furuviks dekor, figurer eller bana. Referensen används för att förstå formatet: långsam vagn, tydliga scenrum, lysande mål, överraskningar och poängjakt.

## Åkturens första struktur

| Del | Känsla | Interaktion |
|---|---|---|
| Slottsporten | Kullerstensväg, rustningar och facklor | enkla spöken lär ut siktet |
| Kryptan | gravstenar och gröna spökljus | mål på båda sidor av vagnen |
| Borggården | torn, murkrön och zombies | tätare vågor och tvåträffsmål |
| Finalhallen | orange portal och dramatisk fackelbelysning | stillastående bossfas med livsmätare och kastattacker |

## Tekniska principer

- Kameran följer en matematisk rälslinje i 3D och tittar en bit framåt längs banan.
- Kinectens djupkoordinater mappas direkt till skärmens sikteskoordinater, utan den spegelvändning som tidigare gav fel riktning.
- Varje spelare har ett mållås. Aktiv hand väljs automatiskt och en träff kräver cirka 0,58 sekunder på samma 3D-collider eller en lätt framåtknuff.
- Vagnen stannar vid bossarenan och släpps vidare först när bossen är besegrad.
- Spelare två kräver stabil spårning, tydligt sidavstånd och liknande djup för att minska falska kroppar.
- Kinect SDK läses dynamiskt, vilket gör att Unity-projektet även kan öppnas och provas med mus på datorer utan SDK:n.

# Meny-assets

## Konceptbild

`konceptbild_menu.png` är den aktuella konceptbilden för huvudmenyn. Den visar
menyn i sin tänkta helhet med bakgrund, titel och de centrala spelvalen.

## Knapp-sheet 3

`menu_sheet_3.png` är referensarket för menyknapparnas interaktionstillstånd.
Varje knapp ska finnas i följande tillstånd:

1. **Normal** – standardläge utan pekare över knappen.
2. **Hover** – används när Kinect-markören ligger över knappen.
3. **Fokus** – tydligt valt läge för tangentbord, controller eller bekräftelse.
4. **Tryck** – kort nedtryckt läge innan handlingen körs.

Hover och fokus ska skilja sig visuellt även när de visas nära varandra. Hover
kan vara en mjuk glöd eller liten skalaffekt; fokus ska ha en stabil kontur som
är lätt att följa utan animation. Övergången mellan normal, hover och fokus ska
vara mjuk och kort, medan tryckläget ska vara en tydlig, snabb feedback.

Rekommenderad animation i Unity:

- normal -> hover: 0,12 s
- normal -> fokus: 0,12 s
- hover/fokus -> tryck: 0,08 s
- tryck -> normal eller vald vy: enligt scenbyte

Assetsen ligger här tillsammans med `menu_background.png`, som är den separata
bakgrunden för implementationen.

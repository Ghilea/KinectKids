# Greve Gast module

The active platform scene now starts `GreveGastStyleCGame`, a contained Style C+
vertical slice. It reuses the production music, JSON timeline, shared platform
input and the existing drawn Greve Gast animation prefab.

The earlier `GreveGastGame` and its generated 3D content remain in their
original locations as a verified legacy fallback. Do not delete or move those
assets until the paper-world replacement covers the complete song.

Current slice scope:

- song-synchronised intro and transition at 21.00 seconds;
- three persistent lanes;
- run, jump, duck, left and right input;
- timeline-driven warnings and paper obstacles;
- chase distance and playful catch recovery;
- animated doors, wall doodles and the drawn Greve Gast;
- common platform pause and return-to-menu flow.

F2 toggles diagnostics. F3 seeks to the first refrain. Page Down seeks to the
next timeline cue during development.

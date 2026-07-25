# Sky Courier / 云海邮差

An original pixel-art aviation deckbuilding roguelite prototype built in Unity 6.

## Prototype goal

The first vertical slice tests one design question: can a three-lane air battle make movement cards, weapon cards, heat management, and cargo constraints matter at the same time?

## Controls

- Click cards to play them.
- Use maneuver cards to change lanes.
- Weapon cards primarily hit enemies in the same lane.
- End the turn before heat reaches the red zone.
- Deliver the fragile medicine without letting a single hit deal 6 or more damage.
- Avoid changing lanes on consecutive turns unless the tracking risk is worth it.
- On the route map, select a connected node; use the mouse wheel, arrow buttons, or scrollbar to inspect the two revealed future columns.
- Press `Esc` to pause, restart the run, or adjust music and sound volume.

## Route map

The v0.26 route contains 8 columns and 19 connected nodes. Its tactical map uses compact node icons, stage markers, highlighted route links, and a separate detail panel. Branches include skirmishes, elite fights, hunts, shops, contract events, repair docks, and the final boss. Only the next two columns are revealed, so each choice preserves uncertainty without hiding the immediate consequences.

## Open

Open this folder with Unity `6000.5.3f1`, then open `Assets/Scenes/Prototype.unity` and press Play.

## Windows build

Run `Tools > Sky Courier > Build Windows Prototype` in Unity. The playable build is written to
`Builds/SkyCourierPrototype/Sky Courier Prototype.exe`.

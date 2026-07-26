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
- Runs are saved automatically at route decisions and resolved nodes. If you quit during combat, Continue restarts that encounter from its entrance state.
- Controller: use the left stick to select, `A` to confirm, `B` to go back, `Y` to end the turn, and Menu to pause.
- Display, VSync, frame-rate, audio, screen-shake, and flash intensity options are available from the title screen and pause menu.
- Simplified Chinese / English can be switched immediately in Settings and the choice is saved locally. v0.32 covers the title, settings, pause, archive, defeat debrief, and core content names; remaining gameplay text is being migrated incrementally.
- Every run has a visible seed. The route HUD, pause screen, and delivery summary show it so an encounter can be reproduced.
- Local JSONL diagnostics are stored under the game's persistent data `Diagnostics` folder and are never uploaded.
- The title-screen Postal Archive keeps local career stats, recent delivered/lost attempts, discoveries, and cosmetic honors without granting permanent combat bonuses.
- Defeats now identify the fatal mechanic, offer an actionable counter, and let the player retry with the same contract and a fresh seed; returning to title preserves the encounter-entry checkpoint.
- Contract selection now uses a pseudo-3D hangar carousel with focused cargo identity, depth-scaled side choices, smooth cycling, and full mouse/keyboard/controller navigation.
- Route events now form an optional Signal Thread: early choices create an aid promise or salvage debt, later events remember it, and the delivery summary records the resulting ending.
- The final boss now activates a readable phase-two matrix derived from the active contract, permanent airframe retrofit, and Signal Thread alignment.
- The last route column now offers two mechanically distinct finales: the Storm Manta marks danger to evade, while the Thunder-Curtain Wyrm marks the only safe corridor to enter.
- Route altitude is now a real strategic choice: Jetstream Corridor, Static Front, and Wreckage Tide expose different enemy pools and steer post-signature rewards toward different build directions.
- The penultimate route column now teaches each finale through Curtain Herald and Flux Skimmer prelude encounters before asking the player to master the full boss pattern.
- Clearing a prelude captures persistent route intel that visibly alters the matching boss's opening lock and survives save/continue.
- Each finale boss now combines with the Signal Thread alignment to produce three outcomes, creating six discoverable endings recorded by the Postal Archive.

## Route map

The route contains 8 columns and 20 connected nodes. Its tactical map uses compact node icons, stage markers, highlighted route links, three color-coded airspace bands, and a separate rule/detail panel. Branches include skirmishes, elite fights, hunts, shops, contract events, repair docks, and two final bosses. Only the next two columns are revealed, so each choice preserves uncertainty without hiding the immediate consequences.

## Open

Open this folder with Unity `6000.5.3f1`, then open `Assets/Scenes/Prototype.unity` and press Play.

## Windows build

Run `Tools > Sky Courier > Build Windows Prototype` in Unity. The playable build is written to
`Builds/SkyCourierPrototype/Sky Courier Prototype.exe`.

## Localization

Runtime text lives in `Assets/Resources/Localization/localization.txt` as a three-column TSV table. Every integrated static key and all dynamic card-name keys are checked before a release build. See `Docs/Localization_v0.32.md` for the current coverage boundary and migration order.

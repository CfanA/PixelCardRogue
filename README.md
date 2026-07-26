# Sky Courier / 云海邮差

An original pixel-art aviation deckbuilding roguelite prototype built in Unity 6.

## Prototype goal

The first vertical slice tests one design question: can a three-lane air battle make movement cards, weapon cards, heat management, and cargo constraints matter at the same time?

## Controls

- Click cards to play them.
- Start each run by choosing standard delivery or one of three fixed-seed challenges, then select from five contracts and sign one of three departure clauses.
- Use maneuver cards to change lanes.
- Weapon cards primarily hit enemies in the same lane.
- End the turn before heat reaches the red zone.
- Deliver the fragile medicine without letting a single hit deal 6 or more damage.
- Avoid changing lanes on consecutive turns unless the tracking risk is worth it.
- On the route map, select a connected node; use the mouse wheel, arrow buttons, or scrollbar to inspect the two revealed future columns.
- Press `Esc` to pause. Press `F1` during a run to open the complete rulebook and glossary.
- Runs are saved automatically at route decisions and resolved nodes. If you quit during combat, Continue restarts that encounter from its entrance state.
- Controller: use the left stick to select, `A` to confirm, `B` to go back, `Y` to end the turn, and Menu to pause.
- Display mode, resolution, VSync, frame-rate, music, SFX, screen-shake, flash intensity, contextual tutorials, and focus highlights are available from the title screen and pause menu.
- Simplified Chinese / English can be switched immediately in Settings and is saved locally. The v0.52 release validates every integrated static key, all 108 card names and rule texts, the complete tutorial glossary, enemy intentions, and battle feedback.
- Every run has a visible seed. The route HUD, pause screen, and delivery summary show it so an encounter can be reproduced.
- Local JSONL diagnostics are stored under the game's persistent data `Diagnostics` folder and are never uploaded.
- The title-screen Postal Archive keeps challenge records, five-contract mastery, boss dossiers, six endings, achievements, and next-run goals without granting permanent combat bonuses.
- Defeats are final and now explain raw fatal damage, shield absorption, the key telemetry-backed mistake, the build weakness, and one concrete next strategy. Players can retry the same contract on the same seed, reroll a new seed, or change contracts immediately.
- Contract selection now uses a pseudo-3D hangar carousel with focused cargo identity, depth-scaled side choices, smooth cycling, and full mouse/keyboard/controller navigation.
- Route events now form an optional Signal Thread: early choices create an aid promise or salvage debt, later events remember it, and the delivery summary records the resulting ending.
- The final boss now activates a readable phase-two matrix derived from the active contract, permanent airframe retrofit, and Signal Thread alignment.
- The last route column now offers two mechanically distinct finales: the Storm Manta marks danger to evade, while the Thunder-Curtain Wyrm marks the only safe corridor to enter.
- Route altitude is now a real strategic choice: Jetstream Corridor, Static Front, and Wreckage Tide expose different enemy pools and steer post-signature rewards toward different build directions.
- The penultimate route column now teaches each finale through Curtain Herald and Flux Skimmer prelude encounters before asking the player to master the full boss pattern.
- Clearing a prelude captures persistent route intel that visibly alters the matching boss's opening lock and survives save/continue.
- Each finale boss now combines with the Signal Thread alignment to produce three outcomes, creating six discoverable endings recorded by the Postal Archive.
- The Signal Seed contract and five reserve-energy cards create a fifth build identity around ending card payments with exactly 1 Energy.
- The card catalog now contains 108 distinct card types: 16 shared cards plus 17-19 cards for each of the five contracts.
- New runs use a fixed 13-card deck. Combat rewards and shops offer seeded three-card selections, while acquired cards may appear in multiple copies in the run deck.
- Repair docks now offer repair, contract-core A/B recalibration, or removal of any one card copy. Deck size and damage-card count are player-controlled with no deletion floor.
- If an opening hand has no direct-damage card but the draw pile contains one, one support card is swapped for it; the game never creates a card that is absent from the deck.
- Runs now have three visible acts: an opening clause before departure, an irreversible airframe retrofit at the midpoint, and a final-approach preparation before the prelude column.
- Final-approach preparation offers a paid Hull patch, removal of any one card copy, a Cargo-for-module overclock, or holding the current course.
- Route details now show act progress, risk, and expected rewards. Altitude labels stay pinned to the map viewport while nodes move under scrollbar drag.
- Signal Thread events now offer cooperative, opportunist, and Silent routes. Silent choices remove a real card copy and carry a neutral thread through all three event chapters.
- Supply stops now include a route workshop for paid card removal and A/B branch rewrites on any owned card type, with each service limited to once per stop.
- Runs capture versioned build snapshots at departure, retrofit, events, rewards, service stops, final approach, and boss approach. Completed and failed runs retain those snapshots in the local Postal Archive for the next debrief iteration.
- The Postal Archive now tracks resolved-attempt win rates by contract, build profile, route profile, and reached boss. Automated 72-run simulation remains a deterministic regression tool rather than a target win rate.
- v0.50 makes two evidence-bounded card changes: Frost Lance loses 1 low-heat bonus damage, while Reserve Shot gains 1 base damage. Enemy, boss, economy, and route values remain unchanged until more complete human runs include v0.49+ snapshots.
- Contextual tutorials now trigger from actual player behavior and appear once per topic. They cover intentions, same-lane attacks, heat, cargo, lane shifts, tracking, retrofit, chronicles, outposts, and Boss rules; every topic remains available from the rulebook.
- First-run players go directly to a standard dispatch. Fixed-seed challenges appear after the first resolved run so the opening flow does not introduce unrelated goals.
- Important combat rules combine color with persistent symbols and text: `[>]` marks the current lane, `X` marks tracking danger, `[O]` marks safety, `[X]` marks danger, and `!!` marks Boss counters.

## Route map

The route contains 8 columns and 20 connected nodes. Its tactical map uses compact node icons, stage markers, highlighted route links, three color-coded airspace bands, and a separate rule/detail panel. Branches include skirmishes, elite fights, hunts, shops, contract events, repair docks, and two final bosses. Only the next two columns are revealed, so each choice preserves uncertainty without hiding the immediate consequences.

## Open

Open this folder with Unity `6000.5.3f1`, then open `Assets/Scenes/Prototype.unity` and press Play.

## Windows build

Run `Tools > Sky Courier > Build Windows Prototype` in Unity. The playable build is written to
`Builds/SkyCourierPrototype_v0.52/Sky Courier Prototype.exe`.

## Localization

Runtime text lives in `Assets/Resources/Localization/localization.txt` as a three-column TSV table. Every integrated static key, all dynamic card names and rules, enemy intentions, tutorial entries, and glossary definitions are checked before a release build.

The complete v0.47.1 card-pool partition and run-deck rules are documented in `Docs/Card_Pool_v0.47.1.md`.

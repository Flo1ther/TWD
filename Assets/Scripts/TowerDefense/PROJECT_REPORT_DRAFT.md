# Tower Defense Project Report Draft

## Project Goal

The project is a playable 2D Tower Defense prototype made in Unity. The player defends a base by placing towers on free grass tiles while enemies move along a fixed road from the entrance portal to the defender base.

## Implemented Gameplay

- Main menu with PvE, PvP hot-seat, and stress test modes.
- 12x8 playable grid with a fixed enemy path.
- Preparation and battle phases.
- Defender economy: starting gold, tower prices, kill rewards, round bonus.
- Four tower types: Archer, Mage, Freezer, Cannon.
- Three enemy types: Goblin, Orc, Ghost.
- Tower placement only on free non-road tiles.
- Tower selection, upgrade, and sell actions during preparation.
- Full tower refund before first completed battle cycle; reduced refund after use.
- PvE adaptive enemy waves that scale by round and defender strength.
- PvP hot-seat attacker wave building with attack budget and enemy count limits.
- Base HP loss when enemies reach the base.
- Immediate game over when base HP reaches zero.
- Victory condition after surviving 10 rounds.
- Final score on game over.
- Stress test with 20 towers and 50 enemies.
- Projectile, enemy, and impact pooling.
- Prototype sound effects generated at runtime.
- Compact runtime HUD with mode, phase, round, defender stats, selected tower stats, attacker stats in PvP, wave preview in PvE, and round/game summaries.
- Battle pause and speed controls.

## Assets Used

Generated 2D sprites are stored in `Assets/Resources/TowerDefense/Sprites`.

Current sprite groups:

- Grass and road tiles.
- Forest border/background tile.
- Enemy entrance portal.
- Defender crystal base.
- Tower sprites for Archer, Mage, Freezer, and Cannon.
- Enemy sprites for Goblin, Orc, and Ghost.
- Projectile sprites for arrow, magic orb, ice shard, and cannonball.

## Main Scripts

- `TowerDefensePrototype.cs`: runtime game bootstrap, grid, UI, battle loop, waves, towers, enemies, projectiles, effects, audio.
- `TowerDefenseData.cs`: serializable tower and enemy data structures.
- `TowerDefenseArtImporter.cs`: editor helper for importing generated sprites.
- `TowerDefenseBuildTools.cs`: WebGL build configuration and build command.

## Demonstration Plan

1. Open `Assets/Scenes/SampleScene.unity`.
2. Press Play.
3. Start PvE.
4. Place several towers on grass tiles.
5. Select a tower and show upgrade/sell buttons.
6. Start battle and show enemies moving along the road.
7. Show pause/speed controls, base HP, gold rewards, and round summary.
8. Return to menu and start PvP.
9. Add enemies as attacker and start the battle.
10. Run Stress Test to show 20 towers and 50 enemies.

## Remaining Polish

- Final numeric balance after a full 10-round playtest.
- WebGL build verification.
- Screenshots for the final written report.

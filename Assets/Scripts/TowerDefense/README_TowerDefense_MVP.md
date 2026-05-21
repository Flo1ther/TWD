# Tower Defense MVP

This folder contains the first playable prototype based on the project requirements:

- 12x8 grid with a fixed waypoint path.
- Preparation and battle phases.
- PvE mode and a basic PvP hot-seat mode.
- Defender economy: starting gold, tower prices, rewards for kills.
- AI attacker wave generation with a growing attack budget.
- Manual attacker wave building in PvP hot-seat mode.
- Enemy movement through waypoints with base HP damage.
- Four tower types: Archer, Mage, Freezer, Cannon.
- Three enemy types: Goblin, Orc, Ghost.
- Tower selection, upgrades, and selling during the preparation phase.
- Full tower refund before its first completed battle cycle, reduced refund after use.
- Round and game-over summaries with kills, leaks, gold, tower builds, upgrades, and sales.
- Final score calculation for the end screen.
- Procedural prototype sound effects for building, upgrading, selling, shots, hits, and leaks.
- Adaptive PvE wave generation that scales by round and defender strength.
- Battle speed and pause controls for easier testing and presentation.
- Basic projectile and enemy pooling to reduce battle-time allocations.
- Prewarmed enemy/projectile pools for the first battle.
- Menu stress test that starts a battle with 20 towers and 50 enemies.
- Runtime-generated placeholder sprites and HUD, so the sample scene can run without manual prefab setup.
- ScriptableObject data classes for towers and enemies.
- Editor menu for generating default tower/enemy data assets.
- Optional sprite fields on tower/enemy data, ready for generated PNG assets later.

## How to try it

Open the Unity project and press Play in `Assets/Scenes/SampleScene.unity`.

The runtime bootstrap creates the prototype automatically. In Preparation, choose a tower button, click a free grid cell to place it, then press `Start Battle`. Click an already placed tower to select it, then use `Upgrade` or `Sell`. During battle, use `Speed x1/x2` or `Pause`.

In `Start PvP`, the defender still places towers, while the attacker uses the enemy buttons to build a wave within the attack budget before battle starts. `Clear Wave` resets the attacker selection.

To create editable balance data, run `Tower Defense > Create Default Data Assets` in the Unity menu. The prototype will then load data from `Assets/Resources/TowerDefense`.

Use `Stress Test` from the main menu to check the required 20 towers / 50 enemies scenario. After the test wave ends, use `Main Menu` to reset the run.

Use `Tower Defense > Configure WebGL Build` to set the project up for WebGL, or `Tower Defense > Build WebGL` to create a build in `Builds/WebGL`.

See `ASSET_REQUESTS.md` for the generated sprite list.

After generated PNGs are sliced into `Assets/Resources/TowerDefense/Sprites`, run `Tower Defense > Import Generated Art` to import them as sprites and assign them to tower/enemy data.

## Next useful steps

1. Tune final numeric balance after playtesting all 10 rounds.
2. Add a final report section for used assets and gameplay screenshots.
3. Create and verify the WebGL build.

## Runtime script structure

- `RuntimeTowerDefenseBootstrap.cs`: creates the runtime scene and loads default data.
- `TowerDefenseGame.cs`: owns the state machine, economy, waves, HUD, audio, and battle loop.
- `Tower.cs`, `Enemy.cs`, and `Projectile.cs`: gameplay actors.
- `ImpactEffect.cs` and `GoldRewardEffect.cs`: battle feedback effects.
- `SpriteLoader.cs` and `SpriteFactory.cs`: generated art loading and fallback sprites.
- `TowerDefenseData.cs`: ScriptableObject-friendly tower and enemy definitions.

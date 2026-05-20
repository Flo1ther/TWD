# Asset Requests

Generate these as transparent PNG sprites, square canvas, readable at small size. A consistent top-down fantasy style is best.

## Towers

Suggested size: 256x256 PNG.

- Archer tower: wooden watchtower or archer post, green accents.
- Mage tower: small arcane tower, purple magic accents.
- Freezer tower: ice/crystal tower, cyan accents.
- Cannon tower: squat cannon turret, orange/metal accents.

## Enemies

Suggested size: 256x256 PNG.

- Goblin: small fast enemy, green.
- Orc: larger tank enemy, bulky, brown/green.
- Ghost: floating pale enemy, translucent or blue-white.

## Projectiles

Suggested size: 128x128 PNG.

- Arrow projectile for Archer.
- Magic orb for Mage.
- Ice shard for Freezer.
- Cannonball for Cannon.

## Tiles and UI Later

Not urgent yet. Placeholder tiles are fine while mechanics are still changing.

- Grass/buildable tile.
- Path tile.
- Base marker.
- Entry marker.

## Import Notes

In Unity, import as `Sprite (2D and UI)`.

Assign sprites in:

- `Assets/Resources/TowerDefense/Towers/*.asset`
- `Assets/Resources/TowerDefense/Enemies/*.asset`

Tower assets have `sprite` and `projectileSprite` fields. Enemy assets have `sprite`.

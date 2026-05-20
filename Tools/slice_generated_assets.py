from collections import deque
from pathlib import Path

from PIL import Image


ROOT = Path(r"D:\UnityProject\TWD")
DOWNLOADS = Path(r"C:\Users\shevc\Downloads")
OUT = ROOT / "Assets" / "Resources" / "TowerDefense" / "Sprites"


SHEETS = {
    "towers": DOWNLOADS / "Gemini_Generated_Image_92wkpr92wkpr92wk.png",
    "enemies": DOWNLOADS / "Gemini_Generated_Image_kcoaupkcoaupkcoa.png",
    "projectiles": DOWNLOADS / "Gemini_Generated_Image_wep1vtwep1vtwep1.png",
    "tiles": DOWNLOADS / "Gemini_Generated_Image_wfk5xpwfk5xpwfk5.png",
}


def is_background_candidate(pixel):
    r, g, b, a = pixel
    if a == 0:
        return True

    # Gemini baked the transparency preview into the image. These gray-ish
    # checker pixels are background, not intended art.
    grayish = abs(r - g) <= 18 and abs(g - b) <= 18 and abs(r - b) <= 18
    black_border = r <= 28 and g <= 28 and b <= 28
    return black_border or grayish and 20 <= r <= 225


def remove_checker_background(image):
    image = image.convert("RGBA")
    pixels = image.load()
    width, height = image.size
    visited = set()
    queue = deque()

    for x in range(width):
        queue.append((x, 0))
        queue.append((x, height - 1))
    for y in range(height):
        queue.append((0, y))
        queue.append((width - 1, y))

    while queue:
        x, y = queue.popleft()
        if (x, y) in visited or x < 0 or y < 0 or x >= width or y >= height:
            continue

        visited.add((x, y))
        if not is_background_candidate(pixels[x, y]):
            continue

        pixels[x, y] = (0, 0, 0, 0)
        queue.append((x + 1, y))
        queue.append((x - 1, y))
        queue.append((x, y + 1))
        queue.append((x, y - 1))

    return image


def trim_alpha(image, padding=10):
    bbox = image.getbbox()
    if bbox is None:
        return image

    left = max(0, bbox[0] - padding)
    top = max(0, bbox[1] - padding)
    right = min(image.width, bbox[2] + padding)
    bottom = min(image.height, bbox[3] + padding)
    return image.crop((left, top, right, bottom))


def save_sprite(sheet_path, box, out_name, trim=True):
    image = Image.open(sheet_path).convert("RGBA")
    sprite = image.crop(box)
    sprite = remove_checker_background(sprite)
    if trim:
        sprite = trim_alpha(sprite)
    out_path = OUT / out_name
    out_path.parent.mkdir(parents=True, exist_ok=True)
    sprite.save(out_path)
    print(out_path)


def slice_quadrants(sheet_key, names, trim=True):
    sheet_path = SHEETS[sheet_key]
    image = Image.open(sheet_path)
    width, height = image.size
    boxes = [
        (0, 0, width // 2, height // 2),
        (width // 2, 0, width, height // 2),
        (0, height // 2, width // 2, height),
        (width // 2, height // 2, width, height),
    ]
    for name, box in zip(names, boxes):
        save_sprite(sheet_path, box, name, trim)


def slice_enemies():
    # Enemies are arranged horizontally in the lower half of the sheet.
    sheet_path = SHEETS["enemies"]
    boxes = [
        (60, 760, 620, 1390),
        (620, 540, 1380, 1430),
        (1380, 600, 1960, 1400),
    ]
    for name, box in zip(["Goblin.png", "Orc.png", "Ghost.png"], boxes):
        save_sprite(sheet_path, box, name, trim=True)


def main():
    slice_quadrants("towers", ["ArcherTower.png", "MageTower.png", "FreezerTower.png", "CannonTower.png"])
    slice_enemies()
    slice_quadrants("projectiles", ["ArrowProjectile.png", "MagicOrbProjectile.png", "IceShardProjectile.png", "CannonballProjectile.png"])
    slice_quadrants("tiles", ["GrassTile.png", "DirtTile.png", "StonePathTile.png", "ForestTile.png"], trim=False)


if __name__ == "__main__":
    main()

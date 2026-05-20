from collections import deque
from pathlib import Path

from PIL import Image


ROOT = Path(r"D:\UnityProject\TWD")
DOWNLOADS = Path(r"C:\Users\shevc\Downloads")
OUT = ROOT / "Assets" / "Resources" / "TowerDefense" / "Sprites"


SHEETS = {
    "towers": DOWNLOADS / "Gemini_Generated_Image_f4rxbxf4rxbxf4rx (1).png",
    "enemies": DOWNLOADS / "Gemini_Generated_Image_f4rxbxf4rxbxf4rx (2).png",
    "projectiles": DOWNLOADS / "Gemini_Generated_Image_f4rxbxf4rxbxf4rx (3).png",
    "tiles": DOWNLOADS / "Gemini_Generated_Image_f4rxbxf4rxbxf4rx (4).png",
    "markers": DOWNLOADS / "Gemini_Generated_Image_f4rxbxf4rxbxf4rx (5).png",
}


def is_background_candidate(pixel):
    r, g, b, a = pixel
    if a == 0:
        return True

    grayish = abs(r - g) <= 18 and abs(g - b) <= 18 and abs(r - b) <= 18
    black_line = r <= 32 and g <= 32 and b <= 32
    return black_line or grayish and 20 <= r <= 230


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


def trim_alpha(image, padding=12):
    bbox = image.getbbox()
    if bbox is None:
        return image

    left = max(0, bbox[0] - padding)
    top = max(0, bbox[1] - padding)
    right = min(image.width, bbox[2] + padding)
    bottom = min(image.height, bbox[3] + padding)
    return image.crop((left, top, right, bottom))


def save_object(sheet_path, box, out_name, padding=12):
    image = Image.open(sheet_path).convert("RGBA")
    sprite = remove_checker_background(image.crop(box))
    sprite = trim_alpha(sprite, padding=padding)
    out_path = OUT / out_name
    out_path.parent.mkdir(parents=True, exist_ok=True)
    sprite.save(out_path)
    print(out_path)


def save_tile(sheet_path, box, out_name):
    image = Image.open(sheet_path).convert("RGBA")
    tile = image.crop(box)
    out_path = OUT / out_name
    out_path.parent.mkdir(parents=True, exist_ok=True)
    tile.save(out_path)
    print(out_path)


def save_transparent_checker_tile(sheet_path, box, out_name):
    image = Image.open(sheet_path).convert("RGBA")
    tile = image.crop(box)
    pixels = tile.load()
    for y in range(tile.height):
        for x in range(tile.width):
            r, g, b, a = pixels[x, y]
            grayish = abs(r - g) <= 18 and abs(g - b) <= 18 and abs(r - b) <= 18
            if grayish and 30 <= r <= 225:
                pixels[x, y] = (0, 0, 0, 0)

    out_path = OUT / out_name
    out_path.parent.mkdir(parents=True, exist_ok=True)
    tile.save(out_path)
    print(out_path)


def slice_quadrants(sheet_key, names):
    image = Image.open(SHEETS[sheet_key])
    width, height = image.size
    boxes = [
        (0, 0, width // 2, height // 2),
        (width // 2, 0, width, height // 2),
        (0, height // 2, width // 2, height),
        (width // 2, height // 2, width, height),
    ]

    for name, box in zip(names, boxes):
        save_object(SHEETS[sheet_key], box, name)


def slice_tiles():
    sheet = SHEETS["tiles"]
    # The sheet is a 3x3 contact grid with black separators. Crop inside each
    # cell to avoid baked divider pixels.
    cell = 2048 // 3
    inset = 18

    def box(col, row):
        left = col * cell + inset
        top = row * cell + inset
        right = (col + 1) * cell - inset if col < 2 else 2048 - inset
        bottom = (row + 1) * cell - inset if row < 2 else 2048 - inset
        return left, top, right, bottom

    save_tile(sheet, box(0, 0), "GrassTile.png")
    save_tile(sheet, box(0, 0), "BuildableGrassTile.png")
    save_tile(sheet, box(1, 0), "RoadVertical.png")
    save_tile(sheet, box(0, 1), "RoadHorizontal.png")
    save_tile(sheet, box(2, 0), "RoadCorner.png")
    save_tile(sheet, box(0, 2), "RoadTJunction.png")
    save_transparent_checker_tile(sheet, box(2, 2), "ForestBorderTile.png")
    save_tile(sheet, box(0, 1), "StonePathTile.png")


def slice_markers():
    sheet = SHEETS["markers"]
    save_object(sheet, (0, 0, 1024, 860), "EntryPortal.png", padding=16)
    save_object(sheet, (900, 0, 1980, 860), "DefenderBase.png", padding=16)


def main():
    slice_quadrants("towers", ["ArcherTowerAlt.png", "MageTower.png", "FreezerTower.png", "CannonTower.png"])
    slice_quadrants("enemies", ["Goblin.png", "Orc.png", "OrcLarge.png", "Ghost.png"])
    slice_quadrants("projectiles", ["ArrowProjectile.png", "MagicOrbProjectile.png", "IceShardProjectile.png", "CannonballProjectile.png"])
    slice_tiles()
    slice_markers()


if __name__ == "__main__":
    main()

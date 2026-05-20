from pathlib import Path

from PIL import Image


ROOT = Path(r"D:\UnityProject\TWD")
SHEET = Path(r"C:\Users\shevc\Downloads\Gemini_Generated_Image_1ayj8s1ayj8s1ayj.png")
OUT = ROOT / "Assets" / "Resources" / "TowerDefense" / "Sprites"


def is_sheet_background(pixel):
    r, g, b, a = pixel
    if a == 0:
        return True

    grayish = abs(r - g) <= 16 and abs(g - b) <= 16 and abs(r - b) <= 16
    whiteish = r >= 230 and g >= 230 and b >= 230
    return whiteish or grayish and 90 <= r <= 230


def find_content_box(image):
    pixels = image.load()
    col_counts = [0] * image.width
    row_counts = [0] * image.height
    for y in range(image.height):
        for x in range(image.width):
            if is_sheet_background(pixels[x, y]):
                continue

            col_counts[x] += 1
            row_counts[y] += 1

    min_col_count = max(12, int(max(col_counts) * 0.45))
    min_row_count = max(12, int(max(row_counts) * 0.45))
    xs = [i for i, count in enumerate(col_counts) if count >= min_col_count]
    ys = [i for i, count in enumerate(row_counts) if count >= min_row_count]
    if not xs or not ys:
        return (0, 0, image.width, image.height)

    return min(xs), min(ys), max(xs) + 1, max(ys) + 1


def save(tile, name):
    # Terrain tiles must stay fully opaque. Transparent margins become black
    # seams in Unity when adjacent board cells are scaled independently.
    tile = repair_checker_edges(tile.convert("RGBA").resize((512, 512), Image.Resampling.LANCZOS))
    OUT.mkdir(parents=True, exist_ok=True)
    out_path = OUT / f"{name}.png"
    tile.save(out_path)
    print(out_path)


def repair_checker_edges(image):
    pixels = image.load()
    width, height = image.size
    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            grayish = abs(r - g) <= 32 and abs(g - b) <= 32 and abs(r - b) <= 32
            if is_sheet_background(pixels[x, y]) or grayish and (r + g + b) // 3 >= 190:
                pixels[x, y] = (0, 0, 0, 0)

    for _ in range(80):
        changed = False
        snapshot = image.copy()
        source = snapshot.load()
        for y in range(height):
            for x in range(width):
                if source[x, y][3] != 0:
                    continue

                colors = []
                for oy in (-1, 0, 1):
                    for ox in (-1, 0, 1):
                        if ox == 0 and oy == 0:
                            continue
                        nx, ny = x + ox, y + oy
                        if 0 <= nx < width and 0 <= ny < height and source[nx, ny][3] != 0:
                            colors.append(source[nx, ny])

                if colors:
                    r = sum(c[0] for c in colors) // len(colors)
                    g = sum(c[1] for c in colors) // len(colors)
                    b = sum(c[2] for c in colors) // len(colors)
                    pixels[x, y] = (r, g, b, 255)
                    changed = True

        if not changed:
            break

    for y in range(height):
        for x in range(width):
            r, g, b, a = pixels[x, y]
            if r >= 170 and g >= 170 and b >= 170:
                pixels[x, y] = (76, 166, 20, 255)
                continue

            edge = x < 28 or y < 28 or x >= width - 28 or y >= height - 28
            grayish = abs(r - g) <= 26 and abs(g - b) <= 26 and abs(r - b) <= 26
            if edge and grayish and r >= 155 and g >= 155 and b >= 155:
                pixels[x, y] = (76, 166, 20, 255)
                continue

            broad_gray = abs(r - g) <= 38 and abs(g - b) <= 38 and abs(r - b) <= 38
            if broad_gray and (r + g + b) // 3 >= 160:
                pixels[x, y] = (76, 166, 20, 255)
                continue

            if a == 0:
                pixels[x, y] = (76, 166, 20, 255)

    return image


def main():
    sheet = Image.open(SHEET).convert("RGBA")
    cell_w = sheet.width // 4
    cell_h = sheet.height // 2

    names = [
        "GrassTile",
        "BuildableGrassTile",
        "RoadHorizontal",
        "RoadVertical",
        "RoadCornerNE",
        "RoadCornerWS",
        "RoadCornerES",
        "RoadCornerWN",
        "ForestBorderTile",
    ]

    cells = [
        (0, 0),
        (0, 0),
        (1, 0),
        (2, 0),
        (3, 0),
        (0, 1),
        (1, 1),
        (2, 1),
        (3, 1),
    ]

    for name, (col, row) in zip(names, cells):
        cell = sheet.crop((col * cell_w, row * cell_h, (col + 1) * cell_w, (row + 1) * cell_h))
        tile = cell.crop(find_content_box(cell))
        save(tile, name)

    # Backwards-compatible fallback for older runtime names.
    save(Image.open(OUT / "RoadHorizontal.png").convert("RGBA"), "StonePathTile")
    save(Image.open(OUT / "RoadCornerNE.png").convert("RGBA"), "RoadCorner")


if __name__ == "__main__":
    main()

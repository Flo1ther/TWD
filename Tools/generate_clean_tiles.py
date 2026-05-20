from pathlib import Path
from random import Random

from PIL import Image, ImageDraw


OUT = Path(r"D:\UnityProject\TWD\Assets\Resources\TowerDefense\Sprites")
SIZE = 256


def grass_tile():
    rng = Random(11)
    image = Image.new("RGBA", (SIZE, SIZE), (86, 144, 62, 255))
    draw = ImageDraw.Draw(image)
    for _ in range(70):
        x = rng.randrange(12, SIZE - 20)
        y = rng.randrange(12, SIZE - 20)
        color = rng.choice([(64, 116, 60, 255), (116, 174, 70, 255), (76, 132, 58, 255)])
        draw.rectangle((x, y, x + rng.randrange(3, 7), y + rng.randrange(10, 20)), fill=color)
    for _ in range(8):
        x = rng.randrange(18, SIZE - 30)
        y = rng.randrange(18, SIZE - 30)
        draw.ellipse((x, y, x + rng.randrange(8, 18), y + rng.randrange(6, 14)), fill=(132, 139, 119, 255))
    return image


def path_tile():
    rng = Random(21)
    image = Image.new("RGBA", (SIZE, SIZE), (125, 111, 88, 255))
    draw = ImageDraw.Draw(image)
    # Soft dirt base.
    for _ in range(80):
        x = rng.randrange(0, SIZE)
        y = rng.randrange(0, SIZE)
        r = rng.randrange(4, 12)
        color = rng.choice([(105, 91, 72, 255), (146, 130, 101, 255), (96, 84, 68, 255)])
        draw.ellipse((x - r, y - r, x + r, y + r), fill=color)
    # Readable stepping stones.
    stones = [(42, 38), (118, 62), (188, 40), (74, 132), (158, 146), (218, 120), (44, 212), (128, 210), (206, 204)]
    for x, y in stones:
        w = rng.randrange(34, 54)
        h = rng.randrange(22, 38)
        draw.rounded_rectangle((x - w // 2, y - h // 2, x + w // 2, y + h // 2), radius=8, fill=(143, 164, 162, 255), outline=(77, 91, 90, 255), width=4)
        draw.line((x - w // 4, y, x + w // 4, y - 2), fill=(178, 196, 192, 255), width=2)
    return image


def save(name, image):
    OUT.mkdir(parents=True, exist_ok=True)
    image.save(OUT / name)
    print(OUT / name)


if __name__ == "__main__":
    save("GrassTile.png", grass_tile())
    save("StonePathTile.png", path_tile())

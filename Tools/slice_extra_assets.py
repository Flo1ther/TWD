from pathlib import Path

from PIL import Image

from slice_generated_assets import remove_checker_background, trim_alpha


ROOT = Path(r"D:\UnityProject\TWD")
DOWNLOADS = Path(r"C:\Users\shevc\Downloads")
OUT = ROOT / "Assets" / "Resources" / "TowerDefense" / "Sprites"


def save_sprite(sheet_path, box, out_name, trim=True):
    image = Image.open(sheet_path).convert("RGBA")
    sprite = remove_checker_background(image.crop(box))
    if trim:
        sprite = trim_alpha(sprite, padding=8)
    out_path = OUT / out_name
    out_path.parent.mkdir(parents=True, exist_ok=True)
    sprite.save(out_path)
    print(out_path)


def main():
    save_sprite(
        DOWNLOADS / "Gemini_Generated_Image_9e24aw9e24aw9e24.png",
        (350, 130, 1680, 1970),
        "ArcherTowerAlt.png",
    )

    sheet = DOWNLOADS / "Gemini_Generated_Image_ixuu7zixuu7zixuu.png"
    save_sprite(sheet, (0, 0, 1024, 1024), "EntryMarker.png")
    save_sprite(sheet, (1024, 1024, 2048, 2048), "BaseMarker.png")


if __name__ == "__main__":
    main()

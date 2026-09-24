"""Fit the generated portrait background under the artwork's actual oval opening."""
from collections import deque
from pathlib import Path
from PIL import Image, ImageFilter, ImageOps

ROOT = Path(__file__).resolve().parents[1]
ART = ROOT / "GUI/SpriteParts/afdui_console_base_option_02_walnut_original_ratio.png"
SOURCE = ROOT / "assets/source/portrait-background-generated.png"


def aperture_mask(art):
    alpha = art.getchannel("A")
    mask = Image.new("L", art.size)
    seed = (round(art.width * 0.145), round(art.height * 0.47))
    assert alpha.getpixel(seed) < 128
    queue = deque([seed])
    mask.putpixel(seed, 255)
    while queue:
        x, y = queue.popleft()
        for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= nx < art.width and 0 <= ny < art.height and mask.getpixel((nx, ny)) == 0 and alpha.getpixel((nx, ny)) < 128:
                mask.putpixel((nx, ny), 255)
                queue.append((nx, ny))
    box = mask.getbbox()
    assert box[0] > 0 and box[1] > 0 and box[2] < art.width and box[3] < art.height, "Opening must be enclosed by the frame"
    return mask


def main():
    art = Image.open(ART).convert("RGBA")
    mask = aperture_mask(art)
    # Extend four source pixels underneath the gold rim to eliminate filtered edge gaps.
    padded = mask.filter(ImageFilter.MaxFilter(9))
    box = padded.getbbox()
    size = (box[2] - box[0], box[3] - box[1])
    background = ImageOps.fit(Image.open(SOURCE).convert("RGB"), size, method=Image.Resampling.LANCZOS).convert("RGBA")
    background.putalpha(padded.crop(box))
    background.save(ROOT / "GUI/SpriteParts/afdui_portrait_background.png")
    print("Background XML x/y/w/h:", [round(v, 6) for v in (box[0] * 1440 / art.width, box[1] * 283 / art.height, size[0] * 1440 / art.width, size[1] * 283 / art.height)])
    preview = Image.new("RGBA", art.size)
    preview.alpha_composite(background, (box[0], box[1]))
    preview.alpha_composite(art)
    preview = Image.alpha_composite(Image.new("RGBA", art.size, (39, 45, 43, 255)), preview)
    preview.crop((95, 0, 450, 375)).save(ROOT / "artifacts/portrait-background-fitted-preview.png")


if __name__ == "__main__":
    main()

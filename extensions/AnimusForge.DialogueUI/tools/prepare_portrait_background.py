"""Derive an opaque, borderless portrait backing from the existing UI material."""
from pathlib import Path
from PIL import Image, ImageEnhance, ImageFilter, ImageOps

ROOT = Path(__file__).resolve().parents[1]
source = Image.open(ROOT / "GUI/SpriteParts/afdui_input_panel.png").convert("RGB")
# Exclude all gold framing; only the interior material belongs behind the face.
material = ImageOps.fit(source.crop((40, 30, 472, 162)), (256, 256))
material = ImageEnhance.Brightness(material).enhance(1.16).filter(ImageFilter.GaussianBlur(0.6))
shade = Image.new("L", material.size)
shade.putdata([int(58 * min(1.0, ((x - 127.5) ** 2 + (y - 112) ** 2) / 21000))
               for y in range(256) for x in range(256)])
material = Image.composite(Image.new("RGB", material.size, (12, 8, 5)), material, shade)
material.convert("RGBA").save(ROOT / "GUI/SpriteParts/afdui_portrait_background.png")

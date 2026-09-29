"""Draw the translucent hover wedges for the scene wheel, one sprite per clickable sector.

Each sprite is cropped to its sector's bounding box; the layout generator places it with wedge_box().
"""
from pathlib import Path
from PIL import Image, ImageChops, ImageDraw, ImageFilter
from wheel_geometry import ART_SIZE, INNER, OUTER, SECTORS

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'GUI/SpriteParts'
SS = 2  # supersampling
FILL = (196, 128, 40, 105)
EDGE = (255, 214, 120, 235)


def wedge_mask(name):
    start, end = SECTORS[name]
    size = ART_SIZE * SS
    c = size / 2
    mask = Image.new('L', (size, size), 0)
    d = ImageDraw.Draw(mask)
    # PIL angles run clockwise from +x; math angles run counter-clockwise.
    d.pieslice((c - OUTER * SS, c - OUTER * SS, c + OUTER * SS, c + OUTER * SS), 360 - end, 360 - start, fill=255)
    d.ellipse((c - INNER * SS, c - INNER * SS, c + INNER * SS, c + INNER * SS), fill=0)
    return mask.resize((ART_SIZE, ART_SIZE), Image.LANCZOS)


def wedge_box(name):
    return wedge_mask(name).getbbox()


def build(name):
    mask = wedge_mask(name)
    edge = ImageChops.subtract(mask, mask.filter(ImageFilter.MinFilter(7)))
    im = Image.new('RGBA', (ART_SIZE, ART_SIZE), (0, 0, 0, 0))
    # The source colours already carry their alpha; the masks only shape them.
    im.paste(Image.new('RGBA', im.size, FILL), (0, 0), mask)
    im.paste(Image.new('RGBA', im.size, EDGE), (0, 0), edge)
    box = mask.getbbox()
    im.crop(box).save(OUT / ('afdui_wheel_wedge_' + name + '.png'))
    print('wrote', name, box)


if __name__ == '__main__':
    for sector in SECTORS:
        if sector != 'rumor':
            build(sector)

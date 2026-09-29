"""Draw the two small auxiliary-panel glyphs (Pen lucide 'search' and 'coins') as sprites."""
from pathlib import Path
from PIL import Image, ImageDraw

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'GUI/SpriteParts'
SIZE, SS = 64, 4  # final pixels, supersampling factor


def canvas():
    return Image.new('RGBA', (SIZE * SS, SIZE * SS), (0, 0, 0, 0))


def save(image, name):
    image.resize((SIZE, SIZE), Image.LANCZOS).save(OUT / (name + '.png'))
    print('wrote', OUT / (name + '.png'))


def search():
    im = canvas(); d = ImageDraw.Draw(im); s = SS
    color = (134, 99, 60, 255)  # Pen #86633C
    w = 6 * s
    d.ellipse((8 * s, 8 * s, 44 * s, 44 * s), outline=color, width=w)
    d.line((40 * s, 40 * s, 56 * s, 56 * s), fill=color, width=w)
    d.ellipse((53 * s, 53 * s, 59 * s, 59 * s), fill=color)
    save(im, 'afdui_icon_search')


def coin():
    im = canvas(); d = ImageDraw.Draw(im); s = SS
    rim, face, mark = (129, 91, 50, 255), (201, 160, 88, 255), (129, 91, 50, 255)  # Pen #815B32
    d.ellipse((4 * s, 4 * s, 60 * s, 60 * s), fill=rim)
    d.ellipse((10 * s, 10 * s, 54 * s, 54 * s), fill=face)
    d.ellipse((18 * s, 18 * s, 46 * s, 46 * s), outline=mark, width=3 * s)
    save(im, 'afdui_icon_coin')


def tint(name, rgba):
    # Flat row-state tint; stretched by the widget, so a tiny texture is enough.
    Image.new('RGBA', (8, 8), rgba).save(OUT / (name + '.png'))
    print('wrote', OUT / (name + '.png'))


search()
coin()
# Pen selected row fill #B8935433; hover is a lighter step, normal is fully transparent.
tint('afdui_row_normal', (0, 0, 0, 0))
tint('afdui_row_hover', (184, 147, 84, 30))
tint('afdui_row_selected', (184, 147, 84, 51))

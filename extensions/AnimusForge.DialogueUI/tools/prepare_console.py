from collections import deque
from pathlib import Path
from PIL import Image

root = Path(__file__).resolve().parents[1]
source = root / 'outputs' / 'console-direct-reference.png'
destination = root / 'GUI' / 'SpriteParts' / 'afdui_console_base.png'
source = source if source.exists() else destination
with Image.open(source) as image:
    im = image.convert('RGBA')
pix = im.load(); width, height = im.size

# Remove the neutral checkerboard when the original source is available.
for y in range(height):
    for x in range(width):
        r, g, b, a = pix[x, y]
        if max(r, g, b) - min(r, g, b) < 5 and r > 145:
            pix[x, y] = (r, g, b, 0)

# Keep only the connected console body; this removes isolated white flecks.
seen = bytearray(width * height)
components = []
for y in range(height):
    for x in range(width):
        index = y * width + x
        if seen[index] or pix[x, y][3] == 0:
            continue
        queue = deque([(x, y)])
        seen[index] = 1
        component = []
        while queue:
            cx, cy = queue.popleft()
            component.append((cx, cy))
            for nx, ny in ((cx - 1, cy), (cx + 1, cy), (cx, cy - 1), (cx, cy + 1),
                           (cx - 1, cy - 1), (cx + 1, cy - 1), (cx - 1, cy + 1), (cx + 1, cy + 1)):
                if 0 <= nx < width and 0 <= ny < height:
                    ni = ny * width + nx
                    if not seen[ni] and pix[nx, ny][3] != 0:
                        seen[ni] = 1
                        queue.append((nx, ny))
        components.append(component)
if components:
    keep = set(max(components, key=len))
    for y in range(height):
        for x in range(width):
            if pix[x, y][3] and (x, y) not in keep:
                r, g, b, _ = pix[x, y]
                pix[x, y] = (r, g, b, 0)

# Remove near-white pixels that touch transparency. These are the baked matte fringe.
for _ in range(3):
    cut = []
    for y in range(height):
        for x in range(width):
            r, g, b, a = pix[x, y]
            if not a or min(r, g, b) < 110 or max(r, g, b) - min(r, g, b) > 95:
                continue
            if any(nx < 0 or ny < 0 or nx >= width or ny >= height or pix[nx, ny][3] == 0
                   for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1),
                                  (x - 1, y - 1), (x + 1, y - 1), (x - 1, y + 1), (x + 1, y + 1))):
                cut.append((x, y))
    for x, y in cut:
        r, g, b, _ = pix[x, y]
        pix[x, y] = (r, g, b, 0)
    if not cut:
        break

# Fill the arch opening with a true black matte.  The 3D tableau is rendered
# above this layer; filling the opening prevents a purple/scene-colour seam from
# showing through the one-pixel cutout edge.
cx, cy, rx, ry = 373, 180, 145, 150
for y in range(35, 391):
    if y < cy:
        t = (y - cy) / ry
        half = rx * (max(0.0, 1.0 - t * t) ** 0.5)
        left, right = int(cx - half), int(cx + half)
    else:
        left, right = 232, 513
    for x in range(max(0, left), min(width, right + 1)):
        pix[x, y] = (0, 0, 0, 255)

# Close the remaining one-pixel transparent seam where the matte meets the
# inner arch. Keep the expansion bounded to the opening so the outer UI stays
# transparent.
for _ in range(4):
    seam = []
    for y in range(35, min(height, 395)):
        for x in range(220, min(width, 525)):
            if pix[x, y][3] != 0:
                continue
            if any(0 <= nx < width and 0 <= ny < height and pix[nx, ny][3] == 255
                   and pix[nx, ny][:3] == (0, 0, 0)
                   for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1))):
                seam.append((x, y))
    for x, y in seam:
        pix[x, y] = (0, 0, 0, 255)
    if not seam:
        break

# Feather only the remaining opaque edge by two pixels.
distance = [-1] * (width * height)
queue = deque()
for y in range(height):
    for x in range(width):
        if pix[x, y][3] == 0:
            index = y * width + x
            distance[index] = 0
            queue.append((x, y))
while queue:
    x, y = queue.popleft()
    next_distance = distance[y * width + x] + 1
    for nx, ny in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1),
                   (x - 1, y - 1), (x + 1, y - 1), (x - 1, y + 1), (x + 1, y + 1)):
        if 0 <= nx < width and 0 <= ny < height:
            index = ny * width + nx
            if distance[index] == -1:
                distance[index] = next_distance
                queue.append((nx, ny))
for y in range(height):
    for x in range(width):
        r, g, b, a = pix[x, y]
        if a and distance[y * width + x] == 1:
            pix[x, y] = (r, g, b, 170)
        elif a and distance[y * width + x] == 2:
            pix[x, y] = (r, g, b, 228)

destination.parent.mkdir(parents=True, exist_ok=True)
im.save(destination, 'PNG', optimize=True)
print(f'{destination} {im.size} {im.mode}')

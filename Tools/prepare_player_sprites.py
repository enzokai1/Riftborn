"""Extract aligned Unity sprite frames from the original sheet, retaining a backup.

Requires Pillow. Run from the repository root after approving scripted image processing.
The original sheet is preserved; output is written to Assets/Art/Player/Frames.
"""
from collections import deque
from pathlib import Path
import re
import uuid
from PIL import Image, ImageOps

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / 'Assets/Art/Player/ChatGPT Image 15 de set. de 2026, 21_49_35.png'
OUTPUT = ROOT / 'Assets/Art/Player/Frames'


def remove_background(image):
    image = image.convert('RGBA')
    width, height = image.size
    pixels = list(image.getdata())
    # Background is pale neutral gray; dark outlines and saturated colors are protected.
    candidates = bytearray(min(p[:3]) >= 110 and max(p[:3]) - min(p[:3]) <= 38 for p in pixels)
    visited = bytearray(width * height)
    queue = deque()
    for x in range(width):
        queue.extend((x, (height - 1) * width + x))
    for y in range(height):
        queue.extend((y * width, y * width + width - 1))
    while queue:
        index = queue.popleft()
        if visited[index] or not candidates[index]:
            continue
        visited[index] = 1
        x, y = index % width, index // width
        if x: queue.append(index - 1)
        if x + 1 < width: queue.append(index + 1)
        if y: queue.append(index - width)
        if y + 1 < height: queue.append(index + width)
    image.putdata([(r, g, b, 0 if visited[i] else a) for i, (r, g, b, a) in enumerate(pixels)])
    return image


def write_meta(path, pixels_per_unit=100):
    meta = Path(str(path) + '.meta')
    if meta.exists():
        return
    template = (ROOT / 'Assets/Art/Environment/HouseWoodFloor.png.meta').read_text()
    template = re.sub(r'^guid: .*', 'guid: ' + uuid.uuid4().hex, template, count=1, flags=re.M)
    template = re.sub(r'  spritePixelsToUnits: \d+', f'  spritePixelsToUnits: {pixels_per_unit}', template)
    meta.write_text(template)


def frame(sheet, box, body_center, normalize_height=False):
    cutout = remove_background(sheet.crop(box))
    anchor = body_center - box[0]
    if normalize_height:
        scale = 148 / cutout.height
        cutout = cutout.resize((round(cutout.width * scale), 148), Image.Resampling.NEAREST)
        anchor = round(anchor * scale)
    canvas = Image.new('RGBA', (256, 160))
    canvas.paste(cutout, (128 - anchor, 0))
    return canvas


def main():
    sheet = Image.open(SOURCE).convert('RGBA')
    if sheet.size != (1254, 1254):
        raise ValueError('Sprite coordinates require the original 1254 x 1254 sheet.')
    OUTPUT.mkdir(parents=True, exist_ok=True)
    folder_meta = Path(str(OUTPUT) + '.meta')
    if not folder_meta.exists():
        folder_meta.write_text('fileFormatVersion: 2\nguid: ' + uuid.uuid4().hex + '\nfolderAsset: yes\n')
    transparent = remove_background(sheet)
    transparent_path = SOURCE.with_name('PlayerSheetTransparent.png')
    transparent.save(transparent_path)
    write_meta(transparent_path)

    frames = {}
    for direction, positions in {'Down': [26, 98, 171, 244], 'Up': [332, 404, 476, 548],
                                 'Right': [946, 1021, 1096, 1171]}.items():
        for i, x in enumerate(positions):
            frames[f'Walk{direction}_{i}'] = frame(sheet, (x, 616, x + 72, 764), x + 36)
    # Use one consistent side cycle, mirrored, so both directions have matching strides.
    for i in range(4):
        frames[f'WalkLeft_{i}'] = ImageOps.mirror(frames[f'WalkRight_{i}'])
    attacks = {
        'Down': [(20, 100, 60), (100, 244, 135), (246, 314, 280)],
        'Up': [(330, 408, 368), (408, 550, 444), (552, 622, 585)],
        'Right': [(944, 1094, 984), (1095, 1170, 1130), (1171, 1248, 1208)],
    }
    for direction, poses in attacks.items():
        for i, (left, right, center) in enumerate(poses):
            frames[f'Shoot{direction}_{i}'] = frame(sheet, (left, 868, right, 1006), center, True)
    for i in range(3):
        frames[f'ShootLeft_{i}'] = ImageOps.mirror(frames[f'ShootRight_{i}'])
    for name, image in frames.items():
        path = OUTPUT / (name + '.png')
        image.save(path)
        write_meta(path)
    print(f'Created {len(frames)} transparent frames in {OUTPUT}')


if __name__ == '__main__':
    main()

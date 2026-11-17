"""Build a Portuguese Alagard variant, retaining the original outlines and metrics.

Requires fonttools. Run from the project root; the source TTF is never overwritten.
"""
from pathlib import Path
import unicodedata
from fontTools.ttLib import TTFont
from fontTools.pens.ttGlyphPen import TTGlyphPen

ROOT = Path(__file__).resolve().parents[2]
SOURCE = ROOT / 'Assets/Resources/UI/Fonts/alagard.ttf'
OUTPUT = SOURCE.with_name('alagard-riftborn-pt.ttf')
CELL = 64  # Original Alagard outlines use a 64-unit pixel grid (1024 UPM).
MARKS = {
    '\u0301': ('001', '010'),
    '\u0300': ('100', '010'),
    '\u0302': ('010', '101'),
    '\u0303': ('01101', '10010'),
    '\u0308': ('101',),
    '\u0327': ('010', '001', '110'),
}


def build():
    font = TTFont(SOURCE)
    cmap = font.getBestCmap().copy()
    original_order = font.getGlyphOrder()[:]
    order = original_order[:]
    glyphs = font['glyf']
    glyph_set = font.getGlyphSet()
    additions = {}
    for code in range(0xC0, 0x100):
        parts = unicodedata.normalize('NFD', chr(code))
        if code in cmap or len(parts) != 2 or parts[1] not in MARKS or ord(parts[0]) not in cmap:
            continue
        base_name = cmap[ord(parts[0])]
        base = glyphs[base_name]
        pen = TTGlyphPen(glyph_set)
        if parts[0] == 'i':
            # Keep the original stem contours, replacing the detached dot with the accent.
            coordinates, ends, flags = base.getCoordinates(glyphs)
            start = 0
            for end in ends:
                contour = coordinates[start:end + 1]
                start = end + 1
                if min(y for x, y in contour) >= 512:
                    continue
                pen.moveTo(contour[0])
                for point in contour[1:]:
                    pen.lineTo(point)
                pen.closePath()
            top = 448
        else:
            glyph_set[base_name].draw(pen)
            top = base.yMax
        pattern = MARKS[parts[1]]
        width = len(pattern[0]) * CELL
        left = round(((base.xMin + base.xMax - width) / 2) / CELL) * CELL
        bottom = -len(pattern) * CELL if parts[1] == '\u0327' else top + CELL
        for row, cells in enumerate(pattern):
            for column, value in enumerate(cells):
                if value != '1':
                    continue
                x = left + column * CELL
                y = bottom + (len(pattern) - row - 1) * CELL
                pen.moveTo((x, y))
                pen.lineTo((x, y + CELL))
                pen.lineTo((x + CELL, y + CELL))
                pen.lineTo((x + CELL, y))
                pen.closePath()
        name = f'uni{code:04X}'
        glyphs[name] = pen.glyph()
        glyphs[name].recalcBounds(glyphs)
        font['hmtx'][name] = (font['hmtx'][base_name][0], glyphs[name].xMin)
        order.append(name)
        additions[code] = name
    font.setGlyphOrder(order)
    for table in font['cmap'].tables:
        if table.isUnicode():
            table.cmap.update(additions)
    # Prevent clipping tall accents and cedillas without reducing the glyph shapes.
    highest = max(glyphs[n].yMax for n in additions.values())
    lowest = min(glyphs[n].yMin for n in additions.values())
    font['hhea'].ascent = max(font['hhea'].ascent, highest)
    font['hhea'].descent = min(font['hhea'].descent, lowest)
    font['OS/2'].usWinAscent = max(font['OS/2'].usWinAscent, highest)
    font['OS/2'].usWinDescent = max(font['OS/2'].usWinDescent, -lowest)
    names = {1: 'Alagard Riftborn PT', 2: 'Regular', 3: 'Alagard Riftborn PT 1.0',
             4: 'Alagard Riftborn PT', 6: 'AlagardRiftbornPT-Regular'}
    for record in font['name'].names:
        if record.nameID in names:
            record.string = names[record.nameID].encode(record.getEncoding())
    font.save(OUTPUT)

    result = TTFont(OUTPUT)
    original = TTFont(SOURCE)
    for name in original_order:
        assert result['glyf'][name].compile(result['glyf']) == original['glyf'][name].compile(original['glyf']), name
        assert result['hmtx'][name] == original['hmtx'][name], name
    required = 'ÁÀÂÃÉÊÍÓÔÕÚÜÇáàâãéêíóôõúüç'
    assert all(ord(c) in result.getBestCmap() for c in required)
    assert all(result.getBestCmap()[c] == n for c, n in cmap.items())
    print(f'{len(additions)} accented glyphs added; original outlines and spacing verified.')
    print(OUTPUT)


if __name__ == '__main__':
    build()

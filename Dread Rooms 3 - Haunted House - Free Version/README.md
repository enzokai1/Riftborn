# Dread Rooms #3 — Haunted House — Free Version

82 files from Dread Rooms #3 — Haunted House, a top-down horror pixel art asset pack: enough to build one room of the house on Halloween night.
The full pack has every wall material, floor and stair style, hundreds of pieces of furniture in three states,
the whole cast and every animated object.

## Sizes — read this first

**Every tile is 16×16 pixels.** The files are included at three sizes, each an exact enlargement with no blur:
`16x16/`, `32x32/` and `48x48/`. Every file is a whole number of tiles wide and tall, with a transparent
background. `index.csv` lists every file with its size, its set, its state and where it goes.

## What is in it

| folder | what | count |
|---|---|---|
| `room-builder/` | the floral wallpaper walls, the hardwood floor, the wall-top border, manor stairs (clean and horror), a doormat (the wall guide is in `guides/`) | 34 |
| `house/furniture/` | an armchair, a fireplace, a dining table and chairs, a bed, a wardrobe, a grandfather clock, a candelabra | 24 |
| `house/halloween/` | jack-o'-lanterns, a bat garland, table candles | 8 |
| `house/wall-props/` | a door and a window | 6 |
| `house/floor-overlays/` and `wall-overlays/` | blood and debris to lay on floors and walls | 3 + 3 |
| `house/animated/` | a flickering jack-o'-lantern and dripping wall blood, one strip each (8 frames) | 2 |
| `characters/babysitter/` | `walk.png` (6 frames) and `idle.png` (4 frames); rows down, left, right, up | 2 sheets |

Furniture and wall props are sorted into `horror/`, `decayed/` and `clean/`.

## Building a room

Open `guides/guide_floral.png` (next to this README): every wall piece with its file name, and a room built from them. A wall
is 2 tiles tall; `band/` is the wall-top border that closes the room at the sides and the bottom; `floor_shadow.png`
goes on the floor row right under a back wall. Props stand on the bottom edge of their file; wall props come in a
file as tall as the wall, so lining the file up with the wall hangs them at the right height.

## Using it in an engine

The Godot 4 and Tiled files are included and tested (Godot 4.4, Tiled 1.11): `godot/dread-rooms.tres` is a TileSet with
every piece here (put props on their own TileMapLayer above the floor), with a terrain brush for the wall-top border;
`godot/characters/` and `godot/animated/` hold SpriteFrames; `tiled/` has a tileset per folder. Any other engine: use
the PNGs, or `16x16/sheets/` with `atlas.json`. Use nearest filtering. The full pack also has a ready sample room
for Godot, plain and lit with Godot lights.

## Made with AI-assisted tools

The art was made with AI-assisted tools, then picked, cleaned, sorted and checked by me.

## Licence

See `LICENSE.txt`. The free version can be used in your projects on the same terms as the full pack.

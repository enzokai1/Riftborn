# Dread Rooms first-level floor

Source: Dread Rooms 3 - Haunted House - Free Version, by PixLog.
Original pack: Tools/ArtSources/Packs/DreadRooms3 (including its license and guides).
Uses 16x16/room-builder/floors/hardwood/floor.png, floor_alt.png and floor_damaged.png.
License retained at Assets/Resources/Art/DreadRoom/LICENSE.txt.

BuildDreadFloor.ps1 packs the original pixels into a deterministic 512x512 tile
variation. No resizing or colour changes. At 16 pixels per unit, one source tile
occupies one world unit, matching the walls. The full pattern repeats every 32 units.
Damaged boards are decorative and have no collision or damage behaviour.
Point filtering, no mipmaps, uncompressed default import, Full Rect sprite mesh.
The existing asset GUID, scene reference and tiled SpriteRenderer are preserved.

Previous texture: Tools/ArtBackups/HouseWoodFloorBeforeDreadRooms.png.
Previous import settings: 256 pixels per unit and spriteMeshType 0.

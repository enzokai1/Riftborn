# Alagard Riftborn PT

Source: `Assets/Resources/UI/Fonts/alagard.ttf`, supplied by the user.
Original embedded author credit: Pix3M (retained in the derived font).
Output: `Assets/Resources/UI/Fonts/alagard-riftborn-pt.ttf`.

The variant adds 51 Latin-1 accented characters using the original letter outlines
and 64-unit square accent pixels on the font's native 1024-unit grid. Existing
glyph outlines, character mappings and advances are preserved. Accented i uses
the original stem with its detached dot replaced by the accent. Portuguese acute,
grave, circumflex, tilde, diaeresis and cedilla are included, in both cases.

Generate with FontTools installed: `python Tools/ArtSources/build_alagard_pt.py`.
The script reloads the generated font and verifies the original outlines, spacing,
character mappings and Portuguese character coverage. No system font substitution
is required for these precomposed accented characters. The source is unchanged.

The HUD draws all text passes in the normal GUIStyle state, avoiding hover-state
colors on outline/shadow layers. Button hover remains on the button background.

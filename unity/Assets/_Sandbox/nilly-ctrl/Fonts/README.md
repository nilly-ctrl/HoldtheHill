# Pixel fonts

Three hand-drawn alphabets for Hold the Hill, made to sit beside the 32x32 icons. Each comes as a plain TTF (tint it yourself) and as baked colour styles (colours, outline and shadow already drawn in).

| Alphabet | Size | Has | Use it for |
|---|---|---|---|
| Body | 5x7 | Capitals, lowercase, accents, symbols, key and gamepad prompts | HUD and menu text, damage numbers |
| Display | 8x12, two-pixel strokes | Capitals, digits, basic punctuation | Headings, wave banners, combo counter |
| Tiny | 3x5 | Capitals, digits, basic punctuation | Costs, bar values, tooltips |

Look at `Source~/Specimen.png` for every glyph, `Source~/FontSheet.png` for the styles and `Source~/ThemeCompare.png` for the themed sets.

## Plain fonts (`TTF/`)

| File | Pixel-exact at size | Notes |
|---|---|---|
| `HoldTheHillPixel-Regular.ttf`, `-Bold.ttf` | 10, 20, 30 | Line height is 10 pixels (it was 9 before lowercase was added). |
| `HoldTheHillDisplay-Regular.ttf` | 20, 40 | Lowercase types as capitals. |
| `HoldTheHillTiny-Regular.ttf` | 10, 20 | Lowercase types as capitals. |

- **Lowercase is real now.** Mixed-case strings used to show as capitals; they now show as written. Write a string in capitals if you want capitals.
- **Digits share one width**, so counters and timers do not shift as they change.
- **Kerning** is in the files (pairs such as `To`, `LT`, `P.`). TextMeshPro can read it; Unity's legacy Text and IMGUI ignore kerning.
- **Symbols and prompts** have names in `DamageNumbers/PixelGlyphs.cs`, for example `$"{PixelGlyphs.KeySpace} START WAVE"` or `PixelGlyphs.Key('e')`. Body font only. The prompts are at U+E000 and up; the table is at the bottom of `Specimen.png`.
- Accented capitals rise two pixels above the line, so leave room above a heading that may contain one.

## Baked styles (`Atlases/`)

Capitals, digits and `+ - . , % ! ? / : x × ( )` only. Use `K` or `M` for thousands and millions.

| Style | Alphabet | Use it for |
|---|---|---|
| `DamageNormal` | Body | Physical hits, MISS / BLOCK |
| `DamageCrit` | Body, 2x | Critical hits. Spawn at 1x and punch-scale. |
| `DamageFire`, `DamagePoison`, `DamageLightning`, `DamageMagic`, `DamageTrue` | Body | Damage by type |
| `Heal` | Body | Healing on ants and the Queen |
| `Resource` | Body | Food gained or spent, rewards |
| `Hud` | Body | Plain cream counters |
| `Heading` | Display | Headings and banner sub-lines |
| `Combo` | Display, slanted | Combo counter and its label |
| `Banner`, `BannerVictory`, `BannerDefeat` | Display, 2x | WAVE 3, WAVE CLEAR, VICTORY, DEFEAT |
| `TinyLabel`, `TinyCost` | Tiny | Small labels and build-bar costs |

Each style is `<Style>.png` plus `<Style>.json` (glyph rects, advances, kerning pairs, and `digitAdvance` for fixed-width counters).

**Themed sets:** `Atlases/Themes/<Theme>/` holds the same 17 styles painted from each of the 13 art themes' palettes (Neon, Space, Spooky, Steampunk, Medieval, Samurai, Candy, Jungle, Pirate, Robot, NeonSpace, Vaporwave, Military). Shapes and file names match the base set, so a theme swaps in by folder. Banners, the combo counter and neutral text change most; damage colours change only where a theme has its own fire, acid or arcane colours.

## Seven more alphabets

Added 2026-10-06, drawn in `Source~/glyphs_faces.py` and built by `Source~/build_faces.py`. See `Source~/SpecimenFaces.png`, `FaceSheet.png` and `ThemeTitles.png`.

| Alphabet | TTF | Has | Character |
|---|---|---|---|
| Serif | `HoldTheHillSerif-Regular.ttf` | Capitals, lowercase | Foot serifs; lore, menus, storybook titles |
| Round | `HoldTheHillRound-Regular.ttf` | Capitals, lowercase | Two-pixel strokes, soft corners; friendly menus |
| Tech | `HoldTheHillTech-Regular.ttf` | Capitals | Squared-off; readouts |
| Gothic | `HoldTheHillGothic-Regular.ttf` | Capitals | Heavy stems, pointed feet; bosses |
| Condensed | `HoldTheHillCondensed-Regular.ttf` | Capitals | Three pixels wide, nine tall; tight spaces |
| Chisel | `HoldTheHillChisel-Regular.ttf` | Capitals | Straight cuts and diamonds |
| Stencil | `HoldTheHillStencil-Regular.ttf` | Capitals | The display alphabet with stencil bridges |

All are pixel-exact at size 10, 20, 30 except Stencil (20, 40). They have letters, digits and `+ - . , % ! ? / : × ' ( )`; no accents, symbols or prompts.

**Colour treatments** (`PAINTS` in `build_faces.py`): Plain, Gold, Chrome, Neon (with a glow), Ice, Blood, Bone, Candy (striped), Jade, Hollow (outline only) and Stamp (flat, no outline). Any treatment can go on any alphabet; add a row to `FACE_STYLES` to bake another pairing. Baked now: `SerifPlain`, `SerifGold`, `RoundPlain`, `RoundCandy`, `TechPlain`, `TechNeon`, `TechChrome`, `GothicBone`, `GothicBlood`, `CondensedPlain`, `CondensedIce`, `ChiselPlain`, `ChiselJade`, `StencilStamp`, `StencilHollow`, `DisplayChrome`, `DisplayNeon`.

**A Title and a Label per theme:** every theme folder (and the base set) also has `Title` (large, in the theme's banner colours) and `Label` (small and plain), set in the alphabet that suits the theme (`THEME_FACE` in `build_faces.py`): Round for Meadow and Candy; Tech for Neon, Space and NeonSpace; Gothic for Spooky; Serif for Steampunk, Medieval and Pirate; Chisel for Samurai and Jungle; Stencil for Robot and Military; Condensed for Vaporwave. `Label` works for damage numbers in a theme's own alphabet.

## In Unity

1. Run **Hold the Hill > Sandbox > Build Pixel Fonts** after the atlases change. It makes one `PixelFontStyle` asset per style in `Generated/`, and `Generated/Themes/<Theme>/` for the themed sets.
2. The floating damage numbers use those assets (see `DamageNumbers/README.md`). `PixelFontBuilder.Configure(spawner, pixelScale, "Neon")` points a spawner at a theme's set.
3. For HUD or menu text use a TTF. Keep to the pixel-exact sizes above and scale by whole numbers.

`Editor/PixelIconImporter.cs` imports the atlases with point filtering and no compression. `Editor/PixelFontImporter.cs` imports the TTFs with hinted raster rendering so the edges stay hard.

Nothing in the game uses the Display or Tiny alphabets, the banner and combo styles, or the themed sets yet.

## Changing things

Needs Python 3 with Pillow and fontTools. From `Source~/`:

```
python build_pixel_fonts.py --check
```

- A colour style: edit its entry in `STYLES` in `build_pixel_fonts.py`. Themed colours come from `theme_styles()` and the palettes in `Animations/Source~/build_theme_sheets.py`.
- A letter: edit `glyphs_body.py`, `glyphs_display.py` or `glyphs_tiny.py`. Prompts are built in `glyphs_buttons.py`.
- `--check` renders the finished TTF files and compares them, pixel for pixel, with the drawings. `--no-themes` skips the themed sets.

Look at the preview sheets before opening Unity.

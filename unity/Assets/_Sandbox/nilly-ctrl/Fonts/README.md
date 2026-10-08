# Pixel fonts

Sixteen hand-drawn alphabets for Hold the Hill, made to sit beside the 32x32 icons. The first three are below; the other thirteen have their own section. Each comes as a plain TTF (tint it yourself) and as baked colour styles (colours, outline and shadow already drawn in).

| Alphabet | Size | Has | Use it for |
|---|---|---|---|
| Body | 5x7 | Capitals, lowercase, accents, symbols, key and gamepad prompts | HUD and menu text, damage numbers |
| Display | 8x12, two-pixel strokes | Capitals, lowercase, digits, basic punctuation | Headings, wave banners, combo counter |
| Tiny | 3x5 | Capitals, digits, basic punctuation | Costs, bar values, tooltips |

**New here? Start with `Source~/CheatSheet.png`**: one page on which font to use where. Then look at `Source~/Specimen.png` for every glyph, `Source~/FontSheet.png` for the styles and `Source~/ThemeCompare.png` for the themed sets.

## Plain fonts (`TTF/`)

| File | Pixel-exact at size | Notes |
|---|---|---|
| `HoldTheHillPixel-Regular.ttf`, `-Bold.ttf` | 10, 20, 30 | Line height is 10 pixels (it was 9 before lowercase was added). |
| `HoldTheHillDisplay-Regular.ttf` | 20, 40 | Has lowercase since 2026-10-07. |
| `HoldTheHillTiny-Regular.ttf` | 10, 20 | Lowercase types as capitals. |

- **Lowercase is real now.** Mixed-case strings used to show as capitals; they now show as written. Write a string in capitals if you want capitals.
- **Digits share one width**, so counters and timers do not shift as they change.
- **Kerning** is in the files (pairs such as `To`, `LT`, `P.`). TextMeshPro can read it; Unity's legacy Text and IMGUI ignore kerning.
- **Symbols and prompts** have names in `DamageNumbers/PixelGlyphs.cs`, for example `$"{PixelGlyphs.KeySpace} START WAVE"` or `PixelGlyphs.Key('e')`. Body font only. The prompts are at U+E000 and up; the table is at the bottom of `Specimen.png`.
- **Game icons** sit inline with text: `PixelGlyphs.IconFood`, `IconWave`, `IconClock`, `IconShield`, `IconPoison`, `IconFire`, `IconBolt`, `IconFrost`, `IconSkull`, `IconSword`, `IconCrown`, `IconAnt`, one per tower caste (`IconTowerBeam` to `IconTowerWorker`), and `IconCoin`, `IconGem`, `IconGear`, `IconSpeaker`, `IconPause`, `IconPlay`, `IconFastForward`. They are in every TTF and in the body-alphabet baked styles (with the heart and star), so `$"{PixelGlyphs.IconFood} 1,250"` works in the Resource style.
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

## Thirteen more alphabets

Added 2026-10-06, drawn in `Source~/glyphs_faces.py` and built by `Source~/build_faces.py`. See `Source~/SpecimenFaces.png`, `FaceSheet.png` and `ThemeTitles.png`.

| Alphabet | TTF | Has | Character |
|---|---|---|---|
| Serif | `HoldTheHillSerif-Regular.ttf` | Capitals, lowercase | Foot serifs; lore, menus, storybook titles |
| Round | `HoldTheHillRound-Regular.ttf` | Capitals, lowercase | Two-pixel strokes, soft corners; friendly menus |
| Tech | `HoldTheHillTech-Regular.ttf` | Capitals, lowercase | Squared-off; readouts |
| Gothic | `HoldTheHillGothic-Regular.ttf` | Capitals, lowercase | Heavy stems, pointed feet; bosses |
| Condensed | `HoldTheHillCondensed-Regular.ttf` | Capitals, lowercase | Three pixels wide, nine tall; tight spaces |
| Chisel | `HoldTheHillChisel-Regular.ttf` | Capitals, lowercase | Straight cuts and diamonds |
| Stencil | `HoldTheHillStencil-Regular.ttf` | Capitals, lowercase | The display alphabet with breaks cut by hand into every letter (`STENCIL_CUTS`) |
| Slab | `HoldTheHillSlab-Regular.ttf` | Capitals, lowercase | Two-pixel stems between slab serifs; signs and posters |
| Bubble | `HoldTheHillBubble-Regular.ttf` | Capitals, lowercase | Three-pixel strokes; loud, soft titles |
| Wide | `HoldTheHillWide-Regular.ttf` | Capitals, small capitals for lowercase | Five pixels tall and stretched; arcade readouts |
| Deco | `HoldTheHillDeco-Regular.ttf` | Capitals, lowercase | Tall and thin with a high waist; marquees and posters |
| Runic | `HoldTheHillRunic-Regular.ttf` | Capitals, small capitals for lowercase | Straight cuts with slanted bars; carved stones |
| Script | `HoldTheHillScript-Regular.ttf` | Capitals, lowercase | A leaning casual hand; notes and signatures. No kerning. |

Each of the thirteen also has a `-Bold.ttf` and an `-Italic.ttf`, made from the regular by rule (thickened, or leant one pixel every three rows). All are pixel-exact at size 10, 20, 30 except Stencil (20, 40). The TTFs also carry accented letters, the body alphabet's symbols and spare punctuation, and the key and gamepad prompts (doubled in size for Stencil), so `PixelGlyphs` works in all of them. There is no ß outside the body font. The baked atlases hold letters, digits and `+ - . , % ! ? / : × ' ( )` only.

**Colour treatments** (`PAINTS` in `build_faces.py`): Plain, Gold, Chrome, Neon (with a glow), Ice, Blood, Bone, Candy (striped), Jade, Wood, Hollow (outline only) and Stamp (flat, no outline). Any treatment can go on any alphabet; add a row to `FACE_STYLES` to bake another pairing. Baked now: `SerifPlain`, `SerifGold`, `RoundPlain`, `RoundCandy`, `TechPlain`, `TechNeon`, `TechChrome`, `GothicBone`, `GothicBlood`, `CondensedPlain`, `CondensedIce`, `ChiselPlain`, `ChiselJade`, `StencilStamp`, `StencilHollow`, `SlabPlain`, `SlabWood`, `BubblePlain`, `BubbleCandy`, `WidePlain`, `WideNeon`, `DecoPlain`, `DecoGold`, `RunicBone`, `RunicIce`, `ScriptPlain`, `ScriptGold`, `DisplayChrome`, `DisplayNeon`.

**Animated styles:** `SerifGold`, `DecoGold`, `ScriptGold` and `BannerVictory` shimmer; `TechNeon`, `DisplayNeon` and `WideNeon` flicker; `GothicBlood` drips; `DamageCrit` pulses; `Combo` has bright rows that climb it; `Heal` sparkles. The last four are in every theme's set too. Each has six frames (`<Style>.f1.png` to `.f5.png` beside the atlas; see `Source~/AnimSheet.png` and `Anim<Style>.gif`). `PixelText` and the damage numbers play them while the game runs. To animate another style, add it to `ANIMATED` in `build_faces.py` or give its `STYLES` entry an `anim`.

**A Title and a Label per theme:** every theme folder (and the base set) also has `Title` (large, in the theme's banner colours) and `Label` (small and plain), set in the alphabet that suits the theme (`THEME_FACE` in `build_faces.py`): Round for Meadow and Candy; Tech for Neon, Space and NeonSpace; Gothic for Spooky; Serif for Steampunk, Medieval and Pirate; Chisel for Samurai and Jungle; Stencil for Robot and Military; Condensed for Vaporwave. `Label` works for damage numbers in a theme's own alphabet.

**Baking a pairing from the artifact:** the font artifact previews any alphabet in any theme's colours. To make one real, add its row to `PAIRINGS` in `build_faces.py` (the artifact writes the rows, for one style or for a whole sample screen) and run the build. A row is `("Neon", "Banner", "slab"),` or, with a colour treatment, bold or italic, `("Neon", "Banner", "slab", "Gold bold"),`. It writes `<Style><Alphabet>...png` and `.json` beside that theme's other atlases. Fifteen starter pairings are baked now (for example Spooky `BannerGothic`, Neon `HudTech`, Candy `BannerBubble`, Steampunk `BannerSlabWood`); see `Source~/PairingSheet.png`. A custom colour treatment designed in the artifact needs its line added to `PAINTS` first.

## In Unity

1. Run **Hold the Hill > Sandbox > Build Pixel Fonts** after the atlases change. It makes one `PixelFontStyle` asset per style in `Generated/`, and `Generated/Themes/<Theme>/` for the themed sets.
2. The floating damage numbers use those assets (see `DamageNumbers/README.md`). `PixelFontBuilder.Configure(spawner, pixelScale, "Neon")` points a spawner at a theme's set.
3. **`PixelText`** (Add Component > Hold the Hill > Sandbox > Pixel Text) draws a line of text in any baked style: banners, titles, labels, counters. It updates in the editor as you type and has a fixed-width digits option.
4. **`PixelFontTheme`** is the one setting for which art theme's fonts a scene uses. It switches the damage numbers and every `PixelText` whose Theme Style field names a style (Title, Label, Banner, Hud...). Leave the theme empty for the base set. With **Prefer Pairings** on (the default) a theme uses its baked pairings where it has them, so Spooky's banner comes out in Gothic. The graybox scene gets one when it is rebuilt; **Hold the Hill > Sandbox > Add Pixel Font Theme Switch To Scene** adds one anywhere else. It reads `Generated/PixelFontThemes.asset`, which Build Pixel Fonts fills in.
5. **`GrayboxWaveBanner`** (in `Graybox/`) shows WAVE 3, WAVE CLEAR, VICTORY! and DEFEAT in the theme's banner styles, with a punch-in and a fade. The graybox scene gets one when it is rebuilt. It needs a `PixelFontTheme` in the scene.
6. **`GrayboxFontThemeKey`** steps the font theme while playing: F6 for the next theme, Shift+F6 for the one before. The graybox scene gets one when it is rebuilt.
7. **`GrayboxPixelHud`** shows food, wave and run time at the top right in the pixel font, with icons and fixed-width digits; `GrayboxHud` drops those three lines from its panel while it is showing. **`GrayboxKillCombo`** shows x3, x4... in the Combo style when kills chain within 2.5 seconds. **`GrayboxStatusPopups`** shows BURN, POISON, SHOCK, FROZEN or SLOW with its icon when a status lands on an enemy, and SHIELD when one breaks. **`GrayboxBossBanner`** announces a boss by name in the dripping `GothicBlood` style and puts a small name label above it. All four come with the next graybox rebuild.
8. **Tools > Hold the Hill > Build Font Demo** makes `Fonts/FontDemo.unity`: every baked style drawn with `PixelText` beside its name. Type a theme into its Pixel Font Theme object to restyle the sheet.
9. For HUD or menu text use a TTF. Keep to the pixel-exact sizes above and scale by whole numbers.

`Editor/PixelIconImporter.cs` imports the atlases with point filtering and no compression. `Editor/PixelFontImporter.cs` imports the TTFs with hinted raster rendering so the edges stay hard.

The Unity scripts compile but have never been run, the graybox scene has not been rebuilt since they were added, and the demo scene has not been built.

## The showcase page

The font artifact's page lives in `Source~/Showcase/`, so it can be opened without a Claude account:

```
python serve.py
```

Run that from `Source~/Showcase/`, then open http://127.0.0.1:8760/. It rebuilds the page from the current fonts each time. The page tries any alphabet with any theme and colour treatment, shows them on a stand-in game screen, keeps a shortlist, and writes the rows for `PAIRINGS`.

## Changing things

Needs Python 3 with Pillow and fontTools. From `Source~/`:

```
python build_pixel_fonts.py --check
```

- A colour style: edit its entry in `STYLES` in `build_pixel_fonts.py`. Themed colours come from `theme_styles()` and the palettes in `Animations/Source~/build_theme_sheets.py`.
- A letter: edit `glyphs_body.py`, `glyphs_display.py` or `glyphs_tiny.py`. Prompts are built in `glyphs_buttons.py`.
- `--check` renders the finished TTF files and compares them, pixel for pixel, with the drawings. `--no-themes` skips the themed sets.

Look at the preview sheets before opening Unity.

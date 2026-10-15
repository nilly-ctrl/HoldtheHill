# Design gaps: the GDD against *Level Up!*

Proposal from Nilly. First written 2026-10-07 against the first ten-pager; **rechecked
2026-10-14 against the new ten-page GDD** (Drive, `Design Documentation/V2`, draft of
2026-10-14) and the cut list (`Design Documentation/Ideas/CutList`). Nothing here has been
agreed by the team; tiers and dates are suggestions.

## What changed with V2

V2 is a different game from the first draft in several ways: no Warden (the player is the
Queen's will), six towers and six enemies at Alpha, two levels (Tutorial Hill and the Meadow),
burrowing as the answer to hazards, click squash, two currencies (Food staffs towers, Honeydew
builds them), a 3/4 side camera, and a story that runs meadow to moon. Several of the old gaps
are closed or no longer apply; a few new ones came with it.

The original pass split the course book (Scott Rogers, *Level Up!*) into 1,740 pieces of advice
and scored the first draft: 404 covered, 674 partly, 347 missing, 315 not applicable. **That
count has not been re-run for V2.** This page rechecks the ten biggest gaps and adds what V2
introduces; a full re-score is a bigger job.

## The ten gaps, rechecked

| # | Gap in the first draft | In V2 | Still to do |
|---|---|---|---|
| 1 | No numbers to tune with | **Partly closed.** Enemy health, speed and bounty for all six; tower prices; click squash at about 4.2% with a 5-click cap; Stag Beetle at 900 health | Tower damage, range and fire rate; enemy damage to the Queen; Food ration size and starting ants (marked TBD); boss numbers beyond the Stag Beetle. The book wants one stat matrix and a time-to-kill check before any wave is tuned |
| 2 | No master beat chart | **Open.** Wave contents are "TBD: wave spreadsheet"; the Meadow has a "variable" number of waves | A wave-by-wave chart for L00 and L01: enemy introduced, hazard, tower unlocked, power-up, lesson, intensity. Say which of the six towers are open on wave 1 |
| 3 | No scope cut line | **Mostly closed.** Alpha is Tutorial Hill + Meadow, six towers, six enemies, two bosses; hazards 5 and 6 are marked after Alpha; the cut list holds the rest | Freeze dates, owners per system, and a fallback for each risky feature (bird snatch, Queen Mantis husks, the final-march tunnel) |
| 4 | The documents disagree | **New disagreements** (see the next section) | One pass to settle them |
| 5 | Punishments the book calls unfair | **Mostly closed.** The Hornet and the Hand are gone; burrowing answers the bird shadow and the pesticide; the bird shadow has a visible timer | Say how each hazard warns (pesticide and diatomaceous earth have no stated warning); say whether a snatched tower is lost for the level and whether its Honeydew comes back |
| 6 | Almost no audio design | **Open, and now absent.** V2 has no audio section at all | Where sound and music come from, licences, a sound list; a sound for every hazard warning, with a matching picture |
| 7 | No playtest plan or test tools | **Open.** Beta says "balance, polish and playtesting" | Who tests when (team, then outsiders), a bug template, a test scene that can fire any hazard and spawn any enemy |
| 8 | The player is not taught | **Partly closed.** Tutorial Hill has five waves that teach the basics | What each of the five waves teaches (place, standing orders, burrow, squash, collect, upgrade), the prompt for each, and a name card the first time each enemy appears |
| 9 | The Warden is a list of verbs | **No longer applies.** The Warden is cut; the Queen is the visible character and the story has a beginning, middle and end | The Queen's personality and text voice; what the colony's ants look like as towers (silhouette per tower) |
| 10 | Missing paperwork | **Partly closed.** A one-sheet, comparable games, a pitch deck and an achievements list now exist | Version history, glossary, open-items list, a short technical section and a licence log for fonts, sounds and AI-made art |

## Where V2 disagrees with itself or the cut list

1. **Which hazards ship at Alpha.** V2 marks four hazards Alpha (diatomaceous earth, wasp
   stingers, bird shadow, pesticide human); the cut list says all six were cut from Alpha on 2026-10-10.
2. **Towers have no health, but diatomaceous earth "can even kill" them.** Pick one.
3. **"Two bosses" at Alpha**, but Tutorial Hill also ends with the Stag Beetle mini-boss. Count it
   or call it out.
4. **The Queen Mantis** is the Meadow's final boss in V2 and a post-launch extra boss in the cut list.
5. **Camera.** V2 is a 3/4 side elevation with side-drawn sprites; the sprites today are top-down
   placeholders and the current build's levels are top-down. Decide before more art is made.
6. **Grid size is no longer stated.** The level metrics (road width, tower ranges in tiles, walk
   times) all hang off it.
7. **Wasps are both an enemy and a hazard** (the stinger drop). Say whether the hazard is the
   enemy's ability or a separate event.
8. **Rating and audience.** ESRB T, ages 16 and up, against a comic tone and *Plants vs. Zombies*
   as a comparable. The book asks that the rating follow the content; say what earns the T or
   aim for E10+.
9. **The HUD key** numbers eight items but not the tower list, which is HUD mechanic 1.

## Where the book argues with V2

These are decisions, not errors.

| V2 design | The book's advice |
|---|---|
| Losing restarts the whole level, and the Meadow has a variable, possibly long, wave count | A long level with no checkpoint punishes a late mistake with a long replay; consider restarting the wave, or a checkpoint at the boss |
| The Titan's roar stuns towers before the charge | Good: the roar is the warning. Make sure it reads and sounds the same every time |
| A bird snatches a tower | Fine if it is clearly shown and answerable; don't also take the Honeydew (don't punish twice) |
| Queen Mantis raises killed bugs as husks | Show which bodies she can raise, so keeping "the fallen away from her" is a plan the player can see |
| Click squash capped at 5 clicks per enemy | A good anti-autoclicker rule; show when an enemy is "used up" so a sixth click is not a silent fail |
| A worker carries a power-up to the Queen in about 5 s | A delay between earning and using is fine; show the carry on screen so it is anticipation, not lag |

## The order that matters

One chain carries most of the design work; nothing downstream is worth tuning until the step
before it exists:

**Lock grid size and camera → level metrics → tower stat matrix + enemy sheets → counter chart
→ which towers open when → wave spreadsheet (beat chart) → Food ration and Honeydew budget →
difficulty modes.**

## Suggested next tasks, by milestone

| Milestone | Design work |
|---|---|
| **Pitch, Oct 16** | Settle the nine disagreements above; fill the yellow TBDs that the pitch shows (contact email, starting ants); decide the four Alpha hazards |
| **Alpha, Nov 6** | Grid size and level metrics; tower damage, range and rate for all six; wave spreadsheet for L00 and L01; Food ration and Honeydew income per wave; hazard warnings (picture and sound) and timings; what each Tutorial Hill wave teaches; audio source and licence plan; playtest plan |
| **Beta, Nov 20** | Boss timelines for the Titan and the Queen Mantis (seconds per attack, the opening to hit back); economy check against the wave spreadsheet; onboarding prompts and enemy name cards; licence log; an un-fun pass after each level is played |
| **Release, Dec 4** | Boss tuning: each boss beatable by at least three tower mixes |

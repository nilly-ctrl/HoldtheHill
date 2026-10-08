# Hold the Hill — Project Map

Where everything for the project lives, and how the pieces connect. Generated 2026-10-07; the links come from a hand-kept list, so tell nilly-ctrl when something moves.

## Where things live

| System | It is the source of truth for | Links |
|---|---|---|
| **Notion** | Tasks, due dates, who is on what | [Dev Hub](https://app.notion.com/p/3bf1fdfffa614874b9140ad926038a68) · [Tasks board](https://app.notion.com/p/4b57b00acb634df58f3c54dde0165591) · [Dev Activity (AI session summaries)](https://app.notion.com/p/3f2101a0f05381eeb0f3e9ab224f0c13) · [Project Map](https://app.notion.com/p/3f3101a0f05381cab882e5af8416fceb) |
| **GitHub** | Code, scenes, art that ships, the two devlogs | [Team repo](https://github.com/MetalBear4-2243/HoldtheHill) · [Indev branch (integration)](https://github.com/MetalBear4-2243/HoldtheHill/tree/Indev) · [Pull requests](https://github.com/MetalBear4-2243/HoldtheHill/pulls) |
| **Google Drive** | Design documents, course paperwork, meeting notes, playable builds | [Turbulent Towers (team folder)](https://drive.google.com/drive/folders/1VftIzL7c_CV-h18aVd-WEPmxm3Dfqm7o) · [Design Documentation](https://drive.google.com/drive/folders/18Tsq9rP0p2FIICY5yCtE88WhYD31GSem) · [Meeting Notes](https://drive.google.com/drive/folders/18OUMMoxfp0Kdh2xWe_lK8cFY9K2FcaQB) · [Art](https://drive.google.com/drive/folders/1OxtztLOjM5nvgyxIyRVZEmjaNTL5QpSO) · [Builds](https://drive.google.com/drive/folders/1fLXSzyWoWgpzYwYOOEhTfyrItTJBywpl) · [Robot Help (AI research)](https://drive.google.com/drive/folders/1MUv8jjESt6vZvISKQ-Kd-jGoj3d2oizg) |
| **Unity** | The game project itself (opened from the repo's unity/ folder) | [Setup and folder rules (README)](https://github.com/MetalBear4-2243/HoldtheHill/blob/Indev/README.md) · [Team devlog](https://github.com/MetalBear4-2243/HoldtheHill/blob/Indev/docs/DEVLOG.md) · [AI use log](https://github.com/MetalBear4-2243/HoldtheHill/blob/Indev/docs/AI_DEVLOG.md) |

Unity version: **6000.6.0f1** exactly. Open the `unity/` folder, not the repo root.

**One rule keeps these in step:** a task lives in Notion, its code lives in a branch named `name/what-it-does`, and the pull request into `Indev` links back to the Notion task. A task is Done when that pull request is merged.

## Tasks and their code

| Task | Notion status | Code | Where |
|---|---|---|---|
| [Enemy 1: Basic wave spawner to test in Hill01](https://app.notion.com/3db101a0f053815d86aadcc9881a27e7) | Done | merged | [PR 3](https://github.com/MetalBear4-2243/HoldtheHill/pull/3), [PR 7](https://github.com/MetalBear4-2243/HoldtheHill/pull/7) |
| [Enemy 1: Follow path to the hill (waypoints)](https://app.notion.com/3db101a0f0538114b486f09963aeea09) | Done | partly merged | [PR 7](https://github.com/MetalBear4-2243/HoldtheHill/pull/7), `nilly/tower-projectile-system` |
| [Combat: Health and damage components shared by player, enemies, hill](https://app.notion.com/3db101a0f05381aa9757d9abb38e1e32) (unconfirmed link) | Not Started | merged | [PR 11](https://github.com/MetalBear4-2243/HoldtheHill/pull/11) |
| [Combat: Targeting helper (nearest enemy in range), reusable by towers](https://app.notion.com/3db101a0f0538170b705fa83ba2282e3) | Done | branch only | `nilly/tower-projectile-system` |
| [Combat: Hit feedback (flash, knockback, placeholder sound)](https://app.notion.com/3db101a0f053814fbc4bd3cc40fc168e) | In Progress | branch only | `nilly/tower-projectile-system` |
| [HUD: Connect HUD to game events (health, currency, wave changes)](https://app.notion.com/3db101a0f053814482b7dd1f600b56c8) | Done | branch only | `nilly/tower-projectile-system` |
| [HUD: Hill health bar with damage flash](https://app.notion.com/3db101a0f05381659bffd8e41f1ce66c) | In Progress | branch only | `nilly/tower-projectile-system` |
| [HUD: Pick HUD elements and sketch layout (hill HP, player HP, currency, wave)](https://app.notion.com/3db101a0f053816baf7af8cc5f6714e3) | Done | branch only | `nilly/tower-projectile-system` |
| [HUD: Pixel-art HUD frame and icons](https://app.notion.com/3db101a0f053812984e2e123f2ef0375) | Done | branch only | `nilly/tower-projectile-system` |
| [Title: Logo and background art](https://app.notion.com/3db101a0f05381dea210d63a184c3788) | Done | branch only | `nilly/tower-projectile-system` |

## Needs attention

- **Enemy 1: Follow path to the hill (waypoints)**: Marked Done, code is not all on Indev. Branch: `nilly/tower-projectile-system`.
- **Combat: Health and damage components shared by player, enemies, hill**: Code merged, task still open.
- **Combat: Targeting helper (nearest enemy in range), reusable by towers**: Marked Done, code is not all on Indev. Branch: `nilly/tower-projectile-system`.
- **HUD: Connect HUD to game events (health, currency, wave changes)**: Marked Done, code is not all on Indev. Branch: `nilly/tower-projectile-system`.
- **HUD: Pick HUD elements and sketch layout (hill HP, player HP, currency, wave)**: Marked Done, code is not all on Indev. Branch: `nilly/tower-projectile-system`.
- **HUD: Pixel-art HUD frame and icons**: Marked Done, code is not all on Indev. Branch: `nilly/tower-projectile-system`.
- **Title: Logo and background art**: Marked Done, code is not all on Indev. Branch: `nilly/tower-projectile-system`.
- **[PR 12](https://github.com/MetalBear4-2243/HoldtheHill/pull/12) "Built tower card UI."** (merged, by 1103-Montgomery-Adam) has no Notion task.
- Branch `BryceTest` (last commit 2026-09-23, Jason_Kimoto) is not merged into Indev and has a pull request.
- Branch `Tower-Card-UI` (last commit 2026-09-25, 1103-Montgomery-Adam) is not merged into Indev and has a pull request.
- Branch `filechecking` (last commit 2026-09-26, Jason_Kimoto) is not merged into Indev and has no pull request.
- Branch `revert-2-Indev` (last commit 2026-09-23, nilly-ctrl) is not merged into Indev and has no pull request.
- Branch `tilemapstest` (last commit 2026-09-16, Jason_Kimoto) is not merged into Indev and has no pull request.

## AI tools

Every AI tool should follow the same rules. These are the files each one reads:

| File | Read by | On Indev |
|---|---|---|
| `AGENTS.md` | Every AI tool: the shared rules | yes |
| `CLAUDE.md` | Claude Code: imports AGENTS.md, adds Claude-only notes | yes |
| `.gemini/rules.md` | Antigravity: points at AGENTS.md | local only |
| `docs/AI_DEVLOG.md` | Everyone: one short row per AI-assisted session | yes |

Any AI-assisted work gets one row in `docs/AI_DEVLOG.md`, and commits keep their `Co-Authored-By` line.

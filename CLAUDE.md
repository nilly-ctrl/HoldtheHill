# Hold the Hill (team repo)

The rules for every AI tool are in `AGENTS.md`; this file only adds notes for Claude Code.

@AGENTS.md
@AGENTS.local.md

## Claude Code notes
- Open pull requests with `gh pr create --repo MetalBear4-2243/HoldtheHill --base Indev`. From a fork the head is `<github-username>:<branch>`.
- A push rejected with `GH007` means the commit carries a private email. Fix the repo's local `user.email`; don't change the global one.
- Levels are additive scene pairs: `Levels/<Level>/<Level>_Terrain` (tilemaps) + `<Level>_Gameplay`; `SceneLoader` in `Scenes/Bootstrap` loads the rest on Play.

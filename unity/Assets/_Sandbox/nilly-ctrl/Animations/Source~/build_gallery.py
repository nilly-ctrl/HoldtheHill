"""Builds the gallery: every sheet in ../Sheets playing as looping animations.

    python build_gallery.py

Gallery.html is a small index; each group has its own page in Gallery/ (Gallery/Towers.html and
so on), so no single page has to carry every image. The pages are self-contained (images are
embedded) and open straight from disk with no server. Run it after any of the
build_*_sheets.py scripts to pick up their changes.
"""
import base64
import glob
import json
import os

HERE = os.path.dirname(os.path.abspath(__file__))
SHEETS = os.path.normpath(os.path.join(HERE, "..", "Sheets"))
ORDER = ["Towers", "Enemies", "Weapons", "Projectiles", "Fx", "Props", "Ui", "Story"]

PAGE = """<!doctype html>
<html lang="en"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>__TITLE__</title>
<style>
  :root { --bg:#2a1d14; --panel:#3a2a1e; --line:#4a3626; --text:#fff1d6; --dim:#c4bfb2; --gold:#ffd23a; --stage:#4f6f34; }
  * { box-sizing: border-box; }
  body { margin:0; background:var(--bg); color:var(--text); font:14px/1.4 system-ui, sans-serif; }
  header { position:sticky; top:0; z-index:2; background:var(--bg); border-bottom:1px solid var(--line);
           padding:10px 16px; display:flex; flex-wrap:wrap; gap:10px 16px; align-items:center; }
  h1 { font-size:16px; margin:0 8px 0 0; }
  header input[type=search] { background:var(--panel); color:var(--text); border:1px solid var(--line); border-radius:4px; padding:6px 8px; width:200px; }
  header label, header a { color:var(--dim); font-size:13px; }
  header a { text-decoration:none; padding:2px 6px; border:1px solid var(--line); border-radius:4px; }
  header a:hover { color:var(--gold); }
  header select { background:var(--panel); color:var(--text); border:1px solid var(--line); border-radius:4px; padding:4px; }
  #count { color:var(--dim); font-size:13px; margin-left:auto; }
  main { padding:8px 16px 40px; }
  h2 { font-size:15px; color:var(--gold); margin:22px 0 8px; border-bottom:1px solid var(--line); padding-bottom:4px; }
  .grid { display:flex; flex-wrap:wrap; gap:10px; }
  .card { background:var(--panel); border:1px solid var(--line); border-radius:6px; padding:8px; }
  .card h3 { font-size:13px; margin:0; }
  .card p { margin:0 0 6px; font-size:12px; color:var(--dim); }
  .tags { display:flex; flex-wrap:wrap; gap:6px; }
  figure { margin:0; text-align:center; }
  canvas { display:block; background:var(--stage); image-rendering:pixelated; border-radius:3px; }
  body.dark canvas { background:#1b110b; }
  body.light canvas { background:#d9d2c0; }
  figcaption { font-size:11px; color:var(--dim); margin-top:2px; }
  figcaption b { color:var(--text); font-weight:600; }
  .hidden { display:none; }
</style></head><body>
<header>
  <h1>__TITLE__</h1>
  <input type="search" id="q" placeholder="Filter by name or tag" aria-label="Filter by name or tag">
  <label>Size <select id="scale"><option>2</option><option selected>3</option><option>4</option><option>6</option></select>x</label>
  <label>Behind <select id="bg"><option value="">grass</option><option value="dark">dark</option><option value="light">light</option></select></label>
  <label><input type="checkbox" id="pause"> Pause</label>
  <nav id="nav">__NAV__</nav>
  <span id="count"></span>
</header>
<main id="main"></main>
<script>
const DATA = __DATA__;
const main = document.getElementById('main'), nav = document.getElementById('nav');
const players = [];
let scale = 3, paused = false;

function makePlayer(img, sprite, tag) {
  const canvas = document.createElement('canvas');
  const ctx = canvas.getContext('2d');
  const frames = sprite.frames.slice(tag.from, tag.to + 1);
  const p = { canvas, ctx, img, frames, i: 0, t: 0, w: frames[0].w, h: frames[0].h, hold: 0, oneShot: tag.repeat === 1 };
  p.resize = () => { canvas.width = p.w * scale; canvas.height = p.h * scale; ctx.imageSmoothingEnabled = false; p.draw(); };
  p.draw = () => {
    const f = frames[p.i];
    ctx.clearRect(0, 0, canvas.width, canvas.height);
    ctx.drawImage(img, f.x, f.y, f.w, f.h, 0, 0, canvas.width, canvas.height);
  };
  p.step = (dt) => {
    if (p.hold > 0) { p.hold -= dt; if (p.hold <= 0) { p.i = 0; p.t = 0; p.draw(); } return; }
    p.t += dt;
    let moved = false;
    while (p.t >= frames[p.i].d) {
      p.t -= frames[p.i].d;
      if (p.i + 1 < frames.length) { p.i++; moved = true; }
      else if (p.oneShot) { p.hold = 600; p.t = 0; break; }   // one-shots rest on their last frame, then replay
      else { p.i = 0; moved = true; }
    }
    if (moved) p.draw();
  };
  return p;
}

let total = 0;
for (const group of DATA) {
  const h = document.createElement('h2'); h.id = 'g-' + group.name; h.textContent = group.name + ' (' + group.sprites.length + ')';
  main.appendChild(h);
  const grid = document.createElement('div'); grid.className = 'grid'; main.appendChild(grid);
  for (const sprite of group.sprites) {
    total++;
    const card = document.createElement('div'); card.className = 'card';
    card.dataset.search = (sprite.name + ' ' + (sprite.what || '') + ' ' + sprite.tags.map(t => t.name).join(' ')).toLowerCase();
    const f0 = sprite.frames[0];
    card.innerHTML = '<h3></h3><p></p><div class="tags"></div>';
    card.querySelector('h3').textContent = sprite.name;
    card.querySelector('p').textContent = (sprite.what ? sprite.what + '. ' : '') + f0.w + 'x' + f0.h + ', ' + sprite.frames.length + ' frames';
    const img = new Image();
    const tagsEl = card.querySelector('.tags');
    const mine = [];
    for (const tag of sprite.tags) {
      const p = makePlayer(img, sprite, tag); mine.push(p); players.push(p);
      const fig = document.createElement('figure'); fig.appendChild(p.canvas);
      const cap = document.createElement('figcaption');
      cap.innerHTML = '<b></b> ' + (tag.to - tag.from + 1) + (p.oneShot ? ' once' : ' loop');
      cap.querySelector('b').textContent = tag.name;
      fig.appendChild(cap); tagsEl.appendChild(fig);
    }
    img.onload = () => mine.forEach(p => p.resize());
    img.src = 'data:image/png;base64,' + sprite.png;
    grid.appendChild(card);
  }
}

const count = document.getElementById('count');
function filter() {
  const q = document.getElementById('q').value.trim().toLowerCase();
  let shown = 0;
  document.querySelectorAll('.card').forEach(c => { const ok = !q || c.dataset.search.includes(q); c.classList.toggle('hidden', !ok); if (ok) shown++; });
  count.textContent = shown + ' of ' + total + ' sheets';
}
document.getElementById('q').addEventListener('input', filter);
document.getElementById('scale').addEventListener('change', e => { scale = +e.target.value; players.forEach(p => p.resize()); });
document.getElementById('bg').addEventListener('change', e => { document.body.className = e.target.value; });
document.getElementById('pause').addEventListener('change', e => { paused = e.target.checked; });
filter();

let last = performance.now();
function tick(now) {
  const dt = Math.min(100, now - last); last = now;
  if (!paused) for (const p of players) p.step(dt);
  requestAnimationFrame(tick);
}
requestAnimationFrame(tick);
</script></body></html>
"""


INDEX = """<!doctype html>
<html lang="en"><head><meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1">
<title>Hold the Hill animation sheets</title>
<style>
  body { margin:0; background:#2a1d14; color:#fff1d6; font:15px/1.5 system-ui, sans-serif; }
  main { max-width:760px; margin:0 auto; padding:28px 16px 48px; }
  h1 { font-size:20px; margin:0 0 4px; }
  p { color:#c4bfb2; margin:0 0 20px; }
  .grid { display:grid; grid-template-columns:repeat(auto-fill, minmax(170px, 1fr)); gap:10px; }
  a.tile { display:block; background:#3a2a1e; border:1px solid #4a3626; border-radius:6px; padding:14px; color:#fff1d6; text-decoration:none; }
  a.tile:hover { border-color:#ffd23a; }
  a.tile b { display:block; color:#ffd23a; }
  a.tile span { color:#c4bfb2; font-size:13px; }
  code { color:#ecc477; }
</style></head><body><main>
<h1>Hold the Hill animation sheets</h1>
<p>__COUNT__ sheets. Each group opens as its own page, with every animation playing. A full list is in <code>ArtCatalogue.csv</code>.</p>
<div class="grid">
__ROWS__
</div>
</main></body></html>
"""


def main():
    groups = []
    names = sorted(d for d in os.listdir(SHEETS) if os.path.isdir(os.path.join(SHEETS, d)))
    names.sort(key=lambda n: ORDER.index(n) if n in ORDER else len(ORDER))
    count = 0
    for group in names:
        sprites = []
        for path in sorted(glob.glob(os.path.join(SHEETS, group, "*.json"))):
            with open(path, encoding="utf-8") as f:
                data = json.load(f)
            with open(path[:-5] + ".png", "rb") as f:
                png = base64.b64encode(f.read()).decode("ascii")
            sprites.append({
                "name": os.path.basename(path)[:-5],
                "what": data["meta"].get("description", ""),
                "png": png,
                "frames": [{"x": fr["frame"]["x"], "y": fr["frame"]["y"], "w": fr["frame"]["w"], "h": fr["frame"]["h"],
                            "d": fr["duration"]} for fr in data["frames"]],
                "tags": [{"name": t["name"], "from": t["from"], "to": t["to"], "repeat": int(t.get("repeat", 0) or 0)}
                         for t in data["meta"]["frameTags"]],
            })
        count += len(sprites)
        groups.append({"name": group, "sprites": sprites})
    pages = os.path.join(HERE, "Gallery")
    os.makedirs(pages, exist_ok=True)
    for old in glob.glob(os.path.join(pages, "*.html")):
        os.remove(old)
    sizes = []
    for g in groups:
        nav = '<a href="../Gallery.html">Index</a> ' + " ".join(
            f'<a href="{o["name"]}.html">{o["name"]}</a>' for o in groups if o is not g)
        html = (PAGE.replace("__DATA__", json.dumps([g], separators=(",", ":")))
                .replace("__NAV__", nav).replace("__TITLE__", f"Hold the Hill sheets: {g['name']}"))
        path = os.path.join(pages, g["name"] + ".html")
        with open(path, "w", encoding="utf-8") as f:
            f.write(html)
        sizes.append((g["name"], len(g["sprites"]), os.path.getsize(path) // 1024))
    rows = "\n".join(
        f'<a class="tile" href="Gallery/{name}.html"><b>{name}</b><span>{n} sheets</span></a>' for name, n, _ in sizes)
    index = INDEX.replace("__ROWS__", rows).replace("__COUNT__", str(count))
    out = os.path.join(HERE, "Gallery.html")
    with open(out, "w", encoding="utf-8") as f:
        f.write(index)
    print(f"{count} sheets in {len(groups)} groups -> {out} and Gallery/ (largest page {max(s[2] for s in sizes)} KB)")


if __name__ == "__main__":
    main()

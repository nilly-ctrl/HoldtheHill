(function () {
  var data = null, theme = "Meadow", style = "Banner", alphabet = "", paint = "";
  var $ = function (id) { return document.getElementById(id); };
  var cache = {}, shortlist = [];

  function rgba(c, a) {
    c = c.replace("#", "");
    return [parseInt(c.substr(0, 2), 16), parseInt(c.substr(2, 2), 16), parseInt(c.substr(4, 2), 16),
            c.length > 6 ? parseInt(c.substr(6, 2), 16) : (a === undefined ? 255 : a)];
  }

  // A glyph as rows of booleans: weight, full height, scale and slant applied. Mirrors build_pixel_fonts.py.
  function maskOf(face, ch, bold, scale, slant) {
    var rows = (bold && face.bold[ch]) || face.glyphs[ch];
    var m = rows.map(function (r) { return r.split("").map(function (c) { return c === "#"; }); });
    if (bold && !face.bold[ch] && ch !== " ") {
      m = m.map(function (row) {
        var out = [];
        for (var x = 0; x <= row.length; x++) out.push((x < row.length && row[x]) || (x > 0 && row[x - 1]));
        return out;
      });
    }
    var w = m[0].length;
    while (m.length < face.cap + face.desc) m.push(new Array(w).fill(false));
    var h = m.length, x, y;
    if (scale > 1) {
      var big = [];
      for (y = 0; y < h * scale; y++) {
        var row = [];
        for (x = 0; x < w * scale; x++) row.push(m[Math.floor(y / scale)][Math.floor(x / scale)]);
        big.push(row);
      }
      m = big; w *= scale; h *= scale;
    }
    if (slant) {
      var extra = Math.floor((h - 1) / slant);
      m = m.map(function (row, yy) {
        var shift = Math.floor((h - 1 - yy) / slant);
        return new Array(shift).fill(false).concat(row, new Array(extra - shift).fill(false));
      });
      w += extra;
    }
    return { m: m, w: w, h: h };
  }

  // One glyph painted in a style: outline, shadow, glow, fill bands, bevel, stripes, hollow.
  function bake(ch, st, face, faceKey) {
    var scale = st.scale, thick = st.thick === undefined ? 1 : st.thick;
    var g = maskOf(face, ch, st.bold, scale, st.slant), m = g.m, w = g.w, h = g.h;
    var sdx = st.shadow[0], sdy = st.shadow[1], scol = st.shadow[2];
    var pad = thick + (st.glow ? 2 : 0);
    var W = w + pad * 2 + sdx, H = h + pad * 2 + sdy;
    var px = new Uint8ClampedArray(W * H * 4), x, y, dx, dy;
    function put(xx, yy, c) { var i = (yy * W + xx) * 4; px[i] = c[0]; px[i + 1] = c[1]; px[i + 2] = c[2]; px[i + 3] = c[3]; }
    function on(xx, yy) { return yy >= 0 && yy < h && xx >= 0 && xx < w && m[yy][xx]; }
    var ol = new Uint8Array(W * H);
    for (y = 0; y < h; y++) for (x = 0; x < w; x++) if (m[y][x]) {
      for (dy = -thick; dy <= thick; dy++) for (dx = -thick; dx <= thick; dx++) {
        if (thick === 1 && dx && dy) continue;
        ol[(y + pad + dy) * W + x + pad + dx] = 1;
      }
    }
    if (st.glow) {
      var reach = thick + 2, gc = st.glow[0], ga = st.glow[1];
      for (y = 0; y < h; y++) for (x = 0; x < w; x++) if (m[y][x]) {
        for (dy = -reach; dy <= reach; dy++) for (dx = -reach; dx <= reach; dx++) {
          var far = Math.abs(dx) + Math.abs(dy);
          if (far > reach + 1) continue;
          var a = far <= reach - 1 ? ga : Math.floor(ga / 2), i = ((y + pad + dy) * W + x + pad + dx) * 4;
          if (px[i + 3] < a) put(x + pad + dx, y + pad + dy, rgba(gc, a));
        }
      }
    }
    if (sdx || sdy) {
      var sh = rgba(scol);
      for (y = 0; y < H; y++) for (x = 0; x < W; x++) {
        if (ol[y * W + x] && y + sdy < H && x + sdx < W) put(x + sdx, y + sdy, sh);
      }
    }
    var olc = rgba(st.outline);
    for (y = 0; y < H; y++) for (x = 0; x < W; x++) if (ol[y * W + x]) put(x, y, olc);
    var bands = st.fill || ["#000000"], bevel = st.bevel || 1, d, c, edge;
    for (y = 0; y < h; y++) for (x = 0; x < w; x++) {
      if (!m[y][x]) continue;
      if (st.hollow) { put(x + pad, y + pad, [0, 0, 0, 0]); continue; }
      c = st.stripes ? bands[Math.floor((x + y) / st.stripes) % bands.length]
                     : bands[Math.min(bands.length - 1, Math.floor(y * bands.length / h))];
      if (st.hi) { edge = false; for (d = 1; d <= bevel; d++) if (!on(x, y - d)) edge = true; if (edge) c = st.hi; }
      if (c !== st.hi && st.lo) { edge = false; for (d = 1; d <= bevel; d++) if (!on(x, y + d)) edge = true; if (edge) c = st.lo; }
      put(x + pad, y + pad, rgba(c));
    }
    var gap = st.gap === undefined ? (scale === 1 ? 1 : 2) : st.gap;
    var advance = ch !== " " ? w + thick + gap : (faceKey === "body" ? w + 1 : w + thick + gap);
    return { w: W, h: H, px: px, advance: advance };
  }

  // What the tester has picked. A screen element can swap in its own alphabet.
  function pick(face) {
    return { face: face === undefined ? alphabet : face, paint: paint, bold: $("bold").checked, italic: $("italic").checked };
  }

  // The style to draw with: the theme's colours unless a treatment is picked, in the chosen alphabet.
  // build_faces.py does the same in pairing_style(), so a baked pairing matches this preview.
  function resolve(t, s, p) {
    var base = data.themes[t].styles[s], own = base.face || "body";
    var faceKey = p.face || own, st = base;
    if (faceKey !== own || p.paint || p.bold || p.italic) {
      st = Object.assign({}, base);
      if (faceKey !== own) { st.face = faceKey; st.gap = data.faces[faceKey].gap; }
      if (p.paint) {
        ["fill", "hi", "lo", "outline", "glow", "stripes", "hollow"].forEach(function (k) { delete st[k]; });
        var pt = data.paints[p.paint];
        Object.keys(pt).forEach(function (k) { if (k !== "shadow") st[k] = pt[k]; });
        st.shadow = [base.shadow[0], base.shadow[1], pt.shadow[2]];      // the style's offset, the treatment's colour
      }
      if (p.bold) st.bold = true;
      if (p.italic) {
        // lean one pixel every three rows whatever the scale, and take the lean back out of the letter gap
        var f = data.faces[faceKey], tall = (f.cap + f.desc) * st.scale;
        st.slant = 3 * st.scale;
        st.gap = (st.gap === undefined ? (st.scale === 1 ? 1 : 2) : st.gap) - Math.floor((tall - 1) / st.slant);
      }
    }
    return { st: st, faceKey: faceKey, face: data.faces[faceKey], own: own,
             key: [t, s, faceKey, p.paint, +p.bold, +p.italic].join("|"),
             changed: faceKey !== own || !!p.paint || p.bold || p.italic };
  }

  function glyph(r, ch) {
    var key = r.key + "|" + ch;
    if (!cache[key]) cache[key] = bake(ch, r.st, r.face, r.faceKey);
    return cache[key];
  }

  // The text at one screen pixel per font pixel, on its own small canvas.
  function layout(t, s, text, kern, p) {
    var r = resolve(t, s, p || pick()), face = r.face, kerns = r.st.bold ? face.kernBold : face.kern;
    var pen = 0, prev = null, runs = [], extra = 0, h = 1, i;
    for (i = 0; i < text.length; i++) {
      var ch = text[i];
      if (!face.glyphs[ch]) ch = ch.toUpperCase();          // capitals-only alphabets
      if (!face.glyphs[ch]) continue;
      var g = glyph(r, ch);
      if (kern && prev !== null && kerns[prev + ch]) pen += kerns[prev + ch] * r.st.scale;
      runs.push([g, pen]);
      pen += g.advance; extra = g.w - g.advance; h = g.h; prev = ch;
    }
    var w = Math.max(1, pen + Math.max(0, extra));
    var buf = new Uint8ClampedArray(w * h * 4);
    runs.forEach(function (run) {            // later glyphs draw over earlier ones, as in the game
      var g = run[0], ox = run[1];
      for (var y = 0; y < g.h; y++) for (var x = 0; x < g.w; x++) {
        var si = (y * g.w + x) * 4, a = g.px[si + 3];
        if (!a || ox + x >= w || ox + x < 0) continue;
        var di = (y * w + ox + x) * 4, da = buf[di + 3], sa = a / 255, oa = sa + da / 255 * (1 - sa);
        for (var k = 0; k < 3; k++) buf[di + k] = (g.px[si + k] * sa + buf[di + k] * da / 255 * (1 - sa)) / oa;
        buf[di + 3] = oa * 255;
      }
    });
    var small = document.createElement("canvas");
    small.width = w; small.height = h;
    small.getContext("2d").putImageData(new ImageData(buf, w, h), 0, 0);
    return small;
  }

  function draw(canvas, t, s, text, zoom, kern, fit) {
    var small = layout(t, s, text, kern), w = small.width, h = small.height;
    if (fit && w * zoom > fit) zoom = Math.max(1, Math.floor(fit / w));
    canvas.width = w * zoom; canvas.height = h * zoom;
    canvas.style.width = w * zoom + "px"; canvas.style.height = h * zoom + "px";
    var c = canvas.getContext("2d");
    c.imageSmoothingEnabled = false;
    c.drawImage(small, 0, 0, w * zoom, h * zoom);
  }

  // The row that bakes a pairing, or null when it is already a game file.
  function bakeRow(t, s, p) {
    var r = resolve(t, s, p);
    if (!r.changed) return null;
    var extras = [paintName(p.paint), p.bold ? "bold" : "", p.italic ? "italic" : ""].filter(Boolean).join(" ");
    return '("' + t + '", "' + s + '", "' + r.faceKey + '"' + (extras ? ', "' + extras + '"' : "") + "),";
  }

  // ---- a colour treatment designed on the page
  function hex2(n) { return ("0" + Math.round(n).toString(16)).slice(-2); }
  function blend(a, b, t) {
    var x = rgba(a), y = rgba(b);
    return "#" + hex2(x[0] + (y[0] - x[0]) * t) + hex2(x[1] + (y[1] - x[1]) * t) + hex2(x[2] + (y[2] - x[2]) * t);
  }
  function customName() { return ($("c-name").value || "Custom").replace(/[^A-Za-z0-9]/g, "") || "Custom"; }
  function paintName(key) { return key === "Custom" ? customName() : key; }
  function customPaint() {
    var top = $("c-top").value, bottom = $("c-bottom").value, p = {
      fill: [top, blend(top, bottom, 0.5), bottom], outline: $("c-outline").value, shadow: [0, 1, $("c-shadow").value]
    };
    if ($("c-hi-on").checked) p.hi = $("c-hi").value;
    return p;
  }
  function customChanged() {
    data.paints.Custom = customPaint();
    var p = data.paints.Custom;
    $("c-line").textContent = '"' + customName() + '": dict(fill=["' + p.fill.join('", "') + '"], ' + (p.hi ? 'hi="' + p.hi + '", ' : "") +
      'outline="' + p.outline + '", shadow=(0, 1, "' + p.shadow[2] + '")),';
    cache = {};
    if (paint === "Custom") { buildStyles(); refresh(); }
  }

  // ---- pairings kept to compare
  function saveShortlist() { try { localStorage.setItem("hth-font-shortlist", JSON.stringify(shortlist)); } catch (e) { /* not kept between visits */ } }
  function drawShortlist() {
    var box = $("shortlist"), rows = [], seen = {};
    box.textContent = "";
    shortlist.forEach(function (item, index) {
      if (!data.themes[item.theme] || !data.themes[item.theme].styles[item.style]) return;
      if (item.def) data.paints[item.p.paint] = item.def;       // a custom treatment travels with its entry
      if (item.p.paint && !data.paints[item.p.paint]) return;
      var card = document.createElement("div"); card.className = "short";
      card.style.background = data.themes[item.theme].backdrop;
      var c = document.createElement("canvas");
      var small = layout(item.theme, item.style, $("text").value, $("kern").checked, item.p);
      var z = small.height > 26 ? 1 : 2;
      c.width = small.width * z; c.height = small.height * z; c.style.width = c.width + "px"; c.style.height = c.height + "px";
      var g = c.getContext("2d"); g.imageSmoothingEnabled = false; g.drawImage(small, 0, 0, c.width, c.height);
      var cap = document.createElement("span"); cap.className = "use";
      var own = data.themes[item.theme].styles[item.style].face || "body";
      cap.textContent = item.theme + " " + item.style + ", " + describe(item.p, own);
      var rm = document.createElement("button"); rm.type = "button"; rm.textContent = "Remove";
      rm.addEventListener("click", function () { shortlist.splice(index, 1); saveShortlist(); drawShortlist(); });
      var wrap = document.createElement("div"); wrap.className = "short-scroll"; wrap.appendChild(c);
      card.appendChild(wrap); card.appendChild(cap); card.appendChild(rm); box.appendChild(card);
      var row = bakeRow(item.theme, item.style, item.p);
      if (row && !seen[row]) { seen[row] = 1; rows.push(row); }
    });
    $("short-empty").hidden = shortlist.length > 0;
    $("short-bake").hidden = !shortlist.length;
    $("short-rows").textContent = rows.length ? rows.join("\n") : "Nothing to bake: every entry is already a game file.";
    $("short-copy").hidden = !rows.length;
  }
  function addToShortlist() {
    var p = pick(), item = { theme: theme, style: style, p: { face: p.face, paint: paintName(p.paint), bold: p.bold, italic: p.italic } };
    if (p.paint === "Custom") { item.def = customPaint(); data.paints[item.p.paint] = item.def; }
    var key = JSON.stringify(item);
    if (shortlist.some(function (o) { return JSON.stringify(o) === key; })) return;
    shortlist.push(item); saveShortlist(); drawShortlist();
  }

  function describe(p, own) {
    var face = p.face || own, bits = [data.faces[face].name];
    if (p.bold) bits.push("bold");
    if (p.italic) bits.push("italic");
    return bits.join(" ") + (p.paint ? ", " + p.paint + " colours" : "");
  }

  function refresh() {
    if (!data) return;
    if (data.themes[theme].order.indexOf(style) < 0) style = "Title";
    $("stage").style.background = data.themes[theme].backdrop;
    draw($("out"), theme, style, $("text").value, +$("zoom").value, $("kern").checked);
    var p = pick(), own = data.themes[theme].styles[style].face || "body";
    $("status").textContent = style + " from " + theme + ", set in " + describe(p, own) +
      ((p.face || own) === own ? "" : " (its own alphabet is " + data.faces[own].name + ")") + ". " + data.uses[style] + ".";
    var row = bakeRow(theme, style, p), folder = "Fonts/Atlases/" + (theme === "Meadow" ? "" : "Themes/" + theme + "/");
    if (row) {
      $("bake-line").textContent = row;
      $("bake-note").textContent = "Not baked yet. Add this row to PAIRINGS in Fonts/Source~/build_faces.py and run build_pixel_fonts.py.";
    } else {
      $("bake-line").textContent = folder + style + ".png";
      $("bake-note").textContent = "Already baked. This is the file the game uses for this style.";
    }
    $("bake-copy").hidden = !row;
    $("custom").hidden = paint !== "Custom";
    $("bake-custom").hidden = !(row && paint === "Custom");
    mockup();
    drawShortlist();
  }

  // The parts of the sample screen, the styles each one uses, and its own alphabet if it has been given one.
  var PARTS = [
    { id: "banner", name: "Banner and title", styles: ["Title", "Banner", "Heading"] },
    { id: "numbers", name: "Damage numbers", styles: ["DamageNormal", "DamageCrit", "DamageFire", "DamagePoison", "DamageLightning", "DamageMagic", "DamageTrue", "Heal", "Resource", "Combo"] },
    { id: "hud", name: "Top bar", styles: ["Hud", "Resource", "Heal"] },
    { id: "bar", name: "Build bar", styles: ["TinyLabel", "TinyCost"] },
    { id: "buttons", name: "Buttons", styles: ["Label"] }
  ];
  var partFace = {};      // part id -> alphabet key; missing means "same as the tester"
  var hot = [];           // clickable regions of the screen, as [x, y, w, h, part id]

  function partPick(id) { return pick(partFace[id] === undefined ? undefined : partFace[id]); }

  // A sample game screen in the chosen theme, with each part in its own alphabet.
  function mockup() {
    var W = 960, H = 540, c = $("mock"), g = c.getContext("2d");
    c.width = W; c.height = H;
    g.imageSmoothingEnabled = false;
    g.fillStyle = data.themes[theme].backdrop; g.fillRect(0, 0, W, H);
    function shade(x, y, w, h, colour, a) { g.globalAlpha = a; g.fillStyle = colour; g.fillRect(x, y, w, h); g.globalAlpha = 1; }
    // place text by its centre (ax = 0.5), left (0) or right (1) edge
    function put(part, s, text, x, y, z, ax) {
      if (!data.themes[theme].styles[s]) return;
      var t = layout(theme, s, text, true, partPick(part)), w = t.width * z, h = t.height * z;
      g.drawImage(t, Math.round(x - w * (ax === undefined ? 0.5 : ax)), Math.round(y - h / 2), w, h);
    }
    shade(0, 0, W, 56, "#000000", 0.35);                      // top bar
    shade(0, 300, W, 60, "#ffffff", 0.06);                    // the path
    shade(0, H - 96, W, 96, "#000000", 0.4);                  // build bar
    put("hud", "Hud", "WAVE 3/10", 20, 28, 2, 0);
    put("hud", "Hud", "02:45", W / 2, 28, 2);
    put("hud", "Resource", "1,250", W - 150, 28, 2, 1);
    put("hud", "Heal", "HP 85%", W - 20, 28, 2, 1);
    put("banner", "Title", "Hold the Hill", W / 2, 96, 1);
    put("banner", "Banner", "WAVE CLEAR", W / 2, 170, 2);
    put("banner", "Heading", "NEXT WAVE IN 5", W / 2, 236, 2);
    [["DamageNormal", "128", 110, 330], ["DamageCrit", "CRIT!", 250, 300], ["DamageFire", "37", 390, 340],
     ["DamagePoison", "18", 480, 318], ["DamageLightning", "64", 570, 342], ["DamageMagic", "33", 660, 316],
     ["DamageTrue", "75", 750, 340], ["Heal", "+25", 850, 312]].forEach(function (h) {
      shade(h[2] - 14, h[3] + 26, 28, 18, "#000000", 0.45);   // a stand-in enemy under each number
      put("numbers", h[0], h[1], h[2], h[3], h[0] === "DamageCrit" ? 1 : 2);
    });
    put("numbers", "Combo", "x12", W - 110, 400, 2);
    put("numbers", "Resource", "+40", 110, 400, 2);
    for (var i = 0; i < 6; i++) {
      var x = 24 + i * 92;
      shade(x, H - 84, 76, 72, "#ffffff", 0.1);
      put("bar", "TinyLabel", String(i + 1), x + 6, H - 72, 2, 0);
      put("bar", "TinyCost", String([50, 75, 125, 200, 350, 500][i]), x + 38, H - 26, 2);
    }
    ["Resume", "Options", "Quit"].forEach(function (label, n) {
      shade(W - 300 + n * 96, H - 66, 88, 36, "#ffffff", 0.1);
      put("buttons", "Label", label, W - 256 + n * 96, H - 48, 1);
    });
    hot = [[0, 0, W, 56, "hud"], [0, 60, W, 210, "banner"], [0, 276, W, 150, "numbers"],
           [0, H - 96, 580, 96, "bar"], [580, H - 96, 380, 96, "buttons"]];

    // every row needed to bake the screen as it stands
    var rows = [], seen = {};
    PARTS.forEach(function (part) {
      part.styles.forEach(function (s) {
        if (!data.themes[theme].styles[s]) return;
        var row = bakeRow(theme, s, partPick(part.id));
        if (row && !seen[row]) { seen[row] = 1; rows.push(row); }
      });
    });
    $("screen-rows").textContent = rows.length ? rows.join("\n") : "Nothing to bake: every part is in its own baked style.";
    $("screen-copy").hidden = !rows.length;
  }

  function buildParts() {
    var box = $("parts");
    PARTS.forEach(function (part) {
      var wrap = document.createElement("div"); wrap.className = "field";
      var label = document.createElement("label"); label.className = "label"; label.textContent = part.name;
      label.htmlFor = "part-" + part.id;
      var sel = document.createElement("select"); sel.id = "part-" + part.id;
      [["", "Same as tester"], ["own", "Style's own"]].concat(Object.keys(data.faces).map(function (k) { return [k, data.faces[k].name]; }))
        .forEach(function (o) { var op = document.createElement("option"); op.value = o[0]; op.textContent = o[1]; sel.appendChild(op); });
      sel.addEventListener("change", function () {
        if (sel.value === "") delete partFace[part.id]; else partFace[part.id] = sel.value === "own" ? "" : sel.value;
        mockup();
      });
      wrap.appendChild(label); wrap.appendChild(sel); box.appendChild(wrap);
    });
    // clicking a part of the screen jumps to its chooser
    $("mock").addEventListener("click", function (e) {
      var r = $("mock").getBoundingClientRect(), x = (e.clientX - r.left) * 960 / r.width, y = (e.clientY - r.top) * 540 / r.height;
      for (var i = 0; i < hot.length; i++) {
        if (x >= hot[i][0] && x < hot[i][0] + hot[i][2] && y >= hot[i][1] && y < hot[i][1] + hot[i][3]) {
          $("part-" + hot[i][4]).focus();
          return;
        }
      }
    });
  }

  function chips(box, items, current, choose) {
    items.forEach(function (it) {
      var b = document.createElement("button");
      b.type = "button"; b.textContent = it[1]; b.setAttribute("aria-pressed", it[0] === current);
      b.addEventListener("click", function () {
        box.querySelectorAll("button").forEach(function (o) { o.setAttribute("aria-pressed", o === b); });
        choose(it[0]);
      });
      box.appendChild(b);
    });
  }

  function buildStyles() {
    var box = $("styles");
    box.textContent = "";
    data.themes[theme].order.forEach(function (s) {
      var b = document.createElement("button");
      b.type = "button"; b.setAttribute("aria-pressed", s === style);
      b.style.background = data.themes[theme].backdrop;
      b.setAttribute("aria-label", s + ": " + data.uses[s]);
      var c = document.createElement("canvas");
      var tall = resolve(theme, s, pick());
      draw(c, theme, s, data.samples[s] || "Hold the Hill", (tall.face.cap + tall.face.desc) * tall.st.scale > 20 ? 1 : 2, true, 200);
      var n = document.createElement("span");
      n.className = "use"; n.textContent = s;
      b.appendChild(c); b.appendChild(n);
      b.addEventListener("click", function () {
        style = s;
        box.querySelectorAll("button").forEach(function (o) { o.setAttribute("aria-pressed", o === b); });
        refresh();
      });
      box.appendChild(b);
    });
  }

  function buildPrompts() {
    var box = $("prompts");
    data.buttons.forEach(function (p) {
      var d = document.createElement("div"); d.className = "prompt";
      var g = document.createElement("span"); g.className = "g"; g.textContent = String.fromCharCode(p[0]);
      var n = document.createElement("span"); n.className = "n";
      var code = document.createElement("code"); code.textContent = p[0].toString(16).toUpperCase();
      n.appendChild(document.createTextNode(p[1] + " ")); n.appendChild(code);
      d.appendChild(g); d.appendChild(n); box.appendChild(d);
    });
  }

  function copier(buttonId, sourceId) {
    $(buttonId).addEventListener("click", function () {
      var button = $(buttonId), source = $(sourceId);
      function done(ok) { button.textContent = ok ? "Copied" : "Select and copy"; setTimeout(function () { button.textContent = "Copy"; }, 1600); }
      function select() {
        var range = document.createRange(); range.selectNodeContents(source);
        var sel = window.getSelection(); sel.removeAllRanges(); sel.addRange(range); done(false);
      }
      try { navigator.clipboard.writeText(source.textContent).then(function () { done(true); }, select); } catch (e) { select(); }
    });
  }
  copier("bake-copy", "bake-line");
  copier("screen-copy", "screen-rows");
  copier("short-copy", "short-rows");
  copier("c-copy", "c-line");
  $("short-add").addEventListener("click", addToShortlist);
  ["c-top", "c-bottom", "c-outline", "c-shadow", "c-hi", "c-hi-on", "c-name"].forEach(function (id) {
    $(id).addEventListener("input", customChanged);
  });

  ["text", "zoom", "kern"].forEach(function (id) { $(id).addEventListener("input", refresh); });
  ["bold", "italic"].forEach(function (id) { $(id).addEventListener("input", function () { buildStyles(); refresh(); }); });

  fetch("fonts.json").then(function (r) { if (!r.ok) throw new Error(r.status); return r.json(); }).then(function (d) {
    data = d;
    data.paints.Custom = customPaint();
    try { shortlist = JSON.parse(localStorage.getItem("hth-font-shortlist") || "[]") || []; } catch (e) { shortlist = []; }
    if (!Array.isArray(shortlist)) shortlist = [];
    var faces = Object.keys(data.faces).map(function (k) { return [k, data.faces[k].name]; });
    chips($("themes"), Object.keys(data.themes).map(function (t) { return [t, t]; }), theme,
      function (t) { theme = t; buildStyles(); refresh(); });
    chips($("alphabets"), [["", "Style's own"]].concat(faces), alphabet, function (k) { alphabet = k; buildStyles(); refresh(); });
    chips($("paints"), [["", "Theme's own"]].concat(Object.keys(data.paints).map(function (k) { return [k, k]; })), paint,
      function (k) { paint = k; buildStyles(); refresh(); });
    buildParts(); buildStyles(); buildPrompts(); customChanged(); refresh();
  }).catch(function () {
    $("status").textContent = "The font data did not load. Reload the page to try again.";
  });
})();

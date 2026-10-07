"""Minimal .aseprite writer and reader (RGBA only), following Aseprite's file spec:
https://github.com/aseprite/aseprite/blob/main/docs/ase-file-specs.md

Written so the sheets can be built without Aseprite installed. The output imports through
Unity's Aseprite Importer (com.unity.2d.aseprite) and passes validate_aseprite.py, an
independent check against the spec. It has not been opened in Aseprite itself.

    write_aseprite(path, w, h, layers, frames, tags, palette)
    read_aseprite(path) -> dict   (used by the build script to verify every file)
"""
import struct
import zlib

from PIL import Image

T_OLD_PALETTE, T_LAYER, T_CEL, T_PROFILE, T_TAGS, T_PALETTE = 0x0004, 0x2004, 0x2005, 0x2007, 0x2018, 0x2019
LOOP = {"forward": 0, "reverse": 1, "pingpong": 2}


def _string(s):
    b = s.encode("utf-8")
    return struct.pack("<H", len(b)) + b


def _chunk(ctype, data):
    return struct.pack("<IH", len(data) + 6, ctype) + data


def write_aseprite(path, width, height, layers, frames, tags=(), palette=()):
    """layers: layer names, bottom first.
    frames: [(duration_ms, {layer_name: RGBA Image of width x height})].
    tags:   [{"name", "from", "to", "repeat" (0 = loop forever), "direction", "color"}].
    palette: [(r, g, b, a)] stored for convenience; pixels are RGBA either way."""
    blobs = []
    for fi, (duration, images) in enumerate(frames):
        chunks = []
        if fi == 0:
            chunks.append(_chunk(T_PROFILE, struct.pack("<HHI", 1, 0, 0) + bytes(8)))  # sRGB
            if palette:
                data = struct.pack("<III", len(palette), 0, len(palette) - 1) + bytes(8)
                for r, g, b, a in palette:
                    data += struct.pack("<HBBBB", 0, r, g, b, a)
                chunks.append(_chunk(T_PALETTE, data))
            for name in layers:
                # flags 3 = visible + editable; normal layer; child level 0; blend normal; opacity 255
                data = struct.pack("<HHHHHHB", 3, 0, 0, 0, 0, 0, 255) + bytes(3) + _string(name)
                chunks.append(_chunk(T_LAYER, data))
            if tags:
                data = struct.pack("<H", len(tags)) + bytes(8)
                for t in tags:
                    data += struct.pack("<HHBH", t["from"], t["to"], LOOP[t.get("direction", "forward")],
                                        t.get("repeat", 0))
                    data += bytes(6) + bytes(t.get("color", (0, 0, 0))) + bytes(1) + _string(t["name"])
                chunks.append(_chunk(T_TAGS, data))
        for li, name in enumerate(layers):
            im = images.get(name)
            if im is None:
                continue
            if im.size != (width, height) or im.mode != "RGBA":
                raise ValueError(f"layer {name} frame {fi}: expected RGBA {width}x{height}")
            box = im.getchannel("A").getbbox()
            if not box:
                continue  # empty cel: Aseprite simply has no cel there
            crop = im.crop(box)
            data = struct.pack("<HhhBHh", li, box[0], box[1], 255, 2, 0) + bytes(5)
            data += struct.pack("<HH", crop.width, crop.height) + zlib.compress(crop.tobytes())
            chunks.append(_chunk(T_CEL, data))
        body = b"".join(chunks)
        n = len(chunks)
        header = struct.pack("<IHHHBBI", 16 + len(body), 0xF1FA, min(n, 0xFFFF), duration, 0, 0, n)
        blobs.append(header + body)

    payload = b"".join(blobs)
    header = struct.pack(
        "<IHHHHHIHIIB3xHBBhhHH84x",
        128 + len(payload), 0xA5E0, len(frames), width, height, 32,
        1,      # flags: layer opacity is valid
        100,    # deprecated speed
        0, 0,
        0,      # transparent palette index (unused in RGBA)
        len(palette) if 0 < len(palette) < 256 else 0,
        1, 1,   # pixel ratio
        0, 0, 16, 16,
    )
    assert len(header) == 128
    with open(path, "wb") as f:
        f.write(header + payload)


def read_aseprite(path):
    """Parse an RGBA .aseprite file back into layers, tags and composited frames."""
    with open(path, "rb") as f:
        buf = f.read()
    size, magic, nframes, w, h, depth = struct.unpack_from("<IHHHHH", buf, 0)
    assert magic == 0xA5E0, "bad header magic"
    assert size == len(buf), f"header says {size} bytes, file is {len(buf)}"
    assert depth == 32, "only RGBA supported"
    pos = 128
    layers, tags, frames = [], [], []
    for _ in range(nframes):
        fsize, fmagic, old_n, duration, _a, _b, new_n = struct.unpack_from("<IHHHBBI", buf, pos)
        assert fmagic == 0xF1FA, "bad frame magic"
        end = pos + fsize
        cpos = pos + 16
        cels = {}
        for _ in range(new_n or old_n):
            csize, ctype = struct.unpack_from("<IH", buf, cpos)
            d = cpos + 6
            if ctype == T_LAYER:
                flags, ltype, child, _w, _h, blend, opacity = struct.unpack_from("<HHHHHHB", buf, d)
                n, = struct.unpack_from("<H", buf, d + 16)
                layers.append(buf[d + 18:d + 18 + n].decode())
            elif ctype == T_TAGS:
                count, = struct.unpack_from("<H", buf, d)
                t = d + 10
                for _ in range(count):
                    fr, to, direction, repeat = struct.unpack_from("<HHBH", buf, t)
                    n, = struct.unpack_from("<H", buf, t + 17)
                    tags.append({"name": buf[t + 19:t + 19 + n].decode(), "from": fr, "to": to,
                                 "direction": direction, "repeat": repeat})
                    t += 19 + n
            elif ctype == T_CEL:
                li, x, y, opacity, ctype2, z = struct.unpack_from("<HhhBHh", buf, d)
                assert buf[d + 11:d + 16] == bytes(5), "cel reserved bytes must be zero"
                assert ctype2 == 2, "expected compressed image cel"
                cw, ch = struct.unpack_from("<HH", buf, d + 16)
                raw = zlib.decompress(buf[d + 20:cpos + csize])
                assert len(raw) == cw * ch * 4
                cels[li] = (x, y, Image.frombytes("RGBA", (cw, ch), raw))
            cpos += csize
        assert cpos == end, "frame size mismatch"
        comp = Image.new("RGBA", (w, h), (0, 0, 0, 0))
        for li in sorted(cels):
            x, y, im = cels[li]
            layer = Image.new("RGBA", (w, h), (0, 0, 0, 0))
            layer.paste(im, (x, y))
            comp.alpha_composite(layer)
        frames.append({"duration": duration, "image": comp, "cels": cels})
        pos = end
    return {"width": w, "height": h, "layers": layers, "tags": tags, "frames": frames}

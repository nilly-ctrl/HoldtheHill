"""Checks every .aseprite under ../Aseprite against the published file format, byte by byte.

    python validate_aseprite.py            (exit code 1 if anything is wrong)

Written from https://github.com/aseprite/aseprite/blob/main/docs/ase-file-specs.md and on purpose
shares no code with aseprite_file.py, which wrote the files: a writer and a reader that agree with
each other can still both be wrong. This is the next best thing to opening the files in Aseprite,
which is not installed on the machine that made them.

What is checked: the header (size, magic number, colour depth, pixel ratio), every frame header
and its byte count, every chunk's size and type, layer names, that each cel names a real layer and
its compressed pixels inflate to exactly width x height x 4 bytes and lie inside the canvas, that
tags are in range, named and unique, and that the palette's entry count matches its range.
"""
import glob
import os
import struct
import sys
import zlib

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.normpath(os.path.join(HERE, "..", "Aseprite"))
KNOWN = {0x0004: "old palette", 0x0011: "old palette", 0x2004: "layer", 0x2005: "cel", 0x2006: "cel extra",
         0x2007: "colour profile", 0x2008: "external files", 0x2016: "mask", 0x2017: "path", 0x2018: "tags",
         0x2019: "palette", 0x2020: "user data", 0x2022: "slice", 0x2023: "tileset"}


class Bad(Exception):
    pass


def need(cond, message):
    if not cond:
        raise Bad(message)


def string(data, at, end):
    need(at + 2 <= end, "string length runs past its chunk")
    (n,) = struct.unpack_from("<H", data, at)
    need(at + 2 + n <= end, "string runs past its chunk")
    return data[at + 2:at + 2 + n].decode("utf-8"), at + 2 + n


def check(path):
    with open(path, "rb") as f:
        data = f.read()
    need(len(data) >= 128, "shorter than a header")
    size, magic, frames, width, height, depth, flags, speed = struct.unpack_from("<IHHHHHIH", data, 0)
    need(magic == 0xA5E0, f"header magic is {magic:#06x}, not 0xA5E0")
    need(size == len(data), f"header says {size} bytes, file has {len(data)}")
    need(frames >= 1, "no frames")
    need(1 <= width <= 65535 and 1 <= height <= 65535, "bad canvas size")
    need(depth == 32, f"colour depth {depth}; these files are meant to be RGBA (32)")
    pixel_w, pixel_h = data[34], data[35]
    need((pixel_w, pixel_h) in ((0, 0), (1, 1)), f"pixel ratio {pixel_w}:{pixel_h} is not square")

    at = 128
    layers, tags, cels, palette_seen, profile_seen = [], [], 0, False, False
    for index in range(frames):
        need(at + 16 <= len(data), f"frame {index}: header runs past the end of the file")
        frame_bytes, frame_magic, old_chunks, duration = struct.unpack_from("<IHHH", data, at)
        (new_chunks,) = struct.unpack_from("<I", data, at + 12)
        need(frame_magic == 0xF1FA, f"frame {index}: magic is {frame_magic:#06x}, not 0xF1FA")
        need(duration > 0, f"frame {index}: duration is 0 ms")
        need(at + frame_bytes <= len(data), f"frame {index}: runs past the end of the file")
        chunks = new_chunks if new_chunks else old_chunks
        need(old_chunks == min(chunks, 0xFFFF), f"frame {index}: old and new chunk counts disagree")
        end_of_frame = at + frame_bytes
        c = at + 16
        for _ in range(chunks):
            need(c + 6 <= end_of_frame, f"frame {index}: chunk header runs past the frame")
            chunk_size, kind = struct.unpack_from("<IH", data, c)
            need(chunk_size >= 6, f"frame {index}: chunk smaller than its own header")
            end = c + chunk_size
            need(end <= end_of_frame, f"frame {index}: {KNOWN.get(kind, hex(kind))} chunk runs past the frame")
            need(kind in KNOWN, f"frame {index}: unknown chunk type {kind:#06x}")
            body = c + 6
            if kind == 0x2004:
                need(index == 0, "layer defined after the first frame")
                _, layer_type, child, _, _, blend, opacity = struct.unpack_from("<HHHHHHB", data, body)
                need(layer_type == 0, f"layer type {layer_type}; only normal image layers are expected")
                name, used = string(data, body + 16, end)
                need(name, "layer with no name")
                need(used == end, f"layer '{name}': {end - used} stray bytes after its name")
                layers.append(name)
            elif kind == 0x2005:
                layer_index, x, y, opacity, cel_type = struct.unpack_from("<HhhBH", data, body)
                need(layer_index < len(layers), f"frame {index}: cel names layer {layer_index}, but there are {len(layers)}")
                need(cel_type == 2, f"frame {index}: cel type {cel_type}; compressed image (2) expected")
                w, h = struct.unpack_from("<HH", data, body + 16)
                need(w >= 1 and h >= 1, f"frame {index}: empty cel")
                need(x >= 0 and y >= 0 and x + w <= width and y + h <= height,
                     f"frame {index}: cel on '{layers[layer_index]}' ({x},{y} {w}x{h}) leaves the {width}x{height} canvas")
                try:
                    raw = zlib.decompress(data[body + 20:end])
                except zlib.error as e:
                    raise Bad(f"frame {index}: cel pixels do not inflate ({e})")
                need(len(raw) == w * h * 4, f"frame {index}: cel inflates to {len(raw)} bytes, expected {w * h * 4}")
                cels += 1
            elif kind == 0x2018:
                need(index == 0, "tags chunk is not in the first frame")
                (count,) = struct.unpack_from("<H", data, body)
                t = body + 10
                for _ in range(count):
                    start, stop, direction, repeat = struct.unpack_from("<HHBH", data, t)
                    name, t = string(data, t + 17, end)
                    need(name, "tag with no name")
                    need(start <= stop < frames, f"tag '{name}' covers frames {start}-{stop} of {frames}")
                    need(direction in (0, 1, 2, 3), f"tag '{name}': unknown direction {direction}")
                    tags.append((name, start, stop, repeat))
                need(t == end, f"tags chunk: {end - t} stray bytes")
            elif kind == 0x2019:
                count, first, last = struct.unpack_from("<III", data, body)
                need(last - first + 1 == count or count >= last - first + 1, f"palette: {count} entries for range {first}-{last}")
                p = body + 20
                for _ in range(first, last + 1):
                    (entry_flags,) = struct.unpack_from("<H", data, p)
                    p += 6
                    if entry_flags & 1:
                        _, p = string(data, p, end)
                need(p == end, f"palette chunk: {end - p} stray bytes")
                palette_seen = True
            elif kind == 0x2007:
                (profile,) = struct.unpack_from("<H", data, body)
                need(profile in (0, 1, 2), f"colour profile type {profile}")
                profile_seen = True
            c = end
        need(c == end_of_frame, f"frame {index}: chunks use {c - at} bytes, frame header says {frame_bytes}")
        at = end_of_frame
    need(at == len(data), f"{len(data) - at} bytes after the last frame")
    need(layers, "no layers")
    need(len(set(layers)) == len(layers), "two layers share a name")
    need(tags, "no tags (Unity would make no clips)")
    names = [t[0] for t in tags]
    need(len(set(names)) == len(names), "two tags share a name: " + ", ".join(sorted(set(n for n in names if names.count(n) > 1))))
    covered = sorted((a, b) for _, a, b, _ in tags)
    for (a0, b0), (a1, b1) in zip(covered, covered[1:]):
        need(a1 > b0, f"tags overlap at frame {a1}")
    need(palette_seen, "no palette chunk")
    need(profile_seen, "no colour profile chunk")
    return frames, len(layers), len(tags), cels


def main():
    paths = sorted(glob.glob(os.path.join(ROOT, "*", "*.aseprite")))
    bad, frames, tags = [], 0, 0
    for path in paths:
        try:
            f, _, t, _ = check(path)
            frames, tags = frames + f, tags + t
        except (Bad, struct.error) as e:
            bad.append((os.path.relpath(path, ROOT), str(e)))
    for name, why in bad:
        print(f"FAIL {name}: {why}")
    print(f"{len(paths) - len(bad)} of {len(paths)} files pass ({frames} frames, {tags} tags)")
    sys.exit(1 if bad else 0)


if __name__ == "__main__":
    main()

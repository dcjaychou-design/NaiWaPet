#!/usr/bin/env python3
"""Verify committed runtime assets without third-party Python packages."""

from __future__ import annotations

import hashlib
import json
from pathlib import Path
import struct
import sys
import wave


PNG_SIGNATURE = b"\x89PNG\r\n\x1a\n"


def png_dimensions(path: Path) -> tuple[int, int]:
    with path.open("rb") as stream:
        header = stream.read(24)
    if len(header) != 24 or header[:8] != PNG_SIGNATURE or header[12:16] != b"IHDR":
        raise ValueError(f"invalid PNG: {path}")
    return struct.unpack(">II", header[16:24])


def png_animation_frames(path: Path) -> int:
    with path.open("rb") as stream:
        if stream.read(8) != PNG_SIGNATURE:
            raise ValueError(f"invalid PNG: {path}")
        while True:
            length_bytes = stream.read(4)
            if len(length_bytes) != 4:
                return 1
            length = struct.unpack(">I", length_bytes)[0]
            chunk_type = stream.read(4)
            payload = stream.read(length)
            stream.read(4)
            if chunk_type == b"acTL":
                return struct.unpack(">II", payload)[0]
            if chunk_type == b"IEND":
                return 1


def sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def verify(root: Path) -> None:
    animation = root / "src/NaiWaPet/Assets/Animation"
    manifest = json.loads((animation / "animation.json").read_text(encoding="utf-8"))
    assert manifest["schemaVersion"] == 1
    assert manifest["frameWidth"] > 0 and manifest["frameHeight"] > 0
    assert manifest["framesPerSecond"] == 30
    assert manifest["totalFrames"] > 0

    expected = 0
    atlas_width = manifest["frameWidth"] * manifest["columns"]
    atlas_height = manifest["frameHeight"] * manifest["rows"]
    for atlas in manifest["atlases"]:
        assert atlas["firstFrame"] == expected
        assert 0 < atlas["frameCount"] <= manifest["columns"] * manifest["rows"]
        assert png_dimensions(animation / atlas["file"]) == (atlas_width, atlas_height)
        expected += atlas["frameCount"]
    assert expected == manifest["totalFrames"]

    mask_path = animation / manifest["hitMaskFile"]
    with mask_path.open("rb") as stream:
        header = stream.read(24)
    magic, version, width, height, frame_count, bytes_per_frame = struct.unpack("<4sIIIII", header)
    assert magic == b"NWMK" and version == 1
    assert (width, height, frame_count) == (
        manifest["frameWidth"],
        manifest["frameHeight"],
        manifest["totalFrames"],
    )
    assert bytes_per_frame == (width * height + 7) // 8
    assert mask_path.stat().st_size == 24 + frame_count * bytes_per_frame

    source = root / manifest["source"]["file"]
    assert sha256(source) == manifest["source"]["sha256"]

    with wave.open(str(root / "src/NaiWaPet/Assets/Audio/laugh.wav"), "rb") as audio:
        assert audio.getnchannels() == 1
        assert audio.getsampwidth() == 2
        assert audio.getframerate() == 22050
        assert audio.getnframes() > 0

    assert png_dimensions(root / "src/NaiWaPet/Assets/App/naiwa.png") == (256, 256)
    preview = root / "docs/preview.png"
    assert png_dimensions(preview) == (manifest["frameWidth"], manifest["frameHeight"])
    assert png_animation_frames(preview) >= 100
    print(
        f"verified {manifest['totalFrames']} frames, {len(manifest['atlases'])} atlases, "
        f"source {manifest['source']['sha256']}"
    )


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    try:
        verify(root)
    except (AssertionError, OSError, ValueError, KeyError, json.JSONDecodeError) as error:
        print(f"asset verification failed: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

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


def require(condition: bool, message: str) -> None:
    if not condition:
        raise ValueError(message)


def resolve_inside(base: Path, relative: str) -> Path:
    require(bool(relative), "asset path is empty")
    path = (base / relative).resolve()
    try:
        path.relative_to(base.resolve())
    except ValueError as error:
        raise ValueError(f"asset path leaves the expected directory: {relative}") from error
    return path


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
    require(manifest["schemaVersion"] == 1, "unsupported animation schema")
    require(manifest["frameWidth"] > 0 and manifest["frameHeight"] > 0, "invalid frame dimensions")
    require(manifest["framesPerSecond"] == 30, "runtime animation must use 30 FPS")
    require(manifest["totalFrames"] > 0, "animation contains no frames")

    expected = 0
    atlas_width = manifest["frameWidth"] * manifest["columns"]
    atlas_height = manifest["frameHeight"] * manifest["rows"]
    declared_atlases: list[str] = []
    for atlas in manifest["atlases"]:
        require(atlas["firstFrame"] == expected, "animation atlas sequence is not contiguous")
        require(
            0 < atlas["frameCount"] <= manifest["columns"] * manifest["rows"],
            f"invalid frame count in {atlas['file']}",
        )
        atlas_path = resolve_inside(animation, atlas["file"])
        require(png_dimensions(atlas_path) == (atlas_width, atlas_height), f"invalid atlas dimensions: {atlas_path}")
        declared_atlases.append(atlas_path.name)
        expected += atlas["frameCount"]
    require(expected == manifest["totalFrames"], "atlas frame counts do not match the manifest")
    committed_atlases = sorted(path.name for path in animation.glob("laugh-*.png"))
    require(sorted(declared_atlases) == committed_atlases, "committed atlas files do not match the manifest")

    mask_path = resolve_inside(animation, manifest["hitMaskFile"])
    with mask_path.open("rb") as stream:
        header = stream.read(24)
    require(len(header) == 24, "hit-mask header is incomplete")
    magic, version, width, height, frame_count, bytes_per_frame = struct.unpack("<4sIIIII", header)
    require(magic == b"NWMK" and version == 1, "hit-mask header is invalid")
    require(
        (width, height, frame_count)
        == (manifest["frameWidth"], manifest["frameHeight"], manifest["totalFrames"]),
        "hit-mask dimensions do not match the manifest",
    )
    require(bytes_per_frame == (width * height + 7) // 8, "hit-mask row size is invalid")
    require(mask_path.stat().st_size == 24 + frame_count * bytes_per_frame, "hit-mask payload length is invalid")

    source = resolve_inside(root, manifest["source"]["file"])
    expected_hash = manifest["source"]["sha256"]
    require(isinstance(expected_hash, str) and len(expected_hash) == 64, "source SHA-256 is invalid")
    require(sha256(source) == expected_hash, "source video SHA-256 does not match the manifest")

    with wave.open(str(root / "src/NaiWaPet/Assets/Audio/laugh.wav"), "rb") as audio:
        require(audio.getnchannels() == 1, "audio must be mono")
        require(audio.getsampwidth() == 2, "audio must use 16-bit samples")
        require(audio.getframerate() == 22050, "audio must use a 22,050 Hz sample rate")
        require(audio.getnframes() > 0, "audio contains no samples")

    require(png_dimensions(root / "src/NaiWaPet/Assets/App/naiwa.png") == (256, 256), "app icon dimensions are invalid")
    preview = root / "docs/preview.png"
    require(png_dimensions(preview) == (manifest["frameWidth"], manifest["frameHeight"]), "preview dimensions are invalid")
    require(png_animation_frames(preview) >= 100, "preview contains too few animation frames")
    print(
        f"verified {manifest['totalFrames']} frames, {len(manifest['atlases'])} atlases, "
        f"source {manifest['source']['sha256']}"
    )


def main() -> int:
    root = Path(__file__).resolve().parents[1]
    try:
        verify(root)
    except (EOFError, OSError, TypeError, ValueError, KeyError, json.JSONDecodeError, struct.error, wave.Error) as error:
        print(f"asset verification failed: {error}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

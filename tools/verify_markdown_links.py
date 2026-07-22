#!/usr/bin/env python3
"""Check local Markdown links without making network requests."""

from __future__ import annotations

from pathlib import Path
import re
import sys
from urllib.parse import unquote


LINK = re.compile(r"!?\[[^\]]*\]\((<[^>]+>|[^\s)]+)(?:\s+['\"][^'\"]*['\"])?\)")
REMOTE_SCHEMES = ("http://", "https://", "mailto:")


def verify(root: Path) -> int:
    errors: list[str] = []
    checked = 0
    for document in sorted(root.rglob("*.md")):
        if any(part in {".git", "artifacts", "bin", "obj"} for part in document.parts):
            continue
        text = document.read_text(encoding="utf-8")
        for line_number, line in enumerate(text.splitlines(), start=1):
            for match in LINK.finditer(line):
                target = match.group(1).strip("<>")
                if not target or target.startswith("#") or target.lower().startswith(REMOTE_SCHEMES):
                    continue
                path_text = unquote(target.split("#", 1)[0])
                target_path = (document.parent / path_text).resolve()
                try:
                    target_path.relative_to(root.resolve())
                except ValueError:
                    errors.append(f"{document.relative_to(root)}:{line_number}: link leaves repository: {target}")
                    continue
                checked += 1
                if not target_path.exists():
                    errors.append(f"{document.relative_to(root)}:{line_number}: missing local link: {target}")
    if errors:
        for error in errors:
            print(error, file=sys.stderr)
        return 1
    print(f"verified {checked} local Markdown links")
    return 0


if __name__ == "__main__":
    raise SystemExit(verify(Path(__file__).resolve().parents[1]))

"""J16c: local Markdown link check for tracked docs.

Checks every tracked *.md outside reference/archive roots: relative link targets must exist,
and `#anchor` fragments into Markdown files must match a heading slug or an explicit
`<a id="...">`. External (http/mailto) links are not fetched. Read-only; exit 1 on any break.

Usage: py -3 tests/docs/LinkCheck/check_links.py [--json OUT] [--baseline FILE]
  --baseline: JSON list of "source -> target" keys tolerated as pre-existing breaks; only
              breaks not in the baseline fail the run.
"""
from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
import unicodedata
from functools import lru_cache
from pathlib import Path
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[3]
SKIP_TOPS = {"原版游戏本体代码1.3.x", "原版游戏本体代码1.4.5", "Phase0_Local_Archive",
             ".codex_tmp", ".tmp", "tmp", "_deps_auto", "artifacts"}
LINK = re.compile(r"(?<!!)\[(?:[^\]\[]|\[[^\]]*\])*\]\(\s*<?([^)\s>]+)>?(?:\s+\"[^\"]*\")?\s*\)")
IMAGE = re.compile(r"!\[[^\]]*\]\(\s*<?([^)\s>]+)>?")
FENCE = re.compile(r"^\s*(```|~~~)")
INLINE_CODE = re.compile(r"`[^`\n]*`")
HEADING = re.compile(r"^\s{0,3}#{1,6}\s+(.*?)\s*#*\s*$")
ANCHOR = re.compile(r"<a\s+(?:[^>]*?\s)?(?:id|name)=\"([^\"]+)\"", re.I)


def tracked_markdown() -> list[str]:
    out = subprocess.run(["git", "ls-files", "-z", "*.md"], cwd=ROOT, capture_output=True, check=True).stdout
    return [p for p in out.decode("utf-8").split("\0") if p and p.split("/")[0] not in SKIP_TOPS]


def code_free_lines(text: str) -> list[str]:
    lines, fenced = [], False
    for line in text.splitlines():
        if FENCE.match(line):
            fenced = not fenced
            lines.append("")
            continue
        lines.append("" if fenced else INLINE_CODE.sub("", line))
    return lines


def slug(heading: str) -> str:
    """GitHub-style slug: lowercase, strip punctuation (keep CJK/letters/digits/-/_), spaces -> '-'."""
    text = re.sub(r"<[^>]+>", "", heading)
    text = re.sub(r"\[([^\]]*)\]\([^)]*\)", r"\1", text).replace("`", "").strip().lower()
    out = []
    for ch in text:
        if ch in " ":
            out.append("-")
        elif ch in "-_" or ch.isalnum() or unicodedata.category(ch).startswith(("L", "N", "M")):
            out.append(ch)
    return "".join(out)


@lru_cache(maxsize=None)
def anchors(path: Path) -> frozenset[str]:
    text = path.read_text(encoding="utf-8-sig", errors="replace")
    found, counts = set(), {}
    for line in code_free_lines(text):
        m = HEADING.match(line)
        if m:
            s = slug(m.group(1))
            n = counts.get(s, 0)
            counts[s] = n + 1
            found.add(s if n == 0 else f"{s}-{n}")
    found.update(ANCHOR.findall(text))
    return frozenset(found)


def check(files: list[str]) -> list[dict]:
    breaks = []
    for rel in files:
        src = ROOT / rel
        text = src.read_text(encoding="utf-8-sig", errors="replace")
        for lineno, line in enumerate(code_free_lines(text), 1):
            for target in LINK.findall(line) + IMAGE.findall(line):
                if re.match(r"^[a-z][a-z0-9+.-]*:", target, re.I):  # http:, mailto:, file:, etc.
                    continue
                path_part, _, frag = target.partition("#")
                path_part = unquote(path_part)
                dest = src if not path_part else (src.parent / path_part).resolve()
                reason = None
                if path_part and not dest.exists():
                    reason = "missing-path"
                elif frag and dest.is_file() and dest.suffix.lower() == ".md":
                    if unquote(frag).lower() not in {a.lower() for a in anchors(dest)}:
                        reason = "missing-anchor"
                if reason:
                    try:
                        shown = dest.relative_to(ROOT).as_posix()
                    except ValueError:
                        shown = str(dest)
                    breaks.append({"source": rel, "line": lineno, "target": target,
                                   "resolved": shown, "reason": reason,
                                   "key": f"{rel} -> {target}"})
    return breaks


def main(argv: list[str]) -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--json")
    ap.add_argument("--baseline")
    args = ap.parse_args(argv)
    files = tracked_markdown()
    breaks = check(files)
    tolerated = set(json.loads(Path(args.baseline).read_text(encoding="utf-8"))) if args.baseline else set()
    new = [b for b in breaks if b["key"] not in tolerated]
    if args.json:
        Path(args.json).write_text(json.dumps(breaks, ensure_ascii=False, indent=1), encoding="utf-8")
    for b in new[:200]:
        print(f"BREAK {b['reason']:<14} {b['source']}:{b['line']} -> {b['target']}")
    stale = sorted(tolerated - {b["key"] for b in breaks})
    print(f"files={len(files)} breaks={len(breaks)} new={len(new)} baseline_resolved={len(stale)}")
    return 1 if new else 0


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))

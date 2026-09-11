"""Check recorded AF code coordinates without executing game code or changing Git."""
import argparse
import hashlib
import json
import subprocess
import sys
from pathlib import Path, PurePosixPath


def verify(repo, manifest_path, working_tree=False):
    data = json.loads(manifest_path.read_text(encoding="utf-8-sig"))
    if data.get("schemaVersion") != 1:
        raise ValueError("Unsupported code-map schema")
    revision = subprocess.check_output(
        ["git", "rev-parse", "--verify", data["sourceRevision"] + "^{commit}"],
        cwd=repo, text=True).strip()
    cache, ids = {}, set()
    anchors = data["anchors"]
    if not anchors:
        raise ValueError("Code map has no anchors")
    for item in anchors:
        if item["id"] in ids:
            raise ValueError("Duplicate anchor: " + item["id"])
        ids.add(item["id"])
        path = PurePosixPath(item["path"])
        if path.is_absolute() or ".." in path.parts or ":" in str(path) or "\\" in str(path):
            raise ValueError("Expected repository-relative path: " + str(path))
        if str(path) not in cache:
            if working_tree:
                resolved = (repo / str(path)).resolve()
                resolved.relative_to(repo)
                raw = resolved.read_bytes()
            else:
                raw = subprocess.check_output(["git", "show", revision + ":" + str(path)], cwd=repo)
            cache[str(path)] = raw.decode("utf-8-sig").replace("\r\n", "\n")
        text = cache[str(path)]
        if hashlib.sha256(text.encode("utf-8")).hexdigest() != item["fileSha256"]:
            raise ValueError("Stale source content: " + str(path))
        lines = text.splitlines()
        start, end = item["start"], item["end"]
        if not (isinstance(start, int) and isinstance(end, int) and 1 <= start <= end <= len(lines)):
            raise ValueError("Invalid line range: " + item["id"])
        mode, symbol = item["match"], item["symbol"]
        if mode not in ("exact", "contains") or not symbol:
            raise ValueError("Invalid match rule: " + item["id"])
        hits = [i + 1 for i, line in enumerate(lines)
                if (line.strip() == symbol if mode == "exact" else symbol in line)]
        if hits != [start]:
            raise ValueError("Missing/ambiguous/moved symbol: " + item["id"])
    return len(anchors), revision


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=Path(__file__).resolve().parents[4])
    parser.add_argument("--map", type=Path, help="Defaults to the repository's current code map")
    parser.add_argument("--working-tree", action="store_true")
    args = parser.parse_args()
    repo = args.repo.resolve()
    manifest = args.map or repo / "docs/architecture/af-framework-code-map.json"
    try:
        count, revision = verify(repo, manifest, args.working_tree)
    except (OSError, ValueError, KeyError, TypeError, subprocess.CalledProcessError) as error:
        print("FAIL code-map:", error, file=sys.stderr)
        return 1
    mode = "working-tree" if args.working_tree else "recorded-revision"
    print(f"PASS code-map anchors={count} mode={mode} source={revision}; not gameplay verification")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

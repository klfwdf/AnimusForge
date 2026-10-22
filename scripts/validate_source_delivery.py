"""Validate reviewed source scope and outgoing Git history without network writes."""
from __future__ import annotations

import argparse
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys
from urllib.parse import urlsplit

OID = re.compile(r"[0-9a-f]{40}\Z")
ENTRY = re.compile(r"[A-Za-z0-9_.-]+\Z")


def git(repo: Path, *args: str) -> bytes:
    result = subprocess.run(["git", "-C", str(repo), *args], capture_output=True)
    if result.returncode:
        raise ValueError("Git source inspection failed; no delivery was approved.")
    return result.stdout


def load_policy(path: Path) -> dict:
    policy = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(policy, dict) or policy.get("schema") != 1:
        raise ValueError("Unsupported source policy schema.")
    for field in ("reviewedTools", "reservedRoots"):
        values = policy.get(field)
        if not isinstance(values, list) or not values:
            raise ValueError("Source policy entries are missing.")
        if any(not isinstance(v, str) or not ENTRY.fullmatch(v) or v in (".", "..") for v in values):
            raise ValueError("Invalid source policy entry.")
        if len({v.casefold() for v in values}) != len(values):
            raise ValueError("Duplicate source policy entries.")
    return policy


def path_violations(paths: list[str], policy: dict) -> int:
    reviewed = {v.casefold() for v in policy["reviewedTools"]}
    reserved = {v.casefold() for v in policy["reservedRoots"]}
    count = 0
    for path in paths:
        parts = path.split("/")
        invalid = ("\\" in path or any(ord(c) < 32 for c in path)
                   or any(p in ("", ".", "..") for p in parts))
        if invalid or parts[0].casefold() in reserved:
            count += 1
        elif parts[0].casefold() == "tools" and (len(parts) < 2 or parts[1].casefold() not in reviewed):
            count += 1
    return count


def repository_name(url: str) -> str:
    if url.startswith("git@github.com:"):
        name = url[len("git@github.com:"):]
    else:
        parsed = urlsplit(url)
        if (parsed.hostname != "github.com" or parsed.scheme not in ("https", "ssh")
                or parsed.password or parsed.port or parsed.query or parsed.fragment
                or (parsed.scheme == "https" and parsed.username)
                or (parsed.scheme == "ssh" and parsed.username != "git")):
            raise ValueError("Unapproved delivery destination.")
        name = parsed.path.lstrip("/")
    name = name.rstrip("/").removesuffix(".git")
    parts = name.split("/")
    if len(parts) != 2 or any(not ENTRY.fullmatch(p) or p in (".", "..") for p in parts):
        raise ValueError("Invalid delivery destination.")
    return name.casefold()


def check_tree(repo: Path, oid: str, policy: dict) -> None:
    paths = git(repo, "ls-tree", "-r", "-z", "--name-only", oid).decode("utf-8").split("\0")[:-1]
    if path_violations(paths, policy):
        raise ValueError("Source scope is not approved; review the delivery manifest locally.")


def check_objects(repo: Path, revision: str, anchors: list[str], excluded: list[str]) -> None:
    needles = tuple({encoded.lower() for text in excluded for encoded in
                     (text.encode("utf-8"), text.encode("utf-16-le"))})
    if not needles:
        return
    tail_size = max(map(len, needles)) - 1
    objects = git(repo, "rev-list", "--objects", revision, "--not", *anchors).splitlines()
    proc = subprocess.Popen(["git", "-C", str(repo), "cat-file", "--batch"],
                            stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE)
    found = False
    try:
        for entry in objects:
            oid = entry.split(b" ", 1)[0]
            proc.stdin.write(oid + b"\n")
            proc.stdin.flush()
            header = proc.stdout.readline().split()
            if len(header) != 3 or header[1] not in (b"blob", b"tree", b"commit", b"tag"):
                raise ValueError("Incomplete source object inspection.")
            left, tail = int(header[2]), b""
            while left:
                chunk = proc.stdout.read(min(left, 1024 * 1024))
                if not chunk:
                    raise ValueError("Incomplete source object inspection.")
                left -= len(chunk)
                data = tail + chunk.lower()
                found = found or any(n in data for n in needles)
                tail = data[-tail_size:] if tail_size else b""
            if proc.stdout.read(1) != b"\n":
                raise ValueError("Incomplete source object framing.")
        proc.stdin.close()
        if proc.wait() != 0:
            raise ValueError("Source object inspection failed.")
    finally:
        if proc.poll() is None:
            proc.kill()
            proc.wait()
        proc.stdout.close()
        proc.stderr.close()
    if found:
        raise ValueError("Outgoing history requires local source review.")


def check_push(repo: Path, policy: dict, policy_path: Path, rules_path: Path, remote: str, lines: list[str]) -> None:
    updates = []
    for line in lines:
        if not line.strip():
            continue
        fields = line.split()
        if (len(fields) != 4 or not OID.fullmatch(fields[1]) or not OID.fullmatch(fields[3])
                or fields[1] == "0" * 40 or not fields[2].startswith(("refs/heads/", "refs/tags/"))):
            raise ValueError("Invalid or unsupported source update.")
        updates.append(fields)
    if not updates:
        return
    rules = json.loads(rules_path.read_text(encoding="utf-8-sig"))
    if not isinstance(rules, dict) or rules.get("schema") != 1 or rules.get("allowPush") is not True:
        raise ValueError("Repository-local delivery approval is not enabled.")
    if repository_name(remote) != rules.get("approvedRepository"):
        raise ValueError("Unapproved delivery destination.")
    if hashlib.sha256(policy_path.read_bytes()).hexdigest() != rules.get("policySha256"):
        raise ValueError("Source policy changes require local review.")
    anchors = rules.get("anchors")
    excluded = rules.get("excludedText")
    if (not isinstance(anchors, dict) or not anchors
            or any(not isinstance(v, str) or not OID.fullmatch(v) for v in anchors.values())
            or not isinstance(excluded, list) or not excluded
            or any(not isinstance(s, str) or not s for s in excluded)):
        raise ValueError("Invalid repository-local delivery approval.")
    for oid in anchors.values():
        git(repo, "cat-file", "-e", oid + "^{commit}")
    for _, oid, remote_ref, _ in updates:
        required = [anchors[remote_ref]] if remote_ref in anchors else list(anchors.values())
        accepted = any(subprocess.run(["git", "-C", str(repo), "merge-base", "--is-ancestor", anchor, oid],
                                      capture_output=True).returncode == 0 for anchor in required)
        if not accepted:
            raise ValueError("Outgoing ancestry is not approved; use a reviewed source checkout.")
        check_tree(repo, oid, policy)
        commits = git(repo, "rev-list", oid, "--not", *anchors.values()).decode().splitlines()
        for commit in commits:
            check_tree(repo, commit, policy)
        check_objects(repo, oid, list(anchors.values()), excluded)


def main(argv=None) -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo", type=Path, default=Path.cwd())
    parser.add_argument("--policy", type=Path, default=Path(__file__).with_name("source_delivery.json"))
    parser.add_argument("--local-rules", type=Path)
    parser.add_argument("--remote-url", default="")
    mode = parser.add_mutually_exclusive_group()
    mode.add_argument("--pre-push", action="store_true")
    mode.add_argument("--ref", default=None)
    args = parser.parse_args(argv)
    try:
        policy = load_policy(args.policy)
        if args.pre_push:
            if args.local_rules is None:
                raise ValueError("Repository-local delivery approval is required.")
            check_push(args.repo, policy, args.policy, args.local_rules, args.remote_url, sys.stdin.read().splitlines())
        else:
            oid = git(args.repo, "rev-parse", "--verify", "--end-of-options", (args.ref or "HEAD") + "^{commit}").decode().strip()
            check_tree(args.repo, oid, policy)
    except (OSError, ValueError, UnicodeError, KeyError, TypeError):
        print("[ERROR] Source delivery was not approved. Check repository-local policy and outgoing history.", file=sys.stderr)
        return 1
    print("[OK] Source delivery checks passed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

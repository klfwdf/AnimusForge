"""Allocate a new repository-local output directory for standalone runners."""
from __future__ import annotations

import os
import stat
from pathlib import Path
from uuid import uuid4


def new_run_root(repo: Path, family: str, requested: Path | None) -> Path:
    root = repo.resolve(strict=True)
    candidate = requested if requested is not None else root / "artifacts/tests" / family / ("run-" + uuid4().hex)
    if ".." in candidate.parts:
        raise SystemExit("--run-root must not traverse parent directories")
    output = Path(os.path.abspath(candidate))
    if not output.is_relative_to(root) or not output.resolve(strict=False).is_relative_to(root):
        raise SystemExit("--run-root must be a new directory inside the repository")

    current = output
    while True:
        try:
            info = current.lstat()
        except FileNotFoundError:
            pass
        else:
            if current.is_symlink() or getattr(current, "is_junction", lambda: False)() \
                    or getattr(info, "st_file_attributes", 0) & getattr(stat, "FILE_ATTRIBUTE_REPARSE_POINT", 0):
                raise SystemExit("--run-root must not use a reparse point")
        if current == root:
            break
        current = current.parent

    try:
        output.mkdir(parents=True, exist_ok=False)
    except FileExistsError:
        raise SystemExit("--run-root must be a new directory inside the repository") from None
    return output

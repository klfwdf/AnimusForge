"""Allocate a new repository-local output directory for standalone runners."""
from __future__ import annotations

import os
import stat
import shutil
from pathlib import Path
from uuid import uuid4


def resolve_dotnet(repo: Path, requested: str | Path | None = None, major: int = 8) -> Path:
    """Resolve a per-run SDK override; never hide a broken explicit selection."""
    selected = requested or os.environ.get(f"AF_DOTNET{major}")
    if not selected and major == 8:
        selected = os.environ.get("DOTNET_EXE") or os.environ.get("AF_DOTNET")
    if selected:
        executable = Path(selected)
        if not executable.is_file():
            raise SystemExit(f"BLOCKED_ENV: selected dotnet executable is missing: {executable}")
        return executable.resolve()
    candidates = [repo / "local/dotnet/8.0.425/dotnet.exe"] if major == 8 else []
    on_path = shutil.which("dotnet")
    if on_path:
        candidates.append(Path(on_path))
    for executable in candidates:
        if executable.is_file():
            return executable.resolve()
    raise SystemExit(f"BLOCKED_ENV: no dotnet host found; set AF_DOTNET{major}")


def minimal_test_environment(dotnet: Path, output: Path, temp_root: Path | None = None) -> dict[str, str]:
    """Do not forward credentials into compilation/replay subprocesses."""
    allowed = ("SystemRoot", "WINDIR", "ProgramData", "OS", "ProgramFiles",
               "ProgramFiles(x86)", "CommonProgramFiles", "CommonProgramFiles(x86)",
               "PROCESSOR_ARCHITECTURE", "PROCESSOR_IDENTIFIER", "NUMBER_OF_PROCESSORS")
    env = {key: os.environ[key] for key in allowed if key in os.environ}
    # The caller owns output isolation. No global caches or user profile are used.
    home = output / "home"
    env.update({"DOTNET_ROOT": str(dotnet.parent), "PATH": str(dotnet.parent),
                "DOTNET_CLI_HOME": str(home), "HOME": str(home), "USERPROFILE": str(home),
                "APPDATA": str(output / "appdata"), "LOCALAPPDATA": str(output / "appdata"),
                "NUGET_PACKAGES": str(output / "nuget-packages"),
                "NUGET_HTTP_CACHE_PATH": str(output / "nuget-http-cache"),
                "NUGET_PLUGINS_CACHE_PATH": str(output / "nuget-plugin-cache"),
                "DOTNET_CLI_TELEMETRY_OPTOUT": "1", "DOTNET_NOLOGO": "1",
                "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1",
                "DOTNET_GENERATE_ASPNET_CERTIFICATE": "false",
                "DOTNET_CLI_UI_LANGUAGE": "en", "PYTHONIOENCODING": "utf-8"})
    # Data-path tests require a separately approved external synthetic TEMP.
    # Other tests stay local rather than inheriting the real user's TEMP.
    temporary = temp_root if temp_root is not None else output / "temp"
    temporary.mkdir(parents=True, exist_ok=True)
    env.update(TEMP=str(temporary), TMP=str(temporary))
    return env


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

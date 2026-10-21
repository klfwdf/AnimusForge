"""Pinned resampy repair for a freshly copied U runtime, never an installed game."""
from pathlib import Path, PurePosixPath
import hashlib
import zipfile

RESAMPY_VERSION = '0.4.3'
RESAMPY_SHA256 = 'ad2ed64516b140a122d96704e32bc0f92b23f45419e8b8f478e5a05f83edcebd'
RESAMPY_URL = 'https://files.pythonhosted.org/packages/4d/b9/3b00ac340a1aab3389ebcc52c779914a44aadf7b0cb7a3bf053195735607/resampy-0.4.3-py3-none-any.whl'


def install_resampy(runtime, wheel):
    """Caller must supply its new, independent build output. Inputs stay read-only."""
    runtime, wheel = Path(runtime).resolve(strict=True), Path(wheel).resolve(strict=True)
    if hashlib.sha256(wheel.read_bytes()).hexdigest() != RESAMPY_SHA256:
        raise ValueError('resampy_wheel_hash_mismatch')
    site = runtime / 'Lib/site-packages'
    site.resolve(strict=True).relative_to(runtime)
    written = []
    with zipfile.ZipFile(wheel) as archive:
        for info in archive.infolist():
            if info.is_dir():
                continue
            relative = PurePosixPath(info.filename)
            if (relative.is_absolute() or '..' in relative.parts or
                    '\\' in info.filename or ':' in info.filename or
                    relative.parts[0] not in ('resampy', 'resampy-0.4.3.dist-info')):
                raise ValueError('unexpected_resampy_wheel_path')
            target = site.joinpath(*relative.parts)
            target.resolve().relative_to(site.resolve())
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_bytes(archive.read(info))
            written.append(target)
    return written

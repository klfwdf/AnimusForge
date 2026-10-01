"""Fixed terminal migration inverse for historical source-review scopes only.

Behavior fixtures must compile current production owners, not this projection.
The packet is generated only after the terminal source freeze. Historical review
digests are not changed, and neither extra source drift nor owner drift is waived.
"""
from contextlib import contextmanager
from contextvars import ContextVar
from functools import lru_cache, wraps
import hashlib
import json
from pathlib import Path
from unittest.mock import patch

from output_isolation import CURRENT_SOURCE_RELOCATIONS, current_source_path

ROOT = Path(__file__).resolve().parents[1]
PACKET = Path(__file__).with_name("af2_terminal_migration_review.json")
_active = ContextVar("af2_terminal_review_projection", default=False)


def normalized(text):
    return text.replace("\r\n", "\n")


def digest(text):
    return hashlib.sha256(normalized(text).encode("utf-8")).hexdigest()


@lru_cache(maxsize=1)
def packet():
    result = json.loads(PACKET.read_text(encoding="utf-8"))
    assert result["schemaVersion"] == 1
    assert result["purpose"] == "approved-terminal-source-review-inverse"
    return result


def key(path):
    path = Path(path)
    if path.is_absolute():
        return path.resolve().relative_to(ROOT).as_posix()
    value = path.as_posix()
    return CURRENT_SOURCE_RELOCATIONS.get(value, value)


def verify_bindings(read=None):
    read = read or Path.read_text
    for relative, expected in packet()["bindings"].items():
        source = read(current_source_path(ROOT, relative), encoding="utf-8-sig")
        assert digest(source) == expected, "Unreviewed terminal dependency: " + relative


def restore(path, source):
    """Apply context-bearing approved inverse hunks, never replace the input."""
    source = normalized(source)
    relative = key(path)
    row = packet()["paths"].get(relative)
    if row is None:
        return source
    actual = digest(source)
    if actual == row["beforeSha256"]:
        return source
    assert actual == row["afterSha256"], "Unreviewed terminal source: " + relative
    for edit in reversed(row["edits"]):
        after, before = edit["after"], edit["before"]
        assert after, "Terminal inverse requires nonempty surrounding context"
        assert source.count(after) == 1, "Unreviewed terminal context: " + relative
        source = source.replace(after, before, 1)
    assert digest(source) == row["beforeSha256"], "Incomplete terminal inverse: " + relative
    return source


@contextmanager
def projection_reads():
    """Use only around historical review functions, not production extraction."""
    if _active.get():
        yield
        return
    read = Path.read_text
    verify_bindings(read)

    def projected(path, *args, **kwargs):
        source = read(path, *args, **kwargs)
        try:
            relative = key(path)
        except ValueError:
            return source
        return restore(relative, source) if relative in packet()["paths"] else source

    token = _active.set(True)
    try:
        with patch.object(Path, "read_text", projected):
            yield
    finally:
        _active.reset(token)


def terminal_review(function):
    """Keep an existing historical review intact, after the fixed inverse."""
    @wraps(function)
    def reviewed(path, source, *args, **kwargs):
        # An enclosing reviewed inverse already verified the live terminal input.
        # Older approved inverses may now pass their own intermediate projection.
        if _active.get():
            return function(path, source, *args, **kwargs)
        with projection_reads():
            return function(path, restore(path, source), *args, **kwargs)
    return reviewed


def historical_source(path):
    """Explicit legacy oracle read; never use for a current owner fixture/build."""
    with projection_reads():
        return current_source_path(ROOT, key(path)).read_text(encoding="utf-8-sig")


def historical_fixture(function):
    """Scope a named legacy extraction; current production replay stays unprojected."""
    @wraps(function)
    def reviewed(*args, **kwargs):
        with projection_reads():
            return function(*args, **kwargs)
    return reviewed


def historical_test_case(test_case):
    """Keep an explicit old source-guard test class as a bound legacy oracle."""
    for name, function in list(vars(test_case).items()):
        if name.startswith("test_") and callable(function):
            setattr(test_case, name, historical_fixture(function))
    return test_case

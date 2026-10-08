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
import os
import re
from pathlib import Path
from unittest.mock import patch

from output_isolation import CURRENT_SOURCE_RELOCATIONS, current_source_path

ROOT = Path(__file__).resolve().parents[1]
PACKET = Path(__file__).with_name("af2_terminal_migration_review.json")
_active = ContextVar("af2_terminal_review_projection", default=False)


def normalized(text):
    return text.removeprefix("\ufeff").replace("\r\n", "\n")


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


@lru_cache(maxsize=1)
def j17_packet():
    """Independent approved current-to-freeze inverse; the old packet never changes."""
    value = json.loads(PACKET.with_name("j17_terminal_migration_review.json").read_text(encoding="utf-8"))
    assert value["schemaVersion"] == 1
    assert value["purpose"] == "approved-j17-current-to-terminal-freeze-inverse"
    assert value["freeze"] == packet()["freeze"]
    assert value["oldPacketSha256"] == hashlib.sha256(PACKET.read_bytes()).hexdigest(), "Old terminal packet changed"
    assert set(value["paths"]).issubset(packet()["bindings"]), "Unapproved J17 inverse path"
    assert set(value["paths"]).issubset(value["approvedPaths"]), "Unapproved J17 delta path"
    assert set(value["currentOwnerBindings"]).issubset(value["approvedPaths"]), "Unapproved J17 current owner"
    assert set(value["requiredOwnerPaths"]).issubset(value["currentOwnerBindings"]), "Unbound J17 migrated owner"
    for layer in value.get("independentLayers", {}).values():
        assert set(layer["paths"]).issubset(value["approvedPaths"]), "Unapproved independent path"
        assert set(layer["paths"]).issubset(set(value["currentOwnerBindings"]) | set(packet()["bindings"])), "Unbound independent current input"
    return value


def restore_independent_layer(layer, path, source):
    """A named legacy guard input, never a source for a current runtime fixture."""
    source = normalized(source)
    row = j17_packet()["independentLayers"][layer]["paths"].get(key(path))
    if row is None:
        return source
    actual = digest(source)
    if actual == row["targetSha256"]:
        return source
    assert actual == row["sourceSha256"], "Unreviewed independent source: " + layer + ":" + key(path)
    for edit in reversed(row["edits"]):
        after, before = edit["after"], edit["before"]
        assert after and edit["symbols"], "Independent inverse requires named unique context"
        assert source.count(after) == 1, "Unreviewed independent context: " + layer + ":" + key(path)
        source = source.replace(after, before, 1)
    assert digest(source) == row["targetSha256"], "Incomplete independent inverse: " + layer + ":" + key(path)
    return source


_independent_active = ContextVar("j17_independent_review_layers", default=())


@contextmanager
def independent_projection_reads(layer):
    """Compose the terminal oracle before exactly one named older dependency guard."""
    with projection_reads():
        if layer in _independent_active.get():
            yield
            return
        rows = j17_packet()["independentLayers"][layer]["paths"]
        read = Path.read_text
        # The physical current owner is protected by verify_bindings at the outer
        # terminal boundary. Here the read may already be a prior reviewed inverse.
        for relative in rows:
            restore_independent_layer(layer, relative, read(ROOT / relative, encoding="utf-8-sig"))

        def projected(path, *args, **kwargs):
            source = read(path, *args, **kwargs)
            try:
                relative = key(path)
            except ValueError:
                return source
            return restore_independent_layer(layer, relative, source) if relative in rows else source

        token = _independent_active.set(_independent_active.get() + (layer,))
        try:
            with patch.object(Path, "read_text", projected):
                yield
        finally:
            _independent_active.reset(token)


def independent_historical_fixture(layer):
    def decorate(function):
        @wraps(function)
        def reviewed(*args, **kwargs):
            with independent_projection_reads(layer):
                return function(*args, **kwargs)
        return reviewed
    return decorate


def restore_j17(path, source, require_current=False):
    """Undo only unique approved contextual deltas before the original oracle layer."""
    source = normalized(source)
    relative = key(path)
    row = j17_packet()["paths"].get(relative)
    if row is None:
        return source
    actual = digest(source)
    if not require_current and actual == row["freezeSha256"]:
        return source
    assert actual == row["currentSha256"], "Unreviewed terminal source J17: " + relative
    for edit in reversed(row["edits"]):
        after, before = edit["after"], edit["before"]
        assert after and edit["symbols"], "J17 inverse requires context and named symbols"
        assert source.count(after) == 1, "Unreviewed terminal context J17: " + relative
        source = source.replace(after, before, 1)
    assert digest(source) == row["freezeSha256"], "Incomplete J17 terminal inverse: " + relative
    assert digest(source) == packet()["bindings"][relative], "J17 freeze binding differs from original packet: " + relative
    return source


def verify_bindings(read=None):
    read = read or Path.read_text
    for relative, expected in j17_packet()["currentOwnerBindings"].items():
        source = read(current_source_path(ROOT, relative), encoding="utf-8-sig")
        assert digest(source) == expected, "Unreviewed J17 current owner: " + relative
    for relative, expected in packet()["bindings"].items():
        source = read(current_source_path(ROOT, relative), encoding="utf-8-sig")
        try:
            source = restore_j17(relative, source, require_current=True)
        except AssertionError as error:
            raise AssertionError("Unreviewed terminal dependency: " + relative) from error
        assert digest(source) == expected, "Unreviewed terminal dependency: " + relative


def restore(path, source):
    """Apply context-bearing approved inverse hunks, never replace the input."""
    source = normalized(source)
    relative = key(path)
    row = packet()["paths"].get(relative)
    if row is None or digest(source) != row["beforeSha256"]:
        source = restore_j17(relative, source)
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


# This scope is a catalog locator oracle only; it is never a runtime source.
_catalog_active = ContextVar("persistence_catalog_d0", default=None)


def persistence_catalog_storage_sources():
    bound = _catalog_active.get()
    assert bound is not None, "PERSISTENCE_CATALOG_D0 scope required"
    return [ROOT / path for path in bound["chunkSourcePaths"]]


def _catalog_physical_inventory(scope, extract, split):
    # Cold test entry: prune before descent, rather than walking artifact trees.
    excluded = {"tools", "tests", "bin", "obj", ".tmp", "tmp", ".codex_tmp", "_codex_tmp",
                "artifacts", "_deps_auto", ".dotnet", ".dotnet_cli"}
    paths = []
    for base, directories, files in os.walk(ROOT):
        directories[:] = [name for name in directories
                          if name not in excluded and name != ".git"
                          and "原版游戏本体代码" not in name]
        paths.extend(Path(base) / name for name in files if name.endswith(".cs"))
    paths.sort()
    actual_paths = [path.relative_to(ROOT).as_posix() for path in paths]
    assert actual_paths == scope["currentInventory"]["sourcePaths"], "Unreviewed current storage source inventory"
    sources = {path: path.read_bytes().decode("utf-8-sig") for path in paths}
    pattern = re.compile(r'\b(?:private|internal|public|protected)?\s*(?:static\s+)?const\s+string\s+(\w+)\s*=\s*"([^"]+)"')
    declarations = {}
    for path, source in sources.items():
        for name, value in pattern.findall(source):
            declarations.setdefault(name, []).append({"path": path.relative_to(ROOT).as_posix(), "value": value})
    unique = {name: next(iter({item["value"] for item in items}))
              for name, items in declarations.items() if len({item["value"] for item in items}) == 1}
    calls, unresolved = [], []
    for path, source in sources.items():
        constants = dict(unique)
        constants.update(dict(pattern.findall(source)))
        for name in ("SaveChunkedString", "LoadChunkedString", "FlattenStringDictionary"):
            for body in extract(source, name):
                arguments = split(body)
                if len(arguments) < 2:
                    continue
                expression = arguments[1]
                value = expression[1:-1] if expression.startswith('"') and expression.endswith('"') else constants.get(expression)
                row = {"path": path.relative_to(ROOT).as_posix(), "method": name,
                       "arguments": arguments, "keyExpression": expression, "resolvedKey": value,
                       "constantDeclarations": declarations.get(expression, [])}
                (calls if value is not None else unresolved).append(row)
    # The exact declaration/call multiset detects conflicts and duplicates too;
    # it is deliberately not filtered through the old catalog's expected keys.
    expected = scope["currentInventory"]
    assert calls == expected["calls"], "Unreviewed current storage call/constant multiset"
    assert unresolved == expected["unresolvedOrDeclarationExpressions"], "Unreviewed current unresolved storage expression"
    chunked = sorted({row["resolvedKey"] for row in calls if row["method"] != "FlattenStringDictionary"})
    flattened = sorted({row["resolvedKey"] for row in calls if row["method"] == "FlattenStringDictionary"})
    assert chunked == expected["keys"]["chunked"] and len(chunked) == 14, "Current chunk storage keys changed"
    assert flattened == expected["keys"]["flattened"] and len(flattened) == 47, "Current flattened storage keys changed"
    assert "_afDeletedPolicyHistory_v1" in flattened, "Current deleted-policy storage responsibility missing"
    for route in scope["currentRoutes"]:
        source = normalized(sources[ROOT / route["path"]])
        for context in route["contexts"]:
            assert source.count(context) == 1, "Unreviewed current persistence route: " + route["path"]


@contextmanager
def persistence_catalog_d0(extract, split):
    """Strict current bindings, then only the approved historical locator view."""
    if _catalog_active.get() is not None:
        yield
        return
    scope = j17_packet()["persistenceCatalogD0"]
    assert scope["name"] == "PERSISTENCE_CATALOG_D0"
    assert set(scope["physicalBindings"]).issubset(j17_packet()["approvedPaths"]), "Unapproved catalog physical binding"
    for relative, expected in scope["physicalBindings"].items():
        assert hashlib.sha256((ROOT / relative).read_bytes()).hexdigest() == expected, "Unreviewed catalog physical input: " + relative
    _catalog_physical_inventory(scope, extract, split)
    views = {}
    for relative, row in scope["paths"].items():
        source = normalized((ROOT / relative).read_bytes().decode("utf-8-sig"))
        assert digest(source) == row["sourceSha256"], "Unreviewed catalog locating source: " + relative
        for edit in row["edits"]:
            assert edit["after"] and edit["symbols"] and source.count(edit["after"]) == 1, "Unreviewed catalog locating context: " + relative
            source = source.replace(edit["after"], edit["before"], 1)
        assert digest(source) == row["targetSha256"], "Incomplete catalog locating inverse: " + relative
        views[relative] = source
    with projection_reads():
        read = Path.read_text
        def located(path, *args, **kwargs):
            try:
                relative = key(path)
            except ValueError:
                return read(path, *args, **kwargs)
            return views[relative] if relative in views else read(path, *args, **kwargs)
        token = _catalog_active.set(scope)
        try:
            with patch.object(Path, "read_text", located):
                yield
        finally:
            _catalog_active.reset(token)


def persistence_catalog_fixture(function):
    @wraps(function)
    def reviewed(*args, **kwargs):
        namespace = function.__globals__
        with persistence_catalog_d0(namespace["extract_call_arguments"], namespace["split_call_arguments"]):
            return function(*args, **kwargs)
    return reviewed

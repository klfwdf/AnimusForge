"""Offline synthetic migration contract; never reads real PlayerExports."""
import importlib.util
import json
import os
import pathlib
import types
import uuid


ROOT = pathlib.Path(__file__).resolve().parents[3]
spec = importlib.util.spec_from_file_location("af2_migrate", ROOT / "tools" / "af2_migrate.py")
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)

fixture = ROOT / "artifacts" / "tests" / "af2-migration" / uuid.uuid4().hex
source_a = fixture / "installed"
source_b = fixture / "repo"
data_root = fixture / "data"
for directory in (source_a, source_b, data_root):
    directory.mkdir(parents=True)


def put(base, rel, body):
    path = base / rel
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(body)
    return path


put(source_a, "PlayerExports/demo/a.json", b'{"from":"installed"}')
put(source_a, "PlayerExports/demo/b.json", b'{"value":2}')
put(source_b, "PlayerExports/demo/a.json", b'{"from":"repo"}')
put(source_b, "PlayerExports/demo/c.json", b'{"value":3}')
put(data_root, "UserData/PlayerExports/demo/b.json", b"{broken-json")
active_package_mtime = (data_root / "UserData/PlayerExports/demo").stat().st_mtime_ns

sources = [("installed", source_a), ("repo", source_b)]
result = module.migrate(sources, data_root, allow_test_root=True)
assert result["backed_up"] == 4, result
assert result["activated"] == 2, result
assert result["conflicts"] == 2, result
assert (data_root / "UserData/PlayerExports/demo/a.json").read_bytes() == b'{"from":"installed"}'
assert (data_root / "UserData/PlayerExports/demo/b.json").read_bytes() == b"{broken-json"
assert (data_root / "UserData/PlayerExports/demo/c.json").read_bytes() == b'{"value":3}'
assert (data_root / "UserData/PlayerExports/demo").stat().st_mtime_ns == active_package_mtime
recovery = pathlib.Path(result["recovery"])
assert (recovery / "sources/installed/PlayerExports/demo/a.json").read_bytes() == b'{"from":"installed"}'
assert (recovery / "sources/repo/PlayerExports/demo/a.json").read_bytes() == b'{"from":"repo"}'
record = json.loads((recovery / "completed.json").read_text(encoding="utf-8"))
assert record["schema"] == 1 and record["manifestSha256"] == result["manifest_sha256"]
repeat = module.migrate(sources, data_root, allow_test_root=True)
assert repeat["already_complete"] is True and repeat["activated"] == 0

interrupted_source = fixture / "interrupt-source"
interrupted_root = fixture / "interrupt-root"
put(interrupted_source, "PlayerExports/demo/one.json", b"1")
put(interrupted_source, "PlayerExports/demo/two.json", b"2")
old_time = 1_600_000_000_000_000_000
os.utime(interrupted_source / "PlayerExports/demo", ns=(old_time, old_time))
interrupted_root.mkdir()


def fail_after_first(stage, count):
    if stage == "activated" and count == 1:
        raise OSError("synthetic interruption")


try:
    module.migrate([("installed", interrupted_source)], interrupted_root, allow_test_root=True, hook=fail_after_first)
    raise AssertionError("interruption was not raised")
except OSError as ex:
    assert "synthetic interruption" in str(ex)
assert not list((interrupted_root / "Recovery").glob("*/completed.json"))
resumed = module.migrate([("installed", interrupted_source)], interrupted_root, allow_test_root=True)
assert resumed["activated"] == 1 and resumed["already_complete"] is False
assert len(list((interrupted_root / "UserData/PlayerExports/demo").glob("*.json"))) == 2
assert (interrupted_root / "UserData/PlayerExports/demo").stat().st_mtime_ns == old_time

changed_source = fixture / "changed-source"
changed_root = fixture / "changed-root"
put(changed_source, "PlayerExports/demo/one.json", b"before")
changed_root.mkdir()


def change_before_activation(stage, count):
    if stage == "backed_up":
        put(changed_source, "PlayerExports/demo/one.json", b"after")


try:
    module.migrate([("installed", changed_source)], changed_root, allow_test_root=True, hook=change_before_activation)
    raise AssertionError("changed source was not rejected")
except RuntimeError as ex:
    assert "changed" in str(ex).lower()
assert not (changed_root / "UserData/PlayerExports/demo/one.json").exists()
assert not list((changed_root / "Recovery").glob("*/completed.json"))

corrupt_source = fixture / "corrupt-source"
corrupt_root = fixture / "corrupt-root"
put(corrupt_source, "PlayerExports/demo/one.json", b"untouched")
corrupt_root.mkdir()


def corrupt_backup(stage, count):
    if stage == "backed_up":
        backups = list((corrupt_root / "Recovery").glob("*/sources/installed/PlayerExports/demo/one.json"))
        assert len(backups) == 1
        backups[0].write_bytes(b"corrupt")


try:
    module.migrate([("installed", corrupt_source)], corrupt_root, allow_test_root=True, hook=corrupt_backup)
    raise AssertionError("corrupt private backup was not rejected")
except RuntimeError as ex:
    assert "verification failed" in str(ex) or "differs" in str(ex)
assert not (corrupt_root / "UserData/PlayerExports/demo/one.json").exists()
assert not list((corrupt_root / "Recovery").glob("*/completed.json"))

locked_source = fixture / "locked-source"
locked_root = fixture / "locked-root"
put(locked_source, "PlayerExports/demo/one.json", b"x")
(locked_root / "Recovery").mkdir(parents=True)
lock = locked_root / "Recovery/.player-exports-migration.lock"
with lock.open("a+b") as held_lock:
    module._lock_stream(held_lock)
    try:
        try:
            module.migrate([("installed", locked_source)], locked_root, allow_test_root=True)
            raise AssertionError("live ownership lock was ignored")
        except OSError:
            pass
    finally:
        module._unlock_stream(held_lock)
assert lock.exists()  # persistent lock file; OS releases ownership on crash
assert not (locked_root / "UserData/PlayerExports/demo/one.json").exists()

empty_source = fixture / "empty-source"
empty_root = fixture / "empty-root"
empty_source.mkdir()
empty_root.mkdir()
empty = module.migrate([("installed", empty_source)], empty_root, allow_test_root=True)
assert empty["backed_up"] == 0 and not (empty_root / "Recovery").exists()

space_source = fixture / "space-source"
space_root = fixture / "space-root"
put(space_source, "PlayerExports/demo/one.json", b"x")
space_root.mkdir()
original_usage = module.shutil.disk_usage
module.shutil.disk_usage = lambda path: types.SimpleNamespace(free=0)
try:
    try:
        module.migrate([("installed", space_source)], space_root, allow_test_root=True)
        raise AssertionError("disk-full preflight was ignored")
    except RuntimeError as ex:
        assert "free space" in str(ex)
finally:
    module.shutil.disk_usage = original_usage
assert not (space_root / "Recovery").exists()

try:
    module.migrate([("installed", source_a)], ROOT, allow_test_root=False)
    raise AssertionError("source workspace accepted as live AF data root")
except ValueError:
    pass

stale_source = fixture / "stale-source"
stale_root = fixture / "stale-root"
put(stale_source, "PlayerExports/.af-export-retired.fake/old.json", b"old")
stale_root.mkdir()
try:
    module.migrate([("installed", stale_source)], stale_root, allow_test_root=True)
    raise AssertionError("retired export was activated as a package")
except RuntimeError as ex:
    assert "Unresolved export" in str(ex)
assert not (stale_root / "Recovery").exists()

print("PASS AF2 migration synthetic: duplicate sources, target conflict, repeat, interruption, mtime, source change, corrupt backup, lock, empty, disk full, bad root, unresolved export")

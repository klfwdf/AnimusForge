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
if os.name == "nt":
    assert module._source_key(pathlib.Path("C:/Games/AnimusForge")) == "341e25efcc78c23c0ae0dd148a422cbd8cbcd7746d6307246c90ab2bea29bf79"

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
ready = json.loads((data_root / "UserData/.player-exports-ready.json").read_text(encoding="utf-8"))
assert ready == {"schema": 1, "sources": {module._source_key(source_a): result["manifest_sha256"],
                                          module._source_key(source_b): result["manifest_sha256"]}}
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
assert not (interrupted_root / "UserData/.player-exports-ready.json").exists()
resumed = module.migrate([("installed", interrupted_source)], interrupted_root, allow_test_root=True)
assert resumed["activated"] == 1 and resumed["already_complete"] is False
assert len(list((interrupted_root / "UserData/PlayerExports/demo").glob("*.json"))) == 2
assert (interrupted_root / "UserData/PlayerExports/demo").stat().st_mtime_ns == old_time
assert (interrupted_root / "UserData/.player-exports-ready.json").exists()

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
assert not (changed_root / "UserData/.player-exports-ready.json").exists()

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
assert not (corrupt_root / "UserData/.player-exports-ready.json").exists()

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

long_source = fixture / "long-source"
long_root = fixture / "long-root"
backup_parent = long_root / "Recovery" / ("player-exports-" + "x" * 24) / "sources/installed/PlayerExports/demo"
leaf_size = max(20, 236 - len(str(backup_parent)) - len(".json") - 1)
long_name = "x" * leaf_size + ".json"
assert len(str(backup_parent / long_name)) < 250
if os.name == "nt":
    assert len(str(backup_parent / long_name)) + len(".partial.") + 32 >= 260
put(long_source, "PlayerExports/demo/" + long_name, b'{"long":true}')
long_root.mkdir()
long_result = module.migrate([("installed", long_source)], long_root, allow_test_root=True)
assert long_result["activated"] == 1
assert (long_root / "UserData/PlayerExports/demo" / long_name).read_bytes() == b'{"long":true}'
assert (pathlib.Path(long_result["recovery"]) / "sources/installed/PlayerExports/demo" / long_name).read_bytes() == b'{"long":true}'

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

prompt_source = fixture / "prompt-installed"
prompt_repo = fixture / "prompt-repo"
prompt_root = fixture / "prompt-root"
baseline_a = b'{"Version":1,"Text":"BASELINE_A"}'
baseline_b = b'{"Version":1,"Text":"BASELINE_B"}'
put(prompt_source, "CustomPrompts/A.json", baseline_a)
put(prompt_source, "CustomPrompts/B.json", b'{"Version":1,"Text":"INSTALLED_CUSTOM"}')
put(prompt_source, "CustomPrompts/extra.json", b'{"Version":1,"Text":"EXTRA_CUSTOM"}')
put(prompt_source, "CustomPrompts/Policy/Effects/X.json", b'{"Version":1,"Text":"SOURCE_X"}')
put(prompt_source, "CustomPrompts/B.json.bad", b"legacy-corrupt-backup")
put(prompt_repo, "CustomPrompts/B.json", b'{"Version":1,"Text":"REPO_CUSTOM"}')
put(prompt_root, "UserData/Overrides/CustomPrompts/Policy/Effects/X.json", b"{broken-existing")
prompt_baselines = {"a.json": module._hash(prompt_source / "CustomPrompts/A.json"),
                    "b.json": module.hashlib.sha256(baseline_b).hexdigest()}
prompt_sources = [("installed", prompt_source), ("repo", prompt_repo)]
prompt_result = module.migrate_prompts(prompt_sources, prompt_root,
                                       baselines=prompt_baselines, allow_test_root=True)
assert prompt_result["backed_up"] == 6 and prompt_result["activated"] == 2, prompt_result
assert prompt_result["conflicts"] == 2, prompt_result
prompt_active = prompt_root / "UserData/Overrides/CustomPrompts"
assert not (prompt_active / "A.json").exists()
assert (prompt_active / "B.json").read_bytes() == b'{"Version":1,"Text":"INSTALLED_CUSTOM"}'
assert (prompt_active / "extra.json").read_bytes() == b'{"Version":1,"Text":"EXTRA_CUSTOM"}'
assert (prompt_active / "Policy/Effects/X.json").read_bytes() == b"{broken-existing"
assert not (prompt_active / "B.json.bad").exists()
prompt_recovery = pathlib.Path(prompt_result["recovery"])
assert (prompt_recovery / "sources/installed/CustomPrompts/A.json").read_bytes() == baseline_a
assert (prompt_recovery / "sources/installed/CustomPrompts/B.json.bad").read_bytes() == b"legacy-corrupt-backup"
assert (prompt_recovery / "sources/repo/CustomPrompts/B.json").read_bytes() == b'{"Version":1,"Text":"REPO_CUSTOM"}'
assert json.loads((prompt_recovery / "completed.json").read_text(encoding="utf-8"))["manifestSha256"] == prompt_result["manifest_sha256"]
assert json.loads((prompt_root / "UserData/.prompt-overrides-ready.json").read_text(encoding="utf-8"))["sources"] == {
    module._source_key(prompt_source): prompt_result["manifest_sha256"],
    module._source_key(prompt_repo): prompt_result["manifest_sha256"]}
prompt_repeat = module.migrate_prompts(prompt_sources, prompt_root,
                                       baselines=prompt_baselines, allow_test_root=True)
assert prompt_repeat["already_complete"] is True and prompt_repeat["activated"] == 0

prompt_interrupt_source = fixture / "prompt-interrupt-source"
prompt_interrupt_root = fixture / "prompt-interrupt-root"
put(prompt_interrupt_source, "CustomPrompts/one.json", b'{"Text":"one"}')
put(prompt_interrupt_source, "CustomPrompts/two.json", b'{"Text":"two"}')
prompt_interrupt_root.mkdir()
try:
    module.migrate_prompts([("installed", prompt_interrupt_source)], prompt_interrupt_root,
                           baselines={}, allow_test_root=True, hook=fail_after_first)
    raise AssertionError("prompt interruption was not raised")
except OSError as ex:
    assert "synthetic interruption" in str(ex)
assert not list((prompt_interrupt_root / "Recovery").glob("prompt-overrides-*/completed.json"))
prompt_resumed = module.migrate_prompts([("installed", prompt_interrupt_source)], prompt_interrupt_root,
                                        baselines={}, allow_test_root=True)
assert prompt_resumed["activated"] == 1 and not prompt_resumed["already_complete"]
assert len(list((prompt_interrupt_root / "UserData/Overrides/CustomPrompts").glob("*.json"))) == 2

prompt_corrupt_source = fixture / "prompt-corrupt-source"
prompt_corrupt_root = fixture / "prompt-corrupt-root"
put(prompt_corrupt_source, "CustomPrompts/one.json", b'{"Text":"original"}')
prompt_corrupt_root.mkdir()


def corrupt_prompt_backup(stage, count):
    if stage == "backed_up":
        backups = list((prompt_corrupt_root / "Recovery").glob("prompt-overrides-*/sources/installed/CustomPrompts/one.json"))
        assert len(backups) == 1
        backups[0].write_bytes(b"corrupt")


try:
    module.migrate_prompts([("installed", prompt_corrupt_source)], prompt_corrupt_root,
                           baselines={}, allow_test_root=True, hook=corrupt_prompt_backup)
    raise AssertionError("corrupt prompt backup was not rejected")
except RuntimeError as ex:
    assert "verification failed" in str(ex) or "differs" in str(ex)
assert not (prompt_corrupt_root / "UserData/Overrides/CustomPrompts/one.json").exists()
assert not list((prompt_corrupt_root / "Recovery").glob("prompt-overrides-*/completed.json"))

settings_source = fixture / "settings-installed"
settings_repo = fixture / "settings-repo"
settings_root = fixture / "settings-root"
put(settings_source, "ModuleData/TerminalSettings.json", b'{"IsHotkeyEnabled":false,"IsMapIconEnabled":true}')
put(settings_repo, "ModuleData/TerminalSettings.json", b'{"IsHotkeyEnabled":true,"IsMapIconEnabled":false}')
settings_sources = [("installed", settings_source), ("repo", settings_repo)]
settings_result = module.migrate_terminal_settings(settings_sources, settings_root, allow_test_root=True)
assert settings_result["backed_up"] == 2 and settings_result["activated"] == 1 and settings_result["conflicts"] == 1, settings_result
settings_target = settings_root / "UserData/Settings/TerminalSettings.json"
assert settings_target.read_bytes() == (settings_source / "ModuleData/TerminalSettings.json").read_bytes()
settings_recovery = pathlib.Path(settings_result["recovery"])
assert (settings_recovery / "sources/repo/TerminalSettings.json").read_bytes() == (settings_repo / "ModuleData/TerminalSettings.json").read_bytes()
assert module.migrate_terminal_settings(settings_sources, settings_root, allow_test_root=True)["already_complete"]
assert module.migrate_terminal_settings(settings_sources, settings_root, allow_test_root=True)["activated"] == 0

settings_bad_source = fixture / "settings-bad-source"
settings_bad_root = fixture / "settings-bad-root"
put(settings_bad_source, "ModuleData/TerminalSettings.json", b"{broken")
bad_result = module.migrate_terminal_settings([("installed", settings_bad_source)], settings_bad_root, allow_test_root=True)
assert bad_result["backed_up"] == 1 and bad_result["activated"] == 0
assert not (settings_bad_root / "UserData/Settings/TerminalSettings.json").exists()
assert (pathlib.Path(bad_result["recovery"]) / "sources/installed/TerminalSettings.json").read_bytes() == b"{broken"

settings_preexisting_root = fixture / "settings-preexisting-root"
put(settings_preexisting_root, "UserData/Settings/TerminalSettings.json", b'{"IsHotkeyEnabled":true}')
preexisting = module.migrate_terminal_settings([("installed", settings_source)], settings_preexisting_root, allow_test_root=True)
assert preexisting["conflicts"] == 1 and preexisting["activated"] == 0
assert (settings_preexisting_root / "UserData/Settings/TerminalSettings.json").read_bytes() == b'{"IsHotkeyEnabled":true}'

settings_interrupt_root = fixture / "settings-interrupt-root"
settings_interrupt_root.mkdir()
try:
    module.migrate_terminal_settings([("installed", settings_source)], settings_interrupt_root,
                                     allow_test_root=True, hook=fail_after_first)
    raise AssertionError("TerminalSettings interruption was not raised")
except OSError as ex:
    assert "synthetic interruption" in str(ex)
assert (settings_interrupt_root / "UserData/Settings/TerminalSettings.json").read_bytes() == (settings_source / "ModuleData/TerminalSettings.json").read_bytes()
assert not list((settings_interrupt_root / "Recovery").glob("terminal-settings-*/completed.json"))
resumed = module.migrate_terminal_settings([("installed", settings_source)], settings_interrupt_root, allow_test_root=True)
assert resumed["activated"] == 0 and not resumed["already_complete"]

model_source = fixture / "models-installed"
model_repo = fixture / "models-repo"
model_root = fixture / "models-root"
model_bytes = {
    "embedding": {"config.json": b"installed-config", "model.onnx": b"embedding-graph"},
    "reranker": {"config.json": b"reranker-config", "model.onnx": b"reranker-graph"},
}
for group, files in model_bytes.items():
    for name, body in files.items():
        put(model_source, "ONNX/" + ("" if group == "embedding" else "reranker/") + name, body)
put(model_repo, "ONNX/config.json", b"repo-config")
put(model_repo, "ONNX/model.onnx", b"embedding-graph")
model_lock = {"schemaVersion": 1, "groups": {
    "embedding": {"variants": {
        "installed": {name: {"size": len(body), "sha256": module.hashlib.sha256(body).hexdigest()}
                      for name, body in model_bytes["embedding"].items()},
        "repo": {name: {"size": len(body), "sha256": module.hashlib.sha256(body).hexdigest()}
                 for name, body in {"config.json": b"repo-config", "model.onnx": b"embedding-graph"}.items()},
    }},
    "reranker": {"variants": {
        "installed": {name: {"size": len(body), "sha256": module.hashlib.sha256(body).hexdigest()}
                      for name, body in model_bytes["reranker"].items()},
    }},
}}
model_sources = [("installed", model_source), ("repo", model_repo)]
assert module._snapshot_models(model_sources, model_root, model_lock)
model_result = module.migrate_models(model_sources, model_root, allow_test_root=True, lock=model_lock)
assert model_result["backed_up"] == 6 and model_result["activated"] == 4 and model_result["conflicts"] == 1, model_result
model_recovery = pathlib.Path(model_result["recovery"])
assert (model_root / "Models/embedding/config.json").read_bytes() == b"installed-config"
assert (model_root / "Models/reranker/model.onnx").read_bytes() == b"reranker-graph"
assert (model_recovery / "sources/repo/embedding/config.json").read_bytes() == b"repo-config"
assert module.migrate_models(model_sources, model_root, allow_test_root=True, lock=model_lock)["already_complete"]
model_ready = json.loads((model_root / "Models/.af-models-ready.json").read_text(encoding="utf-8"))
assert model_ready["schema"] == 1 and set(model_ready["groups"]) == {"embedding", "reranker"}
assert model_ready["groups"]["embedding"]["variant"] == "installed"

model_conflict_root = fixture / "models-conflict-root"
put(model_conflict_root, "Models/embedding/config.json", b"preexisting-private")
conflict_result = module.migrate_models([("installed", model_source)], model_conflict_root,
                                       allow_test_root=True, lock=model_lock)
assert conflict_result["activated"] == 2 and conflict_result["conflicts"] == 1
assert (model_conflict_root / "Models/embedding/config.json").read_bytes() == b"preexisting-private"
assert (pathlib.Path(conflict_result["recovery"]) / "active-existing/embedding/config.json").read_bytes() == b"preexisting-private"
conflict_ready = json.loads((model_conflict_root / "Models/.af-models-ready.json").read_text(encoding="utf-8"))
assert set(conflict_ready["groups"]) == {"reranker"}

model_bad_source = fixture / "models-bad-source"
put(model_bad_source, "ONNX/config.json", b"wrong")
put(model_bad_source, "ONNX/model.onnx", b"embedding-graph")
try:
    module.migrate_models([("installed", model_bad_source)], fixture / "models-bad-root",
                          allow_test_root=True, lock=model_lock)
    raise AssertionError("corrupt model group was accepted")
except RuntimeError as ex:
    assert "model" in str(ex).lower()
assert not (fixture / "models-bad-root/Models").exists()

model_interrupt_root = fixture / "models-interrupt-root"
model_interrupt_root.mkdir()
def interrupt_model(stage, group):
    if stage == "activated" and group == "embedding":
        raise OSError("synthetic interruption")


try:
    module.migrate_models([("installed", model_source)], model_interrupt_root,
                          allow_test_root=True, lock=model_lock, hook=interrupt_model)
    raise AssertionError("model interruption was not raised")
except OSError as ex:
    assert "synthetic interruption" in str(ex)
assert (model_interrupt_root / "Models/embedding/model.onnx").read_bytes() == b"embedding-graph"
assert not list((model_interrupt_root / "Recovery").glob("models-*/completed.json"))
resumed_models = module.migrate_models([("installed", model_source)], model_interrupt_root,
                                       allow_test_root=True, lock=model_lock)
assert resumed_models["activated"] == 2 and resumed_models["conflicts"] == 0
assert (model_interrupt_root / "Models/reranker/model.onnx").read_bytes() == b"reranker-graph"

model_race_root = fixture / "models-race-root"
model_race_root.mkdir()
def create_racing_target(stage, group):
    if stage == "before_activation" and group == "embedding":
        put(model_race_root, "Models/embedding/private.bin", b"newer")


try:
    module.migrate_models([("installed", model_source)], model_race_root,
                          allow_test_root=True, lock=model_lock, hook=create_racing_target)
    raise AssertionError("racing model target was overwritten")
except RuntimeError as ex:
    assert "appeared" in str(ex)
assert (model_race_root / "Models/embedding/private.bin").read_bytes() == b"newer"
assert not list((model_race_root / "Recovery").glob("models-*/completed.json"))

model_space_root = fixture / "models-space-root"
model_space_root.mkdir()
module.shutil.disk_usage = lambda path: types.SimpleNamespace(free=0)
try:
    try:
        module.migrate_models([("installed", model_source)], model_space_root,
                              allow_test_root=True, lock=model_lock)
        raise AssertionError("model disk-full preflight was ignored")
    except RuntimeError as ex:
        assert "free space" in str(ex)
finally:
    module.shutil.disk_usage = original_usage
assert not (model_space_root / "Recovery").exists()

model_corrupt_root = fixture / "models-corrupt-backup-root"
model_corrupt_root.mkdir()
def corrupt_model_backup(stage, count):
    if stage == "backed_up":
        backups = list((model_corrupt_root / "Recovery").glob("models-*/sources/installed/embedding/model.onnx"))
        assert len(backups) == 1
        backups[0].write_bytes(b"corrupt")


try:
    module.migrate_models([("installed", model_source)], model_corrupt_root,
                          allow_test_root=True, lock=model_lock, hook=corrupt_model_backup)
    raise AssertionError("corrupt model backup was accepted")
except RuntimeError as ex:
    assert "verification failed" in str(ex) or "differs" in str(ex)
assert not (model_corrupt_root / "Models").exists()
assert not list((model_corrupt_root / "Recovery").glob("models-*/completed.json"))

print("PASS AF2 migration synthetic: PlayerExports, Prompt, TerminalSettings and locked model groups; conflicts, repeat, interruption, backup corruption, disk full, bad root")

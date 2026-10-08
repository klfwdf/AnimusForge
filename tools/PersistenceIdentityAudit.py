#!/usr/bin/env python3
"""Read-only save/identity audit against the AF refactor baseline commit."""
from __future__ import annotations

import argparse
from PersistenceTypedRef import resolve_snapshot_bindings
import json
import os
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
DEFAULT_BASELINE = "d4cb1467376c6e923f4295dcefc7878c11dbc7c1"
SYNC = re.compile(r'SyncData\s*(?:<[^>\r\n]+>)?\s*\(\s*"([^"\r\n]+)"\s*,\s*ref\s+([A-Za-z_][A-Za-z0-9_]*)')
DECL = re.compile(
    r'(?m)^\s*(?:(?:public|private|protected|internal|static|readonly|volatile|const)\s+)*'
    r'(?!return\b|throw\b|yield\b|new\b)'
    r'([A-Za-z_][A-Za-z0-9_]*(?:\s*<[^\n;=]+>)?(?:\[\])?)\s+'
    r'([A-Za-z_][A-Za-z0-9_]*)\s*(?:=|;)'
)
BEHAVIOR = re.compile(
    r'\bclass\s+([A-Za-z_][A-Za-z0-9_]*)[^\{]{0,500}\bCampaignBehaviorBase\b',
    re.DOTALL,
)


def progress(message: str, quiet: bool) -> None:
    if not quiet:
        print(message, file=sys.stderr)


def sync_bindings(source: str) -> set[tuple[str, str]]:
    declarations = list(DECL.finditer(source))
    result: set[tuple[str, str]] = set()
    for match in SYNC.finditer(source):
        ref_name = match.group(2)
        candidates = [
            (declaration.start(), declaration.group(1).strip())
            for declaration in declarations
            if declaration.group(2) == ref_name and declaration.start() < match.start()
        ]
        result.add((match.group(1), candidates[-1][1] if candidates else "UNRESOLVED"))
    return result


def compile_glob_pruning_patterns(project_source: str) -> list[str]:
    """Only unconditional removals before the first explicit Include can prune defaults.

    Explicit Includes are still evaluated by the original MSBuild project afterwards.
    Conditional/imported/dynamic item semantics are never approximated here.
    """
    project = ET.fromstring(project_source)
    if project.findall('.//Import') or project.findall('.//Choose'):
        raise ValueError('cannot prove early Compile pruning with imports/Choose')
    patterns = []
    for group in project.findall('ItemGroup'):
        for item in group.findall('Compile'):
            if 'Include' in item.attrib:
                return patterns
            if 'Remove' not in item.attrib:
                continue
            if group.get('Condition') or item.get('Condition'):
                continue
            value = item.attrib['Remove']
            if any(token in value for token in ('$(', '@(', '%(')):
                raise ValueError('cannot prove dynamic Compile removal pruning')
            patterns.extend(value.split(';'))
    return patterns


def production_sources() -> list[Path]:
    # The evaluated implementation Compile items, not every .cs in the repository,
    # define production (test shims and removed extension sources are not owners).
    import importlib.util
    spec = importlib.util.spec_from_file_location('_identity_output_isolation', ROOT / 'tests/output_isolation.py')
    isolation = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(isolation)
    dotnet = isolation.resolve_dotnet(ROOT)
    output = isolation.new_run_root(ROOT, 'persistence-source-evaluation', None)
    env = isolation.minimal_test_environment(dotnet, output)
    for key in ('DOTNET_CLI_HOME', 'USERPROFILE', 'APPDATA', 'LOCALAPPDATA'):
        Path(env[key]).mkdir(parents=True, exist_ok=True)
    env['PYTHONDONTWRITEBYTECODE'] = '1'
    paths = set()
    for api in ('1.3', '1.4'):
        command = [str(dotnet), 'msbuild', str(ROOT / 'AnimusForge.csproj'),
                   '-getItem:Compile', '-p:BannerlordApi=' + api, '-nologo']
        patterns = compile_glob_pruning_patterns((ROOT / 'AnimusForge.csproj').read_bytes().decode('utf-8-sig'))
        actual_command = command + ['-getProperty:DefaultItemExcludes']
        actual_env = env.copy()
        if patterns:
            # SDK defaults append to an environment property, unlike an immutable
            # command-line override. The original project's ordered Include/Remove
            # evaluation remains authoritative, including explicit linked sources.
            glob_patterns = [re.sub(r'[\\/]\*\*[\\/]\*\.cs$', '/**', pattern) for pattern in patterns]
            original_env_exclusions = env.get('DefaultItemExcludes', '')
            actual_env['DefaultItemExcludes'] = original_env_exclusions + ';' + ';'.join(glob_patterns)
            (output / (api + '-pruning-contract.json')).write_text(json.dumps({
                'unprunedCommand': command, 'actualCommand': actual_command,
                'originalEnvironmentExclusions': original_env_exclusions,
                'unconditionalPreIncludeRemovals': patterns,
                'earlySdkGlobExclusions': glob_patterns,
                'unprunedStatus': 'NOT_RUN_PREVIOUS_REAL_TIMEOUT_PRESERVED',
                'mode': 'ACTUAL_ORIGINAL_PROJECT_MSBUILD_WITH_EQUIVALENT_DEFAULT_GLOB_PRUNING'}, indent=2), encoding='utf-8')
        result = subprocess.run(actual_command, cwd=ROOT, env=actual_env, capture_output=True,
                                text=True, encoding='utf-8', errors='strict', timeout=120)
        (output / (api + '.log')).write_text(result.stdout + result.stderr, encoding='utf-8')
        if result.returncode:
            raise RuntimeError('production Compile evaluation failed: ' + api)
        for item in json.loads(result.stdout)['Items']['Compile']:
            path = Path(item['FullPath']).resolve()
            path.relative_to(ROOT.resolve())
            paths.add(path)
    return sorted(paths)


def current_source_snapshot() -> list[tuple[Path, str]]:
    paths = production_sources()
    snapshot: list[tuple[Path, str]] = []
    for path in paths:
        snapshot.append((path, path.read_text(encoding="utf-8", errors="replace")))
    return snapshot


def current_sync(snapshot: list[tuple[Path, str]] | None = None) -> set[tuple[str, str]]:
    captured = snapshot if snapshot is not None else current_source_snapshot()
    return resolve_snapshot_bindings(ROOT, captured, sync_bindings)


def parse_batch_cat_file(data: bytes, expected_objects: int | None = None) -> dict[str, str]:
    """Parse git cat-file --batch output without spawning one process/object."""
    result: dict[str, str] = {}
    offset = 0
    records = 0
    while offset < len(data):
        header_end = data.find(b"\n", offset)
        if header_end < 0:
            raise ValueError("truncated git cat-file batch header")
        header = data[offset:header_end].decode("ascii", errors="strict").split()
        offset = header_end + 1
        records += 1
        if len(header) >= 2 and header[1] == "missing":
            continue
        if len(header) != 3 or header[1] != "blob":
            raise ValueError("unexpected git cat-file batch response")
        object_id = header[0]
        try:
            size = int(header[2])
        except ValueError as exc:
            raise ValueError("invalid git cat-file blob size") from exc
        if size < 0 or offset + size >= len(data):
            raise ValueError("truncated git cat-file batch blob")
        payload = data[offset:offset + size]
        offset += size
        if offset >= len(data) or data[offset:offset + 1] != b"\n":
            raise ValueError("missing git cat-file batch separator")
        offset += 1
        result[object_id] = payload.decode("utf-8", errors="replace")
    if expected_objects is not None and records != expected_objects:
        raise ValueError("git cat-file batch object count mismatch")
    return result


def historical_compile_paths(project_source: str, inventory: list[str]) -> list[str]:
    """Evaluate historical Compile XML over its own tracked inventory, never current globs."""
    import copy
    import importlib.util
    original = ET.fromstring(project_source)
    if original.attrib.get('Sdk') != 'Microsoft.NET.Sdk' or original.findall('.//Import'):
        raise ValueError('unsupported historical project SDK/import')
    if original.findall('.//Choose') or original.findall('.//Target/ItemGroup/Compile'):
        raise ValueError('unsupported historical dynamic Compile items')
    spec = importlib.util.spec_from_file_location('_historical_isolation', ROOT / 'tests/output_isolation.py')
    isolation = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(isolation)
    dotnet = isolation.resolve_dotnet(ROOT)
    from uuid import uuid4
    requested = ROOT / 'artifacts/j17-host-implementation-20261004/a' / ('r1-historical-compile-eval-' + uuid4().hex)
    output = isolation.new_run_root(ROOT, 'persistence-historical-compile', requested)
    env = isolation.minimal_test_environment(dotnet, output)
    for key in ('DOTNET_CLI_HOME', 'USERPROFILE', 'APPDATA', 'LOCALAPPDATA', 'TEMP', 'TMP'):
        Path(env[key]).mkdir(parents=True, exist_ok=True)
    env['PYTHONDONTWRITEBYTECODE'] = '1'
    inventory = sorted(set(inventory))
    if any(Path(path).is_absolute() or '..' in path.split('/') for path in inventory):
        raise ValueError('historical inventory escapes project')

    def glob_matches(path: str, pattern: str) -> bool:
        pattern = re.sub(r'/+', '/', pattern.replace('\\', '/'))
        # MSBuild **/ also matches zero directories; ordinary * never crosses /.
        tokens = re.split(r'(\*\*/|\*\*|\*|\?)', pattern)
        expression = ''.join({'**/': '(?:.*/)?', '**': '.*', '*': '[^/]*', '?': '[^/]'}.get(t, re.escape(t)) for t in tokens)
        return re.fullmatch(expression, path, flags=re.IGNORECASE) is not None

    def evaluate(model: ET.Element, api: str, label: str, query: str) -> dict:
        file = output / (api + '-' + label + '.csproj')
        ET.ElementTree(model).write(file, encoding='utf-8', xml_declaration=True)
        command = [str(dotnet), 'msbuild', str(file), query, '-p:BannerlordApi=' + api, '-p:ImportDirectoryBuildProps=false',
                   '-p:ImportDirectoryBuildTargets=false', '-nologo']
        result = subprocess.run(command, cwd=output, env=env, capture_output=True,
                                text=True, encoding='utf-8', errors='strict', timeout=120)
        (output / (api + '-' + label + '.log')).write_text(result.stdout + result.stderr, encoding='utf-8')
        if result.returncode:
            raise RuntimeError('historical Compile evaluation failed: ' + api + '/' + label)
        return json.loads(result.stdout)

    selected = set()
    for api in ('1.3', '1.4'):
        model = ET.Element('Project', {'Sdk': 'Microsoft.NET.Sdk'})
        for group in original.findall('PropertyGroup'):
            model.append(copy.deepcopy(group))
        flags = ET.SubElement(model, 'PropertyGroup')
        for name in ('EnableDefaultCompileItems', 'ImportDirectoryBuildProps', 'ImportDirectoryBuildTargets'):
            ET.SubElement(flags, name).text = 'false'
        properties = evaluate(model, api, 'properties', '-getProperty:DefaultItemExcludes,DefaultExcludesInProjectFolder')['Properties']
        excludes = []
        for value in properties.values():
            for pattern in value.replace('\\', '/').split(';'):
                prefix = output.as_posix() + '/'
                if pattern.startswith(prefix):
                    pattern = pattern[len(prefix):]
                if pattern:
                    excludes.append(pattern)
        defaults = [path for path in inventory if path.endswith('.cs') and not any(glob_matches(path, x) for x in excludes)]
        initial = ET.SubElement(model, 'ItemGroup')
        for path in defaults:
            ET.SubElement(initial, 'Compile', {'Include': path})
        for group in original.findall('ItemGroup'):
            if 'Exists(' in group.get('Condition', ''):
                raise ValueError('unsupported historical filesystem Compile condition')
            target = ET.Element('ItemGroup', group.attrib)
            for node in group.findall('Compile'):
                item = copy.deepcopy(node)
                if 'Exists(' in item.get('Condition', ''):
                    raise ValueError('unsupported historical filesystem Compile condition')
                for attr in ('Include', 'Remove', 'Exclude', 'Update'):
                    value = item.get(attr, '')
                    if any(token in value for token in ('$(', '@(', '%(')):
                        raise ValueError('unsupported historical Compile expression: ' + value)
                if 'Include' in item.attrib:
                    patterns = item.attrib['Include'].split(';')
                    if any(not any(c in pattern for c in '*?') and pattern.replace('\\', '/') not in inventory for pattern in patterns):
                        raise ValueError('historical explicit Compile input unavailable')
                    matches = [path for path in inventory if any(glob_matches(path, pattern) for pattern in patterns)]
                    for path in matches:
                        concrete = copy.deepcopy(item)
                        concrete.set('Include', path)
                        target.append(concrete)
                else:
                    target.append(item)
            if len(target):
                model.append(target)
        result = evaluate(model, api, 'items', '-getItem:Compile')
        for item in result['Items']['Compile']:
            path = item['Identity'].replace('\\', '/')
            if path not in inventory:
                raise ValueError('historical Compile item is not from historical inventory: ' + path)
            selected.add(path)
    (output / 'compile-inputs.json').write_text(json.dumps({'inventory': inventory, 'selected': sorted(selected)}, ensure_ascii=False, indent=2), encoding='utf-8')
    return sorted(selected)


def baseline_source_snapshot(commit: str) -> list[tuple[str, str]]:
    git_env = os.environ.copy()
    git_env["GIT_NO_LAZY_FETCH"] = "1"
    tree = subprocess.run(
        ["git", "--no-optional-locks", "-c", "core.fsmonitor=false", "ls-tree", "-r", "--format=%(objectname)\t%(path)", commit],
        cwd=ROOT, capture_output=True, check=False, env=git_env,
    )
    if tree.returncode != 0:
        raise RuntimeError("cannot load baseline tree")
    objects: list[tuple[str, str]] = []
    for raw in tree.stdout.splitlines():
        try:
            object_id, relative_bytes = raw.split(b"\t", 1)
        except ValueError as exc:
            raise ValueError("malformed baseline tree entry") from exc
        relative = relative_bytes.decode("utf-8", errors="strict")
        if relative in ("Directory.Build.props", "Directory.Build.targets"):
            raise ValueError("unsupported baseline Directory.Build import")
        if not (relative.endswith(".cs") or relative == "AnimusForge.csproj"):
            continue
        objects.append((object_id.decode("ascii"), relative))
    if not objects:
        return []
    process = subprocess.Popen(
        ["git", "--no-optional-locks", "-c", "core.fsmonitor=false", "cat-file", "--batch"],
        cwd=ROOT, stdin=subprocess.PIPE, stdout=subprocess.PIPE, stderr=subprocess.PIPE, env=git_env,
    )
    request = ("".join(object_id + "\n" for object_id, _ in objects)).encode("ascii")
    stdout, stderr = process.communicate(request, timeout=120)
    if process.returncode != 0:
        raise RuntimeError("baseline source batch read failed: " + stderr.decode(errors="replace")[:160])
    blobs = parse_batch_cat_file(stdout, expected_objects=len(objects))
    missing = [object_id for object_id, _relative in objects if object_id not in blobs]
    if missing:
        raise RuntimeError("baseline source blob unavailable (" + str(len(missing)) + " missing)")
    project = next((blobs[oid] for oid, path in objects if path == 'AnimusForge.csproj'), None)
    if project is None:
        raise RuntimeError('baseline actual project unavailable')
    selected = set(historical_compile_paths(project, [path for _oid, path in objects if path.endswith('.cs')]))
    return [(relative, blobs[object_id]) for object_id, relative in objects if relative in selected]


def behavior_names(source: str) -> set[str]:
    return {match.group(1) for match in BEHAVIOR.finditer(source)}


def current_behaviors(snapshot: list[tuple[Path, str]] | None = None) -> set[str]:
    result: set[str] = set()
    for _path, source in snapshot if snapshot is not None else current_source_snapshot():
        result |= behavior_names(source)
    return result


def baseline_behaviors(commit: str, snapshot: list[tuple[str, str]] | None = None) -> set[str]:
    result: set[str] = set()
    for _relative, source in snapshot if snapshot is not None else baseline_source_snapshot(commit):
        result |= behavior_names(source)
    return result


def module_identity(relative: str) -> tuple[str, str, list[str]]:
    root = ET.parse(ROOT / relative).getroot()
    name = root.find("./Name").attrib.get("value", "")
    module_id = root.find("./Id").attrib.get("value", "")
    assemblies = [node.attrib.get("value", "") for node in root.findall(".//Assembly")]
    return name, module_id, assemblies


def reviewed_civilwar_v2(current: set[tuple[str, str]], baseline: set[tuple[str, str]]) -> bool:
    """Only the approved v1 -> v2 string-key replacement; raw differences stay visible.

    This is NOT old-save compatibility: the remote v2 owner deliberately loads v2.
    Every other key/type delta still fails this comparison.
    """
    old = ('_af_kingdom_civil_war_v1', 'string')
    new = ('_af_kingdom_civil_war_v2', 'string')
    return old in baseline and new not in baseline and old not in current \
        and current == (baseline - {old}) | {new}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--baseline", default=DEFAULT_BASELINE)
    parser.add_argument("--json", action="store_true")
    parser.add_argument("--review-civilwar-v2", action="store_true", help="Accept only the approved v1 to v2 string-key delta; not old-save acceptance")
    parser.add_argument("--quiet", action="store_true")
    args = parser.parse_args()
    try:
        progress("current source enumeration", args.quiet)
        current_snapshot = current_source_snapshot()
        progress(f"current source loading ({len(current_snapshot)} files)", args.quiet)
        current = current_sync(current_snapshot)
        current_beh = current_behaviors(current_snapshot)
        progress("baseline tree loading", args.quiet)
        progress("baseline source batch reading", args.quiet)
        baseline_snapshot = baseline_source_snapshot(args.baseline)
        baseline = current_sync([(Path(relative), source) for relative, source in baseline_snapshot])
        baseline_beh = baseline_behaviors(args.baseline, baseline_snapshot)
        progress("comparison complete", args.quiet)
        name, module_id, assemblies = module_identity("AnimusForge/SubModule.xml")
        result = {
            "status": "PASS",
            "baseline": args.baseline,
            "syncCurrent": len(current),
            "syncBaseline": len(baseline),
            "syncAdded": sorted(current - baseline),
            "syncRemoved": sorted(baseline - current),
            "behaviorCurrent": len(current_beh),
            "behaviorBaseline": len(baseline_beh),
            "behaviorAdded": sorted(current_beh - baseline_beh),
            "behaviorRemoved": sorted(baseline_beh - current_beh),
            "moduleName": name,
            "moduleId": module_id,
            "moduleAssemblies": assemblies,
        }
        sync_equal = current == baseline
        if args.review_civilwar_v2:
            result['reviewedCivilWarV2'] = reviewed_civilwar_v2(current, baseline)
            result['oldSaveCompatibility'] = 'NOT_RUN'
            sync_equal = result['reviewedCivilWarV2']
        if not sync_equal or current_beh != baseline_beh or name != "AnimusForge" or module_id != "AnimusForge" or assemblies != ["AnimusForge.Bootstrap.dll"]:
            result["status"] = "FAIL"
    except Exception as exc:
        result = {"status": "FAIL", "error": str(exc)[:240]}
        print("persistence identity audit failed: " + result["error"], file=sys.stderr)
        if args.json:
            print(json.dumps(result, ensure_ascii=False, sort_keys=True))
        else:
            print("FAIL persistenceIdentity " + result["error"])
        return 1
    if args.json:
        print(json.dumps(result, ensure_ascii=False, sort_keys=True))
    else:
        if result["status"] == "PASS":
            print(f"PASS persistenceIdentity sync={len(current)} behavior={len(current_beh)} module=AnimusForge bootstrap=1")
        else:
            print(f"FAIL persistenceIdentity {result}")
    return 0 if result["status"] == "PASS" else 1


if __name__ == "__main__":
    raise SystemExit(main())

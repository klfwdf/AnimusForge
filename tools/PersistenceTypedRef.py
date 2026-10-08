"""Bounded, batched semantic fallback for unresolved SyncData ref expressions.

The fragment sync_bindings API remains unchanged. Only affected source files and
uniquely declared types needed by dotted ref roots enter the semantic compilation.
Unknown symbols stay UNRESOLVED; key literals are never normalized or inferred.
"""
from __future__ import annotations
import importlib.util
import json
import re
import subprocess
from pathlib import Path
from uuid import uuid4
from xml.sax.saxutils import escape

DOTTED_REF = re.compile(r'\bSyncData\s*(?:<[^>\r\n]+>)?\s*\(\s*"[^"\r\n]+"\s*,\s*ref\s+([A-Za-z_]\w*)\s*\.')
_BUILD = {}


def _build(root: Path):
    identity = str(root.resolve())
    if identity in _BUILD:
        return _BUILD[identity]
    spec = importlib.util.spec_from_file_location('_audit_output_isolation', root / 'tests/output_isolation.py')
    isolation = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(isolation)
    dotnet = isolation.resolve_dotnet(root)
    output = isolation.new_run_root(root, 'persistence-typed-ref', None)
    env = isolation.minimal_test_environment(dotnet, output)
    for key in ('DOTNET_CLI_HOME', 'USERPROFILE', 'APPDATA', 'LOCALAPPDATA'):
        Path(env[key]).mkdir(parents=True, exist_ok=True)
    env['PYTHONDONTWRITEBYTECODE'] = '1'
    helper = Path(__file__).with_name('PersistenceTypedRef.cs')
    project = output / 'TypedRef.csproj'
    project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>net8.0</TargetFramework><OutputType>Exe</OutputType><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="' + escape(str(helper), {'"': '&quot;'}) + '"/><Reference Include="Microsoft.CodeAnalysis"><HintPath>$(MSBuildToolsPath)/Roslyn/bincore/Microsoft.CodeAnalysis.dll</HintPath></Reference><Reference Include="Microsoft.CodeAnalysis.CSharp"><HintPath>$(MSBuildToolsPath)/Roslyn/bincore/Microsoft.CodeAnalysis.CSharp.dll</HintPath></Reference></ItemGroup></Project>', encoding='utf-8')
    config = output / 'NuGet.Config'
    config.write_text('<configuration><packageSources><clear /></packageSources></configuration>', encoding='utf-8')
    command = [str(dotnet), 'build', str(project), '--configfile', str(config), '--nologo', '-v:q']
    result = subprocess.run(command, cwd=output, env=env, capture_output=True, text=True, timeout=120)
    (output / 'build.log').write_text(result.stdout + result.stderr, encoding='utf-8')
    if result.returncode:
        raise RuntimeError('typed-ref semantic helper build failed; see ' + str(output / 'build.log'))
    dll = output / 'bin/Debug/net8.0/TypedRef.dll'
    if not dll.is_file():
        raise RuntimeError('typed-ref helper build produced no executable')
    _BUILD[identity] = (dotnet, output, env, dll)
    return _BUILD[identity]


def resolve_snapshot_bindings(root: Path, snapshot, legacy):
    captured = [(str(path), source) for path, source in snapshot]
    affected = {path for path, source in captured
                if DOTTED_REF.search(source) or re.search(r'\bSyncData\s*<', source)
                or any(kind == 'UNRESOLVED' for _, kind in legacy(source))}
    if not affected:
        return set().union(*(legacy(source) for _, source in captured)) if captured else set()
    if len({path for path, _ in captured}) != len(captured):
        raise ValueError('duplicate source path in typed-ref snapshot')
    # Compile one captured production batch. C# namespace/containing-type identity
    # and partial merging must be bound by Roslyn, not a simple-name regex.
    inputs = dict(captured)
    dotnet, output, env, dll = _build(root)
    batch = output / ('batch-' + uuid4().hex)
    batch.mkdir()
    input_file, result_file = batch / 'input.json', batch / 'result.json'
    input_file.write_text(json.dumps(inputs, ensure_ascii=False), encoding='utf-8')
    result = subprocess.run([str(dotnet), str(dll), str(input_file), str(result_file)], cwd=batch,
                            env=env, capture_output=True, text=True, timeout=120)
    (batch / 'run.log').write_text(result.stdout + result.stderr, encoding='utf-8')
    if result.returncode or not result_file.is_file():
        raise RuntimeError('typed-ref semantic batch failed: ' + (result.stderr + result.stdout)[-1500:] + '; see ' + str(batch / 'run.log'))
    rows = json.loads(result_file.read_text(encoding='utf-8'))
    values = set().union(*(legacy(source) for path, source in captured if path not in affected))
    values.update((row['key'], row['type']) for row in rows if row['path'] in affected)
    return values

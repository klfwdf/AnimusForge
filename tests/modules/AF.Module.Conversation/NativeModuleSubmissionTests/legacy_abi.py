"""Compile the pinned Native consumer against old V1; run the unchanged binary with current V1.

Only API sources are historical. Both libraries use the existing source-linked current
Native owner fixture, so this proves CLR member binding + preserved Native behavior,
not the game implementation's dependency/Bootstrap loading (covered separately).
"""
from pathlib import Path
import hashlib
import importlib.util
import json
import os
import shutil
import subprocess

ROOT = Path(__file__).resolve().parents[4]
BASELINE = '39cf9d4724cc372a33503da270fd0c0e9dc6e6af'


def verify(dotnet, out, current_sources):
    spec = importlib.util.spec_from_file_location('abi_projects', ROOT/'tests/AF.Contracts/ModuleFrameworkApiTests/run.py')
    projects = importlib.util.module_from_spec(spec); spec.loader.exec_module(projects)
    base = out/'Baseline'; base.mkdir(parents=True, exist_ok=True)
    sources = list(current_sources)
    proof = {'baseline': BASELINE, 'scope': __doc__, 'historical_sources': {}, 'adaptations': []}

    def historical(path):
        value = subprocess.check_output(['git', 'show', BASELINE+':'+path], cwd=ROOT).decode('utf-8-sig')
        proof['historical_sources'][path] = hashlib.sha256(value.encode()).hexdigest()
        return value

    for path in sorted(projects.API_SOURCES):
        target = base/Path(path).name
        target.write_text(historical(path), encoding='utf-8')
        hits = [i for i, source in enumerate(sources) if source.name == target.name]
        assert len(hits) == 1, path
        sources[hits[0]] = target
    library = projects.project(base/'Library', 'NativeModuleUnderTest', sources)
    client_source = historical('tests/modules/AF.Module.Conversation/NativeModuleSubmissionTests/Client.cs.txt')
    # The old test-control loop knows five CoreCases; newer fixtures append cases.
    # Capability availability is an intentional additive change, not old Native ABI.
    edits = [
        ('bool[] core=NativeHost.CoreCases();', 'bool[] core=NativeHost.CoreCases().Take(5).ToArray();'),
        ('AfApi.GetCapability(AfCapabilityIds.SceneSubmit).State==AfCapabilityState.NotSupported&&AfApi.GetCapability(AfCapabilityIds.CourierSubmit).State==AfCapabilityState.NotSupported',
         'AfApi.GetCapability(AfCapabilityIds.SceneSubmit).State.ToString()==(Environment.GetEnvironmentVariable("AF_ABI_EXPECT_CHANNEL_STATE")??"NotSupported")&&AfApi.GetCapability(AfCapabilityIds.CourierSubmit).State.ToString()==(Environment.GetEnvironmentVariable("AF_ABI_EXPECT_CHANNEL_STATE")??"NotSupported")')]
    for before, after in edits:
        assert client_source.count(before) == 1, 'Pinned consumer anchor drift: '+before
        client_source = client_source.replace(before, after)
        proof['adaptations'].append({'before': before, 'after': after})
    source_file = base/'LegacyClient.cs'; source_file.write_text(client_source, encoding='utf-8')
    consumer = projects.project(base/'Consumer', 'LegacyNativeConsumer', [source_file], [library], True)
    code, log = projects.run_dotnet(str(dotnet), ['run', '--project', str(consumer), '-c', 'Release'], ROOT)
    (base/'run.log').write_text(log, encoding='utf-8')
    assert code == 0 and 'PASS 41 Native module API checks' in log, 'Baseline Native consumer failed:\n'+log
    consumer_output = base/'Consumer/bin/Release/net8.0'
    client_dll = consumer_output/'LegacyNativeConsumer.dll'
    client_hash = hashlib.sha256(client_dll.read_bytes()).hexdigest()
    current_dll = out/'bin/Release/net8.0/NativeModuleUnderTest.dll'
    baseline_dll = consumer_output/'NativeModuleUnderTest.dll'
    assert current_dll.read_bytes() != baseline_dll.read_bytes(), 'Old and current libraries unexpectedly identical'
    # Separate execution directory preserves the baseline build. Never rebuild the client.
    execution = out/'CurrentBinary'; execution.mkdir(exist_ok=True)
    for file in consumer_output.iterdir():
        if file.is_file() and file.name != 'NativeModuleUnderTest.dll': shutil.copyfile(file, execution/file.name)
    shutil.copyfile(current_dll, execution/current_dll.name)
    env = projects.environment(str(dotnet)); env['AF_ABI_EXPECT_CHANNEL_STATE'] = 'Available'
    result = subprocess.run([str(dotnet), str(execution/'LegacyNativeConsumer.dll')], cwd=ROOT, env=env,
        capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=150)
    log = result.stdout+result.stderr; (out/'legacy-current.log').write_text(log, encoding='utf-8')
    assert result.returncode == 0 and 'PASS 41 Native module API checks' in log, 'Unchanged old consumer failed with current V1:\n'+log
    assert client_hash == hashlib.sha256((execution/client_dll.name).read_bytes()).hexdigest(), 'Legacy consumer was rebuilt or modified'
    proof.update(consumer_sha256=client_hash, baseline_library_sha256=hashlib.sha256(baseline_dll.read_bytes()).hexdigest(),
        current_library_sha256=hashlib.sha256(current_dll.read_bytes()).hexdigest(), checks_before=41, checks_after=41,
        consumer_recompiled_after_baseline=False)
    (out/'legacy-abi.json').write_text(json.dumps(proof, indent=2, ensure_ascii=False), encoding='utf-8')
    print('PASS legacy Native consumer baseline=39cf9d47 checks=41+41 unchanged_binary='+client_hash)
    print('NOT TESTED: legacy sub-MOD inside Bannerlord; source-linked current host/game/provider fixtures')

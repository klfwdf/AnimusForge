"""Strict J02 host/composition inverse against the pre-extraction source."""
from pathlib import Path
import importlib.util
import subprocess

ROOT = Path(__file__).resolve().parents[2]
BASELINE = '3706e87dfc4be2cf150b9f45f029e7f105a28fd3'
DEST = ROOT / 'src/AF.GameAdapter.Bannerlord/Composition'
MOVED = ('CampaignComposition.cs', 'CampaignModelComposition.cs',
         'ModuleFrameworkRuntime.cs', 'TeamModuleRegistration.cs', 'TeamModuleServices.cs')
spec = importlib.util.spec_from_file_location('host_declaration', ROOT / 'tools/ChannelCutoverBoundaryTests/run.py')
extractor = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extractor)
declaration = extractor.declaration


def old(path):
    return subprocess.check_output(['git', 'show', BASELINE + ':' + path], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')


def read(path):
    return path.read_text(encoding='utf-8-sig').replace('\r\n', '\n')


def restore_submodule(current):
    prior = old('SubModule.cs')
    startup = declaration(prior, 'protected override void OnBeforeInitialModuleScreenSetAsRoot()')
    app = declaration(prior, 'protected override void OnApplicationTick(float dt)')
    fast = declaration(prior, 'private void RunFastApplicationTickPhases()')
    watched = declaration(prior, 'private void RunWatchedApplicationTickPhases()')
    phase = declaration(prior, 'private static void RunWatchedTickPhase(string name, Action action)')

    moved_startup = startup.replace('protected override void OnBeforeInitialModuleScreenSetAsRoot()',
        'internal static void Register()', 1).replace('\t\tbase.OnBeforeInitialModuleScreenSetAsRoot();\n', '', 1)
    expected_startup = ('using System;\nusing HarmonyLib;\n\nnamespace AnimusForge;\n\n'
        '// Startup patch and service composition stays on the Bannerlord main-thread entry point.\n'
        'internal static class StartupPatchComposition\n{\n\t' + moved_startup + '\n}\n')
    assert read(DEST / 'StartupPatchComposition.cs') == expected_startup, 'Startup body/order/catches changed'

    moved_app = app.replace('protected override void OnApplicationTick(float dt)',
        'internal static void Run(SubModule host, float dt)', 1).replace(
        'RunFastApplicationTickPhases();', 'RunFastApplicationTickPhases(host);', 1).replace(
        'RunWatchedApplicationTickPhases();', 'RunWatchedApplicationTickPhases(host);', 1).replace(
        'TickWarStatsMapButton(dt);', 'host.TickWarStatsMapButton(dt);', 1)
    moved_fast = fast.replace('private void RunFastApplicationTickPhases()',
        'private static void RunFastApplicationTickPhases(SubModule host)', 1).replace(
        'ProcessPendingInitialApiGuideNotice();', 'host.ProcessPendingInitialApiGuideNotice();', 1)
    moved_watched = watched.replace('private void RunWatchedApplicationTickPhases()',
        'private static void RunWatchedApplicationTickPhases(SubModule host)', 1).replace(
        '() => ProcessPendingInitialApiGuideNotice()', 'host.ProcessPendingInitialApiGuideNotice', 1)
    expected_tick = ('using System;\nusing AnimusForge.PolicyEffects;\n\nnamespace AnimusForge;\n\n'
        '// Ordered game-tick dispatch; fast path has no per-frame phase list or delegate allocation.\n'
        'internal static class ApplicationTickComposition\n{\n\t'
        + '\n\n\t'.join((moved_app, moved_fast, moved_watched, phase)) + '\n}\n')
    assert read(DEST / 'ApplicationTickComposition.cs') == expected_tick, 'Tick branches/order/scopes changed'
    assert 'new Action' not in moved_fast and '() =>' not in moved_fast and 'new[]' not in moved_fast
    assert moved_watched.count('host.ProcessPendingInitialApiGuideNotice') == 1

    expected = prior.replace(startup,
        'protected override void OnBeforeInitialModuleScreenSetAsRoot()\n\t{\n'
        '\t\tbase.OnBeforeInitialModuleScreenSetAsRoot();\n\t\tStartupPatchComposition.Register();\n\t}', 1)
    expected = expected.replace(app,
        'protected override void OnApplicationTick(float dt)\n\t{\n'
        '\t\tApplicationTickComposition.Run(this, dt);\n\t}', 1)
    for method in (fast, watched, phase):
        token = '\t' + method + '\n\n'
        assert expected.count(token) == 1, 'Old Tick declaration not unique'
        expected = expected.replace(token, '', 1)
    for signature in ('private void ProcessPendingInitialApiGuideNotice()',
                      'private void TickWarStatsMapButton(float dt)'):
        assert expected.count(signature) == 1
        expected = expected.replace(signature, signature.replace('private', 'internal', 1), 1)
    assert current == expected, 'SubModule differs beyond reviewed host extraction'
    return prior


def verify():
    restore_submodule(read(ROOT / 'SubModule.cs'))
    for name in MOVED:
        assert not (ROOT / 'Refactor/Modules' / name).exists(), 'Old composition source still exists: ' + name
        assert read(DEST / name) == old('Refactor/Modules/' + name), 'Moved composition changed: ' + name
    print('PASS J02 full-file SubModule inverse + exact Startup/Tick bodies + 5 path-only compositions')


if __name__ == '__main__':
    verify()

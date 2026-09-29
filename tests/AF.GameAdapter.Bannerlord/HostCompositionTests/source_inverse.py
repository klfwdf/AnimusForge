"""Strict J02 host/composition inverse against the pre-extraction source."""
from pathlib import Path
import importlib.util
import subprocess

ROOT = Path(__file__).resolve().parents[3]
BASELINE = '3706e87dfc4be2cf150b9f45f029e7f105a28fd3'
DEST = ROOT / 'src/AF.GameAdapter.Bannerlord/Composition'
MOVED = ('CampaignComposition.cs', 'CampaignModelComposition.cs',
         'ModuleFrameworkRuntime.cs', 'TeamModuleRegistration.cs', 'TeamModuleServices.cs')
spec = importlib.util.spec_from_file_location('host_declaration', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extractor = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extractor)
declaration = extractor.declaration


def old(path):
    return subprocess.check_output(['git', 'show', BASELINE + ':' + path], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')


def read(path):
    return path.read_text(encoding='utf-8-sig').replace('\r\n', '\n')


# Post-J02 reviewed SubModule edits, in file order: SceneActions integration (pre-ada9894a)
# is part of the J02 baseline chain already; ada9894a adds Vengeance and IntegratedModuleHost
# lifecycle calls (Initialize/Start, mission inject, register inside the try before
# CaptureOwners, shutdown ordering, dialogue presentation install). Whitespace is verbatim.
SUBMODULE_LIFECYCLE_EDITS = [
    ('\t\tSceneActionsIntegrationBoundary.InitializeRuntime();\n',
     '\t\t\tSceneActionsIntegrationBoundary.InitializeRuntime();\n\t\t\tVengeanceRuntimeBridge.Initialize();\n\t\t\tIntegratedModuleHost.Start();\n'),
    ('\tpublic override void OnMissionBehaviorInitialize(Mission mission)\n\t{\n\t\tbase.OnMissionBehaviorInitialize(mission);\n\t\tSceneActionsIntegrationBoundary.VerifyMissionInitialization(mission);\n\t}',
     'public override void OnMissionBehaviorInitialize(Mission mission)\n\t\t{\n\t\t\tbase.OnMissionBehaviorInitialize(mission);\n\t\t\tVengeanceRuntimeBridge.TryInjectMission(mission);\n\t\t\tSceneActionsIntegrationBoundary.VerifyMissionInitialization(mission);\n\t\t}'),
    ('\t{\n\t\tRemoveMapButtonLayer();\n\t\tAfCampaignRuntimeLifecycle.Stop();\n\t\tModuleFrameworkRuntime.Shutdown();\n\t\tSceneActionsIntegrationBoundary.ShutdownRuntime();\n',
     '\t{\n\t\tIntegratedModuleHost.Shutdown();\n\t\tRemoveMapButtonLayer();\n\t\tAfCampaignRuntimeLifecycle.Stop();\n\t\tModuleFrameworkRuntime.Shutdown();\n\t\t\tSceneActionsIntegrationBoundary.ShutdownRuntime();\n\t\t\tVengeanceRuntimeBridge.Shutdown();\n'),
    ('\t\tStartupPatchComposition.Register();\n\t}',
     '\t\tStartupPatchComposition.Register();\n\t\tIntegratedModuleHost.InstallDialoguePresentation();\n\t}'),
    ('\t\t\tModuleFrameworkRuntime.RegisterCampaign(starterObject);\n',
     '\t\t\t\tModuleFrameworkRuntime.RegisterCampaign(starterObject);\n\t\t\t\tVengeanceRuntimeBridge.RegisterCampaign(starterObject);\n\t\t\t\tIntegratedModuleHost.RegisterCampaign(starterObject);\n'),
]


# ada9894a CivilWar (reviewed): one campaign behavior registered right after MyBehavior and
# one team module port; nothing else in the moved compositions may change.
MOVED_COMPOSITION_EDITS = {
    'CampaignComposition.cs': [(
        '            campaignGameStarter.AddBehavior(new MyBehavior());\n',
        '            campaignGameStarter.AddBehavior(new MyBehavior());\n            campaignGameStarter.AddBehavior(new CivilWarCampaignBehavior());\n')],
    'TeamModuleServices.cs': [(
        '    internal static ISiegeModulePort Siege { get; } = new SiegeModuleAdapter();\n',
        '    internal static ISiegeModulePort Siege { get; } = new SiegeModuleAdapter();\n\t    internal static ICivilWarModulePort CivilWar { get; } = new CivilWarModuleAdapter();\n')],
}


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

    # ada9894a (reviewed): dt is threaded to both phase lists and IntegratedModuleHost.Tick(dt)
    # is appended as the last phase of each, after VassalageBehavior and before WarStats.
    moved_app = app.replace('protected override void OnApplicationTick(float dt)',
        'internal static void Run(SubModule host, float dt)', 1).replace(
        'RunFastApplicationTickPhases();', 'RunFastApplicationTickPhases(host, dt);', 1).replace(
        'RunWatchedApplicationTickPhases();', 'RunWatchedApplicationTickPhases(host, dt);', 1).replace(
        'TickWarStatsMapButton(dt);', 'host.TickWarStatsMapButton(dt);', 1)
    moved_fast = fast.replace('private void RunFastApplicationTickPhases()',
        'private static void RunFastApplicationTickPhases(SubModule host, float dt)', 1).replace(
        'ProcessPendingInitialApiGuideNotice();', 'host.ProcessPendingInitialApiGuideNotice();', 1)
    moved_watched = watched.replace('private void RunWatchedApplicationTickPhases()',
        'private static void RunWatchedApplicationTickPhases(SubModule host, float dt)', 1).replace(
        '() => ProcessPendingInitialApiGuideNotice()', 'host.ProcessPendingInitialApiGuideNotice', 1)
    fast_last = '\t\tVassalageBehavior.Instance?.OnEngineTick();\n\t}'
    watched_last = '\t\t\tRunWatchedTickPhase("SubModule.VassalageBehavior.OnEngineTick", () => VassalageBehavior.Instance?.OnEngineTick());\n\t}'
    assert moved_fast.count(fast_last) == 1 and moved_watched.count(watched_last) == 1, 'Tick tail anchor changed'
    moved_fast = moved_fast.replace(fast_last, fast_last[:-2] + '\t\tIntegratedModuleHost.Tick(dt);\n\t}', 1)
    moved_watched = moved_watched.replace(watched_last, watched_last[:-2]
        + '\t\t\tRunWatchedTickPhase("SubModule.IntegratedModuleHost.Tick", () => IntegratedModuleHost.Tick(dt));\n\t}', 1)
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
    # Later reviewed additive lifecycle hooks (not part of J02). Each must apply exactly once.
    for before, after in SUBMODULE_LIFECYCLE_EDITS:
        assert expected.count(before) == 1, 'SubModule lifecycle anchor changed: ' + before.strip()[:60]
        expected = expected.replace(before, after, 1)
    assert current == expected, 'SubModule differs beyond reviewed host extraction'
    return prior


def verify():
    restore_submodule(read(ROOT / 'SubModule.cs'))
    for name in MOVED:
        assert not (ROOT / 'Refactor/Modules' / name).exists(), 'Old composition source still exists: ' + name
        expected = old('Refactor/Modules/' + name)
        for before, after in MOVED_COMPOSITION_EDITS.get(name, ()):
            assert expected.count(before) == 1, 'Composition anchor changed: ' + name
            expected = expected.replace(before, after, 1)
        assert read(DEST / name) == expected, 'Moved composition changed: ' + name
    print('PASS J02 full-file SubModule inverse + exact Startup/Tick bodies + 5 path-only compositions')


if __name__ == '__main__':
    verify()

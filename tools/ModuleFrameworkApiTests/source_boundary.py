"""Exact inverse for the public snapshot extraction; this is a source guard, not gameplay proof."""
from pathlib import Path
import importlib.util
import subprocess

ROOT = Path(__file__).resolve().parents[2]
BASELINE = '955a6be314840be8d20f1320d3c7f23c7a93fe77'
J02_BASELINE = '60072f0741114ea1c85a021ad4ad510d837b5c10'
FOUNDATION_OWNER = ROOT/'src/AF.Foundation.Runtime/ModuleDirectory/ModuleDirectoryLifecycleOwner.cs'
FOUNDATION_DIRECTORY = ROOT/'src/AF.Foundation.Runtime/ModuleDirectory'
spec=importlib.util.spec_from_file_location('snapshot_decl',ROOT/'tools/ChannelCutoverBoundaryTests/run.py')
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
declaration=m.declaration

def old(path):
    return subprocess.check_output(['git','show',BASELINE+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')

def restore_j02_runtime(current):
    prior = subprocess.check_output(['git','show',J02_BASELINE+':Refactor/Modules/ModuleFrameworkRuntime.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
    owner = FOUNDATION_OWNER.read_text(encoding='utf-8-sig')
    fields = prior[prior.index('    private static readonly object Sync'):prior.index('    internal static bool Initialize(')]
    old_initialize = declaration(prior,'internal static bool Initialize(out string reasonCode)')
    owner_initialize = old_initialize.replace('Initialize(out string reasonCode)', 'Initialize(Func<InternalModuleDirectory> createDirectory, out string reasonCode)').replace('TeamModuleRegistration.CreateDirectory()', 'createDirectory()')
    old_shutdown = declaration(prior,'internal static void Shutdown()')
    old_capture = declaration(prior,'internal static ModuleFrameworkSnapshot CaptureSnapshot()')
    expected_owner = ('using System;\nusing System.Collections.Generic;\n\nnamespace AnimusForge.Refactor.Modules;\n\n'
        '// Owns only the bounded internal directory lifecycle; not a second gameplay host.\n'
        'internal static class ModuleDirectoryLifecycleOwner\n{\n' + fields
        + '    ' + owner_initialize + '\n\n    ' + old_shutdown + '\n\n    ' + old_capture + '\n}\n')
    assert owner == expected_owner, 'Foundation lifecycle differs from reviewed old state/method bodies'
    facade_initialize = ('internal static bool Initialize(out string reasonCode)\n    {\n'
        '        return ModuleDirectoryLifecycleOwner.Initialize(TeamModuleRegistration.CreateDirectory, out reasonCode);\n    }')
    facade_shutdown = ('internal static void Shutdown()\n    {\n        ModuleDirectoryLifecycleOwner.Shutdown();\n    }')
    facade_capture = ('internal static ModuleFrameworkSnapshot CaptureSnapshot()\n    {\n'
        '        return ModuleDirectoryLifecycleOwner.CaptureSnapshot();\n    }')
    expected_runtime = prior.replace('using System.Collections.Generic;\n','').replace(fields,'')
    expected_runtime = expected_runtime.replace(old_initialize,facade_initialize).replace(old_shutdown,facade_shutdown).replace(old_capture,facade_capture)
    assert current == expected_runtime, 'Runtime retains or changes unreviewed lifecycle state/behavior'
    assert all(term not in owner for term in ('TeamModuleRegistration', 'IGameStarter', 'CampaignComposition'))
    assert all(term not in current for term in ('_directory', '_state', '_reason', 'lock (Sync)'))
    for name in ('InternalModuleDirectory.cs', 'ModuleFrameworkSnapshot.cs'):
        original = subprocess.check_output(['git','show',J02_BASELINE+':Refactor/Modules/'+name],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
        assert (FOUNDATION_DIRECTORY/name).read_text(encoding='utf-8-sig').replace('\r\n','\n') == original, 'Relocated directory/snapshot text changed: '+name
    return prior

def restore_runtime(current):
    current=restore_j02_runtime(current)
    prior=old('Refactor/Modules/ModuleFrameworkRuntime.cs')
    mapper=declaration(prior,'private static AfModuleCapabilityState MapStatus(')
    projection=(ROOT/'src/modules/AF.Module.PublicApi/Internal/AfV1SnapshotProjection.cs').read_text(encoding='utf-8-sig')
    assert declaration(projection,'private static AfModuleCapabilityState MapStatus(')==mapper, 'V1 capability mapping changed'
    original=declaration(prior,'internal static AfFrameworkSnapshot GetSnapshot(')
    capture=declaration(current,'internal static ModuleFrameworkSnapshot CaptureSnapshot()')
    restored=capture.replace('internal static ModuleFrameworkSnapshot CaptureSnapshot()', 'internal static AfFrameworkSnapshot GetSnapshot(IReadOnlyList<AfCapabilityInfo> publicCapabilities)')
    restored=restored.replace('new List<ModuleBindingSnapshot>()','new List<AfModuleInfo>()').replace('new List<InternalCapabilityStatus>()','new List<AfModuleCapabilityInfo>()')
    restored=restored.replace('new InternalCapabilityStatus(definition.Id, module.Definition.Id, definition.ContractVersion,\n                                definition.ContractVersion, InternalCapabilityState.ModuleUnavailable, "framework.stopped")',
        'new AfModuleCapabilityInfo(definition.Id, definition.ContractVersion,\n                                AfModuleCapabilityState.Unavailable, "framework.stopped")')
    restored=restored.replace('capabilities.Add(status);','capabilities.Add(new AfModuleCapabilityInfo(definition.Id, definition.ContractVersion,\n                            MapStatus(status.State), status.ReasonCode));')
    restored=restored.replace('new ModuleBindingSnapshot(module.Definition, capabilities)','new AfModuleInfo(module.Definition.Id, module.Definition.ContractVersion, capabilities)')
    restored=restored.replace('new ModuleFrameworkSnapshot(_state, _reason, modules)','new AfFrameworkSnapshot(_state, _reason, publicCapabilities, modules)').replace('ModuleFrameworkLifecycleState','AfFrameworkState')
    assert restored==original, 'Snapshot capture differs beyond typed boundary extraction'
    expected=prior.replace(original,capture).replace('\n    '+mapper+'\n','').replace('using AnimusForge.Api.V1;\n','').replace('AfFrameworkState','ModuleFrameworkLifecycleState')
    assert current==expected, 'Unreviewed root lifecycle/registration delta'
    api=(ROOT/'src/modules/AF.Module.PublicApi/V1/AfApi.cs').read_text(encoding='utf-8-sig')
    api_spec=importlib.util.spec_from_file_location('native_api_inverse',ROOT/'tools/NativeModuleSubmissionTests/source_boundary.py')
    api_inverse=importlib.util.module_from_spec(api_spec);api_spec.loader.exec_module(api_inverse)
    # This inverse checks AfApi's original public shape; Native's historical dependency
    # hashes are independently superseded by current J14 core-consumer tests.
    api=api_inverse.restore('Api/V1/AfApi.cs',api,verify_dependencies=False)
    expected_api=old('Api/V1/AfApi.cs').replace('using AnimusForge.Refactor.Modules;', 'using AnimusForge.Refactor.Modules;\nusing AnimusForge.Api.Internal;').replace('return ModuleFrameworkRuntime.GetSnapshot(Capabilities);', 'return AfV1SnapshotProjection.Create(ModuleFrameworkRuntime.CaptureSnapshot(), Capabilities);')
    assert api==expected_api, 'Unreviewed public API change'
    return prior

def verify():
    assert FOUNDATION_OWNER.is_file(), 'Foundation directory lifecycle owner missing'
    owner = FOUNDATION_OWNER.read_text(encoding='utf-8-sig')
    assert 'TeamModuleRegistration' not in owner and 'IGameStarter' not in owner and 'CampaignComposition' not in owner, 'Foundation owner depends on team/game composition'
    restore_runtime((ROOT/'src/AF.GameAdapter.Bannerlord/Composition/ModuleFrameworkRuntime.cs').read_text(encoding='utf-8-sig'))
    for path in list((ROOT/'Refactor/Modules').glob('*.cs')) + list(FOUNDATION_DIRECTORY.glob('*.cs')) + list((ROOT/'src/AF.GameAdapter.Bannerlord/Composition').glob('*.cs')):
        assert 'using AnimusForge.Api' not in path.read_text(encoding='utf-8-sig'), 'Internal module depends on public API: '+str(path)
    print('PASS snapshot source inverse / unchanged V1 capability mapping / no Modules -> API import')

if __name__=='__main__': verify()

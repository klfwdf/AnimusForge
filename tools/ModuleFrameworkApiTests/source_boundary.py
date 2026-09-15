"""Exact inverse for the public snapshot extraction; this is a source guard, not gameplay proof."""
from pathlib import Path
import importlib.util
import subprocess

ROOT = Path(__file__).resolve().parents[2]
BASELINE = '955a6be314840be8d20f1320d3c7f23c7a93fe77'
spec=importlib.util.spec_from_file_location('snapshot_decl',ROOT/'tools/ChannelCutoverBoundaryTests/run.py')
m=importlib.util.module_from_spec(spec);spec.loader.exec_module(m)
declaration=m.declaration

def old(path):
    return subprocess.check_output(['git','show',BASELINE+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')

def restore_runtime(current):
    prior=old('Refactor/Modules/ModuleFrameworkRuntime.cs')
    mapper=declaration(prior,'private static AfModuleCapabilityState MapStatus(')
    projection=(ROOT/'Api/Internal/AfV1SnapshotProjection.cs').read_text(encoding='utf-8-sig')
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
    api=(ROOT/'Api/V1/AfApi.cs').read_text(encoding='utf-8-sig')
    api_spec=importlib.util.spec_from_file_location('native_api_inverse',ROOT/'tools/NativeModuleSubmissionTests/source_boundary.py')
    api_inverse=importlib.util.module_from_spec(api_spec);api_spec.loader.exec_module(api_inverse)
    api=api_inverse.restore('Api/V1/AfApi.cs',api)
    expected_api=old('Api/V1/AfApi.cs').replace('using AnimusForge.Refactor.Modules;', 'using AnimusForge.Refactor.Modules;\nusing AnimusForge.Api.Internal;').replace('return ModuleFrameworkRuntime.GetSnapshot(Capabilities);', 'return AfV1SnapshotProjection.Create(ModuleFrameworkRuntime.CaptureSnapshot(), Capabilities);')
    assert api==expected_api, 'Unreviewed public API change'
    return prior

def verify():
    restore_runtime((ROOT/'Refactor/Modules/ModuleFrameworkRuntime.cs').read_text(encoding='utf-8-sig'))
    for path in (ROOT/'Refactor/Modules').glob('*.cs'):
        assert 'using AnimusForge.Api' not in path.read_text(encoding='utf-8-sig'), 'Internal module depends on public API: '+str(path)
    print('PASS snapshot source inverse / unchanged V1 capability mapping / no Modules -> API import')

if __name__=='__main__': verify()

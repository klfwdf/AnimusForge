"""Precisely undo separately tested B1 deltas before older owner parity assertions.
This is a source-review adapter, NOT runtime/game acceptance or a B1 completion gate.
"""
import hashlib
import importlib.util
import json
from pathlib import Path
import sys as _relocation_sys
_relocation_sys.path.insert(0, str(Path(__file__).resolve().parents[4] / "tests"))
from output_isolation import current_source_path
from af2_terminal_migration_review import historical_source
from remote_feature_delta import restore_remote_feature_delta
import subprocess

ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent




def _restore_round2_current_paths(path, source):
    """Undo only reviewed current-file locator/import edits; retain original review hashes."""
    reviewed = {'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run.py': {'before_sha256': 'f4d3d5e365ba5f648cb1302f322d7ce7360fff2a227e16ff9abc5fbc53956317', 'edits': [('', 'import sys as _relocation_sys\n_relocation_sys.path.insert(0, str(Path(__file__).resolve().parents[4] / "tests"))\nfrom output_isolation import current_source_path\n'), ('boundary_path = ROOT / "MyBehavior.MemorySummaryMainThread.cs"\n', 'boundary_path = current_source_path(ROOT, "MyBehavior.MemorySummaryMainThread.cs")\n'), ('process = (ROOT / "MyBehavior.cs").read_text(encoding="utf-8-sig")\n', 'process = (current_source_path(ROOT, "MyBehavior.cs")).read_text(encoding="utf-8-sig")\n')]}, 'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_business.py': {'before_sha256': 'c2f7ca8b45f92f4f748f6c240065031e172da8665786750bb84fe3f190491332', 'edits': [('from output_isolation import new_run_root\n', 'from output_isolation import new_run_root, current_source_path\n'), ('        planning = subprocess.check_output(["git","show","155f1b7a:MyBehavior.MemorySummaryPlanning.cs"],cwd=ROOT).decode("utf-8-sig").replace("\\r\\n","\\n") if args.run_owner_baseline else (ROOT / "MyBehavior.MemorySummaryPlanning.cs").read_text(encoding="utf-8-sig")\n', '        planning = subprocess.check_output(["git","show","155f1b7a:MyBehavior.MemorySummaryPlanning.cs"],cwd=ROOT).decode("utf-8-sig").replace("\\r\\n","\\n") if args.run_owner_baseline else (current_source_path(ROOT, "MyBehavior.MemorySummaryPlanning.cs")).read_text(encoding="utf-8-sig")\n'), ('    if not args.original and (ROOT / "MyBehavior.MemoryMaintenanceBudget.cs").is_file():\n', '    if not args.original and (current_source_path(ROOT, "MyBehavior.MemoryMaintenanceBudget.cs")).is_file():\n'), ('            files[target] = (ROOT / relative).read_text(encoding="utf-8-sig")\n', '            files[target] = (current_source_path(ROOT, relative)).read_text(encoding="utf-8-sig")\n'), ("            files[Path(relative).name]=(ROOT/relative).read_text(encoding='utf-8-sig')\n", "            files[Path(relative).name]=(current_source_path(ROOT, relative)).read_text(encoding='utf-8-sig')\n")]}, 'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_captured.py': {'before_sha256': '402fe81edcb425c0ebd13a34a301171900e214decc8aaff6f573ee809c094fbd', 'edits': [('from output_isolation import new_run_root\n', 'from output_isolation import new_run_root, current_source_path\n'), ("    def read(path):return subprocess.check_output(['git','show',a.source_baseline+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\\r\\n','\\n') if a.source_baseline else (ROOT/path).read_text(encoding='utf-8-sig')\n", "    def read(path):return subprocess.check_output(['git','show',a.source_baseline+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\\r\\n','\\n') if a.source_baseline else (current_source_path(ROOT, path)).read_text(encoding='utf-8-sig')\n"), ("    planning=(ROOT/'MyBehavior.MemorySummaryPlanning.cs').read_text(encoding='utf-8-sig')\n", "    planning=(current_source_path(ROOT, 'MyBehavior.MemorySummaryPlanning.cs')).read_text(encoding='utf-8-sig')\n"), ("    recovery=(ROOT/'MyBehavior.MemoryRecovery.cs').read_text(encoding='utf-8-sig')\n", "    recovery=(current_source_path(ROOT, 'MyBehavior.MemoryRecovery.cs')).read_text(encoding='utf-8-sig')\n"), ('    files={\'Product.cs\':product,\'Input.cs\':capture,\'Boundary.cs\':(ROOT/\'MyBehavior.MemorySummaryMainThread.cs\').read_text(encoding=\'utf-8-sig\'),\'Guard.cs\':(ROOT/\'src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs\').read_text(encoding=\'utf-8-sig\'),\'Program.cs\':(HERE/\'CapturedHarness.cs.txt\').read_text(encoding=\'utf-8-sig\'),\'Proof.csproj\':\'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>\'+escape(str(deps))+\'</HintPath></Reference></ItemGroup></Project>\',\'NuGet.Config\':\'<configuration><packageSources><clear/></packageSources></configuration>\'}\n', '    files={\'Product.cs\':product,\'Input.cs\':capture,\'Boundary.cs\':(current_source_path(ROOT, \'MyBehavior.MemorySummaryMainThread.cs\')).read_text(encoding=\'utf-8-sig\'),\'Guard.cs\':(ROOT/\'src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs\').read_text(encoding=\'utf-8-sig\'),\'Program.cs\':(HERE/\'CapturedHarness.cs.txt\').read_text(encoding=\'utf-8-sig\'),\'Proof.csproj\':\'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>\'+escape(str(deps))+\'</HintPath></Reference></ItemGroup></Project>\',\'NuGet.Config\':\'<configuration><packageSources><clear/></packageSources></configuration>\'}\n'), ("            files[Path(relative).name]=(ROOT/relative).read_text(encoding='utf-8-sig')\n", "            files[Path(relative).name]=(current_source_path(ROOT, relative)).read_text(encoding='utf-8-sig')\n")]}, 'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_commit_writers.py': {'before_sha256': '6fb04aac063a82bb38453664236d699cd7489dc7ec6c941d6d71141fac0fe6fb', 'edits': [('from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment\n', 'from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment, current_source_path\n'), ("                if 'signature' not in entry and sha((ROOT/entry['file']).read_text(encoding='utf-8-sig'))!=entry['sha256']:return None\n", "                if 'signature' not in entry and sha((current_source_path(ROOT, entry['file'])).read_text(encoding='utf-8-sig'))!=entry['sha256']:return None\n"), ("        data=(ROOT/name).read_text(encoding='utf-8-sig');inventory.append(dict(file=name,sha256=sha(data)));return data\n", "        data=(current_source_path(ROOT, name)).read_text(encoding='utf-8-sig');inventory.append(dict(file=name,sha256=sha(data)));return data\n")]}, 'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_materials.py': {'before_sha256': '20193ecfa160ca982b7e39e2cbd173e64c4e6f133f6c68a62e5aca0242a21210', 'edits': [('from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment\n', 'from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment, current_source_path\n'), ("    def read(name):return (ROOT/name).read_text(encoding='utf-8-sig')\n", "    def read(name):return (current_source_path(ROOT, name)).read_text(encoding='utf-8-sig')\n")]}, 'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_planning.py': {'before_sha256': '851e20ca775727725bf33e525a096ec94f8562ac7c3f426c779e80eb66f4029c', 'edits': [('from output_isolation import new_run_root\n', 'from output_isolation import new_run_root, current_source_path\n'), (" source=(ROOT/'MyBehavior.cs').read_text(encoding='utf-8-sig');manifest=[];blocks=[]\n", " source=(current_source_path(ROOT, 'MyBehavior.cs')).read_text(encoding='utf-8-sig');manifest=[];blocks=[]\n"), (" recovery=(ROOT/'MyBehavior.MemoryRecovery.cs').read_text(encoding='utf-8-sig')\n", " recovery=(current_source_path(ROOT, 'MyBehavior.MemoryRecovery.cs')).read_text(encoding='utf-8-sig')\n"), (" inp=(ROOT/'MyBehavior.MemorySummaryInput.cs').read_text(encoding='utf-8-sig');body=ex.declaration(inp,'private static string ComputeMemorySummaryFingerprint(');blocks.append(body)\n", " inp=(current_source_path(ROOT, 'MyBehavior.MemorySummaryInput.cs')).read_text(encoding='utf-8-sig');body=ex.declaration(inp,'private static string ComputeMemorySummaryFingerprint(');blocks.append(body)\n"), (" planning=(ROOT/'MyBehavior.MemorySummaryPlanning.cs').read_text(encoding='utf-8-sig');production_planning=planning\n", " planning=(current_source_path(ROOT, 'MyBehavior.MemorySummaryPlanning.cs')).read_text(encoding='utf-8-sig');production_planning=planning\n"), (' files={\'Product.cs\':prefix+\'\\n\'+\'\\n\'.join(blocks)+\'\\n}}\',\'Planning.cs\':planning,\'Boundary.cs\':(ROOT/\'MyBehavior.MemorySummaryMainThread.cs\').read_text(encoding=\'utf-8-sig\'),\'Guard.cs\':(ROOT/\'src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs\').read_text(encoding=\'utf-8-sig\'),\'Program.cs\':(HERE/\'PlanningHarness.cs.txt\').read_text(encoding=\'utf-8-sig\'),\'Proof.csproj\':\'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649;CS0162</NoWarn></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>\'+escape(str(deps))+\'</HintPath></Reference></ItemGroup></Project>\',\'NuGet.Config\':\'<configuration><packageSources><clear/></packageSources></configuration>\'}\n', ' files={\'Product.cs\':prefix+\'\\n\'+\'\\n\'.join(blocks)+\'\\n}}\',\'Planning.cs\':planning,\'Boundary.cs\':(current_source_path(ROOT, \'MyBehavior.MemorySummaryMainThread.cs\')).read_text(encoding=\'utf-8-sig\'),\'Guard.cs\':(ROOT/\'src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs\').read_text(encoding=\'utf-8-sig\'),\'Program.cs\':(HERE/\'PlanningHarness.cs.txt\').read_text(encoding=\'utf-8-sig\'),\'Proof.csproj\':\'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649;CS0162</NoWarn></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>\'+escape(str(deps))+\'</HintPath></Reference></ItemGroup></Project>\',\'NuGet.Config\':\'<configuration><packageSources><clear/></packageSources></configuration>\'}\n'), ("         files[Path(relative).name]=(ROOT/relative).read_text(encoding='utf-8-sig')\n", "         files[Path(relative).name]=(current_source_path(ROOT, relative)).read_text(encoding='utf-8-sig')\n")]}, 'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_sealing.py': {'before_sha256': '455a5765dcf3997986f79ab089d89a5cef027317f1ab5c45bcf5fe90ba575679', 'edits': [('from output_isolation import new_run_root\n', 'from output_isolation import new_run_root, current_source_path\n'), (" def read(path):return subprocess.check_output(['git','show',baseline+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\\r\\n','\\n') if baseline and not path.startswith('tools/') else (ROOT/path).read_text(encoding='utf-8-sig')\n source=read('MyBehavior.cs');manifest=[];snippets=[];sealing_path=ROOT/'MyBehavior.MemorySealing.cs';new_sealing=not a.original and sealing_path.exists()\n", " def read(path):return subprocess.check_output(['git','show',baseline+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\\r\\n','\\n') if baseline and not path.startswith('tools/') else (current_source_path(ROOT, path)).read_text(encoding='utf-8-sig')\n source=read('MyBehavior.cs');manifest=[];snippets=[];sealing_path=current_source_path(ROOT, 'MyBehavior.MemorySealing.cs');new_sealing=not a.original and sealing_path.exists()\n"), ("         files[Path(relative).name]=(ROOT/relative).read_text(encoding='utf-8-sig')\n", "         files[Path(relative).name]=(current_source_path(ROOT, relative)).read_text(encoding='utf-8-sig')\n")]}, 'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_terminal.py': {'before_sha256': 'a835a0b237ebe5aa8dc8f43bd552664ac2dd9a5ebe930d1a72cb5def8f562945', 'edits': [('from output_isolation import new_run_root\n', 'from output_isolation import new_run_root, current_source_path\n'), ("        data=(ROOT/name).read_text(encoding='utf-8-sig');manifest.append(dict(file=name,sha256=hashlib.sha256(data.encode()).hexdigest()));return data\n", "        data=(current_source_path(ROOT, name)).read_text(encoding='utf-8-sig');manifest.append(dict(file=name,sha256=hashlib.sha256(data.encode()).hexdigest()));return data\n"), ('        if (ROOT/extra).exists():files[Path(extra).name]=read(extra)\n', '        if (current_source_path(ROOT, extra)).exists():files[Path(extra).name]=read(extra)\n'), ("            files[Path(relative).name]=(ROOT/relative).read_text(encoding='utf-8-sig')\n", "            files[Path(relative).name]=(current_source_path(ROOT, relative)).read_text(encoding='utf-8-sig')\n")]}, 'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_writers.py': {'before_sha256': '804a5ebececf1cf712a75fdab4f52a81485f50d0f0e37e6c28b5842981e40fa6', 'edits': [('', 'import sys as _relocation_sys\n_relocation_sys.path.insert(0, str(Path(__file__).resolve().parents[4] / "tests"))\nfrom output_isolation import current_source_path\n'), ('        text = (ROOT / name).read_text(encoding="utf-8-sig")\n', '        text = (current_source_path(ROOT, name)).read_text(encoding="utf-8-sig")\n'), ("            files[Path(relative).name]=(ROOT/relative).read_text(encoding='utf-8-sig')\n", "            files[Path(relative).name]=(current_source_path(ROOT, relative)).read_text(encoding='utf-8-sig')\n")]}}
    packet = reviewed.get(str(path).replace(chr(92), "/"))
    if packet is None:
        return source
    if hashlib.sha256(source.encode()).hexdigest() == packet["before_sha256"]:
        return source
    for before, after in reversed(packet["edits"]):
        assert source.count(after) == 1, "Unreviewed B1 evidence round2 source locator: " + str(path)
        source = source.replace(after, before, 1)
    assert hashlib.sha256(source.encode()).hexdigest() == packet["before_sha256"], "Unreviewed B1 evidence round2 surrounding runner: " + str(path)
    return source

def _sha256(text):
    return hashlib.sha256(text.encode()).hexdigest()


def _restore_j02_guard_source_path(path, text):
    text = _restore_round2_current_paths(path, text)
    path = str(path).replace('\\', '/')
    if path in {
        'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run.py',
        'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_business.py',
        'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_captured.py',
        'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_planning.py',
        'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_sealing.py',
        'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_terminal.py',
        'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/run_writers.py',
    }:
        new = 'src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs'
        assert text.count(new) == 1, 'B1 runner guard path drift: ' + path
        text = text.replace(new, 'SaveRuntimeGuard.cs', 1)
    from f3_migration_projection import current_prior_memory_run_producer
    prior = current_prior_memory_run_producer()
    return prior.chain.apply('B1_AFTER_ORIGINAL_LOCATOR',path,text) if prior is not None and prior.chain is not None else text


def _is_reviewed_deleted(item):
    if item.get('reviewed') is False:
        return False
    return item.get('status') != 'UNREVIEWED_WIP'


from contextlib import contextmanager
from contextvars import ContextVar
_b1_review_input = ContextVar("bound_b1_review_input", default=None)


# Fixed original source-review controls only, never current runtime inputs.
_dependency_target = ContextVar('b1_dependency_target', default=None)

def _positive_fixture_inputs():
    from af2_terminal_migration_review import j17_packet
    rows=j17_packet()['independentLayers']['F3']['originalPositiveInputs']
    paths={'MyBehavior.MemorySummaryInput.cs','MyBehavior.MemorySealing.cs','MyBehavior.MemorySummaryPlanning.cs'}
    assert set(rows)==paths, 'Unreviewed original positive fixture set'
    result={}
    for path,row in rows.items():
        physical=current_source_path(ROOT,path)
        assert hashlib.sha256(physical.read_bytes()).hexdigest()==row['physicalRawSha256'], 'Original positive physical drift: '+path
        value=physical.read_text(encoding='utf-8-sig').replace('\r\n','\n')
        assert _sha256(value)==row['sourceSha256'], 'Original positive source drift: '+path
        for e in row['edits']:
            assert e['symbols'] and e['after'] and e['after']!=value and value.count(e['after'])==1, 'Original positive unique context: '+path
            value=value.replace(e['after'],e['before'],1)
        assert _sha256(value)==row['targetSha256'], 'Original positive whole drift: '+path
        result[path]=value
    return result

@contextmanager
def _legacy_dependency_target(bound,target):
    targets={
        'MyBehavior.MemorySummaryInput.cs':('memory_run_inverse','restore'),
        'MyBehavior.MemorySummaryPlanning.cs':('memory_run_inverse','restore'),
        'src/modules/AF.Module.Memory/Summary/MemorySourceFingerprintWriter.cs':('memory_writer_inverse','restore_writer'),
        'MyBehavior.MemorySummaryMainThread.cs':('b1_game_lifetime_inverse','restore'),
    }
    assert target in targets and _b1_review_input.get() is bound, 'Unreviewed B1 mutation target/context'
    assert _dependency_target.get() is None, 'B1 dependency mutation reentry'
    # Original positive executes every unmutated Run/writer/lifetime/evidence
    # assertion first. Mutations never enter those upstream producers.
    bound.verify(bound.source)
    from importlib.machinery import SourceFileLoader
    from unittest.mock import patch
    load=SourceFileLoader.exec_module
    token=_dependency_target.set(target)
    hit=[]
    expected_module,expected_function=targets[target]
    def execute(loader,module):
        load(loader,module)
        if module.__name__!=expected_module:return
        original=getattr(module,expected_function)
        def target_output(path,source,*args,**kwargs):
            output=original(path,source,*args,**kwargs)
            if path==target:
                assert not hit, 'Duplicate B1 target mutation'
                hit.append({'path':path,'originalOutputSha256':_sha256(output)})
                return output+'\n// unreviewed dependency\n'
            return output
        setattr(module,expected_function,target_output)
    try:
        with patch.object(SourceFileLoader,'exec_module',execute):yield
    finally:
        _dependency_target.reset(token)
        if not hit:raise AssertionError('B1 dependency target NOT_REACHED: '+target)

class _BoundFiniteReviewInput:
    def __init__(self, source):self.source=source
    def verify(self, source):
        assert _finite_review_input.get() is self, 'Finite target requires validated upstream context'
        return _verify_current_memory_source(source)
    @contextmanager
    def dependency_mutation(self,target):
        targets={'MyBehavior.MemorySealing.cs','MyBehavior.MemoryMaintenanceBudget.cs','MyBehavior.MemorySummaryInput.cs'}
        assert target in targets and _finite_review_input.get() is self, 'Unreviewed finite mutation target/context'
        assert _dependency_target.get() is None, 'Finite mutation reentry'
        self.verify(self.source)
        from unittest.mock import patch
        read=Path.read_text;physical=current_source_path(ROOT,target);hit=[]
        token=_dependency_target.set(target)
        def changed(path,*args,**kwargs):
            value=read(path,*args,**kwargs)
            if path==physical:
                assert not hit, 'Duplicate finite target mutation'
                hit.append(_sha256(value))
                return value+'\n// unreviewed dependency\n'
            return value
        try:
            with patch.object(Path,'read_text',changed):yield
        finally:
            _dependency_target.reset(token)
            if not hit:raise AssertionError('Finite dependency target NOT_REACHED: '+target)

_finite_review_input=ContextVar('finite_dependency_target',default=None)

@contextmanager
def bound_finite_review_input(source):
    from f3_migration_projection import projection_reads,restore
    assert _finite_review_input.get() is None, 'Finite review reentry'
    with projection_reads():
        canonical=restore('MyBehavior.cs',source)
        bound=_BoundFiniteReviewInput(canonical)
        token=_finite_review_input.set(bound)
        try:
            bound.verify(canonical)
            yield bound
        finally:_finite_review_input.reset(token)

class _BoundB1ReviewInput:
    def __init__(self, source, producer):
        self._source = source
        self._producer = producer

    @property
    def source(self):
        return self._source

    def fixture_input(self,path):
        assert _b1_review_input.get() is self and path in self._positive_inputs, 'Original positive requires bound context'
        return self._positive_inputs[path]

    def dependency_mutation(self,target):
        return _legacy_dependency_target(self,target)

    @property
    def upstream_stages(self):
        return tuple(dict(item) for item in self._producer.stages)

    def verify(self, source):
        assert _b1_review_input.get() is self, 'B1 target requires validated upstream context'
        return _verify_bound_b1_source('MyBehavior.cs', source)

@contextmanager
def bound_b1_review_input():
    """Validate immutable upstream first; only then inspect B1-layer mutations."""
    from f3_migration_projection import projection_reads, prior_memory_run_producer
    assert _b1_review_input.get() is None, 'Bound B1 review reentry'
    with projection_reads():
        positive_inputs = _positive_fixture_inputs()
        with prior_memory_run_producer() as producer:
            run_spec = importlib.util.spec_from_file_location('bound_memory_run_inverse', ROOT / 'tests/modules/AF.Module.Memory/MemorySummaryRunOwnerTests/source_parity.py')
            run_inverse = importlib.util.module_from_spec(run_spec); run_spec.loader.exec_module(run_inverse)
            source = (current_source_path(ROOT, 'MyBehavior.cs')).read_text(encoding='utf-8-sig')
            source = run_inverse.restore('MyBehavior.cs', source)
            persona_spec = importlib.util.spec_from_file_location('bound_persona_inverse', ROOT / 'tests/modules/AF.Module.Persona/HeroPersonaGenerationTests/source_parity.py')
            persona = importlib.util.module_from_spec(persona_spec); persona_spec.loader.exec_module(persona)
            source = persona.restore_historical(source, strict=False)
            producer.stages.append({'stage':'ORIGINAL_PERSONA','sha256':_sha256(source)})
            bound = _BoundB1ReviewInput(source, producer)
            bound._positive_inputs = positive_inputs
            token = _b1_review_input.set(bound)
            try:
                # Original positive B1 whole/evidence checks must pass before yield.
                bound.verify(source)
                yield bound
            finally:
                _b1_review_input.reset(token)

def _restore_memory_summary_source(path, source):
    if path != 'MyBehavior.cs':
        return restore_remote_feature_delta(path,source)
    with bound_b1_review_input() as bound:
        return bound.verify(source)

def _verify_bound_b1_source(path, source):
    assert _b1_review_input.get() is not None, 'B1 target requires validated upstream context'
    run_spec = importlib.util.spec_from_file_location('memory_run_inverse', ROOT / 'tests/modules/AF.Module.Memory/MemorySummaryRunOwnerTests/source_parity.py')
    run_inverse = importlib.util.module_from_spec(run_spec); run_spec.loader.exec_module(run_inverse)
    review = json.loads((HERE / 'source-review-b1.json').read_text(encoding='utf-8'))
    spec = importlib.util.spec_from_file_location('b1_declaration_extractor', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
    extractor = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(extractor)
    # The inverse is permitted only for the exact separately tested harness/runner.
    # It must not turn an arbitrary new assertion deletion into an old-owner PASS.
    assert review.get('testSourceHashNormalization') == 'utf8-no-bom-lf', 'Missing test source normalization'
    reviewed_deleted = [item for item in review.get('deletedDeclarations', []) if _is_reviewed_deleted(item)]
    labels = {label for item in review['declarations'] + reviewed_deleted for label in item.get('evidence') or []}
    for item in (review.get('unreviewedWip') or {}).get('declarations') or []:
        labels.update(item.get('evidence') or [])
    for item in review.get('deletedDeclarations', []):
        if not _is_reviewed_deleted(item):
            labels.update(item.get('evidence') or [])
    for label in labels:
        evidence = review['evidence'][label]
        for role in ('runner', 'harness'):
            text = _restore_j02_guard_source_path(evidence[role], (current_source_path(ROOT, evidence[role])).read_text(encoding='utf-8-sig'))
            text = run_inverse.restore(evidence[role], text)
            assert _sha256(text) == evidence['testSourceSha256'][role], 'Unreviewed B1 evidence source: ' + evidence[role]
        for dependency_path, expected in evidence.get('additionalTestSourceSha256', {}).items():
            text = _restore_j02_guard_source_path(dependency_path, (current_source_path(ROOT, dependency_path)).read_text(encoding='utf-8-sig'))
            text = run_inverse.restore(dependency_path, text)
            assert _sha256(text) == expected, 'Unreviewed B1 evidence dependency: ' + dependency_path
    # New runtime components are reviewed as whole input files, not silently
    # trusted because only the MyBehavior facade is inverse-transformed.
    for dependency_path, expected in review.get('productionDependencies', {}).items():
        text = run_inverse.restore(dependency_path, (current_source_path(ROOT, dependency_path)).read_text(encoding='utf-8-sig'))
        writer_spec = importlib.util.spec_from_file_location('memory_writer_inverse', ROOT / 'tests/modules/AF.Module.Memory/MemorySummaryBudgetTests/source_review.py')
        writer_inverse = importlib.util.module_from_spec(writer_spec); writer_spec.loader.exec_module(writer_inverse)
        text = writer_inverse.restore_writer(dependency_path, text)
        if dependency_path == 'MyBehavior.MemorySummaryMainThread.cs':
            life_spec = importlib.util.spec_from_file_location('b1_game_lifetime_inverse', ROOT / 'tests/AF.GameAdapter.Bannerlord/GameLifetimeTests/source_parity.py')
            life = importlib.util.module_from_spec(life_spec); life_spec.loader.exec_module(life)
            text = life.restore(dependency_path, text)
        assert _sha256(text) == expected, 'Unreviewed B1 production dependency: ' + dependency_path
    for removed_path in review.get('removedProductionFiles', []):
        assert not (ROOT / removed_path).exists(), 'Obsolete B1 production file restored: ' + removed_path
    unreviewed = []
    seen = set()

    def note(message):
        if message in seen:
            return
        seen.add(message)
        unreviewed.append(message)

    def current_text(item_path):
        if item_path == path:
            return source
        return (current_source_path(ROOT, item_path)).read_text(encoding='utf-8-sig').replace('\r\n', '\n')

    for item in (review.get('unreviewedWip') or {}).get('declarations') or []:
        signature = item['signature']
        text = current_text(item['path'])
        current = extractor.declaration(text, signature)
        if _sha256(current) != item['currentSha256']:
            note('Unreviewed B1 WIP declaration hash drifted: ' + signature)
        note('Unreviewed B1 declaration: ' + signature)
    for item in review['declarations']:
        current = extractor.declaration(source, item['signature'])
        if _sha256(current) != item['sha256']:
            note('Unreviewed B1 declaration: ' + item['signature'])
    for item in review.get('deletedDeclarations', []):
        if _is_reviewed_deleted(item):
            continue
        if item['signature'] in source:
            note('Unreviewed B1 deleted declaration still present: ' + item['signature'])
        note('Unreviewed B1 deleted declaration: ' + item['signature'])
    if unreviewed:
        raise AssertionError('Unreviewed B1 declarations:\n' + '\n'.join(unreviewed))
    baseline = subprocess.check_output(['git', 'show', review['baseline'] + ':' + path], cwd=ROOT).decode('utf-8-sig').replace('\r\n', '\n')
    restored = source
    for item in review['declarations']:
        assert item['path'] == path and item['evidence'], 'B1 review entry lacks path or scoped evidence'
        current = extractor.declaration(restored, item['signature'])
        prior = extractor.declaration(baseline, item['baselineSignature'])
        assert _sha256(current) == item['sha256'], 'Unreviewed B1 declaration: ' + item['signature']
        assert _sha256(prior) == item['baselineSha256'], 'B1 baseline declaration changed: ' + item['baselineSignature']
        assert restored.count(current) == 1, 'Ambiguous B1 declaration replacement'
        restored = restored.replace(current, prior, 1)
    for item in reviewed_deleted:
        assert item['path'] == path and item['evidence'], 'Deleted B1 declaration lacks scoped evidence'
        assert item['signature'] not in restored, 'Deleted B1 declaration unexpectedly restored in production'
        prior = extractor.declaration(baseline, item['baselineSignature'])
        assert _sha256(prior) == item['baselineSha256'], 'Deleted B1 body hash changed'
        next_prior = extractor.declaration(baseline, item['nextSignature'])
        next_current = extractor.declaration(restored, item['nextSignature'])
        assert _sha256(next_current) == item['nextDeclarationSha256'], 'Deleted B1 neighbor anchor changed'
        start = baseline.rfind('\n', 0, baseline.index(prior)) + 1
        end = baseline.rfind('\n', 0, baseline.index(next_prior)) + 1
        deleted_span = baseline[start:end]
        assert _sha256(deleted_span) == item['baselineSpanSha256'], 'Deleted B1 source span changed'
        anchor = '\t' + item['nextSignature']
        assert restored.count(anchor) == 1, 'Deleted B1 declaration has ambiguous neighbor anchor'
        restored = restored.replace(anchor, deleted_span + anchor, 1)
    for item in review.get('addedSourceSpans', []):
        span = item['text']
        assert item['path'] == path and item['evidence'], 'Added B1 span lacks scoped evidence'
        assert _sha256(span) == item['sha256'], 'Added B1 span review changed'
        assert span not in baseline and restored.count(span) == 1, 'Unreviewed B1 added source span'
        restored = restored.replace(span, '', 1)
    # No blanket source replacement. Any extra field/method/comment/default drift survives
    # the exact replacements above and fails here, even if all listed hashes still match.
    assert restored == baseline, 'Unreviewed B1 surrounding source changes'
    return restored


CURRENT_SCOPE_REVISION = 'f6e2ead7'

def current_scope_baseline(path):
    return subprocess.check_output(['git','show',CURRENT_SCOPE_REVISION+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')

def current_scope_dependencies(review):
    return list(review['productionDependencies'])+['src/modules/AF.Module.Memory/Records/MemoryPersistenceModels.cs','src/modules/AF.Module.Memory/Records/NpcActionEntry.cs']

def _verify_current_memory_source(source):
    """Finite Memory guards only. No captured/sealing execution or whole-host inverse."""
    spec=importlib.util.spec_from_file_location('b1_current_extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
    extractor=importlib.util.module_from_spec(spec);spec.loader.exec_module(extractor)
    review=json.loads((HERE/'source-review-b1.json').read_text(encoding='utf-8'))
    accepted=current_scope_baseline('MyBehavior.cs')
    for signature in ['private void RebuildEventSourceMaterialIndex(', 'private void TryRunCampaignMemoryMaintenance(', 'private void RunCampaignMemoryMaintenanceCycle(']:
        assert extractor.declaration(source,signature)==extractor.declaration(accepted,signature),'Unreviewed B1 declaration: '+signature
    # Bind the actual index composition once, including selectors. Appended duplicate fields fail.
    import re
    composition=re.search(r'private readonly AnimusForge\.Refactor\.Runtime\.EventSourceMaterialIndex<EventSourceMaterialEntry> _eventSourceMaterialIndexBinding\s*=\s*[^;]+;',accepted).group()
    assert source.count(composition)==1,'Unreviewed B1 added source span'
    for item in review['deletedDeclarations']:
        if _is_reviewed_deleted(item):
            assert item['signature'] not in source,'Deleted B1 declaration unexpectedly restored'
    for path in current_scope_dependencies(review):
        assert (current_source_path(ROOT, path)).read_text(encoding='utf-8-sig')==current_scope_baseline(path),'Unreviewed B1 production dependency: '+path
    # Material runner safety/path updates have their own reviewed and replayed package.
    path=review['evidence']['materials']['runner']
    material=subprocess.check_output(['git','show','35842e17:'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
    assert _restore_round2_current_paths(path, (current_source_path(ROOT, path)).read_text(encoding='utf-8-sig'))==material,'Unreviewed B1 evidence: '+path
    for path in review.get('removedProductionFiles',[]):
        assert not (current_source_path(ROOT, path)).exists(),'Obsolete B1 production file restored: '+path
    assert source==(current_source_path(ROOT, 'MyBehavior.cs')).read_text(encoding='utf-8-sig'),'Unreviewed B1 surrounding source changes'
    return True

def verify_current_memory_source(source):
    # SOURCE is explicit F3/B1 legacy input, including original in-memory mutations.
    # Context verifies actual bindings/inverse; B1 guards inspect original negative controls.
    from f3_migration_projection import projection_reads,restore
    with projection_reads():return _verify_current_memory_source(restore('MyBehavior.cs',source))

if __name__=='__main__':
    import argparse
    parser=argparse.ArgumentParser(description='Explicit finite Memory-only guards; historical whole-owner inverse remains separate.')
    parser.add_argument('--finite',action='store_true')
    args=parser.parse_args()
    if not args.finite:parser.error('select --finite; this adapter is not a whole-host completion gate')
    verify_current_memory_source(historical_source('MyBehavior.cs')) # Entry-only terminal inverse before F3/B1; mutated legacy test arguments are not re-restored.
    # This named finite entry executes the current negative controls too, while
    # the default historical test_source_parity.py entry remains unchanged.
    import unittest
    spec=importlib.util.spec_from_file_location('current_memory_scope_tests',HERE/'test_source_parity.py')
    current_tests=importlib.util.module_from_spec(spec);spec.loader.exec_module(current_tests)
    result=unittest.TextTestRunner(verbosity=2).run(unittest.defaultTestLoader.loadTestsFromTestCase(current_tests.CurrentScopeGuards))
    if not result.wasSuccessful():raise SystemExit(1)
    print('B1_CURRENT_SCOPE_PASS checks='+str(result.testsRun)+' diplomacy_coupled_replays=DEFERRED whole_host_inverse=NOT_RUN')

# Fixed separately tested migration; no old review hashes are refreshed.
from af2_terminal_migration_review import terminal_review

@terminal_review
def restore_memory_summary_source(path,source):
    if path != 'MyBehavior.cs':
        from f3_migration_projection import projection_reads,restore
        with projection_reads():return _restore_memory_summary_source(path,restore(path,source))
    return _restore_memory_summary_source(path,source)

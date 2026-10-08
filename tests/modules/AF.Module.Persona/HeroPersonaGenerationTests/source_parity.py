"""Exact Hero persona inverse before older B1/Native whole-owner parity; no hash-only waiver."""
from pathlib import Path
import sys as _relocation_sys
_relocation_sys.path.insert(0, str(Path(__file__).resolve().parents[4] / "tests"))
from output_isolation import current_source_path
from af2_terminal_migration_review import independent_historical_fixture
from af2_f5_migration_review import exact_inverse, verify_owners
import hashlib,importlib.util,json,subprocess
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
BASELINE='10defeb4976f3ffa096a77e847fba254308f6aba'
spec=importlib.util.spec_from_file_location('persona_decl',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)


def _restore_f5_fixture_wiring(path, source):
    review=json.loads((HERE/'f5-fixture-wiring.json').read_text(encoding='utf-8'))
    for delta in reversed(review['changes'].get(str(path).replace(chr(92),'/'),())):
        assert source.count(delta['after']) == 1, 'Unreviewed F5 persona dependency wiring: '+str(path)
        source=source.replace(delta['after'],delta['before'],1)
    return source

def _restore_round2_current_paths(path, source):
    """Undo only reviewed current-file locator/import edits; retain original review hashes."""
    reviewed = {'tests/modules/AF.Module.Persona/HeroPersonaGenerationTests/run.py': {'before_sha256': 'e90e7941e4d7a472ff9995cdd5c6d2949ea2f12b195bcea4c080387af15122cc', 'edits': [('', 'import sys as _relocation_sys\n_relocation_sys.path.insert(0, str(Path(__file__).resolve().parents[4] / "tests"))\nfrom output_isolation import current_source_path\n'), ("ui_source=s if a.original else (ROOT/'MyBehavior.cs').read_text(encoding='utf-8-sig')\n", "ui_source=s if a.original else (current_source_path(ROOT, 'MyBehavior.cs')).read_text(encoding='utf-8-sig')\n"), (" helper=(ROOT/'MyBehavior.PersonaGeneration.cs').read_text(encoding='utf-8-sig');owner=(ROOT/'src/modules/AF.Module.Persona/Generation/NpcPersonaGenerationOwner.cs').read_text(encoding='utf-8-sig')\n", " helper=(current_source_path(ROOT, 'MyBehavior.PersonaGeneration.cs')).read_text(encoding='utf-8-sig');owner=(ROOT/'src/modules/AF.Module.Persona/Generation/NpcPersonaGenerationOwner.cs').read_text(encoding='utf-8-sig')\n"), (" promoted=(ROOT/'MyBehavior.PromotedPersonaGeneration.cs').read_text(encoding='utf-8-sig')\n", " promoted=(current_source_path(ROOT, 'MyBehavior.PromotedPersonaGeneration.cs')).read_text(encoding='utf-8-sig')\n")]}}
    source = _restore_f5_fixture_wiring(path, source)
    packet = reviewed.get(str(path).replace(chr(92), "/"))
    if packet is None:
        return source
    if hashlib.sha256(source.encode()).hexdigest() == packet["before_sha256"]:
        return source
    for before, after in reversed(packet["edits"]):
        assert source.count(after) == 1, "Unreviewed round2 source locator: " + str(path)
        source = source.replace(after, before, 1)
    assert hashlib.sha256(source.encode()).hexdigest() == packet["before_sha256"], "Unreviewed round2 surrounding runner: " + str(path)
    return source

def prior():return subprocess.check_output(['git','show',BASELINE+':MyBehavior.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
def restore_historical(source,strict=True):
 run_spec=importlib.util.spec_from_file_location('persona_memory_run_inverse',ROOT/'tests/modules/AF.Module.Memory/MemorySummaryRunOwnerTests/source_parity.py');run_inverse=importlib.util.module_from_spec(run_spec);run_spec.loader.exec_module(run_inverse)
 source=run_inverse.restore('MyBehavior.cs',source)
 review=json.loads((HERE/'source-review.json').read_text(encoding='utf-8'))
 for path,expected in review['dependencies'].items():
  dependency=_restore_round2_current_paths(path, (current_source_path(ROOT, path)).read_text(encoding='utf-8-sig'))
  import sys
  f3=sys.modules.get('f3_migration_projection')
  prior_producer=None if f3 is None else f3.current_prior_memory_run_producer()
  if prior_producer is not None and prior_producer.chain is not None:
   dependency=prior_producer.chain.apply('PERSONA_AFTER_ORIGINAL_LOCATOR',path,dependency)
  assert hashlib.sha256(dependency.encode()).hexdigest()==expected,'Unreviewed persona dependency: '+path
 old=prior();result=source
 for signature,expected in review['replacementMethods'].items():
  current=ex.declaration(result,signature)
  assert hashlib.sha256(current.encode()).hexdigest()==expected,'Unreviewed persona declaration: '+signature
  result=result.replace(current,ex.declaration(old,signature),1)
 for start,end in [
  ('\tprivate readonly object _npcPersonaAutoGenLock = new object();','\tprivate HashSet<string> _recentlyDefeatedByPlayer'),
  ('\tprivate async Task EnsureNpcPersonaGeneratedAsync(','\tpublic static async Task GeneratePromotedNonHeroCompanionProfileForExternalAsync('),
  ('\tpublic static async Task EnsureNpcPersonaGeneratedForExternalAsync(','\tpublic static string BuildCurrentDateFactForExternal(')]:
  block=old[old.index(start):old.index(end,old.index(start))]
  assert start not in result and result.count(end)==1,'Persona move has duplicate/missing anchor'
  result=result.replace(end,block+end,1)
 reset='''\t\t\tlock (_npcPersonaAutoGenLock)
\t\t\t{
\t\t\t\t_npcPersonaAutoGenInFlight.Clear();
\t\t\t\t_npcPersonaAutoGenRetryAfterUtcTicks.Clear();
\t\t\t}'''
 assert result.count('\t\t\t_npcPersonaGeneration.Reset();')==1
 result=result.replace('\t\t\t_npcPersonaGeneration.Reset();',reset,1)
 clear='private void ClearAllDataForCurrentSave()\n\t{\n\t\t_npcPersonaGeneration.Reset();'
 assert result.count(clear)==1;result=result.replace(clear,clear.rsplit('\n',1)[0],1)
 # Existing parser, facts, config/auxiliary transport, all other consumers and fields stay exact.
 if strict:assert result==old,'Unreviewed persona surrounding source changes'
 return result

# J17 moved unrelated Memory/Weekly code; compare only this owner, not the old whole host.
CURRENT_REVIEW='f6e2ead7'
@independent_historical_fixture("PERSONA_F6")
def restore(source,strict=True):
 verify_owners()
 source=exact_inverse('MyBehavior.cs',source,verify=False)
 live=exact_inverse('MyBehavior.cs',(current_source_path(ROOT, 'MyBehavior.cs')).read_text(encoding='utf-8-sig'),verify=False)
 review=json.loads((HERE/'source-review.json').read_text(encoding='utf-8'))
 for path,h in review['dependencies'].items():
  raw=(current_source_path(ROOT, path)).read_text(encoding='utf-8-sig')
  if path in ('MyBehavior.PersonaGeneration.cs','MyBehavior.PromotedPersonaGeneration.cs'): raw=exact_inverse(path,raw,verify=False)
  text=_restore_round2_current_paths(path, raw)
  if path.startswith('tests/'):
   # The candidate already records J13 promoted cases, J16 paths and J17 SDK safety.
   expected=subprocess.check_output(['git','show',CURRENT_REVIEW+':'+path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
   if path.endswith('/run.py'):
    for delta in review['runnerSafetyChanges']:
     assert expected.count(delta['before'])==1
     expected=expected.replace(delta['before'],delta['after'],1)
   assert text==expected,'Unreviewed persona dependency: '+path
  else:assert hashlib.sha256(text.encode()).hexdigest()==h,'Unreviewed persona dependency: '+path
 accepted=subprocess.check_output(['git','show',CURRENT_REVIEW+':MyBehavior.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
 for signature in review['replacementMethods']:
  assert ex.declaration(source,signature)==ex.declaration(accepted,signature),'Unreviewed persona declaration: '+signature
 assert source==live,'Unreviewed persona surrounding input changes'
 for signature in ['private void ResetLocalTransientRuntimeForLoadedSave(', 'private void ClearAllDataForCurrentSave(']:
  assert '_npcPersonaGeneration.Reset();' in ex.declaration(source,signature),'Missing persona reset consumer'
 return prior()

@independent_historical_fixture("PERSONA_F6")
def verify():
 restore((current_source_path(ROOT, 'MyBehavior.cs')).read_text(encoding='utf-8-sig'))
 old=ex.declaration(prior(),'private async Task<string> GenerateNpcPersonaAsync(')
 prompt=old[old.index('\t\t\tstring sys = '):old.index('\t\t\tApiCallResult apiCallResult = ')].rstrip()
 helper=exact_inverse('MyBehavior.PersonaGeneration.cs',(current_source_path(ROOT, 'MyBehavior.PersonaGeneration.cs')).read_text(encoding='utf-8-sig'),verify=False)
 assert prompt in helper,'Original persona prompt block changed'
 print('PASS scoped current persona consumers; original prompt unchanged; historical whole inverse retained')
if __name__=='__main__':verify()

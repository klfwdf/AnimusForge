"""Exact Hero persona inverse before older B1/Native whole-owner parity; no hash-only waiver."""
from pathlib import Path
import hashlib,importlib.util,json,subprocess
ROOT=Path(__file__).resolve().parents[2];HERE=Path(__file__).parent
BASELINE='10defeb4976f3ffa096a77e847fba254308f6aba'
spec=importlib.util.spec_from_file_location('persona_decl',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
def prior():return subprocess.check_output(['git','show',BASELINE+':MyBehavior.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
def restore(source,strict=True):
 run_spec=importlib.util.spec_from_file_location('persona_memory_run_inverse',ROOT/'tools/MemorySummaryRunOwnerTests/source_parity.py');run_inverse=importlib.util.module_from_spec(run_spec);run_spec.loader.exec_module(run_inverse)
 source=run_inverse.restore('MyBehavior.cs',source)
 review=json.loads((HERE/'source-review.json').read_text(encoding='utf-8'))
 for path,expected in review['dependencies'].items():
  assert hashlib.sha256((ROOT/path).read_text(encoding='utf-8-sig').encode()).hexdigest()==expected,'Unreviewed persona dependency: '+path
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

def verify():
 restore((ROOT/'MyBehavior.cs').read_text(encoding='utf-8-sig'))
 old=ex.declaration(prior(),'private async Task<string> GenerateNpcPersonaAsync(')
 prompt=old[old.index('\t\t\tstring sys = '):old.index('\t\t\tApiCallResult apiCallResult = ')].rstrip()
 helper=(ROOT/'MyBehavior.PersonaGeneration.cs').read_text(encoding='utf-8-sig')
 assert prompt in helper,'Original persona prompt block changed'
 print('PASS exact whole MyBehavior inverse; original prompt unchanged; new helper/state/evidence bound')
if __name__=='__main__':verify()

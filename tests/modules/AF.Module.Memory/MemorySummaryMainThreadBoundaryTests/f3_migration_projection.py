"""Fixed approved F3 migration inverse; old review hashes remain authoritative."""
import json,hashlib
from pathlib import Path
from contextlib import contextmanager
from unittest.mock import patch
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
PACKET=json.loads((HERE/'source-review-f3-migration.json').read_text(encoding='utf-8'))
def sha(s):return hashlib.sha256(s.replace('\r\n','\n').encode()).hexdigest()
def key(path):
 p=Path(path)
 if p.is_absolute():return p.relative_to(ROOT).as_posix()
 if str(path).startswith('MyBehavior'):return 'src/AF.GameAdapter.Bannerlord/Composition/'+str(path)
 return str(path).replace(chr(92),'/')
def restore(path,source):
 source=source.replace('\r\n','\n');p=key(path);row=PACKET['paths'].get(p)
 if row is None or sha(source)==row['beforeSha256']:return source
 for e in reversed(row['edits']):
  old,new=e['before'],e['after']
  if source.count(new)==1:source=source.replace(new,old,1)
  elif source.count(new)==0 and source.count(old)==1:continue
  else:raise AssertionError('Unreviewed B1 declaration migration: '+p)
 return source
@contextmanager
def _f3_projection_reads():
 read=Path.read_text
 for p,h in PACKET['dependencies'].items():
  assert sha(read(ROOT/p,encoding='utf-8-sig'))==h,'Unreviewed B1 production dependency: '+p
 def projected(path,*args,**kwargs):
  text=read(path,*args,**kwargs)
  try:p=key(path)
  except ValueError:return text
  return restore(p,text) if p in PACKET['paths'] else text
 with patch.object(Path,'read_text',projected):yield

@contextmanager
def projection_reads():
 from af2_terminal_migration_review import projection_reads as terminal_projection_reads, independent_projection_reads
 with terminal_projection_reads():
  with independent_projection_reads("F3"):
   with _f3_projection_reads():yield

def read_current(path):
 with projection_reads():return (ROOT/key(path)).read_text(encoding='utf-8-sig')

# Prior MemoryRun is an explicitly approved F3 input branch, never runtime source.
from contextvars import ContextVar
_prior_memory_run = ContextVar("f3_prior_memory_run", default=None)

def current_prior_memory_run_producer():
 return _prior_memory_run.get()


class _OriginalDependencyGuardChain:
 # Cold historical review only. No generated input is a current behavior fixture.
 def __init__(self, metadata, producer):
  self.producer = producer
  self.active = False
  self.rows = {}
  persona = 'tests/modules/AF.Module.Persona/HeroPersonaGenerationTests/'
  memory = 'tests/modules/AF.Module.Memory/MemorySummaryMainThreadBoundaryTests/'
  roles = {
   'PERSONA_READ': ['MyBehavior.PersonaGeneration.cs', persona+'Harness.cs.txt', persona+'run.py'],
   'PERSONA_AFTER_ORIGINAL_LOCATOR': [persona+'Harness.cs.txt', persona+'run.py'],
   'B1_READ': [memory+x for x in ('run_business.py','run.py','run_captured.py','CapturedHarness.cs.txt','run_fingerprint.py','run_writers.py','run_planning.py','run_commit_writers.py','CommitWritersHarness.cs.txt','run_terminal.py','TerminalHarness.cs.txt','run_sealing.py','SealingHarness.cs.txt','run_materials.py')]+['tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py','MyBehavior.MemorySealing.cs','MyBehavior.MemorySummaryInput.cs','tests/AF.GameAdapter.Bannerlord/GameLifetimeTests/Bindings.cs.txt'],
   'RUN_AFTER_ORIGINAL_LOCATOR': [memory+x for x in ('run_business.py','run.py','run_captured.py','run_writers.py','run_planning.py','run_terminal.py','run_sealing.py')],
   'B1_AFTER_ORIGINAL_LOCATOR': [memory+'run_commit_writers.py',memory+'run_materials.py'],
   'LIFETIME_EXPECTED_IDENTITY': ['MyBehavior.MemorySummaryMainThread.cs'],
  }
  required = {(stage,key(path)) for stage,paths in roles.items() for path in paths}
  for row in metadata['stages']:
   identity = (row['stage'],key(row['legacyPath']))
   assert identity not in self.rows, 'Duplicate original dependency stage'
   assert row['editOrder'] == 'FORWARD_LIST', 'Unreviewed dependency context order'
   self.rows[identity] = row
  assert set(self.rows) == required, 'Incomplete/extra original dependency stage'
  physical = {key(path) for paths in roles.values() for path in paths}
  self.bindings = metadata['physicalBindings']
  assert len(self.bindings) == len(physical) and {item['path'] for item in self.bindings} == physical, 'Unreviewed dependency physical binding set'
  self.owners = metadata['actualOwnerBindings']
  assert len(self.owners) == 1 and self.owners[0]['path'] == 'src/modules/AF.Module.Conversation/Channels/Native/ConversationMainThreadActionDrain.cs', 'Unreviewed lifetime/ActionDrain binding set'
  self.check_physical()
  self.read_rows = {path:row for (stage,path),row in self.rows.items() if stage in ('PERSONA_READ','B1_READ')}
  self.old_arguments = metadata['runOldArgumentSha256']
  registered = {key(path) for path in roles['RUN_AFTER_ORIGINAL_LOCATOR']} | {key('MyBehavior.MemorySummaryInput.cs')}
  assert set(self.old_arguments) == registered, 'Unreviewed original Run argument set'
  self.argument_old = set()

 def check_physical(self):
  for item in self.bindings + self.owners:
   assert hashlib.sha256((ROOT/item['path']).read_bytes()).hexdigest() == item['rawSha256'], 'Unreviewed original dependency physical input: '+item['path']

 def apply(self, stage, path, source):
  assert self.active, 'Original dependency stage requires bound review context'
  row = self.rows.get((stage,key(path)))
  if row is None:
   return source
  self.check_physical()
  source = source.replace('\r\n','\n')
  assert sha(source) == row['sourceSha256'], 'Unreviewed original dependency stage input: '+stage+':'+key(path)
  for change in row['edits']:
   assert change['after'] and change['symbols'] and change['after'] != source, 'Original dependency requires narrow named context'
   assert source.count(change['after']) == 1, 'Unreviewed original dependency unique context: '+stage+':'+key(path)
   source = source.replace(change['after'],change['before'],1)
  assert sha(source) == row['targetSha256'], 'Incomplete original dependency inverse: '+stage+':'+key(path)
  self.producer.stages.append({'stage':stage,'path':key(path),'inputSha256':row['sourceSha256'],'sha256':sha(source)})
  return source

 @contextmanager
 def projection_reads(self):
  assert not self.active, 'Original dependency review reentry'
  self.active = True
  read = Path.read_text
  try:
   # Verify every actual upstream read before the original guard chains execute.
   for path,row in self.read_rows.items():
    self.apply(row['stage'],path,read(ROOT/path,encoding='utf-8-sig'))
   def projected(path,*args,**kwargs):
    source = read(path,*args,**kwargs)
    try:relative = key(path)
    except ValueError:return source
    row = self.read_rows.get(relative)
    return self.apply(row['stage'],relative,source) if row is not None else source
   with patch.object(Path,'read_text',projected):
    yield
  finally:
   self.active = False
   self.argument_old.clear()

 def run_remote_input(self, path, source):
  # This receives only the result of the ORIGINAL remote inverse, never raw SOURCE.
  relative = key(path)
  row = self.rows.get(('RUN_AFTER_ORIGINAL_LOCATOR',relative))
  if row is None:
   return source
  self.check_physical()
  prepared = self.read_rows[relative]
  accepted = {prepared['targetSha256'],row['sourceSha256'],row['targetSha256'],self.old_arguments[relative]}
  assert sha(source) in accepted, 'Unreviewed original Run dependency argument: '+relative
  if sha(source) == self.old_arguments[relative]:
   self.argument_old.add(relative)
  # The full original remote/locator/livewhole/argument guards still run. Only
  # exact validated original identities are presented at their recorded epoch.
  self.producer.stages.append({'stage':'ORIGINAL_REMOTE_DEPENDENCY','path':relative,'sha256':sha(source)})
  if sha(source) == prepared['targetSha256']:
   return source
  if sha(source) == self.old_arguments[relative]:
   lines = source.splitlines(keepends=True)
   for delta in reversed(self._run_review['paths'][prepared['legacyPath']]):
    a,b=delta['start'],delta['end']
    assert ''.join(lines[a:b]) == delta['before'], 'Original Run argument hunk drift'
    lines[a:b] = [delta['after']]
   source = ''.join(lines)
   assert sha(source) == row['targetSha256'], 'Original Run argument whole drift'
  if sha(source) == row['targetSha256']:
   for change in reversed(row['edits']):
    assert source.count(change['before']) == 1, 'Original Run forward named context drift'
    source = source.replace(change['before'],change['after'],1)
  assert sha(source) == row['sourceSha256'], 'Original locator stage identity drift'
  locator = self._run_locators[prepared['legacyPath']]
  new_guard = 'src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs'
  old_guard = 'SaveRuntimeGuard.cs'
  if sha(source) != locator['before_sha256']:
   import re
   contexts = list(re.finditer(r'''(?:ROOT\s*/\s*|read\()\s*([\"'])SaveRuntimeGuard\.cs\1''',source))
   assert len(contexts) == 1, 'Original guard forward context drift'
   context = contexts[0].group(0)
   assert source.count(context) == 1, 'Original guard forward unique context drift'
   source = source.replace(context,context.replace(old_guard,new_guard),1)
  assert sha(source) == locator['before_sha256'], 'Original locator before whole drift'
  for before,after in locator['edits']:
   if before:
    assert source.count(before) == 1, 'Original locator forward context drift'
    source = source.replace(before,after,1)
   else:
    anchor='from pathlib import Path\n'
    assert source.count(anchor) == 1, 'Original locator import anchor drift'
    source = source.replace(anchor,anchor+after,1)
  assert sha(source) == prepared['targetSha256'], 'Original prepared whole drift'
  return source

 def run_locator_output(self, path, source):
  relative = key(path)
  output = self.apply('RUN_AFTER_ORIGINAL_LOCATOR',relative,source)
  if relative in self.argument_old:
   self.argument_old.remove(relative)
   return self._old_inputs[relative]
  return output

 def bind_original_inputs(self):
  import subprocess
  data = json.loads((ROOT/'tests/modules/AF.Module.Memory/MemorySummaryRunOwnerTests/source-review.json').read_text(encoding='utf-8'))
  self._run_review = data
  import ast
  locator_path = ROOT/'tests/modules/AF.Module.Memory/MemorySummaryRunOwnerTests/source_parity.py'
  tree = ast.parse(locator_path.read_text(encoding='utf-8-sig'))
  function = next(n for n in ast.walk(tree) if isinstance(n,ast.FunctionDef) and n.name=='_restore_round2_current_paths')
  self._run_locators = next(ast.literal_eval(n.value) for n in function.body if isinstance(n,ast.Assign) and any(isinstance(t,ast.Name) and t.id=='reviewed' for t in n.targets))
  self._prepared_inputs = {}
  self._old_inputs = {}
  for path,row in self.read_rows.items():
   if path not in self.old_arguments:
    continue
   self._prepared_inputs[path] = (ROOT/path).read_text(encoding='utf-8-sig')
   legacy = row['legacyPath']
   old_path = 'tools/'+legacy.split('/')[-2]+'/'+legacy.split('/')[-1] if legacy.startswith('tests/') else legacy
   old = subprocess.check_output(['git','show',data['baseline']+':'+old_path],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
   assert sha(old) == self.old_arguments[path], 'Original Run argument baseline drift'
   self._old_inputs[path] = old
   row_run = self.rows.get(('RUN_AFTER_ORIGINAL_LOCATOR',path))
   if row_run is not None:
    lines = old.splitlines(keepends=True)
    for delta in reversed(data['paths'][legacy]):
     a,b=delta['start'],delta['end']
     assert ''.join(lines[a:b]) == delta['before'], 'Original Run dependency hunk drift'
     lines[a:b] = [delta['after']]
    assert sha(''.join(lines)) == row_run['targetSha256'], 'Original Run dependency whole target drift'

class _PriorMemoryRunProducer:
 def __init__(self, row):
  from remote_feature_delta import restore_remote_feature_delta
  import subprocess
  self.row = row
  self.remote = restore_remote_feature_delta
  self.ready = False
  self.chain = None
  assert row['path'] == 'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs', 'Unreviewed prior MemoryRun path'
  review = json.loads((ROOT/'tests/modules/AF.Module.Memory/MemorySummaryRunOwnerTests/source-review.json').read_text(encoding='utf-8'))
  old = subprocess.check_output(['git','show',review['baseline']+':MyBehavior.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
  lines = old.splitlines(keepends=True)
  for delta in reversed(review['paths']['MyBehavior.cs']):
   a,b=delta['start'],delta['end']
   assert ''.join(lines[a:b]) == delta['before'], 'Prior MemoryRun original hunk drift'
   lines[a:b] = [delta['after']]
  assert sha(''.join(lines)) == row['targetSha256'], 'Prior MemoryRun original whole target drift'
  assert sha(old) == row['oldArgumentSha256'], 'Prior MemoryRun original argument drift'
  required = {'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs', 'src/modules/AF.Module.Memory/Summary/MemorySummaryRunOwner.cs', 'src/modules/AF.Module.Memory/Summary/MemoryBusinessStateOwner.cs'}
  assert len(row['physicalBindings']) == len(required) and {item['path'] for item in row['physicalBindings']} == required, 'Unreviewed prior MemoryRun binding set'
  self.check_physical()
  source = (ROOT/row['path']).read_text(encoding='utf-8-sig')
  remote_source = self.remote('MyBehavior.cs',source)
  self.stages = [{'stage':'F3_READ','sha256':sha(source)}, {'stage':'ORIGINAL_REMOTE','sha256':sha(remote_source)}]
  self.canonical = self.inverse(remote_source)
  self.stages.append({'stage':'PRIOR_MEMORY_RUN','sha256':sha(self.canonical)})
  self.ready = True

 def check_physical(self):
  for binding in self.row['physicalBindings']:
   assert hashlib.sha256((ROOT/binding['path']).read_bytes()).hexdigest() == binding['physicalRawSha256'], 'Unreviewed prior MemoryRun physical input: '+binding['path']

 def inverse(self, source):
  source = source.replace('\r\n','\n')
  assert sha(source) == self.row['sourceSha256'], 'Unreviewed prior MemoryRun source'
  for edit in reversed(self.row['edits']):
   assert edit['after'] and edit['symbols'], 'Unnamed prior MemoryRun context'
   assert source.count(edit['after']) == 1, 'Unreviewed prior MemoryRun context'
   source = source.replace(edit['after'],edit['before'],1)
  assert sha(source) == self.row['targetSha256'], 'Incomplete prior MemoryRun inverse'
  return source

 def record_run_acceptance(self, path, live, argument, old):
  self.stages.append({'stage':'ORIGINAL_MEMORY_RUN_GUARDS','path':path,'liveSha256':sha(live),'argumentSha256':sha(argument),'outputSha256':sha(old)})

 def restore_remote_input(self, path, source):
  if path != 'MyBehavior.cs':
   remote = self.remote(path,source)
   return self.chain.run_remote_input(path,remote) if self.chain is not None else remote
  self.check_physical()
  # Only the two original MemoryRun argument identities are idempotent.
  # The actual physical chain has already passed remote + contextual validation.
  if self.ready and sha(source) in (self.row['targetSha256'],self.row['oldArgumentSha256']):
   return source
  return self.inverse(self.remote(path,source))

@contextmanager
def prior_memory_run_producer():
 from af2_terminal_migration_review import j17_packet
 assert _prior_memory_run.get() is None, 'Prior MemoryRun producer reentry'
 row = j17_packet()['independentLayers']['F3']['priorMemoryRun']
 producer = _PriorMemoryRunProducer(row)
 token = _prior_memory_run.set(producer)
 try:
  metadata = j17_packet()['independentLayers']['F3'].get('originalGuardChain')
  if metadata is None:
   yield producer
  else:
   producer.chain = _OriginalDependencyGuardChain(metadata,producer)
   with producer.chain.projection_reads():
    producer.chain.bind_original_inputs()
    yield producer
 finally:
  _prior_memory_run.reset(token)

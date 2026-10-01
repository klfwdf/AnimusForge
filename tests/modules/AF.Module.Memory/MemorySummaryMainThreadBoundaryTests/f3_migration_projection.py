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
def projection_reads():
 read=Path.read_text
 for p,h in PACKET['dependencies'].items():
  assert sha(read(ROOT/p,encoding='utf-8-sig'))==h,'Unreviewed B1 production dependency: '+p
 def projected(path,*args,**kwargs):
  text=read(path,*args,**kwargs)
  try:p=key(path)
  except ValueError:return text
  return restore(p,text) if p in PACKET['paths'] else text
 with patch.object(Path,'read_text',projected):yield

def read_current(path):
 with projection_reads():return (ROOT/key(path)).read_text(encoding='utf-8-sig')

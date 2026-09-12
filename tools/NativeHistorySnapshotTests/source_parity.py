"""Exact, separately behavior-tested snapshot deltas for older boundary suites."""
import json,hashlib,subprocess,importlib.util
from pathlib import Path
ROOT=Path(__file__).resolve().parents[2]
def restore_snapshot_source(path,source):
 spec=importlib.util.spec_from_file_location('snapshot_ex',ROOT/'tools/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
 review=json.loads((Path(__file__).parent/'source-review.json').read_text(encoding='utf-8'))
 prior=subprocess.check_output(['git','show',review['baseline']+':'+path],cwd=ROOT).decode('utf-8-sig')
 post_snapshot_baseline=review.get('postSnapshotBaseline')
 if post_snapshot_baseline:
  post_source=subprocess.check_output(['git','show',post_snapshot_baseline+':'+path],cwd=ROOT).decode('utf-8-sig')
  for item in review.get('postSnapshotMethods',[]):
   if item['path']!=path:continue
   current=ex.declaration(source,item['signature'])
   assert hashlib.sha256(current.encode()).hexdigest()==item['sha256'],'Unreviewed post-snapshot source'
   source=source.replace(current,ex.declaration(post_source,item['signature']),1)
 for item in review['methods']:
  if item['path']!=path:continue
  current=ex.declaration(source,item['signature'])
  assert hashlib.sha256(current.encode()).hexdigest()==item['sha256'] and 'TeamModuleServices.' not in current,'Unreviewed snapshot source'
  if path=='MyBehavior.cs':
   inverse=current
   for a,b in reversed(review['defaultRewrites'][item['signature']]):inverse=inverse.replace(b,a)
   assert inverse==ex.declaration(prior,item['baselineSignature']),'Legacy default semantics changed'
  source=source.replace(current,ex.declaration(prior,item['baselineSignature']),1)
 assert source==prior,'Unreviewed surrounding owner changes'
 return source

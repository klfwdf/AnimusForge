"""Every mutant must compile and fail its intended named runtime assertion."""
from pathlib import Path
import json,subprocess,sys
HERE=Path(__file__).resolve().parent;ROOT=HERE.parents[3]
expected={
'value-identity':'equal_value_is_not_ticket_identity',
'release-new-slot':'equal_ticket_cannot_release_slot',
'end-without-epoch':'conversation_end_retires_queued_request_even_if_game_token_reused',
'release-ends-conversation':'foreign_release_does_not_end_conversation',
'ignore-presentation':'new_admission_retires_old_presentation',
'begin-ends-conversation':'presentation_does_not_end_conversation',
'reserve-advances-presentation':'reservation_does_not_consume_presentation'}
results=[]
for mutation,assertion in expected.items():
 r=subprocess.run([sys.executable,'-B',str(HERE/'run.py'),'--mutate',mutation],cwd=ROOT,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=180)
 log=r.stdout+r.stderr
 assert r.returncode!=0 and 'FAIL '+assertion in log and 'error CS' not in log and 'Build FAILED' not in log,(mutation,log)
 results.append({'mutation':mutation,'exitCode':r.returncode,'expectedAssertion':assertion})
 print('PASS runtime mutation rejected: '+mutation+' / '+assertion,flush=True)
(HERE/'.generated/mutations.json').write_text(json.dumps(results,indent=2)+'\n')

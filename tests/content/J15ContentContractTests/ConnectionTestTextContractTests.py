"""Static worker-boundary and bilingual connection-test payload contract."""
from pathlib import Path
import re
import sys
import xml.etree.ElementTree as ET

ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tools/localization'))
from inventory_untranslated import LEX, decode, HAN
source=(ROOT/'src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.cs').read_text(encoding='utf-8-sig')
snapshot=(ROOT/'src/AF.GameAdapter.Bannerlord/Configuration/Mcm/ConnectionTestText.cs').read_text(encoding='utf-8-sig')
checks=0
def check(ok, message):
    global checks
    checks+=1
    if not ok:raise AssertionError(message)
for member,stage in {'TestTownAmbientAiConnection':'Ambient','TestConnection':'Main','TestAuxiliaryConnection':'Preprocess','TestActionPostprocessConnection':'Postprocess','TestEventAndRebellionConnection':'Event'}.items():
    start=source.index('\t\t'+member+' = delegate\n')
    stop=re.search(r'\n\t\t\w+ = delegate\n',source[start+1:])
    body=source[start:start+1+stop.start()] if stop else source[start:]
    capture='ConnectionTestText.Capture("'+stage+'")'
    check(body.count(capture)==1 and body.index(capture)<body.index('Task.Run('),member+' captures text before background work')
    worker=body[body.index('Task.Run('):]
    check('ConnectionTestText.Capture(' not in worker,member+' worker never recaptures localization')
    chinese=[decode(m[0]) for m in LEX.finditer(worker) if m.lastgroup=='string' and HAN.search(decode(m[0]))]
    check(not chinese,member+' still hardcodes Chinese runtime text: '+repr(chinese[:2]))
check('private readonly string _failureStatus;' in snapshot and 'internal string PromptUser { get; }' in snapshot,
      'snapshot payload is immutable')
check('internal string Failure(object status, string error)\n        => Stage + _failureStatus + status + _failureError + error;' in snapshot,
      'worker formatter only combines captured strings and original result data')
check('testText.Failure(statusCode, responseString)' in source and
      'testText.FirstResponseHeader + firstResponseString + testText.RetryResponseHeader + retryResponseString' in source,
      'diagnostics retain full status and both response bodies')
check(source.count('SendConnectionTestRequestAsync("action_postprocess_validation_retry"')==1,
      'one explicit reasoning fallback retry remains')
check('if (string.IsNullOrWhiteSpace(reply) && LlmApiCompat.IsReasoningOnlyTokenLimitResponse(' in source,
      'retry eligibility unchanged')
for name in ('af_runtime_strings.xml','CNs/af_runtime_strings-zh-CN.xml'):
    values={n.get('id'):n.get('text') for n in ET.parse(ROOT/'content/foundation/AF.Foundation.Localization/ModuleData/Languages'/name).getroot().iter('string')}
    check(len(values)==20,name+' has all snapshot keys')
    for stage in ('Main','Preprocess','Postprocess','Event','Ambient'):
        check('AF_RUNTIME_ApiTest_Stage_'+stage in values,name+' stage '+stage)
    check(values['AF_RUNTIME_ApiTest_PostprocessPrompt'].count('[ACTION:MOOD:NEUTRAL]')==1,
          name+' keeps the exact action tag')
    check(values['AF_RUNTIME_ApiTest_FirstResponse'].endswith('\n') and
          values['AF_RUNTIME_ApiTest_RetryResponse'].startswith('\n\n'),name+' preserves diagnostic separation')
print(f'PASS connection-test snapshot static contract: {checks} checks; no provider call or compiled replay')

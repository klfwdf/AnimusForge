import json,sys,subprocess,shutil,re,importlib.util
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3];OUT=ROOT/'artifacts/localization/verification-20261011/prompt-retrieval';OUT.mkdir(exist_ok=True,parents=True)
sys.path.insert(0,str(ROOT/'tests'));from output_isolation import minimal_test_environment
raw=json.loads((ROOT/'content/modules/AF.Module.Prompt/ModuleData/PromptRetrievalCompatibility.json').read_text(encoding='utf-8'));cn=json.loads((ROOT/'content/modules/AF.Module.Prompt/ModuleData/PromptRetrievalTexts.json').read_text(encoding='utf-8'));en=json.loads((ROOT/'content/modules/AF.Module.Prompt/ModuleData/PromptRetrievalTexts.en.json').read_text(encoding='utf-8'));assert cn.keys()==en.keys()
manifest=json.loads((OUT/'migration-manifest.json').read_text(encoding='utf-8'))
for row in manifest:
 if row['kind']=='compatibility':assert raw[row['key']]==row['value']
# Exact inverse of compatibility migrations proves old match/order/regex/protocol remains intact.
for filename in {row['file'] for row in manifest}:
 source=(ROOT/filename).read_text(encoding='utf-8');before=(OUT/(Path(filename).name+'.before.txt')).read_text(encoding='utf-8')
 inverse=source
 for row in sorted([x for x in manifest if x['file']==filename and x['kind']=='compatibility'],key=lambda x:len(x['replacement']),reverse=True):inverse=inverse.replace(row['replacement'],row['originalToken'])
 if Path(filename).name in ['IntentQueryOptimizer.cs','PromptStickyRuleStore.cs','PromptRuleIntentSplitter.cs']:assert inverse==before,filename
print('PASS raw exact values and full input matcher inverse (including ordered dictionaries/arrays)')
d=ROOT/'local/dotnet/8.0.425/dotnet.exe';project=ROOT/'tests/modules/AF.Module.Prompt/Retrieval/PromptCandidateSelectionTests.csproj';env=minimal_test_environment(d,OUT)
for lang,suffix in [('cn',''),('en','.en')]:
 p=OUT/lang;p.mkdir(exist_ok=True)
 shutil.copy2(ROOT/('content/modules/AF.Module.Prompt/ModuleData/PromptRetrievalTexts'+suffix+'.json'),p/'PromptRetrievalTexts.json')
 shutil.copy2(ROOT/('content/foundation/AF.Foundation.Localization/ModuleData/AdapterPromptCapture.'+('zh-CN' if lang=='cn' else 'en')+'.json'),p/'AdapterPromptCapture.json')
 shutil.copy2(ROOT/('content/modules/AF.Module.Economy/ModuleData/EconomyPromptTexts'+suffix+'.json'),p/'EconomyPromptTexts.json')
 runenv=dict(env,AF_TEST_RETRIEVAL_PACKAGE=str(p));a=subprocess.run([str(d),'run','--project',str(project),'-c','Release','--nologo','-p:RestoreConfigFile='+str(OUT/'NuGet.Config')],env=runenv,cwd=ROOT,capture_output=True,text=True,encoding='utf-8',timeout=120);(OUT/(lang+'-retrieval-run.log')).write_text(a.stdout+a.stderr,encoding='utf-8');print(a.stdout[-2200:]+a.stderr);a.check_returncode()

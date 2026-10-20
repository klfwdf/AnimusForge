"""Exact-source regression replay for observed postprocess failures; no game/network."""
from pathlib import Path
import argparse,hashlib,importlib.util,json,subprocess,sys
ROOT=Path(__file__).resolve().parents[4]
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
parser=argparse.ArgumentParser();parser.add_argument('--run-root',type=Path);args=parser.parse_args()
out=new_run_root(ROOT,'postprocess-tag-repair',args.run_root)
spec=importlib.util.spec_from_file_location('extract',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
inputs={}
def read(p):
 data=(ROOT/p).read_bytes();inputs[p]=hashlib.sha256(data).hexdigest();return data.decode('utf-8-sig')
owner=read('src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs')
methods=['NormalizeMoodPostprocessTag','MergeNormalizedPostprocessBlocksForScene','NormalizeAutoGroupRelayPostprocessTagsForScene','NormalizeDiplomacyPostprocessTagsForScene','ExtractDiplomacyPostprocessActionName','BuildRuntimeDiplomacyPostprocessRulesForScene','MergePostprocessRulesForScene','MergePostprocessRuleDescription','MergeSharedDeferredDebtPostprocessRule','NormalizeDuelPostprocessTagsForScene','TranslateDuelStakeItemIndexesForScene','FindDuelStakeOptionByTokenForScene']
import re
body=''
for name in methods:
 match=re.search(r'internal static [^\n]+? '+name+r'\(',owner);assert match,name
 body+=ex.declaration(owner,match[0])+'\n'
(out/'Program.cs').write_text((HERE/'Leaves.cs.txt').read_text('utf-8')+'class Program{'+body+(HERE/'Program.cs.txt').read_text('utf-8')+'}',encoding='utf-8')
(out/'Eligibility.cs').write_text(read('src/modules/AF.Module.Diplomacy/Application/DiplomacyConversationEligibilityApplication.cs'),encoding='utf-8')
for name,path in [('HistoryOwner','src/modules/AF.Module.Prompt/Composition/NativeMemoryHistoryMergeOwner.cs'),('HistoryRole','src/modules/AF.Module.Prompt/Composition/ConversationRoleClassificationOwner.cs')]:
 (out/(name+'.cs')).write_text(read(path),encoding='utf-8')
(out/'History.cs').write_text('using System;using System.Linq;namespace AnimusForge;'+(HERE/'History.cs.txt').read_text('utf-8'),encoding='utf-8')
(out/'PromptCompatibility.json').write_text(read('content/modules/AF.Module.Prompt/ModuleData/PromptCompatibility.json'),encoding='utf-8')
(out/'Replay.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><NoWarn>0649</NoWarn></PropertyGroup><ItemGroup><None Update="PromptCompatibility.json" CopyToOutputDirectory="Always" /></ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
dotnet=resolve_dotnet(ROOT);env=minimal_test_environment(dotnet,out)
r=subprocess.run([str(dotnet),'run','--project',str(out/'Replay.csproj'),'-c','Release'],cwd=out,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
(out/'run.log').write_text(r.stdout+r.stderr,encoding='utf-8');print(r.stdout+r.stderr)
assert all(hashlib.sha256((ROOT/p).read_bytes()).hexdigest()==h for p,h in inputs.items()),'concurrent source changed'
(out/'inputs.json').write_text(json.dumps({'hashes':inputs,'uncovered':['live provider classification','game effects','scene native exception']},indent=2),encoding='utf-8')
raise SystemExit(r.returncode)

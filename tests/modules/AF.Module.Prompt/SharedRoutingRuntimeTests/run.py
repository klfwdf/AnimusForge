import argparse,subprocess,sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
p=argparse.ArgumentParser();p.add_argument('--run-root',required=True,type=Path);p.add_argument('--mutation',choices=['mentions','forced','capture']);a=p.parse_args()
out=new_run_root(ROOT,'shared-routing-runtime',a.run_root)
names=['SharedPromptRoutingRuntime','PromptTopicRoutingStage','PromptBuildRequest','PromptRetrievalCapture','PromptRuleIdPolicy','BuiltInRuleStickyCarry','PromptBuiltInTopicRouter','PromptRuntimeTargetBinding','PromptRuleEligibility','PreprocessFormatException']
for n in names:
 text=(ROOT/f'src/modules/AF.Module.Prompt/Composition/{n}.cs').read_text(encoding='utf-8-sig')
 mutation={'mentions':('mentions.Merge(AIConfigHandler.GetLatestAuxiliaryMentionedEntitiesForExternal());',''), 'forced':('ForcedPreprocessRuleIds = request.ForcedPreprocessRuleIds?.ToArray(),','ForcedPreprocessRuleIds = request.ForcedPreprocessRuleIds,'), 'capture':('new HashSet<string>(request.ExcludedRuleIds, request.ExcludedRuleIds.Comparer)','request.ExcludedRuleIds')}
 if a.mutation and n=='SharedPromptRoutingRuntime':
  old,new=mutation[a.mutation];assert text.count(old)==1;text=text.replace(old,new,1)
 (out/f'{n}.cs').write_text(text,encoding='utf-8')
(out/'GuardrailRuleHit.cs').write_bytes((ROOT/'src/modules/AF.Module.Prompt/Retrieval/GuardrailRuleHit.cs').read_bytes())
(out/'Stubs.cs').write_bytes((ROOT/'tests/modules/AF.Module.Prompt/Composition/Stubs.cs').read_bytes())
(out/'Program.cs').write_bytes((HERE/'Harness.cs.txt').read_bytes())
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><UseAppHost>false</UseAppHost></PropertyGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=resolve_dotnet(ROOT);r=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90)
log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)

"""Captured production GCCZ replay, isolated net472/net8 outputs; no AF build or deployment."""
import argparse,hashlib,json,re,shutil,subprocess,sys
from pathlib import Path
from xml.sax.saxutils import escape
ROOT=Path(__file__).resolve().parents[4]
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
sys.path.insert(0,str(ROOT/'tools/localization'))
from inventory_untranslated import LEX,decode,HAN
parser=argparse.ArgumentParser();parser.add_argument('--run-root',type=Path);args=parser.parse_args()
out=new_run_root(ROOT,'gccz-compatibility-text-replay',args.run_root)
audit=ROOT/'artifacts/localization-audit-repair-20261012/agent-behavior/gccz-delivery-review'
manifest=json.loads((audit/'migration-manifest.json').read_text('utf-8'))
module=ROOT/'AnimusForge.SiegeAftermathIntervention';sources=sorted(module.glob('*.cs'))
resources=[ROOT/'content/modules/AnimusForge.SiegeAftermathIntervention/ModuleData'/n for n in ['CastleAftermathCompatibility.json','SiegeAftermathCompatibility.json']]
owned=[module/n for n in manifest['owned']]+[module/n for n in ['SiegeCastleCompletedInterventionSummaryBuilder.cs','SiegeInterventionMemoryEventFormatter.cs','CastleAftermathCompatibilityTexts.cs','SiegeAftermathCompatibilityTexts.cs','CompatibilityFlatJson.cs']]
paths=owned+resources+[Path(__file__).resolve(),Path(__file__).with_name('Program.cs'),audit/'migration-manifest.json']
def hashes():return {str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in paths}
before=hashes();snapshot=out/'captured';snapshot.mkdir();baseline=out/'delivered-before';baseline.mkdir()
for p in sources+resources:shutil.copyfile(p,snapshot/p.name)
for p in (audit/'before-replay-02/captured').glob('*.cs'):
 text=p.read_text('utf-8-sig').replace('namespace AnimusForge.SiegeAftermathIntervention','namespace DeliveredBefore').replace('using AnimusForge.SiegeAftermathIntervention','using DeliveredBefore')
 (baseline/p.name).write_text(text,encoding='utf-8')
for name in ['SiegeCastleCompletedInterventionSummaryBuilder.cs','SiegeInterventionMemoryEventFormatter.cs']:
 text=(audit/'before/AnimusForge.SiegeAftermathIntervention'/name).read_text('utf-8-sig').replace('namespace AnimusForge.SiegeAftermathIntervention','namespace DeliveredBefore')
 (baseline/name).write_text(text,encoding='utf-8')
for name,newname in [('SiegeCastleCompoundDispositionPlanProfile.cs','GitBaselineCompoundProfile'),('SiegeInterventionMemoryEventFormatter.cs','GitBaselineMemoryFormatter'),('SiegeCastleCompletedInterventionSummaryBuilder.cs','GitBaselineCastleSummary')]:
 raw=subprocess.check_output(['git','show','a911d4d:AnimusForge.SiegeAftermathIntervention/'+name],cwd=ROOT)
 (out/('git-'+name)).write_bytes(raw)
 text=raw.decode('utf-8-sig').replace(Path(name).stem,newname)
 if 'Compound' in name:text=text[:text.index('public sealed class SiegeCastleCompoundDispositionPlan')]
 (out/('Git-'+name)).write_text(text,encoding='utf-8')
shutil.copyfile(Path(__file__).with_name('Program.cs'),out/'Program.cs')
(snapshot/'migration-values.json').write_text(json.dumps([x['value'] for x in manifest['tokens']],ensure_ascii=False),encoding='utf-8')
# Exact reverse migration, rather than a regex-only "no Chinese" claim.
inverse=[]
for name in manifest['owned']:
 if 'Compound' in name:continue
 current=(module/name).read_bytes();text=current.decode('utf-8-sig')
 structural=[x for x in manifest['structural'] if x['file']==name]
 # Undo declarations/character scaffolding before restoring ordered string tokens.
 for x in reversed(structural):
  if x['kind']=='character-token-substitutions':continue
  assert text.count(x['after'])==1,(name,x['kind']);text=text.replace(x['after'],x['before'])
 for x in structural:
  if x['kind']=='character-token-substitutions':
   for token,identifier in x['tokens'].items():text=re.sub(r'\b'+identifier+r'\b',lambda _:token,text)
 tokens=[x for x in manifest['tokens'] if x['file']==name];bykey={}
 for x in tokens:bykey.setdefault(x['replacement'],[]).append(x['raw'])
 def restore(m):
  assert m[0] in bykey and bykey[m[0]],(name,m[0]);return bykey[m[0]].pop(0)
 text=re.sub(r'(?:Castle|Siege)AftermathCompatibilityTexts\.Raw\("[^"\n]+"\)',restore,text)
 assert all(not values for values in bykey.values()),name
 restored=(b'\xef\xbb\xbf' if current.startswith(b'\xef\xbb\xbf') else b'')+text.encode('utf-8')
 original=(audit/'before/AnimusForge.SiegeAftermathIntervention'/name).read_bytes()
 assert restored==original,('Inverse bytes differ',name)
 inverse.append({'file':name,'equal':True,'beforeSha256':hashlib.sha256(original).hexdigest()})
for p in resources:
 old=json.loads((audit/'before'/p.relative_to(ROOT)).read_text('utf-8-sig'));new=json.loads(p.read_text('utf-8-sig'))
 assert all(new.get(k)==v for k,v in old.items()),p
remaining=[]
for p in owned:
 text=p.read_text('utf-8-sig')
 for m in LEX.finditer(text):
  if m.lastgroup=='string' and (HAN.search(decode(m[0])) or re.search(r'\\u[4-9a-fA-F][0-9a-fA-F]{3}',m[0])):remaining.append([p.name,m[0]])
assert not remaining,remaining
framework=Path.home()/'.nuget/packages/microsoft.netframework.referenceassemblies.net472/1.0.3/build/.NETFramework/v4.7.2'
dotnet=resolve_dotnet(ROOT);env=minimal_test_environment(dotnet,out);results=[]
for target in ['net472','net8.0']:
 folder=out/target;folder.mkdir()
 items=''.join('<Compile Include="'+escape(str(p))+'"/>' for p in sorted(snapshot.glob('*.cs'))+sorted(baseline.glob('*.cs'))+sorted(out.glob('Git-*.cs'))+[out/'Program.cs'])
 if target=='net472':
  items+='<Reference Include="Newtonsoft.Json"><HintPath>'+escape(str(ROOT/'_deps_auto/Newtonsoft.Json.dll'))+'</HintPath></Reference><Reference Include="netstandard"><HintPath>'+escape(str(framework/'Facades/netstandard.dll'))+'</HintPath></Reference>'
 for p in resources:items+='<EmbeddedResource Include="'+escape(str(snapshot/p.name))+'" LogicalName="AnimusForge.Defaults.'+p.name+'"/>'
 extra='<AutomaticallyUseReferenceAssemblyPackages>false</AutomaticallyUseReferenceAssemblyPackages><FrameworkPathOverride>'+escape(str(framework))+'</FrameworkPathOverride>' if target=='net472' else ''
 (folder/'Replay.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><TargetFramework>'+target+'</TargetFramework><OutputType>Exe</OutputType><LangVersion>latest</LangVersion><EnableDefaultCompileItems>false</EnableDefaultCompileItems>'+extra+'</PropertyGroup><ItemGroup>'+items+'</ItemGroup></Project>',encoding='utf-8')
 (folder/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
 build=subprocess.run([str(dotnet),'build',str(folder/'Replay.csproj'),'-c','Release','--nologo'],cwd=folder,env=env,capture_output=True,text=True,encoding='utf-8',timeout=60)
 (folder/'build.log').write_text(build.stdout+build.stderr,encoding='utf-8');assert not build.returncode,build.stdout+build.stderr
 command=[str(folder/'bin/Release'/target/'Replay.exe')] if target=='net472' else [str(dotnet),str(folder/'bin/Release'/target/'Replay.dll')]
 run=subprocess.run(command+[str(snapshot),target],cwd=folder,env=env,capture_output=True,text=True,encoding='utf-8',timeout=60)
 (folder/'run.log').write_text(run.stdout+run.stderr,encoding='utf-8');print(run.stdout+run.stderr);assert not run.returncode
 results.append(json.loads((folder/'result.json').read_text('utf-8-sig')))
after=hashes();assert before==after,'Owned live inputs changed during captured replay'
(out/'summary.json').write_text(json.dumps({'ownedInputsUnchanged':True,'before':before,'after':after,'inverseEqual':inverse,'runtimeLiteralRemaining':remaining,'targets':results},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('OUTPUT',out)

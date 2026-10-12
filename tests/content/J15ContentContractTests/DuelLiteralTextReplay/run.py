"""Exact Duel formatter/settlement methods, immutable resources and real native DLLs."""
import argparse, difflib, hashlib, json, re, shutil, subprocess, sys
from pathlib import Path
from xml.sax.saxutils import escape

sys.stdout.reconfigure(encoding='utf-8')
ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
sys.path.insert(0, str(ROOT / 'tools/localization'))
from inventory_untranslated import LEX
parser = argparse.ArgumentParser()
parser.add_argument('--run-root', type=Path)
parser.add_argument('--before-root', type=Path, default=ROOT / 'artifacts/localization-audit-repair-20261012/agent-behavior/duel-literal-before')
parser.add_argument('--reference-13', type=Path, default=ROOT / '_deps_auto')
parser.add_argument('--reference-14', type=Path, default=ROOT / '.tmp/build_check/1.4')
args = parser.parse_args()
out = new_run_root(ROOT, 'duel-literal-text', args.run_root)
base = 'src/modules/AF.Module.Duel/Host/'
inputs = set()
extractions = []
def extract(relative, signature, before=False):
    path = (args.before_root if before else ROOT) / relative
    inputs.add(path)
    source = path.read_text('utf-8-sig')
    mask = LEX.sub(lambda m:' ' * len(m[0]), source)
    start = source.index(signature)
    opening = mask.index('{', start)
    depth = 0
    for end in range(opening, len(mask)):
        depth += (mask[end]=='{') - (mask[end]=='}')
        if not depth:
            result = source[start:end+1]
            extractions.append({'path':str(path),'symbol':signature,'line':source.count('\n',0,start)+1,
                'endLine':source.count('\n',0,end)+1,'sha256':hashlib.sha256(result.encode('utf-8')).hexdigest()})
            return result
    raise AssertionError(signature)

methods = ('private static TextObject DuelText(', 'private static string DuelString(',
    'private class PendingDuelDebtTag', 'private class PendingDuelStake',
    'public static void CachePendingDuelDebtTag(', 'public static bool TryConsumePendingDuelDebtTag(',
    'private static bool TryConsumePendingDuelDebtTagForOutcome(', 'private static bool TryConsumePendingDuelStake(',
    'private static bool HasStakeItems(', 'private static Dictionary<string, int> CloneStakeItems(',
    'private static Dictionary<string, int> SelectStakeItemsForSettlement(',
    'private static string BuildStakeSummaryText(', 'private static string BuildDuelStakeGoldDisplayText(',
    'private static string BuildDuelStakeItemDisplayText(', 'private static string JoinDuelStakeDisplayText(',
    'private static string BuildDuelStakeSummaryDisplayText(', 'private static string BuildDuelStakeDebtDisplayText(',
    'private static string ApplyDuelRenownPenaltyAndBuildResultText(',
    'private static string ApplyDuelStakeSettlementAndBuildResultText(')
generated = ['using System;using System.Collections.Generic;using System.Globalization;using System.Linq;using System.Text.RegularExpressions;using TaleWorlds.Localization;namespace AnimusForge {']
for before, name in ((False,'CurrentDuel'),(True,'BeforeDuel')):
    generated.append('internal static class '+name+' {')
    for signature in methods:
        body = extract(base+'DuelBehavior.cs',signature,before)
        generated.append(body.replace('DuelFactTexts.', 'BeforeDuelFactTexts.') if before else body)
    if not before:
        generated.append(extract(base+'DuelBehavior.cs','private static string DuelFormat('))
        source = (ROOT/base/'DuelBehavior.cs').read_text('utf-8-sig')
        generated.append(next(line for line in source.splitlines() if 'private static readonly Regex DuelLiteralValues =' in line))
        generated.append('internal static string Ui(string id,string template,params (string Name,object Value)[] values)=>DuelFormat(id,template,values);')
    else:
        generated.append('internal static string Ui(string id,string template,params (string Name,object Value)[] values){var text=DuelText(id,template);foreach(var v in values){if(v.Value is int number)text.SetTextVariable(v.Name,number);else text.SetTextVariable(v.Name,v.Value?.ToString()??string.Empty);}return text.ToString();}')
    generated.append('''
private static Dictionary<string,PendingDuelDebtTag> _pendingDuelDebtTags=new Dictionary<string,PendingDuelDebtTag>(StringComparer.OrdinalIgnoreCase);
private static Dictionary<string,PendingDuelStake> _pendingDuelStakes=new Dictionary<string,PendingDuelStake>();
internal static string Debt(string name,string stake,string id,string due,string note)=>BuildDuelStakeDebtDisplayText(name,stake,id,due,note);
internal static string Gold(int value)=>BuildDuelStakeGoldDisplayText(value);
internal static string Item(string name,int count)=>BuildDuelStakeItemDisplayText(name,count);
internal static string RawSummary(int gold,Dictionary<string,int> items)=>BuildStakeSummaryText(gold,items);
internal static string UiSummary(int gold,Dictionary<string,int> items)=>BuildDuelStakeSummaryDisplayText(gold,items);
internal static string Renown(Hero hero,bool won,out DuelOutcomeEffectState state)=>ApplyDuelRenownPenaltyAndBuildResultText(hero,won,out state);
internal static string Settle(Hero hero,bool won,out DuelOutcomeEffectState state)=>ApplyDuelStakeSettlementAndBuildResultText(hero,won,"replay-outcome",out state);
internal static void Seed(Hero hero,bool stake,bool debt,string note,string item,int gold=100){
_pendingDuelDebtTags.Clear();_pendingDuelStakes.Clear();
if(debt){CachePendingDuelDebtTag(hero,100,3,note);_pendingDuelDebtTags[hero.StringId].DuelOutcomeId="replay-outcome";}
if(stake)_pendingDuelStakes[hero.StringId]=new PendingDuelStake{DuelOutcomeId="replay-outcome",Gold=gold,Items=new Dictionary<string,int>{{item,3}}};
}
}''')
generated.append(extract('src/modules/AF.Module.Duel/DuelOutcomeReceipt.cs','internal enum DuelOutcomeEffectState'))
post = ROOT/'src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs'
inputs.add(post)
post_source = post.read_text('utf-8-sig')
capture = next(line.strip() for line in post_source.splitlines() if 'Regex.Match(deferredAdTag,' in line and '\\[AD:' in line)
assert 'CachePendingDuelDebtTag(targetHero, result, result2, (match.Groups[3].Value ?? "").Trim())' in post_source
generated.append('internal static class CaptureLeaf {internal static Match Parse(string deferredAdTag){'+capture+'return match;}}')
generated.append('}')
(out/'ActualMethods.cs').write_text('\n'.join(generated),encoding='utf-8')
before_reader = args.before_root/base/'DuelFactTexts.cs'
inputs.add(before_reader)
(out/'BeforeDuelFactTexts.cs').write_text(before_reader.read_text('utf-8-sig').replace('class DuelFactTexts','class BeforeDuelFactTexts'),encoding='utf-8')
shutil.copyfile(Path(__file__).with_name('Program.cs'),out/'Program.cs')

# Inverse only this slice's expression replacements and inserted helper. All other
# statements, strings, branches and numeric formatting must match the byte snapshot.
edits_path = ROOT/'artifacts/localization-audit-repair-20261012/agent-behavior/duel-literal-edits.json'
inputs.add(edits_path)
edits = json.loads(edits_path.read_text('utf-8'))['edits']
current = (ROOT/base/'DuelBehavior.cs').read_text('utf-8-sig')
restored = current
for edit in reversed(edits):
    assert edit['after'] in restored, edit['key']
    restored = restored.replace(edit['after'],edit['before'].replace('\r\n','\n'),1)
helper = extract(base+'DuelBehavior.cs','private static string DuelFormat(')
restored = restored.replace('\n\tprivate static readonly Regex DuelLiteralValues = new Regex(@"AFDUELLITERAL([0-9]+)END", RegexOptions.CultureInvariant);\n\n\t'+helper+'\n','')
restored = restored.replace('using System.Globalization;\n','',1)
baseline = (args.before_root/base/'DuelBehavior.cs').read_text('utf-8-sig')
assert restored==baseline, '\n'.join(difflib.unified_diff(baseline.splitlines(),restored.splitlines()))
original_bytes=(args.before_root/base/'DuelBehavior.cs').read_bytes()
restored_bytes=restored.encode('utf-8')
if original_bytes.startswith(b'\xef\xbb\xbf'): restored_bytes=b'\xef\xbb\xbf'+restored_bytes
assert restored_bytes==original_bytes,'Business statements/newlines/BOM must restore byte-for-byte'
raw_path = ROOT/'content/modules/AF.Module.Duel/ModuleData/DuelFactTexts.json'
raw = json.loads(raw_path.read_text('utf-8-sig'))
assert not any('{?' in value or '{:' in value for value in raw.values())
assert len(edits)==31 and sum(e['kind']=='DuelFactTexts.Format' for e in edits)==20
assert not any('DuelFactTexts.Object(' in p.read_text('utf-8-sig') for p in (ROOT/'src').rglob('*.cs'))
host = ROOT/'src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs'
inputs.add(host)
history = extract('src/AF.GameAdapter.Bannerlord/Composition/MyBehavior.cs','public static void AppendExternalDialogueHistory(')
assert 'extraFact' in history and 'AppendDialogueHistory' in history
inputs.add(raw_path)
shutil.copyfile(raw_path,out/'DuelFactTexts.json')
for path in ('content/modules/AF.Module.UI/ModuleData/Languages/af_ui_strings.xml',
             'content/modules/AF.Module.UI/ModuleData/Languages/CNs/af_ui_strings-zh-CN.xml',
             'content/modules/AF.Module.Duel/ModuleData/Languages/afduel_strings.xml',
             'content/modules/AF.Module.Duel/ModuleData/Languages/CNs/afduel_strings-zh-CN.xml',
             'content/content-map.json'):
    candidate=ROOT/path
    if candidate.exists(): inputs.add(candidate)
inputs.add(ROOT/base/'DuelFactTexts.cs')
original_contract = ROOT/'tests/content/J15ContentContractTests/DuelSettlementDisplayContractTests.py'
original_contract_before = args.before_root/'tests/content/J15ContentContractTests/DuelSettlementDisplayContractTests.py'
inputs.update((original_contract, original_contract_before))
inputs.update(Path(__file__).parent.glob('*.*'))
references={}
for api,directory in (('1.3',args.reference_13.resolve()),('1.4',args.reference_14.resolve())):
    versions=set(re.findall(r'v\d+\.\d+\.\d+\.\d+',(directory/'TaleWorlds.Library.dll').read_bytes().decode('utf-16le',errors='ignore')))
    assert len(versions)==1 and next(iter(versions)).startswith('v'+api+'.')
    references[api]={'directory':str(directory),'version':next(iter(versions))}
    inputs.update(directory/(n+'.dll') for n in ('TaleWorlds.Localization','TaleWorlds.Library','Newtonsoft.Json'))
def hashes():return {str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(inputs)}
before=hashes()
framework=Path.home()/'.nuget/packages/microsoft.netframework.referenceassemblies.net472/1.0.3/build/.NETFramework/v4.7.2'
assert (framework/'mscorlib.dll').exists()
dotnet=resolve_dotnet(ROOT);env=minimal_test_environment(dotnet,out);results=[]
for api,data in references.items():
    directory=Path(data['directory']);folder=out/api;folder.mkdir()
    compile_files=[out/'ActualMethods.cs',out/'BeforeDuelFactTexts.cs',out/'Program.cs',ROOT/base/'DuelFactTexts.cs']
    items=''.join('<Compile Include="'+escape(str(p))+'"/>' for p in compile_files)
    for name in ('TaleWorlds.Localization','TaleWorlds.Library','Newtonsoft.Json'):
        items+='<Reference Include="'+name+'"><HintPath>'+escape(str(directory/(name+'.dll')))+'</HintPath></Reference>'
    items+='<Reference Include="netstandard"><HintPath>'+escape(str(framework/'Facades/netstandard.dll'))+'</HintPath></Reference>'
    (folder/'Replay.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net472</TargetFramework><LangVersion>latest</LangVersion><NoWarn>0649</NoWarn><AutomaticallyUseReferenceAssemblyPackages>false</AutomaticallyUseReferenceAssemblyPackages><FrameworkPathOverride>'+escape(str(framework))+'</FrameworkPathOverride></PropertyGroup><ItemGroup>'+items+'</ItemGroup></Project>',encoding='utf-8')
    (folder/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>',encoding='utf-8')
    build=subprocess.run([str(dotnet),'build',str(folder/'Replay.csproj'),'-c','Release','--nologo'],cwd=folder,env=env,capture_output=True,text=True,encoding='utf-8',timeout=60)
    (folder/'build.log').write_text(build.stdout+build.stderr,encoding='utf-8')
    assert not build.returncode,build.stdout+build.stderr
    run=subprocess.run([str(folder/'bin/Release/net472/Replay.exe'),str(out),str(ROOT),api],cwd=folder,env=env,capture_output=True,text=True,encoding='utf-8',timeout=60)
    (folder/'run.log').write_text(run.stdout+run.stderr,encoding='utf-8');print(run.stdout+run.stderr)
    assert not run.returncode,(api,run.returncode)
    result=json.loads((folder/'result.json').read_text('utf-8-sig'));result['referenceVersion']=data['version'];results.append(result)
contract_run=subprocess.run([sys.executable,str(original_contract)],cwd=ROOT,capture_output=True,text=True,encoding='utf-8',timeout=30)
(out/'original-contract-after.log').write_text(contract_run.stdout+contract_run.stderr,encoding='utf-8')
print(contract_run.stdout+contract_run.stderr)
assert not contract_run.returncode,'Original settlement boundary contract failed'
contract_before=original_contract_before.read_text('utf-8-sig')
contract_after=original_contract.read_text('utf-8-sig')
assert contract_before.replace('.SetTextVariable("DUE", dueSuffix)', '("DUE", dueSuffix)')==contract_after,'Only legacy syntax assertion may be migrated'
after=hashes();assert before==after,'An input changed during replay'
(out/'inputs.json').write_text(json.dumps({'before':before,'after':after,'inputsUnchanged':True,'references':references,'extractions':extractions,'businessStatementsUnchanged':True,'businessInverseByteEquality':True,'nativeGrammarAbsent':True,'rawFormatterCallSites':20,'uiFormatterCallSites':11,'actualAdCapture':capture},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
(out/'summary.json').write_text(json.dumps({'passed':True,'runs':results,'assertions':sum(r['assertions'] for r in results),'negativeControls':sum(r['negativeControls'] for r in results),'uncovered':['live campaign transfer/debt writes','actual Hero.Name capture before the string formatting boundary','full production assemblies/Bootstrap','actual installed 1.4.8 game','every English wording semantics']},ensure_ascii=False,indent=2)+'\n',encoding='utf-8')
print('OUTPUT',out)

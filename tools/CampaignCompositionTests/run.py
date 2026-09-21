"""Execute real Campaign composition against an engine stub and frozen pre-extraction methods."""
from pathlib import Path
import argparse
import importlib.util
import json
import os
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
BASELINE = '61d578926329ace61bf6b6ae43e12bf7d89b4696'
METHODS = ['RegisterCourierFoodConsumptionModel', 'RegisterCourierMobilePartyAiModel',
           'RegisterAnimusForgeSettlementAccessModel', 'RegisterAnimusForgeSettlementLoyaltyModel']
INIT = 'protected override void InitializeGameStarter('
SOURCES = ['src/AF.GameAdapter.Bannerlord/Composition/CampaignComposition.cs', 'src/AF.GameAdapter.Bannerlord/Composition/CampaignModelComposition.cs',
            'src/AF.GameAdapter.Bannerlord/Composition/ModuleFrameworkRuntime.cs', 'src/AF.GameAdapter.Bannerlord/Composition/TeamModuleRegistration.cs',
            'src/AF.Foundation.Runtime/ModuleDirectory/ModuleDirectoryLifecycleOwner.cs',
            'src/AF.Foundation.Runtime/ModuleDirectory/InternalModuleDirectory.cs', 'Refactor/Contracts/FeatureBridgeContracts.cs',
            'src/modules/AF.Module.PublicApi/V1/AfApi.cs', 'src/AF.Contracts/PublicApi/V1/AfApiContracts.cs',
            'src/AF.Foundation.Runtime/ModuleDirectory/ModuleFrameworkSnapshot.cs', 'src/modules/AF.Module.PublicApi/Internal/AfV1SnapshotProjection.cs']

def load(name, path):
    spec = importlib.util.spec_from_file_location(name, ROOT / path)
    module = importlib.util.module_from_spec(spec); spec.loader.exec_module(module)
    return module

extract = load('campaign_decl', 'tools/ChannelCutoverBoundaryTests/run.py').declaration
util = load('campaign_dotnet', 'tools/ModuleFrameworkApiTests/run.py')

def read(path): return (ROOT / path).read_text(encoding='utf-8-sig')
def old(path): return subprocess.check_output(['git', 'show', f'{BASELINE}:{path}'], cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
def compact(s): return re.sub(r'\s+', '', s)

def restore_submodule(current):
    """Verify the live engine entry delegates once without freezing unrelated lifecycle work."""
    prior = old('SubModule.cs')
    entry = extract(current, INIT)
    assert entry.count('ModuleFrameworkRuntime.RegisterCampaign(starterObject);') == 1, 'Campaign composition entry is not unique'
    assert 'CampaignComposition.Register(' not in entry, 'SubModule bypasses the framework composition owner'
    assert entry.index('AfCampaignRuntimeLifecycle.Begin') < entry.index('ModuleFrameworkRuntime.RegisterCampaign') < entry.index('AfCampaignRuntimeLifecycle.CaptureOwners'), 'Campaign lifetime/composition order drifted'
    assert entry.count('AfCampaignRuntimeLifecycle.End(game);') == 1 and 'throw;' in entry, 'Partial-start cleanup or failure propagation drifted'
    return prior

def verify_source():
    prior = restore_submodule(read('SubModule.cs'))
    models = read(SOURCES[1]); campaign = read(SOURCES[0]); runtime = read(SOURCES[2]); team = read(SOURCES[3])
    for name in METHODS:
        sig = 'private static void ' + name + '('
        assert compact(extract(models,sig)) == compact(extract(prior,sig)), 'Changed model wrapper behavior: '+name
    init = extract(prior, INIT); original_body = init[init.index('{'):]
    current = extract(campaign,'internal static void Register(')
    body = current[current.index('{'):].replace('CampaignModelComposition.Register(campaignGameStarter);',
            '\n'.join(name+'(campaignGameStarter);' for name in METHODS))
    assert compact(body) == compact(original_body), 'Changed behavior construction/order'
    register = extract(models,'internal static void Register(')
    expected = '{'+''.join(name+'(campaignGameStarter);' for name in METHODS)+'}'
    assert compact(register[register.index('{'):]) == expected, 'Changed model registration order'
    assert compact(extract(runtime, 'internal static void RegisterCampaign(')) == compact('''internal static void RegisterCampaign(IGameStarter starterObject) { CampaignComposition.Register(starterObject); }'''), 'Parallel campaign gate/cache/owner'
    runtime = load('snapshot_inverse', 'tools/ModuleFrameworkApiTests/source_boundary.py').restore_runtime(runtime)
    before = old('Refactor/Modules/ModuleFrameworkRuntime.cs')
    for sig in ['private static void RegisterAdapter(', 'private static bool IsKnownBridge(', 'private static string GetBridgeRejectionReason(']:
        method = extract(before,sig)
        assert extract(team,sig) == method, 'Changed bridge binding policy'
        before = before.replace('    '+method+'\n\n','')
    start = before.index('                var directory = new InternalModuleDirectory(')
    end = before.index('\n                InternalModuleValidationResult',start)
    old_registration = before[start:end]
    create = extract(team,'internal static InternalModuleDirectory CreateDirectory(')
    assert compact(create[create.index('{'):]) == compact('{'+old_registration+'return directory;}'), 'Changed typed registration list'
    before = before[:start]+'                var directory = TeamModuleRegistration.CreateDirectory();'+before[end:]
    before = before.replace('    private const int InternalContractVersion = 1;\n','')
    before = before.replace('using AnimusForge.Refactor.Contracts;\n','').replace('using AnimusForge.Refactor.Runtime;','using TaleWorlds.Core;')
    before = before.replace('/// 本类不读取 Campaign/Mission，不把 adapter 已装配误报成游戏请求可执行。',
        '/// Campaign 注册另委托无状态装配清单；不读取当前 Campaign/Mission，不把 adapter 已装配误报成游戏请求可执行。')
    newblock = '''    /// <summary>
    /// 引擎 Campaign 回调的唯一委托点。目录降级不阻断原玩法初始化；不新增启动锁或去重。
    /// 这里只注册实例，不宣告存档就绪，不持有 starter/behavior，也不在 Shutdown 重放副作用。
    /// </summary>
    internal static void RegisterCampaign(IGameStarter starterObject)
    {
        CampaignComposition.Register(starterObject);
    }

'''
    assert runtime.replace(newblock,'') == before, 'Unreviewed runtime lifecycle/API delta'
    print('PASS scoped SubModule delegate + exact runtime inverse + 4 unchanged model bodies + ordered registration/bridge policy')

def main():
    p=argparse.ArgumentParser(description=__doc__); p.add_argument('--skip-mutations',action='store_true'); p.add_argument('--source-only',action='store_true')
    p.add_argument('--dotnet', default=os.environ.get('DOTNET_EXE', r'G:\AFMOD\.dotnet-sdk\dotnet.exe'))
    args=p.parse_args(); verify_source()
    if args.source_only: return 0
    out=HERE/'.generated/current'; out.mkdir(parents=True,exist_ok=True)
    (out/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>',encoding='utf-8')
    prior=old('SubModule.cs'); names=re.findall(r'AddBehavior\(new (\w+)\(\)\)',extract(prior,INIT))
    assert len(names)==36 and len(set(names))==36
    usings='using System; using AnimusForge; using AnimusForge.PolicyEffects; using AnimusForge.Refactor.Modules; using TaleWorlds.Core; using TaleWorlds.CampaignSystem; using TaleWorlds.CampaignSystem.ComponentInterfaces; using TaleWorlds.CampaignSystem.GameComponents; using AFWarStatsTerminal.Behaviors;\n'
    hosts=usings
    # Exercise the actual ModuleFrameworkRuntime/CampaignComposition implementation;
    # SubModule's surrounding game lifetime owner is verified above and in its own suite.
    current_entry = '''protected override void InitializeGameStarter(Game game, IGameStarter starterObject)
\t{
\t\tModuleFrameworkRuntime.RegisterCampaign(starterObject);
\t}'''
    for kind,text in [('Current',None),('Original',prior)]:
        entry = current_entry if kind == 'Current' else extract(text,INIT)
        hosts+='internal class '+kind+'SubModule : StubSubModule {\n'+entry+'\n'
        if kind=='Original': hosts+='\n'.join(extract(text,'private static void '+n+'(') for n in METHODS)
        hosts+='\n}\n'
    hosts+='internal static class Expected { internal static readonly string[] Behaviors = new[] {'+','.join('"'+n+'"' for n in names)+'}; }\n'
    (out/'Hosts.cs').write_text(hosts,encoding='utf-8')
    behaviors=''
    for n in names:
        ns='AFWarStatsTerminal.Behaviors' if n=='AfWarStatsBehavior' else 'AnimusForge'
        behaviors+='namespace '+ns+' { internal class '+n+' : TaleWorlds.CampaignSystem.CampaignBehaviorBase { } }\n'
    (out/'Behaviors.cs').write_text(behaviors,encoding='utf-8')
    api_stubs=read('tools/ModuleFrameworkApiTests/HostStubs.cs').split('// API tests cover assembly-directory state only;')[0]
    (out/'ApiHostStubs.cs').write_text(api_stubs,encoding='utf-8')
    common=[HERE/'HostStubs.cs',HERE/'Program.cs',out/'Hosts.cs',out/'Behaviors.cs',out/'ApiHostStubs.cs']
    sources=[ROOT/s for s in SOURCES]
    mutations={
        'drop_behavior': (SOURCES[0], '            campaignGameStarter.AddBehavior(new MyBehavior());',''),
        'reverse_models': (SOURCES[1], '        RegisterCourierFoodConsumptionModel(campaignGameStarter);\n        RegisterCourierMobilePartyAiModel(campaignGameStarter);','        RegisterCourierMobilePartyAiModel(campaignGameStarter);\n        RegisterCourierFoodConsumptionModel(campaignGameStarter);'),
        'discard_inner': (SOURCES[1], 'new CourierFoodConsumptionModel(inner)', 'new CourierFoodConsumptionModel(new DefaultMobilePartyFoodConsumptionModel())'),
        'gate_on_directory': (SOURCES[2], '        CampaignComposition.Register(starterObject);','        if (ModuleDirectoryLifecycleOwner.CaptureSnapshot().State == ModuleFrameworkLifecycleState.Ready) CampaignComposition.Register(starterObject);')}
    # Exact exception body injection, not a compilation failure.
    mutations['abort_model_failure']=(SOURCES[1], 'catch (Exception ex)\n        {','catch (Exception ex)\n        {\n            throw;')
    results=[]
    for name,mutation in [('current',None)]+([] if args.skip_mutations else list(mutations.items())):
        folder=out/name; folder.mkdir(exist_ok=True); selected=list(sources)
        if mutation:
            path,needle,replacement=mutation; text=read(path)
            assert needle in text, 'Mutation anchor drift: '+name
            mutated=folder/Path(path).name; mutated.write_text(text.replace(needle,replacement,1),encoding='utf-8')
            selected[sources.index(ROOT/path)]=mutated
        project=util.project(folder,'CampaignCompositionChecks',selected+common,executable=True)
        code,log=util.run_dotnet(args.dotnet,['run','--project',str(project),'-c','Release'],out)
        (folder/'run.log').write_text(log,encoding='utf-8')
        if not mutation:
            print(log,end=''); assert code==0, 'Current composition execution failed'
        else:
            assert code!=0 and 'FAIL ' in log and 'error CS' not in log, 'Mutation did not fail runtime assertions: '+name+'\n'+log
            print('PASS behavioral mutation rejected: '+name)
        results.append({'name':name,'exitCode':code,'expectedFailure':mutation is not None})
    (out/'results.json').write_text(json.dumps({'baseline':BASELINE,'results':results},indent=2)+'\n',encoding='utf-8')
    return 0

if __name__=='__main__': raise SystemExit(main())

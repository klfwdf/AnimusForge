"""Execute extracted Hero/Party/Merchant asset owners against boundary-only inventory fixtures."""
from __future__ import annotations
import argparse, hashlib, importlib.util, os, re, subprocess, sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('extraction', ROOT/'tools/ChannelCutoverBoundaryTests/run.py')
extractor = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extractor)

def extract(ref=None):
    owner = extractor.source('RewardSystemBehavior.cs', ref)
    hero = extractor.source('RewardSystemBehavior.EconomyReplay.cs', ref)
    blocks = {}
    methods = [extractor.declaration(hero, marker) for marker in [
        'private bool TryReplayGiveAsset(', 'private bool TryReplayGiveGold(',
        'private EconomyRewardDebtReplayResult ReplayEconomyRewardDebtPlanOnMainThread(',
        'private bool TryReplayAction(', 'private static EconomyRewardDebtReplayResult ReplayFailure(',
        'private static void LogEconomyReplayFailureSafe(', 'private sealed class EconomyMutationObservation']]
    for path, names in [
        ('RewardSystemBehavior.EconomyPartyReplay.cs', ['TryReplayPartyAsset','TryReplayPartyGold']),
        ('RewardSystemBehavior.EconomyMerchantReplay.cs', ['TryReplayMerchantAsset','TryReplayMerchantGold'])]:
        text = extractor.source(path, ref)
        methods += [extractor.declaration(text, 'private bool '+name+'(') for name in names]
    methods += [extractor.declaration(owner, marker) for marker in [
        'private static string GetRewardItemTransferKey(',
        'private static bool TryResolveExactAuthorizedRewardItem(',
        'private bool TryResolveAuthorizedHeroRewardItem(',
        'private static bool TryParseNotableMarketPromptStringId(',
        'private static bool TryParseSettlementMerchantPromptStringId(',
        'private static bool TrySelectAuthorizedRewardItem(',
        'private int ResolveAllRewardItemAmount(',
        'public static bool IsGoldAssetTokenForExternal(',
        'public static bool IsValidGeneratedRpAssetNameForExternal(']]
    blocks['METHODS'] = '\n\n'.join(methods)
    contracts = extractor.source('Refactor/Contracts/EconomyRewardDebtContracts.cs', ref)
    interaction = extractor.source('Refactor/Contracts/InteractionContracts.cs', ref)
    declarations = [extractor.declaration(contracts, marker) for marker in [
        'public static class EconomyRewardDebtCapabilityIds', 'public enum EconomyRewardDebtActionKind',
        'public sealed class EconomyRewardDebtAction', 'public sealed class EconomyRewardDebtReplayPlan',
        'public enum EconomyRewardDebtReplayStatus', 'public sealed class EconomyRewardDebtReplayResult',
        'public interface IEconomyRewardDebtMainThreadPort']]
    declarations += [extractor.declaration(extractor.source('Refactor/Adapters/LegacyEconomyRewardDebtMainThreadPort.cs', ref), 'public sealed class LegacyEconomyRewardDebtMainThreadPort')]
    declarations += [extractor.declaration(interaction, marker) for marker in [
        'public sealed class FactRecord', 'internal static class ContractGuard']]
    declarations += [extractor.declaration(extractor.source('TransferQuantitySpec.cs', ref), 'internal readonly struct TransferQuantitySpec')]
    blocks['CONTRACTS'] = '\n\n'.join(declarations)
    return blocks

def main():
    sys.stdout.reconfigure(encoding='utf-8')
    ap=argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--source-ref')
    ap.add_argument('--mutate',choices=['force-all','drop-modifier','drop-observation','drop-market-route'])
    ap.add_argument('--output-name',default='current')
    ap.add_argument('--dotnet',default=r'G:\AFMOD\.dotnet-sdk\dotnet.exe')
    args=ap.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9_-]+',args.output_name): ap.error('Invalid output name')
    blocks=extract(args.source_ref)
    if args.mutate:
        body=extractor.declaration(blocks['METHODS'],'private bool TryReplayGiveAsset(')
        mutations={
            'force-all': ('forceComplete: !quantity.IsAll && receiver == Hero.MainHero && giver != Hero.MainHero', 'forceComplete: true'),
            'drop-modifier': ('giver, receiver, lookup, requestedAmount, out itemName', "giver, receiver, lookup.Split('@')[0], requestedAmount, out itemName"),
            'drop-observation': ('mutationObservation: mutationObservation', 'mutationObservation: null'),
            'drop-market-route': ('actual = marketItem', 'actual = false'),
        }
        before,after=mutations[args.mutate]
        if before not in body: raise ValueError('Mutation anchor missing: '+before)
        blocks['METHODS']=blocks['METHODS'].replace(body,body.replace(before,after))
    code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8')
    for name, body in blocks.items(): code=code.replace('@@'+name+'@@',body)
    if '@@' in code: raise ValueError('Unexpanded template')
    output=HERE/'.generated'/args.output_name
    output.mkdir(parents=True,exist_ok=True)
    (output/'Program.cs').write_text(code,encoding='utf-8')
    (output/'AssetScope.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup></Project>')
    (output/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
    meta='source='+str(args.source_ref or 'working-tree')+' mutation='+str(args.mutate or 'none')+'\n'+'\n'.join(name+' sha256='+hashlib.sha256(body.encode()).hexdigest() for name,body in blocks.items())+'\n'
    env=os.environ.copy(); env['DOTNET_ROOT']=str(Path(args.dotnet).parent); env['DOTNET_CLI_HOME']=str(output/'cli'); env['DOTNET_NOLOGO']='1'; env['DOTNET_CLI_TELEMETRY_OPTOUT']='1'
    result=subprocess.run([args.dotnet,'run','--project',str(output/'AssetScope.csproj'),'-c','Release'],cwd=output,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
    log=meta+result.stdout+result.stderr
    (output/'run.log').write_text(log,encoding='utf-8'); print(log)
    return result.returncode

if __name__=='__main__': raise SystemExit(main())

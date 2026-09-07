"""Differential execution of the complete original and phase-split production postprocessor."""
from __future__ import annotations
import argparse, hashlib, importlib.util, os, re, subprocess, sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[2]
HERE = Path(__file__).resolve().parent
spec = importlib.util.spec_from_file_location('cutover_extraction', ROOT/'tools/ChannelCutoverBoundaryTests/run.py')
extractor = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extractor)
BASELINE = 'd40808b3'
SIGNATURE = 'private static string TryRunSceneUnifiedActionPostprocess('


def stub_helpers(old_source, method):
    """Only baseline signatures generate stubs; candidate production code cannot redefine its oracle."""
    exact = {'HasPreprocessRuleHit', 'MergePostprocessRulesForScene', 'MergeNormalizedPostprocessBlocksForScene',
             'BuildPostprocessRuleTextForScene', 'BuildSceneActionPostprocessUserPrompt', 'AppendPostprocessContextBlockForScene'}
    special = {
      'StripActionTagsForSceneSpeech': 'return text ?? "";',
      'StripSceneMechanismActionTagsForScene': 'return text.Replace("[ACTION:SCENE_MOVE:99]", "");',
      'StripAutoGroupRelaySignal': 'return text.Replace("<NormalizeAutoGroupRelayPostprocessTagsForScene>", "");',
      'NormalizePlayerNameForScenePostprocess': 'return text;',
      'IsNativeConversationPostprocessChain': 'return chainName == "native";',
      'ResolveScenePostprocessChainName': 'return "scene";',
      'RequestsAllFixedAssetsForPostprocess': 'return F.Current.AllAssets;',
      'RequestsAllOrdinaryAssetsForPostprocess': 'return F.Current.AllAssets;',
      'ShouldSuppressHeroJoinPartyPostprocessForScene': 'return F.Current.SuppressJoin;',
      'IsNpcSurrenderPostprocessContext': 'return F.Current.NpcSurrender;',
      'ResolvePartyTransferRecruitMaxTierForScene': 'return 3;',
      'TryResolveNativeConversationSiegeSurrenderContext': 'settlement = Settlement.CurrentSettlement; targetSide = BattleSideEnum.Attacker; sideLabel = "attacker"; return F.Current.SiegeSurrender;',
      'TryResolveWildernessNonHeroRewardParty': 'party = new PartyBase(); return F.Current.Party;',
      'BuildDisplayIndexedPartyTransferEntriesForScene': 'return entries.ToList();',
      'BuildDisplayIndexedSettlementTransferEntriesForScene': 'return entries.ToList();',
      'BuildAutoGroupRelayPostprocessRulesForScene': 'return enabled ? F.Rules("relay", useSingleFramedNpcDescription) : new();',
      'BuildNpcSurrenderPostprocessRulesForScene': 'return enabled ? F.Rules("npc-surrender") : new();',
      'BuildSiegeSurrenderPostprocessRulesForNativeConversation': 'return enabled ? F.Rules("siege-surrender", settlement, targetSide) : new();',
      'BuildRuntimeDiplomacyPostprocessRulesForScene': 'return F.Rules("diplomacy", targetHero, targetCharacter);',
      'KeepOnlyPersistentAdpDebtTags': 'return "<ADP_ONLY>" + tags;',
      'ContainsKingdomAnnexActionTagForLog': 'return text?.Contains("KINGDOM_ANNEX") == true;',
      'ContainsVassalageActionTagForLog': 'return text?.Contains("VASSALAGE") == true;',
      'HasSiegeSurrenderPostprocessRule': 'return rules?.Any(r => r.Tag.Contains("siege-surrender")) == true;',
      'HasNpcSurrenderPostprocessRule': 'return rules?.Any(r => r.Tag.Contains("npc-surrender")) == true;',
    }
    names = sorted(set(re.findall(r'(?<![.\w])([A-Z]\w+)\(', method)) - {'TryRunSceneUnifiedActionPostprocess'})
    blocks=[]
    for name in names:
        match=re.search(r'(?:private|public|internal) static ([^\n{;=]+?)\b'+name+r'\(([^\n]*)\)',old_source)
        if not match:
            raise ValueError('No baseline helper signature: '+name)
        signature=match.group()
        if name in exact:
            blocks.append(extractor.declaration(old_source,signature).replace('private static','protected static',1))
            continue
        ret=match.group(1).strip(); params=match.group(2)
        parts=re.split(r', (?![^<]*>)',params)
        param_names=[re.sub(r'\s*=.*','',p).strip().split()[-1] for p in parts if p.strip()]
        traced=[p for p in param_names if p not in ('party','settlement','targetSide','sideLabel') or not name.startswith('TryResolve')]
        args=', '.join(traced)
        trace=f'F.Record("{name}"'+(', '+args if args else '')+'); '
        if name in special: body=special[name]
        elif name.startswith('Normalize'): body=f'return F.Normalize("{name}"'+(', '+args if args else '')+');'
        elif ret == 'string': body=f'return F.Value("{name}"'+(', '+args if args else '')+');'
        elif ret == 'List<PostprocessRuleEntry>': body=f'return F.Rules("{name}"'+(', '+args if args else '')+');'
        elif ret == 'void': body=''
        else: raise ValueError(f'No mock policy for {ret} {name}')
        blocks.append(signature.replace('private static','protected static',1)+' { '+trace+body+' }')
    return '\n'.join(blocks)


def extract_candidate(ref):
    scene=extractor.source('ShoutBehavior.cs',ref)
    try:
        if ref:
            proc=subprocess.run(['git','show',f'{ref}:ShoutBehavior.ScenePostprocess.cs'],cwd=ROOT,capture_output=True)
            phase=proc.stdout.decode('utf-8-sig').replace('\r\n','\n') if proc.returncode==0 else ''
        else: phase=extractor.source('ShoutBehavior.ScenePostprocess.cs',ref)
    except (FileNotFoundError, subprocess.CalledProcessError): phase=''
    wrapper=extractor.declaration(scene,SIGNATURE,optional=True) or extractor.declaration(phase,SIGNATURE)
    if not phase: return wrapper, False
    blocks=[wrapper]
    for signature in ['private sealed class SceneActionPostprocessWorkItem',
                      'private static SceneActionPostprocessWorkItem PrepareSceneUnifiedActionPostprocess(',
                      'private static bool TryRequestSceneUnifiedActionPostprocess(',
                      'private static string CompleteSceneUnifiedActionPostprocess(']:
        blocks.append(extractor.declaration(phase,signature))
    return '\n'.join(blocks), True



def main():
    ap=argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--source-ref')
    ap.add_argument('--dotnet',default=r'G:\AFMOD\.dotnet-sdk\dotnet.exe')
    ap.add_argument('--output-name',default='current')
    ap.add_argument('--mutate', choices=['drop-reward','relay-as-direct','drop-rule-hits','skip-normalize','allow-recompletion'])
    args=ap.parse_args()
    if not re.fullmatch(r'[a-zA-Z0-9_-]+',args.output_name): ap.error('Invalid output name')
    old_source=extractor.source('ShoutBehavior.cs',BASELINE)
    original=extractor.declaration(old_source,SIGNATURE)
    candidate,phases=extract_candidate(args.source_ref)
    if args.mutate:
        replacements={
          'allow-recompletion': ('if (!workItem.TryBeginCompletion())', 'if (false)'),
          'drop-reward': ('rewardRuleInjected = rewardRuleInjected &&','rewardRuleInjected = false &&'),
          'relay-as-direct': ('string latestReplyBlock = replyIsDirectPlayerResponse','string latestReplyBlock = true'),
          'drop-rule-hits': ('kingdomVassalageRuleInjected = kingdomVassalageRuleInjected || kingdomVassalagePreprocessHit','kingdomVassalageRuleInjected = kingdomVassalageRuleInjected'),
          'skip-normalize': ('NormalizeDuelPostprocessTagsForScene(content, duelStakeOptions, targetHero)','""'),
        }
        a,b=replacements[args.mutate]
        if a not in candidate: raise ValueError('Mutation anchor missing: '+a)
        candidate=candidate.replace(a,b,1)
    template=(HERE/'Harness.cs.txt').read_text(encoding='utf-8')
    blocks={'BASELINE':original,'CANDIDATE':candidate,'HELPERS':stub_helpers(old_source,original)}
    for key,val in blocks.items(): template=template.replace('@@'+key+'@@',val)
    if '@@' in template: raise ValueError('Unexpanded placeholder')
    output=HERE/'.generated'/args.output_name
    output.mkdir(parents=True,exist_ok=True)
    (output/'Program.cs').write_text(template,encoding='utf-8')
    (output/'Parity.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup></Project>')
    (output/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
    meta=f'baseline={BASELINE} candidate={args.source_ref or "working-tree"} phaseSplit={phases} mutation={args.mutate or "none"}\n'+'\n'.join(k+' sha256='+hashlib.sha256(v.encode()).hexdigest() for k,v in blocks.items())
    env=os.environ.copy(); env['DOTNET_ROOT']=str(Path(args.dotnet).parent); env['DOTNET_CLI_HOME']=str(output/'cli'); env['DOTNET_CLI_TELEMETRY_OPTOUT']='1'; env['DOTNET_NOLOGO']='1'; env['DOTNET_CLI_UI_LANGUAGE']='en'
    r=subprocess.run([args.dotnet,'run','--project',str(output/'Parity.csproj'),'-c','Release'],cwd=output,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
    log=meta+'\n'+r.stdout+r.stderr
    (output/'source-fingerprints.txt').write_text(meta+'\n',encoding='utf-8')
    (output/'run.log').write_text(log,encoding='utf-8'); print(log)
    return r.returncode

if __name__=='__main__': raise SystemExit(main())

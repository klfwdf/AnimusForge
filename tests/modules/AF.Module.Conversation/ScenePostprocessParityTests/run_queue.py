"""Execute the real deferred postprocess Queue against a physical-main-thread scheduler fixture."""
import argparse
import sys as _legacy_sys
from pathlib import Path as _LegacyPath
_legacy_sys.path.insert(0, str(_LegacyPath(__file__).resolve().parents[4] / "tests"))
from af2_terminal_migration_review import historical_source
# Explicit legacy oracle; current owner build/replay inputs are not projected.
AF2_FIXTURE_METADATA = {'sourceClass': 'legacy-oracle-extraction', 'terminalBindingAndExactInverseRequired': True, 'currentOwnerReplayProjected': False}
import hashlib
import os
from pathlib import Path
import sys
sys.path.insert(0, str(Path(__file__).resolve().parents[4] / "tests"))
from output_isolation import new_run_root, minimal_test_environment
import re
import subprocess
import run

HERE=Path(__file__).resolve().parent
# cb045840 moved Scene cancellation onto the real request lifetime and transport owner scope.
QUEUE_LIFETIME_FILES=('src/modules/AF.Module.Conversation/Internal/InteractionRequestLease.cs',
    'src/modules/AF.Module.Conversation/Internal/ConversationRequestLifetime.cs',
    'src/modules/AF.Module.Llm/Transport/LlmNonStreamingTransport.cs')
QUEUE_LIFETIME_ITEMS='<ItemGroup>'+''.join('<Compile Include="'+str(Path(__file__).resolve().parents[4]/p)+'" Link="'+Path(p).name+'" />' for p in QUEUE_LIFETIME_FILES)+'</ItemGroup>'


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet',default=(os.environ.get("DOTNET_EXE") or os.environ.get("AF_DOTNET") or str(Path(__file__).resolve().parents[4] / "local/dotnet/8.0.425/dotnet.exe")))
    parser.add_argument('--source-ref')
    parser.add_argument('--mutate', choices=['ignore-generation','skip-dispatch-guard','lose-execution-context','unguarded-speech','off-thread-game-read','recapture-generation','recapture-session','submit-directive-before-guard','ignore-request-lifetime'])
    parser.add_argument('--output-name',default='queue-current')
    parser.add_argument("--run-root", type=Path)
    args=parser.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9_-]+',args.output_name): parser.error('Invalid output name')
    scene_path='src/modules/AF.Module.Conversation/Channels/Scene/ShoutBehavior.ScenePostprocess.cs'
    shared_path='src/modules/AF.Module.Conversation/Internal/Postprocess/ShoutBehavior.UnifiedActionPostprocess.cs'
    if args.source_ref:
        try: source=run.extractor.source(scene_path,args.source_ref)
        except (FileNotFoundError,subprocess.CalledProcessError): source=run.extractor.source('ShoutBehavior.ScenePostprocess.cs',args.source_ref)
        try: shared=run.extractor.source(shared_path,args.source_ref)
        except (FileNotFoundError,subprocess.CalledProcessError): shared=source
    else: source=historical_source(scene_path)
    if not args.source_ref:
        shared=run.owner_phase_source()
    snippets={
      'QUEUE':run.extractor.declaration(source,'private Task<ScenePostprocessOutcome> QueueDeferredScenePostprocessActions('),
      'OUTCOME':'\n'.join(run.extractor.declaration(source, signature) for signature in
          ['private enum ScenePostprocessStatus', 'private sealed class ScenePostprocessOutcome']),
      'WORK':'\n'.join(run.extractor.declaration(shared,sig) for sig in ('private sealed class SceneActionPostprocessWorkItem','private sealed class PostprocessNetworkRequest')),
      'COMPLETE':run.extractor.declaration(shared,'private static string CompleteSceneUnifiedActionPostprocess('),
      'REQUEST':run.extractor.declaration(shared,'private static bool TryRequestSceneUnifiedActionPostprocess('),
    }
    if args.mutate:
        changes={
          'recapture-generation':('QUEUE','expectedRuntimeGeneration > 0L ? expectedRuntimeGeneration : SaveRuntimeGuard.CaptureGeneration()','SaveRuntimeGuard.CaptureGeneration()'),
          'recapture-session':('QUEUE','expectedSceneSessionId >= 0 ? expectedSceneSessionId : Volatile.Read(ref _sceneHistorySessionId)','Volatile.Read(ref _sceneHistorySessionId)'),
          'ignore-request-lifetime':('QUEUE','return !networkCancellation.IsCancellationRequested && Volatile.Read(ref requestRetired) == 0','return Volatile.Read(ref requestRetired) == 0'),
          'ignore-generation':('QUEUE','&& SaveRuntimeGuard.IsCurrentGeneration(queuedRuntimeGeneration)','&& true'),
          'skip-dispatch-guard':('QUEUE','if (!ValidateCurrentTarget("before_dispatch"))','if (false)'),
          'submit-directive-before-guard':('QUEUE','string sceneActionDirective = ExtractSceneActionDirective(ref text, runtimeTargetAgentIndex);','string sceneActionDirective = ExtractSceneActionDirective(ref text, runtimeTargetAgentIndex); SubmitSceneActionDirective(sceneActionDirective, runtimeTargetAgentIndex, replySnapshot); sceneActionDirective = null;'),
          'lose-execution-context':('QUEUE','scope = requestExecutionContext?.CreateCopy();','scope = null;'),
          'unguarded-speech':('QUEUE','CanStillPublish,\n\t\t\t\t\t\t\tout speechCompletion','() => true,\n\t\t\t\t\t\t\tout speechCompletion'),
          'off-thread-game-read':('REQUEST','return AIConfigHandler.TryCallAuxiliaryActionPostprocess','_ = Mission.Current; return AIConfigHandler.TryCallAuxiliaryActionPostprocess'),
        }
        key,a,b=changes[args.mutate]
        if a not in snippets[key]: raise ValueError('Queue mutation anchor missing: '+a)
        snippets[key]=snippets[key].replace(a,b,1)
    signature=re.search(r'private static SceneActionPostprocessWorkItem PrepareSceneUnifiedActionPostprocess\(([^\n]+)\)',shared).group()
    parameters=signature[signature.index('(')+1:-1]
    names=[re.sub(r'\s*=.*','',p).strip().split()[-1] for p in re.split(r', (?![^<]*>)',parameters)]
    observed=', '.join('["'+n+'"] = '+n for n in names)
    snippets['PREPARE_STUB']=signature+' { Q.AssertMain("prepare"); Q.AssertRequestScope(); Q.PrepareCalls++; if(Q.Current.PrepareThrows)throw new InvalidOperationException("fixture-prepare"); Q.Prepared = new Dictionary<string,object> { '+observed+' }; return Q.Current.Immediate ? new SceneActionPostprocessWorkItem(replyText) : new SceneActionPostprocessWorkItem("system", "user", replyText, raw => { Q.AssertMain("normalize"); Q.AssertRequestScope(); Q.NormalizeCalls++; if(Q.Current.NormalizeThrows) throw new InvalidOperationException("fixture-normalize"); Q.At("before-dispatch"); return Q.Current.CompletedText; }); }'
    template=(HERE/'QueueHarness.cs.txt').read_text(encoding='utf-8-sig')
    for key,value in snippets.items(): template=template.replace('@@'+key+'@@',value)
    if '@@' in template: raise ValueError('Unexpanded queue placeholder')
    output=new_run_root(Path(__file__).resolve().parents[4], "scene-postprocess-queue-"+args.output_name, args.run_root)
    (output/'Program.cs').write_text(template,encoding='utf-8')
    (output/'Queue.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup>'+run.team_module_project_items()+QUEUE_LIFETIME_ITEMS+'</Project>')
    (output/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
    env=minimal_test_environment(Path(args.dotnet),output)
    meta='source='+(args.source_ref or 'working-tree')+' mutation='+(args.mutate or 'none')+'\n'+'\n'.join(key+' sha256='+hashlib.sha256(value.encode()).hexdigest() for key,value in snippets.items())
    result=subprocess.run([args.dotnet,'run','--project',str(output/'Queue.csproj'),'-c','Release'],cwd=output,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
    log=meta+'\n'+result.stdout+result.stderr
    (output/'run.log').write_text(log,encoding='utf-8');(output/'source-fingerprints.txt').write_text(meta,encoding='utf-8');print(log)
    return result.returncode

if __name__=='__main__':raise SystemExit(main())

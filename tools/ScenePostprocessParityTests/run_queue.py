"""Execute the real deferred postprocess Queue against a physical-main-thread scheduler fixture."""
import argparse
import hashlib
import os
from pathlib import Path
import re
import subprocess
import run

HERE=Path(__file__).resolve().parent


def main():
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--dotnet',default=r'G:\AFMOD\.dotnet-sdk\dotnet.exe')
    parser.add_argument('--source-ref')
    parser.add_argument('--mutate', choices=['ignore-generation','skip-dispatch-guard','lose-execution-context','unguarded-speech','off-thread-game-read','recapture-generation','recapture-session'])
    parser.add_argument('--output-name',default='queue-current')
    args=parser.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9_-]+',args.output_name): parser.error('Invalid output name')
    source=run.extractor.source('ShoutBehavior.ScenePostprocess.cs',args.source_ref)
    snippets={
      'QUEUE':run.extractor.declaration(source,'private Task<int> QueueDeferredScenePostprocessActions('),
      'WORK':run.extractor.declaration(source,'private sealed class SceneActionPostprocessWorkItem'),
      'COMPLETE':run.extractor.declaration(source,'private static string CompleteSceneUnifiedActionPostprocess('),
      'REQUEST':run.extractor.declaration(source,'private static bool TryRequestSceneUnifiedActionPostprocess('),
    }
    if args.mutate:
        changes={
          'recapture-generation':('QUEUE','expectedRuntimeGeneration > 0L ? expectedRuntimeGeneration : SaveRuntimeGuard.CaptureGeneration()','SaveRuntimeGuard.CaptureGeneration()'),
          'recapture-session':('QUEUE','expectedSceneSessionId >= 0 ? expectedSceneSessionId : Volatile.Read(ref _sceneHistorySessionId)','Volatile.Read(ref _sceneHistorySessionId)'),
          'ignore-generation':('QUEUE','&& SaveRuntimeGuard.IsCurrentGeneration(queuedRuntimeGeneration)','&& true'),
          'skip-dispatch-guard':('QUEUE','if (!ValidateCurrentTarget("before_dispatch"))','if (false)'),
          'lose-execution-context':('QUEUE','scope = requestExecutionContext?.CreateCopy();','scope = null;'),
          'unguarded-speech':('QUEUE','canStillPublish: CanStillPublish','canStillPublish: () => true'),
          'off-thread-game-read':('REQUEST','return AIConfigHandler.TryCallAuxiliaryActionPostprocess','_ = Mission.Current; return AIConfigHandler.TryCallAuxiliaryActionPostprocess'),
        }
        key,a,b=changes[args.mutate]
        if a not in snippets[key]: raise ValueError('Queue mutation anchor missing: '+a)
        snippets[key]=snippets[key].replace(a,b,1)
    signature=re.search(r'private static SceneActionPostprocessWorkItem PrepareSceneUnifiedActionPostprocess\(([^\n]+)\)',source).group()
    parameters=signature[signature.index('(')+1:-1]
    names=[re.sub(r'\s*=.*','',p).strip().split()[-1] for p in re.split(r', (?![^<]*>)',parameters)]
    observed=', '.join('["'+n+'"] = '+n for n in names)
    snippets['PREPARE_STUB']=signature+' { Q.AssertMain("prepare"); Q.AssertRequestScope(); Q.PrepareCalls++; if(Q.Current.PrepareThrows)throw new InvalidOperationException("fixture-prepare"); Q.Prepared = new Dictionary<string,object> { '+observed+' }; return Q.Current.Immediate ? new SceneActionPostprocessWorkItem(replyText) : new SceneActionPostprocessWorkItem("system", "user", replyText, raw => { Q.AssertMain("normalize"); Q.AssertRequestScope(); Q.NormalizeCalls++; if(Q.Current.NormalizeThrows) throw new InvalidOperationException("fixture-normalize"); Q.At("before-dispatch"); return Q.Current.CompletedText; }); }'
    template=(HERE/'QueueHarness.cs.txt').read_text(encoding='utf-8-sig')
    for key,value in snippets.items(): template=template.replace('@@'+key+'@@',value)
    if '@@' in template: raise ValueError('Unexpanded queue placeholder')
    output=HERE/'.generated'/args.output_name;output.mkdir(parents=True,exist_ok=True)
    (output/'Program.cs').write_text(template,encoding='utf-8')
    (output/'Queue.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup>'+run.team_module_project_items()+'</Project>')
    (output/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
    env=os.environ.copy();env['DOTNET_ROOT']=str(Path(args.dotnet).parent);env['DOTNET_CLI_HOME']=str(run.ROOT/'.tmp/dotnet-cli');env['DOTNET_GENERATE_ASPNET_CERTIFICATE']='false';env['DOTNET_CLI_TELEMETRY_OPTOUT']='1';env['DOTNET_NOLOGO']='1';env['DOTNET_CLI_UI_LANGUAGE']='en'
    meta='source='+(args.source_ref or 'working-tree')+' mutation='+(args.mutate or 'none')+'\n'+'\n'.join(key+' sha256='+hashlib.sha256(value.encode()).hexdigest() for key,value in snippets.items())
    result=subprocess.run([args.dotnet,'run','--project',str(output/'Queue.csproj'),'-c','Release'],cwd=output,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
    log=meta+'\n'+result.stdout+result.stderr
    (output/'run.log').write_text(log,encoding='utf-8');(output/'source-fingerprints.txt').write_text(meta,encoding='utf-8');print(log)
    return result.returncode

if __name__=='__main__':raise SystemExit(main())

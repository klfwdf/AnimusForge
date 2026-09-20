"""Run real main-reply stage + real adapter against the original source-linked phase."""
import argparse,hashlib,json,os,subprocess
from pathlib import Path
from xml.sax.saxutils import escape
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).resolve().parent
STAGE='src/modules/AF.Module.Conversation/Channels/Native/NativeConversationMainReplyStage.cs'
CONTRACTS='src/modules/AF.Module.Conversation/Channels/Native/NativeConversationMainReplyContracts.cs'
HOST='ShoutBehavior.NativeMainReply.cs'
MUTATIONS={
 'skip-generation':(STAGE,'if (host.IsGenerationStale())','if (host.IsGenerationStale() && false)'),
 'skip-target':(STAGE,'if (!validation.IsCurrent)','if (false)'),
 'skip-rollback':(STAGE,'await host.RollbackPendingPlayerHistoryAsync(reason).ConfigureAwait(false);','await Task.CompletedTask.ConfigureAwait(false);'),
 'skip-normalization':(STAGE,'output = LlmVisibleReplyNormalizer.NormalizeComplete(output);',';'),
 'skip-provider-error':(STAGE,'if (output.StartsWith("（错误") || output.StartsWith("（程序错误") || output.StartsWith("（API请求失败") || output.StartsWith("（API响应格式错误"))','if (false)'),
 'wrong-pending-key':(HOST,'_pendingHistoryKey, _pendingHistorySequence, reason);','"different-key", _pendingHistorySequence, reason);'),
 'wrong-target-port':(HOST,'() => _owner.IsNativeConversationAdmissionCurrent(_admission, out reason)','() => _owner.IsNativeConversationAdmissionCurrent(_admission, out reason) || true'),
 'wrong-failure-text':(HOST,'"自由对话正文生成失败"','"错误的失败提示"'),
}
p=argparse.ArgumentParser();p.add_argument('--mutate',choices=[*MUTATIONS,'empty-before-validation','skip-consumer-stop']);args=p.parse_args()
review=json.loads((HERE/'source-review.json').read_text(encoding='utf-8'))
original=subprocess.check_output(['git','show',review['baseline']+':ShoutBehavior.cs'],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n')
evidence=review['files']['ShoutBehavior.cs'];assert hashlib.sha256(original.encode()).hexdigest()==evidence['beforeSha256'];old=evidence['edits'][0]['before'];assert original.count(old)==1
out=HERE/'.generated'/(args.mutate or 'current');out.mkdir(parents=True,exist_ok=True)
consumer=evidence['edits'][0]['after'];live=(ROOT/'ShoutBehavior.cs').read_text(encoding='utf-8-sig');assert live.count(consumer)==1
# Observe the typed status without replacing the actual caller's continuation/stop branch.
consumer=consumer.replace('if (!nativeMainReply.CanContinue)', 'Fixture.LastStatus=nativeMainReply.Status;\n        if (!nativeMainReply.CanContinue)',1)
if args.mutate=='skip-consumer-stop':consumer=consumer.replace('if (!nativeMainReply.CanContinue)','if (false)',1)
code=(HERE/'Harness.cs.txt').read_text(encoding='utf-8-sig').replace('@@ORIGINAL_STAGE@@',old).replace('@@CURRENT_CONSUMER@@',consumer);assert '@@' not in code;(out/'Program.cs').write_text(code,encoding='utf-8')
for path in [STAGE,CONTRACTS,HOST,'src/modules/AF.Module.Llm/Protocol/LlmVisibleReplyNormalizer.cs']:
 s=(ROOT/path).read_text(encoding='utf-8-sig')
 if args.mutate in MUTATIONS and path==MUTATIONS[args.mutate][0]:
  _,before,after=MUTATIONS[args.mutate];assert s.count(before)==1;s=s.replace(before,after,1)
 if args.mutate=='empty-before-validation' and path==STAGE:
  empty='''        if (string.IsNullOrWhiteSpace(output))
            return new NativeConversationMainReplyResult(NativeConversationMainReplyStatus.EmptyReply, "");
''';anchor='        NativeConversationReplyTargetValidation validation =';assert s.count(empty)==1 and s.count(anchor)==1;s=s.replace(empty,'',1).replace(anchor,empty+anchor,1)
 (out/Path(path).name).write_text(s,encoding='utf-8')
newton=Path(os.environ.get('AF_NEWTONSOFT') or ROOT/'local/dotnet/8.0.425/sdk/8.0.425/Newtonsoft.Json.dll');assert newton.is_file(),newton
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup><ItemGroup><Reference Include="Newtonsoft.Json"><HintPath>'+escape(str(newton))+'</HintPath></Reference></ItemGroup></Project>',encoding='utf-8')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
dotnet=Path(os.environ.get('AF_DOTNET') or ROOT/'local/dotnet/8.0.425/dotnet.exe');env=os.environ.copy();env.update(DOTNET_ROOT=str(dotnet.parent),DOTNET_CLI_HOME=str(ROOT/'.tmp/dotnet-cli'),NUGET_PACKAGES=str(ROOT/'.tmp/nuget-packages'),DOTNET_SKIP_FIRST_TIME_EXPERIENCE='1',DOTNET_CLI_TELEMETRY_OPTOUT='1')
r=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=150)
log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log);raise SystemExit(r.returncode)

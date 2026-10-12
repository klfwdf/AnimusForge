from pathlib import Path
import subprocess,tempfile,shutil,sys
R=Path(__file__).resolve().parents[4];H=Path(__file__).resolve().parent
sys.path.insert(0,str(R/'tools/localization'))
from inventory_untranslated import LEX
s=(R/'src/modules/AF.Module.Prompt/Composition/SceneHistoryMessageAssemblyOwner.cs').read_text('utf-8-sig')
start=s.index('internal static string FormatScenePlayerDirectSpeechLabel(');mask=LEX.sub(lambda m:' '*len(m.group()),s);opening=mask.index('{',start);depth=0
for end in range(opening,len(mask)):
 depth+=(mask[end]=='{')-(mask[end]=='}')
 if not depth:break
out=R/'artifacts/localization/prompt-residual-test-generated';out.mkdir(exist_ok=True)
(H/'Projection.cs').write_text('using System; namespace AnimusForge; internal static class Projection {'+s[start:end+1]+'}',encoding='utf-8')
main=(R/'src/modules/AF.Module.Prompt/Composition/MainPromptMessageAssemblyOwner.cs').read_text('utf-8-sig')
mainmask=LEX.sub(lambda m:' '*len(m.group()),main); bodies=[]
for signature in ['internal static object CreateCourierChatMessage(', 'internal static bool TryConvertCourierMemoryMessageToChatMessage(', 'internal static string BuildCourierMemoryMetadataPrefix(', 'internal static string StripCourierPromptScopeLabel(', 'internal static string StripCourierSpeakerPrefix(']:
 start=main.index(signature);opening=mainmask.index('{',start);depth=0
 for end in range(opening,len(mainmask)):
  depth+=(mainmask[end]=='{')-(mainmask[end]=='}')
  if not depth:break
 bodies.append(main[start:end+1])
(H/'CourierProjection.cs').write_text('using System; namespace AnimusForge; internal static class CourierProjection {'+'\n'.join(bodies)+'}',encoding='utf-8')
config=(R/'src/modules/AF.Module.Prompt/Configuration/AIConfigHandler.cs').read_text('utf-8-sig')
configmask=LEX.sub(lambda m:' '*len(m.group()),config)
start=config.index('public static string BuildActionPostprocessLatestReplyBlock(');opening=configmask.index('{',start);depth=0
for end in range(opening,len(configmask)):
 depth+=(configmask[end]=='{')-(configmask[end]=='}')
 if not depth:break
(H/'LatestReplyProjection.cs').write_text('using System; using System.Text; namespace AnimusForge; internal static class LatestReplyProjection {'+config[start:end+1]+'private static string NormalizeActionPostprocessDialogueText(string text)=>(text ?? "").Trim(); private static string ExtractLatestPlayerUtteranceForActionPostprocess(string text)=>text ?? "";}',encoding='utf-8')
subprocess.run(['dotnet','build',str(H/'ResidualLocalization.csproj'),'--nologo','-v:q'],check=True)
dll=subprocess.check_output(['dotnet','msbuild',str(H/'ResidualLocalization.csproj'),'-getProperty:TargetPath'],text=True).strip()
with tempfile.TemporaryDirectory(dir=out,prefix='packs-') as scratch:
 for lang in ['zh-CN','en']:
  p=Path(scratch)/lang;p.mkdir();shutil.copy2(R/('content/modules/AF.Module.Prompt/ModuleData/PromptComposerTexts.'+lang+'.json'),p/'PromptComposerTexts.json')
  subprocess.run(['dotnet',dll,str(p),lang],check=True)

"""Source-linked gate + exact onboarding callback replay; UI/Campaign boundaries are fakes, not a game run."""
import argparse, subprocess, sys, uuid
from pathlib import Path
ROOT=Path(__file__).resolve().parents[4]
HERE=Path(__file__).resolve().parent
parser=argparse.ArgumentParser();parser.add_argument('--dotnet',default='dotnet');parser.add_argument('--baseline',action='store_true');args=parser.parse_args()
out=ROOT/'artifacts'/'persona-startup-tests'/('baseline-' if args.baseline else 'candidate-')/uuid.uuid4().hex
out.mkdir(parents=True)
def read(path):
 return subprocess.check_output(['git','show','4ed61998:'+path],cwd=ROOT).decode('utf-8-sig') if args.baseline else (ROOT/path).read_text(encoding='utf-8-sig')
controller=read('src/AF.GameAdapter.Bannerlord/UI/CampaignSaveExitController.cs')
source=read('src/modules/AF.Module.Onboarding/Host/ModOnboardingBehavior.cs')
a=source.index('private enum OnboardingUiStage');b=source.index('\n\t}',a)+len('\n\t}')
enum=source[a:b]
a=source.index('internal bool IsSetupUiActive');b=source.index(';',a)+1
prop=source[a:b]
a=source.index('private void CompleteOnboardingAndOpenPlayerPersonaSetup(');b=source.index('private void ShowPeaceSceneConflictChoiceAfterPersona(',a)
method=source[a:b]
(out/'OnboardingSlice.cs').write_text('using System; using TaleWorlds.CampaignSystem; using TaleWorlds.Library; using TaleWorlds.Core; namespace AnimusForge { public partial class ModOnboardingBehavior { '+enum+'\n'+prop+'\n'+method+' } }',encoding='utf-8')
(out/'Controller.cs').write_text(controller,encoding='utf-8')
(out/'Guard.cs').write_text((ROOT/'src/AF.Foundation.Runtime/Lifecycle/SaveRuntimeGuard.cs').read_text(encoding='utf-8'),encoding='utf-8')
for name in ['Program.cs','Stubs.cs']:(out/name).write_bytes((HERE/name).read_bytes())
(out/'Replay.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NuGetAudit>false</NuGetAudit><UseAppHost>false</UseAppHost></PropertyGroup></Project>')
result=subprocess.run([args.dotnet,'run','--project',str(out/'Replay.csproj'),'-c','Release'],cwd=ROOT,capture_output=True,text=True,encoding='utf-8',errors='replace')
(out/'run.log').write_text(result.stdout+result.stderr,encoding='utf-8')
print(result.stdout);print(result.stderr);print('Evidence: '+str(out));sys.exit(result.returncode)

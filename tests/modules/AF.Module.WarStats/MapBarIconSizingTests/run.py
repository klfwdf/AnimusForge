"""Actual AF icon loader + vanilla IconBrushWidget, with native texture/UI-resource substitutes."""
from pathlib import Path
import argparse, subprocess, sys, hashlib, json
from xml.sax.saxutils import escape
ROOT=Path(__file__).resolve().parents[4];HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root,resolve_dotnet,minimal_test_environment
parser=argparse.ArgumentParser();parser.add_argument('--run-root',type=Path,required=True);parser.add_argument('--baseline',action='store_true');a=parser.parse_args()
out=new_run_root(ROOT,'terminal-mapbar-icon-sizing',a.run_root)
path='WarStats/AfTerminalMapBarIconSprite.cs'
source=(ROOT/path).read_text(encoding='utf-8-sig')
if a.baseline: source=subprocess.check_output(['git','show','a267f67b4:'+path],cwd=ROOT).decode('utf-8-sig')
(out/'Icon.cs').write_text(source,encoding='utf-8')
vanilla=ROOT/'原版游戏本体代码1.4.5/TaleWorlds.MountAndBlade.GauntletUI.Widgets/TaleWorlds/MountAndBlade/GauntletUI/Widgets/IconBrushWidget.cs'
(out/'IconBrushWidget.cs').write_text(vanilla.read_text(encoding='utf-8-sig'),encoding='utf-8')
(out/'Harness.cs').write_text((HERE/'Harness.cs.txt').read_text(encoding='utf-8'),encoding='utf-8')
(out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup></Project>')
(out/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
(out/'fixture/GUI/SpriteParts/af_terminal').mkdir(parents=True)
asset=ROOT/'content/modules/AF.Module.WarStats/GUI/SpriteParts/af_terminal/af_terminal_icon.png'
(out/'fixture/GUI/SpriteParts/af_terminal/af_terminal_icon.png').write_bytes(asset.read_bytes())
dotnet=resolve_dotnet(ROOT)
r=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release','--',str(out/'fixture')],cwd=out,env=minimal_test_environment(dotnet,out),capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
log=r.stdout+r.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log)
(out/'source-witness.json').write_text(json.dumps({'productionSha256':hashlib.sha256(source.encode()).hexdigest(),'vanillaWidgetPath':str(vanilla),'vanillaWidgetSha256':hashlib.sha256(vanilla.read_bytes()).hexdigest()},indent=2),encoding='utf-8')
raise SystemExit(r.returncode)

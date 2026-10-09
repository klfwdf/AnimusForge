"""Replay unchanged production model-setting spans with real MCM Dropdown/converter.

The host path/logger are isolated fakes. Property ordering mirrors MCM's converter;
this is not a rendered MCM or a complete game settings-provider test.
"""
from pathlib import Path
import argparse, hashlib, importlib.util, json, re, subprocess, sys
from xml.sax.saxutils import escape
ROOT=Path(__file__).resolve().parents[3]
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
p=argparse.ArgumentParser();p.add_argument('--baseline');p.add_argument('--mcm',type=Path,default=ROOT/'.tmp/build_check/1.4/MCMv5.dll');args=p.parse_args()
out=new_run_root(ROOT,'model-presets',None);dotnet=resolve_dotnet(ROOT)
spec=importlib.util.spec_from_file_location('ex',ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py');ex=importlib.util.module_from_spec(spec);spec.loader.exec_module(ex)
def read(path):
    return subprocess.check_output(['git','show',args.baseline+':'+path],cwd=ROOT).decode('utf-8-sig') if args.baseline else (ROOT/path).read_text(encoding='utf-8-sig')
src=read('src/AF.GameAdapter.Bannerlord/Configuration/Mcm/DuelSettings.cs')
town=read('src/AF.GameAdapter.Bannerlord/Configuration/Mcm/TownAmbientSettings.cs')
ui=read('src/AF.GameAdapter.Bannerlord/UI/Common/McmDropdownRuntimeRefresh.cs')
parts=[]
def block(s,a,b): parts.append(s[s.index(a):s.index(b)])
block(src,'private sealed class ModelDropdownCacheSnapshot','private Dropdown<string> _shoutInputUiBackgroundDropdown')
block(src,'private bool _modelDropdownCacheHydrated','private const string UnsupportedContextExtractionApiWarningMessage')
block(src,'private void EnsureModelDropdownCacheHydrated()','public static string NormalizeShoutInputUiBackground(')
block(src,'private static List<string> BuildModelOptionList(','private static string BuildModelListApiUrl(')
for name in ['Main','Auxiliary','ActionPostprocess','EventAndRebellion','TownAmbientAi']:
    s=town if name=='TownAmbientAi' else src
    prop='ModelName' if name=='Main' else name+'ModelName'
    # Include the entire existing attributed property, excluding adjacent buttons.
    a=s.rfind('[SettingPropertyText',0,s.index('public string '+prop+' ' if args.baseline else 'public string '+prop+'\n'))
    b=s.index('[SettingPropertyButton',a);parts.append(s[a:b])
    a=s.rfind('[SettingPropertyDropdown',0,s.index('public Dropdown<string> '+name+'ModelDropdown'))
    b=s.index('[SettingPropertyButton',a);parts.append(s[a:b])
    selected='GetTownAmbientAiSelectedModelOption' if name=='TownAmbientAi' else 'Get'+name+'SelectedModelOption'
    parts.append(ex.declaration(s,'public string '+selected+'('))
    effective='GetEffective'+name+'ModelName'
    parts.append(ex.declaration(s,'public string '+effective+'('))
code='\n'.join(parts)
prefix='internal static void ModelPresetIndexPrefix(IRef __0, ref object __1) {}' if args.baseline else ex.declaration(ui,'internal static void ModelPresetIndexPrefix(')
header='using System; using System.IO; using System.Text; using System.Linq; using System.Collections.Generic; using MCM.Common; using MCM.Abstractions.Attributes; using MCM.Abstractions.Attributes.v2; using Newtonsoft.Json;\n'
(out/'Production.cs').write_text(header+'namespace AnimusForge { public class DuelSettings {\n'+code+'\n} public static class McmDropdownRuntimeRefresh { '+prefix+' } }',encoding='utf-8')
(out/'Program.cs').write_text((HERE/'Program.cs').read_text(encoding='utf-8'),encoding='utf-8')
assets=json.loads((ROOT/'tools/Coup.RuntimeProbe/obj/project.assets.json').read_text(encoding='utf-8-sig'))
refdir=next(Path(x)/'microsoft.netframework.referenceassemblies.net472/1.0.3/build/.NETFramework/v4.7.2' for x in assets['packageFolders'] if (Path(x)/'microsoft.netframework.referenceassemblies.net472/1.0.3/build/.NETFramework/v4.7.2/mscorlib.dll').exists())
deps=[args.mcm.resolve(),args.mcm.parent/'Newtonsoft.Json.dll',refdir/'Facades/netstandard.dll']+list(args.mcm.parent.glob('0Harmony.dll'))+list(args.mcm.parent.glob('Mono*.dll'))
deps+=list(args.mcm.parent.glob('Bannerlord.MBOptionScreen.v*.dll'))
refs=''.join(f'<Reference Include="{x.stem}"><HintPath>{escape(str(x.resolve()))}</HintPath></Reference>' for x in deps)
(out/'Test.csproj').write_text(f'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net472</TargetFramework><PlatformTarget>x64</PlatformTarget><LangVersion>10</LangVersion><Nullable>disable</Nullable><AutomaticallyUseReferenceAssemblyPackages>false</AutomaticallyUseReferenceAssemblyPackages><FrameworkPathOverride>{escape(str(refdir))}</FrameworkPathOverride></PropertyGroup><ItemGroup>{refs}</ItemGroup></Project>',encoding='utf-8')
env=minimal_test_environment(dotnet,out)
result=subprocess.run([str(dotnet),'build',str(out/'Test.csproj')],cwd=out,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace')
(out/'build.log').write_text(result.stdout+result.stderr,encoding='utf-8')
if result.returncode: print(result.stdout+result.stderr);sys.exit(result.returncode)
result=subprocess.run([str(out/'bin/Debug/net472/Test.exe')],cwd=out,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace')
(out/'result.log').write_text(result.stdout+result.stderr,encoding='utf-8')
(out/'receipt.json').write_text(json.dumps({'baseline':args.baseline,'exit':result.returncode,'production_sha256':hashlib.sha256(code.encode()).hexdigest(),'mcm_sha256':hashlib.sha256(args.mcm.read_bytes()).hexdigest()},indent=2),encoding='utf-8')
print(result.stdout+result.stderr);print(out);sys.exit(result.returncode)

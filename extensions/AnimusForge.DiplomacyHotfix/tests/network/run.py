"""Real old AF transport sources + controlled sender; no game or provider is started."""
import argparse
import hashlib
import json
import pathlib
import subprocess
import xml.etree.ElementTree as ET

ROOT = pathlib.Path(__file__).resolve().parents[4]
HERE = pathlib.Path(__file__).resolve().parent
EXT = HERE.parents[1]
parser = argparse.ArgumentParser()
parser.add_argument('--harmony', required=True)
parser.add_argument('--run-root', required=True, type=pathlib.Path)
args = parser.parse_args()
out = args.run_root.resolve()
out.mkdir(parents=True, exist_ok=False)
project = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
props = ET.SubElement(project,'PropertyGroup')
for key, val in {'OutputType':'Exe','TargetFramework':'net472','LangVersion':'latest','PlatformTarget':'x64',
                 'ImplicitUsings':'enable','EnableDefaultCompileItems':'false','NuGetAudit':'false'}.items():
    ET.SubElement(props,key).text=val
items=ET.SubElement(project,'ItemGroup')
ET.SubElement(items,'PackageReference',Include='Microsoft.NETFramework.ReferenceAssemblies.net472',Version='1.0.3',PrivateAssets='all')
draft = ROOT/'tests/modules/AF.Module.Diplomacy/PlayerDocumentDraftTests'
sources = set()
for node in ET.parse(draft/'PlayerDocumentDraftTests.csproj').getroot().findall('./ItemGroup/Compile'):
    path=(draft/node.attrib['Include']).resolve()
    if path.name in ('WorldDiplomacyPlayerDraftApplication.cs','WorldDiplomacyComposePopupVM.cs','WorldDiplomacyComposePopupVM.AutoDraft.cs'):
        continue
    if path.name in ('WorldDiplomacyLlmClient.cs','LegacyWorldDiplomacyLlmGateway.cs'):
        old=subprocess.check_output(['git','show','720b063:'+path.relative_to(ROOT).as_posix()],cwd=ROOT)
        target=out/path.name;target.write_bytes(old);sources.add(target)
    else:sources.add(path)
fixtures=(draft/'Fixtures.cs').read_text(encoding='utf-8-sig').replace('internal static class WorldDiplomacyBehavior','internal static partial class WorldDiplomacyBehavior')
(out/'Fixtures.cs').write_text(fixtures,encoding='utf-8');sources.add(out/'Fixtures.cs')
program=(draft/'Program.cs').read_text(encoding='utf-8-sig')
(out/'ControlledSender.cs').write_text(program[:program.index('internal static class Program')],encoding='utf-8');sources.add(out/'ControlledSender.cs')
replay=(draft/'PreparedDiplomacyGatewayReplay.cs').read_text(encoding='utf-8-sig')
replay='using System.Net.Http;\n'+replay
replay=replay.replace('new LegacyWorldDiplomacyLlmGateway(request, admit)', 'new LegacyWorldDiplomacyLlmGateway(admit)')
replay=replay.replace('new LegacyWorldDiplomacyLlmGateway(request, () =>', 'new LegacyWorldDiplomacyLlmGateway(() =>')
replay=replay.replace('Task.Run(() => gateway.GenerateAsync(', 'Dispatch(() => gateway.GenerateAsync(')
replay=replay.replace('Test.That(DuelSettings.Reads == reads && sends == 2,', 'Test.That(DuelSettings.Reads == reads + 1 && sends == 2,')
replay=replay.replace('handler.Get(0) == null && DuelSettings.Reads == reads,', 'handler.Get(0) == null && DuelSettings.Reads == reads + 1,')
position=replay.index('    private static ControlledHandler Handler()')
helper='''    private static Task<LlmGenerateResult> Dispatch(Func<Task<LlmGenerateResult>> work)
    {
        var done = new TaskCompletionSource<LlmGenerateResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        var snapshot = new WorldDiplomacyRequestSnapshot { Work = async () => {
            try { done.SetResult(await work().ConfigureAwait(false)); }
            catch (Exception ex) { done.SetException(ex); }
        }};
        new WorldDiplomacyBehavior.LlmDispatchSource().StartRequest(snapshot, new JArray());
        return done.Task;
    }
'''
replay=replay[:position]+helper+replay[position:]
(out/'PreparedReplay.cs').write_text(replay,encoding='utf-8');sources.add(out/'PreparedReplay.cs')
(out/'Entry.cs').write_text((HERE/'Entry.cs.txt').read_text(),encoding='utf-8');sources.add(out/'Entry.cs')
for path in sorted(sources):ET.SubElement(items,'Compile',Include=str(path))
for node in ET.parse(draft/'PlayerDocumentDraftTests.csproj').getroot().findall('./ItemGroup/EmbeddedResource'):
    attrs=dict(node.attrib);attrs['Include']=str((draft/attrs['Include']).resolve());ET.SubElement(items,'EmbeddedResource',**attrs)
for name, path in {'0Harmony':pathlib.Path(args.harmony).resolve(),'Newtonsoft.Json':ROOT/'_deps_auto/Newtonsoft.Json.dll',
                   'AnimusForge.DiplomacyHotfix':EXT/'artifacts/1.3/AnimusForge.DiplomacyHotfix.dll'}.items():
    ref=ET.SubElement(items,'Reference',Include=name);ET.SubElement(ref,'HintPath').text=str(path)
ET.SubElement(items,'Reference',Include='System.Net.Http')
ET.ElementTree(project).write(out/'Network.csproj',encoding='utf-8',xml_declaration=True)
(out/'source-manifest.json').write_text(json.dumps({str(p):hashlib.sha256(p.read_bytes()).hexdigest() for p in sorted(sources)},indent=2),encoding='utf-8')
for name, command in [('build',['dotnet','build',str(out/'Network.csproj'),'-v:q']),
                      ('positive',[str(out/'bin/Debug/net472/Network.exe')]),
                      ('negative',[str(out/'bin/Debug/net472/Network.exe'),'negative'])]:
    r=subprocess.run(command,cwd=ROOT,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
    (out/(name+'.log')).write_text(r.stdout+r.stderr,encoding='utf-8')
    print(name,r.returncode,(r.stdout+r.stderr)[-1800:])
    if name=='negative':
        assert r.returncode!=0 and 'strict MCM thread checks' in r.stderr
    else:assert r.returncode==0

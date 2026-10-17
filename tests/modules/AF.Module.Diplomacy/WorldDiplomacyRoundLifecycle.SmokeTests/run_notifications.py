"""Run the existing production notification-owner replay in an isolated compile set."""
from pathlib import Path
import argparse
import hashlib
import json
import os
import subprocess
import xml.etree.ElementTree as ET

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]
parser = argparse.ArgumentParser()
parser.add_argument('--out', type=Path, required=True)
parser.add_argument('--baseline', action='store_true')
args = parser.parse_args()
out = args.out.resolve()
out.mkdir(parents=True, exist_ok=False)

original = ET.parse(HERE / 'WorldDiplomacyRoundLifecycle.SmokeTests.csproj').getroot()
sources = {(HERE / node.attrib['Include']).resolve()
           for node in original.findall('./ItemGroup/Compile') if 'Include' in node.attrib}
# Explicit top-level membership excludes stale bin/obj generated C# and the separate
# persistence attribute substitutes; the fixture uses the real Newtonsoft serializer.
sources.update(path.resolve() for path in HERE.glob('*.cs') if path.name != 'PersistenceRecordStubs.cs')
sources.add((HERE / 'SmokeLocalizationEnvironment.cs').resolve())
notification = ROOT / 'src/modules/AF.Module.Diplomacy/Application/WorldDiplomacyNotificationApplication.cs'
if args.baseline:
    previous = subprocess.check_output(['git', 'show', 'HEAD:' + notification.relative_to(ROOT).as_posix()], cwd=ROOT)
    (out / 'BaselineNotification.cs').write_bytes(previous)
    sources.remove(notification.resolve())
    sources.add(out / 'BaselineNotification.cs')

project = ET.Element('Project', Sdk='Microsoft.NET.Sdk')
properties = ET.SubElement(project, 'PropertyGroup')
for name, value in {'OutputType': 'Exe', 'TargetFramework': 'net6.0',
                    'ImplicitUsings': 'enable', 'Nullable': 'enable',
                    'LangVersion': 'latest',
                    'EnableDefaultCompileItems': 'false', 'StartupObject': 'NoticeReplay',
                    'WarningLevel': '0'}.items():
    ET.SubElement(properties, name).text = value
items = ET.SubElement(project, 'ItemGroup')
for path in sorted(sources):
    ET.SubElement(items, 'Compile', Include=str(path))
ET.SubElement(items, 'Compile', Include='NoticeReplay.cs')
for name in ('TaleWorlds.Localization', 'TaleWorlds.Library'):
    reference = ET.SubElement(items, 'Reference', Include=name)
    ET.SubElement(reference, 'HintPath').text = str(
        Path('F:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord/bin/Win64_Shipping_Client') / (name + '.dll'))
ET.SubElement(items, 'PackageReference', Include='Newtonsoft.Json', Version='13.0.3')
for item_name in ('Content', 'EmbeddedResource'):
    for source in original.findall('./ItemGroup/' + item_name):
        include = source.attrib.get('Include')
        if not include:
            continue
        attributes = {name: value for name, value in source.attrib.items() if name != 'Include'}
        attributes['Include'] = str((HERE / include).resolve())
        target = ET.SubElement(items, item_name, attributes)
        for metadata in source:
            copied = ET.SubElement(target, metadata.tag, metadata.attrib)
            copied.text = metadata.text
ET.ElementTree(project).write(out / 'NotificationReplay.csproj', encoding='utf-8', xml_declaration=True)
(out / 'NoticeReplay.cs').write_text('''using AnimusForge;
internal static class NoticeReplay {
 static int Main() {
  SmokeLocalizationEnvironment.Initialize();
  Dpl090PresentationReplay.Run();
  System.Console.WriteLine("PASS production notification/presentation replay: " + Test.Assertions + " assertions; live widget NOT-RUN");
  return 0;
 }
}
''', encoding='utf-8')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>', encoding='utf-8')
(out / 'source-manifest.json').write_text(json.dumps({str(path): hashlib.sha256(path.read_bytes()).hexdigest()
                                                     for path in sorted(sources)}, indent=2), encoding='utf-8')
commands = [('build.log', ['dotnet', 'build', str(out / 'NotificationReplay.csproj'), '--nologo', '-v:q',
                          '-p:RestoreConfigFile=' + str(out / 'NuGet.Config')]),
            ('run.log', ['dotnet', str(out / 'bin/Debug/net6.0/NotificationReplay.dll')])]
for log_name, command in commands:
    environment = os.environ.copy()
    environment['AF_SMOKE_UI_MODULE'] = str(ROOT / 'content/modules/AF.Module.Diplomacy')
    environment['AF_SMOKE_UI_LANGUAGE'] = 'English'
    result = subprocess.run(command, cwd=ROOT, env=environment, capture_output=True, text=True, encoding='utf-8', errors='replace')
    text = result.stdout + result.stderr
    (out / log_name).write_text(text, encoding='utf-8')
    print(text, end='')
    if result.returncode:
        raise SystemExit(result.returncode)
print('OUTPUT', out)

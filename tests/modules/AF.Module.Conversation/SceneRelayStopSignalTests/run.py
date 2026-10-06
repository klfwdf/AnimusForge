"""Execute the unchanged production relay parser with detached NPC id fixtures."""
import argparse
import hashlib
import importlib.util
from pathlib import Path
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[4]
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

parser = argparse.ArgumentParser()
parser.add_argument('--run-root', type=Path, required=True)
parser.add_argument('--source-ref')
args = parser.parse_args()
out = new_run_root(ROOT, 'scene-relay-stop', args.run_root)
path = 'src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs'
source = (subprocess.check_output(['git', 'show', args.source_ref + ':' + path], cwd=ROOT).decode('utf-8-sig')
          if args.source_ref else (ROOT / path).read_text(encoding='utf-8-sig'))
spec = importlib.util.spec_from_file_location('extractor', ROOT / 'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
extractor = importlib.util.module_from_spec(spec)
spec.loader.exec_module(extractor)
method = extractor.declaration(source, 'internal static string NormalizeAutoGroupRelayPostprocessTagsForScene(')
harness = r'''
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
class NpcDataPacket { public int AgentIndex; }
class Program {
METHOD
static void Main() {
    int passed = 0;
    void Check(string raw, int current, string expected) {
        var candidates = new List<NpcDataPacket> { new NpcDataPacket { AgentIndex = 9 }, new NpcDataPacket { AgentIndex = 11 } };
        string actual = NormalizeAutoGroupRelayPostprocessTagsForScene(raw, candidates, current);
        if (actual != expected) throw new Exception($"raw={raw}; current={current}; expected={expected}; actual={actual}");
        // Production normalizes once in the shared owner, then again in deferred dispatch.
        if (NormalizeAutoGroupRelayPostprocessTagsForScene(actual, candidates, current) != expected)
            throw new Exception("second normalization lost relay result");
        passed++;
    }
    foreach (int speaker in new[] { 1, 4, 6, 12 }) Check($"[RELAY:{speaker}]", speaker, $"[RELAY:{speaker}]");
    Check("[RELAY:9]", 1, "[RELAY:9]");
    Check("[RELAY:11]", 9, "[RELAY:11]");
    Check("[RELAY:999]", 1, "");
    Check("[RELAY:-1]", 1, "");
    Check("[RELAY:2147483648]", 1, "");
    Check("[ACTION:MOOD:JOY]", 1, "");
    Check("[relay: 1]", 1, "[RELAY:1]");
    Check("[RELAY:999][RELAY:9]", 1, "[RELAY:9]");
    Check("[RELAY:1][RELAY:9]", 1, "[RELAY:1]");
    Check("", 1, "");
    Check(null, 1, "");
    Console.WriteLine($"PASS {passed} relay cases, each verified across both normalization passes");
}
}
'''
(out / 'Program.cs').write_text(harness.replace('METHOD', method), encoding='utf-8')
(out / 'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion></PropertyGroup></Project>')
(out / 'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
(out / 'source.sha256').write_text(hashlib.sha256(source.encode('utf-8')).hexdigest())
dotnet = resolve_dotnet(ROOT)
result = subprocess.run([str(dotnet), 'run', '--project', str(out / 'Proof.csproj'), '-c', 'Release'], cwd=ROOT,
                        env=minimal_test_environment(dotnet, out), capture_output=True, text=True, encoding='utf-8', errors='replace', timeout=90)
log = result.stdout + result.stderr
(out / 'run.log').write_text(log, encoding='utf-8')
print(log)
raise SystemExit(result.returncode)

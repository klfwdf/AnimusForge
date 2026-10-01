"""Measure actual AF summary DTO copies and raw-source encoding; never a LIVE frame bound.
Build/extraction failures return 2, behavioral assertion failures 1.
"""
from __future__ import annotations
import argparse, hashlib, importlib.util, json, os, subprocess, sys
from pathlib import Path
ROOT = Path(__file__).resolve().parents[4]
HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT / 'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
MODELS = []
RECORDS = 'src/modules/AF.Module.Memory/Records/MemoryPersistenceModels.cs'
WRITER = 'src/modules/AF.Module.Memory/Summary/MemorySourceFingerprintWriter.cs'
HISTORICAL_WRITER = 'Refactor/Runtime/MemorySourceFingerprintWriter.cs'

def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--writer-baseline', help='Use only this Git revision of the real writer')
    ap.add_argument('--mutate', choices=['omit-string-length', 'lose-high-code-unit', 'skip-full-buffer'])
    ap.add_argument('--run-root', type=Path)
    args = ap.parse_args()
    if args.writer_baseline and args.mutate:
        raise ValueError('Baseline and mutation are exclusive')
    spec = importlib.util.spec_from_file_location('extract', ROOT/'tests/modules/AF.Module.Conversation/ChannelCutoverBoundaryTests/run.py')
    ex = importlib.util.module_from_spec(spec); spec.loader.exec_module(ex)
    read = lambda path: (ROOT/path).read_text(encoding='utf-8-sig')
    root = read('MyBehavior.cs'); capture = read('MyBehavior.MemorySummaryInput.cs')
    declarations = []
    def extract(source, path, signature):
        code = ex.declaration(source, signature)
        declarations.append({'path':path,'signature':signature,
            'line':source[:source.index(code)].count('\n')+1,
            'sha256':hashlib.sha256(code.encode()).hexdigest()})
        return code
    snippets = [extract(root, 'MyBehavior.cs', 'private class '+name) for name in MODELS]
    snippets.append(extract(capture, 'MyBehavior.MemorySummaryInput.cs', 'private sealed class MemorySummarySourceView'))
    snippets.append(extract(capture, 'MyBehavior.MemorySummaryInput.cs', 'private static T CloneMemorySummarySource<T>('))
    recovery = read('MyBehavior.MemoryRecovery.cs')
    for signature in ['internal static bool IsValidMemoryCommitMarker(', 'internal static bool IsMemoryRecoveryHexDigest(']:
        code = ex.declaration(recovery, signature)
        # The shared extractor includes the following block after an expression-bodied method.
        code = code[:code.index(';') + 1]
        declarations.append({'path':'MyBehavior.MemoryRecovery.cs','signature':signature,
            'line':recovery[:recovery.index(code)].count('\n')+1,
            'sha256':hashlib.sha256(code.encode()).hexdigest()})
        snippets.append(code)
    product = 'using System; using System.Collections.Generic; using System.Linq; namespace AnimusForge; public partial class MyBehavior {\n'+'\n'.join(snippets)+'\n}'
    writer = subprocess.check_output(['git','show',args.writer_baseline+':'+HISTORICAL_WRITER],cwd=ROOT).decode('utf-8-sig').replace('\r\n','\n') if args.writer_baseline else read(WRITER)
    if args.mutate:
        old,new = {
            'omit-string-length':('Write(value == null ? -1 : value.Length);','Write(0);'),
            'lose-high-code-unit':('WriteByte((byte)(character >> 8));','WriteByte(0);'),
            'skip-full-buffer':('_hash.TransformBlock(_buffer, 0, _count, _buffer, 0);','/* deliberate missing digest block */'),
        }[args.mutate]
        if writer.count(old)!=1: raise ValueError('Mutation anchor did not uniquely match: '+old)
        writer=writer.replace(old,new)
        if args.mutate == 'lose-high-code-unit' and '_buffer[destination++] = (byte)(character >> 8);' in writer:
            writer=writer.replace('_buffer[destination++] = (byte)(character >> 8);', '_buffer[destination++] = 0;')
    out=new_run_root(ROOT, 'memory-summary-budget', args.run_root)
    files = {'Product.cs':product,'Mapper.cs':read('MyBehavior.MemorySourceFingerprint.cs'),
        'Models.cs':read(RECORDS),
        'NpcActionEntry.cs':read('src/modules/AF.Module.Memory/Records/NpcActionEntry.cs'),
        'TaleWorldsStubs.cs':'namespace TaleWorlds.Library { public static class TWParallel { public static bool IsMainThread() => true; } public static class MBMath { public static int ClampInt(int value, int min, int max) => System.Math.Clamp(value, min, max); } }',
        'Writer.cs':writer,'Program.cs':read('tests/modules/AF.Module.Memory/MemorySummaryBudgetTests/Harness.cs.txt'),
        'Proof.csproj':'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><NoWarn>CS0649</NoWarn></PropertyGroup></Project>',
        'NuGet.Config':'<configuration><packageSources><clear/></packageSources></configuration>'}
    for name,value in files.items(): (out/name).write_text(value,encoding='utf-8',newline='\n')
    manifest={'head':subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip(),
        'writer_baseline':args.writer_baseline,'mutation':args.mutate,'declarations':declarations,
        'source_sha256':{p:hashlib.sha256(read(p).encode()).hexdigest() for p in ['MyBehavior.cs','MyBehavior.MemorySummaryInput.cs','MyBehavior.MemoryRecovery.cs','MyBehavior.MemorySourceFingerprint.cs',RECORDS,WRITER]},
        'generated_sha256':{p:hashlib.sha256(v.encode()).hexdigest() for p,v in files.items()},
        'limits':['Real internal Memory record DTO/copy and complete raw-source mapper/writer; synthetic controlled input, no game objects.',
                  'Stopwatch medians and thread allocations are microbench observations, not game frame guarantees.',
                  'Capture cost row combines real source digest + copy + second binding digest only; excludes prompt, sanitize, context and lookups.',
                  'The rejected chunk-scan examples are TEST-ONLY counterexamples, not production implementations.']}
    (out/'manifest.json').write_text(json.dumps(manifest,ensure_ascii=False,indent=2),encoding='utf-8')
    dotnet=resolve_dotnet(ROOT)
    env=minimal_test_environment(dotnet,out)
    build=subprocess.run([str(dotnet),'build',str(out/'Proof.csproj'),'-c','Release','--nologo','-p:RestoreConfigFile='+str(out/'NuGet.Config')],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',timeout=120)
    (out/'build.log').write_text(build.stdout+build.stderr,encoding='utf-8')
    if build.returncode: print(build.stdout+build.stderr); return 2
    result=subprocess.run([str(dotnet),str(out/'bin/Release/net8.0/Proof.dll')],cwd=ROOT,env=env,capture_output=True,text=True,encoding='utf-8',timeout=120)
    (out/'run.log').write_text(result.stdout+result.stderr,encoding='utf-8'); print(result.stdout+result.stderr,end='')
    measurements=[json.loads(line[len('MEASURE '):]) for line in result.stdout.splitlines() if line.startswith('MEASURE ')]
    (out/'measurements.json').write_text(json.dumps(measurements,ensure_ascii=False,indent=2),encoding='utf-8')
    source_hashes=[json.loads(line[len('SOURCE_HASH '):]) for line in result.stdout.splitlines() if line.startswith('SOURCE_HASH ')]
    (out/'source-hashes.json').write_text(json.dumps(source_hashes,ensure_ascii=False,indent=2),encoding='utf-8')
    return result.returncode if 'BUDGET_RESULT ' in result.stdout else 2
if __name__=='__main__':
    sys.stdout.reconfigure(encoding='utf-8')
    try: sys.exit(main())
    except Exception as exc: print('BUDGET_TOOL_ERROR '+type(exc).__name__+': '+str(exc)); sys.exit(2)

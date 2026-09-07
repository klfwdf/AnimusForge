"""Execute scene postprocess gate ownership races with real task continuations."""
import argparse
import hashlib
import os
from pathlib import Path
import re
import subprocess
import run

HERE=Path(__file__).resolve().parent
SIGNATURES=['private void RegisterScenePostprocessGateTask(', 'private Task GetScenePostprocessGateTask(', 'private void ForceClearScenePostprocessGate(', 'private async Task WaitForScenePostprocessGateAsync(']


def main():
    ap=argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--source-ref')
    ap.add_argument('--dotnet',default=r'G:\AFMOD\.dotnet-sdk\dotnet.exe')
    ap.add_argument('--output-name',default='gate-current')
    ap.add_argument('--case',default='all',choices=['all','late-task','late-waiter','late-timeout','same-gate','fault-cancel','queued-messages'])
    args=ap.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9_-]+',args.output_name):ap.error('Invalid output name')
    source=run.extractor.source('ShoutBehavior.cs',args.source_ref)
    methods='\n'.join(run.extractor.declaration(source,s) for s in SIGNATURES)
    fields=[]
    for name in sorted(set(re.findall(r'\b(_[A-Za-z]\w*)\b',methods))):
        declaration=re.search(r'^\s*private (?:readonly |volatile )?[^\n;{}]+\b'+name+r'\b[^\n;{}]*;',source,re.M)
        if not declaration:raise ValueError('No source field: '+name)
        fields.append(declaration.group().strip())
    template=(HERE/'GateHarness.cs.txt').read_text(encoding='utf-8-sig').replace('@@METHODS@@',methods).replace('@@FIELDS@@','\n'.join(fields))
    output=HERE/'.generated'/args.output_name;output.mkdir(parents=True,exist_ok=True)
    (output/'Program.cs').write_text(template,encoding='utf-8')
    (output/'Gate.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup></Project>')
    (output/'NuGet.Config').write_text('<configuration><packageSources><clear/></packageSources></configuration>')
    env=os.environ.copy();env['DOTNET_ROOT']=str(Path(args.dotnet).parent);env['DOTNET_CLI_HOME']=str(output/'cli');env['DOTNET_CLI_TELEMETRY_OPTOUT']='1';env['DOTNET_NOLOGO']='1';env['DOTNET_CLI_UI_LANGUAGE']='en'
    meta='source='+(args.source_ref or 'working-tree')+' case='+args.case+'\nMETHODS sha256='+hashlib.sha256(methods.encode()).hexdigest()+'\nFIELDS sha256='+hashlib.sha256('\n'.join(fields).encode()).hexdigest()
    result=subprocess.run([args.dotnet,'run','--project',str(output/'Gate.csproj'),'-c','Release','--',args.case],cwd=output,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=90)
    log=meta+'\n'+result.stdout+result.stderr
    (output/'run.log').write_text(log,encoding='utf-8');(output/'source-fingerprints.txt').write_text(meta,encoding='utf-8');print(log);return result.returncode

if __name__=='__main__':raise SystemExit(main())

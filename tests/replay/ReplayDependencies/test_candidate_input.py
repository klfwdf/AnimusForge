"""Candidate gate regression; synthetic bytes only, never loads a game DLL."""
from pathlib import Path
import argparse, subprocess, sys
from xml.sax.saxutils import escape
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment

def main():
    parser=argparse.ArgumentParser();parser.add_argument('--run-root',type=Path);args=parser.parse_args()
    out=new_run_root(ROOT,'replay-candidate-input',args.run_root);dotnet=resolve_dotnet(ROOT)
    code=r'''using System;
using System.IO;
using System.Text.Json;
using System.Security.Cryptography;
class Program {
 static int checks;
 static void Reject(Action action) {try {action();}catch(InvalidOperationException){checks++;return;}throw new Exception("negative gate accepted");}
 static void Main() {
 string file=Path.Combine(AppContext.BaseDirectory,"synthetic-candidate.dll");File.WriteAllText(file,"synthetic bytes only");
 string hash=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(file)));
 string marker=Path.ChangeExtension(file,".build.json");
 void Mark(string role="Implementation",string api="1.4")=>File.WriteAllText(marker,JsonSerializer.Serialize(new {Sha256=hash,Role=role,BannerlordApi=api,BuildFlavor="ANIMUSFORGE_BANNERLORD_API_1_4"}));
 void Manifest(string path,string digest)=>File.WriteAllText(Path.Combine(AppContext.BaseDirectory,"af-replay-dependencies.json"),JsonSerializer.Serialize(new {ImplementationPath=path,ImplementationSha256=digest}));
 Mark();Manifest(file,hash);
 if(ReplayCandidateInput.Read(new[]{file,hash})!=file)throw new Exception("current input rejected");checks++;
 Reject(()=>ReplayCandidateInput.Read(Array.Empty<string>()));
 Reject(()=>ReplayCandidateInput.Read(new[]{"relative.dll",hash}));
 Reject(()=>ReplayCandidateInput.Read(new[]{Path.Combine(Path.GetPathRoot(file),"outside.dll"),hash}));
 Reject(()=>ReplayCandidateInput.Read(new[]{Path.Combine(AppContext.BaseDirectory,"single_module_stage","candidate.dll"),hash}));
 Reject(()=>ReplayCandidateInput.Read(new[]{file,"wrong hash"}));
 Mark("Bootstrap");Reject(()=>ReplayCandidateInput.Read(new[]{file,hash}));
 Mark(api:"1.3");Reject(()=>ReplayCandidateInput.Read(new[]{file,hash}));
 Mark();Manifest(file,"other candidate hash");Reject(()=>ReplayCandidateInput.Read(new[]{file,hash}));
 Manifest(file+".other",hash);Reject(()=>ReplayCandidateInput.Read(new[]{file,hash}));
 Manifest(file,hash);File.Move(marker,marker+".held");Reject(()=>ReplayCandidateInput.Read(new[]{file,hash}));
 string root=Environment.GetEnvironmentVariable("AF_REPLAY_REPO_ROOT");Environment.SetEnvironmentVariable("AF_REPLAY_REPO_ROOT",null);Reject(()=>ReplayCandidateInput.Read(new[]{file,hash}));Environment.SetEnvironmentVariable("AF_REPLAY_REPO_ROOT",root);
 Console.WriteLine("PASS candidate input checks="+checks+"; synthetic only, no game DLL load");
 }
}'''
    (out/'Program.cs').write_text(code,encoding='utf-8')
    helper=ROOT/'tests/replay/ReplayDependencies/ReplayCandidateInput.cs'
    (out/'Proof.csproj').write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><EnableDefaultCompileItems>false</EnableDefaultCompileItems></PropertyGroup><ItemGroup><Compile Include="Program.cs" /><Compile Include="'+escape(str(helper))+'" /></ItemGroup></Project>',encoding='utf-8')
    (out/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>',encoding='utf-8')
    env=minimal_test_environment(dotnet,out);env['AF_REPLAY_REPO_ROOT']=str(ROOT)
    result=subprocess.run([str(dotnet),'run','--project',str(out/'Proof.csproj'),'-c','Release'],cwd=out,env=env,capture_output=True,text=True,encoding='utf-8',errors='replace',timeout=120)
    log=result.stdout+result.stderr;(out/'run.log').write_text(log,encoding='utf-8');print(log,end='');return result.returncode
if __name__=='__main__':raise SystemExit(main())

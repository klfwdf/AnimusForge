"""Validate the real bounded-buffer sink against independent BinaryWriter/SHA256 vectors.
No game, private DTO extraction, external packages or network access.
"""
from pathlib import Path
import os, subprocess, json, hashlib, argparse, sys
from xml.sax.saxutils import escape
ROOT=Path(__file__).resolve().parents[4]
HERE=Path(__file__).resolve().parent
sys.path.insert(0,str(ROOT/'tests'))
from output_isolation import new_run_root, resolve_dotnet, minimal_test_environment
HARNESS=r'''using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using AnimusForge.Refactor.Runtime;
class Program {
 static void Text(BinaryWriter w,string s){w.Write(s==null?-1:s.Length);if(s!=null){var bytes=new byte[s.Length*2];Buffer.BlockCopy(s.ToCharArray(),0,bytes,0,bytes.Length);w.Write(bytes);}}
 static string Expected(string text){
  using(var stream=new MemoryStream())using(var w=new BinaryWriter(stream)){
   w.Write(true);w.Write(false);w.Write(int.MinValue);w.Write(int.MaxValue);w.Write(-1);w.Write(long.MinValue);w.Write(long.MaxValue);
   w.Write(false);w.Write(true);w.Write(false);w.Write(true);w.Write(true); // nullable bool null/false/true
   Text(w,null);Text(w,"");Text(w,text);w.Write(-1);w.Write(0);w.Write(3);Text(w,"a");Text(w,null);Text(w,text);w.Flush();
   using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-","");
  }
 }
 static string Actual(string text){using(var w=new MemorySourceFingerprintWriter()){
  w.Write(true);w.Write(false);w.Write(int.MinValue);w.Write(int.MaxValue);w.Write(-1);w.Write(long.MinValue);w.Write(long.MaxValue);
  w.Write((bool?)null);w.Write((bool?)false);w.Write((bool?)true);w.Write((string)null);w.Write("");w.Write(text);
  w.WriteList<string>(null,(sink,value)=>sink.Write(value));w.WriteList(new List<string>(),(sink,value)=>sink.Write(value));w.WriteList(new List<string>{"a",null,text},(sink,value)=>sink.Write(value));return w.Finish();}}
 static void Reject(Action act){bool threw=false;try{act();}catch(InvalidOperationException){threw=true;}if(!threw)throw new Exception("invalid sink lifecycle/list mutation accepted");}
 static int Main(){try{
  if(!BitConverter.IsLittleEndian)throw new Exception("oracle requires this Windows little-endian host");
  int vectors=0;foreach(int n in new[]{0,1,2,2040,2047,2048,2049,4096,8192}){string text=new string('x',n)+"\0汉😀\ud800\udc01";if(Actual(text)!=Expected(text))throw new Exception("binary framing/hash mismatch length="+n);vectors++;}
  var sink=new MemorySourceFingerprintWriter();sink.Write("done");sink.Finish();Reject(()=>sink.Write(1));Reject(()=>sink.Finish());sink.Dispose();Reject(()=>sink.Write(false));Reject(()=>sink.Finish());
  using(var changed=new MemorySourceFingerprintWriter()){var list=new List<string>{"one","two"};Reject(()=>changed.WriteList(list,(w,value)=>{w.Write(value);list.Add("changed");}));}
  Console.WriteLine("FINGERPRINT_WRITER_RESULT vectors="+vectors+" guards=5 failures=0 actual_sink=true game=NOT_RUN");return 0;
 }catch(Exception e){Console.WriteLine("FINGERPRINT_WRITER_FAIL "+e);return 1;}}
}
'''
def main():
 parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--run-root',type=Path);args=parser.parse_args()
 out=new_run_root(ROOT,'memory-fingerprint',args.run_root)
 source=ROOT/'src/modules/AF.Module.Memory/Summary/MemorySourceFingerprintWriter.cs'
 files={'Program.cs':HARNESS,'NuGet.Config':'<configuration><packageSources><clear/></packageSources></configuration>','Proof.csproj':'<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net8.0</TargetFramework><LangVersion>latest</LangVersion><EnableDefaultCompileItems>false</EnableDefaultCompileItems><CheckForOverflowUnderflow>true</CheckForOverflowUnderflow></PropertyGroup><ItemGroup><Compile Include="Program.cs"/><Compile Include="'+escape(str(source))+'"/></ItemGroup></Project>'}
 for name,data in files.items():(out/name).write_bytes(data.encode())
 dotnet=resolve_dotnet(ROOT);env=minimal_test_environment(dotnet,out)
 command=[str(dotnet),'build',str(out/'Proof.csproj'),'-c','Release','--nologo','-p:RestoreConfigFile='+str(out/'NuGet.Config')]
 build=subprocess.run(command,env=env,cwd=ROOT,capture_output=True,text=True,encoding='utf8');(out/'build.log').write_text(build.stdout+build.stderr,encoding='utf8')
 meta=dict(source=str(source.relative_to(ROOT)),sourceSha256=hashlib.sha256(source.read_bytes()).hexdigest(),runnerSha256=hashlib.sha256(Path(__file__).read_bytes()).hexdigest(),buildCommand=command,buildExit=build.returncode)
 if build.returncode:print(build.stdout+build.stderr);return 2
 run=subprocess.run([str(dotnet),str(out/'bin/Release/net8.0/Proof.dll')],env=env,cwd=ROOT,capture_output=True,text=True,encoding='utf8');(out/'run.log').write_text(run.stdout+run.stderr,encoding='utf8');meta['runExit']=run.returncode;(out/'manifest.json').write_text(json.dumps(meta,indent=2),encoding='utf8');print('BUILD_PASS fingerprint-writer');print(run.stdout+run.stderr,end='');return run.returncode
if __name__=='__main__':raise SystemExit(main())

using System.Diagnostics;
using System.Security.Cryptography;
using AnimusForge;
using Newtonsoft.Json.Linq;

if(args[0]=="--real") { await RealPackage.Run(args[1],args[2]); return; }

int checks=0;
void Check(bool value,string name){if(!value)throw new Exception(name);Console.WriteLine("PASS "+name);checks++;}
string root=Path.GetFullPath(Path.Combine(args[0],Guid.NewGuid().ToString("N"),"AnimusForge"));Directory.CreateDirectory(Path.Combine(root,"ModuleData"));File.WriteAllText(Path.Combine(root,"SubModule.xml"),"<Module><Id value=\"AnimusForge\"/></Module>");
AnimusForgeModulePaths.Root=root;
string package=Path.Combine(root,"AF-GPTSOVITS-L");
Check(GptSoVitsRuntimePackage.Discover(root,new Version(1,5,4)).State==GptSoVitsPackageState.Missing,"missing package");
Directory.CreateDirectory(package);
foreach(string source in Directory.GetFiles(args[1]))File.Copy(source,Path.Combine(package,Path.GetFileName(source)),true);
File.WriteAllText(Path.Combine(package,"server.py"),"# inert entry for the actual process fixture");
string Hash(string p)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(package,p))));
JObject Good()=>new(){["schema_version"]=1,["package_id"]="AF-GPTSOVITS-L",["edition"]="L",["af_runtime_api"]=1,["af_min_version"]="1.0.0",["package_version"]="0.1.0",["engine"]="gpt-sovits-v2pro",["python"]="RuntimeFixture.exe",["entrypoint"]="server.py",["files"]=new JObject{["RuntimeFixture.exe"]=Hash("RuntimeFixture.exe"),["server.py"]=Hash("server.py")}};
void Save(JObject d)=>File.WriteAllText(Path.Combine(package,"runtime.json"),d.ToString());
Save(Good());var found=GptSoVitsRuntimePackage.Discover(root,new Version(1,5,4));Check(found.State==GptSoVitsPackageState.Installed,"compatible package metadata");found.VerifyFiles();Check(true,"real SHA256 verification");
foreach(string field in new[]{"schema_version","af_runtime_api"}){var d=Good();d[field]=99;Save(d);Check(GptSoVitsRuntimePackage.Discover(root,new Version(1,5,4)).State==GptSoVitsPackageState.Incompatible,field+" rejected");}
{var d=Good();d["af_min_version"]="999.0.0";Save(d);Check(GptSoVitsRuntimePackage.Discover(root,new Version(1,5,4)).State==GptSoVitsPackageState.Incompatible,"future AF rejected");}
foreach(string bad in new[]{"../server.py","C:/outside.py","folder/../../file.py","file.py:stream","/root.py"}){var d=Good();d["entrypoint"]=bad;Save(d);Check(GptSoVitsRuntimePackage.Discover(root,new Version(1,5,4)).State==GptSoVitsPackageState.Invalid,"unsafe path rejected "+bad);}
{var d=Good();d["files"]["server.py"]="xx";Save(d);Check(GptSoVitsRuntimePackage.Discover(root,new Version(1,5,4)).State==GptSoVitsPackageState.Invalid,"invalid hash metadata");}
Save(Good());found=GptSoVitsRuntimePackage.Discover(root,new Version(1,5,4));File.AppendAllText(Path.Combine(package,"server.py"),"changed");try{found.VerifyFiles();Check(false,"mutation accepted");}catch(InvalidDataException){Check(true,"post-discovery mutation rejected before launch");}
Save(Good());
string upper=Path.Combine(root,"AF-GPTSOVITS-U");Directory.CreateDirectory(upper);
foreach(string source in Directory.GetFiles(package))File.Copy(source,Path.Combine(upper,Path.GetFileName(source)),true);
var u=Good();u["package_id"]="AF-GPTSOVITS-U";u["edition"]="U";
File.WriteAllText(Path.Combine(upper,"runtime.json"),u.ToString());
Check(GptSoVitsRuntimePackage.Discover(root,new Version(1,5,4),true).Edition=="L","both valid prefer L");
var invalid=Good();invalid["af_runtime_api"]=99;Save(invalid);
Check(GptSoVitsRuntimePackage.Discover(root,new Version(1,5,4),true).Edition=="U","incompatible L falls back to verified U");
Save(Good());File.AppendAllText(Path.Combine(package,"server.py"),"corrupt");
Check(GptSoVitsRuntimePackage.Discover(root,new Version(1,5,4),true).Edition=="U","corrupt L falls back to verified U");
Save(Good());
async Task Wait(string state){var timer=Stopwatch.StartNew();while(GptSoVitsManagedRuntime.State!=state&&timer.Elapsed.TotalSeconds<20)await Task.Delay(50);Check(GptSoVitsManagedRuntime.State==state,"runtime state "+state+" actual="+GptSoVitsManagedRuntime.State+" reason="+GptSoVitsManagedRuntime.Reason);}
var settings=new DuelSettings {EnableTtsSpeech=false,TtsUseGptSoVits=true};
GptSoVitsManagedRuntime.Observe(settings);await Wait("Installed");Check(!Process.GetProcessesByName("RuntimeFixture").Any(),"disabled discovery does not launch");
settings.EnableTtsSpeech=true;GptSoVitsManagedRuntime.Observe(settings);await Wait("Ready");Check(GptSoVitsManagedRuntime.TryCapture(settings.TtsGptSoVitsApiUrl,out string endpoint,out string token)&&token?.Length==32,"owned endpoint and instance capture");
int ownedPid=Process.GetProcessesByName("RuntimeFixture").Single().Id;
for(int i=0;i<100;i++)GptSoVitsManagedRuntime.Observe(settings);
Check(Process.GetProcessesByName("RuntimeFixture").Single().Id==ownedPid,"unchanged observation reuses one worker");
using(var client=new HttpClient()){Check((int)(await client.GetAsync(endpoint.Replace("/tts","/af/health"))).StatusCode==403,"health refuses unrelated instance");}
int cancellations=TtsEngine.Instance.Cancellations;
await GptSoVitsManagedRuntime.RefreshAsync();await Wait("Ready");
Check(TtsEngine.Instance.Cancellations>cancellations&&Process.GetProcessesByName("RuntimeFixture").Single().Id!=ownedPid,"refresh retires accepted local speech and replaces only owned worker");
settings.UsesManagedGptSoVits=false;settings.TtsGptSoVitsApiUrl="http://127.0.0.1:9881/tts";GptSoVitsManagedRuntime.Observe(settings);await Wait("Manual");Check(!Process.GetProcessesByName("RuntimeFixture").Any(),"custom address stops only owned worker");Check(GptSoVitsManagedRuntime.TryCapture(settings.TtsGptSoVitsApiUrl,out endpoint,out token)&&endpoint==settings.TtsGptSoVitsApiUrl&&token==null,"manual endpoint retained without managed header");
settings.TtsGptSoVitsApiUrl="http://127.0.0.1:9880/tts";settings.TtsGptSoVitsManifestPath="custom-manifest.json";GptSoVitsManagedRuntime.Observe(settings);await Wait("Manual");Check(!Process.GetProcessesByName("RuntimeFixture").Any(),"explicit external manifest keeps manual connection");
settings.TtsGptSoVitsManifestPath="";settings.UsesManagedGptSoVits=true;settings.EnableTtsSpeech=true;GptSoVitsManagedRuntime.Observe(settings);await Wait("Ready");settings.EnableTtsSpeech=false;GptSoVitsManagedRuntime.Observe(settings);await Wait("Installed");Check(!Process.GetProcessesByName("RuntimeFixture").Any(),"disable releases owned process");
// Same automatic entry starts U when L is incompatible; never starts both.
invalid=Good();invalid["af_runtime_api"]=99;Save(invalid);
settings.EnableTtsSpeech=true;GptSoVitsManagedRuntime.Observe(settings);await Wait("Ready");
Check(GptSoVitsManagedRuntime.Reason=="AF-GPTSOVITS-U" && Process.GetProcessesByName("RuntimeFixture").Length==1,"U handshake and single process");
settings.TtsGptSoVitsApiUrl="http://127.0.0.1:12345/tts";settings.TtsGptSoVitsManifestPath="ignored-manual-manifest.json";GptSoVitsManagedRuntime.Observe(settings);
Check(GptSoVitsManagedRuntime.State=="Ready","automatic selection ignores saved manual settings");
Process.GetProcessesByName("RuntimeFixture").Single().Kill();await Wait("Failed");
Check(!GptSoVitsManagedRuntime.TryCapture(settings.TtsGptSoVitsApiUrl,out endpoint,out token),"dead owned worker cannot return a ready endpoint");
File.Move(Path.Combine(package,"runtime.json"),Path.Combine(package,"runtime.json.disabled"));File.Move(Path.Combine(upper,"runtime.json"),Path.Combine(upper,"runtime.json.disabled"));
await GptSoVitsManagedRuntime.RefreshAsync();await Wait("Missing");
Check(!GptSoVitsManagedRuntime.TryCapture(settings.TtsGptSoVitsApiUrl,out endpoint,out token),"missing automatic package never silently becomes manual API");
GptSoVitsManagedRuntime.Shutdown();Console.WriteLine("PASS "+checks+" package/runtime assertions; process fixture, not a speech model.");

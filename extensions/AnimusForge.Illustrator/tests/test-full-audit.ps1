param([Parameter(Mandatory=$true)][string]$AssemblyPath,[string]$GameRoot='F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord')
$ErrorActionPreference='Stop'
$module=Split-Path -Parent $PSScriptRoot
$src=Join-Path $module 'src'
Add-Type -TypeDefinition @'
using System; using System.IO; using System.Reflection; using System.Threading; using System.Threading.Tasks; using System.Net; using System.Net.Sockets; using System.Text;
public static class AuditFixture {
 public static string[] Dirs; public static int Completions; public static string Body; public static byte[] RawBody; public static Task ServerTask;
 public static void Install(string[] dirs) { Dirs=dirs; AppDomain.CurrentDomain.AssemblyResolve+=Resolve; }
 static Assembly Resolve(object s,ResolveEventArgs e) { string n=new AssemblyName(e.Name).Name+".dll";foreach(var d in Dirs) { var p=Path.Combine(d,n);if(File.Exists(p))return Assembly.LoadFrom(p); } return null; }
 public static Func<Task<int>> Work() { return () => Task.FromResult(42); }
 public static Action<int,Exception> Complete() { return (v,e) => { if(v==42 && e==null) Interlocked.Increment(ref Completions); }; }
 public static Action Fail() { return () => { throw new InvalidOperationException("fixture redraw preflight"); }; }
 public static Action Noop() { return () => {}; }
 public static string StartServer(string response) {
  var listener=new TcpListener(IPAddress.Loopback,0); listener.Start(); int port=((IPEndPoint)listener.LocalEndpoint).Port;
  ServerTask=Task.Run(() => { try { using(var client=listener.AcceptTcpClient()) using(var stream=client.GetStream()) {
   client.ReceiveTimeout=10000; client.SendTimeout=10000;
   var header=new MemoryStream(); int last=0,b;
   while((b=stream.ReadByte())>=0) { header.WriteByte((byte)b);last=(last<<8)|b;if(last==0x0D0A0D0A) break; if(header.Length>32768)throw new IOException("header limit"); }
   string h=Encoding.ASCII.GetString(header.ToArray()); int length=0;
   foreach(string line in h.Split(new[]{"\r\n"},StringSplitOptions.None)) if(line.StartsWith("Content-Length:",StringComparison.OrdinalIgnoreCase)) length=int.Parse(line.Substring(15).Trim());
   if(h.IndexOf("100-continue",StringComparison.OrdinalIgnoreCase)>=0) { byte[] interim=Encoding.ASCII.GetBytes("HTTP/1.1 100 Continue\r\n\r\n");stream.Write(interim,0,interim.Length); }
   RawBody=new byte[length];int pos=0;while(pos<length) { int n=stream.Read(RawBody,pos,length-pos);if(n==0)throw new IOException("truncated request");pos+=n; }
   Body=Encoding.UTF8.GetString(RawBody);
   byte[] data=Encoding.UTF8.GetBytes(response);byte[] rh=Encoding.ASCII.GetBytes("HTTP/1.1 200 OK\r\nContent-Type: application/json\r\nContent-Length: "+data.Length+"\r\nConnection: close\r\n\r\n");stream.Write(rh,0,rh.Length);stream.Write(data,0,data.Length);
  } } finally { listener.Stop(); } });
  return "http://127.0.0.1:"+port+"/v1";
 }
 public static int PngParts() { int count=0;for(int i=0;i+3<RawBody.Length;i++)if(RawBody[i]==137&&RawBody[i+1]==80&&RawBody[i+2]==78&&RawBody[i+3]==71)count++;return count; }
 public static void RaceDefaults(Assembly assembly,object a,object b,string campaign) {
  MethodInfo method=assembly.GetType("AnimusForge.Illustrator.Engine.DiskImageCacheManager").GetMethod("SetDefault");
  Parallel.For(0,30,i=>method.Invoke(null,new object[]{i%2==0?a:b,campaign}));
 }
}
'@
[AuditFixture]::Install([string[]]@((Split-Path -Parent $AssemblyPath),"$GameRoot\bin\Win64_Shipping_Client","$GameRoot\Modules\SandBox\bin\Win64_Shipping_Client","$GameRoot\Modules\Bannerlord.Harmony\bin\Win64_Shipping_Client","$GameRoot\Modules\Bannerlord.MBOptionScreen\bin\Win64_Shipping_Client","$GameRoot\Modules\AnimusForge\bin\Win64_Shipping_Client"))
$a=[Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
[void][Reflection.Assembly]::LoadFrom("$GameRoot\bin\Win64_Shipping_Client\TaleWorlds.Core.dll")
$static=[Reflection.BindingFlags]'NonPublic,Static'
$instance=[Reflection.BindingFlags]'NonPublic,Instance'
$checks=0;$failures=0
function Check([bool]$ok,[string]$name) { $script:checks++;if($ok){Write-Host "PASS $name"}else{$script:failures++;Write-Host "FAIL $name"} }
function Rejected([scriptblock]$work) { try { &$work|Out-Null;return $false }catch{return $true} }
# F02/F03: real native Equipment with an actual cosmetic override; all other slots survive.
$appearance=$a.GetType('AnimusForge.Illustrator.Context.CharacterAppearanceSnapshot',$true)
$equipment=[TaleWorlds.Core.Equipment]::new()
for($i=0;$i -lt 12;$i++) {$equipment[$i]=[TaleWorlds.Core.EquipmentElement]::new([TaleWorlds.Core.ItemObject]::new("base_$i"),$null,$null,$false)}
$cosmetic=[TaleWorlds.Core.ItemObject]::new('cosmetic_head')
$equipment[5]=[TaleWorlds.Core.EquipmentElement]::new($equipment[5].Item,$null,$cosmetic,$false)
$visible=$appearance.GetMethod('VisibleEquipment',$static).Invoke($null,@($equipment))
Check ($visible.CalculateEquipmentCode().Contains('cosmetic_head')) 'F02 cosmetic ID survives native render code'
Check ($equipment[5].Item.StringId -eq 'base_5' -and $equipment[5].CosmeticItem -eq $cosmetic) 'F02 projection does not mutate source equipment'
for($i=0;$i -lt 12;$i++) {if($i -ne 5){Check ($visible[$i].Item.StringId -eq "base_$i") "F02 retains slot $i"}}
$equipment[5]=[TaleWorlds.Core.EquipmentElement]::new($null,$null,$cosmetic,$false)
Check ($appearance.GetMethod('VisibleEquipment',$static).Invoke($null,@($equipment))[5].Item -eq $cosmetic) 'F02 cosmetic-only slot is not empty'
$frozen=[Activator]::CreateInstance($appearance,[object[]]@('equipment','body','banner',[uint32]0x112233,[uint32]0x445566,[int]0,$false))
Check ($frozen.Color1 -eq 0x112233 -and $frozen.Color2 -eq 0x445566 -and !$appearance.GetProperty('Color1').CanWrite) 'F03 dye colors are immutable captured values'
# F05/F06/F07: invoke actual classifier->anchoring->facts, not a duplicate parser.
$weekly=$a.GetType('AnimusForge.Illustrator.Context.WeeklyReportContextExtractor',$true)
foreach($text in @('攻城部队未能攻陷城池，随后撤退。','拒绝处决俘虏，俘虏获释。','计划海战，但没有出航。')) {
 $context=[Activator]::CreateInstance($a.GetType('AnimusForge.Illustrator.Context.WeeklyReportVisualContext',$true))
 $context.EventTheme=$weekly.GetMethod('ClassifyEventTheme',$static).Invoke($null,@($text))
 $context.EnvironmentProfile=[Activator]::CreateInstance($a.GetType('AnimusForge.Illustrator.Context.EnvironmentVisualProfile',$true))
 $weekly.GetMethod('ApplyEventSceneAnchoring',$static).Invoke($null,@($context,$null))|Out-Null
 Check ([string]::IsNullOrWhiteSpace($context.EnvironmentProfile.BuildHardFactsSummary())) "F05 no inferred outcome for $text"
}
$sea=$a.GetType('AnimusForge.Illustrator.Context.ConversationContextExtractor',$true).GetMethod('IsExplicitSeaScene',$static)
Check (!$sea.Invoke($null,@('海岸附近的野外会面'))) 'F06 coast is land not sea'
Check ($sea.Invoke($null,@('海船甲板上的会面'))) 'F06 explicit deck recognized'
$hero=$a.GetType('AnimusForge.Illustrator.Context.HeroVisualExtractor',$true)
$physical=[string]$hero.GetMethod('ExtractPhysicalFeatures',$static).Invoke($null,[object[]]@($null))
Check (!$physical.Contains('银白') -and $physical.Contains('保持未知')) 'F07 unavailable face remains unknown without invented age or profession features'
# F09: valid PNG/JPEG and bad/oversized image data through actual decoder.
Add-Type -AssemblyName System.Drawing
$bitmap=[Drawing.Bitmap]::new(32,32);$graphics=[Drawing.Graphics]::FromImage($bitmap);$graphics.Clear([Drawing.Color]::Gold);$graphics.Dispose()
$stream=[IO.MemoryStream]::new();$bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Png);$png=$stream.ToArray();$stream.Dispose()
$stream=[IO.MemoryStream]::new();$bitmap.Save($stream,[Drawing.Imaging.ImageFormat]::Jpeg);$jpeg=$stream.ToArray();$stream.Dispose();$bitmap.Dispose()
$payload=$a.GetType('AnimusForge.Illustrator.Engine.ImagePayload',$true);$normalize=$payload.GetMethod('Normalize',$static)
Check (Rejected {$normalize.Invoke($null,[object[]]@(,[byte[]](1..128)))}) 'F09 non-image bytes rejected'
Check (Rejected {$normalize.Invoke($null,[object[]]@(,[byte[]](137,80,78,71)))}) 'F09 truncated PNG rejected'
$huge=[byte[]]$png.Clone();$huge[16]=127
Check (Rejected {$normalize.Invoke($null,[object[]]@(,$huge))}) 'F09 huge PNG dimensions rejected before GDI'
$converted=[byte[]]$normalize.Invoke($null,[object[]]@(,$jpeg))
Check ($converted[0] -eq 137 -and $converted[1] -eq 80) 'F08 JPEG normalized to actual PNG'
Check ([Convert]::ToBase64String($normalize.Invoke($null,[object[]]@(,$png))) -eq [Convert]::ToBase64String($png)) 'F09 valid PNG is preserved without recompression'
$client=$a.GetType('AnimusForge.Illustrator.Core.UniversalOpenAiImageClient',$true)
$bad=[Convert]::ToBase64String([byte[]](1..128));$parse=$client.GetMethod('ParseUriOrBase64Async',$static)
Check ($null -eq $parse.Invoke($null,@($bad,[Threading.CancellationToken]::None)).GetAwaiter().GetResult()) 'F09 original invalid-base64 audit repro now rejected'
$body=@{data=@(@{b64_json=$bad},@{b64_json=[Convert]::ToBase64String($png)})}|ConvertTo-Json -Depth 5 -Compress
$image=$client.GetMethod('ExtractImageAsync',$static).Invoke($null,@([string]$body,[Threading.CancellationToken]::None)).GetAwaiter().GetResult()
Check ($image.Bytes.Length -eq $png.Length) 'F09 invalid candidate does not mask later valid PNG'
Add-Type -AssemblyName System.Net.Http
$content=[Net.Http.ByteArrayContent]::new([byte[]]::new(2048))
Check (Rejected {$payload.GetMethod('ReadBoundedAsync',$static).Invoke($null,@($content,1024,[Threading.CancellationToken]::None)).GetAwaiter().GetResult()}) 'F09 streamed response limit rejects oversized body'
$content.Dispose()
# F01/F14: isolated filesystem only; a single atomic default pointer under concurrent updates.
$fixture=Join-Path $module ('obj\fidelity\full-audit-test-'+[Guid]::NewGuid().ToString('N'));$cacheRoot=Join-Path $fixture 'cache';$external=Join-Path $fixture 'external'
[IO.Directory]::CreateDirectory($cacheRoot)|Out-Null;[IO.Directory]::CreateDirectory($external)|Out-Null
$cache=$a.GetType('AnimusForge.Illustrator.Engine.DiskImageCacheManager',$true);$cache.GetField('CacheBaseDir',$static).SetValue($null,[string]$cacheRoot)
$outside=Join-Path $external 'victim.png';[IO.File]::WriteAllBytes($outside,$png)
$item=[Activator]::CreateInstance($a.GetType('AnimusForge.Illustrator.Engine.CachedIllustrationItem',$true));$item.FilePath=$outside
Check (!$cache.GetMethod('DeleteItem').Invoke($null,@($item,'test')) -and (Test-Path $outside)) 'F01 direct outside deletion rejected'
$category=Join-Path $cacheRoot 'test\conversation';[IO.Directory]::CreateDirectory($category)|Out-Null
$meta=Join-Path $category 'stale.json';@{Key='stale';FilePath=$outside}|ConvertTo-Json|Set-Content -Encoding UTF8 $meta
Check ($null -eq $cache.GetMethod('ReadMetadata',$static).Invoke($null,@([string]$meta))) 'F01 stale external metadata rejected'
Check ($cache.GetMethod('SanitizeKey').Invoke($null,@('..')) -eq 'unknown') 'F01 parent segment rejected'
$save=$cache.GetMethod('SaveImage')
$first=$save.Invoke($null,@('subject',$png,'p','title','conversation','test',20,$true,$true))
$second=$save.Invoke($null,@('subject',$png,'p2','title','conversation','test',20,$false,$false))
Check ($null -ne $first -and $null -ne $second) 'F14 valid images saved'
[AuditFixture]::RaceDefaults($a,$first,$second,'test')
$list=$cache.GetMethod('GetAllCachedIllustrations').Invoke($null,@('test',$true))
Check (@($list|Where-Object {$_.SubjectKey -eq 'subject' -and $_.IsDefault}).Count -eq 1) 'F14 concurrent default changes leave exactly one default'
Check ($cache.GetMethod('SetDefault').Invoke($null,@($second,'test'))) 'F14 explicit default promotion succeeds'
$history=$save.Invoke($null,@('subject',$png,'late','late','conversation','test',20,$false,$false))
$loaded=$cache.GetMethod('LoadImage').Invoke($null,@('subject','test','conversation'))
Check ($loaded.Key -eq $second.Key) 'F14 late unpromoted history does not replace current default'
Check ($null -eq $save.Invoke($null,@('bad',[byte[]](1..128),'bad','bad','conversation','test',20,$true,$true))) 'F09 bad image not saved as default'
Check ($cache.GetMethod('DeleteItem').Invoke($null,@($first,'test'))) 'F01 valid owned image moved to recycle'
Check (Test-Path $outside) 'F01 all real external fixture bytes preserved'
# F13: fill ordinary queue then complete a real accepted worker; completion must never be dropped.
$runtime=$a.GetType('AnimusForge.Illustrator.Core.IllustratorRuntime',$true)
$runtime.GetMethod('Initialize').Invoke($null,@())|Out-Null;$runtime.GetMethod('Tick').Invoke($null,@())|Out-Null
for($i=0;$i -lt 32;$i++) {$runtime.GetMethod('Post').Invoke($null,@([AuditFixture]::Noop()))|Out-Null}
Check (!$runtime.GetMethod('Post').Invoke($null,@([AuditFixture]::Noop()))) 'F13 normal queue retains capacity bound'
$start=$runtime.GetMethod('Start',$static).MakeGenericMethod([int]);$start.Invoke($null,@([AuditFixture]::Work(),[AuditFixture]::Complete()))|Out-Null
$critical=$runtime.GetField('Critical',$static).GetValue($null)
$watch=[Diagnostics.Stopwatch]::StartNew();while($critical.Count -eq 0 -and $watch.ElapsedMilliseconds -lt 5000){[Threading.Thread]::Sleep(5)}
$runtime.GetMethod('Tick').Invoke($null,@())|Out-Null
Check ([AuditFixture]::Completions -eq 1 -and $runtime.GetField('_workers',$static).GetValue($null) -eq 0) 'F13 worker completes once and releases admission despite full normal queue'
# F15 frame fence must span actual Tick invocations, not two actions in one queue drain.
$frameTask=$runtime.GetMethod('AfterFramesAsync',$static).Invoke($null,@(2,[Threading.CancellationToken]::None))
$runtime.GetMethod('Tick').Invoke($null,@())|Out-Null
Check (!$frameTask.IsCompleted) 'F15 one tick cannot satisfy two-frame capture fence'
$runtime.GetMethod('Tick').Invoke($null,@())|Out-Null
$frameTask.GetAwaiter().GetResult()
Check ($frameTask.IsCompleted) 'F15 capture fence completes after second tick'
$cancel=[Threading.CancellationTokenSource]::new();$cancelledFrames=$runtime.GetMethod('AfterFramesAsync',$static).Invoke($null,@(2,$cancel.Token));$cancel.Cancel()
Check (Rejected {$cancelledFrames.GetAwaiter().GetResult()}) 'F15 cancelled frame wait reaches cleanup without waiting for render'
$cancel.Dispose()
# F10: deliberately invalid preflight after SetLoading, without creating native GUI layers.
$popupType=$a.GetType('AnimusForge.Illustrator.UI.Overlays.IllustrationCardPopup',$true)
$popup=[Runtime.Serialization.FormatterServices]::GetUninitializedObject($popupType)
$vm=[Activator]::CreateInstance($a.GetType('AnimusForge.Illustrator.UI.Overlays.IllustrationCardVM',$true),[object[]]@($null,$null))
$popupType.GetField('_dataSource',$instance).SetValue($popup,$vm)
$popupType.GetMethod('ExecuteEncyclopediaGeneration',$instance).Invoke($popup,[object[]]@($null,$null))|Out-Null
Check (!$vm.IsLoading -and $vm.StatusText.Contains('准备失败')) 'F10 actual preflight exception restores idle with visible error'
$redrawVm=[Activator]::CreateInstance($a.GetType('AnimusForge.Illustrator.UI.Overlays.IllustrationCardVM',$true),[object[]]@($null,[AuditFixture]::Fail()))
$redrawVm.ExecuteRegenerate()
Check (!$redrawVm.IsLoading -and $redrawVm.StatusText.Contains('fixture redraw preflight')) 'F10 redraw context extraction failure is visible and does not escape UI command'
# F08: actual multipart over a local TCP mock; no external API request or credentials.
$url=[AuditFixture]::StartServer('{"data":[{"b64_json":"'+[Convert]::ToBase64String($png)+'"}]}')
$reference=$a.GetType('AnimusForge.Illustrator.Core.IllustrationReferenceImage',$true);$refs=[Array]::CreateInstance($reference,2)
$refs.SetValue([Activator]::CreateInstance($reference,@([Convert]::ToBase64String($jpeg),'玩家家族')),0)
$refs.SetValue([Activator]::CreateInstance($reference,@([Convert]::ToBase64String($png),'对方家族')),1)
$edit=$client.GetMethod('AttemptImagesEditsAsync',$static).Invoke($null,[object[]]@([string]$url,'fixture-model','fixture prompt','1024x1024','high','vivid',$refs,'',[Threading.CancellationToken]::None)).GetAwaiter().GetResult()
[AuditFixture]::ServerTask.GetAwaiter().GetResult()
Check ($edit.Item1) 'F08 actual edits HTTP path accepts mock image'
Check ([AuditFixture]::Body.Contains('quality') -and [AuditFixture]::Body.Contains('high')) 'F08 multipart includes chosen quality'
Check ([AuditFixture]::Body.Contains('玩家家族') -and [AuditFixture]::Body.Contains('对方家族')) 'F08 prompt maps both reference owners'
Check ([AuditFixture]::PngParts() -eq 2 -and [AuditFixture]::Body.Contains('image/png')) 'F08 both MIME-declared uploads are real PNG bytes'
# Encoded-color invariant: no setting, options field or caller flag can change RGB.
$ui=$a.GetType('AnimusForge.Illustrator.Engine.GauntletTextureLoader',$true)
$prepare=$ui.GetMethod('PrepareEncodedImageForUi',$static)
$palette=@([Drawing.Color]::Red,[Drawing.Color]::Blue,[Drawing.Color]::Gold,[Drawing.Color]::FromArgb(255,212,159,121),[Drawing.Color]::FromArgb(255,110,55,160),[Drawing.Color]::FromArgb(128,210,95,30))
$swatches=[Drawing.Bitmap]::new(6,1)
for($i=0;$i -lt $palette.Count;$i++){$swatches.SetPixel($i,0,$palette[$i])}
$colorStream=[IO.MemoryStream]::new();$swatches.Save($colorStream,[Drawing.Imaging.ImageFormat]::Png);$encoded=$colorStream.ToArray()
foreach($load in @('first-load','cache-reopen')) {
 $prepared=$prepare.Invoke($null,[object[]]@(,$encoded))
 Check ([Convert]::ToBase64String($prepared) -eq [Convert]::ToBase64String($encoded)) "Color PNG bytes unchanged on $load"
 $readStream=[IO.MemoryStream]::new([byte[]]$prepared);$decoded=[Drawing.Bitmap]::new($readStream)
 for($i=0;$i -lt $palette.Count;$i++){Check ($decoded.GetPixel($i,0).ToArgb() -eq $swatches.GetPixel($i,0).ToArgb()) "Color exact RGBA swatch $i on $load"}
 $decoded.Dispose();$readStream.Dispose()
}
$swatches.Dispose();$colorStream.Dispose()
$uiSource=Get-Content (Join-Path $src 'Engine\GauntletTextureLoader.cs') -Raw -Encoding UTF8
Check ($uiSource.Contains('bytes = PrepareEncodedImageForUi(bytes);') -and !$uiSource.Contains('SwapRedAndBlueInPng')) 'Color actual loader uses tested encoded-color path with no swap'
$settingsType=$a.GetType('AnimusForge.Illustrator.IllustratorSettings',$true)
Check ($null -eq $settingsType.GetProperty('FixColorChannels')) 'Color setting is deleted, not merely hidden'
$optionsType=$a.GetType('AnimusForge.Illustrator.Core.IllustrationOptions',$true)
Check ($null -eq $optionsType.GetProperty('FixColorChannels')) 'Color options snapshot has no color override'
Check ($prepare.GetParameters().Count -eq 1 -and $ui.GetMethod('LoadOrRegisterPngBytes').GetParameters().Count -eq 4) 'Color public loader and preparation have no correction argument'
$forbidden=@(Get-ChildItem $src -Recurse -Filter '*.cs' | Select-String -Pattern 'FixColorChannels|fixColorChannels|SwapRedAndBlueInPng')
Check ($forbidden.Count -eq 0) 'Color no setting, override, or PNG swap remains anywhere in module source'
# Banner ownership is not a held weapon; keep the complete snapshot but classify separately.
$bannerItem=[TaleWorlds.Core.ItemObject]::new('discipline_banner');$bannerItem.Type=[TaleWorlds.Core.ItemObject+ItemTypeEnum]::Banner
$swordItem=[TaleWorlds.Core.ItemObject]::new('real_sword');$swordItem.Type=[TaleWorlds.Core.ItemObject+ItemTypeEnum]::OneHandedWeapon
$gear=[TaleWorlds.Core.Equipment]::new();$gear[0]=[TaleWorlds.Core.EquipmentElement]::new($swordItem,$null,$null,$false);$gear[4]=[TaleWorlds.Core.EquipmentElement]::new($bannerItem,$null,$null,$false)
$profile=[Activator]::CreateInstance($a.GetType('AnimusForge.Illustrator.Context.HeroVisualProfile',$true))
$hero.GetMethod('ExtractWeapons',$static).Invoke($null,[object[]]@($profile,$gear,$null))|Out-Null
Check ($profile.BannerEquipmentDetails.Count -eq 1 -and !($profile.WeaponDetails -join ',').Contains('discipline_banner')) 'Portrait flag classified outside ordinary weapons'
Check (($profile.WeaponDetails -join ',').Contains('real_sword')) 'Portrait real weapon remains recorded'
Check ($gear[4].Item -eq $bannerItem) 'Portrait classification never deletes banner slot from complete equipment'
Check ($profile.BuildVisualSummary().Contains('非现场可见性证据')) 'Portrait summary distinguishes banner inventory from visibility'
$director=$a.GetType('AnimusForge.Illustrator.Core.VisualDirectorEngine',$true)
$planType=$a.GetType('AnimusForge.Illustrator.Core.IllustrationPromptPlan',$true)
$rules=$a.GetType('AnimusForge.Illustrator.Core.VisualFidelityRules',$true)
$portraitContract=[string]$rules.GetField('EncyclopediaPortrait',$static).GetRawConstantValue()
$portraitFacts=[string]($profile.BuildVisualSummary()+$portraitContract)
$plan=[Activator]::CreateInstance($planType,[object[]]@([string]'人物百科纪事',$portraitFacts,[string]'自然姿态',[string]''))
$scenePlan=[Activator]::CreateInstance($planType,[object[]]@('现场会话','现场旗手正在举旗','',''))
$guard=$director.GetMethod('ViolatesPortraitComposition',$static)
Check ($guard.Invoke($null,@('姿态测试哨兵，一手举旗另一手撑桌',$plan))) 'Portrait rejects flag and table tableau from director'
Check (!$guard.Invoke($null,@('现场旗手正在举旗',$scenePlan))) 'Portrait guard does not censor actual flags in scene modes'
Check (!$guard.Invoke($null,@('自然侧身，肩臂放松，以面部为视觉中心',$plan))) 'Portrait accepts natural posture'
$resolved=[string]$director.GetMethod('ResolveDirectorOutput',$static).Invoke($null,[object[]]@('姿态测试哨兵，一手举旗另一手撑桌',$plan,$null))
Check (!$resolved.Contains('姿态测试哨兵') -and $resolved.Contains($portraitContract)) 'Portrait actual resolution falls back locally without losing hard constraints'
$pose=[string]$popupType.GetMethod('GenerateDiversePoseDirective',$static).Invoke($null,[object[]]@($null))
Check ($pose.Contains('自然') -and !$pose.Contains('迈步') -and !$pose.Contains('下马')) 'Portrait default no longer chooses forced action templates'
$effective=[string]$client.GetMethod('BuildEffectivePrompt').Invoke($null,[object[]]@($resolved,'1024x1024','high','vivid',$null,$null,$true,100))
$contract=[string]$rules.GetField('Contract',$static).GetRawConstantValue()
Check (([regex]::Matches($effective,[regex]::Escape($contract))).Count -eq 1) 'Portrait final request includes fidelity contract once'
Check ($effective.Contains($portraitContract) -and $effective.Contains('不强制换动作')) 'Portrait constraints survive maximum randomness and final request assembly'
# User shield policy: fewer shields, no back-mounted shields in any mode.
$shieldGuard=$director.GetMethod('ViolatesShieldVisibility',$static)
foreach($bad in @('他的盾牌自然背负在身后','背后露出一面筝形盾','肩后是一面盾牌','a shield strapped to her back','behind him hangs a shield')) {
 Check ($shieldGuard.Invoke($null,@($bad,$scenePlan))) "Shield rejects back-mounted description: $bad"
}
Check ($shieldGuard.Invoke($null,@('手持盾牌的百科肖像',$plan))) 'Shield portrait rejects even hand-held shields by default'
Check (!$shieldGuard.Invoke($null,@('现场士兵举盾抵挡攻击',$scenePlan))) 'Shield scene hand-held action is not globally forbidden'
$shieldResolved=[string]$director.GetMethod('ResolveDirectorOutput',$static).Invoke($null,[object[]]@('背盾测试哨兵，盾牌背负在身后',$plan,$null))
Check (!$shieldResolved.Contains('背盾测试哨兵') -and $shieldResolved.Contains('所有模式禁止背盾')) 'Shield actual director resolution rejects backed shield and retains policy'
$shieldItem=[TaleWorlds.Core.ItemObject]::new('owned_shield');$shieldItem.Type=[TaleWorlds.Core.ItemObject+ItemTypeEnum]::Shield
$gear[1]=[TaleWorlds.Core.EquipmentElement]::new($shieldItem,$null,$null,$false)
$shieldProfile=[Activator]::CreateInstance($a.GetType('AnimusForge.Illustrator.Context.HeroVisualProfile',$true))
$hero.GetMethod('ExtractWeapons',$static).Invoke($null,[object[]]@($shieldProfile,$gear,$null))|Out-Null
Check ($gear[1].Item -eq $shieldItem -and ($shieldProfile.WeaponDetails -join ',').Contains('owned_shield')) 'Shield complete equipment and owned shield record remain intact'
Check (!($shieldProfile.WeaponDetails -join ',').Contains('自然背负') -and $portraitContract.Contains('默认不画盾牌')) 'Shield metadata no longer instructs default back carrying'
# Static ownership wiring checks supplement, not substitute for live native tests.
$popupSource=Get-Content (Join-Path $src 'UI\Overlays\IllustrationCardPopup.cs') -Raw -Encoding UTF8
$screen=Get-Content (Join-Path $src 'Engine\ScreenCaptureHelper.cs') -Raw -Encoding UTF8
$gallery=Get-Content (Join-Path $src 'UI\Gallery\IllustratorGalleryPopup.cs') -Raw -Encoding UTF8
$heroSource=Get-Content (Join-Path $src 'Context\HeroVisualExtractor.cs') -Raw -Encoding UTF8
Check (!$heroSource.Contains('参考图中的武器盔甲为被俘前装束') -and !$heroSource.Contains('不披挂完整战甲')) 'F04 prisoner template no longer overrides equipment'
Check ($gallery.Contains('catch { Close(); throw; }') -and $gallery.Contains('popup?.Close();')) 'F11 gallery partial construction and show rollback wired'
Check (!$popupSource.Contains('emblemTasks') -and $popupSource.Contains('await playerStage()') -and $popupSource.Contains('await partnerStage()')) 'F12 serial awaited references cannot leave running siblings'
Check ($screen.Contains('AfterFramesAsync(2, token)') -and $screen.Contains('entry.Item1.IsVisible = entry.Item2') -and $screen.Contains('SceneCaptureLock')) 'F15 hide wait restore is serialized across captures'
Check (!$popupSource.Contains('CaptureConversationSceneBase64(768)') -and $popupSource.Contains('CaptureConversationSceneWithoutUiAsync(token)')) 'F15 both first and redraw generation use UI-free capture pipeline'
Write-Host "Full audit checks=$checks failures=$failures; local TCP mock only; no GPU/live save/provider acceptance. Fixture=$fixture"
if($failures -gt 0){exit 1}

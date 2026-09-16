param(
    [Parameter(Mandatory=$true)][string]$AssemblyPath,
    [string]$GameRoot = 'F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord'
)
$ErrorActionPreference = 'Stop'
$module = Split-Path -Parent $PSScriptRoot
$src = Join-Path $module 'src'
$dirs = @((Split-Path -Parent (Resolve-Path $AssemblyPath).Path),
    (Join-Path $GameRoot 'bin\Win64_Shipping_Client'),
    (Join-Path $GameRoot 'Modules\SandBox\bin\Win64_Shipping_Client'),
    (Join-Path $GameRoot 'Modules\Bannerlord.Harmony\bin\Win64_Shipping_Client'),
    (Join-Path $GameRoot 'Modules\Bannerlord.MBOptionScreen\bin\Win64_Shipping_Client'),
    (Join-Path $module 'bin\Win64_Shipping_Client'))
$handler = [ResolveEventHandler]{param($sender,$eventArgs)
    $name = ([Reflection.AssemblyName]::new($eventArgs.Name)).Name
    foreach($dir in $dirs) {
        $candidate = Join-Path $dir "$name.dll"
        if(Test-Path $candidate) { return [Reflection.Assembly]::LoadFrom($candidate) }
    }
    return $null
}
[AppDomain]::CurrentDomain.add_AssemblyResolve($handler)
$assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
$checks=0; $failures=0
function Check([bool]$ok,[string]$name) {
    $script:checks++
    if($ok) { Write-Host "PASS $name" } else { $script:failures++; Write-Host "FAIL $name" }
}
$flags = [Reflection.BindingFlags]'NonPublic,Static'
$extractor = $assembly.GetType('AnimusForge.Illustrator.Context.HeroVisualExtractor',$true)
$color = $extractor.GetMethod('ResolveColorName')
Check (([string]$color.Invoke($null,@([uint32]0xCCC3AB))).Contains('米色')) 'beige is not copper gold'
Check (([string]$color.Invoke($null,@([uint32]0xFFB53E))).Contains('橙色')) 'orange is a color not metallic gold'
Check (([string]$color.Invoke($null,@([uint32]0xFFFFFF))).Contains('白色')) 'white remains white'
Check (([string]$color.Invoke($null,@([uint32]0x000000))).Contains('黑色')) 'black remains black'
$profile = [Activator]::CreateInstance($assembly.GetType('AnimusForge.Illustrator.Context.HeroVisualProfile',$true))
$profile.HeroName='Fixture'
$profile.EquipmentSource='Fixture current equipment'
$profile.HeadgearDetail='已佩戴头部装备：呆喵（MOD物品）；头部入镜必须保留'
$profile.WeaponDetails.Add('盾牌: Fixture shield')
$facts=$profile.BuildVisualSummary()
Check ($facts.Contains('【当前头戴装备】') -and $facts.Contains('呆喵')) 'MOD headgear survives visual summary'
Check ($facts.Contains('Fixture current equipment')) 'equipment provenance survives summary'
Check ($facts.Contains('Fixture shield')) 'equipped shield survives summary'
$director=$assembly.GetType('AnimusForge.Illustrator.Core.VisualDirectorEngine',$true)
$compose=$director.GetMethod('ComposeFinalPrompt',$flags)
$prompt=[string]$compose.Invoke($null,@('Fixture artistic direction',$facts))
$rule=$assembly.GetType('AnimusForge.Illustrator.Core.VisualFidelityRules',$true).GetField('Contract',$flags).GetRawConstantValue()
Check ($prompt.Contains($facts)) 'director composition preserves exact raw facts'
Check ($prompt.EndsWith($rule)) 'final director prompt ends with fidelity contract'
Check ($rule.Contains('不得为展示发型摘盔')) 'headgear beats hairstyle visibility'
Check ($rule.Contains('不得把已装备盾牌替换为旗帜')) 'shield is not replaced with flag'
Check ($rule.Contains('旗帜只在现场/事件明确有旗帜证据时出现')) 'flag requires independent scene evidence'
Check ($rule.Contains('Clan.Color/Color2')) 'livery metadata is not heraldry base color'
$effective=$assembly.GetType('AnimusForge.Illustrator.Core.UniversalOpenAiImageClient',$true).GetMethod('BuildEffectivePrompt')
foreach($chat in @($false,$true)) {
    foreach($random in @(0,100)) {
        $text=[string]$effective.Invoke($null,@($prompt,'1024x1024','high','natural','fixture style','fixture negative',$chat,$random))
        Check ($text.EndsWith($rule) -and $text.Contains('呆喵')) "fidelity follows style and randomness chat=$chat random=$random"
    }
}
$hero=Get-Content (Join-Path $src 'Context\HeroVisualExtractor.cs') -Raw -Encoding UTF8
$popup=Get-Content (Join-Path $src 'UI\Overlays\IllustrationCardPopup.cs') -Raw -Encoding UTF8
$env=Get-Content (Join-Path $src 'Context\EnvironmentVisualExtractor.cs') -Raw -Encoding UTF8
$snapshot=Get-Content (Join-Path $src 'Context\ConversationEquipmentSnapshot.cs') -Raw -Encoding UTF8
Check (!$hero.Contains('if (i == Banner.BackgroundDataIndex) continue;')) 'background layer is no longer skipped'
Check ($hero.Contains('第二底色#')) 'second background color is retained'
Check (!$hero.Contains('家族旗帜识别色：主色')) 'clan metadata is not advertised as banner palette'
Check ($popup.Contains('equipmentCodeOverride: playerEquipmentCode') -and $popup.Contains('equipmentCodeOverride: partnerEquipmentCode')) 'both portraits consume frozen equipment code'
Check (!$popup.Contains('Hero.MainHero, useCivilian: true')) 'player portrait is not forced to civilian outfit'
Check ($snapshot.Contains('agent.Equipment[slot]')) 'live weapon slots account for discarded shields'
Check ($snapshot.Contains('ReferenceEquals(agent.Character, hero.CharacterObject)')) 'agent snapshot checks exact hero identity'
Check ($snapshot.Contains('AssertMainThread()') -and !$snapshot.Contains('mission.Agents')) 'snapshot is main-thread and avoids whole-mission scan'
Check (!$env.Contains('双方军队的旌旗仪仗在身后列阵隐约可见')) 'field template no longer invents flags'
$conv=Get-Content (Join-Path $src 'Context\ConversationContextExtractor.cs') -Raw -Encoding UTF8
$capture=Get-Content (Join-Path $src 'Engine\ScreenCaptureHelper.cs') -Raw -Encoding UTF8
Check ($popup.Contains('string equipmentCode = portrait.EquipmentCode;')) 'encyclopedia reads displayed widget equipment'
Check (!$popup.Contains('hero.IsNotable || (hero.IsNoncombatant')) 'encyclopedia does not guess outfit from occupation'
Check (!$popup.Contains('preCapturedBase64 = ScreenCaptureHelper.CaptureWidgetBase64')) 'encyclopedia does not reuse a stale screenshot'
Check ($popup.Contains('equipmentCodeOverride: equipmentCode')) 'encyclopedia full equipment drives independent portrait'
Check ($conv.Contains('? Mission.Current.DoesMissionRequireCivilianEquipment')) 'mission dress code precedes enemy and siege heuristics'
Check ($conv.Contains('bool partnerCivilian = isCivilian;')) 'player and hero use same scene fallback rule'
Check ($conv.Contains('CaptureCharacter(partnerChar, out source, out body, out var appearance)')) 'NPC face and equipment captured from same agent'
Check ($conv.Contains('ApplyEquipmentSnapshot(profile, snapshot, source)')) 'NPC has same full equipment fact extraction'
Check ($snapshot.Contains('slot < EquipmentIndex.NumAllWeaponSlots')) 'fifth live weapon slot is included'
Check ($hero.Contains('i < EquipmentIndex.NumAllWeaponSlots')) 'fifth weapon slot is included in text facts'
Check ($popup.Contains('玩家完整装备离屏立绘失败') -and $popup.Contains('对方完整装备离屏立绘失败')) 'missing portrait stops rather than silently downgrades'
Check ($popup.Contains('请先开启离屏渲染')) 'disabled offscreen rendering is reported not bypassed'
[void][Reflection.Assembly]::LoadFrom((Join-Path $GameRoot 'bin\Win64_Shipping_Client\TaleWorlds.Core.dll'))
$equipment = [TaleWorlds.Core.Equipment]::new()
for($slot=0; $slot -lt 12; $slot++) {
    $item=[TaleWorlds.Core.ItemObject]::new("fixture_slot_$slot")
    $equipment[$slot]=[TaleWorlds.Core.EquipmentElement]::new($item,$null,$null,$false)
}
$code=$equipment.CalculateEquipmentCode()
for($slot=0; $slot -lt 12; $slot++) {
    Check ($code.Contains("+$slot-fixture_slot_$slot-@null")) "native full snapshot serialization preserves slot $slot"
}
$empty = [TaleWorlds.Core.Equipment]::new()
$apply=$extractor.GetMethod('ApplyEquipmentSnapshot',$flags)
$apply.Invoke($null,@($profile,$empty,'Fixture empty scene snapshot')) | Out-Null
Check ($profile.EquipmentCode.Contains('+5-@null-@null')) 'empty head slot is explicit not replaced by alternate outfit'
Check (!$profile.HeadgearDetail.Contains('呆喵')) 'new snapshot clears stale previous headgear'
Check ($profile.WeaponDetails.Count -eq 1 -and $profile.WeaponDetails[0].Contains('无盾牌')) 'new empty snapshot clears stale previous shield'
Write-Host "Fidelity checks=$checks failures=$failures (offline only; no GPU or provider acceptance)"
if($failures -gt 0) { exit 1 }

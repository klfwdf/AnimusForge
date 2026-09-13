param(
    [string]$AssemblyPath = "",
    [ValidateSet("Debug", "Release")]
    [string]$Configuration = "Release",
    [switch]$Baseline,
    [switch]$SkipBuild
)
$ErrorActionPreference = 'Stop'
$script:failures = 0
$script:checks = 0
function Assert-True([bool]$Condition, [string]$Name) {
    $script:checks++
    if (-not $Condition) { $script:failures++; Write-Host "FAIL $Name" } else { Write-Host "PASS $Name" }
}

$root = Split-Path -Parent $PSScriptRoot
$module = Join-Path $root 'extensions\AnimusForge.Illustrator'
$project = Join-Path $module 'src\AnimusForge.Illustrator.csproj'

if (-not $SkipBuild) {
    foreach ($api in @('1.3', '1.4')) {
        $out = Join-Path $module "bin\test\$api"
        $obj = Join-Path $module "obj\test\$api"
        dotnet build $project -c $Configuration -p:BannerlordApi=$api -p:OutputPath="$out\" -p:BaseIntermediateOutputPath="$obj\" --verbosity:minimal | Out-Host
        Assert-True ($LASTEXITCODE -eq 0) "build BannerlordApi=$api"
    }
    if ([string]::IsNullOrWhiteSpace($AssemblyPath)) {
        $AssemblyPath = Join-Path $module "bin\test\1.4\AnimusForge.Illustrator.dll"
    }
}
elseif ([string]::IsNullOrWhiteSpace($AssemblyPath)) {
    $AssemblyPath = Join-Path $module 'bin\Win64_Shipping_Client\AnimusForge.Illustrator.dll'
}

$assemblyPathResolved = (Resolve-Path $AssemblyPath).Path
$dependencyDirs = @(
    (Split-Path -Parent $assemblyPathResolved),
    (Join-Path $module 'bin\Win64_Shipping_Client'),
    'F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client',
    'F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\Native\bin\Win64_Shipping_Client',
    'F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord\Modules\SandBox\bin\Win64_Shipping_Client',
    (Join-Path $root 'bin\Debug\net472'),
    (Join-Path $root "bin\$Configuration\single_module_artifacts\versions\1.4"),
    (Join-Path $root "bin\$Configuration\single_module_artifacts\versions\1.3")
)
$handler = [System.ResolveEventHandler]{
    param($sender, $args)
    $name = (New-Object Reflection.AssemblyName($args.Name)).Name
    foreach ($dir in $dependencyDirs) {
        $candidate = Join-Path $dir "$name.dll"
        if (Test-Path $candidate) { return [Reflection.Assembly]::LoadFrom($candidate) }
    }
    return $null
}
[AppDomain]::CurrentDomain.add_AssemblyResolve($handler)
foreach ($name in @('System.Numerics.Vectors', 'Newtonsoft.Json', '0Harmony', 'MCMv5', 'TaleWorlds.Library', 'TaleWorlds.Core', 'TaleWorlds.Localization', 'TaleWorlds.ObjectSystem', 'TaleWorlds.DotNet', 'TaleWorlds.Engine', 'TaleWorlds.TwoDimension', 'TaleWorlds.InputSystem', 'TaleWorlds.ScreenSystem', 'TaleWorlds.GauntletUI', 'TaleWorlds.GauntletUI.Data', 'TaleWorlds.Engine.GauntletUI', 'TaleWorlds.MountAndBlade', 'TaleWorlds.MountAndBlade.View', 'TaleWorlds.MountAndBlade.GauntletUI.Widgets', 'TaleWorlds.CampaignSystem', 'TaleWorlds.CampaignSystem.ViewModelCollection', 'SandBox.GauntletUI', 'SandBox.View', 'AnimusForge')) {
    foreach ($dir in $dependencyDirs) {
        $candidate = Join-Path $dir "$name.dll"
        if (Test-Path $candidate) {
            try { [void][Reflection.Assembly]::LoadFrom($candidate) } catch {}
            break
        }
    }
}
$assembly = [Reflection.Assembly]::LoadFrom($assemblyPathResolved)
$director = $assembly.GetType('AnimusForge.Illustrator.Core.VisualDirectorEngine', $true)
$synthesize = $director.GetMethod('SynthesizeRuleBasedPrompt', [Reflection.BindingFlags]'NonPublic,Static')
Assert-True ($null -ne $synthesize) 'rule-based prompt method is available'

$male = '【人物与至高地位】审查角色 (瓦兰迪亚文化, 男性, 约30岁, 身份: 封建贵族领主 (Feudal Noble Lord/Lady))。真实穿戴：黑色锁甲和皮靴。'
$result = [string]$synthesize.Invoke($null, [object[]]@($male))
Assert-True (-not $result.Contains('宗族女领主')) 'male identity is not inferred from Lady in bilingual role'
Assert-True ($result.Contains('审查角色') -and $result.Contains('黑色锁甲')) 'identity and actual equipment survive offline fallback'
$result = [string]$synthesize.Invoke($null, [object[]]@('瓦兰迪亚文化，男性，身穿黑色锁甲和皮靴。'))
Assert-True (-not $result.Contains('天守阁')) 'Chinese conjunction does not classify Japanese culture'
$result = [string]$synthesize.Invoke($null, [object[]]@('【核心事件】奥尼拉遭受围攻，投石机破城，要求战争群像。'))
Assert-True ($result.Contains('奥尼拉') -and $result.Contains('投石机破城')) 'weekly event and location survive fallback'
Assert-True (-not $result.Contains('百战勇士肖像')) 'weekly fallback does not become a generic portrait'

$commandTypes = @(
    'AnimusForge.Illustrator.UI.Overlays.IllustrationCardVM',
    'AnimusForge.Illustrator.UI.Patches.WeeklyReportIllustrationOverlayVM',
    'AnimusForge.Illustrator.UI.Gallery.IllustratorGalleryPopupVM',
    'AnimusForge.Illustrator.UI.Gallery.IllustrationItemVM'
)
$availableCommands = New-Object 'System.Collections.Generic.HashSet[string]'
foreach ($typeName in $commandTypes) {
    $type = $assembly.GetType($typeName, $true)
    foreach ($method in $type.GetMethods([Reflection.BindingFlags]'Public,Instance')) {
        if ($method.Name.StartsWith('Execute')) { [void]$availableCommands.Add($method.Name) }
    }
}
foreach ($required in @('ExecuteRegenerate', 'ExecuteTogglePrompt', 'ExecuteCopyPrompt', 'ExecuteOpenGallery', 'ExecuteClose', 'ExecuteSetDefault', 'ExecuteDelete', 'ExecuteOpenFolder', 'ExecuteSelect')) {
    Assert-True ($availableCommands.Contains($required)) "command method $required"
}

if (-not $Baseline) {
    foreach ($file in @((Join-Path $module 'SubModule.xml')) + @(Get-ChildItem (Join-Path $module 'GUI\Prefabs') -Filter '*.xml' | ForEach-Object FullName)) {
        $document = New-Object System.Xml.XmlDocument
        $document.Load($file)
        Assert-True ($null -ne $document.DocumentElement) "XML $([IO.Path]::GetFileName($file))"
    }

    foreach ($file in Get-ChildItem (Join-Path $module 'GUI\Prefabs') -Filter '*.xml') {
        $commands = [regex]::Matches((Get-Content $file.FullName -Raw), 'Command\.Click="([^"]+)"') | ForEach-Object { $_.Groups[1].Value }
        foreach ($command in $commands) {
            Assert-True ($availableCommands.Contains($command)) "prefab binding $($file.Name):$command"
        }
    }
}
Write-Host "$($script:checks) checks, $($script:failures) failures"
if ($script:failures -gt 0) { exit 1 }

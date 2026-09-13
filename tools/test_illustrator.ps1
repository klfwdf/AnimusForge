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

# 文化头饰：阿塞莱君主必须使用缠头巾形制，严禁西式王冠
$result = [string]$synthesize.Invoke($null, [object[]]@('阿塞莱文化，男性，身份：苏丹/最高统治者 (Sovereign Monarch)，身穿丝绸长袍。'))
Assert-True ($result.Contains('缠头巾')) 'aserai monarch fallback uses turban crown'
Assert-True (-not ($result.Contains('庄严王冠') -and -not $result.Contains('严禁'))) 'aserai monarch does not fall back to western crown'

$srcDir = Join-Path $module 'src'
$screenCapture = Get-Content (Join-Path $srcDir 'Engine\ScreenCaptureHelper.cs') -Raw -Encoding UTF8
Assert-True ($screenCapture.Contains('ExtractBannerOffscreenAsync')) 'banner offscreen extraction exists'
Assert-True ($screenCapture.Contains('ExtractHeroPortraitOffscreenAsync')) 'hero portrait offscreen extraction exists'
Assert-True ($screenCapture.Contains('CaptureConversationSceneBase64')) 'conversation scene band capture exists'
Assert-True ($screenCapture.Contains('BannerTableau') -and $screenCapture.Contains('CharacterTableau')) 'tableau-view offscreen rendering used for banner/portrait'
Assert-True ($screenCapture.Contains('IsRenderTarget') -and $screenCapture.Contains('Skipping GPU render-target')) 'render-target textures are never pixel-read directly'
Assert-True (!$screenCapture.Contains('TransformRenderTargetToResource(')) 'no render-target transform call (crash-prone on shared textures)'
Assert-True ($screenCapture.Contains('RunOnGameThreadAsync')) 'engine access is dispatched to main thread'

$weeklySrc = Get-Content (Join-Path $srcDir 'Context\WeeklyReportContextExtractor.cs') -Raw -Encoding UTF8
Assert-True ($weeklySrc.Contains('ResolveProtagonistHero')) 'weekly report resolves protagonist from text'
Assert-True ($weeklySrc.Contains('AllAliveHeroes')) 'weekly report scans campaign heroes'
Assert-True ($weeklySrc.Contains('eventAnchored: true')) 'weekly report environment is event-anchored'
Assert-True ($weeklySrc.Contains('ApplyEventSceneAnchoring')) 'weekly report scene fields anchored by theme'

$directorSrc = Get-Content (Join-Path $srcDir 'Core\VisualDirectorEngine.cs') -Raw -Encoding UTF8
Assert-True ($directorSrc.Contains('IReadOnlyList<IllustrationReferenceImage>')) 'director accepts labeled reference image list'
Assert-True ($directorSrc.Contains('纹章铁律')) 'system prompt enforces banner-reference fidelity'
Assert-True ($directorSrc.Contains('王权头饰铁律')) 'system prompt enforces culture-specific crowns'

$clientSrc = Get-Content (Join-Path $srcDir 'Core\UniversalOpenAiImageClient.cs') -Raw -Encoding UTF8
Assert-True ($clientSrc.Contains('IReadOnlyList<IllustrationReferenceImage>')) 'image client accepts reference image list'
Assert-True ($clientSrc.Contains('EnableReferenceImageForGeneration')) 'image client honors reference-image toggle'
Assert-True ($clientSrc.Contains('negativePrompt')) 'image client injects negative prompt'

$convSrc = Get-Content (Join-Path $srcDir 'Context\ConversationContextExtractor.cs') -Raw -Encoding UTF8
Assert-True ($convSrc.Contains('playerIsMounted') -and $convSrc.Contains('步行立于地面')) 'conversation pose covers mounted-player vs on-foot partner'

$envSrc = Get-Content (Join-Path $srcDir 'Context\EnvironmentVisualExtractor.cs') -Raw -Encoding UTF8
Assert-True ($envSrc.Contains('GetYear') -and $envSrc.Contains('GetDayOfSeason')) 'environment reads full calendar year and day-of-season'
Assert-True ($envSrc.Contains('DateLabel') -and $envSrc.Contains('纪元时间')) 'calendar date is included in prompt context summary'

$settingsSrc = Get-Content (Join-Path $srcDir 'Settings\IllustratorSettings.cs') -Raw -Encoding UTF8
Assert-True ($settingsSrc.Contains('dark-epic') -and $settingsSrc.Contains('cinematic') -and $settingsSrc.Contains('custom')) 'style dropdown offers dark-epic/cinematic/custom prompt options'
Assert-True ($settingsSrc.Contains('CustomStylePrompt')) 'custom style prompt text setting exists'
Assert-True ($settingsSrc.Contains('EditCustomStylePrompt') -and $settingsSrc.Contains('EditNegativePrompt')) 'prompt settings use button-opened long-text editors'
Assert-True ($settingsSrc.Contains('ShowLongTextEditor')) 'prompt editors reuse main-mod long text editor'

Assert-True ($clientSrc.Contains('customStyleHint')) 'image client injects prompt-level style hints'

$imageClient = $assembly.GetType('AnimusForge.Illustrator.Core.UniversalOpenAiImageClient', $true)
$genOverload = $imageClient.GetMethods([Reflection.BindingFlags]'Public,Static') | Where-Object {
    $_.Name -eq 'GenerateImageAsync' -and $_.GetParameters().Count -ge 2 -and $_.GetParameters()[1].ParameterType.Name -like 'IReadOnlyList*'
}
Assert-True ($null -ne $genOverload) 'GenerateImageAsync multi-reference overload exists'

$directorMethods = $director.GetMethods([Reflection.BindingFlags]'Public,Static') | Where-Object { $_.Name -eq 'ExpandToDetailedPromptAsync' }
$hasListOverload = $false
foreach ($m in $directorMethods) {
    $ps = $m.GetParameters()
    if ($ps.Count -ge 2 -and $ps[1].ParameterType.Name -like 'IReadOnlyList*') { $hasListOverload = $true }
}
Assert-True $hasListOverload 'ExpandToDetailedPromptAsync multi-reference overload exists'

$buildChatPrompt = $imageClient.GetMethod('BuildChatImagePrompt', [Reflection.BindingFlags]'Public,Static')
Assert-True ($null -ne $buildChatPrompt) 'BuildChatImagePrompt available'
$styled = [string]$buildChatPrompt.Invoke($null, [object[]]@('test scene', '1024x1024', 'high', $null, 'dark epic realism'))
Assert-True ($styled.Contains('dark epic realism') -and $styled.Contains('hyper-detailed')) 'chat prompt injects custom style hint and high quality directive'

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

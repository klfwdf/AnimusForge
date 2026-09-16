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
$synthesize = $director.GetMethods([Reflection.BindingFlags]'NonPublic,Static') | Where-Object {
    $_.Name -eq 'SynthesizeRuleBasedPrompt' -and $_.GetParameters().Count -eq 1 -and $_.GetParameters()[0].ParameterType -eq [string]
} | Select-Object -First 1
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

# 文化头饰：放宽后不再凭空发明头饰；阿塞莱君主事实保留且不得出现西式王冠
$result = [string]$synthesize.Invoke($null, [object[]]@('阿塞莱文化，男性，身份：苏丹/最高统治者 (Sovereign Monarch)，身穿丝绸长袍。'))
Assert-True ($result.Contains('苏丹')) 'aserai monarch facts survive fallback'
Assert-True (-not $result.Contains('西式王冠') -and -not $result.Contains('庄严王冠')) 'aserai monarch fallback does not invent western crown'

$srcDir = Join-Path $module 'src'
$screenCapture = Get-Content (Join-Path $srcDir 'Engine\ScreenCaptureHelper.cs') -Raw -Encoding UTF8
Assert-True (-not $screenCapture.Contains('ExtractBannerOffscreenAsync') -and -not $screenCapture.Contains('ExtractEmblemOffscreenAsync')) 'legacy banner/emblem stage entry points removed'
$composer = Get-Content (Join-Path $srcDir 'Engine\BannerEmblemComposer.cs') -Raw -Encoding UTF8 -ErrorAction SilentlyContinue
Assert-True ($composer -and $composer.Contains('NativeBannerPipeline') -and !$composer.Contains('TintIconCell')) 'banner reference uses native renderer without CPU shader reconstruction'
Assert-True ($screenCapture.Contains('ExtractHeroPortraitOffscreenAsync')) 'hero portrait offscreen extraction exists'
Assert-True ($screenCapture.Contains('CaptureConversationSceneBase64')) 'conversation scene band capture exists'
Assert-True ($screenCapture.Contains('BannerTableauWidget') -and $screenCapture.Contains('CharacterTableauWidget')) 'stage-layer widgets used for banner/portrait offscreen render'
Assert-True ($screenCapture.Contains('IsRenderTarget') -and $screenCapture.Contains('Skipping GPU render-target')) 'render-target textures are never pixel-read directly'
Assert-True (!$screenCapture.Contains('TransformRenderTargetToResource(')) 'no render-target transform call (crash-prone on shared textures)'
Assert-True (!$screenCapture.Contains('new BannerTableau(') -and !$screenCapture.Contains('new CharacterTableau(')) 'no self-created tableau scenes (render-thread race crash)'
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
Assert-True ($directorSrc.Contains('IllustrationPromptPlan')) 'director separates hard facts from open art direction'
Assert-True ($directorSrc.Contains('ComposeFinalPrompt')) 'director programmatically preserves hard facts after LLM expansion'
$sysPromptLiteral = [regex]::Match($directorSrc, 'private const string SystemPrompt\s*=\s*([^;]+);').Groups[1].Value
Assert-True (-not $sysPromptLiteral.Contains('伦勃朗')) 'global director prompt no longer hard-locks one artist blend'
Assert-True ($directorSrc.Contains('classic-oil')) 'classic oil style available as optional preset branch'
Assert-True ($clientSrc.Contains('BuildEffectivePrompt')) 'image client exposes the actual final prompt sent to providers'
Assert-True ($clientSrc.Contains('MultipartFormDataContent') -and $clientSrc.Contains('/images/edits')) 'Images protocol can actually send reference images through edits'
Assert-True ($clientSrc.Contains('ActualRefImages')) 'generation diagnostics distinguish requested and actually sent references'
Assert-True (-not $screenCapture.Contains('Directory.GetFiles(tempDir, "af_offscreen_*"')) 'offscreen requests do not delete concurrent request files'
Assert-True ($screenCapture.Contains('CancellationToken cancellationToken')) 'offscreen stage supports scope cancellation'
Assert-True ($convSrc.Contains('RecentDialogueHistory') -and $convSrc.Contains('BuildRecentDialogueHistory')) 'conversation illustration includes recent dialogue history'
Assert-True ($convSrc.Contains('maxRounds: 3')) 'conversation illustration is limited to the most recent three rounds'
Assert-True ($weeklySrc.Contains('WeeklyReportIllustrationSnapshot')) 'weekly report uses the host structured event snapshot'
Assert-True (-not $weeklySrc.Contains('protagonist?.CurrentSettlement ?? protagonist?.HomeSettlement ?? Settlement.CurrentSettlement')) 'weekly report never invents retrospective location from current positions'

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
# Review regressions use the actual compiled methods; no game/native objects are instantiated.
$privateStatic = [Reflection.BindingFlags]'NonPublic,Static'
$privateInstance = [Reflection.BindingFlags]'NonPublic,Instance'
$settingsType = $assembly.GetType('AnimusForge.Illustrator.IllustratorSettings', $true)
$randomProperty = $settingsType.GetProperty('Randomness')
$randomAttribute = $randomProperty.GetCustomAttributesData() | Where-Object { $_.AttributeType.Name -eq 'SettingPropertyIntegerAttribute' } | Select-Object -First 1
Assert-True ($null -ne $randomAttribute -and $randomProperty.GetValue([Activator]::CreateInstance($settingsType)) -eq 0) 'randomness integer slider defaults to legacy zero'
Assert-True ($randomAttribute.ConstructorArguments[0].Value -eq '随机' -and $randomAttribute.ConstructorArguments[1].Value -eq 0 -and $randomAttribute.ConstructorArguments[2].Value -eq 100 -and $randomAttribute.ConstructorArguments[3].Value -eq '0') 'randomness slider uses 0-100 plain integer display'
$imageClientType = $assembly.GetType('AnimusForge.Illustrator.Core.UniversalOpenAiImageClient', $true)
$effectiveMethod = $imageClientType.GetMethod('BuildEffectivePrompt')
$optionsType = $assembly.GetType('AnimusForge.Illustrator.Core.IllustrationOptions', $true)
$optionsConstructor = $optionsType.GetConstructors($privateInstance)[0]
foreach ($chatProtocol in @($false, $true)) {
    foreach ($randomness in @(-1, 0, 1, 50, 100, 150)) {
        $settings = [Activator]::CreateInstance($settingsType)
        $randomProperty.SetValue($settings, $randomness)
        $options = $optionsConstructor.Invoke([object[]]@($settings, '', '', ''))
        $captured = $optionsType.GetProperty('Randomness').GetValue($options)
        $expected = [Math]::Max(0, [Math]::Min(100, $randomness))
        Assert-True ($captured -eq $expected) "randomness $randomness is captured and clamped (chat=$chatProtocol)"
        $basePrompt = '已确认：黑色锁甲；本次重绘采用俯拍。'
        $prompt = [string]$effectiveMethod.Invoke($null, [object[]]@($basePrompt, '1024x1024', '', '', '', '', $chatProtocol, $captured))
        if ($expected -eq 0) {
            $legacy = [string]$effectiveMethod.Invoke($null, [object[]]@($basePrompt, '1024x1024', '', '', '', '', $chatProtocol, [Type]::Missing))
            Assert-True ($prompt -eq $legacy -and !$prompt.Contains('艺术表现随机指导') -and !$prompt.Contains('参考还原度约束')) "zero preserves legacy prompt without extra clause (chat=$chatProtocol)"
        } else {
            Assert-True ($prompt.Contains('黑色锁甲') -and $prompt.Contains('俯拍') -and $prompt.Contains('重绘必须遵循本次换镜头指导')) "randomness $randomness preserves facts and redraw (chat=$chatProtocol)"
            Assert-True ($prompt.Contains('人物身份立绘只用于身份与装备') -and $prompt.Contains('缺少场景参考图时，不从身份立绘补造场景') -and $prompt.Contains('不得虚构物体、人物或事件')) "randomness $randomness respects reference roles (chat=$chatProtocol)"
            $intensity = if ($expected -eq 100) { '最大程度探索' } else { "$expected/100" }
            Assert-True ($prompt.Contains($intensity) -and !$prompt.Contains('参考还原度约束')) "randomness $randomness increases variation instead of similarity (chat=$chatProtocol)"
        }
    }
}

$weeklyPatch = $assembly.GetType('AnimusForge.Illustrator.UI.Patches.WeeklyReportPopupIllustrationPatch', $true)
$scopeType = $assembly.GetType('AnimusForge.Illustrator.Core.IllustrationScope', $true)
$runtimeType = $assembly.GetType('AnimusForge.Illustrator.Core.IllustratorRuntime', $true)
$scopeField = $weeklyPatch.GetField('_scope', $privateStatic)
$closingField = $weeklyPatch.GetField('_closing', $privateStatic)
$eventField = $weeklyPatch.GetField('_currentEventKey', $privateStatic)
$mainThreadField = $runtimeType.GetField('_mainThread', $privateStatic)
$closeForScope = $weeklyPatch.GetMethod('CloseOverlayForScope', $privateStatic)
$oldScope = [Runtime.Serialization.FormatterServices]::GetUninitializedObject($scopeType)
$newScope = [Runtime.Serialization.FormatterServices]::GetUninitializedObject($scopeType)
$requestSource = New-Object Threading.CancellationTokenSource
$scopeType.GetField('_request', $privateInstance).SetValue($newScope, $requestSource)
$savedThread = $mainThreadField.GetValue($null)
try {
    $mainThreadField.SetValue($null, [Environment]::CurrentManagedThreadId)
    $scopeField.SetValue($null, $newScope)
    $closingField.SetValue($null, $false)
    $eventField.SetValue($null, 'new-weekly-event')
    [void]$closeForScope.Invoke($null, [object[]]@($oldScope))
    Assert-True ([object]::ReferenceEquals($scopeField.GetValue($null), $newScope) -and $eventField.GetValue($null) -eq 'new-weekly-event' -and !$closingField.GetValue($null)) 'late old weekly scope cannot close replacement'
    [void]$closeForScope.Invoke($null, [object[]]@($newScope))
    Assert-True ($null -eq $scopeField.GetValue($null) -and $null -eq $eventField.GetValue($null) -and $scopeType.GetField('_closed', $privateInstance).GetValue($newScope) -and $requestSource.IsCancellationRequested) 'current weekly scope still closes and cancels normally'
    [void]$closeForScope.Invoke($null, [object[]]@($oldScope))
    Assert-True ($null -eq $scopeField.GetValue($null)) 'late weekly close remains idempotent'
}
finally {
    $scopeField.SetValue($null, $null)
    $eventField.SetValue($null, $null)
    $closingField.SetValue($null, $false)
    $mainThreadField.SetValue($null, $savedThread)
    $requestSource.Dispose()
}

$fixtureRoot = Join-Path $root ('artifacts\tests\illustrator-review-' + [Guid]::NewGuid().ToString('N'))
[void][IO.Directory]::CreateDirectory($fixtureRoot)
$prefixA = 'af_offscreen_' + [Guid]::NewGuid().ToString('N')
$prefixB = 'af_offscreen_' + [Guid]::NewGuid().ToString('N')
$fileA = Join-Path $fixtureRoot ($prefixA + '.png')
$fileB = Join-Path $fixtureRoot ($prefixB + '.png')
$captureType = $assembly.GetType('AnimusForge.Illustrator.Engine.ScreenCaptureHelper', $true)
$cleanupMethod = $captureType.GetMethod('CleanupTempArtifacts')
try {
    [IO.File]::WriteAllText($fileA, 'completed request A')
    [IO.File]::WriteAllText($fileB, 'request B awaiting read')
    [void]$cleanupMethod.Invoke($null, [object[]]@([string]$fixtureRoot, [string]$prefixA))
    Assert-True (!(Test-Path -LiteralPath $fileA) -and [IO.File]::ReadAllText($fileB) -eq 'request B awaiting read') 'cleanup A preserves another request awaiting image read'
    foreach ($invalidPrefix in @('', 'af_offscreen_*', 'af_offscreen_')) {
        [void]$cleanupMethod.Invoke($null, [object[]]@([string]$fixtureRoot, [string]$invalidPrefix))
        Assert-True (Test-Path -LiteralPath $fileB) "cleanup rejects broad prefix [$invalidPrefix]"
    }

}
finally {
    $resolvedFixture = [IO.Path]::GetFullPath($fixtureRoot)
    $allowedFixtures = [IO.Path]::GetFullPath((Join-Path $root 'artifacts\tests')) + [IO.Path]::DirectorySeparatorChar
    if (!$resolvedFixture.StartsWith($allowedFixtures, [StringComparison]::OrdinalIgnoreCase)) { throw 'Fixture cleanup escaped workspace' }
    Remove-Item -LiteralPath $resolvedFixture -Recurse -Force
}

# Narrative routing: exercise complete contexts and the actual offline/director composition path.
$router = $assembly.GetType('AnimusForge.Illustrator.Context.NarrativeFactRouter', $true)
$eventEvidenceMethod = $router.GetMethod('BuildEventEvidence')
$weeklyContextType = $assembly.GetType('AnimusForge.Illustrator.Context.WeeklyReportVisualContext', $true)
$weeklyContext = [Activator]::CreateInstance($weeklyContextType)
$weeklyContext.Title = '暮色中的王国纪事与命运回响'
$weeklyContext.Subtitle = '诸侯之间的角力仍在继续'
$weeklyContext.HeadlineSummary = '<a href="hero.secret-id">拉盖娅</a>率军围攻奥尼拉。' + ('后勤队伍沿道路运送粮食。' * 45) + '最终未能攻破城门，拉盖娅撤退，奥尼拉仍由守军控制。'
$weeklyHard = [string]$weeklyContext.BuildHardFacts()
$weeklyNarrative = [string]$weeklyContext.BuildDirectorOnlyFacts()
Assert-True ($weeklyHard.Contains('拉盖娅') -and $weeklyHard.Contains('奥尼拉') -and $weeklyHard.Contains('最终未能攻破城门') -and $weeklyHard.Contains('仍由守军控制')) 'weekly evidence preserves actors location and negative outcome beyond 400 characters'
Assert-True (!$weeklyHard.Contains('暮色中的王国纪事') -and !$weeklyHard.Contains('href') -and !$weeklyHard.Contains('secret-id') -and $weeklyNarrative.Contains('暮色中的王国纪事')) 'weekly decorative title is director-only and hyperlink internals are removed from facts'
$speechEvidence = [string]$eventEvidenceMethod.Invoke($null, [object[]]@('', '', '统帅说：“我们已经攻城成功并俘虏了全部守军”。实际围攻尚未开始。'))
Assert-True (!$speechEvidence.Contains('攻城成功') -and $speechEvidence.Contains('尚未开始')) 'quoted victory claim is not promoted to confirmed event'
$namedEvidence = [string]$eventEvidenceMethod.Invoke($null, [object[]]@('', '', '守军仍控制名为“银色城堡”的要塞，进攻者未能占领。'))
Assert-True ($namedEvidence.Contains('银色城堡') -and $namedEvidence.Contains('未能占领')) 'quoted place names and negation remain intact'
$unknownEvidence = [string]$eventEvidenceMethod.Invoke($null, [object[]]@('', '', ''))
Assert-True ($unknownEvidence.Contains('未获明确叙述')) 'empty event evidence stays unknown'
$releaseEvidence = [string]$eventEvidenceMethod.Invoke($null, [object[]]@('俘虏获释', '', '囚徒获释，未被处决。另一路部队停战，没有攻城。'))
Assert-True ($releaseEvidence.Contains('未被处决') -and $releaseEvidence.Contains('没有攻城')) 'release and ceasefire keep their negative outcomes'

$heroProfile = [Activator]::CreateInstance($assembly.GetType('AnimusForge.Illustrator.Context.HeroVisualProfile', $true))
$heroProfile.HeroName = '审查角色'
$heroProfile.Culture = '自定义文化'
$heroProfile.PhysicalFeatures = '银发、浅色皮肤、尖耳'
$heroProfile.CurrentStateDetail = '被囚禁，武器已收缴'
$heroProfile.EquipmentDetails.Add('黑色锁甲')
$heroProfile.CultureLore = '该族为尖耳精灵，银发而长寿。先祖曾迁徙至远方。'
$heroProfile.SpeciesDescription = '兽人'
$heroProfile.BackgroundLore = '他左眼附近有伤疤。曾担任王国财务官。'
$heroProfile.TraitsSummary = '审慎而多疑，算度深远'
$heroProfile.TopSkillsSummary = '战神与神射手'
$heroHard = [string]$heroProfile.BuildVisualSummary()
$heroNarrative = [string]$heroProfile.BuildDirectorOnlyFacts()
Assert-True ($heroHard.Contains('兽人') -and $heroHard.Contains('尖耳') -and !$heroHard.Contains('尖耳精灵') -and !$heroHard.Contains('左眼附近有伤疤') -and $heroHard.Contains('黑色锁甲') -and $heroHard.Contains('武器已收缴')) 'custom species visual identity and current equipment survive background split'
Assert-True (!$heroHard.Contains('算度深远') -and !$heroHard.Contains('神射手') -and !$heroHard.Contains('财务官') -and $heroNarrative.Contains('算度深远') -and $heroNarrative.Contains('神射手')) 'abstract traits skills and nonvisual biography remain director-only'

$environment = [Activator]::CreateInstance($assembly.GetType('AnimusForge.Illustrator.Context.EnvironmentVisualProfile', $true))
$environment.DateLabel = '卡拉迪亚历 1084 年 · 冬季 · 第 3 日'
$environment.SpecificLocation = '野外营地'
$environment.TimeOfDay = '夜晚'
$environment.Weather = '小雨'
$environment.RealSceneName = 'internal_scene_resource_123'
$environment.HostSceneDescription = '野外营地。玩家骑马，对方步行，身后两名护卫。'
$environmentHard = [string]$environment.BuildHardFactsSummary()
Assert-True ($environmentHard.Contains('冬季') -and $environmentHard.Contains('夜晚') -and $environmentHard.Contains('小雨') -and $environmentHard.Contains('两名护卫')) 'season lighting weather and unique host scene details survive routing'
Assert-True (!$environmentHard.Contains('1084') -and !$environmentHard.Contains('internal_scene_resource_123') -and $environment.BuildDirectorOnlyFacts().Contains('1084')) 'exact date and engine resource name move to director-only context'

$conversation = [Activator]::CreateInstance($assembly.GetType('AnimusForge.Illustrator.Context.ConversationVisualContext', $true))
$conversation.MainHeroProfile = $heroProfile
$conversation.EnvironmentProfile = $environment
$conversation.DialogueSentence = '请把我们的这段约定永远牢记在心中'
$conversation.RecentDialogueHistory = '玩家：我希望双方暂时停止这场争斗。'
$convHard = [string]$conversation.BuildHardFacts()
$convNarrative = [string]$conversation.BuildDirectorOnlyFacts()
Assert-True (!$convHard.Contains($conversation.DialogueSentence) -and $convNarrative.Contains($conversation.DialogueSentence) -and $convNarrative.Contains('神射手') -and $convHard.Contains('黑色锁甲')) 'conversation routes dialogue and biography to director while preserving visual identity'

$planType = $assembly.GetType('AnimusForge.Illustrator.Core.IllustrationPromptPlan', $true)
$offlineMethod = $director.GetMethods($privateStatic) | Where-Object { $_.Name -eq 'SynthesizeRuleBasedPrompt' -and $_.GetParameters().Count -eq 2 } | Select-Object -First 1
$resolveDirector = $director.GetMethod('ResolveDirectorOutput', $privateStatic)
foreach ($case in @(
    @{ Name='weekly'; Facts=$weeklyHard; Narrative=$weeklyNarrative; Required='未能攻破城门'; Forbidden=$weeklyContext.Title },
    @{ Name='encyclopedia'; Facts=$heroHard; Narrative=$heroNarrative; Required='黑色锁甲'; Forbidden='算度深远' },
    @{ Name='conversation'; Facts=$convHard; Narrative=$convNarrative; Required='两名护卫'; Forbidden=$conversation.DialogueSentence }
)) {
    $plan = [Activator]::CreateInstance($planType, [object[]]@([string]$case.Name, [string]$case.Facts, '本次采用高位俯拍。', [string]$case.Narrative))
    $offline = [string]$offlineMethod.Invoke($null, [object[]]@($plan, $null))
    Assert-True ($offline.Contains($case.Required) -and $offline.Contains('高位俯拍') -and !$offline.Contains($case.Forbidden)) "offline $($case.Name) preserves visual facts and excludes narrative"
    Assert-True ($plan.BuildDirectorContext().Contains('<director_only_narrative>') -and $plan.BuildDirectorContext().Contains($case.Forbidden)) "director $($case.Name) still receives complete narrative in separate block"
}
$echoPlan = [Activator]::CreateInstance($planType, [object[]]@('会话', '人物穿黑色锁甲。', '高位俯拍。', [string]$convNarrative))
$echo = [string]$resolveDirector.Invoke($null, [object[]]@([string]('字幕写着：' + [string]$conversation.DialogueSentence), $echoPlan, $null))
Assert-True (!$echo.Contains($conversation.DialogueSentence) -and $echo.Contains('黑色锁甲') -and $echo.Contains('高位俯拍')) 'verbatim dialogue echo falls back locally without losing facts'
$validDirector = [string]$resolveDirector.Invoke($null, [object[]]@([string]'夜雨中两人克制地相望，远景留白。', $echoPlan, $null))
Assert-True ($validDirector.Contains('克制地相望') -and $validDirector.Contains('黑色锁甲')) 'visual paraphrase from director is retained'
$weeklyPlan = [Activator]::CreateInstance($planType, [object[]]@('周报', $weeklyHard, '高位俯拍。', $weeklyNarrative))
$titleEcho = [string]$resolveDirector.Invoke($null, [object[]]@([string]('画面标题：' + [string]$weeklyContext.Title), $weeklyPlan, $null))
Assert-True (!$titleEcho.Contains($weeklyContext.Title) -and $titleEcho.Contains('未能攻破城门')) 'weekly title echo falls back without losing event outcome'

# Scene templates must not override sampled mission facts, even if no props could be named.
$sceneFixture = [Activator]::CreateInstance($assembly.GetType('AnimusForge.Illustrator.Context.EnvironmentVisualProfile', $true))
$sceneFixture.HasLiveScene = $true
$sceneFixture.RealProps = 'tavern_table_a、barrel_b'
$sceneFixture.IndoorOutdoorDetails = '模板盾牌与壁炉'
$sceneFixture.SurroundingProps = '模板旗帜'
$sceneFixture.SurroundingCharacters = '模板乐师'
Assert-True (!$sceneFixture.BuildArtDirectionSummary().Contains('模板') -and $sceneFixture.BuildHardFactsSummary().Contains('barrel_b')) 'live scene retains real objects without template props or people'
$sceneFixture.RealProps = ''
Assert-True (!$sceneFixture.BuildArtDirectionSummary().Contains('模板')) 'unnamed live scene does not invent template objects'
$sceneFixture.HasLiveScene = $false
Assert-True ($sceneFixture.BuildArtDirectionSummary().Contains('模板')) 'non-mission scene keeps existing artistic fallback'
$environmentSource = Get-Content (Join-Path $module 'src\Context\EnvironmentVisualExtractor.cs') -Raw
Assert-True (!$environmentSource.Contains('木盾插在沙地') -and !$environmentSource.Contains('手持长戟/盾矛')) 'remaining unsupported shields removed'

# Exercise real decoder using image bytes entirely offline.
Add-Type -AssemblyName System.Drawing
$testBitmap = New-Object Drawing.Bitmap 32,32
$graphics = [Drawing.Graphics]::FromImage($testBitmap)
$graphics.Clear([Drawing.Color]::Gold)
$graphics.FillRectangle([Drawing.Brushes]::Purple, 0, 0, 16, 16)
$graphics.Dispose()
$pngStream = New-Object IO.MemoryStream
$testBitmap.Save($pngStream, [Drawing.Imaging.ImageFormat]::Png)
$pngBytes = $pngStream.ToArray()
$pngBase64 = [Convert]::ToBase64String($pngBytes)
$pngStream.Dispose()
$testBitmap.Dispose()
$dataUri = 'data:image/png;base64,' + $pngBase64
$extractImage = $imageClientType.GetMethod('ExtractImageAsync', $privateStatic)
foreach ($fixture in @(
    @{Name='later data item'; Body=@{data=@(@{b64_json='bad!'}, @{b64_json=$pngBase64})}},
    @{Name='content image block'; Body=@{choices=@(@{message=@{content=@(@{type='text'; text='done'}, @{type='image_url'; image_url=@{url=$dataUri}})}})}},
    @{Name='string image_url'; Body=@{choices=@(@{message=@{images=@(@{image_url=$dataUri})}})}},
    @{Name='later choice'; Body=@{choices=@(@{message=@{content=$null}}, @{message=@{images=@(@{b64_json=$pngBase64})}})}},
    @{Name='markdown'; Body=@{choices=@(@{message=@{content=('![image](' + $dataUri + ')')}})}},
    @{Name='bad image before valid'; Body=@{choices=@(@{message=@{images=@(@{url='data:image/png;base64,bad!'}, @{url=$dataUri})}})}}
)) {
    $body = ConvertTo-Json -InputObject $fixture.Body -Depth 12 -Compress
    $task = $extractImage.Invoke($null, [object[]]@([string]$body, [Threading.CancellationToken]::None))
    $decoded = $task.GetAwaiter().GetResult()
    Assert-True ($null -ne $decoded -and [Convert]::ToBase64String($decoded.Bytes) -eq $pngBase64) "response parser accepts $($fixture.Name)"
}
$emptyResponse = '{"choices":[{"finish_reason":null,"message":{"content":null}}],"created":0,"usage":{"completion_tokens":0}}'
$emptyTask = $extractImage.Invoke($null, [object[]]@($emptyResponse, [Threading.CancellationToken]::None))
Assert-True ($null -eq $emptyTask.GetAwaiter().GetResult()) 'actual empty completion cannot fabricate image bytes'
$diagnose = $imageClientType.GetMethod('DescribeMissingImageResponse', $privateStatic)
Assert-True ($diagnose.Invoke($null, @($emptyResponse)).Contains('服务端返回空回复')) 'empty completion is distinguished from parser failure'
Assert-True ($diagnose.Invoke($null, @('{"choices":[{"finish_reason":"content_filter","message":{"content":null}}]}')).Contains('内容过滤')) 'provider refusal has separate diagnosis'
$fallback = $imageClientType.GetMethod('IsUnsupportedEditEndpoint', $privateStatic)
foreach ($code in @(200, 401, 429, 500)) {
    Assert-True (!$fallback.Invoke($null, [object[]]@($code, $emptyResponse))) "edit failure $code cannot silently retry without reference images"
}
Assert-True ($fallback.Invoke($null, [object[]]@(404, '{}'))) 'unsupported edit endpoint retains compatibility fallback'

$nativeImageType = $assembly.GetType('AnimusForge.Illustrator.Engine.NativeBannerImage', $true)
$nativeEncode = $nativeImageType.GetMethod('Encode', $privateStatic)
Assert-True ($null -eq $nativeEncode.Invoke($null, [object[]]@([byte[]]@(137,80,78,71), 256))) 'native exporter rejects incomplete PNG'
$blank = [Drawing.Bitmap]::new(128,128)
$blankStream = [IO.MemoryStream]::new()
$blank.Save($blankStream, [Drawing.Imaging.ImageFormat]::Png)
Assert-True ($null -eq $nativeEncode.Invoke($null, [object[]]@($blankStream.ToArray(), 256))) 'native exporter rejects transparent render'
$blankStream.Dispose()
$blank.Dispose()
Assert-True ($screenCapture.Contains('NativeBannerExportWidget') -and $screenCapture.Contains('IsNineGrid = true') -and $screenCapture.Contains('CancelActiveStage')) 'native banner uses full canvas and lifecycle cleanup'

Write-Host "$($script:checks) checks, $($script:failures) failures"
if ($script:failures -gt 0) { exit 1 }

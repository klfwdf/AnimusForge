param(
    [Parameter(Mandatory = $true)][string]$ProjectRoot,
    [Parameter(Mandatory = $true)][string]$RunRoot,
    [switch]$ProjectionOnly
)
$ErrorActionPreference = 'Stop'
. (Join-Path $ProjectRoot 'scripts/build/content_layout.ps1')
. (Join-Path $ProjectRoot 'scripts/build/package_localization.ps1')
Assert-AnimusForgePathUnderRoot -Path $RunRoot -Root (Join-Path $ProjectRoot 'artifacts/tests') -Label 'Localized ZIP fixture'
Assert-AnimusForgeNoReparsePoint -Path $RunRoot -Label 'Localized ZIP fixture'
if (Test-Path -LiteralPath $RunRoot) { throw 'Fixture must be new; existing evidence is never cleared.' }
New-Item -ItemType Directory -Path $RunRoot | Out-Null
$checks = 0
function Check([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $script:checks++
}
function Reject([scriptblock]$Action, [string]$Message, [string]$ExpectedError) {
    $actualError = ''
    try { & $Action | Out-Null } catch { $actualError = $_.Exception.Message }
    Check (-not [string]::IsNullOrEmpty($ExpectedError) -and $actualError.Contains($ExpectedError)) "$Message Expected '$ExpectedError'; received '$actualError'."
}
function FixtureText([string]$Path, [string]$Text) {
    New-Item -ItemType Directory -Path (Split-Path -Parent $Path) -Force | Out-Null
    [IO.File]::WriteAllText($Path, $Text, [Text.UTF8Encoding]::new($false))
}
$errors = $null
$ast = [Management.Automation.Language.Parser]::ParseFile((Join-Path $ProjectRoot 'scripts/build/package_mod.ps1'), [ref]$null, [ref]$errors)
Check ($errors.Count -eq 0) 'Main package script must parse.'
foreach ($name in @('Get-FullPathSafe', 'Parse-Version', 'Get-VersionedSubModuleBytes', 'Get-RequiredZipEntry', 'Get-ZipEntrySha256', 'Write-ZipFromModule')) {
    $definitions = @($ast.FindAll({ param($node) $node -is [Management.Automation.Language.FunctionDefinitionAst] -and $node.Name -eq $name }, $false))
    Check ($definitions.Count -eq 1) "Package helper must exist: $name"
    . ([scriptblock]::Create($definitions[0].Extent.Text))
}
Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem
$root = Join-Path $RunRoot 'project'
$stage = Join-Path $root 'stage/AnimusForge'
$han = -join ([char[]](0x4e2d, 0x6587))
$book = -join ([char[]](0x5361, 0x62c9, 0x8fea, 0x4e9a, 0x7f16, 0x5e74, 0x53f2))
$entries = @(
    @{ owner='Fixture'; source='content/Prompt.json'; target='ModuleData/Prompt.json' },
    @{ owner='Fixture'; source='content/en.xml'; target='ModuleData/Languages/en.xml' },
    @{ owner='Fixture'; source='content/en_language.xml'; target='ModuleData/Languages/language_data.xml' },
    @{ owner='Fixture'; source='content/zh.xml'; target='ModuleData/Languages/CNs/zh.xml' },
    @{ owner='Fixture'; source='content/zh_language.xml'; target='ModuleData/Languages/CNs/language_data.xml' },
    @{ owner='Fixture'; source="content/modules/AF.Module.Onboarding/PlayerExports/$book/personality_background/lord_1__original.json"; target="PlayerExports/$book/personality_background/lord_1__original.json" },
    @{ owner='Fixture'; source='content/SceneActionPromptTexts.xml'; target='ModuleData/SceneActionPromptTexts.xml' },
    @{ owner='Fixture'; source="content/modules/AF.Module.Onboarding/PlayerExports/$han/knowledge/rules/original.json"; target="PlayerExports/$han/knowledge/rules/original.json" },
    @{ owner='Fixture'; source='content/SharedRaw.json'; target='ModuleData/SharedRaw.json'; logicalName='SharedRaw.json'; localizationRole='compatibility' }
)
$entries += @(
    @{ owner='Fixture'; source='content/Prompt.json'; target='ModuleData/Defaults/CustomPrompts/Example.json' },
    @{ owner='Fixture'; source='content/Prompt.json'; target='CustomPrompts/Example.json' },
    @{ owner='Fixture'; source='content/Prompt.json'; target='ModuleData/Defaults/CustomPrompts/Policy/Example.json' },
    @{ owner='Fixture'; source='content/Prompt.json'; target='CustomPrompts/Policy/Example.json' }
)
FixtureText (Join-Path $root 'content/Prompt.json') ('{"Text":"' + $han + '"}')
FixtureText (Join-Path $root 'content/Prompt.en.json') '{"Text":"English prompt"}'
FixtureText (Join-Path $root 'content/SharedRaw.json') ('{"OriginalMarker":"' + $han + '"}')
FixtureText (Join-Path $root 'content/SceneActionPromptTexts.xml') ('<base language="zh-CN"><strings><string id="Scene" text="' + $han + ' {NAME} [ACTION:QUEST_TURN_IN]" /></strings></base>')
$sceneEnglish = '<base language="en"><strings><string id="Scene" text="English {NAME} [ACTION:QUEST_TURN_IN]" /></strings></base>'
FixtureText (Join-Path $root 'content/SceneActionPromptTexts.en.xml') $sceneEnglish
FixtureText (Join-Path $RunRoot 'protocol-cn.json') '{"Text":"{HERO} [ACTION:DUEL:1]", "TopicId":"duel", "Rules":["One", "Two"]}'
FixtureText (Join-Path $RunRoot 'protocol-en.json') '{"Text":"{HERO} [ACTION:DUEL:1]", "TopicId":"duel", "Rules":["Translated one", "Translated two"]}'
Assert-AnimusForgeEnglishJsonContract -OriginalPath (Join-Path $RunRoot 'protocol-cn.json') -EnglishPath (Join-Path $RunRoot 'protocol-en.json')
FixtureText (Join-Path $RunRoot 'protocol-en.json') '{"Text":"{NPC} [ACTION:DUEL:2]", "TopicId":"duel", "Rules":["Translated one", "Translated two"]}'
Reject { Assert-AnimusForgeEnglishJsonContract -OriginalPath (Join-Path $RunRoot 'protocol-cn.json') -EnglishPath (Join-Path $RunRoot 'protocol-en.json') } 'Protocol and variable changes must fail closed.' 'Prompt variable/protocol changed:'
FixtureText (Join-Path $RunRoot 'protocol-en.json') '{"Text":"{HERO} [ACTION:DUEL:1]", "TopicId":"duel", "Rules":["Translated one"]}'
Reject { Assert-AnimusForgeEnglishJsonContract -OriginalPath (Join-Path $RunRoot 'protocol-cn.json') -EnglishPath (Join-Path $RunRoot 'protocol-en.json') } 'Dropped constraint arrays must fail closed.' 'JSON array length changed:'
FixtureText (Join-Path $root 'content/en.xml') '<base><strings><string id="Fixture" text="English" /></strings></base>'
FixtureText (Join-Path $root 'content/en_language.xml') '<LanguageData id="English"><LanguageFile xml_path="en.xml" /></LanguageData>'
FixtureText (Join-Path $root 'content/zh.xml') ('<base><strings><string id="Fixture" text="' + $han + '" /></strings></base>')
FixtureText (Join-Path $root 'content/zh_language.xml') ('<LanguageData id="' + $han + '"><LanguageFile xml_path="CNs/zh.xml" /></LanguageData>')
FixtureText (Join-Path $root $entries[5].source) ('{"Personality":"' + $han + '"}')
FixtureText (Join-Path $root 'content/modules/AF.Module.Onboarding/PlayerExports/Calradia Chronicles/personality_background/lord_1__english.json') '{"Personality":"English noble"}'
$mappedEnglishPath = Join-Path $root 'content/modules/AF.Module.Onboarding/PlayerExports/Translated Book/knowledge/rules/renamed.json'
$mappedOriginalId = 'rule_' + $han
$mappedOriginal = @{ Id=$mappedOriginalId; Content=($han + ' [AFEF:' + $han + ']'); Keywords=@($han); Weight=3 } | ConvertTo-Json
$mappedEnglish = @{ Id='rule_english'; Content=('English [AFEF:' + $han + ']'); Keywords=@('English'); Weight=3 } | ConvertTo-Json
FixtureText (Join-Path $root $entries[7].source) $mappedOriginal
FixtureText $mappedEnglishPath $mappedEnglish
$identityMap = @{schema_version='1.0'; worldbooks=@{'Translated Book'=@{
    zh_directory=$han; en_directory='Translated Book'; globals=@{}; heroes=@{};
    rules=@{'knowledge/rules/original.json'=@{zh_id=$mappedOriginalId; en_id='rule_english'; en_file='knowledge/rules/renamed.json'}}
}}}
FixtureText (Join-Path $root 'content/modules/AF.Module.Onboarding/PlayerExports/english-worldbook-identity-map.json') ($identityMap | ConvertTo-Json -Depth 8)
FixtureText (Join-Path $root 'content/content-map.json') (@{schemaVersion=1; entries=$entries} | ConvertTo-Json -Depth 8)
Invoke-AnimusForgeContentProjection -ProjectRoot $root -DestinationModuleDir $stage | Out-Null
FixtureText (Join-Path $stage 'SubModule.xml') '<Module><Version value="v1.5.6" /></Module>'
FixtureText (Join-Path $stage 'bin/Win64_Shipping_Client/versions/1.3/AnimusForge.dll') 'fixture-1.3-shared'
FixtureText (Join-Path $stage 'bin/Win64_Shipping_Client/versions/1.4/AnimusForge.dll') 'fixture-1.4-shared'
$snapshots = @{}
foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -File) { $snapshots[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
$cn = @(Get-AnimusForgePackageProjection -ProjectRoot $root -StageModuleDir $stage -Language CN -PackageVersion v1.5.6)
$en = @(Get-AnimusForgePackageProjection -ProjectRoot $root -StageModuleDir $stage -Language EN -PackageVersion v1.5.6)
foreach ($plan in @(@{Language='CN'; Rows=$cn}, @{Language='EN'; Rows=$en})) {
    foreach ($relative in @('Example.json', 'Policy/Example.json')) {
        $seed = @($plan.Rows | Where-Object Target -eq ('CustomPrompts/' + $relative))[0]
        $fixed = @($plan.Rows | Where-Object Target -eq ('ModuleData/Defaults/CustomPrompts/' + $relative))[0]
        Check ($null -ne $seed -and $seed.Hash -ceq $fixed.Hash) "$($plan.Language) editable prompt must match package defaults: $relative"
        if ($plan.Language -eq 'EN') { Check ($seed.SourcePath.EndsWith('Prompt.en.json')) 'EN editable prompts use English resources.' }
    }
}
$mappedProjection = @($en | Where-Object Target -eq 'PlayerExports/Translated Book/knowledge/rules/renamed.json')[0]
$mappedBody = Get-Content -LiteralPath $mappedProjection.SourcePath -Raw -Encoding UTF8 | ConvertFrom-Json
Check ($mappedProjection.SourcePath -eq $mappedEnglishPath) 'Explicit identity map selects translated rules from nonstandard worldbooks.'
Check ($mappedBody.Id -ceq 'rule_english' -and $mappedBody.Content.StartsWith('English')) 'Projected English prose retains the English rule identity.'
Check ((Get-Content -LiteralPath $mappedEnglishPath -Raw | ConvertFrom-Json).Id -ceq 'rule_english') 'Projection does not rewrite the English source identity.'
FixtureText $mappedEnglishPath ($mappedEnglish.Replace('rule_english', 'rule_wrong'))
Reject { Get-AnimusForgePackageProjection -ProjectRoot $root -StageModuleDir $stage -Language EN -PackageVersion v1.5.6 } 'Stale explicit identities must be rejected.' 'Worldbook rule identity mapping is stale:'
FixtureText $mappedEnglishPath $mappedEnglish
Check (-not (Test-AnimusForgeChineseJsonProse -Text $mappedOriginal.Replace($han + ' [AFEF:', 'English [AFEF:').Replace('"' + $han + '"', '"English"'))) 'Stable identity and matching protocol labels may remain Chinese.'
Check (Test-AnimusForgeChineseJsonProse -Text ('{"Id":"' + $han + '","Text":"' + $han + '"}')) 'Stable identities do not exempt Chinese natural-language values.'
Check (@($en | Where-Object Target -eq 'ModuleData/SceneActionPromptTexts.xml')[0].SourcePath.EndsWith('SceneActionPromptTexts.en.xml')) 'Scene prompt XML selects the EN body under the same target.'
FixtureText (Join-Path $root 'content/SceneActionPromptTexts.en.xml') ($sceneEnglish.Replace('{NAME}', '{WRONG}'))
Reject { Get-AnimusForgePackageProjection -ProjectRoot $root -StageModuleDir $stage -Language EN -PackageVersion v1.5.6 } 'Scene XML variable drift must be rejected.' 'Prompt XML variable/protocol changed:'
FixtureText (Join-Path $root 'content/SceneActionPromptTexts.en.xml') $sceneEnglish
FixtureText (Join-Path $root 'content/Prompt.en.json') '{"Text":"English prompt", "LostContract":true}'
Reject { Get-AnimusForgePackageProjection -ProjectRoot $root -StageModuleDir $stage -Language EN -PackageVersion v1.5.6 } 'English schema drift must fail closed.' 'JSON field set changed:'
FixtureText (Join-Path $root 'content/Prompt.en.json') '{"Text":"English prompt"}'
Check ($cn.Count -eq $en.Count) 'CN and EN must retain the same registered bilingual UI files.'
Check (@($en | Where-Object Target -eq 'ModuleData/SharedRaw.json')[0].Hash -ceq @($cn | Where-Object Target -eq 'ModuleData/SharedRaw.json')[0].Hash) 'Both editions retain the exact compatibility corpus.'
foreach ($target in @('ModuleData/Languages/CNs/zh.xml', 'ModuleData/Languages/CNs/language_data.xml')) {
    Check (@($en | Where-Object Target -eq $target)[0].Hash -eq @($cn | Where-Object Target -eq $target)[0].Hash) "Both editions retain identical UI bytes: $target"
}
Check (@($en | Where-Object { $_.Target -match '\.en(?:-US)?\.json$' }).Count -eq 0) 'Parallel English filenames must not leak into the package.'
Check (@($en | Where-Object { $_.Target -match '^PlayerExports/' -and (Test-AnimusForgeChineseText -Text $_.Target) }).Count -eq 0) 'EN worldbook directories and filenames must be English.'
Check (@($cn | Where-Object Target -eq $entries[5].target).Count -eq 1) 'CN worldbook paths remain intact.'
Check (@($en | Where-Object { $_.Target -eq 'ModuleData/Prompt.json' })[0].SourcePath.EndsWith('Prompt.en.json')) 'English body must replace the original JSON filename.'
foreach ($api in @('1.3', '1.4')) {
    $target = "bin/Win64_Shipping_Client/versions/$api/AnimusForge.dll"
    Check (@($en | Where-Object Target -eq $target)[0].Hash -eq @($cn | Where-Object Target -eq $target)[0].Hash) "CN and EN must share API $api bytes."
}
if ($ProjectionOnly) {
    $factMarker = '[AFEF NPC' + (-join ([char[]](0x884c,0x4e3a,0x8865,0x5145))) + ']'
    Check (-not (Test-AnimusForgeChineseJsonProse -Text ('{"Text":"Observed facts ' + $factMarker + ' only."}'))) 'An exact AFEF history envelope is a stable protocol identity.'
    Check (Test-AnimusForgeChineseJsonProse -Text ('{"Text":"' + $factMarker + $han + '"}')) 'AFEF envelope does not hide untranslated prose following it.'
    Check (Test-AnimusForgeChineseJsonProse -Text ('{"Text":"[AFEF ' + $han + ']"}')) 'Unknown AFEF-looking Chinese labels are not exempt.'
    $protocolOriginal = Join-Path $root 'contract-afef-original.json'
    $protocolEnglish = Join-Path $root 'contract-afef-english.json'
    FixtureText $protocolOriginal ('{"Text":"' + $han + ' ' + $factMarker + '"}')
    FixtureText $protocolEnglish ('{"Text":"English ' + $factMarker + '"}')
    Assert-AnimusForgeEnglishJsonContract -OriginalPath $protocolOriginal -EnglishPath $protocolEnglish
    FixtureText $protocolEnglish '{"Text":"English [AFEF NPC action fact]"}'
    Reject { Assert-AnimusForgeEnglishJsonContract -OriginalPath $protocolOriginal -EnglishPath $protocolEnglish } 'AFEF fact identity cannot be translated away.' 'Prompt variable/protocol changed:'
    foreach ($path in $snapshots.Keys) { Check ((Get-FileHash -LiteralPath $path).Hash -eq $snapshots[$path]) "Projection preserves source/fixture bytes: $path" }
    Write-Output "PASS localized projection checks=$checks; shared bilingual UI, translated assets and identical implementation bytes; no ZIP or compilation"
    return
}
foreach ($language in @('CN', 'EN')) {
    $plan = if ($language -eq 'CN') { $cn } else { $en }
    $zipPath = Join-Path $RunRoot ("fixture_$language.zip")
    Write-AnimusForgeProjectedZip -ZipPath $zipPath -Projection $plan
    Assert-AnimusForgeProjectedZip -ZipPath $zipPath -Projection $plan
    Check (Test-Path -LiteralPath $zipPath) "$language isolated archive must exist."
}
FixtureText (Join-Path $root 'content/Prompt.en.json') '{"Text":"changed after snapshot"}'
Reject { Assert-AnimusForgeProjectedZip -ZipPath (Join-Path $RunRoot 'fixture_EN.zip') -Projection $en } 'Changed source must invalidate the locked ZIP.' 'Localization source changed during packaging:'
FixtureText (Join-Path $root 'content/Prompt.en.json') '{"Text":"English prompt"}'
FixtureText (Join-Path $root 'content/Prompt.en.json') '{"Text":"\u4e2d\u6587"}'
Reject { Get-AnimusForgePackageProjection -ProjectRoot $root -StageModuleDir $stage -Language EN -PackageVersion v1.5.6 } 'Escaped Chinese must block EN.' 'Chinese text remains or English asset is missing: ModuleData/Prompt.json'
FixtureText (Join-Path $root 'content/Prompt.en.json') '{"Text":"English prompt"}'
Move-Item -LiteralPath (Join-Path $root 'content/Prompt.en.json') -Destination (Join-Path $root 'content/Prompt.held.json')
Reject { Get-AnimusForgePackageProjection -ProjectRoot $root -StageModuleDir $stage -Language EN -PackageVersion v1.5.6 } 'Missing English must fail closed.' 'Chinese text remains or English asset is missing: ModuleData/Prompt.json'
Move-Item -LiteralPath (Join-Path $root 'content/Prompt.held.json') -Destination (Join-Path $root 'content/Prompt.en.json')
FixtureText (Join-Path $root 'src/Fixture.cs') ('// ' + $han + "`npublic class Fixture { string Text = " + '"English"; }')
Assert-AnimusForgeEnglishRuntimeSource -ProjectRoot $root
FixtureText (Join-Path $root 'src/Fixture.cs') 'public class Fixture { string Text = "\u4e2d\u6587"; }'
Reject { Assert-AnimusForgeEnglishRuntimeSource -ProjectRoot $root } 'Runtime escaped Chinese literals must block packaging.' 'Runtime Chinese literals block EN packaging'
foreach ($path in $snapshots.Keys) { Check ((Get-FileHash -LiteralPath $path).Hash -eq $snapshots[$path]) "Projection must preserve source/Stage bytes: $path" }
# Only tiny test DLLs are compiled; no production build/Stage/deploy takes place.
$cleanDll = Join-Path $RunRoot 'EnglishFixture.dll'
Add-Type -TypeDefinition 'public class EnglishPackageFixture { public static string Text() { return "English"; } }' -OutputAssembly $cleanDll
Assert-AnimusForgeEnglishRuntimeDll -DllPath $cleanDll
$dirtyDll = Join-Path $RunRoot 'ChineseFixture.dll'
Add-Type -TypeDefinition 'public class ChinesePackageFixture { public static string Text() { return "\u4e2d\u6587"; } }' -OutputAssembly $dirtyDll
Reject { Assert-AnimusForgeEnglishRuntimeDll -DllPath $dirtyDll } 'Actual DLL user strings must block Chinese runtime text.' 'Chinese runtime string remains in actual DLL:'
$sharedDll = Join-Path $RunRoot 'SharedResourceFixture.dll'
$compiler = New-Object CodeDom.Compiler.CompilerParameters
$compiler.GenerateExecutable = $false
$compiler.OutputAssembly = $sharedDll
[void]$compiler.EmbeddedResources.Add((Join-Path $root 'content/SharedRaw.json'))
Add-Type -TypeDefinition 'public class SharedResourceFixture { public static string Text() { return "English"; } }' -CompilerParameters $compiler
$sharedHashes = Get-AnimusForgeSharedTextResourceHashes -ProjectRoot $root
Check ($sharedHashes.ContainsKey('SharedRaw.json')) 'Shared resources require an explicit role and manifest identity.'
Reject { Assert-AnimusForgeEnglishRuntimeDll -DllPath $sharedDll } 'Unclassified embedded Chinese must still be rejected.' 'Chinese embedded text remains in actual DLL:'
Assert-AnimusForgeEnglishRuntimeDll -DllPath $sharedDll -SharedTextResourceHashes $sharedHashes
Reject { Assert-AnimusForgeEnglishRuntimeDll -DllPath $dirtyDll -SharedTextResourceHashes $sharedHashes } 'Shared resources do not exempt Chinese runtime literals.' 'Chinese runtime string remains in actual DLL:'
$wrongHashes = @{'SharedRaw.json'=('0' * 64)}
Reject { Assert-AnimusForgeEnglishRuntimeDll -DllPath $sharedDll -SharedTextResourceHashes $wrongHashes } 'Shared embedded bytes must match the declared source.' 'Shared text resource differs from its verified source:'
# Exercise the production two-archive transaction. These stubs replace only full
# Stage/game-artifact checks; ZIP writing and snapshot validation remain real.
$writer = (Get-Command Write-AnimusForgeProjectedZip).ScriptBlock
function Assert-AnimusForgeCleanStage { param($ProjectRoot, $StageModuleDir, [switch]$RequireCurrentArtifacts) }
function Assert-AnimusForgeEnglishRuntimeSource { param($ProjectRoot) }
function Assert-AnimusForgeEnglishRuntimeDll { param($DllPath) }
function Assert-ZipLayout { param($ZipPath, $ExpectedVersion, $OnnxMustBeAbsent, $CustomPromptsMustBeAbsent) }
function Get-AnimusForgePackageProjection {
    param($ProjectRoot, $StageModuleDir, $Language, $PackageVersion)
    if ($Language -eq 'CN') { return $cn } else { return $en }
}
$script:failEnglish = $false
function Write-AnimusForgeProjectedZip {
    param($ZipPath, $Projection)
    if ($script:failEnglish -and $ZipPath.Contains('.EN.')) { throw 'injected second archive failure' }
    & $writer -ZipPath $ZipPath -Projection $Projection
}
$OutputDir = Join-Path $RunRoot 'transaction-success'
$PackageLabel = ''
$VersionPattern = '^(?<prefix>v?)(?<major>\d+)\.(?<minor>\d+)\.(?<patch>\d+)(?:\.(?<micro>\d))?$'
function Get-Date {
    param([string]$Format)
    return ([datetime]::new(2026, 10, 7, 8, 2, 0)).ToString($Format, [Globalization.CultureInfo]::InvariantCulture)
}
$outputs = @(Write-ZipFromModule -ModulePath $stage -PackageVersion v1.5.6 -ProjectRoot $root -AutoDetected $false)
Check ($outputs.Count -eq 2) 'One transaction must return exactly two archive paths.'
Check ([IO.Path]::GetFileName($outputs[0]) -ceq 'AnimusForge_v1.5.6_CN_1007_0802.zip') 'CN uses the canonical version, language and local minute stamp.'
Check ([IO.Path]::GetFileName($outputs[1]) -ceq 'AnimusForge_v1.5.6_EN_1007_0802.zip') 'EN shares the same version and stamp.'
Check ((Get-ChildItem -LiteralPath $OutputDir -File).Count -eq 2) 'Success must publish both archives without temporary files.'
$firstHashes = @((Get-FileHash -LiteralPath $outputs[0]).Hash, (Get-FileHash -LiteralPath $outputs[1]).Hash)
$repeated = @(Write-ZipFromModule -ModulePath $stage -PackageVersion 1.5.6 -ProjectRoot $root -AutoDetected $false)
Check ([IO.Path]::GetFileName($repeated[0]) -ceq 'AnimusForge_v1.5.6_CN_1007_0802_1.zip') 'Repeated minute normalizes a missing v prefix and selects the next pair suffix.'
Check ([IO.Path]::GetFileName($repeated[1]) -ceq 'AnimusForge_v1.5.6_EN_1007_0802_1.zip') 'Both archives share the collision suffix.'
Check ((Get-FileHash -LiteralPath $outputs[0]).Hash -ceq $firstHashes[0] -and (Get-FileHash -LiteralPath $outputs[1]).Hash -ceq $firstHashes[1]) 'A repeated package does not overwrite the existing pair.'
foreach ($occupiedLanguage in @('CN', 'EN')) {
    $OutputDir = Join-Path $RunRoot ("transaction-collision-$occupiedLanguage")
    $occupiedPath = Join-Path $OutputDir ("AnimusForge_v1.5.6_{0}_1007_0802.zip" -f $occupiedLanguage)
    FixtureText $occupiedPath 'previous archive must be preserved'
    $collisionOutputs = @(Write-ZipFromModule -ModulePath $stage -PackageVersion v1.5.6 -ProjectRoot $root -AutoDetected $false)
    Check ([IO.Path]::GetFileName($collisionOutputs[0]) -ceq 'AnimusForge_v1.5.6_CN_1007_0802_1.zip' -and [IO.Path]::GetFileName($collisionOutputs[1]) -ceq 'AnimusForge_v1.5.6_EN_1007_0802_1.zip') "A single existing $occupiedLanguage archive reserves the stamp for both languages."
    Check ([IO.File]::ReadAllText($occupiedPath) -ceq 'previous archive must be preserved') 'Existing single archives are not touched.'
}
Reject { & (Join-Path $ProjectRoot 'scripts/build/package_mod.ps1') -Build -PackageLabel old-label } 'Custom labels must fail before building or writing version sources.' 'Canonical CN/EN package filenames do not accept -PackageLabel.'
$OutputDir = Join-Path $RunRoot 'transaction-failure'
$script:failEnglish = $true
Reject { Write-ZipFromModule -ModulePath $stage -PackageVersion v1.5.6 -ProjectRoot $root -AutoDetected $false } 'Failure writing EN must reject the pair.' 'injected second archive failure'
Check ((Get-ChildItem -LiteralPath $OutputDir -File -Force).Count -eq 0) 'Second archive failure must clean up both candidates.'
Write-Output "PASS localized ZIP projection checks=$checks; source/Stage preserved; missing/escaped/runtime Chinese rejected; no product build"

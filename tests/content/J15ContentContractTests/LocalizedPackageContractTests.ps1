param(
    [Parameter(Mandatory = $true)][string]$ProjectRoot,
    [Parameter(Mandatory = $true)][string]$RunRoot
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
foreach ($name in @('Get-FullPathSafe', 'Get-VersionedSubModuleBytes', 'Get-RequiredZipEntry', 'Get-ZipEntrySha256', 'Write-ZipFromModule')) {
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
    @{ owner='Fixture'; source="content/modules/AF.Module.Onboarding/PlayerExports/$book/personality_background/lord_1__original.json"; target="PlayerExports/$book/personality_background/lord_1__original.json" }
)
FixtureText (Join-Path $root 'content/Prompt.json') ('{"Text":"' + $han + '"}')
FixtureText (Join-Path $root 'content/Prompt.en.json') '{"Text":"English prompt"}'
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
FixtureText (Join-Path $root 'content/content-map.json') (@{schemaVersion=1; entries=$entries} | ConvertTo-Json -Depth 8)
Invoke-AnimusForgeContentProjection -ProjectRoot $root -DestinationModuleDir $stage | Out-Null
FixtureText (Join-Path $stage 'SubModule.xml') '<Module><Version value="v1.5.6" /></Module>'
FixtureText (Join-Path $stage 'bin/Win64_Shipping_Client/versions/1.3/AnimusForge.dll') 'fixture-1.3-shared'
FixtureText (Join-Path $stage 'bin/Win64_Shipping_Client/versions/1.4/AnimusForge.dll') 'fixture-1.4-shared'
$snapshots = @{}
foreach ($file in Get-ChildItem -LiteralPath $root -Recurse -File) { $snapshots[$file.FullName] = (Get-FileHash -LiteralPath $file.FullName).Hash }
$cn = @(Get-AnimusForgePackageProjection -ProjectRoot $root -StageModuleDir $stage -Language CN -PackageVersion v1.5.6)
$en = @(Get-AnimusForgePackageProjection -ProjectRoot $root -StageModuleDir $stage -Language EN -PackageVersion v1.5.6)
FixtureText (Join-Path $root 'content/Prompt.en.json') '{"Text":"English prompt", "LostContract":true}'
Reject { Get-AnimusForgePackageProjection -ProjectRoot $root -StageModuleDir $stage -Language EN -PackageVersion v1.5.6 } 'English schema drift must fail closed.' 'JSON field set changed:'
FixtureText (Join-Path $root 'content/Prompt.en.json') '{"Text":"English prompt"}'
Check ($cn.Count -eq $en.Count + 2) 'EN must remove the complete CN language branch.'
Check (@($en | Where-Object { $_.Target -match '\.en(?:-US)?\.json$' }).Count -eq 0) 'Parallel English filenames must not leak into the package.'
Check (@($en | Where-Object { $_.Target -eq $entries[5].target }).Count -eq 1) 'Worldbook original filename and directory must remain intact.'
Check (@($en | Where-Object { $_.Target -eq 'ModuleData/Prompt.json' })[0].SourcePath.EndsWith('Prompt.en.json')) 'English body must replace the original JSON filename.'
foreach ($api in @('1.3', '1.4')) {
    $target = "bin/Win64_Shipping_Client/versions/$api/AnimusForge.dll"
    Check (@($en | Where-Object Target -eq $target)[0].Hash -eq @($cn | Where-Object Target -eq $target)[0].Hash) "CN and EN must share API $api bytes."
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
$PackageLabel = 'fixture'
$outputs = @(Write-ZipFromModule -ModulePath $stage -PackageVersion v1.5.6 -ProjectRoot $root -AutoDetected $false)
Check ($outputs.Count -eq 2) 'One transaction must return exactly two archive paths.'
Check ($outputs[0].EndsWith('_CN.zip') -and $outputs[1].EndsWith('_EN.zip')) 'Final ZIP names must distinguish CN and EN.'
Check ((Get-ChildItem -LiteralPath $OutputDir -File).Count -eq 2) 'Success must publish both archives without temporary files.'
$OutputDir = Join-Path $RunRoot 'transaction-failure'
$script:failEnglish = $true
Reject { Write-ZipFromModule -ModulePath $stage -PackageVersion v1.5.6 -ProjectRoot $root -AutoDetected $false } 'Failure writing EN must reject the pair.' 'injected second archive failure'
Check ((Get-ChildItem -LiteralPath $OutputDir -File -Force).Count -eq 0) 'Second archive failure must clean up both candidates.'
Write-Output "PASS localized ZIP projection checks=$checks; source/Stage preserved; missing/escaped/runtime Chinese rejected; no product build"

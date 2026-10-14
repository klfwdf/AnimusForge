param(
    [Parameter(Mandatory=$true)][string]$AssemblyPath,
    [Parameter(Mandatory=$true)][string]$FixtureRoot,
    [ValidateSet('CN','EN')][string]$Language = 'CN',
    [string]$DirectorBodyPath,
    [string]$GamePath = 'F:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord'
)
$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '../../../..')).Path
$resource = Join-Path $repo 'content/modules/AnimusForge.Illustrator/ModuleData'
$module = Join-Path $FixtureRoot 'AnimusForge'
New-Item -ItemType Directory -Force (Join-Path $module 'ModuleData') | Out-Null
Set-Content -LiteralPath (Join-Path $module 'SubModule.xml') -Value '<Module><Id value="AnimusForge" /></Module>' -Encoding UTF8
$promptFile = if ($Language -eq 'EN') { 'IllustratorPromptTexts.en.json' } else { 'IllustratorPromptTexts.json' }
Copy-Item -LiteralPath (Join-Path $resource $promptFile) -Destination (Join-Path $module 'ModuleData/IllustratorPromptTexts.json')
Copy-Item -LiteralPath (Join-Path $resource 'IllustratorCompatibilityTexts.json') -Destination (Join-Path $module 'ModuleData/IllustratorCompatibilityTexts.json')
$env:ANIMUSFORGE_DATA_ROOT = $module
Add-Type -Path (Join-Path $PSScriptRoot 'OfflineAssemblyResolver.cs')
$dirs = @((Split-Path -Parent $AssemblyPath), (Join-Path $GamePath 'bin/Win64_Shipping_Client'))
foreach ($mod in @('SandBox','Native','Bannerlord.Harmony','Bannerlord.MBOptionScreen','AnimusForge')) {
    $dirs += Join-Path $GamePath "Modules/$mod/bin/Win64_Shipping_Client"
}
$resolver = New-Object OfflineAssemblyResolver -ArgumentList (,([string[]]$dirs))
function Check($condition, $name) {
    if (!$condition) { throw "FAIL $name" }
    Write-Output "PASS $Language $name"
}
try {
    $assembly = [Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
    $flags = [Reflection.BindingFlags]'Static,Public,NonPublic'
    $instanceFlags = [Reflection.BindingFlags]'Instance,Public,NonPublic'
    $text = $assembly.GetType('AnimusForge.Illustrator.IllustratorText', $true)
    $get = $text.GetMethod('Get', $flags)
    $raw = $text.GetMethod('Raw', $flags)
    $optionsType = $assembly.GetType('AnimusForge.Illustrator.Core.IllustrationOptions', $true)
    $options = [Runtime.Serialization.FormatterServices]::GetUninitializedObject($optionsType)
    foreach ($pair in @{DirectorModelName='offline'; SelectedStyle='classic-oil'; PreserveEquipmentFidelity=$true}.GetEnumerator()) {
        $optionsType.GetField('<'+$pair.Key+'>k__BackingField', $instanceFlags).SetValue($options, $pair.Value)
    }
    $director = $assembly.GetType('AnimusForge.Illustrator.Core.VisualDirectorEngine', $true)
    $planType = $assembly.GetType('AnimusForge.Illustrator.Core.IllustrationPromptPlan', $true)
    $portraitRule = $get.Invoke($null, @('EncyclopediaDirectorSystem'))
    $profileType = $assembly.GetType('AnimusForge.Illustrator.Context.HeroVisualProfile', $true)
    $profile = [Activator]::CreateInstance($profileType)
    $profileFields = @('HeroName','CultureLore','FactionLore','BackgroundLore','TraitsSummary','TopSkillsSummary')
    foreach ($field in $profileFields) { $profileType.GetProperty($field).SetValue($profile, "PERSONA_$field", $null) }
    $narrative = $profileType.GetMethod('BuildDirectorOnlyFacts').Invoke($profile, @())
    foreach ($modeKey in @('Director_1020','Director_933','Director_937')) {
        $mode = $raw.Invoke($null, @($modeKey))
        $plan = [Activator]::CreateInstance($planType, [object[]]@($mode,'IDENTITY_FIXTURE','',$narrative))
        $payload = $director.GetMethod('BuildDirectorPayload', $flags).Invoke($null, [object[]]@($plan,$options,$null,$false))
        $request = $payload.ToString() | ConvertFrom-Json
        $system = $request.messages[0].content
        Check ($system.Contains($portraitRule) -eq ($modeKey -eq 'Director_1020')) "portrait rule isolated from $modeKey"
        if ($modeKey -eq 'Director_1020') {
            foreach ($field in $profileFields) {
                Check ($payload.ToString().Contains("PERSONA_$field")) "director receives $field"
            }
            Check (!$system.Contains($get.Invoke($null, @('Director_949')))) 'no old mandatory action and hand instructions'
            Check (!$system.Contains($get.Invoke($null, @('Fidelity_18')))) 'no event clothing-state expansion'
            foreach ($marker in @('TITLE','THEME','ACTION','SUBJECT','SCENE','LIGHT','SPATIAL')) {
                Check ($system.Contains("[[AFI_PROTOCOL_$marker]]")) "output protocol $marker retained"
            }
        }
    }
    $extractor = $assembly.GetType('AnimusForge.Illustrator.Context.HeroVisualExtractor', $true)
    $stateParameter = $extractor.GetMethod('Extract').GetParameters() | Where-Object Name -eq 'includeCurrentState'
    Check ($stateParameter.DefaultValue -eq $true) 'other callers retain current state by default'
    $caller = Get-Content -LiteralPath (Join-Path $repo 'extensions/AnimusForge.Illustrator/src/UI/Overlays/IllustrationCardPopup.cs') -Raw
    Check ($caller.Contains('appearance: appearance, includeCurrentState: false)')) 'encyclopedia opts out before state extraction'
    Check ($caller.Contains('string directorFacts = profile.BuildDirectorOnlyFacts();')) 'encyclopedia has no campaign-year prefix'
    $extractSource = Get-Content -LiteralPath (Join-Path $repo 'extensions/AnimusForge.Illustrator/src/Context/HeroVisualExtractor.cs') -Raw
    Check ($extractSource.Contains('if (includeCurrentState) profile.CurrentStateDetail = ExtractCurrentState(hero);')) 'location extraction is skipped, not text-filtered'
    if ($DirectorBodyPath) {
        [string]$body = [IO.File]::ReadAllText((Resolve-Path $DirectorBodyPath).Path, [Text.Encoding]::UTF8)
        $complete = $director.GetMethod('HasRequiredSceneDescription', $flags)
        Check ($complete.Invoke($null, @($body))) 'real director reply with nested headings keeps all four sections'
        $mode = $raw.Invoke($null, @('Director_1020'))
        $plan = [Activator]::CreateInstance($planType, [object[]]@($mode,'IDENTITY_FIXTURE','',''))
        $direction = $director.GetMethod('ResolveDirection', $flags).Invoke($null, [object[]]@($body,$plan,$options))
        Check (!$direction.UsedLocalFallback) 'real reply survives complete director validation without template replacement'
        $lastSection = $body.IndexOf('[[AFI_PROTOCOL_SPATIAL]]')
        Check ($lastSection -gt 0 -and !$complete.Invoke($null, @($body.Substring(0,$lastSection)))) 'genuinely missing spatial section is still rejected'
    }
} finally { $resolver.Dispose() }

param(
    [Parameter(Mandatory=$true)][string]$AssemblyPath,
    [Parameter(Mandatory=$true)][string]$ReferenceDir,
    [Parameter(Mandatory=$true)][string]$McmDir,
    [Parameter(Mandatory=$true)][string]$HarmonyPath
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$searchRoots = @((Resolve-Path $ReferenceDir).Path, (Resolve-Path $McmDir).Path, (Split-Path (Resolve-Path $HarmonyPath).Path))
$resolver = [System.ResolveEventHandler] {
    param($sender, $eventArgs)
    $name = ([System.Reflection.AssemblyName]::new($eventArgs.Name)).Name + '.dll'
    foreach ($directory in $searchRoots) {
        $candidate = Join-Path $directory $name
        if (Test-Path -LiteralPath $candidate) { return [System.Reflection.Assembly]::LoadFrom($candidate) }
    }
    return $null
}
[AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)
$script:checks = 0
function Check($ok, $name) { if (!$ok) { throw $name }; $script:checks++ }
function Get-McmSetting($property) {
    return @($property.GetCustomAttributes($false) | Where-Object { $_.GetType().FullName -like 'MCM.Abstractions.Attributes.v2.SettingProperty*' })[0]
}
function Get-McmGroup($property) {
    return @($property.GetCustomAttributes($false) | Where-Object { $_.GetType().Name -eq 'SettingPropertyGroupAttribute' })[0]
}
try {
    [void][System.Reflection.Assembly]::LoadFrom((Resolve-Path $HarmonyPath).Path)
    $assembly = [System.Reflection.Assembly]::LoadFrom((Resolve-Path $AssemblyPath).Path)
    $owner = $assembly.GetType('AnimusForge.DuelSettings', $true)
    $frontend = $assembly.GetType('AnimusForge.GcczSettings', $true)
    $groupName = $assembly.GetType('AnimusForge.SiegeAftermathIntervention.SiegeNpcResponseLimitProfile', $true).GetField('McmGroupName').GetRawConstantValue()
    $aliases = @{
        GcczNpcResponseUnlimited='Unlimited'; GcczNpcResponseLimit='ResponseLimit';
        GcczVillageInitialAssistance='InitialCooperation'; EnableGcczDiagnosticLog='DiagnosticLog';
        EnableGcczVerboseDiagnosticLog='VerboseLog'; OpenGcczTownManual='TownTutorial';
        ShowGcczTownTutorial='TownTutorial'; ShowGcczVillageTutorial='VillageTutorial'; ShowGcczCastleTutorial='CastleTutorial'
    }
    $covered = @{}
    $legacyCount = 0
    foreach ($property in $owner.GetProperties()) {
        if ((Get-McmGroup $property).GroupName -ne $groupName) { continue }
        $legacyCount++
        $name = $property.Name
        if ($aliases.ContainsKey($name)) { $name = $aliases[$name] }
        $projected = $frontend.GetProperty($name)
        Check ($null -ne $projected) "Missing original GCCZ control: $($property.Name)"
        Check ($projected.PropertyType -eq $property.PropertyType) "Type mismatch: $name"
        $before = Get-McmSetting $property; $after = Get-McmSetting $projected
        Check ($null -ne $after -and $after.GetType() -eq $before.GetType()) "MCM control type mismatch: $name"
        # These four intentionally have current village/tutorial descriptions.
        if ($name -notin @('InitialCooperation','TownTutorial','VillageTutorial','CastleTutorial')) {
            foreach ($field in @('DisplayName','HintText','Content','MinValue','MaxValue','RequireRestart')) {
                $info = $before.GetType().GetProperty($field)
                if ($null -ne $info) { Check ($info.GetValue($before) -eq $info.GetValue($after)) "Original metadata lost: $name.$field" }
            }
        }
        $covered[$name] = $true
    }
    Check ($legacyCount -eq 20 -and $covered.Count -eq 19) 'All 20 original entries must map to 19 unique controls (one duplicate town tutorial)'
    $controls = @($frontend.GetProperties() | Where-Object { $null -ne (Get-McmGroup $_) })
    Check ($controls.Count -eq $covered.Count) 'Unexpected or missing frontend controls'
    $visibility = $assembly.GetType('AnimusForge.AnimusForgeMcmVisibility', $true).GetMethod('IsVisibleProperty', [Reflection.BindingFlags]'NonPublic,Static')
    $keys = [Collections.Generic.HashSet[string]]::new()
    [void]$keys.Add('AF_GCCZ_McmTitle')
    foreach ($property in $controls) {
        Check ($property.IsDefined([Newtonsoft.Json.JsonIgnoreAttribute], $false)) "Frontend must not become a second persistence owner: $($property.Name)"
        Check ($visibility.Invoke($null, @('AnimusForge_GCCZ_v1', $property.Name, (Get-McmGroup $property).GroupName))) "Frontend visibility removed $($property.Name)"
        $attr = Get-McmSetting $property
        foreach ($text in @($attr.DisplayName, $attr.HintText, $attr.Content, (Get-McmGroup $property).GroupName)) {
            foreach ($match in [regex]::Matches([string]$text, '\{=([^}]+)\}')) { [void]$keys.Add($match.Groups[1].Value) }
        }
    }
    # Read registered content roots rather than inventing translations in fixtures.
    $languages = Join-Path $root 'content/modules'
    $cn = @{}; $en = @{}
    foreach ($file in Get-ChildItem -LiteralPath $languages -Recurse -File -Filter '*.xml' | Where-Object { $_.FullName -match '[\\/]Languages[\\/]' }) {
        [xml]$xml = [IO.File]::ReadAllText($file.FullName)
        $target = if ($file.FullName -match '[\\/]CNs[\\/]') { $cn } else { $en }
        foreach ($node in $xml.SelectNodes('//string[@id][@text]')) { $target[$node.id] = $node.text }
    }
    foreach ($key in $keys) { Check ($cn.ContainsKey($key) -and $en.ContainsKey($key)) "Missing CN/EN resource: $key" }
    Check ($cn['AF_GCCZ_McmTitle'] -eq 'AnimusForge 攻城处置(GCCZ)') 'Exact requested Chinese title'
    Check ($en['AF_GCCZ_McmTitle'] -eq 'AnimusForge Siege Aftermath (GCCZ)') 'English title'
    Write-Output "PASS actual DLL MCM metadata: $script:checks checks; 20 legacy entries -> 19 controls; $($keys.Count) paired UI keys."
    Write-Output 'SCOPE real compiled metadata/visibility and source resources. No game UI, settings write, log export or clear executed.'
} finally { [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver) }

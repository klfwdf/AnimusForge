param(
    [Parameter(Mandatory = $true)][string]$ImplementationPath,
    [Parameter(Mandatory = $true)][string]$ReferenceDirectory,
    [Parameter(Mandatory = $true)][string]$GameReferenceDirectory,
    [Parameter(Mandatory = $true)][string]$ProjectRoot
)
$ErrorActionPreference = 'Stop'
if ($PSVersionTable.PSEdition -eq 'Core') { throw 'Run in Windows PowerShell for the game .NET Framework runtime.' }
# A CLR callback preserves the IList object; PowerShell callback returns can
# unwrap/box collection values before MCM receives the existing dropdown.
Add-Type -TypeDefinition @'
public static class TtsDropdownReplayContext {
    public static object Existing;
    public static object GetExisting(string path) { return Existing; }
}
'@
$refs = [IO.Path]::GetFullPath($ReferenceDirectory)
$searchDirectories = @($refs, [IO.Path]::GetFullPath($GameReferenceDirectory))
$resolver = [System.ResolveEventHandler]{
    param($sender, $resolveEvent)
    foreach ($directory in $searchDirectories) {
        $path = [System.IO.Path]::Combine($directory, ([System.Reflection.AssemblyName]::new($resolveEvent.Name)).Name + '.dll')
        if ([System.IO.File]::Exists($path)) { return [System.Reflection.Assembly]::LoadFrom($path) }
    }
    return $null
}
[AppDomain]::CurrentDomain.add_AssemblyResolve($resolver)
$script:checks = 0
function Assert-Replay([bool]$Condition, [string]$Label) {
    if (-not $Condition) { throw $Label }
    $script:checks++
}
try {
    $mcm = [Reflection.Assembly]::LoadFrom((Join-Path $refs 'MCMv5.dll'))
    [void][Reflection.Assembly]::LoadFrom((Join-Path $refs 'Newtonsoft.Json.dll'))
    $af = [Reflection.Assembly]::LoadFrom([IO.Path]::GetFullPath($ImplementationPath))
    $type = $af.GetType('AnimusForge.SpeechSettings', $true)
    $property = $type.GetProperty('ProviderMode')
    $attributes = @($property.GetCustomAttributes($false))
    Assert-Replay (@($attributes | Where-Object { $_.GetType().Name -eq 'SettingPropertyDropdownAttribute' }).Count -eq 1) 'Dropdown metadata missing'
    Assert-Replay (@($attributes | Where-Object { $_.GetType().Name -eq 'SettingPropertyIntegerAttribute' }).Count -eq 0) 'Numeric slider is still registered'
    $revision = $type.GetField('_revision', [Reflection.BindingFlags]'Instance,NonPublic')
    $converterType = $mcm.GetType('MCM.Implementation.DropdownJsonConverter', $true)
    $loggerType = $mcm.GetType('BUTR.DependencyInjection.Logger.DefaultBUTRLogger`1', $true).MakeGenericType($converterType)
    $logger = [Activator]::CreateInstance($loggerType)
    $existing = [Delegate]::CreateDelegate([Func[string,object]], [TtsDropdownReplayContext].GetMethod('GetExisting'))
    $converter = [Activator]::CreateInstance($converterType, [object[]]@($logger, $existing))
    $serializer = [Newtonsoft.Json.JsonSerializer]::Create()
    $serializer.Converters.Add($converter)
    $expected = @(
        '{=AF_TTS_UI_ProviderLocal}Local GPT-SoVITS',
        '{=AF_TTS_UI_ProviderCloud}Volcengine TTS API',
        '{=AF_TTS_UI_ProviderCustom}Custom (coming later)'
    )
    foreach ($index in 0, 1, 2) {
        $script:page = [Activator]::CreateInstance($type)
        [TtsDropdownReplayContext]::Existing = $page.ProviderMode
        $json = [Newtonsoft.Json.Linq.JObject]::Parse('{"ProviderMode":' + $index + '}')
        $loaded = $json['ProviderMode'].ToObject($property.PropertyType, $serializer)
        $property.SetValue($page, $loaded)
        Assert-Replay ($page.ProviderMode.SelectedIndex -eq $index) ('Old integer config failed: ' + $index)
        $saved = [Newtonsoft.Json.Linq.JToken]::FromObject($page.ProviderMode, $serializer)
        Assert-Replay ($saved.Type.ToString() -eq 'Integer' -and $saved.ToString() -eq [string]$index) 'Saved format is no longer the original integer'
        Assert-Replay ($page.ProviderMode.Count -eq 3 -and ($page.ProviderMode -join '|') -eq ($expected -join '|')) 'Fixed localized options changed on load'
    }
    $before = [int]$revision.GetValue($page)
    $page.ProviderMode.SelectedIndex = 1
    Assert-Replay ([int]$revision.GetValue($page) -eq $before + 1) 'In-place MCM selection did not invalidate the runtime projection'
    $before = [int]$revision.GetValue($page)
    $page.ProviderMode.SelectedIndex = 1
    Assert-Replay ([int]$revision.GetValue($page) -eq $before) 'Unchanged selection invalidates the cache'
    $page.ProviderMode.SelectedIndex = 99
    Assert-Replay ($page.ProviderMode.SelectedIndex -eq 2) 'Upper index clamp failed'
    $page.ProviderMode.SelectedIndex = -1
    Assert-Replay ($page.ProviderMode.SelectedIndex -eq 0) 'Lower index clamp failed'
    $uiPath = @(Get-ChildItem -LiteralPath $refs -Filter 'Bannerlord.MBOptionScreen.v*.dll' -File)
    Assert-Replay ($uiPath.Count -eq 1) 'Expected one versioned MCM UI assembly'
    $ui = [Reflection.Assembly]::LoadFrom($uiPath[0].FullName)
    $actionType = $ui.GetType('MCM.UI.Actions.SetSelectedIndexAction', $true)
    $ref = [MCM.Common.PropertyRef]::new($property, $page)
    $incoming = [Activator]::CreateInstance($property.PropertyType, [object[]]@([string[]]@('one', 'two', 'three'), 1))
    $constructor = $actionType.GetConstructor([type[]]@([MCM.Common.IRef], [object]))
    $action = $constructor.Invoke([object[]]@($ref, $incoming))
    $actionType.GetMethod('DoAction').Invoke($action, $null)
    Assert-Replay ($page.ProviderMode.SelectedIndex -eq 1) 'Real MCM selection action failed'
    $actionType.GetMethod('UndoAction').Invoke($action, $null)
    Assert-Replay ($page.ProviderMode.SelectedIndex -eq 0) 'Real MCM cancel/undo failed'
    Assert-Replay (($page.ProviderMode -join '|') -eq ($expected -join '|')) 'Preset replaced the localized labels'
    $languages = Join-Path $ProjectRoot 'content\modules\AF.Module.Llm\ModuleData\Languages'
    foreach ($path in @((Join-Path $languages 'afllm_ui_strings.xml'), (Join-Path $languages 'CNs\afllm_ui_strings-zh-CN.xml'))) {
        [xml]$xml = Get-Content -LiteralPath $path -Raw -Encoding UTF8
        foreach ($key in @('Provider', 'ProviderHint', 'ProviderLocal', 'ProviderCloud', 'ProviderCustom')) {
            $nodes = @($xml.SelectNodes('//string[@id="AF_TTS_UI_' + $key + '"]'))
            Assert-Replay ($nodes.Count -eq 1 -and -not [string]::IsNullOrWhiteSpace($nodes[0].text)) ('Missing/duplicate UI resource: ' + $key)
        }
    }
    Write-Output ('PASS ' + $checks + ' compiled AF / real MCM dropdown, JSON, action, undo and paired-resource checks. No live game/provider calls.')
}
catch { Write-Output $_.Exception.ToString(); Write-Output $_.ScriptStackTrace; throw }
finally { [AppDomain]::CurrentDomain.remove_AssemblyResolve($resolver) }

param(
    [Parameter(Mandatory=$true)][string]$AssemblyPath,
    [string]$GamePath = 'F:/SteamLibrary/steamapps/common/Mount & Blade II Bannerlord',
    [string]$RunRoot = ''
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
if ([string]::IsNullOrWhiteSpace($RunRoot)) {
    $RunRoot = Join-Path $root ('artifacts/interior-localization-' + [Guid]::NewGuid().ToString('N'))
}
$fixture = Join-Path ([System.IO.Path]::GetFullPath($RunRoot)) 'AnimusForge'
if (Test-Path -LiteralPath $fixture) { throw 'Use a fresh test output directory.' }
$data = Join-Path $fixture 'ModuleData/Languages'
New-Item -ItemType Directory -Force (Join-Path $data 'CNs') | Out-Null
Copy-Item -LiteralPath (Join-Path $root 'content/modules/AF.Module.Kingdom/ModuleData/Languages/afkingdom_ui_strings.xml') -Destination $data
Copy-Item -LiteralPath (Join-Path $root 'content/modules/AF.Module.Kingdom/ModuleData/Languages/CNs/afkingdom_ui_strings-zh-CN.xml') -Destination (Join-Path $data 'CNs')
foreach ($language in @('English','简体中文')) {
    $isChinese = $language -eq '简体中文'
    $manifest = if ($isChinese) { Join-Path $data 'CNs/language_data.xml' } else { Join-Path $data 'language_data.xml' }
    $file = if ($isChinese) { 'CNs/afkingdom_ui_strings-zh-CN.xml' } else { 'afkingdom_ui_strings.xml' }
    $iso = if ($isChinese) { 'zh-HANS,zh-cn' } else { 'en-GB,en-US' }
    Set-Content -LiteralPath $manifest -Encoding UTF8 -Value "<LanguageData id='$language' name='$language' supported_iso='$iso'><LanguageFile xml_path='$file' /></LanguageData>"
}
Add-Type -Path (Join-Path $root 'extensions/AnimusForge.Illustrator/tests/audits/OfflineAssemblyResolver.cs')
$api = Split-Path -Leaf (Split-Path -Parent $AssemblyPath)
$dirs = @((Split-Path -Parent (Resolve-Path $AssemblyPath)), (Join-Path $root ".tmp/build_check/$api"), (Join-Path $GamePath 'bin/Win64_Shipping_Client'))
foreach ($module in @('Bannerlord.Harmony','Bannerlord.UIExtenderEx','Bannerlord.MBOptionScreen','SandBox')) {
    $dirs += Join-Path $GamePath "Modules/$module/bin/Win64_Shipping_Client"
}
$resolver = New-Object OfflineAssemblyResolver -ArgumentList (,([string[]]$dirs))
try {
    Add-Type -Path @((Join-Path $PSScriptRoot 'Checks.cs'), (Join-Path $PSScriptRoot 'HeaderLayoutChecks.cs')) -ReferencedAssemblies 'System.Xml.dll'
    [InteriorLocalizationChecks]::Run((Resolve-Path $AssemblyPath).Path, $fixture)
} finally { $resolver.Dispose() }

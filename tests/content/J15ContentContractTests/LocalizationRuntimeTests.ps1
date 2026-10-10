param(
    [Parameter(Mandatory=$true)][string]$ReferenceDir,
    [string]$ProjectRoot = '',
    [string]$ResourceName = 'af_ui_strings'
)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrEmpty($ProjectRoot)) { $ProjectRoot = Join-Path $PSScriptRoot '../../..' }
$ProjectRoot = [IO.Path]::GetFullPath($ProjectRoot)
$ReferenceDir = [IO.Path]::GetFullPath($ReferenceDir)
[void][Reflection.Assembly]::LoadFrom((Join-Path $ReferenceDir 'TaleWorlds.Library.dll'))
$localization = [Reflection.Assembly]::LoadFrom((Join-Path $ReferenceDir 'TaleWorlds.Localization.dll'))
$languageData = $localization.GetType('TaleWorlds.Localization.LanguageData', $true)
$register = $languageData.GetMethod('LoadFromXml', [Reflection.BindingFlags]'Public,Static')
$map = (Get-Content (Join-Path $ProjectRoot 'content/content-map.json') -Raw -Encoding UTF8 | ConvertFrom-Json).entries
$mapped = @{}
foreach ($entry in $map) { $mapped[$entry.target] = Join-Path $ProjectRoot $entry.source }
$pairs = @{}
$registrationRoot = Join-Path $ProjectRoot 'content/foundation/AF.Foundation.Localization/ModuleData/Languages'
foreach ($name in @('language_data.xml', 'CNs/language_data.xml')) {
    $doc = New-Object Xml.XmlDocument
    $doc.Load((Join-Path $registrationRoot $name))
    if ($name.StartsWith('CNs/')) { $chineseLanguageId = $doc.DocumentElement.GetAttribute('id') }
    foreach ($file in @($doc.DocumentElement.SelectNodes('LanguageFile'))) {
        $relative = $file.GetAttribute('xml_path')
        $key = [IO.Path]::GetFileNameWithoutExtension($relative) -replace '-zh-CN$|-en$', ''
        if ($ResourceName -ne '*' -and $key -ne $ResourceName) { continue }
        $target = 'ModuleData/Languages/' + $relative
        if (-not $mapped.ContainsKey($target)) { throw "Registered resource is not mapped: $target" }
        $source = $mapped[$target]
        if (-not $pairs.ContainsKey($key)) { $pairs[$key] = @{} }
        $languageKey = if ($name.StartsWith('CNs/')) { 'zh' } else { 'en' }
        $pairs[$key][$languageKey] = $source
        # Register the actual mapped source without constructing a build or Stage tree.
        $single = $doc.CloneNode($true)
        foreach ($child in @($single.DocumentElement.SelectNodes('LanguageFile'))) {
            [void]$single.DocumentElement.RemoveChild($child)
        }
        $child = $single.CreateElement('LanguageFile')
        $child.SetAttribute('xml_path', [IO.Path]::GetFileName($source))
        [void]$single.DocumentElement.AppendChild($child)
        [void]$register.Invoke($null, [object[]]@($single.PSObject.BaseObject, [string][IO.Path]::GetDirectoryName($source)))
    }
}
if ($pairs.Count -eq 0) { throw "No registered resource matches: $ResourceName" }
$fallback = New-Object Xml.XmlDocument
$fallback.LoadXml('<?xml version="1.0"?><LanguageData id="French" name="French" supported_iso="fr" under_development="false" text_processor="TaleWorlds.Localization.TextProcessor.LanguageProcessors.EnglishTextProcessor" />')
[void]$register.Invoke($null, [object[]]@($fallback.PSObject.BaseObject, [string]$registrationRoot))
$rows = @()
$zh = @{}
foreach ($key in @($pairs.Keys | Sort-Object)) {
    if (-not $pairs[$key].ContainsKey('en') -or -not $pairs[$key].ContainsKey('zh')) { throw "Unpaired resource: $key" }
    $english = New-Object Xml.XmlDocument
    $english.Load($pairs[$key]['en'])
    $chinese = New-Object Xml.XmlDocument
    $chinese.Load($pairs[$key]['zh'])
    $rows += @($english.SelectNodes('/base/strings/string'))
    foreach ($row in $chinese.SelectNodes('/base/strings/string')) { $zh[$row.GetAttribute('id')] = $row.GetAttribute('text') }
}
$checked = 0
foreach ($language in @('English', $chineseLanguageId, 'French', 'English')) {
    if (-not [TaleWorlds.Localization.MBTextManager]::ChangeLanguage($language)) { throw "Cannot change language: $language" }
    foreach ($row in $rows) {
        $id = $row.GetAttribute('id')
        $en = $row.GetAttribute('text')
        $expected = if ($language -eq $chineseLanguageId) { $zh[$id] } else { $en }
        $text = New-Object TaleWorlds.Localization.TextObject -ArgumentList @("{=$id}$en", $null)
        foreach ($variable in [regex]::Matches($en, '\{([A-Za-z][A-Za-z0-9_]*)\}')) {
            $key = $variable.Groups[1].Value
            [void]$text.SetTextVariable($key, "value_$key")
            $expected = $expected.Replace("{$key}", "value_$key")
        }
        $actual = $text.ToString()
        if ($actual -cne $expected) { throw "Text mismatch: $language/$id expected=[$expected] actual=[$actual]" }
        $checked++
    }
}
Write-Output "PASS real localization DLL: pairs=$($pairs.Count), $checked text resolutions (English, Chinese, untranslated French, switch back)."

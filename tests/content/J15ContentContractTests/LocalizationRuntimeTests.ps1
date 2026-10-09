param(
    [Parameter(Mandatory=$true)][string]$ReferenceDir,
    [string]$ProjectRoot = ''
)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrEmpty($ProjectRoot)) { $ProjectRoot = Join-Path $PSScriptRoot '../../..' }
$ProjectRoot = [IO.Path]::GetFullPath($ProjectRoot)
$ReferenceDir = [IO.Path]::GetFullPath($ReferenceDir)
[void][Reflection.Assembly]::LoadFrom((Join-Path $ReferenceDir 'TaleWorlds.Library.dll'))
$localization = [Reflection.Assembly]::LoadFrom((Join-Path $ReferenceDir 'TaleWorlds.Localization.dll'))
$languageData = $localization.GetType('TaleWorlds.Localization.LanguageData', $true)
$register = $languageData.GetMethod('LoadFromXml', [Reflection.BindingFlags]'Public,Static')
$uiRoot = Join-Path $ProjectRoot 'content/modules/AF.Module.UI/ModuleData/Languages'
$registrationRoot = Join-Path $ProjectRoot 'content/foundation/AF.Foundation.Localization/ModuleData/Languages'
foreach ($name in @('language_data.xml', 'CNs/language_data.xml')) {
    $doc = New-Object Xml.XmlDocument
    $doc.Load((Join-Path $registrationRoot $name))
    if ($name.StartsWith('CNs/')) { $chineseLanguageId = $doc.DocumentElement.GetAttribute('id') }
    # Use the real registration entries, narrowed to this resource pair.
    foreach ($file in @($doc.DocumentElement.SelectNodes('LanguageFile'))) {
        if ($file.GetAttribute('xml_path') -notmatch 'af_ui_strings') {
            [void]$doc.DocumentElement.RemoveChild($file)
        }
    }
    [void]$register.Invoke($null, [object[]]@($doc.PSObject.BaseObject, [string]$uiRoot))
}
$fallback = New-Object Xml.XmlDocument
$fallback.LoadXml('<?xml version="1.0"?><LanguageData id="French" name="French" supported_iso="fr" under_development="false" text_processor="TaleWorlds.Localization.TextProcessor.LanguageProcessors.EnglishTextProcessor" />')
[void]$register.Invoke($null, [object[]]@($fallback.PSObject.BaseObject, [string]$uiRoot))
$english = New-Object Xml.XmlDocument
$english.Load((Join-Path $uiRoot 'af_ui_strings.xml'))
$chinese = New-Object Xml.XmlDocument
$chinese.Load((Join-Path $uiRoot 'CNs/af_ui_strings-zh-CN.xml'))
$zh = @{}
foreach ($row in $chinese.SelectNodes('/base/strings/string')) { $zh[$row.GetAttribute('id')] = $row.GetAttribute('text') }
$checked = 0
foreach ($language in @('English', $chineseLanguageId, 'French', 'English')) {
    if (-not [TaleWorlds.Localization.MBTextManager]::ChangeLanguage($language)) { throw "Cannot change language: $language" }
    foreach ($row in $english.SelectNodes('/base/strings/string')) {
        $id = $row.GetAttribute('id')
        $en = $row.GetAttribute('text')
        $expected = if ($language -eq $chineseLanguageId) { $zh[$id] } else { $en }
        $text = New-Object TaleWorlds.Localization.TextObject -ArgumentList @("{=$id}$en", $null)
        foreach ($variable in [regex]::Matches($en, '\{([A-Z][A-Z0-9_]*)\}')) {
            $key = $variable.Groups[1].Value
            [void]$text.SetTextVariable($key, "value_$key")
            $expected = $expected.Replace("{$key}", "value_$key")
        }
        $actual = $text.ToString()
        if ($actual -cne $expected) { throw "Text mismatch: $language/$id expected=[$expected] actual=[$actual]" }
        $checked++
    }
}
Write-Output "PASS real localization DLL: $checked text resolutions (English, Chinese, untranslated French, switch back)."

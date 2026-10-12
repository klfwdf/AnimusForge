param([Parameter(Mandatory = $true)][string]$EvidencePath)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../../../..'))
$evidenceFull = [IO.Path]::GetFullPath((Join-Path $projectRoot $EvidencePath))
$allowedRoot = Join-Path $projectRoot 'artifacts/localization/verification-20261011'
if ([IO.Path]::GetDirectoryName($evidenceFull) -ne $allowedRoot -or [IO.Path]::GetFileName($evidenceFull) -notlike 'knowledge-closeout-*') {
    throw 'Evidence must stay in the assigned knowledge-closeout artifact namespace.'
}
. (Join-Path $projectRoot 'scripts/build/content_layout.ps1')
. (Join-Path $projectRoot 'scripts/build/package_localization.ps1')
$map = Get-Content -LiteralPath (Join-Path $projectRoot 'content/content-map.json') -Raw -Encoding UTF8 | ConvertFrom-Json
$knowledge = @($map.entries | Where-Object owner -eq 'AF.Module.Knowledge')
$evidence = [Collections.Generic.List[object]]::new()
$worldbook = @{ MappedFiles = @{}; Files = @{}; ChineseBook = '' }
foreach ($name in @('KnowledgeRagPromptTexts', 'WorldEntityPromptTexts')) {
    $target = "ModuleData/$name.json"
    $entry = @($knowledge | Where-Object target -ceq $target)
    if ($entry.Count -ne 1) { throw "Expected one live content-map entry: $target" }
    $chinese = [IO.Path]::GetFullPath((Join-Path $projectRoot $entry[0].source))
    $hashBefore = (Get-FileHash -LiteralPath $chinese -Algorithm SHA256).Hash
    $selected = Get-AnimusForgeEnglishAsset -Entry ([pscustomobject]@{ SourcePath = $chinese; Target = $target }) -Worldbook $worldbook
    $expected = [IO.Path]::ChangeExtension($chinese, 'en.json')
    if ($selected -cne $expected) { throw "Production selector chose the wrong English source: $target" }
    Assert-AnimusForgeEnglishJsonContract -OriginalPath $chinese -EnglishPath $selected
    if (Test-AnimusForgeChineseJsonProse -Text ([IO.File]::ReadAllText($selected))) { throw "English prose still contains Chinese: $target" }
    if ($hashBefore -cne (Get-FileHash -LiteralPath $chinese -Algorithm SHA256).Hash) { throw 'Source changed during projection verification.' }
    $evidence.Add([pscustomobject]@{ Target = $target; CN = $chinese; EN = $selected; ProductionSelector = $true; JsonContract = $true; SourceUnchanged = $true })
}
$registered = @{}
foreach ($registration in @($map.entries | Where-Object target -match '^ModuleData/Languages/(?:CNs/)?language_data\.xml$')) {
    [xml]$xml = Get-Content -LiteralPath (Join-Path $projectRoot $registration.source) -Raw -Encoding UTF8
    foreach ($file in $xml.LanguageData.LanguageFile) { $registered['ModuleData/Languages/' + [string]$file.xml_path] = $true }
}
foreach ($target in @('ModuleData/Languages/afknowledge_strings.xml', 'ModuleData/Languages/CNs/afknowledge_strings-zh-CN.xml')) {
    if (@($knowledge | Where-Object target -ceq $target).Count -ne 1 -or -not $registered.ContainsKey($target)) {
        throw "Knowledge interface is not registered and mapped for both content editions: $target"
    }
    $evidence.Add([pscustomobject]@{ Target = $target; BothEditionsShareRegisteredUI = $true })
}
$compatibility = @($knowledge | Where-Object target -ceq 'ModuleData/KnowledgeSourceTexts.json')
if ($compatibility.Count -ne 1 -or $compatibility[0].localizationRole -cne 'compatibility' -or $compatibility[0].logicalName -cne 'AnimusForge.Knowledge.SourceTexts.json') {
    throw 'Knowledge compatibility resource registration changed.'
}
[xml]$project = Get-Content -LiteralPath (Join-Path $projectRoot 'AnimusForge.csproj') -Raw -Encoding UTF8
$embedded = @($project.Project.ItemGroup.EmbeddedResource | Where-Object LogicalName -ceq 'AnimusForge.Knowledge.SourceTexts.json')
if ($embedded.Count -ne 1 -or $embedded[0].Include.Replace('\', '/') -cne $compatibility[0].source) { throw 'Knowledge embedded resource source/logical name mismatch.' }
$evidence.Add([pscustomobject]@{ Target = 'ModuleData/KnowledgeSourceTexts.json'; LogicalName = 'AnimusForge.Knowledge.SourceTexts.json'; CompatibilityPreservedInBothEditions = $true })
$evidence | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $evidenceFull -Encoding UTF8
Write-Output 'PASS Knowledge production package selector, JSON contract, bilingual UI registration and compatibility manifest.'
Write-Output "Evidence: $evidenceFull"

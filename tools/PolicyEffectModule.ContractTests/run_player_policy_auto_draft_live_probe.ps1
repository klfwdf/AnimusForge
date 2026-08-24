param(
    [Parameter(Mandatory = $true)]
    [string]$AssemblyPath,

    [string]$RepositoryRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path,

    [string]$SettingsPath = "$env:USERPROFILE\Documents\Mount and Blade II Bannerlord\Configs\ModSettings\Global\AnimusForge\AnimusForge_global_settings.json"
)

$ErrorActionPreference = 'Stop'
$AssemblyPath = (Resolve-Path -LiteralPath $AssemblyPath).Path
$RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).Path
$settings = Get-Content -LiteralPath $SettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace([string]$settings.ApiKey)) {
    throw 'Player policy API key is not configured.'
}

$apiUrl = ([string]$settings.ApiUrl).Trim().TrimEnd('/')
if (-not $apiUrl.EndsWith('/chat/completions', [StringComparison]::OrdinalIgnoreCase)) {
    $apiUrl += '/chat/completions'
}
$model = [string]$settings.ModelName
$writingPromptPath = Join-Path $RepositoryRoot 'AnimusForge\CustomPrompts\Policy\PlayerPolicyAutoDraftPrompt.json'
$writingPrompt = (Get-Content -LiteralPath $writingPromptPath -Raw -Encoding UTF8 | ConvertFrom-Json).Text

$assemblyDirectory = Split-Path -Parent $AssemblyPath
[AppDomain]::CurrentDomain.add_AssemblyResolve({
    param($sender, $eventArgs)
    $simpleName = (New-Object Reflection.AssemblyName($eventArgs.Name)).Name
    $candidate = Join-Path $assemblyDirectory ($simpleName + '.dll')
    if (Test-Path -LiteralPath $candidate) {
        return [Reflection.Assembly]::LoadFrom($candidate)
    }
    return $null
})

$assembly = [Reflection.Assembly]::LoadFrom($AssemblyPath)
$requestType = $assembly.GetType('AnimusForge.PlayerPolicyAutoDraftRequest', $true)
$builderType = $assembly.GetType('AnimusForge.PlayerPolicyAutoDraftPromptBuilder', $true)
$flags = [Reflection.BindingFlags]'Static,NonPublic,Public'
$buildMessages = $builderType.GetMethod('BuildMessages', $flags)
$parseResult = $builderType.GetMethod('TryParseResult', $flags)
$headers = @{ Authorization = 'Bearer ' + [string]$settings.ApiKey }

$cases = @(
    @{
        Scope = 'kingdom'
        TargetId = 'kingdom_player'
        TargetName = '玩家王国'
        Selected = '发布给玩家王国'
        Description = '我想减少农户在战后恢复期承担的额外负担，同时组织地方官员恢复生产和市场流通。'
    },
    @{
        Scope = 'vassal'
        TargetId = 'kingdom_vassal'
        TargetName = '直属附庸国'
        Selected = '发布给直属附庸国'
        Description = '我想为直属附庸国提供一段时期的粮食与行政援助，同时尊重其现有治理秩序。'
    },
    @{
        Scope = 'local'
        TargetId = 'kingdom_player'
        TargetName = '玩家王国'
        Selected = 'selectedFiefs=2; names=边堡,河镇'
        Description = '我想在选中的封地加强巡逻、整顿腐败并保障商路，但不要扩大到未选择的地区。'
    }
)

$results = @()
foreach ($case in $cases) {
    $request = [Activator]::CreateInstance($requestType, $true)
    $request.PlayerDescription = $case.Description
    $request.ExistingPolicyName = ''
    $request.DurationText = '45'
    $request.ScopeKind = $case.Scope
    $request.TargetKingdomId = $case.TargetId
    $request.TargetKingdomName = $case.TargetName
    $request.SelectedScopeSummary = $case.Selected
    $request.DateText = 'API contract day 42'
    $request.WritingPrompt = [string]$writingPrompt
    $request.EvaluatorPrompt = 'EVALUATOR_PROMPT_MUST_NOT_BE_USED'
    $request.PolicyRuleContext = 'GENERAL_POLICY_RULE_CONTEXT_MUST_NOT_BE_USED'
    $request.WorldContextCompact = 'WORLD_CONTEXT_MUST_NOT_BE_USED'
    $request.ExtensionContext = 'EXTENSION_CONTEXT_MUST_NOT_BE_USED'
    $request.HistoryPrompt = 'HISTORY_CONTEXT_MUST_NOT_BE_USED'

    $messages = $buildMessages.Invoke($null, @($request))
    $messagesJson = $messages | ConvertTo-Json -Depth 8 -Compress
    $body = @{
        model = $model
        messages = @($messages)
        max_tokens = 1200
        temperature = 0.25
        response_format = @{ type = 'json_object' }
    } | ConvertTo-Json -Depth 12 -Compress

    $response = Invoke-RestMethod -Method Post -Uri $apiUrl -Headers $headers -ContentType 'application/json; charset=utf-8' -Body $body -TimeoutSec 180
    $content = [string]$response.choices[0].message.content
    $parseArguments = [object[]]@($content, $request, $null, $null)
    $parseSuccess = [bool]$parseResult.Invoke($null, $parseArguments)
    $parsed = $parseArguments[2]
    $results += [pscustomobject]@{
        scope = $case.Scope
        model = $model
        apiSuccess = $true
        parseSuccess = $parseSuccess
        messageCount = @($messages).Count
        editablePromptExact = ([string]$messages[0].content) -ceq ([string]$writingPrompt)
        forbiddenContextIncluded = $messagesJson.Contains('_MUST_NOT_BE_USED')
        promptChars = $messagesJson.Length
        responseChars = $content.Length
        policyNameChars = $(if ($parseSuccess) { ([string]$parsed.PolicyName).Length } else { 0 })
        policyContentChars = $(if ($parseSuccess) { ([string]$parsed.PolicyContent).Length } else { 0 })
        validationError = $(if ($parseSuccess) { '' } else { [string]$parseArguments[3] })
    }
}

$results | ConvertTo-Json -Depth 5

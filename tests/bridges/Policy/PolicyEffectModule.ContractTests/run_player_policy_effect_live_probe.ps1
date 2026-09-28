[CmdletBinding()]
param(
    [string]$Scenario = 'lord_pay_and_troop_xp',
    [ValidateRange(1, 100)]
    [int]$PositiveTargetCount = 3,
    [string]$PolicyText = '',
    [string]$ContractExecutable = (Join-Path $PSScriptRoot 'bin\Release\net472\PolicyEffectModule.ContractTests.exe'),
    [string]$SettingsPath = (Join-Path $env:USERPROFILE 'Documents\Mount and Blade II Bannerlord\Configs\ModSettings\Global\AnimusForge\AnimusForge_global_settings.json'),
    [string]$ApiUrl = '',
    [string]$ModelName = '',
    [string]$ApiKeyEnvironmentVariable = 'ANIMUSFORGE_POLICY_LIVE_PROBE_API_KEY',
    [ValidateRange(1, 12000)]
    [int]$MaxTokens = 5000,
    [ValidateRange(0.0, 2.0)]
    [double]$Temperature = 0.25,
    [Alias('ExpectedXpMinimum')]
    [ValidateRange(0, 5000)]
    [int]$ExpectedOnceMinimum = 300,
    [Alias('ExpectedXpMaximum')]
    [ValidateRange(0, 5000)]
    [int]$ExpectedOnceMaximum = 600,
    [ValidateRange(0, 100)]
    [int]$ExpectedDailyMinimum = 1,
    [ValidateRange(0, 100)]
    [int]$ExpectedDailyMaximum = 100,
    [ValidateSet('assignment', 'none', 'once', 'daily', 'both')]
    [string]$SoldierXpExpectation = 'assignment',
    [switch]$NoExitOnFailure,
    [switch]$SyntaxOnly
)

$ErrorActionPreference = 'Stop'

if ($SyntaxOnly) {
    Write-Output 'SYNTAX_OK'
    return
}
if ($ExpectedOnceMinimum -gt $ExpectedOnceMaximum) { throw 'ExpectedOnceMinimum cannot exceed ExpectedOnceMaximum.' }
if ($ExpectedDailyMinimum -gt $ExpectedDailyMaximum) { throw 'ExpectedDailyMinimum cannot exceed ExpectedDailyMaximum.' }
if (-not (Test-Path -LiteralPath $ContractExecutable -PathType Leaf)) {
    throw "Required file not found: $ContractExecutable"
}
if ($ApiKeyEnvironmentVariable -notmatch '^[A-Za-z_][A-Za-z0-9_]*$') {
    throw 'API key environment-variable name is invalid.'
}

function Get-FirstJsonObjectText {
    param([Parameter(Mandatory = $true)][string]$Text)
    $start = $Text.IndexOf('{')
    $end = $Text.LastIndexOf('}')
    if ($start -lt 0 -or $end -le $start) { throw 'Assistant response did not contain a JSON object.' }
    return $Text.Substring($start, $end - $start + 1)
}

function Get-Sha256Text {
    param([string]$Text)
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes([string]$Text))) -replace '-', '').ToLowerInvariant()
    }
    finally { $sha.Dispose() }
}

function Invoke-ContractProbe {
    param([string]$AssessmentJson = '', [string]$PostprocessJson = '')
    $oldPolicy = $env:ANIMUSFORGE_POLICY_PROBE_POLICY_TEXT
    $oldAssessment = $env:ANIMUSFORGE_POLICY_PROBE_ASSESSMENT_JSON
    $oldPostprocess = $env:ANIMUSFORGE_POLICY_PROBE_POSTPROCESS_JSON
    try {
        $env:ANIMUSFORGE_POLICY_PROBE_POLICY_TEXT = $PolicyText
        if ([string]::IsNullOrWhiteSpace($AssessmentJson)) { Remove-Item Env:ANIMUSFORGE_POLICY_PROBE_ASSESSMENT_JSON -ErrorAction SilentlyContinue }
        else { $env:ANIMUSFORGE_POLICY_PROBE_ASSESSMENT_JSON = $AssessmentJson }
        if ([string]::IsNullOrWhiteSpace($PostprocessJson)) { Remove-Item Env:ANIMUSFORGE_POLICY_PROBE_POSTPROCESS_JSON -ErrorAction SilentlyContinue }
        else { $env:ANIMUSFORGE_POLICY_PROBE_POSTPROCESS_JSON = $PostprocessJson }
        $raw = & $ContractExecutable --dump-policy-api-probe $Scenario $PositiveTargetCount 2>&1
        if ($LASTEXITCODE -ne 0) { throw "Contract probe failed with exit code $LASTEXITCODE." }
        return (($raw | Out-String).Trim() | ConvertFrom-Json)
    }
    finally {
        $env:ANIMUSFORGE_POLICY_PROBE_POLICY_TEXT = $oldPolicy
        $env:ANIMUSFORGE_POLICY_PROBE_ASSESSMENT_JSON = $oldAssessment
        $env:ANIMUSFORGE_POLICY_PROBE_POSTPROCESS_JSON = $oldPostprocess
    }
}

function Invoke-ChatStage {
    param(
        [Parameter(Mandatory = $true)][string]$Stage,
        [Parameter(Mandatory = $true)]$Messages,
        [Parameter(Mandatory = $true)][Net.Http.HttpClient]$Client,
        [Parameter(Mandatory = $true)][string]$Url,
        [Parameter(Mandatory = $true)][string]$Model
    )
    $body = [ordered]@{
        model = $Model; messages = @($Messages); max_tokens = $MaxTokens
        temperature = $Temperature; stream = $false
    } | ConvertTo-Json -Depth 30 -Compress
    $content = [Net.Http.StringContent]::new($body, [Text.Encoding]::UTF8, 'application/json')
    $stopwatch = [Diagnostics.Stopwatch]::StartNew()
    try {
        $response = $Client.PostAsync($Url, $content).GetAwaiter().GetResult()
        $responseText = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        $statusCode = [int]$response.StatusCode
        if (-not $response.IsSuccessStatusCode) {
            return [pscustomobject]@{ Stage=$Stage; Success=$false; StatusCode=$statusCode; ElapsedMs=[math]::Round($stopwatch.Elapsed.TotalMilliseconds,1); FinishReason=''; Content=''; Usage=$null; Error='HTTP request failed; response body was not recorded.' }
        }
        $outer = $responseText | ConvertFrom-Json
        $assistantContent = [string]$outer.choices[0].message.content
        if ([string]::IsNullOrWhiteSpace($assistantContent)) {
            return [pscustomobject]@{ Stage=$Stage; Success=$false; StatusCode=$statusCode; ElapsedMs=[math]::Round($stopwatch.Elapsed.TotalMilliseconds,1); FinishReason=[string]$outer.choices[0].finish_reason; Content=''; Usage=$outer.usage; Error='Successful HTTP response contained no assistant content.' }
        }
        return [pscustomobject]@{ Stage=$Stage; Success=$true; StatusCode=$statusCode; ElapsedMs=[math]::Round($stopwatch.Elapsed.TotalMilliseconds,1); FinishReason=[string]$outer.choices[0].finish_reason; Content=$assistantContent; Usage=$outer.usage; Error='' }
    }
    catch {
        $root = $_.Exception
        while ($null -ne $root.InnerException) { $root = $root.InnerException }
        return [pscustomobject]@{ Stage=$Stage; Success=$false; StatusCode=0; ElapsedMs=[math]::Round($stopwatch.Elapsed.TotalMilliseconds,1); FinishReason=''; Content=''; Usage=$null; Error=('Transport or response-envelope parsing failed (' + $root.GetType().Name + '); details were not recorded.') }
    }
    finally { $stopwatch.Stop(); $content.Dispose() }
}

function Convert-UsageSummary {
    param($Usage)
    if ($null -eq $Usage) { return $null }
    return [ordered]@{ prompt_tokens=[int]$Usage.prompt_tokens; completion_tokens=[int]$Usage.completion_tokens; total_tokens=[int]$Usage.total_tokens }
}

function Convert-StageSummary {
    param($StageResult)
    if ($null -eq $StageResult) { return $null }
    return [ordered]@{
        success=[bool]$StageResult.Success; http_status=[int]$StageResult.StatusCode
        elapsed_ms=[double]$StageResult.ElapsedMs; finish_reason=[string]$StageResult.FinishReason
        content_chars=([string]$StageResult.Content).Length; usage=Convert-UsageSummary $StageResult.Usage
        error=[string]$StageResult.Error
    }
}

function Test-MainAssessmentContract {
    param($MainObject)
    $expected = @('publicFeedback','impactSummary','numericIntent','policyContentDigest','feedbackDigest','authoritarianWeight','oligarchicWeight','egalitarianWeight','startupGoldCost','dailyMaintenanceGoldCost','effectDurationMode','durationDays') | Sort-Object
    $actual = @($MainObject.PSObject.Properties.Name | Sort-Object)
    if (($actual -join '|') -cne ($expected -join '|')) { return [pscustomobject]@{ Success=$false; Error='Main assessment fields did not exactly match the generic DTO.' } }
    foreach ($name in @('publicFeedback','impactSummary','numericIntent','policyContentDigest','feedbackDigest')) {
        if ([string]::IsNullOrWhiteSpace([string]$MainObject.$name)) { return [pscustomobject]@{ Success=$false; Error="Main assessment text field was empty: $name" } }
    }
    $mode = [string]$MainObject.effectDurationMode
    $days = [int]$MainObject.durationDays
    if (($mode -eq 'finite' -and $days -le 0) -or ($mode -eq 'permanent' -and $days -ne 0) -or $mode -notin @('finite','permanent')) { return [pscustomobject]@{ Success=$false; Error='Main duration contract was invalid.' } }
    $serialized = $MainObject | ConvertTo-Json -Depth 20 -Compress
    foreach ($forbidden in @('effectIntentVersion','effectIntents','intentLeg','moduleId','targetHandles','payload')) {
        if ($serialized.IndexOf($forbidden,[StringComparison]::OrdinalIgnoreCase) -ge 0) { return [pscustomobject]@{ Success=$false; Error="Main assessment leaked forbidden execution field: $forbidden" } }
    }
    return [pscustomobject]@{ Success=$true; Error='' }
}

function Convert-NumericPayloadSummary {
    param($Payload)
    $summary = [ordered]@{}
    if ($null -eq $Payload) { return $summary }
    foreach ($property in @($Payload.PSObject.Properties | Sort-Object Name)) {
        $value = $property.Value
        if ($value -is [bool] -or $value -is [byte] -or $value -is [sbyte] -or
            $value -is [int16] -or $value -is [uint16] -or $value -is [int32] -or
            $value -is [uint32] -or $value -is [int64] -or $value -is [uint64] -or
            $value -is [single] -or $value -is [double] -or $value -is [decimal]) {
            $summary[$property.Name] = $value
        }
    }
    return $summary
}

function Get-ExpectedModuleIds {
    param([string]$Name)
    switch ($Name) {
        'lord_pay_and_troop_xp' { return @('heroGold','soldierTroopXp') }
        'soldier_troop_xp' { if ($SoldierXpExpectation -eq 'none') { return @() }; return @('soldierTroopXp') }
        'effect_aid' { return @('foodPerDay','loyaltyPerDay') }
        'effect_target_militia_gte200_asc2' { return @('securityPerDay') }
        'unsupported_narrative' { return @() }
        default { return $null }
    }
}

$apiKey = [Environment]::GetEnvironmentVariable($ApiKeyEnvironmentVariable,[EnvironmentVariableTarget]::Process)
if ([string]::IsNullOrWhiteSpace($apiKey)) { throw 'API key is missing from the process-only environment variable.' }
$settings = $null
if (([string]::IsNullOrWhiteSpace($ApiUrl) -or [string]::IsNullOrWhiteSpace($ModelName)) -and (Test-Path -LiteralPath $SettingsPath -PathType Leaf)) {
    $settings = Get-Content -LiteralPath $SettingsPath -Raw -Encoding UTF8 | ConvertFrom-Json
}
$model = if ([string]::IsNullOrWhiteSpace($ModelName)) { [string]$settings.ModelName } else { $ModelName }
$baseUrl = if ([string]::IsNullOrWhiteSpace($ApiUrl)) { [string]$settings.ApiUrl } else { $ApiUrl }
$baseUrl = $baseUrl.Trim().TrimEnd('/')
if ([string]::IsNullOrWhiteSpace($model)) { throw 'Model name is not configured.' }
if ([string]::IsNullOrWhiteSpace($baseUrl)) { throw 'API URL is not configured.' }
$url = if ($baseUrl.EndsWith('/chat/completions',[StringComparison]::OrdinalIgnoreCase)) { $baseUrl } else { $baseUrl + '/chat/completions' }
$apiHost = try { ([Uri]$url).Host } catch { 'invalid' }

$mainStage=$null; $effectStage=$null; $mainContract=[pscustomobject]@{Success=$false;Error='not_run'}
$effectContract=$null; $compilerValidation=$null; $campaignValidation=$null
$returnedModuleIds=@(); $returnedTargetHandles=@(); $disposition=''; $reportError=''; $requestCount=0; $policyForHash=''
$mainStartupGoldCost=$null; $mainDailyMaintenanceGoldCost=$null
$soldierTroopXpOnceValue=0; $soldierTroopXpDailyValue=0; $soldierTroopXpValidation=$null
$effectNumericDetails=@()
$handler = [Net.Http.HttpClientHandler]::new(); $handler.UseProxy=$false
$client = [Net.Http.HttpClient]::new($handler); $client.Timeout=[TimeSpan]::FromSeconds(180)
$client.DefaultRequestHeaders.Authorization = [Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer',$apiKey)

try {
    $initialProbe = Invoke-ContractProbe
    $policyForHash = [string]$initialProbe.policyText
    $requestCount++
    $mainStage = Invoke-ChatStage -Stage 'PlayerPolicyMain' -Messages $initialProbe.mainMessages -Client $client -Url $url -Model $model
    if (-not $mainStage.Success) { $reportError='PlayerPolicyMain failed.' }
    else {
        try {
            $mainJson = Get-FirstJsonObjectText ([string]$mainStage.Content)
            $mainObject = $mainJson | ConvertFrom-Json
            $mainContract = Test-MainAssessmentContract $mainObject
            if (-not $mainContract.Success) { throw $mainContract.Error }
            $mainStartupGoldCost=[double]$mainObject.startupGoldCost
            $mainDailyMaintenanceGoldCost=[double]$mainObject.dailyMaintenanceGoldCost
            $effectProbe = Invoke-ContractProbe -AssessmentJson $mainJson
            $requestCount++
            $effectStage = Invoke-ChatStage -Stage 'PlayerPolicyEffectPostprocess' -Messages $effectProbe.postprocessMessages -Client $client -Url $url -Model $model
            if (-not $effectStage.Success) { $reportError='PlayerPolicyEffectPostprocess failed.' }
            else {
                $effectJson = Get-FirstJsonObjectText ([string]$effectStage.Content)
                $validated = Invoke-ContractProbe -AssessmentJson $mainJson -PostprocessJson $effectJson
                $effectContract=$validated.effect_plan_contract_validation
                $compilerValidation=$validated.offline_synthetic_compiler_validation
                $campaignValidation=$validated.campaign_production_validation
                $effectObject=$effectJson | ConvertFrom-Json
                $disposition=[string]$effectObject.disposition
                $returnedModuleIds=@($effectObject.effects | ForEach-Object {[string]$_.moduleId} | Sort-Object -Unique)
                $returnedTargetHandles=@($effectObject.effects | ForEach-Object {@($_.targetHandles)} | Sort-Object -Unique)
                $effectNumericDetails=@($effectObject.effects | ForEach-Object {
                    [ordered]@{
                        mechanism_id=[string]$_.mechanismId
                        mechanism_kind=[string]$_.mechanismKind
                        role=[string]$_.role
                        module_id=[string]$_.moduleId
                        target_handles=@($_.targetHandles | ForEach-Object {[string]$_})
                        numeric_payload=Convert-NumericPayloadSummary $_.payload
                    }
                })
                $authorized=@($validated.candidateModuleIds | ForEach-Object {[string]$_})
                $unauthorized=@($returnedModuleIds | Where-Object {$_ -notin $authorized})
                $expectedValue=Get-ExpectedModuleIds $Scenario
                $moduleSetValid=$null -eq $expectedValue -or (($returnedModuleIds -join '|') -ceq (@($expectedValue | Sort-Object) -join '|'))
                $expectsNoSoldierXp=$Scenario -eq 'soldier_troop_xp' -and $SoldierXpExpectation -eq 'none'
                $nonExecutableValid=($Scenario -ne 'unsupported_narrative' -and -not $expectsNoSoldierXp) -or ($disposition -in @('narrativeOnly','unsupported') -and @($effectObject.effects).Count -eq 0)
                if ($Scenario -eq 'soldier_troop_xp') {
                    $soldierEffects=@($effectObject.effects | Where-Object { [string]$_.moduleId -eq 'soldierTroopXp' })
                    $soldierEffect=if($soldierEffects.Count -eq 1){$soldierEffects[0]}else{$null}
                    $rawOnce=if($null -eq $soldierEffect){$null}else{$soldierEffect.payload.onceDelta}
                    $rawDaily=if($null -eq $soldierEffect){$null}else{$soldierEffect.payload.dailyDelta}
                    $onceInteger=$rawOnce -is [byte] -or $rawOnce -is [sbyte] -or $rawOnce -is [int16] -or $rawOnce -is [uint16] -or $rawOnce -is [int32] -or $rawOnce -is [uint32] -or $rawOnce -is [int64] -or $rawOnce -is [uint64]
                    $dailyInteger=$rawDaily -is [byte] -or $rawDaily -is [sbyte] -or $rawDaily -is [int16] -or $rawDaily -is [uint16] -or $rawDaily -is [int32] -or $rawDaily -is [uint32] -or $rawDaily -is [int64] -or $rawDaily -is [uint64]
                    if($onceInteger){$soldierTroopXpOnceValue=[long]$rawOnce}
                    if($dailyInteger){$soldierTroopXpDailyValue=[long]$rawDaily}
                    $expectedRuntimeModuleIds=switch($SoldierXpExpectation){
                        'assignment' {@('soldierTroopXpOnce')}
                        'once' {@('soldierTroopXpOnce')}
                        'daily' {@('soldierTroopXpPerDay')}
                        'both' {@('soldierTroopXpOnce','soldierTroopXpPerDay')}
                        default {@()}
                    }
                    $actualRuntimeModuleIds=@($compilerValidation.module_ids | ForEach-Object {[string]$_})
                    $payloadShapeValid=$null -ne $soldierEffect -and $onceInteger -and $dailyInteger -and $soldierTroopXpOnceValue -ge 0 -and $soldierTroopXpOnceValue -le 5000 -and $soldierTroopXpDailyValue -ge 0 -and $soldierTroopXpDailyValue -le 100
                    $valueValid=switch($SoldierXpExpectation){
                        'assignment' {$soldierTroopXpOnceValue -ge $ExpectedOnceMinimum -and $soldierTroopXpOnceValue -le $ExpectedOnceMaximum -and $soldierTroopXpDailyValue -eq 0}
                        'once' {$soldierTroopXpOnceValue -ge $ExpectedOnceMinimum -and $soldierTroopXpOnceValue -le $ExpectedOnceMaximum -and $soldierTroopXpDailyValue -eq 0}
                        'daily' {$soldierTroopXpOnceValue -eq 0 -and $soldierTroopXpDailyValue -ge $ExpectedDailyMinimum -and $soldierTroopXpDailyValue -le $ExpectedDailyMaximum}
                        'both' {$soldierTroopXpOnceValue -ge $ExpectedOnceMinimum -and $soldierTroopXpOnceValue -le $ExpectedOnceMaximum -and $soldierTroopXpDailyValue -ge $ExpectedDailyMinimum -and $soldierTroopXpDailyValue -le $ExpectedDailyMaximum}
                        'none' {$soldierEffects.Count -eq 0}
                        default {$false}
                    }
                    $expansionValid=($actualRuntimeModuleIds -join '|') -ceq (@($expectedRuntimeModuleIds) -join '|')
                    $soldierValid=($SoldierXpExpectation -eq 'none' -or ($soldierEffects.Count -eq 1 -and @($soldierEffect.targetHandles).Count -eq 1 -and [string]$soldierEffect.targetHandles[0] -eq 'H0' -and $payloadShapeValid)) -and $valueValid -and $expansionValid
                    $soldierTroopXpValidation=[ordered]@{valid=$soldierValid;expectation=$SoldierXpExpectation;once_xp_per_troop=$soldierTroopXpOnceValue;daily_xp_per_troop=$soldierTroopXpDailyValue;expected_runtime_module_ids=@($expectedRuntimeModuleIds);actual_runtime_module_ids=$actualRuntimeModuleIds;expansion_valid=$expansionValid}
                    if(-not $soldierValid){$reportError='Clan soldier XP value or runtime expansion did not match the requested expectation.'}
                }
                if ($null -eq $effectContract -or -not [bool]$effectContract.success) { $reportError='EffectPlan failed the direct wire contract.' }
                elseif ($null -eq $compilerValidation -or -not [bool]$compilerValidation.success) { $reportError='EffectPlan failed offline strict Compiler validation.' }
                elseif ($unauthorized.Count -gt 0) { $reportError='EffectPlan used a module that was not injected.' }
                elseif (-not $moduleSetValid) { $reportError='EffectPlan did not use the expected scenario module set.' }
                elseif (-not $nonExecutableValid) { $reportError='Narrative-only scenario invented executable effects.' }
            }
        }
        catch { if ([string]::IsNullOrWhiteSpace($reportError)) { $reportError='A model response failed the next production-stage contract.' } }
    }
}
finally { $client.Dispose(); $handler.Dispose(); $apiKey=$null }

$passed=[string]::IsNullOrWhiteSpace($reportError) -and $null -ne $mainStage -and [bool]$mainStage.Success -and [bool]$mainContract.Success -and $null -ne $effectStage -and [bool]$effectStage.Success -and $null -ne $effectContract -and [bool]$effectContract.success -and $null -ne $compilerValidation -and [bool]$compilerValidation.success
$report=[ordered]@{
    schema_version=7; executed_utc=[DateTime]::UtcNow.ToString('o'); scenario=$Scenario;positive_target_count=$PositiveTargetCount
    policy=[ordered]@{chars=$policyForHash.Length;sha256=Get-Sha256Text $policyForHash;raw_recorded=$false}
    api=[ordered]@{host=$apiHost;model=$model;request_count=$requestCount;max_tokens=$MaxTokens;temperature=$Temperature;api_key_recorded=$false;prompt_recorded=$false;response_content_recorded=$false;proxy_mode='forced-direct'}
    main=Convert-StageSummary $mainStage
    main_contract=[ordered]@{success=[bool]$mainContract.Success;error=[string]$mainContract.Error}
    main_policy_costs=[ordered]@{startup_gold_cost=$mainStartupGoldCost;daily_maintenance_gold_cost=$mainDailyMaintenanceGoldCost}
    main_numeric_assessment=[ordered]@{authoritarian_weight=$(if($null -eq $mainObject){$null}else{[double]$mainObject.authoritarianWeight});oligarchic_weight=$(if($null -eq $mainObject){$null}else{[double]$mainObject.oligarchicWeight});egalitarian_weight=$(if($null -eq $mainObject){$null}else{[double]$mainObject.egalitarianWeight});duration_mode=$(if($null -eq $mainObject){''}else{[string]$mainObject.effectDurationMode});duration_days=$(if($null -eq $mainObject){0}else{[int]$mainObject.durationDays})}
    effect=Convert-StageSummary $effectStage
    effect_plan=[ordered]@{disposition=$disposition;module_ids=$returnedModuleIds;target_handles=$returnedTargetHandles;effects=$effectNumericDetails}
    soldier_troop_xp_validation=$soldierTroopXpValidation
    effect_plan_contract_validation=$effectContract
    offline_synthetic_compiler_validation=$compilerValidation
    campaign_production_validation=$campaignValidation
    routing_covered=$false;automatic_repair_api_used=$false;error=$reportError;passed=$passed
}
$report | ConvertTo-Json -Depth 20
if (-not $passed -and -not $NoExitOnFailure) { exit 2 }

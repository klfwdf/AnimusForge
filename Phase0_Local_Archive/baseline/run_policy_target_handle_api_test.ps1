[CmdletBinding()]
param(
	[switch]$CrlPrecision,
	[switch]$SyntaxOnly
)

$ErrorActionPreference = 'Stop'
if ($SyntaxOnly) {
	Write-Output 'SYNTAX_OK'
	return
}

$configPath = Join-Path $env:USERPROFILE 'Documents\Mount and Blade II Bannerlord\Configs\ModSettings\Global\AnimusForge\AnimusForge_global_settings.json'
$config = Get-Content -LiteralPath $configPath -Raw -Encoding utf8 | ConvertFrom-Json
$apiKey = [string]$config.ApiKey
if ([string]::IsNullOrWhiteSpace($apiKey)) { throw 'API key is missing.' }
$baseUrl = ([string]$config.ApiUrl).Trim().TrimEnd('/')
$url = if ($baseUrl.EndsWith('/chat/completions', [System.StringComparison]::OrdinalIgnoreCase)) { $baseUrl } else { $baseUrl + '/chat/completions' }
$model = [string]$config.ModelName
if ([string]::IsNullOrWhiteSpace($model)) { throw 'Model name is missing.' }

$crlCases = @(
	[pscustomobject]@{
		id = 'clan_typo_mechanical_vs_background'
		policy = '巴努·胡勒延只负责咨询；免除贡达夫家族全部领地的赋税。'
		handles = @(
			'S|settlement|政策发布地',
			'C0|clan|巴努·胡勒延|精确名称候选',
			'C1|clan|贡达罗夫|实体名称近似命中:贡达罗夫'
		)
		expected = @('C1')
	},
	[pscustomobject]@{
		id = 'ruler_typo_family_target'
		policy = '由朗德领主及其家族承担军费；库洛夫家族仅列席。'
		handles = @(
			'S|settlement|政策发布地',
			'R1|ruler|朗瓦德→其氏族领地|实体名称近似命中:朗瓦德',
			'C0|clan|库洛夫|精确名称候选'
		)
		expected = @('R1')
	},
	[pscustomobject]@{
		id = 'city_typo_vs_named_village'
		policy = '修缮萨特的城墙与粮仓；萨万特村不在本令范围内。'
		handles = @(
			'S|settlement|政策发布地',
			'L1|settlement|萨哥特|实体名称近似命中:萨哥特',
			'L2|settlement|萨万特|精确名称候选'
		)
		expected = @('L1')
	},
	[pscustomobject]@{
		id = 'castle_typo_vs_bound_village'
		policy = '加强乌斯科堡的驻防；乌斯托科村不执行本令。'
		handles = @(
			'S|settlement|政策发布地',
			'L1|settlement|乌斯托科堡|实体名称近似命中:乌斯托科堡',
			'L2|settlement|乌斯托科|精确名称候选'
		)
		expected = @('L1')
	},
	[pscustomobject]@{
		id = 'village_typo_vs_bound_castle'
		policy = '改善于克村的农田与户数；于桑克堡不执行本令。'
		handles = @(
			'S|settlement|政策发布地',
			'L1|settlement|于桑克|实体名称近似命中:于桑克',
			'L2|settlement|于桑克堡|精确名称候选'
		)
		expected = @('L1')
	},
	[pscustomobject]@{
		id = 'richest_clan_materialized_top1'
		policy = '向本国最富有家族的全部领地征收特别税。'
		handles = @(
			'S|settlement|政策发布地',
			'C1|clan|俄斯提科斯|Facet:metric_wealth_high+relation_domestic+type_clan'
		)
		expected = @('C1')
	},
	[pscustomobject]@{
		id = 'border_granary_city_materialized_top1'
		policy = '增援本国边境粮仓城市，提高该城粮食储备。'
		handles = @(
			'S|settlement|政策发布地',
			'L1|settlement|厄庇克洛忒亚|Facet:geography_border+metric_food_high+relation_domestic+type_city'
		)
		expected = @('L1')
	},
	[pscustomobject]@{
		id = 'named_contacts_without_mechanical_effect'
		policy = '请贡达夫家族联络乌斯科堡并报告粮仓与守军情况，不实施任何机械效果。'
		handles = @(
			'S|settlement|政策发布地',
			'C1|clan|贡达罗夫|实体名称近似命中:贡达罗夫',
			'L1|settlement|乌斯托科堡|实体名称近似命中:乌斯托科堡'
		)
		expected = @()
	}
)

$defaultCases = @(
	[pscustomobject]@{
		id = 'north_enemy_kingdom'
		policy = '制裁北方敌国的商路，使其贸易收入持续下降。'
		handles = @(
			'K0|kingdom|玩家王国',
			'K1|kingdom|南方盟国',
			'K2|kingdom|北方敌国|语义依据:敌对+北方',
			'K3|kingdom|东方敌国'
		)
		expected = @('K2')
	},
	[pscustomobject]@{
		id = 'richest_domestic_clan'
		policy = '向本国最富有家族的全部领地征收特别税。'
		handles = @(
			'S|settlement|政策发布地',
			'C0|clan|本国贫穷家族',
			'C1|clan|本国最富有家族|语义依据:国内+家族+财富最高',
			'C2|clan|本国普通家族'
		)
		expected = @('C1')
	},
	[pscustomobject]@{
		id = 'border_granary_city'
		policy = '增援本国边境粮仓城市，提高该城粮食储备。'
		handles = @(
			'S|settlement|政策发布地',
			'L1|settlement|内陆低粮城市',
			'L2|settlement|边境高粮城市|语义依据:城市+边境+粮食最高',
			'L3|settlement|边境低粮城市'
		)
		expected = @('L2')
	},
	[pscustomobject]@{
		id = 'vassal_east_weakest_enemy'
		policy = '援助附庸以东最弱的敌国，使该国稳定度提高。'
		handles = @(
			'K0|kingdom|目标附庸国',
			'K1|kingdom|玩家宗主国',
			'K2|kingdom|西方较强敌国',
			'K3|kingdom|附庸以东最弱敌国|语义依据:敌对+东方+实力最低'
		)
		expected = @('K3')
	},
	[pscustomobject]@{
		id = 'contact_report_only'
		policy = '请外交官联络北方敌国并报告局势，不实施制裁、援助或任何机械效果。'
		handles = @(
			'K0|kingdom|玩家王国',
			'K2|kingdom|北方敌国|仅作为联络与报告对象'
		)
		expected = @()
	}
)
$cases = if ($CrlPrecision) { $crlCases } else { $defaultCases }

$systemPrompt = @'
你是玩家政策链路的目标句柄选择校验器。只输出一个 JSON 对象，不要 Markdown，不要解释。
输出结构：{"results":[{"case_id":"...","selected_targets":["合法短句柄"],"reason":"极短原因"}]}。
每个案例必须且只能输出一次。selected_targets 只能填写该案例句柄目录中每行第一个 | 左侧的短句柄，禁止复制名称、跨案例引用或编造。
只选择政策会直接产生机械数值变化的对象。仅作为联系人、报告对象、求助对象、协调对象或背景出现的实体不是机械目标；没有机械目标时必须输出空数组。
句柄中的“实体名称近似命中”或“Facet”依据已由本地 ONNX 召回并经 C# 作用域校验；原文简称或轻微错字可以对应到该句柄，但候选存在不代表必须选择。征税、免税、承担军费、修缮、增援等直接改变数值的措施属于机械效果，即使没有写具体数值也必须选择其直接目标。
'@

$caseBlocks = foreach ($case in $cases) {
	@(
		('【case_id】' + $case.id),
		('【政策】' + $case.policy),
		'【合法目标句柄】',
		($case.handles -join [Environment]::NewLine)
	) -join [Environment]::NewLine
}
$userPrompt = $caseBlocks -join ([Environment]::NewLine + [Environment]::NewLine)
$body = [ordered]@{
	model = $model
	messages = @(
		[ordered]@{ role = 'system'; content = $systemPrompt },
		[ordered]@{ role = 'user'; content = $userPrompt }
	)
	temperature = 0.25
	max_tokens = 2000
	stream = $false
	response_format = [ordered]@{ type = 'json_object' }
	thinking = [ordered]@{ type = 'disabled' }
} | ConvertTo-Json -Depth 20 -Compress

$handler = [System.Net.Http.HttpClientHandler]::new()
$proxyValue = [string]$env:HTTPS_PROXY
if ([string]::IsNullOrWhiteSpace($proxyValue)) { $proxyValue = [string]$env:HTTP_PROXY }
if (-not [string]::IsNullOrWhiteSpace($proxyValue)) {
	$handler.Proxy = [System.Net.WebProxy]::new($proxyValue)
	$handler.UseProxy = $true
}
$client = [System.Net.Http.HttpClient]::new($handler)
$client.Timeout = [TimeSpan]::FromSeconds(180)
$client.DefaultRequestHeaders.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $apiKey)
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$httpStatus = 0
$finishReason = ''
$usage = $null
$validation = [System.Collections.Generic.List[object]]::new()
$requestError = ''
$allPassed = $false
$httpContent = [System.Net.Http.StringContent]::new($body, [System.Text.Encoding]::UTF8, 'application/json')
try {
	$response = $client.PostAsync($url, $httpContent).GetAwaiter().GetResult()
	$outerText = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
	$httpStatus = [int]$response.StatusCode
	if (-not $response.IsSuccessStatusCode) {
		$requestError = 'HTTP request failed.'
	}
	else {
		$outer = $outerText | ConvertFrom-Json
		$finishReason = [string]$outer.choices[0].finish_reason
		$usage = $outer.usage
		$assistantText = [string]$outer.choices[0].message.content
		$parsed = $assistantText | ConvertFrom-Json
		$resultMap = @{}
		$duplicateCaseIds = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
		foreach ($item in @($parsed.results)) {
			$id = [string]$item.case_id
			if ($resultMap.ContainsKey($id)) { [void]$duplicateCaseIds.Add($id) } else { $resultMap[$id] = $item }
		}
		foreach ($case in $cases) {
			$errors = [System.Collections.Generic.List[string]]::new()
			$selected = [System.Collections.Generic.List[string]]::new()
			$allowed = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
			foreach ($line in $case.handles) { [void]$allowed.Add(([string]$line).Split('|')[0]) }
			if (-not $resultMap.ContainsKey($case.id)) {
				$errors.Add('missing_case')
			}
			else {
				$seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
				foreach ($target in @($resultMap[$case.id].selected_targets)) {
					$key = [string]$target
					$selected.Add($key)
					if (-not $allowed.Contains($key)) { $errors.Add('invented_or_cross_case_handle:' + $key) }
					if (-not $seen.Add($key)) { $errors.Add('duplicate_handle:' + $key) }
				}
			}
			$expectedSet = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
			foreach ($key in $case.expected) { [void]$expectedSet.Add([string]$key) }
			$selectedSet = [System.Collections.Generic.HashSet[string]]::new($selected, [System.StringComparer]::OrdinalIgnoreCase)
			if (-not $selectedSet.SetEquals($expectedSet)) { $errors.Add('expected_target_mismatch') }
			if ($duplicateCaseIds.Contains($case.id)) { $errors.Add('duplicate_case') }
			$validation.Add([pscustomobject]@{
				case_id = $case.id
				passed = ($errors.Count -eq 0)
				selected_targets = @($selected)
				expected_targets = @($case.expected)
				errors = @($errors)
			})
		}
		foreach ($returnedId in $resultMap.Keys) {
			if (-not ($cases.id -contains $returnedId)) {
				$validation.Add([pscustomobject]@{ case_id = [string]$returnedId; passed = $false; selected_targets = @(); expected_targets = @(); errors = @('unexpected_case') })
			}
		}
		$allPassed = @($validation | Where-Object { -not $_.passed }).Count -eq 0 -and $resultMap.Count -eq $cases.Count
	}
}
catch {
	$requestError = $_.Exception.Message
}
finally {
	$stopwatch.Stop()
	$httpContent.Dispose()
	$client.Dispose()
	$handler.Dispose()
	$apiKey = $null
}

$report = [ordered]@{
	schema_version = 1
	executed_local_date = '2026-08-08'
	test = if ($CrlPrecision) { 'policy_target_crl_precision_legal_handle_selection' } else { 'policy_target_legal_handle_selection' }
	api = [ordered]@{
		host = ([Uri]$url).Host
		model = $model
		request_count = 1
		retry_count = 0
		max_tokens = 2000
		temperature = 0.25
		json_object_response_format = $true
		thinking_type = 'disabled'
		api_key_recorded = $false
		prompt_recorded = $false
		response_content_recorded = $false
	}
	http_status = $httpStatus
	elapsed_ms = [math]::Round($stopwatch.Elapsed.TotalMilliseconds, 1)
	finish_reason = $finishReason
	usage = if ($null -eq $usage) { $null } else { [ordered]@{
		prompt_tokens = [int]$usage.prompt_tokens
		completion_tokens = [int]$usage.completion_tokens
		cache_hit_tokens = [int]$usage.prompt_cache_hit_tokens
		cache_miss_tokens = [int]$usage.prompt_cache_miss_tokens
	} }
	request_error = $requestError
	cases = @($validation)
	passed = ($allPassed -and [string]::IsNullOrWhiteSpace($requestError))
}
$reportName = if ($CrlPrecision) { 'policy_target_crl_handle_selection_api_20260808.json' } else { 'policy_target_handle_selection_api_20260808.json' }
$reportPath = Join-Path (Get-Location) ('Phase0_Local_Archive\reports\' + $reportName)
$report | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $reportPath -Encoding utf8NoBOM
$report | ConvertTo-Json -Depth 20
if (-not $report.passed) { exit 2 }

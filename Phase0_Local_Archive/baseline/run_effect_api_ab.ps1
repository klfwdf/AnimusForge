[CmdletBinding()]
param([switch]$SyntaxOnly)

$ErrorActionPreference = 'Stop'
if ($SyntaxOnly) {
	Write-Output 'SYNTAX_OK'
	return
}

$configPath = Join-Path $env:USERPROFILE 'Documents\Mount and Blade II Bannerlord\Configs\ModSettings\Global\AnimusForge\AnimusForge_global_settings.json'
$config = Get-Content -LiteralPath $configPath -Raw | ConvertFrom-Json
$apiKey = [string]$config.ApiKey
if ([string]::IsNullOrWhiteSpace($apiKey)) { throw 'API key is missing.' }
$baseUrl = ([string]$config.ApiUrl).Trim().TrimEnd('/')
$url = if ($baseUrl.EndsWith('/chat/completions', [System.StringComparison]::OrdinalIgnoreCase)) { $baseUrl } else { $baseUrl + '/chat/completions' }
$model = [string]$config.ModelName
if ([string]::IsNullOrWhiteSpace($model)) { throw 'Model name is missing.' }

$catalog = @'
- prosperityPerDay: 城镇每日繁荣变化
- foodPerDay: 城镇每日粮食库存变化
- hearthPerDay: 村庄每日户数变化
- loyaltyPerDay: 城镇每日忠诚变化
- securityPerDay: 城镇每日治安变化
- militiaPerDay: 定居点每日民兵变化
- taxIncomePct: 城镇或城堡所属氏族税收百分比变化
- constructionPerDay: 城镇或城堡每日建造力点数变化
- kingdomStabilityOnce: 政策生效时一次性王国稳定度变化
'@

$selectedRules = @'
- prosperityPerDay：每座目标城镇每日繁荣变化，正数增加、负数减少。仅在贸易、工商业、市场信心、税负冲击、战争破坏或明确发展措施会持续改变繁荣时输出。轻微影响通常每天 ±0.5～2，普通政策 ±2～5，明显经济刺激、封锁或破坏 ±5～10，全国重大投资或灾难通常 ±10～40；极端动员、巨额投入或国家级经济灾难且执行路径明确时可达重大档的 2～4 倍。
- foodPerDay：每座目标城镇每日粮食库存变化，正数增加、负数减少；只有储备、采购、运输、征收、赈济、军粮消耗、封锁或破坏直接改变粮食时输出。轻微影响通常每天 ±1～3，普通粮政或仓储运输 ±3～8，大规模赈济、强征或军粮调拨 ±8～15，全国重大补给、饥荒或封锁通常 ±10～40，极端灾害或全国粮食行动且措施明确时可达约 ±30～45 或相称更高档。
- loyaltyPerDay：每座目标城镇每日忠诚变化，正数提高、负数降低；不要用繁荣代替政治认同。只有民心、公平感、文化关系、自治、荣誉、压迫或利益分配受到直接持续影响时输出。轻微影响通常每天 ±0.1～0.4，普通安抚、税负或自治调整 ±0.4～1.2，重大改革、强力压迫、广泛赈济或明显歧视通常 ±2～6，极端暴政、重大救民、系统性迫害或接近叛乱级刺激可达 ±4～12。
- securityPerDay：每座目标城镇每日治安变化，正数改善、负数恶化；只在巡逻、执法、腐败整顿、匪患、军管、镇压或秩序崩坏直接改变地方安全时输出。轻微影响通常每天 ±0.1～0.3，普通巡逻或执法调整 ±0.3～0.7，强力治安行动、军管或匪患爆发 ±0.8～1.5，重大高压治理或严重失序通常 ±2～6；极端内乱、血腥镇压或大规模匪患且正文直接支持时可达重大档的 2～4 倍。
- constructionPerDay：每座目标城镇或城堡每日直接增加的原版建造力固定点数，正数加快、负数拖慢；这不是百分比，+100 就是每日增加 100 建造力。只有修建、修缮、工匠、劳力、建材、工程运输、停工或破坏等明确建设措施才输出。小规模通常 ±20～60，持续扩充工程资源 +60～150，全国重大建设通常 +300～1000；极端动员、巨额专项投入或玩家明确要求极端强度且执行路径清楚时可达重大档的 2～4 倍，超过 +1000 完全合法。
- kingdomStabilityOnce：目标王国正式生效时一次性稳定度变化，正数提高、负数降低，最终 changes 值必须是整数；只用于王国或附庸国政策，地方政策禁止，不随持续天数每日累加，也不按城镇数量叠加。纯行政或普通地方数值变化通常为 0；明显改变民众信心、财政威望、封臣信任或王权合法性通常 ±4～7，重大改革、全国动员、贵族冲突或严重危机 ±7～14，内战边缘、国家存亡、体系崩溃或决定性胜利通常 ±14～22；极端且直接支持时可相称更高。
'@

$stablePrefix = '你是三段式玩家政策链路的最终效果后处理阶段。只输出一个 json 对象，不要 Markdown，不要解释。ONNX 选中模块会提供详细量纲规则；极短能力目录中的其他已注册模块只在主评议意图明确需要时使用。不得改变政策方向、作用域或权威期限。极短字段结构：impactSummary:string；durationDays:positive_integer；effects:[{targets:[handle],changes:{moduleId:finite_number},reason:string}]。解析或校验失败时系统不会重试。'
$commonCalibration = '除 kingdomStabilityOnce 外，所有模块值都是目标对象每天实际结算的变化，不是整个周期总量；持续时间用于判断措施能否维持，不得把直接成立的每日强度机械摊薄成象征性数值。政策正文、numericIntent 或玩家 MCM 评判偏好里明确的金额、倍率、档位、范围和强弱必须用于定标，但不要把金额机械线性换算成某一数值。同一执行方案直接产生的多项效果可以同时输出，不限制一至三项；巨额财政投入本身已是代价，不得为了平衡臆造负面效果。普通政策按模块常规档，重大改革、全国行动、巨额投入或极端破坏按模块重大/极端档；不要无依据把所有变化默认成 1 或 5。'
$stableSystem = @(
	$stablePrefix
	'【通用强度定标】'
	$commonCalibration
	'【当前作用域的效果能力极短目录】'
	$catalog.Trim()
	'目录项不是效果要求；只有政策语义明确需要时才输出对应模块。'
) -join [Environment]::NewLine
$dynamicSystem = @(
	'【ONNX 优先模块的后处理详细规则】'
	$selectedRules.Trim()
	''
	'【本次作用域规则】'
	'只能引用 K* 合法句柄；K0 是玩家王国，其他 K* 仅在政策确实直接改变对应外国时引用。'
) -join [Environment]::NewLine

$cases = @(
	[pscustomobject]@{
		id = 'vague-original'
		name = '百万发展投资'
		content = '我出一百万为了王国发展。'
		digest = '统治者投入一百万第纳尔推动王国整体发展。'
	},
	[pscustomobject]@{
		id = 'explicit-original'
		name = '王国综合发展投资'
		content = '投入一百万第纳尔，修建公共工程、扩充各城镇粮仓、安抚民众提高忠诚，并加强治安巡逻，推动王国繁荣发展。'
		digest = '投入巨额资金修建公共工程、扩充粮仓、安抚民众、加强治安并推动王国繁荣。'
	}
)

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

$results = [System.Collections.Generic.List[object]]::new()
try {
	foreach ($case in $cases) {
		$assessment = [ordered]@{
			impactSummary = '王国投资将持续推动繁荣、公共工程、粮食供给、民众忠诚和社会治安改善。'
			numericIntent = 'K0：持续提高城镇繁荣；显著增加各城镇和城堡每日建造力；改善各城镇粮食供给；持续提高各城镇忠诚；持续改善各城镇治安。'
			durationDays = 30
		} | ConvertTo-Json -Compress

		$user = @(
			'【政策】'
			('名称：' + $case.name)
			('内容摘要：' + $case.digest)
			('原文摘录：' + $case.content)
			''
			'【主评议结果】'
			$assessment
			''
			'【本次全部合法目标句柄】'
			'- K0|kingdom|玩家王国'
			''
			'【硬规则】'
			''
			'durationDays 必须严格等于 30。'
			'effects 每条只能使用 {"targets":["合法短句柄"],"changes":{"目录中的模块ID":有限数字},"reason":"可选短原因"}。'
			'候选句柄只是可选目标；仅作联系人、报告、求助、协调对象或背景出现的实体不是效果目标。'
			'不同目标可输出不同 changes，相同 changes 可共用 targets；同一 target 与模块组合只能出现一次。'
			'不影响的模块必须省略，不要填一排 0；若政策没有机械数值效果，输出 effects=[]。'
			'targets 只能填写目标目录每行第一个 | 左侧的短句柄，不得复制名称或整行目录。'
		) -join [Environment]::NewLine

		$body = [ordered]@{
			model = $model
			messages = @(
				[ordered]@{ role = 'system'; content = $stableSystem },
				[ordered]@{ role = 'system'; content = $dynamicSystem },
				[ordered]@{ role = 'user'; content = $user }
			)
			temperature = 0.25
			max_tokens = 5000
			stream = $false
			response_format = [ordered]@{ type = 'json_object' }
			thinking = [ordered]@{ type = 'disabled' }
		} | ConvertTo-Json -Depth 20 -Compress

		$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
		$httpContent = [System.Net.Http.StringContent]::new($body, [System.Text.Encoding]::UTF8, 'application/json')
		try {
			$response = $client.PostAsync($url, $httpContent).GetAwaiter().GetResult()
			$outerText = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
			$stopwatch.Stop()
			if (-not $response.IsSuccessStatusCode) {
				$results.Add([pscustomobject]@{
					case_id = $case.id
					success = $false
					http_status = [int]$response.StatusCode
					elapsed_ms = [math]::Round($stopwatch.Elapsed.TotalMilliseconds, 1)
					error = 'HTTP request failed'
				})
				continue
			}

			$outer = $outerText | ConvertFrom-Json
			$assistantText = [string]$outer.choices[0].message.content
			$parsed = $null
			$parseError = ''
			try { $parsed = $assistantText | ConvertFrom-Json } catch { $parseError = $_.Exception.Message }
			$moduleIds = [System.Collections.Generic.List[string]]::new()
			if ($null -ne $parsed -and $null -ne $parsed.effects) {
				foreach ($effect in $parsed.effects) {
					if ($null -ne $effect.changes) {
						foreach ($property in $effect.changes.PSObject.Properties) { $moduleIds.Add($property.Name) }
					}
				}
			}
			$results.Add([pscustomobject]@{
				case_id = $case.id
				success = ($null -ne $parsed)
				http_status = [int]$response.StatusCode
				elapsed_ms = [math]::Round($stopwatch.Elapsed.TotalMilliseconds, 1)
				finish_reason = [string]$outer.choices[0].finish_reason
				prompt_tokens = [int]$outer.usage.prompt_tokens
				completion_tokens = [int]$outer.usage.completion_tokens
				cache_hit_tokens = [int]$outer.usage.prompt_cache_hit_tokens
				cache_miss_tokens = [int]$outer.usage.prompt_cache_miss_tokens
				parse_error = $parseError
				modules = @($moduleIds)
				result = $parsed
			})
		}
		catch {
			$stopwatch.Stop()
			$results.Add([pscustomobject]@{
				case_id = $case.id
				success = $false
				elapsed_ms = [math]::Round($stopwatch.Elapsed.TotalMilliseconds, 1)
				error = $_.Exception.Message
			})
		}
		finally {
			$httpContent.Dispose()
		}
	}
}
finally {
	$client.Dispose()
	$handler.Dispose()
	$apiKey = $null
}

$report = [ordered]@{
	schema_version = 1
	executed_local_date = '2026-08-08'
	test = 'player_policy_effect_ai_vague_vs_explicit_calibrated_5000'
	api = [ordered]@{
		host = ([Uri]$url).Host
		model = $model
		request_count = $cases.Count
		retry_count = 0
		max_tokens = 5000
		temperature = 0.25
		json_object_response_format = $true
		thinking_type = 'disabled'
		api_key_recorded = $false
	}
	hypothesis = 'Compare a vague original policy against an explicit original while keeping the same multi-effect numericIntent and calibrated recalled-module rules.'
	results = @($results)
}
$reportPath = Join-Path (Get-Location) 'Phase0_Local_Archive\reports\player_policy_effect_ai_vague_vs_explicit_calibrated_5000_20260808.json'
$report | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $reportPath -Encoding utf8NoBOM
$report | ConvertTo-Json -Depth 20

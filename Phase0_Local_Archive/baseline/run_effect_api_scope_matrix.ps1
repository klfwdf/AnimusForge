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

$moduleCatalog = [ordered]@{
	prosperityPerDay = '城镇每日繁荣变化'
	foodPerDay = '城镇每日粮食库存变化'
	hearthPerDay = '村庄每日户数变化'
	loyaltyPerDay = '城镇每日忠诚变化'
	securityPerDay = '城镇每日治安变化'
	militiaPerDay = '定居点每日民兵变化'
	taxIncomePct = '城镇或城堡所属氏族税收百分比变化'
	constructionPerDay = '城镇或城堡每日建造力点数变化'
	kingdomStabilityOnce = '政策生效时一次性王国稳定度变化'
}
$moduleRules = @{
	prosperityPerDay = '每座目标城镇每日繁荣变化，正数增加、负数减少。仅在贸易、工商业、市场信心、税负冲击、战争破坏或明确发展措施会持续改变繁荣时输出。轻微影响通常每天 ±0.5～2，普通政策 ±2～5，明显经济刺激、封锁或破坏 ±5～10，全国重大投资或灾难通常 ±10～40；极端动员、巨额投入或国家级经济灾难且执行路径明确时可达重大档的 2～4 倍。'
	foodPerDay = '每座目标城镇每日粮食库存变化，正数增加、负数减少；只有储备、采购、运输、征收、赈济、军粮消耗、封锁或破坏直接改变粮食时输出。轻微影响通常每天 ±1～3，普通粮政或仓储运输 ±3～8，大规模赈济、强征或军粮调拨 ±8～15，全国重大补给、饥荒或封锁通常 ±10～40，极端灾害或全国粮食行动且措施明确时可达约 ±30～45 或相称更高档。'
	hearthPerDay = '每座目标村庄每日户数变化，正数增加、负数减少；只用于劳动力、安置、迁徙、徭役、逃亡、屠掠或灾荒直接造成的农村人口变化。轻微影响通常每天 ±0.1～0.5，普通恢复、徭役或迁徙 ±0.5～1.5，强力移民、重税压迫或大规模劳役 ±2～4，全国人口扶持、屠掠或灾荒通常 ±3～12；极端强制迁徙或人口灾难且正文直接支持时可达重大档的 2～4 倍。'
	loyaltyPerDay = '每座目标城镇每日忠诚变化，正数提高、负数降低；不要用繁荣代替政治认同。只有民心、公平感、文化关系、自治、荣誉、压迫或利益分配受到直接持续影响时输出。轻微影响通常每天 ±0.1～0.4，普通安抚、税负或自治调整 ±0.4～1.2，重大改革、强力压迫、广泛赈济或明显歧视通常 ±2～6，极端暴政、重大救民、系统性迫害或接近叛乱级刺激可达 ±4～12。'
	securityPerDay = '每座目标城镇每日治安变化，正数改善、负数恶化；只在巡逻、执法、腐败整顿、匪患、军管、镇压或秩序崩坏直接改变地方安全时输出。轻微影响通常每天 ±0.1～0.3，普通巡逻或执法调整 ±0.3～0.7，强力治安行动、军管或匪患爆发 ±0.8～1.5，重大高压治理或严重失序通常 ±2～6；极端内乱、血腥镇压或大规模匪患且正文直接支持时可达重大档的 2～4 倍。'
	militiaPerDay = '每座目标定居点每日民兵变化，正数增加、负数减少；只有训练、征召、裁撤、士气、地方防务或军事崩溃直接改变民兵时输出。轻微影响通常每天 ±0.5～1.5，普通训练或征召 ±1.5～4，强力民兵动员或防务改革 ±4～8，全国战争动员或大规模军事化通常 ±10～40；极端总动员或严重军事崩溃且资源与执行路径明确时可达重大档的 2～4 倍。'
	taxIncomePct = '目标城镇或城堡所属氏族最终收到的原版主税收收入百分比点变化，正数增收、负数减收；不是当地百姓被抽取的税额，也不含村庄独立收入或关税。税款由 A 转给 B 时分别表达 A 负、B 正，不要用繁荣代替。普通税制微调通常 ±5%～15%，明显增减税、征收改革或贡税转移 ±15%～35%，全国重大税制通常 ±20%～60%；正文明确取消全部主税、极端掠夺或极端税制时可超过 ±60%，接近全部取消时才接近 -100%。'
	constructionPerDay = '每座目标城镇或城堡每日直接增加的原版建造力固定点数，正数加快、负数拖慢；这不是百分比，+100 就是每日增加 100 建造力。只有修建、修缮、工匠、劳力、建材、工程运输、停工或破坏等明确建设措施才输出。小规模通常 ±20～60，持续扩充工程资源 +60～150，全国重大建设通常 +300～1000；极端动员、巨额专项投入或玩家明确要求极端强度且执行路径清楚时可达重大档的 2～4 倍，超过 +1000 完全合法。'
	kingdomStabilityOnce = '目标王国正式生效时一次性稳定度变化，正数提高、负数降低，最终 changes 值必须是整数；只用于王国或附庸国政策，地方政策禁止，不随持续天数每日累加，也不按城镇数量叠加。纯行政或普通地方数值变化通常为 0；明显改变民众信心、财政威望、封臣信任或王权合法性通常 ±4～7，重大改革、全国动员、贵族冲突或严重危机 ±7～14，内战边缘、国家存亡、体系崩溃或决定性胜利通常 ±14～22；极端且直接支持时可相称更高。'
}
$knownModules = [System.Collections.Generic.HashSet[string]]::new([string[]]$moduleCatalog.Keys, [System.StringComparer]::Ordinal)

function New-Requirement([string]$target, [string]$module, [string]$sign, [double]$minimumAbs = 0) {
	[pscustomobject]@{ target = $target; module = $module; sign = $sign; minimum_abs = $minimumAbs }
}

$cases = @(
	[pscustomobject]@{
		id = 'local-other-settlement'
		scope = 'local'
		name = '德赫林姆粮道工程'
		content = '政策虽然在帕拉汶发布，但资金只用于德赫林姆：扩充粮仓并修筑道路，发布地和其他领地不变。'
		digest = '只改善德赫林姆的粮食储备和公共工程，其他领地不变。'
		impact = '德赫林姆的粮食供给和建造力持续提高。'
		intent = 'L0：增加每日粮食并提高每日建造力；S 与 C0 不产生数值变化。'
		duration = 30
		handles = @('- S|source|发布地（帕拉汶）', '- L0|settlement|德赫林姆', '- C0|clan|德伊·梅罗克家族领地')
		selectedModules = @('foodPerDay', 'constructionPerDay', 'prosperityPerDay', 'loyaltyPerDay')
		required = @((New-Requirement 'L0' 'foodPerDay' 'positive' 3), (New-Requirement 'L0' 'constructionPerDay' 'positive' 20))
		forbiddenTargets = @('S', 'C0')
	},
	[pscustomobject]@{
		id = 'local-clan-fiefs-relief'
		scope = 'local'
		name = '梅罗克领地自治减税令'
		content = '只对德伊·梅罗克家族当前全部领地减税并给予自治，以提高当地忠诚；发布地帕拉汶和德赫林姆不适用。'
		digest = '只对指定家族领地减税并提高忠诚。'
		impact = '指定家族领地税收下降、忠诚提高。'
		intent = 'C0：税收收入百分比下降，城镇每日忠诚提高；S 与 L0 不变。'
		duration = 45
		handles = @('- S|source|发布地（帕拉汶）', '- C0|clan|德伊·梅罗克家族领地', '- L0|settlement|德赫林姆')
		selectedModules = @('taxIncomePct', 'loyaltyPerDay', 'prosperityPerDay', 'securityPerDay')
		required = @((New-Requirement 'C0' 'taxIncomePct' 'negative' 5), (New-Requirement 'C0' 'loyaltyPerDay' 'positive' 0.4))
		forbiddenTargets = @('S', 'L0')
	},
	[pscustomobject]@{
		id = 'local-source-crackdown'
		scope = 'local'
		name = '帕拉汶战时重税巡防令'
		content = '仅在发布地帕拉汶征收重税、扩大民兵并加强巡逻；这会改善治安，但持续损害当地忠诚。其他领地不变。'
		digest = '发布地重税并扩大民兵巡逻，以忠诚下降换取治安。'
		impact = '发布地税收、民兵和治安提高，忠诚下降。'
		intent = 'S：税收收入百分比提高、每日民兵增加、每日治安提高、每日忠诚下降；C0 与 L0 不变。'
		duration = 20
		handles = @('- S|source|发布地（帕拉汶）', '- C0|clan|德伊·梅罗克家族领地', '- L0|settlement|德赫林姆')
		selectedModules = @('taxIncomePct', 'militiaPerDay', 'securityPerDay', 'loyaltyPerDay', 'prosperityPerDay')
		required = @((New-Requirement 'S' 'taxIncomePct' 'positive' 15), (New-Requirement 'S' 'militiaPerDay' 'positive' 1.5), (New-Requirement 'S' 'securityPerDay' 'positive' 0.3), (New-Requirement 'S' 'loyaltyPerDay' 'negative' 0.4))
		forbiddenTargets = @('C0', 'L0')
	},
	[pscustomobject]@{
		id = 'kingdom-foreign-aid'
		scope = 'kingdom'
		name = '阿塞莱粮建援助'
		content = '玩家王国出资援助阿塞莱，帮助其各城镇扩充粮仓并开展公共工程；只改变阿塞莱，不改变玩家王国和瓦兰迪亚。'
		digest = '援助阿塞莱粮食与公共工程，其他王国不变。'
		impact = '阿塞莱的粮食和建造力持续提高。'
		intent = 'K1：每日粮食增加、每日建造力提高；K0 与 K2 不产生数值变化。'
		duration = 30
		handles = @('- K0|kingdom|玩家王国', '- K1|kingdom|阿塞莱', '- K2|kingdom|瓦兰迪亚')
		selectedModules = @('foodPerDay', 'constructionPerDay', 'prosperityPerDay', 'loyaltyPerDay')
		required = @((New-Requirement 'K1' 'foodPerDay' 'positive' 3), (New-Requirement 'K1' 'constructionPerDay' 'positive' 20))
		forbiddenTargets = @('K0', 'K2')
	},
	[pscustomobject]@{
		id = 'kingdom-foreign-sanctions'
		scope = 'kingdom'
		name = '库赛特贸易粮运封锁'
		content = '封锁库赛特贸易并截断其粮运，使库赛特税收、粮食与繁荣持续下降；玩家王国和阿塞莱不变。'
		digest = '只削弱库赛特的税收、粮食与繁荣。'
		impact = '库赛特税收、粮食和繁荣下降。'
		intent = 'K1：税收收入百分比下降、每日粮食减少、每日繁荣下降；K0 与 K2 不变。'
		duration = 25
		handles = @('- K0|kingdom|玩家王国', '- K1|kingdom|库赛特', '- K2|kingdom|阿塞莱')
		selectedModules = @('taxIncomePct', 'foodPerDay', 'prosperityPerDay', 'securityPerDay')
		required = @((New-Requirement 'K1' 'taxIncomePct' 'negative' 5), (New-Requirement 'K1' 'foodPerDay' 'negative' 3), (New-Requirement 'K1' 'prosperityPerDay' 'negative' 2))
		forbiddenTargets = @('K0', 'K2')
	},
	[pscustomobject]@{
		id = 'kingdom-bilateral-development'
		scope = 'kingdom'
		name = '瓦兰迪亚共同市场'
		content = '玩家王国与瓦兰迪亚建立共同市场和商路，两国各自的城镇繁荣与税收收入都持续提高；库赛特不受影响。'
		digest = '玩家王国与瓦兰迪亚共同提高繁荣和税收。'
		impact = '双方繁荣和税收持续提高。'
		intent = 'K0 与 K1：每日繁荣提高、税收收入百分比提高；K2 不变。'
		duration = 60
		handles = @('- K0|kingdom|玩家王国', '- K1|kingdom|瓦兰迪亚', '- K2|kingdom|库赛特')
		selectedModules = @('prosperityPerDay', 'taxIncomePct', 'constructionPerDay', 'foodPerDay')
		required = @((New-Requirement 'K0' 'prosperityPerDay' 'positive' 2), (New-Requirement 'K0' 'taxIncomePct' 'positive' 5), (New-Requirement 'K1' 'prosperityPerDay' 'positive' 2), (New-Requirement 'K1' 'taxIncomePct' 'positive' 5))
		forbiddenTargets = @('K2')
	},
	[pscustomobject]@{
		id = 'kingdom-foreign-tax-transfer'
		scope = 'kingdom'
		name = '巴旦尼亚贡税转移'
		content = '巴旦尼亚将十五个百分点的领地税收转交玩家王国：巴旦尼亚税收下降，玩家王国税收等额提高；西帝国和其他数值不变。'
		digest = '巴旦尼亚向玩家王国转移税收。'
		impact = '巴旦尼亚税收下降，玩家王国税收提高。'
		intent = 'K1：税收收入百分比下降；K0：税收收入百分比提高；K2 与其他模块不变。'
		duration = 40
		handles = @('- K0|kingdom|玩家王国', '- K1|kingdom|巴旦尼亚', '- K2|kingdom|西帝国')
		selectedModules = @('taxIncomePct', 'prosperityPerDay', 'loyaltyPerDay', 'kingdomStabilityOnce')
		required = @((New-Requirement 'K1' 'taxIncomePct' 'negative' 15), (New-Requirement 'K0' 'taxIncomePct' 'positive' 15))
		forbiddenTargets = @('K2')
	},
	[pscustomobject]@{
		id = 'kingdom-million-development'
		scope = 'kingdom'
		name = '百万全国发展计划'
		content = '投入一百万第纳尔实施全国重大建设：持续组织工匠、劳力、建材和工程运输修筑公共设施，同时采购并运输粮食、补贴工商业，使玩家王国的建造力、粮食和繁荣显著提高；瓦兰迪亚不受影响。'
		digest = '投入一百万实施全国公共工程、粮食采购运输和工商业扶持。'
		impact = '玩家王国的建造力、粮食和繁荣按全国重大投入档提高。'
		intent = 'K0：一百万第纳尔、全国重大档；每日建造力、每日粮食和每日繁荣显著提高，必须按重大投入尺度定标；K1 不变。'
		duration = 60
		handles = @('- K0|kingdom|玩家王国', '- K1|kingdom|瓦兰迪亚')
		selectedModules = @('constructionPerDay', 'foodPerDay', 'prosperityPerDay', 'loyaltyPerDay')
		required = @((New-Requirement 'K0' 'constructionPerDay' 'positive' 300), (New-Requirement 'K0' 'foodPerDay' 'positive' 10), (New-Requirement 'K0' 'prosperityPerDay' 'positive' 10))
		forbiddenTargets = @('K1')
	},
	[pscustomobject]@{
		id = 'vassal-tax-transfer'
		scope = 'vassal'
		name = '附庸贡税令'
		content = '附庸国将十个百分点税收交给宗主国：附庸国税收下降，宗主国税收提高；阿塞莱及其他数值不变。'
		digest = '附庸国向宗主国转移税收。'
		impact = '附庸国税收下降，宗主国税收提高。'
		intent = 'K0：税收收入百分比下降；K1：税收收入百分比提高；K2 与其他模块不变。'
		duration = 35
		handles = @('- K0|kingdom|附庸国', '- K1|kingdom|宗主国', '- K2|kingdom|阿塞莱')
		selectedModules = @('taxIncomePct', 'prosperityPerDay', 'loyaltyPerDay', 'kingdomStabilityOnce')
		required = @((New-Requirement 'K0' 'taxIncomePct' 'negative' 10), (New-Requirement 'K1' 'taxIncomePct' 'positive' 10))
		forbiddenTargets = @('K2')
	}
)

function Get-ScopeRule([string]$scope) {
	if ($scope -eq 'local') { return '只能引用 S/L*/C*/R* 合法句柄；地方政策禁止王国稳定度效果。' }
	if ($scope -eq 'vassal') { return '只能引用 K* 合法句柄；K0 是附庸国，K1 是宗主国。只有宗主国直接承担或获得数值变化时才引用 K1。' }
	return '只能引用 K* 合法句柄；K0 是玩家王国，其他 K* 仅在政策确实直接改变对应外国时引用。'
}

function Build-Catalog([string]$scope) {
	$lines = foreach ($entry in $moduleCatalog.GetEnumerator()) {
		if ($scope -eq 'local' -and $entry.Key -eq 'kingdomStabilityOnce') { continue }
		'- {0}: {1}' -f $entry.Key, $entry.Value
	}
	return $lines -join [Environment]::NewLine
}

function Test-Sign([double]$value, [string]$sign) {
	if ($sign -eq 'positive') { return $value -gt 0 }
	if ($sign -eq 'negative') { return $value -lt 0 }
	return $true
}

$stablePrefix = '你是三段式玩家政策链路的最终效果后处理阶段。只输出一个 json 对象，不要 Markdown，不要解释。ONNX 选中模块会提供详细量纲规则；极短能力目录中的其他已注册模块只在主评议意图明确需要时使用。不得改变政策方向、作用域或权威期限。极短字段结构：impactSummary:string；durationDays:positive_integer；effects:[{targets:[handle],changes:{moduleId:finite_number},reason:string}]。解析或校验失败时系统不会重试。'
$commonCalibration = '除 kingdomStabilityOnce 外，所有模块值都是目标对象每天实际结算的变化，不是整个周期总量；持续时间用于判断措施能否维持，不得把直接成立的每日强度机械摊薄成象征性数值。政策正文、numericIntent 或玩家 MCM 评判偏好里明确的金额、倍率、档位、范围和强弱必须用于定标，但不要把金额机械线性换算成某一数值。同一执行方案直接产生的多项效果可以同时输出，不限制一至三项；巨额财政投入本身已是代价，不得为了平衡臆造负面效果。普通政策按模块常规档，重大改革、全国行动、巨额投入或极端破坏按模块重大/极端档；不要无依据把所有变化默认成 1 或 5。'

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
		$catalog = Build-Catalog $case.scope
		$selectedRules = ($case.selectedModules | ForEach-Object { '- {0}：{1}' -f $_, $moduleRules[$_] }) -join [Environment]::NewLine
		$stableSystem = @(
			$stablePrefix
			'【通用强度定标】'
			$commonCalibration
			'【当前作用域的效果能力极短目录】'
			$catalog
			'目录项不是效果要求；只有政策语义明确需要时才输出对应模块。'
		) -join [Environment]::NewLine
		$dynamicSystem = @(
			'【ONNX 优先模块的后处理详细规则】'
			$selectedRules
			''
			'【本次作用域规则】'
			(Get-ScopeRule $case.scope)
		) -join [Environment]::NewLine
		$assessment = [ordered]@{
			impactSummary = $case.impact
			numericIntent = $case.intent
			durationDays = $case.duration
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
			($case.handles -join [Environment]::NewLine)
			''
			'【硬规则】'
			''
			('durationDays 必须严格等于 ' + $case.duration + '。')
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
				$results.Add([pscustomobject]@{ case_id = $case.id; scope = $case.scope; success = $false; expectation_pass = $false; http_status = [int]$response.StatusCode; elapsed_ms = [math]::Round($stopwatch.Elapsed.TotalMilliseconds, 1); error = 'HTTP request failed' })
				continue
			}
			$outer = $outerText | ConvertFrom-Json
			$assistantText = [string]$outer.choices[0].message.content
			$parsed = $null
			$parseError = ''
			try { $parsed = $assistantText | ConvertFrom-Json } catch { $parseError = $_.Exception.Message }
			$pairs = [System.Collections.Generic.List[object]]::new()
			$validationErrors = [System.Collections.Generic.List[string]]::new()
			$seen = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
			$allowedHandles = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
			foreach ($handleLine in $case.handles) {
				$handle = (($handleLine -replace '^\s*-\s*', '') -split '\|')[0].Trim()
				$null = $allowedHandles.Add($handle)
			}
			if ($null -eq $parsed) {
				$validationErrors.Add('json_parse_failed')
			}
			else {
				if ([int]$parsed.durationDays -ne [int]$case.duration) { $validationErrors.Add('duration_mismatch') }
				foreach ($effect in @($parsed.effects)) {
					foreach ($target in @($effect.targets)) {
						if (-not $allowedHandles.Contains([string]$target)) { $validationErrors.Add('unknown_target:' + $target) }
						foreach ($property in $effect.changes.PSObject.Properties) {
							$module = [string]$property.Name
							$value = [double]$property.Value
							$key = ([string]$target) + '|' + $module
							if (-not $knownModules.Contains($module)) { $validationErrors.Add('unknown_module:' + $module) }
							if ([double]::IsNaN($value) -or [double]::IsInfinity($value)) { $validationErrors.Add('non_finite:' + $key) }
							if ($module -eq 'kingdomStabilityOnce' -and [Math]::Abs($value - [Math]::Round($value, [MidpointRounding]::AwayFromZero)) -gt 0.000001) { $validationErrors.Add('stability_non_integer:' + $target) }
							if (-not $seen.Add($key)) { $validationErrors.Add('duplicate:' + $key) }
							if ($case.scope -eq 'local' -and $module -eq 'kingdomStabilityOnce') { $validationErrors.Add('local_stability_forbidden') }
							$pairs.Add([pscustomobject]@{ target = [string]$target; module = $module; value = $value })
						}
					}
				}
			}
			$missing = [System.Collections.Generic.List[string]]::new()
			foreach ($required in $case.required) {
				$match = @($pairs | Where-Object { $_.target -eq $required.target -and $_.module -eq $required.module -and (Test-Sign $_.value $required.sign) -and [math]::Abs([double]$_.value) -ge [double]$required.minimum_abs })
				if ($match.Count -eq 0) { $missing.Add($required.target + '|' + $required.module + '|' + $required.sign + '|minAbs=' + $required.minimum_abs) }
			}
			$forbiddenHits = @($pairs | Where-Object { $case.forbiddenTargets -contains $_.target } | ForEach-Object { $_.target + '|' + $_.module })
			$expectationPass = $null -ne $parsed -and $validationErrors.Count -eq 0 -and $missing.Count -eq 0 -and $forbiddenHits.Count -eq 0
			$results.Add([pscustomobject]@{
				case_id = $case.id
				scope = $case.scope
				success = ($null -ne $parsed)
				expectation_pass = $expectationPass
				http_status = [int]$response.StatusCode
				elapsed_ms = [math]::Round($stopwatch.Elapsed.TotalMilliseconds, 1)
				finish_reason = [string]$outer.choices[0].finish_reason
				prompt_tokens = [int]$outer.usage.prompt_tokens
				completion_tokens = [int]$outer.usage.completion_tokens
				cache_hit_tokens = [int]$outer.usage.prompt_cache_hit_tokens
				cache_miss_tokens = [int]$outer.usage.prompt_cache_miss_tokens
				parse_error = $parseError
				selected_modules = @($case.selectedModules)
				pairs = @($pairs)
				missing_required = @($missing)
				forbidden_target_hits = @($forbiddenHits)
				validation_errors = @($validationErrors)
			})
		}
		catch {
			$stopwatch.Stop()
			$results.Add([pscustomobject]@{ case_id = $case.id; scope = $case.scope; success = $false; expectation_pass = $false; elapsed_ms = [math]::Round($stopwatch.Elapsed.TotalMilliseconds, 1); error = $_.Exception.Message })
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
	test = 'player_policy_effect_scope_matrix_calibrated_5000'
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
	results = @($results)
}
$reportPath = Join-Path (Get-Location) 'Phase0_Local_Archive\reports\player_policy_effect_scope_matrix_calibrated_5000_20260808.json'
$report | ConvertTo-Json -Depth 20 | Set-Content -LiteralPath $reportPath -Encoding utf8NoBOM
$results | Select-Object case_id, scope, success, expectation_pass, prompt_tokens, completion_tokens, @{ Name = 'pairs'; Expression = { ($_.pairs | ForEach-Object { $_.target + ':' + $_.module + '=' + $_.value }) -join ',' } } | Format-Table -AutoSize

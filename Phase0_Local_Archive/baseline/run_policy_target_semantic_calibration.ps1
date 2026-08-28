[CmdletBinding()]
param(
	[string]$ProjectRoot = "",
	[string]$StageModuleRoot = "",
	[string]$ReferenceDir = "",
	[string]$GameRoot = "",
	[string]$ImplementationAssembly = "",
	[string]$ReportPath = "",
	[ValidateSet("1.3", "1.4")]
	[string]$ImplementationVersion = "1.4",
	[switch]$EntityPrecision,
	[switch]$SyntaxOnly
)

$ErrorActionPreference = "Stop"
if ($SyntaxOnly) {
	Write-Output "SYNTAX_OK"
	return
}

function Get-FullPath {
	param([Parameter(Mandatory = $true)][string]$Path)
	return [System.IO.Path]::GetFullPath($Path)
}

function Assert-File {
	param([Parameter(Mandatory = $true)][string]$Path)
	if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
		throw "Required file not found: $Path"
	}
}

function Add-Score {
	param(
		[Parameter(Mandatory = $true)]$Map,
		[Parameter(Mandatory = $true)][string]$Id,
		[Parameter(Mandatory = $true)][double]$Score
	)
	if (-not $Map.ContainsKey($Id)) {
		$Map[$Id] = [System.Collections.Generic.List[double]]::new()
	}
	$Map[$Id].Add($Score)
}

function Add-LocalizationFile {
	param(
		[Parameter(Mandatory = $true)]$Map,
		[Parameter(Mandatory = $true)][string]$Path
	)
	Assert-File -Path $Path
	[xml]$document = Get-Content -LiteralPath $Path -Raw
	foreach ($entry in @($document.base.strings.string)) {
		$Map[[string]$entry.id] = [string]$entry.text
	}
}

function Resolve-LocalizedText {
	param(
		[Parameter(Mandatory = $true)][string]$Raw,
		[Parameter(Mandatory = $true)]$Map
	)
	$match = [regex]::Match($Raw, '^\{=([^}]+)\}(.*)$')
	if ($match.Success -and $Map.ContainsKey($match.Groups[1].Value)) { return [string]$Map[$match.Groups[1].Value] }
	if ($match.Success) { return [string]$match.Groups[2].Value }
	return $Raw
}

function Get-FuzzyEntityName {
	param([Parameter(Mandatory = $true)][string]$Name)
	$core = $Name.Trim()
	$separator = $core.LastIndexOf('·')
	if ($separator -ge 0 -and $separator + 1 -lt $core.Length) { $core = $core.Substring($separator + 1) }
	if ($core.Length -lt 3) { return '' }
	$removeAt = [math]::Floor($core.Length / 2)
	return $core.Remove($removeAt, 1)
}

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
	$ProjectRoot = Join-Path $PSScriptRoot "..\.."
}
$projectRootFull = Get-FullPath -Path $ProjectRoot
if ([string]::IsNullOrWhiteSpace($StageModuleRoot)) {
	$StageModuleRoot = Join-Path $projectRootFull "bin\Debug\single_module_stage\AnimusForge"
}
$stageModuleRootFull = Get-FullPath -Path $StageModuleRoot
if ([string]::IsNullOrWhiteSpace($ReferenceDir)) {
	$ReferenceDir = Join-Path $projectRootFull (".tmp\build_check\" + $ImplementationVersion)
}
$referenceDirFull = Get-FullPath -Path $ReferenceDir

$stageBin = Join-Path $stageModuleRootFull "bin\Win64_Shipping_Client"
$stageImplementation = if ([string]::IsNullOrWhiteSpace($ImplementationAssembly)) {
	Join-Path $stageBin ("versions\" + $ImplementationVersion + "\AnimusForge.dll")
} else {
	Get-FullPath -Path $ImplementationAssembly
}
$stageManagedOnnx = Join-Path $stageBin "Microsoft.ML.OnnxRuntime.dll"
$stageNativeOnnx = Join-Path $stageBin "onnxruntime.dll"
$stageNativeProviders = Join-Path $stageBin "onnxruntime_providers_shared.dll"
$embeddingModel = Join-Path $stageModuleRootFull "ONNX\model.onnx"
$embeddingModelData = Join-Path $stageModuleRootFull "ONNX\model.onnx_data"
$embeddingTokenizer = Join-Path $stageModuleRootFull "ONNX\tokenizer.json"
$embeddingConfig = Join-Path $stageModuleRootFull "ONNX\config.json"
$casesPath = Join-Path $PSScriptRoot "cases\policy_target_semantic_calibration.jsonl"
$requiredFiles = [System.Collections.Generic.List[string]]::new()
foreach ($required in @(
	$stageImplementation,
	$stageManagedOnnx,
	$stageNativeOnnx,
	$stageNativeProviders,
	$embeddingModel,
	$embeddingTokenizer
)) {
	$requiredFiles.Add($required)
}
if ($EntityPrecision) {
	if ([string]::IsNullOrWhiteSpace($GameRoot)) { throw "-GameRoot is required with -EntityPrecision." }
	$gameRootFull = Get-FullPath -Path $GameRoot
	$moduleDataRoot = Join-Path $gameRootFull "Modules\SandBox\ModuleData"
	foreach ($required in @(
		(Join-Path $moduleDataRoot "spclans.xml"),
		(Join-Path $moduleDataRoot "lords.xml"),
		(Join-Path $moduleDataRoot "settlements.xml"),
		(Join-Path $moduleDataRoot "Languages\CNs\std_spclans_xml-zho-CN.xml"),
		(Join-Path $moduleDataRoot "Languages\CNs\std_lords_xml-zho-CN.xml"),
		(Join-Path $gameRootFull "Modules\Native\ModuleData\Languages\CNs\std_common_strings_xml-zho-CN.xml")
	)) { $requiredFiles.Add($required) }
}
else {
	$requiredFiles.Add($casesPath)
}
foreach ($required in $requiredFiles) {
	Assert-File -Path $required
}

$runId = [DateTime]::UtcNow.ToString("yyyyMMdd_HHmmss_fff") + "_" + $PID.ToString([Globalization.CultureInfo]::InvariantCulture)
$runtimeRoot = Join-Path $projectRootFull ("bin\Debug\policy_target_calibration_runtime\" + $runId + "\AnimusForge")
$runtimeBin = Join-Path $runtimeRoot "bin\Win64_Shipping_Client"
$runtimeImplementationDir = Join-Path $runtimeBin ("versions\" + $ImplementationVersion)
$runtimeOnnx = Join-Path $runtimeRoot "ONNX"
[void](New-Item -ItemType Directory -Path $runtimeImplementationDir -Force)
[void](New-Item -ItemType Directory -Path $runtimeOnnx -Force)
[void](New-Item -ItemType Directory -Path (Join-Path $runtimeRoot "ModuleData") -Force)
[void](New-Item -ItemType HardLink -Path (Join-Path $runtimeRoot "SubModule.xml") -Target (Join-Path $stageModuleRootFull "SubModule.xml"))
[void](New-Item -ItemType HardLink -Path (Join-Path $runtimeImplementationDir "AnimusForge.dll") -Target $stageImplementation)
[void](New-Item -ItemType HardLink -Path (Join-Path $runtimeBin "Microsoft.ML.OnnxRuntime.dll") -Target $stageManagedOnnx)
[void](New-Item -ItemType HardLink -Path (Join-Path $runtimeBin "onnxruntime.dll") -Target $stageNativeOnnx)
[void](New-Item -ItemType HardLink -Path (Join-Path $runtimeBin "onnxruntime_providers_shared.dll") -Target $stageNativeProviders)
[void](New-Item -ItemType HardLink -Path (Join-Path $runtimeOnnx "model.onnx") -Target $embeddingModel)
if (Test-Path -LiteralPath $embeddingModelData -PathType Leaf) {
	[void](New-Item -ItemType HardLink -Path (Join-Path $runtimeOnnx "model.onnx_data") -Target $embeddingModelData)
}
[void](New-Item -ItemType HardLink -Path (Join-Path $runtimeOnnx "tokenizer.json") -Target $embeddingTokenizer)
if (Test-Path -LiteralPath $embeddingConfig -PathType Leaf) {
	[void](New-Item -ItemType HardLink -Path (Join-Path $runtimeOnnx "config.json") -Target $embeddingConfig)
}

Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;

public static class PolicyTargetCalibrationNativeLoader
{
    [System.Runtime.InteropServices.DllImport("kernel32", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    public static extern IntPtr LoadLibrary(string path);
}

public delegate bool PolicyTargetCalibrationEmbeddingCall(string text, out float[] vector);

public static class PolicyTargetCalibrationRuntime
{
    public static float[] Embed(PolicyTargetCalibrationEmbeddingCall call, string text)
    {
        float[] vector;
        if (!call(text, out vector) || vector == null || vector.Length == 0)
            throw new InvalidOperationException("Embedding failed.");
        return vector;
    }

public static int[] DenseTopK(PolicyTargetCalibrationEmbeddingCall call, float[] query, string[] documents, int topK)
    {
        int count = Math.Min(Math.Max(0, topK), documents.Length);
        float[] bestScores = new float[count];
        int[] bestIndices = new int[count];
        for (int index = 0; index < count; index++) { bestScores[index] = float.NegativeInfinity; bestIndices[index] = -1; }
        for (int index = 0; index < documents.Length; index++)
        {
            float[] vector = Embed(call, documents[index]);
            float score = Cosine(query, vector);
            int insert = count;
            for (int rank = 0; rank < count; rank++) if (score > bestScores[rank]) { insert = rank; break; }
            if (insert >= count) continue;
            for (int rank = count - 1; rank > insert; rank--) { bestScores[rank] = bestScores[rank - 1]; bestIndices[rank] = bestIndices[rank - 1]; }
            bestScores[insert] = score;
            bestIndices[insert] = index;
        }
        return bestIndices;
    }

    public static int[] DenseTopKFromVectors(float[] query, IReadOnlyList<float[]> vectors, int topK)
    {
        int count = Math.Min(Math.Max(0, topK), vectors.Count);
        float[] bestScores = new float[count];
        int[] bestIndices = new int[count];
        for (int index = 0; index < count; index++) { bestScores[index] = float.NegativeInfinity; bestIndices[index] = -1; }
        for (int index = 0; index < vectors.Count; index++)
        {
            float score = Cosine(query, vectors[index]);
            int insert = count;
            for (int rank = 0; rank < count; rank++) if (score > bestScores[rank]) { insert = rank; break; }
            if (insert >= count) continue;
            for (int rank = count - 1; rank > insert; rank--) { bestScores[rank] = bestScores[rank - 1]; bestIndices[rank] = bestIndices[rank - 1]; }
            bestScores[insert] = score;
            bestIndices[insert] = index;
        }
        return bestIndices;
    }

    public static List<float> DenseScores(PolicyTargetCalibrationEmbeddingCall call, float[] query, string[] documents)
    {
        List<float> scores = new List<float>(documents.Length);
        for (int index = 0; index < documents.Length; index++)
            scores.Add(Cosine(query, Embed(call, documents[index])));
        return scores;
    }

    public static List<float> DenseScoresFromVectors(float[] query, IReadOnlyList<float[]> vectors)
    {
        List<float> scores = new List<float>(vectors.Count);
        for (int index = 0; index < vectors.Count; index++)
            scores.Add(Cosine(query, vectors[index]));
        return scores;
    }

    private static float Cosine(float[] left, float[] right)
    {
        if (left == null || right == null || left.Length == 0 || left.Length != right.Length) return float.NegativeInfinity;
        double dot = 0.0, leftNorm = 0.0, rightNorm = 0.0;
        for (int index = 0; index < left.Length; index++)
        {
            dot += (double)left[index] * right[index];
            leftNorm += (double)left[index] * left[index];
            rightNorm += (double)right[index] * right[index];
        }
        return leftNorm <= 0.0 || rightNorm <= 0.0 ? float.NegativeInfinity : (float)(dot / Math.Sqrt(leftNorm * rightNorm));
    }
}
'@

function Invoke-EntityPrecisionCalibration {
	param(
		[Parameter(Mandatory = $true)]$EmbeddingCall,
		[Parameter(Mandatory = $true)][string]$ModuleDataRoot,
		[Parameter(Mandatory = $true)][string]$GameRootFull,
		[Parameter(Mandatory = $true)][string]$ProjectRootFull,
		[Parameter(Mandatory = $true)][string]$EmbeddingModel,
		[Parameter(Mandatory = $true)][string]$ImplementationVersion,
		[Parameter(Mandatory = $true)]$RouterType
	)
	$localization = @{}
	Add-LocalizationFile -Map $localization -Path (Join-Path $ModuleDataRoot "Languages\CNs\std_spclans_xml-zho-CN.xml")
	Add-LocalizationFile -Map $localization -Path (Join-Path $ModuleDataRoot "Languages\CNs\std_lords_xml-zho-CN.xml")
	Add-LocalizationFile -Map $localization -Path (Join-Path $GameRootFull "Modules\Native\ModuleData\Languages\CNs\std_common_strings_xml-zho-CN.xml")

	[xml]$clanXml = Get-Content -LiteralPath (Join-Path $ModuleDataRoot "spclans.xml") -Raw
	[xml]$lordXml = Get-Content -LiteralPath (Join-Path $ModuleDataRoot "lords.xml") -Raw
	[xml]$settlementXml = Get-Content -LiteralPath (Join-Path $ModuleDataRoot "settlements.xml") -Raw
	$heroNames = @{}
	foreach ($hero in @($lordXml.NPCCharacters.NPCCharacter)) {
		$id = [string]$hero.id
		if (-not [string]::IsNullOrWhiteSpace($id)) { $heroNames[$id] = Resolve-LocalizedText -Raw ([string]$hero.name) -Map $localization }
	}

	$clans = [System.Collections.Generic.List[object]]::new()
	$clansById = @{}
	foreach ($clan in @($clanXml.Factions.Faction)) {
		if ([string]$clan.is_noble -ne "true" -or [string]::IsNullOrWhiteSpace([string]$clan.super_faction)) { continue }
		$id = [string]$clan.id
		$name = Resolve-LocalizedText -Raw ([string]$clan.name) -Map $localization
		$kingdom = ([string]$clan.super_faction).Replace("Kingdom.", "")
		$leaderId = ([string]$clan.owner).Replace("Hero.", "")
		$leaderName = if ($heroNames.ContainsKey($leaderId)) { [string]$heroNames[$leaderId] } else { $leaderId }
		$alias = $name
		$separator = $alias.LastIndexOf('·')
		if ($separator -ge 0 -and $separator + 1 -lt $alias.Length) { $alias = $alias.Substring($separator + 1) }
		$item = [pscustomobject]@{
			id = $id
			name = $name
			alias = $alias
			kingdom = $kingdom
			leader_id = $leaderId
			leader_name = $leaderName
			culture = ([string]$clan.culture).Replace("Culture.", "")
		}
		$clans.Add($item)
		$clansById[$id] = $item
	}

	$entities = [System.Collections.Generic.List[object]]::new()
	foreach ($clan in $clans) {
		$entities.Add([pscustomobject]@{
			doc_id = "clan:" + $clan.id
			kind = "clan"
			entity_id = $clan.id
			owner_clan_id = $clan.id
			kingdom = $clan.kingdom
			name = $clan.name
			mention_aliases = [string[]]@($clan.name, $clan.alias, $clan.id)
			text = "家族 氏族 贵族 " + $clan.name + " 别名 " + $clan.alias + " ID " + $clan.id + " 领袖 " + $clan.leader_name + " 所属国家 " + $clan.kingdom + " 文化 " + $clan.culture
		})
		if (-not [string]::IsNullOrWhiteSpace($clan.leader_id) -and -not [string]::IsNullOrWhiteSpace($clan.leader_name)) {
			$entities.Add([pscustomobject]@{
				doc_id = "ruler:" + $clan.leader_id
				kind = "ruler"
				entity_id = $clan.id
				owner_clan_id = $clan.id
				kingdom = $clan.kingdom
				name = $clan.leader_name
				mention_aliases = [string[]]@($clan.leader_name, $clan.leader_id)
				text = "领袖 统治者 贵族 " + $clan.leader_name + " ID " + $clan.leader_id + " 所属氏族 " + $clan.name + " 所属国家 " + $clan.kingdom
			})
		}
	}
	$settlementNodes = @($settlementXml.Settlements.Settlement)
	$settlementOwnerById = @{}
	foreach ($settlement in $settlementNodes) {
		$directOwnerClanId = ([string]$settlement.owner).Replace("Faction.", "")
		if ($clansById.ContainsKey($directOwnerClanId)) {
			$settlementOwnerById[[string]$settlement.id] = $directOwnerClanId
		}
	}
	foreach ($settlement in $settlementNodes) {
		$type = if ($null -ne $settlement.Components.Town) {
			if ([string]$settlement.Components.Town.is_castle -eq "true") { "castle" } else { "city" }
		} else { "" }
		if ([string]::IsNullOrWhiteSpace($type)) { continue }
		$ownerClanId = ([string]$settlement.owner).Replace("Faction.", "")
		if (-not $clansById.ContainsKey($ownerClanId)) { continue }
		$name = Resolve-LocalizedText -Raw ([string]$settlement.name) -Map $localization
		$alias = $name
		$separator = [math]::Max($alias.LastIndexOf('·'), $alias.LastIndexOf('.'))
		if ($separator -ge 0 -and $separator + 1 -lt $alias.Length) { $alias = $alias.Substring($separator + 1) }
		$typeText = if ($type -eq "castle") { "城堡" } else { "城市" }
		$ownerClan = $clansById[$ownerClanId]
		$entities.Add([pscustomobject]@{
			doc_id = "settlement:" + [string]$settlement.id
			kind = "settlement"
			entity_id = [string]$settlement.id
			owner_clan_id = $ownerClanId
			kingdom = $ownerClan.kingdom
			name = $name
			settlement_type = $type
			mention_aliases = [string[]]@($name, $alias, [string]$settlement.id)
			text = "定居点 " + $typeText + " 领地 " + $name + " ID " + [string]$settlement.id + " 所属氏族 " + $ownerClan.name + " 所属国家 " + $ownerClan.kingdom + " 文化 " + $ownerClan.culture
		})
	}
	$villageSourceCount = @($settlementNodes | Where-Object { $null -ne $_.Components.Village }).Count
	$villageEntityCount = @($entities | Where-Object { $_.kind -eq "settlement" -and $_.settlement_type -eq "village" }).Count
	$primaryFiefEntityCount = @($entities | Where-Object kind -eq "settlement").Count
	$runtimeEntityCountEstimate = $entities.Count + @($clans.kingdom | Sort-Object -Unique).Count
	$primaryFiefIds = @{}
	foreach ($settlement in $settlementNodes) {
		if ($null -ne $settlement.Components.Town) { $primaryFiefIds[[string]$settlement.id] = $true }
	}
	$validVillageBoundCount = 0
	$invalidVillageBoundCount = 0
	$villageCountsByParent = @{}
	foreach ($village in @($settlementNodes | Where-Object { $null -ne $_.Components.Village })) {
		$boundId = ([string]$village.Components.Village.bound).Replace("Settlement.", "")
		if ($primaryFiefIds.ContainsKey($boundId)) {
			$validVillageBoundCount++
			$villageCountsByParent[$boundId] = 1 + $(if ($villageCountsByParent.ContainsKey($boundId)) { [int]$villageCountsByParent[$boundId] } else { 0 })
		}
		else { $invalidVillageBoundCount++ }
	}
	if ($villageEntityCount -ne 0) { throw "Village entities remain in the embedding calibration index: $villageEntityCount" }
	if ($invalidVillageBoundCount -ne 0) { throw "Villages without a valid town/castle Bound remain: $invalidVillageBoundCount" }
	if ($runtimeEntityCountEstimate -ge 573) { throw "Primary-fief entity estimate did not shrink below the previous 573 entities: $runtimeEntityCountEstimate" }
	$entitySnapshotType = $RouterType.Assembly.GetType("AnimusForge.PolicyTargets.PolicyTargetEntitySnapshot", $true)
	$mentionMethod = $RouterType.GetMethod("PolicyTextApproximatelyMentionsEntity", [Reflection.BindingFlags]"NonPublic,Static")
	$typeCueMethod = $RouterType.GetMethod("EntityMatchesExplicitTypeCue", [Reflection.BindingFlags]"NonPublic,Static")
	$configuredThresholds = $RouterType.GetField("CalibratedEntityThresholds", [Reflection.BindingFlags]"NonPublic,Static").GetValue($null)
	$configuredMinimumGap = [double]$RouterType.GetField("DirectEntityMinimumRecallScoreGap", [Reflection.BindingFlags]"NonPublic,Static").GetRawConstantValue()
	$semanticExpansionEnabled = [bool]$RouterType.GetField("SemanticExpansionEnabled", [Reflection.BindingFlags]"NonPublic,Static").GetRawConstantValue()
	foreach ($entity in $entities) {
		$runtimeEntity = [Activator]::CreateInstance($entitySnapshotType, $true)
		$entitySnapshotType.GetProperty("Kind", [Reflection.BindingFlags]"NonPublic,Instance").SetValue($runtimeEntity, [string]$entity.kind, $null)
		$entitySnapshotType.GetProperty("EntityId", [Reflection.BindingFlags]"NonPublic,Instance").SetValue($runtimeEntity, [string]$entity.entity_id, $null)
		$entitySnapshotType.GetProperty("OwnerClanId", [Reflection.BindingFlags]"NonPublic,Instance").SetValue($runtimeEntity, [string]$entity.owner_clan_id, $null)
		$entitySnapshotType.GetProperty("MentionAliases", [Reflection.BindingFlags]"NonPublic,Instance").SetValue($runtimeEntity, [string[]]$entity.mention_aliases, $null)
		if ($entity.kind -eq "settlement") {
			$entitySnapshotType.GetProperty("IsCity", [Reflection.BindingFlags]"NonPublic,Instance").SetValue($runtimeEntity, ($entity.settlement_type -eq "city"), $null)
			$entitySnapshotType.GetProperty("IsCastle", [Reflection.BindingFlags]"NonPublic,Instance").SetValue($runtimeEntity, ($entity.settlement_type -eq "castle"), $null)
		}
		$entity | Add-Member -NotePropertyName runtime_snapshot -NotePropertyValue $runtimeEntity
	}

	$entitiesByKingdom = @{}
	foreach ($entity in $entities) {
		if (-not $entitiesByKingdom.ContainsKey($entity.kingdom)) { $entitiesByKingdom[$entity.kingdom] = [System.Collections.Generic.List[object]]::new() }
		$entitiesByKingdom[$entity.kingdom].Add($entity)
	}
	$cases = [System.Collections.Generic.List[object]]::new()
	foreach ($kingdom in @($clans.kingdom | Sort-Object -Unique)) {
		$kingdomClans = @($clans | Where-Object kingdom -eq $kingdom | Sort-Object id)
		$selectedClans = @($kingdomClans | Where-Object { -not [string]::IsNullOrWhiteSpace((Get-FuzzyEntityName -Name $_.name)) } | Select-Object -First 2)
		foreach ($clan in $selectedClans) {
			$fuzzy = Get-FuzzyEntityName -Name $clan.name
			$cases.Add([pscustomobject]@{ case_id = "clan_" + $clan.id; kind = "clan"; kingdom = $kingdom; expected_doc_id = "clan:" + $clan.id; query = "免除" + $fuzzy + "家族全部领地的赋税"; source_name = $clan.name; fuzzy_name = $fuzzy })
		}
		$selectedRuler = @($kingdomClans | Where-Object { -not [string]::IsNullOrWhiteSpace((Get-FuzzyEntityName -Name $_.leader_name)) } | Select-Object -First 1)
		foreach ($clan in $selectedRuler) {
			$fuzzy = Get-FuzzyEntityName -Name $clan.leader_name
			$cases.Add([pscustomobject]@{ case_id = "ruler_" + $clan.leader_id; kind = "ruler"; kingdom = $kingdom; expected_doc_id = "ruler:" + $clan.leader_id; query = "由" + $fuzzy + "领主及其家族承担军费"; source_name = $clan.leader_name; fuzzy_name = $fuzzy })
		}
		foreach ($type in @("city", "castle")) {
			$selectedSettlement = @($entities | Where-Object { $_.kingdom -eq $kingdom -and $_.kind -eq "settlement" -and $_.settlement_type -eq $type -and -not [string]::IsNullOrWhiteSpace((Get-FuzzyEntityName -Name $_.name)) } | Sort-Object entity_id | Select-Object -First 1)
			foreach ($entity in $selectedSettlement) {
				$fuzzy = Get-FuzzyEntityName -Name $entity.name
				$query = if ($type -eq "city") { "修缮" + $fuzzy + "的城墙与粮仓" } else { "加强" + $fuzzy + "的驻防" }
				$cases.Add([pscustomobject]@{ case_id = "settlement_" + $entity.entity_id; kind = "settlement"; kingdom = $kingdom; expected_doc_id = $entity.doc_id; query = $query; source_name = $entity.name; fuzzy_name = $fuzzy; settlement_type = $type })
			}
		}
	}
	if ($cases.Count -lt 40) { throw "Insufficient generated entity precision cases: $($cases.Count)" }

	$vectorByDocumentId = @{}
	foreach ($entity in $entities) {
		$vectorByDocumentId[$entity.doc_id] = [PolicyTargetCalibrationRuntime]::Embed($EmbeddingCall, [string]$entity.text)
	}
	$positiveScores = @{}
	$rankGaps = @{}
	$caseResults = [System.Collections.Generic.List[object]]::new()
	foreach ($case in $cases) {
		$candidates = @($entitiesByKingdom[$case.kingdom])
		$candidateVectors = [System.Collections.Generic.List[float[]]]::new()
		foreach ($candidate in $candidates) { $candidateVectors.Add([float[]]$vectorByDocumentId[$candidate.doc_id]) }
		$queryVector = [PolicyTargetCalibrationRuntime]::Embed($EmbeddingCall, [string]$case.query)
		$topIndices = [PolicyTargetCalibrationRuntime]::DenseTopKFromVectors($queryVector, $candidateVectors, 10)
		$denseItems = [System.Collections.Generic.List[object]]::new()
		foreach ($index in $topIndices) { if ($index -ge 0) { $denseItems.Add($candidates[$index]) } }
		$promotedItems = @($candidates | Where-Object {
			[bool]$mentionMethod.Invoke($null, @([string]$case.query, $_.runtime_snapshot)) `
				-and [bool]$typeCueMethod.Invoke($null, @([string]$case.query, $_.runtime_snapshot))
		} | Select-Object -First 4)
		$topItems = [System.Collections.Generic.List[object]]::new()
		foreach ($item in @($promotedItems) + @($denseItems)) {
			if ($topItems.Count -ge 10) { break }
			if (@($topItems | Where-Object doc_id -eq $item.doc_id).Count -eq 0) { $topItems.Add($item) }
		}
		$topVectors = [System.Collections.Generic.List[float[]]]::new()
		foreach ($item in $topItems) { $topVectors.Add([float[]]$vectorByDocumentId[$item.doc_id]) }
		$scores = [PolicyTargetCalibrationRuntime]::DenseScoresFromVectors($queryVector, $topVectors)
		$ranked = [System.Collections.Generic.List[object]]::new()
		for ($index = 0; $index -lt $topItems.Count; $index++) {
			$ranked.Add([pscustomobject]@{ doc_id = [string]$topItems[$index].doc_id; kind = [string]$topItems[$index].kind; name = [string]$topItems[$index].name; score = [double]$scores[$index]; entity = $topItems[$index] })
		}
		$ordered = @($ranked | Sort-Object score -Descending)
		$expected = @($ordered | Where-Object doc_id -eq $case.expected_doc_id | Select-Object -First 1)
		$denseRecalled = @($denseItems | Where-Object doc_id -eq $case.expected_doc_id).Count -eq 1
		$retrievalRecalled = $expected.Count -eq 1
		$approximatePromoted = @($promotedItems | Where-Object doc_id -eq $case.expected_doc_id).Count -eq 1
		$rawTop1 = $retrievalRecalled -and $ordered.Count -gt 0 -and $ordered[0].doc_id -eq $case.expected_doc_id
		$competitionCategory = if ($case.kind -eq "clan" -or $case.kind -eq "ruler") { "family" } else { $case.kind }
		$guarded = @($ordered | Where-Object {
			$kindCategory = if ($_.kind -eq "clan" -or $_.kind -eq "ruler") { "family" } else { $_.kind }
			$kindCategory -eq $competitionCategory `
				-and [bool]$mentionMethod.Invoke($null, @([string]$case.query, $_.entity.runtime_snapshot)) `
				-and [bool]$typeCueMethod.Invoke($null, @([string]$case.query, $_.entity.runtime_snapshot))
		})
		$mechanical = @($guarded | Group-Object { if ($_.kind -eq "clan" -or $_.kind -eq "ruler") { "family:" + $_.entity.owner_clan_id } else { $_.kind + ":" + $_.entity.entity_id } } | ForEach-Object { $_.Group | Sort-Object score -Descending | Select-Object -First 1 } | Sort-Object score -Descending)
		$top1 = $retrievalRecalled -and $mechanical.Count -gt 0 -and $mechanical[0].doc_id -eq $case.expected_doc_id
		$gapScore = [double]::NaN
		if ($top1) {
			Add-Score -Map $positiveScores -Id $case.kind -Score ([double]$expected[0].score)
			if ($mechanical.Count -gt 1) { $gapScore = [double]$mechanical[0].score - [double]$mechanical[1].score } else { $gapScore = [double]::PositiveInfinity }
			Add-Score -Map $rankGaps -Id $case.kind -Score $gapScore
		}
		$caseResults.Add([pscustomobject]@{
			case_id = $case.case_id
			kind = $case.kind
			kingdom = $case.kingdom
			query = $case.query
			source_name = $case.source_name
			fuzzy_name = $case.fuzzy_name
			expected_doc_id = $case.expected_doc_id
			dense_recalled = $denseRecalled
			approximate_promoted = $approximatePromoted
			retrieval_recalled = $retrievalRecalled
			raw_top1 = $rawTop1
			guarded_candidate_count = $mechanical.Count
			selected_doc_id = $(if ($mechanical.Count -gt 0) { [string]$mechanical[0].doc_id } else { "" })
			top1 = $top1
			gap_score = $(if ([double]::IsNaN($gapScore)) { $null } elseif ([double]::IsPositiveInfinity($gapScore)) { 999.0 } else { [math]::Round($gapScore, 6) })
			ranked = @($ordered | Select-Object -First 5 | ForEach-Object { [ordered]@{ doc_id = $_.doc_id; kind = $_.kind; name = $_.name; score = [math]::Round($_.score, 6) } })
		})
	}

	$negativeScores = @{}
	$negativeQueries = @(
		"向本国最富有家族的领地征税",
		"增援边境粮仓城市并提高粮食储备",
		"发展本国经济并改善人民生活",
		"召集各家族商议新的全国税制"
	)
	$negativeResults = [System.Collections.Generic.List[object]]::new()
	foreach ($kingdom in @($entitiesByKingdom.Keys | Sort-Object)) {
		$candidates = @($entitiesByKingdom[$kingdom])
		$candidateVectors = [System.Collections.Generic.List[float[]]]::new()
		foreach ($candidate in $candidates) { $candidateVectors.Add([float[]]$vectorByDocumentId[$candidate.doc_id]) }
		foreach ($query in $negativeQueries) {
			$queryVector = [PolicyTargetCalibrationRuntime]::Embed($EmbeddingCall, $query)
			$topIndices = [PolicyTargetCalibrationRuntime]::DenseTopKFromVectors($queryVector, $candidateVectors, 10)
			$topItems = [System.Collections.Generic.List[object]]::new()
			foreach ($index in $topIndices) { if ($index -ge 0) { $topItems.Add($candidates[$index]) } }
			$topVectors = [System.Collections.Generic.List[float[]]]::new()
			foreach ($item in $topItems) { $topVectors.Add([float[]]$vectorByDocumentId[$item.doc_id]) }
			$scores = [PolicyTargetCalibrationRuntime]::DenseScoresFromVectors($queryVector, $topVectors)
			$topByKind = @{}
			$guardedCount = 0
			for ($index = 0; $index -lt $topItems.Count; $index++) {
				$kind = [string]$topItems[$index].kind
				$score = [double]$scores[$index]
				$guarded = [bool]$mentionMethod.Invoke($null, @([string]$query, $topItems[$index].runtime_snapshot)) `
					-and [bool]$typeCueMethod.Invoke($null, @([string]$query, $topItems[$index].runtime_snapshot))
				if ($guarded) {
					$guardedCount++
					Add-Score -Map $negativeScores -Id $kind -Score $score
					if (-not $topByKind.ContainsKey($kind) -or $score -gt $topByKind[$kind]) { $topByKind[$kind] = $score }
				}
			}
			$negativeResults.Add([pscustomobject]@{ kingdom = $kingdom; query = $query; guarded_count = $guardedCount; max_by_kind = $topByKind })
		}
	}

	$calibration = [System.Collections.Generic.List[object]]::new()
	$allPassed = $true
	foreach ($kind in @("clan", "ruler", "settlement")) {
		$kindCases = @($caseResults | Where-Object kind -eq $kind)
		$positives = if ($positiveScores.ContainsKey($kind)) { @($positiveScores[$kind]) } else { @() }
		$negatives = if ($negativeScores.ContainsKey($kind)) { @($negativeScores[$kind]) } else { @() }
		$gaps = if ($rankGaps.ContainsKey($kind)) { @($rankGaps[$kind]) } else { @() }
		$minPositive = if ($positives.Count -gt 0) { [double]($positives | Measure-Object -Minimum).Minimum } else { [double]::NaN }
		$maxNegative = if ($negatives.Count -gt 0) { [double]($negatives | Measure-Object -Maximum).Maximum } else { [double]::NaN }
		$margin = if (-not [double]::IsNaN($minPositive) -and -not [double]::IsNaN($maxNegative)) { $minPositive - $maxNegative } else { [double]::NaN }
		$threshold = if (-not [double]::IsNaN($margin)) { ($minPositive + $maxNegative) / 2.0 } else { $minPositive - 0.0001 }
		$minRankGap = if ($gaps.Count -gt 0) { [double]($gaps | Measure-Object -Minimum).Minimum } else { [double]::NaN }
		$top1Count = @($kindCases | Where-Object top1).Count
		$falseActivations = $negatives.Count
		$configuredThreshold = [double]$configuredThresholds[$kind]
		$passed = $semanticExpansionEnabled `
			-and $kindCases.Count -gt 0 `
			-and $top1Count -eq $kindCases.Count `
			-and $falseActivations -eq 0 `
			-and -not [double]::IsNaN($minRankGap) `
			-and $minRankGap -ge $configuredMinimumGap `
			-and $minPositive -ge $configuredThreshold
		if (-not $passed) { $allPassed = $false }
		$calibration.Add([pscustomobject]@{
			kind = $kind
			case_count = $kindCases.Count
			dense_recall_count = @($kindCases | Where-Object dense_recalled).Count
			retrieval_recall_count = @($kindCases | Where-Object retrieval_recalled).Count
			top1_count = $top1Count
			min_positive = $(if ([double]::IsNaN($minPositive)) { $null } else { [math]::Round($minPositive, 6) })
			max_unnamed_negative = $(if ([double]::IsNaN($maxNegative)) { $null } else { [math]::Round($maxNegative, 6) })
			unnamed_false_activation_count = $falseActivations
			margin = $(if ([double]::IsNaN($margin)) { $null } else { [math]::Round($margin, 6) })
			min_top1_gap = $(if ([double]::IsNaN($minRankGap)) { $null } elseif ([double]::IsInfinity($minRankGap)) { 999.0 } else { [math]::Round($minRankGap, 6) })
			threshold = $(if ([double]::IsNaN($threshold)) { $null } else { [math]::Round($threshold, 6) })
			configured_threshold = [math]::Round($configuredThreshold, 6)
			configured_minimum_gap = [math]::Round($configuredMinimumGap, 6)
			passed = $passed
		})
	}

	$report = [ordered]@{
		schema_version = 2
		collected_utc = [DateTime]::UtcNow.ToString("o")
		implementation_version = $ImplementationVersion
		source = [ordered]@{
			game_root = $GameRootFull
			spclans_sha256 = (Get-FileHash -LiteralPath (Join-Path $ModuleDataRoot "spclans.xml") -Algorithm SHA256).Hash.ToLowerInvariant()
			lords_sha256 = (Get-FileHash -LiteralPath (Join-Path $ModuleDataRoot "lords.xml") -Algorithm SHA256).Hash.ToLowerInvariant()
			settlements_sha256 = (Get-FileHash -LiteralPath (Join-Path $ModuleDataRoot "settlements.xml") -Algorithm SHA256).Hash.ToLowerInvariant()
		}
		models = [ordered]@{
			embedding_sha256 = (Get-FileHash -LiteralPath $EmbeddingModel -Algorithm SHA256).Hash.ToLowerInvariant()
		}
		rules = [ordered]@{ scoring = "embedding_cosine"; entity_recall_limit = 10; configured_minimum_score_gap = $configuredMinimumGap; semantic_expansion_enabled = $semanticExpansionEnabled; fuzzy_named_cases = $cases.Count; unnamed_negative_queries_per_kingdom = $negativeQueries.Count; source_village_count = $villageSourceCount; indexed_village_count = $villageEntityCount; indexed_primary_fief_count = $primaryFiefEntityCount; valid_village_bound_count = $validVillageBoundCount; invalid_village_bound_count = $invalidVillageBoundCount; parents_with_multiple_villages = @($villageCountsByParent.GetEnumerator() | Where-Object { $_.Value -gt 1 }).Count; estimated_runtime_entity_count = $runtimeEntityCountEstimate; api_calls = 0 }
		calibration = @($calibration)
		cases = @($caseResults)
		unnamed_negatives = @($negativeResults)
		passed = $allPassed
	}
	$reportPath = Join-Path $ProjectRootFull "Phase0_Local_Archive\reports\policy_target_primary_fief_entity_embedding_calibration_20260809.json"
	$reportJson = $report | ConvertTo-Json -Depth 20
	[IO.File]::WriteAllText($reportPath, $reportJson, [Text.UTF8Encoding]::new($false))
	Write-Host "Entity precision calibration: cases=$($cases.Count), passed=$allPassed"
	Write-Host ($calibration | Format-Table kind, case_count, dense_recall_count, retrieval_recall_count, top1_count, unnamed_false_activation_count, min_top1_gap, passed -AutoSize | Out-String)
	Write-Host "Report: $reportPath"
	return $allPassed
}

$nativeHandle = [PolicyTargetCalibrationNativeLoader]::LoadLibrary((Join-Path $runtimeBin "onnxruntime.dll"))
if ($nativeHandle -eq [IntPtr]::Zero) {
	throw "Failed to load onnxruntime.dll, Win32Error=$([Runtime.InteropServices.Marshal]::GetLastWin32Error())"
}
$env:PATH = $runtimeBin + ";" + $referenceDirFull + ";" + $env:PATH
Push-Location -LiteralPath $runtimeBin
try {
	foreach ($file in Get-ChildItem -LiteralPath $referenceDirFull -File -Filter "*.dll") {
		if ([string]::Equals($file.Name, "AnimusForge.dll", [StringComparison]::OrdinalIgnoreCase)) { continue }
		try { [void][Reflection.Assembly]::LoadFrom($file.FullName) } catch { }
	}
	$assembly = [Reflection.Assembly]::LoadFrom((Join-Path $runtimeImplementationDir "AnimusForge.dll"))
	$embeddingType = $assembly.GetType("AnimusForge.OnnxEmbeddingEngine", $true)
	$embeddingInstance = $embeddingType.GetProperty("Instance", [Reflection.BindingFlags]"Public,Static").GetValue($null, $null)
	if (-not [bool]$embeddingType.GetProperty("IsAvailable").GetValue($embeddingInstance, $null)) {
		throw "Embedding unavailable: $($embeddingType.GetProperty('LastError').GetValue($embeddingInstance, $null))"
	}
	$embeddingCall = [Delegate]::CreateDelegate([PolicyTargetCalibrationEmbeddingCall], $embeddingInstance, $embeddingType.GetMethod("TryGetEmbedding"))
	$routerType = $assembly.GetType("AnimusForge.PolicyTargets.PolicyTargetSemanticRouter", $true)
	$facetCueMethod = $routerType.GetMethod("FacetMatchesExplicitCue", [Reflection.BindingFlags]"NonPublic,Static")
	$configuredFacetThresholds = $routerType.GetField("CalibratedFacetThresholds", [Reflection.BindingFlags]"NonPublic,Static").GetValue($null)
	$configuredEntityThresholds = $routerType.GetField("CalibratedEntityThresholds", [Reflection.BindingFlags]"NonPublic,Static").GetValue($null)
	$semanticExpansionEnabled = [bool]$routerType.GetField("SemanticExpansionEnabled", [Reflection.BindingFlags]"NonPublic,Static").GetRawConstantValue()
	$selectorCatalogType = $assembly.GetType("AnimusForge.PolicyTargets.PolicyTargetSelectorCatalog", $true)
	$selectorDescriptors = @($selectorCatalogType.GetProperty("Descriptors", [Reflection.BindingFlags]"NonPublic,Static").GetValue($null, $null))
	$selectorDescriptorTextById = @{}
	foreach ($descriptor in $selectorDescriptors) {
		$descriptorType = $descriptor.GetType()
		$id = [string]$descriptorType.GetProperty("Id", [Reflection.BindingFlags]"NonPublic,Instance").GetValue($descriptor, $null)
		$text = [string]$descriptorType.GetProperty("RetrievalText", [Reflection.BindingFlags]"NonPublic,Instance").GetValue($descriptor, $null)
		$selectorDescriptorTextById[$id] = $text
	}
	if ($EntityPrecision) {
		$precisionPassed = Invoke-EntityPrecisionCalibration `
			-EmbeddingCall $embeddingCall `
			-ModuleDataRoot $moduleDataRoot `
			-GameRootFull $gameRootFull `
			-ProjectRootFull $projectRootFull `
			-EmbeddingModel $embeddingModel `
			-ImplementationVersion $ImplementationVersion `
			-RouterType $routerType
		if (-not $precisionPassed) { exit 2 }
		return
	}

	$effectRouterType = $assembly.GetType("AnimusForge.PolicyEffects.PolicyEffectModuleRouter", $true)
	$effectSelectionType = $assembly.GetType("AnimusForge.PolicyEffects.PolicyEffectModuleSelection", $true)
	$effectFlags = [Reflection.BindingFlags]"NonPublic,Static"
	$getEffectQueryEmbeddingMethod = $effectRouterType.GetMethod("GetQueryEmbedding", $effectFlags)
	$recallEffectModulesMethod = $effectRouterType.GetMethod("Recall", $effectFlags)
	$selectEffectModulesMethod = $effectRouterType.GetMethod("SelectFromRecallScores", $effectFlags)
	$effectModuleProperty = $effectSelectionType.GetProperty("Module", [Reflection.BindingFlags]"NonPublic,Instance")
	$effectDetailHardMaximum = [int]$effectRouterType.GetField("DetailHardMaximum", [Reflection.BindingFlags]"NonPublic,Static").GetRawConstantValue()
	$effectQueryVector = [float[]]$getEffectQueryEmbeddingMethod.Invoke($null, @("降低税负、改善粮食与繁荣，并维持治安稳定"))
	$effectSelectionResults = [System.Collections.Generic.List[object]]::new()
	foreach ($scope in @("kingdom", "local", "vassal")) {
		$recallArguments = [object[]]::new(2)
		$recallArguments[0] = $effectQueryVector
		$recallArguments[1] = $scope
		$recalledEffectModules = $recallEffectModulesMethod.Invoke($null, $recallArguments)
		foreach ($limit in @(1, 6, 30)) {
			$selectionArguments = [object[]]::new(2)
			$selectionArguments[0] = $recalledEffectModules
			$selectionArguments[1] = $limit
			$selectedFirst = $selectEffectModulesMethod.Invoke($null, $selectionArguments)
			$selectedSecond = $selectEffectModulesMethod.Invoke($null, $selectionArguments)
			$firstIds = @($selectedFirst | ForEach-Object {
				$module = $effectModuleProperty.GetValue($_, $null)
				[string]$module.GetType().GetProperty("Id", [Reflection.BindingFlags]"Public,NonPublic,Instance").GetValue($module, $null)
			})
			$secondIds = @($selectedSecond | ForEach-Object {
				$module = $effectModuleProperty.GetValue($_, $null)
				[string]$module.GetType().GetProperty("Id", [Reflection.BindingFlags]"Public,NonPublic,Instance").GetValue($module, $null)
			})
			$expectedCount = [math]::Min([math]::Min($limit, $recalledEffectModules.Count), $effectDetailHardMaximum)
			$stable = [string]::Equals(($firstIds -join "|"), ($secondIds -join "|"), [StringComparison]::Ordinal)
			$scopeSafe = -not [string]::Equals($scope, "local", [StringComparison]::OrdinalIgnoreCase) -or $firstIds -notcontains "kingdomStabilityOnce"
			$effectSelectionResults.Add([pscustomobject]@{
				scope = $scope
				requested = $limit
				recalled_count = $recalledEffectModules.Count
				selected_count = $firstIds.Count
				selected_ids = $firstIds
				stable = $stable
				scope_safe = $scopeSafe
				passed = ($firstIds.Count -eq $expectedCount -and $stable -and $scopeSafe)
			})
		}
	}
	$behaviorType = $assembly.GetType("AnimusForge.CustomPolicyBehavior", $true)
	$requestType = $assembly.GetType("AnimusForge.CustomPolicyBehavior+PolicyDraftRequest", $true)
	$targetHandleType = $assembly.GetType("AnimusForge.CustomPolicyBehavior+PolicyTargetHandleSaveData", $true)
	$request = [Activator]::CreateInstance($requestType, $true)
	$requestType.GetField("ScopeKind", [Reflection.BindingFlags]"Public,NonPublic,Instance").SetValue($request, "local")
	$requestType.GetField("ManualDurationDays", [Reflection.BindingFlags]"Public,NonPublic,Instance").SetValue($request, 7)
	$sourceHandle = [Activator]::CreateInstance($targetHandleType, $true)
	$targetHandleType.GetProperty("Key", [Reflection.BindingFlags]"Public,NonPublic,Instance").SetValue($sourceHandle, "S", $null)
	$targetHandleType.GetProperty("Kind", [Reflection.BindingFlags]"Public,NonPublic,Instance").SetValue($sourceHandle, "source", $null)
	$targetHandleListType = [Collections.Generic.List``1].MakeGenericType($targetHandleType)
	$targetHandleList = [Activator]::CreateInstance($targetHandleListType)
	$targetHandleList.Add($sourceHandle)
	$requestType.GetField("TargetHandles", [Reflection.BindingFlags]"Public,NonPublic,Instance").SetValue($request, $targetHandleList)
	$assessmentType = $assembly.GetType("AnimusForge.CustomPolicyBehavior+PolicyMainAssessmentResult", $true)
	$assessment = [Activator]::CreateInstance($assessmentType, $true)
	$buildFinalMethod = $behaviorType.GetMethod("TryBuildFinalPolicyPostprocess", [Reflection.BindingFlags]"NonPublic,Static")
	$parseArguments = [object[]]@($request, $assessment, '{"durationDays":7,"effects":[{"targets":["S附属村庄"],"changes":{"hearthPerDay":1}}]}', $null, $null)
	$compositeHandleAccepted = [bool]$buildFinalMethod.Invoke($null, $parseArguments)
	$compositeHandleContract = [pscustomobject]@{
		input = "S附属村庄"
		accepted = $compositeHandleAccepted
		error = [string]$parseArguments[4]
		passed = -not $compositeHandleAccepted
	}
	$entitySnapshotType = $assembly.GetType("AnimusForge.PolicyTargets.PolicyTargetEntitySnapshot", $true)
	$worldSnapshotType = $assembly.GetType("AnimusForge.PolicyTargets.PolicyTargetWorldSnapshot", $true)
	$villageSnapshotEntity = [Activator]::CreateInstance($entitySnapshotType, $true)
	$entitySnapshotType.GetProperty("Kind", [Reflection.BindingFlags]"NonPublic,Instance").SetValue($villageSnapshotEntity, "settlement", $null)
	$entitySnapshotType.GetProperty("EntityId", [Reflection.BindingFlags]"NonPublic,Instance").SetValue($villageSnapshotEntity, "village_test", $null)
	$entitySnapshotType.GetProperty("OwnerKingdomId", [Reflection.BindingFlags]"NonPublic,Instance").SetValue($villageSnapshotEntity, "kingdom_test", $null)
	$entitySnapshotListType = [Collections.Generic.List``1].MakeGenericType($entitySnapshotType)
	$entitySnapshotList = [Activator]::CreateInstance($entitySnapshotListType)
	$entitySnapshotList.Add($villageSnapshotEntity)
	$worldSnapshot = [Activator]::CreateInstance($worldSnapshotType, $true)
	$worldSnapshotType.GetProperty("Entities", [Reflection.BindingFlags]"NonPublic,Instance").SetValue($worldSnapshot, $entitySnapshotList, $null)
	$requestType.GetField("PlayerKingdomId", [Reflection.BindingFlags]"Public,NonPublic,Instance").SetValue($request, "kingdom_test")
	$requestType.GetField("SemanticTargetSnapshot", [Reflection.BindingFlags]"Public,NonPublic,Instance").SetValue($request, $worldSnapshot)
	$villageHandle = [Activator]::CreateInstance($targetHandleType, $true)
	$targetHandleType.GetProperty("Key", [Reflection.BindingFlags]"Public,NonPublic,Instance").SetValue($villageHandle, "L0", $null)
	$targetHandleType.GetProperty("Kind", [Reflection.BindingFlags]"Public,NonPublic,Instance").SetValue($villageHandle, "settlement", $null)
	$targetHandleType.GetProperty("EntityId", [Reflection.BindingFlags]"Public,NonPublic,Instance").SetValue($villageHandle, "village_test", $null)
	$targetHandleAllowedMethod = $behaviorType.GetMethod("IsPolicyTargetHandleAllowedForRequest", [Reflection.BindingFlags]"NonPublic,Static")
	$villageHandleAccepted = [bool]$targetHandleAllowedMethod.Invoke($null, @($request, $villageHandle))
	$villageHandleContract = [pscustomobject]@{
		input = "L0:village_test"
		accepted = $villageHandleAccepted
		passed = -not $villageHandleAccepted
	}

	$routerSource = [IO.File]::ReadAllText((Join-Path $projectRootFull "PolicySystem\Targets\PolicyTargetSemanticRouter.cs"), [Text.Encoding]::UTF8)
	$generationSource = [IO.File]::ReadAllText((Join-Path $projectRootFull "PolicySystem\Core\CustomPolicyBehavior.Generation.cs"), [Text.Encoding]::UTF8)
	$targetsSource = [IO.File]::ReadAllText((Join-Path $projectRootFull "PolicySystem\Core\CustomPolicyBehavior.Targets.cs"), [Text.Encoding]::UTF8)
	$hearthSource = [IO.File]::ReadAllText((Join-Path $projectRootFull "PolicySystem\Effects\Modules\hearthPerDay\HearthPerDayEffectModule.cs"), [Text.Encoding]::UTF8)
	$primaryFiefContracts = [ordered]@{
		router_has_no_village_snapshot_fields = ($routerSource -notmatch '\bIsVillage\b' -and $routerSource -notmatch '\bHearth\b')
		router_has_no_village_target_facets = ($routerSource -notmatch 'type_village' -and $routerSource -notmatch 'metric_hearth_')
		local_context_has_no_village_rows_or_metrics = ($generationSource -notmatch '附属村庄 ID=' -and $generationSource -notmatch '村庄均值' -and $generationSource -notmatch '村庄关键项' -and $generationSource -notmatch '展开后定居点')
		explicit_village_mentions_use_parent_resolver = ($targetsSource -match 'ResolvePrimaryPolicyFief\(settlement\)' -and $targetsSource -match 'primaryFief\.Name')
		direct_parent_handles_expand_in_csharp = ($targetsSource -match 'ExpandLocalPolicySettlements\(new\[\] \{ primaryFief \}\)')
		settlement_handles_require_primary_fiefs = ($targetsSource -match 'IsPrimaryPolicyFiefTarget\(request, target\)')
		prompts_forbid_composite_village_handles = ($generationSource -match '禁止生成 S附属村庄、L0附属村庄' -and $generationSource -match '不得在短句柄后拼接村庄名称')
		hearth_module_uses_parent_handle_scope = ($hearthSource -match '合法城镇/城堡父级句柄' -and $hearthSource -match '附属村庄结算')
	}
	$primaryFiefContractsPassed = @($primaryFiefContracts.Values | Where-Object { $_ -ne $true }).Count -eq 0

	$facetsField = $routerType.GetField("Facets", [Reflection.BindingFlags]"NonPublic,Static")
	$facetObjects = @($facetsField.GetValue($null))
	$facets = [System.Collections.Generic.List[object]]::new()
	foreach ($facetObject in $facetObjects) {
		$type = $facetObject.GetType()
		$facets.Add([pscustomobject]@{
			id = [string]$type.GetProperty("Id", [Reflection.BindingFlags]"NonPublic,Instance").GetValue($facetObject, $null)
			group = [string]$type.GetProperty("Group", [Reflection.BindingFlags]"NonPublic,Instance").GetValue($facetObject, $null)
			text = [string]$type.GetProperty("RetrievalText", [Reflection.BindingFlags]"NonPublic,Instance").GetValue($facetObject, $null)
			runtime = $facetObject
		})
	}
	$forbiddenPrimaryFiefFacetIds = @("type_village", "metric_hearth_high", "metric_hearth_low")
	$unexpectedPrimaryFiefFacets = @($facets | Where-Object { $forbiddenPrimaryFiefFacetIds -contains $_.id })
	if ($unexpectedPrimaryFiefFacets.Count -gt 0) {
		throw "Village target facets remain enabled: $(@($unexpectedPrimaryFiefFacets.id) -join ',')"
	}
	$cases = [System.Collections.Generic.List[object]]::new()
	foreach ($line in [IO.File]::ReadLines($casesPath, [Text.Encoding]::UTF8)) {
		if (-not [string]::IsNullOrWhiteSpace($line)) { $cases.Add(($line | ConvertFrom-Json)) }
	}
	if ($cases.Count -eq 0) { throw "Calibration cases are empty." }

	$positiveFacetScores = @{}
	$negativeFacetScores = @{}
	$hardRejectedFacetCounts = @{}
	$positiveEntityScores = @{}
	$negativeEntityScores = @{}
	$positiveSelectorScores = @{}
	$negativeSelectorScores = @{}
	$caseResults = [System.Collections.Generic.List[object]]::new()
	foreach ($case in $cases) {
		$query = [string]$case.query
		$queryVector = [PolicyTargetCalibrationRuntime]::Embed($embeddingCall, $query)
		$entityCandidates = @($case.entity_candidates)
		$selectorCandidates = @($case.selector_candidates | Where-Object {
			$null -ne $_ -and -not [string]::IsNullOrWhiteSpace([string]$_.id) -and -not [string]::IsNullOrWhiteSpace([string]$_.text)
		})
		foreach ($candidate in $selectorCandidates) {
			$id = [string]$candidate.id
			if ($selectorDescriptorTextById.ContainsKey($id)) { $candidate.text = [string]$selectorDescriptorTextById[$id] }
		}
		$entityDocuments = [string[]]@($entityCandidates | ForEach-Object { [string]$_.text })
		$selectorDocuments = [string[]]@($selectorCandidates | ForEach-Object { [string]$_.text })
		$facetDocuments = [string[]]@($facets | ForEach-Object { [string]$_.text })
		$entityTop = [PolicyTargetCalibrationRuntime]::DenseTopK($embeddingCall, $queryVector, $entityDocuments, 10)
		$selectorTop = [PolicyTargetCalibrationRuntime]::DenseTopK($embeddingCall, $queryVector, $selectorDocuments, 6)
		$facetOrdered = [PolicyTargetCalibrationRuntime]::DenseTopK($embeddingCall, $queryVector, $facetDocuments, $facetDocuments.Length)
		$entityAllScores = [PolicyTargetCalibrationRuntime]::DenseScores($embeddingCall, $queryVector, $entityDocuments)
		$selectorAllScores = [PolicyTargetCalibrationRuntime]::DenseScores($embeddingCall, $queryVector, $selectorDocuments)
		$allFacetScores = [PolicyTargetCalibrationRuntime]::DenseScores($embeddingCall, $queryVector, $facetDocuments)
		$entityScoreMap = @{}
		for ($index = 0; $index -lt $entityCandidates.Count; $index++) { $entityScoreMap[[string]$entityCandidates[$index].id] = [double]$entityAllScores[$index] }
		$selectorScoreMap = @{}
		for ($index = 0; $index -lt $selectorCandidates.Count; $index++) { $selectorScoreMap[[string]$selectorCandidates[$index].id] = [double]$selectorAllScores[$index] }
		$allFacetScoreMap = @{}
		for ($index = 0; $index -lt $facets.Count; $index++) { $allFacetScoreMap[[string]$facets[$index].id] = [double]$allFacetScores[$index] }
		$facetTop = [System.Collections.Generic.List[int]]::new()
		$facetGroupCounts = @{}
		foreach ($index in $facetOrdered) {
			if ($index -lt 0) { continue }
			$group = [string]$facets[$index].group
			$count = if ($facetGroupCounts.ContainsKey($group)) { [int]$facetGroupCounts[$group] } else { 0 }
			if ($count -ge 2) { continue }
			$facetTop.Add($index)
			$facetGroupCounts[$group] = $count + 1
			if ($facetTop.Count -ge 8) { break }
		}
		$items = [System.Collections.Generic.List[object]]::new()
		foreach ($index in $entityTop) {
			if ($index -ge 0) { $items.Add([pscustomobject]@{ category = "entity"; id = [string]$entityCandidates[$index].id; kind = [string]$entityCandidates[$index].kind; score = [double]$entityAllScores[$index] }) }
		}
		foreach ($index in $facetTop) {
			if ($index -ge 0) { $items.Add([pscustomobject]@{ category = "facet"; id = [string]$facets[$index].id; kind = ""; score = [double]$allFacetScores[$index] }) }
		}
		$scored = @($items)
		$facetScoreMap = @{}
		foreach ($item in @($scored | Where-Object category -eq "facet")) { $facetScoreMap[$item.id] = [double]$item.score }
		$missingExpectedFacets = [System.Collections.Generic.List[string]]::new()
		foreach ($facetId in @($case.expected_facets)) {
			$id = [string]$facetId
			$facet = @($facets | Where-Object id -eq $id | Select-Object -First 1)
			$cueMatched = $facet.Count -eq 1 -and [bool]$facetCueMethod.Invoke($null, @($query, $facet[0].runtime))
			if (-not $facetScoreMap.ContainsKey($id) -or -not $cueMatched) { $missingExpectedFacets.Add($id) }
			if ($cueMatched -and $allFacetScoreMap.ContainsKey($id)) { Add-Score -Map $positiveFacetScores -Id $id -Score $allFacetScoreMap[$id] }
		}
		foreach ($facetId in @($case.forbidden_facets)) {
			$id = [string]$facetId
			$facet = @($facets | Where-Object id -eq $id | Select-Object -First 1)
			$cueMatched = $facet.Count -eq 1 -and [bool]$facetCueMethod.Invoke($null, @($query, $facet[0].runtime))
			if ($cueMatched -and $allFacetScoreMap.ContainsKey($id)) {
				Add-Score -Map $negativeFacetScores -Id $id -Score $allFacetScoreMap[$id]
			}
			elseif ($facet.Count -eq 1) {
				$hardRejectedFacetCounts[$id] = 1 + $(if ($hardRejectedFacetCounts.ContainsKey($id)) { [int]$hardRejectedFacetCounts[$id] } else { 0 })
			}
		}
		$expectedEntityId = [string]$case.expected_entity_id
		$entityOrdered = @($scored | Where-Object category -eq "entity" | Sort-Object score -Descending)
		$entityTopId = if ($entityOrdered.Count -gt 0) { [string]$entityOrdered[0].id } else { "" }
		$entityPassed = $true
		if (-not [string]::IsNullOrWhiteSpace($expectedEntityId)) {
			$entityPassed = $entityScoreMap.ContainsKey($expectedEntityId) -and $entityTopId -eq $expectedEntityId
			$expectedCandidate = @($entityCandidates | Where-Object id -eq $expectedEntityId | Select-Object -First 1)
			if ($entityScoreMap.ContainsKey($expectedEntityId) -and $expectedCandidate.Count -eq 1) {
				Add-Score -Map $positiveEntityScores -Id ([string]$expectedCandidate[0].kind) -Score ([double]$entityScoreMap[$expectedEntityId])
			}
		}
		foreach ($entityId in @($case.forbidden_entity_ids)) {
			$id = [string]$entityId
			$forbiddenCandidate = @($entityCandidates | Where-Object id -eq $id | Select-Object -First 1)
			if ($entityScoreMap.ContainsKey($id) -and $forbiddenCandidate.Count -eq 1) {
				Add-Score -Map $negativeEntityScores -Id ([string]$forbiddenCandidate[0].kind) -Score ([double]$entityScoreMap[$id])
			}
		}
		$expectedSelectorId = [string]$case.expected_selector_id
		$selectorOrdered = @($selectorTop | Where-Object { $_ -ge 0 } | ForEach-Object {
			[pscustomobject]@{ id = [string]$selectorCandidates[$_].id; score = [double]$selectorAllScores[$_] }
		} | Sort-Object score -Descending)
		$selectorTopId = if ($selectorOrdered.Count -gt 0) { [string]$selectorOrdered[0].id } else { "" }
		$selectorPassed = $true
		if (-not [string]::IsNullOrWhiteSpace($expectedSelectorId)) {
			if ($selectorDescriptorTextById.ContainsKey($expectedSelectorId)) {
				$selectorPassed = $selectorScoreMap.ContainsKey($expectedSelectorId)
				if ($selectorPassed) { Add-Score -Map $positiveSelectorScores -Id $expectedSelectorId -Score $selectorScoreMap[$expectedSelectorId] }
			}
			else {
				$selectorPassed = $selectorTopId -eq $expectedSelectorId
			}
		}
		foreach ($selectorId in @($case.forbidden_selector_ids)) {
			$id = [string]$selectorId
			if ($selectorDescriptorTextById.ContainsKey($id) -and $selectorScoreMap.ContainsKey($id)) {
				Add-Score -Map $negativeSelectorScores -Id $id -Score $selectorScoreMap[$id]
			}
			if (-not [string]::IsNullOrWhiteSpace($id) -and $selectorTopId -eq $id) { $selectorPassed = $false }
		}
		$caseResults.Add([pscustomobject]@{
			case_id = [string]$case.case_id
			query = $query
			passed = ($missingExpectedFacets.Count -eq 0 -and $entityPassed -and $selectorPassed)
			missing_expected_facets = @($missingExpectedFacets)
			production_facet_ids = @($facetTop | ForEach-Object { [string]$facets[$_].id })
			entity_top_id = $entityTopId
			expected_entity_id = $expectedEntityId
			selector_top_id = $selectorTopId
			expected_selector_id = $expectedSelectorId
			ranked_facets = @($scored | Where-Object category -eq "facet" | Sort-Object score -Descending | ForEach-Object { [ordered]@{ id = $_.id; score = [math]::Round($_.score, 6) } })
			ranked_entities = @($entityOrdered | ForEach-Object { [ordered]@{ id = $_.id; score = [math]::Round($_.score, 6) } })
			ranked_selectors = @($selectorOrdered | ForEach-Object { [ordered]@{ id = $_.id; score = [math]::Round($_.score, 6) } })
		})
	}

	$facetCalibration = [System.Collections.Generic.List[object]]::new()
	foreach ($facetId in @($positiveFacetScores.Keys | Sort-Object)) {
		$positives = @($positiveFacetScores[$facetId])
		$negatives = if ($negativeFacetScores.ContainsKey($facetId)) { @($negativeFacetScores[$facetId]) } else { @() }
		$minPositive = [double]($positives | Measure-Object -Minimum).Minimum
		$maxNegative = if ($negatives.Count -gt 0) { [double]($negatives | Measure-Object -Maximum).Maximum } else { [double]::NaN }
		$margin = if ($negatives.Count -gt 0) { $minPositive - $maxNegative } else { [double]::NaN }
		$hardRejectedCount = if ($hardRejectedFacetCounts.ContainsKey($facetId)) { [int]$hardRejectedFacetCounts[$facetId] } else { 0 }
		$threshold = if ($negatives.Count -gt 0) { ($minPositive + $maxNegative) / 2.0 } elseif ($hardRejectedCount -gt 0) { $minPositive - 0.0001 } else { [double]::NaN }
		$configuredThreshold = [double]$configuredFacetThresholds[$facetId]
		$separated = (-not [double]::IsNaN($margin) -and $margin -gt 0.0) -or ($negatives.Count -eq 0 -and $hardRejectedCount -gt 0 -and -not [double]::IsNaN($threshold))
		$configuredPassed = $minPositive -ge $configuredThreshold -and ($negatives.Count -eq 0 -or $maxNegative -lt $configuredThreshold)
		$passed = $semanticExpansionEnabled -and $separated -and $configuredPassed
		$facetCalibration.Add([pscustomobject]@{
			id = [string]$facetId
			min_positive = [math]::Round($minPositive, 6)
			max_negative = $(if ([double]::IsNaN($maxNegative)) { $null } else { [math]::Round($maxNegative, 6) })
			margin = $(if ([double]::IsNaN($margin)) { $null } else { [math]::Round($margin, 6) })
			threshold = $(if ([double]::IsNaN($threshold)) { $null } else { [math]::Round($threshold, 6) })
			configured_threshold = [math]::Round($configuredThreshold, 6)
			eligible_negative_count = $negatives.Count
			hard_rejected_negative_count = $hardRejectedCount
			passed = $passed
		})
	}
	$entityCalibration = [System.Collections.Generic.List[object]]::new()
	foreach ($kind in @($positiveEntityScores.Keys | Sort-Object)) {
		$positives = @($positiveEntityScores[$kind])
		$negatives = if ($negativeEntityScores.ContainsKey($kind)) { @($negativeEntityScores[$kind]) } else { @() }
		$minPositive = [double]($positives | Measure-Object -Minimum).Minimum
		$maxNegative = if ($negatives.Count -gt 0) { [double]($negatives | Measure-Object -Maximum).Maximum } else { [double]::NaN }
		$margin = if ($negatives.Count -gt 0) { $minPositive - $maxNegative } else { [double]::NaN }
		$threshold = if ($negatives.Count -gt 0) { ($minPositive + $maxNegative) / 2.0 } else { [double]::NaN }
		$configuredThreshold = [double]$configuredEntityThresholds[$kind]
		$configuredPassed = $minPositive -ge $configuredThreshold
		$entityCalibration.Add([pscustomobject]@{
			kind = [string]$kind
			min_positive = [math]::Round($minPositive, 6)
			max_negative = $(if ([double]::IsNaN($maxNegative)) { $null } else { [math]::Round($maxNegative, 6) })
			margin = $(if ([double]::IsNaN($margin)) { $null } else { [math]::Round($margin, 6) })
			threshold = $(if ([double]::IsNaN($threshold)) { $null } else { [math]::Round($threshold, 6) })
			configured_threshold = [math]::Round($configuredThreshold, 6)
			passed = ($semanticExpansionEnabled -and -not [double]::IsNaN($margin) -and $margin -gt 0.0 -and $configuredPassed)
		})
	}
	$selectorCalibration = [System.Collections.Generic.List[object]]::new()
	foreach ($selectorId in @($selectorDescriptorTextById.Keys | Sort-Object)) {
		$positives = if ($positiveSelectorScores.ContainsKey($selectorId)) { @($positiveSelectorScores[$selectorId]) } else { @() }
		$negatives = if ($negativeSelectorScores.ContainsKey($selectorId)) { @($negativeSelectorScores[$selectorId]) } else { @() }
		$minPositive = if ($positives.Count -gt 0) { [double]($positives | Measure-Object -Minimum).Minimum } else { [double]::NaN }
		$maxNegative = if ($negatives.Count -gt 0) { [double]($negatives | Measure-Object -Maximum).Maximum } else { [double]::NaN }
		$margin = if ($positives.Count -gt 0 -and $negatives.Count -gt 0) { $minPositive - $maxNegative } else { [double]::NaN }
		$selectorCalibration.Add([pscustomobject]@{
			id = [string]$selectorId
			min_positive = $(if ([double]::IsNaN($minPositive)) { $null } else { [math]::Round($minPositive, 6) })
			max_negative = $(if ([double]::IsNaN($maxNegative)) { $null } else { [math]::Round($maxNegative, 6) })
			margin = $(if ([double]::IsNaN($margin)) { $null } else { [math]::Round($margin, 6) })
			positive_count = $positives.Count
			negative_count = $negatives.Count
			passed = ($positives.Count -gt 0 -and $negatives.Count -gt 0 -and $margin -gt 0.0)
		})
	}
	$allPassed = @($caseResults | Where-Object { -not $_.passed }).Count -eq 0 `
		-and @($facetCalibration | Where-Object { -not $_.passed }).Count -eq 0 `
		-and $entityCalibration.Count -gt 0 `
		-and @($entityCalibration | Where-Object { -not $_.passed }).Count -eq 0 `
		-and $selectorCalibration.Count -eq $selectorDescriptorTextById.Count `
		-and @($selectorCalibration | Where-Object { -not $_.passed }).Count -eq 0 `
		-and @($effectSelectionResults | Where-Object { -not $_.passed }).Count -eq 0 `
		-and $compositeHandleContract.passed `
		-and $villageHandleContract.passed `
		-and $primaryFiefContractsPassed

	$report = [ordered]@{
		schema_version = 2
		collected_utc = [DateTime]::UtcNow.ToString("o")
		implementation_version = $ImplementationVersion
		models = [ordered]@{
			embedding_sha256 = (Get-FileHash -LiteralPath $embeddingModel -Algorithm SHA256).Hash.ToLowerInvariant()
		}
		rules = [ordered]@{ scoring = "embedding_cosine"; entity_recall_limit = 10; facet_recall_limit = 8; facet_group_limit = 2; minimum_margin = 0.0; semantic_expansion_enabled = $semanticExpansionEnabled; calibration_scores_all_facets = $true }
		cases = @($caseResults)
		facet_calibration = @($facetCalibration)
		entity_calibration = @($entityCalibration)
		selector_calibration = @($selectorCalibration)
		effect_module_selection = @($effectSelectionResults)
		composite_handle_fail_closed = $compositeHandleContract
		village_entity_handle_fail_closed = $villageHandleContract
		primary_fief_contracts = $primaryFiefContracts
		passed = $allPassed
		api_calls = 0
	}
	$reportPath = if ([string]::IsNullOrWhiteSpace($ReportPath)) {
		Join-Path $projectRootFull "Phase0_Local_Archive\reports\policy_target_primary_fief_embedding_calibration_20260809.json"
	} else {
		Get-FullPath -Path $ReportPath
	}
	$reportJson = $report | ConvertTo-Json -Depth 20
	[IO.File]::WriteAllText($reportPath, $reportJson, [Text.UTF8Encoding]::new($false))
	$reportJson
	if (-not $allPassed) { exit 2 }
}
finally {
	Pop-Location
}

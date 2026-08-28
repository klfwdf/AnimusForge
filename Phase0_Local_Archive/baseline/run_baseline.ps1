param(
    [string]$ProjectRoot = "",
    [string]$StageModuleRoot = "",
    [string]$ReferenceDir = "",
    [string]$RerankerModelDir = "",
    [ValidateSet("1.3", "1.4")]
    [string]$ImplementationVersion = "1.4",
    [ValidateRange(3, 101)]
    [int]$Iterations = 11,
    [ValidateRange(8, 512)]
    [int]$CorpusSize = 32,
    [ValidateRange(1, 64)]
    [int]$TopK = 8,
    [string]$OutputPath = ""
)

$ErrorActionPreference = "Stop"

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

function Assert-Directory {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path -PathType Container)) {
        throw "Required directory not found: $Path"
    }
}

function New-HardLinkChecked {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Target
    )
    Assert-File -Path $Target
    [void](New-Item -ItemType HardLink -Path $Path -Target $Target)
}

function Read-JsonLines {
    param([Parameter(Mandatory = $true)][string]$Path)
    Assert-File -Path $Path
    $items = New-Object System.Collections.Generic.List[object]
    $lineNumber = 0
    foreach ($line in [System.IO.File]::ReadLines($Path, [System.Text.Encoding]::UTF8)) {
        $lineNumber++
        if ([string]::IsNullOrWhiteSpace($line)) {
            continue
        }
        try {
            $items.Add(($line | ConvertFrom-Json))
        }
        catch {
            throw "Invalid JSONL at ${Path}:$lineNumber - $($_.Exception.Message)"
        }
    }
    if ($items.Count -eq 0) {
        throw "Evaluation set is empty: $Path"
    }
    $duplicateIds = $items | Group-Object -Property case_id | Where-Object { [string]::IsNullOrWhiteSpace($_.Name) -or $_.Count -ne 1 }
    if ($duplicateIds) {
        throw "Evaluation set has missing or duplicate case_id values: $Path"
    }
    return $items
}

function Assert-EvaluationCases {
    param(
        [Parameter(Mandatory = $true)]$Cases,
        [Parameter(Mandatory = $true)][ValidateSet("policy_history", "effect_module")][string]$Kind
    )
    foreach ($case in $Cases) {
        $text = if ($Kind -eq "policy_history") { [string]$case.query } else { [string]$case.request }
        if ([string]::IsNullOrWhiteSpace($text) -or $null -eq $case.scope) {
            throw "Evaluation case $($case.case_id) is missing text or scope."
        }
        $candidateIds = @($case.candidates | ForEach-Object { [string]$_.id })
        if ($candidateIds.Count -eq 0 -or @($candidateIds | Where-Object { [string]::IsNullOrWhiteSpace($_) }).Count -ne 0 -or @($candidateIds | Select-Object -Unique).Count -ne $candidateIds.Count) {
            throw "Evaluation case $($case.case_id) has missing or duplicate candidate ids."
        }
        $referencedIds = if ($Kind -eq "policy_history") {
            @($case.expected_relevant_ids) + @($case.hard_negative_ids) + @($case.forbidden_ids)
        }
        else {
            $rejectedDependencyIds = if ($null -ne $case.rejected_dependencies) { @($case.rejected_dependencies.PSObject.Properties.Name) } else { @() }
            @($case.expected_semantic_ids) + @($case.expected_final_ids) + @($case.forbidden_ids) + $rejectedDependencyIds
        }
        foreach ($referencedId in @($referencedIds | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_) })) {
            if ($candidateIds -notcontains [string]$referencedId) {
                throw "Evaluation case $($case.case_id) references missing candidate id: $referencedId"
            }
        }
        if ($Kind -eq "policy_history") {
            if ([int]$case.top_k -lt 1 -or [int]$case.abolished_quota -lt 0) {
                throw "Policy history case $($case.case_id) has invalid top_k or abolished_quota."
            }
            if (-not [string]::IsNullOrWhiteSpace([string]$case.expected_first_id) -and @($case.expected_relevant_ids) -notcontains [string]$case.expected_first_id) {
                throw "Policy history case $($case.case_id) has expected_first_id outside expected_relevant_ids."
            }
        }
        elseif ([string]::IsNullOrWhiteSpace([string]$case.expected_outcome)) {
            throw "Effect module case $($case.case_id) is missing expected_outcome."
        }
    }

    $stabilityGroups = @($Cases | Where-Object { -not [string]::IsNullOrWhiteSpace([string]$_.stability_group) } | Group-Object -Property stability_group)
    foreach ($group in $stabilityGroups) {
        if ($group.Count -lt 2) {
            throw "Stability group $($group.Name) must contain at least two cases."
        }
    }
}

function Get-PercentileSummary {
    param([Parameter(Mandatory = $true)]$Measurement)
    return [ordered]@{
        count = [int]$Measurement.Count
        p50_ms = [math]::Round([double]$Measurement.P50Ms, 4)
        p95_ms = [math]::Round([double]$Measurement.P95Ms, 4)
        max_ms = [math]::Round([double]$Measurement.MaxMs, 4)
        allocated_bytes_total = [long]$Measurement.AllocatedBytes
        allocated_bytes_per_operation = [math]::Round([double]$Measurement.AllocatedBytesPerOperation, 2)
    }
}

if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = Join-Path $PSScriptRoot "..\.."
}
$projectRootFull = Get-FullPath -Path $ProjectRoot

if ([string]::IsNullOrWhiteSpace($StageModuleRoot)) {
    $StageModuleRoot = Join-Path $projectRootFull "bin\Debug\single_module_stage\AnimusForge"
}
$stageModuleRootFull = Get-FullPath -Path $StageModuleRoot
Assert-Directory -Path $stageModuleRootFull

if ([string]::IsNullOrWhiteSpace($ReferenceDir)) {
    $ReferenceDir = Join-Path $projectRootFull (".tmp\build_check\" + $ImplementationVersion)
}
$referenceDirFull = Get-FullPath -Path $ReferenceDir
Assert-Directory -Path $referenceDirFull

if ([string]::IsNullOrWhiteSpace($RerankerModelDir)) {
    throw "-RerankerModelDir is required. The Phase 0 baseline is fail-closed and does not run without the existing reranker assets."
}
$rerankerModelDirFull = Get-FullPath -Path $RerankerModelDir
Assert-Directory -Path $rerankerModelDirFull

$embeddingModel = Join-Path $stageModuleRootFull "ONNX\model.onnx"
$embeddingModelData = Join-Path $stageModuleRootFull "ONNX\model.onnx_data"
$embeddingTokenizer = Join-Path $stageModuleRootFull "ONNX\tokenizer.json"
$embeddingConfig = Join-Path $stageModuleRootFull "ONNX\config.json"
$rerankerModel = Join-Path $rerankerModelDirFull "model.onnx"
$rerankerTokenizer = Join-Path $rerankerModelDirFull "tokenizer.json"
foreach ($required in @($embeddingModel, $embeddingTokenizer, $rerankerModel, $rerankerTokenizer)) {
    Assert-File -Path $required
}

$stageBin = Join-Path $stageModuleRootFull "bin\Win64_Shipping_Client"
$stageImplementation = Join-Path $stageBin ("versions\" + $ImplementationVersion + "\AnimusForge.dll")
$stageManagedOnnx = Join-Path $stageBin "Microsoft.ML.OnnxRuntime.dll"
$stageNativeOnnx = Join-Path $stageBin "onnxruntime.dll"
$stageNativeProviders = Join-Path $stageBin "onnxruntime_providers_shared.dll"
foreach ($required in @($stageImplementation, $stageManagedOnnx, $stageNativeOnnx, $stageNativeProviders)) {
    Assert-File -Path $required
}

$runId = [DateTime]::UtcNow.ToString("yyyyMMdd_HHmmss_fff") + "_" + $PID.ToString([System.Globalization.CultureInfo]::InvariantCulture)
$runtimeRoot = Join-Path $projectRootFull ("bin\Debug\policy_phase0_runtime\" + $runId + "\AnimusForge")
$runtimeBin = Join-Path $runtimeRoot "bin\Win64_Shipping_Client"
$runtimeImplementationDir = Join-Path $runtimeBin ("versions\" + $ImplementationVersion)
$runtimeOnnx = Join-Path $runtimeRoot "ONNX"
[void](New-Item -ItemType Directory -Path $runtimeImplementationDir -Force)
[void](New-Item -ItemType Directory -Path (Join-Path $runtimeRoot "ModuleData") -Force)
[void](New-Item -ItemType Directory -Path $runtimeOnnx -Force)

New-HardLinkChecked -Path (Join-Path $runtimeRoot "SubModule.xml") -Target (Join-Path $stageModuleRootFull "SubModule.xml")
New-HardLinkChecked -Path (Join-Path $runtimeImplementationDir "AnimusForge.dll") -Target $stageImplementation
New-HardLinkChecked -Path (Join-Path $runtimeBin "Microsoft.ML.OnnxRuntime.dll") -Target $stageManagedOnnx
New-HardLinkChecked -Path (Join-Path $runtimeBin "onnxruntime.dll") -Target $stageNativeOnnx
New-HardLinkChecked -Path (Join-Path $runtimeBin "onnxruntime_providers_shared.dll") -Target $stageNativeProviders
New-HardLinkChecked -Path (Join-Path $runtimeOnnx "model.onnx") -Target $embeddingModel
if (Test-Path -LiteralPath $embeddingModelData -PathType Leaf) {
    New-HardLinkChecked -Path (Join-Path $runtimeOnnx "model.onnx_data") -Target $embeddingModelData
}
New-HardLinkChecked -Path (Join-Path $runtimeOnnx "tokenizer.json") -Target $embeddingTokenizer
if (Test-Path -LiteralPath $embeddingConfig -PathType Leaf) {
    New-HardLinkChecked -Path (Join-Path $runtimeOnnx "config.json") -Target $embeddingConfig
}
[void](New-Item -ItemType Junction -Path (Join-Path $runtimeOnnx "reranker") -Target $rerankerModelDirFull)

$policyCasesPath = Join-Path $PSScriptRoot "cases\policy_history_retrieval.jsonl"
$moduleCasesPath = Join-Path $PSScriptRoot "cases\effect_module_selection.jsonl"
$policyCases = @(Read-JsonLines -Path $policyCasesPath)
$moduleCases = @(Read-JsonLines -Path $moduleCasesPath)
Assert-EvaluationCases -Cases $policyCases -Kind "policy_history"
Assert-EvaluationCases -Cases $moduleCases -Kind "effect_module"

$benchmarkTexts = New-Object System.Collections.Generic.List[string]
foreach ($case in $policyCases) {
    if (-not [string]::IsNullOrWhiteSpace([string]$case.query)) {
        $benchmarkTexts.Add([string]$case.query)
    }
    foreach ($candidate in @($case.candidates)) {
        if (-not [string]::IsNullOrWhiteSpace([string]$candidate.text)) {
            $benchmarkTexts.Add([string]$candidate.text)
        }
    }
}
foreach ($case in $moduleCases) {
    if (-not [string]::IsNullOrWhiteSpace([string]$case.request)) {
        $benchmarkTexts.Add([string]$case.request)
    }
    foreach ($candidate in @($case.candidates)) {
        if (-not [string]::IsNullOrWhiteSpace([string]$candidate.text)) {
            $benchmarkTexts.Add([string]$candidate.text)
        }
    }
}
$baseTexts = @($benchmarkTexts | Select-Object -Unique)
if ($baseTexts.Count -lt 2) {
    throw "The evaluation fixtures do not contain enough benchmark text."
}

$corpusTexts = New-Object string[] $CorpusSize
for ($i = 0; $i -lt $CorpusSize; $i++) {
    $corpusTexts[$i] = $baseTexts[$i % $baseTexts.Count] + " [phase0-document-" + $i.ToString([System.Globalization.CultureInfo]::InvariantCulture) + "]"
}
$coldQueries = New-Object string[] $Iterations
for ($i = 0; $i -lt $Iterations; $i++) {
    $coldQueries[$i] = $baseTexts[$i % $baseTexts.Count] + " [phase0-cold-query-" + $runId + "-" + $i.ToString([System.Globalization.CultureInfo]::InvariantCulture) + "]"
}
$warmQueries = New-Object string[] $Iterations
for ($i = 0; $i -lt $Iterations; $i++) {
    $warmQueries[$i] = "phase0-warm-query-" + $runId
}
$rerankDocumentCount = [math]::Min($TopK, $corpusTexts.Length)
$rerankDocuments = New-Object string[] $rerankDocumentCount
[Array]::Copy($corpusTexts, $rerankDocuments, $rerankDocumentCount)

Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

public static class Phase0NativeLoader
{
    [System.Runtime.InteropServices.DllImport("kernel32", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Unicode)]
    public static extern IntPtr LoadLibrary(string path);
}

public delegate bool Phase0EmbeddingCall(string text, out float[] vector);
public delegate bool Phase0RerankCall(string query, IReadOnlyList<string> documents, out List<float> scores);
public delegate object Phase0DeserializeCall(string json);
public delegate object Phase0EffectLookupCall(object target, string effectId, string raw);

public sealed class Phase0Measurement
{
    public int Count { get; set; }
    public double P50Ms { get; set; }
    public double P95Ms { get; set; }
    public double MaxMs { get; set; }
    public long AllocatedBytes { get; set; }
    public double AllocatedBytesPerOperation { get; set; }
}

public sealed class Phase0IndexBuildResult
{
    public Phase0Measurement Measurement { get; set; }
    public float[][] Vectors { get; set; }
}

public static class Phase0BenchmarkRuntime
{
    public static Phase0DeserializeCall CreateDeserializer(Type jsonConvertType, Type dtoType)
    {
        MethodInfo method = jsonConvertType.GetMethod("DeserializeObject", new[] { typeof(string), typeof(Type) });
        if (method == null) throw new MissingMethodException(jsonConvertType.FullName, "DeserializeObject(string, Type)");
        ParameterExpression raw = Expression.Parameter(typeof(string), "raw");
        MethodCallExpression call = Expression.Call(method, raw, Expression.Constant(dtoType, typeof(Type)));
        return Expression.Lambda<Phase0DeserializeCall>(Expression.Convert(call, typeof(object)), raw).Compile();
    }

    public static Phase0EffectLookupCall CreateEffectLookup(MethodInfo method)
    {
        if (method == null) throw new ArgumentNullException(nameof(method));
        ParameterExpression target = Expression.Parameter(typeof(object), "target");
        ParameterExpression effectId = Expression.Parameter(typeof(string), "effectId");
        ParameterExpression raw = Expression.Parameter(typeof(string), "raw");
        MethodCallExpression call = Expression.Call(Expression.Convert(target, method.DeclaringType), method, effectId, raw);
        return Expression.Lambda<Phase0EffectLookupCall>(Expression.Convert(call, typeof(object)), target, effectId, raw).Compile();
    }

    public static Func<object> CreateFactory(Type type)
    {
        ConstructorInfo ctor = type.GetConstructor(Type.EmptyTypes);
        if (ctor == null) throw new MissingMethodException(type.FullName, ".ctor()");
        return Expression.Lambda<Func<object>>(Expression.Convert(Expression.New(ctor), typeof(object))).Compile();
    }

    private static long AllocatedNow()
    {
        return GC.GetAllocatedBytesForCurrentThread();
    }

    private static Phase0Measurement Summarize(double[] samples, long allocatedBytes)
    {
        double[] sorted = (double[])samples.Clone();
        Array.Sort(sorted);
        int p50 = (int)Math.Ceiling(sorted.Length * 0.50) - 1;
        int p95 = (int)Math.Ceiling(sorted.Length * 0.95) - 1;
        if (p50 < 0) p50 = 0;
        if (p95 < 0) p95 = 0;
        return new Phase0Measurement
        {
            Count = sorted.Length,
            P50Ms = sorted[p50],
            P95Ms = sorted[p95],
            MaxMs = sorted[sorted.Length - 1],
            AllocatedBytes = allocatedBytes,
            AllocatedBytesPerOperation = sorted.Length == 0 ? 0.0 : (double)allocatedBytes / sorted.Length
        };
    }

    public static Phase0Measurement MeasureEmbedding(Phase0EmbeddingCall call, string[] queries, string warmupQuery)
    {
        float[] ignored;
        if (!call(warmupQuery, out ignored)) throw new InvalidOperationException("Embedding warmup failed.");
        double[] samples = new double[queries.Length];
        long allocatedBefore = AllocatedNow();
        for (int i = 0; i < queries.Length; i++)
        {
            long start = Stopwatch.GetTimestamp();
            float[] vector;
            if (!call(queries[i], out vector) || vector == null || vector.Length == 0) throw new InvalidOperationException("Embedding query failed at index " + i);
            samples[i] = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
        }
        return Summarize(samples, AllocatedNow() - allocatedBefore);
    }

    public static Phase0IndexBuildResult BuildIndex(Phase0EmbeddingCall call, string[] documents)
    {
        float[][] vectors = new float[documents.Length][];
        long allocatedBefore = AllocatedNow();
        long start = Stopwatch.GetTimestamp();
        for (int i = 0; i < documents.Length; i++)
        {
            if (!call(documents[i], out vectors[i]) || vectors[i] == null || vectors[i].Length == 0) throw new InvalidOperationException("Index embedding failed at index " + i);
        }
        double elapsed = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
        return new Phase0IndexBuildResult { Measurement = Summarize(new[] { elapsed }, AllocatedNow() - allocatedBefore), Vectors = vectors };
    }

    public static float[] Embed(Phase0EmbeddingCall call, string text)
    {
        float[] vector;
        if (!call(text, out vector) || vector == null || vector.Length == 0)
            throw new InvalidOperationException("Dense recall query embedding failed.");
        return vector;
    }

    public static Phase0Measurement MeasureDenseRecall(float[] query, float[][] corpus, int topK, int iterations)
    {
        DenseTopK(query, corpus, topK);
        double[] samples = new double[iterations];
        long allocatedBefore = AllocatedNow();
        for (int n = 0; n < iterations; n++)
        {
            long start = Stopwatch.GetTimestamp();
            DenseTopK(query, corpus, topK);
            samples[n] = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
        }
        return Summarize(samples, AllocatedNow() - allocatedBefore);
    }

    private static int[] DenseTopK(float[] query, float[][] corpus, int topK)
    {
        int count = Math.Min(Math.Max(1, topK), corpus.Length);
        float[] bestScores = new float[count];
        int[] bestIndices = new int[count];
        for (int i = 0; i < count; i++) { bestScores[i] = float.NegativeInfinity; bestIndices[i] = -1; }
        for (int i = 0; i < corpus.Length; i++)
        {
            float[] vector = corpus[i];
            int length = Math.Min(query.Length, vector.Length);
            float score = 0f;
            for (int j = 0; j < length; j++) score += query[j] * vector[j];
            int insert = count;
            for (int j = 0; j < count; j++) if (score > bestScores[j]) { insert = j; break; }
            if (insert >= count) continue;
            for (int j = count - 1; j > insert; j--) { bestScores[j] = bestScores[j - 1]; bestIndices[j] = bestIndices[j - 1]; }
            bestScores[insert] = score;
            bestIndices[insert] = i;
        }
        return bestIndices;
    }

    public static Phase0Measurement MeasureRerank(Phase0RerankCall call, string[] queries, string[] documents, string warmupQuery)
    {
        List<float> ignored;
        if (!call(warmupQuery, documents, out ignored)) throw new InvalidOperationException("Reranker warmup failed.");
        double[] samples = new double[queries.Length];
        long allocatedBefore = AllocatedNow();
        for (int i = 0; i < queries.Length; i++)
        {
            long start = Stopwatch.GetTimestamp();
            List<float> scores;
            if (!call(queries[i], documents, out scores) || scores == null || scores.Count != documents.Length) throw new InvalidOperationException("Reranker query failed at index " + i);
            samples[i] = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
        }
        return Summarize(samples, AllocatedNow() - allocatedBefore);
    }

    public static Phase0Measurement MeasureDeserialize(Phase0DeserializeCall call, string[] raws, int iterations)
    {
        object ignored = call(raws[0]);
        if (ignored == null) throw new InvalidOperationException("JSON warmup failed.");
        double[] samples = new double[iterations];
        long allocatedBefore = AllocatedNow();
        for (int i = 0; i < iterations; i++)
        {
            long start = Stopwatch.GetTimestamp();
            object value = call(raws[i % raws.Length]);
            if (value == null) throw new InvalidOperationException("JSON deserialize returned null.");
            samples[i] = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
        }
        return Summarize(samples, AllocatedNow() - allocatedBefore);
    }

    public static Phase0Measurement MeasureGetterScanCore(Phase0DeserializeCall call, string[] raws, int iterations, bool warmCache)
    {
        Dictionary<string, object> sharedCache = warmCache ? new Dictionary<string, object>(StringComparer.Ordinal) : null;
        if (warmCache) for (int i = 0; i < raws.Length; i++) sharedCache[raws[i]] = call(raws[i]);
        double[] samples = new double[iterations];
        long allocatedBefore = AllocatedNow();
        int sink = 0;
        for (int n = 0; n < iterations; n++)
        {
            Dictionary<string, object> cache = warmCache ? sharedCache : new Dictionary<string, object>(StringComparer.Ordinal);
            long start = Stopwatch.GetTimestamp();
            string[] snapshot = (string[])raws.Clone();
            for (int i = 0; i < snapshot.Length; i++)
            {
                object value;
                if (!cache.TryGetValue(snapshot[i], out value)) { value = call(snapshot[i]); cache[snapshot[i]] = value; }
                if (value != null) sink ^= System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(value);
            }
            samples[n] = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
        }
        GC.KeepAlive(sink);
        return Summarize(samples, AllocatedNow() - allocatedBefore);
    }

    public static Phase0Measurement MeasureDailyLookupCore(Func<object> factory, Phase0EffectLookupCall lookup, string[] raws, int iterations, bool warmCache)
    {
        object sharedTarget = warmCache ? factory() : null;
        if (warmCache) for (int i = 0; i < raws.Length; i++) lookup(sharedTarget, "effect-" + i, raws[i]);
        double[] samples = new double[iterations];
        long allocatedBefore = AllocatedNow();
        for (int n = 0; n < iterations; n++)
        {
            object target = warmCache ? sharedTarget : factory();
            long start = Stopwatch.GetTimestamp();
            for (int i = 0; i < raws.Length; i++)
            {
                object value = lookup(target, "effect-" + i, raws[i]);
                if (value == null) throw new InvalidOperationException("Effect lookup returned null.");
            }
            samples[n] = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
        }
        return Summarize(samples, AllocatedNow() - allocatedBefore);
    }
}
'@

$nativeHandle = [Phase0NativeLoader]::LoadLibrary((Join-Path $runtimeBin "onnxruntime.dll"))
if ($nativeHandle -eq [IntPtr]::Zero) {
    throw "Failed to load onnxruntime.dll, Win32Error=$([System.Runtime.InteropServices.Marshal]::GetLastWin32Error())"
}
$env:PATH = $runtimeBin + ";" + $referenceDirFull + ";" + $env:PATH
Set-Location -LiteralPath $runtimeBin

$preloaded = 0
foreach ($file in Get-ChildItem -LiteralPath $referenceDirFull -File -Filter "*.dll") {
    if ([string]::Equals($file.Name, "AnimusForge.dll", [StringComparison]::OrdinalIgnoreCase)) {
        continue
    }
    try {
        [void][Reflection.Assembly]::LoadFrom($file.FullName)
        $preloaded++
    }
    catch {
    }
}

$implementationPath = Join-Path $runtimeImplementationDir "AnimusForge.dll"
$assembly = [Reflection.Assembly]::LoadFrom($implementationPath)
$embeddingType = $assembly.GetType("AnimusForge.OnnxEmbeddingEngine", $true)
$embeddingInstance = $embeddingType.GetProperty("Instance", [Reflection.BindingFlags]"Public,Static").GetValue($null, $null)
if (-not [bool]$embeddingType.GetProperty("IsAvailable").GetValue($embeddingInstance, $null)) {
    throw "Existing embedding engine unavailable: $($embeddingType.GetProperty('LastError').GetValue($embeddingInstance, $null))"
}
$embeddingCall = [Delegate]::CreateDelegate([Phase0EmbeddingCall], $embeddingInstance, $embeddingType.GetMethod("TryGetEmbedding"))

$rerankerType = $assembly.GetType("AnimusForge.OnnxCrossEncoderReranker", $true)
$rerankerInstance = $rerankerType.GetProperty("Instance", [Reflection.BindingFlags]"Public,Static").GetValue($null, $null)
if (-not [bool]$rerankerType.GetProperty("IsAvailable").GetValue($rerankerInstance, $null)) {
    throw "Existing reranker unavailable: $($rerankerType.GetProperty('LastError').GetValue($rerankerInstance, $null))"
}
$rerankCall = [Delegate]::CreateDelegate([Phase0RerankCall], $rerankerInstance, $rerankerType.GetMethod("TryScoreBatch"))

$jsonAssembly = [AppDomain]::CurrentDomain.GetAssemblies() | Where-Object { $_.GetName().Name -eq "Newtonsoft.Json" } | Select-Object -First 1
if ($null -eq $jsonAssembly) {
    throw "Newtonsoft.Json was not loaded from the verified reference directory."
}
$jsonConvertType = $jsonAssembly.GetType("Newtonsoft.Json.JsonConvert", $true)
$behaviorType = $assembly.GetType("AnimusForge.CustomPolicyBehavior", $true)
$effectDtoType = $behaviorType.GetNestedType("ActivePolicyEffectSaveData", [Reflection.BindingFlags]"NonPublic")
$deserializeCall = [Phase0BenchmarkRuntime]::CreateDeserializer($jsonConvertType, $effectDtoType)
$effectLookupMethod = $behaviorType.GetMethod(
    "GetActivePolicyEffectForWork",
    [Reflection.BindingFlags]"Instance,NonPublic",
    $null,
    [Type[]]@([string], [string]),
    $null)
if ($null -eq $effectLookupMethod) {
    throw "CustomPolicyBehavior.GetActivePolicyEffectForWork(string, string) was not found."
}
$effectLookupCall = [Phase0BenchmarkRuntime]::CreateEffectLookup($effectLookupMethod)
$behaviorFactory = [Phase0BenchmarkRuntime]::CreateFactory($behaviorType)

$rawEffects = New-Object string[] $CorpusSize
for ($i = 0; $i -lt $CorpusSize; $i++) {
    $rawEffects[$i] = ([ordered]@{
        Version = 4
        ScopeKind = "local"
        LocalTargetScope = "fief"
        TargetHandle = "settlement:town_" + $i
        TargetLabel = "BaselineTown" + $i
        TargetFiefIds = @("town_" + $i)
        TargetSettlementIds = @("town_" + $i)
        TargetClanIds = @("clan_" + ($i % 4))
        DirectTargetSettlementIds = @("town_" + $i)
        FollowCurrentRulingClan = $false
        EffectId = "effect-" + $i
        RecordId = "record-" + $i
        PolicyName = "Phase0 baseline policy " + $i
        SubmittedDay = 100
        CreatedUtcTicks = 638900000000000000 + $i
        TargetKingdomId = "kingdom_west"
        TargetKingdomName = "West"
        ProsperityDailyDeltaPerTown = 0.1
        FoodDailyDeltaPerTown = 0.2
        HearthDailyDeltaPerVillage = 0.1
        LoyaltyDailyDeltaPerTown = 0.05
        SecurityDailyDeltaPerTown = 0.05
        MilitiaDailyDeltaPerTown = 0.1
        TownTaxPercent = 1.0
        ConstructionSpeedPercent = 1.0
        KingdomStabilityDailyDelta = 0
        TotalDurationDays = 30
        RemainingDays = 20
        LastAppliedDay = 99
        Reason = "phase0 synthetic measurement fixture"
        Ended = $false
    } | ConvertTo-Json -Depth 5 -Compress)
}

$embeddingCold = [Phase0BenchmarkRuntime]::MeasureEmbedding($embeddingCall, $coldQueries, "phase0-embedding-warmup-" + $runId)
$embeddingWarm = [Phase0BenchmarkRuntime]::MeasureEmbedding($embeddingCall, $warmQueries, $warmQueries[0])
$indexBuildCold = [Phase0BenchmarkRuntime]::BuildIndex($embeddingCall, $corpusTexts)
$indexBuildWarmSamples = New-Object System.Collections.Generic.List[object]
for ($i = 0; $i -lt $Iterations; $i++) {
    $indexBuildWarmSamples.Add([Phase0BenchmarkRuntime]::BuildIndex($embeddingCall, $corpusTexts).Measurement)
}
$indexWarmP50 = @($indexBuildWarmSamples | ForEach-Object { $_.P50Ms } | Sort-Object)[[math]::Ceiling($Iterations * 0.50) - 1]
$indexWarmP95 = @($indexBuildWarmSamples | ForEach-Object { $_.P50Ms } | Sort-Object)[[math]::Ceiling($Iterations * 0.95) - 1]
$indexWarmMax = @($indexBuildWarmSamples | ForEach-Object { $_.P50Ms } | Measure-Object -Maximum).Maximum
$indexWarmAllocated = [long](@($indexBuildWarmSamples | Measure-Object -Property AllocatedBytes -Sum).Sum)
$indexBuildWarm = [pscustomobject]@{
    Count = $Iterations
    P50Ms = [double]$indexWarmP50
    P95Ms = [double]$indexWarmP95
    MaxMs = [double]$indexWarmMax
    AllocatedBytes = $indexWarmAllocated
    AllocatedBytesPerOperation = [double]$indexWarmAllocated / $Iterations
}

$denseQueryVector = [Phase0BenchmarkRuntime]::Embed($embeddingCall, "phase0-dense-query-" + $runId)
$denseRecall = [Phase0BenchmarkRuntime]::MeasureDenseRecall($denseQueryVector, $indexBuildCold.Vectors, $TopK, $Iterations)
$rerankCold = [Phase0BenchmarkRuntime]::MeasureRerank($rerankCall, $coldQueries, $rerankDocuments, "phase0-rerank-warmup-" + $runId)
$rerankWarm = [Phase0BenchmarkRuntime]::MeasureRerank($rerankCall, $warmQueries, $rerankDocuments, $warmQueries[0])
$jsonDeserialize = [Phase0BenchmarkRuntime]::MeasureDeserialize($deserializeCall, $rawEffects, $Iterations)
$getterCold = [Phase0BenchmarkRuntime]::MeasureGetterScanCore($deserializeCall, $rawEffects, $Iterations, $false)
$getterWarm = [Phase0BenchmarkRuntime]::MeasureGetterScanCore($deserializeCall, $rawEffects, $Iterations, $true)
$dailyCold = [Phase0BenchmarkRuntime]::MeasureDailyLookupCore($behaviorFactory, $effectLookupCall, $rawEffects, $Iterations, $false)
$dailyWarm = [Phase0BenchmarkRuntime]::MeasureDailyLookupCore($behaviorFactory, $effectLookupCall, $rawEffects, $Iterations, $true)

$cpu = $null
$memoryBytes = $null
try {
    $cpu = (Get-CimInstance Win32_Processor | Select-Object -First 1 -ExpandProperty Name).Trim()
    $memoryBytes = [long](Get-CimInstance Win32_ComputerSystem | Select-Object -ExpandProperty TotalPhysicalMemory)
}
catch {
    $cpu = $env:PROCESSOR_IDENTIFIER
}

$report = [ordered]@{
    schema_version = 1
    collected_utc = [DateTime]::UtcNow.ToString("o")
    run_id = $runId
    scope = "Phase 0 read-only baseline; no production prompt or save write"
    implementation = [ordered]@{
        version = $ImplementationVersion
        assembly_path = $stageImplementation
        assembly_sha256 = (Get-FileHash -LiteralPath $stageImplementation -Algorithm SHA256).Hash.ToLowerInvariant()
        reference_dir = $referenceDirFull
        preloaded_reference_assemblies = $preloaded
    }
    hardware = [ordered]@{
        cpu = $cpu
        logical_processor_count = [Environment]::ProcessorCount
        physical_memory_bytes = $memoryBytes
        os = [Environment]::OSVersion.VersionString
        process_64_bit = [Environment]::Is64BitProcess
        clr = [Environment]::Version.ToString()
    }
    models = [ordered]@{
        embedding_model_sha256 = (Get-FileHash -LiteralPath $embeddingModel -Algorithm SHA256).Hash.ToLowerInvariant()
        embedding_model_data_sha256 = $(if (Test-Path -LiteralPath $embeddingModelData -PathType Leaf) { (Get-FileHash -LiteralPath $embeddingModelData -Algorithm SHA256).Hash.ToLowerInvariant() } else { $null })
        embedding_tokenizer_sha256 = (Get-FileHash -LiteralPath $embeddingTokenizer -Algorithm SHA256).Hash.ToLowerInvariant()
        reranker_model_sha256 = (Get-FileHash -LiteralPath $rerankerModel -Algorithm SHA256).Hash.ToLowerInvariant()
        reranker_tokenizer_sha256 = (Get-FileHash -LiteralPath $rerankerTokenizer -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    scale = [ordered]@{
        iterations = $Iterations
        policy_history_case_count = $policyCases.Count
        effect_module_case_count = $moduleCases.Count
        corpus_document_count = $CorpusSize
        active_effect_count = $CorpusSize
        top_k = $TopK
        rerank_document_count = $rerankDocumentCount
        embedding_dimension = $denseQueryVector.Length
    }
    evaluation_sets = [ordered]@{
        policy_history_retrieval = [ordered]@{
            path = $policyCasesPath
            sha256 = (Get-FileHash -LiteralPath $policyCasesPath -Algorithm SHA256).Hash.ToLowerInvariant()
            case_count = $policyCases.Count
        }
        effect_module_selection = [ordered]@{
            path = $moduleCasesPath
            sha256 = (Get-FileHash -LiteralPath $moduleCasesPath -Algorithm SHA256).Hash.ToLowerInvariant()
            case_count = $moduleCases.Count
        }
    }
    measurements = [ordered]@{
        getter_scan_core_cold = Get-PercentileSummary -Measurement $getterCold
        getter_scan_core_warm = Get-PercentileSummary -Measurement $getterWarm
        daily_effect_lookup_core_cold = Get-PercentileSummary -Measurement $dailyCold
        daily_effect_lookup_core_warm = Get-PercentileSummary -Measurement $dailyWarm
        json_deserialize = Get-PercentileSummary -Measurement $jsonDeserialize
        index_build_cold = Get-PercentileSummary -Measurement $indexBuildCold.Measurement
        index_build_warm = Get-PercentileSummary -Measurement $indexBuildWarm
        embedding_cold = Get-PercentileSummary -Measurement $embeddingCold
        embedding_warm = Get-PercentileSummary -Measurement $embeddingWarm
        dense_recall = Get-PercentileSummary -Measurement $denseRecall
        batch_rerank_cold = Get-PercentileSummary -Measurement $rerankCold
        batch_rerank_warm = Get-PercentileSummary -Measurement $rerankWarm
    }
    boundaries = @(
        "getter_scan_core measures the current full snapshot + JSON-cache scan shape without TaleWorlds settlement filtering or explanation creation",
        "daily_effect_lookup_core invokes the actual GetActivePolicyEffectForWork cache/deserializer but not target expansion or settlement application",
        "allocation values use GC.GetAllocatedBytesForCurrentThread around the measured call and include returned result allocations",
        "dense_recall is exact dot-product Top-K over the vectors returned by the existing embedding engine",
        "embedding and reranker calls use the existing AnimusForge engines; unavailable models fail the run instead of falling back"
    )
}

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    $archiveRoot = Split-Path -Parent $PSScriptRoot
    $OutputPath = Join-Path $archiveRoot ("reports\baseline_" + $runId + ".json")
}
$outputPathFull = Get-FullPath -Path $OutputPath
$outputDirectory = Split-Path -Parent $outputPathFull
[void](New-Item -ItemType Directory -Path $outputDirectory -Force)
$report | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $outputPathFull -Encoding UTF8
Write-Output "Phase 0 baseline report: $outputPathFull"
Write-Output ($report | ConvertTo-Json -Depth 8 -Compress)

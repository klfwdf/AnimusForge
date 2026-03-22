using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AnimusForge;

internal static class RagWarmupCoordinator
{
	private static int _warmupState;

	public static void TryStartBackgroundWarmup(string source)
	{
		if (Interlocked.CompareExchange(ref _warmupState, 1, 0) != 0)
		{
			return;
		}
		string warmupSource = string.IsNullOrWhiteSpace(source) ? "unknown" : source.Trim();
		Logger.Log("RagWarmup", "start source=" + warmupSource);
		Task.Run((Action)delegate
		{
			RunWarmup(warmupSource);
		});
	}

	private static void RunWarmup(string source)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		bool embeddingOk = false;
		bool rerankerOk = false;
		string embeddingError = "";
		string rerankerError = "";
		try
		{
			embeddingOk = OnnxEmbeddingEngine.Instance.TryGetEmbedding("warmup", out var _);
			embeddingError = OnnxEmbeddingEngine.Instance.LastError ?? "";
		}
		catch (Exception ex)
		{
			embeddingError = ex.Message ?? "embedding warmup exception";
		}
		try
		{
			rerankerOk = OnnxCrossEncoderReranker.Instance.TryScore("warmup", "warmup", out var _);
			rerankerError = OnnxCrossEncoderReranker.Instance.LastError ?? "";
		}
		catch (Exception ex2)
		{
			rerankerError = ex2.Message ?? "reranker warmup exception";
		}
		stopwatch.Stop();
		Interlocked.Exchange(ref _warmupState, 2);
		if (embeddingOk)
		{
			try
			{
				KnowledgeLibraryBehavior.TryStartBackgroundIndexWarmup("rag_warmup_complete");
			}
			catch
			{
			}
			try
			{
				AIConfigHandler.TryStartBackgroundSemanticWarmup("rag_warmup_complete");
			}
			catch
			{
			}
		}
		Logger.Log("RagWarmup", $"complete source={source} ms={Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2)} embeddingOk={embeddingOk} rerankerOk={rerankerOk} embeddingError={embeddingError} rerankerError={rerankerError}");
	}
}

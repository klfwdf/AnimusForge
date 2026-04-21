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
		if (Interlocked.CompareExchange(ref _warmupState, 1, 0) == 0)
		{
			string warmupSource = (string.IsNullOrWhiteSpace(source) ? "unknown" : source.Trim());
			Logger.Log("RagWarmup", "start source=" + warmupSource);
			Task.Run(delegate
			{
				RunWarmup(warmupSource);
			});
		}
	}

	private static void RunWarmup(string source)
	{
		Stopwatch stopwatch = Stopwatch.StartNew();
		bool flag = false;
		bool flag2 = false;
		string text = "";
		string text2 = "";
		try
		{
			flag = OnnxEmbeddingEngine.Instance.TryGetEmbedding("warmup", out var _);
			text = OnnxEmbeddingEngine.Instance.LastError ?? "";
		}
		catch (Exception ex)
		{
			text = ex.Message ?? "embedding warmup exception";
		}
		try
		{
			flag2 = OnnxCrossEncoderReranker.Instance.TryScore("warmup", "warmup", out var _);
			text2 = OnnxCrossEncoderReranker.Instance.LastError ?? "";
		}
		catch (Exception ex2)
		{
			text2 = ex2.Message ?? "reranker warmup exception";
		}
		stopwatch.Stop();
		Interlocked.Exchange(ref _warmupState, 2);
		if (flag)
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
		Logger.Log("RagWarmup", $"complete source={source} ms={Math.Round(stopwatch.Elapsed.TotalMilliseconds, 2)} embeddingOk={flag} rerankerOk={flag2} embeddingError={text} rerankerError={text2}");
	}
}

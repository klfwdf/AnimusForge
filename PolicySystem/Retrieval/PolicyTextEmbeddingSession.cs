using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace AnimusForge;

internal sealed class PolicyTextEmbeddingSession
{
	private const int QueryCacheCapacity = 64;
	private readonly object _sync = new object();
	private readonly Func<string, float[]> _embeddingProvider;
	private readonly string _baseFingerprint;
	private readonly Dictionary<string, float[]> _vectors = new Dictionary<string, float[]>(StringComparer.Ordinal);
	private int _dimension;

	internal PolicyTextEmbeddingSession()
		: this(null, null)
	{
	}

	internal PolicyTextEmbeddingSession(Func<string, float[]> embeddingProvider, string fingerprint)
	{
		_embeddingProvider = embeddingProvider;
		OnnxEmbeddingEngine engine = embeddingProvider == null ? OnnxEmbeddingEngine.Instance : null;
		_baseFingerprint = string.IsNullOrWhiteSpace(fingerprint)
			? (embeddingProvider == null
				? "onnx-" + RuntimeHelpers.GetHashCode(engine).ToString("x8", CultureInfo.InvariantCulture)
				: "provider-" + RuntimeHelpers.GetHashCode(embeddingProvider).ToString("x8", CultureInfo.InvariantCulture))
			: fingerprint.Trim();
	}

	internal string EmbeddingFingerprint
	{
		get
		{
			lock (_sync)
			{
				return _baseFingerprint + ":d" + Math.Max(0, _dimension).ToString(CultureInfo.InvariantCulture);
			}
		}
	}

	internal int Dimension
	{
		get
		{
			lock (_sync)
			{
				return _dimension;
			}
		}
	}

	internal float[] GetEmbedding(string text)
	{
		string normalized = (text ?? string.Empty).Trim();
		if (normalized.Length == 0)
		{
			throw new InvalidOperationException("政策文本 embedding 输入为空。");
		}
		string cacheKey = _baseFingerprint + ":" + StableTextHash(normalized);
		lock (_sync)
		{
			if (_vectors.TryGetValue(cacheKey, out float[] cached))
			{
				return cached;
			}
		}

		float[] vector;
		if (_embeddingProvider != null)
		{
			vector = _embeddingProvider(normalized);
		}
		else
		{
			OnnxEmbeddingEngine engine = OnnxEmbeddingEngine.Instance;
			if (engine == null || !engine.IsAvailable || !engine.TryGetEmbedding(normalized, out vector))
			{
				throw new InvalidOperationException("政策文本 ONNX embedding 不可用：" + (engine?.LastError ?? "unknown"));
			}
		}
		if (vector == null || vector.Length == 0)
		{
			throw new InvalidOperationException("政策文本 embedding 结果为空。");
		}
		lock (_sync)
		{
			if (_dimension == 0)
			{
				_dimension = vector.Length;
			}
			else if (_dimension != vector.Length)
			{
				_vectors.Clear();
				throw new InvalidOperationException("政策文本 embedding 维度在同一请求中发生变化。");
			}
			if (_vectors.Count >= QueryCacheCapacity)
			{
				_vectors.Clear();
			}
			_vectors[cacheKey] = vector;
		}
		return vector;
	}

	internal static string StableTextHash(string text)
	{
		ulong hash = 14695981039346656037UL;
		foreach (byte value in Encoding.UTF8.GetBytes(text ?? string.Empty))
		{
			hash ^= value;
			hash *= 1099511628211UL;
		}
		return hash.ToString("x16", CultureInfo.InvariantCulture);
	}
}

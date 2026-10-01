using System;
using System.Collections.Concurrent;
using System.Text;

namespace AnimusForge;

internal static class PromptComposer
{
	private static readonly ConcurrentDictionary<string, string> _fixedLayerCache = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

	public static string GetOrBuildFixedLayer(string cacheKey, bool allowCache, Func<string> builder, out bool cacheHit)
	{
		if (allowCache && !string.IsNullOrEmpty(cacheKey) && _fixedLayerCache.TryGetValue(cacheKey, out var value))
		{
			cacheHit = true;
			return value;
		}
		cacheHit = false;
		string text = builder();
		if (allowCache && !string.IsNullOrEmpty(cacheKey))
		{
			_fixedLayerCache[cacheKey] = text;
		}
		return text;
	}

	public static string Compose(string fixedLayer, string deltaLayer, string modeLabel)
	{
		if (string.IsNullOrEmpty(deltaLayer))
		{
			return fixedLayer ?? string.Empty;
		}
		if (string.IsNullOrEmpty(fixedLayer))
		{
			return deltaLayer;
		}
		StringBuilder stringBuilder = new StringBuilder(fixedLayer.Length + deltaLayer.Length + 2);
		stringBuilder.Append(fixedLayer);
		if (!fixedLayer.EndsWith("\n"))
		{
			stringBuilder.AppendLine();
		}
		stringBuilder.Append(deltaLayer);
		return stringBuilder.ToString();
	}

	public static int CountChars(string text)
	{
		return text?.Length ?? 0;
	}

	public static int GetFixedLayerCacheSize()
	{
		return _fixedLayerCache.Count;
	}

	public static void InvalidateFixedLayer(string cacheKeyPrefix)
	{
		if (string.IsNullOrEmpty(cacheKeyPrefix))
		{
			return;
		}
		foreach (string key in _fixedLayerCache.Keys)
		{
			if (key.StartsWith(cacheKeyPrefix, StringComparison.Ordinal))
			{
				_fixedLayerCache.TryRemove(key, out var _);
			}
		}
	}

	public static void ClearAllFixedLayerCache()
	{
		_fixedLayerCache.Clear();
	}
}

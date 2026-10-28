using System;
using System.Collections.Concurrent;

namespace MCM.Abstractions.Base.PerSave;

public abstract class PerSaveSettings<T> : PerSaveSettings where T : PerSaveSettings, new()
{
	public static T? Instance
	{
		get
		{
			if (!PerSaveSettings.Cache.ContainsKey(typeof(T)))
			{
				PerSaveSettings.Cache.TryAdd(typeof(T), new T().Id);
			}
			return BaseSettingsProvider.Instance?.GetSettings(PerSaveSettings.Cache[typeof(T)]) as T;
		}
	}
}
public abstract class PerSaveSettings : BaseSettings
{
	protected static readonly ConcurrentDictionary<Type, string> Cache = new ConcurrentDictionary<Type, string>();

	protected static readonly ConcurrentDictionary<string, BaseSettings?> CacheInstance = new ConcurrentDictionary<string, BaseSettings>();

	public sealed override string FormatType { get; } = "json2";

	internal static void InvalidateCache()
	{
		Cache.Clear();
		CacheInstance.Clear();
	}
}

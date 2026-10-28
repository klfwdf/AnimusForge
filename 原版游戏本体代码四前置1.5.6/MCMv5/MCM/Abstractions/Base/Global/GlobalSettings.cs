using System;
using System.Collections.Concurrent;

namespace MCM.Abstractions.Base.Global;

public abstract class GlobalSettings<T> : GlobalSettings where T : GlobalSettings, new()
{
	public static T? Instance
	{
		get
		{
			if (!GlobalSettings.Cache.ContainsKey(typeof(T)))
			{
				GlobalSettings.Cache.TryAdd(typeof(T), new T().Id);
			}
			return BaseSettingsProvider.Instance?.GetSettings(GlobalSettings.Cache[typeof(T)]) as T;
		}
	}
}
public abstract class GlobalSettings : BaseSettings
{
	protected static readonly ConcurrentDictionary<Type, string> Cache = new ConcurrentDictionary<Type, string>();

	protected static readonly ConcurrentDictionary<string, BaseSettings?> CacheInstance = new ConcurrentDictionary<string, BaseSettings>();

	internal static void InvalidateCache()
	{
		Cache.Clear();
		CacheInstance.Clear();
	}
}

using System;
using System.Collections.Concurrent;

namespace MCM.Abstractions.Base.PerCampaign;

public abstract class PerCampaignSettings<T> : PerCampaignSettings where T : PerCampaignSettings, new()
{
	public static T? Instance
	{
		get
		{
			if (!PerCampaignSettings.Cache.ContainsKey(typeof(T)))
			{
				PerCampaignSettings.Cache.TryAdd(typeof(T), new T().Id);
			}
			return BaseSettingsProvider.Instance?.GetSettings(PerCampaignSettings.Cache[typeof(T)]) as T;
		}
	}
}
public abstract class PerCampaignSettings : BaseSettings
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

using System;
using System.Collections.Concurrent;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using TaleWorlds.MountAndBlade;

namespace MCM;

public class SettingsProviderMissionBehavior : MissionBehavior
{
	private readonly BaseSettingsProvider? _baseSettingsProvider;

	private readonly ConcurrentDictionary<Type, string> _cache = new ConcurrentDictionary<Type, string>();

	public override MissionBehaviorType BehaviorType => MissionBehaviorType.Other;

	public SettingsProviderMissionBehavior(BaseSettingsProvider? baseSettingsProvider)
	{
		_baseSettingsProvider = baseSettingsProvider;
	}

	public TSettings? Get<TSettings>() where TSettings : BaseSettings, new()
	{
		if (!_cache.ContainsKey(typeof(TSettings)))
		{
			_cache.TryAdd(typeof(TSettings), new TSettings().Id);
		}
		return _baseSettingsProvider?.GetSettings(_cache[typeof(TSettings)]) as TSettings;
	}
}

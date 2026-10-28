using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib.BUTR.Extensions;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Properties;
using MCM.Abstractions.Wrapper;
using MCM.Common;

namespace MCM.UI.Adapter.MCMv5.Properties;

public sealed class MCMv5FluentSettingsPropertyDiscoverer : IAttributeSettingsPropertyDiscoverer, ISettingsPropertyDiscoverer
{
	private delegate IEnumerable? GetSettingPropertyGroupsDelegate(object instance);

	private static readonly ConcurrentDictionary<Type, GetSettingPropertyGroupsDelegate?> _cache = new ConcurrentDictionary<Type, GetSettingPropertyGroupsDelegate>();

	public IEnumerable<string> DiscoveryTypes { get; } = new string[1] { "mcm_v5_fluent" };

	public IEnumerable<ISettingsPropertyDefinition> GetProperties(BaseSettings settings)
	{
		if (1 == 0)
		{
		}
		object obj2;
		if (settings is IWrapper wrapper)
		{
			object obj = wrapper.Object;
			if (obj != null)
			{
				obj2 = obj;
				goto IL_002a;
			}
		}
		obj2 = settings;
		goto IL_002a;
		IL_002a:
		if (1 == 0)
		{
		}
		object obj3 = obj2;
		List<SettingsPropertyGroupDefinition> source = _cache.GetOrAdd(obj3.GetType(), (Type x) => HarmonyLib.BUTR.Extensions.AccessTools2.GetPropertyGetterDelegate<GetSettingPropertyGroupsDelegate>(x, "SettingPropertyGroups"))?.Invoke(obj3)?.Cast<object>().Select((object x) => new SettingsPropertyGroupDefinitionWrapper(x)).Cast<SettingsPropertyGroupDefinition>()
			.ToList() ?? new List<SettingsPropertyGroupDefinition>();
		return source.SelectMany(SettingsUtils.GetAllSettingPropertyDefinitions);
	}
}

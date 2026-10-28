using System.Collections.Generic;
using System.Linq;
using HarmonyLib.BUTR.Extensions;
using MCM.Abstractions;
using MCM.Abstractions.Base;
using MCM.Abstractions.Properties;
using MCM.Abstractions.Wrapper;

namespace MCM.Implementation;

internal sealed class FluentSettingsPropertyDiscoverer : IFluentSettingsPropertyDiscoverer, ISettingsPropertyDiscoverer
{
	private delegate List<SettingsPropertyGroupDefinition> GetSettingPropertyGroupsDelegate(object instance);

	private static readonly GetSettingPropertyGroupsDelegate? _getSettingPropertyGroups = AccessTools2.GetPropertyGetterDelegate<GetSettingPropertyGroupsDelegate>("MCM.Abstractions.Base.IFluentSettings:SettingPropertyGroups");

	public IEnumerable<string> DiscoveryTypes { get; } = new string[1] { "fluent" };

	public IEnumerable<ISettingsPropertyDefinition> GetProperties(BaseSettings settings)
	{
		List<SettingsPropertyGroupDefinition> source = _getSettingPropertyGroups?.Invoke(settings).Select((SettingsPropertyGroupDefinition x) => new SettingsPropertyGroupDefinitionWrapper(x)).Cast<SettingsPropertyGroupDefinition>()
			.ToList() ?? new List<SettingsPropertyGroupDefinition>();
		return source.SelectMany(SettingsUtils.GetAllSettingPropertyDefinitions);
	}
}

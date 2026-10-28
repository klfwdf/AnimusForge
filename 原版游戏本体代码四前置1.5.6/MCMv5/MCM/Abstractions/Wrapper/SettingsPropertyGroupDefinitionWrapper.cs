using System.Collections;
using System.Collections.Generic;
using HarmonyLib.BUTR.Extensions;

namespace MCM.Abstractions.Wrapper;

public sealed class SettingsPropertyGroupDefinitionWrapper : SettingsPropertyGroupDefinition
{
	private static string? GetGroupName(object @object)
	{
		return @object.GetType().GetProperty("GroupName")?.GetValue(@object) as string;
	}

	private static string? GetGroupNameOverride(object @object)
	{
		return @object.GetType().GetProperty("GroupNameOverride")?.GetValue(@object) as string;
	}

	private static int? GetGroupOrder(object @object)
	{
		return @object.GetType().GetProperty("Order")?.GetValue(@object) as int?;
	}

	private static char? GetSubGroupDelimiter(object @object)
	{
		return @object.GetType().GetProperty("SubGroupDelimiter")?.GetValue(@object) as char?;
	}

	public SettingsPropertyGroupDefinitionWrapper(object @object)
		: base(GetGroupName(@object) ?? "ERROR", GetGroupOrder(@object) ?? (-1))
	{
		SetSubGroupDelimiter(GetSubGroupDelimiter(@object) ?? '/');
		subGroups.AddRange(GetSubGroups(@object).SortDefault());
		settingProperties.AddRange(GetSettingProperties(@object).SortDefault());
	}

	private static IEnumerable<SettingsPropertyGroupDefinition> GetSubGroups(object @object)
	{
		object obj = (AccessTools2.Property(@object.GetType(), "SubGroups") ?? AccessTools2.Property(@object.GetType(), "SettingPropertyGroups"))?.GetValue(@object);
		if (!(obj is IEnumerable list))
		{
			yield break;
		}
		foreach (object obj2 in list)
		{
			yield return new SettingsPropertyGroupDefinitionWrapper(obj2);
		}
	}

	private static IEnumerable<ISettingsPropertyDefinition> GetSettingProperties(object @object)
	{
		object obj = AccessTools2.Property(@object.GetType(), "SettingProperties")?.GetValue(@object);
		if (!(obj is IEnumerable list))
		{
			yield break;
		}
		foreach (object obj2 in list)
		{
			yield return new SettingsPropertyDefinitionWrapper(obj2);
		}
	}
}

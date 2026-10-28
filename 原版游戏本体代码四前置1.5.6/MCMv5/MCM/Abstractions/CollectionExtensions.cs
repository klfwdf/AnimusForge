using System.Collections.Generic;
using System.Linq;
using Bannerlord.ModuleManager;
using MCM.Common;

namespace MCM.Abstractions;

internal static class CollectionExtensions
{
	public static IOrderedEnumerable<SettingsPropertyGroupDefinition> SortDefault(this IEnumerable<SettingsPropertyGroupDefinition> enumerable)
	{
		return (from x in enumerable
			orderby x.GroupName == SettingsPropertyGroupDefinition.DefaultGroupName descending, x.Order descending
			select x).ThenByDescending<SettingsPropertyGroupDefinition, string>((SettingsPropertyGroupDefinition x) => LocalizationUtils.Localize(x.GroupName), new AlphanumComparatorFast());
	}

	public static IOrderedEnumerable<ISettingsPropertyDefinition> SortDefault(this IEnumerable<ISettingsPropertyDefinition> enumerable)
	{
		return enumerable.OrderBy((ISettingsPropertyDefinition x) => x.Order).ThenBy<ISettingsPropertyDefinition, string>((ISettingsPropertyDefinition x) => LocalizationUtils.Localize(x.DisplayName), new AlphanumComparatorFast());
	}

	public static SettingsPropertyGroupDefinition? GetGroupFromName(this IEnumerable<SettingsPropertyGroupDefinition> groupsList, string groupName)
	{
		return groupsList.FirstOrDefault((SettingsPropertyGroupDefinition x) => x.GroupNameRaw == groupName);
	}
}

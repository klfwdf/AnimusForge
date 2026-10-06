using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Encyclopedia;

public class EncyclopediaFilterGroup : ViewModel
{
	public readonly List<EncyclopediaFilterItem> Filters;

	public readonly TextObject Name;

	public Predicate<object> Predicate => delegate(object item)
	{
		if (!Filters.Any((EncyclopediaFilterItem f) => f.IsActive))
		{
			return true;
		}
		foreach (EncyclopediaFilterItem filter in Filters)
		{
			if (filter.IsActive && filter.Predicate(item))
			{
				return true;
			}
		}
		return false;
	};

	public EncyclopediaFilterGroup(List<EncyclopediaFilterItem> filters, TextObject name)
	{
		Filters = filters;
		Name = name;
	}
}

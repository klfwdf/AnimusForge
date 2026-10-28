using System;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Encyclopedia;

public class EncyclopediaFilterItem
{
	public readonly TextObject Name;

	public readonly Predicate<object> Predicate;

	public bool IsActive;

	public EncyclopediaFilterItem(TextObject name, Predicate<object> predicate)
	{
		Name = name;
		Predicate = predicate;
		IsActive = false;
	}
}

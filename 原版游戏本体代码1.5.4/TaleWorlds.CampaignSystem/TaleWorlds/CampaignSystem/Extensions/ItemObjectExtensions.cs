using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.Extensions;

public static class ItemObjectExtensions
{
	public static ItemCategory GetItemCategory(this ItemObject item)
	{
		return item.ItemCategory;
	}
}

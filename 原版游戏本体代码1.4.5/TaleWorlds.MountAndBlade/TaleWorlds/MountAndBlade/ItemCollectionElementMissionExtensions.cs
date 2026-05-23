using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public static class ItemCollectionElementMissionExtensions
{
	public static StackArray.StackArray4Int GetItemHolsterIndices(this ItemObject item)
	{
		StackArray.StackArray4Int result = default(StackArray.StackArray4Int);
		for (int i = 0; i < item.ItemHolsters.Length; i++)
		{
			result[i] = ((item.ItemHolsters[i].Length > 0) ? MBItem.GetItemHolsterIndex(item.ItemHolsters[i]) : (-1));
		}
		for (int j = item.ItemHolsters.Length; j < 4; j++)
		{
			result[j] = -1;
		}
		return result;
	}
}

using System.Collections.Generic;
using System.Linq;

namespace TaleWorlds.MountAndBlade;

public static class IntermissionVoteItemListExtensions
{
	public static bool ContainsItem(this List<IntermissionVoteItem> intermissionVoteItems, string id)
	{
		if (intermissionVoteItems != null)
		{
			return intermissionVoteItems.FirstOrDefault((IntermissionVoteItem item) => item.Id == id) != null;
		}
		return false;
	}

	public static IntermissionVoteItem Add(this List<IntermissionVoteItem> intermissionVoteItems, string id)
	{
		IntermissionVoteItem result = null;
		if (intermissionVoteItems != null)
		{
			int count = intermissionVoteItems.Count;
			IntermissionVoteItem intermissionVoteItem = new IntermissionVoteItem(id, count);
			intermissionVoteItems.Add(intermissionVoteItem);
			result = intermissionVoteItem;
		}
		return result;
	}

	public static IntermissionVoteItem GetItem(this List<IntermissionVoteItem> intermissionVoteItems, string id)
	{
		IntermissionVoteItem result = null;
		if (intermissionVoteItems != null)
		{
			result = intermissionVoteItems.FirstOrDefault((IntermissionVoteItem item) => item.Id == id);
		}
		return result;
	}
}

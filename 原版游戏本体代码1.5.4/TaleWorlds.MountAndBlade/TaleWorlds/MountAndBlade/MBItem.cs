using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

public static class MBItem
{
	public static int GetItemUsageIndex(string itemUsageName)
	{
		return MBAPI.IMBItem.GetItemUsageIndex(itemUsageName);
	}

	public static int GetItemHolsterIndex(string itemHolsterName)
	{
		return MBAPI.IMBItem.GetItemHolsterIndex(itemHolsterName);
	}

	public static bool GetItemIsPassiveUsage(string itemUsageName)
	{
		return MBAPI.IMBItem.GetItemIsPassiveUsage(itemUsageName);
	}

	public static MatrixFrame GetHolsterFrameByIndex(int index)
	{
		MatrixFrame outFrame = default(MatrixFrame);
		MBAPI.IMBItem.GetHolsterFrameByIndex(index, ref outFrame);
		return outFrame;
	}

	public static ItemObject.ItemUsageSetFlags GetItemUsageSetFlags(string ItemUsageName)
	{
		return (ItemObject.ItemUsageSetFlags)MBAPI.IMBItem.GetItemUsageSetFlags(ItemUsageName);
	}

	public static ActionIndexCache GetItemUsageReloadActionCode(string itemUsageName, int usageDirection, bool isMounted, int leftHandUsageSetIndex, bool isLeftStance, bool isLowLookDirection)
	{
		return new ActionIndexCache(MBAPI.IMBItem.GetItemUsageReloadActionCode(itemUsageName, usageDirection, isMounted, leftHandUsageSetIndex, isLeftStance, isLowLookDirection));
	}

	public static int GetItemUsageStrikeType(string itemUsageName, int usageDirection, bool isMounted, int leftHandUsageSetIndex, bool isLeftStance, bool isLowLookDirection)
	{
		return MBAPI.IMBItem.GetItemUsageStrikeType(itemUsageName, usageDirection, isMounted, leftHandUsageSetIndex, isLeftStance, isLowLookDirection);
	}

	public static float GetMissileRange(float shotSpeed, float zDiff)
	{
		return MBAPI.IMBItem.GetMissileRange(shotSpeed, zDiff);
	}
}

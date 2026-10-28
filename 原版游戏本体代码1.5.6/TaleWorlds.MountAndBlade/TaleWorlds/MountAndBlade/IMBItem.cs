using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade;

[ScriptingInterfaceBase]
internal interface IMBItem
{
	[EngineMethod("get_item_usage_index", false, null, false)]
	int GetItemUsageIndex(string itemusagename);

	[EngineMethod("get_item_holster_index", false, null, false)]
	int GetItemHolsterIndex(string itemholstername);

	[EngineMethod("get_item_is_passive_usage", false, null, false)]
	bool GetItemIsPassiveUsage(string itemUsageName);

	[EngineMethod("get_holster_frame_by_index", false, null, false)]
	void GetHolsterFrameByIndex(int index, ref MatrixFrame outFrame);

	[EngineMethod("get_item_usage_set_flags", false, null, false)]
	int GetItemUsageSetFlags(string ItemUsageName);

	[EngineMethod("get_item_usage_reload_action_code", false, null, false)]
	int GetItemUsageReloadActionCode(string itemUsageName, int usageDirection, bool isMounted, int leftHandUsageSetIndex, bool isLeftStance, bool isLowLookDirection);

	[EngineMethod("get_item_usage_strike_type", false, null, false)]
	int GetItemUsageStrikeType(string itemUsageName, int usageDirection, bool isMounted, int leftHandUsageSetIndex, bool isLeftStance, bool isLowLookDirection);

	[EngineMethod("get_missile_range", false, null, false)]
	float GetMissileRange(float shootSpeed, float zDiff);
}

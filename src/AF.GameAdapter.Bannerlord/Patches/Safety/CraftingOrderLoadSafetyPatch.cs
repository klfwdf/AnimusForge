using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CraftingSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace AnimusForge;

internal static class CraftingOrderLoadSafetyPatch
{
	private static readonly MethodInfo InitializeCraftedItemDataMethod = AccessTools.Method(typeof(CraftingCampaignBehavior), "InitializeCraftedItemData");
	private static readonly MethodInfo RemoveCustomOrderMethod = AccessTools.Method(typeof(CraftingCampaignBehavior.CraftingOrderSlots), "RemoveCustomOrder");
	private static readonly FieldInfo CraftingItemsHistoryField = AccessTools.Field(typeof(CraftingCampaignBehavior), "_cratingItemsHistory");

	private static bool _patched;

	internal static void EnsurePatched(Harmony harmony)
	{
		if (_patched || harmony == null)
		{
			return;
		}
		MethodInfo target = ResolveOnBeforeNonReadyObjectsDeleted();
		if (target == null)
		{
			Logger.Log("CraftingLoadSafety", "Crafting load safety patch skipped: target method not found.");
			return;
		}
		harmony.Patch(target, prefix: new HarmonyMethod(typeof(CraftingOrderLoadSafetyPatch), nameof(OnBeforeNonReadyObjectsDeletedPrefix)));
		_patched = true;
		Logger.Log("CraftingLoadSafety", "Crafting order load safety patch applied.");
	}

	private static MethodInfo ResolveOnBeforeNonReadyObjectsDeleted()
	{
		Type behaviorType = typeof(CraftingCampaignBehavior);
		MethodInfo method = AccessTools.Method(behaviorType, "TaleWorlds.CampaignSystem.CampaignBehaviors.INonReadyObjectHandler.OnBeforeNonReadyObjectsDeleted");
		if (method != null)
		{
			return method;
		}
		try
		{
			InterfaceMapping interfaceMap = behaviorType.GetInterfaceMap(typeof(INonReadyObjectHandler));
			for (int i = 0; i < interfaceMap.InterfaceMethods.Length; i++)
			{
				if (string.Equals(interfaceMap.InterfaceMethods[i]?.Name, nameof(INonReadyObjectHandler.OnBeforeNonReadyObjectsDeleted), StringComparison.Ordinal))
				{
					return interfaceMap.TargetMethods[i];
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static bool OnBeforeNonReadyObjectsDeletedPrefix(CraftingCampaignBehavior __instance)
	{
		if (__instance == null)
		{
			return true;
		}
		try
		{
			RunSafeOnBeforeNonReadyObjectsDeleted(__instance);
			return false;
		}
		catch (Exception ex)
		{
			Logger.Log("CraftingLoadSafety", "Safe crafting-order load cleanup failed; falling back to native handler. " + ex);
			return true;
		}
	}

	private static void RunSafeOnBeforeNonReadyObjectsDeleted(CraftingCampaignBehavior behavior)
	{
		TryInitializeCraftedItemData(behavior);
		int removedTownOrders = 0;
		int removedCustomOrders = 0;
		IReadOnlyDictionary<Town, CraftingCampaignBehavior.CraftingOrderSlots> craftingOrders = behavior.CraftingOrders;
		if (craftingOrders != null)
		{
			foreach (KeyValuePair<Town, CraftingCampaignBehavior.CraftingOrderSlots> pair in craftingOrders)
			{
				CraftingCampaignBehavior.CraftingOrderSlots slots = pair.Value;
				if (slots == null)
				{
					continue;
				}
				CraftingOrder[] townSlots = slots.Slots;
				if (townSlots != null)
				{
					for (int i = 0; i < townSlots.Length; i++)
					{
						CraftingOrder order = townSlots[i];
						if (order == null)
						{
							continue;
						}
						if (!TryPrepareOrderForLoad(order, out string reason))
						{
							townSlots[i] = null;
							removedTownOrders++;
							Logger.Log("CraftingLoadSafety", $"Removed invalid town crafting order during load. town={pair.Key?.StringId ?? "unknown"} slot={i} reason={reason}");
						}
					}
				}
				List<CraftingOrder> invalidCustomOrders = new List<CraftingOrder>();
				if (slots.CustomOrders != null)
				{
					foreach (CraftingOrder order in slots.CustomOrders)
					{
						if (order != null && !TryPrepareOrderForLoad(order, out string reason))
						{
							invalidCustomOrders.Add(order);
							Logger.Log("CraftingLoadSafety", $"Queued invalid custom crafting order for removal during load. town={pair.Key?.StringId ?? "unknown"} reason={reason}");
						}
					}
				}
				for (int i = 0; i < invalidCustomOrders.Count; i++)
				{
					if (TryRemoveCustomOrder(slots, invalidCustomOrders[i]))
					{
						removedCustomOrders++;
					}
				}
			}
		}
		int removedHistoryItems = CleanCraftingItemsHistory(behavior);
		if (removedTownOrders > 0 || removedCustomOrders > 0 || removedHistoryItems > 0)
		{
			Logger.Log("CraftingLoadSafety", $"Crafting load cleanup completed. removedTownOrders={removedTownOrders} removedCustomOrders={removedCustomOrders} removedHistoryItems={removedHistoryItems}");
		}
	}

	private static void TryInitializeCraftedItemData(CraftingCampaignBehavior behavior)
	{
		if (InitializeCraftedItemDataMethod == null)
		{
			return;
		}
		try
		{
			InitializeCraftedItemDataMethod.Invoke(behavior, Array.Empty<object>());
		}
		catch (TargetInvocationException ex)
		{
			Exception inner = ex.InnerException ?? ex;
			Logger.Log("CraftingLoadSafety", "Crafted item data initialization failed during load; continuing with order cleanup. " + inner);
		}
		catch (Exception ex)
		{
			Logger.Log("CraftingLoadSafety", "Crafted item data initialization reflection failed during load; continuing with order cleanup. " + ex);
		}
	}

	private static bool TryPrepareOrderForLoad(CraftingOrder order, out string reason)
	{
		reason = "";
		if (order == null)
		{
			reason = "null_order";
			return false;
		}
		try
		{
			if (!order.IsPreCraftedWeaponDesignValid())
			{
				reason = "invalid_design";
				return false;
			}
		}
		catch (Exception ex)
		{
			reason = "design_validation_exception:" + ex.GetType().Name;
			return false;
		}
		try
		{
			order.InitializeCraftingOrderOnLoad();
		}
		catch (Exception ex)
		{
			reason = "initialize_exception:" + ex.GetType().Name;
			return false;
		}
		ItemObject item = null;
		try
		{
			item = order.PreCraftedWeaponDesignItem;
		}
		catch (Exception ex)
		{
			reason = "item_read_exception:" + ex.GetType().Name;
			return false;
		}
		if (item == null)
		{
			reason = "null_item_after_initialize";
			return false;
		}
		if (item == DefaultItems.Trash)
		{
			reason = "trash_item_after_initialize";
			return false;
		}
		if (!item.IsReady)
		{
			reason = "item_not_ready_after_initialize";
			return false;
		}
		return true;
	}

	private static bool TryRemoveCustomOrder(CraftingCampaignBehavior.CraftingOrderSlots slots, CraftingOrder order)
	{
		if (slots == null || order == null)
		{
			return false;
		}
		if (RemoveCustomOrderMethod == null)
		{
			Logger.Log("CraftingLoadSafety", "RemoveCustomOrder method not found; invalid custom crafting order was left in place.");
			return false;
		}
		try
		{
			RemoveCustomOrderMethod.Invoke(slots, new object[] { order });
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("CraftingLoadSafety", "Failed to remove invalid custom crafting order during load. " + ex);
			return false;
		}
	}

	private static int CleanCraftingItemsHistory(CraftingCampaignBehavior behavior)
	{
		if (CraftingItemsHistoryField == null)
		{
			return 0;
		}
		try
		{
			if (CraftingItemsHistoryField.GetValue(behavior) is not List<ItemObject> history)
			{
				return 0;
			}
			int removed = 0;
			for (int i = history.Count - 1; i >= 0; i--)
			{
				ItemObject item = history[i];
				if (item == null || item == DefaultItems.Trash)
				{
					history.RemoveAt(i);
					removed++;
				}
			}
			return removed;
		}
		catch (Exception ex)
		{
			Logger.Log("CraftingLoadSafety", "Failed to clean crafting item history during load. " + ex);
			return 0;
		}
	}
}

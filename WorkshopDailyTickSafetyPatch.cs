using System;
using System.Collections.Generic;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.GameComponents;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Settlements.Workshops;
using TaleWorlds.Library;

namespace AnimusForge;

internal static class WorkshopDailyTickSafetyPatch
{
	private static bool _patched;
	private static readonly HashSet<string> _loggedKeys = new HashSet<string>(StringComparer.Ordinal);

	public static void EnsurePatched(Harmony harmony)
	{
		if (_patched || harmony == null)
		{
			return;
		}
		_patched = true;
		try
		{
			var runTownWorkshop = AccessTools.Method(typeof(WorkshopsCampaignBehavior), "RunTownWorkshop", new[] { typeof(Town), typeof(Workshop) });
			if (runTownWorkshop != null)
			{
				harmony.Patch(runTownWorkshop, prefix: new HarmonyMethod(typeof(WorkshopDailyTickSafetyPatch), nameof(RunTownWorkshopPrefix)));
				Logger.Log("WorkshopSafety", "RunTownWorkshop guard applied.");
			}
			else
			{
				Logger.Log("WorkshopSafety", "RunTownWorkshop not found; guard skipped.");
			}

			var handleDailyExpense = AccessTools.Method(typeof(WorkshopsCampaignBehavior), "HandleDailyExpense", new[] { typeof(Workshop) });
			if (handleDailyExpense != null)
			{
				harmony.Patch(handleDailyExpense, prefix: new HarmonyMethod(typeof(WorkshopDailyTickSafetyPatch), nameof(HandleDailyExpensePrefix)));
				Logger.Log("WorkshopSafety", "HandleDailyExpense guard applied.");
			}
			else
			{
				Logger.Log("WorkshopSafety", "HandleDailyExpense not found; guard skipped.");
			}

			var conversionSpeed = AccessTools.Method(typeof(DefaultWorkshopModel), nameof(DefaultWorkshopModel.GetEffectiveConversionSpeedOfProduction), new[] { typeof(Workshop), typeof(float), typeof(bool) });
			if (conversionSpeed != null)
			{
				harmony.Patch(conversionSpeed, prefix: new HarmonyMethod(typeof(WorkshopDailyTickSafetyPatch), nameof(GetEffectiveConversionSpeedPrefix)));
				Logger.Log("WorkshopSafety", "DefaultWorkshopModel conversion speed guard applied.");
			}
			else
			{
				Logger.Log("WorkshopSafety", "DefaultWorkshopModel conversion speed method not found; guard skipped.");
			}

			var afterLoad = AccessTools.Method(typeof(Workshop), "AfterLoad", Type.EmptyTypes);
			if (afterLoad != null)
			{
				harmony.Patch(afterLoad, finalizer: new HarmonyMethod(typeof(WorkshopDailyTickSafetyPatch), nameof(WorkshopAfterLoadFinalizer)));
				Logger.Log("WorkshopSafety", "Workshop.AfterLoad guard applied.");
			}
			else
			{
				Logger.Log("WorkshopSafety", "Workshop.AfterLoad not found; load guard skipped.");
			}
		}
		catch (Exception ex)
		{
			Logger.Log("WorkshopSafety", "Failed to apply workshop guards: " + ex.Message);
		}
	}

	public static bool RunTownWorkshopPrefix(Town townComponent, Workshop workshop)
	{
		return IsWorkshopSafeForDailyTick(workshop, townComponent, "run");
	}

	public static bool HandleDailyExpensePrefix(Workshop shop)
	{
		return IsWorkshopSafeForDailyTick(shop, shop?.Settlement?.Town, "expense");
	}

	public static bool GetEffectiveConversionSpeedPrefix(Workshop workshop, float speed, bool includeDescription, ref ExplainedNumber __result)
	{
		if (IsWorkshopSafeForDailyTick(workshop, workshop?.Settlement?.Town, "speed"))
		{
			return true;
		}
		__result = new ExplainedNumber(speed, includeDescription);
		return false;
	}

	public static Exception WorkshopAfterLoadFinalizer(Workshop __instance, Exception __exception)
	{
		if (__exception == null)
		{
			return null;
		}
		if (!IsRecoverableWorkshopException(__exception))
		{
			return __exception;
		}
		try
		{
			string key = GetWorkshopKey(__instance);
			string repairReason = "afterload exception suppressed: " + __exception.GetType().Name + ": " + __exception.Message;
			WorkshopType workshopType = __instance?.WorkshopType;
			if (workshopType?.Productions != null)
			{
				EnsureProductionProgress(__instance, workshopType, out string progressReason);
				if (!string.IsNullOrWhiteSpace(progressReason))
				{
					repairReason += "; " + progressReason;
				}
				__instance.UpdateLastRunTime();
			}
			LogSkip("afterload_exception_suppressed", key, repairReason);
			return null;
		}
		catch (Exception ex)
		{
			LogSkip("afterload_exception_suppressed", GetWorkshopKey(__instance), "afterload repair guard failed: " + ex.Message);
			return null;
		}
	}

	private static bool IsWorkshopSafeForDailyTick(Workshop workshop, Town expectedTown, string stage)
	{
		try
		{
			if (workshop == null)
			{
				LogSkip(stage, "null", "workshop is null");
				return false;
			}
			Settlement settlement = workshop.Settlement;
			Town town = settlement?.Town;
			if (settlement == null || town == null || (expectedTown != null && expectedTown != town))
			{
				LogSkip(stage, GetWorkshopKey(workshop), "missing settlement/town");
				return false;
			}
			if (settlement.OwnerClan == null)
			{
				LogSkip(stage, GetWorkshopKey(workshop), "settlement owner clan is null");
				return false;
			}
			if (town.Owner?.ItemRoster == null)
			{
				LogSkip(stage, GetWorkshopKey(workshop), "town market owner/roster is null");
				return false;
			}
			WorkshopType workshopType = workshop.WorkshopType;
			if (workshopType?.Productions == null)
			{
				LogSkip(stage, GetWorkshopKey(workshop), "workshop type/productions is null");
				return false;
			}
			if (!EnsureProductionProgress(workshop, workshopType, out string progressReason))
			{
				LogSkip(stage, GetWorkshopKey(workshop), progressReason);
				return false;
			}
			if (!EnsureValidOwner(workshop, out string ownerReason))
			{
				LogSkip(stage, GetWorkshopKey(workshop), ownerReason);
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			LogSkip(stage, GetWorkshopKey(workshop), "guard exception: " + ex.Message);
			return false;
		}
	}

	private static bool EnsureProductionProgress(Workshop workshop, WorkshopType workshopType, out string reason)
	{
		reason = "";
		try
		{
			var field = AccessTools.Field(typeof(Workshop), "_productionProgress");
			if (field == null)
			{
				reason = "production progress field not found";
				return false;
			}
			int expectedCount = workshopType.Productions.Count;
			float[] progress = field.GetValue(workshop) as float[];
			if (progress != null && progress.Length == expectedCount)
			{
				return true;
			}
			workshop.ChangeWorkshopProduction(workshopType);
			reason = "repaired production progress length to " + expectedCount;
			LogSkip("repair", GetWorkshopKey(workshop), reason);
			return true;
		}
		catch (Exception ex)
		{
			reason = "production progress repair failed: " + ex.Message;
			return false;
		}
	}

	private static bool EnsureValidOwner(Workshop workshop, out string reason)
	{
		reason = "";
		Hero owner = workshop?.Owner;
		if (IsValidWorkshopOwner(owner))
		{
			return true;
		}

		Hero replacement = FindReplacementOwner(workshop);
		if (!IsValidWorkshopOwner(replacement))
		{
			reason = "invalid owner and no replacement owner";
			return false;
		}

		try
		{
			SetWorkshopOwner(workshop, owner, replacement);
			reason = "repaired owner to " + (replacement.StringId ?? replacement.Name?.ToString() ?? "unknown");
			LogSkip("repair", GetWorkshopKey(workshop), reason);
			return true;
		}
		catch (Exception ex)
		{
			reason = "owner repair failed: " + ex.Message;
			return false;
		}
	}

	private static bool IsValidWorkshopOwner(Hero hero)
	{
		try
		{
			return hero != null && hero.IsAlive && hero.CharacterObject != null;
		}
		catch
		{
			return false;
		}
	}

	private static Hero FindReplacementOwner(Workshop workshop)
	{
		try
		{
			Hero modelChoice = Campaign.Current?.Models?.WorkshopModel?.GetNotableOwnerForWorkshop(workshop);
			if (IsValidWorkshopOwner(modelChoice))
			{
				return modelChoice;
			}
		}
		catch
		{
		}
		try
		{
			foreach (Hero notable in workshop?.Settlement?.Notables)
			{
				if (IsValidWorkshopOwner(notable) && notable != workshop.Owner)
				{
					return notable;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static void SetWorkshopOwner(Workshop workshop, Hero oldOwner, Hero newOwner)
	{
		if (workshop == null || newOwner == null)
		{
			return;
		}
		if (oldOwner != null)
		{
			workshop.ChangeOwnerOfWorkshop(newOwner, workshop.WorkshopType, workshop.Capital);
		}
		else
		{
			var ownerField = AccessTools.Field(typeof(Workshop), "_owner");
			if (ownerField == null)
			{
				throw new InvalidOperationException("Workshop._owner field not found");
			}
			ownerField.SetValue(workshop, newOwner);
			newOwner.AddOwnedWorkshop(workshop);
		}
		if (workshop.Owner != newOwner)
		{
			throw new InvalidOperationException("Workshop owner did not update");
		}
		CampaignEventDispatcher.Instance.OnWorkshopOwnerChanged(workshop, oldOwner);
	}

	private static bool IsRecoverableWorkshopException(Exception exception)
	{
		return exception is NullReferenceException
			|| exception is InvalidOperationException
			|| exception is ArgumentException
			|| exception is IndexOutOfRangeException;
	}

	private static string GetWorkshopKey(Workshop workshop)
	{
		try
		{
			string settlementId = workshop?.Settlement?.StringId ?? "no_settlement";
			string tag = workshop?.Tag ?? "no_tag";
			return settlementId + "/" + tag;
		}
		catch
		{
			return "unknown";
		}
	}

	private static void LogSkip(string stage, string key, string reason)
	{
		try
		{
			string logKey = (stage ?? "") + "|" + (key ?? "") + "|" + (reason ?? "");
			lock (_loggedKeys)
			{
				if (!_loggedKeys.Add(logKey))
				{
					return;
				}
			}
			Logger.Log("WorkshopSafety", "stage=" + (stage ?? "") + " workshop=" + (key ?? "") + " reason=" + (reason ?? ""));
		}
		catch
		{
		}
	}
}

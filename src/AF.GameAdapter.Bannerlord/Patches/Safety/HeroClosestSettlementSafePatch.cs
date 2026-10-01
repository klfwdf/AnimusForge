using System;
using HarmonyLib;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Library;

namespace AnimusForge;

internal static class HeroClosestSettlementSafePatch
{
	private static bool _patched;
	private static int _guardedInvalidParties;

	public static void EnsurePatched(Harmony harmony)
	{
		if (_patched || harmony == null)
		{
			return;
		}
		_patched = true;
		try
		{
			var target = AccessTools.Method(typeof(HeroHelper), nameof(HeroHelper.GetClosestSettlement), new[] { typeof(Hero) });
			if (target == null)
			{
				Logger.Log("HeroLocationSafety", "HeroHelper.GetClosestSettlement not found; invalid party guard skipped.");
				return;
			}
			harmony.Patch(target, prefix: new HarmonyMethod(typeof(HeroClosestSettlementSafePatch), nameof(GetClosestSettlementPrefix)));
			Logger.Log("HeroLocationSafety", "HeroHelper.GetClosestSettlement invalid mobile party guard applied.");
		}
		catch (Exception ex)
		{
			Logger.Log("HeroLocationSafety", "Failed to apply closest settlement guard: " + ex.Message);
		}
	}

	public static bool GetClosestSettlementPrefix(Hero hero, ref Settlement __result)
	{
		try
		{
			if (!NeedsInvalidPartyGuard(hero, out MobileParty mobileParty))
			{
				return true;
			}
			__result = ResolveFallbackSettlement(hero, mobileParty);
			LogGuardedHero(hero, mobileParty, __result);
			return false;
		}
		catch
		{
			return true;
		}
	}

	private static bool NeedsInvalidPartyGuard(Hero hero, out MobileParty mobileParty)
	{
		mobileParty = null;
		if (hero == null || hero.CurrentSettlement != null)
		{
			return false;
		}
		PartyBase partyBase = hero.PartyBelongedTo?.Party ?? hero.PartyBelongedToAsPrisoner;
		if (partyBase == null || !partyBase.IsMobile)
		{
			return false;
		}
		mobileParty = partyBase.MobileParty;
		return mobileParty != null && !mobileParty.Position.IsValid();
	}

	private static Settlement ResolveFallbackSettlement(Hero hero, MobileParty mobileParty)
	{
		return NormalizeSettlement(hero?.LastKnownClosestSettlement)
			?? NormalizeSettlement(hero?.HomeSettlement)
			?? NormalizeSettlement(hero?.BornSettlement)
			?? NormalizeSettlement(mobileParty?.CurrentSettlement)
			?? NormalizeSettlement(mobileParty?.HomeSettlement)
			?? NormalizeSettlement(hero?.Clan?.HomeSettlement)
			?? NormalizeSettlement(hero?.Clan?.InitialHomeSettlement)
			?? FindNearestToMainParty()
			?? FindAnyVillageOrFortification();
	}

	private static Settlement NormalizeSettlement(Settlement settlement)
	{
		if (settlement == null)
		{
			return null;
		}
		if (settlement.IsVillage || settlement.IsFortification)
		{
			return settlement;
		}
		try
		{
			return SettlementHelper.FindNearestSettlementToSettlement(settlement, MobileParty.NavigationType.All, x => x.IsVillage || x.IsFortification) ?? settlement;
		}
		catch
		{
			return settlement;
		}
	}

	private static Settlement FindNearestToMainParty()
	{
		try
		{
			MobileParty mainParty = MobileParty.MainParty;
			if (mainParty != null && mainParty.Position.IsValid())
			{
				return SettlementHelper.FindNearestSettlementToMobileParty(mainParty, MobileParty.NavigationType.All, x => x.IsVillage || x.IsFortification);
			}
		}
		catch
		{
		}
		return null;
	}

	private static Settlement FindAnyVillageOrFortification()
	{
		try
		{
			foreach (Settlement settlement in Settlement.All)
			{
				if (settlement != null && (settlement.IsVillage || settlement.IsFortification))
				{
					return settlement;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static void LogGuardedHero(Hero hero, MobileParty mobileParty, Settlement fallback)
	{
		_guardedInvalidParties++;
		if (_guardedInvalidParties > 5)
		{
			return;
		}
		string heroId = SafeId(hero?.StringId, hero?.Name?.ToString());
		string partyId = SafeId(mobileParty?.StringId, mobileParty?.Name?.ToString());
		string settlementId = SafeId(fallback?.StringId, fallback?.Name?.ToString());
		Logger.Log("HeroLocationSafety", "Guarded invalid mobile party closest settlement lookup. count=" + _guardedInvalidParties + " hero=" + heroId + " party=" + partyId + " fallback=" + settlementId);
	}

	private static string SafeId(string id, string name)
	{
		if (!string.IsNullOrEmpty(id))
		{
			return id;
		}
		if (!string.IsNullOrEmpty(name))
		{
			return name;
		}
		return "unknown";
	}
}

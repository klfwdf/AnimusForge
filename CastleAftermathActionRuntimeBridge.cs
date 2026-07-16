using System;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;

namespace AnimusForge;

/// <summary>
/// Thin Bannerlord roster adapter for castle GCCZ prisoner actions.
/// Role gates, count policy and wording remain in the standalone GCCZ core.
/// </summary>
internal static class CastleAftermathActionRuntimeBridge
{
	internal static CastleAftermathActionApplyResult RecruitSelectedRegularPrisoners()
	{
		TroopRoster selected = CastleAftermathRuntimeBridge.GetSelectedPrisonerRosterSnapshot();
		TroopRoster mainPrisoners = PartyBase.MainParty?.PrisonRoster;
		TroopRoster mainMembers = MobileParty.MainParty?.MemberRoster;
		if (mainPrisoners == null || mainMembers == null || PartyBase.MainParty == null)
		{
			return CastleAftermathActionApplyResult.Failed(
				SiegeCastlePrisonerDispositionProfile.RosterUnavailableReason,
				CastleAftermathRuntimeBridge.SelectedRegularPrisonerCount);
		}

		int availableRegularPrisoners = CastleAftermathRuntimeBridge.SelectedRegularPrisonerCount;
		if (selected == null || availableRegularPrisoners <= 0)
		{
			return CastleAftermathActionApplyResult.Completed(
				0,
				0,
				SiegeCastlePrisonerDispositionProfile.NoMatchingRegularPrisonersReason);
		}

		int freeSlots = Math.Max(0, PartyBase.MainParty.PartySizeLimit - PartyBase.MainParty.NumberOfAllMembers);
		int requested = SiegeCastlePrisonerDispositionProfile.ResolveRecruitCount(
			availableRegularPrisoners,
			freeSlots);
		if (requested <= 0)
		{
			return CastleAftermathActionApplyResult.Completed(
				0,
				CastleAftermathRuntimeBridge.SelectedRegularPrisonerCount,
				SiegeCastlePrisonerDispositionProfile.PartyCapacityFullReason);
		}

		TroopRoster resolved = TroopRoster.CreateDummyTroopRoster();
		int affected = 0;
		try
		{
			foreach (TroopRosterElement selectedElement in selected.GetTroopRoster().ToList())
			{
				CharacterObject character = selectedElement.Character;
				if (character == null || character.IsHero || selectedElement.Number <= 0 || affected >= requested)
				{
					continue;
				}

				int sourceIndex = mainPrisoners.FindIndexOfTroop(character);
				if (sourceIndex < 0)
				{
					continue;
				}
				TroopRosterElement sourceElement = mainPrisoners.GetElementCopyAtIndex(sourceIndex);
				int number = Math.Min(Math.Min(selectedElement.Number, sourceElement.Number), requested - affected);
				if (number <= 0)
				{
					continue;
				}

				int wounded = SiegeCastlePrisonerDispositionProfile.ResolveTransferredWounded(
					sourceElement.Number,
					sourceElement.WoundedNumber,
					number);
				int xp = SiegeCastlePrisonerDispositionProfile.ResolveTransferredXp(
					sourceElement.Number,
					sourceElement.Xp,
					number);
				resolved.AddToCounts(character, number, false, wounded, xp, true, -1);
				try
				{
					mainPrisoners.AddToCounts(character, -number, false, -wounded, -xp, true, -1);
					try
					{
						mainMembers.AddToCounts(character, number, false, wounded, xp, true, -1);
					}
					catch
					{
						mainPrisoners.AddToCounts(character, number, false, wounded, xp, true, -1);
						throw;
					}
				}
				catch
				{
					resolved.AddToCounts(character, -number, false, -wounded, -xp, true, -1);
					throw;
				}
				affected += number;
			}

			CastleAftermathRuntimeBridge.RemoveResolvedRegularPrisoners(resolved, "castle_recruit_prisoners");
			return CastleAftermathActionApplyResult.Completed(
				affected,
				CastleAftermathRuntimeBridge.SelectedRegularPrisonerCount,
				affected > 0
					? SiegeCastlePrisonerDispositionProfile.RecruitedReason
					: SiegeCastlePrisonerDispositionProfile.NoMatchingRegularPrisonersReason);
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Recruit selected regular prisoners failed after affected=" + affected + ": " + ex);
			if (affected > 0)
			{
				CastleAftermathRuntimeBridge.RemoveResolvedRegularPrisoners(resolved, "castle_recruit_prisoners_partial_error");
			}
			return new CastleAftermathActionApplyResult(
				succeeded: affected > 0,
				affected,
				CastleAftermathRuntimeBridge.SelectedRegularPrisonerCount,
				SiegeCastlePrisonerDispositionProfile.ExceptionReasonPrefix + ex.GetType().Name);
		}
	}

	internal static CastleAftermathActionApplyResult BeginSlaughterOfSelectedRegularPrisoners()
	{
		int selected = CastleAftermathRuntimeBridge.SelectedRegularPrisonerCount;
		if (selected <= 0)
		{
			return CastleAftermathActionApplyResult.Completed(0, 0, SiegeCastlePrisonerDispositionProfile.NoMatchingRegularPrisonersReason);
		}
		try
		{
			int started = CastleAftermathRuntimeBridge.BeginRegularPrisonerSlaughter();
			return started > 0
				? CastleAftermathActionApplyResult.Completed(started, selected, "slaughter_started")
				: CastleAftermathActionApplyResult.Failed("slaughter_scene_agents_unavailable", selected);
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Begin real prisoner slaughter failed: " + ex);
			return CastleAftermathActionApplyResult.Failed(
				SiegeCastlePrisonerDispositionProfile.ExceptionReasonPrefix + ex.GetType().Name,
				selected);
		}
	}

	internal static CastleAftermathActionApplyResult ReleaseSelectedRegularPrisoners()
	{
		return RemoveSelectedRegularPrisoners("castle_release_prisoners", grantRansomGold: false);
	}

	internal static CastleAftermathActionApplyResult SellSelectedRegularPrisoners()
	{
		return RemoveSelectedRegularPrisoners("castle_sell_prisoners", grantRansomGold: true);
	}

	internal static CastleAftermathActionApplyResult AssignSelectedRegularPrisonersToService(string source)
	{
		return RemoveSelectedRegularPrisoners(source ?? "castle_prisoner_service", grantRansomGold: false);
	}

	internal static CastleAftermathActionApplyResult ProvideCareToSelectedRegularPrisoners()
	{
		TroopRoster selected = CastleAftermathRuntimeBridge.GetSelectedPrisonerRosterSnapshot();
		ItemRoster items = PartyBase.MainParty?.ItemRoster;
		int affected = CastleAftermathRuntimeBridge.SelectedRegularPrisonerCount;
		if (selected == null || items == null || affected <= 0)
		{
			return CastleAftermathActionApplyResult.Failed("care_roster_unavailable", affected);
		}

		int burden = selected.GetTroopRoster()
			.Where(element => element.Character != null && !element.Character.IsHero && element.Number > 0)
			.Sum(element => Math.Max(1, element.Character.Tier + 1) * element.Number);
		int requiredFood = Math.Max(1, (int)Math.Ceiling(burden / 12d));
		int availableFood = 0;
		for (int i = 0; i < items.Count; i++)
		{
			ItemRosterElement element = items.GetElementCopyAtIndex(i);
			if (element.EquipmentElement.Item?.IsFood == true)
			{
				availableFood += Math.Max(0, element.Amount);
			}
		}
		if (availableFood < requiredFood)
		{
			return CastleAftermathActionApplyResult.Failed("care_supplies_insufficient", affected);
		}

		int remaining = requiredFood;
		foreach (ItemRosterElement element in items.ToList())
		{
			if (remaining <= 0)
			{
				break;
			}
			if (element.EquipmentElement.Item?.IsFood != true || element.Amount <= 0)
			{
				continue;
			}
			int consume = Math.Min(remaining, element.Amount);
			items.AddToCounts(element.EquipmentElement, -consume);
			remaining -= consume;
		}
		Logger.Log("CastleAftermath", "Provided care supplies to selected regular prisoners. Affected="
			+ affected + ", Food=" + requiredFood);
		return CastleAftermathActionApplyResult.Completed(affected, affected, "care_supplies_applied");
	}

	internal static CastleAftermathActionApplyResult ProvideCareToCapturedLord()
	{
		ItemRoster items = PartyBase.MainParty?.ItemRoster;
		if (items == null)
		{
			return CastleAftermathActionApplyResult.Failed("care_roster_unavailable", 0);
		}
		for (int i = 0; i < items.Count; i++)
		{
			ItemRosterElement element = items.GetElementCopyAtIndex(i);
			if (element.Amount <= 0 || element.EquipmentElement.Item?.IsFood != true)
			{
				continue;
			}
			items.AddToCounts(element.EquipmentElement, -1);
			return CastleAftermathActionApplyResult.Completed(1, 0, "care_supplies_applied");
		}
		return CastleAftermathActionApplyResult.Failed("care_supplies_insufficient", 0);
	}

	private static CastleAftermathActionApplyResult RemoveSelectedRegularPrisoners(string source, bool grantRansomGold)
	{
		TroopRoster selected = CastleAftermathRuntimeBridge.GetSelectedPrisonerRosterSnapshot();
		TroopRoster mainPrisoners = PartyBase.MainParty?.PrisonRoster;
		if (selected == null || mainPrisoners == null)
		{
			return CastleAftermathActionApplyResult.Failed(
				SiegeCastlePrisonerDispositionProfile.RosterUnavailableReason,
				CastleAftermathRuntimeBridge.SelectedRegularPrisonerCount);
		}

		TroopRoster resolved = TroopRoster.CreateDummyTroopRoster();
		int affected = 0;
		int gold = 0;
		try
		{
			foreach (TroopRosterElement selectedElement in selected.GetTroopRoster().ToList())
			{
				CharacterObject character = selectedElement.Character;
				if (character == null || character.IsHero || selectedElement.Number <= 0)
				{
					continue;
				}
				int sourceIndex = mainPrisoners.FindIndexOfTroop(character);
				if (sourceIndex < 0)
				{
					continue;
				}
				TroopRosterElement sourceElement = mainPrisoners.GetElementCopyAtIndex(sourceIndex);
				int number = Math.Min(selectedElement.Number, sourceElement.Number);
				if (number <= 0)
				{
					continue;
				}
				int wounded = SiegeCastlePrisonerDispositionProfile.ResolveTransferredWounded(sourceElement.Number, sourceElement.WoundedNumber, number);
				int xp = SiegeCastlePrisonerDispositionProfile.ResolveTransferredXp(sourceElement.Number, sourceElement.Xp, number);
				resolved.AddToCounts(character, number, false, wounded, xp, true, -1);
				mainPrisoners.AddToCounts(character, -number, false, -wounded, -xp, true, -1);
				if (grantRansomGold && Campaign.Current?.Models?.RansomValueCalculationModel != null)
				{
					gold += Math.Max(0, Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(character, Hero.MainHero)) * number;
				}
				affected += number;
			}
			if (gold > 0 && Hero.MainHero != null)
			{
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, gold, disableNotification: true);
			}
			CastleAftermathRuntimeBridge.RemoveResolvedRegularPrisoners(resolved, source);
			return CastleAftermathActionApplyResult.Completed(
				affected,
				CastleAftermathRuntimeBridge.SelectedRegularPrisonerCount,
				grantRansomGold ? "sold" : "removed_for_" + source,
				gold);
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Resolve selected regular prisoners failed. Source=" + source
				+ ", Affected=" + affected + ", Error=" + ex);
			if (affected > 0)
			{
				CastleAftermathRuntimeBridge.RemoveResolvedRegularPrisoners(resolved, source + "_partial_error");
			}
			return new CastleAftermathActionApplyResult(
				affected > 0,
				affected,
				CastleAftermathRuntimeBridge.SelectedRegularPrisonerCount,
				SiegeCastlePrisonerDispositionProfile.ExceptionReasonPrefix + ex.GetType().Name);
		}
	}
}

internal sealed class CastleAftermathActionApplyResult
{
	internal CastleAftermathActionApplyResult(bool succeeded, int affectedCount, int remainingRegularPrisoners, string reasonCode, int gold = 0)
	{
		Succeeded = succeeded;
		AffectedCount = Math.Max(0, affectedCount);
		RemainingRegularPrisoners = Math.Max(0, remainingRegularPrisoners);
		ReasonCode = reasonCode ?? string.Empty;
		Gold = Math.Max(0, gold);
	}

	internal bool Succeeded { get; }

	internal int AffectedCount { get; }

	internal int RemainingRegularPrisoners { get; }

	internal string ReasonCode { get; }

	internal int Gold { get; }

	internal static CastleAftermathActionApplyResult Completed(int affectedCount, int remainingRegularPrisoners, string reasonCode, int gold = 0)
	{
		return new CastleAftermathActionApplyResult(true, affectedCount, remainingRegularPrisoners, reasonCode, gold);
	}

	internal static CastleAftermathActionApplyResult Failed(string reasonCode, int remainingRegularPrisoners)
	{
		return new CastleAftermathActionApplyResult(false, 0, remainingRegularPrisoners, reasonCode);
	}
}

using System;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using Helpers;
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
	internal static CastleAftermathActionApplyResult RecruitRegularPrisoners(TroopRoster requestedRoster)
	{
		TroopRoster selected = requestedRoster;
		TroopRoster mainPrisoners = PartyBase.MainParty?.PrisonRoster;
		TroopRoster mainMembers = MobileParty.MainParty?.MemberRoster;
		if (mainPrisoners == null || mainMembers == null || PartyBase.MainParty == null)
		{
			return CastleAftermathActionApplyResult.Failed(
				SiegeCastlePrisonerDispositionProfile.RosterUnavailableReason,
				CastleAftermathRuntimeBridge.SelectedRegularPrisonerCount);
		}

		int availableRegularPrisoners = CountRegular(selected);
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

	internal static CastleAftermathActionApplyResult BeginSlaughterOfRegularPrisoners(TroopRoster requestedRoster)
	{
		int selected = CountRegular(requestedRoster);
		if (selected <= 0)
		{
			return CastleAftermathActionApplyResult.Completed(0, 0, SiegeCastlePrisonerDispositionProfile.NoMatchingRegularPrisonersReason);
		}
		try
		{
			int started = CastleAftermathRuntimeBridge.BeginRegularPrisonerSlaughter(requestedRoster);
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

	internal static CastleAftermathActionApplyResult ReleaseRegularPrisoners(TroopRoster requestedRoster)
	{
		return RemoveRegularPrisoners(requestedRoster, "castle_release_prisoners");
	}

	internal static CastleAftermathActionApplyResult SellRegularPrisoners(TroopRoster requestedRoster)
	{
		TroopRoster selected = requestedRoster;
		TroopRoster mainPrisoners = PartyBase.MainParty?.PrisonRoster;
		if (selected == null || mainPrisoners == null || PartyBase.MainParty == null || Hero.MainHero == null)
		{
			return CastleAftermathActionApplyResult.Failed(
				SiegeCastlePrisonerDispositionProfile.RosterUnavailableReason,
				CastleAftermathRuntimeBridge.SelectedRegularPrisonerCount);
		}

		TroopRoster resolved = TroopRoster.CreateDummyTroopRoster();
		var sourceCounts = new System.Collections.Generic.Dictionary<CharacterObject, int>();
		int expectedGold = 0;
		int goldBefore = Hero.MainHero.Gold;
		try
		{
			TroopRoster sellable = MobilePartyHelper.GetPlayerPrisonersPlayerCanSell();
			foreach (TroopRosterElement selectedElement in selected.GetTroopRoster().ToList())
			{
				CharacterObject character = selectedElement.Character;
				if (character == null || character.IsHero || selectedElement.Number <= 0)
				{
					continue;
				}

				int sourceIndex = mainPrisoners.FindIndexOfTroop(character);
				int sellableIndex = sellable?.FindIndexOfTroop(character) ?? -1;
				if (sourceIndex < 0 || sellableIndex < 0)
				{
					continue;
				}

				TroopRosterElement sourceElement = mainPrisoners.GetElementCopyAtIndex(sourceIndex);
				TroopRosterElement sellableElement = sellable.GetElementCopyAtIndex(sellableIndex);
				int number = Math.Min(selectedElement.Number, Math.Min(sourceElement.Number, sellableElement.Number));
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
				sourceCounts[character] = sourceElement.Number;
				expectedGold += Campaign.Current.Models.RansomValueCalculationModel.PrisonerRansomValue(
					character,
					PartyBase.MainParty.LeaderHero) * number;
			}

			int affected = resolved.TotalManCount;
			if (affected <= 0)
			{
				return CastleAftermathActionApplyResult.Completed(
					0,
					CastleAftermathRuntimeBridge.SelectedRegularPrisonerCount,
					SiegeCastlePrisonerDispositionProfile.NoSellableRegularPrisonersReason);
			}

			SellPrisonersAction.ApplyForSelectedPrisoners(PartyBase.MainParty, null, resolved);
			int gold = Math.Max(0, Hero.MainHero.Gold - goldBefore);
			CastleAftermathRuntimeBridge.RemoveResolvedRegularPrisoners(resolved, "castle_sell_prisoners_vanilla");
			Logger.Log("CastleAftermath", "Sold selected regular prisoners through vanilla tavern action. Affected="
				+ affected + ", ExpectedGold=" + expectedGold + ", ActualGold=" + gold);
			return CastleAftermathActionApplyResult.Completed(
				affected,
				CastleAftermathRuntimeBridge.SelectedRegularPrisonerCount,
				"sold_via_vanilla_tavern_action",
				gold);
		}
		catch (Exception ex)
		{
			TroopRoster actuallySold = ResolveRemovedRoster(resolved, mainPrisoners, sourceCounts);
			int affected = actuallySold.TotalManCount;
			int gold = Math.Max(0, Hero.MainHero.Gold - goldBefore);
			if (affected > 0)
			{
				CastleAftermathRuntimeBridge.RemoveResolvedRegularPrisoners(
					actuallySold,
					"castle_sell_prisoners_vanilla_partial_error");
			}
			Logger.Log("CastleAftermath", "Vanilla sale of selected regular prisoners failed. Affected="
				+ affected + ", ExpectedGold=" + expectedGold + ", Error=" + ex);
			return new CastleAftermathActionApplyResult(
				affected > 0,
				affected,
				CastleAftermathRuntimeBridge.SelectedRegularPrisonerCount,
				SiegeCastlePrisonerDispositionProfile.ExceptionReasonPrefix + ex.GetType().Name,
				gold);
		}
	}

	internal static CastleAftermathActionApplyResult ResolveRegularPrisonersForSettlementEffect(TroopRoster requestedRoster, string source)
	{
		return RemoveRegularPrisoners(requestedRoster, source ?? "castle_prisoner_settlement_effect");
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

	private static CastleAftermathActionApplyResult RemoveRegularPrisoners(TroopRoster requestedRoster, string source)
	{
		TroopRoster selected = requestedRoster;
		TroopRoster mainPrisoners = PartyBase.MainParty?.PrisonRoster;
		if (selected == null || mainPrisoners == null)
		{
			return CastleAftermathActionApplyResult.Failed(
				SiegeCastlePrisonerDispositionProfile.RosterUnavailableReason,
				CastleAftermathRuntimeBridge.SelectedRegularPrisonerCount);
		}

		TroopRoster resolved = TroopRoster.CreateDummyTroopRoster();
		int affected = 0;
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
				affected += number;
			}
			CastleAftermathRuntimeBridge.RemoveResolvedRegularPrisoners(resolved, source);
			return CastleAftermathActionApplyResult.Completed(
				affected,
				CastleAftermathRuntimeBridge.SelectedRegularPrisonerCount,
				affected > 0
					? SiegeCastlePrisonerDispositionProfile.RemovedForReasonPrefix + source
					: SiegeCastlePrisonerDispositionProfile.NoMatchingRegularPrisonersReason);
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

	private static TroopRoster ResolveRemovedRoster(
		TroopRoster requested,
		TroopRoster current,
		System.Collections.Generic.IReadOnlyDictionary<CharacterObject, int> sourceCounts)
	{
		TroopRoster removed = TroopRoster.CreateDummyTroopRoster();
		if (requested == null || current == null || sourceCounts == null)
		{
			return removed;
		}

		foreach (TroopRosterElement element in requested.GetTroopRoster())
		{
			CharacterObject character = element.Character;
			if (character == null || !sourceCounts.TryGetValue(character, out int before))
			{
				continue;
			}
			int index = current.FindIndexOfTroop(character);
			int after = index >= 0 ? current.GetElementCopyAtIndex(index).Number : 0;
			int count = Math.Min(element.Number, Math.Max(0, before - after));
			if (count > 0)
			{
				removed.AddToCounts(character, count);
			}
		}
		return removed;
	}

	private static int CountRegular(TroopRoster roster)
	{
		return roster?.GetTroopRoster()
			.Where(element => element.Character != null && !element.Character.IsHero && element.Number > 0)
			.Sum(element => element.Number) ?? 0;
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

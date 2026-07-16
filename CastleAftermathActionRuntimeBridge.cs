using System;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
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

	internal static CastleAftermathActionApplyResult SlaughterSelectedRegularPrisoners()
	{
		TroopRoster selected = CastleAftermathRuntimeBridge.GetSelectedPrisonerRosterSnapshot();
		TroopRoster mainPrisoners = PartyBase.MainParty?.PrisonRoster;
		if (mainPrisoners == null)
		{
			return CastleAftermathActionApplyResult.Failed(
				SiegeCastlePrisonerDispositionProfile.RosterUnavailableReason,
				CastleAftermathRuntimeBridge.SelectedRegularPrisonerCount);
		}
		if (selected == null || CastleAftermathRuntimeBridge.SelectedRegularPrisonerCount <= 0)
		{
			return CastleAftermathActionApplyResult.Completed(
				0,
				0,
				SiegeCastlePrisonerDispositionProfile.NoMatchingRegularPrisonersReason);
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
				}
				catch
				{
					resolved.AddToCounts(character, -number, false, -wounded, -xp, true, -1);
					throw;
				}
				affected += number;
			}

			CastleAftermathRuntimeBridge.RemoveResolvedRegularPrisoners(resolved, "castle_slaughter_prisoners");
			return CastleAftermathActionApplyResult.Completed(
				affected,
				CastleAftermathRuntimeBridge.SelectedRegularPrisonerCount,
				affected > 0
					? SiegeCastlePrisonerDispositionProfile.SlaughteredReason
					: SiegeCastlePrisonerDispositionProfile.NoMatchingRegularPrisonersReason);
		}
		catch (Exception ex)
		{
			Logger.Log("CastleAftermath", "Slaughter selected regular prisoners failed after affected=" + affected + ": " + ex);
			if (affected > 0)
			{
				CastleAftermathRuntimeBridge.RemoveResolvedRegularPrisoners(resolved, "castle_slaughter_prisoners_partial_error");
			}
			return new CastleAftermathActionApplyResult(
				succeeded: affected > 0,
				affected,
				CastleAftermathRuntimeBridge.SelectedRegularPrisonerCount,
				SiegeCastlePrisonerDispositionProfile.ExceptionReasonPrefix + ex.GetType().Name);
		}
	}
}

internal sealed class CastleAftermathActionApplyResult
{
	internal CastleAftermathActionApplyResult(bool succeeded, int affectedCount, int remainingRegularPrisoners, string reasonCode)
	{
		Succeeded = succeeded;
		AffectedCount = Math.Max(0, affectedCount);
		RemainingRegularPrisoners = Math.Max(0, remainingRegularPrisoners);
		ReasonCode = reasonCode ?? string.Empty;
	}

	internal bool Succeeded { get; }

	internal int AffectedCount { get; }

	internal int RemainingRegularPrisoners { get; }

	internal string ReasonCode { get; }

	internal static CastleAftermathActionApplyResult Completed(int affectedCount, int remainingRegularPrisoners, string reasonCode)
	{
		return new CastleAftermathActionApplyResult(true, affectedCount, remainingRegularPrisoners, reasonCode);
	}

	internal static CastleAftermathActionApplyResult Failed(string reasonCode, int remainingRegularPrisoners)
	{
		return new CastleAftermathActionApplyResult(false, 0, remainingRegularPrisoners, reasonCode);
	}
}

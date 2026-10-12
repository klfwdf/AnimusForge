using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using Helpers;
using Newtonsoft.Json;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.Map;
using TaleWorlds.CampaignSystem.MapEvents;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.Siege;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

public sealed partial class WorldMapPartyCommandBehavior : CampaignBehaviorBase
{
	private void ProcessPendingCreateCompanionPartyRequests()
	{
		if (Volatile.Read(ref _hasPendingCreateCompanionPartyRequests) == 0)
		{
			return;
		}
		lock (_queueLock)
		{
			if (_pendingCreatePartyRequests.Count == 0)
			{
				return;
			}
		}
		if (_createCompanionPartyScreenRequests.HasActiveRequest || !CanOpenCreateCompanionPartyScreenNow(out _))
		{
			return;
		}
		PendingCreateCompanionPartyRequest request = null;
		lock (_queueLock)
		{
			if (_pendingCreatePartyRequests.Count == 0)
			{
				return;
			}
			request = _pendingCreatePartyRequests.Values.FirstOrDefault();
			if (request != null)
			{
				_pendingCreatePartyRequests.Remove(request.HeroId ?? "");
			}
			Volatile.Write(ref _hasPendingCreateCompanionPartyRequests, _pendingCreatePartyRequests.Count > 0 ? 1 : 0);
		}
		if (request == null || string.IsNullOrWhiteSpace(request.HeroId))
		{
			return;
		}
		Hero hero = ResolveHeroByIdAny(request.HeroId);
		if (hero == null)
		{
			Log("pending create skipped missing hero=" + request.HeroId);
			return;
		}
		if (!TryOpenCreateCompanionParty(hero, request.FollowUpCommands, out string message))
		{
			LogFact(hero, GetHeroName(hero) + WorldMapSourceTexts.Get("raw_literals.wm_lit_0003") + message);
		}
	}

	private bool TryStartGovernorExpeditionRequest(Hero hero, List<PartyCommandEntry> followUpCommands, out string message, out bool queuedForChannelExit)
	{
		message = "";
		queuedForChannelExit = false;
		List<PartyCommandEntry> safeCommands = SanitizeFollowUpCommands(followUpCommands);
		if (safeCommands.Count == 0 || !safeCommands.Any(IsTaskCommandForImplicitPartyCreation))
		{
			message = WorldMapUiText.GovernorNoTask;
			return false;
		}
		if (!TryValidateGovernorExpeditionCandidate(hero, out Settlement origin, out MobileParty _, out int _, out string validationMessage))
		{
			message = validationMessage;
			return false;
		}
		if (!CanCreateGovernorExpeditionNow(out string blockedReason))
		{
			if (!TryQueuePendingGovernorExpedition(hero, origin, safeCommands, out message))
			{
				return false;
			}
			queuedForChannelExit = true;
			message = WorldMapUiText.GovernorRequestQueued(GetHeroName(hero), blockedReason);
			return true;
		}
		return TryCreateGovernorExpeditionNow(hero, origin.StringId, origin.OwnerClan?.StringId, safeCommands, out message);
	}

	private static bool CanCreateGovernorExpeditionNow(out string blockedReason)
	{
		blockedReason = "";
		if (Campaign.Current == null)
		{
			blockedReason = WorldMapUiText.Result("AF_WM_GovernorBlockedCampaign", "the campaign system is not ready");
			return false;
		}
		if (Mission.Current != null)
		{
			blockedReason = WorldMapUiText.Result("AF_WM_GovernorBlockedScene", "the player is still in a mission");
			return false;
		}
		if (Campaign.Current.ConversationManager?.IsConversationInProgress == true)
		{
			blockedReason = WorldMapUiText.Result("AF_WM_GovernorBlockedConversation", "a conversation is still in progress");
			return false;
		}
		if (IsPartyScreenStillActive())
		{
			blockedReason = WorldMapUiText.Result("AF_WM_GovernorBlockedPartyScreen", "a party screen is already open");
			return false;
		}
		return true;
	}

	private bool TryQueuePendingGovernorExpedition(Hero hero, Settlement origin, List<PartyCommandEntry> commands, out string message)
	{
		message = "";
		if (hero == null || origin == null || string.IsNullOrWhiteSpace(hero.StringId))
		{
			message = WorldMapUiText.Result("AF_WM_GovernorRequestMissingIdentity", "The governor expedition request is missing required identity information.");
			return false;
		}
		lock (_queueLock)
		{
			if (_pendingGovernorExpeditionRequests.TryGetValue(hero.StringId, out PendingGovernorExpeditionRequest existing) && existing != null)
			{
				if (!_governorExpeditionRequestTickets.IsCurrent(hero.StringId, existing.RequestTicket))
				{
					message = WorldMapUiText.Result("AF_WM_GovernorRequestTicketExpired", "An existing governor expedition request has an expired runtime ticket.");
					return false;
				}
				if (!string.Equals(existing.OriginSettlementId, origin.StringId, StringComparison.OrdinalIgnoreCase)
					|| !string.Equals(existing.OriginClanId, origin.OwnerClan?.StringId ?? "", StringComparison.OrdinalIgnoreCase))
				{
					message = WorldMapUiText.Result("AF_WM_GovernorRequestStateChanged", "A pending governor expedition request already exists, but its settlement or clan has changed.");
					return false;
				}
				existing.FollowUpCommands = existing.FollowUpCommands ?? new List<PartyCommandEntry>();
				existing.FollowUpCommands.AddRange(SanitizeFollowUpCommands(commands));
			}
			else
			{
				long requestTicket = _governorExpeditionRequestTickets.GetOrCreate(hero.StringId);
				if (requestTicket <= 0L)
				{
					message = WorldMapUiText.Result("AF_WM_GovernorRequestTicketFailed", "The governor expedition request ticket could not be created.");
					return false;
				}
				_pendingGovernorExpeditionRequests[hero.StringId] = new PendingGovernorExpeditionRequest
				{
					RequestTicket = requestTicket,
					HeroId = hero.StringId,
					OriginSettlementId = origin.StringId,
					OriginClanId = origin.OwnerClan?.StringId ?? "",
					FollowUpCommands = SanitizeFollowUpCommands(commands)
				};
			}
			Volatile.Write(ref _hasPendingGovernorExpeditionRequests, 1);
		}
		Log("queued governor expedition hero=" + hero.StringId + " origin=" + origin.StringId + " commands=" + (commands?.Count ?? 0));
		return true;
	}

	private void ProcessPendingGovernorExpeditionRequests()
	{
		if (Volatile.Read(ref _hasPendingGovernorExpeditionRequests) == 0 || !CanCreateGovernorExpeditionNow(out _))
		{
			return;
		}
		PendingGovernorExpeditionRequest request = null;
		lock (_queueLock)
		{
			request = _pendingGovernorExpeditionRequests.Values.FirstOrDefault();
			if (request != null && _governorExpeditionRequestTickets.TryClaim(request.HeroId, request.RequestTicket))
			{
				_pendingGovernorExpeditionRequests.Remove(request.HeroId ?? "");
			}
			else
			{
				request = null;
			}
			Volatile.Write(ref _hasPendingGovernorExpeditionRequests, _pendingGovernorExpeditionRequests.Count > 0 ? 1 : 0);
		}
		if (request == null || string.IsNullOrWhiteSpace(request.HeroId))
		{
			return;
		}
		Hero hero = ResolveHeroByIdAny(request.HeroId);
		if (hero == null)
		{
			Log("pending governor expedition skipped missing hero=" + request.HeroId);
			return;
		}
		if (!TryCreateGovernorExpeditionNow(hero, request.OriginSettlementId, request.OriginClanId, request.FollowUpCommands, out string message))
		{
			LogFact(hero, GetHeroName(hero) + WorldMapSourceTexts.Get("raw_literals.wm_lit_0005") + message);
		}
	}

	private bool TryOpenCreateCompanionParty(Hero hero, List<PartyCommandEntry> followUpCommands, out string message)
	{
		message = "";
		try
		{
			List<PartyCommandEntry> safeFollowUpCommands = SanitizeFollowUpCommands(followUpCommands);
			if (!IsHeroActuallyInPlayerMainPartyRoster(hero))
			{
				message = WorldMapUiText.Result("AF_WM_CompanionHeroMissing", "Only a non-player Hero present in the player's main-party roster can implicitly create a task party.");
				return false;
			}
			if (!CanOpenCreateCompanionPartyScreenNow(out string blockedReason))
			{
				QueuePendingCreateCompanionParty(hero, safeFollowUpCommands);
				message = WorldMapUiText.CompanionRequestQueued(GetHeroName(hero), blockedReason);
				return true;
			}
			return OpenCreateCompanionPartyScreen(hero, safeFollowUpCommands, out message);
		}
		catch (Exception ex)
		{
			message = WorldMapUiText.Result("AF_WM_CompanionScreenOpenFailed", "Opening the native companion-party screen failed: {ERROR}", ("ERROR", ex.Message));
			Log("create companion party failed hero=" + (hero?.StringId ?? "") + " error=" + ex);
			return false;
		}
	}

	private static List<PartyCommandEntry> SanitizeFollowUpCommands(List<PartyCommandEntry> followUpCommands)
	{
		return (followUpCommands ?? new List<PartyCommandEntry>())
			.Where(command => command != null && IsExecutableCommand(command))
			.Select(CloneCommand)
			.ToList();
	}

	private void QueuePendingCreateCompanionParty(Hero hero, List<PartyCommandEntry> followUpCommands)
	{
		if (hero == null || string.IsNullOrWhiteSpace(hero.StringId))
		{
			return;
		}
		lock (_queueLock)
		{
			List<PartyCommandEntry> commands = SanitizeFollowUpCommands(followUpCommands);
			if (_pendingCreatePartyRequests.TryGetValue(hero.StringId, out PendingCreateCompanionPartyRequest existing) && existing != null)
			{
				existing.FollowUpCommands = existing.FollowUpCommands ?? new List<PartyCommandEntry>();
				existing.FollowUpCommands.AddRange(commands);
			}
			else
			{
				_pendingCreatePartyRequests[hero.StringId] = new PendingCreateCompanionPartyRequest
				{
					HeroId = hero.StringId,
					FollowUpCommands = commands
				};
			}
			Volatile.Write(ref _hasPendingCreateCompanionPartyRequests, _pendingCreatePartyRequests.Count > 0 ? 1 : 0);
		}
		Log("queued create companion party hero=" + hero.StringId + " followUp=" + (followUpCommands?.Count ?? 0));
	}

	private static bool CanOpenCreateCompanionPartyScreenNow(out string blockedReason)
	{
		blockedReason = "";
		if (Mission.Current != null)
		{
			blockedReason = WorldMapUiText.Result("AF_WM_CompanionBlockedMission", "the player is still in a mission or review");
			return false;
		}
		if (Campaign.Current?.ConversationManager?.IsConversationInProgress == true)
		{
			blockedReason = WorldMapUiText.Result("AF_WM_CompanionBlockedConversation", "a conversation is still in progress");
			return false;
		}
		if (IsPartyScreenStillActive())
		{
			blockedReason = WorldMapUiText.Result("AF_WM_CompanionBlockedPartyScreen", "a party screen is already open");
			return false;
		}
		if (Game.Current?.GameStateManager == null)
		{
			blockedReason = WorldMapUiText.Result("AF_WM_CompanionBlockedGameState", "the game UI state is not ready");
			return false;
		}
		if (!IsPartyUsable(MobileParty.MainParty))
		{
			blockedReason = WorldMapUiText.Result("AF_WM_CompanionBlockedMainParty", "the player's main party is unavailable");
			return false;
		}
		return true;
	}

	private static bool IsPartyScreenStillActive()
	{
		try
		{
			string activeStateName = Game.Current?.GameStateManager?.ActiveState?.GetType().Name ?? "";
			return activeStateName.IndexOf("PartyState", StringComparison.OrdinalIgnoreCase) >= 0;
		}
		catch
		{
			return false;
		}
	}

	private bool OpenCreateCompanionPartyScreen(Hero hero, List<PartyCommandEntry> followUpCommands, out string message)
	{
		message = "";
		if (!_createCompanionPartyScreenRequests.TryBegin(out long requestTicket))
		{
			message = WorldMapUiText.Result("AF_WM_CompanionScreenPending", "A companion-party creation screen is already waiting to complete.");
			return false;
		}
		try
		{
			List<PartyCommandEntry> safeFollowUpCommands = SanitizeFollowUpCommands(followUpCommands);
			PartyScreenClosedDelegate onClosed = (leftOwnerParty, leftMemberRoster, leftPrisonRoster, rightOwnerParty, rightMemberRoster, rightPrisonRoster, fromCancel) =>
			{
				if (!_createCompanionPartyScreenRequests.TryClaimCompletion(requestTicket))
				{
					Log("ignored stale or duplicate create companion party callback hero=" + (hero?.StringId ?? "") + " ticket=" + requestTicket);
					return;
				}
				OnCreateCompanionPartyScreenClosed(hero.StringId, safeFollowUpCommands, leftMemberRoster, leftPrisonRoster, rightOwnerParty, fromCancel);
			};
			if (hero.Clan != null)
			{
				PartyScreenHelper.OpenScreenAsCreateClanPartyForHero(hero, onClosed);
			}
			else
			{
				OpenClanlessHeroCreatePartyScreen(hero, onClosed);
			}
			message = WorldMapUiText.Result("AF_WM_CompanionScreenOpened", "Opened the party-splitting screen for {HERO}.", ("HERO", GetHeroName(hero)));
			return true;
		}
		catch (Exception ex)
		{
			_createCompanionPartyScreenRequests.TryCancelOpen(requestTicket);
			message = WorldMapUiText.Result("AF_WM_CompanionScreenOpenFailed", "Opening the native companion-party screen failed: {ERROR}", ("ERROR", ex.Message));
			Log("open create companion party screen failed hero=" + (hero?.StringId ?? "") + " error=" + ex);
			return false;
		}
	}

	private static void OpenClanlessHeroCreatePartyScreen(Hero hero, PartyScreenClosedDelegate onClosed)
	{
		TroopRoster leftMembers = TroopRoster.CreateDummyTroopRoster();
		TroopRoster leftPrisoners = TroopRoster.CreateDummyTroopRoster();
		TroopRoster rightMembers = MobileParty.MainParty.MemberRoster.CloneRosterData();
		TroopRoster rightPrisoners = MobileParty.MainParty.PrisonRoster.CloneRosterData();
		leftMembers.AddToCounts(hero.CharacterObject, 1, false, 0, 0, true, -1);
		if (rightMembers.Contains(hero.CharacterObject))
		{
			rightMembers.AddToCounts(hero.CharacterObject, -1, false, 0, 0, true, -1);
		}
		TextObject partyName = new TextObject("{=AF_WM_PartyName}{HERO}'s party");
		partyName.SetTextVariable("HERO", hero.Name);
		int partyLimit = Math.Max(1, MobileParty.MainParty?.Party?.PartySizeLimit ?? 1);
		try
		{
			Clan capacityClan = Clan.PlayerClan;
			if (capacityClan != null)
			{
				partyLimit = Math.Max(1, Campaign.Current.Models.PartySizeLimitModel.GetAssumedPartySizeForLordParty(hero, capacityClan.MapFaction, capacityClan));
			}
		}
		catch
		{
		}
		PartyScreenHelper.OpenScreenWithDummyRoster(
			leftMembers,
			leftPrisoners,
			rightMembers,
			rightPrisoners,
			partyName,
			MobileParty.MainParty.Name,
			partyLimit,
			MobileParty.MainParty.Party.PartySizeLimit,
			null,
			onClosed,
			new IsTroopTransferableDelegate(CreatePartyTroopTransferable));
	}

	private static bool CreatePartyTroopTransferable(CharacterObject character, PartyScreenLogic.TroopType type, PartyScreenLogic.PartyRosterSide side, PartyBase leftOwnerParty)
	{
		return character?.IsHero != true;
	}

	private void OnCreateCompanionPartyScreenClosed(string heroId, List<PartyCommandEntry> followUpCommands, TroopRoster leftMemberRoster, TroopRoster leftPrisonRoster, PartyBase rightOwnerParty, bool fromCancel)
	{
		Hero hero = ResolveHeroByIdAny(heroId);
		MobileParty createdParty = null;
		try
		{
			if (hero == null)
			{
				Log("create companion party closed missing hero=" + (heroId ?? ""));
				return;
			}
			if (fromCancel)
			{
				LogFact(hero, GetHeroName(hero) + WorldMapSourceTexts.Get("raw_literals.wm_lit_0233"));
				return;
			}
			Hero partyHero = FindHeroInRoster(leftMemberRoster) ?? hero;
			int partyGoldLowerThreshold = Campaign.Current.Models.ClanFinanceModel.PartyGoldLowerThreshold;
			if (partyHero.Gold < partyGoldLowerThreshold)
			{
				GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, partyHero, partyGoldLowerThreshold - partyHero.Gold, false);
			}
			createdParty = MobilePartyHelper.CreateNewClanMobileParty(partyHero, partyHero.Clan);
			if (!IsPartyUsable(createdParty) || createdParty.LeaderHero != partyHero)
			{
				throw new InvalidOperationException(WorldMapSourceTexts.Get("raw_literals.wm_lit_0234"));
			}
			RegisterPlayerDetachedParty(partyHero, createdParty);
			int movedMembers = MoveSelectedTroopsToCreatedParty(createdParty, partyHero, leftMemberRoster, rightOwnerParty);
			int movedPrisoners = MoveSelectedPrisonersToCreatedParty(createdParty, leftPrisonRoster, rightOwnerParty);
			LogFact(partyHero, GetHeroName(partyHero) + WorldMapSourceTexts.Get("raw_literals.wm_lit_0235") + movedMembers + WorldMapSourceTexts.Get("raw_literals.wm_lit_0236") + (movedPrisoners > 0 ? ("、" + movedPrisoners + WorldMapSourceTexts.Get("raw_literals.wm_lit_0237")) : "") + "。");
			if (followUpCommands != null && followUpCommands.Count > 0)
			{
				if (TryAppendQueue(partyHero, followUpCommands, out string fact, out string queueMessage, out _))
				{
					if (!string.IsNullOrWhiteSpace(fact))
					{
						MyBehavior.AppendExternalDialogueHistory(partyHero, null, null, fact);
					}
					DisplayCommandMessage(queueMessage, isFailure: false);
				}
				else
				{
					LogFact(partyHero, GetHeroName(partyHero) + WorldMapSourceTexts.Get("raw_literals.wm_lit_0238") + queueMessage);
				}
			}
		}
		catch (Exception ex)
		{
			Log("create companion party close failed hero=" + (heroId ?? "") + " error=" + ex);
			if (hero != null)
			{
				EnsureHeroRemainsAvailableAfterCreateFailure(hero, createdParty);
				LogFact(hero, GetHeroName(hero) + WorldMapSourceTexts.Get("raw_literals.wm_lit_0239") + ex.Message);
			}
		}
	}
}

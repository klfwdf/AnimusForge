using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Thin Bannerlord adapter for GCCZ village administration.
/// Authority, tags, values, wording and culture modes live in the standalone GCCZ core.
/// </summary>
public sealed class VillageAftermathBehavior : CampaignBehaviorBase
{
	private const uint SuccessColor = 0xFFB6F7A8u;
	private const uint WarningColor = 0xFFFFD27Fu;
	private const uint DestructiveColor = 0xFFFF7777u;

	private static Mission _activeMission;
	private static string _activeVillageId = "";
	private static string _pendingIncidentVillageId = "";
	private static DateTime _pendingIncidentQueuedUtc = DateTime.MinValue;
	private static VillageAftermathAuthorityKind _activeAuthorityKind;
	private static bool _cultureInquiryPending;
	private static readonly HashSet<VillageAftermathActionKind> AppliedActions = new HashSet<VillageAftermathActionKind>();

	private Dictionary<string, string> _gradualCultureTargetByVillageId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private Dictionary<string, int> _gradualCultureFinishDayByVillageId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	public override void RegisterEvents()
	{
		CampaignEvents.DailyTickEvent.AddNonSerializedListener(this, OnDailyTick);
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, OnNewGameCreated);
		CampaignEvents.OnGameLoadedEvent.AddNonSerializedListener(this, OnGameLoaded);
	}

	public override void SyncData(IDataStore dataStore)
	{
		dataStore.SyncData("_gcczVillageGradualCultureTargets_v1", ref _gradualCultureTargetByVillageId);
		dataStore.SyncData("_gcczVillageGradualCultureFinishDays_v1", ref _gradualCultureFinishDayByVillageId);
		_gradualCultureTargetByVillageId ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		_gradualCultureFinishDayByVillageId ??= new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
	}

	private void OnNewGameCreated(CampaignGameStarter starter)
	{
		_pendingIncidentVillageId = "";
		_pendingIncidentQueuedUtc = DateTime.MinValue;
		ClearMissionState("new_game");
		_gradualCultureTargetByVillageId.Clear();
		_gradualCultureFinishDayByVillageId.Clear();
	}

	private void OnGameLoaded(CampaignGameStarter starter)
	{
		_pendingIncidentVillageId = "";
		_pendingIncidentQueuedUtc = DateTime.MinValue;
		ClearMissionState("game_loaded");
	}

	internal static bool QueueIncidentDispositionMission(Settlement villageSettlement, string source)
	{
		if (villageSettlement?.IsVillage != true || villageSettlement.Village == null || string.IsNullOrWhiteSpace(villageSettlement.StringId))
		{
			return false;
		}
		_pendingIncidentVillageId = villageSettlement.StringId;
		_pendingIncidentQueuedUtc = DateTime.UtcNow;
		GcczDiagnosticLog.Log("VillageEntry", "queued incident disposition village=" + _pendingIncidentVillageId
			+ " source=" + (source ?? "N/A"));
		return true;
	}

	internal static bool TryConsumeQueuedIncidentDisposition(Settlement villageSettlement, string source)
	{
		if (villageSettlement?.IsVillage != true
			|| !string.Equals(_pendingIncidentVillageId, villageSettlement.StringId, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		bool expired = _pendingIncidentQueuedUtc == DateTime.MinValue
			|| (DateTime.UtcNow - _pendingIncidentQueuedUtc).TotalSeconds > 30d;
		_pendingIncidentVillageId = "";
		_pendingIncidentQueuedUtc = DateTime.MinValue;
		GcczDiagnosticLog.Log("VillageEntry", (expired ? "expired" : "consumed")
			+ " queued incident disposition village=" + (villageSettlement.StringId ?? "N/A")
			+ " source=" + (source ?? "N/A"));
		return !expired;
	}

	internal static void CancelQueuedIncidentDisposition(string source)
	{
		if (!string.IsNullOrWhiteSpace(_pendingIncidentVillageId))
		{
			GcczDiagnosticLog.Log("VillageEntry", "cancelled queued incident disposition village=" + _pendingIncidentVillageId
				+ " source=" + (source ?? "N/A"));
		}
		_pendingIncidentVillageId = "";
		_pendingIncidentQueuedUtc = DateTime.MinValue;
	}

	internal static bool TryActivateForSetsVillage(Settlement villageSettlement, Mission mission, string source)
	{
		try
		{
			if (mission == null || villageSettlement?.IsVillage != true || villageSettlement.Village == null)
			{
				return false;
			}
			Settlement bound = villageSettlement.Village.Bound;
			Clan playerClan = Clan.PlayerClan;
			Kingdom playerKingdom = playerClan?.Kingdom ?? Hero.MainHero?.Clan?.Kingdom;
			bool playerIsRuler = playerKingdom != null
				&& (playerKingdom.RulingClan == playerClan || playerKingdom.Leader == Hero.MainHero);
			VillageAftermathEntryDecision decision = VillageAftermathEntryPolicy.Evaluate(
				new VillageAftermathEntryFacts(
					isVillage: true,
					hasBoundSettlement: bound != null,
					villageStateIsNormal: villageSettlement.Village.VillageState == Village.VillageStates.Normal,
					isUnderSiegeOrRaid: villageSettlement.IsUnderSiege || villageSettlement.Party?.MapEvent != null,
					playerClanOwnsBoundSettlement: bound?.OwnerClan == playerClan,
					playerIsKingdomRuler: playerIsRuler,
					boundSettlementBelongsToPlayerKingdom: playerKingdom != null && bound?.OwnerClan?.Kingdom == playerKingdom));
			if (!decision.Allowed)
			{
				GcczDiagnosticLog.LogVerbose("VillageEntry", "denied village=" + (villageSettlement.StringId ?? "N/A")
					+ " bound=" + (bound?.StringId ?? "N/A")
					+ " reason=" + decision.ReasonCode
					+ " source=" + (source ?? "N/A"));
				return false;
			}

			_activeMission = mission;
			_activeVillageId = villageSettlement.StringId ?? "";
			_activeAuthorityKind = decision.AuthorityKind;
			_cultureInquiryPending = false;
			AppliedActions.Clear();
			GcczDiagnosticLog.Log("VillageEntry", "activated village=" + _activeVillageId
				+ " bound=" + (bound?.StringId ?? "N/A")
				+ " authority=" + _activeAuthorityKind
				+ " source=" + (source ?? "N/A"));
			InformationManager.DisplayMessage(new InformationMessage(
				SiegeAftermathUiText.VillageActivated,
				Color.FromUint(SuccessColor)));
			return true;
		}
		catch (Exception ex)
		{
			Logger.Log("VillageAftermath", "Activate failed: " + ex);
			return false;
		}
	}

	internal static bool IsActive()
	{
		return _activeMission != null
			&& ReferenceEquals(_activeMission, Mission.Current)
			&& !string.IsNullOrWhiteSpace(_activeVillageId);
	}

	internal static bool IsActiveForMission(Mission mission)
	{
		return mission != null && IsActive() && ReferenceEquals(_activeMission, mission);
	}

	internal static void TickForSetsMission(Mission mission)
	{
		if (!IsActiveForMission(mission) || !_cultureInquiryPending)
		{
			return;
		}
		if (mission.Mode == MissionMode.Conversation || mission.Mode == MissionMode.Barter)
		{
			return;
		}
		_cultureInquiryPending = false;
		try
		{
			OpenCultureModeInquiry();
		}
		catch (Exception ex)
		{
			Logger.Log("VillageAftermath", "Open culture inquiry failed: " + ex);
			TryDisplayFailureMessage(SiegeAftermathUiText.VillageCultureFailure(false));
		}
	}

	internal static void EndForSetsMission(Mission mission, string source)
	{
		if (mission == null || !ReferenceEquals(_activeMission, mission))
		{
			return;
		}
		ClearMissionState(source);
	}

	internal static string BuildRuntimePromptForExternal()
	{
		Settlement village = ResolveActiveVillage();
		return IsActive()
			? VillageAftermathRuntimePromptProfile.BuildPrompt(village?.Name?.ToString(), village?.Village?.Bound?.Name?.ToString(), _activeAuthorityKind, textCatalog: GcczTownPromptResourceProvider.GetCatalog())
			: string.Empty;
	}

	internal static List<PostprocessRuleEntry> BuildPostprocessRulesForExternal(bool replyIsDirectPlayerResponse)
	{
		if (!IsActive() || !replyIsDirectPlayerResponse)
		{
			return new List<PostprocessRuleEntry>();
		}

		return new List<PostprocessRuleEntry>
		{
			Rule(VillageAftermathActionTagCatalog.GatherEldersTag, (GcczTownPromptResourceProvider.UseEnglishPrompt ? "Emit only when the player explicitly asks to gather villagers, elders or headmen; place adult villagers in civilian formation 3." : SiegeAftermathCompatibilityTexts.Raw("auto_village_71a959836d48"))),
			Rule(VillageAftermathActionTagCatalog.RestrainTroopsTag, (GcczTownPromptResourceProvider.UseEnglishPrompt ? "Emit only when the player explicitly asks to restrain accompanying troops and stop disturbing civilians." : SiegeAftermathCompatibilityTexts.Raw("auto_village_eb81fdbfe883"))),
			Rule(VillageAftermathActionTagCatalog.PacifyTag, (GcczTownPromptResourceProvider.UseEnglishPrompt ? "Emit only when the player explicitly decides to pacify the village and end the pursuit of blame." : SiegeAftermathCompatibilityTexts.Raw("auto_village_d5aaa911e197"))),
			Rule(VillageAftermathActionTagCatalog.ReliefTag, (GcczTownPromptResourceProvider.UseEnglishPrompt ? "Emit only when the player explicitly decides to pay denars for village relief." : SiegeAftermathCompatibilityTexts.Raw("auto_village_587d0579c543"))),
			Rule(VillageAftermathActionTagCatalog.FineTag, (GcczTownPromptResourceProvider.UseEnglishPrompt ? "Emit only when the player explicitly decides to demand a fine from the village." : SiegeAftermathCompatibilityTexts.Raw("auto_village_4e3d1b886b3d"))),
			Rule(VillageAftermathActionTagCatalog.RequisitionFoodTag, (GcczTownPromptResourceProvider.UseEnglishPrompt ? "Emit only when the player explicitly decides to requisition food." : SiegeAftermathCompatibilityTexts.Raw("auto_village_0ba93283dc50"))),
			Rule(VillageAftermathActionTagCatalog.RequisitionProduceTag, (GcczTownPromptResourceProvider.UseEnglishPrompt ? "Emit only when the player explicitly decides to requisition village produce." : SiegeAftermathCompatibilityTexts.Raw("auto_village_da1dad5f51f3"))),
			Rule(VillageAftermathActionTagCatalog.RequisitionLivestockTag, (GcczTownPromptResourceProvider.UseEnglishPrompt ? "Emit only when the player explicitly decides to requisition livestock." : SiegeAftermathCompatibilityTexts.Raw("auto_village_e206eb1f8f8f"))),
			Rule(VillageAftermathActionTagCatalog.LevyRecruitsTag, (GcczTownPromptResourceProvider.UseEnglishPrompt ? "Emit only when the player explicitly decides to levy recruits from the village militia." : SiegeAftermathCompatibilityTexts.Raw("auto_village_ab66b507a1df"))),
			Rule(VillageAftermathActionTagCatalog.PunishRingleaderTag, (GcczTownPromptResourceProvider.UseEnglishPrompt ? "Emit only when the player explicitly decides to punish the village ringleader." : SiegeAftermathCompatibilityTexts.Raw("auto_village_aa047ffa0a27"))),
			Rule(VillageAftermathActionTagCatalog.ConfiscatePropertyTag, (GcczTownPromptResourceProvider.UseEnglishPrompt ? "Emit only when the player explicitly decides to confiscate village property." : SiegeAftermathCompatibilityTexts.Raw("auto_village_72c9eaf28061"))),
			Rule(VillageAftermathActionTagCatalog.DestroyLivelihoodTag, (GcczTownPromptResourceProvider.UseEnglishPrompt ? "Emit only when the player explicitly decides to destroy village production and livelihoods." : SiegeAftermathCompatibilityTexts.Raw("auto_village_c09e3bdbb9f9"))),
			Rule(VillageAftermathActionTagCatalog.MassacreTag, (GcczTownPromptResourceProvider.UseEnglishPrompt ? "Emit only when the player explicitly decides to massacre the village." : SiegeAftermathCompatibilityTexts.Raw("auto_village_8fc1c244b8f7"))),
			Rule(VillageAftermathActionTagCatalog.CulturalReformTag, (GcczTownPromptResourceProvider.UseEnglishPrompt ? "Emit only when the player explicitly requests a change of village culture; only open the player confirmation UI." : SiegeAftermathCompatibilityTexts.Raw("auto_village_ed6b7dc90a5c"))),
		};
	}

	internal static string BuildPostprocessContextForExternal(bool replyIsDirectPlayerResponse)
	{
		if (!IsActive())
		{
			return string.Empty;
		}
		Settlement village = ResolveActiveVillage();
		if (GcczTownPromptResourceProvider.UseEnglishPrompt)
            return "Current village=" + (village?.Name?.ToString() ?? "Unknown")
                + "; bound settlement=" + (village?.Village?.Bound?.Name?.ToString() ?? "Unknown")
                + "; player authority=" + _activeAuthorityKind
                + "; direct reply to player=" + replyIsDirectPlayerResponse
                + ". GCCZ village tags must not replace or block ordinary AF or native tags.";
		return SiegeAftermathCompatibilityTexts.Raw("auto_village_210e9ee4acd2") + (village?.Name?.ToString() ?? SiegeAftermathCompatibilityTexts.Raw("auto_village_d9c32a4c3dda"))
			+ SiegeAftermathCompatibilityTexts.Raw("auto_village_fbc6f6884123") + (village?.Village?.Bound?.Name?.ToString() ?? SiegeAftermathCompatibilityTexts.Raw("auto_village_d9c32a4c3dda"))
			+ SiegeAftermathCompatibilityTexts.Raw("auto_village_5764d1d108d0") + _activeAuthorityKind
			+ SiegeAftermathCompatibilityTexts.Raw("auto_village_9c5a9e0b2b31") + replyIsDirectPlayerResponse
			+ SiegeAftermathCompatibilityTexts.Raw("auto_village_f2b367a5fec9");
	}

	internal static string NormalizePostprocessTagsForExternal(string raw, List<PostprocessRuleEntry> rules)
	{
		if (!IsActive() || string.IsNullOrWhiteSpace(raw))
		{
			return string.Empty;
		}
		HashSet<string> allowed = (rules ?? new List<PostprocessRuleEntry>())
			.Select(rule => (rule?.Tag ?? string.Empty).Trim())
			.Where(tag => !string.IsNullOrWhiteSpace(tag))
			.ToHashSet(StringComparer.OrdinalIgnoreCase);
		List<string> tags = new List<string>();
		foreach (VillageAftermathActionKind kind in VillageAftermathActionTagCatalog.ExtractKinds(raw))
		{
			if (VillageAftermathActionTagCatalog.TryGetCanonicalTag(kind, out string tag) && allowed.Contains(tag))
			{
				tags.Add(tag);
			}
		}
		return string.Join("\n", tags);
	}

	internal static bool TryProcessActionTagsForExternal(
		int targetAgentIndex,
		ref string text,
		out bool actionHandled,
		bool replyIsDirectPlayerResponse)
	{
		actionHandled = false;
		IReadOnlyList<VillageAftermathActionKind> actions = VillageAftermathActionTagCatalog.ExtractKinds(text);
		if (actions.Count == 0)
		{
			return false;
		}
		text = VillageAftermathActionTagCatalog.RemoveTags(text);
		if (!IsActive() || !replyIsDirectPlayerResponse || !IsValidMissionSpeaker(targetAgentIndex))
		{
			GcczDiagnosticLog.Log("VillageAction", "stripped unauthorized tags direct=" + replyIsDirectPlayerResponse
				+ " targetAgent=" + targetAgentIndex
				+ " active=" + IsActive());
			return true;
		}

		foreach (VillageAftermathActionKind action in actions)
		{
			if (action == VillageAftermathActionKind.GatherElders)
			{
				actionHandled |= SettlementEntryTroopSelectionBehavior.TryGatherSettlementCiviliansForExternal(
					targetAgentIndex,
					SetsSettlementCivilianGatherProfile.AiActionTagSource);
				continue;
			}
			if (action == VillageAftermathActionKind.CulturalReform)
			{
				if (!AppliedActions.Contains(action) && !_cultureInquiryPending)
				{
					_cultureInquiryPending = true;
					actionHandled = true;
					InformationManager.DisplayMessage(new InformationMessage(SiegeAftermathUiText.VillageCultureAfterDialogue, Color.FromUint(WarningColor)));
				}
				continue;
			}
			if (!AppliedActions.Add(action))
			{
				continue;
			}
			actionHandled |= ApplyAction(action);
		}
		return true;
	}

	private static PostprocessRuleEntry Rule(string tag, string description)
	{
		return new PostprocessRuleEntry { Tag = tag, Description = description };
	}

	private static bool IsValidMissionSpeaker(int targetAgentIndex)
	{
		try
		{
			return targetAgentIndex >= 0
				&& _activeMission?.Agents?.Any(agent => agent != null && agent.Index == targetAgentIndex && agent.IsHuman && agent.IsActive()) == true;
		}
		catch
		{
			return false;
		}
	}

	private static bool ApplyAction(VillageAftermathActionKind action)
	{
		try
		{
			Settlement village = ResolveActiveVillage();
			if (village?.Village == null || Hero.MainHero == null)
			{
				return false;
			}
			VillageAftermathEffectProfile effect = VillageAftermathEffectProfile.Resolve(action);
			if (effect.GoldDelta < 0 && Hero.MainHero.Gold < -effect.GoldDelta)
			{
				AppliedActions.Remove(action);
				InformationManager.DisplayMessage(new InformationMessage(SiegeAftermathUiText.VillageInsufficientGold(SiegeAftermathUiText.VillageActionName(action)), Color.FromUint(WarningColor)));
				return true;
			}
			if (effect.GoldDelta < 0)
			{
				GiveGoldAction.ApplyBetweenCharacters(Hero.MainHero, null, -effect.GoldDelta, true);
			}
			else if (effect.GoldDelta > 0)
			{
				GiveGoldAction.ApplyBetweenCharacters(null, Hero.MainHero, effect.GoldDelta, true);
			}

			float oldHearth = village.Village.Hearth;
			village.Village.Hearth = effect.ApplyHearth(oldHearth);
			ApplyOwnerRelation(effect.OwnerRelationDelta);

			int gained = 0;
			if (action == VillageAftermathActionKind.RequisitionFood)
			{
				gained = AddVillageProductionToPlayer(village, item => item.IsFood, 12);
			}
			else if (action == VillageAftermathActionKind.RequisitionLivestock)
			{
				gained = AddVillageProductionToPlayer(village, item => item.IsAnimal, 6);
			}
			else if (action == VillageAftermathActionKind.RequisitionProduce)
			{
				gained = AddVillageProductionToPlayer(village, item => !item.IsAnimal, 8);
			}
			else if (action == VillageAftermathActionKind.LevyRecruits)
			{
				gained = LevyVillageMilitia(village, 10);
			}
			else if (action == VillageAftermathActionKind.Massacre)
			{
				KillVillageNotables(village, "village_massacre");
			}

			if (effect.IsDestructive && village.SettlementHitPoints > 0f)
			{
				IncreaseSettlementHealthAction.Apply(village, -village.SettlementHitPoints * (action == VillageAftermathActionKind.Massacre ? 0.8f : 0.5f));
			}

			string extra = gained > 0 ? SiegeAftermathCompatibilityTexts.Raw("auto_village_7b6d341a56a4") + gained + SiegeAftermathCompatibilityTexts.Raw("auto_village_d2f9922e1ecb") : string.Empty;
			uint color = effect.IsDestructive ? DestructiveColor : SuccessColor;
			InformationManager.DisplayMessage(new InformationMessage(
				SiegeAftermathUiText.VillageActionApplied(SiegeAftermathUiText.VillageActionName(action), oldHearth.ToString("0"), village.Village.Hearth.ToString("0"), extra),
				Color.FromUint(color)));
			GcczDiagnosticLog.Log("VillageAction", "applied village=" + (village.StringId ?? "N/A")
				+ " action=" + action
				+ " goldDelta=" + effect.GoldDelta
				+ " hearth=" + oldHearth.ToString("0.##") + "->" + village.Village.Hearth.ToString("0.##")
				+ " relation=" + effect.OwnerRelationDelta
				+ " gained=" + gained);
			return true;
		}
		catch (Exception ex)
		{
			AppliedActions.Remove(action);
			Logger.Log("VillageAftermath", "Apply action failed. action=" + action + " error=" + ex);
			return false;
		}
	}

	private static int AddVillageProductionToPlayer(Settlement village, Func<ItemObject, bool> predicate, int countPerType)
	{
		if (village?.Village?.VillageType?.Productions == null || MobileParty.MainParty?.ItemRoster == null)
		{
			return 0;
		}
		int total = 0;
		foreach (var production in village.Village.VillageType.Productions)
		{
			ItemObject item = production.Item1;
			if (item == null || predicate?.Invoke(item) == false)
			{
				continue;
			}
			int count = Math.Max(1, countPerType);
			MobileParty.MainParty.ItemRoster.AddToCounts(item, count);
			total += count;
		}
		return total;
	}

	private static int LevyVillageMilitia(Settlement village, int maximum)
	{
		TroopRoster militia = village?.MilitiaPartyComponent?.MobileParty?.MemberRoster;
		TroopRoster player = MobileParty.MainParty?.MemberRoster;
		if (militia == null || player == null || maximum <= 0)
		{
			return 0;
		}
		int moved = 0;
		foreach (TroopRosterElement element in militia.GetTroopRoster().ToList())
		{
			CharacterObject troop = element.Character;
			if (troop == null || troop.IsHero || element.Number <= 0)
			{
				continue;
			}
			int count = Math.Min(element.Number, maximum - moved);
			if (count <= 0)
			{
				break;
			}
			militia.AddToCounts(troop, -count);
			player.AddToCounts(troop, count);
			moved += count;
		}
		return moved;
	}

	private static void ApplyOwnerRelation(int delta)
	{
		if (delta == 0 || _activeAuthorityKind != VillageAftermathAuthorityKind.KingdomRuler)
		{
			return;
		}
		Hero owner = ResolveActiveVillage()?.Village?.Bound?.OwnerClan?.Leader;
		if (owner != null && owner != Hero.MainHero)
		{
			ChangeRelationAction.ApplyPlayerRelation(owner, delta, true, true);
		}
	}

	private static void OpenCultureModeInquiry()
	{
		if (!IsActive())
		{
			return;
		}
		List<InquiryElement> choices = new List<InquiryElement>
		{
			new InquiryElement(VillageCultureChangeMode.GradualEducation, SiegeAftermathUiText.VillageCultureModeGradual, null, true, SiegeAftermathUiText.VillageCultureModeGradualHint),
			new InquiryElement(VillageCultureChangeMode.MigrantResettlement, SiegeAftermathUiText.VillageCultureModeMigration, null, true, SiegeAftermathUiText.VillageCultureModeMigrationHint),
			new InquiryElement(VillageCultureChangeMode.PurgeColonization, SiegeAftermathUiText.VillageCultureModePurge, null, true, SiegeAftermathUiText.VillageCultureModePurgeHint),
		};
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			SiegeAftermathUiText.VillageCultureInquiryTitle,
			SiegeAftermathUiText.VillageCultureInquiryDescription,
			choices,
			isExitShown: true,
			1,
			1,
			SiegeAftermathUiText.NextButton,
			SiegeAftermathUiText.CancelButton,
			selected =>
			{
				if (selected?.FirstOrDefault()?.Identifier is VillageCultureChangeMode mode)
				{
					OpenCultureTargetInquiry(mode);
				}
			},
			_ => { },
			"",
			isSeachAvailable: false), pauseGameActiveState: true);
	}

	private static void OpenCultureTargetInquiry(VillageCultureChangeMode mode)
	{
		Settlement village = ResolveActiveVillage();
		Dictionary<string, CultureObject> cultures = new Dictionary<string, CultureObject>(StringComparer.OrdinalIgnoreCase);
		AddCulture(cultures, Hero.MainHero?.Culture);
		AddCulture(cultures, Clan.PlayerClan?.Culture);
		AddCulture(cultures, Clan.PlayerClan?.Kingdom?.Culture);
		AddCulture(cultures, village?.Village?.Bound?.Culture);
		List<InquiryElement> choices = cultures.Values
			.Select(culture => new InquiryElement(culture.StringId, culture.Name?.ToString() ?? culture.StringId, null, true, culture.StringId))
			.ToList();
		if (choices.Count == 0)
		{
			InformationManager.DisplayMessage(new InformationMessage(SiegeAftermathUiText.VillageNoTargetCulture, Color.FromUint(WarningColor)));
			return;
		}
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			SiegeAftermathUiText.VillageCultureTargetTitle,
			SiegeAftermathUiText.VillageCultureTargetDescription(SiegeAftermathUiText.VillageCultureModeName(mode)),
			choices,
			isExitShown: true,
			1,
			1,
			SiegeAftermathUiText.ConfirmButton,
			SiegeAftermathUiText.BackButton,
			selected =>
			{
				string cultureId = selected?.FirstOrDefault()?.Identifier as string;
				CultureObject culture = cultures.TryGetValue(cultureId ?? string.Empty, out CultureObject selectedCulture) ? selectedCulture : null;
				if (culture != null)
				{
					ApplyCultureChange(mode, culture);
				}
			},
			_ => OpenCultureModeInquiry(),
			"",
			isSeachAvailable: false), pauseGameActiveState: true);
	}

	private static void AddCulture(Dictionary<string, CultureObject> cultures, CultureObject culture)
	{
		if (culture != null && !string.IsNullOrWhiteSpace(culture.StringId))
		{
			cultures[culture.StringId] = culture;
		}
	}

	private static void ApplyCultureChange(VillageCultureChangeMode mode, CultureObject targetCulture)
	{
		bool mutationStarted = false;
		try
		{
			Settlement village = ResolveActiveVillage();
			VillageAftermathBehavior behavior = Campaign.Current?.GetCampaignBehavior<VillageAftermathBehavior>();
			if (village?.Village == null || targetCulture == null || behavior == null)
			{
				return;
			}
			if (mode == VillageCultureChangeMode.GradualEducation)
			{
				int finishDay = CurrentDay + VillageCultureChangeProfile.GradualEducationDays;
				mutationStarted = true;
				behavior._gradualCultureTargetByVillageId[village.StringId] = targetCulture.StringId;
				behavior._gradualCultureFinishDayByVillageId[village.StringId] = finishDay;
				AppliedActions.Add(VillageAftermathActionKind.CulturalReform);
				InformationManager.DisplayMessage(new InformationMessage(
					SiegeAftermathUiText.VillageCultureGradualStarted(targetCulture.Name?.ToString() ?? targetCulture.StringId),
					Color.FromUint(SuccessColor)));
				return;
			}

			float oldHearth = village.Village.Hearth;
			if (!TryPersistVillageCulture(village, targetCulture, "village_" + mode))
			{
				throw new InvalidOperationException("Village culture persistence rejected the mutation before side effects started.");
			}
			mutationStarted = true;
			village.Village.Hearth = VillageCultureChangeProfile.ApplyImmediateHearth(mode, oldHearth);
			if (mode == VillageCultureChangeMode.PurgeColonization)
			{
				KillVillageNotables(village, "village_purge_colonization");
			}
			ApplyVillageNotableCulture(
				village,
				targetCulture,
				clearVolunteerTypes: true,
				spawnReplacementNotables: mode == VillageCultureChangeMode.PurgeColonization);
			LogVillageCultureApplied(village, targetCulture, mode == VillageCultureChangeMode.PurgeColonization);
			ApplyOwnerRelation(mode == VillageCultureChangeMode.PurgeColonization
				? VillageCultureChangeProfile.PurgeColonizationOwnerRelationDelta
				: VillageCultureChangeProfile.MigrantResettlementOwnerRelationDelta);
			AppliedActions.Add(VillageAftermathActionKind.CulturalReform);
			InformationManager.DisplayMessage(new InformationMessage(
				SiegeAftermathUiText.VillageCultureChanged(
					SiegeAftermathUiText.VillageCultureModeName(mode),
					 targetCulture.Name?.ToString() ?? targetCulture.StringId,
					oldHearth.ToString("0"),
					village.Village.Hearth.ToString("0")),
				Color.FromUint(mode == VillageCultureChangeMode.PurgeColonization ? DestructiveColor : WarningColor)));
		}
		catch (Exception ex)
		{
			if (mutationStarted)
			{
				AppliedActions.Add(VillageAftermathActionKind.CulturalReform);
			}
			else
			{
				AppliedActions.Remove(VillageAftermathActionKind.CulturalReform);
			}
			Logger.Log("VillageAftermath", "Apply culture change failed. mode=" + mode + " target=" + (targetCulture?.StringId ?? "N/A") + " error=" + ex);
			TryDisplayFailureMessage(SiegeAftermathUiText.VillageCultureFailure(mutationStarted));
		}
	}

	private static void TryDisplayFailureMessage(string message)
	{
		try
		{
			InformationManager.DisplayMessage(new InformationMessage(message ?? string.Empty, Color.FromUint(WarningColor)));
		}
		catch
		{
		}
	}

	private void OnDailyTick()
	{
		try
		{
			foreach (string villageId in _gradualCultureFinishDayByVillageId
				.Where(pair => pair.Value <= CurrentDay)
				.Select(pair => pair.Key)
				.ToList())
			{
				Settlement village = Settlement.Find(villageId);
				string cultureId = _gradualCultureTargetByVillageId.TryGetValue(villageId, out string id) ? id : string.Empty;
				CultureObject culture = Game.Current?.ObjectManager?.GetObject<CultureObject>(cultureId);
				if (village?.IsVillage == true && culture != null)
				{
					bool applied = ApplyVillageCultureNow(
						village,
						culture,
						clearVolunteerTypes: true,
						spawnReplacementNotables: false,
						source: "village_gradual_education_completed");
					if (!applied)
					{
						Logger.Log("VillageAftermath", "Deferred gradual culture completion because persistence was unavailable. village="
							+ (village.StringId ?? villageId) + " target=" + (culture.StringId ?? "N/A"));
						continue;
					}
					InformationManager.DisplayMessage(new InformationMessage(
						SiegeAftermathUiText.VillageCultureGradualCompleted(
							village.Name?.ToString() ?? villageId,
							culture.Name?.ToString() ?? culture.StringId),
						Color.FromUint(SuccessColor)));
				}
				_gradualCultureFinishDayByVillageId.Remove(villageId);
				_gradualCultureTargetByVillageId.Remove(villageId);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("VillageAftermath", "Daily culture update failed: " + ex);
		}
	}

	private static bool ApplyVillageCultureNow(
		Settlement village,
		CultureObject targetCulture,
		bool clearVolunteerTypes,
		bool spawnReplacementNotables,
		string source)
	{
		if (!TryPersistVillageCulture(village, targetCulture, source))
		{
			return false;
		}

		ApplyVillageNotableCulture(village, targetCulture, clearVolunteerTypes, spawnReplacementNotables);
		LogVillageCultureApplied(village, targetCulture, spawnReplacementNotables);
		return true;
	}

	private static bool TryPersistVillageCulture(Settlement village, CultureObject targetCulture, string source)
	{
		return GcczSettlementCulturePersistenceBehavior.ApplyAndRemember(village, targetCulture, source);
	}

	private static void LogVillageCultureApplied(Settlement village, CultureObject targetCulture, bool spawnedReplacements)
	{
		GcczDiagnosticLog.Log("VillageCulture", "applied village=" + (village?.StringId ?? "N/A")
			+ " culture=" + (targetCulture?.StringId ?? "N/A") + " replacement=" + spawnedReplacements);
	}

	private static void ApplyVillageNotableCulture(
		Settlement village,
		CultureObject targetCulture,
		bool clearVolunteerTypes,
		bool spawnReplacementNotables)
	{
		foreach (Hero notable in village.Notables?.Where(hero => hero != null && hero.IsAlive && hero.IsRuralNotable).ToList() ?? new List<Hero>())
		{
			notable.Culture = targetCulture;
			if (clearVolunteerTypes && notable.VolunteerTypes != null)
			{
				for (int i = 0; i < notable.VolunteerTypes.Length; i++)
				{
					notable.VolunteerTypes[i] = null;
				}
			}
		}
		if (spawnReplacementNotables)
		{
			SpawnReplacementNotables(village, targetCulture, Occupation.Headman);
			SpawnReplacementNotables(village, targetCulture, Occupation.RuralNotable);
		}
	}

	private static void KillVillageNotables(Settlement village, string source)
	{
		foreach (Hero notable in village?.Notables?.Where(hero => hero != null && hero.IsAlive && hero.IsRuralNotable).ToList() ?? new List<Hero>())
		{
			try
			{
				KillCharacterAction.ApplyByMurder(notable, Hero.MainHero, false);
				if (notable.IsAlive)
				{
					KillCharacterAction.ApplyByRemove(notable, false, true);
				}
			}
			catch (Exception ex)
			{
				Logger.Log("VillageAftermath", "Kill rural notable failed. source=" + source + " hero=" + (notable.StringId ?? "N/A") + " error=" + ex.Message);
			}
		}
	}

	private static void SpawnReplacementNotables(Settlement village, CultureObject culture, Occupation occupation)
	{
		int targetCount = Campaign.Current?.Models?.NotableSpawnModel?.GetTargetNotableCountForSettlement(village, occupation) ?? 0;
		int currentCount = village?.Notables?.Count(hero => hero != null && hero.IsAlive && hero.Occupation == occupation) ?? 0;
		for (int i = currentCount; i < targetCount; i++)
		{
			Hero notable = HeroCreator.CreateNotable(occupation, village);
			if (notable == null)
			{
				continue;
			}
			notable.Culture = culture;
			if (notable.CurrentSettlement != village && notable.StayingInSettlement != village)
			{
				EnterSettlementAction.ApplyForCharacterOnly(notable, village);
			}
		}
	}

	private static Settlement ResolveActiveVillage()
	{
		return string.IsNullOrWhiteSpace(_activeVillageId) ? null : Settlement.Find(_activeVillageId);
	}

	private static int CurrentDay => Math.Max(0, (int)Math.Floor(CampaignTime.Now.ToDays));

	private static void ClearMissionState(string source)
	{
		if (_activeMission != null || !string.IsNullOrWhiteSpace(_activeVillageId))
		{
			GcczDiagnosticLog.Log("VillageEntry", "cleared village=" + (_activeVillageId ?? "N/A") + " source=" + (source ?? "N/A"));
		}
		_activeMission = null;
		_activeVillageId = "";
		_activeAuthorityKind = VillageAftermathAuthorityKind.None;
		_cultureInquiryPending = false;
		AppliedActions.Clear();
	}
}

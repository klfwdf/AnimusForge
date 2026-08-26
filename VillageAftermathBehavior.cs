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
				"【攻城处置&内部暴乱·村庄】你可以在本村执行贵族处置；普通 AF 与原版对话功能保持可用。",
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
			TryDisplayFailureMessage("【GCCZ村庄】文化改造界面打开失败，本次未执行。");
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
			? VillageAftermathRuntimePromptProfile.BuildPrompt(village?.Name?.ToString(), village?.Village?.Bound?.Name?.ToString(), _activeAuthorityKind)
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
			Rule(VillageAftermathActionTagCatalog.GatherEldersTag, "玩家明确要求召集村民、长老或头人时输出；把成年村民编入3号民众编队。"),
			Rule(VillageAftermathActionTagCatalog.RestrainTroopsTag, "玩家明确要求约束随行军纪、停止扰民时输出。"),
			Rule(VillageAftermathActionTagCatalog.PacifyTag, "玩家明确决定平息村情、停止追究时输出。"),
			Rule(VillageAftermathActionTagCatalog.ReliefTag, "玩家明确决定支付第纳尔赈济村民时输出。"),
			Rule(VillageAftermathActionTagCatalog.FineTag, "玩家明确决定向村庄罚赎时输出。"),
			Rule(VillageAftermathActionTagCatalog.RequisitionFoodTag, "玩家明确决定征收粮食时输出。"),
			Rule(VillageAftermathActionTagCatalog.RequisitionProduceTag, "玩家明确决定征收村庄物产时输出。"),
			Rule(VillageAftermathActionTagCatalog.RequisitionLivestockTag, "玩家明确决定征收牲畜时输出。"),
			Rule(VillageAftermathActionTagCatalog.LevyRecruitsTag, "玩家明确决定从村庄民兵征丁时输出。"),
			Rule(VillageAftermathActionTagCatalog.PunishRingleaderTag, "玩家明确决定惩办村庄首恶时输出。"),
			Rule(VillageAftermathActionTagCatalog.ConfiscatePropertyTag, "玩家明确决定查抄村产时输出。"),
			Rule(VillageAftermathActionTagCatalog.DestroyLivelihoodTag, "玩家明确决定毁坏村庄生产与生计时输出。"),
			Rule(VillageAftermathActionTagCatalog.MassacreTag, "玩家明确决定屠村时输出。"),
			Rule(VillageAftermathActionTagCatalog.CulturalReformTag, "玩家明确要求改变村庄文化时输出；只打开玩家确认界面。"),
		};
	}

	internal static string BuildPostprocessContextForExternal(bool replyIsDirectPlayerResponse)
	{
		if (!IsActive())
		{
			return string.Empty;
		}
		Settlement village = ResolveActiveVillage();
		return "当前村庄=" + (village?.Name?.ToString() ?? "未知")
			+ "；上级封地=" + (village?.Village?.Bound?.Name?.ToString() ?? "未知")
			+ "；玩家权限=" + _activeAuthorityKind
			+ "；是否直接回复玩家=" + replyIsDirectPlayerResponse
			+ "。GCCZ村庄标签不得取代或阻断普通AF与原版标签。";
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
					InformationManager.DisplayMessage(new InformationMessage("【GCCZ村庄】对话结束后将打开文化改造确认界面。", Color.FromUint(WarningColor)));
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
				InformationManager.DisplayMessage(new InformationMessage("【GCCZ村庄】第纳尔不足，无法执行" + effect.DisplayName + "。", Color.FromUint(WarningColor)));
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

			string extra = gained > 0 ? "；获得/征集 " + gained + " 单位" : string.Empty;
			uint color = effect.IsDestructive ? DestructiveColor : SuccessColor;
			InformationManager.DisplayMessage(new InformationMessage(
				"【GCCZ村庄】已执行" + effect.DisplayName + "：炉户 " + oldHearth.ToString("0") + " → " + village.Village.Hearth.ToString("0") + extra + "。",
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
			new InquiryElement(VillageCultureChangeMode.GradualEducation, "教化改俗", null, true, "180 天后改为目标文化；不立即损失炉户。"),
			new InquiryElement(VillageCultureChangeMode.MigrantResettlement, "迁民改俗", null, true, "立即改为目标文化；炉户损失 25%，清空旧募兵槽。"),
			new InquiryElement(VillageCultureChangeMode.PurgeColonization, "屠村迁殖", null, true, "立即改为目标文化；炉户损失 60%，清除旧农村要人并重建募兵来源。"),
		};
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			"GCCZ村庄：文化改造",
			"先选择改造方式。该标签不会替你自动决定。",
			choices,
			isExitShown: true,
			1,
			1,
			"下一步",
			"取消",
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
			InformationManager.DisplayMessage(new InformationMessage("【GCCZ村庄】没有可用的目标文化。", Color.FromUint(WarningColor)));
			return;
		}
		MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(
			"GCCZ村庄：选择目标文化",
			"改造方式：" + VillageCultureChangeProfile.GetDisplayName(mode),
			choices,
			isExitShown: true,
			1,
			1,
			"确认",
			"返回",
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
					"【GCCZ村庄】已开始教化改俗：约 180 天后转为" + (targetCulture.Name?.ToString() ?? targetCulture.StringId) + "。",
					Color.FromUint(SuccessColor)));
				return;
			}

			float oldHearth = village.Village.Hearth;
			mutationStarted = true;
			village.Village.Hearth = VillageCultureChangeProfile.ApplyImmediateHearth(mode, oldHearth);
			if (mode == VillageCultureChangeMode.PurgeColonization)
			{
				KillVillageNotables(village, "village_purge_colonization");
			}
			ApplyVillageCultureNow(
				village,
				targetCulture,
				clearVolunteerTypes: true,
				spawnReplacementNotables: mode == VillageCultureChangeMode.PurgeColonization,
				source: "village_" + mode);
			ApplyOwnerRelation(mode == VillageCultureChangeMode.PurgeColonization
				? VillageCultureChangeProfile.PurgeColonizationOwnerRelationDelta
				: VillageCultureChangeProfile.MigrantResettlementOwnerRelationDelta);
			AppliedActions.Add(VillageAftermathActionKind.CulturalReform);
			InformationManager.DisplayMessage(new InformationMessage(
				"【GCCZ村庄】" + VillageCultureChangeProfile.GetDisplayName(mode) + "完成：文化改为"
				+ (targetCulture.Name?.ToString() ?? targetCulture.StringId)
				+ "，炉户 " + oldHearth.ToString("0") + " → " + village.Village.Hearth.ToString("0") + "。",
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
			TryDisplayFailureMessage(
				mutationStarted
					? "【GCCZ村庄】文化改造执行异常，已锁定本次处置以避免重复扣除；请导出 GCCZ 日志。"
					: "【GCCZ村庄】文化改造执行失败，本次未生效；请导出 GCCZ 日志。");
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
					ApplyVillageCultureNow(
						village,
						culture,
						clearVolunteerTypes: true,
						spawnReplacementNotables: false,
						source: "village_gradual_education_completed");
					InformationManager.DisplayMessage(new InformationMessage(
						"【GCCZ村庄】" + (village.Name?.ToString() ?? villageId) + "的教化改俗完成，文化已转为" + (culture.Name?.ToString() ?? culture.StringId) + "。",
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

	private static void ApplyVillageCultureNow(
		Settlement village,
		CultureObject targetCulture,
		bool clearVolunteerTypes,
		bool spawnReplacementNotables,
		string source)
	{
		GcczSettlementCulturePersistenceBehavior.ApplyAndRemember(village, targetCulture, source);
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
		GcczDiagnosticLog.Log("VillageCulture", "applied village=" + (village.StringId ?? "N/A") + " culture=" + (targetCulture.StringId ?? "N/A") + " replacement=" + spawnReplacementNotables);
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

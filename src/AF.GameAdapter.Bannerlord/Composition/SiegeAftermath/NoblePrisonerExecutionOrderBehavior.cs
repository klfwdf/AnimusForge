using System;
using System.Collections.Generic;
using System.Linq;
using AnimusForge.SiegeAftermathIntervention;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Roster;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace AnimusForge;

/// <summary>
/// Campaign-side adapter for consented noble execution orders. Pure tag,
/// attribution, delay and save-record rules live in the mirrored GCCZ policy.
/// </summary>
public sealed class NoblePrisonerExecutionOrderBehavior : CampaignBehaviorBase
{
	private const string StorageKey = "_afNoblePrisonerExecutionOrders_v1";
	private const uint WarningColor = 0xFFFF6B6Bu;
	private const uint SuccessColor = 0xFF8DDC7Eu;

	private Dictionary<string, string> _serializedTasks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
	private readonly Dictionary<string, NobleExecutionTaskRecord> _tasks = new Dictionary<string, NobleExecutionTaskRecord>(StringComparer.OrdinalIgnoreCase);

	internal static NoblePrisonerExecutionOrderBehavior Instance { get; private set; }

	public override void RegisterEvents()
	{
		Instance = this;
		CampaignEvents.HourlyTickEvent.AddNonSerializedListener(this, OnHourlyTick);
		CampaignEvents.OnNewGameCreatedEvent.AddNonSerializedListener(this, _ => ResetRuntime());
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (dataStore.IsSaving)
		{
			_serializedTasks = _tasks.Values
				.Select(record => new KeyValuePair<string, string>(record.OperationId, NobleExecutionTaskCodec.Serialize(record)))
				.Where(pair => !string.IsNullOrWhiteSpace(pair.Key) && !string.IsNullOrWhiteSpace(pair.Value))
				.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
		}
		dataStore.SyncData(StorageKey, ref _serializedTasks);
		if (dataStore.IsLoading)
		{
			_tasks.Clear();
			foreach (string encoded in (_serializedTasks ?? new Dictionary<string, string>()).Values)
			{
				if (NobleExecutionTaskCodec.TryDeserialize(encoded, out NobleExecutionTaskRecord record))
				{
					_tasks[record.OperationId] = record;
				}
			}
			_serializedTasks = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		}
	}

	internal static List<PostprocessRuleEntry> BuildRuntimePostprocessRules(
		Hero actor,
		int actorAgentIndex)
	{
		var rules = new List<PostprocessRuleEntry>();
		if (!TryEvaluateActor(actor, out NobleExecutionActorDecision decision))
		{
			return rules;
		}
		if (Mission.Current != null && ResolveHeroAgent(actor, actorAgentIndex) == null)
		{
			return rules;
		}

		IEnumerable<Hero> targets = Mission.Current != null
			? NoblePrisonerEscortBehavior.GetEscortedHeroesForExecution()
			: EnumerateMainPartyHeroPrisoners();
		foreach (Hero prisoner in targets
			.Where(hero => IsLiveMainPartyPrisoner(hero) && hero != actor)
			.GroupBy(hero => hero.StringId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
			.Select(group => group.First()))
		{
			if (Mission.Current != null)
			{
				AddEscortRules(rules, actor, prisoner, decision);
			}
			else
			{
				string tag = NoblePrisonerExecutionActionTagCatalog.BuildPartyPrisonerTag(prisoner.StringId);
				if (!string.IsNullOrWhiteSpace(tag))
				{
					rules.Add(new PostprocessRuleEntry
					{
						Tag = tag,
						Description = BuildConsentRule(actor, prisoner,
							"同意在离开对话后的 6—18 个游戏小时内，于大地图上处决该玩家主队俘虏。")
					});
				}
			}
		}
		return rules;
	}

	internal static string NormalizePostprocessTags(
		string content,
		IEnumerable<PostprocessRuleEntry> allowedRules)
	{
		string source = content ?? string.Empty;
		List<string> matches = (allowedRules ?? Enumerable.Empty<PostprocessRuleEntry>())
			.Select(rule => (rule?.Tag ?? string.Empty).Trim())
			.Where(tag => tag.Length > 0 && source.IndexOf(tag, StringComparison.OrdinalIgnoreCase) >= 0)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Take(2)
			.ToList();
		return matches.Count == 1 ? matches[0] : string.Empty;
	}

	internal static bool TryProcessAcceptedTag(
		Hero actor,
		int actorAgentIndex,
		bool replyIsDirectPlayerResponse,
		ref string content,
		out string reason)
	{
		reason = string.Empty;
		string source = content ?? string.Empty;
		bool hasEscortTag = NoblePrisonerExecutionActionTagCatalog.TryExtractEscort(
			source,
			out string escortedPrisonerId,
			out NobleExecutionHeadDisposition disposition);
		bool hasPartyTag = NoblePrisonerExecutionActionTagCatalog.TryExtractPartyPrisoner(
			source,
			out string partyPrisonerId);
		if (!hasEscortTag && !hasPartyTag)
		{
			return false;
		}
		content = NoblePrisonerExecutionActionTagCatalog.StripExecutionTags(source);
		if (!replyIsDirectPlayerResponse)
		{
			reason = "execution_order_not_direct_reply";
			return false;
		}
		if (!TryEvaluateActor(actor, out NobleExecutionActorDecision actorDecision))
		{
			reason = "execution_actor_not_eligible";
			return false;
		}
		if (hasEscortTag)
		{
			if (Mission.Current == null)
			{
				reason = "escort_execution_requires_mission";
				return false;
			}
			if (!NoblePrisonerExecutionPolicy.IsHeadDispositionAllowed(actorDecision, disposition))
			{
				reason = "head_disposition_not_allowed";
				return false;
			}
			Hero prisoner = ResolveHero(escortedPrisonerId);
			Agent actorAgent = ResolveHeroAgent(actor, actorAgentIndex);
			if (prisoner == null
				|| actorAgent == null
				|| !NoblePrisonerEscortBehavior.TryGetEscortedAgentForHero(prisoner, out Agent prisonerAgent)
				|| !NoblePrisonerExecutionRuntime.TryQueueActorExecution(
					actor,
					actorAgent,
					prisoner,
					prisonerAgent,
					disposition,
					actorDecision.RelationAttribution,
					out reason))
			{
				Show("【贵族俘虏随行】处决行动未能开始。", WarningColor);
				return false;
			}
			return true;
		}

		if (Mission.Current != null)
		{
			reason = "party_execution_requires_campaign_map";
			return false;
		}
		Hero partyPrisoner = ResolveHero(partyPrisonerId);
		if (!IsLiveMainPartyPrisoner(partyPrisoner))
		{
			reason = "party_prisoner_unavailable";
			return false;
		}
		return Instance != null
			&& Instance.TryQueueMapExecution(actor, partyPrisoner, out reason);
	}

	internal static bool TryEvaluateActor(Hero actor, out NobleExecutionActorDecision decision)
	{
		bool isPlayer = actor != null && actor == Hero.MainHero;
		bool playerClan = actor != null
			&& !isPlayer
			&& (actor.Clan == Clan.PlayerClan
				|| actor.CompanionOf == Clan.PlayerClan
				|| actor.IsPlayerCompanion);
		bool friendlyNoble = actor != null
			&& !isPlayer
			&& !playerClan
			&& actor.IsLord
			&& actor.MapFaction != null
			&& Hero.MainHero?.MapFaction != null
			&& (ReferenceEquals(actor.MapFaction, Hero.MainHero.MapFaction)
				|| string.Equals(actor.MapFaction.StringId, Hero.MainHero.MapFaction.StringId, StringComparison.OrdinalIgnoreCase));
		decision = NoblePrisonerExecutionPolicy.EvaluateActor(new NobleExecutionActorFacts(
			actor?.IsAlive == true,
			actor?.IsPrisoner == true,
			isPlayer,
			playerClan,
			friendlyNoble));
		return decision.IsEligible;
	}

	private static void AddEscortRules(
		List<PostprocessRuleEntry> rules,
		Hero actor,
		Hero prisoner,
		NobleExecutionActorDecision decision)
	{
		string giveTag = NoblePrisonerExecutionActionTagCatalog.BuildEscortTag(
			prisoner.StringId,
			NobleExecutionHeadDisposition.GiveToPlayer);
		if (!string.IsNullOrWhiteSpace(giveTag))
		{
			rules.Add(new PostprocessRuleEntry
			{
				Tag = giveTag,
				Description = BuildConsentRule(actor, prisoner,
					"同意退出当前对话后亲自走向该随行俘虏、拔出武器处决，并把头颅交给玩家。")
			});
		}
		if (!decision.MustGiveHeadToPlayer)
		{
			string keepTag = NoblePrisonerExecutionActionTagCatalog.BuildEscortTag(
				prisoner.StringId,
				NobleExecutionHeadDisposition.KeepByExecutioner);
			if (!string.IsNullOrWhiteSpace(keepTag))
			{
				rules.Add(new PostprocessRuleEntry
				{
					Tag = keepTag,
					Description = BuildConsentRule(actor, prisoner,
						"同意退出当前对话后亲自处决该随行俘虏，但基于自身性格决定不把头颅交给玩家。")
				});
			}
		}
	}

	private static string BuildConsentRule(Hero actor, Hero prisoner, string acceptedEffect)
	{
		return "仅在玩家本轮明确请求当前回应者“"
			+ (actor?.Name?.ToString() ?? "NPC")
			+ "”处决“"
			+ (prisoner?.Name?.ToString() ?? "俘虏")
			+ "”，且当前回应者按人格与关系自主明确同意时输出；拒绝、犹豫、提问、转述、假设、历史内容或处决其他人时不得输出。效果："
			+ acceptedEffect;
	}

	private bool TryQueueMapExecution(Hero actor, Hero prisoner, out string reason)
	{
		reason = string.Empty;
		if (actor == null || prisoner == null || !IsLiveMainPartyPrisoner(prisoner))
		{
			reason = "map_execution_invalid_participants";
			return false;
		}
		if (_tasks.Values.Any(task => string.Equals(task.PrisonerHeroId, prisoner.StringId, StringComparison.OrdinalIgnoreCase)))
		{
			reason = "map_execution_already_pending";
			return false;
		}
		long nowHour = CurrentCampaignHour();
		long dueHour = nowHour + NoblePrisonerExecutionPolicy.ComputeMapDelayHours(actor.StringId, prisoner.StringId);
		string operationId = "exec_" + actor.StringId + "_" + prisoner.StringId + "_" + dueHour;
		var record = new NobleExecutionTaskRecord(operationId, actor.StringId, prisoner.StringId, dueHour);
		_tasks[operationId] = record;
		Show("【处决委托】" + actor.Name + "已同意处决" + prisoner.Name + "，将在稍后执行。", SuccessColor);
		MyBehavior.AppendExternalDialogueHistory(actor, null, null,
			"[AFEF NPC行为补充] 你已明确同意玩家的请求，将在稍后处决玩家主队中的俘虏“" + prisoner.Name + "”。");
		return true;
	}

	private void OnHourlyTick()
	{
		if (_tasks.Count == 0
			|| Mission.Current != null
			|| Campaign.Current?.ConversationManager?.IsConversationInProgress == true
			|| MobileParty.MainParty?.MapEvent != null)
		{
			return;
		}
		long nowHour = CurrentCampaignHour();
		foreach (NobleExecutionTaskRecord record in _tasks.Values.ToList())
		{
			if (record.DueHour > nowHour)
			{
				continue;
			}
			Hero actor = ResolveHero(record.ActorHeroId);
			Hero prisoner = ResolveHero(record.PrisonerHeroId);
			if (!TryEvaluateActor(actor, out NobleExecutionActorDecision decision)
				|| !IsLiveMainPartyPrisoner(prisoner))
			{
				_tasks.Remove(record.OperationId);
				continue;
			}
			Hero responsible = decision.RelationAttribution == NobleExecutionRelationAttribution.Player
				? Hero.MainHero
				: actor;
			try
			{
				KillCharacterAction.ApplyByExecution(prisoner, responsible, showNotification: true, isForced: true);
				if (IsExecutionAccepted(prisoner))
				{
					_tasks.Remove(record.OperationId);
					Show(actor.Name + "处决" + prisoner.Name, SuccessColor);
					MyBehavior.AppendExternalDialogueHistory(actor, null, null,
						"[AFEF NPC行为补充] 你依约处决了“" + prisoner.Name + "”。此事已发生，不能否认。 ");
				}
			}
			catch (Exception ex)
			{
				Logger.Log("NoblePrisonerExecution", "map execution failed operation=" + record.OperationId + " error=" + ex.Message);
				if (IsExecutionAccepted(prisoner))
				{
					_tasks.Remove(record.OperationId);
				}
			}
		}
	}

	private static bool IsExecutionAccepted(Hero hero)
	{
		return hero != null
			&& (!hero.IsAlive
				|| hero.DeathMark == KillCharacterAction.KillCharacterActionDetail.Executed
				|| hero.DeathMark == KillCharacterAction.KillCharacterActionDetail.ExecutionAfterMapEvent);
	}

	private static Agent ResolveHeroAgent(Hero hero, int agentIndex)
	{
		return Mission.Current?.Agents?.FirstOrDefault(agent =>
			agent != null
			&& agent.IsActive()
			&& ((agentIndex >= 0 && agent.Index == agentIndex)
				|| (agent.Character as CharacterObject)?.HeroObject == hero));
	}

	private static IEnumerable<Hero> EnumerateMainPartyHeroPrisoners()
	{
		TroopRoster roster = PartyBase.MainParty?.PrisonRoster;
		if (roster == null)
		{
			yield break;
		}
		for (int i = 0; i < roster.Count; i++)
		{
			TroopRosterElement element = roster.GetElementCopyAtIndex(i);
			Hero hero = element.Character?.HeroObject;
			if (element.Number > 0 && IsLiveMainPartyPrisoner(hero))
			{
				yield return hero;
			}
		}
	}

	private static bool IsLiveMainPartyPrisoner(Hero hero)
	{
		return hero != null
			&& hero != Hero.MainHero
			&& hero.IsAlive
			&& hero.IsPrisoner
			&& hero.PartyBelongedToAsPrisoner == PartyBase.MainParty;
	}

	private static Hero ResolveHero(string heroId)
	{
		string id = (heroId ?? string.Empty).Trim();
		return id.Length == 0
			? null
			: Hero.Find(id) ?? Hero.FindFirst(hero => hero != null && string.Equals(hero.StringId, id, StringComparison.OrdinalIgnoreCase));
	}

	private static long CurrentCampaignHour()
	{
		try
		{
			return Math.Max(0L, (long)Math.Floor(CampaignTime.Now.ToHours));
		}
		catch
		{
			return 0L;
		}
	}

	private static void ResetRuntime()
	{
		Instance?._tasks.Clear();
	}

	private static void Show(string text, uint color)
	{
		InformationManager.DisplayMessage(new InformationMessage(text ?? string.Empty, Color.FromUint(color)));
	}
}

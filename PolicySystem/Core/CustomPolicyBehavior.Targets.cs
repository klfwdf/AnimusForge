using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using AnimusForge.PolicyEffects;
using AnimusForge.PolicyTargets;
using HarmonyLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions.ItemTypes;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Policies;
using TaleWorlds.Core;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade;
using TaleWorlds.ObjectSystem;
using TaleWorlds.ScreenSystem;

namespace AnimusForge;

public sealed partial class CustomPolicyBehavior
{
	private static string BuildPolicyExplicitEntityHintText(PolicyDraftRequest request)
	{
		MentionedWorldEntities entities = request?.KnowledgeMentionedEntities;
		if (entities == null || entities.IsEmpty)
		{
			return "";
		}
		List<string> values = (entities.Entities ?? new List<string>())
			.Where(x => !string.IsNullOrWhiteSpace(x))
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Take(16)
			.ToList();
		return values.Count == 0 ? "" : LimitDisplayChars(CompactPolicyContextText("相关实体：" + string.Join("、", values)), 500);
	}

	private static MentionedWorldEntities BuildPolicyKnowledgeMentionedEntitiesSnapshot(string policyName, string policyContent, Kingdom playerKingdom)
	{
		MentionedWorldEntities entities = new MentionedWorldEntities();
		string haystack = ((policyName ?? "") + "\n" + (policyContent ?? "")).Trim();
		AddPolicyKnowledgeEntity(entities.Entities, GetKingdomName(playerKingdom), playerKingdom?.StringId);
		AddPolicyKnowledgeEntity(entities.Entities, playerKingdom?.Culture?.Name?.ToString(), null);
		if (string.IsNullOrWhiteSpace(haystack))
		{
			return entities;
		}
		try
		{
			foreach (Kingdom kingdom in Kingdom.All ?? Enumerable.Empty<Kingdom>())
			{
				if (kingdom != null && PolicyTextMentionsKingdom(haystack, kingdom))
				{
					AddPolicyKnowledgeEntity(entities.Entities, GetKingdomName(kingdom), kingdom.StringId);
				}
			}
		}
		catch
		{
		}
		try
		{
			foreach (Settlement settlement in Settlement.All ?? Enumerable.Empty<Settlement>())
			{
				if (settlement != null && PolicyTextMentions(haystack, settlement.StringId ?? "", settlement.Name?.ToString() ?? ""))
				{
					Settlement primaryFief = ResolvePrimaryPolicyFief(settlement);
					if (primaryFief != null)
					{
						AddPolicyKnowledgeEntity(entities.Entities, primaryFief.Name?.ToString(), primaryFief.StringId);
					}
				}
			}
		}
		catch
		{
		}
		try
		{
			foreach (Hero hero in Hero.AllAliveHeroes ?? Enumerable.Empty<Hero>())
			{
				if (hero != null && PolicyTextMentions(haystack, hero.StringId ?? "", hero.Name?.ToString() ?? ""))
				{
					AddPolicyKnowledgeEntity(entities.Entities, hero.Name?.ToString(), hero.StringId);
				}
			}
		}
		catch
		{
		}
		try
		{
			foreach (Clan clan in Clan.All ?? Enumerable.Empty<Clan>())
			{
				if (clan != null && PolicyTextMentions(haystack, clan.StringId ?? "", clan.Name?.ToString() ?? ""))
				{
					AddPolicyKnowledgeEntity(entities.Entities, clan.Name?.ToString(), clan.StringId);
				}
			}
		}
		catch
		{
		}
		return entities;
	}

	private static void AddPolicyKnowledgeEntity(List<string> target, string displayName, string fallbackId)
	{
		string value = string.IsNullOrWhiteSpace(displayName) ? (fallbackId ?? "").Trim() : displayName.Trim();
		if (!string.IsNullOrWhiteSpace(value) && target != null && target.Count < 8 && !target.Contains(value, StringComparer.OrdinalIgnoreCase))
		{
			target.Add(value);
		}
	}

	private static int CountPolicyKnowledgeMentions(MentionedWorldEntities entities)
	{
		if (entities == null)
		{
			return 0;
		}
		return entities.Entities?.Count ?? 0;
	}

	private static string CompressPolicyKnowledgeContext(string raw)
	{
		string text = (raw ?? "").Replace("\r\n", "\n").Replace('\r', '\n').Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		const string knowledgeHeader = "参与互动让你的脑海里浮现了这些知识";
		int knowledgeStart = text.IndexOf(knowledgeHeader, StringComparison.Ordinal);
		if (knowledgeStart >= 0)
		{
			text = text.Substring(knowledgeStart + knowledgeHeader.Length).Trim();
		}
		else if (text.IndexOf("【玩家外貌信息（常驻）】", StringComparison.Ordinal) >= 0)
		{
			return "";
		}
		List<string> candidates = new List<string>();
		HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string rawLine in text.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
		{
			string line = CompactPolicyContextText(rawLine);
			if (string.IsNullOrWhiteSpace(line)
				|| line.StartsWith("【以下是关于（", StringComparison.Ordinal)
				|| line.StartsWith("【玩家外貌信息", StringComparison.Ordinal)
				|| line.IndexOf("与玩家面对面互动时", StringComparison.Ordinal) >= 0)
			{
				continue;
			}
			foreach (string sentence in Regex.Split(line, @"(?<=[。！？!?；;])"))
			{
				string candidate = CompactPolicyContextText(sentence);
				if (!string.IsNullOrWhiteSpace(candidate) && candidate.Length <= PolicyKnowledgeMaxChars && seen.Add(candidate))
				{
					candidates.Add(candidate);
				}
			}
		}
		StringBuilder result = new StringBuilder();
		foreach (string candidate in candidates)
		{
			int nextLength = result.Length + (result.Length > 0 ? 1 : 0) + candidate.Length;
			if (nextLength <= PolicyKnowledgeTargetChars || (result.Length < PolicyKnowledgeMinChars && nextLength <= PolicyKnowledgeMaxChars))
			{
				if (result.Length > 0)
				{
					result.Append(' ');
				}
				result.Append(candidate);
			}
		}
		return result.ToString().Trim();
	}

	private static bool PolicyTextMentions(string haystack, params string[] candidates)
	{
		if (string.IsNullOrWhiteSpace(haystack))
		{
			return false;
		}
		foreach (string candidate in candidates ?? Array.Empty<string>())
		{
			string text = (candidate ?? "").Trim();
			if (text.Length >= 2 && haystack.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
			{
				return true;
			}
		}
		return false;
	}

	private static bool PolicyTextMentionsKingdom(string haystack, Kingdom kingdom)
	{
		if (kingdom == null || string.IsNullOrWhiteSpace(haystack))
		{
			return false;
		}
		List<string> candidates = BuildPolicyKingdomMentionCandidates(kingdom);
		if (PolicyTextMentions(haystack, candidates.ToArray()))
		{
			return true;
		}
		try
		{
			foreach (Clan clan in (((IEnumerable<Clan>)kingdom.Clans) ?? Enumerable.Empty<Clan>()))
			{
				if (clan == null)
				{
					continue;
				}
				if (PolicyTextMentions(haystack,
					clan.StringId ?? "",
					clan.Name?.ToString() ?? "",
					clan.Leader?.StringId ?? "",
					clan.Leader?.Name?.ToString() ?? ""))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		try
		{
			foreach (Settlement settlement in Settlement.All ?? Enumerable.Empty<Settlement>())
			{
				if (settlement == null || (settlement.MapFaction != kingdom && settlement.OwnerClan?.Kingdom != kingdom))
				{
					continue;
				}
				if (PolicyTextMentions(haystack, settlement.StringId ?? "", settlement.Name?.ToString() ?? ""))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static List<string> BuildPolicyKingdomMentionCandidates(Kingdom kingdom)
	{
		List<string> candidates = new List<string>();
		if (kingdom == null)
		{
			return candidates;
		}
		AddPolicyMentionCandidate(candidates, kingdom.StringId);
		AddPolicyMentionCandidate(candidates, GetKingdomName(kingdom));
		AddPolicyMentionCandidate(candidates, kingdom.Name?.ToString());
		AddPolicyMentionCandidate(candidates, kingdom.Leader?.StringId);
		AddPolicyMentionCandidate(candidates, kingdom.Leader?.Name?.ToString());
		AddPolicyMentionCandidate(candidates, kingdom.RulingClan?.StringId);
		AddPolicyMentionCandidate(candidates, kingdom.RulingClan?.Name?.ToString());
		AddPolicyMentionCandidate(candidates, kingdom.RulingClan?.Leader?.StringId);
		AddPolicyMentionCandidate(candidates, kingdom.RulingClan?.Leader?.Name?.ToString());
		foreach (string alias in GetPolicyKingdomAliases(kingdom))
		{
			AddPolicyMentionCandidate(candidates, alias);
		}
		return candidates.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static void AddPolicyMentionCandidate(List<string> candidates, string value)
	{
		string text = (value ?? "").Trim();
		if (text.Length >= 2)
		{
			candidates.Add(text);
		}
	}

	private static IEnumerable<string> GetPolicyKingdomAliases(Kingdom kingdom)
	{
		string id = (kingdom?.StringId ?? "").Trim().ToLowerInvariant();
		switch (id)
		{
			case "battania":
				return new[] { "巴旦尼亚", "巴坦尼亚", "巴塔尼亚", "Battanian", "Battanians" };
			case "vlandia":
				return new[] { "瓦兰迪亚", "瓦兰地亚", "Vlandian", "Vlandians" };
			case "sturgia":
				return new[] { "斯特吉亚", "斯特基亚", "Sturgian", "Sturgians" };
			case "khuzait":
				return new[] { "库赛特", "库塞特", "Khuzait", "Khuzaits" };
			case "aserai":
				return new[] { "阿塞莱", "阿塞来", "Aserai" };
			case "empire":
				return new[] { "北帝国", "北部帝国", "Northern Empire" };
			case "empire_s":
				return new[] { "南帝国", "南部帝国", "Southern Empire" };
			case "empire_w":
				return new[] { "西帝国", "西部帝国", "Western Empire" };
			case "nord":
				return new[] { "诺德", "诺德王国", "Nord", "Nords" };
			default:
				return Array.Empty<string>();
		}
	}

	private static HashSet<string> BuildPostprocessAllowedTargetKeys(PolicyTargetHandleDirectory directory)
	{
		if (directory?.StructureVersion != PolicyTargetHandleDirectoryContract.CurrentVersion
			|| directory.Targets == null)
		{
			return new HashSet<string>(StringComparer.Ordinal);
		}
		return new HashSet<string>(directory.Targets.Keys, StringComparer.Ordinal);
	}

	private static HashSet<string> BuildPostprocessAllowedModuleIds(PolicyTargetHandleDirectory directory)
	{
		if (directory?.StructureVersion != PolicyTargetHandleDirectoryContract.CurrentVersion
			|| directory.Capabilities == null)
		{
			return new HashSet<string>(StringComparer.Ordinal);
		}
		return new HashSet<string>(directory.Capabilities.Keys, StringComparer.Ordinal);
	}

	private static HashSet<string> BuildPostprocessAllowedTargetKeys(
		PolicyTargetHandleDirectory directory,
		string moduleId)
	{
		if (directory?.StructureVersion != PolicyTargetHandleDirectoryContract.CurrentVersion
			|| directory.Capabilities == null
			|| moduleId == null
			|| !directory.Capabilities.TryGetValue(moduleId, out PolicyEffectCapabilityDirectoryEntry capability)
			|| capability?.AllowedTargetHandles == null)
		{
			return new HashSet<string>(StringComparer.Ordinal);
		}
		return new HashSet<string>(capability.AllowedTargetHandles, StringComparer.Ordinal);
	}

	private static string BuildPolicyTargetHandlePromptText(PolicyDraftRequest request, ISet<string> allowedKeys = null)
	{
		List<PolicyTargetHandleSaveData> handles = NormalizePolicyTargetHandles(request?.TargetHandles)
			.Where(handle => allowedKeys == null || allowedKeys.Contains(handle.Key))
			.ToList();
		if (handles.Count <= 0)
		{
			return "（无合法目标句柄；effects 必须为空。）";
		}
		List<string> rendered = handles.Select(handle =>
		{
			string kind;
			if (string.Equals(handle.Kind, PolicyTargetKindSource, StringComparison.OrdinalIgnoreCase))
			{
				kind = "发布地";
			}
			else if (string.Equals(handle.Kind, PolicyTargetKindSettlement, StringComparison.OrdinalIgnoreCase))
			{
				kind = "本国城镇/城堡";
			}
			else if (string.Equals(handle.Kind, PolicyTargetKindClan, StringComparison.OrdinalIgnoreCase))
			{
				kind = "本国家族当前领地";
			}
			else if (string.Equals(handle.Kind, PolicyTargetKindRuler, StringComparison.OrdinalIgnoreCase))
			{
				kind = handle.FollowCurrentRulingClan ? "当前统治家族领地（动态）" : "本国领袖提交时所属家族领地";
			}
			else if (string.Equals(handle.Kind, PolicyTargetKindHero, StringComparison.OrdinalIgnoreCase))
			{
				kind = "人物或人物集合";
			}
			else if (string.Equals(handle.Kind, PolicyTargetKindSelector, StringComparison.OrdinalIgnoreCase))
			{
				kind = "集合目标 selector";
			}
			else if (string.Equals(handle.Kind, PolicyTargetKindPlan, StringComparison.OrdinalIgnoreCase))
			{
				kind = "TargetPlan 集合目标";
			}
			else
			{
				kind = "王国";
			}
			string count = handle.CurrentSettlementCount > 0
				? "，当前" + handle.CurrentSettlementCount.ToString(CultureInfo.InvariantCulture) + "处城镇/城堡"
				: "";
			string semanticEvidence = handle.IsSemanticTarget && !string.IsNullOrWhiteSpace(handle.SemanticEvidence)
				? "；语义依据=" + LimitDisplayChars(CompactPolicyContextText(handle.SemanticEvidence), 80)
				: "";
			string selectorDescriptor = string.Equals(handle.Kind, PolicyTargetKindSelector, StringComparison.OrdinalIgnoreCase)
				&& PolicyTargetSelectorCatalog.TryGet(handle.SelectorId, out PolicyTargetSelectorDescriptor descriptor)
				? "；descriptor=" + LimitDisplayChars(CompactPolicyContextText(descriptor.RetrievalText), 240)
				: "";
			return "- " + handle.Key + "=" + FirstNonEmpty(handle.DisplayName, handle.SelectorId, handle.EntityId, "未知")
				+ " [" + kind + count + selectorDescriptor + semanticEvidence + "]";
		}).ToList();
		List<string> concreteLines = rendered.Where((line, index) =>
			!string.Equals(handles[index].Kind, PolicyTargetKindSelector, StringComparison.OrdinalIgnoreCase)
			&& !string.Equals(handles[index].Kind, PolicyTargetKindPlan, StringComparison.OrdinalIgnoreCase)).ToList();
		List<string> selectorLines = rendered.Where((line, index) =>
			string.Equals(handles[index].Kind, PolicyTargetKindSelector, StringComparison.OrdinalIgnoreCase)
			|| string.Equals(handles[index].Kind, PolicyTargetKindPlan, StringComparison.OrdinalIgnoreCase)).ToList();
		return JoinPolicyPromptSections(
			concreteLines.Count > 0 ? "【具体实体/作用域目标】\n" + string.Join("\n", concreteLines) : string.Empty,
			selectorLines.Count > 0
				? "【集合目标 selector】\n" + string.Join("\n", selectorLines)
				: string.Empty);
	}

	private static PolicyTargetHandleDirectory EnsurePlayerPolicyTargetHandleDirectory(PolicyDraftRequest request)
	{
		if (request == null)
		{
			return new PolicyTargetHandleDirectory();
		}
		if (request.EffectTargetDirectory == null)
		{
			request.EffectTargetDirectory = new PolicyTargetHandleDirectory();
		}
		return request.EffectTargetDirectory;
	}

	private static PolicyTargetHandleDirectory BuildPlayerPolicyTargetHandleDirectory(
		PolicyDraftRequest request,
		IReadOnlyList<IPolicyEffectModule> injectedModules)
	{
		List<PolicyTargetHandleSaveData> handles = NormalizePolicyTargetHandles(request?.TargetHandles)
			.Where(handle => IsPolicyTargetHandleAllowedForRequest(request, handle))
			.ToList();
		Dictionary<string, PolicyTargetHandleSaveData> handleByKey = new Dictionary<string, PolicyTargetHandleSaveData>(StringComparer.Ordinal);
		foreach (PolicyTargetHandleSaveData handle in handles)
		{
			string key = handle?.Key ?? string.Empty;
			if (key.Length > 0)
			{
				handleByKey.Add(key, handle);
			}
		}
		PolicyEffectTargetResolver targetResolver = CreatePlayerPolicyEffectTargetResolver(request, handleByKey);
		return BuildPolicyTargetHandleDirectory(
			handles,
			injectedModules,
			targetResolver,
			request?.IssuerKingdomId);
	}

	private static PolicyTargetHandleDirectory BuildPolicyTargetHandleDirectory(
		IReadOnlyList<PolicyTargetHandleSaveData> handles,
		IReadOnlyList<IPolicyEffectModule> injectedModules,
		PolicyEffectTargetResolver targetResolver,
		string issuerKingdomId)
	{
		List<PolicyTargetHandleDirectoryCandidate> candidates = (handles
			?? Array.Empty<PolicyTargetHandleSaveData>())
			.Where(handle => handle != null && !string.IsNullOrWhiteSpace(handle.Key))
			.Select(handle => new PolicyTargetHandleDirectoryCandidate
			{
				Handle = handle.Key,
				Entry = BuildPolicyTargetHandleDirectoryEntry(handle)
			})
			.ToList();
		return PolicyTargetHandleDirectoryBuilder.Build(
			candidates,
			injectedModules,
			targetResolver,
			issuerKingdomId);
	}

	private static PolicyTargetHandleDirectoryEntry BuildPolicyTargetHandleDirectoryEntry(PolicyTargetHandleSaveData handle)
	{
		string description = FirstNonEmpty(handle?.DisplayName, handle?.SelectorId, handle?.EntityId, "未知目标");
		if (handle != null
			&& (string.Equals(handle.Kind, PolicyTargetKindSelector, StringComparison.OrdinalIgnoreCase)
				|| string.Equals(handle.Kind, PolicyTargetKindPlan, StringComparison.OrdinalIgnoreCase))
			&& description.IndexOf("当前" + handle.CurrentSettlementCount.ToString(CultureInfo.InvariantCulture) + "处", StringComparison.Ordinal) < 0)
		{
			description += "；当前" + Math.Max(0, handle.CurrentSettlementCount).ToString(CultureInfo.InvariantCulture) + "处城镇/城堡";
		}
		if (handle != null
			&& string.Equals(handle.Kind, PolicyTargetKindSelector, StringComparison.OrdinalIgnoreCase)
			&& PolicyTargetSelectorCatalog.TryGet(handle.SelectorId, out PolicyTargetSelectorDescriptor descriptor))
		{
			description += "；descriptor=" + descriptor.RetrievalText;
		}
		return new PolicyTargetHandleDirectoryEntry
		{
			Kind = handle?.Kind ?? string.Empty,
			Description = LimitDisplayChars(CompactPolicyContextText(description), 240),
			SelectorId = string.IsNullOrWhiteSpace(handle?.SelectorId) ? null : handle.SelectorId,
			EntityId = string.IsNullOrWhiteSpace(handle?.EntityId) ? null : handle.EntityId,
			TargetPlanVersion = handle?.TargetPlan?.PlanVersion ?? 0,
			CurrentSettlementCount = Math.Max(0, handle?.CurrentSettlementCount ?? 0),
			FollowCurrentRulingClan = handle?.FollowCurrentRulingClan == true
		};
	}

	private static string SerializePlayerPolicyTargetHandleDirectory(PolicyTargetHandleDirectory directory)
	{
		return PolicyEffectDirectPlanContract.SerializeDirectory(directory);
	}

	private static string BuildPolicyTargetSelectionPromptRule(PolicyDraftRequest request)
	{
		bool hasSelector = NormalizePolicyTargetHandles(request?.TargetHandles)
			.Any(handle => string.Equals(handle.Kind, PolicyTargetKindSelector, StringComparison.OrdinalIgnoreCase));
		bool hasPlan = NormalizePolicyTargetHandles(request?.TargetHandles)
			.Any(handle => string.Equals(handle.Kind, PolicyTargetKindPlan, StringComparison.OrdinalIgnoreCase));
		string rule = "【目标句柄选择规则】\n"
			+ "目标句柄只由本阶段根据政策原文、实际注入能力及本次合法目录决定；第一次通用评议中的范围结论不具授权效力。"
			+ "目录中的句柄都只是候选。先按原文确定每个实际采用效果真正结算的对象，再选择该能力允许且语义等价的句柄。"
			+ "仅作为发布者、执行者、联系人、报告对象、政策启动费或维护费承担人出现，不等于可执行效果目标。"
			+ "不得回填玩家本人、玩家家族、玩家王国或任何目录外对象，也不得把缺失锚点改写为其他可用目标。";
		if (hasSelector)
		{
			rule += "X* 仅用于旧请求兼容；只能在目录实际出现且语义完全一致时使用。";
		}
		if (hasPlan)
		{
			rule += "P* 是由政策原文中的明确实体、关系、组合、排除、排序、方向、距离或指标条件确定性解析形成的 TargetPlan。若政策效果范围与 P* 摘要一致，可原样选择；不得自行改写、拼接或扩展该计划。";
		}
		return rule;
	}

	private static bool IsPolicyTargetHandleAllowedForRequest(PolicyDraftRequest request, PolicyTargetHandleSaveData target)
	{
		if (request == null || target == null)
		{
			return false;
		}
		PlayerPolicyTargetAuthorization authorization = EnsurePlayerPolicyTargetAuthorization(request);
		if (string.Equals(target.Kind, PolicyTargetKindSelector, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (string.Equals(target.Kind, PolicyTargetKindPlan, StringComparison.OrdinalIgnoreCase))
		{
			return PolicyTargetPlanResolver.TryNormalizeAndValidate(
					target.TargetPlan,
					out PolicyTargetPlanSaveData normalizedPlan,
					out _)
				&& authorization.PlanSignatures.Contains(normalizedPlan.NormalizedSignature)
				&& IsPolicyTargetPlanPositivelyBounded(normalizedPlan)
				&& TryResolvePolicyTargetPlanForRequest(
					request,
					target.TargetPlan,
					out PolicyTargetPlanResolution planResolution,
					out _)
				&& planResolution?.IsTemporarilyEmpty == false;
		}
		if (string.Equals(target.Kind, PolicyTargetKindHero, StringComparison.OrdinalIgnoreCase))
		{
			return IsPlayerPolicyHeroTargetAuthorized(request, target, authorization);
		}
		string entityId = (target.EntityId ?? string.Empty).Trim();
		string kingdomId = (target.KingdomId ?? string.Empty).Trim();
		if (IsLocalPolicyRequest(request))
		{
			if (string.Equals(target.Kind, PolicyTargetKindSource, StringComparison.OrdinalIgnoreCase))
			{
				return string.Equals(target.Key, "S", StringComparison.OrdinalIgnoreCase);
			}
			return entityId.Length > 0
				&& string.Equals(kingdomId, request.PlayerKingdomId ?? string.Empty, StringComparison.OrdinalIgnoreCase)
				&& authorization.EntityKeys.Contains(BuildPlayerPolicyEntityAuthorizationKey(target.Kind, entityId));
		}
		if (!string.Equals(target.Kind, PolicyTargetKindKingdom, StringComparison.OrdinalIgnoreCase)
			|| entityId.Length == 0
			|| kingdomId.Length == 0
			|| !string.Equals(entityId, kingdomId, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		return authorization.KingdomIds.Contains(entityId);
	}

	private static PlayerPolicyTargetAuthorization EnsurePlayerPolicyTargetAuthorization(PolicyDraftRequest request)
	{
		if (request == null)
		{
			return new PlayerPolicyTargetAuthorization();
		}
		PolicyTargetWorldSnapshot snapshot = request.SemanticTargetSnapshot;
		string query = ((request.PolicyName ?? string.Empty) + "\n" + (request.PolicyContent ?? string.Empty)).Trim();
		string cacheKey = string.Join("\n", new[]
		{
			request.ScopeKind ?? string.Empty,
			request.PlayerKingdomId ?? string.Empty,
			request.IssuerKingdomId ?? string.Empty,
			request.ProposerClanId ?? string.Empty,
			string.Join(",", (request.SelectedFiefIds ?? new List<string>()).OrderBy(id => id, StringComparer.OrdinalIgnoreCase)),
			(snapshot?.StableVersion ?? 0L).ToString(CultureInfo.InvariantCulture),
			(snapshot?.DynamicVersion ?? 0L).ToString(CultureInfo.InvariantCulture),
			query
		});
		if (request.TargetAuthorization != null
			&& string.Equals(request.TargetAuthorization.CacheKey, cacheKey, StringComparison.Ordinal))
		{
			return request.TargetAuthorization;
		}

		PlayerPolicyTargetAuthorization authorization = new PlayerPolicyTargetAuthorization { CacheKey = cacheKey };
		string targetKingdomId = (request.PlayerKingdomId ?? string.Empty).Trim();
		if (targetKingdomId.Length > 0)
		{
			authorization.KingdomIds.Add(targetKingdomId);
			authorization.AllowedKingdomReferenceIds.Add(targetKingdomId);
		}
		if (IsCurrentDirectVassalPolicyAnchor(request)
			&& !string.IsNullOrWhiteSpace(request.IssuerKingdomId)
			&& !string.Equals(request.IssuerKingdomId, targetKingdomId, StringComparison.OrdinalIgnoreCase))
		{
			authorization.KingdomIds.Add(request.IssuerKingdomId);
			authorization.AllowedKingdomReferenceIds.Add(request.IssuerKingdomId);
		}

		foreach (string sourceId in request.SelectedFiefIds ?? new List<string>())
		{
			AddPlayerPolicyEntityReference(authorization, PolicyTargetEntityKinds.Settlement, sourceId);
		}
		if (!string.IsNullOrWhiteSpace(request.ProposerClanId))
		{
			authorization.AllowedEntityReferenceIds.Add(request.ProposerClanId);
		}

		if (snapshot?.Entities != null)
		{
			foreach (string kind in new[]
			{
				PolicyTargetEntityKinds.Kingdom,
				PolicyTargetEntityKinds.Clan,
				PolicyTargetEntityKinds.Ruler,
				PolicyTargetEntityKinds.Settlement
			})
			{
				foreach (PolicyTargetEntitySnapshot entity in PolicyTargetObjectiveEvidence.FindStrictMentionedEntities(
					snapshot.Entities,
					query,
					kind))
				{
					string ownerKingdomId = (entity.OwnerKingdomId ?? string.Empty).Trim();
					if (ownerKingdomId.Length > 0)
					{
						authorization.KingdomIds.Add(ownerKingdomId);
						authorization.AllowedKingdomReferenceIds.Add(ownerKingdomId);
					}
					AddPlayerPolicyEntityReference(authorization, entity.Kind, entity.EntityId);
				}
			}
			AddStrictPolicyKingdomAliasEvidence(query, authorization);
			AddExplicitPolicyRelationKingdoms(query, targetKingdomId, snapshot, authorization);
		}

		PolicyTargetSemanticContext context = new PolicyTargetSemanticContext
		{
			QueryText = query,
			Scope = request.ScopeKind ?? PolicyScopeKingdom,
			TargetKingdomId = targetKingdomId,
			IssuerKingdomId = request.IssuerKingdomId ?? string.Empty,
			PlayerClanId = TryGetPlayerClanId(),
			ProposerClanId = request.ProposerClanId ?? string.Empty,
			SourceSettlementIds = request.SelectedFiefIds ?? new List<string>(),
			Snapshot = snapshot,
			StrictEntityEvidence = true
		};
		authorization.PlanRoute = PolicyTargetPlanRouter.RouteDeterministicForPlayer(query, context);
		if (IsLocalPolicyRequest(request))
		{
			authorization.PlanRoute = new PolicyTargetPlanRouteResult
			{
				Candidates = authorization.PlanRoute.Candidates
					.Where(candidate => IsLocalPlayerPolicyPlanObjectivelyBounded(candidate?.Plan, authorization))
					.ToArray(),
				MatchedExistingHandleKeys = authorization.PlanRoute.MatchedExistingHandleKeys,
				Issues = authorization.PlanRoute.Issues,
				HasExplicitTargetIntent = authorization.PlanRoute.HasExplicitTargetIntent
			};
		}
		foreach (PolicyTargetPlanCandidate candidate in authorization.PlanRoute.Candidates ?? Array.Empty<PolicyTargetPlanCandidate>())
		{
			if (PolicyTargetPlanResolver.TryNormalizeAndValidate(candidate?.Plan, out PolicyTargetPlanSaveData plan, out _))
			{
				authorization.PlanSignatures.Add(plan.NormalizedSignature);
				foreach (PolicyTargetPlanBranchSaveData branch in plan.Branches)
				{
					foreach (string id in branch.EntityReferences.Concat(branch.ExcludedEntityReferences))
					{
						authorization.AllowedEntityReferenceIds.Add(id);
					}
					if (!string.IsNullOrWhiteSpace(branch.ReferenceClanId))
					{
						authorization.AllowedEntityReferenceIds.Add(branch.ReferenceClanId);
					}
					foreach (string id in branch.NamedKingdomIds.Concat(new[] { branch.AnchorKingdomId }))
					{
						if (!string.IsNullOrWhiteSpace(id)) authorization.AllowedKingdomReferenceIds.Add(id);
					}
				}
			}
		}
		request.TargetAuthorization = authorization;
		return authorization;
	}

	private static bool IsCurrentDirectVassalPolicyAnchor(PolicyDraftRequest request)
	{
		if (!IsVassalPolicyRequest(request)
			|| string.IsNullOrWhiteSpace(request?.PlayerKingdomId)
			|| string.IsNullOrWhiteSpace(request.IssuerKingdomId))
		{
			return false;
		}
		try
		{
			Kingdom issuer = Clan.PlayerClan?.Kingdom;
			return issuer != null
				&& string.Equals(issuer.StringId, request.IssuerKingdomId, StringComparison.OrdinalIgnoreCase)
				&& VassalageBehavior.GetPlayerDirectVassalKingdomsForExternal().Any(kingdom =>
					kingdom != null
					&& string.Equals(kingdom.StringId, request.PlayerKingdomId, StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return false;
		}
	}

	private static bool IsLocalPlayerPolicyPlanObjectivelyBounded(
		PolicyTargetPlanSaveData source,
		PlayerPolicyTargetAuthorization authorization)
	{
		if (!PolicyTargetPlanResolver.TryNormalizeAndValidate(source, out PolicyTargetPlanSaveData plan, out _))
		{
			return false;
		}
		return plan.Branches.All(branch =>
		{
			List<string> references = branch.EntityReferences
				.Concat(branch.ExcludedEntityReferences)
				.Concat(new[] { branch.ReferenceClanId })
				.Where(id => !string.IsNullOrWhiteSpace(id))
				.ToList();
			return references.Count > 0
				&& references.All(id => authorization.AllowedEntityReferenceIds.Contains(id))
				&& branch.NamedKingdomIds.Count == 0
				&& branch.Relation != PolicyTargetPlanRelation.Enemy
				&& branch.Relation != PolicyTargetPlanRelation.Ally
				&& branch.Relation != PolicyTargetPlanRelation.Foreign;
		});
	}

	private static bool IsPlayerPolicyHeroTargetAuthorized(
		PolicyDraftRequest request,
		PolicyTargetHandleSaveData target,
		PlayerPolicyTargetAuthorization authorization)
	{
		if (IsLocalPolicyRequest(request)
			|| !PolicyHeroTargetSelectorResolver.TryDescribeSelector(
				target.SelectorId,
				out string selectorKind,
				out string selectorValue,
				out string selectorKingdomId))
		{
			return false;
		}
		string declaredKingdomId = (target.KingdomId ?? string.Empty).Trim();
		if (declaredKingdomId.Length == 0
			|| !string.Equals(declaredKingdomId, selectorKingdomId, StringComparison.OrdinalIgnoreCase)
			|| !authorization.KingdomIds.Contains(selectorKingdomId))
		{
			return false;
		}
		bool currentKingdom = string.Equals(selectorKingdomId, request.PlayerKingdomId ?? string.Empty, StringComparison.OrdinalIgnoreCase);
		if (currentKingdom
			&& string.Equals(selectorKind, "role", StringComparison.Ordinal)
			&& (string.Equals(selectorValue, "ruler", StringComparison.Ordinal)
				|| string.Equals(selectorValue, "lords", StringComparison.Ordinal)
				|| string.Equals(selectorValue, "clan-leaders", StringComparison.Ordinal)))
		{
			return true;
		}
		return PolicyHeroTargetSelectorResolver.IsSelectorExplicitlyMentioned(
			target.SelectorId,
			((request.PolicyName ?? string.Empty) + "\n" + (request.PolicyContent ?? string.Empty)).Trim(),
			authorization.KingdomIds);
	}

	private static void AddPlayerPolicyEntityReference(
		PlayerPolicyTargetAuthorization authorization,
		string kind,
		string entityId)
	{
		string id = (entityId ?? string.Empty).Trim();
		if (authorization == null || id.Length == 0)
		{
			return;
		}
		authorization.EntityKeys.Add(BuildPlayerPolicyEntityAuthorizationKey(kind, id));
		authorization.AllowedEntityReferenceIds.Add(id);
	}

	private static string BuildPlayerPolicyEntityAuthorizationKey(string kind, string entityId)
	{
		return ((kind ?? string.Empty).Trim().ToLowerInvariant()) + "\n" + ((entityId ?? string.Empty).Trim().ToLowerInvariant());
	}

	private static void AddStrictPolicyKingdomAliasEvidence(
		string query,
		PlayerPolicyTargetAuthorization authorization)
	{
		try
		{
			List<KeyValuePair<string, Kingdom>> aliases = (Kingdom.All ?? Enumerable.Empty<Kingdom>())
				.Where(kingdom => kingdom != null && !string.IsNullOrWhiteSpace(kingdom.StringId))
				.SelectMany(kingdom => GetPolicyKingdomAliases(kingdom)
					.Select(alias => new KeyValuePair<string, Kingdom>((alias ?? string.Empty).Trim(), kingdom)))
				.Where(pair => pair.Key.Length >= 2 && !PolicyTargetObjectiveEvidence.IsGenericAlias(pair.Key))
				.ToList();
			foreach (IGrouping<string, KeyValuePair<string, Kingdom>> group in aliases.GroupBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase))
			{
				List<Kingdom> owners = group.Select(pair => pair.Value)
					.GroupBy(kingdom => kingdom.StringId, StringComparer.OrdinalIgnoreCase)
					.Select(owner => owner.First())
					.ToList();
				if (owners.Count == 1 && query.IndexOf(group.Key, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					authorization.KingdomIds.Add(owners[0].StringId);
					authorization.AllowedKingdomReferenceIds.Add(owners[0].StringId);
				}
			}
		}
		catch
		{
		}
	}

	private static void AddExplicitPolicyRelationKingdoms(
		string query,
		string anchorKingdomId,
		PolicyTargetWorldSnapshot snapshot,
		PlayerPolicyTargetAuthorization authorization)
	{
		IReadOnlyCollection<string> pairs = PolicyTextMentions(query, "敌国", "交战国", "敌对国家")
			? snapshot?.WarPairs
			: PolicyTextMentions(query, "盟国", "同盟国", "盟友国家")
				? snapshot?.AlliancePairs
				: null;
		foreach (string pair in pairs ?? Array.Empty<string>())
		{
			string[] ids = (pair ?? string.Empty).Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
			if (ids.Length != 2)
			{
				continue;
			}
			string other = string.Equals(ids[0], anchorKingdomId, StringComparison.OrdinalIgnoreCase) ? ids[1]
				: string.Equals(ids[1], anchorKingdomId, StringComparison.OrdinalIgnoreCase) ? ids[0]
				: string.Empty;
			if (other.Length > 0)
			{
				authorization.KingdomIds.Add(other);
				authorization.AllowedKingdomReferenceIds.Add(other);
			}
		}
	}

	private static string TryGetPlayerClanId()
	{
		try
		{
			return Clan.PlayerClan?.StringId ?? string.Empty;
		}
		catch
		{
			return string.Empty;
		}
	}

	private static bool IsPolicyTargetPlanPositivelyBounded(PolicyTargetPlanSaveData source)
	{
		if (!PolicyTargetPlanResolver.TryNormalizeAndValidate(
			source,
			out PolicyTargetPlanSaveData plan,
			out _))
		{
			return false;
		}
		return plan.Branches.All(branch =>
			branch.Relation != PolicyTargetPlanRelation.Any
			|| branch.EntityReferences.Count > 0
			|| branch.OwnerClanPredicate == PolicyTargetPlanOwnerClanPredicate.PlayerClan
			|| branch.OwnerClanPredicate == PolicyTargetPlanOwnerClanPredicate.ProposerClan
			|| branch.OwnerClanPredicate == PolicyTargetPlanOwnerClanPredicate.SpecificClan);
	}

	private static string NormalizeLocalPolicyTargetScope(string value)
	{
		string scope = (value ?? "").Trim();
		if (string.Equals(scope, LocalPolicyTargetScopeSource, StringComparison.OrdinalIgnoreCase))
		{
			return LocalPolicyTargetScopeSource;
		}
		if (string.Equals(scope, LocalPolicyTargetScopeMentioned, StringComparison.OrdinalIgnoreCase))
		{
			return LocalPolicyTargetScopeMentioned;
		}
		return "";
	}

	private Kingdom ResolveTargetKingdom(PolicyEffectDto effect, Kingdom playerKingdom)
	{
		string id = (effect?.TargetKingdomId ?? "").Trim();
		string name = (effect?.TargetKingdomName ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(name))
		{
			return playerKingdom;
		}
		try
		{
			foreach (Kingdom kingdom in Kingdom.All.Where(k => k != null))
			{
				if (!string.IsNullOrWhiteSpace(id) && string.Equals(kingdom.StringId, id, StringComparison.OrdinalIgnoreCase))
				{
					return kingdom;
				}
				if (!string.IsNullOrWhiteSpace(name) && string.Equals(GetKingdomName(kingdom), name, StringComparison.OrdinalIgnoreCase))
				{
					return kingdom;
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static bool IsLocalPolicyRequest(PolicyDraftRequest request)
	{
		return string.Equals(request?.ScopeKind ?? "", PolicyScopeLocal, StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsVassalPolicyRequest(PolicyDraftRequest request)
	{
		return string.Equals(request?.ScopeKind ?? "", PolicyScopeVassal, StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsLocalActivePolicyEffect(ActivePolicyEffectSaveData effect)
	{
		return string.Equals(effect?.ScopeKind ?? "", PolicyScopeLocal, StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsVassalActivePolicyEffect(ActivePolicyEffectSaveData effect)
	{
		return string.Equals(effect?.ScopeKind ?? "", PolicyScopeVassal, StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsSourceVassalPolicyEffect(ActivePolicyEffectSaveData effect)
	{
		return IsVassalActivePolicyEffect(effect)
			&& string.Equals(effect?.TargetHandle ?? "", "K0", StringComparison.OrdinalIgnoreCase);
	}

	private static string GetLocalPolicyTargetScope(ActivePolicyEffectSaveData effect)
	{
		string scope = NormalizeLocalPolicyTargetScope(effect?.LocalTargetScope);
		return string.IsNullOrWhiteSpace(scope) ? LocalPolicyTargetScopeSource : scope;
	}

	private static bool IsMentionedLocalPolicyEffect(ActivePolicyEffectSaveData effect)
	{
		return IsLocalActivePolicyEffect(effect)
			&& string.Equals(GetLocalPolicyTargetScope(effect), LocalPolicyTargetScopeMentioned, StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsPlayerOwnedLocalPolicyFief(Settlement settlement)
	{
		try
		{
			return settlement != null
				&& (settlement.IsTown || settlement.IsCastle)
				&& Clan.PlayerClan != null
				&& settlement.OwnerClan == Clan.PlayerClan;
		}
		catch
		{
			return false;
		}
	}

	private static Settlement ResolvePrimaryPolicyFief(Settlement settlement)
	{
		if (settlement == null)
		{
			return null;
		}
		if (settlement.IsTown || settlement.IsCastle)
		{
			return settlement;
		}
		Settlement bound = settlement.Village?.Bound;
		return bound != null && (bound.IsTown || bound.IsCastle) ? bound : null;
	}

	private static int CountLocalPolicyPrimaryFiefs(IEnumerable<Settlement> settlements)
	{
		return (settlements ?? Enumerable.Empty<Settlement>())
			.Select(ResolvePrimaryPolicyFief)
			.Where(settlement => settlement != null && !string.IsNullOrWhiteSpace(settlement.StringId))
			.Select(settlement => settlement.StringId)
			.Distinct(StringComparer.OrdinalIgnoreCase)
			.Count();
	}

	private static bool IsPrimaryPolicyFiefTarget(PolicyDraftRequest request, PolicyTargetHandleSaveData target)
	{
		string entityId = (target?.EntityId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(entityId))
		{
			return false;
		}
		IReadOnlyList<PolicyTargetEntitySnapshot> snapshotEntities = request?.SemanticTargetSnapshot?.Entities;
		if (snapshotEntities != null)
		{
			return snapshotEntities.Any(entity => entity != null
				&& string.Equals(entity.Kind, PolicyTargetEntityKinds.Settlement, StringComparison.OrdinalIgnoreCase)
				&& (entity.IsCity || entity.IsCastle)
				&& string.Equals(entity.EntityId, entityId, StringComparison.OrdinalIgnoreCase)
				&& (target?.IsSemanticTarget == true
					|| string.Equals(entity.OwnerKingdomId, request?.PlayerKingdomId ?? "", StringComparison.OrdinalIgnoreCase))
				&& request?.SelectedFiefIds?.Contains(entityId, StringComparer.OrdinalIgnoreCase) != true);
		}
		try
		{
			Settlement settlement = (Settlement.All ?? Enumerable.Empty<Settlement>()).FirstOrDefault(candidate => candidate != null
				&& string.Equals(candidate.StringId, entityId, StringComparison.OrdinalIgnoreCase));
			return settlement != null
				&& (settlement.IsTown || settlement.IsCastle)
				&& (target?.IsSemanticTarget == true || IsSettlementInCurrentPlayerKingdom(settlement))
				&& request?.SelectedFiefIds?.Contains(entityId, StringComparer.OrdinalIgnoreCase) != true;
		}
		catch
		{
			return false;
		}
	}

	private LocalPolicyMentionTargetSelection ResolveLocalPolicyMentionTargets(string policyName, string policyContent, Kingdom playerKingdom, IEnumerable<Settlement> sourceFiefs)
	{
		LocalPolicyMentionTargetSelection result = new LocalPolicyMentionTargetSelection();
		string policyText = ((policyName ?? "") + "\n" + (policyContent ?? "")).Trim();
		if (playerKingdom == null || string.IsNullOrWhiteSpace(policyText))
		{
			return result;
		}
		HashSet<string> sourceSettlementIds = new HashSet<string>(
			ExpandLocalPolicySettlements(sourceFiefs).Select(x => x?.StringId ?? "").Where(x => !string.IsNullOrWhiteSpace(x)),
			StringComparer.OrdinalIgnoreCase);
		int clanHandleIndex = 0;
		int rulerHandleIndex = 0;
		int settlementHandleIndex = 0;
		HashSet<string> addedPrimaryFiefIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		try
		{
			foreach (Clan clan in ((IEnumerable<Clan>)playerKingdom.Clans ?? Enumerable.Empty<Clan>()).Where(x => x != null))
			{
				bool mentionsClan = PolicyTextMentions(policyText,
					clan.StringId ?? "",
					clan.Name?.ToString() ?? "",
					clan.InformalName?.ToString() ?? "");
				bool mentionsLeader = PolicyTextMentions(policyText,
					clan.Leader?.StringId ?? "",
					clan.Leader?.Name?.ToString() ?? "");
				if (!mentionsClan && !mentionsLeader)
				{
					continue;
				}
				result.ClanIds.Add(clan.StringId ?? "");
				if (mentionsClan)
				{
					result.TargetHandles.Add(new PolicyTargetHandleSaveData
					{
						Key = "C" + (clanHandleIndex++).ToString(CultureInfo.InvariantCulture),
						Kind = PolicyTargetKindClan,
						EntityId = clan.StringId ?? "",
						DisplayName = clan.Name?.ToString() ?? clan.StringId ?? "未知家族",
						KingdomId = playerKingdom.StringId ?? "",
						KingdomName = GetKingdomName(playerKingdom)
					});
				}
				else
				{
					result.TargetHandles.Add(new PolicyTargetHandleSaveData
					{
						Key = "R" + (rulerHandleIndex++).ToString(CultureInfo.InvariantCulture),
						Kind = PolicyTargetKindRuler,
						EntityId = clan.StringId ?? "",
						DisplayName = (clan.Leader?.Name?.ToString() ?? clan.Name?.ToString() ?? clan.StringId ?? "未知领袖") + "→其氏族领地",
						KingdomId = playerKingdom.StringId ?? "",
						KingdomName = GetKingdomName(playerKingdom)
					});
				}
			}
		}
		catch
		{
		}
		try
		{
			string rulerTitle = playerKingdom.EncyclopediaRulerTitle?.ToString() ?? "";
			bool namesCurrentRuler = PolicyTextMentions(
				policyText,
				playerKingdom.Leader?.StringId ?? "",
				playerKingdom.Leader?.Name?.ToString() ?? "");
			if (!namesCurrentRuler && PolicyTextMentions(policyText,
				rulerTitle,
				"统治者",
				"君主",
				"国王",
				"女王",
				"皇帝",
				"女皇",
				"可汗",
				"苏丹",
				"执政者"))
			{
				result.FollowCurrentRulingClan = true;
				result.TargetHandles.Add(new PolicyTargetHandleSaveData
				{
					Key = "R" + (rulerHandleIndex++).ToString(CultureInfo.InvariantCulture),
					Kind = PolicyTargetKindRuler,
					EntityId = "",
					DisplayName = "当前统治者→当前统治家族领地",
					KingdomId = playerKingdom.StringId ?? "",
					KingdomName = GetKingdomName(playerKingdom),
					FollowCurrentRulingClan = true
				});
			}
		}
		catch
		{
		}
		try
		{
			foreach (Settlement settlement in GetKingdomSettlements(playerKingdom))
			{
				string mentionedSettlementId = (settlement?.StringId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(mentionedSettlementId)
					|| !PolicyTextMentions(policyText, mentionedSettlementId, settlement.Name?.ToString() ?? ""))
				{
					continue;
				}
				Settlement primaryFief = ResolvePrimaryPolicyFief(settlement);
				string primaryFiefId = (primaryFief?.StringId ?? "").Trim();
				if (string.IsNullOrWhiteSpace(primaryFiefId)
					|| sourceSettlementIds.Contains(primaryFiefId)
					|| !addedPrimaryFiefIds.Add(primaryFiefId))
				{
					continue;
				}
				result.SettlementIds.Add(primaryFiefId);
				result.TargetHandles.Add(new PolicyTargetHandleSaveData
				{
					Key = "L" + (settlementHandleIndex++).ToString(CultureInfo.InvariantCulture),
					Kind = PolicyTargetKindSettlement,
					EntityId = primaryFiefId,
					DisplayName = primaryFief.Name?.ToString() ?? primaryFiefId,
					KingdomId = playerKingdom.StringId ?? "",
					KingdomName = GetKingdomName(playerKingdom)
				});
			}
		}
		catch
		{
		}
		foreach (PolicyHeroTargetCandidate candidate in PolicyHeroTargetSelectorResolver.BuildCandidates(
			policyText,
			new[] { playerKingdom }))
		{
			result.TargetHandles.Add(new PolicyTargetHandleSaveData
			{
				Key = "H" + result.TargetHandles.Count(handle =>
					string.Equals(handle?.Kind, PolicyTargetKindHero, StringComparison.OrdinalIgnoreCase))
					.ToString(CultureInfo.InvariantCulture),
				Kind = PolicyTargetKindHero,
				SelectorId = candidate.SelectorId,
				EntityId = candidate.CurrentHeroIds.Count == 1 ? candidate.CurrentHeroIds[0] : string.Empty,
				DisplayName = candidate.DisplayName,
				KingdomId = candidate.AnchorKingdomId,
				KingdomName = GetKingdomName(playerKingdom)
			});
		}
		List<string> normalizedClanIds = NormalizeIdList(result.ClanIds);
		List<string> normalizedSettlementIds = NormalizeIdList(result.SettlementIds);
		result.ClanIds.Clear();
		result.ClanIds.AddRange(normalizedClanIds);
		result.SettlementIds.Clear();
		result.SettlementIds.AddRange(normalizedSettlementIds);
		result.TargetHandles.RemoveAll(x => x == null || string.IsNullOrWhiteSpace(x.Key));
		result.CurrentSettlementCount = CountLocalPolicyPrimaryFiefs(ResolveLocalMentionedPolicySettlements(
			result.ClanIds,
			result.SettlementIds,
			result.FollowCurrentRulingClan,
			sourceFiefs));
		return result;
	}

	private List<PolicyTargetHandleSaveData> BuildLocalPolicyTargetHandles(
		LocalPolicyMentionTargetSelection selection,
		IEnumerable<Settlement> sourceFiefs,
		Kingdom playerKingdom)
	{
		List<Settlement> sourceList = (sourceFiefs ?? Enumerable.Empty<Settlement>()).Where(x => x != null).ToList();
		List<PolicyTargetHandleSaveData> result = new List<PolicyTargetHandleSaveData>
		{
			new PolicyTargetHandleSaveData
			{
				Key = "S",
				Kind = PolicyTargetKindSource,
				EntityId = "",
				DisplayName = "发布地（" + string.Join("、", sourceList.Select(x => x.Name?.ToString() ?? x.StringId)) + "）",
				KingdomId = playerKingdom?.StringId ?? "",
				KingdomName = playerKingdom == null ? "" : GetKingdomName(playerKingdom),
				CurrentSettlementCount = CountLocalPolicyPrimaryFiefs(sourceList)
			}
		};
		foreach (PolicyTargetHandleSaveData candidate in selection?.TargetHandles ?? new List<PolicyTargetHandleSaveData>())
		{
			if (candidate == null || string.IsNullOrWhiteSpace(candidate.Key))
			{
				continue;
			}
			PolicyTargetHandleSaveData copy = ClonePolicyTargetHandle(candidate);
			copy.CurrentSettlementCount = CountLocalPolicyPrimaryFiefs(ResolveLocalPolicyHandleSettlements(copy, sourceList));
			result.Add(copy);
		}
		return NormalizePolicyTargetHandles(result);
	}

	private static List<PolicyTargetHandleSaveData> BuildKingdomPolicyTargetHandles(
		string policyName,
		string policyContent,
		Kingdom targetKingdom,
		Kingdom issuerKingdom,
		PolicyTargetWorldSnapshot snapshot)
	{
		List<PolicyTargetHandleSaveData> result = new List<PolicyTargetHandleSaveData>();
		if (targetKingdom != null)
		{
			result.Add(new PolicyTargetHandleSaveData
			{
				Key = "K0",
				Kind = PolicyTargetKindKingdom,
				EntityId = targetKingdom.StringId ?? "",
				DisplayName = GetKingdomName(targetKingdom),
				KingdomId = targetKingdom.StringId ?? "",
				KingdomName = GetKingdomName(targetKingdom)
			});
		}
		if (issuerKingdom != null && issuerKingdom != targetKingdom)
		{
			result.Add(new PolicyTargetHandleSaveData
			{
				Key = "K1",
				Kind = PolicyTargetKindKingdom,
				EntityId = issuerKingdom.StringId ?? "",
				DisplayName = GetKingdomName(issuerKingdom),
				KingdomId = issuerKingdom.StringId ?? "",
				KingdomName = GetKingdomName(issuerKingdom)
			});
		}
		string policyText = ((policyName ?? "") + "\n" + (policyContent ?? "")).Trim();
		if (string.IsNullOrWhiteSpace(policyText))
		{
			return result;
		}
		int index = result.Count;
		PlayerPolicyTargetAuthorization objectiveEvidence = new PlayerPolicyTargetAuthorization();
		foreach (string kind in new[]
		{
			PolicyTargetEntityKinds.Kingdom,
			PolicyTargetEntityKinds.Clan,
			PolicyTargetEntityKinds.Ruler,
			PolicyTargetEntityKinds.Settlement
		})
		{
			foreach (PolicyTargetEntitySnapshot entity in PolicyTargetObjectiveEvidence.FindStrictMentionedEntities(
				snapshot?.Entities,
				policyText,
				kind))
			{
				if (!string.IsNullOrWhiteSpace(entity.OwnerKingdomId))
				{
					objectiveEvidence.KingdomIds.Add(entity.OwnerKingdomId);
				}
			}
		}
		AddStrictPolicyKingdomAliasEvidence(policyText, objectiveEvidence);
		AddExplicitPolicyRelationKingdoms(policyText, targetKingdom?.StringId ?? string.Empty, snapshot, objectiveEvidence);
		try
		{
			foreach (Kingdom kingdom in (Kingdom.All ?? Enumerable.Empty<Kingdom>())
				.Where(x => x != null && x != targetKingdom && x != issuerKingdom)
				.OrderBy(x => GetKingdomName(x), StringComparer.OrdinalIgnoreCase))
			{
				if (!objectiveEvidence.KingdomIds.Contains(kingdom.StringId ?? string.Empty))
				{
					continue;
				}
				result.Add(new PolicyTargetHandleSaveData
				{
					Key = "K" + (index++).ToString(CultureInfo.InvariantCulture),
					Kind = PolicyTargetKindKingdom,
					EntityId = kingdom.StringId ?? "",
					DisplayName = GetKingdomName(kingdom),
					KingdomId = kingdom.StringId ?? "",
					KingdomName = GetKingdomName(kingdom)
				});
			}
		}
		catch
		{
		}
		Dictionary<string, Kingdom> heroAnchorKingdoms = (Kingdom.All ?? Enumerable.Empty<Kingdom>())
			.Where(kingdom => kingdom != null && result.Any(handle => string.Equals(
				handle?.Kind,
				PolicyTargetKindKingdom,
				StringComparison.OrdinalIgnoreCase)
				&& string.Equals(handle.EntityId, kingdom.StringId, StringComparison.OrdinalIgnoreCase)))
			.GroupBy(kingdom => kingdom.StringId, StringComparer.OrdinalIgnoreCase)
			.ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
		foreach (PolicyHeroTargetCandidate candidate in PolicyHeroTargetSelectorResolver.BuildCandidates(
			policyText,
			heroAnchorKingdoms.Values))
		{
			result.Add(new PolicyTargetHandleSaveData
			{
				Key = GetNextPolicyTargetHandleKey(result, "H"),
				Kind = PolicyTargetKindHero,
				SelectorId = candidate.SelectorId,
				EntityId = candidate.CurrentHeroIds.Count == 1 ? candidate.CurrentHeroIds[0] : string.Empty,
				DisplayName = candidate.DisplayName,
				KingdomId = candidate.AnchorKingdomId,
				KingdomName = GetKingdomName((Kingdom.All ?? Enumerable.Empty<Kingdom>()).FirstOrDefault(kingdom => kingdom != null
					&& string.Equals(kingdom.StringId, candidate.AnchorKingdomId, StringComparison.OrdinalIgnoreCase)))
			});
		}
		return NormalizePolicyTargetHandles(result);
	}

	private static bool PolicyTextMentionsForeignKingdomEntity(string policyText, Kingdom kingdom)
	{
		if (kingdom == null || string.IsNullOrWhiteSpace(policyText))
		{
			return false;
		}
		if (PolicyTextMentionsKingdom(policyText, kingdom)
			|| PolicyTextMentions(policyText, kingdom.Leader?.StringId ?? "", kingdom.Leader?.Name?.ToString() ?? ""))
		{
			return true;
		}
		try
		{
			foreach (Clan clan in ((IEnumerable<Clan>)kingdom.Clans ?? Enumerable.Empty<Clan>()).Where(x => x != null))
			{
				if (PolicyTextMentions(policyText,
					clan.StringId ?? "",
					clan.Name?.ToString() ?? "",
					clan.InformalName?.ToString() ?? "",
					clan.Leader?.StringId ?? "",
					clan.Leader?.Name?.ToString() ?? ""))
				{
					return true;
				}
			}
		}
		catch
		{
		}
		try
		{
			return GetKingdomSettlements(kingdom).Any(settlement =>
				settlement != null
				&& PolicyTextMentions(policyText, settlement.StringId ?? "", settlement.Name?.ToString() ?? ""));
		}
		catch
		{
			return false;
		}
	}

	private static void MergeSemanticPolicyTargetHandles(
		PolicyDraftRequest request,
		IReadOnlyList<PolicyTargetSemanticProposal> proposals)
	{
		if (request == null || proposals == null || proposals.Count <= 0)
		{
			return;
		}
		List<PolicyTargetHandleSaveData> handles = NormalizePolicyTargetHandles(request.TargetHandles);
		foreach (PolicyTargetSemanticProposal proposal in proposals)
		{
			if (proposal == null
				|| !PolicyTargetSemanticRouter.IsSemanticTargetAllowed(
					request.SemanticTargetSnapshot,
					request.ScopeKind,
					request.PlayerKingdomId,
					request.IssuerKingdomId,
					request.SelectedFiefIds,
					proposal.HandleKind,
					proposal.EntityId,
					proposal.OwnerKingdomId))
			{
				continue;
			}
			bool duplicate = handles.Any(handle =>
				string.Equals(handle.Kind, proposal.HandleKind, StringComparison.OrdinalIgnoreCase)
				&& string.Equals(handle.EntityId, proposal.EntityId, StringComparison.OrdinalIgnoreCase));
			if (duplicate)
			{
				continue;
			}
			string prefix;
			if (string.Equals(proposal.HandleKind, PolicyTargetKindKingdom, StringComparison.OrdinalIgnoreCase)) prefix = "K";
			else if (string.Equals(proposal.HandleKind, PolicyTargetKindClan, StringComparison.OrdinalIgnoreCase)) prefix = "C";
			else if (string.Equals(proposal.HandleKind, PolicyTargetKindRuler, StringComparison.OrdinalIgnoreCase)) prefix = "R";
			else if (string.Equals(proposal.HandleKind, PolicyTargetKindSettlement, StringComparison.OrdinalIgnoreCase)) prefix = "L";
			else continue;
			string displayName = proposal.DisplayName;
			string kingdomName = "";
			if (request.SemanticTargetSnapshot?.Kingdoms != null
				&& request.SemanticTargetSnapshot.Kingdoms.TryGetValue(proposal.OwnerKingdomId ?? "", out PolicyTargetKingdomSnapshot kingdom))
			{
				kingdomName = kingdom.DisplayName ?? "";
				if (string.Equals(proposal.HandleKind, PolicyTargetKindKingdom, StringComparison.OrdinalIgnoreCase))
				{
					displayName = kingdomName;
				}
			}
			handles.Add(new PolicyTargetHandleSaveData
			{
				Key = GetNextPolicyTargetHandleKey(handles, prefix),
				Kind = proposal.HandleKind,
				EntityId = proposal.EntityId,
				DisplayName = displayName,
				KingdomId = proposal.OwnerKingdomId,
				KingdomName = kingdomName,
				CurrentSettlementCount = Math.Max(0, proposal.CurrentSettlementCount),
				IsSemanticTarget = true,
				SemanticEvidence = proposal.Evidence ?? ""
			});
		}
		request.TargetHandles = NormalizePolicyTargetHandles(handles);
	}

	private static void MergePolicyTargetSelectorHandles(
		PolicyDraftRequest request,
		IReadOnlyList<PolicyTargetSelectorCandidate> candidates)
	{
		if (request == null || candidates == null || candidates.Count <= 0)
		{
			return;
		}
		List<PolicyTargetHandleSaveData> handles = NormalizePolicyTargetHandles(request.TargetHandles);
		foreach (PolicyTargetSelectorCandidate candidate in candidates)
		{
			PolicyTargetSelectorDescriptor descriptor = candidate?.Descriptor;
			if (descriptor == null
				|| !descriptor.SupportedScopes.Contains(request.ScopeKind ?? "", StringComparer.OrdinalIgnoreCase)
				|| handles.Any(handle => string.Equals(handle.SelectorId, descriptor.Id, StringComparison.Ordinal)))
			{
				continue;
			}
			handles.Add(new PolicyTargetHandleSaveData
			{
				Key = GetNextPolicyTargetHandleKey(handles, "X"),
				Kind = PolicyTargetKindSelector,
				SelectorId = descriptor.Id,
				EntityId = "",
				DisplayName = descriptor.DisplayName,
				KingdomId = request.PlayerKingdomId ?? "",
				KingdomName = request.PlayerKingdomName ?? "",
				IsSemanticTarget = true,
				SemanticEvidence = "selector:" + descriptor.Id
					+ " mode=embedding score=" + candidate.RecallScore.ToString("0.000", CultureInfo.InvariantCulture)
			});
		}
		request.TargetHandles = NormalizePolicyTargetHandles(handles);
	}

	private static void MergeDeterministicPolicyHeroTargetHandles(
		PolicyDraftRequest request,
		IReadOnlyList<PolicyHeroTargetCandidate> candidates)
	{
		if (request == null || candidates == null || candidates.Count == 0)
		{
			return;
		}
		List<PolicyTargetHandleSaveData> handles = NormalizePolicyTargetHandles(request.TargetHandles);
		foreach (PolicyHeroTargetCandidate candidate in candidates)
		{
			if (candidate == null
				|| string.IsNullOrWhiteSpace(candidate.SelectorId)
				|| handles.Any(handle => string.Equals(handle.SelectorId, candidate.SelectorId, StringComparison.Ordinal)))
			{
				continue;
			}
			handles.Add(new PolicyTargetHandleSaveData
			{
				Key = GetNextPolicyTargetHandleKey(handles, "H"),
				Kind = PolicyTargetKindHero,
				SelectorId = candidate.SelectorId,
				EntityId = candidate.CurrentHeroIds.Count == 1 ? candidate.CurrentHeroIds[0] : string.Empty,
				DisplayName = candidate.DisplayName,
				KingdomId = candidate.AnchorKingdomId,
				KingdomName = request.PlayerKingdomName ?? string.Empty,
				IsSemanticTarget = false,
				SemanticEvidence = "deterministic-neutral-group"
			});
		}
		request.TargetHandles = NormalizePolicyTargetHandles(handles);
	}

	private static int MergePolicyTargetPlanHandles(
		PolicyDraftRequest request,
		IReadOnlyList<PolicyTargetPlanCandidate> candidates)
	{
		if (request == null || candidates == null || candidates.Count <= 0)
		{
			return 0;
		}
		List<PolicyTargetHandleSaveData> handles = NormalizePolicyTargetHandles(request.TargetHandles);
		int addedCount = 0;
		foreach (PolicyTargetPlanCandidate candidate in candidates)
		{
			if (candidate?.Plan == null
				|| !PolicyTargetPlanResolver.TryNormalizeAndValidate(candidate.Plan, out PolicyTargetPlanSaveData plan, out _)
				|| handles.Any(handle => string.Equals(
					handle?.TargetPlan?.NormalizedSignature,
					plan.NormalizedSignature,
					StringComparison.Ordinal)))
			{
				continue;
			}
			if (!TryResolvePolicyTargetPlanForRequest(request, plan, out PolicyTargetPlanResolution resolution, out string resolveError))
			{
				PolicySystemLog.Failure("Player", "target-plan-resolve-failed", resolveError,
					"signature=" + plan.NormalizedSignature);
				continue;
			}
			handles.Add(new PolicyTargetHandleSaveData
			{
				Key = GetNextPolicyTargetHandleKey(handles, "P"),
				Kind = PolicyTargetKindPlan,
				TargetPlan = plan,
				DisplayName = candidate.DisplayName,
				KingdomId = request.PlayerKingdomId ?? string.Empty,
				KingdomName = request.PlayerKingdomName ?? string.Empty,
				CurrentSettlementCount = PolicyTargetPlanResolver.ExpandPrimarySettlementIds(
					resolution,
					request.SemanticTargetSnapshot).Count,
				IsSemanticTarget = false,
				SemanticEvidence = candidate.Evidence + " mode=deterministic"
			});
			addedCount++;
		}
		request.TargetHandles = NormalizePolicyTargetHandles(handles);
		return addedCount;
	}

	private static bool TryResolvePolicyTargetPlanForRequest(
		PolicyDraftRequest request,
		PolicyTargetPlanSaveData plan,
		out PolicyTargetPlanResolution resolution,
		out string error)
	{
		resolution = null;
		error = string.Empty;
		if (request?.SemanticTargetSnapshot?.Entities == null || plan == null)
		{
			error = "TargetPlan 缺少政策请求世界快照。";
			return false;
		}
		PlayerPolicyTargetAuthorization authorization = EnsurePlayerPolicyTargetAuthorization(request);
		if (!PolicyTargetPlanResolver.TryNormalizeAndValidate(plan, out PolicyTargetPlanSaveData normalizedPlan, out error)
			|| !authorization.PlanSignatures.Contains(normalizedPlan.NormalizedSignature))
		{
			error = string.IsNullOrWhiteSpace(error)
				? "TargetPlan 不属于本次政策原文的确定性授权集合。"
				: error;
			return false;
		}
		return PolicyTargetPlanResolver.TryResolve(
			normalizedPlan,
			new PolicyTargetPlanResolutionContext
			{
				Scope = request.ScopeKind ?? string.Empty,
				TargetKingdomId = request.PlayerKingdomId ?? string.Empty,
				IssuerKingdomId = request.IssuerKingdomId ?? string.Empty,
				PlayerClanId = TryGetPlayerClanId(),
				ProposerClanId = request.ProposerClanId ?? string.Empty,
				SourceSettlementIds = request.SelectedFiefIds ?? new List<string>(),
				AllowedEntityReferenceIds = authorization.AllowedEntityReferenceIds,
				AllowedKingdomReferenceIds = authorization.AllowedKingdomReferenceIds,
				Snapshot = request.SemanticTargetSnapshot
			},
			out resolution,
			out error);
	}

	private static string GetNextPolicyTargetHandleKey(IEnumerable<PolicyTargetHandleSaveData> handles, string prefix)
	{
		HashSet<string> keys = new HashSet<string>((handles ?? Enumerable.Empty<PolicyTargetHandleSaveData>())
			.Select(handle => handle?.Key ?? ""), StringComparer.OrdinalIgnoreCase);
		for (int index = 0; index < 1000; index++)
		{
			string key = prefix + index.ToString(CultureInfo.InvariantCulture);
			if (!keys.Contains(key))
			{
				return key;
			}
		}
		throw new InvalidOperationException("政策目标句柄数量超过安全上限。");
	}

	private static PolicyTargetHandleSaveData ClonePolicyTargetHandle(PolicyTargetHandleSaveData handle)
	{
		return handle == null
			? null
			: new PolicyTargetHandleSaveData
			{
				Key = (handle.Key ?? "").Trim(),
				Kind = (handle.Kind ?? "").Trim(),
				EntityId = (handle.EntityId ?? "").Trim(),
				SelectorId = (handle.SelectorId ?? "").Trim(),
				TargetPlan = PolicyTargetPlanResolver.Clone(handle.TargetPlan),
				DisplayName = CleanPolicyDisplayText(handle.DisplayName ?? ""),
				KingdomId = (handle.KingdomId ?? "").Trim(),
				KingdomName = CleanPolicyDisplayText(handle.KingdomName ?? ""),
				FollowCurrentRulingClan = handle.FollowCurrentRulingClan,
				CurrentSettlementCount = Math.Max(0, handle.CurrentSettlementCount),
				IsSemanticTarget = handle.IsSemanticTarget,
				SemanticEvidence = CleanPolicyDisplayText(handle.SemanticEvidence ?? "")
			};
	}

	private static List<PolicyTargetHandleSaveData> NormalizePolicyTargetHandles(IEnumerable<PolicyTargetHandleSaveData> handles)
	{
		List<PolicyTargetHandleSaveData> result = new List<PolicyTargetHandleSaveData>();
		HashSet<string> keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (PolicyTargetHandleSaveData handle in handles ?? Enumerable.Empty<PolicyTargetHandleSaveData>())
		{
			PolicyTargetHandleSaveData copy = ClonePolicyTargetHandle(handle);
			if (copy == null
				|| string.IsNullOrWhiteSpace(copy.Key)
				|| (string.Equals(copy.Kind, PolicyTargetKindPlan, StringComparison.OrdinalIgnoreCase) && copy.TargetPlan == null)
				|| !keys.Add(copy.Key))
			{
				continue;
			}
			result.Add(copy);
		}
		return result;
	}

	private List<Settlement> ResolveLocalPolicyHandleSettlements(
		PolicyTargetHandleSaveData handle,
		IEnumerable<Settlement> sourceFiefs,
		PolicyDraftRequest request = null)
	{
		if (handle == null)
		{
			return new List<Settlement>();
		}
		if (string.Equals(handle.Kind, PolicyTargetKindSource, StringComparison.OrdinalIgnoreCase))
		{
			return ExpandLocalPolicySettlements(sourceFiefs);
		}
		if (string.Equals(handle.Kind, PolicyTargetKindPlan, StringComparison.OrdinalIgnoreCase)
			&& request != null
			&& TryResolvePolicyTargetPlanForRequest(request, handle.TargetPlan, out PolicyTargetPlanResolution planResolution, out _))
		{
			List<Settlement> planSettlements = ResolvePolicyEffectSettlementsById(planResolution.PrimarySettlementIds)
				.Select(ResolvePrimaryPolicyFief)
				.Where(primary => primary != null)
				.ToList();
			foreach (string clanId in planResolution.ClanIds)
			{
				Clan clan = ResolveClanById(clanId);
				planSettlements.AddRange((clan?.Settlements ?? Enumerable.Empty<Settlement>())
					.Select(ResolvePrimaryPolicyFief)
					.Where(primary => primary != null));
			}
			foreach (string kingdomId in planResolution.KingdomIds)
			{
				planSettlements.AddRange(GetKingdomSettlements(ResolveKingdomByIdOrName(kingdomId, string.Empty))
					.Select(ResolvePrimaryPolicyFief)
					.Where(primary => primary != null));
			}
			return planSettlements
				.Where(primary => primary.IsTown || primary.IsCastle)
				.GroupBy(primary => primary.StringId ?? string.Empty, StringComparer.OrdinalIgnoreCase)
				.Select(group => group.First())
				.ToList();
		}
		return ResolveLocalMentionedPolicySettlements(
			string.Equals(handle.Kind, PolicyTargetKindClan, StringComparison.OrdinalIgnoreCase)
				|| (string.Equals(handle.Kind, PolicyTargetKindRuler, StringComparison.OrdinalIgnoreCase) && !handle.FollowCurrentRulingClan)
				? new[] { handle.EntityId }
				: Array.Empty<string>(),
			string.Equals(handle.Kind, PolicyTargetKindSettlement, StringComparison.OrdinalIgnoreCase)
				? new[] { handle.EntityId }
				: Array.Empty<string>(),
			string.Equals(handle.Kind, PolicyTargetKindRuler, StringComparison.OrdinalIgnoreCase) && handle.FollowCurrentRulingClan,
			sourceFiefs);
	}

	private static string BuildLocalPolicyMentionSummary(LocalPolicyMentionTargetSelection selection)
	{
		if (selection == null || !selection.HasSelectors)
		{
			return "合法具体实体目标：无（集合 TargetPlan/legacy selector 以本次合法目标句柄目录为准）。";
		}
		List<string> parts = new List<string>();
		if (selection.ClanIds.Count > 0)
		{
			parts.Add("家族" + selection.ClanIds.Count.ToString(CultureInfo.InvariantCulture));
		}
		if (selection.SettlementIds.Count > 0)
		{
			parts.Add("城镇/城堡" + selection.SettlementIds.Count.ToString(CultureInfo.InvariantCulture));
		}
		if (selection.FollowCurrentRulingClan)
		{
			parts.Add("当前统治家族");
		}
		return LimitDisplayChars(
			"合法具体实体目标：" + string.Join("、", parts) + "；当前排除发布地后覆盖" + selection.CurrentSettlementCount.ToString(CultureInfo.InvariantCulture) + "处城镇/城堡。",
			LocalPolicyMentionSummaryMaxChars);
	}

	private static bool HasLocalPolicyMentionSelectors(PolicyDraftRequest request)
	{
		return request != null
			&& (NormalizeIdList(request.LocalMentionedClanIds).Count > 0
				|| NormalizeIdList(request.LocalMentionedSettlementIds).Count > 0
				|| request.LocalMentionedCurrentRulingClan);
	}

	private List<Settlement> ResolveLocalMentionedPolicySettlements(
		IEnumerable<string> clanIds,
		IEnumerable<string> directSettlementIds,
		bool followCurrentRulingClan,
		IEnumerable<Settlement> sourceFiefs)
	{
		Kingdom playerKingdom = GetPlayerKingdom();
		if (playerKingdom == null)
		{
			return new List<Settlement>();
		}
		HashSet<string> sourceSettlementIds = new HashSet<string>(
			ExpandLocalPolicySettlements(sourceFiefs).Select(x => x?.StringId ?? "").Where(x => !string.IsNullOrWhiteSpace(x)),
			StringComparer.OrdinalIgnoreCase);
		HashSet<string> targetClanIds = new HashSet<string>(NormalizeIdList(clanIds), StringComparer.OrdinalIgnoreCase);
		if (followCurrentRulingClan && !string.IsNullOrWhiteSpace(playerKingdom.RulingClan?.StringId))
		{
			targetClanIds.Add(playerKingdom.RulingClan.StringId);
		}
		HashSet<string> directIds = new HashSet<string>(NormalizeIdList(directSettlementIds), StringComparer.OrdinalIgnoreCase);
		List<Settlement> result = new List<Settlement>();
		foreach (string clanId in targetClanIds)
		{
			Clan clan = ResolveClanById(clanId);
			if (clan == null || clan.Kingdom != playerKingdom)
			{
				continue;
			}
			foreach (Settlement settlement in ExpandLocalPolicySettlements(clan.Settlements ?? Enumerable.Empty<Settlement>()))
			{
				string settlementId = (settlement?.StringId ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(settlementId)
					&& !sourceSettlementIds.Contains(settlementId)
					&& IsSettlementInCurrentPlayerKingdom(settlement))
				{
					result.Add(settlement);
				}
			}
		}
		foreach (string settlementId in directIds)
		{
			Settlement primaryFief = ResolvePrimaryPolicyFief(ResolveSettlementById(settlementId));
			if (primaryFief == null || !IsSettlementInCurrentPlayerKingdom(primaryFief))
			{
				continue;
			}
			foreach (Settlement settlement in ExpandLocalPolicySettlements(new[] { primaryFief }))
			{
				string expandedId = (settlement?.StringId ?? "").Trim();
				if (!string.IsNullOrWhiteSpace(expandedId)
					&& !sourceSettlementIds.Contains(expandedId)
					&& IsSettlementInCurrentPlayerKingdom(settlement))
				{
					result.Add(settlement);
				}
			}
		}
		return result
			.Where(x => x != null)
			.GroupBy(x => x.StringId ?? "", StringComparer.OrdinalIgnoreCase)
			.Select(x => x.First())
			.ToList();
	}

	private static List<string> NormalizeIdList(IEnumerable<string> ids)
	{
		return (ids ?? Enumerable.Empty<string>()).Select(x => (x ?? "").Trim()).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
	}

	private static LocalPolicyFiefData BuildLocalPolicyFiefUiData(Settlement fief)
	{
		return new LocalPolicyFiefData
		{
			FiefId = fief?.StringId ?? "",
			NameText = fief?.Name?.ToString() ?? fief?.StringId ?? "未知封地",
			TypeText = GetLocalPolicyFiefTypeText(fief)
		};
	}

	private static bool IsSettlementInActiveLocalPolicyScope(ActivePolicyEffectSaveData effect, Settlement settlement)
	{
		if (!IsLocalActivePolicyEffect(effect) || settlement == null)
		{
			return false;
		}
		if (IsMentionedLocalPolicyEffect(effect) && !IsSettlementInCurrentPlayerKingdom(settlement))
		{
			return false;
		}
		if (!IsMentionedLocalPolicyEffect(effect) && !IsSettlementOwnedByPlayerClan(settlement))
		{
			return false;
		}
		return effect.ContainsTargetSettlementId((settlement.StringId ?? "").Trim());
	}

	private static bool IsSettlementOwnedByPlayerClan(Settlement settlement)
	{
		Clan playerClan = Clan.PlayerClan;
		return settlement != null
			&& playerClan != null
			&& (settlement.OwnerClan == playerClan || settlement.Village?.Bound?.OwnerClan == playerClan);
	}

	private static bool IsSettlementInCurrentPlayerKingdom(Settlement settlement)
	{
		Kingdom playerKingdom = GetPlayerKingdom();
		if (settlement == null || playerKingdom == null)
		{
			return false;
		}
		Kingdom settlementKingdom = settlement.OwnerClan?.Kingdom
			?? settlement.Village?.Bound?.OwnerClan?.Kingdom
			?? settlement.MapFaction as Kingdom;
		return settlementKingdom != null
			&& (ReferenceEquals(settlementKingdom, playerKingdom)
				|| string.Equals(settlementKingdom.StringId ?? "", playerKingdom.StringId ?? "", StringComparison.OrdinalIgnoreCase));
	}

	private static bool IsSettlementEligibleForCachedLocalPolicyEffect(Settlement settlement)
	{
		return IsSettlementOwnedByPlayerClan(settlement) || IsSettlementInCurrentPlayerKingdom(settlement);
	}

	private static List<Settlement> GetKingdomSettlements(Kingdom kingdom)
	{
		if (kingdom == null)
		{
			return new List<Settlement>();
		}
		try
		{
			return Settlement.All.Where(s => s != null && s.MapFaction == kingdom && (s.Town != null || s.Village != null)).ToList();
		}
		catch
		{
			return new List<Settlement>();
		}
	}

	private Settlement ResolveSettlementById(string settlementId)
	{
		string id = (settlementId ?? "").Trim();
		if (string.IsNullOrWhiteSpace(id))
		{
			return null;
		}
		try
		{
			Campaign campaign = Campaign.Current;
			if (!ReferenceEquals(_settlementByIdRuntimeCacheCampaign, campaign))
			{
				_settlementByIdRuntimeCache.Clear();
				_settlementByIdRuntimeCacheCampaign = campaign;
				foreach (Settlement settlement in Settlement.All)
				{
					if (!string.IsNullOrWhiteSpace(settlement?.StringId))
					{
						_settlementByIdRuntimeCache[settlement.StringId] = settlement;
					}
				}
			}
			if (_settlementByIdRuntimeCache.TryGetValue(id, out Settlement cachedSettlement))
			{
				return cachedSettlement;
			}
			Settlement resolvedSettlement = Settlement.All.FirstOrDefault(x => x != null && string.Equals(x.StringId, id, StringComparison.OrdinalIgnoreCase));
			_settlementByIdRuntimeCache[id] = resolvedSettlement;
			return resolvedSettlement;
		}
		catch
		{
			return null;
		}
	}
}

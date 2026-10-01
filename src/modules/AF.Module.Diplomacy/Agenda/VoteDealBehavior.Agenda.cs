using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge
{
	public partial class VoteDealBehavior
	{
		private sealed class UnifiedAgendaOption
		{
			public string Code;
			public string ExistingOutcomeKey;
			public string ExistingOutcomeBasicKey;
			public string Title;
			public string Description;
			public string SponsorName;
			public bool CreatesProposal;
		}

		private sealed class UnifiedAgendaEntry
		{
			public string Code;
			public string ExistingDecisionKey;
			public string ExistingDecisionBasicKey;
			public string Title;
			public string TypeLabel;
			public string ProposalType;
			public string TargetId;
			public string Direction;
			public string ProposerName;
			public float RemainingDays;
			public bool IsOwnProposal;
			public List<UnifiedAgendaOption> Options = new List<UnifiedAgendaOption>();
		}

		private sealed class UnifiedAgendaSnapshot
		{
			public DateTime CreatedUtc;
			public string HeroId;
			public List<UnifiedAgendaEntry> Entries = new List<UnifiedAgendaEntry>();
		}

		private static readonly object UnifiedAgendaSnapshotLock = new object();
		private static readonly Dictionary<string, UnifiedAgendaSnapshot> UnifiedAgendaSnapshots = new Dictionary<string, UnifiedAgendaSnapshot>(StringComparer.OrdinalIgnoreCase);
		private static readonly Regex UnifiedAgendaTagRx = new Regex(@"\[ACTION:AGENDA:(A\d+):(O\d+):(SLIGHTLY_FAVOR|STRONGLY_FAVOR|FULLY_PUSH)\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

		public static List<PostprocessRuleEntry> BuildAgendaVotePostprocessRulesForExternal()
		{
			List<PostprocessRuleEntry> result = new List<PostprocessRuleEntry>();
			foreach (PostprocessRuleEntry rule in AIConfigHandler.GetGuardrailRulePostprocessRules("kingdom_agenda") ?? new List<PostprocessRuleEntry>())
			{
				string tag = (rule?.Tag ?? "").Trim();
				if (string.IsNullOrWhiteSpace(tag)
					|| string.Equals(tag, KingdomAgendaCustomPolicyBehavior.ActionTag, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}
				result.Add(new PostprocessRuleEntry
				{
					Tag = tag,
					Description = rule.Description ?? ""
				});
			}
			return result;
		}

		public static WorldEntityPromptContext BuildUnifiedAgendaPromptContextForExternal(Hero npc, MentionedWorldEntities mentions)
		{
			WorldEntityPromptContext result = new WorldEntityPromptContext();
			try
			{
				Clan clan = npc?.Clan;
				Kingdom kingdom = clan?.Kingdom;
				if (npc == null || clan == null || kingdom == null || clan.IsUnderMercenaryService || npc != clan.Leader)
				{
					return result;
				}

				UnifiedAgendaSnapshot snapshot = new UnifiedAgendaSnapshot
				{
					CreatedUtc = DateTime.UtcNow,
					HeroId = npc.StringId ?? ""
				};
				int candidateLimit = PromptListRetrievalService.GetMaxCandidateCount();
				AppendExistingAgendaEntries(npc, snapshot);
				TrimUnifiedAgendaEntries(snapshot, candidateLimit);

				List<UnifiedAgendaEntry>[] explicitGroups = BuildExplicitPotentialAgendaGroups(clan, kingdom, mentions);
				AppendExplicitAgendaGroupsRoundRobin(snapshot, explicitGroups, candidateLimit);
				AppendFallbackPolicyEntries(clan, kingdom, snapshot, candidateLimit);
				RenumberUnifiedAgendaEntries(snapshot);
				PublishUnifiedAgendaSnapshot(snapshot);

				List<PolicyObject> policies = ResolveSnapshotPolicies(snapshot);
				result.MainPromptBlock = BuildUnifiedAgendaMainPromptBlock(snapshot, clan, kingdom, policies);
				result.PostprocessPromptBlock = BuildUnifiedAgendaPostprocessBlock(snapshot);
				result.MatchCount = snapshot.Entries.Count;
				return result;
			}
			catch (Exception ex)
			{
				Logger.Log("Agenda", "build unified agenda context failed: " + ex.Message);
				return result;
			}
		}

		private static List<PolicyObject> SelectMentionedPolicies(MentionedWorldEntities mentions, bool fillWithFallback)
		{
			return PromptListRetrievalService.SelectCandidates(PolicyObject.All ?? Enumerable.Empty<PolicyObject>(), mentions,
				p => new[] { p?.Name?.ToString() ?? "", p?.StringId ?? "" }, PromptListRetrievalService.GetMaxCandidateCount(), fillWithFallback);
		}

		private static List<UnifiedAgendaEntry>[] BuildExplicitPotentialAgendaGroups(Clan clan, Kingdom kingdom, MentionedWorldEntities mentions)
		{
			UnifiedAgendaSnapshot policies = new UnifiedAgendaSnapshot();
			UnifiedAgendaSnapshot kingdoms = new UnifiedAgendaSnapshot();
			UnifiedAgendaSnapshot clans = new UnifiedAgendaSnapshot();
			UnifiedAgendaSnapshot fiefs = new UnifiedAgendaSnapshot();
			AppendPotentialPolicyEntries(clan, kingdom, SelectMentionedPolicies(mentions, false), policies);
			AppendPotentialKingdomEntries(clan, kingdom, mentions, kingdoms);
			AppendPotentialClanEntries(clan, kingdom, mentions, clans);
			AppendPotentialFiefEntries(clan, kingdom, mentions, fiefs);
			return new[] { policies.Entries, kingdoms.Entries, clans.Entries, fiefs.Entries };
		}

		private static void AppendExplicitAgendaGroupsRoundRobin(UnifiedAgendaSnapshot snapshot, IList<UnifiedAgendaEntry>[] groups, int limit)
		{
			if (snapshot == null || groups == null) return;
			int[] offsets = new int[groups.Length];
			bool progressed = true;
			while (snapshot.Entries.Count < limit && progressed)
			{
				progressed = false;
				for (int groupIndex = 0; groupIndex < groups.Length && snapshot.Entries.Count < limit; groupIndex++)
				{
					IList<UnifiedAgendaEntry> group = groups[groupIndex] ?? new List<UnifiedAgendaEntry>();
					while (offsets[groupIndex] < group.Count)
					{
						UnifiedAgendaEntry entry = group[offsets[groupIndex]++];
						if (!CanAppendUnifiedAgendaEntry(snapshot, entry)) continue;
						snapshot.Entries.Add(entry);
						progressed = true;
						break;
					}
				}
			}
			TrimUnifiedAgendaEntries(snapshot, limit);
		}

		private static bool CanAppendUnifiedAgendaEntry(UnifiedAgendaSnapshot snapshot, UnifiedAgendaEntry entry)
		{
			if (snapshot == null || entry == null) return false;
			return !snapshot.Entries.Any(existing =>
				(!string.IsNullOrWhiteSpace(existing.ExistingDecisionBasicKey) && string.Equals(existing.Title, entry.Title, StringComparison.OrdinalIgnoreCase)) ||
				(string.Equals(existing.ProposalType, entry.ProposalType, StringComparison.OrdinalIgnoreCase) &&
				 string.Equals(existing.TargetId, entry.TargetId, StringComparison.OrdinalIgnoreCase) &&
				 string.Equals(existing.Direction, entry.Direction, StringComparison.OrdinalIgnoreCase)));
		}

		private static void AppendFallbackPolicyEntries(Clan clan, Kingdom kingdom, UnifiedAgendaSnapshot snapshot, int limit)
		{
			if (snapshot == null || snapshot.Entries.Count >= limit) return;
			HashSet<string> includedPolicyIds = new HashSet<string>(snapshot.Entries
				.Where(e => string.Equals(e.ProposalType, "POLICY", StringComparison.OrdinalIgnoreCase))
				.Select(e => e.TargetId ?? ""), StringComparer.OrdinalIgnoreCase);
			foreach (PolicyObject policy in PolicyObject.All ?? Enumerable.Empty<PolicyObject>())
			{
				if (snapshot.Entries.Count >= limit) break;
				if (policy == null || includedPolicyIds.Contains(policy.StringId ?? "")) continue;
				int before = snapshot.Entries.Count;
				AppendPotentialPolicyEntries(clan, kingdom, new[] { policy }, snapshot);
				if (snapshot.Entries.Count > before) includedPolicyIds.Add(policy.StringId ?? "");
			}
			TrimUnifiedAgendaEntries(snapshot, limit);
		}

		private static List<PolicyObject> ResolveSnapshotPolicies(UnifiedAgendaSnapshot snapshot)
		{
			Dictionary<string, PolicyObject> policiesById = (PolicyObject.All ?? Enumerable.Empty<PolicyObject>())
				.Where(p => p != null && !string.IsNullOrWhiteSpace(p.StringId))
				.GroupBy(p => p.StringId, StringComparer.OrdinalIgnoreCase)
				.ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
			List<PolicyObject> result = new List<PolicyObject>();
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (UnifiedAgendaEntry entry in snapshot?.Entries ?? new List<UnifiedAgendaEntry>())
			{
				if (!string.Equals(entry.ProposalType, "POLICY", StringComparison.OrdinalIgnoreCase) || !seen.Add(entry.TargetId ?? "")) continue;
				if (policiesById.TryGetValue(entry.TargetId ?? "", out PolicyObject policy)) result.Add(policy);
			}
			return result;
		}

		private static void AppendExistingAgendaEntries(Hero npc, UnifiedAgendaSnapshot snapshot)
		{
			foreach (VoteDealAgendaEntry agenda in BuildVoteDealAgendaEntries(npc))
			{
				if (IsWorldDiplomacyControlledAgendaDecision(agenda.Decision)) continue;
				UnifiedAgendaEntry entry = new UnifiedAgendaEntry
				{
					ExistingDecisionKey = agenda.DecisionKey,
					ExistingDecisionBasicKey = agenda.DecisionBasicKey,
					Title = agenda.Title,
					TypeLabel = agenda.TypeLabel,
					ProposerName = agenda.ProposerName,
					RemainingDays = agenda.RemainingDays,
					IsOwnProposal = agenda.Decision?.ProposerClan?.StringId == npc.Clan?.StringId
				};
				if (agenda.Decision is KingdomPolicyDecision policyDecision && policyDecision.Policy != null)
				{
					entry.ProposalType = "POLICY";
					entry.TargetId = policyDecision.Policy.StringId ?? "";
					entry.Direction = GetPolicyDirectionForAgenda(policyDecision);
				}
				foreach (VoteDealOptionEntry option in agenda.Options)
				{
					entry.Options.Add(new UnifiedAgendaOption
					{
						ExistingOutcomeKey = option.OutcomeKey,
						ExistingOutcomeBasicKey = option.OutcomeBasicKey,
						Title = option.Title,
						Description = option.Description,
						SponsorName = option.SponsorName
					});
				}
				snapshot.Entries.Add(entry);
			}
		}

		private static void AppendPotentialPolicyEntries(Clan clan, Kingdom kingdom, IEnumerable<PolicyObject> policies, UnifiedAgendaSnapshot snapshot)
		{
			foreach (PolicyObject policy in policies ?? Enumerable.Empty<PolicyObject>())
			{
				if (policy == null || HasPendingPolicyDecision(kingdom, policy)) continue;
				PolicyObject canonicalPolicy = ResolvePolicyForKingdomAgenda(kingdom, policy, out bool active);
				if (canonicalPolicy == null) continue;
				KingdomPolicyDecision decision = new KingdomPolicyDecision(clan, canonicalPolicy, active);
				AppendPotentialDecision(snapshot, decision, "POLICY", canonicalPolicy.StringId, active ? "ABOLISH" : "ADOPT", active ? "废除政策" : "采纳政策");
			}
		}

		private static void AppendPotentialKingdomEntries(Clan clan, Kingdom kingdom, MentionedWorldEntities mentions, UnifiedAgendaSnapshot snapshot)
		{
			// When world diplomacy takeover is enabled, war/peace/alliance/trade are
			// immediate sovereign diplomacy actions, not kingdom-agenda proposals.
			if (IsWorldDiplomacyTakeoverEnabled()) return;
			foreach (Kingdom target in PromptListRetrievalService.SelectCandidates(Kingdom.All, mentions, k => new[] { k?.Name?.ToString() ?? "", k?.InformalName?.ToString() ?? "", k?.StringId ?? "" }, PromptListRetrievalService.GetMaxCandidateCount(), false))
			{
				if (target == null || target == kingdom || target.IsEliminated) continue;
				IAllianceCampaignBehavior allianceBehavior = Campaign.Current?.GetCampaignBehavior<IAllianceCampaignBehavior>();
				ITradeAgreementsCampaignBehavior tradeBehavior = Campaign.Current?.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
				bool alreadyAllied = allianceBehavior?.IsAllyWithKingdom(kingdom, target) == true;
				bool alreadyTrading = BannerlordApiCompat.HasTradeAgreement(tradeBehavior, kingdom, target);
				if (FactionManager.IsAtWarAgainstFaction(kingdom, target))
				{
					AppendPotentialDecision(snapshot, new MakePeaceKingdomDecision(clan, target), "PEACE", target.StringId, "", "议和");
				}
				else
				{
					AppendPotentialDecision(snapshot, new DeclareWarDecision(clan, target), "WAR", target.StringId, "", "宣战");
					if (FactionManager.IsNeutralWithFaction(kingdom, target) && !alreadyAllied)
					{
						AppendPotentialDecision(snapshot, new StartAllianceDecision(clan, target), "ALLIANCE", target.StringId, "", "结盟");
					}
					if (!alreadyTrading) AppendPotentialDecision(snapshot, new TradeAgreementDecision(clan, target), "TRADE", target.StringId, "", "贸易协定");
				}
			}
		}

		private static bool IsWorldDiplomacyTakeoverEnabled()
		{
			try
			{
				return DuelSettings.GetSettings()?.EnableWorldDiplomacy ?? false;
			}
			catch
			{
				return false;
			}
		}

		private static bool IsWorldDiplomacyControlledAgendaDecision(KingdomDecision decision)
		{
			return IsWorldDiplomacyTakeoverEnabled()
				&& (decision is DeclareWarDecision
					|| decision is MakePeaceKingdomDecision
					|| decision is StartAllianceDecision
					|| decision is TradeAgreementDecision);
		}

		private static void AppendPotentialClanEntries(Clan clan, Kingdom kingdom, MentionedWorldEntities mentions, UnifiedAgendaSnapshot snapshot)
		{
			foreach (Clan target in PromptListRetrievalService.SelectCandidates(kingdom.Clans, mentions, c => new[] { c?.Name?.ToString() ?? "", c?.InformalName?.ToString() ?? "", c?.StringId ?? "" }, PromptListRetrievalService.GetMaxCandidateCount(), false))
			{
				if (target == null || target == clan || target == kingdom.RulingClan) continue;
				AppendPotentialDecision(snapshot, new ExpelClanFromKingdomDecision(clan, target), "EXPEL", target.StringId, "", "驱逐家族");
			}
		}

		private static void AppendPotentialFiefEntries(Clan clan, Kingdom kingdom, MentionedWorldEntities mentions, UnifiedAgendaSnapshot snapshot)
		{
			if (!HasMultipleEligibleFiefClaimants(kingdom)) return;
			IEnumerable<Settlement> candidates = Settlement.All.Where(s => s != null && (s.IsTown || s.IsCastle) && s.MapFaction == kingdom);
			foreach (Settlement target in PromptListRetrievalService.SelectCandidates(candidates, mentions, s => new[] { s?.Name?.ToString() ?? "", s?.StringId ?? "" }, PromptListRetrievalService.GetMaxCandidateCount(), false))
			{
				AppendPotentialDecision(snapshot, new SettlementClaimantPreliminaryDecision(clan, target), "FIEF", target.StringId, "", "重新分配封地");
			}
		}

		private static bool HasMultipleEligibleFiefClaimants(Kingdom kingdom)
		{
			if (kingdom?.Clans == null) return false;
			int eligibleCount = 0;
			foreach (Clan candidate in kingdom.Clans)
			{
				if (candidate == null || candidate.IsUnderMercenaryService || candidate.IsEliminated || candidate.Leader == null || candidate.Leader.IsDead) continue;
				if (++eligibleCount > 1) return true;
			}
			return false;
		}

		private static void AppendPotentialDecision(UnifiedAgendaSnapshot snapshot, KingdomDecision decision, string type, string targetId, string direction, string typeLabel)
		{
			try
			{
				if (decision == null || snapshot == null || snapshot.Entries.Any(e => !string.IsNullOrWhiteSpace(e.ExistingDecisionBasicKey) && string.Equals(e.Title, GetDecisionDisplayTitleForAgenda(decision), StringComparison.OrdinalIgnoreCase))) return;
				MBList<DecisionOutcome> initial = new MBList<DecisionOutcome>();
				foreach (DecisionOutcome outcome in decision.DetermineInitialCandidates() ?? Enumerable.Empty<DecisionOutcome>()) if (outcome != null) initial.Add(outcome);
				if (initial.Count == 0) return;
				MBList<DecisionOutcome> narrowed = decision.NarrowDownCandidates(initial, 3);
				if (narrowed == null || narrowed.Count == 0) return;
				decision.DetermineSponsors(narrowed);
				UnifiedAgendaEntry entry = new UnifiedAgendaEntry
				{
					Title = GetDecisionDisplayTitleForAgenda(decision),
					TypeLabel = typeLabel,
					ProposalType = type,
					TargetId = targetId ?? "",
					Direction = direction ?? ""
				};
				for (int i = 0; i < narrowed.Count; i++)
				{
					DecisionOutcome outcome = narrowed[i];
					entry.Options.Add(new UnifiedAgendaOption
					{
						Title = GetDecisionOutcomeDisplayTitleForAgenda(decision, outcome),
						Description = GetDecisionOutcomeDisplayDescriptionForAgenda(decision, outcome),
						CreatesProposal = i == 0
					});
				}
				snapshot.Entries.Add(entry);
			}
			catch (Exception ex)
			{
				Logger.Log("Agenda", "skip potential decision type=" + type + " target=" + targetId + " error=" + ex.Message);
			}
		}

		private static void RenumberUnifiedAgendaEntries(UnifiedAgendaSnapshot snapshot)
		{
			for (int i = 0; i < snapshot.Entries.Count; i++)
			{
				snapshot.Entries[i].Code = "A" + (i + 1);
				for (int j = 0; j < snapshot.Entries[i].Options.Count; j++) snapshot.Entries[i].Options[j].Code = "O" + (j + 1);
			}
		}

		private static void TrimUnifiedAgendaEntries(UnifiedAgendaSnapshot snapshot, int limit)
		{
			if (snapshot == null) return;
			limit = PromptListRetrievalService.ClampCandidateLimit(limit);
			if (snapshot.Entries.Count > limit) snapshot.Entries.RemoveRange(limit, snapshot.Entries.Count - limit);
		}

		private static bool HasPendingPolicyDecision(Kingdom kingdom, PolicyObject policy)
		{
			return kingdom?.UnresolvedDecisions?.OfType<KingdomPolicyDecision>().Any(d => d != null && IsSamePolicyForAgenda(d.Policy, policy) && !d.ShouldBeCancelled()) == true;
		}

		private static string BuildUnifiedAgendaMainPromptBlock(UnifiedAgendaSnapshot snapshot, Clan clan, Kingdom kingdom, IEnumerable<PolicyObject> policies)
		{
			StringBuilder sb = new StringBuilder();
			List<UnifiedAgendaEntry> activeEntries = snapshot?.Entries?.Where(e => !string.IsNullOrWhiteSpace(e.ExistingDecisionKey)).ToList() ?? new List<UnifiedAgendaEntry>();
			if (activeEntries.Count > 0)
			{
				sb.AppendLine("【王国当前活跃议程】以下议程优先占用本轮议程候选栏位；正文只自然谈论议程与选项，不要念系统编号。");
				foreach (UnifiedAgendaEntry entry in activeEntries)
				{
					string timing = entry.RemainingDays > 0f ? ("，剩余" + entry.RemainingDays.ToString("F1") + "天进入投票") : "，即将进入投票";
					string own = entry.IsOwnProposal ? "【你的家族提案，不可建立投票交易】" : "";
					sb.Append("- [").Append(entry.TypeLabel).Append("] ").Append(entry.Title)
						.Append("（提案人:").Append(string.IsNullOrWhiteSpace(entry.ProposerName) ? "未知" : entry.ProposerName).Append(timing).Append("）").AppendLine(own);
					foreach (UnifiedAgendaOption option in entry.Options)
					{
						sb.Append("  - 可投选项: ").Append(option.Title);
						if (!string.IsNullOrWhiteSpace(option.SponsorName) && option.SponsorName != "未知") sb.Append("，赞助/候选:").Append(option.SponsorName);
						if (!string.IsNullOrWhiteSpace(option.Description)) sb.Append("，说明:").Append(option.Description);
						sb.AppendLine();
					}
				}
			}
			List<UnifiedAgendaEntry> potentialPoliticalEntries = snapshot?.Entries?
				.Where(e => string.IsNullOrWhiteSpace(e.ExistingDecisionKey) && !string.Equals(e.ProposalType, "POLICY", StringComparison.OrdinalIgnoreCase))
				.ToList() ?? new List<UnifiedAgendaEntry>();
			if (potentialPoliticalEntries.Count > 0)
			{
				if (sb.Length > 0) sb.AppendLine();
				sb.AppendLine("【相关政治提议】以下是本轮检索到、可由你正式提交的潜在王国议程；结合当前局势自然表态，不要念系统编号。");
				foreach (UnifiedAgendaEntry entry in potentialPoliticalEntries)
				{
					sb.Append("- [").Append(entry.TypeLabel).Append("] ").Append(entry.Title).AppendLine();
					if (entry.Options.Count > 0)
					{
						sb.Append("  可选结果：").Append(string.Join(" / ", entry.Options.Select(o => o.Title))).AppendLine();
					}
				}
			}
			string policyBlock = BuildPolicyMainPromptBlock(clan, kingdom, policies);
			if (!string.IsNullOrWhiteSpace(policyBlock))
			{
				if (sb.Length > 0) sb.AppendLine();
				sb.Append(policyBlock);
			}
			return sb.ToString().TrimEnd();
		}

		private static string BuildPolicyMainPromptBlock(Clan clan, Kingdom kingdom, IEnumerable<PolicyObject> policies)
		{
			List<PolicyObject> list = (policies ?? Enumerable.Empty<PolicyObject>()).Where(p => p != null).ToList();
			if (list.Count == 0) return "";
			StringBuilder sb = new StringBuilder("【相关政策实体】以下态度是你作为家族族长的初始政治倾向，不是不可改变的承诺；自然说话时不要念出ID。\n");
			foreach (PolicyObject policy in list)
			{
				PolicyObject canonicalPolicy = ResolvePolicyForKingdomAgenda(kingdom, policy, out bool active);
				if (canonicalPolicy == null) continue;
				float support = CalculatePolicySupportSafe(clan, canonicalPolicy, active);
				string pending = HasPendingPolicyDecision(kingdom, canonicalPolicy) ? "已有待决议程" : "暂无待决议程";
				string direction = active ? "废除" : "推行";
				sb.Append("- ").Append(canonicalPolicy.Name?.ToString() ?? canonicalPolicy.StringId)
					.Append(" | ").Append(active ? "已生效" : "未生效").Append(" | ").Append(pending)
					.Append(" | 本轮仅可议案=").Append(direction)
					.Append(" | 对").Append(direction).Append("案的初始态度=").Append(FormatPolicyAttitude(support))
					.Append(" | 原因=").Append(BuildPolicyAttitudeReason(clan, canonicalPolicy, active)).AppendLine();
				string introduction = CleanVoteDealText(canonicalPolicy.Description?.ToString() ?? "");
				if (string.IsNullOrWhiteSpace(introduction)) introduction = CleanVoteDealText(canonicalPolicy.LogEntryDescription?.ToString() ?? "");
				if (introduction.Length > 320) introduction = introduction.Substring(0, 320).TrimEnd() + "…";
				if (!string.IsNullOrWhiteSpace(introduction)) sb.Append("  介绍：").Append(introduction).AppendLine();
			}
			return sb.ToString().TrimEnd();
		}

		private static string BuildUnifiedAgendaPostprocessBlock(UnifiedAgendaSnapshot snapshot)
		{
			if (snapshot == null || snapshot.Entries.Count == 0) return "";
			StringBuilder sb = new StringBuilder("【议程后处理候选】只能复制下列A/O编号；现有议程会记录投票承诺，潜在议程选择第一个正向选项时会正式提交。A开头的编号为议程ID，O开头的为选项ID\n");
			foreach (UnifiedAgendaEntry entry in snapshot.Entries)
			{
				sb.Append(entry.Code).Append(" [").Append(entry.TypeLabel).Append("] ").Append(entry.Title).AppendLine();
				foreach (UnifiedAgendaOption option in entry.Options) sb.Append("- ").Append(option.Code).Append(": ").Append(option.Title).AppendLine();
			}
			return sb.ToString().TrimEnd();
		}

		private static float CalculatePolicySupportSafe(Clan clan, PolicyObject policy, bool isInvertedDecision)
		{
			try { return new KingdomPolicyDecision(clan, policy, isInvertedDecision).CalculateSupport(clan); }
			catch { return 0f; }
		}

		private static string FormatPolicyAttitude(float support)
		{
			if (support >= 100f) return "强烈支持";
			if (support >= 35f) return "倾向支持";
			if (support <= -100f) return "强烈反对";
			if (support <= -35f) return "倾向反对";
			return "中立";
		}

		private static string BuildPolicyAttitudeReason(Clan clan, PolicyObject policy, bool isInvertedDecision)
		{
			try
			{
				float e = Math.Abs(policy.EgalitarianWeight), o = Math.Abs(policy.OligarchicWeight), a = Math.Abs(policy.AuthoritarianWeight);
				string axis = e >= o && e >= a ? "平民取向" : (o >= a ? "贵族寡头取向" : "王权取向");
				int trait = axis.StartsWith("平民") ? clan.Leader.GetTraitLevel(DefaultTraits.Egalitarian) : (axis.StartsWith("贵族") ? clan.Leader.GetTraitLevel(DefaultTraits.Oligarchic) : clan.Leader.GetTraitLevel(DefaultTraits.Authoritarian));
				float policyFactor = Math.Max(e, Math.Max(o, a));
				float statusFactor = clan.Kingdom?.RulingClan == clan ? 3f : (clan.IsMinorFaction || clan.Tier >= 5 ? 1.5f : 0.5f);
				float traitFactor = Math.Abs(trait) * 1.25f;
				string directionPrefix = isInvertedDecision ? "本轮为废除案，支持废除表示反对该政策：" : "本轮为推行案，支持推行表示支持该政策：";
				if (statusFactor >= policyFactor && statusFactor >= traitFactor)
				{
					if (clan.Kingdom?.RulingClan == clan) return directionPrefix + "作为统治家族，维护王国权力结构是首要考虑";
					if (clan.IsMinorFaction) return directionPrefix + "小派系的生存与政治空间是首要考虑";
					return directionPrefix + "家族的高阶政治地位是首要考虑";
				}
				if (traitFactor >= policyFactor)
				{
					return directionPrefix + (trait >= 0 ? "族长的" + axis + "政治性格最符合这项政策" : "族长的政治性格最排斥这项政策的" + axis);
				}
				return directionPrefix + "政策本身鲜明的" + axis + "是主要影响";
			}
			catch { return (isInvertedDecision ? "本轮为废除案；" : "本轮为推行案；") + "由政策取向、家族地位和族长政治性格共同决定"; }
		}

		private static void PublishUnifiedAgendaSnapshot(UnifiedAgendaSnapshot snapshot)
		{
			if (snapshot == null || string.IsNullOrWhiteSpace(snapshot.HeroId)) return;
			lock (UnifiedAgendaSnapshotLock)
			{
				UnifiedAgendaSnapshots[snapshot.HeroId] = snapshot;
				foreach (string key in UnifiedAgendaSnapshots.Where(x => DateTime.UtcNow - x.Value.CreatedUtc > TimeSpan.FromMinutes(10)).Select(x => x.Key).ToList()) UnifiedAgendaSnapshots.Remove(key);
			}
		}

		private static bool TryGetUnifiedAgendaSnapshot(Hero npc, out UnifiedAgendaSnapshot snapshot)
		{
			snapshot = null;
			string id = npc?.StringId ?? "";
			lock (UnifiedAgendaSnapshotLock)
			{
				return !string.IsNullOrWhiteSpace(id) && UnifiedAgendaSnapshots.TryGetValue(id, out snapshot) && snapshot != null && DateTime.UtcNow - snapshot.CreatedUtc <= TimeSpan.FromMinutes(10);
			}
		}

		public static void ProcessAgendaTagsDispatch(Hero npc, ref string text)
		{
			if (npc == null || string.IsNullOrWhiteSpace(text) || text.IndexOf("ACTION:AGENDA", StringComparison.OrdinalIgnoreCase) < 0) return;
			VoteDealBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<VoteDealBehavior>();
			bool directDiplomacyPresent = text.IndexOf("[ACTION:DIPLOMACY:", StringComparison.OrdinalIgnoreCase) >= 0;
			text = UnifiedAgendaTagRx.Replace(text, match =>
			{
				if (behavior == null || !TryGetUnifiedAgendaSnapshot(npc, out var snapshot))
				{
					Logger.Log("Agenda", "tag rejected: snapshot missing npc=" + (npc.StringId ?? ""));
					return "";
				}
				UnifiedAgendaEntry entry = snapshot.Entries.FirstOrDefault(e => string.Equals(e.Code, match.Groups[1].Value, StringComparison.OrdinalIgnoreCase));
				UnifiedAgendaOption option = entry?.Options.FirstOrDefault(o => string.Equals(o.Code, match.Groups[2].Value, StringComparison.OrdinalIgnoreCase));
				if (entry == null || option == null) return "";
				if (!string.IsNullOrWhiteSpace(entry.ExistingDecisionKey))
				{
					List<VoteDealAgendaEntry> current = BuildVoteDealAgendaEntries(npc);
					VoteDealAgendaEntry agenda = current.FirstOrDefault(a => string.Equals(a.DecisionKey, entry.ExistingDecisionKey, StringComparison.Ordinal) || string.Equals(a.DecisionBasicKey, entry.ExistingDecisionBasicKey, StringComparison.Ordinal));
					VoteDealOptionEntry selected = agenda?.Options.FirstOrDefault(o => string.Equals(o.OutcomeKey, option.ExistingOutcomeKey, StringComparison.Ordinal) || string.Equals(o.OutcomeBasicKey, option.ExistingOutcomeBasicKey, StringComparison.Ordinal));
					if (agenda != null && selected != null)
					{
						int before = behavior._activeDeals?.Count ?? 0;
						behavior.ProcessTargetedVoteDealTag(npc, agenda.Code, selected.Code, match.Groups[3].Value, "");
						if ((behavior._activeDeals?.Count ?? 0) > before)
						{
							MyBehavior.AppendExternalDialogueHistory(npc, null, null, "[AFEF NPC行为补充] " + (npc.Name?.ToString() ?? "NPC") + "已承诺在议程“" + agenda.Title + "”中选择“" + selected.Title + "”。");
						}
					}
				}
				else if (option.CreatesProposal)
				{
					if (directDiplomacyPresent && (entry.ProposalType == "WAR" || entry.ProposalType == "PEACE" || entry.ProposalType == "ALLIANCE" || entry.ProposalType == "TRADE"))
					{
						Logger.Log("Agenda", "proposal suppressed because direct diplomacy tag is present type=" + entry.ProposalType + " target=" + entry.TargetId);
						return "";
					}
					int before = npc.Clan?.Kingdom?.UnresolvedDecisions?.Count ?? 0;
					ExecutePropose(npc, entry.ProposalType, entry.TargetId, entry.Direction, match.Groups[3].Value);
					if ((npc.Clan?.Kingdom?.UnresolvedDecisions?.Count ?? 0) > before)
					{
						MyBehavior.AppendExternalDialogueHistory(npc, null, null, "[AFEF NPC行为补充] " + (npc.Name?.ToString() ?? "NPC") + "已代表家族正式提交议程“" + entry.Title + "”。");
					}
				}
				return "";
			});
			text = Regex.Replace(text, @"\[ACTION:AGENDA:[^\]\r\n]*\]", "", RegexOptions.IgnoreCase).Trim();
		}
	}
}

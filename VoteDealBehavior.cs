using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Xml;
using HarmonyLib;
using Bannerlord.UIExtenderEx;
using Bannerlord.UIExtenderEx.Attributes;
using Bannerlord.UIExtenderEx.Prefabs2;
using Bannerlord.UIExtenderEx.ViewModels;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.CampaignSystem.MapNotificationTypes;
using TaleWorlds.CampaignSystem.ViewModelCollection;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement;
using TaleWorlds.CampaignSystem.ViewModelCollection.KingdomManagement.Decisions;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.ImageIdentifiers;
using TaleWorlds.Library;

namespace System.Runtime.CompilerServices
{
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	internal sealed class ModuleInitializerAttribute : Attribute { }
}

namespace AnimusForge
{
	public partial class VoteDealBehavior : CampaignBehaviorBase
	{
		public static VoteDealBehavior Instance { get; private set; }

		private sealed class VoteDealRecord
		{
			public string DealId;
			public string NpcHeroStringId;
			public string NpcClanStringId;
			public string TargetDecisionKey;
			public string TargetDecisionBasicKey;
			public string TargetDecisionTitle;
			public string TargetOptionKey;
			public string TargetOptionBasicKey;
			public string TargetOptionTitle;
			public string TargetOptionSponsorClanId;
			public int SupportWeightValue;
			public float CreatedDay;
			public string Notes;
			public bool IsConsumed;               // true after first vote cast
		}

		private sealed class VoteDealAgendaEntry
		{
			public string Code;
			public KingdomDecision Decision;
			public string DecisionKey;
			public string DecisionBasicKey;
			public string TypeLabel;
			public string Title;
			public string ProposerName;
			public float RemainingDays;
			public List<VoteDealOptionEntry> Options = new List<VoteDealOptionEntry>();
		}

		private sealed class VoteDealOptionEntry
		{
			public string Code;
			public DecisionOutcome Outcome;
			public string OutcomeKey;
			public string OutcomeBasicKey;
			public string Title;
			public string Description;
			public string SponsorName;
			public string SponsorClanId;
		}

		private List<VoteDealRecord> _activeDeals = new List<VoteDealRecord>();
		private Dictionary<string, string> _serializedDeals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		private const string DealIdPrefix = "VD";
		private const char RecordDelimiter = '\x01';

		private static bool s_globalPatchesApplied;

		// ── Module Initializer ─────────────────────────────────────────────

		[ModuleInitializer]
		internal static void ModuleInit()
		{
			ApplyGlobalPatchesOnce();
		}


		private static void ApplyGlobalPatchesOnce()
		{
			if (s_globalPatchesApplied) return;
			s_globalPatchesApplied = true;
			try
			{
				Harmony harmony = new Harmony("com.AnimusForge.votedeal");
				harmony.Patch(
					typeof(MyBehavior).GetMethod("BuildShoutPromptContextForExternal",
						BindingFlags.Public | BindingFlags.Static),
					postfix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_BuildContext_Postfix)));

				// DetermineSupport is abstract → cannot patch base directly.
				// Enumerate all concrete subclasses and patch each override, like BellumCivile does.
				int patchedCount = 0;
				int errorCount = 0;
				foreach (Type subType in typeof(KingdomDecision).Assembly.GetTypes())
				{
					if (!subType.IsClass || subType.IsAbstract) continue;
					if (!typeof(KingdomDecision).IsAssignableFrom(subType)) continue;

					MethodInfo subMethod = subType.GetMethod("DetermineSupport",
						BindingFlags.Public | BindingFlags.Instance);
					if (subMethod == null || subMethod.IsAbstract) continue;

					try
					{
						harmony.Patch(subMethod,
							prefix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_DetermineSupport_Prefix)));
						patchedCount++;
					}
					catch (Exception ex)
					{
						errorCount++;
						Logger.Log("VoteDeal", $"[Harmony] Failed to patch {subType.Name}.DetermineSupport: {ex.Message}");
					}
				}
				Logger.Log("VoteDeal", $"[Harmony] DetermineSupport: patched {patchedCount} concrete subclass(es), {errorCount} error(s).");

				// ── 21-day delay: extend TriggerTime for player kingdom decisions ──
				harmony.Patch(
					typeof(Kingdom).GetMethod("AddDecision"),
					postfix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_AddDecision_21DayDelay_Postfix)));
				Logger.Log("VoteDeal", "[Harmony] Kingdom.AddDecision 21-day delay hook applied.");

				// ── Block ForceDecideDecision when TriggerTime is still future ──
				Type kingdomMgmtVmType = typeof(KingdomManagementVM);
				if (kingdomMgmtVmType != null)
				{
					harmony.Patch(
						kingdomMgmtVmType.GetMethod("ForceDecideDecision", BindingFlags.NonPublic | BindingFlags.Instance),
						prefix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_ForceDecideDecision_Block_Prefix)));
					Logger.Log("VoteDeal", "[Harmony] ForceDecideDecision block patch applied.");
				}

				harmony.Patch(
					typeof(CampaignInformationManager).GetMethod(nameof(CampaignInformationManager.NewMapNoticeAdded),
						BindingFlags.Public | BindingFlags.Instance),
					prefix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_NewMapNoticeAdded_SuppressKingdomVoteReminder_Prefix)));
				harmony.Patch(
					typeof(KingdomDecisionMapNotification).GetMethod(nameof(KingdomDecisionMapNotification.IsValid),
						BindingFlags.Public | BindingFlags.Instance),
					prefix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_KingdomDecisionMapNotification_IsValid_Prefix)));
				harmony.Patch(
					typeof(KingdomDecisionsVM).GetMethod(nameof(KingdomDecisionsVM.HandleDecision),
						BindingFlags.Public | BindingFlags.Instance),
					prefix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_KingdomDecisionsVM_HandleDecision_SuppressVoteInquiry_Prefix)));
				harmony.Patch(
					typeof(KingdomDecisionsVM).GetMethod(nameof(KingdomDecisionsVM.OnFrameTick),
						BindingFlags.Public | BindingFlags.Instance),
					prefix: new HarmonyMethod(typeof(VoteDealBehavior), nameof(Patch_KingdomDecisionsVM_OnFrameTick_MarkAgendaDecisionsExamined_Prefix)));
				Logger.Log("VoteDeal", "[Harmony] Original kingdom vote reminders suppressed.");

				// ── Kingdom Agenda patches ──
				try
				{
					KingdomAgendaTabSelectionPatch.Apply(harmony);
					Logger.Log("VoteDeal", "[Agenda] TabSelection patches applied via VoteDeal Harmony.");
				}
				catch (Exception agendaEx)
				{
					Logger.Log("VoteDeal", $"[Agenda] TabSelection patches failed: {agendaEx.Message}");
				}


			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[Harmony Error] {ex.Message}");
			}
		}

		// ── Support weight parsing ────────────────────────────────────────

		private static Supporter.SupportWeights ParseSupportWeight(string weightStr)
		{
			switch ((weightStr ?? "").Trim().ToUpperInvariant())
			{
				case "FULLY_PUSH": case "FULLYPUSH": case "FULLY": case "4":
					return Supporter.SupportWeights.FullyPush;
				case "STRONGLY_FAVOR": case "STRONGLYFAVOR": case "STRONGLY": case "3":
					return Supporter.SupportWeights.StronglyFavor;
				default:
					return Supporter.SupportWeights.SlightlyFavor;
			}
		}

		// ── ID generation ──────────────────────────────────────────────────

		private static string GenerateDealId()
		{
			byte[] bytes = new byte[4];
			new Random().NextBytes(bytes);
			return DealIdPrefix + bytes[0].ToString("x2") + bytes[1].ToString("x2")
				+ bytes[2].ToString("x2") + bytes[3].ToString("x2");
		}

		// ── CampaignBehavior overrides ─────────────────────────────────────

		public override void RegisterEvents()
		{
			ApplyGlobalPatchesOnce();
			Instance = this;
			Logger.Log("VoteDeal", "[Lifecycle] RegisterEvents called, Instance set");
			CampaignEvents.KingdomDecisionConcluded.AddNonSerializedListener(this, OnKingdomDecisionConcluded);
		}

		public override void SyncData(IDataStore dataStore)
		{
			try
			{
				if (_activeDeals == null) _activeDeals = new List<VoteDealRecord>();
				if (_serializedDeals == null) _serializedDeals = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

				if (dataStore.IsSaving)
				{
					_serializedDeals.Clear();
					for (int i = 0; i < _activeDeals.Count; i++)
					{
						var d = _activeDeals[i];
						_serializedDeals["vd_" + i] = string.Join(RecordDelimiter.ToString(),
							d.DealId ?? "",
							d.NpcHeroStringId ?? "",
							d.NpcClanStringId ?? "",
							d.SupportWeightValue.ToString(),
							d.CreatedDay.ToString("F6"),
							d.Notes ?? "",
							d.IsConsumed ? "1" : "0",
							d.TargetDecisionKey ?? "",
							d.TargetDecisionBasicKey ?? "",
							d.TargetDecisionTitle ?? "",
							d.TargetOptionKey ?? "",
							d.TargetOptionBasicKey ?? "",
							d.TargetOptionTitle ?? "",
							d.TargetOptionSponsorClanId ?? "");
					}
				}

				dataStore.SyncData("_vdSerializedDeals", ref _serializedDeals);

				if (dataStore.IsLoading)
				{
					_activeDeals.Clear();
					if (_serializedDeals != null && _serializedDeals.Count > 0)
					{
						foreach (string val in _serializedDeals.Values)
						{
							if (string.IsNullOrEmpty(val)) continue;
							string[] parts = val.Split(RecordDelimiter);
							if (parts.Length < 6) continue;
							int offset = IsLegacySerializedDirection(parts.Length > 3 ? parts[3] : null) ? 1 : 0;
							var record = new VoteDealRecord
							{
								DealId = parts[0],
								NpcHeroStringId = parts[1],
								NpcClanStringId = parts[2],
								SupportWeightValue = int.TryParse(parts[3 + offset], out int sw) ? sw : 2,
								CreatedDay = parts.Length > 4 + offset && float.TryParse(parts[4 + offset], out float cd) ? cd : 0f,
								Notes = parts.Length > 5 + offset ? parts[5 + offset] ?? "" : "",
								IsConsumed = parts.Length > 6 + offset && parts[6 + offset] == "1",
								TargetDecisionKey = parts.Length > 7 + offset ? parts[7 + offset] ?? "" : "",
								TargetDecisionBasicKey = parts.Length > 8 + offset ? parts[8 + offset] ?? "" : "",
								TargetDecisionTitle = parts.Length > 9 + offset ? parts[9 + offset] ?? "" : "",
								TargetOptionKey = parts.Length > 10 + offset ? parts[10 + offset] ?? "" : "",
								TargetOptionBasicKey = parts.Length > 11 + offset ? parts[11 + offset] ?? "" : "",
								TargetOptionTitle = parts.Length > 12 + offset ? parts[12 + offset] ?? "" : "",
								TargetOptionSponsorClanId = parts.Length > 13 + offset ? parts[13 + offset] ?? "" : ""
							};
							if (!string.IsNullOrWhiteSpace(record.TargetDecisionKey))
							{
								_activeDeals.Add(record);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[SyncData Error] {ex.Message}");
				_activeDeals = new List<VoteDealRecord>();
				_serializedDeals = new Dictionary<string, string>();
			}
		}

		// ── KingdomDecisionConcluded handler ───────────────────────────────

		private void OnKingdomDecisionConcluded(KingdomDecision decision, DecisionOutcome chosenOutcome, bool isPlayerInvolved)
		{
			try
			{
				if (_activeDeals == null || _activeDeals.Count == 0) return;
				if (decision?.Kingdom == null) return;

				List<VoteDealRecord> processedDeals = new List<VoteDealRecord>();
				foreach (VoteDealRecord deal in _activeDeals.Where(d => !d.IsConsumed))
				{
					Hero npc = Hero.FindFirst(h => h.StringId == deal.NpcHeroStringId);
					if (npc?.Clan?.Kingdom != decision.Kingdom) continue;
					if (!DoesVoteDealMatchDecision(deal, decision)) continue;

					deal.IsConsumed = true;
					processedDeals.Add(deal);

					string completionText = !string.IsNullOrWhiteSpace(deal.TargetOptionTitle)
						? $"投票交易完成：{npc.Name}已按承诺投票选择「{deal.TargetOptionTitle}」。"
						: $"投票交易完成：{npc.Name}已按承诺投票。";
					AnimusForgeQuickInfo.ShowForDuration(
						completionText,
						5000, npc.CharacterObject);
				}

				if (processedDeals.Count > 0)
					Logger.Log("VoteDeal", $"[DecisionConcluded] Processed {processedDeals.Count} deal(s) for kingdom={decision.Kingdom.StringId}.");
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[OnKingdomDecisionConcluded Error] {ex.Message}");
			}
		}

		// ── Tag processing ─────────────────────────────────────────────────

		private void ProcessVoteDealTagsInternal(Hero npc, ref string responseText)
		{
			if (npc == null || string.IsNullOrEmpty(responseText)) return;

			Regex voteDealRegex = new Regex(
				@"\[ACTION:VOTE_DEAL:[^\]\r\n]*\]",
				RegexOptions.IgnoreCase);

			int matchCount = 0;
			responseText = voteDealRegex.Replace(responseText, match =>
			{
				matchCount++;
				return ProcessSingleVoteDealTag(npc, match.Value);
			});

			if (matchCount > 0)
			{
				responseText = Regex.Replace(responseText,
					@"\[ACTION:VOTE_DEAL:[^\]]*\]", "", RegexOptions.IgnoreCase);
				responseText = responseText.Trim();
			}
		}

		private string ProcessSingleVoteDealTag(Hero npc, string tag)
		{
			try
			{
				string payload = (tag ?? "").Trim();
				if (payload.StartsWith("[ACTION:VOTE_DEAL:", StringComparison.OrdinalIgnoreCase))
				{
					payload = payload.Substring("[ACTION:VOTE_DEAL:".Length).TrimEnd(']');
				}
				if (string.IsNullOrWhiteSpace(payload)) return "";

				string firstToken = payload.Split(':').FirstOrDefault() ?? "";
				if (!IsVoteDealAgendaCode(firstToken))
				{
					Logger.Log("VoteDeal", $"Vote deal tag skipped — not an agenda code: {tag}");
					return "";
				}
				string[] targetParts = payload.Split(new[] { ':' }, 4);
				if (targetParts.Length < 4)
				{
					Logger.Log("VoteDeal", $"Vote deal tag skipped, bad format (need A:O:weight:notes): {tag}");
					return "";
				}
				return ProcessTargetedVoteDealTag(npc, targetParts[0], targetParts[1], targetParts[2], targetParts[3]);
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[ProcessSingleVoteDealTag Error] {ex.Message}");
				return "";
			}
		}

		private string ProcessTargetedVoteDealTag(Hero npc, string agendaCode, string optionCode, string weightStr, string notes)
		{
			try
			{
				Supporter.SupportWeights weight = ParseSupportWeight(weightStr);
				Clan clan = npc?.Clan;
				Kingdom kingdom = clan?.Kingdom;
				if (clan == null || kingdom == null) return "";
				if (clan.IsUnderMercenaryService) return "";

				// Only clan leader can make vote deals
				if (npc != clan.Leader)
				{
					Logger.Log("VoteDeal", $"Non-leader deal rejected: npc={npc.StringId} clan={clan.StringId}");
					return "";
				}

				if (!TryResolveVoteDealTarget(npc, agendaCode, optionCode, out VoteDealAgendaEntry agenda, out VoteDealOptionEntry option, out string error))
				{
					Logger.Log("VoteDeal", $"Targeted deal skipped: {error}");
					return "";
				}

				// Proposer clan cannot be persuaded — agenda would be cancelled
				if (agenda.Decision.ProposerClan != null && clan.StringId == agenda.Decision.ProposerClan.StringId)
				{
					Logger.Log("VoteDeal", $"Proposer clan deal rejected: clan={clan.StringId} agenda={agenda.Title}");
					return "";
				}

				if (_activeDeals.Any(d => d.NpcClanStringId == clan.StringId
					&& !d.IsConsumed
					&& (string.IsNullOrWhiteSpace(d.TargetDecisionKey) || DoesVoteDealMatchDecisionKeys(d, agenda.DecisionKey, agenda.DecisionBasicKey))))
				{
					Logger.Log("VoteDeal", $"Duplicate targeted deal skipped: clan={clan.StringId} decision={agenda.Title}");
					return "";
				}

				var record = new VoteDealRecord
				{
					DealId = GenerateDealId(),
					NpcHeroStringId = npc.StringId,
					NpcClanStringId = clan.StringId,
					TargetDecisionKey = agenda.DecisionKey,
					TargetDecisionBasicKey = agenda.DecisionBasicKey,
					TargetDecisionTitle = agenda.Title,
					TargetOptionKey = option.OutcomeKey,
					TargetOptionBasicKey = option.OutcomeBasicKey,
					TargetOptionTitle = option.Title,
					TargetOptionSponsorClanId = option.SponsorClanId,
					SupportWeightValue = (int)weight,
					CreatedDay = CampaignTime.Now.ElapsedDaysUntilNow,
					Notes = notes ?? "",
					IsConsumed = false
				};

				_activeDeals.Add(record);

				string clanName = clan.Name?.ToString() ?? "未知家族";
				AnimusForgeQuickInfo.ShowForDuration(
					$"{clanName}家族 承诺在「{agenda.Title}」中投票选择「{option.Title}」",
					6000, npc.CharacterObject);

				Logger.Log("VoteDeal", $"Targeted vote deal created: id={record.DealId} clan={clanName} agenda={agenda.Code}:{agenda.Title} option={option.Code}:{option.Title} weight={weight}");
				return "";
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[ProcessTargetedVoteDealTag Error] {ex.Message}");
				return "";
			}
		}

		// ── AI context builder ─────────────────────────────────────────────

		private static bool IsVoteDealAgendaCode(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return false;
			value = value.Trim();
			return value.Length > 1
				&& (value[0] == 'A' || value[0] == 'a')
				&& int.TryParse(value.Substring(1), out int index)
				&& index > 0;
		}

		private static bool IsLegacySerializedDirection(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return false;
			string direction = value.Trim();
			return direction.Equals("SUPPORT", StringComparison.OrdinalIgnoreCase)
				|| direction.Equals("OPPOSE", StringComparison.OrdinalIgnoreCase)
				|| direction.Equals("OPTION", StringComparison.OrdinalIgnoreCase);
		}

		private static bool IsVoteDealOptionCode(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return false;
			value = value.Trim();
			return value.Length > 1
				&& (value[0] == 'O' || value[0] == 'o')
				&& int.TryParse(value.Substring(1), out int index)
				&& index > 0;
		}

		private static int ParseVoteDealCodeIndex(string value)
		{
			if (string.IsNullOrWhiteSpace(value) || value.Length < 2) return -1;
			return int.TryParse(value.Trim().Substring(1), out int index) ? index : -1;
		}

		private static string CleanVoteDealText(string value)
		{
			if (string.IsNullOrWhiteSpace(value)) return "";
			value = value.Replace(RecordDelimiter, ' ').Replace("\r", " ").Replace("\n", " ").Trim();
			return Regex.Replace(value, @"\s+", " ");
		}

		private static string NormalizeVoteDealKeyPart(string value)
		{
			return CleanVoteDealText(value).ToLowerInvariant();
		}

		private static string BuildVoteDealDecisionKey(KingdomDecision decision, bool includeProposer)
		{
			if (decision == null) return "";
			return string.Join("|",
				decision.GetType().FullName ?? "",
				decision.Kingdom?.StringId ?? "",
				includeProposer ? (decision.ProposerClan?.StringId ?? "") : "",
				NormalizeVoteDealKeyPart(GetDecisionTypeLabel(decision)),
				NormalizeVoteDealKeyPart(decision.GetGeneralTitle()?.ToString() ?? ""));
		}

		private static string BuildVoteDealOutcomeKey(DecisionOutcome outcome, bool includeSponsor)
		{
			if (outcome == null) return "";
			return string.Join("|",
				outcome.GetType().FullName ?? "",
				includeSponsor ? (outcome.SponsorClan?.StringId ?? "") : "",
				NormalizeVoteDealKeyPart(outcome.GetDecisionTitle()?.ToString() ?? ""),
				NormalizeVoteDealKeyPart(outcome.GetDecisionDescription()?.ToString() ?? ""));
		}

		private static bool DoesVoteDealMatchDecisionKeys(VoteDealRecord deal, string decisionKey, string decisionBasicKey)
		{
			if (deal == null) return false;
			return (!string.IsNullOrWhiteSpace(deal.TargetDecisionKey) && string.Equals(deal.TargetDecisionKey, decisionKey, StringComparison.Ordinal))
				|| (!string.IsNullOrWhiteSpace(deal.TargetDecisionBasicKey) && string.Equals(deal.TargetDecisionBasicKey, decisionBasicKey, StringComparison.Ordinal));
		}

		private static bool DoesVoteDealMatchDecision(VoteDealRecord deal, KingdomDecision decision)
		{
			if (deal == null || decision == null) return false;
			return DoesVoteDealMatchDecisionKeys(deal, BuildVoteDealDecisionKey(decision, includeProposer: true), BuildVoteDealDecisionKey(decision, includeProposer: false));
		}

		private static bool DoesVoteDealMatchOutcome(VoteDealRecord deal, DecisionOutcome outcome)
		{
			if (deal == null || outcome == null) return false;
			string outcomeKey = BuildVoteDealOutcomeKey(outcome, includeSponsor: true);
			string outcomeBasicKey = BuildVoteDealOutcomeKey(outcome, includeSponsor: false);
			if (!string.IsNullOrWhiteSpace(deal.TargetOptionKey) && string.Equals(deal.TargetOptionKey, outcomeKey, StringComparison.Ordinal))
			{
				return true;
			}
			if (!string.IsNullOrWhiteSpace(deal.TargetOptionBasicKey) && string.Equals(deal.TargetOptionBasicKey, outcomeBasicKey, StringComparison.Ordinal))
			{
				return true;
			}
			string title = CleanVoteDealText(outcome.GetDecisionTitle()?.ToString() ?? "");
			string sponsorId = outcome.SponsorClan?.StringId ?? "";
			return !string.IsNullOrWhiteSpace(deal.TargetOptionTitle)
				&& string.Equals(CleanVoteDealText(deal.TargetOptionTitle), title, StringComparison.OrdinalIgnoreCase)
				&& (string.IsNullOrWhiteSpace(deal.TargetOptionSponsorClanId) || string.Equals(deal.TargetOptionSponsorClanId, sponsorId, StringComparison.OrdinalIgnoreCase));
		}

		private static float GetForcedVoteDealSupportScore(int supportWeightValue)
		{
			Supporter.SupportWeights weight = (Supporter.SupportWeights)supportWeightValue;
			switch (weight)
			{
				case Supporter.SupportWeights.FullyPush:
					return 1000f;
				case Supporter.SupportWeights.StronglyFavor:
					return 700f;
				default:
					return 350f;
			}
		}

		private static List<VoteDealAgendaEntry> BuildVoteDealAgendaEntries(Hero npc)
		{
			List<VoteDealAgendaEntry> result = new List<VoteDealAgendaEntry>();
			try
			{
				Kingdom kingdom = npc?.Clan?.Kingdom;
				if (kingdom?.UnresolvedDecisions == null) return result;
				int agendaIndex = 1;
				foreach (KingdomDecision decision in kingdom.UnresolvedDecisions)
				{
					if (decision == null || decision.ShouldBeCancelled()) continue;
					try
					{
						IEnumerable<DecisionOutcome> initialCandidates = decision.DetermineInitialCandidates();
						if (initialCandidates == null) continue;
						MBList<DecisionOutcome> candidateList = new MBList<DecisionOutcome>();
						foreach (DecisionOutcome candidate in initialCandidates)
						{
							if (candidate != null) candidateList.Add(candidate);
						}
						if (candidateList.Count == 0) continue;
						MBList<DecisionOutcome> narrowed = decision.NarrowDownCandidates(candidateList, 3);
						if (narrowed == null || narrowed.Count == 0) continue;
						decision.DetermineSponsors(narrowed);

						VoteDealAgendaEntry agenda = new VoteDealAgendaEntry
						{
							Code = "A" + agendaIndex,
							Decision = decision,
							DecisionKey = BuildVoteDealDecisionKey(decision, includeProposer: true),
							DecisionBasicKey = BuildVoteDealDecisionKey(decision, includeProposer: false),
							TypeLabel = GetDecisionTypeLabel(decision),
							Title = CleanVoteDealText(decision.GetGeneralTitle()?.ToString() ?? "未命名提案"),
							ProposerName = CleanVoteDealText(decision.ProposerClan?.Name?.ToString() ?? "未知"),
							RemainingDays = decision.TriggerTime.RemainingDaysFromNow
						};

						int optionIndex = 1;
						foreach (DecisionOutcome outcome in narrowed)
						{
							if (outcome == null) continue;
							agenda.Options.Add(new VoteDealOptionEntry
							{
								Code = "O" + optionIndex,
								Outcome = outcome,
								OutcomeKey = BuildVoteDealOutcomeKey(outcome, includeSponsor: true),
								OutcomeBasicKey = BuildVoteDealOutcomeKey(outcome, includeSponsor: false),
								Title = CleanVoteDealText(outcome.GetDecisionTitle()?.ToString() ?? "未知选项"),
								Description = CleanVoteDealText(outcome.GetDecisionDescription()?.ToString() ?? ""),
								SponsorName = CleanVoteDealText(outcome.SponsorClan?.Name?.ToString() ?? "未知"),
								SponsorClanId = outcome.SponsorClan?.StringId ?? ""
							});
							optionIndex++;
						}
						result.Add(agenda);
						agendaIndex++;
					}
					catch (Exception ex)
					{
						Logger.Log("VoteDeal", $"[BuildAgendaEntries] skipped decision: {ex.Message}");
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[BuildAgendaEntries Error] {ex.Message}");
			}
			return result;
		}

		private static bool TryResolveVoteDealTarget(Hero npc, string agendaCode, string optionCode, out VoteDealAgendaEntry agenda, out VoteDealOptionEntry option, out string error)
		{
			agenda = null;
			option = null;
			error = "";
			if (!IsVoteDealAgendaCode(agendaCode) || !IsVoteDealOptionCode(optionCode))
			{
				error = $"bad target code agenda={agendaCode} option={optionCode}";
				return false;
			}
			List<VoteDealAgendaEntry> agendas = BuildVoteDealAgendaEntries(npc);
			int agendaIndex = ParseVoteDealCodeIndex(agendaCode) - 1;
			if (agendaIndex < 0 || agendaIndex >= agendas.Count)
			{
				error = $"agenda code out of range: {agendaCode}";
				return false;
			}
			agenda = agendas[agendaIndex];
			int optionIndex = ParseVoteDealCodeIndex(optionCode) - 1;
			if (optionIndex < 0 || optionIndex >= agenda.Options.Count)
			{
				error = $"option code out of range: {agendaCode}/{optionCode}";
				return false;
			}
			option = agenda.Options[optionIndex];
			return true;
		}

		public static string BuildPendingDecisionsContext(Hero npc)
		{
			try
			{
				if (npc == null) return "";
				Clan clan = npc.Clan;
				Kingdom kingdom = clan?.Kingdom;
				if (kingdom == null) return "";
				if (clan.IsUnderMercenaryService) return "";

				StringBuilder sb = new StringBuilder();

				// Non-leader cannot make vote deals
				if (clan.Leader != null && npc != clan.Leader)
				{
					sb.Append("【投票交易身份限制】你不是家族族长（你的族长是");
					sb.Append(clan.Leader.Name?.ToString() ?? "未知");
					sb.AppendLine("），你不能代表家族做出投票承诺。如果有人找你拉票，你必须告知对方去找你的族长商议。你不得输出VOTE_DEAL标签。");
				}

				// Existing commitments block
				VoteDealBehavior inst = Instance ?? Campaign.Current?.GetCampaignBehavior<VoteDealBehavior>();
				if (inst != null && inst._activeDeals != null)
				{
					string clanId = clan.StringId;
					List<VoteDealRecord> existing = inst._activeDeals
						.Where(d => d.NpcClanStringId == clanId && !d.IsConsumed)
						.ToList();
					if (existing.Count > 0)
					{
						sb.AppendLine("【你已承诺的投票交易】（务必遵守！不可反悔！）");
						foreach (var deal in existing)
						{
							sb.Append(" - 议程:").Append(string.IsNullOrWhiteSpace(deal.TargetDecisionTitle) ? "未知议程" : deal.TargetDecisionTitle);
							sb.Append("，已承诺投票选择:").Append(string.IsNullOrWhiteSpace(deal.TargetOptionTitle) ? "未知选项" : deal.TargetOptionTitle);
							sb.Append("，权重:").Append((Supporter.SupportWeights)deal.SupportWeightValue);
							if (!string.IsNullOrEmpty(deal.Notes))
								sb.Append("，备注:").Append(deal.Notes);
							sb.AppendLine();
						}
					}
				}

				// Kingdom agenda context
				if (kingdom.UnresolvedDecisions != null && kingdom.UnresolvedDecisions.Count > 0)
				{
					sb.AppendLine();
					sb.AppendLine("【王国当前议程】（正在公示中的提案，公示期结束后将进入投票阶段。你可以按议程名称、城镇/国家/政策名、候选人或家族名理解玩家想拉票的对象；正文只自然说话，不要输出任何系统标签。）");
					List<VoteDealAgendaEntry> agendas = BuildVoteDealAgendaEntries(npc);
					foreach (VoteDealAgendaEntry agenda in agendas)
					{
						bool isOwnProposal = agenda.Decision.ProposerClan?.StringId == clan.StringId;
						string proposerNote = isOwnProposal ? "【你的家族提案，不可交易】" : "";
						if (agenda.RemainingDays > 0)
							sb.AppendLine($" - [{agenda.TypeLabel}] {agenda.Title}（提案人: {agenda.ProposerName}，剩余 {agenda.RemainingDays:F1} 天进入投票）{proposerNote}");
						else
							sb.AppendLine($" - [{agenda.TypeLabel}] {agenda.Title}（提案人: {agenda.ProposerName}，即将进入投票）{proposerNote}");
						foreach (VoteDealOptionEntry option in agenda.Options)
						{
							string sponsorText = string.IsNullOrWhiteSpace(option.SponsorName) || option.SponsorName == "未知" ? "" : $"，赞助/候选:{option.SponsorName}";
							string descriptionText = string.IsNullOrWhiteSpace(option.Description) ? "" : $"，说明:{option.Description}";
							sb.AppendLine($"   - 可投选项: {option.Title}{sponsorText}{descriptionText}");
						}
					}
				}

				return sb.ToString().TrimEnd();
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[BuildPendingDecisionsContext Error] {ex.Message}");
				return "";
			}
		}

		public static string BuildVoteDealPostprocessContext(Hero npc)
		{
			try
			{
				if (!CanUseVoteDealPostprocess(npc))
				{
					return "";
				}

				StringBuilder sb = new StringBuilder();
				List<VoteDealAgendaEntry> agendas = BuildVoteDealAgendaEntries(npc);
				if (agendas.Count == 0)
				{
					sb.AppendLine("【投票交易后处理清单】当前没有可拉票的活跃议程。玩家可能与NPC讨论未来可能提出的提案，但因无法确定具体议程和选项，禁止输出 VOTE_DEAL。");
					return sb.ToString().TrimEnd();
				}

				sb.AppendLine("【投票交易后处理清单】以下 A/O 编号只供后处理输出隐藏标签使用，不得让NPC正文照读。玩家可以用议程名称、城镇/国家/政策名、候选人、家族名、支持/反对等自然说法表达拉票目标；只有能唯一匹配到一个议程和一个选项时，才允许输出 [ACTION:VOTE_DEAL:议程编号:选项编号:权重:备注]。");
				foreach (VoteDealAgendaEntry agenda in agendas)
				{
					bool isOwnProposal = agenda.Decision.ProposerClan?.StringId == (npc?.Clan?.StringId ?? "");
					string proposerNote = isOwnProposal ? "【你的家族提案，不可交易】 " : "";
					string timing = agenda.RemainingDays > 0 ? $"剩余 {agenda.RemainingDays:F1} 天" : "即将投票";
					sb.AppendLine($"{agenda.Code}: [{agenda.TypeLabel}] {agenda.Title}（提案人:{agenda.ProposerName}，{timing}）{proposerNote}");
					foreach (VoteDealOptionEntry option in agenda.Options)
					{
						string sponsorText = string.IsNullOrWhiteSpace(option.SponsorName) || option.SponsorName == "未知" ? "" : $"；赞助/候选:{option.SponsorName}";
						string descriptionText = string.IsNullOrWhiteSpace(option.Description) ? "" : $"；说明:{option.Description}";
						sb.AppendLine($"- {option.Code}: {option.Title}{sponsorText}{descriptionText}");
					}
				}
				sb.AppendLine("【投票交易后处理硬约束】若玩家或NPC没有把议程与选项说清楚、多个议程或多个选项都可能匹配、NPC只是继续谈条件或拒绝，禁止输出 VOTE_DEAL。若NPC不是家族族长，禁止输出VOTE_DEAL。若NPC所属氏族是某议程的提案氏族，禁止就该议程输出VOTE_DEAL。若NPC已对同一议程有承诺，禁止改投其他选项。");
				return sb.ToString().TrimEnd();
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[BuildVoteDealPostprocessContext Error] {ex.Message}");
				return "";
			}
		}

		internal static string GetSupportWeightImagePath(Supporter.SupportWeights weight)
		{
			switch (weight)
			{
				case Supporter.SupportWeights.SlightlyFavor: return "SPKingdom\\voter_strength1";
				case Supporter.SupportWeights.StronglyFavor: return "SPKingdom\\voter_strength2";
				case Supporter.SupportWeights.FullyPush: return "SPKingdom\\voter_strength3";
			}
			return string.Empty;
		}

		private static string GetDecisionTypeLabel(KingdomDecision decision)
	{
		if (decision is DeclareWarDecision) return "宣战";
		if (decision is MakePeaceKingdomDecision) return "和平";
		if (decision is KingdomPolicyDecision) return "政策";
		if (decision is SettlementClaimantPreliminaryDecision) return "封地";
		if (decision is SettlementClaimantDecision) return "封地";
		if (decision is KingSelectionKingdomDecision) return "王选";
		if (decision is ExpelClanFromKingdomDecision) return "驱逐";
		if (decision is StartAllianceDecision) return "联盟";
		if (decision is TradeAgreementDecision) return "贸易";
		return "决议";
	}

	public static bool CanUseVoteDealPostprocess(Hero npc)
		{
			try
			{
				Clan clan = npc?.Clan;
				return clan != null && clan.Kingdom != null && !clan.IsUnderMercenaryService;
			}
			catch
			{
				return false;
			}
		}

		private static string BuildRuntimeVoteDealInstruction(Hero npc)
		{
			try
			{
				Clan clan = npc?.Clan;
				Kingdom kingdom = clan?.Kingdom;

				string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
				if (string.IsNullOrWhiteSpace(playerName))
				{
					playerName = "玩家";
				}
				var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
				{
					["playerName"] = playerName,
				};

				string stateKey = "";
				if (kingdom == null)
				{
					stateKey = "no_kingdom";
				}
				else if (clan.IsUnderMercenaryService)
				{
					stateKey = "mercenary";
				}
				else
				{
					VoteDealBehavior inst = Instance ?? Campaign.Current?.GetCampaignBehavior<VoteDealBehavior>();
					bool hasExistingDeal = inst?._activeDeals?.Any(d => d.NpcClanStringId == clan.StringId && !d.IsConsumed) == true;
					if (hasExistingDeal)
					{
						stateKey = "has_existing_deal";
					}
					else if (kingdom.UnresolvedDecisions == null || kingdom.UnresolvedDecisions.Count == 0)
					{
						stateKey = "no_pending";
					}
				}

				var results = new List<string>();

				if (!string.IsNullOrWhiteSpace(stateKey))
				{
					string stateTemplate = AIConfigHandler.ResolveRuleRuntimeText("vote_deal", stateKey, forConstraint: false, tokens);
					if (!string.IsNullOrWhiteSpace(stateTemplate))
					{
						results.Add(stateTemplate);
					}
					if (stateKey == "no_kingdom" || stateKey == "mercenary" || stateKey == "no_pending")
					{
						return string.Join("\n", results);
					}
				}

				if (kingdom != null && !clan.IsUnderMercenaryService)
				{
					int trustLevelIndex = 6;
					try
					{
						int trust = RewardSystemBehavior.Instance?.GetEffectiveTrust(npc) ?? 0;
						trustLevelIndex = RewardSystemBehavior.GetTrustLevelIndex(trust);
					}
					catch
					{
						trustLevelIndex = 6;
					}

					string trustTemplate = AIConfigHandler.ResolveRuleRuntimeText("vote_deal", "level_" + trustLevelIndex, forConstraint: false, tokens);
					if (!string.IsNullOrWhiteSpace(trustTemplate))
					{
						results.Add(trustTemplate);
					}

					if (kingdom.RulingClan?.Leader == npc)
					{
						string kingTemplate = AIConfigHandler.ResolveRuleRuntimeText("vote_deal", "is_king", forConstraint: false, tokens);
						if (!string.IsNullOrWhiteSpace(kingTemplate))
						{
							results.Add(kingTemplate);
						}
					}
				}

				return string.Join("\n", results);
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[BuildRuntimeVoteDealInstruction Error] {ex.Message}");
				return "";
			}
		}

		// ── Dispatch entry point (called directly by ShoutBehavior) ─────

		public static void ProcessVoteDealTagsDispatch(Hero npc, ref string text)
		{
			if (npc == null)
			{
				Logger.Log("VoteDeal", "[Dispatch] npc is null, abort");
				return;
			}
			if (string.IsNullOrEmpty(text))
			{
				Logger.Log("VoteDeal", "[Dispatch] text is empty, abort");
				return;
			}
			if (!text.Contains("VOTE_DEAL"))
			{
				return;
			}
			VoteDealBehavior behavior = Instance
				?? Campaign.Current?.GetCampaignBehavior<VoteDealBehavior>();
			if (behavior == null)
			{
				Logger.Log("VoteDeal", "[Dispatch] Instance is null, abort");
				return;
			}
			behavior.ProcessVoteDealTagsInternal(npc, ref text);
		}

		// ── Patch: Inject pending decisions context ───────────────────────

		private static void Patch_BuildContext_Postfix(Hero targetHero, string input, string extraFact,
			string cultureIdOverride, bool hasAnyHero, CharacterObject targetCharacter,
			string kingdomIdOverride, int targetAgentIndex, bool suppressDynamicRuleAndLore,
			bool usePrefetchedLoreContext, string prefetchedLoreContext,
			ref MyBehavior.ShoutPromptContext __result)
		{
			try
			{
				if (__result == null) return;
				Hero contextTarget = targetHero ?? (targetCharacter?.HeroObject);
				bool voteDealRuleInjected = (__result.Extras ?? "").IndexOf("【附加规则:vote_deal】", StringComparison.OrdinalIgnoreCase) >= 0;
				if (voteDealRuleInjected)
				{
					string runtimeInstruction = BuildRuntimeVoteDealInstruction(contextTarget);
					if (!string.IsNullOrWhiteSpace(runtimeInstruction))
					{
						__result.Extras = (__result.Extras ?? "") + "\n" + runtimeInstruction;
					}
				}
				if ((__result.Extras ?? "").IndexOf("【附加规则:propose_agenda】", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					string pr = BuildProposeRuntimeInstruction(contextTarget);
					if (!string.IsNullOrWhiteSpace(pr))
						__result.Extras = (__result.Extras ?? "") + "\n" + pr;
				}
				string ctx = BuildPendingDecisionsContext(contextTarget);
				if (!string.IsNullOrEmpty(ctx))
				{
					__result.Extras = (__result.Extras ?? "") + "\n" + ctx;
				}
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[Patch_BuildContext Error] {ex.Message}");
			}
		}

		// ── Patch: Override NPC vote via DetermineSupport ─

		private static bool Patch_DetermineSupport_Prefix(
			KingdomDecision __instance,
			Clan clan,
			DecisionOutcome possibleOutcome,
			ref float __result)
		{
			try
			{
				if (clan == null || string.IsNullOrEmpty(clan.StringId)) return true;

				// Safety: proposer clan vote must not be overridden — would cancel the decision
				if (clan.StringId == __instance.ProposerClan?.StringId) return true;

				VoteDealBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<VoteDealBehavior>();
				if (behavior == null) return true;
				if (behavior._activeDeals == null || behavior._activeDeals.Count == 0) return true;

				VoteDealRecord deal = behavior._activeDeals
					.Where(d => d.NpcClanStringId == clan.StringId && !d.IsConsumed && !string.IsNullOrWhiteSpace(d.TargetDecisionKey))
					.FirstOrDefault(d => DoesVoteDealMatchDecision(d, __instance));
				if (deal != null)
				{
					__result = DoesVoteDealMatchOutcome(deal, possibleOutcome)
						? GetForcedVoteDealSupportScore(deal.SupportWeightValue)
						: 0f;
					return false;
				}
				return true;

			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[Patch_DetermineSupport Error] {ex.Message}");
				return true;
			}
		}

		// ── Patch: 21-day delay for player kingdom decisions ──

		private static void Patch_AddDecision_21DayDelay_Postfix(KingdomDecision kingdomDecision)
		{
			try
			{
				if (kingdomDecision == null) return;
				if (kingdomDecision.TriggerTime.IsPast) return;
				Kingdom kingdom = kingdomDecision.Kingdom;
				if (kingdom == null) return;
				if (kingdom != Clan.PlayerClan?.Kingdom) return;

				Traverse.Create(kingdomDecision).Property("TriggerTime")
					.SetValue(CampaignTime.DaysFromNow(21f));
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[21DayDelay] Error: {ex.Message}");
			}
		}

		private static bool Patch_NewMapNoticeAdded_SuppressKingdomVoteReminder_Prefix(InformationData informationData)
		{
			try
			{
				if (informationData is KingdomDecisionMapNotification notification &&
					ShouldSuppressOriginalKingdomVoteReminder(notification.Decision))
				{
					Logger.Log("VoteDeal", "[ReminderSuppress] Hidden original map kingdom vote reminder.");
					return false;
				}
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[ReminderSuppress] NewMapNoticeAdded error: {ex.Message}");
			}
			return true;
		}

		private static bool Patch_KingdomDecisionMapNotification_IsValid_Prefix(
			KingdomDecisionMapNotification __instance, ref bool __result)
		{
			try
			{
				if (__instance != null && ShouldSuppressOriginalKingdomVoteReminder(__instance.Decision))
				{
					__result = false;
					return false;
				}
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[ReminderSuppress] Map notification validity error: {ex.Message}");
			}
			return true;
		}

		private static bool Patch_KingdomDecisionsVM_HandleDecision_SuppressVoteInquiry_Prefix(
			object __instance, KingdomDecision curDecision)
		{
			try
			{
				if (!ShouldSuppressOriginalKingdomVoteReminder(curDecision)) return true;

				MarkDecisionExaminedWithoutInquiry(__instance, curDecision);
				Logger.Log("VoteDeal", "[ReminderSuppress] Hidden original kingdom vote inquiry.");
				return false;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[ReminderSuppress] HandleDecision error: {ex.Message}");
				return true;
			}
		}

		private static void Patch_KingdomDecisionsVM_OnFrameTick_MarkAgendaDecisionsExamined_Prefix(object __instance)
		{
			try
			{
				if (__instance == null) return;
				Kingdom kingdom = Clan.PlayerClan?.Kingdom;
				if (kingdom == null) return;

				foreach (KingdomDecision decision in kingdom.UnresolvedDecisions)
				{
					if (ShouldSuppressOriginalKingdomVoteReminder(decision))
					{
						MarkDecisionExaminedWithoutInquiry(__instance, decision);
					}
				}
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[ReminderSuppress] OnFrameTick mark error: {ex.Message}");
			}
		}

		private static bool ShouldSuppressOriginalKingdomVoteReminder(KingdomDecision decision)
		{
			try
			{
				if (decision == null) return false;
				if (decision.IsEnforced) return false;
				if (!decision.IsPlayerParticipant) return false;
				if (decision.Kingdom == null || decision.Kingdom != Clan.PlayerClan?.Kingdom) return false;
				if (decision.ShouldBeCancelled()) return false;
				return true;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[ReminderSuppress] Match error: {ex.Message}");
				return false;
			}
		}

		private static void MarkDecisionExaminedWithoutInquiry(object kingdomDecisionsVm, KingdomDecision decision)
		{
			if (kingdomDecisionsVm == null || decision == null) return;

			Traverse traverse = Traverse.Create(kingdomDecisionsVm);
			List<KingdomDecision> examined = traverse.Field("_examinedDecisionsSinceInit")
				.GetValue<List<KingdomDecision>>();
			if (examined != null && !examined.Contains(decision))
			{
				examined.Add(decision);
			}

			traverse.Property("_shouldCheckForDecision").SetValue(true);
			traverse.Field("_queryData").SetValue(null);
		}

		// ── Patch: Block ForceDecideDecision when TriggerTime is future ──

		private static bool Patch_ForceDecideDecision_Block_Prefix(
			object __instance, KingdomDecision decision)
		{
			try
			{
				if (decision == null) return true;
				if (!decision.TriggerTime.IsFuture) return true;
				if (decision.Kingdom != Clan.PlayerClan?.Kingdom) return true;

				float remainingDays = decision.TriggerTime.RemainingDaysFromNow;
				InformationManager.DisplayMessage(new InformationMessage(
					$"提案「{decision.GetGeneralTitle()}」将在 {remainingDays:F1} 天后开始投票",
					Color.FromUint(0xFFD700)));
				return false;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[ForceDecideBlock] Error: {ex.Message}");
				return true;
			}
		}

		}

		// ════════════════════════════════════════════════════════════════════════
		//  Agenda UI: PrefabExtensions + ViewModels (property injection via KingdomAgendaPatch.cs)
		// ════════════════════════════════════════════════════════════════════════

		[PrefabExtension("KingdomManagement", "descendant::ButtonWidget[@Id='ArmiesTabButton']")]
		internal sealed class KingdomAgendaTabButtonPatch : PrefabExtensionInsertPatch
		{
			private readonly XmlDocument _document;

			public override InsertType Type => (InsertType)4;

			public KingdomAgendaTabButtonPatch()
			{
				_document = new XmlDocument();
				_document.LoadXml(@"
					<ButtonWidget Id='AgendaTabButton' IsSelected='@IsAgendaSelected' DoNotPassEventsToChildren='true' WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='!Header.Tab.Center.Width.Scaled' SuggestedHeight='!Header.Tab.Center.Height.Scaled' VerticalAlignment='Center' PositionYOffset='2' Brush='Header.Tab.Center' Command.Click='ExecuteShowAgenda' UpdateChildrenStates='true'>
					  <Children>
					    <TextWidget DataSource='{..}' WidthSizePolicy='CoverChildren' HeightSizePolicy='CoverChildren' HorizontalAlignment='Center' VerticalAlignment='Center' Brush='Clan.TabControl.Text' Text='@AgendaTabText' />
					  </Children>
					</ButtonWidget>");
			}

			[PrefabExtensionXmlDocument(false)]
			public XmlDocument GetPrefabExtension()
			{
				return _document;
			}
		}

	[PrefabExtension("KingdomManagement", "descendant::DiplomacyPanel[@Id='DiplomacyPanel']")]
	internal sealed class KingdomAgendaPanelPatch : PrefabExtensionInsertPatch
	{
		private readonly XmlDocument _document;

		public override InsertType Type => (InsertType)4;

		public KingdomAgendaPanelPatch()
		{
			_document = new XmlDocument();
									_document.LoadXml(@"
				<Widget Id='AgendaPanelRoot' IsVisible='@IsAgendaSelected' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' MarginTop='188' MarginBottom='75'>
				  <Children>
				    <Widget Id='AgendaPanel' DataSource='{Agenda}' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent'>
				      <Children>
				        <ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent'>
				          <Children>
				            <BrushListPanel WidthSizePolicy='Fixed' SuggestedWidth='585' HeightSizePolicy='StretchToParent' VerticalAlignment='Bottom' MarginLeft='5' MarginTop='6' MarginBottom='9' Brush='Frame1Brush' StackLayout.LayoutMethod='VerticalBottomToTop'>
				              <Children>
				                <ListPanel WidthSizePolicy='CoverChildren' HeightSizePolicy='CoverChildren' RenderLate='true'>
				                  <Children>
				                    <Widget WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='585' SuggestedHeight='60' Sprite='SPKingdom\header_policies' ExtendTop='21' ExtendRight='13' ExtendBottom='20' RenderLate='true'>
				                      <Children>
				                        <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Brush='Kingdom.PoliciesCollapserTitle.Text' MarginBottom='8' IsDisabled='true' Text='活跃议程' />
				                      </Children>
				                    </Widget>
				                    <Widget WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='23' SuggestedHeight='60' Sprite='StdAssets\scroll_header' ExtendRight='3' ExtendTop='6' ExtendLeft='3' ExtendBottom='4' HorizontalAlignment='Right' />
				                  </Children>
				                </ListPanel>

				                <Widget WidthSizePolicy='CoverChildren' HeightSizePolicy='StretchToParent'>
				                  <Children>
				                    <RichTextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' VerticalAlignment='Center' HorizontalAlignment='Center' MarginLeft='24' MarginRight='24' Brush='Popup.Description.Text' Brush.TextHorizontalAlignment='Center' Text='暂无进行中的决议' IsVisible='@!HasItems' DoNotAcceptEvents='true' />

				                    <ScrollablePanel WidthSizePolicy='CoverChildren' HeightSizePolicy='StretchToParent' MarginLeft='3' MarginBottom='10' AutoHideScrollBars='true' ClipRect='AgendaClipRect' InnerPanel='AgendaClipRect\AgendaInnerPanel' VerticalScrollbar='..\AgendaScrollbar\Scrollbar' IsVisible='@HasItems'>
				                      <Children>
				                        <Widget Id='AgendaClipRect' WidthSizePolicy='CoverChildren' HeightSizePolicy='StretchToParent' ClipContents='true'>
				                          <Children>
				                            <NavigatableListPanel Id='AgendaInnerPanel' DataSource='{AgendaItems}' WidthSizePolicy='Fixed' SuggestedWidth='585' HeightSizePolicy='CoverChildren' StackLayout.LayoutMethod='VerticalBottomToTop' MinIndex='0' StepSize='1000'>
				                              <ItemTemplate>
				                                <ButtonWidget DoNotPassEventsToChildren='true' WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='85' Brush='Kingdom.Policy.Active.Tuple' Command.Click='ExecuteSelect' UpdateChildrenStates='true' IsSelected='@IsSelected'>
				                                  <Children>
				                                    <ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' StackLayout.LayoutMethod='VerticalBottomToTop' MarginLeft='12' MarginRight='12'>
				                                      <Children>
				                                        <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='35' VerticalAlignment='Center' Brush='Kingdom.PoliciesItem.Text' Text='@TitleText' ClipContents='false' />
				                                        <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='20' HorizontalAlignment='Center' VerticalAlignment='Center' Brush='Popup.Description.Text' Brush.FontSize='14' Brush.TextHorizontalAlignment='Center' Text='@DaysRemainingText' />
				                                        <ListPanel WidthSizePolicy='CoverChildren' HeightSizePolicy='Fixed' SuggestedHeight='20' StackLayout.LayoutMethod='HorizontalLeftToRight' MarginTop='2'>
				                                          <Children>
				                                            <Widget WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='44' SuggestedHeight='20' HorizontalAlignment='Left' VerticalAlignment='Center' Sprite='BlankWhiteSquare_9' Color='#8A6E3EFF' AlphaFactor='0.18'>
				                                              <Children>
				                                                <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Brush='Popup.Description.Text' Brush.FontSize='12' Brush.TextHorizontalAlignment='Center' Text='@DecisionTypeText' />
				                                              </Children>
				                                            </Widget>
				                                          </Children>
				                                        </ListPanel>
				                                      </Children>
				                                    </ListPanel>
				                                  </Children>
				                                </ButtonWidget>
				                              </ItemTemplate>
				                            </NavigatableListPanel>
				                          </Children>
				                        </Widget>
				                      </Children>
				                    </ScrollablePanel>
				                    <Standard.VerticalScrollbar Id='AgendaScrollbar' WidthSizePolicy='CoverChildren' HeightSizePolicy='StretchToParent' HorizontalAlignment='Right' MarginRight='2' MarginLeft='2' MarginBottom='10' />
				                  </Children>
				                </Widget>
				              </Children>
				            </BrushListPanel>

				<Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' MarginLeft='8' MarginRight='8' MarginTop='8' Sprite='BlankWhiteSquare_9' Color='#C8A070FF' AlphaFactor='0.15'>
				  <Children>
				    <RichTextWidget IsVisible='@!HasItems' WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' VerticalAlignment='Center' HorizontalAlignment='Center' MarginTop='80' Brush='Kingdom.PoliciesCollapserTitle.Text' Brush.TextHorizontalAlignment='Center' Brush.FontSize='24' Text='暂无进行中的议程' DoNotAcceptEvents='true' />
				    <Widget IsVisible='@HasItems' WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren'>
				      <Children>
				        <RichTextWidget IsVisible='@!HasSelectedItem' WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' VerticalAlignment='Center' HorizontalAlignment='Center' MarginTop='80' Brush='Kingdom.PoliciesCollapserTitle.Text' Brush.TextHorizontalAlignment='Center' Brush.FontSize='22' Text='选择一个议程条目查看详情' DoNotAcceptEvents='true' />
				      </Children>
				    </Widget>
				    <Widget IsVisible='@HasSelectedItem' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent'>
				      <Children>
				        <ScrollablePanel WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' AutoHideScrollBars='true' ClipRect='DetailClipRect2' InnerPanel='DetailClipRect2\DetailInnerPanel2'>
				          <Children>
				            <Widget Id='DetailClipRect2' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' ClipContents='true'>
				              <Children>
				                <ListPanel Id='DetailInnerPanel2' DataSource='{SelectedItem}' WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' StackLayout.LayoutMethod='VerticalBottomToTop' MarginRight='5'>
				                  <Children>
				                    <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='54' MarginTop='4' Brush='Kingdom.DecisionTitleBig.Text' Text='@TitleText' Brush.FontSize='46' />
				                    <RichTextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' MarginTop='4' Brush='Popup.Description.Text' Brush.FontSize='18' Text='@DaysRemainingText' />
				                    <ButtonWidget IsVisible='@CanCallVoteMeeting' DoNotPassEventsToChildren='true' WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='260' SuggestedHeight='36' HorizontalAlignment='Center' MarginTop='10' Brush='ButtonBrush2' Command.Click='ExecuteCallVoteMeeting' UpdateChildrenStates='true'>
				                      <Children>
				                        <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Brush='Kingdom.GeneralButtons.Text' Text='召开投票会议' DoNotAcceptEvents='true' />
				                      </Children>
				                    </ButtonWidget>
				                    <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='2' Sprite='SPKingdom\Diplomacy\divider_left' MarginTop='10' MarginBottom='10' AlphaFactor='0.5' />
				                    <Widget IsVisible='@HasDetail' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent'>
				                      <Children>
				                        <ListPanel DataSource='{Options}' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' StackLayout.LayoutMethod='HorizontalLeftToRight'>
				                          <ItemTemplate>
				                            <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' MarginRight='6'>
				                              <Children>
				                                <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Sprite='BlankWhiteSquare_9' Color='#F0D9A0FF' AlphaFactor='0.25' />
				                                <ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' StackLayout.LayoutMethod='VerticalBottomToTop' MarginLeft='14' MarginRight='14' MarginTop='14' MarginBottom='14'>
				                                  <Children>
				                                    <ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='48' StackLayout.LayoutMethod='HorizontalLeftToRight'>
					                                      <Children>
					                                        <Widget DataSource='{SponsorVisual}' WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='42' SuggestedHeight='42' VerticalAlignment='Center'>
					                                          <Children>
					                                            <ImageIdentifierWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' ImageId='@Id' AdditionalArgs='@AdditionalArgs' TextureProviderName='@TextureProviderName' />
					                                          </Children>
					                                        </Widget>
					                                        <Widget IsVisible='@HasSponsorBanner' WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='28' SuggestedHeight='28' VerticalAlignment='Center' MarginLeft='8'>
					                                          <Children>
					                                            <ImageIdentifierWidget DataSource='{SponsorBanner}' WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' ImageId='@Id' AdditionalArgs='@AdditionalArgs' TextureProviderName='@TextureProviderName' />
					                                          </Children>
					                                        </Widget>
					                                        <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' VerticalAlignment='Center' MarginLeft='8' Brush='Popup.Description.Text' Brush.FontSize='17' Text='@SponsorName' />
					                                      </Children>
					                                    </ListPanel>
					                                    <ListPanel DataSource='{Supporters}' WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' StackLayout.LayoutMethod='VerticalBottomToTop' MarginTop='8'>
				                                      <ItemTemplate>
				                                        <ListPanel WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='36' StackLayout.LayoutMethod='HorizontalLeftToRight'>
					                                          <Children>
					                                            <Widget DataSource='{Visual}' WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='24' SuggestedHeight='24' VerticalAlignment='Center'>
					                                              <Children>
					                                                <ImageIdentifierWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' ImageId='@Id' AdditionalArgs='@AdditionalArgs' TextureProviderName='@TextureProviderName' />
					                                              </Children>
					                                            </Widget>
					                                            <Widget WidthSizePolicy='Fixed' HeightSizePolicy='Fixed' SuggestedWidth='20' SuggestedHeight='20' Sprite='@SupportWeightImagePath' VerticalAlignment='Center' DoNotAcceptEvents='true' MarginLeft='4' />
				                                            <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' VerticalAlignment='Center' MarginLeft='6' Brush='Popup.Description.Text' Brush.FontSize='14' Text='@Name' />
				                                            <RichTextWidget WidthSizePolicy='CoverChildren' HeightSizePolicy='StretchToParent' VerticalAlignment='Center' MarginLeft='6' Brush='Popup.Description.Text' Brush.FontSize='13' Text='@WeightText' />
				                                          </Children>
				                                        </ListPanel>
				                                      </ItemTemplate>
				                                    </ListPanel>
				                                    <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='32' MarginTop='12' Brush='Kingdom.PoliciesItem.Text' Brush.FontSize='22' Text='@Name' />
				                                    <Widget WidthSizePolicy='StretchToParent' HeightSizePolicy='Fixed' SuggestedHeight='36' MarginTop='10' Sprite='BlankWhiteSquare_9' Color='#000000FF' AlphaFactor='0.12'>
				                                      <Children>
				                                        <TextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='StretchToParent' Brush='Kingdom.PoliciesCollapserTitle.Text' Brush.FontSize='18' Brush.TextHorizontalAlignment='Center' VerticalAlignment='Center' IntText='@SupportPercentage' />
				                                      </Children>
				                                    </Widget>
				                                    <RichTextWidget WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' MarginTop='10' Brush='Popup.Description.Text' Brush.FontSize='16' Text='@Description' />
				                                  </Children>
				                                </ListPanel>
				                              </Children>
				                            </Widget>
				                          </ItemTemplate>
				                        </ListPanel>
				                      </Children>
				                    </Widget>
				                    <RichTextWidget IsVisible='@!HasDetail' WidthSizePolicy='StretchToParent' HeightSizePolicy='CoverChildren' VerticalAlignment='Center' HorizontalAlignment='Center' MarginTop='40' Brush='Popup.Description.Text' Brush.TextHorizontalAlignment='Center' Brush.FontSize='18' Text='此议程暂无投票详情' DoNotAcceptEvents='true' />
				                  </Children>
				                </ListPanel>
				              </Children>
				            </Widget>
				          </Children>
				        </ScrollablePanel>
				      </Children>
				    </Widget>
				  </Children>
				</Widget>
				          </Children>
				        </ListPanel>
				      </Children>
				    </Widget>
				  </Children>
				</Widget>");
		}

		[PrefabExtensionXmlDocument(false)]
		public XmlDocument GetPrefabExtension()
		{
			return _document;
		}
	}

	[PrefabExtension("KingdomManagement", "descendant::Constant[@Name='Header.Tab.Center.Width.Scaled']")]
	internal sealed class KingdomAgendaScalingPatch : PrefabExtensionSetAttributePatch
	{
		public override List<Attribute> Attributes => new List<Attribute>
		{
			new Attribute("MultiplyResult", "0.50")
		};
	}

		public class KingdomAgendaVM : ViewModel
	{
		private MBBindingList<KingdomAgendaItemVM> _agendaItems;
		private bool _hasItems;
		private KingdomAgendaItemVM _selectedItem;
		internal Action<KingdomDecision> CallVoteMeetingRequested;

		[DataSourceProperty]
		public MBBindingList<KingdomAgendaItemVM> AgendaItems
		{
			get => _agendaItems;
			set
			{
				if (_agendaItems != value)
				{
					_agendaItems = value;
					OnPropertyChanged("AgendaItems");
				}
			}
		}

		[DataSourceProperty]
		public bool HasItems
		{
			get => _hasItems;
			set
			{
				if (_hasItems != value)
				{
					_hasItems = value;
					OnPropertyChanged("HasItems");
				}
			}
		}

		[DataSourceProperty]
		public KingdomAgendaItemVM SelectedItem
		{
			get => _selectedItem;
			set
			{
				if (_selectedItem != value)
				{
					_selectedItem = value;
					OnPropertyChanged("SelectedItem");
					OnPropertyChanged("HasSelectedItem");
				}
			}
		}

		[DataSourceProperty]
		public bool HasSelectedItem => SelectedItem != null;

		public KingdomAgendaVM()
		{
			AgendaItems = new MBBindingList<KingdomAgendaItemVM>();
		}

		public void RefreshAgendaItems()
		{
			try
			{
				AgendaItems.Clear();
				Kingdom kingdom = Clan.PlayerClan?.Kingdom;
				if (kingdom == null)
				{
					HasItems = false;
					return;
				}

				foreach (KingdomDecision decision in kingdom.UnresolvedDecisions)
				{
					if (decision.ShouldBeCancelled()) continue;
					KingdomAgendaItemVM item = new KingdomAgendaItemVM(decision);
					item.Parent = this;
					AgendaItems.Add(item);
				}

				HasItems = AgendaItems.Count > 0;
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[AgendaVM] RefreshAgendaItems error: {ex.Message}");
				HasItems = false;
			}
		}

		public void SelectItem(KingdomAgendaItemVM item)
		{
			if (SelectedItem != null)
				SelectedItem.IsSelected = false;

			if (item != null)
			{
				item.IsSelected = true;
				item.LoadDetail();
			}

			SelectedItem = item;
		}

		internal void CallVoteMeeting(KingdomAgendaItemVM item)
		{
			KingdomDecision decision = item?.Decision;
			if (decision == null) return;

			CallVoteMeetingRequested?.Invoke(decision);
			item.RefreshTimingState();
		}

		public void ClearSelection()
		{
			if (SelectedItem != null)
			{
				SelectedItem.IsSelected = false;
				SelectedItem = null;
			}
		}
	}

	public class KingdomAgendaItemVM : ViewModel
	{
		private string _titleText;
		private string _decisionTypeText;
		private string _daysRemainingText;
		private bool _isUrgent;
		private bool _isSelected;
		private bool _canCallVoteMeeting;
		private KingdomDecision _decision;
		internal KingdomAgendaVM Parent;
		private MBBindingList<AgendaOptionVM> _options;
		private bool _hasDetail;

		[DataSourceProperty]
		public string TitleText
		{
			get => _titleText;
			set { _titleText = value; OnPropertyChanged("TitleText"); }
		}

		[DataSourceProperty]
		public string DecisionTypeText
		{
			get => _decisionTypeText;
			set { _decisionTypeText = value; OnPropertyChanged("DecisionTypeText"); }
		}

		[DataSourceProperty]
		public string DaysRemainingText
		{
			get => _daysRemainingText;
			set { _daysRemainingText = value; OnPropertyChanged("DaysRemainingText"); }
		}

		[DataSourceProperty]
		public bool IsUrgent
		{
			get => _isUrgent;
			set { _isUrgent = value; OnPropertyChanged("IsUrgent"); }
		}

		[DataSourceProperty]
		public bool IsSelected
		{
			get => _isSelected;
			set
			{
				if (_isSelected != value)
				{
					_isSelected = value;
					OnPropertyChanged("IsSelected");
				}
			}
		}

		public KingdomDecision Decision => _decision;

		[DataSourceProperty]
		public bool CanCallVoteMeeting
		{
			get => _canCallVoteMeeting;
			set
			{
				if (_canCallVoteMeeting != value)
				{
					_canCallVoteMeeting = value;
					OnPropertyChanged("CanCallVoteMeeting");
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<AgendaOptionVM> Options
		{
			get => _options;
			set
			{
				if (_options != value)
				{
					_options = value;
					OnPropertyChanged("Options");
				}
			}
		}

		[DataSourceProperty]
		public bool HasDetail
		{
			get => _hasDetail;
			set
			{
				if (_hasDetail != value)
				{
					_hasDetail = value;
					OnPropertyChanged("HasDetail");
				}
			}
		}

		public KingdomAgendaItemVM(KingdomDecision decision)
		{
			_decision = decision;
			TitleText = decision.GetGeneralTitle()?.ToString() ?? "未命名提案";
			DecisionTypeText = GetDecisionTypeLabel(decision);
			RefreshTimingState();
		}

		internal void RefreshTimingState()
		{
			if (_decision == null)
			{
				DaysRemainingText = "提案人: 未知";
				CanCallVoteMeeting = false;
				return;
			}

			float remainingDays = _decision.TriggerTime.RemainingDaysFromNow;
			DaysRemainingText = (remainingDays > 0)
				? $"提案人: {_decision.ProposerClan?.Name?.ToString() ?? "未知"} · 剩余 {remainingDays:F1} 天"
				: $"提案人: {_decision.ProposerClan?.Name?.ToString() ?? "未知"} · 可以投票";
			IsUrgent = remainingDays <= 1f;
			CanCallVoteMeeting = _decision.Kingdom == Clan.PlayerClan?.Kingdom &&
				_decision.IsPlayerParticipant &&
				!_decision.IsEnforced &&
				!_decision.ShouldBeCancelled();
		}

		[DataSourceMethod]
		public void ExecuteSelect()
		{
			Parent?.SelectItem(this);
		}

		[DataSourceMethod]
		public void ExecuteCallVoteMeeting()
		{
			Parent?.CallVoteMeeting(this);
		}

		private static string GetWeightText(Supporter.SupportWeights weight)
		{
			switch (weight)
			{
				case Supporter.SupportWeights.SlightlyFavor: return "略微支持";
				case Supporter.SupportWeights.StronglyFavor: return "强力支持";
				case Supporter.SupportWeights.FullyPush: return "全力推动";
				default: return "中立";
			}
		}

		internal void LoadDetail()
		{
			if (_options != null) return;
			if (_decision == null) return;

			try
			{
				var initialCandidates = _decision.DetermineInitialCandidates();
				if (initialCandidates == null) return;

				var candidateList = new MBList<DecisionOutcome>();
				foreach (var cnd in initialCandidates)
				{
					if (cnd != null) candidateList.Add(cnd);
				}
				if (candidateList.Count == 0) return;

				var narrowed = _decision.NarrowDownCandidates(candidateList, 3);
				if (narrowed == null || narrowed.Count == 0) return;

				_options = new MBBindingList<AgendaOptionVM>();

				_decision.DetermineSponsors(narrowed);

				var supporters = _decision.DetermineSupporters()?.ToList();
				if (supporters == null || supporters.Count == 0) return;

				var likelihoodProp = typeof(DecisionOutcome).GetProperty("Likelihood");
				var initialSupportProp = typeof(DecisionOutcome).GetProperty("InitialSupport");
				float totalInitial = 0f;
				foreach (var outcome in narrowed)
				{
					if (outcome == null) continue;
					float initSup = 0f;
					foreach (var supporter in supporters)
					{
						if (!supporter.IsPlayer)
							initSup += MathF.Clamp(_decision.DetermineSupport(supporter.Clan, outcome), 0f, 100f);
					}
					initialSupportProp?.SetValue(outcome, initSup);
					totalInitial += initSup;
				}
				foreach (var outcome in narrowed)
				{
					if (outcome == null) continue;
					float initSup = (float)(initialSupportProp?.GetValue(outcome) ?? 0f);
					likelihoodProp?.SetValue(outcome, totalInitial > 0f ? initSup / totalInitial : 0f);
				}
				var outcomeWeights = new Dictionary<DecisionOutcome, int>();
				var outcomeSupporters = new Dictionary<DecisionOutcome, List<(Supporter supporter, Supporter.SupportWeights weight)>>();
				foreach (var outcome in narrowed)
				{
					if (outcome == null) continue;
					outcomeWeights[outcome] = 0;
					outcomeSupporters[outcome] = new List<(Supporter, Supporter.SupportWeights)>();
				}

				int totalWeight = 0;
				foreach (var supporter in supporters)
				{
					if (supporter.IsPlayer) continue;
					try
					{
						Supporter.SupportWeights weight;
						var chosen = _decision.DetermineSupportOption(supporter, narrowed, out weight, false);
						if (chosen != null && weight > Supporter.SupportWeights.StayNeutral && outcomeWeights.ContainsKey(chosen))
						{
							outcomeSupporters[chosen].Add((supporter, weight));
						}
					}
					catch { }
				}

				foreach (var outcome in narrowed)
				{
					if (outcome == null) continue;
					outcomeWeights[outcome] = 0;
					outcomeSupporters[outcome].Clear();
				}
				totalWeight = 0;

				_decision.DetermineSponsors(narrowed);

				foreach (var supporter in supporters)
				{
					if (supporter.IsPlayer) continue;
					try
					{
						Supporter.SupportWeights weight;
						var chosen = _decision.DetermineSupportOption(supporter, narrowed, out weight, true);
						if (chosen != null && weight > Supporter.SupportWeights.StayNeutral && outcomeWeights.ContainsKey(chosen))
						{
							int w = (int)weight - (int)Supporter.SupportWeights.StayNeutral;
							outcomeWeights[chosen] += w;
							totalWeight += w;
							outcomeSupporters[chosen].Add((supporter, weight));
						}
					}
					catch { }
				}

				foreach (var outcome in narrowed)
				{
					if (outcome == null) continue;
					var opt = new AgendaOptionVM();
					opt.Name = outcome.GetDecisionTitle()?.ToString() ?? "未知选项";
					opt.Description = outcome.GetDecisionDescription()?.ToString() ?? "";
					opt.SponsorName = outcome.SponsorClan?.Name?.ToString() ?? "未知";
					opt.SupportPercentage = totalWeight > 0 ? (int)Math.Round(outcomeWeights[outcome] * 100.0 / totalWeight) : 0;

					// Sponsor portrait
					try
					{
						Hero sponsorLeader = outcome.SponsorClan?.Leader;
						if (sponsorLeader?.CharacterObject != null)
							opt.SponsorVisual = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(sponsorLeader.CharacterObject));
					}
					catch { }

					// Sponsor clan banner
					try
					{
						Clan sponsorClan = outcome.SponsorClan;
						if (sponsorClan?.Banner != null)
							opt.SponsorBanner = new BannerImageIdentifierVM(sponsorClan.Banner, false);
					}
					catch { }

					foreach (var (supporter, weight) in outcomeSupporters[outcome])
					{
						var supporterVM = new AgendaSupporterVM
						{
							Name = supporter.Name?.ToString() ?? "未知",
							WeightText = GetWeightText(weight),
							SupportWeightImagePath = VoteDealBehavior.GetSupportWeightImagePath(weight)
						};
						try
						{
							Hero leader = supporter.Clan?.Leader;
							if (leader?.CharacterObject != null)
								supporterVM.Visual = new CharacterImageIdentifierVM(CharacterCode.CreateFrom(leader.CharacterObject));
						}
						catch { }
						opt.Supporters.Add(supporterVM);
					}

					_options.Add(opt);
				}
				HasDetail = _options.Count > 0;
				OnPropertyChanged("Options");
				OnPropertyChanged("HasDetail");
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", $"[AgendaDetail] LoadDetail error: {ex.Message}");
			}
		}

		private static string GetDecisionTypeLabel(KingdomDecision decision)
		{
			if (decision is DeclareWarDecision) return "宣战";
			if (decision is MakePeaceKingdomDecision) return "和平";
			if (decision is KingdomPolicyDecision) return "政策";
			if (decision is SettlementClaimantPreliminaryDecision) return "封地";
			if (decision is SettlementClaimantDecision) return "封地";
			if (decision is KingSelectionKingdomDecision) return "王选";
			if (decision is ExpelClanFromKingdomDecision) return "驱逐";
			if (decision is StartAllianceDecision) return "联盟";
			if (decision is TradeAgreementDecision) return "贸易";
			return "决议";
		}
	}

		public class AgendaSupporterVM : ViewModel
		{
			private string _name;
			private string _weightText;
			private CharacterImageIdentifierVM _visual;
			private string _supportWeightImagePath;

			[DataSourceProperty]
			public string Name
			{
				get => _name;
				set { _name = value; OnPropertyChanged("Name"); }
			}

			[DataSourceProperty]
			public string WeightText
			{
				get => _weightText;
				set { _weightText = value; OnPropertyChanged("WeightText"); }
			}

			[DataSourceProperty]
			public CharacterImageIdentifierVM Visual
			{
				get => _visual;
				set
				{
					if (_visual != value)
					{
						_visual = value;
						OnPropertyChanged("Visual");
					}
				}
			}

			[DataSourceProperty]
			public string SupportWeightImagePath
			{
				get => _supportWeightImagePath;
				set
				{
					if (_supportWeightImagePath != value)
					{
						_supportWeightImagePath = value;
						OnPropertyChanged("SupportWeightImagePath");
					}
				}
			}
		}
	public class AgendaOptionVM : ViewModel
	{
		private string _name;
		private string _description;
		private string _sponsorName;
		private int _supportPercentage;
		private MBBindingList<AgendaSupporterVM> _supporters;
			private CharacterImageIdentifierVM _sponsorVisual;
		private ImageIdentifierVM _sponsorBanner;
		private bool _hasSponsorBanner;

		[DataSourceProperty]
		public string Name
		{
			get => _name;
			set { _name = value; OnPropertyChanged("Name"); }
		}

		[DataSourceProperty]
		public string Description
		{
			get => _description;
			set { _description = value; OnPropertyChanged("Description"); }
		}

		[DataSourceProperty]
		public string SponsorName
		{
			get => _sponsorName;
			set { _sponsorName = value; OnPropertyChanged("SponsorName"); }
		}

		[DataSourceProperty]
		public int SupportPercentage
		{
			get => _supportPercentage;
			set
			{
				if (_supportPercentage != value)
				{
					_supportPercentage = value;
					OnPropertyChanged("SupportPercentage");
				}
			}
		}

		[DataSourceProperty]
		public MBBindingList<AgendaSupporterVM> Supporters
		{
			get => _supporters;
			set
			{
				if (_supporters != value)
				{
					_supporters = value;
					OnPropertyChanged("Supporters");
				}
			}
		}


			[DataSourceProperty]
			public CharacterImageIdentifierVM SponsorVisual
			{
				get => _sponsorVisual;
				set
				{
					if (_sponsorVisual != value)
					{
						_sponsorVisual = value;
						OnPropertyChanged("SponsorVisual");
					}
				}
			}

		[DataSourceProperty]
		public ImageIdentifierVM SponsorBanner
		{
			get => _sponsorBanner;
			set
			{
				if (_sponsorBanner != value)
				{
					_sponsorBanner = value;
					OnPropertyChanged("SponsorBanner");
					HasSponsorBanner = _sponsorBanner != null;
				}
			}
		}

		[DataSourceProperty]
		public bool HasSponsorBanner
		{
			get => _hasSponsorBanner;
			set { _hasSponsorBanner = value; OnPropertyChanged("HasSponsorBanner"); }
		}

				public AgendaOptionVM()
		{
			Supporters = new MBBindingList<AgendaSupporterVM>();
		}
	}

	internal static class KingdomAgendaTabState
	{
		private static readonly ConditionalWeakTable<KingdomManagementVM, State> _states = new();

		private sealed class State
		{
			public Action Clear;
			public Action Select;
			public bool ReturnToAgendaAfterRefresh;
		}

		public static void Register(KingdomManagementVM vm, Action clear, Action select)
		{
			_states.Add(vm, new State { Clear = clear, Select = select });
		}

		public static void Clear(KingdomManagementVM vm)
		{
			if (_states.TryGetValue(vm, out var state))
				state.Clear?.Invoke();
		}

		public static void Select(KingdomManagementVM vm)
		{
			if (_states.TryGetValue(vm, out var state))
				state.Select?.Invoke();
		}

		public static void RequestReturnAfterRefresh(KingdomManagementVM vm)
		{
			if (_states.TryGetValue(vm, out var state))
				state.ReturnToAgendaAfterRefresh = true;
		}

		public static void SelectAfterRefreshIfRequested(KingdomManagementVM vm)
		{
			if (!_states.TryGetValue(vm, out var state)) return;
			if (!state.ReturnToAgendaAfterRefresh) return;

			state.ReturnToAgendaAfterRefresh = false;
			state.Select?.Invoke();
		}
	}

		[ViewModelMixin("RefreshValues", true)]
		internal sealed class KingdomAgendaVMMixin : BaseViewModelMixin<KingdomManagementVM>
		{

			[DataSourceProperty]
			public string AgendaTabText { get; set; }

			[DataSourceProperty]
			public bool IsAgendaSelected { get; set; }

			[DataSourceProperty]
			public KingdomAgendaVM Agenda { get; set; }

			public KingdomAgendaVMMixin(KingdomManagementVM vm) : base(vm)
			{
				AgendaTabText = "议程";
				Agenda = new KingdomAgendaVM();
				Agenda.CallVoteMeetingRequested = StartVoteMeeting;
				IsAgendaSelected = false;

				KingdomAgendaTabState.Register(vm, ClearAgendaSelection, SelectAgenda);

			}

			public override void OnRefresh()
			{
				Agenda?.RefreshAgendaItems();
			}

			[DataSourceMethod]
			public void ExecuteShowAgenda()
			{
				SelectAgenda();
			}

			private void SelectAgenda()
			{
				ViewModel.Clan.Show = false;
				ViewModel.Settlement.Show = false;
				ViewModel.Policy.Show = false;
				ViewModel.Army.Show = false;
				ViewModel.Diplomacy.Show = false;

				IsAgendaSelected = true;
				Agenda?.RefreshAgendaItems();
				Agenda?.RefreshValues();

				ViewModel.OnPropertyChanged("IsAgendaSelected");
				ViewModel.OnPropertyChangedWithValue(true, "IsAgendaSelected");
				ViewModel.OnPropertyChanged("Agenda");

			}

			private void StartVoteMeeting(KingdomDecision decision)
			{
				try
				{
					if (decision == null) return;
					if (decision.Kingdom != Clan.PlayerClan?.Kingdom) return;
					if (!decision.IsPlayerParticipant) return;
					if (decision.ShouldBeCancelled())
					{
						InformationManager.DisplayMessage(new InformationMessage("该议程已经失效，无法召开投票会议", Color.FromUint(0xFFD166)));
						Agenda?.RefreshAgendaItems();
						return;
					}

					if (decision.TriggerTime.IsFuture)
					{
						Traverse.Create(decision).Property("TriggerTime").SetValue(CampaignTime.Now);
					}

					KingdomAgendaTabState.RequestReturnAfterRefresh(ViewModel);
					ViewModel.Decision.RefreshWith(decision);
					ViewModel.Decision.IsActive = true;
					ViewModel.OnPropertyChanged("Decision");
					Agenda?.ClearSelection();
					InformationManager.DisplayMessage(new InformationMessage(
						$"已召开投票会议：{decision.GetGeneralTitle()}",
						Color.FromUint(0xFFD166)));
				}
				catch (Exception ex)
				{
					Logger.Log("VoteDeal", $"[AgendaMeeting] StartVoteMeeting error: {ex.Message}");
					InformationManager.DisplayMessage(new InformationMessage("召开投票会议失败，请查看 AnimusForge 日志", Color.FromUint(0xFF6B6B)));
				}
			}

			private void ClearAgendaSelection()
			{
				IsAgendaSelected = false;
				ViewModel.OnPropertyChanged("IsAgendaSelected");
				ViewModel.OnPropertyChangedWithValue(false, "IsAgendaSelected");
			}
		}

		internal static class KingdomAgendaTabSelectionPatch
		{
			public static void Apply(Harmony harmony)
			{
				if (harmony == null) return;

				var postfix = new HarmonyMethod(typeof(KingdomAgendaTabSelectionPatch), nameof(ClearPostfix));

				string[] methodNames = { "ExecuteShowClan", "ExecuteShowFiefs", "ExecuteShowPolicies", "ExecuteShowArmy", "ExecuteShowDiplomacy" };

				foreach (string name in methodNames)
				{
					MethodInfo original = AccessTools.Method(typeof(KingdomManagementVM), name);
					if (original != null)
					{
						harmony.Patch(original, postfix: postfix);
					}
					else
					{
						Logger.Log("VoteDeal", $"[AgendaTabSelection] WARNING: KingdomManagementVM.{name} not found.");
					}
				}

				MethodInfo onRefresh = AccessTools.Method(typeof(KingdomManagementVM), "OnRefresh");
				if (onRefresh != null)
				{
					harmony.Patch(onRefresh,
						postfix: new HarmonyMethod(typeof(KingdomAgendaTabSelectionPatch), nameof(OnRefreshPostfix)));
				}
				else
				{
					Logger.Log("VoteDeal", "[AgendaTabSelection] WARNING: KingdomManagementVM.OnRefresh not found.");
				}
			}

			private static void ClearPostfix(KingdomManagementVM __instance)
			{
				KingdomAgendaTabState.Clear(__instance);
			}

			private static void OnRefreshPostfix(KingdomManagementVM __instance)
			{
				KingdomAgendaTabState.SelectAfterRefreshIfRequested(__instance);
			}

		}

	}

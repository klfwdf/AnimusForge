using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge
{
	public partial class VoteDealBehavior
	{
		private readonly System.Collections.Generic.Dictionary<string, BilateralDiplomacyRecord> _factionPeaceOperations
			= new System.Collections.Generic.Dictionary<string, BilateralDiplomacyRecord>(StringComparer.Ordinal);
		private readonly System.Collections.Generic.Dictionary<KingdomDecision, BilateralDiplomacyRecord> _factionPeaceDecisions
			= new System.Collections.Generic.Dictionary<KingdomDecision, BilateralDiplomacyRecord>();

		private bool _factionPeacePersistenceAvailable = true;

		// Campaign-thread only. reason is a stable diagnostic code, not localized UI text.
		internal static string GetOrSubmitFactionPeaceAgenda(string operationId, string factionId,
			Kingdom home, Kingdom target, Clan proposer, out string reason)
		{
			reason = "owner_unavailable";
			VoteDealBehavior owner = Instance ?? Campaign.Current?.GetCampaignBehavior<VoteDealBehavior>();
			if (owner == null) return "failed";
			if (!owner._factionPeacePersistenceAvailable)
			{
				reason = "persistence_unavailable";
				return "failed";
			}
			if (!IsFactionPeaceIdentifier(operationId) || !IsFactionPeaceIdentifier(factionId))
			{
				reason = "invalid_identity";
				return "failed";
			}
			if (owner._factionPeaceOperations.TryGetValue(operationId, out var existing))
			{
				// Query comes before eligibility checks. A changed caller cannot repurpose an ID.
				if (existing.FactionId != factionId || existing.FirstKingdomId != (home?.StringId ?? "")
					|| existing.SecondKingdomId != (target?.StringId ?? "") || existing.OperationProposerId != (proposer?.StringId ?? ""))
				{
					reason = "operation_identity_mismatch";
					return "failed";
				}
				try { owner.ReconcileFactionPeaceOperation(existing); }
				catch (Exception ex)
				{
					SetFactionPeaceTerminal(existing, "failed", "query_exception");
					Logger.Log("VoteDeal", "[FactionPeace] Query failed id=" + operationId + " error=" + ex.Message);
				}
				return GetFactionPeaceStatus(existing, out reason);
			}
			var record = new BilateralDiplomacyRecord
			{
				RecordId = GenerateBilateralDiplomacyId(), Kind = "Peace",
				OperationId = operationId, FactionId = factionId, FirstKingdomId = home?.StringId ?? "",
				SecondKingdomId = target?.StringId ?? "", OperationProposerId = proposer?.StringId ?? "",
				OperationState = "pending", OperationReason = "submitting"
			};
			owner._factionPeaceOperations.Add(operationId, record);
			owner._bilateralDiplomacyRecords.Add(record);
			try
			{
				string failure = null;
				if (Campaign.Current == null || home == null || target == null || home == target
					|| home.IsEliminated || target.IsEliminated) failure = "invalid_kingdom";
				else if (proposer == null || proposer.Kingdom != home || proposer.IsEliminated
					|| proposer.IsUnderMercenaryService || proposer.Leader == null || !proposer.Leader.IsAlive)
					failure = "ineligible_proposer";
				else if (!FactionManager.IsAtWarAgainstFaction(home, target)) failure = "not_at_war";
				else if (target.RulingClan == null || target.RulingClan.Kingdom != target) failure = "counterpart_unavailable";
				else if (owner.HasConflictingPeaceAgenda(record, home, target)) failure = "duplicate_peace_agenda";
				if (failure != null) SetFactionPeaceTerminal(record, "failed", failure);
				else
				{
					var decision = new MakePeaceKingdomDecision(proposer, target);
					if (proposer.Influence < Campaign.Current.Models.DiplomacyModel.GetInfluenceCostOfProposingPeace(proposer)
						|| proposer.Influence < decision.GetInfluenceCost(proposer))
						SetFactionPeaceTerminal(record, "failed", "insufficient_influence");
					else if (!decision.IsAllowed()) SetFactionPeaceTerminal(record, "failed", "decision_not_allowed");
					else
					{
						record.CreatedDay = (float)CampaignTime.Now.ToDays;
						record.FirstDecision = decision;
						record.TributeFromFirstToSecond = decision.DailyTributeToBePaid;
						record.TributeDurationDays = decision.DailyTributeDurationInDays;
						record.FirstDecisionBasicKey = BuildVoteDealDecisionKey(decision, false);
						owner._factionPeaceDecisions[decision] = record;
						home.AddDecision(decision, false);
						if (!IsDecisionPendingInKingdom(decision, home))
						{
							SetFactionPeaceTerminal(record, "failed", "first_not_retained");
						}
						else if (record.OperationState == "pending") record.OperationReason = "first_pending";
					}
				}
			}
			catch (Exception ex)
			{
				SetFactionPeaceTerminal(record, "failed", "submission_exception");
				Logger.Log("VoteDeal", "[FactionPeace] Submission failed id=" + operationId + " error=" + ex.Message);
			}
			return GetFactionPeaceStatus(record, out reason);
		}

		private static bool IsFactionPeaceStatus(string state) => state == "pending" || state == "completed"
			|| state == "rejected" || state == "failed";

		private static string GetFactionPeaceStatus(BilateralDiplomacyRecord record, out string reason)
		{
			if (!IsFactionPeaceStatus(record.OperationState))
			{
				// Preserve the operation as a failed tombstone; never expose unknown save states.
				record.OperationState = "failed";
				record.OperationReason = "invalid_operation_state";
			}
			reason = record.OperationReason ?? "";
			return record.OperationState;
		}

		private static bool IsFactionPeaceIdentifier(string value) => !string.IsNullOrWhiteSpace(value)
			&& value.Length <= 256 && value.IndexOf(RecordDelimiter) < 0;

		private bool HasConflictingPeaceAgenda(BilateralDiplomacyRecord own, Kingdom home, Kingdom target)
		{
			foreach (var record in _bilateralDiplomacyRecords)
			{
				if (record == own || ParseKind(record.Kind) != BilateralDiplomacyKind.Peace
					|| (!string.IsNullOrEmpty(record.OperationId) && record.OperationState != "pending")) continue;
				if ((record.FirstKingdomId == home.StringId && record.SecondKingdomId == target.StringId)
					|| (record.FirstKingdomId == target.StringId && record.SecondKingdomId == home.StringId)) return true;
			}
			foreach (var kingdom in new[] { home, target })
				foreach (var decision in kingdom.UnresolvedDecisions)
					if (decision is MakePeaceKingdomDecision peace
						&& peace.FactionToMakePeaceWith == (kingdom == home ? target : home)) return true;
			return false;
		}

		internal static bool IsFactionPeaceDecision(KingdomDecision decision)
		{
			var owner = Instance ?? Campaign.Current?.GetCampaignBehavior<VoteDealBehavior>();
			return decision is MakePeaceKingdomDecision && owner != null && owner._factionPeacePersistenceAvailable
				&& owner._factionPeaceDecisions.TryGetValue(decision, out var record)
				&& record.OperationState == "pending"
				&& (ReferenceEquals(record.FirstDecision, decision) || ReferenceEquals(record.CounterpartDecision, decision));
		}

		private bool IsFactionPeaceRoyalProposer(KingdomDecision decision, Clan clan)
		{
			return clan != null && clan != Clan.PlayerClan && decision?.ProposerClan == clan
				&& clan.Kingdom?.RulingClan == clan
				&& _factionPeaceDecisions.TryGetValue(decision, out var record)
				&& record.OperationState == "pending" && ReferenceEquals(record.FirstDecision, decision)
				&& record.OperationProposerId == clan.StringId;
		}

		private BilateralDiplomacyRecord FindFactionPeaceOperation(KingdomDecision decision)
			=> decision != null && _factionPeaceDecisions.TryGetValue(decision, out var record) ? record : null;

		private static void SetFactionPeaceTerminal(BilateralDiplomacyRecord record, string state, string reason)
		{
			if (record == null || string.IsNullOrEmpty(record.OperationId) || record.OperationState != "pending") return;
			record.OperationState = IsFactionPeaceStatus(state) && state != "pending" ? state : "failed";
			record.OperationReason = reason;
			// Queue an owner re-evaluation only. Diplomacy never finishes the faction itself.
			// Commit status before notifying so a subsequent query observes the terminal receipt.
			try
			{
				Kingdom home = ResolveKingdom(record.FirstKingdomId);
				if (home != null)
					AnimusForge.Refactor.Modules.TeamModuleServices.CivilWar.NotifyPoliticalChange(home, "faction_peace_vote");
			}
			catch (Exception ex)
			{
				Logger.Log("VoteDeal", "[FactionPeace] Political notification failed id=" + record.OperationId + " error=" + ex.Message);
			}
		}

		private void ReconcileFactionPeaceOperation(BilateralDiplomacyRecord record)
		{
			if (record.OperationState != "pending" || record.OperationReason == "submitting"
				|| record.OperationReason == "applying") return;
			Kingdom first = ResolveKingdom(record.FirstKingdomId), second = ResolveKingdom(record.SecondKingdomId);
			KingdomDecision active = record.CounterpartCreated ? record.CounterpartDecision : record.FirstDecision;
			if (first == null || second == null || first.IsEliminated || second.IsEliminated)
				SetFactionPeaceTerminal(record, "failed", "kingdom_unavailable");
			else if (!FactionManager.IsAtWarAgainstFaction(first, second))
				SetFactionPeaceTerminal(record, "failed", "war_ended_before_bilateral_completion");
			else if (active == null || !IsDecisionPendingInKingdom(active, record.CounterpartCreated ? second : first))
				SetFactionPeaceTerminal(record, "failed", "decision_missing_or_cancelled");
			else if (!IsBilateralCounterpartDecisionHardValid(active, out _))
				SetFactionPeaceTerminal(record, "failed", "decision_invalidated");
		}

		private static bool FactionPeaceBindingsValid(BilateralDiplomacyRecord record)
		{
			if (ParseKind(record.Kind) != BilateralDiplomacyKind.Peace
				|| !(record.FirstDecision is MakePeaceKingdomDecision first)
				|| first.Kingdom?.StringId != record.FirstKingdomId
				|| first.ProposerClan?.StringId != record.OperationProposerId
				|| first.FactionToMakePeaceWith?.StringId != record.SecondKingdomId
				|| first.DailyTributeToBePaid != record.TributeFromFirstToSecond
				|| first.DailyTributeDurationInDays != record.TributeDurationDays) return false;
			if (!record.CounterpartCreated) return record.CounterpartDecision == null;
			return record.CounterpartDecision is MakePeaceKingdomDecision second
				&& second.Kingdom?.StringId == record.SecondKingdomId
				&& second.FactionToMakePeaceWith?.StringId == record.FirstKingdomId
				&& second.DailyTributeToBePaid == -record.TributeFromFirstToSecond
				&& second.DailyTributeDurationInDays == record.TributeDurationDays;
		}

		private void RestoreFactionPeaceOperations()
		{
			_factionPeaceOperations.Clear();
			_factionPeaceDecisions.Clear();
			foreach (var record in _bilateralDiplomacyRecords)
			{
				if (string.IsNullOrEmpty(record.OperationId)) continue;
				if (_factionPeaceOperations.ContainsKey(record.OperationId))
					throw new CampaignSaveChunkHelper.PersistenceIntegrityException("_vdBilateralDiplomacy", "duplicate_faction_peace_operation");
				_factionPeaceOperations.Add(record.OperationId, record);
				if (!IsFactionPeaceStatus(record.OperationState))
				{
					record.OperationState = "failed";
					record.OperationReason = "invalid_saved_state";
				}
				foreach (var decision in new[] { record.FirstDecision, record.CounterpartDecision })
				{
					if (!(decision is MakePeaceKingdomDecision)) continue;
					if (_factionPeaceDecisions.ContainsKey(decision))
						throw new CampaignSaveChunkHelper.PersistenceIntegrityException("_vdFactionPeaceDecisionRefs", "duplicate_decision_binding");
					_factionPeaceDecisions.Add(decision, record);
				}
				if (record.OperationState == "pending" && !FactionPeaceBindingsValid(record))
					SetFactionPeaceTerminal(record, "failed", "invalid_saved_binding");
				// No reconstruction/re-submission, including saves interrupted during apply.
				if (record.OperationReason == "applying" || record.OperationReason == "submitting")
					SetFactionPeaceTerminal(record, "failed", "interrupted_operation");
				ReconcileFactionPeaceOperation(record);
			}
		}

		private static string ExecutePropose(Hero npc, string type, string tgt, string dir, string supportWeight)
		{
			try
			{
				Clan cl = npc?.Clan;
				Kingdom kd = cl?.Kingdom;
				if (cl == null || kd == null || cl.IsUnderMercenaryService || npc != cl.Leader)
					return "";
				if (IsWorldDiplomacyTakeoverEnabled()
					&& (string.Equals(type, "WAR", StringComparison.OrdinalIgnoreCase)
						|| string.Equals(type, "PEACE", StringComparison.OrdinalIgnoreCase)
						|| string.Equals(type, "ALLIANCE", StringComparison.OrdinalIgnoreCase)
						|| string.Equals(type, "TRADE", StringComparison.OrdinalIgnoreCase)))
				{
					Logger.Log("ProposeAgenda", "Rejected: world diplomacy takeover requires direct sovereign action type=" + type + " target=" + tgt);
					return "";
				}

				var dm = Campaign.Current.Models.DiplomacyModel;
				KingdomDecision d = null;

				switch (type)
				{
					case "WAR":
					{
						var tg = FindFactionByName(tgt);
						if (tg == null || !tg.IsKingdomFaction) return "";
						if (FactionManager.IsAtWarAgainstFaction(kd, tg)) return "";
						int cst = dm.GetInfluenceCostOfProposingWar(cl);
						if (cl.Influence < cst) return "";
						d = new DeclareWarDecision(cl, (Kingdom)tg);
						break;
					}
					case "PEACE":
					{
						var tg = FindFactionByName(tgt);
						if (tg == null || !tg.IsKingdomFaction) return "";
						if (!FactionManager.IsAtWarAgainstFaction(kd, tg)) return "";
						int cst = dm.GetInfluenceCostOfProposingPeace(cl);
						if (cl.Influence < cst) return "";
						d = new MakePeaceKingdomDecision(cl, (Kingdom)tg);
						break;
					}
					case "POLICY":
					{
						var po = FindPolicyByName(tgt);
						if (po == null) return "";
						po = ResolvePolicyForKingdomAgenda(kd, po, out bool ia);
						if (po == null) return "";
						bool ad = dir == "ADOPT", ab = dir == "ABOLISH";
						if (!ad && !ab) { ad = !ia; ab = ia; }
						if ((ad && ia) || (ab && !ia)) return "";
						int cst = dm.GetInfluenceCostOfPolicyProposalAndDisavowal(cl);
						if (cl.Influence < cst) return "";
						d = new KingdomPolicyDecision(cl, po, ab);
						break;
					}
					case "EXPEL":
					{
						var tc = FindClanByName(tgt, kd);
						if (tc == null || tc == kd.RulingClan || tc == cl) return "";
						d = new ExpelClanFromKingdomDecision(cl, tc);
						if (cl.Influence < d.GetInfluenceCost(cl)) return "";
						break;
					}
					case "ALLIANCE":
					{
						var tg = FindFactionByName(tgt);
						if (tg == null || !tg.IsKingdomFaction) return "";
						if (!FactionManager.IsNeutralWithFaction(kd, tg)) return "";
						IAllianceCampaignBehavior allianceBehavior = Campaign.Current.GetCampaignBehavior<IAllianceCampaignBehavior>();
						if (allianceBehavior?.IsAllyWithKingdom(kd, (Kingdom)tg) == true) return "";
						d = new StartAllianceDecision(cl, (Kingdom)tg);
						if (cl.Influence < d.GetInfluenceCost(cl)) return "";
						break;
					}
					case "TRADE":
					{
						var tg = FindFactionByName(tgt);
						if (tg == null || !tg.IsKingdomFaction) return "";
						if (FactionManager.IsAtWarAgainstFaction(kd, tg)) return "";
						ITradeAgreementsCampaignBehavior tradeBehavior = Campaign.Current.GetCampaignBehavior<ITradeAgreementsCampaignBehavior>();
						if (BannerlordApiCompat.HasTradeAgreement(tradeBehavior, kd, (Kingdom)tg)) return "";
						d = new TradeAgreementDecision(cl, (Kingdom)tg);
						if (cl.Influence < d.GetInfluenceCost(cl)) return "";
						break;
					}
					case "FIEF":
					{
						Town tw = FindTownByName(tgt, kd);
						if (tw == null) return "";
						int cst = dm.GetInfluenceCostOfAnnexation(cl);
						if (cl.Influence < cst) return "";
						d = new SettlementClaimantPreliminaryDecision(cl, tw.Settlement);
						break;
					}
				}

				if (d == null)
				{
					Logger.Log("ProposeAgenda", "Rejected: decision_build_failed type=" + type + " target=" + tgt + " npc=" + (npc?.StringId ?? ""));
					return "";
				}
				// Do not call ShouldBeCancelled() here. For a non-player proposer it also
				// re-evaluates whether the NPC clan would support its own proposal using
				// vanilla political weights, which would override the explicit agreement
				// reached in dialogue. Player proposal rights and player influence are not
				// relevant: this decision is proposed by the NPC clan.
				if (!d.IsAllowed())
				{
					Logger.Log("ProposeAgenda", "Rejected: decision_not_allowed type=" + type + " target=" + tgt + " npc=" + (npc?.StringId ?? ""));
					AnimusForgeQuickInfo.ShowForDuration(
						DiplomacyUiLocalization.Text(
							"AF_DIP_UI_Vote_ProposalRejectedNotAllowed",
							"Agenda submission failed: the current kingdom rules do not allow this agenda."),
						6000,
						npc?.CharacterObject);
					return "";
				}
				string newTitle = d.GetGeneralTitle()?.ToString();
				bool isDuplicate = false;
				foreach (KingdomDecision existing in kd.UnresolvedDecisions)
				{
					if (existing != null && existing.GetType() == d.GetType() && existing.GetGeneralTitle()?.ToString() == newTitle)
					{ isDuplicate = true; break; }
				}
				if (isDuplicate) { Logger.Log("ProposeAgenda", "Duplicate: " + type + " " + tgt); return ""; }
				VoteDealBehavior behavior = Instance ?? Campaign.Current?.GetCampaignBehavior<VoteDealBehavior>();
				behavior?.RegisterDialogueProposedDecision(d, supportWeight);
				try
				{
					kd.AddDecision(d, false);
				}
				catch
				{
					behavior?.UnregisterDialogueProposedDecision(d);
					throw;
				}
				if (kd.UnresolvedDecisions == null || !kd.UnresolvedDecisions.Contains(d))
				{
					behavior?.UnregisterDialogueProposedDecision(d);
					Logger.Log("ProposeAgenda", "Rejected: AddDecision did not retain decision type=" + type + " target=" + tgt);
					AnimusForgeQuickInfo.ShowForDuration(
						DiplomacyUiLocalization.Text(
							"AF_DIP_UI_Vote_ProposalRejectedNotRetained",
							"Agenda submission failed: the kingdom did not retain this agenda."),
						6000,
						npc?.CharacterObject);
					return "";
				}
				string cn = cl.Name?.ToString() ?? "?";
				AnimusForgeQuickInfo.ShowForDuration(
					DiplomacyUiLocalization.Format(
						"AF_DIP_UI_Vote_ProposalSubmitted",
						"{CLAN} proposed a new agenda.",
						("CLAN", cn)),
					6000,
					npc.CharacterObject);
				Logger.Log("ProposeAgenda", "OK: " + type + " " + tgt);
				return "";
			}
			catch (Exception ex)
			{
				Logger.Log("ProposeAgenda", "Exec: " + ex.Message);
				return "";
			}
		}

		private static IFaction FindFactionByName(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) return null;
			name = name.Trim();
			foreach (Kingdom k in Kingdom.All)
			{
				if (string.Equals(k.Name?.ToString() ?? "", name, StringComparison.OrdinalIgnoreCase)) return k;
				if (string.Equals(k.StringId, name, StringComparison.OrdinalIgnoreCase)) return k;
			}
			foreach (Kingdom k in Kingdom.All)
			{
				string kn = k.Name?.ToString() ?? "";
				if (!string.IsNullOrWhiteSpace(kn) && kn.Contains(name)) return k;
			}
			return null;
		}

		private static PolicyObject FindPolicyByName(string name)
		{
			if (string.IsNullOrWhiteSpace(name)) return null;
			name = name.Trim();
			foreach (PolicyObject p in PolicyObject.All)
			{
				if (string.Equals(p.Name?.ToString() ?? "", name, StringComparison.OrdinalIgnoreCase)) return p;
				if (string.Equals(p.StringId, name, StringComparison.OrdinalIgnoreCase)) return p;
			}
			foreach (PolicyObject p in PolicyObject.All)
			{
				string pn = p.Name?.ToString() ?? "";
				if (!string.IsNullOrWhiteSpace(pn) && pn.Contains(name)) return p;
			}
			return null;
		}

		private static Clan FindClanByName(string name, Kingdom kingdom)
		{
			if (string.IsNullOrWhiteSpace(name) || kingdom == null) return null;
			name = name.Trim();
			foreach (Clan cl in kingdom.Clans)
			{
				if (string.Equals(cl.Name?.ToString() ?? "", name, StringComparison.OrdinalIgnoreCase)) return cl;
				if (string.Equals(cl.StringId, name, StringComparison.OrdinalIgnoreCase)) return cl;
			}
			foreach (Clan cl in kingdom.Clans)
			{
				string cn = cl.Name?.ToString() ?? "";
				if (!string.IsNullOrWhiteSpace(cn) && cn.Contains(name)) return cl;
			}
			return null;
		}

		private static Town FindTownByName(string name, Kingdom kingdom)
		{
			if (string.IsNullOrWhiteSpace(name) || kingdom == null) return null;
			name = name.Trim();
			foreach (Town t in kingdom.Fiefs)
			{
				if (string.Equals(t.Name?.ToString() ?? "", name, StringComparison.OrdinalIgnoreCase)) return t;
				if (string.Equals(t.StringId, name, StringComparison.OrdinalIgnoreCase)) return t;
			}
			foreach (Town t in kingdom.Fiefs)
			{
				string tn = t.Name?.ToString() ?? "";
				if (!string.IsNullOrWhiteSpace(tn) && tn.Contains(name)) return t;
			}
			return null;
		}

	}
}

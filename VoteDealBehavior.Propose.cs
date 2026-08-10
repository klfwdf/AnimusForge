using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge
{
	public partial class VoteDealBehavior
	{
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
					AnimusForgeQuickInfo.ShowForDuration("议程提交失败：当前王国规则不允许该议程", 6000, npc?.CharacterObject);
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
					AnimusForgeQuickInfo.ShowForDuration("议程提交失败：王国未保留该议程", 6000, npc?.CharacterObject);
					return "";
				}
				string cn = cl.Name?.ToString() ?? "?";
				AnimusForgeQuickInfo.ShowForDuration(cn + "家族 提出新议程", 6000, npc.CharacterObject);
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

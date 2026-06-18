using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;

namespace AnimusForge
{
	public partial class VoteDealBehavior
	{
		private static readonly Regex ProposeTagRx = new Regex(
			@"\[ACTION:PROPOSE:[^\]\r\n]*\]",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private static readonly string[] ProposeTypes =
			{ "WAR", "PEACE", "POLICY", "EXPEL", "ALLIANCE", "TRADE", "FIEF" };

		public static void ProcessProposeDispatch(Hero npc, ref string text)
		{
			if (npc == null || string.IsNullOrEmpty(text) || !text.Contains("PROPOSE"))
				return;

			var b = Instance ?? Campaign.Current?.GetCampaignBehavior<VoteDealBehavior>();
			if (b == null) return;

			int mc = 0;
			text = ProposeTagRx.Replace(text, m =>
			{
				var t = m.Value;
				var r = ProcessProposeTag(npc, t);
				mc++;
				return r;
			});

			if (mc > 0)
			{
				text = ProposeTagRx.Replace(text, "", 1);
				text = text.Trim();
			}
		}

		private static string ProcessProposeTag(Hero npc, string tag)
		{
			try
			{
				var p = (tag ?? "").Trim();
				if (p.StartsWith("[ACTION:PROPOSE:", StringComparison.OrdinalIgnoreCase))
					p = p.Substring("[ACTION:PROPOSE:".Length).TrimEnd(']');
				if (string.IsNullOrWhiteSpace(p)) return "";

				var ps = p.Split(new[] { ':' }, 3);
				if (ps.Length < 2) return "";

				var tp = (ps[0] ?? "").Trim().ToUpperInvariant();
				var tn = (ps[1] ?? "").Trim();
				var dr = ps.Length >= 3 ? (ps[2] ?? "").Trim().ToUpperInvariant() : "";

				if (!((IList<string>)ProposeTypes).Contains(tp)) return "";

				return ExecutePropose(npc, tp, tn, dr);
			}
			catch (Exception ex)
			{
				Logger.Log("ProposeAgenda", "Tag: " + ex.Message);
				return "";
			}
		}

		private static string ExecutePropose(Hero npc, string type, string tgt, string dir)
		{
			try
			{
				Clan cl = npc?.Clan;
				Kingdom kd = cl?.Kingdom;
				if (cl == null || kd == null || cl.IsUnderMercenaryService || npc != cl.Leader)
					return "";

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
						bool ia = kd.ActivePolicies.Contains(po);
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
						d = new StartAllianceDecision(cl, (Kingdom)tg);
						if (cl.Influence < d.GetInfluenceCost(cl)) return "";
						break;
					}
					case "TRADE":
					{
						var tg = FindFactionByName(tgt);
						if (tg == null || !tg.IsKingdomFaction) return "";
						if (FactionManager.IsAtWarAgainstFaction(kd, tg)) return "";
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

				if (d == null) return "";
			string newTitle = d.GetGeneralTitle()?.ToString();
			bool isDuplicate = false;
			foreach (KingdomDecision existing in kd.UnresolvedDecisions)
			{
				if (existing != null && existing.GetType() == d.GetType() && existing.GetGeneralTitle()?.ToString() == newTitle)
				{ isDuplicate = true; break; }
			}
			if (isDuplicate) { Logger.Log("ProposeAgenda", "Duplicate: " + type + " " + tgt); return ""; }
			kd.AddDecision(d, false);
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

		public static string BuildProposePostprocessContext(Hero npc)
		{
			try
			{
				if (!CanPropose(npc)) return "";
				var sb = new StringBuilder();
				sb.AppendLine("【议程提议后处理】同意后输出[ACTION:PROPOSE:类型:目标名:方向]。类型=WAR/PEACE/POLICY/EXPEL/ALLIANCE/TRADE/FIEF。方向仅POLICY用ADOPT/ABOLISH。");
				return sb.ToString().TrimEnd();
			}
			catch { return ""; }
		}

		public static bool CanPropose(Hero npc)
		{
			try
			{
				var cl = npc?.Clan;
				return cl != null && cl.Kingdom != null && !cl.IsUnderMercenaryService && npc == cl.Leader;
			}
			catch { return false; }
		}

		public static string BuildProposeRuntimeInstruction(Hero npc)
		{
			try
			{
				Clan cl = npc?.Clan;
				Kingdom kd = cl?.Kingdom;
				string pn = MyBehavior.BuildPlayerPublicDisplayNameForExternal() ?? "玩家";
				var tk = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["playerName"] = pn };

				string sk = kd == null ? "no_kingdom"
					: cl.IsUnderMercenaryService ? "mercenary"
					: npc != cl.Leader ? "not_clan_leader" : "";

				var rl = new List<string>();

				if (!string.IsNullOrWhiteSpace(sk))
				{
					var st = AIConfigHandler.ResolveRuleRuntimeText(
						"propose_agenda", sk, forConstraint: false, tokens: tk);
					if (!string.IsNullOrWhiteSpace(st)) rl.Add(st);
					return string.Join("\n", rl);
				}

				if (kd != null && !cl.IsUnderMercenaryService && npc == cl.Leader)
				{
					int ti = 6;
					try { ti = RewardSystemBehavior.GetTrustLevelIndex(RewardSystemBehavior.Instance?.GetEffectiveTrust(npc) ?? 0); } catch { }

					var tt = AIConfigHandler.ResolveRuleRuntimeText(
						"propose_agenda", "level_" + ti, forConstraint: false, tokens: tk);
					if (!string.IsNullOrWhiteSpace(tt)) rl.Add(tt);

					if (kd.RulingClan?.Leader == npc)
					{
						var kt = AIConfigHandler.ResolveRuleRuntimeText(
							"propose_agenda", "is_king", forConstraint: false, tokens: tk);
						if (!string.IsNullOrWhiteSpace(kt)) rl.Add(kt);
					}
				}

				return string.Join("\n", rl);
			}
			catch (Exception ex)
			{
				Logger.Log("ProposeAgenda", "RT: " + ex.Message);
				return "";
			}
		}
	}
}

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge
{
internal static class DiplomacyCrossDomainActionOwner
{
	private static readonly Regex VassalageSubmitTag = new Regex("\\[ACTION:VASSALAGE:SUBMIT:(TRIBUTARY|GARRISON|VASSAL|MILITARY|PROTECTORATE):([a-zA-Z0-9_\\-]+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private static readonly Regex AnyVassalageTag = new Regex("\\[ACTION:VASSALAGE:[^\\]\\r\\n]*\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private static readonly Regex KingdomAnnexTag = new Regex("\\[ACTION:KINGDOM_ANNEX:target_kingdom_id=([a-zA-Z0-9_\\-]+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);
	private static readonly Regex AnyKingdomAnnexTag = new Regex("\\[ACTION:KINGDOM_ANNEX:[^\\]\\r\\n]*\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	internal static bool ApplyVassalageRewardTags(Hero giver, Hero receiver, ref string responseText, List<string> giverFacts, List<string> receiverFacts)
	{
		bool anyVassalageApplied = false;
		responseText = VassalageSubmitTag.Replace(responseText, delegate(Match m)
		{
			string typeToken = (m.Groups[1].Value ?? "").Trim();
			string kingdomToken = (m.Groups[2].Value ?? "").Trim();
			VassalageDiagnosticLog.Event("reward_tags.vassalage_submit.matched", new Dictionary<string, object>
			{
				["tag"] = m.Value ?? "",
				["giver"] = VassalageDiagnosticLog.DescribeHero(giver),
				["receiver"] = VassalageDiagnosticLog.DescribeHero(receiver),
				["receiverIsMainHero"] = receiver == Hero.MainHero,
				["giverIsMainHero"] = giver == Hero.MainHero,
				["typeToken"] = typeToken,
				["kingdomToken"] = kingdomToken
			});
			if (receiver == Hero.MainHero && giver != Hero.MainHero)
			{
				string statusText = "";
				bool flag2 = false;
				VassalageBehavior vassalageBehavior = VassalageBehavior.Instance;
				if (vassalageBehavior != null)
				{
					flag2 = vassalageBehavior.TryApplyVassalageAction(giver, "SUBMIT", typeToken, kingdomToken, out statusText);
				}
				else
				{
					statusText = "臣属条款未执行：臣属国系统尚未初始化。";
				}
				VassalageDiagnosticLog.Event("reward_tags.vassalage_submit.applied", new Dictionary<string, object>
				{
					["ok"] = flag2,
					["tag"] = m.Value ?? "",
					["giver"] = VassalageDiagnosticLog.DescribeHero(giver),
					["receiver"] = VassalageDiagnosticLog.DescribeHero(receiver),
					["typeToken"] = typeToken,
					["kingdomToken"] = kingdomToken,
					["statusText"] = statusText
				});
				if (!string.IsNullOrWhiteSpace(statusText))
				{
					if (flag2)
					{
						anyVassalageApplied = true;
					}
					giverFacts.Add(statusText);
					receiverFacts.Add(statusText);
					InformationManager.DisplayMessage(new InformationMessage((flag2 ? "【臣属国条约】" : "【臣属国条约失败】") + statusText, flag2 ? Color.FromUint(4278242559u) : Color.FromUint(4294936661u)));
				}
			}
			else
			{
				VassalageDiagnosticLog.Event("reward_tags.vassalage_submit.skipped", new Dictionary<string, object>
				{
					["reason"] = "not_npc_to_main_hero",
					["tag"] = m.Value ?? "",
					["giver"] = VassalageDiagnosticLog.DescribeHero(giver),
					["receiver"] = VassalageDiagnosticLog.DescribeHero(receiver)
				});
			}
			return string.Empty;
		});
		responseText = AnyVassalageTag.Replace(responseText, delegate(Match m)
		{
			VassalageDiagnosticLog.Event("reward_tags.vassalage_unsupported.matched", new Dictionary<string, object>
			{
				["tag"] = m.Value ?? "",
				["giver"] = VassalageDiagnosticLog.DescribeHero(giver),
				["receiver"] = VassalageDiagnosticLog.DescribeHero(receiver)
			});
			if (receiver == Hero.MainHero && giver != Hero.MainHero)
			{
				string statusText = "臣属条款未执行：不支持该 VASSALAGE 动作。";
				giverFacts.Add(statusText);
				receiverFacts.Add(statusText);
				InformationManager.DisplayMessage(new InformationMessage("【臣属国条约失败】" + statusText, Color.FromUint(4294936661u)));
				Logger.Log("Vassalage", "Unsupported tag ignored: " + (m.Value ?? ""));
			}
			return string.Empty;
		});
		return anyVassalageApplied;
	}

	internal static bool ApplyKingdomAnnexationRewardTags(Hero giver, Hero receiver, ref string responseText, List<string> giverFacts, List<string> receiverFacts)
	{
		bool anyKingdomAnnexationApplied = false;
		responseText = KingdomAnnexTag.Replace(responseText, delegate(Match m)
		{
			string kingdomToken = (m.Groups[1].Value ?? "").Trim();
			Logger.Obs("KingdomAnnexation", "reward_tags.matched", new Dictionary<string, object>
			{
				["tag"] = m.Value ?? "",
				["giverId"] = giver?.StringId ?? "",
				["receiverId"] = receiver?.StringId ?? "",
				["targetKingdomId"] = kingdomToken
			});
			KingdomAnnexationDiagnosticLog.Event("reward_tags.matched", new Dictionary<string, object>
			{
				["tag"] = m.Value ?? "",
				["giver"] = KingdomAnnexationDiagnosticLog.DescribeHero(giver),
				["receiver"] = KingdomAnnexationDiagnosticLog.DescribeHero(receiver),
				["targetKingdomId"] = kingdomToken
			});
			if (receiver == Hero.MainHero && giver != Hero.MainHero)
			{
				string statusText = "";
				bool flag2 = KingdomAnnexationBehavior.Instance?.TryApplyKingdomAnnexation(giver, kingdomToken, out statusText) ?? false;
				Logger.Obs("KingdomAnnexation", "reward_tags.applied", new Dictionary<string, object>
				{
					["ok"] = flag2,
					["tag"] = m.Value ?? "",
					["giverId"] = giver?.StringId ?? "",
					["receiverId"] = receiver?.StringId ?? "",
					["targetKingdomId"] = kingdomToken,
					["statusText"] = statusText
				});
				KingdomAnnexationDiagnosticLog.Event("reward_tags.applied", new Dictionary<string, object>
				{
					["ok"] = flag2,
					["tag"] = m.Value ?? "",
					["giver"] = KingdomAnnexationDiagnosticLog.DescribeHero(giver),
					["receiver"] = KingdomAnnexationDiagnosticLog.DescribeHero(receiver),
					["targetKingdomId"] = kingdomToken,
					["statusText"] = statusText
				});
				if (string.IsNullOrWhiteSpace(statusText) && KingdomAnnexationBehavior.Instance == null)
				{
					statusText = "国家吞并未执行：吞并系统尚未初始化。";
				}
				if (!string.IsNullOrWhiteSpace(statusText))
				{
					if (flag2)
					{
						anyKingdomAnnexationApplied = true;
					}
					giverFacts.Add(statusText);
					receiverFacts.Add(statusText);
					InformationManager.DisplayMessage(new InformationMessage((flag2 ? "【国家吞并】" : "【国家吞并失败】") + statusText, flag2 ? Color.FromUint(4278242559u) : Color.FromUint(4294936661u)));
				}
			}
			return string.Empty;
		});
		responseText = AnyKingdomAnnexTag.Replace(responseText, delegate(Match m)
		{
			Logger.Log("KingdomAnnexation", "Unsupported tag ignored: " + (m.Value ?? ""));
			KingdomAnnexationDiagnosticLog.Event("reward_tags.unsupported", new Dictionary<string, object>
			{
				["tag"] = m.Value ?? "",
				["giver"] = KingdomAnnexationDiagnosticLog.DescribeHero(giver),
				["receiver"] = KingdomAnnexationDiagnosticLog.DescribeHero(receiver)
			});
			if (receiver == Hero.MainHero && giver != Hero.MainHero)
			{
				string statusText = "国家吞并未执行：不支持该 KINGDOM_ANNEX 标签格式。";
				giverFacts.Add(statusText);
				receiverFacts.Add(statusText);
				InformationManager.DisplayMessage(new InformationMessage("【国家吞并失败】" + statusText, Color.FromUint(4294936661u)));
			}
			return string.Empty;
		});
		return anyKingdomAnnexationApplied;
	}
}
}

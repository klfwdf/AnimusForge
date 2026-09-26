using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.CampaignSystem.Election;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace AnimusForge
{
	public partial class DiplomacyBehavior : CampaignBehaviorBase
	{
		public static DiplomacyBehavior Instance { get; private set; }

		private static bool s_globalPatchesApplied;

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
				Harmony harmony = new Harmony("com.AnimusForge.diplomacy");
				harmony.Patch(
					typeof(MyBehavior).GetMethod("BuildShoutPromptContextForExternal",
						BindingFlags.Public | BindingFlags.Static),
					postfix: new HarmonyMethod(typeof(DiplomacyBehavior), nameof(Patch_BuildDiplomacyContext_Postfix)));
				Logger.Log("DiplomacyBehavior", "[Harmony] Diplomacy context injection applied.");
			}
			catch (Exception ex)
			{
				Logger.Log("DiplomacyBehavior", $"[Harmony Error] {ex.Message}");
			}
		}

		public override void RegisterEvents()
		{
			ApplyGlobalPatchesOnce();
			Instance = this;
			Logger.Log("DiplomacyBehavior", "[Lifecycle] Registered.");
		}

		public override void SyncData(IDataStore dataStore)
		{
		}

		public static void ProcessDiplomacyTagsDispatch(Hero npc, ref string text)
		{
			if (npc == null || string.IsNullOrEmpty(text)) return;
			if (text.IndexOf("DIPLOMACY", StringComparison.OrdinalIgnoreCase) < 0) return;

			DiplomacyBehavior behavior = Instance
				?? Campaign.Current?.GetCampaignBehavior<DiplomacyBehavior>();
			if (behavior == null)
			{
				Logger.Log("DiplomacyBehavior", "[Dispatch] Instance is null, abort.");
				return;
			}
			behavior.ProcessDiplomacyTags(npc, ref text);
		}

		private static readonly Regex DiplomacyTagRegex = new Regex(
			@"\[ACTION:DIPLOMACY:([A-Z_]+)(?::([^\]]+))?\]",
			RegexOptions.IgnoreCase | RegexOptions.Compiled);

		private const string IndependentClanPeaceActionName = "INDEPENDENT_CLAN_PEACE";

		internal const string IndependentClanPeaceTag = "[ACTION:DIPLOMACY:INDEPENDENT_CLAN_PEACE]";

		private void ProcessDiplomacyTags(Hero npc, ref string responseText)
		{
			int matchCount = 0;
			responseText = DiplomacyTagRegex.Replace(responseText, match =>
			{
				matchCount++;
				return ProcessSingleDiplomacyTag(npc, match.Groups[1].Value, match.Groups[2].Value);
			});
			if (matchCount > 0)
			{
				responseText = DiplomacyTagRegex.Replace(responseText, "");
				responseText = responseText.Trim();
			}
		}

		private string ProcessSingleDiplomacyTag(Hero npc, string action, string payload)
		{
			try
			{
				Logger.Log("DiplomacyBehavior", $"[Tag] action={action} payload={payload} npc={npc.StringId}");
				switch (action.ToUpperInvariant())
				{
					case "DECLARE_WAR":    return TryExecuteDeclareWar(npc, payload);
					case "MAKE_PEACE":     return TryExecuteMakePeace(npc, payload);
					case IndependentClanPeaceActionName: return TryExecuteIndependentClanPeace(npc, payload);
					case "FORM_ALLIANCE":  return TryExecuteFormAlliance(npc, payload);
					case "BREAK_ALLIANCE": return TryExecuteBreakAlliance(npc, payload);
					case "MAKE_TRADE":     return TryExecuteMakeTrade(npc, payload);
					case "CANCEL_TRADE":   return TryExecuteCancelTrade(npc, payload);
					default:
						Logger.Log("DiplomacyBehavior", $"[Tag] Unknown action: {action}");
						return "";
				}
			}
			catch (Exception ex)
			{
				Logger.Log("DiplomacyBehavior", $"[Tag Error] action={action}: {ex.Message}");
				return "";
			}
		}

		// ════════════════════════════════════════════════════════ LLM context

		internal static string BuildDiplomacyInstructionContext(Hero npc)
		{
			try
			{
				if (npc == null) return "";
				if (!CanInjectDiplomacyRuleForExternal(npc)) return "";
				Kingdom npcKingdom = npc.Clan?.Kingdom;
				if (npcKingdom == null) return "";
				Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;
				bool playerIsKing = IsPlayerKing();
				bool playerIsIndependentSettlementClan = IsPlayerIndependentSettlementClan();

				StringBuilder sb = new StringBuilder();
				sb.AppendLine();
				sb.AppendLine("【国王外交规则】");
				sb.AppendLine("重要：游戏内84天=一年，21天=一季度，没有月和周的概念。谈论时间请用季度或年。");
				if (playerIsKing)
				{
					sb.AppendLine("你和玩家都是国王，可以讨论宣战、议和、结盟、贸易等外交事务。");
				}
				else
				{
					if (playerIsIndependentSettlementClan)
					{
						sb.AppendLine("玩家尚未建国，但其独立家族占有城镇/城堡，可与国王谈政治承认、停战、互不侵犯、贡金或建国前条件。正式王国同盟、贸易协议和王国和约需建国后。");
						sb.AppendLine(BuildPlayerIndependentSettlementClanContext());
					}
					else
					{
						sb.AppendLine("玩家不是国王，不能签正式王国外交；可谈政治交涉、觐见、承认、停战或建国前条件。");
					}
				}

				List<Kingdom> warTargets = new List<Kingdom>();
				foreach (Kingdom k in Kingdom.All)
				{ if (!k.IsEliminated && k != npcKingdom && FactionManager.IsAtWarAgainstFaction(npcKingdom, k)) warTargets.Add(k); }
				if (playerKingdom != null && playerKingdom != npcKingdom && !playerKingdom.IsEliminated && FactionManager.IsAtWarAgainstFaction(npcKingdom, playerKingdom) && !warTargets.Contains(playerKingdom))
					warTargets.Add(playerKingdom);
				foreach (Kingdom enemy in warTargets) AppendWarStatsBlock(sb, npcKingdom, enemy);

				if (playerKingdom != null && playerKingdom != npcKingdom && !playerKingdom.IsEliminated && !FactionManager.IsAtWarAgainstFaction(npcKingdom, playerKingdom))
				{ sb.AppendLine(); sb.AppendLine($"【与{GetKingdomDisplayName(playerKingdom)}的和平状态】双方目前处于和平状态。"); }

				string annexationInstruction = KingdomAnnexationBehavior.BuildRuntimeAnnexationInstructionForExternal(npc);
				if (!string.IsNullOrWhiteSpace(annexationInstruction))
				{
					sb.AppendLine();
					sb.AppendLine(annexationInstruction);
				}

				return sb.ToString().TrimEnd();
			}
			catch (Exception ex) { Logger.Log("DiplomacyBehavior", $"[BuildInstruction Error] {ex.Message}"); return ""; }
		}

		internal static string BuildIndependentClanPeaceInstructionForExternal(Hero targetHero, CharacterObject targetCharacter = null)
		{
			try
			{
				Hero npc = targetHero ?? targetCharacter?.HeroObject;
				if (!TryResolveIndependentClanPeaceContext(npc, out Clan playerClan, out Kingdom targetKingdom))
				{
					return "";
				}

				Dictionary<string, string> tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
				{
					["playerClanName"] = playerClan.Name?.ToString() ?? playerClan.StringId ?? "玩家家族",
					["playerSettlementCount"] = (playerClan.Settlements?.Count ?? 0).ToString(),
					["targetKingdomName"] = targetKingdom.Name?.ToString() ?? targetKingdom.StringId ?? "目标王国"
				};
				return AIConfigHandler.ResolveRuleRuntimeText("diplomacy", "independent_clan_peace", forConstraint: false, tokens);
			}
			catch (Exception ex)
			{
				Logger.Log("DiplomacyBehavior", "[IndependentClanPeace] Build instruction failed: " + ex.Message);
				return "";
			}
		}

		private static string BuildPlayerIndependentSettlementClanContext()
		{
			try
			{
				Clan playerClan = Clan.PlayerClan ?? Hero.MainHero?.Clan;
				string clanName = playerClan?.Name?.ToString() ?? "玩家家族";
				List<string> fiefs = (playerClan?.Settlements ?? Enumerable.Empty<Settlement>())
					.Where((Settlement x) => x != null && (x.IsTown || x.IsCastle))
					.Select((Settlement x) => x.Name?.ToString() ?? x.StringId ?? "")
					.Where((string x) => !string.IsNullOrWhiteSpace(x))
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.Take(4)
					.ToList();
				string fiefText = fiefs.Count == 0 ? "无明确据点" : string.Join("、", fiefs);
				return "【玩家政治身份】独立有城家族：" + clanName + "；据点：" + fiefText + "。";
			}
			catch
			{
				return "【玩家政治身份】独立有城家族。";
			}
		}

		private static void AppendWarStatsBlock(StringBuilder sb, Kingdom myKingdom, Kingdom enemy)
		{
			var dm = Campaign.Current.Models.DiplomacyModel;
			StanceLink stance = myKingdom.GetStanceWith(enemy);
			sb.AppendLine(); sb.AppendLine($"【与{GetKingdomDisplayName(enemy)}的战争局势】（仅供判断谈判立场，勿在正文逐条朗读）");
			sb.AppendLine($"- 战争已持续：{(int)(stance.WarStartDate.ElapsedDaysUntilNow)} 天");
			sb.AppendLine($"- 你方战争进展分：{dm.GetWarProgressScore(myKingdom, enemy).ResultNumber:F0} / 750");
			sb.AppendLine($"- 敌方战争进展分：{dm.GetWarProgressScore(enemy, myKingdom).ResultNumber:F0} / 750");
			sb.AppendLine($"  你方击杀：{stance.GetCasualties(enemy)}，敌方击杀：{stance.GetCasualties(myKingdom)}");
			sb.AppendLine($"  你方占城：{stance.GetSuccessfulTownSieges(myKingdom)}城{stance.GetSuccessfulSieges(myKingdom)-stance.GetSuccessfulTownSieges(myKingdom)}堡");
			sb.AppendLine($"- 你方总战力：{myKingdom.CurrentTotalStrength:F0}，敌方总战力：{enemy.CurrentTotalStrength:F0}");

			float enemyProsperity = enemy.Fiefs.Sum(x => x.Prosperity);
			sb.AppendLine($"- 敌方繁荣度：{enemyProsperity:F0}，参考贡金范围 0 ~ {(int)(enemyProsperity*0.15f*0.35f)} 第纳尔/天（仅供谈判参考，不是系统上限；双方可商定任意非负整数金额）");

			int myOtherEnemies = 0; float myOtherStr = 0;
			foreach (Kingdom k in Kingdom.All)
			{ if (k != enemy && !k.IsEliminated && k != myKingdom && FactionManager.IsAtWarAgainstFaction(myKingdom, k)) { myOtherEnemies++; myOtherStr += k.CurrentTotalStrength; } }
			if (myOtherEnemies > 0) sb.AppendLine($"- 多线作战：同时与 {myOtherEnemies} 个敌人交战（总战力 {myOtherStr:F0}）");

			float diff = dm.GetWarProgressScore(myKingdom, enemy).ResultNumber - dm.GetWarProgressScore(enemy, myKingdom).ResultNumber;
			if (diff > 100) sb.AppendLine($"- 【谈判立场】你方明显占优");
			else if (diff < -100) sb.AppendLine($"- 【谈判立场】你方明显劣势");
			else sb.AppendLine($"- 【谈判立场】双方大体持平");
		}

		internal static string BuildDiplomacyPostprocessContext(Hero npc)
		{
			try
			{
				if (npc == null) return "";
				if (TryResolveIndependentClanPeaceContext(npc, out Clan playerClan, out Kingdom targetKingdom))
				{
					StringBuilder independent = new StringBuilder();
					independent.AppendLine("【独立家族议和运行时事实】");
					independent.AppendLine("玩家家族“" + (playerClan.Name?.ToString() ?? "玩家家族") + "”是独立家族；定居点数：" + (playerClan.Settlements?.Count ?? 0));
					independent.AppendLine("你是敌对王国“" + (targetKingdom.Name?.ToString() ?? "目标王国") + "”的当前国王。");
					independent.AppendLine("双方当前敌对：是");
					return independent.ToString().TrimEnd();
				}
				bool allowFullDiplomacy = CanUseFullDiplomacyActionPostprocessForExternal(npc);
				bool allowNpcDeclareWar = CanUseNpcSovereignDeclareWarPostprocessForExternal(npc);
				if (!allowFullDiplomacy && !allowNpcDeclareWar) return "";
				Kingdom npcKingdom = npc.Clan?.Kingdom;
				if (npcKingdom == null) return "";
				Kingdom playerKingdom = Clan.PlayerClan?.Kingdom;

				StringBuilder sb = new StringBuilder();
				sb.AppendLine(); sb.AppendLine("【外交后处理标签】");
				sb.AppendLine("天数说明：21天=一季度，84天=一年，没有月和周概念。");
				sb.AppendLine("【王国ID对照表】");
				foreach (Kingdom k in Kingdom.All) { if (!k.IsEliminated) sb.AppendLine($"  {k.StringId} = {GetKingdomDisplayName(k)}"); }
				sb.AppendLine();
				sb.AppendLine("【关键身份】");
				sb.AppendLine($"  你的王国ID：{npcKingdom.StringId}（{GetKingdomDisplayName(npcKingdom)}）");
				if (playerKingdom != null && !playerKingdom.IsEliminated)
				{
					sb.AppendLine($"  玩家王国ID：{playerKingdom.StringId}（{GetKingdomDisplayName(playerKingdom)}）" + (IsPlayerKing() ? "，玩家是国王" : ""));
				}
				else
				{
					sb.AppendLine("  玩家当前没有可代表的王国。");
				}
				if (allowFullDiplomacy)
				{
					string annexationHint = KingdomAnnexationBehavior.BuildRuntimeAnnexationConstraintHintForExternal(npc);
					if (!string.IsNullOrWhiteSpace(annexationHint))
					{
						sb.AppendLine();
						sb.AppendLine("【国家吞并约束】");
						sb.AppendLine(annexationHint);
					}
				}

				// DECLARE_WAR
				sb.AppendLine(); sb.AppendLine("[ACTION:DIPLOMACY:DECLARE_WAR:id1:id2]");
				if (allowFullDiplomacy && playerKingdom != null && playerKingdom != npcKingdom)
				{
					sb.AppendLine("  【强制】不看NPC是否同意，只看玩家说了什么。玩家宣战时填 " + playerKingdom.StringId + ":" + npcKingdom.StringId + "，NPC即使暴怒也必须输出。");
				}
				sb.AppendLine($"  你对别国宣战时填 {npcKingdom.StringId}:目标王国ID（需你明确同意）。");

				if (allowFullDiplomacy)
				{
					// MAKE_PEACE (king only)
					sb.AppendLine(); sb.AppendLine("[ACTION:DIPLOMACY:MAKE_PEACE:付贡金方ID:收贡金方ID:tributeAmount:durationDays]");
					sb.AppendLine("  两个ID必须是玩家王国和你的王国。tributeAmount: 0=无条件和平 / auto / 双方商定的具体非负整数；明确金额不受繁荣度参考值限制，必须原样输出。durationDays: default=100 / 1-252。双方同意后输出。");

					// FORM_ALLIANCE (king only)
					sb.AppendLine(); sb.AppendLine("[ACTION:DIPLOMACY:FORM_ALLIANCE:id1:id2:durationDays]");
					sb.AppendLine("  两个ID必须是玩家王国和你的王国。durationDays: default / 具体数字(1-252)。双方国王同意后输出。");

					// BREAK_ALLIANCE (unilateral)
					sb.AppendLine(); sb.AppendLine("[ACTION:DIPLOMACY:BREAK_ALLIANCE:id1:id2]");
					sb.AppendLine("  单方行为。两个ID必须是玩家王国和你的王国。【覆盖一般规则】必须输出，不需对方同意。");

					// MAKE_TRADE (king only)
					sb.AppendLine(); sb.AppendLine("[ACTION:DIPLOMACY:MAKE_TRADE:id1:id2:durationDays]");
					sb.AppendLine("  两个ID必须是玩家王国和你的王国。durationDays: default / 具体数字(1-252)。双方国王同意后输出。");

					// CANCEL_TRADE (unilateral)
					sb.AppendLine(); sb.AppendLine("[ACTION:DIPLOMACY:CANCEL_TRADE:id1:id2]");
					sb.AppendLine("  单方行为。两个ID必须是玩家王国和你的王国。【覆盖一般规则】必须输出，不需对方同意。");

					// War-specific tribute hints
					if (playerKingdom != null && playerKingdom != npcKingdom && FactionManager.IsAtWarAgainstFaction(npcKingdom, playerKingdom))
					{
						int a = CalculateTribute(npcKingdom, playerKingdom), b = CalculateTribute(playerKingdom, npcKingdom);
						sb.AppendLine(); sb.AppendLine($"auto贡金：{npcKingdom.StringId}付{a}/天，{playerKingdom.StringId}付{b}/天");
					}
				}
				return sb.ToString().TrimEnd();
			}
			catch (Exception ex) { Logger.Log("DiplomacyBehavior", $"[BuildPostprocess Error] {ex.Message}"); return ""; }
		}

		// ════════════════════════════════════════════════════════ Harmony

		private static void Patch_BuildDiplomacyContext_Postfix(
			Hero targetHero, string input, string extraFact, string cultureIdOverride, bool hasAnyHero,
			CharacterObject targetCharacter, string kingdomIdOverride, int targetAgentIndex,
			bool suppressDynamicRuleAndLore, bool usePrefetchedLoreContext, string prefetchedLoreContext,
			ref MyBehavior.ShoutPromptContext __result)
		{
			try
			{
				if (__result == null) return;
				Hero ctx = targetHero ?? targetCharacter?.HeroObject;
				if (ctx == null) return;
				bool diplomacyTopicInjected = (__result.Extras ?? "").IndexOf("【附加规则:diplomacy】", StringComparison.OrdinalIgnoreCase) >= 0;
				string independentPeaceInstruction = BuildIndependentClanPeaceInstructionForExternal(ctx, targetCharacter);
				if (!diplomacyTopicInjected && string.IsNullOrWhiteSpace(independentPeaceInstruction)) return;

				if (diplomacyTopicInjected && CanInjectDiplomacyRuleForExternal(ctx, targetCharacter))
				{
					string ins = BuildDiplomacyInstructionContext(ctx);
					if (!string.IsNullOrWhiteSpace(ins)) __result.Extras = (__result.Extras ?? "") + "\n" + ins;
					string trustIns = BuildDiplomacyRuntimeInstruction(ctx);
					if (!string.IsNullOrWhiteSpace(trustIns)) __result.Extras = (__result.Extras ?? "") + "\n" + trustIns;
				}

				if (!string.IsNullOrWhiteSpace(independentPeaceInstruction))
				{
					__result.Extras = (__result.Extras ?? "") + "\n" + independentPeaceInstruction;
					Logger.Log("DiplomacyBehavior", "[IndependentClanPeace] resident main prompt injected npc=" + ctx.StringId);
				}
			}
			catch (Exception ex) { Logger.Log("DiplomacyBehavior", $"[PatchContext Error] {ex.Message}"); }
		}

		private static string BuildDiplomacyRuntimeInstruction(Hero npc)
		{
			try
			{
				if (npc == null) return "";
				if (!CanInjectDiplomacyRuleForExternal(npc)) return "";
				Clan clan = npc.Clan;
				Kingdom kingdom = clan?.Kingdom;
				string playerName = MyBehavior.BuildPlayerPublicDisplayNameForExternal();
				if (string.IsNullOrWhiteSpace(playerName)) playerName = "玩家";
				var tokens = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["playerName"] = playerName };

				// Determine state
				string stateKey = "";
				if (kingdom == null) { stateKey = "no_kingdom"; }
				else if (clan.IsUnderMercenaryService) { stateKey = "mercenary"; }
				else if (npc != kingdom.RulingClan?.Leader) { stateKey = "not_king"; }
				else if (!IsPlayerKing())
				{
					stateKey = IsPlayerIndependentSettlementClan() ? "player_independent_settlement_clan" : "player_not_king";
				}

				if (!string.IsNullOrWhiteSpace(stateKey))
				{
					string stateTemplate = AIConfigHandler.ResolveRuleRuntimeText("diplomacy", stateKey, forConstraint: false, tokens);
					if (!string.IsNullOrWhiteSpace(stateTemplate)) return stateTemplate;
					if (stateKey == "no_kingdom" || stateKey == "mercenary" || stateKey == "not_king" || stateKey == "player_not_king")
						return "";
				}

				// Trust level - only for alliance/trade negotiations
				if (kingdom != null && !clan.IsUnderMercenaryService && npc == kingdom.RulingClan?.Leader)
				{
					int trust = RewardSystemBehavior.Instance?.GetEffectiveTrust(npc) ?? 0;
					int trustLevelIndex = RewardSystemBehavior.GetTrustLevelIndex(trust);
					string trustTemplate = AIConfigHandler.ResolveRuleRuntimeText("diplomacy", "level_" + trustLevelIndex, forConstraint: false, tokens);
					if (!string.IsNullOrWhiteSpace(trustTemplate)) return trustTemplate;
				}

				return "";
			}
			catch (Exception ex) { Logger.Log("DiplomacyBehavior", $"[RuntimeInstruction Error] {ex.Message}"); return ""; }
		}
	}
}

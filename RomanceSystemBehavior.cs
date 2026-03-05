using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

public class RomanceSystemBehavior : CampaignBehaviorBase
{
	private const int LoveMin = -100;

	private const int LoveMax = 100;

	private const int BridePriceMin = 100000;

	private const int BridePriceMax = 2000000;

	private static readonly string[] LoveLevelTexts = new string[10] { "极度排斥", "明显反感", "疏离警惕", "保持距离", "态度保留", "普通往来", "有些好感", "明显心动", "深度依恋", "非你不可" };

	private static readonly Regex LoveDeltaRegex = new Regex("\\[ACTION:LOVE_DELTA:([^\\]:]+):([+\\-]?\\d+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex MarriageFormalRegex = new Regex("\\[ACTION:MARRIAGE_FORMAL:([^\\]:]+)(?::(\\d+))?\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private static readonly Regex MarriageElopeRegex = new Regex("\\[ACTION:MARRIAGE_ELOPE:([^\\]:]+)\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	private Dictionary<string, int> _privateLove = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

	public static RomanceSystemBehavior Instance { get; private set; }

	public RomanceSystemBehavior()
	{
		Instance = this;
	}

	public override void RegisterEvents()
	{
	}

	public override void SyncData(IDataStore dataStore)
	{
		if (_privateLove == null)
		{
			_privateLove = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		dataStore.SyncData("_romancePrivateLove_v1", ref _privateLove);
		if (_privateLove == null)
		{
			_privateLove = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		List<string> list = _privateLove.Keys.ToList();
		for (int i = 0; i < list.Count; i++)
		{
			string key = list[i];
			if (string.IsNullOrWhiteSpace(key))
			{
				_privateLove.Remove(key);
				continue;
			}
			int value = ClampLove(_privateLove[key]);
			if (value == 0)
			{
				_privateLove.Remove(key);
			}
			else
			{
				_privateLove[key] = value;
			}
		}
	}

	private static int ClampLove(int value)
	{
		if (value < -100)
		{
			value = -100;
		}
		if (value > 100)
		{
			value = 100;
		}
		return value;
	}

	private static int ToLoveLevelIndex(int value)
	{
		double num = ((double)ClampLove(value) + 100.0) / 200.0;
		int num2 = (int)Math.Floor(num * 10.0) + 1;
		if (num2 < 1)
		{
			num2 = 1;
		}
		if (num2 > 10)
		{
			num2 = 10;
		}
		return num2;
	}

	private static string BuildLoveKey(Hero hero)
	{
		string text = (hero?.StringId ?? "").Trim().ToLowerInvariant();
		if (string.IsNullOrWhiteSpace(text))
		{
			return "";
		}
		return "hero:" + text;
	}

	public static string GetPrivateLoveLevelText(int value)
	{
		return LoveLevelTexts[ToLoveLevelIndex(value) - 1];
	}

	public int GetPrivateLove(Hero hero)
	{
		if (hero == null)
		{
			return 0;
		}
		string text = BuildLoveKey(hero);
		if (string.IsNullOrWhiteSpace(text))
		{
			return 0;
		}
		if (_privateLove == null)
		{
			_privateLove = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		if (_privateLove.TryGetValue(text, out var value))
		{
			return ClampLove(value);
		}
		return 0;
	}

	public void SetPrivateLove(Hero hero, int value, string reason)
	{
		if (hero == null)
		{
			return;
		}
		string text = BuildLoveKey(hero);
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		if (_privateLove == null)
		{
			_privateLove = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
		}
		int privateLove = GetPrivateLove(hero);
		int num = ClampLove(value);
		if (num == 0)
		{
			_privateLove.Remove(text);
		}
		else
		{
			_privateLove[text] = num;
		}
		Logger.Log("Romance", $"hero={hero.StringId} reason={reason} love={privateLove}->{num}");
	}

	public void AdjustPrivateLove(Hero hero, int delta, string reason)
	{
		if (hero == null || delta == 0)
		{
			return;
		}
		SetPrivateLove(hero, GetPrivateLove(hero) + delta, reason);
	}

	private static int GetRelationSafe(Hero left, Hero right)
	{
		try
		{
			if (left == null || right == null)
			{
				return 0;
			}
			return left.GetRelation(right);
		}
		catch
		{
			return 0;
		}
	}

	private static bool IsMarriageableHero(Hero hero)
	{
		if (hero == null)
		{
			return false;
		}
		try
		{
			if (hero.IsPrisoner)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (hero.Age < 18f)
			{
				return false;
			}
		}
		catch
		{
		}
		try
		{
			if (hero.Spouse != null)
			{
				return false;
			}
		}
		catch
		{
		}
		return true;
	}

	private static bool CanPlayerMarryTarget(Hero targetHero, out string reason)
	{
		reason = "";
		Hero mainHero = Hero.MainHero;
		if (mainHero == null || targetHero == null)
		{
			reason = "未找到婚配双方。";
			return false;
		}
		if (mainHero == targetHero)
		{
			reason = "不能和自己结婚。";
			return false;
		}
		if (!IsMarriageableHero(mainHero))
		{
			reason = "玩家当前状态不满足结婚条件（可能已婚、未成年或被囚）。";
			return false;
		}
		if (!IsMarriageableHero(targetHero))
		{
			reason = "目标当前状态不满足结婚条件（可能已婚、未成年或被囚）。";
			return false;
		}
		return true;
	}

	private static int TransferGold(Hero giver, Hero receiver, int amount)
	{
		try
		{
			if (giver == null || receiver == null || amount <= 0)
			{
				return 0;
			}
			int num = Math.Min(amount, Math.Max(0, giver.Gold));
			if (num <= 0)
			{
				return 0;
			}
			giver.ChangeHeroGold(-num);
			receiver.ChangeHeroGold(num);
			return num;
		}
		catch
		{
			return 0;
		}
	}

	private static bool TryApplyMarriageAction(Hero left, Hero right, out string failReason)
	{
		failReason = "";
		try
		{
			Type type = typeof(ChangeRelationAction).Assembly.GetType("TaleWorlds.CampaignSystem.Actions.MarriageAction");
			if (type == null)
			{
				failReason = "未找到原版 MarriageAction。";
				return false;
			}
			MethodInfo method = type.GetMethod("Apply", BindingFlags.Public | BindingFlags.Static, null, new Type[3]
			{
				typeof(Hero),
				typeof(Hero),
				typeof(bool)
			}, null);
			if (method != null)
			{
				method.Invoke(null, new object[3] { left, right, true });
				return true;
			}
			MethodInfo method2 = type.GetMethod("Apply", BindingFlags.Public | BindingFlags.Static, null, new Type[2]
			{
				typeof(Hero),
				typeof(Hero)
			}, null);
			if (method2 != null)
			{
				method2.Invoke(null, new object[2] { left, right });
				return true;
			}
			failReason = "MarriageAction.Apply 签名不匹配。";
			return false;
		}
		catch (Exception ex)
		{
			failReason = ex.Message;
			return false;
		}
	}

	private static IEnumerable<Hero> GetClanMembersCompat(Clan clan)
	{
		if (clan == null)
		{
			yield break;
		}
		IEnumerable enumerable = null;
		try
		{
			PropertyInfo property = clan.GetType().GetProperty("Lords", BindingFlags.Instance | BindingFlags.Public);
			if (property != null)
			{
				enumerable = property.GetValue(clan, null) as IEnumerable;
			}
			if (enumerable == null)
			{
				PropertyInfo property2 = clan.GetType().GetProperty("Heroes", BindingFlags.Instance | BindingFlags.Public);
				if (property2 != null)
				{
					enumerable = property2.GetValue(clan, null) as IEnumerable;
				}
			}
		}
		catch
		{
			enumerable = null;
		}
		if (enumerable == null)
		{
			yield break;
		}
		foreach (object item in enumerable)
		{
			if (item is Hero hero && hero != null)
			{
				yield return hero;
			}
		}
	}

	public int ComputeTargetFamilyHarmony(Hero target)
	{
		try
		{
			if (target == null || target.Clan == null)
			{
				return 0;
			}
			Hero leader = target.Clan.Leader;
			int relationSafe = GetRelationSafe(target, leader);
			if (leader == target)
			{
				relationSafe = 40;
			}
			List<int> list = new List<int>();
			if (target.Spouse != null && target.Spouse != target && target.Spouse.Clan == target.Clan)
			{
				list.Add(GetRelationSafe(target, target.Spouse));
			}
			if (target.Father != null && target.Father != target && target.Father.Clan == target.Clan)
			{
				list.Add(GetRelationSafe(target, target.Father));
			}
			if (target.Mother != null && target.Mother != target && target.Mother.Clan == target.Clan)
			{
				list.Add(GetRelationSafe(target, target.Mother));
			}
			double num = ((list.Count > 0) ? list.Average() : 0.0);
			List<int> list2 = new List<int>();
			foreach (Hero clanMembersCompat in GetClanMembersCompat(target.Clan))
			{
				if (clanMembersCompat != null && clanMembersCompat != target)
				{
					if (clanMembersCompat.Age < 18f)
					{
						continue;
					}
					list2.Add(GetRelationSafe(target, clanMembersCompat));
				}
			}
			double num2 = ((list2.Count > 0) ? list2.Average() : 0.0);
			int num3 = (int)Math.Round((double)relationSafe * 0.6 + num * 0.25 + num2 * 0.15);
			if (num3 < -100)
			{
				num3 = -100;
			}
			if (num3 > 100)
			{
				num3 = 100;
			}
			return num3;
		}
		catch
		{
			return 0;
		}
	}

	private Hero ResolveTargetHeroToken(Hero speaker, string token)
	{
		string text = (token ?? "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		if (string.Equals(text, "self", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "current", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "me", StringComparison.OrdinalIgnoreCase))
		{
			return speaker;
		}
		try
		{
			Hero hero = Hero.Find(text);
			if (hero != null)
			{
				return hero;
			}
		}
		catch
		{
		}
		if (speaker != null && !string.IsNullOrWhiteSpace(speaker.Name?.ToString()) && string.Equals(text, speaker.Name.ToString(), StringComparison.OrdinalIgnoreCase))
		{
			return speaker;
		}
		return null;
	}

	private static int GetClanRelationWithPlayer(Hero clanLeader)
	{
		return GetRelationSafe(clanLeader, Hero.MainHero);
	}

	private bool TryExecuteFormalMarriage(Hero speaker, Hero targetHero, int? bridePrice, out string status)
	{
		status = "";
		if (!CanPlayerMarryTarget(targetHero, out var reason))
		{
			status = "正规联姻失败：" + reason;
			return false;
		}
		if (targetHero.Clan == null)
		{
			status = "正规联姻失败：目标没有家族，无法走家族联姻流程。";
			return false;
		}
		Hero leader = targetHero.Clan.Leader;
		if (leader == null)
		{
			status = "正规联姻失败：未找到对方家族族长。";
			return false;
		}
		if (speaker == null || speaker != leader)
		{
			status = "正规联姻失败：必须由目标家族族长亲自同意。";
			return false;
		}
		int num = Hero.MainHero?.Clan?.Tier ?? 0;
		int num2 = targetHero.Clan?.Tier ?? 0;
		int num3 = num - num2;
		if (num3 <= -2)
		{
			status = $"正规联姻失败：家族等级差距过大（玩家={num}，对方={num2}）。";
			return false;
		}
		int clanRelationWithPlayer = GetClanRelationWithPlayer(leader);
		int effectiveTrust = RewardSystemBehavior.Instance?.GetEffectiveTrust(leader) ?? 0;
		if (num3 == -1)
		{
			if (clanRelationWithPlayer < 20 || effectiveTrust < 20)
			{
				status = $"正规联姻失败：门槛不足（家族关系={clanRelationWithPlayer}，族长综合信任={effectiveTrust}；需要 >=20 / >=20）。";
				return false;
			}
			if (!bridePrice.HasValue)
			{
				status = $"正规联姻失败：家族低一级时需明确彩礼金额，范围 {BridePriceMin:N0}~{BridePriceMax:N0} 第纳尔。";
				return false;
			}
			int value = bridePrice.Value;
			if (value < BridePriceMin || value > BridePriceMax)
			{
				status = $"正规联姻失败：彩礼金额超出范围（{BridePriceMin:N0}~{BridePriceMax:N0}）。";
				return false;
			}
			if ((Hero.MainHero?.Gold).GetValueOrDefault() < value)
			{
				status = $"正规联姻失败：玩家金币不足，需 {value:N0}。";
				return false;
			}
		}
		else
		{
			int num4 = ((num3 <= 0) ? 5 : (-5 * num3));
			if (num4 < -20)
			{
				num4 = -20;
			}
			if (clanRelationWithPlayer < num4 || effectiveTrust < num4)
			{
				status = $"正规联姻失败：门槛不足（当前关系={clanRelationWithPlayer}，信任={effectiveTrust}；需要 >= {num4}）。";
				return false;
			}
			if (bridePrice.HasValue && bridePrice.Value > 0 && (Hero.MainHero?.Gold).GetValueOrDefault() < bridePrice.Value)
			{
				status = $"正规联姻失败：玩家金币不足，需 {bridePrice.Value:N0}。";
				return false;
			}
		}
		int num5 = 0;
		if (bridePrice.HasValue && bridePrice.Value > 0)
		{
			num5 = TransferGold(Hero.MainHero, leader, bridePrice.Value);
		}
		if (!TryApplyMarriageAction(Hero.MainHero, targetHero, out var failReason))
		{
			status = "正规联姻失败：执行 MarriageAction 失败，" + failReason;
			return false;
		}
		AdjustPrivateLove(targetHero, 10, "formal_marriage_success");
		status = $"正规联姻成功：你与 {targetHero.Name} 已成婚。等级差D={num3}，家族关系={clanRelationWithPlayer}，族长综合信任={effectiveTrust}。";
		if (num5 > 0)
		{
			status += $" 已支付彩礼 {num5:N0} 第纳尔。";
		}
		return true;
	}

	private bool TryExecuteElopeMarriage(Hero speaker, Hero targetHero, out string status)
	{
		status = "";
		if (!CanPlayerMarryTarget(targetHero, out var reason))
		{
			status = "私奔失败：" + reason;
			return false;
		}
		if (speaker == null || speaker != targetHero)
		{
			status = "私奔失败：必须由结婚对象本人亲口同意。";
			return false;
		}
		Hero leader = targetHero.Clan?.Leader;
		int clanRelationWithPlayer = GetClanRelationWithPlayer(leader);
		int effectiveTrust = RewardSystemBehavior.Instance?.GetEffectiveTrust(leader) ?? 0;
		int npcTrust = RewardSystemBehavior.Instance?.GetNpcTrust(targetHero) ?? 0;
		int privateLove = GetPrivateLove(targetHero);
		int num = 70;
		int num2 = 20;
		if (clanRelationWithPlayer < -20 || effectiveTrust < -20)
		{
			num = 85;
			num2 = 35;
		}
		int num3 = ComputeTargetFamilyHarmony(targetHero);
		if (num3 <= -40)
		{
			num -= 15;
			num2 -= 10;
		}
		else if (num3 <= -20)
		{
			num -= 8;
			num2 -= 5;
		}
		else if (num3 >= 20)
		{
			num += 10;
			num2 += 5;
		}
		if (num < 20)
		{
			num = 20;
		}
		if (num2 < -20)
		{
			num2 = -20;
		}
		if (privateLove < num || npcTrust < num2)
		{
			AdjustPrivateLove(targetHero, -3, "elope_rejected");
			status = $"私奔失败：门槛不足（L={privateLove}/{num}，对象私人信任={npcTrust}/{num2}，对象家族融洽度F={num3}）。";
			return false;
		}
		if (!TryApplyMarriageAction(Hero.MainHero, targetHero, out var failReason))
		{
			status = "私奔失败：执行 MarriageAction 失败，" + failReason;
			return false;
		}
		AdjustPrivateLove(targetHero, 12, "elope_success");
		if (leader != null && leader != targetHero)
		{
			try
			{
				ChangeRelationAction.ApplyRelationChangeBetweenHeroes(Hero.MainHero, leader, -25);
			}
			catch
			{
			}
			RewardSystemBehavior.Instance?.AdjustTrustForExternal(leader, -15, -10, "elope_penalty");
		}
		status = $"私奔成功：你与 {targetHero.Name} 已成婚。当前 L={privateLove}，对象私人信任={npcTrust}，对象家族融洽度F={num3}。";
		return true;
	}

	public string BuildMarriageRuntimeConstraintHint(Hero speaker)
	{
		try
		{
			if (Hero.MainHero == null)
			{
				return "【运行时硬约束】未找到玩家英雄，本轮禁止输出任何结婚标签。";
			}
			Hero hero = speaker;
			if (hero == null)
			{
				try
				{
					hero = Hero.OneToOneConversationHero;
				}
				catch
				{
					hero = null;
				}
			}
			if (hero == null)
			{
				return "【运行时硬约束】未找到当前对话对象，结婚标签可能执行失败。";
			}
			Hero leader = hero.Clan?.Leader;
			int num = Hero.MainHero?.Clan?.Tier ?? 0;
			int num2 = hero.Clan?.Tier ?? 0;
			int num3 = num - num2;
			int clanRelationWithPlayer = GetClanRelationWithPlayer(leader);
			int effectiveTrust = RewardSystemBehavior.Instance?.GetEffectiveTrust(leader) ?? 0;
			int privateLove = GetPrivateLove(hero);
			int npcTrust = RewardSystemBehavior.Instance?.GetNpcTrust(hero) ?? 0;
			int num4 = ComputeTargetFamilyHarmony(hero);
			StringBuilder stringBuilder = new StringBuilder();
			if (leader != null)
			{
				if (leader == hero)
				{
					stringBuilder.AppendLine("【族长信息】你就是该家族族长，可审批正规联姻。");
				}
				else
				{
					stringBuilder.AppendLine("【族长信息】该家族族长是 " + (leader.Name?.ToString() ?? "未知") + "（StringId=" + (leader.StringId ?? "") + "）。你无权审批正规联姻，只能引导玩家去找此人。");
				}
			}
			stringBuilder.AppendLine($"【运行时硬约束】婚姻判定：玩家家族等级={num}，对象家族等级={num2}，D={num3}；族长家族关系R={clanRelationWithPlayer}，族长综合信任T={effectiveTrust}；对象私人关系L={privateLove}（{GetPrivateLoveLevelText(privateLove)}），对象私人信任={npcTrust}，对象家族融洽度F={num4}。");
			stringBuilder.AppendLine("正规联姻：只能由对象家族族长同意，使用 [ACTION:MARRIAGE_FORMAL:targetHeroId]；当 D=-1 时必须用 [ACTION:MARRIAGE_FORMAL:targetHeroId:彩礼金额]，金额范围 100000~2000000。");
			stringBuilder.AppendLine("私奔：只能由结婚对象本人同意，使用 [ACTION:MARRIAGE_ELOPE:targetHeroId]。");
			stringBuilder.Append("若条件不足，你必须拒绝并且禁止输出任何结婚标签。");
			return stringBuilder.ToString().Trim();
		}
		catch
		{
			return "";
		}
	}

	public void ApplyMarriageTags(Hero speaker, Hero receiver, ref string responseText)
	{
		try
		{
			string text = responseText ?? "";
			if (string.IsNullOrWhiteSpace(text))
			{
				return;
			}
			string value = "";
			MatchCollection matchCollection = LoveDeltaRegex.Matches(text);
			for (int i = 0; i < matchCollection.Count; i++)
			{
				Match match = matchCollection[i];
				Hero hero = ResolveTargetHeroToken(speaker, match.Groups[1].Value);
				if (hero != null && int.TryParse(match.Groups[2].Value, out var result))
				{
					if (result < -20)
					{
						result = -20;
					}
					if (result > 20)
					{
						result = 20;
					}
					AdjustPrivateLove(hero, result, "llm_tag");
				}
			}
			MatchCollection matchCollection2 = MarriageFormalRegex.Matches(text);
			for (int j = 0; j < matchCollection2.Count; j++)
			{
				Match match2 = matchCollection2[j];
				Hero hero2 = ResolveTargetHeroToken(speaker, match2.Groups[1].Value);
				int? bridePrice = null;
				if (match2.Groups.Count > 2 && int.TryParse(match2.Groups[2].Value, out var result2))
				{
					bridePrice = result2;
				}
				string status = "";
				if (hero2 == null)
				{
					status = "正规联姻失败：未找到目标对象（targetHeroId）。";
				}
				else if (TryExecuteFormalMarriage(speaker, hero2, bridePrice, out status))
				{
					value = status;
				}
				else
				{
					value = status;
				}
			}
			MatchCollection matchCollection3 = MarriageElopeRegex.Matches(text);
			for (int k = 0; k < matchCollection3.Count; k++)
			{
				Match match3 = matchCollection3[k];
				Hero hero3 = ResolveTargetHeroToken(speaker, match3.Groups[1].Value);
				string status2 = "";
				if (hero3 == null)
				{
					status2 = "私奔失败：未找到目标对象（targetHeroId）。";
				}
				else if (TryExecuteElopeMarriage(speaker, hero3, out status2))
				{
					value = status2;
				}
				else
				{
					value = status2;
				}
			}
			text = LoveDeltaRegex.Replace(text, "");
			text = MarriageFormalRegex.Replace(text, "");
			text = MarriageElopeRegex.Replace(text, "");
			responseText = text.Trim();
			if (!string.IsNullOrWhiteSpace(value))
			{
				InformationManager.DisplayMessage(new InformationMessage("[婚姻系统] " + value, Color.FromUint(4283878655u)));
				Logger.Log("Romance", value);
			}
		}
		catch (Exception ex)
		{
			Logger.Log("Romance", "[ERROR] ApplyMarriageTags 异常: " + ex);
		}
	}
}

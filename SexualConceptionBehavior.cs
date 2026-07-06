using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using HarmonyLib;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.CampaignBehaviors;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace AnimusForge;

public static class SexualConceptionBehavior
{
	public const string IntimacyActionTag = "[ACTION:INTIMACY_INTERNAL]";

	private const float PregnancyChance = 0.15f;

	private static readonly Regex IntimacyTagRegex = new Regex("\\[ACTION:INTIMACY_INTERNAL\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	[ThreadStatic]
	private static Hero _pendingMother;

	[ThreadStatic]
	private static Hero _pendingFather;

	private static bool _pregnancyFatherPatchReady;

	public static void RegisterHarmonyPatches(Harmony harmony)
	{
		if (harmony == null)
		{
			throw new ArgumentNullException(nameof(harmony));
		}
		Type pregnancyType = typeof(PregnancyCampaignBehavior).GetNestedType("Pregnancy", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		ConstructorInfo constructor = pregnancyType?.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[3]
		{
			typeof(Hero),
			typeof(Hero),
			typeof(CampaignTime)
		}, null);
		if (constructor == null)
		{
			throw new MissingMethodException("PregnancyCampaignBehavior.Pregnancy(Hero, Hero, CampaignTime)");
		}
		harmony.Patch(constructor, new HarmonyMethod(typeof(SexualConceptionBehavior), nameof(PregnancyConstructorPrefix)));
		_pregnancyFatherPatchReady = true;
		Logger.Log("SexualConception", "pregnancy father patch ready");
	}

	private static void PregnancyConstructorPrefix(Hero pregnantHero, ref Hero father)
	{
		if (_pendingMother == pregnantHero && _pendingFather != null)
		{
			father = _pendingFather;
		}
	}

	public static List<PostprocessRuleEntry> BuildRuntimePostprocessRules(Hero targetHero, string chainName)
	{
		if (IsCourierChain(chainName) || !TryResolveAdultOppositeSexPair(targetHero, out var _, out var _))
		{
			return new List<PostprocessRuleEntry>();
		}
		return (AIConfigHandler.IntimacyPostprocessRules ?? new List<PostprocessRuleEntry>())
			.Where((PostprocessRuleEntry rule) => string.Equals((rule?.Tag ?? "").Trim(), IntimacyActionTag, StringComparison.OrdinalIgnoreCase))
			.ToList();
	}

	public static string NormalizePostprocessTags(string raw, IEnumerable<PostprocessRuleEntry> rules)
	{
		bool allowed = (rules ?? Enumerable.Empty<PostprocessRuleEntry>()).Any((PostprocessRuleEntry rule) => string.Equals((rule?.Tag ?? "").Trim(), IntimacyActionTag, StringComparison.OrdinalIgnoreCase));
		return allowed && IntimacyTagRegex.IsMatch(raw ?? "") ? IntimacyActionTag : "";
	}

	public static bool TryApplyIntimacyTags(Hero targetHero, ref string responseText, string chainName)
	{
		string text = responseText ?? "";
		if (!IntimacyTagRegex.IsMatch(text))
		{
			return false;
		}
		responseText = IntimacyTagRegex.Replace(text, "").Trim();
		if (IsCourierChain(chainName))
		{
			Logger.Log("SexualConception", "ignored intimacy tag in courier chain");
			return true;
		}
		if (!TryResolveAdultOppositeSexPair(targetHero, out var mother, out var father))
		{
			Logger.Log("SexualConception", "ignored intimacy tag: target is not a valid adult opposite-sex Hero pair");
			return true;
		}

		string npcName = targetHero.Name?.ToString() ?? "NPC";
		InformationManager.DisplayMessage(new InformationMessage("你与" + npcName + "发生了亲密行为。", Colors.Magenta));

		bool becamePregnant = false;
		string skipReason = "roll_failed";
		if (CanStartPregnancy(mother, father, out skipReason) && MBRandom.RandomFloat < PregnancyChance)
		{
			becamePregnant = TryMakePregnantWithFather(mother, father, out skipReason);
		}

		string fact = "[AFEF 玩家行为补充] 玩家与" + npcName + "发生了亲密关系。";
		if (becamePregnant)
		{
			fact += (mother == Hero.MainHero)
				? ("系统已确认玩家怀孕，父亲是" + npcName + "。")
				: ("系统已确认" + npcName + "怀孕，父亲是玩家。");
		}
		MyBehavior.AppendExternalDialogueHistory(targetHero, null, null, fact);
		Logger.Log("SexualConception", "intimacy applied target=" + (targetHero.StringId ?? "") + " mother=" + (mother.StringId ?? "") + " father=" + (father.StringId ?? "") + " pregnancy=" + becamePregnant + " result=" + skipReason);
		return true;
	}

	private static bool TryResolveAdultOppositeSexPair(Hero targetHero, out Hero mother, out Hero father)
	{
		mother = null;
		father = null;
		Hero player = Hero.MainHero;
		if (player == null || targetHero == null || targetHero == player || !player.IsAlive || !targetHero.IsAlive || player.IsFemale == targetHero.IsFemale)
		{
			return false;
		}
		float adulthoodAge;
		try
		{
			adulthoodAge = Campaign.Current?.Models?.AgeModel?.HeroComesOfAge ?? 18;
		}
		catch
		{
			adulthoodAge = 18f;
		}
		if (player.Age < adulthoodAge || targetHero.Age < adulthoodAge)
		{
			return false;
		}
		mother = player.IsFemale ? player : targetHero;
		father = player.IsFemale ? targetHero : player;
		return true;
	}

	private static bool CanStartPregnancy(Hero mother, Hero father, out string reason)
	{
		reason = "eligible";
		if (mother == null || father == null || !mother.IsAlive || !father.IsAlive)
		{
			reason = "parent_invalid";
			return false;
		}
		if (mother.IsPregnant)
		{
			reason = "already_pregnant";
			return false;
		}
		try
		{
			if (CampaignOptions.IsLifeDeathCycleDisabled)
			{
				reason = "life_death_cycle_disabled";
				return false;
			}
		}
		catch
		{
		}
		if (mother.Clan != null && mother.Clan.IsRebelClan)
		{
			reason = "rebel_clan_not_processed_by_vanilla";
			return false;
		}
		try
		{
			if (mother.CharacterObject?.Race != father.CharacterObject?.Race)
			{
				reason = "race_mismatch";
				return false;
			}
		}
		catch
		{
			reason = "race_check_failed";
			return false;
		}
		if (!_pregnancyFatherPatchReady)
		{
			reason = "father_patch_unavailable";
			return false;
		}
		return true;
	}

	private static bool TryMakePregnantWithFather(Hero mother, Hero father, out string reason)
	{
		reason = "pregnant";
		_pendingMother = mother;
		_pendingFather = father;
		try
		{
			MakePregnantAction.Apply(mother);
			if (!mother.IsPregnant)
			{
				reason = "vanilla_action_did_not_mark_pregnant";
				return false;
			}
			return true;
		}
		catch (Exception ex)
		{
			reason = "exception:" + ex.GetType().Name;
			Logger.Log("SexualConception", "make pregnant failed: " + ex);
			return false;
		}
		finally
		{
			_pendingMother = null;
			_pendingFather = null;
		}
	}

	private static bool IsCourierChain(string chainName)
	{
		return (chainName ?? "").IndexOf("courier", StringComparison.OrdinalIgnoreCase) >= 0;
	}
}

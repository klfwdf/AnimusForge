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
	internal const string IntimacyDecisionTemplate = "[ACTION:INTIMACY_INTERNAL:occurred=YES|NO|UNCERTAIN;npc_denial=YES|NO|UNCERTAIN]";

	private const float DefaultPregnancyChance = 0.5f;

	private static readonly Regex IntimacyTagRegex = new Regex("\\[ACTION:INTIMACY_INTERNAL\\]", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	[ThreadStatic]
	private static Hero _pendingMother;

	[ThreadStatic]
	private static Hero _pendingFather;

	private static bool _pregnancyFatherPatchReady;

	private static string _pregnancyFatherPatchStatus = "not_registered";

	public static void RegisterHarmonyPatches(Harmony harmony)
	{
		if (harmony == null)
		{
			throw new ArgumentNullException(nameof(harmony));
		}
		Type pregnancyType = AccessTools.Inner(typeof(PregnancyCampaignBehavior), "Pregnancy");
		ConstructorInfo constructor = AccessTools.Constructor(pregnancyType, new Type[3]
		{
			typeof(Hero),
			typeof(Hero),
			typeof(CampaignTime)
		});
		if (constructor == null)
		{
			_pregnancyFatherPatchStatus = "constructor_not_found";
			throw new MissingMethodException("PregnancyCampaignBehavior.Pregnancy(Hero, Hero, CampaignTime)");
		}
		MethodInfo prefixMethod = AccessTools.Method(typeof(SexualConceptionBehavior), nameof(PregnancyConstructorPrefix));
		if (prefixMethod == null)
		{
			_pregnancyFatherPatchStatus = "prefix_not_found";
			throw new MissingMethodException(nameof(PregnancyConstructorPrefix));
		}
		harmony.Patch(constructor, new HarmonyMethod(prefixMethod));
		Patches patchInfo = Harmony.GetPatchInfo(constructor);
		_pregnancyFatherPatchReady = patchInfo?.Prefixes?.Any((Patch patch) => string.Equals(patch.owner, harmony.Id, StringComparison.Ordinal) && patch.PatchMethod == prefixMethod) == true;
		_pregnancyFatherPatchStatus = _pregnancyFatherPatchReady ? ("ready owner=" + harmony.Id) : "patch_verification_failed";
		if (!_pregnancyFatherPatchReady)
		{
			throw new InvalidOperationException("Pregnancy father patch verification failed");
		}
		Logger.Log("SexualConception", "pregnancy father patch " + _pregnancyFatherPatchStatus);
	}

	private static void PregnancyConstructorPrefix(Hero __0, ref Hero __1)
	{
		if (_pendingMother == __0 && _pendingFather != null)
		{
			__1 = _pendingFather;
			Logger.Log("SexualConception", "pregnancy father override mother=" + (__0?.StringId ?? "") + " father=" + (__1?.StringId ?? ""));
		}
	}

	public static List<PostprocessRuleEntry> BuildRuntimePostprocessRules(Hero targetHero, string chainName)
	{
		if (IsCourierChain(chainName) || !TryResolveAdultOppositeSexPair(targetHero, out var _, out var _))
		{
			return new List<PostprocessRuleEntry>();
		}
		return (AIConfigHandler.IntimacyPostprocessRules ?? new List<PostprocessRuleEntry>())
			.Where((PostprocessRuleEntry rule) => string.Equals((rule?.Tag ?? "").Trim(), IntimacyActionTag, StringComparison.OrdinalIgnoreCase)
                || string.Equals((rule?.Tag ?? "").Trim(), IntimacyDecisionTemplate, StringComparison.OrdinalIgnoreCase))
            .Select(rule => new PostprocessRuleEntry {
                Tag = IntimacyDecisionTemplate,
                Description = (rule.Description ?? "") + "\n" + SocialPromptTexts.Get("IntimacyDecisionContract")
            })
			.ToList();
	}

	public static string NormalizePostprocessTags(string raw, IEnumerable<PostprocessRuleEntry> rules)
	{
		bool allowed = (rules ?? Enumerable.Empty<PostprocessRuleEntry>()).Any(rule =>
            string.Equals((rule?.Tag ?? "").Trim(), IntimacyDecisionTemplate, StringComparison.OrdinalIgnoreCase));
        if (!allowed) return "";
        var decisions = Regex.Matches(raw ?? "", @"\[ACTION:INTIMACY_INTERNAL:occurred=(YES|NO|UNCERTAIN);npc_denial=(YES|NO|UNCERTAIN)\]", RegexOptions.IgnoreCase);
        // Missing, ambiguous or contradictory classifications never become a pregnancy/fact event.
        if (decisions.Count != 1) return "";
        return string.Equals(decisions[0].Groups[1].Value, "YES", StringComparison.OrdinalIgnoreCase)
            && string.Equals(decisions[0].Groups[2].Value, "NO", StringComparison.OrdinalIgnoreCase) ? IntimacyActionTag : "";
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
		InformationManager.DisplayMessage(new InformationMessage(SocialUiLocalization.Format("AF_SOCIAL_UI_IntimacyCompleted", "You and {NAME} shared an intimate encounter.", ("NAME", npcName)), Colors.Magenta));

		bool becamePregnant = false;
		float pregnancyChance = ResolvePregnancyChance();
		float pregnancyRoll = -1f;
		string skipReason = "roll_failed";
		if (CanStartPregnancy(mother, father, out skipReason))
		{
			pregnancyRoll = MBRandom.RandomFloat;
			if (pregnancyRoll < pregnancyChance)
			{
				becamePregnant = TryMakePregnantWithFather(mother, father, out skipReason);
			}
			else
			{
				skipReason = "roll_failed";
			}
		}

		string fact = SocialSourceTexts.Get("S215") + npcName + SocialSourceTexts.Get("S216");
		if (becamePregnant)
		{
			fact += (mother == Hero.MainHero)
				? (SocialSourceTexts.Get("S217") + npcName + "。")
				: (SocialSourceTexts.Get("S218") + npcName + SocialSourceTexts.Get("S219"));
		}
		MyBehavior.AppendExternalDialogueHistory(targetHero, null, null, fact);
		Logger.Log("SexualConception", "intimacy applied target=" + (targetHero.StringId ?? "") + " mother=" + (mother.StringId ?? "") + " father=" + (father.StringId ?? "") + " chance=" + pregnancyChance.ToString("0.00") + " roll=" + ((pregnancyRoll < 0f) ? "not_rolled" : pregnancyRoll.ToString("0.0000")) + " pregnancy=" + becamePregnant + " result=" + skipReason + " patch=" + _pregnancyFatherPatchStatus);
		return true;
	}

	private static float ResolvePregnancyChance()
	{
		try
		{
			int percent = DuelSettings.GetSettings()?.IntimacyPregnancyChancePercent ?? 50;
			return Math.Max(0f, Math.Min(1f, percent / 100f));
		}
		catch (Exception ex)
		{
			Logger.Log("SexualConception", "failed to read MCM pregnancy chance, using default 50%: " + ex.Message);
			return DefaultPregnancyChance;
		}
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
			reason = "father_patch_unavailable:" + _pregnancyFatherPatchStatus;
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

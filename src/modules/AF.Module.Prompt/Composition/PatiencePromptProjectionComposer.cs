using System;
using System.Text;
using PatienceSnapshot = AnimusForge.MyBehavior.PatienceSnapshot;

namespace AnimusForge.Refactor.Modules;

internal static class PatiencePromptProjectionComposer
{
	internal static string BuildCompactStateLine(PatienceSnapshot snap, bool includeClanRelation = true)
	{
		int num = (int)Math.Round(snap.Current);
		if (!includeClanRelation)
		{
			return PromptComposerTexts.Format("patience_compact_three", ("CURRENT", num), ("MAX", snap.Max), ("PATIENCE", snap.PatienceLevel), ("TRUST", snap.Trust), ("TRUST_LEVEL", snap.TrustLevel), ("LOVE", snap.PrivateLove), ("LOVE_LEVEL", snap.PrivateLoveLevel));
		}
		return PromptComposerTexts.Format("patience_compact_four", ("CURRENT", num), ("MAX", snap.Max), ("PATIENCE", snap.PatienceLevel), ("RELATION", snap.Relation), ("RELATION_LEVEL", snap.RelationLevel), ("TRUST", snap.Trust), ("TRUST_LEVEL", snap.TrustLevel), ("LOVE", snap.PrivateLove), ("LOVE_LEVEL", snap.PrivateLoveLevel));
	}
	internal static string BuildSceneInlineStateLine(PatienceSnapshot snap, string targetPronoun, string fallbackRelationLevel, bool includeClanRelation = true)
	{
		int currentPatience = (int)Math.Round(snap.Current);
		string privateLoveLevel = string.IsNullOrWhiteSpace(snap.PrivateLoveLevel) ? RomanceSystemBehavior.GetPrivateLoveLevelText(snap.PrivateLove) : snap.PrivateLoveLevel;
		string trustLevel = string.IsNullOrWhiteSpace(snap.TrustLevel) ? RewardSystemBehavior.GetTrustLevelText(snap.Trust) : snap.TrustLevel;
		string patienceLevel = string.IsNullOrWhiteSpace(snap.PatienceLevel) ? PatienceRules.GetPatienceLevelText(snap.Current, snap.Max) : snap.PatienceLevel;
		if (!includeClanRelation)
		{
			return PromptComposerTexts.Format("patience_inline_three", ("TARGET", targetPronoun), ("LOVE_LEVEL", privateLoveLevel), ("LOVE", snap.PrivateLove), ("TRUST_LEVEL", trustLevel), ("TRUST", snap.Trust), ("CURRENT", currentPatience), ("MAX", snap.Max), ("PATIENCE", patienceLevel));
		}
		string relationLevel = string.IsNullOrWhiteSpace(snap.RelationLevel) ? fallbackRelationLevel : snap.RelationLevel;
		return PromptComposerTexts.Format("patience_inline_four", ("TARGET", targetPronoun), ("LOVE_LEVEL", privateLoveLevel), ("LOVE", snap.PrivateLove), ("TRUST_LEVEL", trustLevel), ("TRUST", snap.Trust), ("RELATION_LEVEL", relationLevel), ("RELATION", snap.Relation), ("CURRENT", currentPatience), ("MAX", snap.Max), ("PATIENCE", patienceLevel));
	}
	internal static string BuildSceneInlineStateText(PatienceSnapshot snap, bool includeRelationPenalty, string targetPronoun, string playerDisplayName, string fallbackRelationLevel, bool includeClanRelation = true)
	{
		string text = BuildSceneInlineStateLine(snap, targetPronoun, fallbackRelationLevel, includeClanRelation);
		if (snap.Current <= 0.01f)
		{
			text += BuildExhaustedPatienceInstruction(includeRelationPenalty, playerDisplayName);
		}
		return text;
	}
	internal static string BuildExhaustedReply(string npcName, int relation, int refusalCount = 1)
	{
		string text = (string.IsNullOrWhiteSpace(npcName) ? PromptComposerTexts.Get("patience_reply_self") : npcName);
		if (refusalCount < 1)
		{
			refusalCount = 1;
		}
		if (relation >= 40)
		{
			string[] array = new string[3]
			{
				text + PromptComposerTexts.Get("patience_reply_friendly_1"),
				text + PromptComposerTexts.Get("patience_reply_friendly_2"),
				text + PromptComposerTexts.Get("patience_reply_friendly_3")
			};
			return array[(refusalCount - 1) % array.Length];
		}
		if (relation <= -20)
		{
			string[] array2 = new string[3]
			{
				text + PromptComposerTexts.Get("patience_reply_hostile_1"),
				text + PromptComposerTexts.Get("patience_reply_hostile_2"),
				text + PromptComposerTexts.Get("patience_reply_hostile_3")
			};
			return array2[(refusalCount - 1) % array2.Length];
		}
		string[] array3 = new string[3]
		{
			text + PromptComposerTexts.Get("patience_reply_neutral_1"),
			text + PromptComposerTexts.Get("patience_reply_neutral_2"),
			text + PromptComposerTexts.Get("patience_reply_neutral_3")
		};
		return array3[(refusalCount - 1) % array3.Length];
	}
	internal static string BuildExhaustedPatienceInstruction(bool includeRelationPenalty, string playerDisplayName)
	{
		string text = playerDisplayName;
		if (string.IsNullOrWhiteSpace(text))
		{
			text = PromptComposerTexts.Get("patience_player");
		}
		if (!includeRelationPenalty)
		{
			return PromptComposerTexts.Format("patience_exhausted", ("PLAYER", text));
		}
		return PromptComposerTexts.Format("patience_exhausted_penalty", ("PLAYER", text));
	}
	internal static string BuildPatiencePromptText(PatienceSnapshot snap, string playerDisplayName, bool includeClanRelation = true)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(PromptComposerTexts.Get(includeClanRelation ? "patience_header_four" : "patience_header_three"));
		stringBuilder.AppendLine(BuildCompactStateLine(snap, includeClanRelation));
		stringBuilder.AppendLine(PromptComposerTexts.Get("patience_header"));
		if (snap.Current <= 0.01f)
		{
			stringBuilder.AppendLine(BuildExhaustedPatienceInstruction(true, playerDisplayName));
		}
		else
		{
			stringBuilder.AppendLine(PromptComposerTexts.Get("patience_instruction"));
		}
		return stringBuilder.ToString();
	}
	internal static string BuildScenePatienceInstruction()
	{
		return "";
	}
}

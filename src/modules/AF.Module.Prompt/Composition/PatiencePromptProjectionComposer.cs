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
			return $"P(耐心)={num}/{snap.Max}({snap.PatienceLevel}) | T(综合信任)={snap.Trust}({snap.TrustLevel}) | L(私人关系)={snap.PrivateLove}({snap.PrivateLoveLevel})";
		}
		return $"P(耐心)={num}/{snap.Max}({snap.PatienceLevel}) | R(家族关系)={snap.Relation}({snap.RelationLevel}) | T(综合信任)={snap.Trust}({snap.TrustLevel}) | L(私人关系)={snap.PrivateLove}({snap.PrivateLoveLevel})";
	}
	internal static string BuildSceneInlineStateLine(PatienceSnapshot snap, string targetPronoun, string fallbackRelationLevel, bool includeClanRelation = true)
	{
		int currentPatience = (int)Math.Round(snap.Current);
		string privateLoveLevel = string.IsNullOrWhiteSpace(snap.PrivateLoveLevel) ? RomanceSystemBehavior.GetPrivateLoveLevelText(snap.PrivateLove) : snap.PrivateLoveLevel;
		string trustLevel = string.IsNullOrWhiteSpace(snap.TrustLevel) ? RewardSystemBehavior.GetTrustLevelText(snap.Trust) : snap.TrustLevel;
		string patienceLevel = string.IsNullOrWhiteSpace(snap.PatienceLevel) ? PatienceRules.GetPatienceLevelText(snap.Current, snap.Max) : snap.PatienceLevel;
		if (!includeClanRelation)
		{
			return $"你对{targetPronoun}的私人关系是{privateLoveLevel}（私人关系评级）{snap.PrivateLove}，信任度是{trustLevel}（信任度评级）{snap.Trust}，你现在对{targetPronoun}的耐心是{currentPatience}/{snap.Max}（{patienceLevel}）。";
		}
		string relationLevel = string.IsNullOrWhiteSpace(snap.RelationLevel) ? fallbackRelationLevel : snap.RelationLevel;
		return $"你对{targetPronoun}的私人关系是{privateLoveLevel}（私人关系评级）{snap.PrivateLove}，信任度是{trustLevel}（信任度评级）{snap.Trust}，你的家族对{targetPronoun}的关系是{relationLevel}（家族关系评级）{snap.Relation}，你现在对{targetPronoun}的耐心是{currentPatience}/{snap.Max}（{patienceLevel}）。";
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
		string text = (string.IsNullOrWhiteSpace(npcName) ? "我" : npcName);
		if (refusalCount < 1)
		{
			refusalCount = 1;
		}
		if (relation >= 40)
		{
			string[] array = new string[3]
			{
				text + "压低声音：今天我确实有些乏了，改天再聊。",
				text + "轻叹：我现在没有余力应付长谈，先到这里吧。",
				text + "摇头道：等我缓一缓，再听你说。"
			};
			return array[(refusalCount - 1) % array.Length];
		}
		if (relation <= -20)
		{
			string[] array2 = new string[3]
			{
				text + "皱眉道：我眼下有要事，没空陪你闲扯，退下。",
				text + "冷声道：军务在身，今天到此为止。",
				text + "摆手打断：我还要处理别的事，改日再说。"
			};
			return array2[(refusalCount - 1) % array2.Length];
		}
		string[] array3 = new string[3]
		{
			text + "摆摆手：我这边还有事，今天就先到这。",
			text + "皱了皱眉：行程紧，改日再谈。",
			text + "侧过身：先到这里，我得去处理别的事。"
		};
		return array3[(refusalCount - 1) % array3.Length];
	}
	internal static string BuildExhaustedPatienceInstruction(bool includeRelationPenalty, string playerDisplayName)
	{
		string text = playerDisplayName;
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "玩家";
		}
		if (!includeRelationPenalty)
		{
			return "耐心已归零：本轮优先用军务在身、行程紧、需要离开、精神疲惫或另有要事等理由，给出委婉的回避式回复，尽量收束或回避当前话题；若" + text + "继续追问，仍可继续回应，但语气应明显更谨慎或更不耐烦，不要直说系统规则或数值。";
		}
		return "耐心已归零：本轮优先用军务在身、行程紧、需要离开、精神疲惫或另有要事等理由，给出委婉的回避式回复，尽量收束或回避当前话题；若" + text + "继续追问，仍可继续回应，但语气应明显更谨慎或更不耐烦，系统会额外降低你与" + text + "的关系，不要直说系统规则或数值。";
	}
	internal static string BuildPatiencePromptText(PatienceSnapshot snap, string playerDisplayName, bool includeClanRelation = true)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(includeClanRelation ? "【4.四值状态】" : "【4.三值状态】");
		stringBuilder.AppendLine(BuildCompactStateLine(snap, includeClanRelation));
		stringBuilder.AppendLine("【NPC耐心状态】");
		if (snap.Current <= 0.01f)
		{
			stringBuilder.AppendLine(BuildExhaustedPatienceInstruction(true, playerDisplayName));
		}
		else
		{
			stringBuilder.AppendLine("耐心越低，语气越不耐烦；若耐心耗尽，应优先给出委婉的回避式回复。");
		}
		return stringBuilder.ToString();
	}
	internal static string BuildScenePatienceInstruction()
	{
		return "";
	}
}

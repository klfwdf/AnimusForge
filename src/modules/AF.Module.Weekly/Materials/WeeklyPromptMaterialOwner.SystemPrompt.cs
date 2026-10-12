using System;using System.Text;using static AnimusForge.MyBehavior;
namespace AnimusForge;
internal static partial class WeeklyPromptMaterialOwner
{

	internal static WeeklyReportPromptProfile GetWeeklyReportPromptProfile(int num)
	{
        num = Math.Max(1, Math.Min(4,num));
		return num switch
		{
			1 => new WeeklyReportPromptProfile
			{
				Preset = 1,
				MinWords = 200,
				MaxWords = 400,
				Label = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_000")
			},
			2 => new WeeklyReportPromptProfile
			{
				Preset = 2,
				MinWords = 200,
				MaxWords = 800,
				Label = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_001")
			},
			3 => new WeeklyReportPromptProfile
			{
				Preset = 3,
				MinWords = 200,
				MaxWords = 1200,
				Label = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_002")
			},
			4 => new WeeklyReportPromptProfile
			{
				Preset = 4,
				MinWords = 200,
				MaxWords = 1500,
				Label = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_003")
			},
			_ => new WeeklyReportPromptProfile
			{
				Preset = 2,
				MinWords = 200,
				MaxWords = 800,
				Label = WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_004")
			},
		};
	}

	internal static void AppendWeeklyReportWritingRequirements(StringBuilder stringBuilder, string requirements)
	{
		if (stringBuilder == null)
		{
			return;
		}
		string text = (requirements ?? "").Replace("\r", "").Trim();
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_005"));
		stringBuilder.AppendLine(text);
	}

	internal static void AppendWeeklyReportPoliticalReasonWritingRule(StringBuilder stringBuilder, bool fullReport)
	{
		if (stringBuilder == null)
		{
			return;
		}
		stringBuilder.AppendLine(fullReport ? WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_006") : WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_007"));
	}

	internal static void AppendWeeklyReportSettlementReasonWritingRule(StringBuilder stringBuilder)
	{
		if (stringBuilder == null)
		{
			return;
		}
		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_008"));
	}

	internal static void AppendWeeklyReportVillageRaidWritingRule(StringBuilder stringBuilder)
	{
		if (stringBuilder == null)
		{
			return;
		}
		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_009"));
	}

	internal static string BuildWeeklyReportSystemPrompt(WeeklyEventMaterialPreviewGroup group, WeeklyReportPromptProfile profile, string requirements)
	{
		WeeklyReportPromptProfile weeklyReportPromptProfile = profile;
		bool flag = string.Equals((group?.GroupKind ?? "").Trim(), "world", StringComparison.OrdinalIgnoreCase);
		bool flag2 = (group?.OutputMode ?? WeeklyReportOutputMode.FullReport) == WeeklyReportOutputMode.TitleShortTagsOnly;
		string text = flag ? WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_010") : WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_011");
		string text2 = flag ? WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_012") : WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_013");
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_014"));
		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_015"));
		stringBuilder.AppendLine(text2);
		AppendWeeklyReportWritingRequirements(stringBuilder, requirements);
		AppendWeeklyReportVillageRaidWritingRule(stringBuilder);
		AppendWeeklyReportPoliticalReasonWritingRule(stringBuilder, !flag2);
		if (!flag2)
		{
			AppendWeeklyReportSettlementReasonWritingRule(stringBuilder);
		}
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_016"));
		stringBuilder.AppendLine(string.Format(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_017"), weeklyReportPromptProfile.Label));
		if (!flag2)
		{
			stringBuilder.AppendLine(string.Format(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_018"), weeklyReportPromptProfile.MinWords, weeklyReportPromptProfile.MaxWords));
		}
		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_019"));
		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_020"));
		stringBuilder.AppendLine(" ");
		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_021"));
		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_022"));
		stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_023"));
		if (!flag2)
		{
			stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_024"));
		}
		stringBuilder.AppendLine("[TAGS]");
		stringBuilder.AppendLine("STAB_FLAT");
		stringBuilder.AppendLine(" ");
		if (flag2)
		{
			stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_025"));
			stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_026"));
		}
		else
		{
			stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_027"));
			stringBuilder.AppendLine(WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_028"));
		}
		stringBuilder.AppendLine(flag2 ? WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_029") : WeeklyMaterialTexts.Prompt("AF_WEEKLY_Prompt_030"));
		return PromptComposerTexts.ApplyOutputLanguage(stringBuilder.ToString().TrimEnd());
	}


}

using System.Globalization;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Localization;

namespace AnimusForge;

/// <summary>Bilingual UI strings (af_ui_strings.xml / CNs/af_ui_strings-zh-CN.xml).</summary>
internal static class FieldCampText
{
	internal static TextObject MenuTitle() => new TextObject("{=AF_FC_Menu_Title}Field camp");

	internal static TextObject Disabled() => new TextObject("{=AF_FC_Disabled}Field camp is disabled in the settings.");

	internal static TextObject Pitched() => new TextObject("{=AF_FC_Pitched}Camp is pitched. The troops settle in to rest.");

	internal static TextObject Struck() => new TextObject("{=AF_FC_Struck}Camp is struck. The march can resume.");

	internal static TextObject MoveBlocked() => new TextObject("{=AF_FC_MoveBlocked}You are camped. Strike camp from the camp menu before marching.");

	internal static TextObject BlockedState() => new TextObject("{=AF_FC_BlockedState}You cannot pitch camp right now.");

	internal static TextObject BlockedEncounter() => new TextObject("{=AF_FC_BlockedEncounter}You cannot pitch camp during an encounter or conversation.");

	internal static TextObject BlockedSiege() => new TextObject("{=AF_FC_BlockedSiege}You cannot pitch a field camp during a siege.");

	internal static TextObject BlockedArmyMember() => new TextObject("{=AF_FC_BlockedArmyMember}Only the army leader can order the army to camp.");

	internal static TextObject BlockedSea() => new TextObject("{=AF_FC_BlockedSea}You cannot pitch camp at sea or on water.");

	internal static TextObject BlockedSettlement(Settlement settlement)
	{
		var text = new TextObject("{=AF_FC_BlockedSettlement}You are too close to {SETTLEMENT} to pitch a field camp.");
		text.SetTextVariable("SETTLEMENT", settlement.Name);
		return text;
	}

	internal static TextObject PatrolTooltip(float hours)
	{
		var text = new TextObject("{=AF_FC_PatrolTooltip}Walk among the tents and troops. Leaving the camp scene costs about {HOURS} hours.");
		text.SetTextVariable("HOURS", hours.ToString("0.#"));
		return text;
	}

	internal static TextObject PatrolWounded() => new TextObject("{=AF_FC_PatrolWounded}You are too badly wounded to walk the camp.");

	internal static TextObject PatrolFailed() => new TextObject("{=AF_FC_PatrolFailed}The camp scene could not be opened here.");

	internal static TextObject SetupDegraded() => new TextObject("{=AF_FC_SetupDegraded}Some camp facilities could not be initialized. The scene stays open until you choose to leave.");

	internal static TextObject NoLevelSite() => new TextObject("{=AF_FC_NoLevelSite}No level area large enough for the entire camp was found in this scene. Camp facilities were not forced onto a hillside. You can leave manually without a patrol time charge.");

	internal static TextObject CeremonyPreparing() => new TextObject("{=AF_FC_CeremonyPreparing}Preparing your execution site: camp residents are walking clear. Once the site is clear, deployment continues with the spectators already in place.");

	internal static TextObject OptionalFurnitureSkipped(int count)
	{
		var text = new TextObject("{=AF_FC_OptionalFurnitureSkipped}Camp is ready. {COUNT} optional tent furnishings could not fit; these skipped furnishings do not block camp use or custom-site deployment.");
		text.SetTextVariable("COUNT", count);
		return text;
	}

	internal static TextObject Assembled() => new TextObject("{=AF_FC_Assembled}The troops fall in by troop type.");

	internal static TextObject Dismissed() => new TextObject("{=AF_FC_Dismissed}The troops are dismissed and return to camp.");

	internal static TextObject InspectionBlocked() => new TextObject("{=AF_FC_InspectionBlocked}Strike camp before using the troop inspection flow.");

	internal static TextObject StrikeTooltip(float hours)
	{
		var text = new TextObject("{=AF_FC_StrikeTooltip}Takes {HOURS} hours. Resting stops; the march resumes once camp is struck.");
		text.SetTextVariable("HOURS", hours.ToString("0.#"));
		return text;
	}

	// ---------- camp issues ----------

	/// <summary>Quick-info line after a resolution: action, optional cost and discharges, morale change.</summary>
	internal static string IssueResolved(FieldCampIssues.Resolution resolution, float morale, int cost, int dismissed)
	{
		TextObject action;
		switch (resolution)
		{
			case FieldCampIssues.Resolution.Encourage:
				action = new TextObject("{=AF_FC_IssueResolved_Encourage}You reassured the men.");
				break;
			case FieldCampIssues.Resolution.Grant:
				action = new TextObject("{=AF_FC_IssueResolved_Grant}You granted the request.");
				break;
			case FieldCampIssues.Resolution.Refuse:
				action = new TextObject("{=AF_FC_IssueResolved_Refuse}You turned the request down.");
				break;
			default:
				action = new TextObject("{=AF_FC_IssueResolved_Discipline}You restored discipline.");
				break;
		}
		string text = action.ToString();
		if (dismissed > 0)
		{
			var discharged = new TextObject("{=AF_FC_IssueResolved_Dismissed}{COUNT} troublemakers were discharged.");
			discharged.SetTextVariable("COUNT", dismissed);
			text += " " + discharged;
		}
		if (cost > 0)
		{
			var spent = new TextObject("{=AF_FC_IssueResolved_Spent}Spent {GOLD} denars.");
			spent.SetTextVariable("GOLD", cost);
			text += " " + spent;
		}
		var moraleText = new TextObject("{=AF_FC_IssueResolved_Morale}Morale {MORALE}.");
		moraleText.SetTextVariable("MORALE", SignedMorale(morale));
		return text + " " + moraleText;
	}

	/// <summary>Issue sentence in the installed Prompt package language (prompt block, rules, AFEF facts).</summary>
	internal static string IssuePromptText(FieldCampIssues.Issue issue)
	{
		return ConversationRuntimeTexts.FormatPrompt("AF_FC_Issue_" + issue.Kind,
			("TROOP", SubjectName(issue, issue.Subject)),
			("OTHER", SubjectName(issue, issue.Subject2)));
	}

	internal static string ResolutionFact(FieldCampIssues.Issue issue, FieldCampIssues.Resolution resolution, float morale, int cost, int dismissed)
	{
		string issueText = IssuePromptText(issue).Trim().TrimEnd('。', '.');
		string costText = cost > 0 ? ConversationRuntimeTexts.FormatPrompt("AF_FC_Fact_Cost", ("GOLD", cost)) : "";
		string dismissedText = dismissed > 0 ? ConversationRuntimeTexts.FormatPrompt("AF_FC_Fact_Dismissed", ("COUNT", dismissed)) : "";
		string body = ConversationRuntimeTexts.FormatPrompt("AF_FC_Fact_" + resolution,
			("ISSUE", issueText),
			("MORALE", SignedMorale(morale)),
			("COST", costText),
			("DISMISSED", dismissedText));
		return AdapterPromptCaptureTexts.PcapCompat("Host_AfefPlayerPrefix") + body;
	}

	internal static string SignedMorale(float morale) => morale.ToString("+0.#;-0.#;0", CultureInfo.InvariantCulture);

	private static string SubjectName(FieldCampIssues.Issue issue, string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return "";
		}
		try
		{
			if (issue.IsCompanionIssue)
			{
				string heroName = Hero.Find(id)?.Name?.ToString();
				if (!string.IsNullOrWhiteSpace(heroName))
				{
					return heroName;
				}
			}
			string troopName = CharacterObject.Find(id)?.Name?.ToString();
			return string.IsNullOrWhiteSpace(troopName) ? id : troopName;
		}
		catch
		{
			return id;
		}
	}

	internal static TextObject Status(FieldCampBehavior.CampState state, bool resting, int parties, double campedHours, double remainingHours)
	{
		TextObject text;
		switch (state)
		{
			case FieldCampBehavior.CampState.Pitching:
				text = new TextObject("{=AF_FC_Status_Pitching}Your troops are pitching camp. About {HOURS} hours remain.");
				text.SetTextVariable("HOURS", remainingHours.ToString("0.#"));
				return text;
			case FieldCampBehavior.CampState.Striking:
				text = new TextObject("{=AF_FC_Status_Striking}Your troops are striking camp. About {HOURS} hours remain.");
				text.SetTextVariable("HOURS", remainingHours.ToString("0.#"));
				return text;
			default:
				text = resting
					? new TextObject("{=AF_FC_Status_Resting}The camp rests. The wounded recover faster and morale slowly returns. Camped for {HOURS} hours; {PARTIES} parties in camp.")
					: new TextObject("{=AF_FC_Status_Camped}The camp is quiet. Resting heals the wounded faster and slowly restores morale, but the march is halted. Camped for {HOURS} hours; {PARTIES} parties in camp.");
				text.SetTextVariable("HOURS", campedHours.ToString("0"));
				text.SetTextVariable("PARTIES", parties);
				return text;
		}
	}
}

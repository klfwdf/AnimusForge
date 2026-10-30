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

	internal static TextObject Assembled() => new TextObject("{=AF_FC_Assembled}The troops fall in by troop type.");

	internal static TextObject Dismissed() => new TextObject("{=AF_FC_Dismissed}The troops are dismissed and return to camp.");

	internal static TextObject InspectionBlocked() => new TextObject("{=AF_FC_InspectionBlocked}Strike camp before using the troop inspection flow.");

	internal static TextObject StrikeTooltip(float hours)
	{
		var text = new TextObject("{=AF_FC_StrikeTooltip}Takes {HOURS} hours. Resting stops; the march resumes once camp is struck.");
		text.SetTextVariable("HOURS", hours.ToString("0.#"));
		return text;
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

using TaleWorlds.CampaignSystem;

namespace AnimusForge;

public partial class MyBehavior
{
	internal static void RecordCivilWarMemoryFact(Hero hero, string text)
	{
		if (hero == null || hero == Hero.MainHero || string.IsNullOrWhiteSpace(text)) return;
		AppendExternalDialogueHistory(hero, "", "", "[AFEF内战事实] " + text);
	}
	// Thin main-thread adapter to the existing report, bulletin and character-fact owners.
	internal static void RecordCivilWarPoliticalResult(Kingdom kingdom, string key, string text, bool bulletin)
	{
		if (kingdom == null || string.IsNullOrWhiteSpace(text)) return;
		RecordEventSourceMaterialForExternal("civil_war", "内战政治 - " + kingdom.Name, text, key, kingdom.StringId, true, true);
		string kind = key.Contains(":war:outbreak") ? "civil_war" : key.Contains(":war:resolved") ? "civil_war_resolution" : "civil_war_politics";
		if (bulletin) Instance?.CaptureWorldBulletinEvent(kind, key, 80, kingdom.Name + "：" + text,
			Clan.PlayerClan?.Kingdom == kingdom, "realm:" + kingdom.StringId, text, kingdom.StringId);
	}
}

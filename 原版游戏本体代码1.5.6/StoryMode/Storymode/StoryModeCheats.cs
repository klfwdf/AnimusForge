using System.Collections.Generic;
using StoryMode.StoryModeObjects;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace StoryMode;

public static class StoryModeCheats
{
	public const string NotStoryMode = "Game mode is not correct!";

	public static bool CheckCheatUsage(ref string message)
	{
		if (!CampaignCheats.CheckCheatUsage(ref message))
		{
			return false;
		}
		if (StoryModeManager.Current == null)
		{
			message = "Game mode is not correct!";
			return false;
		}
		return true;
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("add_family_members", "storymode")]
	public static string AddFamilyMembers(List<string> strings)
	{
		string message = string.Empty;
		if (!CheckCheatUsage(ref message))
		{
			return message;
		}
		foreach (Hero item in new List<Hero>
		{
			StoryModeHeroes.LittleBrother,
			StoryModeHeroes.ElderBrother,
			StoryModeHeroes.LittleSister
		})
		{
			AddHeroToPartyAction.Apply(item, MobileParty.MainParty);
			item.Clan = Clan.PlayerClan;
		}
		return "Success";
	}
}

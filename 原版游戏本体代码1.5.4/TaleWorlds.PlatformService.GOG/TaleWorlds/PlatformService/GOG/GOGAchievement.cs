namespace TaleWorlds.PlatformService.GOG;

public class GOGAchievement
{
	public int AchievementID;

	public string AchievementName { get; set; }

	public string Name { get; set; }

	public string Description { get; set; }

	public bool Achieved { get; set; }

	public int Progress { get; set; }
}

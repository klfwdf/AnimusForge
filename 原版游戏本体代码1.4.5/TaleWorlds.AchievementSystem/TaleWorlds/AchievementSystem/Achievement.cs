namespace TaleWorlds.AchievementSystem;

public class Achievement
{
	public string Id { get; set; }

	public string LockedDisplayName { get; set; }

	public string UnlockedDisplayName { get; set; }

	public string LockedDescription { get; set; }

	public string UnlockedDescription { get; set; }

	public int TargetProgress { get; set; }

	public bool IsUnlocked { get; set; }

	public int CurrentProgress { get; set; }
}

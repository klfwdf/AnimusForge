using AFWarStatsTerminal.Behaviors;
using TaleWorlds.CampaignSystem;

namespace AnimusForge;

// War weariness is owned by the war-stats terminal (one value per side of each war).
// Civil war, stability and diplomacy read it here so every consumer sees the same number the terminal shows.
public partial class MyBehavior
{
	// Weariness of `self` in its current war with `enemy`, 0..100; 0 when they are not at war.
	internal static int GetWarWearinessForExternal(Kingdom self, Kingdom enemy)
	{
		try { return AfWarStatsBehavior.Instance?.GetWarWeariness(self, enemy) ?? 0; }
		catch { return 0; }
	}

	// Highest weariness among the kingdom's current wars, 0..100.
	internal static int GetMaxWarWearinessForExternal(Kingdom self)
	{
		try { return AfWarStatsBehavior.Instance?.GetMaxWarWeariness(self) ?? 0; }
		catch { return 0; }
	}

	// Casualties `self` has suffered in its current war with `enemy`.
	internal static int GetWarCasualtiesForExternal(Kingdom self, Kingdom enemy)
	{
		try { return AfWarStatsBehavior.Instance?.GetWarCasualties(self, enemy) ?? 0; }
		catch { return 0; }
	}
}

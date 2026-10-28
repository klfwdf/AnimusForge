using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond.MultiplayerBadges;

public struct KillData
{
	public PlayerId KillerId { get; set; }

	public PlayerId VictimId { get; set; }

	public string KillerFaction { get; set; }

	public string VictimFaction { get; set; }

	public string KillerTroop { get; set; }

	public string VictimTroop { get; set; }
}

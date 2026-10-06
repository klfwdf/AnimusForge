using System.Globalization;

namespace TaleWorlds.MountAndBlade.Diamond;

public static class Parameters
{
	public static readonly int MaxPlayerCountInParty = 6;

	public static readonly int BigBattleTeamPlayerCount = 60;

	public static readonly int ClanNameMaxLength = 30;

	public static CultureInfo DefaultCulture = CultureInfo.InvariantCulture;

	public static readonly int UsernameMinLength = 5;

	public static readonly int UsernameMaxLength = 30;

	public static readonly int UserIdMax = 9999;

	public static readonly int ClanNameMinLength = 5;

	public static readonly int ClanOfficerCount = 3;

	public static readonly int ClanTagMaxLength = 4;

	public static readonly int ClanTagMinLength = 2;

	public static readonly int ClanInformationMaxLength = 255;

	public static readonly int ClanAnnouncementMaxLength = 500;

	public static readonly int LootRewardPerBadgeEarned = 5;

	public static readonly string RandomSelectionString = "RandomSelection";
}

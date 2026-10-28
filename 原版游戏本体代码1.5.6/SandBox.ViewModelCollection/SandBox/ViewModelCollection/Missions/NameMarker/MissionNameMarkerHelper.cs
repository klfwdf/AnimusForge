using TaleWorlds.Library;

namespace SandBox.ViewModelCollection.Missions.NameMarker;

public static class MissionNameMarkerHelper
{
	public static readonly Vec3 AgentHeightOffset = new Vec3(0f, 0f, 0.35f);

	public static readonly Vec3 DefaultHeightOffset = new Vec3(0f, 0f, 2f);

	public const string NameTypeNeutral = "Normal";

	public const string NameTypeFriendly = "Friendly";

	public const string NameTypeEnemy = "Enemy";

	public const string NameTypeNoble = "Noble";

	public const string NameTypePassage = "Passage";

	public const string NameTypeEnemyPassage = "Passage";

	public const string IconTypeCommonArea = "common_area";

	public const string IconTypeStealthArea = "stealth_area";

	public const string IconTypeCharacter = "character";

	public const string IconTypePrisoner = "prisoner";

	public const string IconTypeNoble = "noble";

	public const string IconTypeBarber = "barber";

	public const string IconTypeSentry = "sentry";

	public const string IconTypeBlacksmith = "blacksmith";

	public const string IconTypeGameHost = "game_host";

	public const string IconTypeHermit = "hermit";

	public const string IconTypeShipWright = "shipwright";

	public const string IconTypeQuest = "quest";
}

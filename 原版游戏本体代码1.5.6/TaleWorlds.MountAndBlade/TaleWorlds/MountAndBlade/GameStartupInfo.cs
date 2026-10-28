namespace TaleWorlds.MountAndBlade;

public class GameStartupInfo
{
	public GameStartupType StartupType { get; internal set; }

	public DedicatedServerType DedicatedServerType { get; internal set; }

	public bool PlayerHostedDedicatedServer { get; internal set; }

	public bool IsSinglePlatformServer { get; internal set; }

	public string CustomServerHostIP { get; internal set; } = string.Empty;

	public int ServerPort { get; internal set; }

	public string ServerRegion { get; internal set; }

	public sbyte ServerPriority { get; internal set; }

	public string ServerGameMode { get; internal set; }

	public string CustomGameServerConfigFile { get; internal set; }

	public string CustomGameServerNameOverride { get; internal set; }

	public string CustomGameServerPasswordOverride { get; internal set; }

	public string CustomGameServerAuthToken { get; internal set; }

	public bool CustomGameServerAllowsOptionalModules { get; internal set; } = true;

	public string OverridenUserName { get; internal set; }

	public string PremadeGameType { get; internal set; }

	public int Permission { get; internal set; }

	public string PlatformInterface { get; internal set; }

	public string EpicUserId { get; internal set; }

	public string EpicUserName { get; internal set; }

	public bool IsContinueGame { get; internal set; }

	public double ServerBandwidthLimitInMbps { get; internal set; }

	public int ServerTickRate { get; internal set; }
}

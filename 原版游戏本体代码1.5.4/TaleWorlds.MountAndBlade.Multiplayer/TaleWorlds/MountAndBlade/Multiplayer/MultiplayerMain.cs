using System.Collections.Generic;
using TaleWorlds.Diamond.ClientApplication;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade.Diamond;
using TaleWorlds.PlatformService;
using TaleWorlds.ServiceDiscovery.Client;

namespace TaleWorlds.MountAndBlade.Multiplayer;

public static class MultiplayerMain
{
	private static ClientApplicationConfiguration _lobbyClientApplicationConfiguration;

	private static DiamondClientApplication _diamondClientApplication;

	public static LobbyClient GameClient => NetworkMain.GameClient;

	public static bool IsInitialized { get; private set; }

	static MultiplayerMain()
	{
		IsInitialized = false;
		ServiceAddressManager.Initalize();
		_lobbyClientApplicationConfiguration = new ClientApplicationConfiguration();
		_lobbyClientApplicationConfiguration.FillFrom("LobbyClient");
		ModuleInfo moduleInfo = ModuleHelper.GetModuleInfo("Multiplayer");
		if (!GameNetwork.IsDedicatedServer && moduleInfo != null)
		{
			_diamondClientApplication = new DiamondClientApplication(moduleInfo.Version);
			_diamondClientApplication.Initialize(_lobbyClientApplicationConfiguration);
			NetworkMain.SetPeers(_diamondClientApplication.GetClient<LobbyClient>("LobbyClient"), new CommunityClient(), null);
			MachineId.Initialize();
		}
	}

	public static void Initialize(IGameNetworkHandler gameNetworkHandler)
	{
		Debug.Print("Initializing NetworkMain");
		MBCommon.CurrentGameType = MBCommon.GameType.Single;
		GameNetwork.InitializeCompressionInfos();
		if (!IsInitialized)
		{
			IsInitialized = true;
			GameNetwork.Initialize(gameNetworkHandler);
		}
		PermaMuteList.SetPermanentMuteAvailableCallback(() => PlatformServices.Instance.IsPermanentMuteAvailable);
		Debug.Print("NetworkMain Initialized");
	}

	public static void InitializeAsDedicatedServer(IGameNetworkHandler gameNetworkHandler)
	{
		MBCommon.CurrentGameType = MBCommon.GameType.MultiServer;
		GameNetwork.InitializeCompressionInfos();
		if (!IsInitialized)
		{
			IsInitialized = true;
			GameNetwork.Initialize(gameNetworkHandler);
			GameStartupInfo startupInfo = Module.CurrentModule.StartupInfo;
			GameNetwork.SetServerBandwidthLimitInMbps(startupInfo.ServerBandwidthLimitInMbps);
			GameNetwork.SetServerTickRate(startupInfo.ServerTickRate);
		}
	}

	internal static void Tick(float dt)
	{
		if (IsInitialized)
		{
			if (GameClient != null)
			{
				GameClient.Update();
			}
			if (_diamondClientApplication != null)
			{
				_diamondClientApplication.Update();
			}
			GameNetwork.Tick(dt);
		}
	}

	public static MultiplayerGameType[] GetAvailableRankedGameModes()
	{
		return new MultiplayerGameType[2]
		{
			MultiplayerGameType.Captain,
			MultiplayerGameType.Skirmish
		};
	}

	public static MultiplayerGameType[] GetAvailableCustomGameModes()
	{
		return new MultiplayerGameType[2]
		{
			MultiplayerGameType.TeamDeathmatch,
			MultiplayerGameType.Siege
		};
	}

	public static MultiplayerGameType[] GetAvailableQuickPlayGameModes()
	{
		return new MultiplayerGameType[2]
		{
			MultiplayerGameType.Captain,
			MultiplayerGameType.Skirmish
		};
	}

	public static string[] GetAvailableMatchmakerRegions()
	{
		return new string[4] { "USE", "USW", "EU", "EA" };
	}

	public static string GetUserDefaultRegion()
	{
		return "None";
	}

	public static string GetUserCurrentRegion()
	{
		LobbyClient gameClient = GameClient;
		if (gameClient != null && gameClient.LoggedIn && GameClient.PlayerData != null)
		{
			return GameClient.PlayerData.LastRegion;
		}
		return GetUserDefaultRegion();
	}

	public static string[] GetUserSelectedGameTypes()
	{
		LobbyClient gameClient = GameClient;
		if (gameClient != null && gameClient.LoggedIn)
		{
			return GameClient.PlayerData.LastGameTypes;
		}
		return new string[0];
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("gettoken", "customserver")]
	public static string GetDedicatedCustomServerAuthToken(List<string> strings)
	{
		if (!(Common.PlatformFileHelper is PlatformFileHelperPC))
		{
			return "Platform not supported.";
		}
		if (GameClient == null)
		{
			return "Not logged into lobby.";
		}
		GetDedicatedCustomServerAuthToken();
		return string.Empty;
	}

	private static async void GetDedicatedCustomServerAuthToken()
	{
		string text = await GameClient.GetDedicatedCustomServerAuthToken();
		if (text == null)
		{
			MBDebug.EchoCommandWindow("Could not get token.");
			return;
		}
		PlatformDirectoryPath folderPath = new PlatformDirectoryPath(PlatformFileType.User, "Tokens");
		PlatformFilePath path = new PlatformFilePath(folderPath, "DedicatedCustomServerAuthToken.txt");
		FileHelper.SaveFileString(path, text);
		MBDebug.EchoCommandWindow(text + " (Saved to " + path.FileFullPath + ")");
	}
}

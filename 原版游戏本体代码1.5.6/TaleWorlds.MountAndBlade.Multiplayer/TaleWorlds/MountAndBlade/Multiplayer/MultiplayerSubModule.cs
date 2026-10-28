using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.PlatformService;

namespace TaleWorlds.MountAndBlade.Multiplayer;

public class MultiplayerSubModule : MBSubModuleBase
{
	private bool _isConnectingToMultiplayer;

	protected internal override void OnSubModuleLoad()
	{
		base.OnSubModuleLoad();
		Module.CurrentModule.AddMultiplayerGameMode(new MissionBasedMultiplayerGameMode("TeamDeathmatch"));
		Module.CurrentModule.AddMultiplayerGameMode(new MissionBasedMultiplayerGameMode("Duel"));
		Module.CurrentModule.AddMultiplayerGameMode(new MissionBasedMultiplayerGameMode("Siege"));
		Module.CurrentModule.AddMultiplayerGameMode(new MissionBasedMultiplayerGameMode("Captain"));
		Module.CurrentModule.AddMultiplayerGameMode(new MissionBasedMultiplayerGameMode("Skirmish"));
		Module.CurrentModule.AddMultiplayerGameMode(new MissionBasedMultiplayerGameMode("Battle"));
		TextObject coreContentDisabledReason = new TextObject("{=V8BXjyYq}Disabled during installation.");
		if (Module.CurrentModule.StartupInfo.StartupType != GameStartupType.Singleplayer)
		{
			Module.CurrentModule.AddInitialStateOption(new InitialStateOption("Multiplayer", new TextObject("{=YDYnuBmC}Multiplayer"), 9997, StartMultiplayer, () => (Module.CurrentModule.IsOnlyCoreContentEnabled, coreContentDisabledReason)));
		}
		TauntUsageManager.Initialize();
	}

	public override void OnGameLoaded(Game game, object initializerObject)
	{
		base.OnGameLoaded(game, initializerObject);
		MultiplayerMain.Initialize(new GameNetworkHandler());
	}

	protected internal override void OnApplicationTick(float dt)
	{
		base.OnApplicationTick(dt);
	}

	protected internal override void OnBeforeInitialModuleScreenSetAsRoot()
	{
		base.OnBeforeInitialModuleScreenSetAsRoot();
		if (GameNetwork.IsDedicatedServer)
		{
			MBGameManager.StartNewGame(new MultiplayerGameManager());
		}
	}

	public override void OnInitialState()
	{
		base.OnInitialState();
		if (Utilities.CommandLineArgumentExists("+connect_lobby"))
		{
			MBGameManager.StartNewGame(new MultiplayerGameManager());
		}
		else if (!Module.CurrentModule.IsOnlyCoreContentEnabled && Module.CurrentModule.MultiplayerRequested)
		{
			MBGameManager.StartNewGame(new MultiplayerGameManager());
		}
	}

	private async void StartMultiplayer()
	{
		if (_isConnectingToMultiplayer)
		{
			return;
		}
		_isConnectingToMultiplayer = true;
		bool flag = NetworkMain.GameClient != null && await NetworkMain.GameClient.CheckConnection();
		bool isConnected = flag;
		PlatformServices.Instance.CheckPrivilege(Privilege.Multiplayer, displayResolveUI: true, delegate(bool result)
		{
			if (!isConnected || !result)
			{
				string titleText = new TextObject("{=ksq1IBh3}No connection").ToString();
				string text = new TextObject("{=5VIbo2Cb}No connection could be established to the lobby server. Check your internet connection and try again.").ToString();
				InformationManager.ShowInquiry(new InquiryData(titleText, text, isAffirmativeOptionShown: false, isNegativeOptionShown: true, "", new TextObject("{=dismissnotification}Dismiss").ToString(), null, delegate
				{
					InformationManager.HideInquiry();
				}));
			}
			else
			{
				MBGameManager.StartNewGame(new MultiplayerGameManager());
			}
		});
		_isConnectingToMultiplayer = false;
	}

	protected internal override void OnNetworkTick(float dt)
	{
		base.OnNetworkTick(dt);
		MultiplayerMain.Tick(dt);
		InternetAvailabilityChecker.Tick(dt);
	}
}

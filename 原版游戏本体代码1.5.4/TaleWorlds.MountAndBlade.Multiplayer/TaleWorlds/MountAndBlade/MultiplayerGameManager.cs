using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade.Multiplayer;
using TaleWorlds.PlatformService;

namespace TaleWorlds.MountAndBlade;

public class MultiplayerGameManager : MBGameManager
{
	public MultiplayerGameManager()
	{
		MBMusicManager.Current?.PauseMusicManagerSystem();
	}

	protected override void DoLoadingForGameManager(GameManagerLoadingSteps gameManagerLoadingStep, out GameManagerLoadingSteps nextStep)
	{
		nextStep = GameManagerLoadingSteps.None;
		switch (gameManagerLoadingStep)
		{
		case GameManagerLoadingSteps.PreInitializeZerothStep:
			nextStep = GameManagerLoadingSteps.FirstInitializeFirstStep;
			break;
		case GameManagerLoadingSteps.FirstInitializeFirstStep:
			MBGameManager.LoadModuleData(isLoadGame: false);
			MBDebug.Print("Game creating...");
			MBGlobals.InitializeReferences();
			Game.CreateGame(new MultiplayerGame(), this).DoLoading();
			nextStep = GameManagerLoadingSteps.WaitSecondStep;
			break;
		case GameManagerLoadingSteps.WaitSecondStep:
			MBGameManager.StartNewGame();
			nextStep = GameManagerLoadingSteps.SecondInitializeThirdState;
			break;
		case GameManagerLoadingSteps.SecondInitializeThirdState:
			nextStep = (Game.Current.DoLoading() ? GameManagerLoadingSteps.PostInitializeFourthState : GameManagerLoadingSteps.SecondInitializeThirdState);
			break;
		case GameManagerLoadingSteps.PostInitializeFourthState:
		{
			bool flag = true;
			foreach (MBSubModuleBase item in Module.CurrentModule.CollectSubModules())
			{
				flag = flag && item.DoLoading(Game.Current);
			}
			nextStep = (flag ? GameManagerLoadingSteps.FinishLoadingFifthStep : GameManagerLoadingSteps.PostInitializeFourthState);
			break;
		}
		case GameManagerLoadingSteps.FinishLoadingFifthStep:
			nextStep = GameManagerLoadingSteps.None;
			break;
		}
	}

	public override void OnLoadFinished()
	{
		base.OnLoadFinished();
		MBGlobals.InitializeReferences();
		GameState gameState = null;
		if (GameNetwork.IsDedicatedServer)
		{
			_ = Module.CurrentModule.StartupInfo.DedicatedServerType;
			gameState = Game.Current.GameStateManager.CreateState<UnspecifiedDedicatedServerState>();
			Utilities.SetFrameLimiterWithSleep(value: true);
		}
		else
		{
			gameState = Game.Current.GameStateManager.CreateState<LobbyState>();
		}
		Game.Current.GameStateManager.CleanAndPushState(gameState);
	}

	public override void OnAfterCampaignStart(Game game)
	{
		if (GameNetwork.IsDedicatedServer)
		{
			MultiplayerMain.InitializeAsDedicatedServer(new GameNetworkHandler());
		}
		else
		{
			MultiplayerMain.Initialize(new GameNetworkHandler());
		}
	}

	public override void OnNewCampaignStart(Game game, object starterObject)
	{
		foreach (MBSubModuleBase item in Module.CurrentModule.CollectSubModules())
		{
			item.OnMultiplayerGameStart(game, starterObject);
		}
	}

	public override void OnSessionInvitationAccepted(SessionInvitationType sessionInvitationType)
	{
		if (sessionInvitationType != SessionInvitationType.Multiplayer)
		{
			base.OnSessionInvitationAccepted(sessionInvitationType);
		}
	}

	public override void OnPlatformRequestedMultiplayer()
	{
	}
}

using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterCreationContent;
using TaleWorlds.CampaignSystem.Encounters;
using TaleWorlds.CampaignSystem.GameState;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade;
using TaleWorlds.SaveSystem;
using TaleWorlds.SaveSystem.Load;

namespace SandBox;

public class SandBoxGameManager : MBGameManager
{
	public delegate Campaign CampaignCreatorDelegate();

	private LoadResult _loadedGameResult;

	private CampaignCreatorDelegate _campaignCreator;

	public bool LoadingSavedGame { get; private set; }

	public MetaData MetaData => _loadedGameResult?.MetaData;

	public SandBoxGameManager(CampaignCreatorDelegate campaignCreator)
	{
		LoadingSavedGame = false;
		_campaignCreator = campaignCreator;
	}

	public SandBoxGameManager(LoadResult loadedGameResult)
	{
		LoadingSavedGame = true;
		_loadedGameResult = loadedGameResult;
	}

	public override void OnGameEnd(Game game)
	{
		MBDebug.SetErrorReportScene(null);
		base.OnGameEnd(game);
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
			MBGameManager.LoadModuleData(LoadingSavedGame);
			nextStep = GameManagerLoadingSteps.WaitSecondStep;
			break;
		case GameManagerLoadingSteps.WaitSecondStep:
			if (!LoadingSavedGame)
			{
				MBGameManager.StartNewGame();
			}
			nextStep = GameManagerLoadingSteps.SecondInitializeThirdState;
			break;
		case GameManagerLoadingSteps.SecondInitializeThirdState:
			MBGlobals.InitializeReferences();
			if (!LoadingSavedGame)
			{
				MBDebug.Print("Initializing new game begin...");
				Campaign campaign = _campaignCreator();
				Game.CreateGame(campaign, this);
				campaign.SetLoadingParameters(Campaign.GameLoadingType.NewCampaign);
				MBDebug.Print("Initializing new game end...");
			}
			else
			{
				MBDebug.Print("Initializing saved game begin...");
				((Campaign)Game.LoadSaveGame(_loadedGameResult, this).GameType).SetLoadingParameters(Campaign.GameLoadingType.SavedCampaign);
				_loadedGameResult = null;
				Common.MemoryCleanupGC();
				MBDebug.Print("Initializing saved game end...");
			}
			Game.Current.DoLoading();
			nextStep = GameManagerLoadingSteps.PostInitializeFourthState;
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
			nextStep = (Game.Current.DoLoading() ? GameManagerLoadingSteps.None : GameManagerLoadingSteps.FinishLoadingFifthStep);
			break;
		}
	}

	public override void OnAfterCampaignStart(Game game)
	{
	}

	public override void OnLoadFinished()
	{
		if (!LoadingSavedGame)
		{
			MBDebug.Print("Switching to menu window...");
			if (!Game.Current.IsDevelopmentMode)
			{
				MBDebug.Print("OnLoadFinished Not DevelopmentMode");
				VideoPlaybackState videoPlaybackState = Game.Current.GameStateManager.CreateState<VideoPlaybackState>();
				string text = ModuleHelper.GetModuleFullPath("SandBox") + "Videos/CampaignIntro/";
				string subtitleFileBasePath = text + "campaign_intro";
				string videoPath = text + "campaign_intro.ivf";
				string audioPath = text + "campaign_intro.ogg";
				videoPlaybackState.SetStartingParameters(videoPath, audioPath, subtitleFileBasePath);
				videoPlaybackState.SetOnVideoFinisedDelegate(LaunchSandboxCharacterCreation);
				Game.Current.GameStateManager.CleanAndPushState(videoPlaybackState);
			}
			else
			{
				MBDebug.Print("OnLoadFinished DevelopmentMode");
				MBDebug.Print("Launching Sandbox Character Creation");
				LaunchSandboxCharacterCreation();
			}
		}
		else
		{
			MBDebug.Print("Loading Save Game");
			Game.Current.GameStateManager.OnSavedGameLoadFinished();
			Game.Current.GameStateManager.CleanAndPushState(Game.Current.GameStateManager.CreateState<MapState>());
			MapState mapState = Game.Current.GameStateManager.ActiveState as MapState;
			string text2 = mapState?.GameMenuId;
			if (!string.IsNullOrEmpty(text2))
			{
				if (Campaign.Current.GameMenuManager.GetGameMenu(text2) != null)
				{
					PlayerEncounter.Current?.OnLoad();
					Campaign.Current.GameMenuManager.SetNextMenu(text2);
				}
				else
				{
					PlayerEncounter.Finish();
				}
			}
			PartyBase.MainParty.SetVisualAsDirty();
			Campaign.Current.CampaignInformationManager.OnGameLoaded();
			foreach (Settlement item in Settlement.All)
			{
				item.Party.SetLevelMaskIsDirty();
			}
			CampaignEventDispatcher.Instance.OnGameLoadFinished();
			mapState?.OnLoadingFinished();
		}
		base.IsLoaded = true;
	}

	private void LaunchSandboxCharacterCreation()
	{
		CharacterCreationState gameState = Game.Current.GameStateManager.CreateState<CharacterCreationState>();
		Game.Current.GameStateManager.CleanAndPushState(gameState);
	}
}

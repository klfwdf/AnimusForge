using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.MountAndBlade;

namespace SandBox;

public class EditorSceneMissionManager : MBGameManager
{
	private string _missionName;

	private string _sceneName;

	private string _levels;

	private bool _forReplay;

	private string _replayFileName;

	private bool _isRecord;

	private float _startTime;

	private float _endTime;

	public EditorSceneMissionManager(string missionName, string sceneName, string levels, bool forReplay, string replayFileName, bool isRecord, float startTime, float endTime)
	{
		_missionName = missionName;
		_sceneName = sceneName;
		_levels = levels;
		_forReplay = forReplay;
		_replayFileName = replayFileName;
		_isRecord = isRecord;
		_startTime = startTime;
		_endTime = endTime;
	}

	protected override void DoLoadingForGameManager(GameManagerLoadingSteps gameManagerLoadingSteps, out GameManagerLoadingSteps nextStep)
	{
		nextStep = GameManagerLoadingSteps.None;
		switch (gameManagerLoadingSteps)
		{
		case GameManagerLoadingSteps.PreInitializeZerothStep:
		{
			MBGameManager.LoadModuleData(isLoadGame: false);
			MBDebug.Print("Game creating...");
			MBGlobals.InitializeReferences();
			Game game;
			if (_forReplay)
			{
				game = Game.CreateGame(new EditorGame(), this);
			}
			else
			{
				Campaign campaign = new Campaign(CampaignGameMode.Tutorial);
				game = Game.CreateGame(campaign, this);
				campaign.SetLoadingParameters(Campaign.GameLoadingType.Tutorial);
			}
			game.DoLoading();
			nextStep = GameManagerLoadingSteps.FirstInitializeFirstStep;
			break;
		}
		case GameManagerLoadingSteps.FirstInitializeFirstStep:
		{
			bool flag = true;
			foreach (MBSubModuleBase item in Module.CurrentModule.CollectSubModules())
			{
				flag = flag && item.DoLoading(Game.Current);
			}
			Campaign.Current.DefaultWeatherNodeDimension = 32;
			Campaign.Current.Models.MapWeatherModel.InitializeCaches();
			nextStep = ((!flag) ? GameManagerLoadingSteps.FirstInitializeFirstStep : GameManagerLoadingSteps.WaitSecondStep);
			break;
		}
		case GameManagerLoadingSteps.WaitSecondStep:
			MBGameManager.StartNewGame();
			nextStep = GameManagerLoadingSteps.SecondInitializeThirdState;
			break;
		case GameManagerLoadingSteps.SecondInitializeThirdState:
			nextStep = (Game.Current.DoLoading() ? GameManagerLoadingSteps.PostInitializeFourthState : GameManagerLoadingSteps.SecondInitializeThirdState);
			break;
		case GameManagerLoadingSteps.PostInitializeFourthState:
			nextStep = GameManagerLoadingSteps.FinishLoadingFifthStep;
			break;
		case GameManagerLoadingSteps.FinishLoadingFifthStep:
			nextStep = GameManagerLoadingSteps.None;
			break;
		}
	}

	public override void OnAfterCampaignStart(Game game)
	{
	}

	public override void OnLoadFinished()
	{
		base.OnLoadFinished();
		MBGlobals.InitializeReferences();
		if (!_forReplay)
		{
			Campaign.Current.InitializeGamePlayReferences();
		}
		Module.CurrentModule.StartMissionForEditorAux(_missionName, _sceneName, _levels, _forReplay, _replayFileName, _isRecord);
		MissionState.Current.MissionReplayStartTime = _startTime;
		MissionState.Current.MissionEndTime = _endTime;
	}
}

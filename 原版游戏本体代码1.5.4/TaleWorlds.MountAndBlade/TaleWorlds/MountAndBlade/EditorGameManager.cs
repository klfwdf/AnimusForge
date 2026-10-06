using TaleWorlds.Core;

namespace TaleWorlds.MountAndBlade;

public class EditorGameManager : MBGameManager
{
	protected override void DoLoadingForGameManager(GameManagerLoadingSteps gameManagerLoadingStep, out GameManagerLoadingSteps nextStep)
	{
		nextStep = GameManagerLoadingSteps.None;
		switch (gameManagerLoadingStep)
		{
		case GameManagerLoadingSteps.PreInitializeZerothStep:
			MBGameManager.LoadModuleData(isLoadGame: false);
			MBGlobals.InitializeReferences();
			Game.CreateGame(new EditorGame(), this).DoLoading();
			nextStep = GameManagerLoadingSteps.FirstInitializeFirstStep;
			break;
		case GameManagerLoadingSteps.FirstInitializeFirstStep:
		{
			bool flag = true;
			foreach (MBSubModuleBase item in Module.CurrentModule.CollectSubModules())
			{
				flag = flag && item.DoLoading(Game.Current);
			}
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
	}
}

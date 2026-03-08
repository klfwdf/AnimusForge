using System;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;

namespace SandBox
{
	// Token: 0x02000006 RID: 6
	public class EditorSceneMissionManager : MBGameManager
	{
		// Token: 0x0600000C RID: 12 RVA: 0x00003340 File Offset: 0x00001540
		public EditorSceneMissionManager(string missionName, string sceneName, string levels, bool forReplay, string replayFileName, bool isRecord, float startTime, float endTime)
		{
			this._missionName = missionName;
			this._sceneName = sceneName;
			this._levels = levels;
			this._forReplay = forReplay;
			this._replayFileName = replayFileName;
			this._isRecord = isRecord;
			this._startTime = startTime;
			this._endTime = endTime;
		}

		// Token: 0x0600000D RID: 13 RVA: 0x00003390 File Offset: 0x00001590
		protected override void DoLoadingForGameManager(GameManagerLoadingSteps gameManagerLoadingSteps, out GameManagerLoadingSteps nextStep)
		{
			nextStep = GameManagerLoadingSteps.None;
			switch (gameManagerLoadingSteps)
			{
			case GameManagerLoadingSteps.PreInitializeZerothStep:
			{
				MBGameManager.LoadModuleData(false);
				MBDebug.Print("Game creating...", 0, Debug.DebugColor.White, 17592186044416UL);
				MBGlobals.InitializeReferences();
				Game game;
				if (this._forReplay)
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
				return;
			}
			case GameManagerLoadingSteps.FirstInitializeFirstStep:
			{
				bool flag = true;
				foreach (MBSubModuleBase mbsubModuleBase in Module.CurrentModule.CollectSubModules())
				{
					flag = flag && mbsubModuleBase.DoLoading(Game.Current);
				}
				Campaign.Current.DefaultWeatherNodeDimension = 32;
				Campaign.Current.Models.MapWeatherModel.InitializeCaches();
				nextStep = (flag ? GameManagerLoadingSteps.WaitSecondStep : GameManagerLoadingSteps.FirstInitializeFirstStep);
				return;
			}
			case GameManagerLoadingSteps.WaitSecondStep:
				MBGameManager.StartNewGame();
				nextStep = GameManagerLoadingSteps.SecondInitializeThirdState;
				return;
			case GameManagerLoadingSteps.SecondInitializeThirdState:
				nextStep = (Game.Current.DoLoading() ? GameManagerLoadingSteps.PostInitializeFourthState : GameManagerLoadingSteps.SecondInitializeThirdState);
				return;
			case GameManagerLoadingSteps.PostInitializeFourthState:
				nextStep = GameManagerLoadingSteps.FinishLoadingFifthStep;
				return;
			case GameManagerLoadingSteps.FinishLoadingFifthStep:
				nextStep = GameManagerLoadingSteps.None;
				return;
			default:
				return;
			}
		}

		// Token: 0x0600000E RID: 14 RVA: 0x000034BC File Offset: 0x000016BC
		public override void OnAfterCampaignStart(Game game)
		{
		}

		// Token: 0x0600000F RID: 15 RVA: 0x000034C0 File Offset: 0x000016C0
		public override void OnLoadFinished()
		{
			base.OnLoadFinished();
			MBGlobals.InitializeReferences();
			if (!this._forReplay)
			{
				Campaign.Current.InitializeGamePlayReferences();
			}
			Module.CurrentModule.StartMissionForEditorAux(this._missionName, this._sceneName, this._levels, this._forReplay, this._replayFileName, this._isRecord);
			MissionState.Current.MissionReplayStartTime = this._startTime;
			MissionState.Current.MissionEndTime = this._endTime;
		}

		// Token: 0x0400001F RID: 31
		private string _missionName;

		// Token: 0x04000020 RID: 32
		private string _sceneName;

		// Token: 0x04000021 RID: 33
		private string _levels;

		// Token: 0x04000022 RID: 34
		private bool _forReplay;

		// Token: 0x04000023 RID: 35
		private string _replayFileName;

		// Token: 0x04000024 RID: 36
		private bool _isRecord;

		// Token: 0x04000025 RID: 37
		private float _startTime;

		// Token: 0x04000026 RID: 38
		private float _endTime;
	}
}

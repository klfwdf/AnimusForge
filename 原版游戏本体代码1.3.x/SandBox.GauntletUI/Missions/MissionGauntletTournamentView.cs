using System;
using SandBox.Tournaments.MissionLogics;
using SandBox.View.Missions.Tournaments;
using SandBox.ViewModelCollection.Tournament;
using TaleWorlds.CampaignSystem.TournamentGames;
using TaleWorlds.Core;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.ScreenSystem;

namespace SandBox.GauntletUI.Missions
{
	// Token: 0x02000023 RID: 35
	[OverrideView(typeof(MissionTournamentView))]
	public class MissionGauntletTournamentView : MissionView
	{
		// Token: 0x060001D3 RID: 467 RVA: 0x0000BCB9 File Offset: 0x00009EB9
		public MissionGauntletTournamentView()
		{
			this.ViewOrderPriority = 48;
		}

		// Token: 0x060001D4 RID: 468 RVA: 0x0000BCD0 File Offset: 0x00009ED0
		public override void OnMissionScreenInitialize()
		{
			base.OnMissionScreenInitialize();
			this._dataSource = new TournamentVM(new Action(this.DisableUi), this._behavior);
			this._gauntletLayer = new GauntletLayer("MissionTournament", this.ViewOrderPriority, false);
			this._gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			this._gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			this._gauntletLayer.IsFocusLayer = true;
			ScreenManager.TrySetFocus(this._gauntletLayer);
			this._dataSource.SetDoneInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Confirm"));
			this._dataSource.SetCancelInputKey(HotKeyManager.GetCategory("GenericPanelGameKeyCategory").GetHotKey("Exit"));
			this._gauntletMovie = this._gauntletLayer.LoadMovie("Tournament", this._dataSource);
			base.MissionScreen.CustomCamera = this._customCamera;
			base.MissionScreen.AddLayer(this._gauntletLayer);
		}

		// Token: 0x060001D5 RID: 469 RVA: 0x0000BDD8 File Offset: 0x00009FD8
		public override void OnMissionScreenFinalize()
		{
			this._gauntletLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(this._gauntletLayer);
			this._gauntletLayer.InputRestrictions.ResetInputRestrictions();
			this._gauntletMovie = null;
			this._gauntletLayer = null;
			this._dataSource.OnFinalize();
			this._dataSource = null;
			base.OnMissionScreenFinalize();
		}

		// Token: 0x060001D6 RID: 470 RVA: 0x0000BE34 File Offset: 0x0000A034
		public override void AfterStart()
		{
			this._behavior = base.Mission.GetMissionBehavior<TournamentBehavior>();
			GameEntity gameEntity = base.Mission.Scene.FindEntityWithTag("camera_instance");
			this._customCamera = Camera.CreateCamera();
			Vec3 vec = default(Vec3);
			gameEntity.GetCameraParamsFromCameraScript(this._customCamera, ref vec);
		}

		// Token: 0x060001D7 RID: 471 RVA: 0x0000BE88 File Offset: 0x0000A088
		public override void OnMissionTick(float dt)
		{
			if (this._behavior == null)
			{
				return;
			}
			if (this._gauntletLayer.IsFocusLayer && this._dataSource.IsCurrentMatchActive)
			{
				this._gauntletLayer.InputRestrictions.ResetInputRestrictions();
				this._gauntletLayer.IsFocusLayer = false;
				ScreenManager.TryLoseFocus(this._gauntletLayer);
			}
			else if (!this._gauntletLayer.IsFocusLayer && !this._dataSource.IsCurrentMatchActive)
			{
				this._gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
				this._gauntletLayer.IsFocusLayer = true;
				ScreenManager.TrySetFocus(this._gauntletLayer);
			}
			if (this._dataSource.IsBetWindowEnabled)
			{
				if (this._gauntletLayer.Input.IsHotKeyReleased("Confirm"))
				{
					UISoundsHelper.PlayUISound("event:/ui/default");
					this._dataSource.ExecuteBet();
					this._dataSource.IsBetWindowEnabled = false;
				}
				else if (this._gauntletLayer.Input.IsHotKeyReleased("Exit"))
				{
					UISoundsHelper.PlayUISound("event:/ui/default");
					this._dataSource.IsBetWindowEnabled = false;
				}
			}
			if (!this._viewEnabled && ((this._behavior.LastMatch != null && this._behavior.CurrentMatch == null) || this._behavior.CurrentMatch.IsReady))
			{
				this._dataSource.Refresh();
				this.ShowUi();
			}
			if (!this._viewEnabled && this._dataSource.CurrentMatch.IsValid)
			{
				TournamentMatch currentMatch = this._behavior.CurrentMatch;
				if (currentMatch != null && currentMatch.State == TournamentMatch.MatchState.Started)
				{
					this._dataSource.CurrentMatch.RefreshActiveMatch();
				}
			}
			if (this._dataSource.IsOver && this._viewEnabled && !base.DebugInput.IsControlDown() && base.DebugInput.IsHotKeyPressed("ShowHighlightsSummary"))
			{
				HighlightsController missionBehavior = base.Mission.GetMissionBehavior<HighlightsController>();
				if (missionBehavior == null)
				{
					return;
				}
				missionBehavior.ShowSummary();
			}
		}

		// Token: 0x060001D8 RID: 472 RVA: 0x0000C06C File Offset: 0x0000A26C
		private void DisableUi()
		{
			if (!this._viewEnabled)
			{
				return;
			}
			base.MissionScreen.UpdateFreeCamera(this._customCamera.Frame);
			base.MissionScreen.CustomCamera = null;
			this._viewEnabled = false;
			this._gauntletLayer.InputRestrictions.ResetInputRestrictions();
		}

		// Token: 0x060001D9 RID: 473 RVA: 0x0000C0BB File Offset: 0x0000A2BB
		private void ShowUi()
		{
			if (this._viewEnabled)
			{
				return;
			}
			base.MissionScreen.CustomCamera = this._customCamera;
			this._viewEnabled = true;
			this._gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
		}

		// Token: 0x060001DA RID: 474 RVA: 0x0000C0F0 File Offset: 0x0000A2F0
		public override bool IsOpeningEscapeMenuOnFocusChangeAllowed()
		{
			return !this._viewEnabled;
		}

		// Token: 0x060001DB RID: 475 RVA: 0x0000C0FB File Offset: 0x0000A2FB
		public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow killingBlow)
		{
			base.OnAgentRemoved(affectedAgent, affectorAgent, agentState, killingBlow);
			this._dataSource.OnAgentRemoved(affectedAgent);
		}

		// Token: 0x060001DC RID: 476 RVA: 0x0000C114 File Offset: 0x0000A314
		public override void OnPhotoModeActivated()
		{
			base.OnPhotoModeActivated();
			if (this._gauntletLayer != null)
			{
				this._gauntletLayer.UIContext.ContextAlpha = 0f;
			}
		}

		// Token: 0x060001DD RID: 477 RVA: 0x0000C139 File Offset: 0x0000A339
		public override void OnPhotoModeDeactivated()
		{
			base.OnPhotoModeDeactivated();
			if (this._gauntletLayer != null)
			{
				this._gauntletLayer.UIContext.ContextAlpha = 1f;
			}
		}

		// Token: 0x04000099 RID: 153
		private TournamentBehavior _behavior;

		// Token: 0x0400009A RID: 154
		private Camera _customCamera;

		// Token: 0x0400009B RID: 155
		private bool _viewEnabled = true;

		// Token: 0x0400009C RID: 156
		private GauntletMovieIdentifier _gauntletMovie;

		// Token: 0x0400009D RID: 157
		private GauntletLayer _gauntletLayer;

		// Token: 0x0400009E RID: 158
		private TournamentVM _dataSource;
	}
}

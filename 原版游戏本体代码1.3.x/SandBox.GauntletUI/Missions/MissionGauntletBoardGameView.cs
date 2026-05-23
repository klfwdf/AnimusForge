using System;
using SandBox.BoardGames.MissionLogics;
using SandBox.ViewModelCollection.BoardGame;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Screens;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI.Missions
{
	// Token: 0x0200001B RID: 27
	[OverrideView(typeof(BoardGameView))]
	public class MissionGauntletBoardGameView : MissionView, IBoardGameHandler
	{
		// Token: 0x17000034 RID: 52
		// (get) Token: 0x06000184 RID: 388 RVA: 0x0000A811 File Offset: 0x00008A11
		// (set) Token: 0x06000185 RID: 389 RVA: 0x0000A819 File Offset: 0x00008A19
		public MissionBoardGameLogic _missionBoardGameHandler { get; private set; }

		// Token: 0x17000035 RID: 53
		// (get) Token: 0x06000186 RID: 390 RVA: 0x0000A822 File Offset: 0x00008A22
		// (set) Token: 0x06000187 RID: 391 RVA: 0x0000A82A File Offset: 0x00008A2A
		public Camera Camera { get; private set; }

		// Token: 0x06000188 RID: 392 RVA: 0x0000A833 File Offset: 0x00008A33
		public MissionGauntletBoardGameView()
		{
			this.ViewOrderPriority = 2;
		}

		// Token: 0x06000189 RID: 393 RVA: 0x0000A842 File Offset: 0x00008A42
		public override void OnMissionScreenInitialize()
		{
			base.OnMissionScreenInitialize();
			base.MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("BoardGameHotkeyCategory"));
		}

		// Token: 0x0600018A RID: 394 RVA: 0x0000A869 File Offset: 0x00008A69
		public override void OnMissionScreenActivate()
		{
			base.OnMissionScreenActivate();
			this._missionBoardGameHandler = base.Mission.GetMissionBehavior<MissionBoardGameLogic>();
			if (this._missionBoardGameHandler != null)
			{
				this._missionBoardGameHandler.Handler = this;
			}
		}

		// Token: 0x0600018B RID: 395 RVA: 0x0000A896 File Offset: 0x00008A96
		public override bool OnEscape()
		{
			return this._dataSource != null;
		}

		// Token: 0x0600018C RID: 396 RVA: 0x0000A8A1 File Offset: 0x00008AA1
		void IBoardGameHandler.Activate()
		{
			this._dataSource.Activate();
		}

		// Token: 0x0600018D RID: 397 RVA: 0x0000A8AE File Offset: 0x00008AAE
		void IBoardGameHandler.SwitchTurns()
		{
			BoardGameVM dataSource = this._dataSource;
			if (dataSource == null)
			{
				return;
			}
			dataSource.SwitchTurns();
		}

		// Token: 0x0600018E RID: 398 RVA: 0x0000A8C0 File Offset: 0x00008AC0
		void IBoardGameHandler.DiceRoll(int roll)
		{
			BoardGameVM dataSource = this._dataSource;
			if (dataSource == null)
			{
				return;
			}
			dataSource.DiceRoll(roll);
		}

		// Token: 0x0600018F RID: 399 RVA: 0x0000A8D4 File Offset: 0x00008AD4
		void IBoardGameHandler.Install()
		{
			this._spriteCategory = UIResourceManager.LoadSpriteCategory("ui_boardgame");
			this._dataSource = new BoardGameVM();
			this._dataSource.SetRollDiceKey(HotKeyManager.GetCategory("BoardGameHotkeyCategory").GetHotKey("BoardGameRollDice"));
			this._gauntletLayer = new GauntletLayer("MissionBoardGame", this.ViewOrderPriority, false);
			this._gauntletMovie = this._gauntletLayer.LoadMovie("BoardGame", this._dataSource);
			this._gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
			this._cameraHolder = base.Mission.Scene.FindEntityWithTag("camera_holder");
			this.CreateCamera();
			if (this._cameraHolder == null)
			{
				this._cameraHolder = base.Mission.Scene.FindEntityWithTag("camera_holder");
			}
			if (this.Camera == null)
			{
				this.CreateCamera();
			}
			this._gauntletLayer.InputRestrictions.SetInputRestrictions(true, InputUsageMask.All);
			this._missionMouseVisibilityState = base.MissionScreen.SceneLayer.InputRestrictions.MouseVisibility;
			this._missionInputRestrictions = base.MissionScreen.SceneLayer.InputRestrictions.InputUsageMask;
			base.MissionScreen.SceneLayer.InputRestrictions.SetInputRestrictions(false, InputUsageMask.All);
			base.MissionScreen.SceneLayer.IsFocusLayer = true;
			base.MissionScreen.AddLayer(this._gauntletLayer);
			base.MissionScreen.SetLayerCategoriesStateAndDeactivateOthers(new string[] { "SceneLayer", "MissionBoardGame" }, true);
			ScreenManager.TrySetFocus(base.MissionScreen.SceneLayer);
			this.SetStaticCamera();
		}

		// Token: 0x06000190 RID: 400 RVA: 0x0000AA80 File Offset: 0x00008C80
		void IBoardGameHandler.Uninstall()
		{
			if (this._dataSource != null)
			{
				this._dataSource.OnFinalize();
				this._dataSource = null;
			}
			this._gauntletLayer.IsFocusLayer = false;
			ScreenManager.TryLoseFocus(this._gauntletLayer);
			this._gauntletLayer.InputRestrictions.ResetInputRestrictions();
			base.MissionScreen.SceneLayer.InputRestrictions.SetInputRestrictions(this._missionMouseVisibilityState, this._missionInputRestrictions);
			base.MissionScreen.RemoveLayer(this._gauntletLayer);
			this._gauntletMovie = null;
			this._gauntletLayer = null;
			this.Camera = null;
			this._cameraHolder = null;
			base.MissionScreen.CustomCamera = null;
			base.MissionScreen.SetLayerCategoriesStateAndToggleOthers(new string[] { "MissionBoardGame" }, false);
			base.MissionScreen.SetLayerCategoriesState(new string[] { "SceneLayer" }, true);
			this._spriteCategory.Unload();
		}

		// Token: 0x06000191 RID: 401 RVA: 0x0000AB68 File Offset: 0x00008D68
		private bool IsHotkeyPressedInAnyLayer(string hotkeyID)
		{
			SceneLayer sceneLayer = base.MissionScreen.SceneLayer;
			bool flag = sceneLayer != null && sceneLayer.Input.IsHotKeyPressed(hotkeyID);
			GauntletLayer gauntletLayer = this._gauntletLayer;
			bool flag2 = gauntletLayer != null && gauntletLayer.Input.IsHotKeyPressed(hotkeyID);
			return flag || flag2;
		}

		// Token: 0x06000192 RID: 402 RVA: 0x0000ABB0 File Offset: 0x00008DB0
		private bool IsHotkeyDownInAnyLayer(string hotkeyID)
		{
			SceneLayer sceneLayer = base.MissionScreen.SceneLayer;
			bool flag = sceneLayer != null && sceneLayer.Input.IsHotKeyDown(hotkeyID);
			GauntletLayer gauntletLayer = this._gauntletLayer;
			bool flag2 = gauntletLayer != null && gauntletLayer.Input.IsHotKeyDown(hotkeyID);
			return flag || flag2;
		}

		// Token: 0x06000193 RID: 403 RVA: 0x0000ABF8 File Offset: 0x00008DF8
		private bool IsGameKeyReleasedInAnyLayer(string hotKeyID)
		{
			SceneLayer sceneLayer = base.MissionScreen.SceneLayer;
			bool flag = sceneLayer != null && sceneLayer.Input.IsHotKeyReleased(hotKeyID);
			GauntletLayer gauntletLayer = this._gauntletLayer;
			bool flag2 = gauntletLayer != null && gauntletLayer.Input.IsHotKeyReleased(hotKeyID);
			return flag || flag2;
		}

		// Token: 0x06000194 RID: 404 RVA: 0x0000AC40 File Offset: 0x00008E40
		private void CreateCamera()
		{
			this.Camera = Camera.CreateCamera();
			if (this._cameraHolder != null)
			{
				this.Camera.Entity = this._cameraHolder;
			}
			this.Camera.SetFovVertical(0.7853982f, 1.7777778f, 0.01f, 3000f);
		}

		// Token: 0x06000195 RID: 405 RVA: 0x0000AC98 File Offset: 0x00008E98
		private void SetStaticCamera()
		{
			if (this._cameraHolder != null && this.Camera.Entity != null)
			{
				base.MissionScreen.CustomCamera = this.Camera;
				return;
			}
			Debug.FailedAssert("[DEBUG]Camera entities are null.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.GauntletUI\\Missions\\MissionGauntletBoardGameView.cs", "SetStaticCamera", 189);
		}

		// Token: 0x06000196 RID: 406 RVA: 0x0000ACF4 File Offset: 0x00008EF4
		public override void OnMissionScreenTick(float dt)
		{
			MissionBoardGameLogic missionBoardGameHandler = this._missionBoardGameHandler;
			if (missionBoardGameHandler != null && missionBoardGameHandler.IsGameInProgress)
			{
				MissionScreen missionScreen = base.MissionScreen;
				if (missionScreen == null || !missionScreen.IsPhotoModeEnabled)
				{
					base.OnMissionScreenTick(dt);
					if (this._gauntletLayer != null && this._dataSource != null)
					{
						if (this.IsHotkeyPressedInAnyLayer("Exit"))
						{
							this._dataSource.ExecuteForfeit();
						}
						else if (this.IsHotkeyPressedInAnyLayer("BoardGameRollDice") && this._dataSource.IsGameUsingDice)
						{
							this._dataSource.ExecuteRoll();
						}
					}
					if (this._missionBoardGameHandler.Board != null)
					{
						Vec3 rayBegin;
						Vec3 rayEnd;
						base.MissionScreen.ScreenPointToWorldRay(base.Input.GetMousePositionRanged(), out rayBegin, out rayEnd);
						this._missionBoardGameHandler.Board.SetUserRay(rayBegin, rayEnd);
					}
					return;
				}
			}
		}

		// Token: 0x06000197 RID: 407 RVA: 0x0000ADBC File Offset: 0x00008FBC
		public override void OnMissionScreenFinalize()
		{
			if (this._dataSource != null)
			{
				this._dataSource.OnFinalize();
				this._dataSource = null;
			}
			this._gauntletLayer = null;
			this._gauntletMovie = null;
			base.OnMissionScreenFinalize();
		}

		// Token: 0x06000198 RID: 408 RVA: 0x0000ADEC File Offset: 0x00008FEC
		public override void OnPhotoModeActivated()
		{
			base.OnPhotoModeActivated();
			if (this._gauntletLayer != null)
			{
				this._gauntletLayer.UIContext.ContextAlpha = 0f;
			}
		}

		// Token: 0x06000199 RID: 409 RVA: 0x0000AE11 File Offset: 0x00009011
		public override void OnPhotoModeDeactivated()
		{
			base.OnPhotoModeDeactivated();
			if (this._gauntletLayer != null)
			{
				this._gauntletLayer.UIContext.ContextAlpha = 1f;
			}
		}

		// Token: 0x04000079 RID: 121
		private BoardGameVM _dataSource;

		// Token: 0x0400007A RID: 122
		private GauntletLayer _gauntletLayer;

		// Token: 0x0400007B RID: 123
		private GauntletMovieIdentifier _gauntletMovie;

		// Token: 0x0400007E RID: 126
		private GameEntity _cameraHolder;

		// Token: 0x0400007F RID: 127
		private SpriteCategory _spriteCategory;

		// Token: 0x04000080 RID: 128
		private bool _missionMouseVisibilityState;

		// Token: 0x04000081 RID: 129
		private InputUsageMask _missionInputRestrictions;
	}
}

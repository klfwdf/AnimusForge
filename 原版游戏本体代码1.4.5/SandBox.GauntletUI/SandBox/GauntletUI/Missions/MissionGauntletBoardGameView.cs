using System;
using SandBox.BoardGames.MissionLogics;
using SandBox.ViewModelCollection.BoardGame;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Source.Missions.Handlers;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.MissionViews;
using TaleWorlds.MountAndBlade.View.MissionViews.Singleplayer;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace SandBox.GauntletUI.Missions;

[OverrideView(typeof(BoardGameView))]
public class MissionGauntletBoardGameView : MissionView, IBoardGameHandler
{
	private BoardGameVM _dataSource;

	private GauntletLayer _gauntletLayer;

	private GauntletMovieIdentifier _gauntletMovie;

	private GameEntity _cameraHolder;

	private SpriteCategory _spriteCategory;

	private bool _missionMouseVisibilityState;

	private InputUsageMask _missionInputRestrictions;

	public MissionBoardGameLogic _missionBoardGameHandler { get; private set; }

	public Camera Camera { get; private set; }

	public MissionGauntletBoardGameView()
	{
		ViewOrderPriority = 2;
	}

	public override void OnMissionScreenInitialize()
	{
		base.OnMissionScreenInitialize();
		base.MissionScreen.SceneLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("BoardGameHotkeyCategory"));
	}

	public override void OnMissionScreenActivate()
	{
		base.OnMissionScreenActivate();
		_missionBoardGameHandler = base.Mission.GetMissionBehavior<MissionBoardGameLogic>();
		if (_missionBoardGameHandler != null)
		{
			_missionBoardGameHandler.Handler = this;
		}
	}

	public override bool OnEscape()
	{
		return _dataSource != null;
	}

	void IBoardGameHandler.Activate()
	{
		_dataSource.Activate();
	}

	void IBoardGameHandler.SwitchTurns()
	{
		_dataSource?.SwitchTurns();
	}

	void IBoardGameHandler.DiceRoll(int roll)
	{
		_dataSource?.DiceRoll(roll);
	}

	void IBoardGameHandler.Install()
	{
		_spriteCategory = UIResourceManager.LoadSpriteCategory("ui_boardgame");
		_dataSource = new BoardGameVM();
		_dataSource.SetRollDiceKey(HotKeyManager.GetCategory("BoardGameHotkeyCategory").GetHotKey("BoardGameRollDice"));
		_gauntletLayer = new GauntletLayer("MissionBoardGame", ViewOrderPriority);
		_gauntletMovie = _gauntletLayer.LoadMovie("BoardGame", _dataSource);
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		_cameraHolder = base.Mission.Scene.FindEntityWithTag("camera_holder");
		CreateCamera();
		if (_cameraHolder == null)
		{
			_cameraHolder = base.Mission.Scene.FindEntityWithTag("camera_holder");
		}
		if (Camera == null)
		{
			CreateCamera();
		}
		_gauntletLayer.InputRestrictions.SetInputRestrictions();
		_missionMouseVisibilityState = base.MissionScreen.SceneLayer.InputRestrictions.MouseVisibility;
		_missionInputRestrictions = base.MissionScreen.SceneLayer.InputRestrictions.InputUsageMask;
		base.MissionScreen.SceneLayer.InputRestrictions.SetInputRestrictions(isMouseVisible: false);
		base.MissionScreen.SceneLayer.IsFocusLayer = true;
		base.MissionScreen.AddLayer(_gauntletLayer);
		base.MissionScreen.SetLayerCategoriesStateAndDeactivateOthers(new string[2] { "SceneLayer", "MissionBoardGame" }, isActive: true);
		ScreenManager.TrySetFocus(base.MissionScreen.SceneLayer);
		SetStaticCamera();
	}

	void IBoardGameHandler.Uninstall()
	{
		if (_dataSource != null)
		{
			_dataSource.OnFinalize();
			_dataSource = null;
		}
		_gauntletLayer.IsFocusLayer = false;
		ScreenManager.TryLoseFocus(_gauntletLayer);
		_gauntletLayer.InputRestrictions.ResetInputRestrictions();
		base.MissionScreen.SceneLayer.InputRestrictions.SetInputRestrictions(_missionMouseVisibilityState, _missionInputRestrictions);
		base.MissionScreen.RemoveLayer(_gauntletLayer);
		_gauntletMovie = null;
		_gauntletLayer = null;
		Camera = null;
		_cameraHolder = null;
		base.MissionScreen.CustomCamera = null;
		base.MissionScreen.SetLayerCategoriesStateAndToggleOthers(new string[1] { "MissionBoardGame" }, isActive: false);
		base.MissionScreen.SetLayerCategoriesState(new string[1] { "SceneLayer" }, isActive: true);
		_spriteCategory.Unload();
	}

	private bool IsHotkeyPressedInAnyLayer(string hotkeyID)
	{
		bool num = base.MissionScreen.SceneLayer?.Input.IsHotKeyPressed(hotkeyID) ?? false;
		bool flag = _gauntletLayer?.Input.IsHotKeyPressed(hotkeyID) ?? false;
		return num || flag;
	}

	private bool IsHotkeyDownInAnyLayer(string hotkeyID)
	{
		bool num = base.MissionScreen.SceneLayer?.Input.IsHotKeyDown(hotkeyID) ?? false;
		bool flag = _gauntletLayer?.Input.IsHotKeyDown(hotkeyID) ?? false;
		return num || flag;
	}

	private bool IsGameKeyReleasedInAnyLayer(string hotKeyID)
	{
		bool num = base.MissionScreen.SceneLayer?.Input.IsHotKeyReleased(hotKeyID) ?? false;
		bool flag = _gauntletLayer?.Input.IsHotKeyReleased(hotKeyID) ?? false;
		return num || flag;
	}

	private void CreateCamera()
	{
		Camera = Camera.CreateCamera();
		if (_cameraHolder != null)
		{
			Camera.Entity = _cameraHolder;
		}
		Camera.SetFovVertical(System.MathF.PI / 4f, 1.7777778f, 0.01f, 3000f);
	}

	private void SetStaticCamera()
	{
		if (_cameraHolder != null && Camera.Entity != null)
		{
			base.MissionScreen.CustomCamera = Camera;
		}
		else
		{
			Debug.FailedAssert("[DEBUG]Camera entities are null.", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\SandBox.GauntletUI\\Missions\\MissionGauntletBoardGameView.cs", "SetStaticCamera", 189);
		}
	}

	public override void OnMissionScreenTick(float dt)
	{
		MissionBoardGameLogic missionBoardGameHandler = _missionBoardGameHandler;
		if (missionBoardGameHandler == null || !missionBoardGameHandler.IsGameInProgress)
		{
			return;
		}
		MissionScreen missionScreen = base.MissionScreen;
		if (missionScreen != null && missionScreen.IsPhotoModeEnabled)
		{
			return;
		}
		base.OnMissionScreenTick(dt);
		if (_gauntletLayer != null && _dataSource != null)
		{
			if (IsHotkeyPressedInAnyLayer("Exit"))
			{
				_dataSource.ExecuteForfeit();
			}
			else if (IsHotkeyPressedInAnyLayer("BoardGameRollDice") && _dataSource.IsGameUsingDice)
			{
				_dataSource.ExecuteRoll();
			}
		}
		if (_missionBoardGameHandler.Board != null)
		{
			base.MissionScreen.ScreenPointToWorldRay(base.Input.GetMousePositionRanged(), out var rayBegin, out var rayEnd);
			_missionBoardGameHandler.Board.SetUserRay(rayBegin, rayEnd);
		}
	}

	public override void OnMissionScreenFinalize()
	{
		if (_dataSource != null)
		{
			_dataSource.OnFinalize();
			_dataSource = null;
		}
		_gauntletLayer = null;
		_gauntletMovie = null;
		base.OnMissionScreenFinalize();
	}

	public override void OnPhotoModeActivated()
	{
		base.OnPhotoModeActivated();
		if (_gauntletLayer != null)
		{
			_gauntletLayer.UIContext.ContextAlpha = 0f;
		}
	}

	public override void OnPhotoModeDeactivated()
	{
		base.OnPhotoModeDeactivated();
		if (_gauntletLayer != null)
		{
			_gauntletLayer.UIContext.ContextAlpha = 1f;
		}
	}
}

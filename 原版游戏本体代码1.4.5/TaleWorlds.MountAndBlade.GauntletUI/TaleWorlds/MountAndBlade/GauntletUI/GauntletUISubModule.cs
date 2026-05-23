using System;
using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Information;
using TaleWorlds.Core.ViewModelCollection.Information.RundownTooltip;
using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.Engine.Options;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.ExtraWidgets;
using TaleWorlds.GauntletUI.GamepadNavigation;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.GauntletUI.SceneNotification;
using TaleWorlds.MountAndBlade.GauntletUI.Widgets;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI;

public class GauntletUISubModule : MBSubModuleBase
{
	private bool _initialized;

	private bool _isMultiplayer;

	private GauntletQueryManager _queryManager;

	private SpriteCategory _fullBackgroundCategory;

	private SpriteCategory _fullscreensCategory;

	private bool _areResourcesDirty;

	private bool _loadingWindowCreated;

	public static GauntletUISubModule Instance { get; private set; }

	protected override void OnSubModuleLoad()
	{
		base.OnSubModuleLoad();
		CustomWidgetManager.TouchAssembly();
		BannerlordCustomWidgetManager.TouchAssembly();
		RefreshResources(initialLoad: true);
		ScreenManager.OnControllerDisconnected += OnControllerDisconnected;
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Combine(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
		NativeOptions.GetConfig(NativeOptions.NativeOptionsType.DisplayMode);
		GauntletGamepadNavigationManager.Initialize();
		GauntletGameVersionView.AddModuleVersionInfo("Bannerlord", Utilities.GetApplicationVersionWithBuildNumber().ToString());
		Instance = this;
	}

	private void RefreshResources(bool initialLoad)
	{
		Dictionary<GauntletLayer, List<GauntletMovieIdentifier>> dictionary = new Dictionary<GauntletLayer, List<GauntletMovieIdentifier>>();
		if (!initialLoad)
		{
			foreach (ScreenLayer sortedLayer in ScreenManager.SortedLayers)
			{
				if (sortedLayer is GauntletLayer gauntletLayer)
				{
					gauntletLayer.OnResourceRefreshBegin(out var previouslyLoadedMovies);
					dictionary.Add(gauntletLayer, previouslyLoadedMovies);
				}
			}
		}
		WidgetInfo.Refresh();
		UIResourceManager.Refresh();
		_fullBackgroundCategory?.Unload();
		_fullscreensCategory?.Unload();
		_fullBackgroundCategory = UIResourceManager.LoadSpriteCategory("ui_fullbackgrounds");
		_fullscreensCategory = UIResourceManager.LoadSpriteCategory("ui_fullscreens");
		GauntletGameVersionView.Refresh();
		SpriteCategory[] array = UIResourceManager.SpriteData.SpriteCategories.Values.Where((SpriteCategory x) => x.AlwaysLoad).ToArray();
		float num = 0.2f / (float)(array.Length - 1);
		for (int num2 = 0; num2 < array.Length; num2++)
		{
			array[num2].Load();
			if (initialLoad)
			{
				Utilities.SetLoadingScreenPercentage(0.4f + (float)num2 * num);
			}
		}
		if (initialLoad)
		{
			Utilities.SetLoadingScreenPercentage(0.6f);
		}
		if (initialLoad)
		{
			return;
		}
		foreach (ScreenLayer sortedLayer2 in ScreenManager.SortedLayers)
		{
			if (sortedLayer2 is GauntletLayer gauntletLayer2)
			{
				gauntletLayer2.OnResourceRefreshEnd(dictionary[gauntletLayer2]);
			}
		}
	}

	private void OnControllerDisconnected()
	{
	}

	private void OnManagedOptionChanged(ManagedOptions.ManagedOptionsType changedManagedOptionsType)
	{
		switch (changedManagedOptionsType)
		{
		case ManagedOptions.ManagedOptionsType.Language:
			UIResourceManager.OnLanguageChange(BannerlordConfig.Language);
			ScreenManager.UpdateLayout();
			break;
		case ManagedOptions.ManagedOptionsType.UIScale:
			ScreenManager.OnScaleChange(BannerlordConfig.UIScale);
			break;
		}
	}

	protected override void OnNewModuleLoad()
	{
		_areResourcesDirty = true;
	}

	protected override void OnSubModuleUnloaded()
	{
		ScreenManager.OnControllerDisconnected -= OnControllerDisconnected;
		ManagedOptions.OnManagedOptionChanged = (ManagedOptions.OnManagedOptionChangedDelegate)Delegate.Remove(ManagedOptions.OnManagedOptionChanged, new ManagedOptions.OnManagedOptionChangedDelegate(OnManagedOptionChanged));
		UIResourceManager.Clear();
		if (GauntletGamepadNavigationManager.Instance != null)
		{
			GauntletGamepadNavigationManager.Instance.OnFinalize();
		}
		_fullBackgroundCategory?.Unload();
		_fullscreensCategory?.Unload();
		GauntletGameVersionView.RemoveModuleVersionInfo("Bannerlord");
		Instance = null;
		GauntletInformationView.OnFinalize();
		base.OnSubModuleUnloaded();
	}

	protected override void OnBeforeInitialModuleScreenSetAsRoot()
	{
		if (!_initialized)
		{
			if (!Utilities.CommandLineArgumentExists("VisualTests"))
			{
				GauntletInformationView.Initialize();
				GauntletSceneNotification.Initialize();
				GauntletSceneNotification.Current.RegisterContextProvider(new NativeSceneNotificationContextProvider());
				GauntletChatLogView.Initialize();
				GauntletGamepadCursor.Initialize();
				GauntletGameVersionView.Initialize();
				GauntletCameraFadeView.Initialize();
				InformationManager.RegisterTooltip<List<TooltipProperty>, PropertyBasedTooltipVM>(PropertyBasedTooltipVM.RefreshGenericPropertyBasedTooltip, "PropertyBasedTooltip");
				InformationManager.RegisterTooltip<RundownLineVM, RundownTooltipVM>(RundownTooltipVM.RefreshGenericRundownTooltip, "RundownTooltip");
				InformationManager.RegisterTooltip<string, HintVM>(HintVM.RefreshGenericHintTooltip, "HintTooltip");
				_queryManager = new GauntletQueryManager();
				_queryManager.Initialize();
				_queryManager.InitializeKeyVisuals();
			}
			UIResourceManager.OnLanguageChange(BannerlordConfig.Language);
			ScreenManager.OnScaleChange(BannerlordConfig.UIScale);
			_initialized = true;
		}
	}

	public override void OnMultiplayerGameStart(Game game, object starterObject)
	{
		base.OnMultiplayerGameStart(game, starterObject);
		if (!_isMultiplayer)
		{
			LoadingWindow.LoadingWindowManager?.SetCurrentModeIsMultiplayer(isMultiplayer: true);
			_isMultiplayer = true;
		}
	}

	public override void OnGameEnd(Game game)
	{
		base.OnGameEnd(game);
		if (_isMultiplayer)
		{
			LoadingWindow.LoadingWindowManager?.SetCurrentModeIsMultiplayer(isMultiplayer: false);
			_isMultiplayer = false;
		}
	}

	protected override void OnApplicationTick(float dt)
	{
		if (_areResourcesDirty)
		{
			RefreshResources(initialLoad: false);
			_areResourcesDirty = false;
		}
		base.OnApplicationTick(dt);
		if (!_loadingWindowCreated)
		{
			if (LoadingWindow.LoadingWindowManager == null)
			{
				LoadingWindow.InitializeWith<GauntletDefaultLoadingWindowManager>();
			}
			_loadingWindowCreated = true;
		}
		UIResourceManager.Update();
		if (GauntletGamepadNavigationManager.Instance != null && ScreenManager.GetMouseVisibility())
		{
			GauntletGamepadNavigationManager.Instance.IsTouchpadMouseEnabled = NativeOptions.GetConfig(NativeOptions.NativeOptionsType.EnableTouchpadMouse) != 0f;
			GauntletGamepadNavigationManager.Instance.Update(dt);
		}
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("clear", "chatlog")]
	public static string ClearChatLog(List<string> strings)
	{
		InformationManager.ClearAllMessages();
		return "Chatlog cleared!";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("can_focus_while_in_mission", "chatlog")]
	public static string SetCanFocusWhileInMission(List<string> strings)
	{
		if (strings[0] == "0" || strings[0] == "1")
		{
			GauntletChatLogView.Current.SetCanFocusWhileInMission(strings[0] == "1");
			return "Chat window will" + ((strings[0] == "1") ? " " : " NOT ") + " be able to gain focus now.";
		}
		return "Wrong input";
	}
}

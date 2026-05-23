using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;

namespace TaleWorlds.ScreenSystem;

public static class ScreenManager
{
	public delegate void OnPushScreenEvent(ScreenBase pushedScreen);

	public delegate void OnPopScreenEvent(ScreenBase poppedScreen);

	public delegate void OnControllerDisconnectedEvent();

	public delegate bool OnPlatformTextRequestedDelegate(string initialText, string descriptionText, int maxLength, int keyboardTypeEnum);

	private static IScreenManagerEngineConnection _engineInterface;

	private static Vec2 _usableArea;

	private static ObservableCollection<ScreenBase> _screenList;

	private static ObservableCollection<GlobalLayer> _globalLayers;

	private static List<ScreenLayer> _sortedLayers;

	private static ScreenLayer[] _sortedActiveLayersCopyForUpdate;

	private static bool _isSortedActiveLayersDirty;

	private static bool _isScreenDebugInformationEnabled;

	private static List<InputKey> _lastMouseActiveKeys;

	public static bool DisableScreenManagerTicks;

	private static bool _activeMouseVisible;

	private static IReadOnlyList<int> _lastPressedKeys;

	private static bool _globalOrderDirty;

	private static ScreenLayer _mouseDownLayer;

	private static bool _isWindowFocused;

	private static bool _isRefreshActive;

	public static IScreenManagerEngineConnection EngineInterface => _engineInterface;

	public static float Scale { get; private set; }

	public static Vec2 UsableArea
	{
		get
		{
			return _usableArea;
		}
		private set
		{
			if (value != _usableArea)
			{
				_usableArea = value;
				OnUsableAreaChanged(_usableArea);
			}
		}
	}

	public static bool IsEnterButtonRDown => _engineInterface.GetIsEnterButtonRDown();

	public static bool IsLateTickInProgress { get; private set; }

	public static List<ScreenLayer> SortedLayers
	{
		get
		{
			if (_isSortedActiveLayersDirty || _sortedLayers.Count != TopScreen?.Layers.Count + _globalLayers?.Count)
			{
				_sortedLayers.Clear();
				if (TopScreen != null)
				{
					for (int i = 0; i < TopScreen.Layers.Count; i++)
					{
						ScreenLayer screenLayer = TopScreen.Layers[i];
						if (screenLayer != null)
						{
							_sortedLayers.Add(screenLayer);
						}
					}
				}
				foreach (GlobalLayer globalLayer in _globalLayers)
				{
					_sortedLayers.Add(globalLayer.Layer);
				}
				_sortedLayers.Sort();
				_isSortedActiveLayersDirty = false;
			}
			return _sortedLayers;
		}
	}

	public static ScreenBase TopScreen { get; private set; }

	public static ScreenLayer FocusedLayer { get; private set; }

	public static ScreenLayer FirstHitLayer { get; private set; }

	public static event OnPushScreenEvent OnPushScreen;

	public static event OnPopScreenEvent OnPopScreen;

	public static event OnControllerDisconnectedEvent OnControllerDisconnected;

	public static event Action FocusGained;

	public static event OnPlatformTextRequestedDelegate PlatformTextRequested;

	static ScreenManager()
	{
		Scale = 1f;
		_usableArea = new Vec2(1f, 1f);
		_sortedLayers = new List<ScreenLayer>(16);
		_sortedActiveLayersCopyForUpdate = new ScreenLayer[16];
		_isSortedActiveLayersDirty = true;
		DisableScreenManagerTicks = false;
		_isRefreshActive = false;
		_globalLayers = new ObservableCollection<GlobalLayer>();
		_screenList = new ObservableCollection<ScreenBase>();
		_lastMouseActiveKeys = new List<InputKey>();
		_screenList.CollectionChanged += OnScreenListChanged;
		_globalLayers.CollectionChanged += OnGlobalListChanged;
		ScreenLayer.OnLayerActiveStateChanged += OnLayerActiveStateChanged;
		FocusedLayer = null;
		FirstHitLayer = null;
		_isWindowFocused = true;
	}

	private static void OnLayerActiveStateChanged(ScreenLayer layer)
	{
		SetSortedLayersDirty();
	}

	public static void Initialize(IScreenManagerEngineConnection engineInterface)
	{
		_engineInterface = engineInterface;
	}

	internal static void RefreshGlobalOrder()
	{
		if (_isRefreshActive)
		{
			return;
		}
		_isRefreshActive = true;
		int currentOrder = -2000;
		int currentOrder2 = 10000;
		for (int i = 0; i < SortedLayers.Count; i++)
		{
			if (SortedLayers[i] == null)
			{
				continue;
			}
			if (!SortedLayers[i].IsFinalized)
			{
				ScreenLayer screenLayer = SortedLayers[i];
				if (screenLayer != null && screenLayer.IsActive)
				{
					SortedLayers[i]?.RefreshGlobalOrder(ref currentOrder);
				}
				else
				{
					SortedLayers[i]?.RefreshGlobalOrder(ref currentOrder2);
				}
			}
			_globalOrderDirty = false;
		}
		_isRefreshActive = false;
	}

	public static void RemoveGlobalLayer(GlobalLayer layer)
	{
		TaleWorlds.Library.Debug.Print("RemoveGlobalLayer");
		_globalLayers.Remove(layer);
		layer.Layer.HandleDeactivate();
		_globalOrderDirty = true;
	}

	public static void AddGlobalLayer(GlobalLayer layer, bool isFocusable)
	{
		TaleWorlds.Library.Debug.Print("AddGlobalLayer");
		int index = _globalLayers.Count;
		for (int i = 0; i < _globalLayers.Count; i++)
		{
			if (_globalLayers[i].Layer.InputRestrictions.Order >= layer.Layer.InputRestrictions.Order)
			{
				index = i;
				break;
			}
		}
		_globalLayers.Insert(index, layer);
		layer.Layer.HandleActivate();
		_globalOrderDirty = true;
	}

	public static void OnConstrainStateChanged(bool isConstrained)
	{
		TaleWorlds.Library.Debug.Print("OnConstrainStateChanged: " + isConstrained);
		OnGameWindowFocusChange(!isConstrained);
	}

	public static bool ScreenTypeExistsAtList(ScreenBase screen)
	{
		Type type = screen.GetType();
		foreach (ScreenBase screen2 in _screenList)
		{
			if (screen2.GetType() == type)
			{
				return true;
			}
		}
		return false;
	}

	public static void UpdateLayout()
	{
		foreach (GlobalLayer globalLayer in _globalLayers)
		{
			globalLayer.UpdateLayout();
		}
		foreach (ScreenBase screen in _screenList)
		{
			screen.UpdateLayout();
		}
	}

	public static void SetSuspendLayer(ScreenLayer layer, bool isSuspended)
	{
		if (isSuspended)
		{
			layer.HandleDeactivate();
		}
		else
		{
			layer.HandleActivate();
		}
		layer.LastActiveState = !isSuspended;
	}

	public static void OnFinalize()
	{
		DeactivateAndFinalizeAllScreens();
		_screenList.CollectionChanged -= OnScreenListChanged;
		_globalLayers.CollectionChanged -= OnGlobalListChanged;
		ScreenLayer.OnLayerActiveStateChanged -= OnLayerActiveStateChanged;
		_screenList = null;
		_globalLayers = null;
		FocusedLayer = null;
	}

	private static void DeactivateAndFinalizeAllScreens()
	{
		if (!TWParallel.IsMainThread())
		{
			TaleWorlds.Library.Debug.FailedAssert("Screen should be changed from main thread", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.ScreenSystem\\ScreenManager.cs", "DeactivateAndFinalizeAllScreens", 291);
		}
		TaleWorlds.Library.Debug.Print("DeactivateAndFinalizeAllScreens");
		for (int num = _screenList.Count - 1; num >= 0; num--)
		{
			_screenList[num].HandlePause();
			_screenList[num].HandleDeactivate();
			_screenList[num].HandleFinalize();
			ScreenManager.OnPopScreen?.Invoke(_screenList[num]);
			_screenList.RemoveAt(num);
		}
		Common.MemoryCleanupGC();
	}

	public static void Tick(float dt)
	{
		if (DisableScreenManagerTicks)
		{
			return;
		}
		for (int i = 0; i < _globalLayers.Count; i++)
		{
			_globalLayers[i]?.EarlyTick(dt);
		}
		Update();
		if (TopScreen != null)
		{
			TopScreen.FrameTick(dt);
			FindPredecessor(TopScreen)?.IdleTick(dt);
		}
		for (int j = 0; j < SortedLayers.Count; j++)
		{
			ScreenLayer screenLayer = SortedLayers[j];
			if (screenLayer != null && screenLayer.IsActive && !screenLayer.IsFinalized)
			{
				screenLayer.Tick(dt);
			}
		}
		for (int k = 0; k < _globalLayers.Count; k++)
		{
			_globalLayers[k]?.Tick(dt);
		}
		LateUpdate(dt);
		for (int l = 0; l < _globalLayers.Count; l++)
		{
			_globalLayers[l]?.LateTick(dt);
		}
		if (TopScreen != null)
		{
			TopScreen.PostFrameTick(dt);
		}
		ShowScreenDebugInformation();
	}

	public static void LateTick(float dt)
	{
		IsLateTickInProgress = true;
		for (int i = 0; i < SortedLayers.Count; i++)
		{
			ScreenLayer screenLayer = SortedLayers[i];
			if (screenLayer != null && screenLayer.IsActive && !screenLayer.IsFinalized)
			{
				screenLayer.RenderTick(dt);
			}
		}
		for (int j = 0; j < SortedLayers.Count; j++)
		{
			ScreenLayer screenLayer2 = SortedLayers[j];
			if (screenLayer2 != null && screenLayer2.IsFocusLayer)
			{
				screenLayer2.Input.UnregisterReleasedKeys();
			}
		}
		IsLateTickInProgress = false;
	}

	public static bool OnPlatformScreenKeyboardRequested(string initialText, string descriptionText, int maxLength, int keyboardTypeEnum)
	{
		return ScreenManager.PlatformTextRequested?.Invoke(initialText, descriptionText, maxLength, keyboardTypeEnum) ?? false;
	}

	public static void OnOnscreenKeyboardDone(string inputText)
	{
		Input.IsOnScreenKeyboardActive = false;
		FocusedLayer?.OnOnScreenKeyboardDone(inputText);
	}

	public static void OnOnscreenKeyboardCanceled()
	{
		Input.IsOnScreenKeyboardActive = false;
		FocusedLayer?.OnOnScreenKeyboardCanceled();
	}

	public static void OnGameWindowFocusChange(bool focusGained)
	{
		_isWindowFocused = focusGained;
		if (_isWindowFocused)
		{
			_activeMouseVisible = EngineInterface.GetMouseVisible();
		}
		TaleWorlds.Library.Debug.Print("OnGameWindowFocusChange: " + _isWindowFocused);
		TaleWorlds.Library.Debug.Print("TopScreen: " + TopScreen?.GetType()?.Name);
		bool flag = false;
		if (!Debugger.IsAttached && !flag)
		{
			TopScreen?.OnFocusChangeOnGameWindow(focusGained);
		}
		if (focusGained)
		{
			ScreenManager.FocusGained?.Invoke();
		}
		FocusedLayer?.Input.ResetLastDownKeys();
	}

	public static void ReplaceTopScreen(ScreenBase screen)
	{
		TaleWorlds.Library.Debug.Print("ReplaceToTopScreen");
		if (_screenList.Count > 0)
		{
			TopScreen.HandlePause();
			TopScreen.HandleDeactivate();
			TopScreen.HandleFinalize();
			ScreenManager.OnPopScreen?.Invoke(TopScreen);
			_screenList.Remove(TopScreen);
		}
		_screenList.Add(screen);
		screen.HandleInitialize();
		screen.HandleActivate();
		screen.HandleResume();
		_globalOrderDirty = true;
		ScreenManager.OnPushScreen?.Invoke(screen);
	}

	public static List<ScreenLayer> GetPersistentInputRestrictions()
	{
		List<ScreenLayer> list = new List<ScreenLayer>();
		foreach (GlobalLayer globalLayer in _globalLayers)
		{
			list.Add(globalLayer.Layer);
		}
		return list;
	}

	public static void SetAndActivateRootScreen(ScreenBase screen)
	{
		TaleWorlds.Library.Debug.Print("SetAndActivateRootScreen");
		if (TopScreen != null)
		{
			throw new Exception("TopScreen is not null.");
		}
		_screenList.Add(screen);
		screen.HandleInitialize();
		screen.HandleActivate();
		screen.HandleResume();
		_globalOrderDirty = true;
		ScreenManager.OnPushScreen?.Invoke(screen);
	}

	public static void CleanAndPushScreen(ScreenBase screen)
	{
		if (!TWParallel.IsMainThread())
		{
			TaleWorlds.Library.Debug.FailedAssert("Screen should be changed from main thread", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.ScreenSystem\\ScreenManager.cs", "CleanAndPushScreen", 517);
		}
		TaleWorlds.Library.Debug.Print("CleanAndPushScreen");
		DeactivateAndFinalizeAllScreens();
		_screenList.Add(screen);
		screen.HandleInitialize();
		screen.HandleActivate();
		screen.HandleResume();
		_globalOrderDirty = true;
		ScreenManager.OnPushScreen?.Invoke(screen);
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("cb_clear_siege_machine_selection", "ui")]
	public static string ClearSiegeMachineSelection(List<string> args)
	{
		ScreenBase screenBase = _screenList.FirstOrDefault((ScreenBase x) => x.GetType().GetMethod("ClearSiegeMachineSelections") != null);
		screenBase?.GetType().GetMethod("ClearSiegeMachineSelections").Invoke(screenBase, null);
		return "Siege machine selections have been cleared.";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("cb_copy_battle_layout_to_clipboard", "ui")]
	public static string CopyCustomBattle(List<string> args)
	{
		ScreenBase screenBase = _screenList.FirstOrDefault((ScreenBase x) => x.GetType().GetMethod("CopyBattleLayoutToClipboard") != null);
		if (screenBase != null)
		{
			screenBase.GetType().GetMethod("CopyBattleLayoutToClipboard").Invoke(screenBase, null);
			return "Custom battle layout has been copied to clipboard as text.";
		}
		return "Something went wrong";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("cb_apply_battle_layout_from_string", "ui")]
	public static string ApplyCustomBattleLayout(List<string> args)
	{
		ScreenBase screenBase = _screenList.FirstOrDefault((ScreenBase x) => x.GetType().GetMethod("ApplyCustomBattleLayout") != null);
		if (screenBase != null && args.Count > 0)
		{
			string text = args.Aggregate((string i, string j) => i + " " + j);
			if (text.Count() > 5)
			{
				screenBase.GetType().GetMethod("ApplyCustomBattleLayout").Invoke(screenBase, new object[1] { text });
				return "Applied new layout from text.";
			}
			return "Argument is not right.";
		}
		return "Something went wrong.";
	}

	public static void PushScreen(ScreenBase screen)
	{
		if (!TWParallel.IsMainThread())
		{
			TaleWorlds.Library.Debug.FailedAssert("Screen should be changed from main thread", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.ScreenSystem\\ScreenManager.cs", "PushScreen", 594);
		}
		TaleWorlds.Library.Debug.Print("PushScreen");
		if (_screenList.Count > 0)
		{
			TopScreen.HandlePause();
			if (TopScreen.IsActive)
			{
				TopScreen.HandleDeactivate();
			}
		}
		_screenList.Add(screen);
		screen.HandleInitialize();
		screen.HandleActivate();
		screen.HandleResume();
		_globalOrderDirty = true;
		ScreenManager.OnPushScreen?.Invoke(screen);
	}

	public static void PopScreen()
	{
		if (!TWParallel.IsMainThread())
		{
			TaleWorlds.Library.Debug.FailedAssert("Screen should be changed from main thread", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.ScreenSystem\\ScreenManager.cs", "PopScreen", 625);
		}
		TaleWorlds.Library.Debug.Print("PopScreen");
		if (_screenList.Count > 0)
		{
			TopScreen.HandlePause();
			TopScreen.HandleDeactivate();
			TopScreen.HandleFinalize();
			TaleWorlds.Library.Debug.Print("PopScreen - " + TopScreen.GetType().ToString());
			ScreenManager.OnPopScreen?.Invoke(TopScreen);
			_screenList.Remove(TopScreen);
		}
		if (_screenList.Count > 0)
		{
			ScreenBase topScreen = TopScreen;
			TopScreen.HandleActivate();
			if (topScreen == TopScreen)
			{
				TopScreen.HandleResume();
			}
		}
		_globalOrderDirty = true;
	}

	public static void CleanScreens()
	{
		if (!TWParallel.IsMainThread())
		{
			TaleWorlds.Library.Debug.FailedAssert("Screen should be changed from main thread", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.ScreenSystem\\ScreenManager.cs", "CleanScreens", 659);
		}
		TaleWorlds.Library.Debug.Print("CleanScreens");
		while (_screenList.Count > 0)
		{
			TopScreen.HandlePause();
			TopScreen.HandleDeactivate();
			TopScreen.HandleFinalize();
			ScreenManager.OnPopScreen?.Invoke(TopScreen);
			_screenList.Remove(TopScreen);
		}
		_globalOrderDirty = true;
	}

	private static ScreenBase FindPredecessor(ScreenBase screen)
	{
		ScreenBase result = null;
		int num = _screenList.IndexOf(screen);
		if (num > 0)
		{
			result = _screenList[num - 1];
		}
		return result;
	}

	public static void Update(IReadOnlyList<int> lastKeysPressed)
	{
		_lastPressedKeys = lastKeysPressed;
		ScreenBase topScreen = TopScreen;
		if (topScreen != null && topScreen.IsActive)
		{
			TopScreen.Update(_lastPressedKeys);
		}
		for (int i = 0; i < _globalLayers.Count; i++)
		{
			GlobalLayer globalLayer = _globalLayers[i];
			if (globalLayer.Layer.IsActive)
			{
				globalLayer.Update(_lastPressedKeys);
			}
		}
	}

	private static bool? GetMouseInput()
	{
		bool? result = null;
		List<InputKey> activeMouseKeys = GetActiveMouseKeys();
		if (_lastMouseActiveKeys.Count != activeMouseKeys.Count || !_lastMouseActiveKeys.SequenceEqual(activeMouseKeys))
		{
			result = activeMouseKeys.Count > 0;
		}
		_lastMouseActiveKeys = activeMouseKeys;
		return result;
	}

	private static List<InputKey> GetActiveMouseKeys()
	{
		List<InputKey> list = new List<InputKey>();
		InputKey inputKey = (IsEnterButtonRDown ? InputKey.ControllerRDown : InputKey.ControllerRRight);
		InputKey[] obj = new InputKey[6]
		{
			InputKey.LeftMouseButton,
			InputKey.RightMouseButton,
			InputKey.MiddleMouseButton,
			InputKey.X1MouseButton,
			InputKey.X2MouseButton,
			(InputKey)0
		};
		obj[5] = inputKey;
		InputKey[] array = obj;
		for (int i = 0; i < array.Length; i++)
		{
			if (Input.IsKeyDown(array[i]))
			{
				list.Add(array[i]);
			}
		}
		return list;
	}

	public static void EarlyUpdate(Vec2 usableArea)
	{
		UsableArea = usableArea;
		RefreshGlobalOrder();
		InputType inputType = InputType.None;
		bool? mouseInput = GetMouseInput();
		if (mouseInput == false)
		{
			_mouseDownLayer = null;
		}
		for (int num = SortedLayers.Count - 1; num >= 0; num--)
		{
			ScreenLayer screenLayer = SortedLayers[num];
			if (screenLayer != null && screenLayer.IsActive && !screenLayer.IsFinalized)
			{
				InputType inputType2 = InputType.None;
				InputUsageMask inputUsageMask = screenLayer.InputUsageMask;
				screenLayer.ScreenOrderInLastFrame = num;
				_ = screenLayer.IsHitThisFrame;
				screenLayer.IsHitThisFrame = false;
				if (screenLayer.HitTest())
				{
					if (_mouseDownLayer != null && _mouseDownLayer != screenLayer && mouseInput.HasValue && !inputType.HasAnyFlag(InputType.MouseButton) && inputUsageMask.HasAnyFlag(InputUsageMask.MouseButtons))
					{
						_mouseDownLayer = null;
					}
					if (FirstHitLayer == null)
					{
						FirstHitLayer = screenLayer;
						_engineInterface.ActivateMouseCursor(screenLayer.ActiveCursor);
					}
					if (_mouseDownLayer == screenLayer || (_mouseDownLayer == null && !inputType.HasAnyFlag(InputType.MouseButton) && inputUsageMask.HasAnyFlag(InputUsageMask.MouseButtons)))
					{
						inputType2 |= InputType.MouseButton;
						inputType |= InputType.MouseButton;
						screenLayer.IsHitThisFrame = true;
						if (mouseInput == true)
						{
							_mouseDownLayer = screenLayer;
						}
					}
					if (!inputType.HasAnyFlag(InputType.MouseWheel) && inputUsageMask.HasAnyFlag(InputUsageMask.MouseWheels))
					{
						inputType2 |= InputType.MouseWheel;
						inputType |= InputType.MouseWheel;
						screenLayer.IsHitThisFrame = true;
					}
				}
				if (!inputType.HasAnyFlag(InputType.Key) && FocusTest(screenLayer))
				{
					inputType2 |= InputType.Key;
					inputType |= InputType.Key;
				}
				screenLayer.EarlyProcessEvents(inputType2);
			}
			if (_mouseDownLayer == screenLayer)
			{
				screenLayer.IsHitThisFrame = true;
				screenLayer.Input.MouseOnMe = true;
			}
			else
			{
				screenLayer.Input.MouseOnMe = screenLayer.IsActive && screenLayer.IsHitThisFrame;
			}
		}
		for (int num2 = _sortedLayers.Count - 1; num2 >= 0; num2--)
		{
			ScreenLayer screenLayer2 = _sortedLayers[num2];
			if (screenLayer2.IsFocusLayer)
			{
				screenLayer2.Input.RegisterDownKeys();
			}
			else
			{
				screenLayer2.Input.ResetLastDownKeys();
			}
		}
	}

	private static void Update()
	{
		int num = 0;
		for (int i = 0; i < SortedLayers.Count; i++)
		{
			if (SortedLayers[i].IsActive)
			{
				num++;
			}
		}
		if (_sortedActiveLayersCopyForUpdate.Length < num)
		{
			_sortedActiveLayersCopyForUpdate = new ScreenLayer[num];
		}
		int num2 = 0;
		for (int j = 0; j < SortedLayers.Count; j++)
		{
			ScreenLayer screenLayer = SortedLayers[j];
			if (screenLayer.IsActive)
			{
				_sortedActiveLayersCopyForUpdate[num2] = screenLayer;
				num2++;
			}
		}
		for (int num3 = num2 - 1; num3 >= 0; num3--)
		{
			ScreenLayer screenLayer2 = _sortedActiveLayersCopyForUpdate[num3];
			if (!screenLayer2.IsFinalized)
			{
				screenLayer2.ProcessEvents();
			}
		}
		for (int k = 0; k < _sortedActiveLayersCopyForUpdate.Length; k++)
		{
			_sortedActiveLayersCopyForUpdate[k] = null;
		}
	}

	private static void LateUpdate(float dt)
	{
		for (int i = 0; i < SortedLayers.Count; i++)
		{
			ScreenLayer screenLayer = SortedLayers[i];
			if (screenLayer != null && screenLayer.IsActive && !screenLayer.IsFinalized)
			{
				screenLayer.LateUpdate(dt);
			}
		}
		FirstHitLayer = null;
		UpdateMouseVisibility();
		if (_globalOrderDirty)
		{
			RefreshGlobalOrder();
		}
	}

	internal static void UpdateMouseVisibility()
	{
		if (!_isWindowFocused)
		{
			_activeMouseVisible = EngineInterface.GetMouseVisible();
		}
		for (int i = 0; i < SortedLayers.Count; i++)
		{
			ScreenLayer screenLayer = SortedLayers[i];
			if (screenLayer.IsActive && screenLayer.InputRestrictions.MouseVisibility)
			{
				if (!_activeMouseVisible)
				{
					SetMouseVisible(value: true);
				}
				return;
			}
		}
		if (_activeMouseVisible)
		{
			SetMouseVisible(value: false);
		}
	}

	public static bool IsControllerActive()
	{
		if (Input.IsControllerConnected && Input.IsGamepadActive && !Input.IsMouseActive)
		{
			return _engineInterface.GetMouseVisible();
		}
		return false;
	}

	public static bool IsMouseCursorHidden()
	{
		if (!Input.IsMouseActive)
		{
			return _engineInterface.GetMouseVisible();
		}
		return false;
	}

	public static bool IsMouseCursorActive()
	{
		if (Input.IsMouseActive)
		{
			return _engineInterface.GetMouseVisible();
		}
		return false;
	}

	public static bool IsLayerBlockedAtPosition(ScreenLayer layer, Vector2 position)
	{
		for (int num = SortedLayers.Count - 1; num >= 0; num--)
		{
			ScreenLayer screenLayer = SortedLayers[num];
			if (layer == screenLayer)
			{
				return false;
			}
			if (screenLayer != null && screenLayer.IsActive && !screenLayer.IsFinalized && screenLayer.HitTest(position))
			{
				if (screenLayer.InputUsageMask.HasAnyFlag(InputUsageMask.MouseButtons))
				{
					return layer != SortedLayers[num];
				}
				if (screenLayer.InputUsageMask.HasAnyFlag(InputUsageMask.MouseWheels))
				{
					return layer != SortedLayers[num];
				}
			}
		}
		return false;
	}

	private static void SetMouseVisible(bool value)
	{
		_activeMouseVisible = value;
		_engineInterface.SetMouseVisible(value);
	}

	public static bool GetMouseVisibility()
	{
		return _activeMouseVisible;
	}

	public static void TrySetFocus(ScreenLayer layer)
	{
		if ((FocusedLayer == null || FocusedLayer.InputRestrictions.Order <= layer.InputRestrictions.Order || !layer.IsActive) && (layer.IsFocusLayer || layer.FocusTest()) && FocusedLayer != layer)
		{
			FocusedLayer?.HandleLoseFocus();
			FocusedLayer = layer;
			FocusedLayer?.HandleGainFocus();
		}
	}

	public static void TryLoseFocus(ScreenLayer layer)
	{
		if (FocusedLayer != layer)
		{
			return;
		}
		FocusedLayer?.HandleLoseFocus();
		for (int num = SortedLayers.Count - 1; num >= 0; num--)
		{
			ScreenLayer screenLayer = SortedLayers[num];
			if (screenLayer.IsActive && screenLayer.IsFocusLayer && layer != screenLayer)
			{
				FocusedLayer?.HandleGainFocus();
				FocusedLayer = screenLayer;
				return;
			}
		}
		FocusedLayer = null;
	}

	private static bool FocusTest(ScreenLayer layer)
	{
		return FocusedLayer == layer;
	}

	public static void OnScaleChange(float newScale)
	{
		Scale = newScale;
		foreach (GlobalLayer globalLayer in _globalLayers)
		{
			globalLayer.UpdateLayout();
		}
		foreach (ScreenBase screen in _screenList)
		{
			screen.UpdateLayout();
		}
	}

	public static void OnControllerDisconnect()
	{
		ScreenManager.OnControllerDisconnected?.Invoke();
	}

	private static void SetSortedLayersDirty()
	{
		_isSortedActiveLayersDirty = true;
	}

	private static void OnScreenListChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		TaleWorlds.Library.Debug.Print("OnScreenListChanged");
		SetSortedLayersDirty();
		ObservableCollection<ScreenBase> screenList = _screenList;
		if (screenList != null && screenList.Count > 0)
		{
			if (TopScreen != null)
			{
				TopScreen.OnAddLayer -= OnLayerAddedToTopLayer;
				TopScreen.OnRemoveLayer -= OnLayerRemovedFromTopLayer;
			}
			TopScreen = _screenList[_screenList.Count - 1];
			if (TopScreen != null)
			{
				TopScreen.OnAddLayer += OnLayerAddedToTopLayer;
				TopScreen.OnRemoveLayer += OnLayerRemovedFromTopLayer;
			}
		}
		else
		{
			if (TopScreen != null)
			{
				TopScreen.OnAddLayer -= OnLayerAddedToTopLayer;
				TopScreen.OnRemoveLayer -= OnLayerRemovedFromTopLayer;
			}
			TopScreen = null;
		}
		SetSortedLayersDirty();
	}

	private static void OnLayerAddedToTopLayer(ScreenLayer layer)
	{
		SetSortedLayersDirty();
	}

	private static void OnLayerRemovedFromTopLayer(ScreenLayer layer)
	{
		SetSortedLayersDirty();
	}

	private static void OnGlobalListChanged(object sender, NotifyCollectionChangedEventArgs e)
	{
		SetSortedLayersDirty();
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("set_screen_debug_information_enabled", "ui")]
	public static string SetScreenDebugInformationEnabled(List<string> args)
	{
		string result = "Usage: ui.set_screen_debug_information_enabled [True/False]";
		if (args.Count != 1)
		{
			return result;
		}
		if (bool.TryParse(args[0], out var result2))
		{
			SetScreenDebugInformationEnabled(result2);
			return "Success.";
		}
		return result;
	}

	public static void SetScreenDebugInformationEnabled(bool isEnabled)
	{
		_isScreenDebugInformationEnabled = isEnabled;
	}

	private static void ShowScreenDebugInformation()
	{
		if (!_isScreenDebugInformationEnabled)
		{
			return;
		}
		_engineInterface.BeginDebugPanel("Screen Debug Information");
		for (int i = 0; i < SortedLayers.Count; i++)
		{
			ScreenLayer screenLayer = SortedLayers[i];
			List<string> list = new List<string>();
			_ = screenLayer.InputRestrictions.InputUsageMask;
			if (screenLayer.IsFocusLayer && FocusedLayer == screenLayer)
			{
				list.Add("(FocusLayer)");
			}
			list.Add(screenLayer.Name);
			if (screenLayer.InputRestrictions.MouseVisibility)
			{
				list.Add("MouseVisibile");
			}
			if (screenLayer.InputRestrictions.InputUsageMask != InputUsageMask.Invalid)
			{
				list.Add("Input");
			}
			string text = string.Join(" - ", list);
			if (_engineInterface.DrawDebugTreeNode($"{text}###{screenLayer.Name}.{i}.{screenLayer.Name.GetDeterministicHashCode()}"))
			{
				screenLayer.DrawDebugInfo();
				_engineInterface.PopDebugTreeNode();
			}
		}
		_engineInterface.EndDebugPanel();
	}

	private static void OnUsableAreaChanged(Vec2 newUsableArea)
	{
		UpdateLayout();
	}
}

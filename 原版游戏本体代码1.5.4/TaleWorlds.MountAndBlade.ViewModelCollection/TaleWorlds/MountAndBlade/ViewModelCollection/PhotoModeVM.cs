using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Core.ViewModelCollection.Selector;
using TaleWorlds.Engine;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.ViewModelCollection.Input;

namespace TaleWorlds.MountAndBlade.ViewModelCollection;

public class PhotoModeVM : ViewModel
{
	private readonly Scene _missionScene;

	private InputKeyItemVM _takePictureKey;

	private InputKeyItemVM _fasterCameraKey;

	private SelectorVM<SelectorItemVM> _colorGradeSelector;

	private SelectorVM<SelectorItemVM> _overlaySelector;

	private MBBindingList<InputKeyItemVM> _keys;

	private PhotoModeValueOptionVM _focusEndValueOption;

	private PhotoModeValueOptionVM _focusStartValueOption;

	private PhotoModeValueOptionVM _focusValueOption;

	private PhotoModeValueOptionVM _exposureOption;

	private PhotoModeValueOptionVM _verticalFovOption;

	[DataSourceProperty]
	public MBBindingList<InputKeyItemVM> Keys
	{
		get
		{
			return _keys;
		}
		set
		{
			if (value != _keys)
			{
				_keys = value;
				OnPropertyChangedWithValue(value, "Keys");
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<SelectorItemVM> ColorGradeSelector
	{
		get
		{
			return _colorGradeSelector;
		}
		set
		{
			if (value != _colorGradeSelector)
			{
				_colorGradeSelector = value;
				OnPropertyChangedWithValue(value, "ColorGradeSelector");
			}
		}
	}

	[DataSourceProperty]
	public SelectorVM<SelectorItemVM> OverlaySelector
	{
		get
		{
			return _overlaySelector;
		}
		set
		{
			if (value != _overlaySelector)
			{
				_overlaySelector = value;
				OnPropertyChangedWithValue(value, "OverlaySelector");
			}
		}
	}

	[DataSourceProperty]
	public PhotoModeValueOptionVM FocusEndValueOption
	{
		get
		{
			return _focusEndValueOption;
		}
		set
		{
			if (value != _focusEndValueOption)
			{
				_focusEndValueOption = value;
				OnPropertyChangedWithValue(value, "FocusEndValueOption");
			}
		}
	}

	[DataSourceProperty]
	public PhotoModeValueOptionVM FocusStartValueOption
	{
		get
		{
			return _focusStartValueOption;
		}
		set
		{
			if (value != _focusStartValueOption)
			{
				_focusStartValueOption = value;
				OnPropertyChangedWithValue(value, "FocusStartValueOption");
			}
		}
	}

	[DataSourceProperty]
	public PhotoModeValueOptionVM FocusValueOption
	{
		get
		{
			return _focusValueOption;
		}
		set
		{
			if (value != _focusValueOption)
			{
				_focusValueOption = value;
				OnPropertyChangedWithValue(value, "FocusValueOption");
			}
		}
	}

	[DataSourceProperty]
	public PhotoModeValueOptionVM ExposureOption
	{
		get
		{
			return _exposureOption;
		}
		set
		{
			if (value != _exposureOption)
			{
				_exposureOption = value;
				OnPropertyChangedWithValue(value, "ExposureOption");
			}
		}
	}

	[DataSourceProperty]
	public PhotoModeValueOptionVM VerticalFovOption
	{
		get
		{
			return _verticalFovOption;
		}
		set
		{
			if (value != _verticalFovOption)
			{
				_verticalFovOption = value;
				OnPropertyChangedWithValue(value, "VerticalFovOption");
			}
		}
	}

	public PhotoModeVM(Scene missionScene, Func<bool> getVignetteOn, Func<bool> getHideAgentsOn)
	{
		_missionScene = missionScene;
		Keys = new MBBindingList<InputKeyItemVM>();
		float focus = 0f;
		float focusStart = 0f;
		float focusEnd = 0f;
		float exposure = 0f;
		bool vignetteOn = false;
		float num = 65f;
		missionScene.SetPhotoModeFov(num);
		_missionScene.GetPhotoModeFocus(ref focus, ref focusStart, ref focusEnd, ref exposure, ref vignetteOn);
		FocusEndValueOption = new PhotoModeValueOptionVM(new TextObject("{=eeJcVeQG}Focus End"), 0f, 1000f, focusEnd, OnFocusEndValueChange);
		FocusStartValueOption = new PhotoModeValueOptionVM(new TextObject("{=j5pLIV91}Focus Start"), 0f, 100f, focusStart, OnFocusStartValueChange);
		FocusValueOption = new PhotoModeValueOptionVM(new TextObject("{=photomodefocus}Focus"), 0f, 100f, focus, OnFocusValueChange);
		ExposureOption = new PhotoModeValueOptionVM(new TextObject("{=iPx4jep6}Exposure"), -5f, 5f, exposure, OnExposureValueChange);
		VerticalFovOption = new PhotoModeValueOptionVM(new TextObject("{=7XtICVeZ}Field of View"), 2f, 140f, num, OnVerticalFovValueChange);
	}

	private void OnFocusValueChange(float newFocusValue)
	{
		_missionScene.SetPhotoModeFocus(FocusStartValueOption.CurrentValue, FocusEndValueOption.CurrentValue, newFocusValue, ExposureOption.CurrentValue);
	}

	private void OnFocusStartValueChange(float newFocusStartValue)
	{
		_missionScene.SetPhotoModeFocus(newFocusStartValue, FocusEndValueOption.CurrentValue, FocusValueOption.CurrentValue, ExposureOption.CurrentValue);
	}

	private void OnFocusEndValueChange(float newFocusEndValue)
	{
		_missionScene.SetPhotoModeFocus(FocusStartValueOption.CurrentValue, newFocusEndValue, FocusValueOption.CurrentValue, ExposureOption.CurrentValue);
	}

	private void OnExposureValueChange(float newExposureValue)
	{
		_missionScene.SetPhotoModeFocus(FocusStartValueOption.CurrentValue, FocusEndValueOption.CurrentValue, FocusValueOption.CurrentValue, newExposureValue);
	}

	private void OnVerticalFovValueChange(float newVerticalFov)
	{
		_missionScene.SetPhotoModeFov(newVerticalFov);
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		Keys.ApplyActionOnAllItems(delegate(InputKeyItemVM k)
		{
			k.RefreshValues();
		});
		FocusEndValueOption.RefreshValues();
		FocusStartValueOption.RefreshValues();
		FocusValueOption.RefreshValues();
		ExposureOption.RefreshValues();
		VerticalFovOption.RefreshValues();
		List<string> list = new List<string>();
		string[] array = _missionScene.GetAllColorGradeNames().Split(new string[1] { "*/*" }, StringSplitOptions.RemoveEmptyEntries);
		foreach (string variation in array)
		{
			string item = GameTexts.FindText("str_photo_mode_color_grade", variation).ToString();
			list.Add(item);
		}
		if (list.Count == 0)
		{
			list.Add("Photo Mode Not Active");
		}
		ColorGradeSelector = new SelectorVM<SelectorItemVM>(list, _missionScene.GetSceneColorGradeIndex(), OnColorGradeSelectionChanged);
		List<string> list2 = new List<string>();
		array = _missionScene.GetAllFilterNames().Split(new string[1] { "*/*" }, StringSplitOptions.RemoveEmptyEntries);
		foreach (string variation2 in array)
		{
			string item2 = GameTexts.FindText("str_photo_mode_overlay", variation2).ToString();
			list2.Add(item2);
		}
		if (list2.Count == 0)
		{
			list.Add("Photo Mode Not Active");
		}
		OverlaySelector = new SelectorVM<SelectorItemVM>(list2, _missionScene.GetSceneFilterIndex(), OnOverlaySelectionChanged);
	}

	public void AddTakePictureKey(GameKey key)
	{
		_takePictureKey = InputKeyItemVM.CreateFromGameKey(key, isConsoleOnly: false);
		Keys.Add(_takePictureKey);
	}

	public void AddFasterCameraKey(HotKey hotkey)
	{
		_fasterCameraKey = InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: false);
		Keys.Add(_fasterCameraKey);
	}

	public void AddKey(GameKey key)
	{
		Keys.Add(InputKeyItemVM.CreateFromGameKey(key, isConsoleOnly: false));
	}

	public void AddHotkey(HotKey hotkey)
	{
		Keys.Add(InputKeyItemVM.CreateFromHotKey(hotkey, isConsoleOnly: false));
	}

	public void AddHotkeyWithForcedName(HotKey hotkey, TextObject forcedName)
	{
		Keys.Add(InputKeyItemVM.CreateFromHotKeyWithForcedName(hotkey, forcedName, isConsoleOnly: false));
	}

	public void AddConsoleTakePictureKey(string keyID, TextObject forcedName)
	{
		_takePictureKey = InputKeyItemVM.CreateFromForcedID(keyID, forcedName, isConsoleOnly: false);
		Keys.Add(_takePictureKey);
	}

	public override void OnFinalize()
	{
		base.OnFinalize();
		foreach (InputKeyItemVM key in Keys)
		{
			key.OnFinalize();
		}
	}

	private void OnColorGradeSelectionChanged(SelectorVM<SelectorItemVM> obj)
	{
		if (_missionScene.GetSceneColorGradeIndex() != obj.SelectedIndex)
		{
			_missionScene.SetSceneColorGradeIndex(obj.SelectedIndex);
		}
	}

	private void OnOverlaySelectionChanged(SelectorVM<SelectorItemVM> obj)
	{
		if (_missionScene.GetSceneFilterIndex() != obj.SelectedIndex)
		{
			int num = _missionScene.SetSceneFilterIndex(obj.SelectedIndex);
			if (num >= 0)
			{
				ColorGradeSelector.SelectedIndex = num;
			}
		}
	}

	public void Reset()
	{
		ColorGradeSelector.SelectedIndex = 0;
		OverlaySelector.SelectedIndex = 0;
		FocusValueOption.CurrentValue = 0f;
		FocusStartValueOption.CurrentValue = 0f;
		FocusEndValueOption.CurrentValue = 0f;
		ExposureOption.CurrentValue = 0f;
		VerticalFovOption.CurrentValue = 65f;
	}

	public void UpdateTakePictureKeyVisibility(bool canTakePicture)
	{
		_takePictureKey?.SetForcedVisibility(canTakePicture ? ((bool?)null) : new bool?(false));
	}

	public void UpdateFasterCameraKeyVisibility(bool canMoveCamera)
	{
		_fasterCameraKey?.SetForcedVisibility(canMoveCamera ? ((bool?)null) : new bool?(false));
	}
}

using System;
using System.Collections.Generic;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.MountAndBlade.ViewModelCollection;
using TaleWorlds.ScreenSystem;

namespace TaleWorlds.MountAndBlade.GauntletUI;

public class GauntletGameVersionView : GlobalLayer
{
	private static readonly List<Tuple<string, string>> _versionTexts = new List<Tuple<string, string>>();

	private GameVersionVM _dataSource;

	private bool _isEnabled = true;

	public static GauntletGameVersionView Current { get; private set; }

	public GauntletGameVersionView()
	{
		_dataSource = new GameVersionVM(CollectAllVersionTexts);
		GauntletLayer gauntletLayer = new GauntletLayer("MainMenuGameVersion", 15001);
		gauntletLayer.LoadMovie("GameVersion", _dataSource);
		base.Layer = gauntletLayer;
	}

	public static void Initialize()
	{
		if (Current == null)
		{
			Current = new GauntletGameVersionView();
			ScreenManager.AddGlobalLayer(Current, isFocusable: false);
		}
	}

	public static void Refresh()
	{
		Current?._dataSource?.RefreshValues();
	}

	public static void AddModuleVersionInfo(string title, string versionStr)
	{
		_versionTexts.Add(new Tuple<string, string>(title, versionStr));
	}

	public static void RemoveModuleVersionInfo(string title)
	{
		_versionTexts.RemoveAll((Tuple<string, string> x) => x.Item1 == title);
	}

	private static List<string> CollectAllVersionTexts()
	{
		List<string> list = new List<string>();
		foreach (Tuple<string, string> versionText in _versionTexts)
		{
			list.Add(versionText.Item1 + ": " + versionText.Item2);
		}
		return list;
	}

	protected override void OnTick(float dt)
	{
		base.OnTick(dt);
		bool enabled = ScreenManager.TopScreen is GauntletInitialScreen || ScreenManager.TopScreen is GauntletOptionsScreen;
		SetEnabled(enabled);
	}

	private void SetEnabled(bool isEnabled)
	{
		if (_isEnabled != isEnabled)
		{
			_isEnabled = isEnabled;
			ScreenManager.SetSuspendLayer(base.Layer, !_isEnabled);
			if (_isEnabled)
			{
				Refresh();
			}
		}
	}
}

using TaleWorlds.Engine;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.GauntletUI.GamepadNavigation;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI;

public class GauntletDefaultLoadingWindowManager : GlobalLayer, ILoadingWindowManager
{
	private GauntletMovieIdentifier _movie;

	private GauntletLayer _gauntletLayer;

	private LoadingWindowViewModel _loadingWindowViewModel;

	private SpriteCategory _sploadingCategory;

	private SpriteCategory _mpLoadingCategory;

	private SpriteCategory _mpBackgroundCategory;

	private bool _isMultiplayer;

	void ILoadingWindowManager.Initialize()
	{
		string spriteCategoryName = GetSpriteCategoryName();
		UIResourceManager.SpriteData.SpriteCategories.ContainsKey(spriteCategoryName);
		_sploadingCategory = UIResourceManager.GetSpriteCategory(spriteCategoryName);
		_sploadingCategory.InitializePartialLoad();
		_loadingWindowViewModel = new LoadingWindowViewModel(LoadImage, UnloadImage);
		_loadingWindowViewModel.Enabled = false;
		_loadingWindowViewModel.SetTotalGenericImageCount(_sploadingCategory.SpriteSheetCount);
		_loadingWindowViewModel.IsNavalDLCEnabled = ModuleHelper.IsModuleActive("NavalDLC");
		bool shouldClear = false;
		_gauntletLayer = new GauntletLayer("LoadingWindow", 115003, shouldClear);
		_movie = _gauntletLayer.LoadMovie("LoadingWindow", _loadingWindowViewModel);
		base.Layer = _gauntletLayer;
		ScreenManager.AddGlobalLayer(this, isFocusable: false);
	}

	void ILoadingWindowManager.Destroy()
	{
		if (_gauntletLayer == null)
		{
			Debug.FailedAssert("Trying to destroy loading window but it was not initialized", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade.GauntletUI\\GauntletDefaultLoadingWindowManager.cs", "Destroy", 64);
			return;
		}
		_loadingWindowViewModel?.OnFinalize();
		_gauntletLayer?.ReleaseMovie(_movie);
		ScreenManager.RemoveGlobalLayer(this);
	}

	void ILoadingWindowManager.EnableLoadingWindow()
	{
		_loadingWindowViewModel.Enabled = true;
		base.Layer.IsFocusLayer = true;
		ScreenManager.TrySetFocus(base.Layer);
		base.Layer.InputRestrictions.SetInputRestrictions(isMouseVisible: false);
		Utilities.StartLoadingStuckCheckState(720f);
	}

	void ILoadingWindowManager.DisableLoadingWindow()
	{
		_loadingWindowViewModel.Enabled = false;
		base.Layer.IsFocusLayer = false;
		ScreenManager.TryLoseFocus(base.Layer);
		base.Layer.InputRestrictions.ResetInputRestrictions();
		GauntletGamepadNavigationManager.Instance?.SetAllDirty();
		Utilities.EndLoadingStuckCheckState();
	}

	protected virtual string GetSpriteCategoryName()
	{
		return "ui_loading";
	}

	protected override void OnLateTick(float dt)
	{
		base.OnLateTick(dt);
		_loadingWindowViewModel.Update();
	}

	public void SetCurrentModeIsMultiplayer(bool isMultiplayer)
	{
		if (_isMultiplayer != isMultiplayer)
		{
			_isMultiplayer = isMultiplayer;
			_loadingWindowViewModel.IsMultiplayer = isMultiplayer;
			if (isMultiplayer)
			{
				_mpLoadingCategory = UIResourceManager.LoadSpriteCategory("ui_mploading");
				_mpBackgroundCategory = UIResourceManager.LoadSpriteCategory("ui_mpbackgrounds");
			}
			else
			{
				_mpLoadingCategory.Unload();
				_mpBackgroundCategory.Unload();
			}
		}
	}

	private void LoadImage(int index, out string imageName)
	{
		if (_sploadingCategory != null)
		{
			_sploadingCategory.PartialLoadAtIndex(UIResourceManager.ResourceContext, UIResourceManager.ResourceDepot, index);
			imageName = _sploadingCategory.SpriteParts[index - 1].Name;
		}
		else
		{
			imageName = string.Empty;
		}
	}

	private void UnloadImage(int index)
	{
		if (_sploadingCategory != null)
		{
			_sploadingCategory.PartialUnloadAtIndex(index);
		}
	}
}

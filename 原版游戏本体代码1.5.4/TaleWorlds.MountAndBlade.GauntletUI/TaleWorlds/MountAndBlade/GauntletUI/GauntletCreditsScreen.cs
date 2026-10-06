using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ModuleManager;
using TaleWorlds.MountAndBlade.View;
using TaleWorlds.MountAndBlade.View.Screens;
using TaleWorlds.MountAndBlade.ViewModelCollection.Credits;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI;

[OverrideView(typeof(CreditsScreen))]
public class GauntletCreditsScreen : ScreenBase
{
	private CreditsVM _datasource;

	private GauntletLayer _gauntletLayer;

	private GauntletMovieIdentifier _movie;

	private SpriteCategory _creditsCategory;

	protected override void OnInitialize()
	{
		base.OnInitialize();
		_creditsCategory = UIResourceManager.LoadSpriteCategory("ui_credits");
		_datasource = new CreditsVM();
		string path = string.Concat(ModuleHelper.GetModuleFullPath("Native") + "ModuleData/", "Credits.xml");
		_datasource.FillFromFile(path);
		_gauntletLayer = new GauntletLayer("CreditsScreen", 100);
		_gauntletLayer.IsFocusLayer = true;
		AddLayer(_gauntletLayer);
		_gauntletLayer.InputRestrictions.SetInputRestrictions();
		ScreenManager.TrySetFocus(_gauntletLayer);
		_movie = _gauntletLayer.LoadMovie("CreditsScreen", _datasource);
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		InformationManager.HideAllMessages();
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		_creditsCategory.Unload();
		_datasource.OnFinalize();
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		if (_gauntletLayer.Input.IsHotKeyPressed("Exit"))
		{
			ScreenManager.PopScreen();
		}
	}
}

using MCM.UI.GUI.ViewModels;
using Microsoft.Extensions.Logging;
using TaleWorlds.Engine.GauntletUI;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace MCM.UI.GUI.GauntletUI;

internal sealed class ModOptionsGauntletScreen : ScreenBase, IMCMOptionsScreen
{
	private readonly ILogger<ModOptionsGauntletScreen> _logger;

	private GauntletLayer? _gauntletLayer;

	private GauntletMovieIdentifier? _gauntletMovie;

	private ModOptionsVM _dataSource;

	private SpriteCategory? _spriteCategoryMCM;

	public ModOptionsGauntletScreen(ILogger<ModOptionsGauntletScreen> logger)
	{
		_logger = logger;
	}

	protected override void OnInitialize()
	{
		base.OnInitialize();
		SpriteData spriteData = UIResourceManager.SpriteData;
		TwoDimensionEngineResourceContext resourceContext = UIResourceManager.ResourceContext;
		ResourceDepot resourceDepot = UIResourceManager.ResourceDepot;
		_spriteCategoryMCM = (spriteData.SpriteCategories.TryGetValue("ui_mcm", out var value) ? value : null);
		_spriteCategoryMCM?.Load(resourceContext, resourceDepot);
		_dataSource = new ModOptionsVM();
		_gauntletLayer = new GauntletLayer("GauntletLayer", 4000);
		_gauntletMovie = _gauntletLayer.LoadMovie("ModOptionsView_MCM", _dataSource);
		_gauntletLayer.Input.RegisterHotKeyCategory(HotKeyManager.GetCategory("GenericPanelGameKeyCategory"));
		_gauntletLayer.InputRestrictions.SetInputRestrictions();
		_gauntletLayer.IsFocusLayer = true;
		AddLayer(_gauntletLayer);
		ScreenManager.TrySetFocus(_gauntletLayer);
	}

	protected override void OnFrameTick(float dt)
	{
		base.OnFrameTick(dt);
		if (_gauntletLayer != null && _gauntletLayer.Input.IsHotKeyReleased("Exit"))
		{
			_dataSource.ExecuteClose();
			ScreenManager.TryLoseFocus(_gauntletLayer);
			ScreenManager.PopScreen();
		}
	}

	protected override void OnFinalize()
	{
		base.OnFinalize();
		if (_spriteCategoryMCM != null)
		{
			_spriteCategoryMCM.Unload();
		}
		if (_gauntletLayer != null)
		{
			RemoveLayer(_gauntletLayer);
		}
		if (_gauntletLayer != null && _gauntletMovie != null)
		{
			_gauntletLayer.ReleaseMovie(_gauntletMovie);
		}
		_gauntletLayer = null;
		_gauntletMovie = null;
		_dataSource.ExecuteSelect(null);
		_dataSource = null;
	}
}

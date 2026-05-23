using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Launcher.Library.CustomWidgets;
using TaleWorlds.MountAndBlade.Launcher.Library.UserDatas;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.Launcher.Library;

public class LauncherUI
{
	private Material _material;

	private TwoDimensionContext _twoDimensionContext;

	private UIContext _context;

	private IGauntletMovie _movie;

	private LauncherVM _viewModel;

	private SpriteData _spriteData;

	private WidgetFactory _widgetFactory;

	private UserDataManager _userDataManager;

	private readonly Action _onClose;

	private readonly Action _onMinimize;

	private Stopwatch _stopwatch;

	public bool HasUnofficialModulesSelected => _viewModel.ModsData.Modules.Any((LauncherModuleVM m) => !m.IsOfficial && m.IsSelected);

	public string AdditionalArgs
	{
		get
		{
			if (_viewModel == null)
			{
				return "";
			}
			return _viewModel.GameTypeArgument + " " + _viewModel.ModsData.ModuleListCode + _viewModel.ContinueGameArgument;
		}
	}

	public static event Action<string> OnAddHintInformation;

	public static event Action OnHideHintInformation;

	public LauncherUI(UserDataManager userDataManager, UIContext context, Action onClose, Action onMinimize)
	{
		_context = context;
		_twoDimensionContext = _context.TwoDimensionContext;
		_userDataManager = userDataManager;
		_onClose = onClose;
		_onMinimize = onMinimize;
		_stopwatch = new Stopwatch();
		_stopwatch.Start();
	}

	public void Initialize()
	{
		_spriteData = _context.SpriteData;
		_spriteData.SpriteCategories["ui_launcher"].Load(_twoDimensionContext.ResourceContext, _twoDimensionContext.ResourceDepot);
		_spriteData.SpriteCategories["ui_fonts_launcher"].Load(_twoDimensionContext.ResourceContext, _twoDimensionContext.ResourceDepot);
		_material = new PrimitivePolygonMaterial(new Color(0.5f, 0.5f, 0.5f));
		_widgetFactory = new WidgetFactory(_context.ResourceDepot, "Prefabs");
		_widgetFactory.PrefabExtensionContext.AddExtension(new PrefabDatabindingExtension());
		_widgetFactory.Initialize();
		_viewModel = new LauncherVM(_userDataManager, _onClose, _onMinimize);
		_movie = GauntletMovie.Load(_context, _widgetFactory, "UILauncher", _viewModel, doNotUseGeneratedPrefabs: false, hotReloadEnabled: true);
		GauntletGamepadNavigationContext context = new GauntletGamepadNavigationContext(GetIsBlockedOnPosition, GetLastScreenOrder, GetIsAvailableForGamepadNavigation);
		_context.InitializeGamepadNavigation(context);
		_context.EventManager.OnGetIsHitThisFrame = GetIsHitThisFrame;
	}

	public void OnFinalize()
	{
		_context.EventManager.OnGetIsHitThisFrame = null;
	}

	private int GetLastScreenOrder()
	{
		return 0;
	}

	private bool GetIsHitThisFrame()
	{
		return true;
	}

	private bool GetIsBlockedOnPosition(Vector2 pos)
	{
		return false;
	}

	private bool GetIsAvailableForGamepadNavigation()
	{
		return false;
	}

	public void Update()
	{
		_movie.Update();
		if (_stopwatch.IsRunning)
		{
			_stopwatch.Stop();
			TaleWorlds.Library.Debug.Print("Total startup time: " + ((float)_stopwatch.ElapsedMilliseconds / 1000f).ToString("0.0000"));
		}
	}

	public bool CheckMouseOverWindowDragArea()
	{
		return _context.EventManager.HoveredWidget is LauncherDragWindowAreaWidget;
	}

	public bool HitTest()
	{
		if (_movie == null)
		{
			return false;
		}
		return _context.HitTest(_movie.RootWidget);
	}

	public static void AddHintInformation(string message)
	{
		LauncherUI.OnAddHintInformation?.Invoke(message);
	}

	public static void HideHintInformation()
	{
		LauncherUI.OnHideHintInformation?.Invoke();
	}
}

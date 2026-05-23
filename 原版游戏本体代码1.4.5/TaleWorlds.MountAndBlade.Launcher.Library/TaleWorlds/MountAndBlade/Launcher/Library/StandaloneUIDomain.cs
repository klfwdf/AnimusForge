using System;
using System.Threading;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.GamepadNavigation;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.Launcher.Library.UserDatas;
using TaleWorlds.TwoDimension;
using TaleWorlds.TwoDimension.Standalone;

namespace TaleWorlds.MountAndBlade.Launcher.Library;

public class StandaloneUIDomain : FrameworkDomain
{
	private SingleThreadedSynchronizationContext _synchronizationContext;

	private bool _initialized;

	private bool _shouldDestroy;

	private GraphicsForm _graphicsForm;

	private GraphicsContext _graphicsContext;

	private UIContext _gauntletUIContext;

	private TwoDimensionContext _twoDimensionContext;

	private LauncherUI _launcherUI;

	private readonly ResourceDepot _resourceDepot;

	public UserDataManager UserDataManager { get; private set; }

	public string AdditionalArgs
	{
		get
		{
			if (_launcherUI == null)
			{
				return "";
			}
			return _launcherUI.AdditionalArgs;
		}
	}

	public bool HasUnofficialModulesSelected => _launcherUI.HasUnofficialModulesSelected;

	public StandaloneUIDomain(GraphicsForm graphicsForm, ResourceDepot resourceDepot)
	{
		_graphicsForm = graphicsForm;
		_resourceDepot = resourceDepot;
		UserDataManager = new UserDataManager();
	}

	public override void Update()
	{
		if (_synchronizationContext == null)
		{
			_synchronizationContext = new SingleThreadedSynchronizationContext();
			SynchronizationContext.SetSynchronizationContext(_synchronizationContext);
		}
		if (!_initialized)
		{
			WidgetInfo.Refresh();
			GauntletGamepadNavigationManager.Initialize();
			UserDataManager.LoadUserData();
			Input.Initialize(new StandaloneInputManager(_graphicsForm), null);
			_graphicsForm.InitializeGraphicsContext(_resourceDepot);
			_graphicsContext = _graphicsForm.GraphicsContext;
			TwoDimensionPlatform twoDimensionPlatform = new TwoDimensionPlatform(_graphicsForm, isAssetsUnderDefaultFolders: true);
			_twoDimensionContext = new TwoDimensionContext(twoDimensionPlatform, twoDimensionPlatform, _resourceDepot);
			InputContext inputContext = new InputContext();
			inputContext.MouseOnMe = true;
			inputContext.IsKeysAllowed = true;
			inputContext.IsMouseButtonAllowed = true;
			inputContext.IsMouseWheelAllowed = true;
			_gauntletUIContext = new UIContext(_twoDimensionContext, inputContext);
			_gauntletUIContext.IsDynamicScaleEnabled = false;
			_gauntletUIContext.Initialize();
			_launcherUI = new LauncherUI(UserDataManager, _gauntletUIContext, OnCloseRequest, OnMinimizeRequest);
			_launcherUI.Initialize();
			_initialized = true;
			_graphicsForm.BeginFrame();
			_graphicsContext.SwapBuffers();
		}
		else
		{
			_resourceDepot.CheckForChanges();
			_synchronizationContext.Tick();
			bool mouseOverDragArea = _launcherUI.CheckMouseOverWindowDragArea();
			_graphicsForm.UpdateInput(mouseOverDragArea);
			_graphicsForm.BeginFrame();
			Input.Update();
			_graphicsForm.Update();
			_gauntletUIContext.UpdateInput(InputType.MouseButton | InputType.MouseWheel | InputType.Key);
			_gauntletUIContext.Update(1f / 60f);
			_launcherUI.Update();
			_gauntletUIContext.LateUpdate(1f / 60f);
			_gauntletUIContext.RenderTick(1f / 60f);
			_graphicsForm.PostRender();
			_graphicsContext.SwapBuffers();
			if (_shouldDestroy)
			{
				DestroyAux();
				_shouldDestroy = false;
			}
		}
	}

	public override void Destroy()
	{
		_shouldDestroy = true;
	}

	private void DestroyAux()
	{
		GauntletGamepadNavigationManager.Instance?.OnFinalize();
		_synchronizationContext = null;
		_initialized = false;
		_graphicsContext?.DestroyContext();
		_gauntletUIContext = null;
		_launcherUI.OnFinalize();
		_launcherUI = null;
		_graphicsForm?.Destroy();
	}

	private void OnStartGameRequest()
	{
	}

	private void OnCloseRequest()
	{
		Environment.Exit(0);
	}

	private void OnMinimizeRequest()
	{
		_graphicsForm.MinimizeWindow();
	}
}

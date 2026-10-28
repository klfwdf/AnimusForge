using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.Engine.GauntletUI;

public class GauntletLayer : ScreenLayer
{
	private readonly MBList<GauntletMovieIdentifier> _movieIdentifiers;

	private readonly TwoDimensionContext _twoDimensionContext;

	public readonly TwoDimensionView TwoDimensionView;

	public readonly ITwoDimensionPlatform TwoDimensionPlatform;

	public IGamepadNavigationContext GamepadNavigationContext { get; private set; }

	public UIContext UIContext { get; private set; }

	private void InitializeContext()
	{
		UIContext = new UIContext(_twoDimensionContext, base.Input, UIResourceManager.SpriteData, UIResourceManager.FontFactory, UIResourceManager.BrushFactory);
		UIContext.ScaleModifier = base.Scale;
		UIContext.Initialize();
		GamepadNavigationContext = new GauntletGamepadNavigationContext(GetIsBlockedAtPosition, GetLastScreenOrder, GetIsAvailableForGamepadNavigation);
		UIContext.InitializeGamepadNavigation(GamepadNavigationContext);
		UIContext.EventManager.OnFocusedWidgetChanged += EventManagerOnFocusedWidgetChanged;
		UIContext.EventManager.OnGetIsHitThisFrame = GetIsHitThisFrame;
		UIContext.EventManager.UsableArea = base.UsableArea;
		RefreshContextName();
	}

	private void RefreshContextName()
	{
		if (UIContext != null)
		{
			UIContext.Name = base.Name;
		}
	}

	private void ClearContext()
	{
		foreach (GauntletMovieIdentifier movieIdentifier in _movieIdentifiers)
		{
			movieIdentifier.Movie.Release();
		}
		UIContext.EventManager.OnGetIsHitThisFrame = null;
		UIContext.EventManager.OnFocusedWidgetChanged -= EventManagerOnFocusedWidgetChanged;
		UIContext.OnFinalize();
		UIContext = null;
	}

	public void OnResourceRefreshBegin(out List<GauntletMovieIdentifier> previouslyLoadedMovies)
	{
		previouslyLoadedMovies = _movieIdentifiers.ToList();
		for (int i = 0; i < _movieIdentifiers.Count; i++)
		{
			ReleaseMovie(_movieIdentifiers[i]);
		}
		ClearContext();
	}

	public void OnResourceRefreshEnd(List<GauntletMovieIdentifier> previouslyLoadedMovies)
	{
		InitializeContext();
		for (int i = 0; i < previouslyLoadedMovies.Count; i++)
		{
			LoadMovie(previouslyLoadedMovies[i]);
		}
	}

	public GauntletLayer(string name, int localOrder, bool shouldClear = false)
		: base(name, localOrder)
	{
		_movieIdentifiers = new MBList<GauntletMovieIdentifier>();
		ResourceDepot resourceDepot = UIResourceManager.ResourceDepot;
		TwoDimensionView = TwoDimensionView.CreateTwoDimension(name);
		if (shouldClear)
		{
			TwoDimensionView.SetClearColor(255u);
			TwoDimensionView.SetRenderOption(View.ViewRenderOptions.ClearColor, value: true);
		}
		TwoDimensionPlatform = new TwoDimensionEnginePlatform(TwoDimensionView);
		_twoDimensionContext = new TwoDimensionContext(TwoDimensionPlatform, UIResourceManager.ResourceContext, resourceDepot);
		InitializeContext();
	}

	private void EventManagerOnFocusedWidgetChanged()
	{
		if (UIContext.EventManager.FocusedWidget != null)
		{
			ScreenManager.TrySetFocus(this);
		}
		else if (!base.IsFocusLayer)
		{
			ScreenManager.TryLoseFocus(this);
		}
	}

	public GauntletMovieIdentifier GetMovieIdentifier(string movieName)
	{
		for (int i = 0; i < _movieIdentifiers.Count; i++)
		{
			if (_movieIdentifiers[i].MovieName == movieName)
			{
				return _movieIdentifiers[i];
			}
		}
		return null;
	}

	public GauntletMovieIdentifier LoadMovie(string movieName, ViewModel dataSource)
	{
		GauntletMovieIdentifier gauntletMovieIdentifier = new GauntletMovieIdentifier(movieName, dataSource);
		LoadMovie(gauntletMovieIdentifier);
		return gauntletMovieIdentifier;
	}

	private void LoadMovie(GauntletMovieIdentifier identifier)
	{
		identifier.Movie = LoadMovieAux(identifier.MovieName, identifier.DataSource);
		_movieIdentifiers.Add(identifier);
		RefreshContextName();
	}

	private IGauntletMovie LoadMovieAux(string movieName, ViewModel dataSource)
	{
		bool isUsingGeneratedPrefabs = UIConfig.GetIsUsingGeneratedPrefabs();
		bool isHotReloadEnabled = UIConfig.GetIsHotReloadEnabled();
		return GauntletMovie.Load(UIContext, UIResourceManager.WidgetFactory, movieName, dataSource, !isUsingGeneratedPrefabs, isHotReloadEnabled);
	}

	public void ReleaseMovie(GauntletMovieIdentifier identifier)
	{
		if (_movieIdentifiers.Contains(identifier))
		{
			if (!identifier.Movie.IsReleased)
			{
				identifier.Movie.Release();
			}
			_movieIdentifiers.Remove(identifier);
			RefreshContextName();
		}
		else
		{
			Debug.FailedAssert("Failed to release movie from gauntlet layer: " + identifier.MovieName, "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine.GauntletUI\\GauntletLayer.cs", "ReleaseMovie", 208);
		}
	}

	protected override void OnActivate()
	{
		base.OnActivate();
		TwoDimensionView.SetEnable(value: true);
		UIContext.Activate();
	}

	protected override void OnDeactivate()
	{
		TwoDimensionPlatform.Clear();
		TwoDimensionView.Clear();
		TwoDimensionView.SetEnable(value: false);
		UIContext.Deactivate();
		base.OnDeactivate();
	}

	protected override void Tick(float dt)
	{
		base.Tick(dt);
		UIContext.Update(dt);
		foreach (GauntletMovieIdentifier movieIdentifier in _movieIdentifiers)
		{
			movieIdentifier.Movie.Update();
		}
	}

	protected override void LateUpdate(float dt)
	{
		base.LateUpdate(dt);
		UIContext.SetIsMouseEnabled(base.IsHitThisFrame);
		UIContext.LateUpdate(dt);
		base.ActiveCursor = (CursorType)UIContext.ActiveCursorOfContext;
	}

	protected override void RenderTick(float dt)
	{
		base.RenderTick(dt);
		TwoDimensionView.BeginFrame();
		TwoDimensionPlatform.OnFrameBegin();
		UIContext.RenderTick(dt);
		TwoDimensionView.EndFrame();
		TwoDimensionPlatform.OnFrameEnd();
	}

	protected override void Update(IReadOnlyList<int> lastKeysPressed)
	{
		UIContext.EventManager.FocusedWidget?.HandleInput(lastKeysPressed);
	}

	protected override void OnFinalize()
	{
		ClearContext();
		for (int i = 0; i < _movieIdentifiers.Count; i++)
		{
			if (_movieIdentifiers[i].Movie.IsLoaded)
			{
				Debug.FailedAssert("Movie was not released before finalizing layer: " + _movieIdentifiers[i].MovieName, "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine.GauntletUI\\GauntletLayer.cs", "OnFinalize", 288);
			}
		}
		TwoDimensionView.ManualInvalidate();
		base.OnFinalize();
	}

	protected override void RefreshGlobalOrder(ref int currentOrder)
	{
		TwoDimensionView.SetRenderOrder(currentOrder);
		currentOrder++;
	}

	public override void ProcessEvents()
	{
		base.ProcessEvents();
		UIContext.UpdateInput(base._usedInputs);
	}

	public override bool HitTest(Vector2 position)
	{
		foreach (GauntletMovieIdentifier movieIdentifier in _movieIdentifiers)
		{
			if (UIContext.HitTest(movieIdentifier.Movie.RootWidget, position))
			{
				return true;
			}
		}
		return false;
	}

	private bool GetIsBlockedAtPosition(Vector2 position)
	{
		return ScreenManager.IsLayerBlockedAtPosition(this, position);
	}

	public override bool HitTest()
	{
		foreach (GauntletMovieIdentifier movieIdentifier in _movieIdentifiers)
		{
			if (UIContext.HitTest(movieIdentifier.Movie.RootWidget))
			{
				return true;
			}
		}
		UIContext.EventManager.HoveredWidget = null;
		return false;
	}

	public override bool FocusTest()
	{
		foreach (GauntletMovieIdentifier movieIdentifier in _movieIdentifiers)
		{
			if (UIContext.FocusTest(movieIdentifier.Movie.RootWidget))
			{
				return true;
			}
		}
		return false;
	}

	public override bool IsFocusedOnInput()
	{
		return UIContext.EventManager.FocusedWidget is EditableTextWidget;
	}

	protected override void OnLoseFocus()
	{
		base.OnLoseFocus();
		UIContext.EventManager.ClearFocus();
	}

	public override void OnOnScreenKeyboardDone(string inputText)
	{
		if (inputText == null)
		{
			Debug.FailedAssert("OnScreenKeyboardDone returned null!", "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine.GauntletUI\\GauntletLayer.cs", "OnOnScreenKeyboardDone", 381);
			inputText = string.Empty;
		}
		base.OnOnScreenKeyboardDone(inputText);
		UIContext.OnOnScreenkeyboardTextInputDone(inputText);
	}

	public override void OnOnScreenKeyboardCanceled()
	{
		base.OnOnScreenKeyboardCanceled();
		UIContext.OnOnScreenKeyboardCanceled();
	}

	public override void UpdateLayout()
	{
		base.UpdateLayout();
		UIContext.ScaleModifier = base.Scale;
		UIContext.EventManager.UsableArea = base.UsableArea;
		_movieIdentifiers.ForEach(delegate(GauntletMovieIdentifier m)
		{
			m.DataSource.RefreshValues();
		});
		_movieIdentifiers.ForEach(delegate(GauntletMovieIdentifier m)
		{
			m.Movie.RefreshBindingWithChildren();
		});
		UIContext.EventManager.UpdateLayout();
	}

	public bool GetIsAvailableForGamepadNavigation()
	{
		if (base.LastActiveState && base.IsActive)
		{
			if (!base.IsFocusLayer)
			{
				return (base.InputRestrictions.InputUsageMask & InputUsageMask.Mouse) != 0;
			}
			return true;
		}
		return false;
	}

	private bool GetIsHitThisFrame()
	{
		return base.IsHitThisFrame;
	}

	private int GetLastScreenOrder()
	{
		return base.ScreenOrderInLastFrame;
	}

	public override void DrawDebugInfo()
	{
		foreach (GauntletMovieIdentifier movieIdentifier in _movieIdentifiers)
		{
			Imgui.Text("Movie: " + movieIdentifier.MovieName);
			Imgui.Text("Data Source: " + (movieIdentifier.DataSource?.GetType().Name ?? "No Datasource"));
		}
		base.DrawDebugInfo();
		Imgui.Text("Press 'Shift+F' to take widget hierarchy snapshot.");
		UIContext.DrawWidgetDebugInfo();
	}
}

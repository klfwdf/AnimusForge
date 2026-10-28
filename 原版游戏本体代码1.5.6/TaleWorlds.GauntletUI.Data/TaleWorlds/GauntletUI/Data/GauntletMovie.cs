using System.Collections.Generic;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.Data;

public class GauntletMovie : IGauntletMovie
{
	private WidgetPrefab _moviePrefab;

	private IViewModel _viewModel;

	private Widget _movieRootNode;

	private bool _isHotReloadEnabled;

	public WidgetFactory WidgetFactory { get; private set; }

	public BrushFactory BrushFactory { get; private set; }

	public UIContext Context { get; private set; }

	public IViewModel ViewModel => _viewModel;

	public string MovieName { get; private set; }

	public GauntletView RootView { get; private set; }

	public Widget RootWidget
	{
		get
		{
			if (RootView == null)
			{
				return null;
			}
			return RootView.Target;
		}
	}

	public bool IsLoaded { get; private set; }

	public bool IsReleased { get; private set; }

	private GauntletMovie(string movieName, UIContext context, WidgetFactory widgetFactory, IViewModel viewModel, bool hotReloadEnabled)
	{
		WidgetFactory = widgetFactory;
		BrushFactory = context.BrushFactory;
		Context = context;
		_isHotReloadEnabled = hotReloadEnabled;
		WidgetFactory.PrefabChange += OnResourceChanged;
		BrushFactory.BrushChange += OnResourceChanged;
		_viewModel = viewModel;
		MovieName = movieName;
		_movieRootNode = new Widget(Context);
		Context.Root.AddChild(_movieRootNode);
		_movieRootNode.WidthSizePolicy = SizePolicy.Fixed;
		_movieRootNode.HeightSizePolicy = SizePolicy.Fixed;
		_movieRootNode.ScaledSuggestedWidth = Context.TwoDimensionContext.Width;
		_movieRootNode.ScaledSuggestedHeight = Context.TwoDimensionContext.Height;
		_movieRootNode.DoNotAcceptEvents = true;
		IsLoaded = false;
		IsReleased = false;
	}

	public void RefreshDataSource(IViewModel dataSourve)
	{
		_viewModel = dataSourve;
		RootView.RefreshBindingWithChildren();
	}

	private void RefreshResources()
	{
		RootView.ClearEventHandlersWithChildren();
		RootView = null;
		_movieRootNode.RemoveAllChildren();
		Context.OnMovieReleased(MovieName);
		IsLoaded = false;
		LoadMovie();
	}

	private void OnResourceChanged()
	{
		if (_isHotReloadEnabled)
		{
			RefreshResources();
		}
	}

	private void LoadMovie()
	{
		_moviePrefab = WidgetFactory.GetCustomType(MovieName);
		if (_moviePrefab != null)
		{
			IsLoaded = true;
			IsReleased = false;
			WidgetCreationData widgetCreationData = new WidgetCreationData(Context, WidgetFactory);
			widgetCreationData.AddExtensionData(this);
			WidgetInstantiationResult widgetInstantiationResult = _moviePrefab.Instantiate(widgetCreationData);
			RootView = widgetInstantiationResult.GetGauntletView();
			Widget target = RootView.Target;
			_movieRootNode.AddChild(target);
			RootView.RefreshBindingWithChildren();
			Context.OnMovieLoaded(MovieName);
		}
	}

	public void Release()
	{
		_movieRootNode.OnBeforeRemovedChild(_movieRootNode);
		RootView?.ReleaseBindingWithChildren();
		_moviePrefab.OnRelease();
		WidgetFactory.OnUnload(MovieName);
		WidgetFactory.PrefabChange -= OnResourceChanged;
		BrushFactory.BrushChange -= OnResourceChanged;
		Context.OnMovieReleased(MovieName);
		_movieRootNode.ParentWidget = null;
		IsLoaded = false;
		IsReleased = true;
	}

	internal void OnItemRemoved(string type)
	{
		WidgetFactory.OnUnload(type);
	}

	public void Update()
	{
		_movieRootNode.ScaledSuggestedWidth = Context.TwoDimensionContext.Width;
		_movieRootNode.ScaledSuggestedHeight = Context.TwoDimensionContext.Height;
	}

	internal object GetViewModelAtPath(BindingPath path, bool isListExpected)
	{
		if (_viewModel != null && path != null)
		{
			BindingPath path2 = path.Simplify();
			return _viewModel.GetViewModelAtPath(path2, isListExpected);
		}
		return null;
	}

	public static IGauntletMovie Load(UIContext context, WidgetFactory widgetFactory, string movieName, IViewModel datasource, bool doNotUseGeneratedPrefabs, bool hotReloadEnabled)
	{
		IGauntletMovie gauntletMovie = null;
		if (!doNotUseGeneratedPrefabs)
		{
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			string variantName = "Default";
			if (datasource != null)
			{
				dictionary.Add("DataSource", datasource);
				variantName = datasource.GetType().FullName;
			}
			GeneratedPrefabInstantiationResult generatedPrefabInstantiationResult = widgetFactory.GeneratedPrefabContext.InstantiatePrefab(context, movieName, variantName, dictionary);
			if (generatedPrefabInstantiationResult != null)
			{
				gauntletMovie = generatedPrefabInstantiationResult.GetExtensionData("Movie") as IGauntletMovie;
				context.OnMovieLoaded(movieName);
			}
		}
		if (gauntletMovie == null)
		{
			GauntletMovie gauntletMovie2 = new GauntletMovie(movieName, context, widgetFactory, datasource, hotReloadEnabled);
			gauntletMovie2.LoadMovie();
			gauntletMovie = gauntletMovie2;
		}
		return gauntletMovie;
	}

	public void RefreshBindingWithChildren()
	{
		RootView.RefreshBindingWithChildren();
	}

	public GauntletView FindViewOf(Widget widget)
	{
		return widget.GetComponent<GauntletView>();
	}
}

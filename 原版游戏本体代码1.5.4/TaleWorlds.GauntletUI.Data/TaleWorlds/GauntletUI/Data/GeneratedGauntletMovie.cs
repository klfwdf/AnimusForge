using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.Data;

public class GeneratedGauntletMovie : IGauntletMovie
{
	private Widget _movieRootNode;

	private IGeneratedGauntletMovieRoot _root;

	public UIContext Context { get; private set; }

	public Widget RootWidget { get; private set; }

	public string MovieName { get; private set; }

	public bool IsLoaded { get; private set; }

	public bool IsReleased { get; private set; }

	public GeneratedGauntletMovie(string movieName, Widget rootWidget)
	{
		MovieName = movieName;
		RootWidget = rootWidget;
		Context = rootWidget.Context;
		_root = (IGeneratedGauntletMovieRoot)rootWidget;
		_movieRootNode = new Widget(Context);
		Context.Root.AddChild(_movieRootNode);
		_movieRootNode.WidthSizePolicy = SizePolicy.Fixed;
		_movieRootNode.HeightSizePolicy = SizePolicy.Fixed;
		_movieRootNode.ScaledSuggestedWidth = Context.TwoDimensionContext.Width;
		_movieRootNode.ScaledSuggestedHeight = Context.TwoDimensionContext.Height;
		_movieRootNode.DoNotAcceptEvents = true;
		_movieRootNode.AddChild(rootWidget);
		IsLoaded = true;
		IsReleased = false;
	}

	public void Update()
	{
		_movieRootNode.ScaledSuggestedWidth = Context.TwoDimensionContext.Width;
		_movieRootNode.ScaledSuggestedHeight = Context.TwoDimensionContext.Height;
	}

	public void Release()
	{
		IsLoaded = false;
		IsReleased = true;
		_movieRootNode.OnBeforeRemovedChild(_movieRootNode);
		_root.DestroyDataSource();
		_movieRootNode.ParentWidget = null;
		Context.OnMovieReleased(MovieName);
	}

	public void RefreshBindingWithChildren()
	{
		_root.RefreshBindingWithChildren();
	}

	public void OnResourcesRefreshed(SpriteData spriteData, WidgetFactory widgetFactory, BrushFactory brushFactory, FontFactory fontFactory)
	{
		Context.RefreshResources(spriteData, fontFactory, brushFactory);
		RefreshBindingWithChildren();
	}
}

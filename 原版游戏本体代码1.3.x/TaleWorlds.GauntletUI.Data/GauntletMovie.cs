using System;
using System.Collections.Generic;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.Library;

namespace TaleWorlds.GauntletUI.Data
{
	// Token: 0x02000004 RID: 4
	public class GauntletMovie : IGauntletMovie
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x06000003 RID: 3 RVA: 0x00002058 File Offset: 0x00000258
		// (set) Token: 0x06000004 RID: 4 RVA: 0x00002060 File Offset: 0x00000260
		public WidgetFactory WidgetFactory { get; private set; }

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000005 RID: 5 RVA: 0x00002069 File Offset: 0x00000269
		// (set) Token: 0x06000006 RID: 6 RVA: 0x00002071 File Offset: 0x00000271
		public BrushFactory BrushFactory { get; private set; }

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000007 RID: 7 RVA: 0x0000207A File Offset: 0x0000027A
		// (set) Token: 0x06000008 RID: 8 RVA: 0x00002082 File Offset: 0x00000282
		public UIContext Context { get; private set; }

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000009 RID: 9 RVA: 0x0000208B File Offset: 0x0000028B
		public IViewModel ViewModel
		{
			get
			{
				return this._viewModel;
			}
		}

		// Token: 0x17000005 RID: 5
		// (get) Token: 0x0600000A RID: 10 RVA: 0x00002093 File Offset: 0x00000293
		// (set) Token: 0x0600000B RID: 11 RVA: 0x0000209B File Offset: 0x0000029B
		public string MovieName { get; private set; }

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x0600000C RID: 12 RVA: 0x000020A4 File Offset: 0x000002A4
		// (set) Token: 0x0600000D RID: 13 RVA: 0x000020AC File Offset: 0x000002AC
		public GauntletView RootView { get; private set; }

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x0600000E RID: 14 RVA: 0x000020B5 File Offset: 0x000002B5
		public Widget RootWidget
		{
			get
			{
				if (this.RootView == null)
				{
					return null;
				}
				return this.RootView.Target;
			}
		}

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x0600000F RID: 15 RVA: 0x000020CC File Offset: 0x000002CC
		// (set) Token: 0x06000010 RID: 16 RVA: 0x000020D4 File Offset: 0x000002D4
		public bool IsLoaded { get; private set; }

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000011 RID: 17 RVA: 0x000020DD File Offset: 0x000002DD
		// (set) Token: 0x06000012 RID: 18 RVA: 0x000020E5 File Offset: 0x000002E5
		public bool IsReleased { get; private set; }

		// Token: 0x06000013 RID: 19 RVA: 0x000020F0 File Offset: 0x000002F0
		private GauntletMovie(string movieName, UIContext context, WidgetFactory widgetFactory, IViewModel viewModel, bool hotReloadEnabled)
		{
			this.WidgetFactory = widgetFactory;
			this.BrushFactory = context.BrushFactory;
			this.Context = context;
			this._isHotReloadEnabled = hotReloadEnabled;
			this.WidgetFactory.PrefabChange += this.OnResourceChanged;
			this.BrushFactory.BrushChange += this.OnResourceChanged;
			this._viewModel = viewModel;
			this.MovieName = movieName;
			this._movieRootNode = new Widget(this.Context);
			this.Context.Root.AddChild(this._movieRootNode);
			this._movieRootNode.WidthSizePolicy = SizePolicy.Fixed;
			this._movieRootNode.HeightSizePolicy = SizePolicy.Fixed;
			this._movieRootNode.ScaledSuggestedWidth = this.Context.TwoDimensionContext.Width;
			this._movieRootNode.ScaledSuggestedHeight = this.Context.TwoDimensionContext.Height;
			this._movieRootNode.DoNotAcceptEvents = true;
			this.IsLoaded = false;
			this.IsReleased = false;
		}

		// Token: 0x06000014 RID: 20 RVA: 0x000021F1 File Offset: 0x000003F1
		public void RefreshDataSource(IViewModel dataSourve)
		{
			this._viewModel = dataSourve;
			this.RootView.RefreshBindingWithChildren();
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00002205 File Offset: 0x00000405
		private void RefreshResources()
		{
			this.RootView.ClearEventHandlersWithChildren();
			this.RootView = null;
			this._movieRootNode.RemoveAllChildren();
			this.Context.OnMovieReleased(this.MovieName);
			this.IsLoaded = false;
			this.LoadMovie();
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002242 File Offset: 0x00000442
		private void OnResourceChanged()
		{
			if (!this._isHotReloadEnabled)
			{
				return;
			}
			this.RefreshResources();
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002254 File Offset: 0x00000454
		private void LoadMovie()
		{
			this._moviePrefab = this.WidgetFactory.GetCustomType(this.MovieName);
			if (this._moviePrefab == null)
			{
				return;
			}
			this.IsLoaded = true;
			this.IsReleased = false;
			WidgetCreationData widgetCreationData = new WidgetCreationData(this.Context, this.WidgetFactory);
			widgetCreationData.AddExtensionData(this);
			WidgetInstantiationResult widgetInstantiationResult = this._moviePrefab.Instantiate(widgetCreationData);
			this.RootView = widgetInstantiationResult.GetGauntletView();
			Widget target = this.RootView.Target;
			this._movieRootNode.AddChild(target);
			this.RootView.RefreshBindingWithChildren();
			this.Context.OnMovieLoaded(this.MovieName);
		}

		// Token: 0x06000018 RID: 24 RVA: 0x000022F8 File Offset: 0x000004F8
		public void Release()
		{
			this._movieRootNode.OnBeforeRemovedChild(this._movieRootNode);
			GauntletView rootView = this.RootView;
			if (rootView != null)
			{
				rootView.ReleaseBindingWithChildren();
			}
			this._moviePrefab.OnRelease();
			this.WidgetFactory.OnUnload(this.MovieName);
			this.WidgetFactory.PrefabChange -= this.OnResourceChanged;
			this.BrushFactory.BrushChange -= this.OnResourceChanged;
			this.Context.OnMovieReleased(this.MovieName);
			this._movieRootNode.ParentWidget = null;
			this.IsLoaded = false;
			this.IsReleased = true;
		}

		// Token: 0x06000019 RID: 25 RVA: 0x0000239C File Offset: 0x0000059C
		internal void OnItemRemoved(string type)
		{
			this.WidgetFactory.OnUnload(type);
		}

		// Token: 0x0600001A RID: 26 RVA: 0x000023AA File Offset: 0x000005AA
		public void Update()
		{
			this._movieRootNode.ScaledSuggestedWidth = this.Context.TwoDimensionContext.Width;
			this._movieRootNode.ScaledSuggestedHeight = this.Context.TwoDimensionContext.Height;
		}

		// Token: 0x0600001B RID: 27 RVA: 0x000023E4 File Offset: 0x000005E4
		internal object GetViewModelAtPath(BindingPath path, bool isListExpected)
		{
			if (this._viewModel != null && path != null)
			{
				BindingPath path2 = path.Simplify();
				return this._viewModel.GetViewModelAtPath(path2, isListExpected);
			}
			return null;
		}

		// Token: 0x0600001C RID: 28 RVA: 0x00002418 File Offset: 0x00000618
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

		// Token: 0x0600001D RID: 29 RVA: 0x00002493 File Offset: 0x00000693
		public void RefreshBindingWithChildren()
		{
			this.RootView.RefreshBindingWithChildren();
		}

		// Token: 0x0600001E RID: 30 RVA: 0x000024A0 File Offset: 0x000006A0
		public GauntletView FindViewOf(Widget widget)
		{
			return widget.GetComponent<GauntletView>();
		}

		// Token: 0x04000004 RID: 4
		private WidgetPrefab _moviePrefab;

		// Token: 0x04000005 RID: 5
		private IViewModel _viewModel;

		// Token: 0x04000007 RID: 7
		private Widget _movieRootNode;

		// Token: 0x0400000B RID: 11
		private bool _isHotReloadEnabled;
	}
}

using System;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.PrefabSystem;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.Data
{
	// Token: 0x02000006 RID: 6
	public class GeneratedGauntletMovie : IGauntletMovie
	{
		// Token: 0x17000011 RID: 17
		// (get) Token: 0x0600005B RID: 91 RVA: 0x000038EE File Offset: 0x00001AEE
		// (set) Token: 0x0600005C RID: 92 RVA: 0x000038F6 File Offset: 0x00001AF6
		public UIContext Context { get; private set; }

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x0600005D RID: 93 RVA: 0x000038FF File Offset: 0x00001AFF
		// (set) Token: 0x0600005E RID: 94 RVA: 0x00003907 File Offset: 0x00001B07
		public Widget RootWidget { get; private set; }

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x0600005F RID: 95 RVA: 0x00003910 File Offset: 0x00001B10
		// (set) Token: 0x06000060 RID: 96 RVA: 0x00003918 File Offset: 0x00001B18
		public string MovieName { get; private set; }

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x06000061 RID: 97 RVA: 0x00003921 File Offset: 0x00001B21
		// (set) Token: 0x06000062 RID: 98 RVA: 0x00003929 File Offset: 0x00001B29
		public bool IsLoaded { get; private set; }

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x06000063 RID: 99 RVA: 0x00003932 File Offset: 0x00001B32
		// (set) Token: 0x06000064 RID: 100 RVA: 0x0000393A File Offset: 0x00001B3A
		public bool IsReleased { get; private set; }

		// Token: 0x06000065 RID: 101 RVA: 0x00003944 File Offset: 0x00001B44
		public GeneratedGauntletMovie(string movieName, Widget rootWidget)
		{
			this.MovieName = movieName;
			this.RootWidget = rootWidget;
			this.Context = rootWidget.Context;
			this._root = (IGeneratedGauntletMovieRoot)rootWidget;
			this._movieRootNode = new Widget(this.Context);
			this.Context.Root.AddChild(this._movieRootNode);
			this._movieRootNode.WidthSizePolicy = SizePolicy.Fixed;
			this._movieRootNode.HeightSizePolicy = SizePolicy.Fixed;
			this._movieRootNode.ScaledSuggestedWidth = this.Context.TwoDimensionContext.Width;
			this._movieRootNode.ScaledSuggestedHeight = this.Context.TwoDimensionContext.Height;
			this._movieRootNode.DoNotAcceptEvents = true;
			this._movieRootNode.AddChild(rootWidget);
			this.IsLoaded = true;
			this.IsReleased = false;
		}

		// Token: 0x06000066 RID: 102 RVA: 0x00003A18 File Offset: 0x00001C18
		public void Update()
		{
			this._movieRootNode.ScaledSuggestedWidth = this.Context.TwoDimensionContext.Width;
			this._movieRootNode.ScaledSuggestedHeight = this.Context.TwoDimensionContext.Height;
		}

		// Token: 0x06000067 RID: 103 RVA: 0x00003A50 File Offset: 0x00001C50
		public void Release()
		{
			this.IsLoaded = false;
			this.IsReleased = true;
			this._movieRootNode.OnBeforeRemovedChild(this._movieRootNode);
			this._root.DestroyDataSource();
			this._movieRootNode.ParentWidget = null;
			this.Context.OnMovieReleased(this.MovieName);
		}

		// Token: 0x06000068 RID: 104 RVA: 0x00003AA4 File Offset: 0x00001CA4
		public void RefreshBindingWithChildren()
		{
			this._root.RefreshBindingWithChildren();
		}

		// Token: 0x06000069 RID: 105 RVA: 0x00003AB1 File Offset: 0x00001CB1
		public void OnResourcesRefreshed(SpriteData spriteData, WidgetFactory widgetFactory, BrushFactory brushFactory, FontFactory fontFactory)
		{
			this.Context.RefreshResources(spriteData, fontFactory, brushFactory);
			this.RefreshBindingWithChildren();
		}

		// Token: 0x0400001A RID: 26
		private Widget _movieRootNode;

		// Token: 0x0400001D RID: 29
		private IGeneratedGauntletMovieRoot _root;
	}
}

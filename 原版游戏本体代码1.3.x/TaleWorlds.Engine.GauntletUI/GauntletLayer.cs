using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.Data;
using TaleWorlds.Library;
using TaleWorlds.ScreenSystem;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.Engine.GauntletUI
{
	// Token: 0x02000007 RID: 7
	public class GauntletLayer : ScreenLayer
	{
		// Token: 0x17000009 RID: 9
		// (get) Token: 0x06000016 RID: 22 RVA: 0x00002199 File Offset: 0x00000399
		// (set) Token: 0x06000017 RID: 23 RVA: 0x000021A1 File Offset: 0x000003A1
		public IGamepadNavigationContext GamepadNavigationContext { get; private set; }

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x06000018 RID: 24 RVA: 0x000021AA File Offset: 0x000003AA
		// (set) Token: 0x06000019 RID: 25 RVA: 0x000021B2 File Offset: 0x000003B2
		public UIContext UIContext { get; private set; }

		// Token: 0x0600001A RID: 26 RVA: 0x000021BC File Offset: 0x000003BC
		private void InitializeContext()
		{
			this.UIContext = new UIContext(this._twoDimensionContext, base.Input, UIResourceManager.SpriteData, UIResourceManager.FontFactory, UIResourceManager.BrushFactory);
			this.UIContext.ScaleModifier = base.Scale;
			this.UIContext.Initialize();
			this.GamepadNavigationContext = new GauntletGamepadNavigationContext(new Func<Vector2, bool>(this.GetIsBlockedAtPosition), new Func<int>(this.GetLastScreenOrder), new Func<bool>(this.GetIsAvailableForGamepadNavigation));
			this.UIContext.InitializeGamepadNavigation(this.GamepadNavigationContext);
			this.UIContext.EventManager.OnFocusedWidgetChanged += this.EventManagerOnFocusedWidgetChanged;
			this.UIContext.EventManager.OnGetIsHitThisFrame = new Func<bool>(this.GetIsHitThisFrame);
			this.UIContext.EventManager.UsableArea = base.UsableArea;
			this.RefreshContextName();
		}

		// Token: 0x0600001B RID: 27 RVA: 0x0000229F File Offset: 0x0000049F
		private void RefreshContextName()
		{
			if (this.UIContext == null)
			{
				return;
			}
			this.UIContext.Name = base.Name;
		}

		// Token: 0x0600001C RID: 28 RVA: 0x000022BC File Offset: 0x000004BC
		private void ClearContext()
		{
			foreach (GauntletMovieIdentifier gauntletMovieIdentifier in this._movieIdentifiers)
			{
				gauntletMovieIdentifier.Movie.Release();
			}
			this.UIContext.EventManager.OnGetIsHitThisFrame = null;
			this.UIContext.EventManager.OnFocusedWidgetChanged -= this.EventManagerOnFocusedWidgetChanged;
			this.UIContext.OnFinalize();
			this.UIContext = null;
		}

		// Token: 0x0600001D RID: 29 RVA: 0x00002350 File Offset: 0x00000550
		public void OnResourceRefreshBegin(out List<GauntletMovieIdentifier> previouslyLoadedMovies)
		{
			previouslyLoadedMovies = this._movieIdentifiers.ToList<GauntletMovieIdentifier>();
			for (int i = 0; i < this._movieIdentifiers.Count; i++)
			{
				this.ReleaseMovie(this._movieIdentifiers[i]);
			}
			this.ClearContext();
		}

		// Token: 0x0600001E RID: 30 RVA: 0x00002398 File Offset: 0x00000598
		public void OnResourceRefreshEnd(List<GauntletMovieIdentifier> previouslyLoadedMovies)
		{
			this.InitializeContext();
			for (int i = 0; i < previouslyLoadedMovies.Count; i++)
			{
				this.LoadMovie(previouslyLoadedMovies[i]);
			}
		}

		// Token: 0x0600001F RID: 31 RVA: 0x000023CC File Offset: 0x000005CC
		public GauntletLayer(string name, int localOrder, bool shouldClear = false)
			: base(name, localOrder)
		{
			this._movieIdentifiers = new MBList<GauntletMovieIdentifier>();
			ResourceDepot resourceDepot = UIResourceManager.ResourceDepot;
			this.TwoDimensionView = TwoDimensionView.CreateTwoDimension(name);
			if (shouldClear)
			{
				this.TwoDimensionView.SetClearColor(255U);
				this.TwoDimensionView.SetRenderOption(View.ViewRenderOptions.ClearColor, true);
			}
			this.TwoDimensionPlatform = new TwoDimensionEnginePlatform(this.TwoDimensionView);
			this._twoDimensionContext = new TwoDimensionContext(this.TwoDimensionPlatform, UIResourceManager.ResourceContext, resourceDepot);
			this.InitializeContext();
		}

		// Token: 0x06000020 RID: 32 RVA: 0x0000244C File Offset: 0x0000064C
		private void EventManagerOnFocusedWidgetChanged()
		{
			if (this.UIContext.EventManager.FocusedWidget != null)
			{
				ScreenManager.TrySetFocus(this);
				return;
			}
			if (!base.IsFocusLayer)
			{
				ScreenManager.TryLoseFocus(this);
			}
		}

		// Token: 0x06000021 RID: 33 RVA: 0x00002478 File Offset: 0x00000678
		public GauntletMovieIdentifier GetMovieIdentifier(string movieName)
		{
			for (int i = 0; i < this._movieIdentifiers.Count; i++)
			{
				if (this._movieIdentifiers[i].MovieName == movieName)
				{
					return this._movieIdentifiers[i];
				}
			}
			return null;
		}

		// Token: 0x06000022 RID: 34 RVA: 0x000024C4 File Offset: 0x000006C4
		public GauntletMovieIdentifier LoadMovie(string movieName, ViewModel dataSource)
		{
			GauntletMovieIdentifier gauntletMovieIdentifier = new GauntletMovieIdentifier(movieName, dataSource);
			this.LoadMovie(gauntletMovieIdentifier);
			return gauntletMovieIdentifier;
		}

		// Token: 0x06000023 RID: 35 RVA: 0x000024E1 File Offset: 0x000006E1
		private void LoadMovie(GauntletMovieIdentifier identifier)
		{
			identifier.Movie = this.LoadMovieAux(identifier.MovieName, identifier.DataSource);
			this._movieIdentifiers.Add(identifier);
			this.RefreshContextName();
		}

		// Token: 0x06000024 RID: 36 RVA: 0x00002510 File Offset: 0x00000710
		private IGauntletMovie LoadMovieAux(string movieName, ViewModel dataSource)
		{
			bool isUsingGeneratedPrefabs = UIConfig.GetIsUsingGeneratedPrefabs();
			bool isHotReloadEnabled = UIConfig.GetIsHotReloadEnabled();
			return GauntletMovie.Load(this.UIContext, UIResourceManager.WidgetFactory, movieName, dataSource, !isUsingGeneratedPrefabs, isHotReloadEnabled);
		}

		// Token: 0x06000025 RID: 37 RVA: 0x00002540 File Offset: 0x00000740
		public void ReleaseMovie(GauntletMovieIdentifier identifier)
		{
			if (this._movieIdentifiers.Contains(identifier))
			{
				if (!identifier.Movie.IsReleased)
				{
					identifier.Movie.Release();
				}
				this._movieIdentifiers.Remove(identifier);
				this.RefreshContextName();
				return;
			}
			Debug.FailedAssert("Failed to release movie from gauntlet layer: " + identifier.MovieName, "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine.GauntletUI\\GauntletLayer.cs", "ReleaseMovie", 208);
		}

		// Token: 0x06000026 RID: 38 RVA: 0x000025AB File Offset: 0x000007AB
		protected override void OnActivate()
		{
			base.OnActivate();
			this.TwoDimensionView.SetEnable(true);
			this.UIContext.Activate();
		}

		// Token: 0x06000027 RID: 39 RVA: 0x000025CA File Offset: 0x000007CA
		protected override void OnDeactivate()
		{
			this.TwoDimensionPlatform.Clear();
			this.TwoDimensionView.Clear();
			this.TwoDimensionView.SetEnable(false);
			this.UIContext.Deactivate();
			base.OnDeactivate();
		}

		// Token: 0x06000028 RID: 40 RVA: 0x00002600 File Offset: 0x00000800
		protected override void Tick(float dt)
		{
			base.Tick(dt);
			this.UIContext.Update(dt);
			foreach (GauntletMovieIdentifier gauntletMovieIdentifier in this._movieIdentifiers)
			{
				gauntletMovieIdentifier.Movie.Update();
			}
		}

		// Token: 0x06000029 RID: 41 RVA: 0x00002668 File Offset: 0x00000868
		protected override void LateUpdate(float dt)
		{
			base.LateUpdate(dt);
			this.UIContext.SetIsMouseEnabled(base.IsHitThisFrame);
			this.UIContext.LateUpdate(dt);
			base.ActiveCursor = (CursorType)this.UIContext.ActiveCursorOfContext;
		}

		// Token: 0x0600002A RID: 42 RVA: 0x000026A0 File Offset: 0x000008A0
		protected override void RenderTick(float dt)
		{
			base.RenderTick(dt);
			this.TwoDimensionView.BeginFrame();
			this.TwoDimensionPlatform.OnFrameBegin();
			this.UIContext.RenderTick(dt);
			this.TwoDimensionView.EndFrame();
			this.TwoDimensionPlatform.OnFrameEnd();
		}

		// Token: 0x0600002B RID: 43 RVA: 0x000026EC File Offset: 0x000008EC
		protected override void Update(IReadOnlyList<int> lastKeysPressed)
		{
			Widget focusedWidget = this.UIContext.EventManager.FocusedWidget;
			if (focusedWidget == null)
			{
				return;
			}
			focusedWidget.HandleInput(lastKeysPressed);
		}

		// Token: 0x0600002C RID: 44 RVA: 0x0000270C File Offset: 0x0000090C
		protected override void OnFinalize()
		{
			this.ClearContext();
			for (int i = 0; i < this._movieIdentifiers.Count; i++)
			{
				if (this._movieIdentifiers[i].Movie.IsLoaded)
				{
					Debug.FailedAssert("Movie was not released before finalizing layer: " + this._movieIdentifiers[i].MovieName, "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine.GauntletUI\\GauntletLayer.cs", "OnFinalize", 288);
				}
			}
			this.TwoDimensionView.ManualInvalidate();
			base.OnFinalize();
		}

		// Token: 0x0600002D RID: 45 RVA: 0x0000278D File Offset: 0x0000098D
		protected override void RefreshGlobalOrder(ref int currentOrder)
		{
			this.TwoDimensionView.SetRenderOrder(currentOrder);
			currentOrder++;
		}

		// Token: 0x0600002E RID: 46 RVA: 0x000027A2 File Offset: 0x000009A2
		public override void ProcessEvents()
		{
			base.ProcessEvents();
			this.UIContext.UpdateInput(base._usedInputs);
		}

		// Token: 0x0600002F RID: 47 RVA: 0x000027BC File Offset: 0x000009BC
		public override bool HitTest(Vector2 position)
		{
			foreach (GauntletMovieIdentifier gauntletMovieIdentifier in this._movieIdentifiers)
			{
				if (this.UIContext.HitTest(gauntletMovieIdentifier.Movie.RootWidget, position))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000030 RID: 48 RVA: 0x00002828 File Offset: 0x00000A28
		private bool GetIsBlockedAtPosition(Vector2 position)
		{
			return ScreenManager.IsLayerBlockedAtPosition(this, position);
		}

		// Token: 0x06000031 RID: 49 RVA: 0x00002834 File Offset: 0x00000A34
		public override bool HitTest()
		{
			foreach (GauntletMovieIdentifier gauntletMovieIdentifier in this._movieIdentifiers)
			{
				if (this.UIContext.HitTest(gauntletMovieIdentifier.Movie.RootWidget))
				{
					return true;
				}
			}
			this.UIContext.EventManager.SetHoveredView(null);
			return false;
		}

		// Token: 0x06000032 RID: 50 RVA: 0x000028B0 File Offset: 0x00000AB0
		public override bool FocusTest()
		{
			foreach (GauntletMovieIdentifier gauntletMovieIdentifier in this._movieIdentifiers)
			{
				if (this.UIContext.FocusTest(gauntletMovieIdentifier.Movie.RootWidget))
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x06000033 RID: 51 RVA: 0x0000291C File Offset: 0x00000B1C
		public override bool IsFocusedOnInput()
		{
			return this.UIContext.EventManager.FocusedWidget is EditableTextWidget;
		}

		// Token: 0x06000034 RID: 52 RVA: 0x00002936 File Offset: 0x00000B36
		protected override void OnLoseFocus()
		{
			base.OnLoseFocus();
			this.UIContext.EventManager.ClearFocus();
		}

		// Token: 0x06000035 RID: 53 RVA: 0x0000294E File Offset: 0x00000B4E
		public override void OnOnScreenKeyboardDone(string inputText)
		{
			if (inputText == null)
			{
				Debug.FailedAssert("OnScreenKeyboardDone returned null!", "C:\\BuildAgent\\work\\mb3\\Source\\Engine\\TaleWorlds.Engine.GauntletUI\\GauntletLayer.cs", "OnOnScreenKeyboardDone", 381);
				inputText = string.Empty;
			}
			base.OnOnScreenKeyboardDone(inputText);
			this.UIContext.OnOnScreenkeyboardTextInputDone(inputText);
		}

		// Token: 0x06000036 RID: 54 RVA: 0x00002986 File Offset: 0x00000B86
		public override void OnOnScreenKeyboardCanceled()
		{
			base.OnOnScreenKeyboardCanceled();
			this.UIContext.OnOnScreenKeyboardCanceled();
		}

		// Token: 0x06000037 RID: 55 RVA: 0x0000299C File Offset: 0x00000B9C
		public override void UpdateLayout()
		{
			base.UpdateLayout();
			this.UIContext.ScaleModifier = base.Scale;
			this.UIContext.EventManager.UsableArea = base.UsableArea;
			this._movieIdentifiers.ForEach(delegate(GauntletMovieIdentifier m)
			{
				m.DataSource.RefreshValues();
			});
			this._movieIdentifiers.ForEach(delegate(GauntletMovieIdentifier m)
			{
				m.Movie.RefreshBindingWithChildren();
			});
			this.UIContext.EventManager.UpdateLayout();
		}

		// Token: 0x06000038 RID: 56 RVA: 0x00002A3A File Offset: 0x00000C3A
		public bool GetIsAvailableForGamepadNavigation()
		{
			return base.LastActiveState && base.IsActive && (base.IsFocusLayer || (base.InputRestrictions.InputUsageMask & InputUsageMask.Mouse) > InputUsageMask.Invalid);
		}

		// Token: 0x06000039 RID: 57 RVA: 0x00002A68 File Offset: 0x00000C68
		private bool GetIsHitThisFrame()
		{
			return base.IsHitThisFrame;
		}

		// Token: 0x0600003A RID: 58 RVA: 0x00002A70 File Offset: 0x00000C70
		private int GetLastScreenOrder()
		{
			return base.ScreenOrderInLastFrame;
		}

		// Token: 0x0600003B RID: 59 RVA: 0x00002A78 File Offset: 0x00000C78
		public override void DrawDebugInfo()
		{
			foreach (GauntletMovieIdentifier gauntletMovieIdentifier in this._movieIdentifiers)
			{
				Imgui.Text("Movie: " + gauntletMovieIdentifier.MovieName);
				string str = "Data Source: ";
				ViewModel dataSource = gauntletMovieIdentifier.DataSource;
				Imgui.Text(str + (((dataSource != null) ? dataSource.GetType().Name : null) ?? "No Datasource"));
			}
			base.DrawDebugInfo();
			Imgui.Text("Press 'Shift+F' to take widget hierarchy snapshot.");
			this.UIContext.DrawWidgetDebugInfo();
		}

		// Token: 0x04000005 RID: 5
		private readonly MBList<GauntletMovieIdentifier> _movieIdentifiers;

		// Token: 0x04000006 RID: 6
		private readonly TwoDimensionContext _twoDimensionContext;

		// Token: 0x04000007 RID: 7
		public readonly TwoDimensionView TwoDimensionView;

		// Token: 0x04000008 RID: 8
		public readonly ITwoDimensionPlatform TwoDimensionPlatform;
	}
}

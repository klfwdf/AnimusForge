using System;
using System.Collections.Generic;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.GauntletInput;
using TaleWorlds.InputSystem;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x02000033 RID: 51
	public class UIContext
	{
		// Token: 0x17000102 RID: 258
		// (get) Token: 0x06000369 RID: 873 RVA: 0x0000EFD5 File Offset: 0x0000D1D5
		// (set) Token: 0x0600036A RID: 874 RVA: 0x0000EFDD File Offset: 0x0000D1DD
		public UIContext.MouseCursors ActiveCursorOfContext { get; set; }

		// Token: 0x17000103 RID: 259
		// (get) Token: 0x0600036B RID: 875 RVA: 0x0000EFE6 File Offset: 0x0000D1E6
		// (set) Token: 0x0600036C RID: 876 RVA: 0x0000EFEE File Offset: 0x0000D1EE
		public bool IsDynamicScaleEnabled { get; set; } = true;

		// Token: 0x17000104 RID: 260
		// (get) Token: 0x0600036D RID: 877 RVA: 0x0000EFF7 File Offset: 0x0000D1F7
		// (set) Token: 0x0600036E RID: 878 RVA: 0x0000EFFF File Offset: 0x0000D1FF
		public float ScaleModifier { get; set; } = 1f;

		// Token: 0x17000105 RID: 261
		// (get) Token: 0x0600036F RID: 879 RVA: 0x0000F008 File Offset: 0x0000D208
		// (set) Token: 0x06000370 RID: 880 RVA: 0x0000F010 File Offset: 0x0000D210
		public string Name { get; set; }

		// Token: 0x17000106 RID: 262
		// (get) Token: 0x06000371 RID: 881 RVA: 0x0000F019 File Offset: 0x0000D219
		// (set) Token: 0x06000372 RID: 882 RVA: 0x0000F021 File Offset: 0x0000D221
		public bool IsActive { get; private set; }

		// Token: 0x17000107 RID: 263
		// (get) Token: 0x06000373 RID: 883 RVA: 0x0000F02A File Offset: 0x0000D22A
		// (set) Token: 0x06000374 RID: 884 RVA: 0x0000F032 File Offset: 0x0000D232
		public float ContextAlpha { get; set; } = 1f;

		// Token: 0x17000108 RID: 264
		// (get) Token: 0x06000375 RID: 885 RVA: 0x0000F03B File Offset: 0x0000D23B
		// (set) Token: 0x06000376 RID: 886 RVA: 0x0000F043 File Offset: 0x0000D243
		public float Scale { get; private set; }

		// Token: 0x17000109 RID: 265
		// (get) Token: 0x06000377 RID: 887 RVA: 0x0000F04C File Offset: 0x0000D24C
		// (set) Token: 0x06000378 RID: 888 RVA: 0x0000F054 File Offset: 0x0000D254
		public float CustomScale { get; private set; }

		// Token: 0x1700010A RID: 266
		// (get) Token: 0x06000379 RID: 889 RVA: 0x0000F05D File Offset: 0x0000D25D
		// (set) Token: 0x0600037A RID: 890 RVA: 0x0000F065 File Offset: 0x0000D265
		public float CustomInverseScale { get; private set; }

		// Token: 0x1700010B RID: 267
		// (get) Token: 0x0600037B RID: 891 RVA: 0x0000F06E File Offset: 0x0000D26E
		// (set) Token: 0x0600037C RID: 892 RVA: 0x0000F076 File Offset: 0x0000D276
		public string CurrentLanugageCode { get; private set; }

		// Token: 0x1700010C RID: 268
		// (get) Token: 0x0600037D RID: 893 RVA: 0x0000F07F File Offset: 0x0000D27F
		// (set) Token: 0x0600037E RID: 894 RVA: 0x0000F087 File Offset: 0x0000D287
		public Random UIRandom { get; private set; }

		// Token: 0x1700010D RID: 269
		// (get) Token: 0x0600037F RID: 895 RVA: 0x0000F090 File Offset: 0x0000D290
		// (set) Token: 0x06000380 RID: 896 RVA: 0x0000F098 File Offset: 0x0000D298
		public float InverseScale { get; private set; }

		// Token: 0x1700010E RID: 270
		// (get) Token: 0x06000381 RID: 897 RVA: 0x0000F0A1 File Offset: 0x0000D2A1
		// (set) Token: 0x06000382 RID: 898 RVA: 0x0000F0A9 File Offset: 0x0000D2A9
		public EventManager EventManager { get; private set; }

		// Token: 0x1700010F RID: 271
		// (get) Token: 0x06000383 RID: 899 RVA: 0x0000F0B2 File Offset: 0x0000D2B2
		public Widget Root
		{
			get
			{
				return this.EventManager.Root;
			}
		}

		// Token: 0x17000110 RID: 272
		// (get) Token: 0x06000384 RID: 900 RVA: 0x0000F0BF File Offset: 0x0000D2BF
		public ResourceDepot ResourceDepot
		{
			get
			{
				return this.TwoDimensionContext.ResourceDepot;
			}
		}

		// Token: 0x17000111 RID: 273
		// (get) Token: 0x06000385 RID: 901 RVA: 0x0000F0CC File Offset: 0x0000D2CC
		// (set) Token: 0x06000386 RID: 902 RVA: 0x0000F0D4 File Offset: 0x0000D2D4
		public TwoDimensionContext TwoDimensionContext { get; private set; }

		// Token: 0x17000112 RID: 274
		// (get) Token: 0x06000387 RID: 903 RVA: 0x0000F0DD File Offset: 0x0000D2DD
		public IEnumerable<Brush> Brushes
		{
			get
			{
				return this.BrushFactory.Brushes;
			}
		}

		// Token: 0x17000113 RID: 275
		// (get) Token: 0x06000388 RID: 904 RVA: 0x0000F0EA File Offset: 0x0000D2EA
		public Brush DefaultBrush
		{
			get
			{
				return this.BrushFactory.DefaultBrush;
			}
		}

		// Token: 0x17000114 RID: 276
		// (get) Token: 0x06000389 RID: 905 RVA: 0x0000F0F7 File Offset: 0x0000D2F7
		// (set) Token: 0x0600038A RID: 906 RVA: 0x0000F0FF File Offset: 0x0000D2FF
		public SpriteData SpriteData { get; private set; }

		// Token: 0x17000115 RID: 277
		// (get) Token: 0x0600038B RID: 907 RVA: 0x0000F108 File Offset: 0x0000D308
		// (set) Token: 0x0600038C RID: 908 RVA: 0x0000F110 File Offset: 0x0000D310
		public BrushFactory BrushFactory { get; private set; }

		// Token: 0x17000116 RID: 278
		// (get) Token: 0x0600038D RID: 909 RVA: 0x0000F119 File Offset: 0x0000D319
		// (set) Token: 0x0600038E RID: 910 RVA: 0x0000F121 File Offset: 0x0000D321
		public FontFactory FontFactory { get; private set; }

		// Token: 0x17000117 RID: 279
		// (get) Token: 0x0600038F RID: 911 RVA: 0x0000F12A File Offset: 0x0000D32A
		public IReadonlyInputContext InputContext
		{
			get
			{
				return this._uiInputContext;
			}
		}

		// Token: 0x17000118 RID: 280
		// (get) Token: 0x06000390 RID: 912 RVA: 0x0000F132 File Offset: 0x0000D332
		// (set) Token: 0x06000391 RID: 913 RVA: 0x0000F13A File Offset: 0x0000D33A
		public IGamepadNavigationContext GamepadNavigation { get; private set; }

		// Token: 0x17000119 RID: 281
		// (get) Token: 0x06000392 RID: 914 RVA: 0x0000F143 File Offset: 0x0000D343
		// (set) Token: 0x06000393 RID: 915 RVA: 0x0000F14B File Offset: 0x0000D34B
		public ulong LocalFrameNumber { get; private set; }

		// Token: 0x06000394 RID: 916 RVA: 0x0000F154 File Offset: 0x0000D354
		public UIContext(TwoDimensionContext twoDimensionContext, IInputContext inputContext, SpriteData spriteData, FontFactory fontFactory, BrushFactory brushFactory)
		{
			this._isMouseEnabled = true;
			this._inputContext = inputContext;
			this._initializedWithExistingResources = true;
			this._uiInputContext = new GauntletInputContext(inputContext);
			this.TwoDimensionContext = twoDimensionContext;
			this.GamepadNavigation = new EmptyGamepadNavigationContext();
			this.SpriteData = spriteData;
			this.FontFactory = fontFactory;
			this.BrushFactory = brushFactory;
			this.ReferenceHeight = twoDimensionContext.Platform.ReferenceHeight;
			this.InverseReferenceHeight = 1f / this.ReferenceHeight;
			this.ReferenceAspectRatio = twoDimensionContext.Platform.ReferenceWidth / twoDimensionContext.Platform.ReferenceHeight;
		}

		// Token: 0x06000395 RID: 917 RVA: 0x0000F210 File Offset: 0x0000D410
		public UIContext(TwoDimensionContext twoDimensionContext, IInputContext inputContext)
		{
			this._isMouseEnabled = true;
			this._initializedWithExistingResources = false;
			this._inputContext = inputContext;
			this._uiInputContext = new GauntletInputContext(inputContext);
			this.TwoDimensionContext = twoDimensionContext;
			this.GamepadNavigation = new EmptyGamepadNavigationContext();
			this.ReferenceHeight = twoDimensionContext.Platform.ReferenceHeight;
			this.InverseReferenceHeight = 1f / this.ReferenceHeight;
			this.ReferenceAspectRatio = twoDimensionContext.Platform.ReferenceWidth / twoDimensionContext.Platform.ReferenceHeight;
		}

		// Token: 0x06000396 RID: 918 RVA: 0x0000F2B4 File Offset: 0x0000D4B4
		public void Initialize()
		{
			if (!this._initializedWithExistingResources)
			{
				this.SpriteData = new SpriteData("SpriteData");
				this.SpriteData.Load(this.ResourceDepot);
				this.FontFactory = new FontFactory(this.ResourceDepot);
				this.FontFactory.LoadAllFonts(this.SpriteData);
				this.BrushFactory = new BrushFactory(this.ResourceDepot, "Brushes", this.SpriteData, this.FontFactory);
				this.BrushFactory.Initialize();
			}
			this.EventManager = new EventManager(this);
			Widget root = this.Root;
			root.WidthSizePolicy = SizePolicy.Fixed;
			root.HeightSizePolicy = SizePolicy.Fixed;
			root.SuggestedWidth = this.TwoDimensionContext.Width;
			root.SuggestedHeight = this.TwoDimensionContext.Height;
			this.UIRandom = new Random();
			this.UpdateScale();
		}

		// Token: 0x06000397 RID: 919 RVA: 0x0000F38B File Offset: 0x0000D58B
		public Brush GetBrush(string name)
		{
			return this.BrushFactory.GetBrush(name);
		}

		// Token: 0x06000398 RID: 920 RVA: 0x0000F399 File Offset: 0x0000D599
		public void RefreshResources(SpriteData spriteData, FontFactory fontFactory, BrushFactory brushFactory)
		{
			this.SpriteData = spriteData;
			this.FontFactory = fontFactory;
			this.BrushFactory = brushFactory;
		}

		// Token: 0x06000399 RID: 921 RVA: 0x0000F3B0 File Offset: 0x0000D5B0
		public void OnFinalize()
		{
			this.GamepadNavigation.OnFinalize();
			this.EventManager.OnFinalize();
			this.GamepadNavigation.OnFinalize();
		}

		// Token: 0x0600039A RID: 922 RVA: 0x0000F3D3 File Offset: 0x0000D5D3
		public void Activate()
		{
			this.IsActive = true;
			this.EventManager.OnContextActivated();
		}

		// Token: 0x0600039B RID: 923 RVA: 0x0000F3E7 File Offset: 0x0000D5E7
		public void Deactivate()
		{
			this.IsActive = false;
			this.EventManager.OnContextDeactivated();
		}

		// Token: 0x0600039C RID: 924 RVA: 0x0000F3FC File Offset: 0x0000D5FC
		public void Update(float dt)
		{
			this.ActiveCursorOfContext = UIContext.MouseCursors.Default;
			if (!this._initializedWithExistingResources)
			{
				this.BrushFactory.CheckForUpdates();
			}
			if (this.IsDynamicScaleEnabled)
			{
				this.UpdateScale();
			}
			Widget root = this.Root;
			root.SuggestedWidth = this.TwoDimensionContext.Width;
			root.SuggestedHeight = this.TwoDimensionContext.Height;
			this.EventManager.Update(dt);
			ulong localFrameNumber = this.LocalFrameNumber;
			this.LocalFrameNumber = localFrameNumber + 1UL;
		}

		// Token: 0x0600039D RID: 925 RVA: 0x0000F478 File Offset: 0x0000D678
		public void LateUpdate(float dt)
		{
			Vector2 pageSize;
			pageSize..ctor(this.TwoDimensionContext.Width, this.TwoDimensionContext.Height);
			this.EventManager.CalculateCanvas(pageSize, dt);
			this.EventManager.LateUpdate(dt);
			this.EventManager.RecalculateCanvas();
		}

		// Token: 0x0600039E RID: 926 RVA: 0x0000F4C6 File Offset: 0x0000D6C6
		public void RenderTick(float dt)
		{
			this.EventManager.UpdateBrushes(dt);
			this.EventManager.Render(this.TwoDimensionContext);
		}

		// Token: 0x0600039F RID: 927 RVA: 0x0000F4E5 File Offset: 0x0000D6E5
		public void InitializeGamepadNavigation(IGamepadNavigationContext context)
		{
			this.GamepadNavigation = context;
		}

		// Token: 0x060003A0 RID: 928 RVA: 0x0000F4F0 File Offset: 0x0000D6F0
		private void UpdateScale()
		{
			float num;
			if (this.TwoDimensionContext != null)
			{
				num = this.TwoDimensionContext.Height * this.InverseReferenceHeight;
				float num2 = this.TwoDimensionContext.Width / this.TwoDimensionContext.Height;
				if (num2 < this.ReferenceAspectRatio * 0.98f)
				{
					float num3 = num2 / (this.ReferenceAspectRatio * 0.98f);
					num *= num3;
				}
			}
			else
			{
				num = 1f;
			}
			if (this.Scale != num || this.CustomScale != this.Scale * this.ScaleModifier)
			{
				this.Scale = num;
				this.CustomScale = this.Scale * this.ScaleModifier;
				this.InverseScale = 1f / num;
				this.CustomInverseScale = 1f / this.CustomScale;
				this.EventManager.UpdateLayout();
			}
		}

		// Token: 0x060003A1 RID: 929 RVA: 0x0000F5C4 File Offset: 0x0000D7C4
		public void OnOnScreenkeyboardTextInputDone(string inputText)
		{
			EditableTextWidget editableTextWidget;
			if ((editableTextWidget = this.EventManager.FocusedWidget as EditableTextWidget) != null)
			{
				editableTextWidget.SetAllText(inputText);
			}
			this.ReleaseMouseWithoutClick();
		}

		// Token: 0x060003A2 RID: 930 RVA: 0x0000F5F2 File Offset: 0x0000D7F2
		public void OnOnScreenKeyboardCanceled()
		{
			this.ReleaseMouseWithoutClick();
		}

		// Token: 0x060003A3 RID: 931 RVA: 0x0000F5FA File Offset: 0x0000D7FA
		public bool HitTest(Widget root, Vector2 position)
		{
			return EventManager.HitTest(root, position);
		}

		// Token: 0x060003A4 RID: 932 RVA: 0x0000F603 File Offset: 0x0000D803
		public bool HitTest(Widget root)
		{
			return root != null && EventManager.HitTest(root, this._uiInputContext.GetMousePosition());
		}

		// Token: 0x060003A5 RID: 933 RVA: 0x0000F61B File Offset: 0x0000D81B
		public bool FocusTest(Widget root)
		{
			return this.EventManager.FocusTest(root);
		}

		// Token: 0x060003A6 RID: 934 RVA: 0x0000F629 File Offset: 0x0000D829
		public void SetIsMouseEnabled(bool isMouseEnabled)
		{
			this._isMouseEnabled = isMouseEnabled;
		}

		// Token: 0x060003A7 RID: 935 RVA: 0x0000F634 File Offset: 0x0000D834
		public void UpdateInput(InputType handleInputs)
		{
			if (this._isMouseEnabled || this.EventManager.DraggedWidget != null || this.EventManager.FocusedWidget != null)
			{
				if (handleInputs.HasAnyFlag(InputType.MouseButton))
				{
					this.EventManager.MouseMove();
					foreach (InputKey key in this._inputContext.GetClickKeys())
					{
						if (this._inputContext.IsKeyPressed(key))
						{
							this.EventManager.MouseDown();
							break;
						}
					}
					InputKey[] clickKeys;
					foreach (InputKey key2 in clickKeys)
					{
						if (this._inputContext.IsKeyReleased(key2))
						{
							this.EventManager.MouseUp();
							break;
						}
					}
					if (this._inputContext.IsKeyPressed(InputKey.RightMouseButton))
					{
						this.EventManager.MouseAlternateDown();
					}
					if (this._inputContext.IsKeyReleased(InputKey.RightMouseButton))
					{
						this.EventManager.MouseAlternateUp();
					}
				}
				if (handleInputs.HasAnyFlag(InputType.MouseWheel))
				{
					this.EventManager.MouseScroll();
				}
				this.EventManager.RightStickMovement();
				this._previousFrameMouseEnabled = true;
				return;
			}
			if (this._previousFrameMouseEnabled)
			{
				this.ReleaseMouseWithoutClick();
				this._previousFrameMouseEnabled = false;
			}
		}

		// Token: 0x060003A8 RID: 936 RVA: 0x0000F75B File Offset: 0x0000D95B
		public void OnMovieLoaded(string movieName)
		{
			this.GamepadNavigation.OnMovieLoaded(movieName);
		}

		// Token: 0x060003A9 RID: 937 RVA: 0x0000F769 File Offset: 0x0000D969
		public void OnMovieReleased(string movieName)
		{
			this.GamepadNavigation.OnMovieReleased(movieName);
		}

		// Token: 0x060003AA RID: 938 RVA: 0x0000F778 File Offset: 0x0000D978
		private void ReleaseMouseWithoutClick()
		{
			this._uiInputContext.SetMousePositionOverride(new Vector2(-5000f, -5000f));
			this.EventManager.MouseMove();
			this.EventManager.MouseUp();
			this.EventManager.MouseAlternateUp();
			this.EventManager.ClearFocus();
			this._uiInputContext.ResetMousePositionOverride();
		}

		// Token: 0x060003AB RID: 939 RVA: 0x0000F7D8 File Offset: 0x0000D9D8
		public void DrawWidgetDebugInfo()
		{
			if (Input.IsKeyDown(InputKey.LeftShift) && Input.IsKeyPressed(InputKey.F))
			{
				this.IsDebugWidgetInformationFroze = !this.IsDebugWidgetInformationFroze;
				this._currentRootNode = new UIContext.DebugWidgetTreeNode(this.TwoDimensionContext, this.Root, 0);
			}
			if (this.IsDebugWidgetInformationFroze)
			{
				UIContext.DebugWidgetTreeNode currentRootNode = this._currentRootNode;
				if (currentRootNode == null)
				{
					return;
				}
				currentRootNode.DebugDraw();
			}
		}

		// Token: 0x040001A7 RID: 423
		private readonly float ReferenceHeight;

		// Token: 0x040001A8 RID: 424
		private readonly float InverseReferenceHeight;

		// Token: 0x040001A9 RID: 425
		private readonly float ReferenceAspectRatio;

		// Token: 0x040001AA RID: 426
		private const float ReferenceAspectRatioCoeff = 0.98f;

		// Token: 0x040001BB RID: 443
		private readonly GauntletInputContext _uiInputContext;

		// Token: 0x040001BC RID: 444
		private readonly IInputContext _inputContext;

		// Token: 0x040001BF RID: 447
		private bool _initializedWithExistingResources;

		// Token: 0x040001C0 RID: 448
		private bool _previousFrameMouseEnabled;

		// Token: 0x040001C1 RID: 449
		private bool _isMouseEnabled;

		// Token: 0x040001C2 RID: 450
		private bool IsDebugWidgetInformationFroze;

		// Token: 0x040001C3 RID: 451
		private UIContext.DebugWidgetTreeNode _currentRootNode;

		// Token: 0x02000081 RID: 129
		public enum MouseCursors
		{
			// Token: 0x04000448 RID: 1096
			System,
			// Token: 0x04000449 RID: 1097
			Default,
			// Token: 0x0400044A RID: 1098
			Attack,
			// Token: 0x0400044B RID: 1099
			Move,
			// Token: 0x0400044C RID: 1100
			HorizontalResize,
			// Token: 0x0400044D RID: 1101
			VerticalResize,
			// Token: 0x0400044E RID: 1102
			DiagonalRightResize,
			// Token: 0x0400044F RID: 1103
			DiagonalLeftResize,
			// Token: 0x04000450 RID: 1104
			Rotate,
			// Token: 0x04000451 RID: 1105
			Custom,
			// Token: 0x04000452 RID: 1106
			Disabled,
			// Token: 0x04000453 RID: 1107
			RightClickLink
		}

		// Token: 0x02000082 RID: 130
		private class DebugWidgetTreeNode
		{
			// Token: 0x1700029E RID: 670
			// (get) Token: 0x060008EC RID: 2284 RVA: 0x0002311F File Offset: 0x0002131F
			private string ID
			{
				get
				{
					return string.Format("{0}.{1}.{2}", this._depth, this._current.GetSiblingIndex(), this._fullIDPath);
				}
			}

			// Token: 0x060008ED RID: 2285 RVA: 0x0002314C File Offset: 0x0002134C
			public DebugWidgetTreeNode(TwoDimensionContext context, Widget current, int depth)
			{
				this._context = context;
				this._current = current;
				this._depth = depth;
				Widget current2 = this._current;
				this._fullIDPath = ((current2 != null) ? current2.GetFullIDPath() : null) ?? string.Empty;
				int num = this._fullIDPath.LastIndexOf('\\');
				if (num != -1)
				{
					this._displayedName = this._fullIDPath.Substring(num + 1);
				}
				if (string.IsNullOrEmpty(this._displayedName))
				{
					this._displayedName = this._current.Id;
				}
				this._children = new List<UIContext.DebugWidgetTreeNode>();
				this.AddChildren();
			}

			// Token: 0x060008EE RID: 2286 RVA: 0x000231EC File Offset: 0x000213EC
			private void AddChildren()
			{
				foreach (Widget widget in this._current.Children)
				{
					if (widget.ParentWidget == this._current)
					{
						UIContext.DebugWidgetTreeNode item = new UIContext.DebugWidgetTreeNode(this._context, widget, this._depth + 1);
						this._children.Add(item);
					}
				}
			}

			// Token: 0x060008EF RID: 2287 RVA: 0x0002326C File Offset: 0x0002146C
			public void DebugDraw()
			{
				if (this._context.DrawDebugTreeNode(this._displayedName + "###Root." + this.ID))
				{
					if (this._context.IsDebugItemHovered())
					{
						this.DrawArea();
					}
					this._context.DrawCheckbox("Show Area###Area." + this.ID, ref this._isShowingArea);
					if (this._isShowingArea)
					{
						this.DrawArea();
					}
					this.DrawProperties();
					this.DrawChildren();
					this._context.PopDebugTreeNode();
					return;
				}
				if (this._context.IsDebugItemHovered())
				{
					this.DrawArea();
				}
			}

			// Token: 0x060008F0 RID: 2288 RVA: 0x0002330C File Offset: 0x0002150C
			private void DrawProperties()
			{
				if (this._context.DrawDebugTreeNode("Properties###Properties." + this.ID))
				{
					this._context.DrawDebugText("General");
					string str = (string.IsNullOrEmpty(this._current.Id) ? "_No ID_" : this._current.Id);
					this._context.DrawDebugText("\tID: " + str);
					this._context.DrawDebugText("\tPath: " + this._current.GetFullIDPath());
					this._context.DrawDebugText(string.Format("\tVisible: {0}", this._current.IsVisible));
					this._context.DrawDebugText(string.Format("\tEnabled: {0}", this._current.IsEnabled));
					this._context.DrawDebugText("\nSize");
					this._context.DrawDebugText(string.Format("\tWidth Size Policy: {0}", this._current.WidthSizePolicy));
					this._context.DrawDebugText(string.Format("\tHeight Size Policy: {0}", this._current.HeightSizePolicy));
					this._context.DrawDebugText(string.Format("\tSize: {0}", this._current.Size));
					this._context.DrawDebugText("\nPosition");
					this._context.DrawDebugText(string.Format("\tGlobal Position: {0}", this._current.GlobalPosition));
					this._context.DrawDebugText(string.Format("\tLocal Position: {0}", this._current.LocalPosition));
					this._context.DrawDebugText(string.Format("\tPosition Offset: <{0}, {1}>", this._current.PositionXOffset, this._current.PositionYOffset));
					this._context.DrawDebugText("\nEvents");
					this._context.DrawDebugText("\tCurrent State: " + this._current.CurrentState);
					this._context.DrawDebugText(string.Format("\tCan Accept Events: {0}", this._current.CanAcceptEvents));
					this._context.DrawDebugText(string.Format("\tPasses Events To Children: {0}", !this._current.DoNotPassEventsToChildren));
					this._context.DrawDebugText("\nVisuals");
					BrushWidget brushWidget = this._current as BrushWidget;
					if (brushWidget != null)
					{
						this._context.DrawDebugText("\tBrush: " + brushWidget.Brush.Name);
					}
					TextWidget textWidget;
					RichTextWidget richTextWidget;
					if ((textWidget = this._current as TextWidget) != null)
					{
						this._context.DrawDebugText("\tText: " + textWidget.Text);
					}
					else if ((richTextWidget = this._current as RichTextWidget) != null)
					{
						this._context.DrawDebugText("\tText: " + richTextWidget.Text);
					}
					else if (brushWidget != null)
					{
						TwoDimensionContext context = this._context;
						string str2 = "\tSprite: ";
						BrushRenderer brushRenderer = brushWidget.BrushRenderer;
						string text;
						if (brushRenderer == null)
						{
							text = null;
						}
						else
						{
							Style currentStyle = brushRenderer.CurrentStyle;
							if (currentStyle == null)
							{
								text = null;
							}
							else
							{
								StyleLayer layer = currentStyle.GetLayer(brushWidget.BrushRenderer.CurrentState);
								if (layer == null)
								{
									text = null;
								}
								else
								{
									Sprite sprite = layer.Sprite;
									text = ((sprite != null) ? sprite.Name : null);
								}
							}
						}
						context.DrawDebugText(str2 + (text ?? "None"));
						TwoDimensionContext context2 = this._context;
						string str3 = "\tColor: ";
						Brush brush = brushWidget.Brush;
						string str4;
						if (brush == null)
						{
							str4 = null;
						}
						else
						{
							BrushLayer layer2 = brush.GetLayer(brushWidget.CurrentState);
							str4 = ((layer2 != null) ? layer2.ToString() : null);
						}
						context2.DrawDebugText(str3 + str4);
					}
					else
					{
						TwoDimensionContext context3 = this._context;
						string str5 = "\tSprite: ";
						Sprite sprite2 = this._current.Sprite;
						context3.DrawDebugText(str5 + (((sprite2 != null) ? sprite2.Name : null) ?? "None"));
						this._context.DrawDebugText("\tColor: " + this._current.Color.ToString());
					}
					this._context.PopDebugTreeNode();
				}
			}

			// Token: 0x060008F1 RID: 2289 RVA: 0x00023728 File Offset: 0x00021928
			private void DrawChildren()
			{
				if (this._children.Count > 0 && this._context.DrawDebugTreeNode("Children###Children." + this.ID))
				{
					foreach (UIContext.DebugWidgetTreeNode debugWidgetTreeNode in this._children)
					{
						debugWidgetTreeNode.DebugDraw();
					}
					this._context.PopDebugTreeNode();
				}
			}

			// Token: 0x060008F2 RID: 2290 RVA: 0x000237B0 File Offset: 0x000219B0
			private void DrawArea()
			{
				float x = this._current.GlobalPosition.X;
				float y = this._current.GlobalPosition.Y;
				float num = this._current.GlobalPosition.X + this._current.Size.X;
				float num2 = this._current.GlobalPosition.Y + this._current.Size.Y;
				if (x == num || y == num2 || this._current.Size.X == 0f || this._current.Size.Y == 0f)
				{
					return;
				}
				float num3 = 2f;
				float num4 = num3 / 2f;
				float num5 = num3 / 2f;
				float num6 = num3 / 2f;
				float num7 = num3 / 2f;
				float num8 = num3 / 2f;
				float num9 = num3 / 2f;
				float num10 = num3 / 2f;
				float num11 = num3 / 2f;
			}

			// Token: 0x04000454 RID: 1108
			private readonly TwoDimensionContext _context;

			// Token: 0x04000455 RID: 1109
			private readonly Widget _current;

			// Token: 0x04000456 RID: 1110
			private readonly List<UIContext.DebugWidgetTreeNode> _children;

			// Token: 0x04000457 RID: 1111
			private readonly string _fullIDPath;

			// Token: 0x04000458 RID: 1112
			private readonly string _displayedName;

			// Token: 0x04000459 RID: 1113
			private readonly int _depth;

			// Token: 0x0400045A RID: 1114
			private bool _isShowingArea;
		}
	}
}

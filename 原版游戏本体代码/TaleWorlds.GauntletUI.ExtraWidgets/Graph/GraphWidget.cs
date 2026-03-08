using System;
using System.Collections.Generic;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.Layout;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.ExtraWidgets.Graph
{
	// Token: 0x0200001B RID: 27
	public class GraphWidget : Widget
	{
		// Token: 0x06000154 RID: 340 RVA: 0x0000765B File Offset: 0x0000585B
		public GraphWidget(UIContext context)
			: base(context)
		{
			this.RefreshOnNextLateUpdate();
		}

		// Token: 0x06000155 RID: 341 RVA: 0x0000766C File Offset: 0x0000586C
		protected override void OnLateUpdate(float dt)
		{
			base.OnLateUpdate(dt);
			bool flag = Mathf.Abs(this._totalSizeCached.X - base.Size.X) > 1E-05f || Mathf.Abs(this._totalSizeCached.Y - base.Size.Y) > 1E-05f;
			this._totalSizeCached = base.Size;
			if (flag)
			{
				this.RefreshOnNextLateUpdate();
			}
		}

		// Token: 0x06000156 RID: 342 RVA: 0x000076E4 File Offset: 0x000058E4
		private void Refresh()
		{
			if (this._dynamicWidgetsContainer != null)
			{
				base.RemoveChild(this._dynamicWidgetsContainer);
			}
			this._dynamicWidgetsContainer = new Widget(base.Context)
			{
				WidthSizePolicy = SizePolicy.StretchToParent,
				HeightSizePolicy = SizePolicy.StretchToParent
			};
			base.AddChildAtIndex(this._dynamicWidgetsContainer, 0);
			this._planeExtendedSize = base.Size * base._inverseScaleToUse - new Vec2(this.LeftSpace + this.RightSpace, this.TopSpace + this.BottomSpace);
			this._planeSize = this._planeExtendedSize - new Vec2(this.PlaneMarginRight, this.PlaneMarginTop);
			Widget widget = new Widget(base.Context)
			{
				WidthSizePolicy = SizePolicy.StretchToParent,
				HeightSizePolicy = SizePolicy.StretchToParent,
				MarginLeft = this.LeftSpace,
				MarginRight = this.RightSpace,
				MarginBottom = this.BottomSpace,
				MarginTop = this.TopSpace,
				DoNotAcceptEvents = true,
				DoNotPassEventsToChildren = true
			};
			this._dynamicWidgetsContainer.AddChild(widget);
			this.RefreshPlaneLines(widget);
			this.RefreshLabels(this._dynamicWidgetsContainer, true);
			this.RefreshLabels(this._dynamicWidgetsContainer, false);
			this.RefreshGraphLines();
			this._willRefreshThisFrame = false;
		}

		// Token: 0x06000157 RID: 343 RVA: 0x00007828 File Offset: 0x00005A28
		private void RefreshPlaneLines(Widget planeWidget)
		{
			int num = 1;
			ListPanel listPanel = this.CreatePlaneLinesListPanel(LayoutMethod.VerticalBottomToTop);
			float marginBottom = this._planeSize.Y / (float)this.RowCount - (float)num;
			for (int i = 0; i < this.RowCount; i++)
			{
				Widget widget = new Widget(base.Context)
				{
					WidthSizePolicy = SizePolicy.StretchToParent,
					HeightSizePolicy = SizePolicy.Fixed,
					SuggestedHeight = (float)num,
					MarginBottom = marginBottom,
					Sprite = this.PlaneLineSprite,
					Color = this.PlaneLineColor
				};
				listPanel.AddChild(widget);
			}
			ListPanel listPanel2 = this.CreatePlaneLinesListPanel(LayoutMethod.HorizontalLeftToRight);
			float marginLeft = this._planeSize.X / (float)this.ColumnCount - (float)num;
			for (int j = 0; j < this.ColumnCount; j++)
			{
				Widget widget2 = new Widget(base.Context)
				{
					WidthSizePolicy = SizePolicy.Fixed,
					HeightSizePolicy = SizePolicy.StretchToParent,
					SuggestedWidth = (float)num,
					MarginLeft = marginLeft,
					Sprite = this.PlaneLineSprite,
					Color = this.PlaneLineColor
				};
				listPanel2.AddChild(widget2);
			}
			planeWidget.AddChild(listPanel);
			planeWidget.AddChild(listPanel2);
		}

		// Token: 0x06000158 RID: 344 RVA: 0x00007944 File Offset: 0x00005B44
		private void RefreshLabels(Widget container, bool isHorizontal)
		{
			int num = (isHorizontal ? this.HorizontalLabelCount : this.VerticalLabelCount);
			float num2 = (isHorizontal ? this.HorizontalMaxValue : this.VerticalMaxValue);
			float num3 = (isHorizontal ? this.HorizontalMinValue : this.VerticalMinValue);
			if (num > 1)
			{
				int num4 = (isHorizontal ? 2 : 4);
				ListPanel listPanel = new ListPanel(base.Context)
				{
					WidthSizePolicy = (isHorizontal ? SizePolicy.StretchToParent : SizePolicy.Fixed),
					HeightSizePolicy = (isHorizontal ? SizePolicy.Fixed : SizePolicy.StretchToParent),
					SuggestedWidth = (isHorizontal ? 0f : (this.LeftSpace - (float)num4)),
					SuggestedHeight = (isHorizontal ? (this.BottomSpace - (float)num4) : 0f),
					HorizontalAlignment = HorizontalAlignment.Left,
					VerticalAlignment = VerticalAlignment.Bottom,
					MarginLeft = (isHorizontal ? this.LeftSpace : 0f),
					MarginBottom = (isHorizontal ? 0f : this.BottomSpace),
					DoNotAcceptEvents = true,
					DoNotPassEventsToChildren = true
				};
				listPanel.StackLayout.LayoutMethod = (isHorizontal ? LayoutMethod.HorizontalLeftToRight : LayoutMethod.VerticalTopToBottom);
				float num5 = (num2 - num3) / (float)(num - 1);
				for (int i = 0; i < num - 1; i++)
				{
					float labelValue = num3 + num5 * (float)i;
					TextWidget widget = this.CreateLabelText(labelValue, isHorizontal);
					listPanel.AddChild(widget);
				}
				Widget widget2 = new Widget(base.Context)
				{
					WidthSizePolicy = (isHorizontal ? SizePolicy.Fixed : SizePolicy.StretchToParent),
					HeightSizePolicy = (isHorizontal ? SizePolicy.StretchToParent : SizePolicy.Fixed),
					SuggestedWidth = (isHorizontal ? (this.RightSpace + this.PlaneMarginRight) : 0f),
					SuggestedHeight = (isHorizontal ? 0f : (this.TopSpace + this.PlaneMarginTop))
				};
				TextWidget widget3 = this.CreateLabelText(num2, isHorizontal);
				widget2.AddChild(widget3);
				listPanel.AddChild(widget2);
				container.AddChild(listPanel);
			}
		}

		// Token: 0x06000159 RID: 345 RVA: 0x00007B0C File Offset: 0x00005D0C
		private void RefreshGraphLines()
		{
			if (this.LineContainerWidget != null)
			{
				using (List<Widget>.Enumerator enumerator = this.LineContainerWidget.Children.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						GraphLineWidget graphLineWidget;
						if ((graphLineWidget = enumerator.Current as GraphLineWidget) != null)
						{
							this.RefreshLine(graphLineWidget);
						}
					}
				}
			}
		}

		// Token: 0x0600015A RID: 346 RVA: 0x00007B74 File Offset: 0x00005D74
		private void RefreshLine(GraphLineWidget graphLineWidget)
		{
			graphLineWidget.MarginLeft = this.LeftSpace;
			graphLineWidget.MarginRight = this.RightSpace + this.PlaneMarginRight;
			graphLineWidget.MarginBottom = this.BottomSpace;
			graphLineWidget.MarginTop = this.TopSpace + this.PlaneMarginTop;
			Widget pointContainerWidget = graphLineWidget.PointContainerWidget;
			using (List<Widget>.Enumerator enumerator = (((pointContainerWidget != null) ? pointContainerWidget.Children : null) ?? new List<Widget>()).GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					GraphLinePointWidget graphLinePointWidget;
					if ((graphLinePointWidget = enumerator.Current as GraphLinePointWidget) != null)
					{
						this.RefreshPoint(graphLinePointWidget, graphLineWidget);
					}
				}
			}
		}

		// Token: 0x0600015B RID: 347 RVA: 0x00007C24 File Offset: 0x00005E24
		private void RefreshPoint(GraphLinePointWidget graphLinePointWidget, GraphLineWidget graphLineWidget)
		{
			bool flag = this.HorizontalMaxValue - this.HorizontalMinValue > 1E-05f;
			bool flag2 = this.VerticalMaxValue - this.VerticalMinValue > 1E-05f;
			if (flag && flag2)
			{
				float num = (graphLinePointWidget.HorizontalValue - this.HorizontalMinValue) / (this.HorizontalMaxValue - this.HorizontalMinValue);
				num = MathF.Clamp(num, 0f, 1f);
				float marginLeft = this._planeSize.X * num - graphLinePointWidget.SuggestedWidth * 0.5f;
				float num2 = (graphLinePointWidget.VerticalValue - this.VerticalMinValue) / (this.VerticalMaxValue - this.VerticalMinValue);
				num2 = MathF.Clamp(num2, 0f, 1f);
				float marginBottom = this._planeSize.Y * num2 - graphLinePointWidget.SuggestedHeight * 0.5f;
				string state = (string.IsNullOrEmpty(graphLineWidget.LineBrushStateName) ? "Default" : graphLineWidget.LineBrushStateName);
				graphLinePointWidget.MarginLeft = marginLeft;
				graphLinePointWidget.MarginBottom = marginBottom;
				graphLinePointWidget.SetState(state);
			}
		}

		// Token: 0x0600015C RID: 348 RVA: 0x00007D27 File Offset: 0x00005F27
		private ListPanel CreatePlaneLinesListPanel(LayoutMethod layoutMethod)
		{
			return new ListPanel(base.Context)
			{
				WidthSizePolicy = SizePolicy.StretchToParent,
				HeightSizePolicy = SizePolicy.StretchToParent,
				MarginTop = this.PlaneMarginTop,
				MarginRight = this.PlaneMarginRight,
				StackLayout = 
				{
					LayoutMethod = layoutMethod
				}
			};
		}

		// Token: 0x0600015D RID: 349 RVA: 0x00007D68 File Offset: 0x00005F68
		private TextWidget CreateLabelText(float labelValue, bool isHorizontal)
		{
			TextWidget textWidget = new TextWidget(base.Context)
			{
				WidthSizePolicy = SizePolicy.StretchToParent,
				HeightSizePolicy = SizePolicy.StretchToParent,
				Text = labelValue.ToString("G" + this.NumberOfValueLabelDecimalPlaces.ToString())
			};
			Brush brush = (isHorizontal ? this.HorizontalValueLabelsBrush : this.VerticalValueLabelsBrush);
			if (brush != null)
			{
				textWidget.Brush = brush.Clone();
			}
			textWidget.Brush.TextHorizontalAlignment = (isHorizontal ? TextHorizontalAlignment.Left : TextHorizontalAlignment.Right);
			textWidget.Brush.TextVerticalAlignment = (isHorizontal ? TextVerticalAlignment.Top : TextVerticalAlignment.Bottom);
			return textWidget;
		}

		// Token: 0x0600015E RID: 350 RVA: 0x00007DFC File Offset: 0x00005FFC
		private void OnLineContainerEventFire(Widget widget, string eventName, object[] eventArgs)
		{
			GraphLineWidget graphLineWidget;
			if (eventArgs.Length != 0 && (graphLineWidget = eventArgs[0] as GraphLineWidget) != null)
			{
				if (eventName == "ItemAdd")
				{
					GraphLineWidget graphLineWidget3 = graphLineWidget;
					graphLineWidget3.OnPointAdded = (Action<GraphLineWidget, GraphLinePointWidget>)Delegate.Combine(graphLineWidget3.OnPointAdded, new Action<GraphLineWidget, GraphLinePointWidget>(this.OnPointAdded));
					this.AddLateUpdateAction(delegate
					{
						this.RefreshLine(graphLineWidget);
					});
					return;
				}
				if (eventName == "ItemRemove")
				{
					GraphLineWidget graphLineWidget2 = graphLineWidget;
					graphLineWidget2.OnPointAdded = (Action<GraphLineWidget, GraphLinePointWidget>)Delegate.Remove(graphLineWidget2.OnPointAdded, new Action<GraphLineWidget, GraphLinePointWidget>(this.OnPointAdded));
				}
			}
		}

		// Token: 0x0600015F RID: 351 RVA: 0x00007EAC File Offset: 0x000060AC
		private void OnPointAdded(GraphLineWidget graphLineWidget, GraphLinePointWidget graphLinePointWidget)
		{
			this.AddLateUpdateAction(delegate
			{
				this.RefreshPoint(graphLinePointWidget, graphLineWidget);
			});
		}

		// Token: 0x06000160 RID: 352 RVA: 0x00007EE8 File Offset: 0x000060E8
		private void AddLateUpdateAction(Action action)
		{
			base.EventManager.AddLateUpdateAction(this, delegate(float _)
			{
				Action action2 = action;
				if (action2 == null)
				{
					return;
				}
				action2();
			}, 1);
		}

		// Token: 0x06000161 RID: 353 RVA: 0x00007F1B File Offset: 0x0000611B
		private void RefreshOnNextLateUpdate()
		{
			if (!this._willRefreshThisFrame)
			{
				this._willRefreshThisFrame = true;
				this.AddLateUpdateAction(new Action(this.Refresh));
			}
		}

		// Token: 0x1700007F RID: 127
		// (get) Token: 0x06000162 RID: 354 RVA: 0x00007F3E File Offset: 0x0000613E
		// (set) Token: 0x06000163 RID: 355 RVA: 0x00007F46 File Offset: 0x00006146
		public int RowCount
		{
			get
			{
				return this._rowCount;
			}
			set
			{
				if (value != this._rowCount)
				{
					this._rowCount = value;
					base.OnPropertyChanged(value, "RowCount");
					this.RefreshOnNextLateUpdate();
				}
			}
		}

		// Token: 0x17000080 RID: 128
		// (get) Token: 0x06000164 RID: 356 RVA: 0x00007F6A File Offset: 0x0000616A
		// (set) Token: 0x06000165 RID: 357 RVA: 0x00007F72 File Offset: 0x00006172
		public int ColumnCount
		{
			get
			{
				return this._columnCount;
			}
			set
			{
				if (value != this._columnCount)
				{
					this._columnCount = value;
					base.OnPropertyChanged(value, "ColumnCount");
					this.RefreshOnNextLateUpdate();
				}
			}
		}

		// Token: 0x17000081 RID: 129
		// (get) Token: 0x06000166 RID: 358 RVA: 0x00007F96 File Offset: 0x00006196
		// (set) Token: 0x06000167 RID: 359 RVA: 0x00007F9E File Offset: 0x0000619E
		public int HorizontalLabelCount
		{
			get
			{
				return this._horizontalLabelCount;
			}
			set
			{
				if (value != this._horizontalLabelCount)
				{
					this._horizontalLabelCount = value;
					base.OnPropertyChanged(value, "HorizontalLabelCount");
					this.RefreshOnNextLateUpdate();
				}
			}
		}

		// Token: 0x17000082 RID: 130
		// (get) Token: 0x06000168 RID: 360 RVA: 0x00007FC2 File Offset: 0x000061C2
		// (set) Token: 0x06000169 RID: 361 RVA: 0x00007FCA File Offset: 0x000061CA
		public float HorizontalMinValue
		{
			get
			{
				return this._horizontalMinValue;
			}
			set
			{
				if (value != this._horizontalMinValue)
				{
					this._horizontalMinValue = value;
					base.OnPropertyChanged(value, "HorizontalMinValue");
					this.RefreshOnNextLateUpdate();
				}
			}
		}

		// Token: 0x17000083 RID: 131
		// (get) Token: 0x0600016A RID: 362 RVA: 0x00007FEE File Offset: 0x000061EE
		// (set) Token: 0x0600016B RID: 363 RVA: 0x00007FF6 File Offset: 0x000061F6
		public float HorizontalMaxValue
		{
			get
			{
				return this._horizontalMaxValue;
			}
			set
			{
				if (value != this._horizontalMaxValue)
				{
					this._horizontalMaxValue = value;
					base.OnPropertyChanged(value, "HorizontalMaxValue");
					this.RefreshOnNextLateUpdate();
				}
			}
		}

		// Token: 0x17000084 RID: 132
		// (get) Token: 0x0600016C RID: 364 RVA: 0x0000801A File Offset: 0x0000621A
		// (set) Token: 0x0600016D RID: 365 RVA: 0x00008022 File Offset: 0x00006222
		public int VerticalLabelCount
		{
			get
			{
				return this._verticalLabelCount;
			}
			set
			{
				if (value != this._verticalLabelCount)
				{
					this._verticalLabelCount = value;
					base.OnPropertyChanged(value, "VerticalLabelCount");
					this.RefreshOnNextLateUpdate();
				}
			}
		}

		// Token: 0x17000085 RID: 133
		// (get) Token: 0x0600016E RID: 366 RVA: 0x00008046 File Offset: 0x00006246
		// (set) Token: 0x0600016F RID: 367 RVA: 0x0000804E File Offset: 0x0000624E
		public float VerticalMinValue
		{
			get
			{
				return this._verticalMinValue;
			}
			set
			{
				if (value != this._verticalMinValue)
				{
					this._verticalMinValue = value;
					base.OnPropertyChanged(value, "VerticalMinValue");
					this.RefreshOnNextLateUpdate();
				}
			}
		}

		// Token: 0x17000086 RID: 134
		// (get) Token: 0x06000170 RID: 368 RVA: 0x00008072 File Offset: 0x00006272
		// (set) Token: 0x06000171 RID: 369 RVA: 0x0000807A File Offset: 0x0000627A
		public float VerticalMaxValue
		{
			get
			{
				return this._verticalMaxValue;
			}
			set
			{
				if (value != this._verticalMaxValue)
				{
					this._verticalMaxValue = value;
					base.OnPropertyChanged(value, "VerticalMaxValue");
					this.RefreshOnNextLateUpdate();
				}
			}
		}

		// Token: 0x17000087 RID: 135
		// (get) Token: 0x06000172 RID: 370 RVA: 0x0000809E File Offset: 0x0000629E
		// (set) Token: 0x06000173 RID: 371 RVA: 0x000080A6 File Offset: 0x000062A6
		public Sprite PlaneLineSprite
		{
			get
			{
				return this._planeLineSprite;
			}
			set
			{
				if (value != this._planeLineSprite)
				{
					this._planeLineSprite = value;
					base.OnPropertyChanged<Sprite>(value, "PlaneLineSprite");
					this.RefreshOnNextLateUpdate();
				}
			}
		}

		// Token: 0x17000088 RID: 136
		// (get) Token: 0x06000174 RID: 372 RVA: 0x000080CA File Offset: 0x000062CA
		// (set) Token: 0x06000175 RID: 373 RVA: 0x000080D2 File Offset: 0x000062D2
		public Color PlaneLineColor
		{
			get
			{
				return this._planeLineColor;
			}
			set
			{
				if (value != this._planeLineColor)
				{
					this._planeLineColor = value;
					base.OnPropertyChanged(value, "PlaneLineColor");
					this.RefreshOnNextLateUpdate();
				}
			}
		}

		// Token: 0x17000089 RID: 137
		// (get) Token: 0x06000176 RID: 374 RVA: 0x000080FB File Offset: 0x000062FB
		// (set) Token: 0x06000177 RID: 375 RVA: 0x00008103 File Offset: 0x00006303
		public float LeftSpace
		{
			get
			{
				return this._leftSpace;
			}
			set
			{
				if (value != this._leftSpace)
				{
					this._leftSpace = value;
					base.OnPropertyChanged(value, "LeftSpace");
					this.RefreshOnNextLateUpdate();
				}
			}
		}

		// Token: 0x1700008A RID: 138
		// (get) Token: 0x06000178 RID: 376 RVA: 0x00008127 File Offset: 0x00006327
		// (set) Token: 0x06000179 RID: 377 RVA: 0x0000812F File Offset: 0x0000632F
		public float TopSpace
		{
			get
			{
				return this._topSpace;
			}
			set
			{
				if (value != this._topSpace)
				{
					this._topSpace = value;
					base.OnPropertyChanged(value, "TopSpace");
					this.RefreshOnNextLateUpdate();
				}
			}
		}

		// Token: 0x1700008B RID: 139
		// (get) Token: 0x0600017A RID: 378 RVA: 0x00008153 File Offset: 0x00006353
		// (set) Token: 0x0600017B RID: 379 RVA: 0x0000815B File Offset: 0x0000635B
		public float RightSpace
		{
			get
			{
				return this._rightSpace;
			}
			set
			{
				if (value != this._rightSpace)
				{
					this._rightSpace = value;
					base.OnPropertyChanged(value, "RightSpace");
					this.RefreshOnNextLateUpdate();
				}
			}
		}

		// Token: 0x1700008C RID: 140
		// (get) Token: 0x0600017C RID: 380 RVA: 0x0000817F File Offset: 0x0000637F
		// (set) Token: 0x0600017D RID: 381 RVA: 0x00008187 File Offset: 0x00006387
		public float BottomSpace
		{
			get
			{
				return this._bottomSpace;
			}
			set
			{
				if (value != this._bottomSpace)
				{
					this._bottomSpace = value;
					base.OnPropertyChanged(value, "BottomSpace");
					this.RefreshOnNextLateUpdate();
				}
			}
		}

		// Token: 0x1700008D RID: 141
		// (get) Token: 0x0600017E RID: 382 RVA: 0x000081AB File Offset: 0x000063AB
		// (set) Token: 0x0600017F RID: 383 RVA: 0x000081B3 File Offset: 0x000063B3
		public float PlaneMarginTop
		{
			get
			{
				return this._planeMarginTop;
			}
			set
			{
				if (value != this._planeMarginTop)
				{
					this._planeMarginTop = value;
					base.OnPropertyChanged(value, "PlaneMarginTop");
					this.RefreshOnNextLateUpdate();
				}
			}
		}

		// Token: 0x1700008E RID: 142
		// (get) Token: 0x06000180 RID: 384 RVA: 0x000081D7 File Offset: 0x000063D7
		// (set) Token: 0x06000181 RID: 385 RVA: 0x000081DF File Offset: 0x000063DF
		public float PlaneMarginRight
		{
			get
			{
				return this._planeMarginRight;
			}
			set
			{
				if (value != this._planeMarginRight)
				{
					this._planeMarginRight = value;
					base.OnPropertyChanged(value, "PlaneMarginRight");
					this.RefreshOnNextLateUpdate();
				}
			}
		}

		// Token: 0x1700008F RID: 143
		// (get) Token: 0x06000182 RID: 386 RVA: 0x00008203 File Offset: 0x00006403
		// (set) Token: 0x06000183 RID: 387 RVA: 0x0000820B File Offset: 0x0000640B
		public int NumberOfValueLabelDecimalPlaces
		{
			get
			{
				return this._numberOfValueLabelDecimalPlaces;
			}
			set
			{
				if (value != this._numberOfValueLabelDecimalPlaces)
				{
					this._numberOfValueLabelDecimalPlaces = value;
					base.OnPropertyChanged(value, "NumberOfValueLabelDecimalPlaces");
					this.RefreshOnNextLateUpdate();
				}
			}
		}

		// Token: 0x17000090 RID: 144
		// (get) Token: 0x06000184 RID: 388 RVA: 0x0000822F File Offset: 0x0000642F
		// (set) Token: 0x06000185 RID: 389 RVA: 0x00008237 File Offset: 0x00006437
		public Brush HorizontalValueLabelsBrush
		{
			get
			{
				return this._horizontalValueLabelsBrush;
			}
			set
			{
				if (value != this._horizontalValueLabelsBrush)
				{
					this._horizontalValueLabelsBrush = value;
					base.OnPropertyChanged<Brush>(value, "HorizontalValueLabelsBrush");
					this.RefreshOnNextLateUpdate();
				}
			}
		}

		// Token: 0x17000091 RID: 145
		// (get) Token: 0x06000186 RID: 390 RVA: 0x0000825B File Offset: 0x0000645B
		// (set) Token: 0x06000187 RID: 391 RVA: 0x00008263 File Offset: 0x00006463
		public Brush VerticalValueLabelsBrush
		{
			get
			{
				return this._verticalValueLabelsBrush;
			}
			set
			{
				if (value != this._verticalValueLabelsBrush)
				{
					this._verticalValueLabelsBrush = value;
					base.OnPropertyChanged<Brush>(value, "VerticalValueLabelsBrush");
					this.RefreshOnNextLateUpdate();
				}
			}
		}

		// Token: 0x17000092 RID: 146
		// (get) Token: 0x06000188 RID: 392 RVA: 0x00008287 File Offset: 0x00006487
		// (set) Token: 0x06000189 RID: 393 RVA: 0x0000828F File Offset: 0x0000648F
		public Brush LineBrush
		{
			get
			{
				return this._lineBrush;
			}
			set
			{
				if (value != this._lineBrush)
				{
					this._lineBrush = value;
					base.OnPropertyChanged<Brush>(value, "LineBrush");
					this.RefreshOnNextLateUpdate();
				}
			}
		}

		// Token: 0x17000093 RID: 147
		// (get) Token: 0x0600018A RID: 394 RVA: 0x000082B3 File Offset: 0x000064B3
		// (set) Token: 0x0600018B RID: 395 RVA: 0x000082BC File Offset: 0x000064BC
		public Widget LineContainerWidget
		{
			get
			{
				return this._lineContainerWidget;
			}
			set
			{
				if (value != this._lineContainerWidget)
				{
					if (this._lineContainerWidget != null)
					{
						this._lineContainerWidget.EventFire -= this.OnLineContainerEventFire;
						using (List<Widget>.Enumerator enumerator = this.LineContainerWidget.Children.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								GraphLineWidget graphLineWidget;
								if ((graphLineWidget = enumerator.Current as GraphLineWidget) != null)
								{
									GraphLineWidget graphLineWidget2 = graphLineWidget;
									graphLineWidget2.OnPointAdded = (Action<GraphLineWidget, GraphLinePointWidget>)Delegate.Remove(graphLineWidget2.OnPointAdded, new Action<GraphLineWidget, GraphLinePointWidget>(this.OnPointAdded));
								}
							}
						}
					}
					this._lineContainerWidget = value;
					if (this._lineContainerWidget != null)
					{
						this._lineContainerWidget.EventFire += this.OnLineContainerEventFire;
						using (List<Widget>.Enumerator enumerator = this.LineContainerWidget.Children.GetEnumerator())
						{
							while (enumerator.MoveNext())
							{
								GraphLineWidget graphLineWidget3;
								if ((graphLineWidget3 = enumerator.Current as GraphLineWidget) != null)
								{
									GraphLineWidget graphLineWidget4 = graphLineWidget3;
									graphLineWidget4.OnPointAdded = (Action<GraphLineWidget, GraphLinePointWidget>)Delegate.Combine(graphLineWidget4.OnPointAdded, new Action<GraphLineWidget, GraphLinePointWidget>(this.OnPointAdded));
								}
							}
						}
					}
					base.OnPropertyChanged<Widget>(value, "LineContainerWidget");
					this.RefreshOnNextLateUpdate();
				}
			}
		}

		// Token: 0x040000A3 RID: 163
		private Widget _dynamicWidgetsContainer;

		// Token: 0x040000A4 RID: 164
		private bool _willRefreshThisFrame;

		// Token: 0x040000A5 RID: 165
		private Vec2 _planeExtendedSize;

		// Token: 0x040000A6 RID: 166
		private Vec2 _planeSize;

		// Token: 0x040000A7 RID: 167
		private Vec2 _totalSizeCached;

		// Token: 0x040000A8 RID: 168
		private Widget _lineContainerWidget;

		// Token: 0x040000A9 RID: 169
		private int _rowCount;

		// Token: 0x040000AA RID: 170
		private int _columnCount;

		// Token: 0x040000AB RID: 171
		private int _horizontalLabelCount;

		// Token: 0x040000AC RID: 172
		private float _horizontalMinValue;

		// Token: 0x040000AD RID: 173
		private float _horizontalMaxValue;

		// Token: 0x040000AE RID: 174
		private int _verticalLabelCount;

		// Token: 0x040000AF RID: 175
		private float _verticalMinValue;

		// Token: 0x040000B0 RID: 176
		private float _verticalMaxValue;

		// Token: 0x040000B1 RID: 177
		private Sprite _planeLineSprite;

		// Token: 0x040000B2 RID: 178
		private Color _planeLineColor;

		// Token: 0x040000B3 RID: 179
		private float _leftSpace;

		// Token: 0x040000B4 RID: 180
		private float _topSpace;

		// Token: 0x040000B5 RID: 181
		private float _rightSpace;

		// Token: 0x040000B6 RID: 182
		private float _bottomSpace;

		// Token: 0x040000B7 RID: 183
		private float _planeMarginTop;

		// Token: 0x040000B8 RID: 184
		private float _planeMarginRight;

		// Token: 0x040000B9 RID: 185
		private int _numberOfValueLabelDecimalPlaces;

		// Token: 0x040000BA RID: 186
		private Brush _horizontalValueLabelsBrush;

		// Token: 0x040000BB RID: 187
		private Brush _verticalValueLabelsBrush;

		// Token: 0x040000BC RID: 188
		private Brush _lineBrush;
	}
}

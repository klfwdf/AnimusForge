using System;
using System.Collections.Generic;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.Layout
{
	// Token: 0x02000047 RID: 71
	public class StackLayout : ILayout
	{
		// Token: 0x17000140 RID: 320
		// (get) Token: 0x0600043B RID: 1083 RVA: 0x00010CB1 File Offset: 0x0000EEB1
		// (set) Token: 0x0600043C RID: 1084 RVA: 0x00010CB9 File Offset: 0x0000EEB9
		public ContainerItemDescription DefaultItemDescription { get; private set; }

		// Token: 0x17000141 RID: 321
		// (get) Token: 0x0600043D RID: 1085 RVA: 0x00010CC2 File Offset: 0x0000EEC2
		// (set) Token: 0x0600043E RID: 1086 RVA: 0x00010CCA File Offset: 0x0000EECA
		public LayoutMethod LayoutMethod { get; set; }

		// Token: 0x0600043F RID: 1087 RVA: 0x00010CD3 File Offset: 0x0000EED3
		public StackLayout()
		{
			this.DefaultItemDescription = new ContainerItemDescription();
			this._layoutBoxes = new Dictionary<int, LayoutBox>(64);
			this._parallelMeasureBasicChildDelegate = new TWParallel.ParallelForAuxPredicate(this.ParallelMeasureBasicChild);
		}

		// Token: 0x06000440 RID: 1088 RVA: 0x00010D08 File Offset: 0x0000EF08
		public ContainerItemDescription GetItemDescription(Widget owner, Widget child, int childIndex)
		{
			Container container;
			if ((container = owner as Container) != null)
			{
				return container.GetItemDescription(child.Id, childIndex);
			}
			return this.DefaultItemDescription;
		}

		// Token: 0x06000441 RID: 1089 RVA: 0x00010D34 File Offset: 0x0000EF34
		public Vector2 MeasureChildren(Widget widget, Vector2 measureSpec, SpriteData spriteData, float renderScale)
		{
			Container container = widget as Container;
			Vector2 result = default(Vector2);
			if (widget.ChildCount > 0)
			{
				if (this.LayoutMethod == LayoutMethod.HorizontalLeftToRight || this.LayoutMethod == LayoutMethod.HorizontalRightToLeft || this.LayoutMethod == LayoutMethod.HorizontalCentered || this.LayoutMethod == LayoutMethod.HorizontalSpaced)
				{
					result = this.MeasureLinear(widget, measureSpec, AlignmentAxis.Horizontal);
					if (container != null && container.IsDragHovering)
					{
						result.X += 20f;
					}
				}
				else if (this.LayoutMethod == LayoutMethod.VerticalBottomToTop || this.LayoutMethod == LayoutMethod.VerticalTopToBottom || this.LayoutMethod == LayoutMethod.VerticalCentered)
				{
					result = this.MeasureLinear(widget, measureSpec, AlignmentAxis.Vertical);
					if (container != null && container.IsDragHovering)
					{
						result.Y += 20f;
					}
				}
			}
			return result;
		}

		// Token: 0x06000442 RID: 1090 RVA: 0x00010DE8 File Offset: 0x0000EFE8
		public void OnLayout(Widget widget, float left, float bottom, float right, float top)
		{
			if (this.LayoutMethod == LayoutMethod.HorizontalLeftToRight || this.LayoutMethod == LayoutMethod.HorizontalRightToLeft || this.LayoutMethod == LayoutMethod.HorizontalCentered || this.LayoutMethod == LayoutMethod.HorizontalSpaced)
			{
				this.LayoutLinearHorizontalLocal(widget, left, bottom, right, top);
				return;
			}
			if (this.LayoutMethod == LayoutMethod.VerticalBottomToTop || this.LayoutMethod == LayoutMethod.VerticalTopToBottom || this.LayoutMethod == LayoutMethod.VerticalCentered)
			{
				this.LayoutLinearVertical(widget, left, bottom, right, top);
			}
		}

		// Token: 0x06000443 RID: 1091 RVA: 0x00010E4E File Offset: 0x0000F04E
		private static float GetData(Vector2 vector2, int row)
		{
			if (row == 0)
			{
				return vector2.X;
			}
			return vector2.Y;
		}

		// Token: 0x06000444 RID: 1092 RVA: 0x00010E60 File Offset: 0x0000F060
		private static void SetData(ref Vector2 vector2, int row, float data)
		{
			if (row == 0)
			{
				vector2.X = data;
			}
			vector2.Y = data;
		}

		// Token: 0x06000445 RID: 1093 RVA: 0x00010E74 File Offset: 0x0000F074
		public int GetIndexForDrop(Container widget, Vector2 draggedWidgetPosition)
		{
			int row = 0;
			if (this.LayoutMethod == LayoutMethod.VerticalBottomToTop || this.LayoutMethod == LayoutMethod.VerticalTopToBottom || this.LayoutMethod == LayoutMethod.VerticalCentered)
			{
				row = 1;
			}
			bool flag = this.LayoutMethod == LayoutMethod.HorizontalRightToLeft || this.LayoutMethod == LayoutMethod.VerticalTopToBottom || this.LayoutMethod == LayoutMethod.VerticalCentered;
			float data = StackLayout.GetData(draggedWidgetPosition, row);
			int result = 0;
			bool flag2 = false;
			int num = 0;
			while (num != widget.ChildCount && !flag2)
			{
				Widget child = widget.GetChild(num);
				if (child != null)
				{
					float data2 = StackLayout.GetData(child.GlobalPosition * child.Context.CustomScale, row);
					float num2 = data2 + StackLayout.GetData(child.Size, row);
					float num3 = (data2 + num2) / 2f;
					if (!flag)
					{
						if (data < num3)
						{
							result = num;
							flag2 = true;
						}
					}
					else if (data > num3)
					{
						result = num;
						flag2 = true;
					}
				}
				num++;
			}
			if (!flag2)
			{
				result = widget.ChildCount;
			}
			return result;
		}

		// Token: 0x06000446 RID: 1094 RVA: 0x00010F54 File Offset: 0x0000F154
		private void ParallelMeasureBasicChild(int startInclusive, int endExclusive)
		{
			for (int i = startInclusive; i < endExclusive; i++)
			{
				Widget child = this._parallelMeasureBasicChildWidget.GetChild(i);
				if (child == null)
				{
					Debug.FailedAssert("Trying to measure a null child for parent" + this._parallelMeasureBasicChildWidget.GetFullIDPath(), "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Layout\\StackLayout.cs", "ParallelMeasureBasicChild", 184);
				}
				else if (child.IsVisible)
				{
					AlignmentAxis parallelMeasureBasicChildAlignmentAxis = this._parallelMeasureBasicChildAlignmentAxis;
					if (parallelMeasureBasicChildAlignmentAxis != AlignmentAxis.Horizontal)
					{
						if (parallelMeasureBasicChildAlignmentAxis == AlignmentAxis.Vertical)
						{
							if (child.HeightSizePolicy != SizePolicy.StretchToParent)
							{
								child.Measure(this._parallelMeasureBasicChildMeasureSpec);
							}
						}
					}
					else if (child.WidthSizePolicy != SizePolicy.StretchToParent)
					{
						child.Measure(this._parallelMeasureBasicChildMeasureSpec);
					}
				}
			}
		}

		// Token: 0x06000447 RID: 1095 RVA: 0x00010FF4 File Offset: 0x0000F1F4
		private Vector2 MeasureLinear(Widget widget, Vector2 measureSpec, AlignmentAxis alignmentAxis)
		{
			this._parallelMeasureBasicChildWidget = widget;
			this._parallelMeasureBasicChildMeasureSpec = measureSpec;
			this._parallelMeasureBasicChildAlignmentAxis = alignmentAxis;
			TWParallel.For(0, widget.ChildCount, this._parallelMeasureBasicChildDelegate, 64);
			this._parallelMeasureBasicChildWidget = null;
			float num = 0f;
			float num2 = 0f;
			float num3 = 0f;
			int num4 = 0;
			for (int i = 0; i < widget.ChildCount; i++)
			{
				Widget child = widget.GetChild(i);
				if (child == null)
				{
					Debug.FailedAssert("Trying to measure a null child for parent" + widget.GetFullIDPath(), "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Layout\\StackLayout.cs", "MeasureLinear", 234);
				}
				else if (child.IsVisible)
				{
					ContainerItemDescription itemDescription = this.GetItemDescription(widget, child, i);
					if (alignmentAxis == AlignmentAxis.Horizontal)
					{
						if (child.WidthSizePolicy == SizePolicy.StretchToParent)
						{
							num4++;
							num3 += itemDescription.WidthStretchRatio;
						}
						else
						{
							num2 += child.MeasuredSize.X + child.ScaledMarginLeft + child.ScaledMarginRight;
						}
						num = MathF.Max(num, child.MeasuredSize.Y + child.ScaledMarginTop + child.ScaledMarginBottom);
					}
					else if (alignmentAxis == AlignmentAxis.Vertical)
					{
						if (child.HeightSizePolicy == SizePolicy.StretchToParent)
						{
							num4++;
							num3 += itemDescription.HeightStretchRatio;
						}
						else
						{
							num += child.MeasuredSize.Y + child.ScaledMarginTop + child.ScaledMarginBottom;
						}
						num2 = MathF.Max(num2, child.MeasuredSize.X + child.ScaledMarginLeft + child.ScaledMarginRight);
					}
				}
			}
			if (num4 > 0)
			{
				float num5 = 0f;
				if (alignmentAxis == AlignmentAxis.Horizontal)
				{
					num5 = measureSpec.X - num2;
				}
				else if (alignmentAxis == AlignmentAxis.Vertical)
				{
					num5 = measureSpec.Y - num;
				}
				float num6 = num5;
				int num7 = num4;
				for (int j = 0; j < widget.ChildCount; j++)
				{
					Widget child2 = widget.GetChild(j);
					if (child2 == null)
					{
						Debug.FailedAssert("Trying to measure a null child for parent" + widget.GetFullIDPath(), "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Layout\\StackLayout.cs", "MeasureLinear", 296);
					}
					else if (child2.IsVisible && ((alignmentAxis == AlignmentAxis.Horizontal && child2.WidthSizePolicy == SizePolicy.StretchToParent) || (alignmentAxis == AlignmentAxis.Vertical && child2.HeightSizePolicy == SizePolicy.StretchToParent)))
					{
						ContainerItemDescription itemDescription2 = this.GetItemDescription(widget, child2, j);
						Vector2 measureSpec2;
						measureSpec2..ctor(0f, 0f);
						if (num6 <= 0f)
						{
							if (alignmentAxis == AlignmentAxis.Horizontal)
							{
								measureSpec2..ctor(0f, measureSpec.Y);
							}
							else if (alignmentAxis == AlignmentAxis.Vertical)
							{
								measureSpec2..ctor(measureSpec.X, 0f);
							}
						}
						else if (alignmentAxis == AlignmentAxis.Horizontal)
						{
							float num8 = num5 * itemDescription2.WidthStretchRatio / num3;
							if (num7 == 1)
							{
								num8 = num6;
							}
							measureSpec2..ctor(num8, measureSpec.Y);
						}
						else if (alignmentAxis == AlignmentAxis.Vertical)
						{
							float num9 = num5 * itemDescription2.HeightStretchRatio / num3;
							if (num7 == 1)
							{
								num9 = num6;
							}
							measureSpec2..ctor(measureSpec.X, num9);
						}
						child2.Measure(measureSpec2);
						num7--;
						Vector2 measuredSize = child2.MeasuredSize;
						measuredSize.X += child2.ScaledMarginLeft + child2.ScaledMarginRight;
						measuredSize.Y += child2.ScaledMarginTop + child2.ScaledMarginBottom;
						if (alignmentAxis == AlignmentAxis.Horizontal)
						{
							num6 -= measuredSize.X;
							num2 += measuredSize.X;
							num = MathF.Max(num, measuredSize.Y);
						}
						else if (alignmentAxis == AlignmentAxis.Vertical)
						{
							num6 -= measuredSize.Y;
							num += measuredSize.Y;
							num2 = MathF.Max(num2, measuredSize.X);
						}
					}
				}
			}
			float num10 = num2;
			float num11 = num;
			return new Vector2(num10, num11);
		}

		// Token: 0x06000448 RID: 1096 RVA: 0x00011378 File Offset: 0x0000F578
		private void ParallelUpdateLayouts(Widget widget)
		{
			StackLayout.<>c__DisplayClass23_0 CS$<>8__locals1 = new StackLayout.<>c__DisplayClass23_0();
			CS$<>8__locals1.widget = widget;
			CS$<>8__locals1.<>4__this = this;
			TWParallel.For(0, CS$<>8__locals1.widget.ChildCount, new TWParallel.ParallelForAuxPredicate(CS$<>8__locals1.<ParallelUpdateLayouts>g__UpdateChildLayoutMT|0), 16);
		}

		// Token: 0x06000449 RID: 1097 RVA: 0x000113B8 File Offset: 0x0000F5B8
		private void LayoutLinearHorizontalLocal(Widget widget, float left, float bottom, float right, float top)
		{
			Container container = widget as Container;
			float num = 0f;
			float top2 = 0f;
			float num2 = right - left;
			float bottom2 = bottom - top;
			if (this.LayoutMethod != LayoutMethod.HorizontalRightToLeft && this.LayoutMethod == LayoutMethod.HorizontalCentered)
			{
				float num3 = 0f;
				for (int i = 0; i < widget.ChildCount; i++)
				{
					Widget child = widget.GetChild(i);
					if (child == null)
					{
						Debug.FailedAssert("Trying to measure a null child for parent" + widget.GetFullIDPath(), "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Layout\\StackLayout.cs", "LayoutLinearHorizontalLocal", 422);
					}
					else if (child.IsVisible)
					{
						num3 += child.MeasuredSize.X + child.ScaledMarginLeft + child.ScaledMarginRight;
					}
				}
				num = (right - left) / 2f - num3 / 2f;
			}
			this._layoutBoxes.Clear();
			int num4 = 0;
			for (int j = 0; j < widget.ChildCount; j++)
			{
				if (widget.Children[j].IsVisible)
				{
					num4++;
				}
			}
			if (num4 > 0)
			{
				for (int k = 0; k < widget.ChildCount; k++)
				{
					Widget widget2 = widget.Children[k];
					if (widget2 == null)
					{
						Debug.FailedAssert("Trying to measure a null child for parent" + widget.GetFullIDPath(), "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\GauntletUI\\TaleWorlds.GauntletUI\\Layout\\StackLayout.cs", "LayoutLinearHorizontalLocal", 453);
					}
					else if (widget2.IsVisible)
					{
						float num5 = widget2.MeasuredSize.X + widget2.ScaledMarginLeft + widget2.ScaledMarginRight;
						if (container != null && container.IsDragHovering && k == container.DragHoverInsertionIndex)
						{
							num5 += 20f;
						}
						if (this.LayoutMethod == LayoutMethod.HorizontalRightToLeft)
						{
							num = num2 - num5;
						}
						else if (this.LayoutMethod == LayoutMethod.HorizontalSpaced)
						{
							if (num4 > 1)
							{
								if (k == 0)
								{
									num = 0f;
									num2 = left + widget2.MeasuredSize.X;
								}
								else if (k == widget.ChildCount - 1)
								{
									num2 = right - left;
									num = num2 - widget2.MeasuredSize.X;
								}
								else
								{
									float num6 = (widget.MeasuredSize.X - widget2.MeasuredSize.X * (float)num4) / (float)(num4 - 1);
									num += widget2.MeasuredSize.X + num6;
									num2 = num + widget2.MeasuredSize.X;
								}
							}
							else
							{
								num = widget.MeasuredSize.X / 2f - widget2.MeasuredSize.X / 2f;
								num2 = num + widget2.MeasuredSize.X / 2f;
							}
						}
						else
						{
							num2 = num + num5;
						}
						if (widget.ChildCount < 64)
						{
							widget2.Layout(num, bottom2, num2, top2);
						}
						else
						{
							LayoutBox value = default(LayoutBox);
							value.Left = num;
							value.Right = num2;
							value.Bottom = bottom2;
							value.Top = top2;
							this._layoutBoxes.Add(k, value);
						}
						if (this.LayoutMethod == LayoutMethod.HorizontalRightToLeft)
						{
							num2 = num;
						}
						else if (this.LayoutMethod == LayoutMethod.HorizontalLeftToRight || this.LayoutMethod == LayoutMethod.HorizontalCentered)
						{
							num = num2;
						}
					}
					else
					{
						this._layoutBoxes.Add(k, default(LayoutBox));
					}
				}
			}
			if (widget.ChildCount >= 64)
			{
				this.ParallelUpdateLayouts(widget);
			}
		}

		// Token: 0x0600044A RID: 1098 RVA: 0x000116FC File Offset: 0x0000F8FC
		private void LayoutLinearVertical(Widget widget, float left, float bottom, float right, float top)
		{
			Container container = widget as Container;
			float left2 = 0f;
			float num = 0f;
			float num2 = bottom - top;
			float right2 = right - left;
			if (this.LayoutMethod != LayoutMethod.VerticalTopToBottom && this.LayoutMethod == LayoutMethod.VerticalCentered)
			{
				float num3 = 0f;
				for (int i = 0; i < widget.ChildCount; i++)
				{
					Widget child = widget.GetChild(i);
					if (child != null && child.IsVisible)
					{
						num3 += child.MeasuredSize.Y + child.ScaledMarginTop + child.ScaledMarginBottom;
					}
				}
				float num4 = (bottom - top) * 0.5f;
				float num5 = num3 * 0.5f;
				num2 = num4 + num5;
				num = num4 - num5;
			}
			this._layoutBoxes.Clear();
			for (int j = 0; j < widget.ChildCount; j++)
			{
				Widget child2 = widget.GetChild(j);
				if (child2 != null && child2.IsVisible)
				{
					if (container != null && container.IsDragHovering && j == container.DragHoverInsertionIndex)
					{
						if (this.LayoutMethod == LayoutMethod.VerticalBottomToTop)
						{
							num += 20f;
						}
						else
						{
							num2 -= 20f;
						}
					}
					float num6 = child2.MeasuredSize.Y + child2.ScaledMarginTop + child2.ScaledMarginBottom;
					if (this.LayoutMethod == LayoutMethod.VerticalBottomToTop || this.LayoutMethod == LayoutMethod.VerticalCentered)
					{
						num2 = num + num6;
					}
					else if (this.LayoutMethod == LayoutMethod.VerticalTopToBottom)
					{
						num = num2 - num6;
					}
					if (widget.ChildCount < 64)
					{
						child2.Layout(left2, num2, right2, num);
					}
					else
					{
						LayoutBox value = default(LayoutBox);
						value.Left = left2;
						value.Right = right2;
						value.Bottom = num2;
						value.Top = num;
						this._layoutBoxes.Add(j, value);
					}
					if (this.LayoutMethod == LayoutMethod.VerticalBottomToTop || this.LayoutMethod == LayoutMethod.VerticalCentered)
					{
						num = num2;
					}
					else
					{
						num2 = num;
					}
				}
				else
				{
					this._layoutBoxes.Add(j, default(LayoutBox));
				}
			}
			if (widget.ChildCount >= 64)
			{
				this.ParallelUpdateLayouts(widget);
			}
		}

		// Token: 0x0600044B RID: 1099 RVA: 0x000118F4 File Offset: 0x0000FAF4
		public Vector2 GetDropGizmoPosition(Container widget, Vector2 draggedWidgetPosition)
		{
			int row = 0;
			if (this.LayoutMethod == LayoutMethod.VerticalBottomToTop || this.LayoutMethod == LayoutMethod.VerticalTopToBottom || this.LayoutMethod == LayoutMethod.VerticalCentered)
			{
				row = 1;
			}
			bool flag = this.LayoutMethod == LayoutMethod.HorizontalRightToLeft || this.LayoutMethod == LayoutMethod.VerticalTopToBottom || this.LayoutMethod == LayoutMethod.VerticalCentered;
			int indexForDrop = this.GetIndexForDrop(widget, draggedWidgetPosition);
			int num = indexForDrop - 1;
			Vector2 globalPosition = widget.GlobalPosition;
			Vector2 globalPosition2 = widget.GlobalPosition;
			if (!flag)
			{
				if (num >= 0 && num < widget.ChildCount)
				{
					Widget child = widget.GetChild(num);
					StackLayout.SetData(ref globalPosition, row, StackLayout.GetData(child.GlobalPosition, row) + StackLayout.GetData(child.Size, row));
				}
				if (indexForDrop >= 0 && indexForDrop < widget.ChildCount)
				{
					StackLayout.SetData(ref globalPosition2, row, StackLayout.GetData(widget.GetChild(indexForDrop).GlobalPosition, row));
				}
				else if (indexForDrop >= widget.ChildCount && widget.ChildCount > 0)
				{
					StackLayout.SetData(ref globalPosition2, row, StackLayout.GetData(globalPosition, row) + 20f);
				}
			}
			else
			{
				StackLayout.SetData(ref globalPosition, row, StackLayout.GetData(globalPosition, row) + StackLayout.GetData(widget.Size, row));
				StackLayout.SetData(ref globalPosition2, row, StackLayout.GetData(globalPosition2, row) + StackLayout.GetData(widget.Size, row));
				if (num >= 0 && num < widget.ChildCount)
				{
					Widget child2 = widget.GetChild(num);
					StackLayout.SetData(ref globalPosition, row, StackLayout.GetData(child2.GlobalPosition, row));
				}
				if (indexForDrop >= 0 && indexForDrop < widget.ChildCount)
				{
					Widget child3 = widget.GetChild(indexForDrop);
					StackLayout.SetData(ref globalPosition2, row, StackLayout.GetData(child3.GlobalPosition, row) + StackLayout.GetData(child3.Size, row));
				}
				else if (indexForDrop >= widget.ChildCount && widget.ChildCount > 0)
				{
					StackLayout.SetData(ref globalPosition2, row, StackLayout.GetData(globalPosition, row) - 20f);
				}
			}
			return new Vector2((globalPosition.X + globalPosition2.X) / 2f, (globalPosition.Y + globalPosition2.Y) / 2f);
		}

		// Token: 0x0400021D RID: 541
		private const int DragHoverAperture = 20;

		// Token: 0x0400021E RID: 542
		private readonly Dictionary<int, LayoutBox> _layoutBoxes;

		// Token: 0x0400021F RID: 543
		private Widget _parallelMeasureBasicChildWidget;

		// Token: 0x04000220 RID: 544
		private Vector2 _parallelMeasureBasicChildMeasureSpec;

		// Token: 0x04000221 RID: 545
		private AlignmentAxis _parallelMeasureBasicChildAlignmentAxis;

		// Token: 0x04000222 RID: 546
		private TWParallel.ParallelForAuxPredicate _parallelMeasureBasicChildDelegate;
	}
}

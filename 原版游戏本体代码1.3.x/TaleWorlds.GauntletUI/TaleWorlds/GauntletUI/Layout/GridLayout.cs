using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.GauntletUI.Layout
{
	// Token: 0x02000043 RID: 67
	public class GridLayout : ILayout
	{
		// Token: 0x1700013B RID: 315
		// (get) Token: 0x06000428 RID: 1064 RVA: 0x0001059F File Offset: 0x0000E79F
		// (set) Token: 0x06000429 RID: 1065 RVA: 0x000105A7 File Offset: 0x0000E7A7
		public GridVerticalLayoutMethod VerticalLayoutMethod { get; set; }

		// Token: 0x1700013C RID: 316
		// (get) Token: 0x0600042A RID: 1066 RVA: 0x000105B0 File Offset: 0x0000E7B0
		// (set) Token: 0x0600042B RID: 1067 RVA: 0x000105B8 File Offset: 0x0000E7B8
		public GridHorizontalLayoutMethod HorizontalLayoutMethod { get; set; }

		// Token: 0x1700013D RID: 317
		// (get) Token: 0x0600042C RID: 1068 RVA: 0x000105C1 File Offset: 0x0000E7C1
		// (set) Token: 0x0600042D RID: 1069 RVA: 0x000105C9 File Offset: 0x0000E7C9
		public GridDirection Direction { get; set; }

		// Token: 0x1700013E RID: 318
		// (get) Token: 0x0600042E RID: 1070 RVA: 0x000105D2 File Offset: 0x0000E7D2
		// (set) Token: 0x0600042F RID: 1071 RVA: 0x000105DA File Offset: 0x0000E7DA
		public IReadOnlyList<float> RowHeights { get; private set; } = new List<float>();

		// Token: 0x1700013F RID: 319
		// (get) Token: 0x06000430 RID: 1072 RVA: 0x000105E3 File Offset: 0x0000E7E3
		// (set) Token: 0x06000431 RID: 1073 RVA: 0x000105EB File Offset: 0x0000E7EB
		public IReadOnlyList<float> ColumnWidths { get; private set; } = new List<float>();

		// Token: 0x06000432 RID: 1074 RVA: 0x000105F4 File Offset: 0x0000E7F4
		public GridLayout()
		{
			this.VerticalLayoutMethod = GridVerticalLayoutMethod.TopToBottom;
			this.HorizontalLayoutMethod = GridHorizontalLayoutMethod.LeftToRight;
		}

		// Token: 0x06000433 RID: 1075 RVA: 0x00010620 File Offset: 0x0000E820
		Vector2 ILayout.MeasureChildren(Widget widget, Vector2 measureSpec, SpriteData spriteData, float renderScale)
		{
			GridWidget gridWidget = (GridWidget)widget;
			Vector2 result = default(Vector2);
			int num = gridWidget.Children.Count((Widget x) => x.IsVisible);
			if (num > 0)
			{
				foreach (Widget widget2 in gridWidget.Children)
				{
					if (widget2.IsVisible && (widget2.WidthSizePolicy == SizePolicy.CoverChildren || widget2.HeightSizePolicy == SizePolicy.CoverChildren))
					{
						widget2.Measure(default(Vector2));
					}
				}
				int rowCount;
				int usedRowCount;
				int columnCount;
				int usedColumnCount;
				this.CalculateRowColumnCounts(gridWidget, this.Direction, num, out rowCount, out usedRowCount, out columnCount, out usedColumnCount);
				this.UpdateCellSizes(gridWidget, rowCount, usedRowCount, columnCount, usedColumnCount, measureSpec.X, measureSpec.Y);
				int num2 = 0;
				for (int i = 0; i < gridWidget.Children.Count; i++)
				{
					Widget widget3 = gridWidget.Children[i];
					if (widget3.IsVisible)
					{
						int index;
						int index2;
						this.CalculateRowColumnIndices(num2, rowCount, columnCount, out index, out index2);
						float element = this.GetElement(this.ColumnWidths, index2);
						float element2 = this.GetElement(this.RowHeights, index);
						widget3.Measure(new Vector2(element, element2));
						num2++;
					}
				}
				result..ctor(this.ColumnWidths.Sum(), this.RowHeights.Sum());
			}
			return result;
		}

		// Token: 0x06000434 RID: 1076 RVA: 0x000107A8 File Offset: 0x0000E9A8
		void ILayout.OnLayout(Widget widget, float left, float bottom, float right, float top)
		{
			GridWidget gridWidget = (GridWidget)widget;
			int num = gridWidget.Children.Count((Widget x) => x.IsVisible);
			if (num > 0)
			{
				float totalWidth = right - left;
				float totalHeight = bottom - top;
				int rowCount;
				int num2;
				int columnCount;
				int num3;
				this.CalculateRowColumnCounts(gridWidget, this.Direction, num, out rowCount, out num2, out columnCount, out num3);
				this.UpdateCellSizes(gridWidget, rowCount, num2, columnCount, num3, totalWidth, totalHeight);
				float[] array = new float[this.RowHeights.Count + 1];
				float[] array2 = new float[this.ColumnWidths.Count + 1];
				array[0] = 0f;
				array2[0] = 0f;
				for (int i = 0; i < this.RowHeights.Count; i++)
				{
					array[i + 1] = this.RowHeights[i] + array[i];
				}
				for (int j = 0; j < this.ColumnWidths.Count; j++)
				{
					array2[j + 1] = this.ColumnWidths[j] + array2[j];
				}
				int num4 = 0;
				for (int k = 0; k < gridWidget.Children.Count; k++)
				{
					Widget widget2 = gridWidget.Children[k];
					if (widget2.IsVisible)
					{
						int num5;
						int num6;
						this.CalculateRowColumnIndices(num4, rowCount, columnCount, out num5, out num6);
						int index = num2 - num5 - 1;
						int index2 = num3 - num6 - 1;
						float element = this.GetElement(this.ColumnWidths, num6);
						float element2 = this.GetElement(this.RowHeights, num5);
						float num7 = 0f;
						float num8 = 0f;
						if (this.VerticalLayoutMethod == GridVerticalLayoutMethod.TopToBottom)
						{
							num7 = this.GetElement(array, num5);
						}
						else if (this.VerticalLayoutMethod == GridVerticalLayoutMethod.Center)
						{
							if (this.Direction == GridDirection.ColumnFirst)
							{
								int index3 = MathF.Max(0, (num6 + 1) * num2 - num) / 2 + num5;
								num7 = this.GetElement(array, index3);
							}
						}
						else if (this.VerticalLayoutMethod == GridVerticalLayoutMethod.BottomToTop)
						{
							num7 = this.GetElement(array, index);
						}
						if (this.HorizontalLayoutMethod == GridHorizontalLayoutMethod.LeftToRight)
						{
							num8 = this.GetElement(array2, num6);
						}
						else if (this.HorizontalLayoutMethod == GridHorizontalLayoutMethod.Center)
						{
							if (this.Direction == GridDirection.RowFirst)
							{
								int index4 = MathF.Max(0, (num5 + 1) * num3 - num) / 2 + num6;
								num8 = this.GetElement(array2, index4);
							}
						}
						else if (this.HorizontalLayoutMethod == GridHorizontalLayoutMethod.RightToLeft)
						{
							num8 = this.GetElement(array2, index2);
						}
						widget2.Layout(num8, num7 + element2, num8 + element, num7);
						num4++;
					}
				}
			}
		}

		// Token: 0x06000435 RID: 1077 RVA: 0x00010A2C File Offset: 0x0000EC2C
		private void UpdateCellSizes(GridWidget gridWidget, int rowCount, int usedRowCount, int columnCount, int usedColumnCount, float totalWidth, float totalHeight)
		{
			float[] array = new float[usedRowCount];
			float[] array2 = new float[usedColumnCount];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = 0f;
			}
			for (int j = 0; j < array2.Length; j++)
			{
				array2[j] = 0f;
			}
			int num = 0;
			for (int k = 0; k < gridWidget.Children.Count; k++)
			{
				Widget widget = gridWidget.Children[k];
				if (widget.IsVisible)
				{
					int num2;
					int num3;
					this.CalculateRowColumnIndices(num, rowCount, columnCount, out num2, out num3);
					float a;
					if (gridWidget.WidthSizePolicy == SizePolicy.CoverChildren)
					{
						if (gridWidget.UseDynamicCellWidth && widget.WidthSizePolicy != SizePolicy.StretchToParent)
						{
							a = widget.MeasuredSize.X + widget.ScaledMarginLeft + widget.ScaledMarginRight;
						}
						else
						{
							a = gridWidget.DefaultScaledCellWidth;
						}
					}
					else
					{
						a = totalWidth / (float)columnCount;
					}
					float a2;
					if (gridWidget.HeightSizePolicy == SizePolicy.CoverChildren)
					{
						if (gridWidget.UseDynamicCellHeight && widget.HeightSizePolicy != SizePolicy.StretchToParent)
						{
							a2 = widget.MeasuredSize.Y + widget.ScaledMarginTop + widget.ScaledMarginBottom;
						}
						else
						{
							a2 = gridWidget.DefaultScaledCellHeight;
						}
					}
					else
					{
						a2 = totalHeight / (float)rowCount;
					}
					if (num2 >= 0 && num2 < array.Length)
					{
						array[num2] = MathF.Max(a2, array[num2]);
					}
					if (num3 >= 0 && num3 < array2.Length)
					{
						array2[num3] = MathF.Max(a, array2[num3]);
					}
					num++;
				}
			}
			this.RowHeights = array;
			this.ColumnWidths = array2;
		}

		// Token: 0x06000436 RID: 1078 RVA: 0x00010BB3 File Offset: 0x0000EDB3
		private void CalculateRowColumnIndices(int visibleIndex, int rowCount, int columnCount, out int row, out int column)
		{
			if (this.Direction == GridDirection.RowFirst)
			{
				row = visibleIndex / columnCount;
				column = visibleIndex % columnCount;
				return;
			}
			row = visibleIndex % rowCount;
			column = visibleIndex / rowCount;
		}

		// Token: 0x06000437 RID: 1079 RVA: 0x00010BD8 File Offset: 0x0000EDD8
		private void CalculateRowColumnCounts(GridWidget gridWidget, GridDirection direction, int visibleChildrenCount, out int rowCount, out int usedRowCount, out int columnCount, out int usedColumnCount)
		{
			bool flag = gridWidget.RowCount < 0;
			bool flag2 = gridWidget.ColumnCount < 0;
			rowCount = (flag ? 3 : gridWidget.RowCount);
			columnCount = (flag2 ? 3 : gridWidget.ColumnCount);
			int num;
			int num2;
			if (direction == GridDirection.RowFirst)
			{
				num = MathF.Min(visibleChildrenCount, columnCount);
				num2 = ((visibleChildrenCount % columnCount > 0) ? 1 : 0) + visibleChildrenCount / columnCount;
			}
			else
			{
				num2 = MathF.Min(visibleChildrenCount, rowCount);
				num = ((visibleChildrenCount % rowCount > 0) ? 1 : 0) + visibleChildrenCount / rowCount;
			}
			bool flag3 = gridWidget.HeightSizePolicy != SizePolicy.CoverChildren;
			bool flag4 = gridWidget.WidthSizePolicy != SizePolicy.CoverChildren;
			usedRowCount = (flag3 ? rowCount : num2);
			usedColumnCount = (flag4 ? columnCount : num);
		}

		// Token: 0x06000438 RID: 1080 RVA: 0x00010C90 File Offset: 0x0000EE90
		private float GetElement(IReadOnlyList<float> elements, int index)
		{
			if (index < 0 || index >= elements.Count)
			{
				return 0f;
			}
			return elements[index];
		}
	}
}

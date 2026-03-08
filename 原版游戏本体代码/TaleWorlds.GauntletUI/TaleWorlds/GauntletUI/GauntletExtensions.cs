using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI
{
	// Token: 0x02000024 RID: 36
	public static class GauntletExtensions
	{
		// Token: 0x060002EF RID: 751 RVA: 0x0000E758 File Offset: 0x0000C958
		public static void SetGlobalAlphaRecursively(this Widget widget, float alphaFactor)
		{
			widget.SetAlpha(alphaFactor);
			List<Widget> children = widget.Children;
			for (int i = 0; i < children.Count; i++)
			{
				children[i].SetGlobalAlphaRecursively(alphaFactor);
			}
		}

		// Token: 0x060002F0 RID: 752 RVA: 0x0000E794 File Offset: 0x0000C994
		public static void SetAlpha(this Widget widget, float alphaFactor)
		{
			BrushWidget brushWidget;
			if ((brushWidget = widget as BrushWidget) != null)
			{
				brushWidget.Brush.GlobalAlphaFactor = alphaFactor;
			}
			TextureWidget textureWidget;
			if ((textureWidget = widget as TextureWidget) != null)
			{
				textureWidget.Brush.GlobalAlphaFactor = alphaFactor;
			}
			widget.AlphaFactor = alphaFactor;
		}

		// Token: 0x060002F1 RID: 753 RVA: 0x0000E7D4 File Offset: 0x0000C9D4
		public static void RegisterBrushStatesOfWidget(this Widget widget)
		{
			BrushWidget brushWidget;
			if ((brushWidget = widget as BrushWidget) != null)
			{
				foreach (Style style in brushWidget.ReadOnlyBrush.Styles)
				{
					if (!widget.ContainsState(style.Name))
					{
						widget.AddState(style.Name);
					}
				}
			}
		}

		// Token: 0x060002F2 RID: 754 RVA: 0x0000E84C File Offset: 0x0000CA4C
		public static string GetFullIDPath(this Widget widget)
		{
			StringBuilder stringBuilder = new StringBuilder(string.IsNullOrEmpty(widget.Id) ? widget.GetType().Name : widget.Id);
			for (Widget parentWidget = widget.ParentWidget; parentWidget != null; parentWidget = parentWidget.ParentWidget)
			{
				stringBuilder.Insert(0, (string.IsNullOrEmpty(parentWidget.Id) ? parentWidget.GetType().Name : parentWidget.Id) + "\\");
			}
			return stringBuilder.ToString();
		}

		// Token: 0x060002F3 RID: 755 RVA: 0x0000E8CC File Offset: 0x0000CACC
		public static void ApplyActionForThisAndAllChildren(this Widget widget, Action<Widget> action)
		{
			action(widget);
			List<Widget> children = widget.Children;
			for (int i = 0; i < children.Count; i++)
			{
				children[i].ApplyActionForThisAndAllChildren(action);
			}
		}
	}
}

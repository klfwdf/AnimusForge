using System;
using System.Collections.Generic;
using System.Text;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.GauntletUI;

public static class GauntletExtensions
{
	public static void SetGlobalAlphaRecursively(this Widget widget, float alphaFactor)
	{
		widget.SetAlpha(alphaFactor);
		List<Widget> children = widget.Children;
		for (int i = 0; i < children.Count; i++)
		{
			children[i].SetGlobalAlphaRecursively(alphaFactor);
		}
	}

	public static void SetAlpha(this Widget widget, float alphaFactor)
	{
		if (widget is BrushWidget brushWidget)
		{
			brushWidget.Brush.GlobalAlphaFactor = alphaFactor;
		}
		if (widget is TextureWidget textureWidget)
		{
			textureWidget.Brush.GlobalAlphaFactor = alphaFactor;
		}
		widget.AlphaFactor = alphaFactor;
	}

	public static void RegisterBrushStatesOfWidget(this Widget widget)
	{
		if (!(widget is BrushWidget brushWidget))
		{
			return;
		}
		foreach (Style style in brushWidget.ReadOnlyBrush.Styles)
		{
			if (!widget.ContainsState(style.Name))
			{
				widget.AddState(style.Name);
			}
		}
	}

	public static string GetFullIDPath(this Widget widget)
	{
		StringBuilder stringBuilder = new StringBuilder(string.IsNullOrEmpty(widget.Id) ? widget.GetType().Name : widget.Id);
		for (Widget parentWidget = widget.ParentWidget; parentWidget != null; parentWidget = parentWidget.ParentWidget)
		{
			stringBuilder.Insert(0, (string.IsNullOrEmpty(parentWidget.Id) ? parentWidget.GetType().Name : parentWidget.Id) + "\\");
		}
		return stringBuilder.ToString();
	}

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

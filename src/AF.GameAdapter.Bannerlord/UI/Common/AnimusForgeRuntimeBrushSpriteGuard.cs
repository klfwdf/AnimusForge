using System;
using TaleWorlds.GauntletUI;
using TaleWorlds.TwoDimension;

namespace AnimusForge;

internal static class AnimusForgeRuntimeBrushSpriteGuard
{
	public static bool TryApplyLayerStyle(Brush brush, string layerName, Sprite sprite, out string failureReason)
	{
		failureReason = "";
		if (brush == null)
		{
			failureReason = "brush is null";
			return false;
		}
		if (string.IsNullOrWhiteSpace(layerName))
		{
			failureReason = "layer name is empty";
			return false;
		}
		if (!IsSpriteUsable(sprite, out failureReason))
		{
			return false;
		}
		if (brush.DefaultStyle == null)
		{
			failureReason = "brush default style is null";
			return false;
		}
		if (brush.Layers == null || brush.Styles == null)
		{
			failureReason = "brush layer/style collection is null";
			return false;
		}

		BrushLayer layer = null;
		Sprite previousSprite = null;
		bool previousLayerHidden = false;
		bool addedLayer = false;
		bool addedStyle = false;
		try
		{
			layer = brush.GetLayer(layerName);
			if (layer == null)
			{
				RemoveOrphanStyleLayers(brush, layerName);
				layer = new BrushLayer
				{
					Name = layerName,
					Sprite = sprite,
					Color = TaleWorlds.Library.Color.White,
					ColorFactor = 1f,
					AlphaFactor = 1f,
					IsHidden = true
				};
				brush.AddLayer(layer);
				addedLayer = true;
			}
			else
			{
				previousSprite = layer.Sprite;
				previousLayerHidden = layer.IsHidden;
				layer.Sprite = sprite;
			}

			Style style = brush.GetStyle(layerName);
			if (style == null)
			{
				style = new Style(brush.Layers)
				{
					DefaultStyle = brush.DefaultStyle,
					Name = layerName
				};
				brush.AddStyle(style);
				addedStyle = true;
			}
			else if (style.DefaultStyle == null)
			{
				style.DefaultStyle = brush.DefaultStyle;
			}

			StyleLayer styleLayer = style.GetLayer(layerName);
			if (styleLayer != null && styleLayer.SourceLayer != layer)
			{
				style.RemoveLayer(layerName);
				styleLayer = null;
			}
			if (styleLayer == null)
			{
				styleLayer = new StyleLayer(layer);
				style.AddLayer(styleLayer);
			}
			styleLayer.IsHidden = false;

			if (!IsApplied(brush, layerName, out failureReason))
			{
				throw new InvalidOperationException(failureReason);
			}
			return true;
		}
		catch (Exception ex)
		{
			failureReason = string.IsNullOrWhiteSpace(failureReason) ? ex.GetType().Name + ": " + ex.Message : failureReason + "; " + ex.GetType().Name + ": " + ex.Message;
			Rollback(brush, layerName, layer, previousSprite, previousLayerHidden, addedLayer, addedStyle);
			return false;
		}
	}

	private static bool IsSpriteUsable(Sprite sprite, out string reason)
	{
		reason = "";
		if (sprite == null)
		{
			reason = "sprite is null";
			return false;
		}
		try
		{
			if (sprite.Texture == null)
			{
				reason = "sprite texture is null";
				return false;
			}
			sprite.GetMinUvs();
			sprite.GetMaxUvs();
			return true;
		}
		catch (Exception ex)
		{
			reason = "sprite validation failed: " + ex.GetType().Name + ": " + ex.Message;
			return false;
		}
	}

	private static bool IsApplied(Brush brush, string layerName, out string reason)
	{
		reason = "";
		BrushLayer layer = brush.GetLayer(layerName);
		if (layer == null)
		{
			reason = "brush layer missing after apply";
			return false;
		}
		if (layer.Sprite == null)
		{
			reason = "brush layer sprite missing after apply";
			return false;
		}
		Style style = brush.GetStyle(layerName);
		if (style == null)
		{
			reason = "brush style missing after apply";
			return false;
		}
		StyleLayer styleLayer = style.GetLayer(layerName);
		if (styleLayer == null)
		{
			reason = "style layer missing after apply";
			return false;
		}
		if (styleLayer.SourceLayer != layer)
		{
			reason = "style layer source mismatch after apply";
			return false;
		}
		return true;
	}

	private static void RemoveOrphanStyleLayers(Brush brush, string layerName)
	{
		try
		{
			foreach (Style style in brush.Styles)
			{
				try
				{
					if (style?.GetLayer(layerName) != null)
					{
						style.RemoveLayer(layerName);
					}
				}
				catch
				{
				}
			}
		}
		catch
		{
		}
	}

	private static void Rollback(Brush brush, string layerName, BrushLayer layer, Sprite previousSprite, bool previousLayerHidden, bool addedLayer, bool addedStyle)
	{
		try
		{
			if (addedStyle)
			{
				brush.RemoveStyle(layerName);
			}
		}
		catch
		{
		}
		try
		{
			if (addedLayer)
			{
				brush.RemoveLayer(layerName);
			}
			else if (layer != null)
			{
				layer.Sprite = previousSprite;
				layer.IsHidden = previousLayerHidden;
			}
		}
		catch
		{
		}
	}
}

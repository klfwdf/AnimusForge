using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets;

public class DefaultLayerSpriteChangerBrushWidget : BrushWidget
{
	private Brush _spriteBrush;

	private string _spriteBrushLayerName;

	[Editor(false)]
	public Brush SpriteBrush
	{
		get
		{
			return _spriteBrush;
		}
		set
		{
			if (_spriteBrush != value)
			{
				_spriteBrush = value;
				OnPropertyChanged(value, "SpriteBrush");
				UpdateDefaultLayerSprite();
			}
		}
	}

	[Editor(false)]
	public string SpriteBrushLayerName
	{
		get
		{
			return _spriteBrushLayerName;
		}
		set
		{
			if (_spriteBrushLayerName != value)
			{
				_spriteBrushLayerName = value;
				OnPropertyChanged(value, "SpriteBrushLayerName");
				UpdateDefaultLayerSprite();
			}
		}
	}

	public DefaultLayerSpriteChangerBrushWidget(UIContext context)
		: base(context)
	{
		UpdateDefaultLayerSprite();
	}

	private void UpdateDefaultLayerSprite()
	{
		Sprite sprite = ((SpriteBrushLayerName == null) ? null : SpriteBrush?.GetLayer(SpriteBrushLayerName)?.Sprite);
		base.IsVisible = sprite != null;
		if (base.IsVisible)
		{
			base.Brush.Sprite = sprite;
			base.Brush.DefaultLayer.Sprite = sprite;
		}
	}
}

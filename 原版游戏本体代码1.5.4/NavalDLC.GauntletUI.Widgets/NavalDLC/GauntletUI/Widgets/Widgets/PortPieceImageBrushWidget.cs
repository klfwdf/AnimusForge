using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace NavalDLC.GauntletUI.Widgets.Widgets;

public class PortPieceImageBrushWidget : BrushWidget
{
	private string _identifier;

	public string Identifier
	{
		get
		{
			return _identifier;
		}
		set
		{
			if (value != _identifier)
			{
				_identifier = value;
				OnPropertyChanged(value, "Identifier");
				UpdateIcon();
			}
		}
	}

	public PortPieceImageBrushWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateIcon()
	{
		if (base.Brush == null)
		{
			return;
		}
		Sprite sprite = base.Context.SpriteData.GetSprite("PieceThumbnails\\" + Identifier);
		base.Brush.Sprite = sprite;
		foreach (BrushLayer layer in base.Brush.Layers)
		{
			layer.Sprite = sprite;
		}
	}
}

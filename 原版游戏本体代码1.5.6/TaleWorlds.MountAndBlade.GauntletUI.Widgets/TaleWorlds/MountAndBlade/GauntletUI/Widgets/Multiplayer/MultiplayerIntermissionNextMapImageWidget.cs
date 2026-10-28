using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer;

public class MultiplayerIntermissionNextMapImageWidget : Widget
{
	private string _mapID;

	[DataSourceProperty]
	public string MapID
	{
		get
		{
			return _mapID;
		}
		set
		{
			if (value != _mapID)
			{
				_mapID = value;
				OnPropertyChanged(value, "MapID");
				UpdateMapImage();
			}
		}
	}

	public MultiplayerIntermissionNextMapImageWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateMapImage()
	{
		if (!string.IsNullOrEmpty(MapID))
		{
			base.Sprite = base.Context.SpriteData.GetSprite(MapID);
		}
	}
}

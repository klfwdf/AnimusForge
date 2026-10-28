using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.TwoDimension;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby.Home;

public class MultiplayerLobbyAnnouncementIconBrushWidget : BrushWidget
{
	private string _announcementType;

	private Brush _iconBrush;

	public string AnnouncementType
	{
		get
		{
			return _announcementType;
		}
		set
		{
			if (value != _announcementType)
			{
				_announcementType = value;
				OnPropertyChanged(value, "AnnouncementType");
				UpdateIcon();
			}
		}
	}

	public Brush IconBrush
	{
		get
		{
			return _iconBrush;
		}
		set
		{
			if (value != _iconBrush)
			{
				_iconBrush = value;
				OnPropertyChanged(value, "IconBrush");
				UpdateIcon();
			}
		}
	}

	public MultiplayerLobbyAnnouncementIconBrushWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateIcon()
	{
		if (AnnouncementType == null)
		{
			return;
		}
		Sprite sprite = IconBrush?.GetLayer(AnnouncementType)?.Sprite;
		if (base.Brush == null)
		{
			return;
		}
		base.Brush.Sprite = sprite;
		foreach (BrushLayer layer in base.Brush.Layers)
		{
			layer.Sprite = sprite;
		}
	}
}

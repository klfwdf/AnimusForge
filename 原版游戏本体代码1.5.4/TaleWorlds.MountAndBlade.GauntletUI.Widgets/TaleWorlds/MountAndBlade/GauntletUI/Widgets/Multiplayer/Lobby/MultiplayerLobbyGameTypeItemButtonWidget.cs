using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby;

public class MultiplayerLobbyGameTypeItemButtonWidget : ButtonWidget
{
	private string _gameTypeID;

	[Editor(false)]
	public string GameTypeID
	{
		get
		{
			return _gameTypeID;
		}
		set
		{
			if (value != _gameTypeID)
			{
				_gameTypeID = value;
				OnPropertyChanged(value, "GameTypeID");
				UpdateSprite();
			}
		}
	}

	public MultiplayerLobbyGameTypeItemButtonWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateSprite()
	{
		base.Brush.DefaultLayer.Sprite = base.Context.SpriteData.GetSprite("MPLobby\\GameTypes\\" + GameTypeID);
	}
}

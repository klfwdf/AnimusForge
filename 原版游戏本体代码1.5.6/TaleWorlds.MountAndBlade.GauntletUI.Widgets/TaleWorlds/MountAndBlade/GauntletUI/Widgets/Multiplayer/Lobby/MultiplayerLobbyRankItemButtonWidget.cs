using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby;

public class MultiplayerLobbyRankItemButtonWidget : ButtonWidget
{
	private const string _defaultRankID = "unranked";

	private string _rankID;

	[Editor(false)]
	public string RankID
	{
		get
		{
			return _rankID;
		}
		set
		{
			if (value != _rankID)
			{
				_rankID = value;
				OnPropertyChanged(value, "RankID");
				UpdateSprite();
			}
		}
	}

	public MultiplayerLobbyRankItemButtonWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateSprite()
	{
		string text = "unranked";
		if (RankID != string.Empty)
		{
			text = RankID;
		}
		base.Brush.DefaultLayer.Sprite = base.Context.SpriteData.GetSprite("MPGeneral\\MPRanks\\" + text);
	}
}

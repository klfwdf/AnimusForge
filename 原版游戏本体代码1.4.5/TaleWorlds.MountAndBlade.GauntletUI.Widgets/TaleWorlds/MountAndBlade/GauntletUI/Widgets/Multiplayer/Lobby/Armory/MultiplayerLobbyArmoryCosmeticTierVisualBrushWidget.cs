using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby.Armory;

public class MultiplayerLobbyArmoryCosmeticTierVisualBrushWidget : BrushWidget
{
	private int _rarity = -1;

	[Editor(false)]
	public int Rarity
	{
		get
		{
			return _rarity;
		}
		set
		{
			if (_rarity != value)
			{
				_rarity = value;
				OnPropertyChanged(value, "Rarity");
				UpdateVisual();
			}
		}
	}

	public MultiplayerLobbyArmoryCosmeticTierVisualBrushWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateVisual()
	{
		switch (_rarity)
		{
		case 0:
		case 1:
			SetState("Common");
			break;
		case 2:
			SetState("Rare");
			break;
		case 3:
			SetState("Unique");
			break;
		}
	}
}

using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer.Lobby.Armory;

public class MultiplayerLobbyArmoryCosmeticItemBrushWidget : BrushWidget
{
	private const string BaseBrushName = "MPLobby.Armory.CosmeticButton";

	private bool _isUsed;

	private int _rarity;

	[Editor(false)]
	public bool IsUsed
	{
		get
		{
			return _isUsed;
		}
		set
		{
			if (value != _isUsed)
			{
				_isUsed = value;
				OnPropertyChanged(value, "IsUsed");
				OnUsageChanged();
			}
		}
	}

	[Editor(false)]
	public int Rarity
	{
		get
		{
			return _rarity;
		}
		set
		{
			if (value != _rarity)
			{
				_rarity = value;
				OnPropertyChanged(value, "Rarity");
				OnRarityChanged();
			}
		}
	}

	public MultiplayerLobbyArmoryCosmeticItemBrushWidget(UIContext context)
		: base(context)
	{
	}

	public override void SetState(string stateName)
	{
	}

	private void OnUsageChanged()
	{
		base.SetState(IsUsed ? "Selected" : "Default");
	}

	private void OnRarityChanged()
	{
		switch (Rarity)
		{
		case 0:
		case 1:
			base.Brush = base.Context.GetBrush("MPLobby.Armory.CosmeticButton.Common");
			break;
		case 2:
			base.Brush = base.Context.GetBrush("MPLobby.Armory.CosmeticButton.Rare");
			break;
		case 3:
			base.Brush = base.Context.GetBrush("MPLobby.Armory.CosmeticButton.Unique");
			break;
		}
	}
}

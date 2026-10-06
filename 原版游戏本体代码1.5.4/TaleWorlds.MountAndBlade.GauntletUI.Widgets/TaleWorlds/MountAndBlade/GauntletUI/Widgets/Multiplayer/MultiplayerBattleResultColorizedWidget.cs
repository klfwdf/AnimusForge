using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Multiplayer;

public class MultiplayerBattleResultColorizedWidget : Widget
{
	private int _battleResult = -1;

	private Color _drawColor;

	private Color _victoryColor;

	private Color _defeatColor;

	[Editor(false)]
	public int BattleResult
	{
		get
		{
			return _battleResult;
		}
		set
		{
			if (_battleResult != value)
			{
				_battleResult = value;
				OnPropertyChanged(value, "BattleResult");
				BattleResultUpdated();
			}
		}
	}

	[Editor(false)]
	public Color DrawColor
	{
		get
		{
			return _drawColor;
		}
		set
		{
			if (_drawColor != value)
			{
				_drawColor = value;
				OnPropertyChanged(value, "DrawColor");
			}
		}
	}

	[Editor(false)]
	public Color VictoryColor
	{
		get
		{
			return _victoryColor;
		}
		set
		{
			if (_victoryColor != value)
			{
				_victoryColor = value;
				OnPropertyChanged(value, "VictoryColor");
			}
		}
	}

	[Editor(false)]
	public Color DefeatColor
	{
		get
		{
			return _defeatColor;
		}
		set
		{
			if (_defeatColor != value)
			{
				_defeatColor = value;
				OnPropertyChanged(value, "DefeatColor");
			}
		}
	}

	public MultiplayerBattleResultColorizedWidget(UIContext context)
		: base(context)
	{
	}

	private void BattleResultUpdated()
	{
		if (BattleResult == 2)
		{
			base.Color = DrawColor;
		}
		else if (BattleResult == 1)
		{
			base.Color = VictoryColor;
		}
		else if (BattleResult == 0)
		{
			base.Color = DefeatColor;
		}
	}
}

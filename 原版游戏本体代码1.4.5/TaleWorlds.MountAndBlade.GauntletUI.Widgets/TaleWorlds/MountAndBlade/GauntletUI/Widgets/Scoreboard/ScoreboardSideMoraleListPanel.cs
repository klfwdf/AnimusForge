using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;
using TaleWorlds.GauntletUI.ExtraWidgets;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Scoreboard;

public class ScoreboardSideMoraleListPanel : ListPanel
{
	private float _morale;

	private float _maxMorale = 100f;

	[Editor(false)]
	public float Morale
	{
		get
		{
			return _morale;
		}
		set
		{
			if (_morale != value)
			{
				_morale = value;
				OnPropertyChanged(value, "Morale");
				OnMoraleUpdated();
			}
		}
	}

	[Editor(false)]
	public float MaxMorale
	{
		get
		{
			return _maxMorale;
		}
		set
		{
			if (_maxMorale != value)
			{
				_maxMorale = value;
				OnPropertyChanged(value, "MaxMorale");
				OnMoraleUpdated();
			}
		}
	}

	public ScoreboardSideMoraleListPanel(UIContext context)
		: base(context)
	{
	}

	private void OnMoraleUpdated()
	{
		if (base.ChildCount <= 0)
		{
			return;
		}
		float value = ((MaxMorale != 0f) ? (Morale / MaxMorale * 100f) : 0f);
		value = MathF.Clamp(value, 0f, 100f);
		value = MathF.Round(value);
		float num = 100 / base.ChildCount;
		for (int i = 0; i < base.ChildCount; i++)
		{
			float value2 = (value - (float)i * num) / num;
			value2 = MathF.Clamp(value2, 0f, 1f);
			Widget child = GetChild(i);
			if (value2 > 0f)
			{
				child.SetState("Default");
			}
			else
			{
				child.SetState("Disabled");
			}
			(child as FillBarWidget).InitialAmountAsFloat = value2;
		}
	}
}

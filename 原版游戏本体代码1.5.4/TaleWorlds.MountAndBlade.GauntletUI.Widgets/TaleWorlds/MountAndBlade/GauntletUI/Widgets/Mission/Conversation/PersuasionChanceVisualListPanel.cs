using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Mission.Conversation;

public class PersuasionChanceVisualListPanel : ListPanel
{
	private int _chanceValue;

	public bool IsFailChance { get; set; }

	public int ChanceValue
	{
		get
		{
			return _chanceValue;
		}
		set
		{
			if (_chanceValue != value)
			{
				_chanceValue = value;
				OnPropertyChanged(value, "ChanceValue");
			}
		}
	}

	public PersuasionChanceVisualListPanel(UIContext context)
		: base(context)
	{
	}

	protected override void OnLateUpdate(float dt)
	{
		base.OnLateUpdate(dt);
		base.IsVisible = !IsFailChance && ChanceValue > 0;
	}
}

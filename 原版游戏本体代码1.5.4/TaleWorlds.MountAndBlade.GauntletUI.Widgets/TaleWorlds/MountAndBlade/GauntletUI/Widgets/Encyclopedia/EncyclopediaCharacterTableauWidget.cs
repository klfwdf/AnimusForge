using TaleWorlds.GauntletUI;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Encyclopedia;

public class EncyclopediaCharacterTableauWidget : CharacterTableauWidget
{
	private bool _isDead;

	[Editor(false)]
	public bool IsDead
	{
		get
		{
			return _isDead;
		}
		set
		{
			if (_isDead != value)
			{
				_isDead = value;
				OnPropertyChanged(value, "IsDead");
				UpdateVisual(value);
			}
		}
	}

	public EncyclopediaCharacterTableauWidget(UIContext context)
		: base(context)
	{
	}

	private void UpdateVisual(bool isDead)
	{
		base.Brush.SaturationFactor = (isDead ? (-100) : 0);
	}
}

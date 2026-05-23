using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.Party;

public class PartyUpgradesContainerWidget : Widget
{
	private const float _noRequirementOffset = 8f;

	private bool _anyUpgradeHasRequirement = true;

	[Editor(false)]
	public bool AnyUpgradeHasRequirement
	{
		get
		{
			return _anyUpgradeHasRequirement;
		}
		set
		{
			if (_anyUpgradeHasRequirement != value)
			{
				_anyUpgradeHasRequirement = value;
				OnAnyUpgradeHasRequirementChanged(value);
				OnPropertyChanged(value, "AnyUpgradeHasRequirement");
			}
		}
	}

	public PartyUpgradesContainerWidget(UIContext context)
		: base(context)
	{
	}

	private void OnAnyUpgradeHasRequirementChanged(bool value)
	{
		base.ScaledPositionYOffset = (value ? 0f : 8f);
	}
}

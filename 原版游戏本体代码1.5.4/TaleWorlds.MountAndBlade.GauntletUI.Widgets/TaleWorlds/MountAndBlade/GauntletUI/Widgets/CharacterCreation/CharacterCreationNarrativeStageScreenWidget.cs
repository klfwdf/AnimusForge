using TaleWorlds.GauntletUI;
using TaleWorlds.GauntletUI.BaseTypes;

namespace TaleWorlds.MountAndBlade.GauntletUI.Widgets.CharacterCreation;

public class CharacterCreationNarrativeStageScreenWidget : Widget
{
	public ButtonWidget NextButton { get; set; }

	public ButtonWidget PreviousButton { get; set; }

	public ListPanel ItemList { get; set; }

	public CharacterCreationNarrativeStageScreenWidget(UIContext context)
		: base(context)
	{
	}
}

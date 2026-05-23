using TaleWorlds.MountAndBlade.Missions.Hints;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Missions.Interaction.InteractionItems;

internal class MissionHintInteractionItemVM : MissionInteractionItemBaseVM
{
	public readonly MissionHint Hint;

	public MissionHintInteractionItemVM(MissionHint hint)
	{
		Hint = hint;
		RefreshValues();
	}

	public override void RefreshValues()
	{
		base.RefreshValues();
		base.Message = Hint.Description.ToString();
	}
}

using TaleWorlds.MountAndBlade.ViewModelCollection.Missions.Interaction;
using TaleWorlds.MountAndBlade.ViewModelCollection.Missions.Interaction.InteractionItems;

namespace TaleWorlds.MountAndBlade.View.MissionViews;

public class MissionAgentStatusUIHandler : MissionBattleUIBaseView, IInteractionInterfaceHandler
{
	public virtual void AddInteractionMessage(MissionInteractionItemBaseVM message)
	{
	}

	public virtual void RemoveInteractionMessage(MissionInteractionItemBaseVM message)
	{
	}

	public virtual bool HasInteractionMessage(MissionInteractionItemBaseVM message)
	{
		return false;
	}

	protected override void OnCreateView()
	{
	}

	protected override void OnDestroyView()
	{
	}

	protected override void OnSuspendView()
	{
	}

	protected override void OnResumeView()
	{
	}
}

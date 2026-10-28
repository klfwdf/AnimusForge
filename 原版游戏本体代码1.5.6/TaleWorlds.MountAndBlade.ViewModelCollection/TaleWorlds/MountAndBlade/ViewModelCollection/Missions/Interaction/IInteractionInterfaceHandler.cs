using TaleWorlds.MountAndBlade.ViewModelCollection.Missions.Interaction.InteractionItems;

namespace TaleWorlds.MountAndBlade.ViewModelCollection.Missions.Interaction;

public interface IInteractionInterfaceHandler
{
	void AddInteractionMessage(MissionInteractionItemBaseVM message);

	void RemoveInteractionMessage(MissionInteractionItemBaseVM message);

	bool HasInteractionMessage(MissionInteractionItemBaseVM message);
}

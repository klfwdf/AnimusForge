using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade.Source.Objects;

public class NavigationMeshDeactivator : ScriptComponentBehavior
{
	public int DisableFaceWithId = -1;

	public int DisableFaceWithIdForAnimals = -1;

	public void DisableAssignedFaces(Scene scene)
	{
		scene.SetAbilityOfFacesWithId(DisableFaceWithId, isEnabled: false);
	}

	public void EnableAssignedFaces(Scene scene)
	{
		scene.SetAbilityOfFacesWithId(DisableFaceWithId, isEnabled: true);
	}
}

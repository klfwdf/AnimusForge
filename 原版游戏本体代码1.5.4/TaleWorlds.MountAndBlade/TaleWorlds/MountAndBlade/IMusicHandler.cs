namespace TaleWorlds.MountAndBlade;

public interface IMusicHandler
{
	bool IsPausable { get; }

	void OnUpdated(float dt);
}

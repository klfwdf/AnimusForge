namespace TaleWorlds.Core;

public interface IGameStateListener
{
	void OnActivate();

	void OnDeactivate();

	void OnInitialize();

	void OnFinalize();
}

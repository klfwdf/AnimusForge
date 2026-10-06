namespace TaleWorlds.Engine;

public interface ILoadingWindowManager
{
	void EnableLoadingWindow();

	void DisableLoadingWindow();

	void SetCurrentModeIsMultiplayer(bool isMultiplayer);

	void Initialize();

	void Destroy();
}

namespace TaleWorlds.Core;

public interface ICameraModeLogic
{
	SpectatorCameraTypes GetMissionCameraLockMode(bool lockedToMainPlayer);
}

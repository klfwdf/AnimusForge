namespace TaleWorlds.PlayerServices.Avatar;

public interface IAvatarService
{
	AvatarData GetPlayerAvatar(PlayerId playerId);

	void Initialize();

	void ClearCache();

	bool IsInitialized();

	void Tick(float dt);
}

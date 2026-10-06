namespace TaleWorlds.PlayerServices.Avatar;

public class AvatarDataResponse
{
	public bool IsFallBack { get; private set; }

	public AvatarData AvatarData { get; private set; }

	public AvatarDataResponse(bool isFallBack, AvatarData avatarData)
	{
		IsFallBack = isFallBack;
		AvatarData = avatarData;
	}
}

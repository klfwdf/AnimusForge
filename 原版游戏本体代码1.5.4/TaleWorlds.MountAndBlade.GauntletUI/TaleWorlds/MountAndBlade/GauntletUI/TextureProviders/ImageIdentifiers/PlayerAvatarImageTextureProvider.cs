using System.Linq;
using TaleWorlds.Avatar.PlayerServices;
using TaleWorlds.MountAndBlade.View.Tableaus;
using TaleWorlds.MountAndBlade.View.Tableaus.Thumbnails;
using TaleWorlds.PlayerServices;
using TaleWorlds.PlayerServices.Avatar;

namespace TaleWorlds.MountAndBlade.GauntletUI.TextureProviders.ImageIdentifiers;

public class PlayerAvatarImageTextureProvider : ImageIdentifierTextureProvider
{
	private const float AvatarFallbackWaitTime = 1f;

	private const float AvatarFailWaitTime = 5f;

	private AvatarData _receivedAvatarData;

	private bool _isUsingFallbackAvatar;

	private float _timeSinceAvatarFail;

	public PlayerAvatarImageTextureProvider()
	{
		_timeSinceAvatarFail = 5f;
	}

	public override void Tick(float dt)
	{
		_timeSinceAvatarFail += dt;
		base.Tick(dt);
	}

	protected override void OnCreateImageWithId(string id, string additionalArgs)
	{
		PlayerId playerId = PlayerId.FromString(id);
		int num = -1;
		int result;
		AvatarDataResponse avatarDataResponse = AvatarServices.GetPlayerAvatar(forcedIndex: (string.IsNullOrEmpty(additionalArgs) || !int.TryParse(additionalArgs, out result)) ? (GameNetwork.NetworkPeers.FirstOrDefault((NetworkCommunicator np) => np.VirtualPlayer.Id == playerId)?.ForcedAvatarIndex ?? (-1)) : result, playerId: playerId);
		if (avatarDataResponse != null)
		{
			_receivedAvatarData = avatarDataResponse.AvatarData;
			_isUsingFallbackAvatar = avatarDataResponse.IsFallBack;
			if (_isUsingFallbackAvatar)
			{
				_timeSinceAvatarFail = 0f;
			}
		}
		else
		{
			_timeSinceAvatarFail = 0f;
		}
	}

	protected override bool GetCanForceCheckTexture()
	{
		return TextureNeedsRefresh();
	}

	protected override void OnCheckTexture()
	{
		if (_receivedAvatarData != null)
		{
			if (_receivedAvatarData.Status == AvatarData.DataStatus.Ready)
			{
				AvatarData receivedAvatarData = _receivedAvatarData;
				_receivedAvatarData = null;
				OnAvatarLoaded(base.ImageId + "." + base.AdditionalArgs, receivedAvatarData);
			}
			else if (_receivedAvatarData.Status == AvatarData.DataStatus.Failed)
			{
				_receivedAvatarData = null;
				OnTextureCreated(null);
				ForceRefreshTextures();
				_timeSinceAvatarFail = 0f;
			}
		}
		else if (_isUsingFallbackAvatar && _timeSinceAvatarFail > 1f)
		{
			CreateImageWithId(base.ImageId, base.AdditionalArgs);
		}
		else if (_timeSinceAvatarFail > 5f)
		{
			CreateImageWithId(base.ImageId, base.AdditionalArgs);
		}
	}

	private void OnAvatarLoaded(string avatarID, AvatarData avatarData)
	{
		if (avatarData != null)
		{
			AvatarThumbnailCreationData thumbnailCreationData = new AvatarThumbnailCreationData(avatarID, avatarData.Image, avatarData.Width, avatarData.Height, avatarData.Type);
			OnTextureCreated(ThumbnailCacheManager.Current.CreateTexture(thumbnailCreationData).Texture);
		}
	}

	private bool TextureNeedsRefresh()
	{
		if (_receivedAvatarData != null || (_isUsingFallbackAvatar && _timeSinceAvatarFail > 1f) || _timeSinceAvatarFail > 5f)
		{
			return true;
		}
		return false;
	}
}

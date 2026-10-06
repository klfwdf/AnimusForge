using System.Collections.Generic;
using System.IO;
using TaleWorlds.Avatar.PlayerServices;
using TaleWorlds.Library;

namespace TaleWorlds.PlayerServices.Avatar;

internal class ForcedAvatarService : IAvatarService
{
	private readonly string _resourceFolder = BasePath.Name + "Modules/Native/MultiplayerForcedAvatars/";

	private readonly List<byte[]> _avatarImagesAsByteArrays = new List<byte[]>();

	private bool _isInitialized;

	public int AvatarCount => _avatarImagesAsByteArrays.Count;

	public AvatarData GetPlayerAvatar(PlayerId playerId)
	{
		if (_avatarImagesAsByteArrays.Count == 0)
		{
			return new AvatarData();
		}
		return GetForcedPlayerAvatar(AvatarServices.GetForcedAvatarIndexOfPlayer(playerId));
	}

	private AvatarData GetForcedPlayerAvatar(int forcedIndex)
	{
		return new AvatarData(_avatarImagesAsByteArrays[forcedIndex]);
	}

	public void Initialize()
	{
		if (_isInitialized)
		{
			return;
		}
		_avatarImagesAsByteArrays.Clear();
		if (Directory.Exists(_resourceFolder))
		{
			string[] files = Directory.GetFiles(_resourceFolder, "*.png");
			foreach (string path in files)
			{
				_avatarImagesAsByteArrays.Add(File.ReadAllBytes(path));
			}
		}
		_isInitialized = true;
	}

	public bool IsInitialized()
	{
		return _isInitialized;
	}

	public void ClearCache()
	{
		if (_isInitialized)
		{
			_avatarImagesAsByteArrays.Clear();
			_isInitialized = false;
		}
	}

	public void Tick(float dt)
	{
	}
}

using System.Collections.Generic;
using System.IO;
using TaleWorlds.Library;
using TaleWorlds.PlayerServices;
using TaleWorlds.PlayerServices.Avatar;

namespace TaleWorlds.PlatformService.Epic;

public class EpicPlatformAvatarService : IAvatarService
{
	private readonly string _resourceFolder = BasePath.Name + "Modules\\Native\\MultiplayerForcedAvatars\\";

	private readonly List<byte[]> _avatarImagesAsByteArrays;

	private bool _isInitialized;

	public EpicPlatformAvatarService()
	{
		_avatarImagesAsByteArrays = new List<byte[]>();
	}

	public AvatarData GetPlayerAvatar(PlayerId playerId)
	{
		int index = (int)((uint)playerId.Id2 % (uint)_avatarImagesAsByteArrays.Count);
		return new AvatarData(_avatarImagesAsByteArrays[index]);
	}

	public void Initialize()
	{
		if (!_isInitialized)
		{
			_avatarImagesAsByteArrays.Clear();
			string[] files = Directory.GetFiles(_resourceFolder, "*.png");
			foreach (string path in files)
			{
				_avatarImagesAsByteArrays.Add(File.ReadAllBytes(path));
			}
			_isInitialized = true;
		}
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

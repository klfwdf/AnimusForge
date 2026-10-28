using System.Collections.Generic;
using System.IO;
using TaleWorlds.Library;

namespace TaleWorlds.PlayerServices.Avatar;

public class GOGAvatarService : IAvatarService
{
	private readonly Dictionary<ulong, AvatarData> _avatarImageCache = new Dictionary<ulong, AvatarData>();

	private readonly string _resourceFolder = BasePath.Name + "Modules/Native/MultiplayerForcedAvatars/";

	private readonly List<byte[]> _avatarImagesAsByteArrays = new List<byte[]>();

	private bool _isInitalized;

	public void Initialize()
	{
		if (!_isInitalized)
		{
			string[] files = Directory.GetFiles(_resourceFolder, "*.png");
			foreach (string path in files)
			{
				_avatarImagesAsByteArrays.Add(File.ReadAllBytes(path));
			}
			_isInitalized = true;
		}
	}

	public void ClearCache()
	{
		if (_isInitalized)
		{
			_avatarImageCache.Clear();
			_avatarImagesAsByteArrays.Clear();
			_isInitalized = false;
		}
	}

	public AvatarData GetPlayerAvatar(PlayerId playerId)
	{
		int index = (int)((uint)playerId.Id2 % (uint)_avatarImagesAsByteArrays.Count);
		return new AvatarData(_avatarImagesAsByteArrays[index]);
	}

	public bool IsInitialized()
	{
		return _isInitalized;
	}

	public void Tick(float dt)
	{
	}
}

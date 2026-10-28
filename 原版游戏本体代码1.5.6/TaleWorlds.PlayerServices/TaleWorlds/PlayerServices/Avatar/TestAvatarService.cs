using System.Collections.Generic;
using System.IO;
using TaleWorlds.Library;

namespace TaleWorlds.PlayerServices.Avatar;

public class TestAvatarService : IAvatarService
{
	private readonly Dictionary<ulong, AvatarData> _avatarImageCache;

	private readonly string _resourceFolder = BasePath.Name + "Modules/Native/MultiplayerTestAvatars/";

	private readonly List<byte[]> _avatarImagesAsByteArrays;

	private bool _isInitialized;

	public TestAvatarService()
	{
		_avatarImageCache = new Dictionary<ulong, AvatarData>();
		_avatarImagesAsByteArrays = new List<byte[]>();
	}

	public void ClearCache()
	{
		if (_isInitialized)
		{
			_avatarImageCache.Clear();
			_avatarImagesAsByteArrays.Clear();
			_isInitialized = false;
		}
	}

	public AvatarData GetPlayerAvatar(PlayerId playerId)
	{
		if (_avatarImagesAsByteArrays.Count == 0)
		{
			return new AvatarData();
		}
		int index = (int)((uint)playerId.Id2 % (uint)_avatarImagesAsByteArrays.Count);
		return new AvatarData(_avatarImagesAsByteArrays[index]);
	}

	public void Initialize()
	{
		if (_isInitialized)
		{
			return;
		}
		if (Directory.Exists(_resourceFolder))
		{
			string[] files = Directory.GetFiles(_resourceFolder, "*.jpg");
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

	public void Tick(float dt)
	{
	}
}

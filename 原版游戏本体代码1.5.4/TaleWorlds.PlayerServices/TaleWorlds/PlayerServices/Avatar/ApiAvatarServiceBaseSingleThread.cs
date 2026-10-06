using System.Collections.Generic;
using System.Threading.Tasks;

namespace TaleWorlds.PlayerServices.Avatar;

public abstract class ApiAvatarServiceBaseSingleThread : IAvatarService
{
	protected Dictionary<ulong, AvatarData> AvatarImageCache { get; }

	protected List<(ulong accountId, AvatarData avatarData)> WaitingAccounts { get; set; }

	protected List<(ulong accountId, AvatarData avatarData)> InProgressAccounts { get; set; }

	protected Task FetchAvatarsTask { get; set; }

	protected ApiAvatarServiceBaseSingleThread()
	{
		AvatarImageCache = new Dictionary<ulong, AvatarData>();
		WaitingAccounts = new List<(ulong, AvatarData)>();
		InProgressAccounts = new List<(ulong, AvatarData)>();
		FetchAvatarsTask = null;
	}

	public void Tick(float dt)
	{
		CheckWaitingAccounts();
	}

	public AvatarData GetPlayerAvatar(PlayerId playerId)
	{
		ulong part = playerId.Part4;
		if (AvatarImageCache.TryGetValue(part, out var value) && value.Status != AvatarData.DataStatus.Failed)
		{
			return value;
		}
		if (AvatarImageCache.Count > 300)
		{
			AvatarImageCache.Clear();
		}
		value = new AvatarData();
		AvatarImageCache[part] = value;
		WaitingAccounts.Add((part, value));
		return value;
	}

	private async void CheckWaitingAccounts()
	{
		if (FetchAvatarsTask != null || WaitingAccounts.Count <= 0)
		{
			return;
		}
		FetchAvatarsTask = FetchAvatars();
		await FetchAvatarsTask;
		if (FetchAvatarsTask.IsFaulted)
		{
			FetchAvatarsTask = null;
			foreach (var inProgressAccount in InProgressAccounts)
			{
				AvatarData item = inProgressAccount.avatarData;
				if (item.Status == AvatarData.DataStatus.NotReady)
				{
					item.SetFailed();
				}
			}
		}
		if (FetchAvatarsTask != null)
		{
			FetchAvatarsTask.Dispose();
			FetchAvatarsTask = null;
		}
		InProgressAccounts.Clear();
	}

	protected abstract Task FetchAvatars();

	public void Initialize()
	{
	}

	public void ClearCache()
	{
		AvatarImageCache.Clear();
		WaitingAccounts.Clear();
	}

	public bool IsInitialized()
	{
		return true;
	}
}

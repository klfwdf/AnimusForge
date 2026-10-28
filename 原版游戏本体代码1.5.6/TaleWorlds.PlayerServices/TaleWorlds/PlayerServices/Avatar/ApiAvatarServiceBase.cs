using System.Collections.Generic;
using System.Threading.Tasks;

namespace TaleWorlds.PlayerServices.Avatar;

public abstract class ApiAvatarServiceBase : IAvatarService
{
	protected Dictionary<ulong, AvatarData> AvatarImageCache { get; }

	protected List<(ulong accountId, AvatarData avatarData)> WaitingAccounts { get; set; }

	protected List<(ulong accountId, AvatarData avatarData)> InProgressAccounts { get; set; }

	protected Task FetchAvatarsTask { get; set; }

	protected ApiAvatarServiceBase()
	{
		AvatarImageCache = new Dictionary<ulong, AvatarData>();
		WaitingAccounts = new List<(ulong, AvatarData)>();
		InProgressAccounts = new List<(ulong, AvatarData)>();
		FetchAvatarsTask = null;
	}

	public void Tick(float dt)
	{
	}

	public AvatarData GetPlayerAvatar(PlayerId playerId)
	{
		ulong part = playerId.Part4;
		AvatarData value;
		lock (AvatarImageCache)
		{
			if (AvatarImageCache.TryGetValue(part, out value) && value.Status != AvatarData.DataStatus.Failed)
			{
				return value;
			}
			if (AvatarImageCache.Count > 300)
			{
				AvatarImageCache.Clear();
			}
			value = new AvatarData();
			AvatarImageCache[part] = value;
		}
		lock (WaitingAccounts)
		{
			WaitingAccounts.Add((part, value));
		}
		CheckWaitingAccounts();
		return value;
	}

	private void CheckWaitingAccounts()
	{
		lock (WaitingAccounts)
		{
			if (FetchAvatarsTask != null && !FetchAvatarsTask.IsCompleted)
			{
				return;
			}
			Task fetchAvatarsTask = FetchAvatarsTask;
			if (fetchAvatarsTask != null && fetchAvatarsTask.IsFaulted)
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
			if (WaitingAccounts.Count > 0)
			{
				FetchAvatarsTask = FetchAvatars();
				Task.Run(async delegate
				{
					await FetchAvatarsTask;
					CheckWaitingAccounts();
				});
			}
		}
	}

	protected abstract Task FetchAvatars();

	public void Initialize()
	{
	}

	public void ClearCache()
	{
		lock (AvatarImageCache)
		{
			AvatarImageCache.Clear();
		}
		lock (WaitingAccounts)
		{
			WaitingAccounts.Clear();
		}
	}

	public bool IsInitialized()
	{
		return true;
	}
}

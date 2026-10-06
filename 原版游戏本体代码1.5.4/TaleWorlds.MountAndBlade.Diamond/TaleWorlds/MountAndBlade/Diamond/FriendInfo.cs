using System;
using TaleWorlds.PlayerServices;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class FriendInfo
{
	public PlayerId Id { get; set; }

	public FriendStatus Status { get; set; }

	public string Name { get; set; }

	public bool IsOnline { get; set; }
}

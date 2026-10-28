using System;

namespace TaleWorlds.MountAndBlade.Diamond;

[Flags]
public enum Features
{
	None = 0,
	Matchmaking = 1,
	CustomGame = 2,
	Party = 4,
	Clan = 8,
	BannerlordFriendList = 0x10,
	TextChat = 0x20,
	All = -1
}

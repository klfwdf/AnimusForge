namespace TaleWorlds.MountAndBlade.Diamond.MultiplayerBadges;

public abstract class GameBadgeTracker
{
	public virtual void OnPlayerJoin(PlayerData playerData)
	{
	}

	public virtual void OnKill(KillData killData)
	{
	}

	public virtual void OnStartingNextBattle()
	{
	}
}

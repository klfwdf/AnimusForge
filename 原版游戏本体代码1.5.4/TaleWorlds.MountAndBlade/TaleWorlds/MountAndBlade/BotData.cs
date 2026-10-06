namespace TaleWorlds.MountAndBlade;

public class BotData
{
	public int AliveCount;

	public int KillCount;

	public int DeathCount;

	public int AssistCount;

	public int Score => KillCount * 3 + AssistCount;

	public bool IsAnyValid
	{
		get
		{
			if (KillCount == 0 && DeathCount == 0 && AssistCount == 0)
			{
				return AliveCount != 0;
			}
			return true;
		}
	}

	public BotData()
	{
	}

	public BotData(int kill, int assist, int death, int alive)
	{
		KillCount = kill;
		DeathCount = death;
		AssistCount = assist;
		AliveCount = alive;
	}

	public void ResetKillDeathAssist()
	{
		KillCount = 0;
		DeathCount = 0;
		AssistCount = 0;
	}
}

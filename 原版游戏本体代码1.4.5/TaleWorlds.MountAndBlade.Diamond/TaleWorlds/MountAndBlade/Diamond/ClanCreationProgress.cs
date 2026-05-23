using System;

namespace TaleWorlds.MountAndBlade.Diamond;

[Serializable]
public class ClanCreationProgress
{
	public Progress Progress
	{
		get
		{
			int num = 0;
			int num2 = 0;
			ClanCreationPlayerData[] clanCreationPlayerData = ClanCreationPlayerData;
			foreach (ClanCreationPlayerData clanCreationPlayerData2 in clanCreationPlayerData)
			{
				if (clanCreationPlayerData2.ClanCreationAnswer == ClanCreationAnswer.Accepted)
				{
					num++;
				}
				else if (clanCreationPlayerData2.ClanCreationAnswer == ClanCreationAnswer.Declined)
				{
					num2++;
				}
			}
			if (num == ClanCreationPlayerData.Length)
			{
				return Progress.Success;
			}
			if (num2 > 0)
			{
				return Progress.Fail;
			}
			return Progress.Undecided;
		}
	}

	public ClanCreationPlayerData[] ClanCreationPlayerData { get; private set; }

	public ClanCreationProgress(ClanCreationPlayerData[] clanCreationPlayerData)
	{
		ClanCreationPlayerData = clanCreationPlayerData;
	}
}

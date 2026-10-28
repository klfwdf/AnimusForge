using System.Collections.Generic;
using NetworkMessages.FromServer;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.MissionRepresentatives;

public class FlagDominationMissionRepresentative : MissionRepresentativeBase
{
	private bool _survivedLastRound;

	private bool Forfeited => base.Gold < 0;

	public int GetGoldAmountForVisual()
	{
		if (base.Gold < 0)
		{
			return 80;
		}
		return base.Gold;
	}

	public void UpdateSelectedClassServer(Agent agent)
	{
		_survivedLastRound = agent != null;
	}

	public bool CheckIfSurvivedLastRoundAndReset()
	{
		bool survivedLastRound = _survivedLastRound;
		_survivedLastRound = false;
		return survivedLastRound;
	}

	public int GetGoldGainsFromKillData(MPPerkObject.MPPerkHandler killerPerkHandler, MPPerkObject.MPPerkHandler assistingHitterPerkHandler, MultiplayerClassDivisions.MPHeroClass victimClass, bool isAssist, bool isFriendly)
	{
		if (isFriendly || Forfeited)
		{
			return 0;
		}
		int num = 0;
		if (isAssist)
		{
			num = (killerPerkHandler?.GetRewardedGoldOnAssist() ?? 0) + (assistingHitterPerkHandler?.GetGoldOnAssist() ?? 0);
		}
		else
		{
			int num2 = ((base.ControlledAgent != null) ? MultiplayerClassDivisions.GetMPHeroClassForCharacter(base.ControlledAgent.Character).TroopBattleCost : 0);
			num = killerPerkHandler?.GetGoldOnKill(num2, victimClass.TroopBattleCost) ?? 0;
		}
		if (num > 0)
		{
			GameNetwork.BeginModuleEventAsServer(base.Peer);
			GameNetwork.WriteMessage(new GoldGain(new List<KeyValuePair<ushort, int>>
			{
				new KeyValuePair<ushort, int>(2048, num)
			}));
			GameNetwork.EndModuleEventAsServer();
		}
		return num;
	}

	public int GetGoldGainFromKillDataAndUpdateFlags(MultiplayerClassDivisions.MPHeroClass victimClass, bool isAssist)
	{
		int num = 0;
		int num2 = 50;
		List<KeyValuePair<ushort, int>> list = new List<KeyValuePair<ushort, int>>();
		if (base.ControlledAgent != null)
		{
			num2 += victimClass.TroopBattleCost - MultiplayerClassDivisions.GetMPHeroClassForCharacter(base.ControlledAgent.Character).TroopBattleCost / 2;
		}
		if (isAssist)
		{
			int num3 = MathF.Max(5, num2 / 10);
			num += num3;
			list.Add(new KeyValuePair<ushort, int>(256, num3));
		}
		else if (base.ControlledAgent != null)
		{
			int num4 = MathF.Max(10, num2 / 5);
			num += num4;
			list.Add(new KeyValuePair<ushort, int>(128, num4));
		}
		if (list.Count > 0 && !base.Peer.Communicator.IsServerPeer && base.Peer.Communicator.IsConnectionActive)
		{
			GameNetwork.BeginModuleEventAsServer(base.Peer);
			GameNetwork.WriteMessage(new GoldGain(list));
			GameNetwork.EndModuleEventAsServer();
		}
		return num;
	}

	public int GetGoldGainsFromAllyDeathReward(int baseAmount)
	{
		if (Forfeited)
		{
			return 0;
		}
		if (baseAmount > 0 && !base.Peer.Communicator.IsServerPeer && base.Peer.Communicator.IsConnectionActive)
		{
			GameNetwork.BeginModuleEventAsServer(base.Peer);
			GameNetwork.WriteMessage(new GoldGain(new List<KeyValuePair<ushort, int>>
			{
				new KeyValuePair<ushort, int>(2048, baseAmount)
			}));
			GameNetwork.EndModuleEventAsServer();
		}
		return baseAmount;
	}
}

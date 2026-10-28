using System.Collections.Generic;
using NetworkMessages.FromServer;
using TaleWorlds.Engine;
using TaleWorlds.Library;

namespace TaleWorlds.MountAndBlade.MissionRepresentatives;

public class SiegeMissionRepresentative : MissionRepresentativeBase
{
	private const int FirstRangedKillGold = 10;

	private const int FirstMeleeKillGold = 10;

	private const int FirstAssistGold = 10;

	private const int SecondAssistGold = 10;

	private const int ThirdAssistGold = 10;

	private const int FifthKillGold = 20;

	private const int TenthKillGold = 30;

	private GoldGainFlags _currentGoldGains;

	private int _killCountOnSpawn;

	private int _assistCountOnSpawn;

	public override void OnAgentSpawned()
	{
		_currentGoldGains = (GoldGainFlags)0;
		_killCountOnSpawn = base.MissionPeer.KillCount;
		_assistCountOnSpawn = base.MissionPeer.AssistCount;
	}

	public int GetGoldGainsFromKillDataAndUpdateFlags(MPPerkObject.MPPerkHandler killerPerkHandler, MPPerkObject.MPPerkHandler assistingHitterPerkHandler, MultiplayerClassDivisions.MPHeroClass victimClass, bool isAssist, bool isRanged, bool isFriendly)
	{
		int num = 0;
		List<KeyValuePair<ushort, int>> list = new List<KeyValuePair<ushort, int>>();
		if (isAssist)
		{
			int num2 = 1;
			if (!isFriendly)
			{
				int num3 = (killerPerkHandler?.GetRewardedGoldOnAssist() ?? 0) + (assistingHitterPerkHandler?.GetGoldOnAssist() ?? 0);
				if (num3 > 0)
				{
					num += num3;
					_currentGoldGains |= GoldGainFlags.PerkBonus;
					list.Add(new KeyValuePair<ushort, int>(2048, num3));
				}
			}
			switch (base.MissionPeer.AssistCount - _assistCountOnSpawn)
			{
			case 1:
				num += 10;
				_currentGoldGains |= GoldGainFlags.FirstAssist;
				list.Add(new KeyValuePair<ushort, int>(4, 10));
				break;
			case 2:
				num += 10;
				_currentGoldGains |= GoldGainFlags.SecondAssist;
				list.Add(new KeyValuePair<ushort, int>(8, 10));
				break;
			case 3:
				num += 10;
				_currentGoldGains |= GoldGainFlags.ThirdAssist;
				list.Add(new KeyValuePair<ushort, int>(16, 10));
				break;
			default:
				num += num2;
				list.Add(new KeyValuePair<ushort, int>(256, num2));
				break;
			}
		}
		else
		{
			int num4 = 0;
			if (base.ControlledAgent != null)
			{
				num4 = MultiplayerClassDivisions.GetMPHeroClassForCharacter(base.ControlledAgent.Character).TroopCasualCost;
				int num5 = victimClass.TroopCasualCost - num4;
				int num6 = 2 + MathF.Max(0, num5 / 2);
				num += num6;
				list.Add(new KeyValuePair<ushort, int>(128, num6));
			}
			int num7 = killerPerkHandler?.GetGoldOnKill(num4, victimClass.TroopCasualCost) ?? 0;
			if (num7 > 0)
			{
				num += num7;
				_currentGoldGains |= GoldGainFlags.PerkBonus;
				list.Add(new KeyValuePair<ushort, int>(2048, num7));
			}
			switch (base.MissionPeer.KillCount - _killCountOnSpawn)
			{
			case 5:
				num += 20;
				_currentGoldGains |= GoldGainFlags.FifthKill;
				list.Add(new KeyValuePair<ushort, int>(32, 20));
				break;
			case 10:
				num += 30;
				_currentGoldGains |= GoldGainFlags.TenthKill;
				list.Add(new KeyValuePair<ushort, int>(64, 30));
				break;
			}
			if (isRanged && !_currentGoldGains.HasAnyFlag(GoldGainFlags.FirstRangedKill))
			{
				num += 10;
				_currentGoldGains |= GoldGainFlags.FirstRangedKill;
				list.Add(new KeyValuePair<ushort, int>(1, 10));
			}
			if (!isRanged && !_currentGoldGains.HasAnyFlag(GoldGainFlags.FirstMeleeKill))
			{
				num += 10;
				_currentGoldGains |= GoldGainFlags.FirstMeleeKill;
				list.Add(new KeyValuePair<ushort, int>(2, 10));
			}
		}
		int num8 = 0;
		if (base.MissionPeer.Team == Mission.Current.Teams.Attacker)
		{
			num8 = MultiplayerOptions.OptionType.GoldGainChangePercentageTeam1.GetIntValue();
		}
		else if (base.MissionPeer.Team == Mission.Current.Teams.Defender)
		{
			num8 = MultiplayerOptions.OptionType.GoldGainChangePercentageTeam2.GetIntValue();
		}
		if (num8 != 0 && (num > 0 || list.Count > 0))
		{
			num = 0;
			float num9 = 1f + (float)num8 * 0.01f;
			for (int i = 0; i < list.Count; i++)
			{
				int num10 = (int)((float)list[i].Value * num9);
				list[i] = new KeyValuePair<ushort, int>(list[i].Key, num10);
				num += num10;
			}
		}
		if (list.Count > 0 && !base.Peer.Communicator.IsServerPeer && base.Peer.Communicator.IsConnectionActive)
		{
			GameNetwork.BeginModuleEventAsServer(base.Peer);
			GameNetwork.WriteMessage(new GoldGain(list));
			GameNetwork.EndModuleEventAsServer();
		}
		return num;
	}

	public int GetGoldGainsFromObjectiveAssist(GameEntity objectiveMostParentEntity, float contributionRatio, bool isCompleted)
	{
		int num = (int)(contributionRatio * (float)GetTotalGoldDistributionForDestructable(objectiveMostParentEntity));
		if (num > 0 && !base.Peer.Communicator.IsServerPeer && base.Peer.Communicator.IsConnectionActive)
		{
			GameNetwork.BeginModuleEventAsServer(base.Peer);
			GameNetwork.WriteMessage(new GoldGain(new List<KeyValuePair<ushort, int>>
			{
				new KeyValuePair<ushort, int>((ushort)(isCompleted ? 512 : 1024), num)
			}));
			GameNetwork.EndModuleEventAsServer();
		}
		return num;
	}

	public int GetGoldGainsFromAllyDeathReward(int baseAmount)
	{
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

	private int GetTotalGoldDistributionForDestructable(GameEntity objectiveMostParentEntity)
	{
		string text = null;
		string[] tags = objectiveMostParentEntity.Tags;
		foreach (string text2 in tags)
		{
			if (text2.StartsWith("mp_siege_objective_"))
			{
				text = text2;
				break;
			}
		}
		if (text == null)
		{
			return 20;
		}
		switch (text.Replace("mp_siege_objective_", ""))
		{
		case "wall_breach":
		case "castle_gate":
			return 500;
		case "battering_ram":
		case "siege_tower":
			return 600;
		default:
			return 20;
		}
	}
}

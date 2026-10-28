using System.Collections.Generic;
using System.Linq;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade.MissionRepresentatives;
using TaleWorlds.MountAndBlade.Missions.Multiplayer;
using TaleWorlds.ObjectSystem;

namespace TaleWorlds.MountAndBlade;

public class MissionMultiplayerTeamDeathmatch : MissionMultiplayerGameModeBase
{
	public const int MaxScoreToEndMatch = 1023000;

	private const int FirstSpawnGold = 120;

	private const int RespawnGold = 100;

	private MissionScoreboardComponent _missionScoreboardComponent;

	public override bool IsGameModeHidingAllAgentVisuals => true;

	public override bool IsGameModeUsingOpposingTeams => true;

	public override MultiplayerGameType GetMissionType()
	{
		return MultiplayerGameType.TeamDeathmatch;
	}

	public override void OnBehaviorInitialize()
	{
		base.OnBehaviorInitialize();
		_missionScoreboardComponent = base.Mission.GetMissionBehavior<MissionScoreboardComponent>();
	}

	public override void AfterStart()
	{
		string strValue = MultiplayerOptions.OptionType.CultureTeam1.GetStrValue();
		string strValue2 = MultiplayerOptions.OptionType.CultureTeam2.GetStrValue();
		BasicCultureObject basicCultureObject = MBObjectManager.Instance.GetObject<BasicCultureObject>(strValue);
		BasicCultureObject basicCultureObject2 = MBObjectManager.Instance.GetObject<BasicCultureObject>(strValue2);
		MultiplayerBattleColors multiplayerBattleColors = MultiplayerBattleColors.CreateWith(basicCultureObject, basicCultureObject2);
		Banner banner = new Banner(basicCultureObject.Banner, multiplayerBattleColors.AttackerColors.BannerBackgroundColorUint, multiplayerBattleColors.AttackerColors.BannerForegroundColorUint);
		Banner banner2 = new Banner(basicCultureObject2.Banner, multiplayerBattleColors.DefenderColors.BannerBackgroundColorUint, multiplayerBattleColors.DefenderColors.BannerForegroundColorUint);
		base.Mission.Teams.Add(BattleSideEnum.Attacker, multiplayerBattleColors.AttackerColors.BannerBackgroundColorUint, multiplayerBattleColors.AttackerColors.BannerForegroundColorUint, banner);
		base.Mission.Teams.Add(BattleSideEnum.Defender, multiplayerBattleColors.DefenderColors.BannerBackgroundColorUint, multiplayerBattleColors.DefenderColors.BannerForegroundColorUint, banner2);
	}

	protected override void HandleEarlyNewClientAfterLoadingFinished(NetworkCommunicator networkPeer)
	{
		networkPeer.AddComponent<TeamDeathmatchMissionRepresentative>();
	}

	protected override void HandleNewClientAfterSynchronized(NetworkCommunicator networkPeer)
	{
		ChangeCurrentGoldForPeer(networkPeer.GetComponent<MissionPeer>(), 120);
		GameModeBaseClient.OnGoldAmountChangedForRepresentative(networkPeer.GetComponent<TeamDeathmatchMissionRepresentative>(), 120);
	}

	public override void OnPeerChangedTeam(NetworkCommunicator peer, Team oldTeam, Team newTeam)
	{
		if (oldTeam != null && oldTeam != newTeam && oldTeam.Side != BattleSideEnum.None)
		{
			ChangeCurrentGoldForPeer(peer.GetComponent<MissionPeer>(), 100);
		}
	}

	public override void OnAgentRemoved(Agent affectedAgent, Agent affectorAgent, AgentState agentState, KillingBlow blow)
	{
		if (blow.DamageType == DamageTypes.Invalid || (agentState != AgentState.Unconscious && agentState != AgentState.Killed) || !affectedAgent.IsHuman)
		{
			return;
		}
		if (affectorAgent != null)
		{
			if (affectorAgent.IsEnemyOf(affectedAgent))
			{
				_missionScoreboardComponent.ChangeTeamScore(affectorAgent.Team, GetScoreForKill(affectedAgent));
			}
			else
			{
				_missionScoreboardComponent.ChangeTeamScore(affectedAgent.Team, -GetScoreForKill(affectedAgent));
			}
		}
		MissionPeer missionPeer = affectedAgent.MissionPeer;
		if (missionPeer != null)
		{
			int num = 100;
			if (affectorAgent != affectedAgent)
			{
				List<MissionPeer>[] array = new List<MissionPeer>[2];
				for (int i = 0; i < array.Length; i++)
				{
					array[i] = new List<MissionPeer>();
				}
				foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
				{
					MissionPeer component = networkPeer.GetComponent<MissionPeer>();
					if (component != null && component.Team != null && component.Team.Side != BattleSideEnum.None)
					{
						array[(int)component.Team.Side].Add(component);
					}
				}
				int num2 = array[1].Count - array[0].Count;
				BattleSideEnum battleSideEnum = ((num2 == 0) ? BattleSideEnum.None : ((num2 < 0) ? BattleSideEnum.Attacker : BattleSideEnum.Defender));
				if (battleSideEnum != BattleSideEnum.None && battleSideEnum == missionPeer.Team.Side)
				{
					num2 = MathF.Abs(num2);
					int count = array[(int)battleSideEnum].Count;
					if (count > 0)
					{
						int num3 = num * num2 / 10 / count * 10;
						num += num3;
					}
				}
			}
			ChangeCurrentGoldForPeer(missionPeer, missionPeer.Representative.Gold + num);
		}
		bool isFriendly = affectorAgent?.Team != null && affectedAgent.Team != null && affectorAgent.Team.Side == affectedAgent.Team.Side;
		MultiplayerClassDivisions.MPHeroClass mPHeroClassForCharacter = MultiplayerClassDivisions.GetMPHeroClassForCharacter(affectedAgent.Character);
		Agent.Hitter assistingHitter = affectedAgent.GetAssistingHitter(affectorAgent?.MissionPeer);
		if (affectorAgent?.MissionPeer != null && affectorAgent != affectedAgent && !affectorAgent.IsFriendOf(affectedAgent))
		{
			TeamDeathmatchMissionRepresentative teamDeathmatchMissionRepresentative = affectorAgent.MissionPeer.Representative as TeamDeathmatchMissionRepresentative;
			int goldGainsFromKillDataAndUpdateFlags = teamDeathmatchMissionRepresentative.GetGoldGainsFromKillDataAndUpdateFlags(MPPerkObject.GetPerkHandler(affectorAgent.MissionPeer), MPPerkObject.GetPerkHandler(assistingHitter?.HitterPeer), mPHeroClassForCharacter, isAssist: false, blow.IsMissile, isFriendly);
			ChangeCurrentGoldForPeer(affectorAgent.MissionPeer, teamDeathmatchMissionRepresentative.Gold + goldGainsFromKillDataAndUpdateFlags);
		}
		if (assistingHitter?.HitterPeer != null && !assistingHitter.IsFriendlyHit)
		{
			TeamDeathmatchMissionRepresentative teamDeathmatchMissionRepresentative2 = assistingHitter.HitterPeer.Representative as TeamDeathmatchMissionRepresentative;
			int goldGainsFromKillDataAndUpdateFlags2 = teamDeathmatchMissionRepresentative2.GetGoldGainsFromKillDataAndUpdateFlags(MPPerkObject.GetPerkHandler(affectorAgent?.MissionPeer), MPPerkObject.GetPerkHandler(assistingHitter.HitterPeer), mPHeroClassForCharacter, isAssist: true, blow.IsMissile, isFriendly);
			ChangeCurrentGoldForPeer(assistingHitter.HitterPeer, teamDeathmatchMissionRepresentative2.Gold + goldGainsFromKillDataAndUpdateFlags2);
		}
		if (missionPeer?.Team == null)
		{
			return;
		}
		IEnumerable<(MissionPeer, int)> enumerable = MPPerkObject.GetPerkHandler(missionPeer)?.GetTeamGoldRewardsOnDeath();
		if (enumerable == null)
		{
			return;
		}
		foreach (var (missionPeer2, baseAmount) in enumerable)
		{
			if (missionPeer2?.Representative is TeamDeathmatchMissionRepresentative teamDeathmatchMissionRepresentative3)
			{
				int goldGainsFromAllyDeathReward = teamDeathmatchMissionRepresentative3.GetGoldGainsFromAllyDeathReward(baseAmount);
				if (goldGainsFromAllyDeathReward > 0)
				{
					ChangeCurrentGoldForPeer(missionPeer2, teamDeathmatchMissionRepresentative3.Gold + goldGainsFromAllyDeathReward);
				}
			}
		}
	}

	public override bool CheckForMatchEnd()
	{
		int minScoreToWinMatch = MultiplayerOptions.OptionType.MinScoreToWinMatch.GetIntValue();
		return _missionScoreboardComponent.Sides.Any((MissionScoreboardComponent.MissionScoreboardSide side) => side.SideScore >= minScoreToWinMatch);
	}

	public override Team GetWinnerTeam()
	{
		int intValue = MultiplayerOptions.OptionType.MinScoreToWinMatch.GetIntValue();
		Team result = null;
		MissionScoreboardComponent.MissionScoreboardSide[] sides = _missionScoreboardComponent.Sides;
		if (sides[1].SideScore < intValue && sides[0].SideScore >= intValue)
		{
			result = base.Mission.Teams.Defender;
		}
		if (sides[0].SideScore < intValue && sides[1].SideScore >= intValue)
		{
			result = base.Mission.Teams.Attacker;
		}
		return result;
	}
}

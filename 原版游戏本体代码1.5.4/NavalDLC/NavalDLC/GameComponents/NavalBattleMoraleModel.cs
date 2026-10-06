using Helpers;
using NavalDLC.CharacterDevelopment;
using NavalDLC.Missions.MissionLogics;
using NavalDLC.Missions.Objects;
using SandBox.GameComponents;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace NavalDLC.GameComponents;

public class NavalBattleMoraleModel : BattleMoraleModel
{
	private NavalShipsLogic GetNavalShipsLogic()
	{
		return Mission.Current.GetMissionBehavior<NavalShipsLogic>();
	}

	public override (float affectedSideMaxMoraleLoss, float affectorSideMaxMoraleGain) CalculateMaxMoraleChangeDueToAgentIncapacitated(Agent affectedAgent, AgentState affectedAgentState, Agent affectorAgent, in KillingBlow killingBlow)
	{
		float casualtiesFactor = CalculateCasualtiesFactor(affectedAgent.Team?.Side ?? BattleSideEnum.None);
		var (bonuses, explainedNumber) = SandboxBattleMoraleModel.CalculateMaxMoraleChangeDueToAgentIncapacitatedExplained(affectedAgent, affectedAgentState, affectorAgent, in killingBlow, casualtiesFactor);
		if (affectorAgent?.Character is CharacterObject && affectorAgent?.Formation?.Captain?.Character is CharacterObject captainCharacter)
		{
			PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Mariner.TerrorOfTheSeas, affectorAgent.CurrentBattleEnvironment, captainCharacter, ref bonuses);
		}
		return (affectedSideMaxMoraleLoss: MathF.Max(bonuses.ResultNumber, 0f), affectorSideMaxMoraleGain: MathF.Max(explainedNumber.ResultNumber, 0f));
	}

	public override (float affectedSideMaxMoraleLoss, float affectorSideMaxMoraleGain) CalculateMaxMoraleChangeDueToAgentPanicked(Agent agent)
	{
		return base.BaseModel.CalculateMaxMoraleChangeDueToAgentPanicked(agent);
	}

	public override float CalculateMoraleChangeToCharacter(Agent agent, float maxMoraleChange)
	{
		return base.BaseModel.CalculateMoraleChangeToCharacter(agent, maxMoraleChange);
	}

	public override float GetEffectiveInitialMorale(Agent agent, float baseMorale)
	{
		ExplainedNumber stat = SandboxBattleMoraleModel.GetEffectiveInitialMoraleExplained(agent, baseMorale);
		PartyBase partyBase = (PartyBase)(agent?.Origin?.BattleCombatant);
		MobileParty mobileParty = ((partyBase != null && partyBase.IsMobile) ? partyBase.MobileParty : null);
		CharacterObject characterObject = agent?.Character as CharacterObject;
		bool flag = false;
		Ship ship = null;
		if (mobileParty != null && characterObject != null)
		{
			BattleEnvironment currentBattleEnvironment = agent.CurrentBattleEnvironment;
			CharacterObject characterObject2 = mobileParty.Army?.LeaderParty?.LeaderHero?.CharacterObject;
			CharacterObject characterObject3 = mobileParty.LeaderHero?.CharacterObject;
			CharacterObject characterObject4 = agent.Formation?.Captain?.Character as CharacterObject;
			if (characterObject == characterObject4)
			{
				characterObject4 = null;
			}
			if (partyBase != null && partyBase.Ships?.Count > 0)
			{
				ship = partyBase.FlagShip;
				Figurehead figurehead = ship?.Figurehead;
				flag = characterObject2 != null && characterObject2.GetPerkValue(NavalPerks.Shipmaster.Commodore) && ship != null && figurehead != null;
				if (flag && figurehead == DefaultFigureheads.Lion)
				{
					stat.Add(figurehead.EffectAmount);
				}
			}
			characterObject2 = ((characterObject2 != characterObject) ? characterObject2 : null);
			characterObject3 = ((characterObject3 != characterObject) ? characterObject3 : null);
			if (characterObject3 != null)
			{
				if (Mission.Current.IsNavalBattle)
				{
					PerkHelper.AddPerkBonusForParty(NavalPerks.Mariner.RallyingCry, currentBattleEnvironment, mobileParty, isPrimaryBonus: true, ref stat);
					if (characterObject.IsMariner)
					{
						PerkHelper.AddPerkBonusForParty(NavalPerks.Mariner.AxeOfTheNorthwind, currentBattleEnvironment, mobileParty, isPrimaryBonus: false, ref stat);
					}
					else
					{
						PerkHelper.AddPerkBonusForParty(NavalPerks.Mariner.SunnyDisposition, currentBattleEnvironment, mobileParty, isPrimaryBonus: false, ref stat);
					}
				}
				if (characterObject3.IsHero && characterObject3.HeroObject.Clan?.Kingdom != null && characterObject3.HeroObject.Clan.Kingdom.HasPolicy(NavalPolicies.FraternalFleetDoctrine))
				{
					stat.AddFactor(0.2f, NavalPolicies.FraternalFleetDoctrine.Name);
				}
			}
		}
		NavalShipsLogic missionBehavior = Mission.Current.GetMissionBehavior<NavalShipsLogic>();
		if (missionBehavior != null)
		{
			foreach (MissionShip allShip in missionBehavior.AllShips)
			{
				Ship ship2 = allShip.ShipOrigin as Ship;
				if (!flag || ship2 != ship)
				{
					Figurehead figurehead2 = (allShip.ShipOrigin as Ship)?.Figurehead;
					if (figurehead2 != null && figurehead2 == DefaultFigureheads.Lion && allShip.GetIsAgentOnShip(agent))
					{
						stat.Add(figurehead2.EffectAmount);
					}
				}
			}
		}
		return stat.ResultNumber;
	}

	public override bool CanPanicDueToMorale(Agent agent)
	{
		return base.BaseModel.CanPanicDueToMorale(agent);
	}

	public override float CalculateCasualtiesFactor(BattleSideEnum battleSide)
	{
		return base.BaseModel.CalculateCasualtiesFactor(battleSide);
	}

	public override float GetAverageMorale(Formation formation)
	{
		return base.BaseModel.GetAverageMorale(formation);
	}

	public CharacterObject GetEnemyArmyLeaderCharacter(IShipOrigin shipOrigin)
	{
		GetNavalShipsLogic().FindAssignmentOfShipOrigin(shipOrigin, out var shipAssignment);
		Agent agent = shipAssignment?.Formation?.GetFirstUnit();
		if (agent != null)
		{
			foreach (Team team in Mission.Current.Teams)
			{
				if (team.IsEnemyOf(agent.Team) && team.ActiveAgents.Count > 0)
				{
					PartyBase partyBase = (PartyBase)(team.ActiveAgents[0]?.Origin?.BattleCombatant);
					return ((partyBase != null && partyBase.IsMobile) ? partyBase.MobileParty : null)?.Army?.LeaderParty?.LeaderHero?.CharacterObject;
				}
			}
		}
		return null;
	}

	public override float CalculateMoraleChangeOnShipSunk(IShipOrigin shipOrigin)
	{
		float num = base.BaseModel.CalculateMoraleChangeOnShipSunk(shipOrigin);
		CharacterObject enemyArmyLeaderCharacter = GetEnemyArmyLeaderCharacter(shipOrigin);
		if (enemyArmyLeaderCharacter != null && enemyArmyLeaderCharacter.GetPerkValue(NavalPerks.Mariner.EnemyOfTheWood))
		{
			num += NavalPerks.Mariner.EnemyOfTheWood.PrimaryBonus;
		}
		return num;
	}

	public override float CalculateMoraleOnRamming(Agent agent, IShipOrigin rammingShip, IShipOrigin rammedShip)
	{
		float baseNumber = base.BaseModel.CalculateMoraleOnRamming(agent, rammingShip, rammedShip);
		ExplainedNumber bonuses = new ExplainedNumber(baseNumber);
		CharacterObject characterObject = (agent.Formation?.Captain)?.Character as CharacterObject;
		if (agent?.Character == characterObject)
		{
			characterObject = null;
		}
		PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Shipmaster.ShockAndAwe, agent.CurrentBattleEnvironment, characterObject, ref bonuses);
		Figurehead figurehead = (rammingShip as Ship).Figurehead;
		if (figurehead != null && figurehead == DefaultFigureheads.Ram)
		{
			bonuses.AddFactor(figurehead.EffectAmount);
		}
		return bonuses.ResultNumber;
	}

	public override float CalculateMoraleOnShipsConnected(Agent agent, IShipOrigin ownerShip, IShipOrigin targetShip)
	{
		float baseNumber = base.BaseModel.CalculateMoraleOnShipsConnected(agent, ownerShip, targetShip);
		ExplainedNumber explainedNumber = new ExplainedNumber(baseNumber);
		Figurehead figurehead = (ownerShip as Ship).Figurehead;
		if (figurehead != null && figurehead == DefaultFigureheads.Dragon)
		{
			explainedNumber.Add(figurehead.EffectAmount);
		}
		return explainedNumber.ResultNumber;
	}
}

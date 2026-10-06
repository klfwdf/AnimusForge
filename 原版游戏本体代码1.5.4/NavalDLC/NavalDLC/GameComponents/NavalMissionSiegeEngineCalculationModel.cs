using Helpers;
using NavalDLC.CharacterDevelopment;
using NavalDLC.Missions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace NavalDLC.GameComponents;

public class NavalMissionSiegeEngineCalculationModel : MissionSiegeEngineCalculationModel
{
	public override float CalculateReloadSpeed(Agent userAgent, float baseSpeed)
	{
		float baseNumber = base.BaseModel.CalculateReloadSpeed(userAgent, baseSpeed);
		ExplainedNumber bonuses = new ExplainedNumber(baseNumber);
		if (Mission.Current.IsNavalBattle)
		{
			CharacterObject characterObject = (userAgent?.Formation?.Captain)?.Character as CharacterObject;
			if (userAgent?.Character == characterObject)
			{
				characterObject = null;
			}
			if (characterObject != null)
			{
				PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Boatswain.StreamlinedOperations, userAgent.CurrentBattleEnvironment, characterObject, ref bonuses);
			}
			AgentNavalComponent agentNavalComponent = userAgent?.GetComponent<AgentNavalComponent>();
			if (agentNavalComponent != null && agentNavalComponent.SteppedShip != null)
			{
				Figurehead figurehead = (agentNavalComponent.SteppedShip.ShipOrigin as Ship).Figurehead;
				if (figurehead != null && figurehead == DefaultFigureheads.Viper)
				{
					bonuses.AddFactor(figurehead.EffectAmount);
				}
			}
		}
		return bonuses.ResultNumber;
	}

	public override int CalculateShipSiegeWeaponAmmoCount(IShipOrigin shipOrigin, Agent captain, RangedSiegeWeapon weapon)
	{
		ExplainedNumber bonuses = new ExplainedNumber(weapon.AmmoCount);
		if (captain?.Character is CharacterObject captainCharacter && weapon is Ballista)
		{
			PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Boatswain.SmoothOperator, captain.CurrentBattleEnvironment, captainCharacter, ref bonuses);
		}
		return MathF.Ceiling(bonuses.ResultNumber);
	}

	public override int CalculateDamage(Agent attackerAgent, float baseDamage)
	{
		int num = base.BaseModel.CalculateDamage(attackerAgent, baseDamage);
		ExplainedNumber bonuses = new ExplainedNumber(num);
		if (attackerAgent.IsHero && attackerAgent.Character is CharacterObject character)
		{
			PerkHelper.AddPerkBonusForCharacter(NavalPerks.Boatswain.ShipwrightsInsight, attackerAgent.CurrentBattleEnvironment, character, isPrimaryBonus: true, ref bonuses);
		}
		return MBMath.ClampInt(MathF.Ceiling(bonuses.ResultNumber), 0, 2000);
	}
}

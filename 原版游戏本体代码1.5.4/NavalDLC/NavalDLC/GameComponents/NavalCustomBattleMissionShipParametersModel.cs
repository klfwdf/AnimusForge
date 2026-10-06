using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.MountAndBlade;
using TaleWorlds.MountAndBlade.ComponentInterfaces;

namespace NavalDLC.GameComponents;

public class NavalCustomBattleMissionShipParametersModel : MissionShipParametersModel
{
	public override int CalculateMainDeckCrewSize(IShipOrigin shipOrigin, Agent formationUnit)
	{
		return MathF.Min(MathF.Ceiling(new ExplainedNumber(shipOrigin.MainDeckCrewCapacity).ResultNumber), shipOrigin.TotalCrewCapacity);
	}

	public override float CalculateWindBonus(IShipOrigin shipOrigin, Agent captain, float baseSailForceMagnitude)
	{
		ExplainedNumber explainedNumber = new ExplainedNumber(baseSailForceMagnitude);
		if (captain != null && captain.Character is CharacterObject characterObject)
		{
			int skillValue = characterObject.GetSkillValue(NavalSkills.Shipmaster);
			SkillHelper.AddSkillBonusForSkillLevel(NavalSkillEffects.WindBonus, ref explainedNumber, skillValue);
			PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Shipmaster.Windborne, captain.CurrentBattleEnvironment, characterObject, ref explainedNumber);
		}
		return explainedNumber.ResultNumber;
	}

	public override float CalculateOarForceMultiplier(Agent pilotAgent, float baseOarForceMultiplier)
	{
		ExplainedNumber bonuses = new ExplainedNumber(baseOarForceMultiplier);
		bonuses.LimitMin(0f);
		Agent agent = pilotAgent?.Formation?.Captain;
		if (agent != null && agent.Character is CharacterObject captainCharacter)
		{
			PerkHelper.AddPerkBonusFromCaptain(NavalPerks.Shipmaster.ChainToOars, agent.CurrentBattleEnvironment, captainCharacter, ref bonuses);
		}
		return bonuses.ResultNumber;
	}
}

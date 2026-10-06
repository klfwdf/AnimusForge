using StoryMode.Extensions;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.Settlements;
using TaleWorlds.Core;

namespace StoryMode.GameComponents;

public class StoryModeCombatXpModel : CombatXpModel
{
	public override float CaptainRadius => base.BaseModel.CaptainRadius;

	public override SkillObject GetSkillForWeapon(WeaponComponentData weapon, bool isSiegeEngineHit)
	{
		return base.BaseModel.GetSkillForWeapon(weapon, isSiegeEngineHit);
	}

	public override ExplainedNumber GetXpFromHit(CharacterObject attackerTroop, CharacterObject captain, CharacterObject attackedTroop, PartyBase attackerParty, int damage, bool isFatal, MissionTypeEnum missionType)
	{
		if (Settlement.CurrentSettlement != null && Settlement.CurrentSettlement.IsTrainingField())
		{
			return new ExplainedNumber(0f, includeDescriptions: false, null);
		}
		return base.BaseModel.GetXpFromHit(attackerTroop, captain, attackedTroop, attackerParty, damage, isFatal, missionType);
	}

	public override float GetXpMultiplierFromShotDifficulty(float shotDifficulty)
	{
		return base.BaseModel.GetXpMultiplierFromShotDifficulty(shotDifficulty);
	}
}

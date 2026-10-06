using Helpers;
using NavalDLC.CharacterDevelopment;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace NavalDLC.GameComponents;

public class NavalDLCCombatXpModel : CombatXpModel
{
	private const float NavalXPBonusForNonHeroTroops = 0.5f;

	public override float CaptainRadius => base.BaseModel.CaptainRadius;

	public override SkillObject GetSkillForWeapon(WeaponComponentData weapon, bool isSiegeEngineHit)
	{
		return base.BaseModel.GetSkillForWeapon(weapon, isSiegeEngineHit);
	}

	public override ExplainedNumber GetXpFromHit(CharacterObject attackerTroop, CharacterObject captain, CharacterObject attackedTroop, PartyBase attackerParty, int damage, bool isFatal, MissionTypeEnum missionType)
	{
		ExplainedNumber stat = base.BaseModel.GetXpFromHit(attackerTroop, captain, attackedTroop, attackerParty, damage, isFatal, missionType);
		if (attackerParty?.MapEvent != null)
		{
			if (attackerParty.MapEvent.IsNavalMapEvent)
			{
				if (!attackerTroop.IsHero)
				{
					stat.AddFactor(0.5f);
				}
				else if (attackerTroop.HeroObject.CompanionOf != null && attackerParty.IsMobile)
				{
					PerkHelper.AddPerkBonusForParty(NavalPerks.Mariner.NavalFightingTraining, attackerParty.MobileParty, isPrimaryBonus: true, ref stat);
				}
			}
			if (attackerParty.LeaderHero?.Clan?.Kingdom != null && attackerParty.LeaderHero.Clan.Kingdom.HasPolicy(NavalPolicies.FraternalFleetDoctrine))
			{
				stat.AddFactor(-0.15f, NavalPolicies.FraternalFleetDoctrine.Name);
			}
		}
		return stat;
	}

	public override float GetXpMultiplierFromShotDifficulty(float shotDifficulty)
	{
		return base.BaseModel.GetXpMultiplierFromShotDifficulty(shotDifficulty);
	}
}

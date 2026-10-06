using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.CharacterDevelopment;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace NavalDLC.Missions.Objects.UsableMachines;

public class UserDamageCalculateComponent : UsableMissionObjectComponent
{
	private PerkObject _perkObject;

	private bool _isPrimaryBonus;

	public float DamageReductionFactor { get; private set; }

	public UserDamageCalculateComponent(PerkObject perkObject, bool isPrimaryBonus, float damageReductionFactor)
	{
		_perkObject = perkObject;
		_isPrimaryBonus = isPrimaryBonus;
		DamageReductionFactor = damageReductionFactor;
	}

	public void ApplyPerkBonusForCharacter(PerkObject perkObject, bool isPrimaryBonus, CharacterObject agentCharacterObject, BattleEnvironment battleEnvironment, ref ExplainedNumber damageResult)
	{
		if (perkObject == _perkObject && isPrimaryBonus == _isPrimaryBonus)
		{
			PerkHelper.AddPerkBonusForCharacter(_perkObject, battleEnvironment, agentCharacterObject, _isPrimaryBonus, ref damageResult);
		}
	}
}

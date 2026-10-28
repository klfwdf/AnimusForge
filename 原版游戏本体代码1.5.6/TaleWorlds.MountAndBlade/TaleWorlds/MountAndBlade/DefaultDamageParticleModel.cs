using TaleWorlds.Engine;

namespace TaleWorlds.MountAndBlade;

public class DefaultDamageParticleModel : DamageParticleModel
{
	private int _bloodStartHitParticleIndex = -1;

	private int _bloodContinueHitParticleIndex = -1;

	private int _bloodEndHitParticleIndex = -1;

	private int _sweatStartHitParticleIndex = -1;

	private int _sweatContinueHitParticleIndex = -1;

	private int _sweatEndHitParticleIndex = -1;

	private int _missileHitParticleIndex = -1;

	public DefaultDamageParticleModel()
	{
		_bloodStartHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_blood_sword_enter");
		_bloodContinueHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_blood_sword_inside");
		_bloodEndHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_blood_sword_exit");
		_sweatStartHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_sweat_sword_enter");
		_sweatContinueHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_sweat_sword_enter");
		_sweatEndHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_sweat_sword_enter");
		_missileHitParticleIndex = ParticleSystemManager.GetRuntimeIdByName("psys_game_blood_sword_enter");
	}

	public override void GetMeleeAttackBloodParticles(Agent attacker, Agent victim, in Blow blow, in AttackCollisionData collisionData, out HitParticleResultData particleResultData)
	{
		particleResultData.StartHitParticleIndex = _bloodStartHitParticleIndex;
		particleResultData.ContinueHitParticleIndex = _bloodContinueHitParticleIndex;
		particleResultData.EndHitParticleIndex = _bloodEndHitParticleIndex;
	}

	public override void GetMeleeAttackSweatParticles(Agent attacker, Agent victim, in Blow blow, in AttackCollisionData collisionData, out HitParticleResultData particleResultData)
	{
		particleResultData.StartHitParticleIndex = _sweatStartHitParticleIndex;
		particleResultData.ContinueHitParticleIndex = _sweatContinueHitParticleIndex;
		particleResultData.EndHitParticleIndex = _sweatEndHitParticleIndex;
	}

	public override int GetMissileAttackParticle(Agent attacker, Agent victim, in Blow blow, in AttackCollisionData collisionData)
	{
		return _missileHitParticleIndex;
	}
}

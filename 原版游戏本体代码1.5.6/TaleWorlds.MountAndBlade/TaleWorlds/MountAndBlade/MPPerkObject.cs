using System;
using System.Collections.Generic;
using System.Xml;
using TaleWorlds.Core;
using TaleWorlds.Library;
using TaleWorlds.Localization;
using TaleWorlds.MountAndBlade.Network.Gameplay.Perks.Conditions;

namespace TaleWorlds.MountAndBlade;

public class MPPerkObject : IReadOnlyPerkObject
{
	private class MPOnSpawnPerkHandlerInstance : MPOnSpawnPerkHandler
	{
		public MPOnSpawnPerkHandlerInstance(IEnumerable<IReadOnlyPerkObject> perks)
			: base(perks)
		{
		}

		public MPOnSpawnPerkHandlerInstance(MissionPeer peer)
			: base(peer)
		{
		}
	}

	private class MPPerkHandlerInstance : MPPerkHandler
	{
		public MPPerkHandlerInstance(Agent agent)
			: base(agent)
		{
		}

		public MPPerkHandlerInstance(MissionPeer peer)
			: base(peer)
		{
		}
	}

	private class MPCombatPerkHandlerInstance : MPCombatPerkHandler
	{
		public MPCombatPerkHandlerInstance(Agent attacker, Agent defender)
			: base(attacker, defender)
		{
		}
	}

	public class MPOnSpawnPerkHandler
	{
		private IEnumerable<IReadOnlyPerkObject> _perks;

		public bool IsWarmup => Mission.Current?.GetMissionBehavior<MissionMultiplayerGameModeBase>()?.WarmupComponent?.IsInWarmup ?? false;

		protected MPOnSpawnPerkHandler(IEnumerable<IReadOnlyPerkObject> perks)
		{
			_perks = perks;
		}

		protected MPOnSpawnPerkHandler(MissionPeer peer)
		{
			_perks = peer.SelectedPerks;
		}

		public float GetExtraTroopCount()
		{
			float num = 0f;
			bool isWarmup = IsWarmup;
			foreach (IReadOnlyPerkObject perk in _perks)
			{
				if (perk != null)
				{
					num += (float)perk.GetExtraTroopCount(isWarmup);
				}
			}
			return num;
		}

		public IEnumerable<(EquipmentIndex, EquipmentElement)> GetAlternativeEquipments(bool isPlayer)
		{
			List<(EquipmentIndex, EquipmentElement)> list = null;
			bool isWarmup = IsWarmup;
			foreach (IReadOnlyPerkObject perk in _perks)
			{
				if (perk != null)
				{
					list = perk.GetAlternativeEquipments(isWarmup, isPlayer, list);
				}
			}
			return list;
		}

		public float GetDrivenPropertyBonusOnSpawn(bool isPlayer, DrivenProperty drivenProperty, float baseValue)
		{
			float num = 0f;
			bool isWarmup = IsWarmup;
			foreach (IReadOnlyPerkObject perk in _perks)
			{
				if (perk != null)
				{
					num += perk.GetDrivenPropertyBonusOnSpawn(isWarmup, isPlayer, drivenProperty, baseValue);
				}
			}
			return num;
		}

		public float GetHitpoints(bool isPlayer)
		{
			float num = 0f;
			bool isWarmup = IsWarmup;
			foreach (IReadOnlyPerkObject perk in _perks)
			{
				num += perk.GetHitpoints(isWarmup, isPlayer);
			}
			return num;
		}
	}

	public class MPPerkHandler
	{
		private readonly Agent _agent;

		private readonly MBReadOnlyList<MPPerkObject> _perks;

		public bool IsWarmup => Mission.Current?.GetMissionBehavior<MissionMultiplayerGameModeBase>()?.WarmupComponent?.IsInWarmup ?? false;

		protected MPPerkHandler(Agent agent)
		{
			_agent = agent;
			_perks = (_agent?.MissionPeer?.SelectedPerks ?? _agent?.OwningAgentMissionPeer?.SelectedPerks) ?? new MBList<MPPerkObject>();
		}

		protected MPPerkHandler(MissionPeer peer)
		{
			_agent = peer?.ControlledAgent;
			_perks = peer?.SelectedPerks ?? new MBList<MPPerkObject>();
		}

		public void OnEvent(MPPerkCondition.PerkEventFlags flags)
		{
			bool isWarmup = IsWarmup;
			foreach (MPPerkObject perk in _perks)
			{
				perk.OnEvent(isWarmup, flags);
			}
		}

		public void OnEvent(Agent agent, MPPerkCondition.PerkEventFlags flags)
		{
			bool isWarmup = IsWarmup;
			foreach (MPPerkObject perk in _perks)
			{
				perk.OnEvent(isWarmup, agent, flags);
			}
		}

		public void OnTick(int tickCount)
		{
			bool isWarmup = IsWarmup;
			foreach (MPPerkObject perk in _perks)
			{
				perk.OnTick(isWarmup, tickCount);
			}
		}

		public float GetDrivenPropertyBonus(DrivenProperty drivenProperty, float baseValue)
		{
			float num = 0f;
			bool isWarmup = IsWarmup;
			foreach (MPPerkObject perk in _perks)
			{
				num += perk.GetDrivenPropertyBonus(isWarmup, _agent, drivenProperty, baseValue);
			}
			return num;
		}

		public float GetRangedAccuracy()
		{
			float num = 0f;
			bool isWarmup = IsWarmup;
			foreach (MPPerkObject perk in _perks)
			{
				num += perk.GetRangedAccuracy(isWarmup, _agent);
			}
			return num;
		}

		public float GetThrowingWeaponSpeed(WeaponComponentData attackerWeapon)
		{
			float num = 0f;
			bool isWarmup = IsWarmup;
			foreach (MPPerkObject perk in _perks)
			{
				num += perk.GetThrowingWeaponSpeed(isWarmup, _agent, attackerWeapon);
			}
			return num;
		}

		public float GetDamageInterruptionThreshold()
		{
			float num = 0f;
			bool isWarmup = IsWarmup;
			foreach (MPPerkObject perk in _perks)
			{
				num += perk.GetDamageInterruptionThreshold(isWarmup, _agent);
			}
			return num;
		}

		public float GetMountManeuver()
		{
			float num = 0f;
			bool isWarmup = IsWarmup;
			foreach (MPPerkObject perk in _perks)
			{
				num += perk.GetMountManeuver(isWarmup, _agent);
			}
			return num;
		}

		public float GetMountSpeed()
		{
			float num = 0f;
			bool isWarmup = IsWarmup;
			foreach (MPPerkObject perk in _perks)
			{
				num += perk.GetMountSpeed(isWarmup, _agent);
			}
			return num;
		}

		public int GetGoldOnKill(float attackerValue, float victimValue)
		{
			int num = 0;
			bool isWarmup = IsWarmup;
			foreach (MPPerkObject perk in _perks)
			{
				num += perk.GetGoldOnKill(isWarmup, _agent, attackerValue, victimValue);
			}
			return num;
		}

		public int GetGoldOnAssist()
		{
			int num = 0;
			bool isWarmup = IsWarmup;
			foreach (MPPerkObject perk in _perks)
			{
				num += perk.GetGoldOnAssist(isWarmup, _agent);
			}
			return num;
		}

		public int GetRewardedGoldOnAssist()
		{
			int num = 0;
			bool isWarmup = IsWarmup;
			foreach (MPPerkObject perk in _perks)
			{
				num += perk.GetRewardedGoldOnAssist(isWarmup, _agent);
			}
			return num;
		}

		public bool GetIsTeamRewardedOnDeath()
		{
			bool isWarmup = IsWarmup;
			foreach (MPPerkObject perk in _perks)
			{
				if (perk.GetIsTeamRewardedOnDeath(isWarmup, _agent))
				{
					return true;
				}
			}
			return false;
		}

		public IEnumerable<(MissionPeer, int)> GetTeamGoldRewardsOnDeath()
		{
			if (GetIsTeamRewardedOnDeath())
			{
				MissionPeer missionPeer = _agent?.MissionPeer ?? _agent?.OwningAgentMissionPeer;
				List<(MissionPeer, int)> list = new List<(MissionPeer, int)>();
				foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
				{
					MissionPeer component = networkPeer.GetComponent<MissionPeer>();
					if (component != missionPeer && component.Team == missionPeer.Team)
					{
						list.Add((component, 0));
					}
				}
				bool isWarmup = IsWarmup;
				{
					foreach (MPPerkObject perk in _perks)
					{
						perk.CalculateRewardedGoldOnDeath(isWarmup, _agent, list);
					}
					return list;
				}
			}
			return null;
		}

		public float GetEncumbrance(bool isOnBody)
		{
			float num = 0f;
			bool isWarmup = IsWarmup;
			foreach (MPPerkObject perk in _perks)
			{
				num += perk.GetEncumbrance(isWarmup, _agent, isOnBody);
			}
			return num;
		}
	}

	public class MPCombatPerkHandler
	{
		private readonly Agent _attacker;

		private readonly Agent _defender;

		private readonly MBReadOnlyList<MPPerkObject> _attackerPerks;

		private readonly MBReadOnlyList<MPPerkObject> _defenderPerks;

		public bool IsWarmup => Mission.Current?.GetMissionBehavior<MissionMultiplayerGameModeBase>()?.WarmupComponent?.IsInWarmup ?? false;

		protected MPCombatPerkHandler(Agent attacker, Agent defender)
		{
			_attacker = attacker;
			_defender = defender;
			attacker = ((attacker != null && attacker.IsMount) ? attacker.RiderAgent : attacker);
			defender = ((defender != null && defender.IsMount) ? defender.RiderAgent : defender);
			_attackerPerks = attacker?.MissionPeer?.SelectedPerks ?? attacker?.OwningAgentMissionPeer?.SelectedPerks ?? new MBList<MPPerkObject>();
			_defenderPerks = defender?.MissionPeer?.SelectedPerks ?? defender?.OwningAgentMissionPeer?.SelectedPerks ?? new MBList<MPPerkObject>();
		}

		public float GetDamage(WeaponComponentData attackerWeapon, DamageTypes damageType, bool isAlternativeAttack)
		{
			float num = 0f;
			if (_attackerPerks.Count > 0 && _defender != null)
			{
				bool isWarmup = IsWarmup;
				if (_defender.IsMount)
				{
					foreach (MPPerkObject attackerPerk in _attackerPerks)
					{
						num += attackerPerk.GetMountDamage(isWarmup, _attacker, attackerWeapon, damageType, isAlternativeAttack);
					}
				}
				foreach (MPPerkObject attackerPerk2 in _attackerPerks)
				{
					num += attackerPerk2.GetDamage(isWarmup, _attacker, attackerWeapon, damageType, isAlternativeAttack);
				}
			}
			return num;
		}

		public float GetDamageTaken(WeaponComponentData attackerWeapon, DamageTypes damageType)
		{
			float num = 0f;
			if (_defenderPerks.Count > 0)
			{
				bool isWarmup = IsWarmup;
				if (_defender.IsMount)
				{
					foreach (MPPerkObject defenderPerk in _defenderPerks)
					{
						num += defenderPerk.GetMountDamageTaken(isWarmup, _defender, attackerWeapon, damageType);
					}
				}
				else
				{
					foreach (MPPerkObject defenderPerk2 in _defenderPerks)
					{
						num += defenderPerk2.GetDamageTaken(isWarmup, _defender, attackerWeapon, damageType);
					}
				}
			}
			return num;
		}

		public float GetSpeedBonusEffectiveness(WeaponComponentData attackerWeapon, DamageTypes damageType)
		{
			float num = 0f;
			bool isWarmup = IsWarmup;
			foreach (MPPerkObject attackerPerk in _attackerPerks)
			{
				num += attackerPerk.GetSpeedBonusEffectiveness(isWarmup, _attacker, attackerWeapon, damageType);
			}
			return num;
		}

		public float GetShieldDamage(bool isCorrectSideBlock)
		{
			float num = 0f;
			if (_defender != null)
			{
				bool isWarmup = IsWarmup;
				foreach (MPPerkObject attackerPerk in _attackerPerks)
				{
					num += attackerPerk.GetShieldDamage(isWarmup, _attacker, _defender, isCorrectSideBlock);
				}
			}
			return num;
		}

		public float GetShieldDamageTaken(bool isCorrectSideBlock)
		{
			float num = 0f;
			bool isWarmup = IsWarmup;
			foreach (MPPerkObject defenderPerk in _defenderPerks)
			{
				num += defenderPerk.GetShieldDamageTaken(isWarmup, _attacker, _defender, isCorrectSideBlock);
			}
			return num;
		}

		public float GetRangedHeadShotDamage()
		{
			float num = 0f;
			if (_attacker != null)
			{
				bool isWarmup = IsWarmup;
				foreach (MPPerkObject attackerPerk in _attackerPerks)
				{
					num += attackerPerk.GetRangedHeadShotDamage(isWarmup, _attacker);
				}
			}
			return num;
		}
	}

	private readonly MissionPeer _peer;

	private readonly MPConditionalEffect.ConditionalEffectContainer _conditionalEffects;

	private readonly MPPerkCondition.PerkEventFlags _perkEventFlags;

	private readonly string _name;

	private readonly string _description;

	private readonly List<MPPerkEffectBase> _effects;

	public TextObject Name => new TextObject(_name);

	public TextObject Description => new TextObject(_description);

	public bool HasBannerBearer { get; }

	public List<string> GameModes { get; }

	public int PerkListIndex { get; }

	public string IconId { get; }

	public string HeroIdleAnimOverride { get; }

	public string HeroMountIdleAnimOverride { get; }

	public string TroopIdleAnimOverride { get; }

	public string TroopMountIdleAnimOverride { get; }

	public MPPerkObject(MissionPeer peer, string name, string description, List<string> gameModes, int perkListIndex, string iconId, IEnumerable<MPConditionalEffect> conditionalEffects, IEnumerable<MPPerkEffectBase> effects, string heroIdleAnimOverride, string heroMountIdleAnimOverride, string troopIdleAnimOverride, string troopMountIdleAnimOverride)
	{
		_peer = peer;
		_name = name;
		_description = description;
		GameModes = gameModes;
		PerkListIndex = perkListIndex;
		IconId = iconId;
		_conditionalEffects = new MPConditionalEffect.ConditionalEffectContainer(conditionalEffects);
		_effects = new List<MPPerkEffectBase>(effects);
		HeroIdleAnimOverride = heroIdleAnimOverride;
		HeroMountIdleAnimOverride = heroMountIdleAnimOverride;
		TroopIdleAnimOverride = troopIdleAnimOverride;
		TroopMountIdleAnimOverride = troopMountIdleAnimOverride;
		_perkEventFlags = MPPerkCondition.PerkEventFlags.None;
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			foreach (MPPerkCondition condition in conditionalEffect.Conditions)
			{
				_perkEventFlags |= condition.EventFlags;
			}
		}
		foreach (MPConditionalEffect conditionalEffect2 in _conditionalEffects)
		{
			foreach (MPPerkCondition condition2 in conditionalEffect2.Conditions)
			{
				if (condition2 is BannerBearerCondition)
				{
					HasBannerBearer = true;
				}
			}
		}
	}

	private MPPerkObject(XmlNode node)
	{
		_peer = null;
		_conditionalEffects = new MPConditionalEffect.ConditionalEffectContainer();
		_effects = new List<MPPerkEffectBase>();
		_name = node.Attributes["name"].Value;
		_description = node.Attributes["description"].Value;
		GameModes = new List<string>(node.Attributes["game_mode"].Value.Split(new char[1] { ',' }));
		for (int i = 0; i < GameModes.Count; i++)
		{
			GameModes[i] = GameModes[i].Trim();
		}
		IconId = node.Attributes["icon"].Value;
		PerkListIndex = 0;
		XmlNode xmlNode = node.Attributes["perk_list"];
		if (xmlNode != null)
		{
			PerkListIndex = Convert.ToInt32(xmlNode.Value);
			int perkListIndex = PerkListIndex;
			PerkListIndex = perkListIndex - 1;
		}
		foreach (XmlNode childNode in node.ChildNodes)
		{
			if (childNode.NodeType != XmlNodeType.Comment && childNode.NodeType != XmlNodeType.SignificantWhitespace)
			{
				if (childNode.Name == "ConditionalEffect")
				{
					_conditionalEffects.Add(new MPConditionalEffect(GameModes, childNode));
				}
				else if (childNode.Name == "Effect")
				{
					_effects.Add(MPPerkEffect.CreateFrom(childNode));
				}
				else if (childNode.Name == "OnSpawnEffect")
				{
					_effects.Add(MPOnSpawnPerkEffect.CreateFrom(childNode));
				}
				else if (childNode.Name == "RandomOnSpawnEffect")
				{
					_effects.Add(MPRandomOnSpawnPerkEffect.CreateFrom(childNode));
				}
				else
				{
					Debug.FailedAssert("Unknown child element", "C:\\BuildAgent\\work\\mb3\\Source\\Bannerlord\\TaleWorlds.MountAndBlade\\Network\\Gameplay\\Perks\\MPPerkObject.cs", ".ctor", 750);
				}
			}
		}
		HeroIdleAnimOverride = node.Attributes["hero_idle_anim"]?.Value;
		HeroMountIdleAnimOverride = node.Attributes["hero_mount_idle_anim"]?.Value;
		TroopIdleAnimOverride = node.Attributes["troop_idle_anim"]?.Value;
		TroopMountIdleAnimOverride = node.Attributes["troop_mount_idle_anim"]?.Value;
		_perkEventFlags = MPPerkCondition.PerkEventFlags.None;
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			foreach (MPPerkCondition condition in conditionalEffect.Conditions)
			{
				_perkEventFlags |= condition.EventFlags;
			}
		}
		foreach (MPConditionalEffect conditionalEffect2 in _conditionalEffects)
		{
			foreach (MPPerkCondition condition2 in conditionalEffect2.Conditions)
			{
				if (condition2 is BannerBearerCondition)
				{
					HasBannerBearer = true;
				}
			}
		}
	}

	public MPPerkObject Clone(MissionPeer peer)
	{
		return new MPPerkObject(peer, _name, _description, GameModes, PerkListIndex, IconId, _conditionalEffects, _effects, HeroIdleAnimOverride, HeroMountIdleAnimOverride, TroopIdleAnimOverride, TroopMountIdleAnimOverride);
	}

	public void Reset()
	{
		_conditionalEffects.ResetStates();
	}

	private void OnEvent(bool isWarmup, MPPerkCondition.PerkEventFlags flags)
	{
		if ((flags & _perkEventFlags) == 0)
		{
			return;
		}
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			if ((flags & conditionalEffect.EventFlags) != MPPerkCondition.PerkEventFlags.None)
			{
				conditionalEffect.OnEvent(isWarmup, _peer, _conditionalEffects);
			}
		}
	}

	private void OnEvent(bool isWarmup, Agent agent, MPPerkCondition.PerkEventFlags flags)
	{
		if (agent?.MissionPeer == null)
		{
			_ = agent?.OwningAgentMissionPeer;
		}
		if ((flags & _perkEventFlags) == 0)
		{
			return;
		}
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			if ((flags & conditionalEffect.EventFlags) != MPPerkCondition.PerkEventFlags.None)
			{
				conditionalEffect.OnEvent(isWarmup, agent, _conditionalEffects);
			}
		}
	}

	private void OnTick(bool isWarmup, int tickCount)
	{
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			if (conditionalEffect.IsTickRequired)
			{
				conditionalEffect.OnTick(isWarmup, _peer, tickCount);
			}
		}
		foreach (MPPerkEffectBase effect in _effects)
		{
			if ((!isWarmup || !effect.IsDisabledInWarmup) && effect.IsTickRequired)
			{
				effect.OnTick(_peer, tickCount);
			}
		}
	}

	private float GetDamage(bool isWarmup, Agent agent, WeaponComponentData attackerWeapon, DamageTypes damageType, bool isAlternativeAttack)
	{
		agent = agent ?? _peer?.ControlledAgent;
		float num = 0f;
		foreach (MPPerkEffectBase effect in _effects)
		{
			if (!isWarmup || !effect.IsDisabledInWarmup)
			{
				num += effect.GetDamage(attackerWeapon, damageType, isAlternativeAttack);
			}
		}
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			if (!conditionalEffect.Check(agent))
			{
				continue;
			}
			foreach (MPPerkEffectBase effect2 in conditionalEffect.Effects)
			{
				if (!isWarmup || !effect2.IsDisabledInWarmup)
				{
					num += effect2.GetDamage(attackerWeapon, damageType, isAlternativeAttack);
				}
			}
		}
		return num;
	}

	private float GetMountDamage(bool isWarmup, Agent agent, WeaponComponentData attackerWeapon, DamageTypes damageType, bool isAlternativeAttack)
	{
		agent = agent ?? _peer?.ControlledAgent;
		float num = 0f;
		foreach (MPPerkEffectBase effect in _effects)
		{
			if (!isWarmup || !effect.IsDisabledInWarmup)
			{
				num += effect.GetMountDamage(attackerWeapon, damageType, isAlternativeAttack);
			}
		}
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			if (!conditionalEffect.Check(agent))
			{
				continue;
			}
			foreach (MPPerkEffectBase effect2 in conditionalEffect.Effects)
			{
				if (!isWarmup || !effect2.IsDisabledInWarmup)
				{
					num += effect2.GetMountDamage(attackerWeapon, damageType, isAlternativeAttack);
				}
			}
		}
		return num;
	}

	private float GetDamageTaken(bool isWarmup, Agent agent, WeaponComponentData attackerWeapon, DamageTypes damageType)
	{
		agent = agent ?? _peer?.ControlledAgent;
		float num = 0f;
		foreach (MPPerkEffectBase effect in _effects)
		{
			if (!isWarmup || !effect.IsDisabledInWarmup)
			{
				num += effect.GetDamageTaken(attackerWeapon, damageType);
			}
		}
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			if (!conditionalEffect.Check(agent))
			{
				continue;
			}
			foreach (MPPerkEffectBase effect2 in conditionalEffect.Effects)
			{
				if (!isWarmup || !effect2.IsDisabledInWarmup)
				{
					num += effect2.GetDamageTaken(attackerWeapon, damageType);
				}
			}
		}
		return num;
	}

	private float GetMountDamageTaken(bool isWarmup, Agent agent, WeaponComponentData attackerWeapon, DamageTypes damageType)
	{
		agent = agent ?? _peer?.ControlledAgent;
		float num = 0f;
		foreach (MPPerkEffectBase effect in _effects)
		{
			if (!isWarmup || !effect.IsDisabledInWarmup)
			{
				num += effect.GetMountDamageTaken(attackerWeapon, damageType);
			}
		}
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			if (!conditionalEffect.Check(agent))
			{
				continue;
			}
			foreach (MPPerkEffectBase effect2 in conditionalEffect.Effects)
			{
				if (!isWarmup || !effect2.IsDisabledInWarmup)
				{
					num += effect2.GetMountDamageTaken(attackerWeapon, damageType);
				}
			}
		}
		return num;
	}

	private float GetSpeedBonusEffectiveness(bool isWarmup, Agent agent, WeaponComponentData attackerWeapon, DamageTypes damageType)
	{
		agent = agent ?? _peer?.ControlledAgent;
		float num = 0f;
		foreach (MPPerkEffectBase effect in _effects)
		{
			if (!isWarmup || !effect.IsDisabledInWarmup)
			{
				num += effect.GetSpeedBonusEffectiveness(agent, attackerWeapon, damageType);
			}
		}
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			if (!conditionalEffect.Check(agent))
			{
				continue;
			}
			foreach (MPPerkEffectBase effect2 in conditionalEffect.Effects)
			{
				if (!isWarmup || !effect2.IsDisabledInWarmup)
				{
					num += effect2.GetSpeedBonusEffectiveness(agent, attackerWeapon, damageType);
				}
			}
		}
		return num;
	}

	private float GetShieldDamage(bool isWarmup, Agent attacker, Agent defender, bool isCorrectSideBlock)
	{
		float num = 0f;
		foreach (MPPerkEffectBase effect in _effects)
		{
			if (!isWarmup || !effect.IsDisabledInWarmup)
			{
				num += effect.GetShieldDamage(isCorrectSideBlock);
			}
		}
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			if (!conditionalEffect.Check(attacker))
			{
				continue;
			}
			foreach (MPPerkEffectBase effect2 in conditionalEffect.Effects)
			{
				if (!isWarmup || !effect2.IsDisabledInWarmup)
				{
					num += effect2.GetShieldDamage(isCorrectSideBlock);
				}
			}
		}
		return num;
	}

	private float GetShieldDamageTaken(bool isWarmup, Agent attacker, Agent defender, bool isCorrectSideBlock)
	{
		float num = 0f;
		foreach (MPPerkEffectBase effect in _effects)
		{
			if (!isWarmup || !effect.IsDisabledInWarmup)
			{
				num += effect.GetShieldDamageTaken(isCorrectSideBlock);
			}
		}
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			if (!conditionalEffect.Check(defender))
			{
				continue;
			}
			foreach (MPPerkEffectBase effect2 in conditionalEffect.Effects)
			{
				if (!isWarmup || !effect2.IsDisabledInWarmup)
				{
					num += effect2.GetShieldDamageTaken(isCorrectSideBlock);
				}
			}
		}
		return num;
	}

	private float GetRangedAccuracy(bool isWarmup, Agent agent)
	{
		agent = agent ?? _peer?.ControlledAgent;
		float num = 0f;
		foreach (MPPerkEffectBase effect in _effects)
		{
			if (!isWarmup || !effect.IsDisabledInWarmup)
			{
				num += effect.GetRangedAccuracy();
			}
		}
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			if (!conditionalEffect.Check(agent))
			{
				continue;
			}
			foreach (MPPerkEffectBase effect2 in conditionalEffect.Effects)
			{
				if (!isWarmup || !effect2.IsDisabledInWarmup)
				{
					num += effect2.GetRangedAccuracy();
				}
			}
		}
		return num;
	}

	private float GetThrowingWeaponSpeed(bool isWarmup, Agent agent, WeaponComponentData attackerWeapon)
	{
		agent = agent ?? _peer?.ControlledAgent;
		float num = 0f;
		foreach (MPPerkEffectBase effect in _effects)
		{
			if (!isWarmup || !effect.IsDisabledInWarmup)
			{
				num += effect.GetThrowingWeaponSpeed(attackerWeapon);
			}
		}
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			if (!conditionalEffect.Check(agent))
			{
				continue;
			}
			foreach (MPPerkEffectBase effect2 in conditionalEffect.Effects)
			{
				if (!isWarmup || !effect2.IsDisabledInWarmup)
				{
					num += effect2.GetThrowingWeaponSpeed(attackerWeapon);
				}
			}
		}
		return num;
	}

	private float GetDamageInterruptionThreshold(bool isWarmup, Agent agent)
	{
		agent = agent ?? _peer?.ControlledAgent;
		float num = 0f;
		foreach (MPPerkEffectBase effect in _effects)
		{
			if (!isWarmup || !effect.IsDisabledInWarmup)
			{
				num += effect.GetDamageInterruptionThreshold();
			}
		}
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			if (!conditionalEffect.Check(agent))
			{
				continue;
			}
			foreach (MPPerkEffectBase effect2 in conditionalEffect.Effects)
			{
				if (!isWarmup || !effect2.IsDisabledInWarmup)
				{
					num += effect2.GetDamageInterruptionThreshold();
				}
			}
		}
		return num;
	}

	private float GetMountManeuver(bool isWarmup, Agent agent)
	{
		agent = agent ?? _peer?.ControlledAgent;
		float num = 0f;
		foreach (MPPerkEffectBase effect in _effects)
		{
			if (!isWarmup || !effect.IsDisabledInWarmup)
			{
				num += effect.GetMountManeuver();
			}
		}
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			if (!conditionalEffect.Check(agent))
			{
				continue;
			}
			foreach (MPPerkEffectBase effect2 in conditionalEffect.Effects)
			{
				if (!isWarmup || !effect2.IsDisabledInWarmup)
				{
					num += effect2.GetMountManeuver();
				}
			}
		}
		return num;
	}

	private float GetMountSpeed(bool isWarmup, Agent agent)
	{
		agent = agent ?? _peer?.ControlledAgent;
		float num = 0f;
		foreach (MPPerkEffectBase effect in _effects)
		{
			if (!isWarmup || !effect.IsDisabledInWarmup)
			{
				num += effect.GetMountSpeed();
			}
		}
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			if (!conditionalEffect.Check(agent))
			{
				continue;
			}
			foreach (MPPerkEffectBase effect2 in conditionalEffect.Effects)
			{
				if (!isWarmup || !effect2.IsDisabledInWarmup)
				{
					num += effect2.GetMountSpeed();
				}
			}
		}
		return num;
	}

	private float GetRangedHeadShotDamage(bool isWarmup, Agent agent)
	{
		agent = agent ?? _peer?.ControlledAgent;
		float num = 0f;
		foreach (MPPerkEffectBase effect in _effects)
		{
			if (!isWarmup || !effect.IsDisabledInWarmup)
			{
				num += effect.GetRangedHeadShotDamage();
			}
		}
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			if (!conditionalEffect.Check(agent))
			{
				continue;
			}
			foreach (MPPerkEffectBase effect2 in conditionalEffect.Effects)
			{
				if (!isWarmup || !effect2.IsDisabledInWarmup)
				{
					num += effect2.GetRangedHeadShotDamage();
				}
			}
		}
		return num;
	}

	public int GetExtraTroopCount(bool isWarmup)
	{
		int num = 0;
		foreach (MPPerkEffectBase effect in _effects)
		{
			if (effect is IOnSpawnPerkEffect onSpawnPerkEffect && (!isWarmup || !effect.IsDisabledInWarmup))
			{
				num += onSpawnPerkEffect.GetExtraTroopCount();
			}
		}
		return num;
	}

	public List<(EquipmentIndex, EquipmentElement)> GetAlternativeEquipments(bool isWarmup, bool isPlayer, List<(EquipmentIndex, EquipmentElement)> alternativeEquipments, bool getAllEquipments = false)
	{
		foreach (MPPerkEffectBase effect in _effects)
		{
			if (effect is IOnSpawnPerkEffect onSpawnPerkEffect && (!isWarmup || !effect.IsDisabledInWarmup))
			{
				alternativeEquipments = onSpawnPerkEffect.GetAlternativeEquipments(isPlayer, alternativeEquipments, getAllEquipments);
			}
		}
		return alternativeEquipments;
	}

	private float GetDrivenPropertyBonus(bool isWarmup, Agent agent, DrivenProperty drivenProperty, float baseValue)
	{
		agent = agent ?? _peer?.ControlledAgent;
		float num = 0f;
		foreach (MPPerkEffectBase effect in _effects)
		{
			if (!isWarmup || !effect.IsDisabledInWarmup)
			{
				num += effect.GetDrivenPropertyBonus(drivenProperty, baseValue);
			}
		}
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			if (!conditionalEffect.Check(agent))
			{
				continue;
			}
			foreach (MPPerkEffectBase effect2 in conditionalEffect.Effects)
			{
				if (!isWarmup || !effect2.IsDisabledInWarmup)
				{
					num += effect2.GetDrivenPropertyBonus(drivenProperty, baseValue);
				}
			}
		}
		return num;
	}

	public float GetDrivenPropertyBonusOnSpawn(bool isWarmup, bool isPlayer, DrivenProperty drivenProperty, float baseValue)
	{
		float num = 0f;
		foreach (MPPerkEffectBase effect in _effects)
		{
			if (effect is IOnSpawnPerkEffect onSpawnPerkEffect && (!isWarmup || !effect.IsDisabledInWarmup))
			{
				num += onSpawnPerkEffect.GetDrivenPropertyBonusOnSpawn(isPlayer, drivenProperty, baseValue);
			}
		}
		return num;
	}

	public float GetHitpoints(bool isWarmup, bool isPlayer)
	{
		float num = 0f;
		foreach (MPPerkEffectBase effect in _effects)
		{
			if (effect is IOnSpawnPerkEffect onSpawnPerkEffect && (!isWarmup || !effect.IsDisabledInWarmup))
			{
				num += onSpawnPerkEffect.GetHitpoints(isPlayer);
			}
		}
		return num;
	}

	private float GetEncumbrance(bool isWarmup, Agent agent, bool isOnBody)
	{
		agent = agent ?? _peer?.ControlledAgent;
		float num = 0f;
		foreach (MPPerkEffectBase effect in _effects)
		{
			if (!isWarmup || !effect.IsDisabledInWarmup)
			{
				num += effect.GetEncumbrance(isOnBody);
			}
		}
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			if (!conditionalEffect.Check(agent))
			{
				continue;
			}
			foreach (MPPerkEffectBase effect2 in conditionalEffect.Effects)
			{
				if (!isWarmup || !effect2.IsDisabledInWarmup)
				{
					num += effect2.GetEncumbrance(isOnBody);
				}
			}
		}
		return num;
	}

	private int GetGoldOnKill(bool isWarmup, Agent agent, float attackerValue, float victimValue)
	{
		agent = agent ?? _peer?.ControlledAgent;
		int num = 0;
		foreach (MPPerkEffectBase effect in _effects)
		{
			if (!isWarmup || !effect.IsDisabledInWarmup)
			{
				num += effect.GetGoldOnKill(attackerValue, victimValue);
			}
		}
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			if (!conditionalEffect.Check(agent))
			{
				continue;
			}
			foreach (MPPerkEffectBase effect2 in conditionalEffect.Effects)
			{
				if (!isWarmup || !effect2.IsDisabledInWarmup)
				{
					num += effect2.GetGoldOnKill(attackerValue, victimValue);
				}
			}
		}
		return num;
	}

	private int GetGoldOnAssist(bool isWarmup, Agent agent)
	{
		agent = agent ?? _peer?.ControlledAgent;
		int num = 0;
		foreach (MPPerkEffectBase effect in _effects)
		{
			if (!isWarmup || !effect.IsDisabledInWarmup)
			{
				num += effect.GetGoldOnAssist();
			}
		}
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			if (!conditionalEffect.Check(agent))
			{
				continue;
			}
			foreach (MPPerkEffectBase effect2 in conditionalEffect.Effects)
			{
				if (!isWarmup || !effect2.IsDisabledInWarmup)
				{
					num += effect2.GetGoldOnAssist();
				}
			}
		}
		return num;
	}

	private int GetRewardedGoldOnAssist(bool isWarmup, Agent agent)
	{
		agent = agent ?? _peer?.ControlledAgent;
		int num = 0;
		foreach (MPPerkEffectBase effect in _effects)
		{
			if (!isWarmup || !effect.IsDisabledInWarmup)
			{
				num += effect.GetRewardedGoldOnAssist();
			}
		}
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			if (!conditionalEffect.Check(agent))
			{
				continue;
			}
			foreach (MPPerkEffectBase effect2 in conditionalEffect.Effects)
			{
				if (!isWarmup || !effect2.IsDisabledInWarmup)
				{
					num += effect2.GetRewardedGoldOnAssist();
				}
			}
		}
		return num;
	}

	private bool GetIsTeamRewardedOnDeath(bool isWarmup, Agent agent)
	{
		agent = agent ?? _peer?.ControlledAgent;
		foreach (MPPerkEffectBase effect in _effects)
		{
			if ((!isWarmup || !effect.IsDisabledInWarmup) && effect.GetIsTeamRewardedOnDeath())
			{
				return true;
			}
		}
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			if (!conditionalEffect.Check(agent))
			{
				continue;
			}
			foreach (MPPerkEffectBase effect2 in conditionalEffect.Effects)
			{
				if ((!isWarmup || !effect2.IsDisabledInWarmup) && effect2.GetIsTeamRewardedOnDeath())
				{
					return true;
				}
			}
		}
		return false;
	}

	private void CalculateRewardedGoldOnDeath(bool isWarmup, Agent agent, List<(MissionPeer, int)> teamMembers)
	{
		agent = agent ?? _peer?.ControlledAgent;
		teamMembers.Shuffle();
		foreach (MPPerkEffectBase effect in _effects)
		{
			if (!isWarmup || !effect.IsDisabledInWarmup)
			{
				effect.CalculateRewardedGoldOnDeath(agent, teamMembers);
			}
		}
		foreach (MPConditionalEffect conditionalEffect in _conditionalEffects)
		{
			if (!conditionalEffect.Check(agent))
			{
				continue;
			}
			foreach (MPPerkEffectBase effect2 in conditionalEffect.Effects)
			{
				if (!isWarmup || !effect2.IsDisabledInWarmup)
				{
					effect2.CalculateRewardedGoldOnDeath(agent, teamMembers);
				}
			}
		}
	}

	public static int GetTroopCount(MultiplayerClassDivisions.MPHeroClass heroClass, int botsPerFormation, MPOnSpawnPerkHandler onSpawnPerkHandler)
	{
		int num = TaleWorlds.Library.MathF.Ceiling((float)botsPerFormation * heroClass.TroopMultiplier - 1E-05f);
		if (onSpawnPerkHandler != null)
		{
			num += (int)onSpawnPerkHandler.GetExtraTroopCount();
		}
		return TaleWorlds.Library.MathF.Max(num, 1);
	}

	public static IReadOnlyPerkObject Deserialize(XmlNode node)
	{
		return new MPPerkObject(node);
	}

	public static MPPerkHandler GetPerkHandler(Agent agent)
	{
		MBReadOnlyList<MPPerkObject> mBReadOnlyList = agent?.MissionPeer?.SelectedPerks ?? agent?.OwningAgentMissionPeer?.SelectedPerks;
		if (mBReadOnlyList != null && mBReadOnlyList.Count > 0 && !agent.IsMount)
		{
			return new MPPerkHandlerInstance(agent);
		}
		return null;
	}

	public static MPPerkHandler GetPerkHandler(MissionPeer peer)
	{
		MBReadOnlyList<MPPerkObject> mBReadOnlyList = peer?.SelectedPerks ?? peer?.SelectedPerks;
		if (mBReadOnlyList != null && mBReadOnlyList.Count > 0)
		{
			return new MPPerkHandlerInstance(peer);
		}
		return null;
	}

	public static MPCombatPerkHandler GetCombatPerkHandler(Agent attacker, Agent defender)
	{
		Agent agent = ((attacker != null && attacker.IsMount) ? attacker.RiderAgent : attacker);
		MBReadOnlyList<MPPerkObject> mBReadOnlyList = agent?.MissionPeer?.SelectedPerks ?? agent?.OwningAgentMissionPeer?.SelectedPerks;
		Agent agent2 = ((defender != null && defender.IsMount) ? defender.RiderAgent : defender);
		MBReadOnlyList<MPPerkObject> mBReadOnlyList2 = agent2?.MissionPeer?.SelectedPerks ?? agent2?.OwningAgentMissionPeer?.SelectedPerks;
		if (attacker != defender && ((mBReadOnlyList != null && mBReadOnlyList.Count > 0) || (mBReadOnlyList2 != null && mBReadOnlyList2.Count > 0)))
		{
			return new MPCombatPerkHandlerInstance(attacker, defender);
		}
		return null;
	}

	public static MPOnSpawnPerkHandler GetOnSpawnPerkHandler(MissionPeer peer)
	{
		if ((peer?.SelectedPerks ?? peer?.SelectedPerks) != null)
		{
			return new MPOnSpawnPerkHandlerInstance(peer);
		}
		return null;
	}

	public static MPOnSpawnPerkHandler GetOnSpawnPerkHandler(IEnumerable<IReadOnlyPerkObject> perks)
	{
		if (perks != null)
		{
			return new MPOnSpawnPerkHandlerInstance(perks);
		}
		return null;
	}

	public static void RaiseEventForAllPeers(MPPerkCondition.PerkEventFlags flags)
	{
		if (!GameNetwork.IsServerOrRecorder)
		{
			return;
		}
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			GetPerkHandler(networkPeer.GetComponent<MissionPeer>())?.OnEvent(flags);
		}
	}

	public static void RaiseEventForAllPeersOnTeam(Team side, MPPerkCondition.PerkEventFlags flags)
	{
		if (!GameNetwork.IsServerOrRecorder)
		{
			return;
		}
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			MissionPeer component = networkPeer.GetComponent<MissionPeer>();
			if (component != null && component.Team == side)
			{
				GetPerkHandler(component)?.OnEvent(flags);
			}
		}
	}

	public static void TickAllPeerPerks(int tickCount)
	{
		if (!GameNetwork.IsServerOrRecorder)
		{
			return;
		}
		foreach (NetworkCommunicator networkPeer in GameNetwork.NetworkPeers)
		{
			MissionPeer component = networkPeer.GetComponent<MissionPeer>();
			if (component != null && component.Team != null && component.Culture != null && component.Team.Side != BattleSideEnum.None)
			{
				GetPerkHandler(component)?.OnTick(tickCount);
			}
		}
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("raise_event", "mp_perks")]
	public static string RaiseEventForAllPeersCommand(List<string> strings)
	{
		if (GameNetwork.IsServerOrRecorder)
		{
			MPPerkCondition.PerkEventFlags perkEventFlags = MPPerkCondition.PerkEventFlags.None;
			foreach (string @string in strings)
			{
				if (Enum.TryParse<MPPerkCondition.PerkEventFlags>(@string, ignoreCase: true, out var result))
				{
					perkEventFlags |= result;
				}
			}
			RaiseEventForAllPeers(perkEventFlags);
			return "Raised event with flags " + perkEventFlags;
		}
		return "Can't run this command on clients";
	}

	[CommandLineFunctionality.CommandLineArgumentFunction("tick_perks", "mp_perks")]
	public static string TickAllPeerPerksCommand(List<string> strings)
	{
		if (GameNetwork.IsServerOrRecorder)
		{
			if (strings.Count == 0 || !int.TryParse(strings[0], out var result))
			{
				result = 1;
			}
			TickAllPeerPerks(result);
			return "Peer perks on tick with tick count " + result;
		}
		return "Can't run this command on clients";
	}
}

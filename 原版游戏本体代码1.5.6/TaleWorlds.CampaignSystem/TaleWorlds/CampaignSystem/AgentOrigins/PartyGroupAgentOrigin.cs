using System;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.TroopSuppliers;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.AgentOrigins;

public class PartyGroupAgentOrigin : IAgentOriginBase
{
	private readonly PartyGroupTroopSupplier _supplier;

	private readonly UniqueTroopDescriptor _descriptor;

	private readonly int _rank;

	private bool _isRemoved;

	private bool _hasThrownWeapon;

	private bool _hasHeavyArmor;

	private bool _hasShield;

	private bool _hasSpear;

	public PartyBase Party => _supplier.GetParty(_descriptor);

	public IBattleCombatant BattleCombatant => Party;

	public Banner Banner
	{
		get
		{
			if (Party.LeaderHero == null)
			{
				return Party.MapFaction.Banner;
			}
			return Party.LeaderHero.ClanBanner;
		}
	}

	public int UniqueSeed => _descriptor.UniqueSeed;

	public CharacterObject Troop => _supplier.GetTroop(_descriptor);

	bool IAgentOriginBase.HasThrownWeapon => _hasThrownWeapon;

	bool IAgentOriginBase.HasHeavyArmor => _hasHeavyArmor;

	bool IAgentOriginBase.HasShield => _hasShield;

	bool IAgentOriginBase.HasSpear => _hasSpear;

	BasicCharacterObject IAgentOriginBase.Troop => Troop;

	public UniqueTroopDescriptor TroopDesc => _descriptor;

	public int Rank => _rank;

	public bool IsUnderPlayersCommand
	{
		get
		{
			if (Troop == Hero.MainHero.CharacterObject)
			{
				return true;
			}
			return PartyBase.IsPartyUnderPlayerCommand(Party);
		}
	}

	public bool IsInSameArmyAsPlayer
	{
		get
		{
			PartyBase party = Party;
			MobileParty mobileParty;
			Army army;
			if (party != null && (mobileParty = party.MobileParty) != null && (army = mobileParty.Army) != null && army == MobileParty.MainParty.Army && (army.LeaderParty == mobileParty || mobileParty.AttachedTo == army.LeaderParty))
			{
				if (army.LeaderParty != MobileParty.MainParty)
				{
					return MobileParty.MainParty.AttachedTo == army.LeaderParty;
				}
				return true;
			}
			return false;
		}
	}

	public uint FactionColor => Party.MapFaction.Color;

	public uint FactionColor2 => Party.MapFaction.Color2;

	public int Seed => CharacterHelper.GetPartyMemberFaceSeed(Party, Troop, Rank);

	internal PartyGroupAgentOrigin(PartyGroupTroopSupplier supplier, UniqueTroopDescriptor descriptor, int rank)
	{
		_supplier = supplier;
		_descriptor = descriptor;
		_rank = rank;
		AgentOriginUtilities.GetDefaultTroopTraits(Troop, out _hasThrownWeapon, out _hasSpear, out _hasShield, out _hasHeavyArmor);
	}

	public void SetWounded()
	{
		if (!_isRemoved)
		{
			_supplier.OnTroopWounded(_descriptor);
			_isRemoved = true;
		}
	}

	public void SetKilled()
	{
		if (!_isRemoved)
		{
			_supplier.OnTroopKilled(_descriptor);
			if (Troop.IsHero)
			{
				KillCharacterAction.ApplyByBattle(Troop.HeroObject, null);
			}
			_isRemoved = true;
		}
	}

	public void SetRouted(bool isOrderRetreat)
	{
		if (!_isRemoved)
		{
			_supplier.OnTroopRouted(_descriptor, isOrderRetreat);
			_isRemoved = true;
		}
	}

	public void OnAgentRemoved(float agentHealth)
	{
		if (Troop.IsHero)
		{
			Troop.HeroObject.HitPoints = TaleWorlds.Library.MathF.Max(1, TaleWorlds.Library.MathF.Round(agentHealth));
		}
	}

	void IAgentOriginBase.OnScoreHit(BasicCharacterObject victim, BasicCharacterObject captain, int damage, bool isFatal, bool isTeamKill, WeaponComponentData attackerWeapon)
	{
		_supplier.OnTroopScoreHit(_descriptor, victim, damage, isFatal, isTeamKill, attackerWeapon);
	}

	public void SetBanner(Banner banner)
	{
		throw new NotImplementedException();
	}

	TroopTraitsMask IAgentOriginBase.GetTraitsMask()
	{
		return AgentOriginUtilities.GetDefaultTraitsMask(this);
	}
}

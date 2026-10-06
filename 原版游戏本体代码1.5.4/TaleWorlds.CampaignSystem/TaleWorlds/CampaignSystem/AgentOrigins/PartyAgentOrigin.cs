using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.AgentOrigins;

public class PartyAgentOrigin : IAgentOriginBase
{
	private PartyBase _party;

	private Banner _banner;

	private CharacterObject _troop;

	private bool _hasThrownWeapon;

	private bool _hasHeavyArmor;

	private bool _hasShield;

	private bool _hasSpear;

	private readonly UniqueTroopDescriptor _descriptor;

	private readonly bool _alwaysWounded;

	private readonly bool _isInvincible;

	public PartyBase Party
	{
		get
		{
			PartyBase party = _party;
			if (_troop.IsHero && _troop.HeroObject.PartyBelongedTo != null && _troop.HeroObject.PartyBelongedTo.Party != null)
			{
				party = _troop.HeroObject.PartyBelongedTo.Party;
			}
			return party;
		}
		set
		{
			_party = value;
		}
	}

	public IBattleCombatant BattleCombatant => Party;

	public Banner Banner
	{
		get
		{
			Banner banner = _banner;
			if (banner == null)
			{
				if (Party == null)
				{
					if (!_troop.IsHero)
					{
						return null;
					}
					return _troop.HeroObject.MapFaction.Banner;
				}
				if (Party.LeaderHero == null)
				{
					return Party.MapFaction.Banner;
				}
				banner = Party.LeaderHero.ClanBanner;
			}
			return banner;
		}
	}

	bool IAgentOriginBase.HasThrownWeapon => _hasThrownWeapon;

	bool IAgentOriginBase.HasHeavyArmor => _hasHeavyArmor;

	bool IAgentOriginBase.HasShield => _hasShield;

	bool IAgentOriginBase.HasSpear => _hasSpear;

	public BasicCharacterObject Troop => _troop;

	public int Rank { get; private set; }

	public bool IsUnderPlayersCommand
	{
		get
		{
			PartyBase party = Party;
			if ((party == null || party != PartyBase.MainParty) && party.Owner != Hero.MainHero)
			{
				return party.MapFaction.Leader == Hero.MainHero;
			}
			return true;
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

	public uint FactionColor
	{
		get
		{
			if (Party == null)
			{
				return _troop.HeroObject.MapFaction.Color;
			}
			return Party.MapFaction.Color2;
		}
	}

	public uint FactionColor2
	{
		get
		{
			if (Party == null)
			{
				return _troop.HeroObject.MapFaction.Color2;
			}
			return Party.MapFaction.Color2;
		}
	}

	public int Seed
	{
		get
		{
			if (Party == null)
			{
				return 0;
			}
			return CharacterHelper.GetPartyMemberFaceSeed(Party, _troop, Rank);
		}
	}

	public int UniqueSeed => _descriptor.UniqueSeed;

	public PartyAgentOrigin(PartyBase partyBase, CharacterObject characterObject, int rank = -1, UniqueTroopDescriptor uniqueNo = default(UniqueTroopDescriptor), bool alwaysWounded = false, bool isInvincible = false)
	{
		Party = partyBase;
		_troop = characterObject;
		_descriptor = ((!uniqueNo.IsValid) ? new UniqueTroopDescriptor(Game.Current.NextUniqueTroopSeed) : uniqueNo);
		Rank = ((rank == -1) ? MBRandom.RandomInt(10000) : rank);
		_alwaysWounded = alwaysWounded;
		_isInvincible = isInvincible;
		AgentOriginUtilities.GetDefaultTroopTraits(Troop, out _hasThrownWeapon, out _hasSpear, out _hasShield, out _hasHeavyArmor);
	}

	public void SetWounded()
	{
		if (!_isInvincible)
		{
			if (_troop.IsHero)
			{
				_troop.HeroObject.MakeWounded();
			}
			if (Party != null)
			{
				Party.MemberRoster.AddToCounts(_troop, 0, insertAtFront: false, 1);
			}
		}
	}

	public void SetKilled()
	{
		if (!_isInvincible)
		{
			if (_alwaysWounded)
			{
				SetWounded();
			}
			else if (_troop.IsHero)
			{
				KillCharacterAction.ApplyByBattle(_troop.HeroObject, null);
			}
			else if (!_troop.IsHero)
			{
				Party?.MemberRoster.AddToCounts(_troop, -1);
			}
		}
	}

	public void SetRouted(bool isOrderRetreat)
	{
	}

	public void OnAgentRemoved(float agentHealth)
	{
		if (_troop.IsHero && _troop.HeroObject.HeroState != Hero.CharacterStates.Dead && !_isInvincible)
		{
			_troop.HeroObject.HitPoints = MathF.Max(1, MathF.Round(agentHealth));
		}
	}

	void IAgentOriginBase.OnScoreHit(BasicCharacterObject victim, BasicCharacterObject captain, int damage, bool isFatal, bool isTeamKill, WeaponComponentData attackerWeapon)
	{
	}

	public void SetBanner(Banner banner)
	{
		_banner = banner;
	}

	TroopTraitsMask IAgentOriginBase.GetTraitsMask()
	{
		return AgentOriginUtilities.GetDefaultTraitsMask(this);
	}
}

using System;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.AgentOrigins
{
	// Token: 0x0200048B RID: 1163
	public class PartyAgentOrigin : IAgentOriginBase
	{
		// Token: 0x17000E77 RID: 3703
		// (get) Token: 0x060048EF RID: 18671 RVA: 0x0016F874 File Offset: 0x0016DA74
		// (set) Token: 0x060048F0 RID: 18672 RVA: 0x0016F8D5 File Offset: 0x0016DAD5
		public PartyBase Party
		{
			get
			{
				PartyBase party = this._party;
				if (this._troop.IsHero && this._troop.HeroObject.PartyBelongedTo != null && this._troop.HeroObject.PartyBelongedTo.Party != null)
				{
					party = this._troop.HeroObject.PartyBelongedTo.Party;
				}
				return party;
			}
			set
			{
				this._party = value;
			}
		}

		// Token: 0x17000E78 RID: 3704
		// (get) Token: 0x060048F1 RID: 18673 RVA: 0x0016F8DE File Offset: 0x0016DADE
		public IBattleCombatant BattleCombatant
		{
			get
			{
				return this.Party;
			}
		}

		// Token: 0x17000E79 RID: 3705
		// (get) Token: 0x060048F2 RID: 18674 RVA: 0x0016F8E8 File Offset: 0x0016DAE8
		public Banner Banner
		{
			get
			{
				Banner result;
				if ((result = this._banner) == null)
				{
					if (this.Party == null)
					{
						if (!this._troop.IsHero)
						{
							return null;
						}
						return this._troop.HeroObject.MapFaction.Banner;
					}
					else
					{
						if (this.Party.LeaderHero == null)
						{
							return this.Party.MapFaction.Banner;
						}
						result = this.Party.LeaderHero.ClanBanner;
					}
				}
				return result;
			}
		}

		// Token: 0x17000E7A RID: 3706
		// (get) Token: 0x060048F3 RID: 18675 RVA: 0x0016F95A File Offset: 0x0016DB5A
		bool IAgentOriginBase.HasThrownWeapon
		{
			get
			{
				return this._hasThrownWeapon;
			}
		}

		// Token: 0x17000E7B RID: 3707
		// (get) Token: 0x060048F4 RID: 18676 RVA: 0x0016F962 File Offset: 0x0016DB62
		bool IAgentOriginBase.HasHeavyArmor
		{
			get
			{
				return this._hasHeavyArmor;
			}
		}

		// Token: 0x17000E7C RID: 3708
		// (get) Token: 0x060048F5 RID: 18677 RVA: 0x0016F96A File Offset: 0x0016DB6A
		bool IAgentOriginBase.HasShield
		{
			get
			{
				return this._hasShield;
			}
		}

		// Token: 0x17000E7D RID: 3709
		// (get) Token: 0x060048F6 RID: 18678 RVA: 0x0016F972 File Offset: 0x0016DB72
		bool IAgentOriginBase.HasSpear
		{
			get
			{
				return this._hasSpear;
			}
		}

		// Token: 0x17000E7E RID: 3710
		// (get) Token: 0x060048F7 RID: 18679 RVA: 0x0016F97A File Offset: 0x0016DB7A
		public BasicCharacterObject Troop
		{
			get
			{
				return this._troop;
			}
		}

		// Token: 0x17000E7F RID: 3711
		// (get) Token: 0x060048F8 RID: 18680 RVA: 0x0016F982 File Offset: 0x0016DB82
		// (set) Token: 0x060048F9 RID: 18681 RVA: 0x0016F98A File Offset: 0x0016DB8A
		public int Rank { get; private set; }

		// Token: 0x17000E80 RID: 3712
		// (get) Token: 0x060048FA RID: 18682 RVA: 0x0016F994 File Offset: 0x0016DB94
		public bool IsUnderPlayersCommand
		{
			get
			{
				PartyBase party = this.Party;
				return (party != null && party == PartyBase.MainParty) || party.Owner == Hero.MainHero || party.MapFaction.Leader == Hero.MainHero;
			}
		}

		// Token: 0x17000E81 RID: 3713
		// (get) Token: 0x060048FB RID: 18683 RVA: 0x0016F9D4 File Offset: 0x0016DBD4
		public uint FactionColor
		{
			get
			{
				if (this.Party == null)
				{
					return this._troop.HeroObject.MapFaction.Color;
				}
				return this.Party.MapFaction.Color2;
			}
		}

		// Token: 0x17000E82 RID: 3714
		// (get) Token: 0x060048FC RID: 18684 RVA: 0x0016FA04 File Offset: 0x0016DC04
		public uint FactionColor2
		{
			get
			{
				if (this.Party == null)
				{
					return this._troop.HeroObject.MapFaction.Color2;
				}
				return this.Party.MapFaction.Color2;
			}
		}

		// Token: 0x17000E83 RID: 3715
		// (get) Token: 0x060048FD RID: 18685 RVA: 0x0016FA34 File Offset: 0x0016DC34
		public int Seed
		{
			get
			{
				if (this.Party == null)
				{
					return 0;
				}
				return CharacterHelper.GetPartyMemberFaceSeed(this.Party, this._troop, this.Rank);
			}
		}

		// Token: 0x17000E84 RID: 3716
		// (get) Token: 0x060048FE RID: 18686 RVA: 0x0016FA58 File Offset: 0x0016DC58
		public int UniqueSeed
		{
			get
			{
				return this._descriptor.UniqueSeed;
			}
		}

		// Token: 0x060048FF RID: 18687 RVA: 0x0016FA74 File Offset: 0x0016DC74
		public PartyAgentOrigin(PartyBase partyBase, CharacterObject characterObject, int rank = -1, UniqueTroopDescriptor uniqueNo = default(UniqueTroopDescriptor), bool alwaysWounded = false, bool isInvincible = false)
		{
			this.Party = partyBase;
			this._troop = characterObject;
			this._descriptor = ((!uniqueNo.IsValid) ? new UniqueTroopDescriptor(Game.Current.NextUniqueTroopSeed) : uniqueNo);
			this.Rank = ((rank == -1) ? MBRandom.RandomInt(10000) : rank);
			this._alwaysWounded = alwaysWounded;
			this._isInvincible = isInvincible;
			AgentOriginUtilities.GetDefaultTroopTraits(this.Troop, out this._hasThrownWeapon, out this._hasSpear, out this._hasShield, out this._hasHeavyArmor);
		}

		// Token: 0x06004900 RID: 18688 RVA: 0x0016FB04 File Offset: 0x0016DD04
		public void SetWounded()
		{
			if (!this._isInvincible)
			{
				if (this._troop.IsHero)
				{
					this._troop.HeroObject.MakeWounded(null, KillCharacterAction.KillCharacterActionDetail.None);
				}
				if (this.Party != null)
				{
					this.Party.MemberRoster.AddToCounts(this._troop, 0, false, 1, 0, true, -1);
				}
			}
		}

		// Token: 0x06004901 RID: 18689 RVA: 0x0016FB60 File Offset: 0x0016DD60
		public void SetKilled()
		{
			if (!this._isInvincible)
			{
				if (this._alwaysWounded)
				{
					this.SetWounded();
					return;
				}
				if (this._troop.IsHero)
				{
					KillCharacterAction.ApplyByBattle(this._troop.HeroObject, null, true);
					return;
				}
				if (!this._troop.IsHero)
				{
					PartyBase party = this.Party;
					if (party == null)
					{
						return;
					}
					party.MemberRoster.AddToCounts(this._troop, -1, false, 0, 0, true, -1);
				}
			}
		}

		// Token: 0x06004902 RID: 18690 RVA: 0x0016FBD3 File Offset: 0x0016DDD3
		public void SetRouted(bool isOrderRetreat)
		{
		}

		// Token: 0x06004903 RID: 18691 RVA: 0x0016FBD8 File Offset: 0x0016DDD8
		public void OnAgentRemoved(float agentHealth)
		{
			if (this._troop.IsHero && this._troop.HeroObject.HeroState != Hero.CharacterStates.Dead && !this._isInvincible)
			{
				this._troop.HeroObject.HitPoints = MathF.Max(1, MathF.Round(agentHealth));
			}
		}

		// Token: 0x06004904 RID: 18692 RVA: 0x0016FC29 File Offset: 0x0016DE29
		void IAgentOriginBase.OnScoreHit(BasicCharacterObject victim, BasicCharacterObject captain, int damage, bool isFatal, bool isTeamKill, WeaponComponentData attackerWeapon)
		{
		}

		// Token: 0x06004905 RID: 18693 RVA: 0x0016FC2B File Offset: 0x0016DE2B
		public void SetBanner(Banner banner)
		{
			this._banner = banner;
		}

		// Token: 0x06004906 RID: 18694 RVA: 0x0016FC34 File Offset: 0x0016DE34
		TroopTraitsMask IAgentOriginBase.GetTraitsMask()
		{
			return AgentOriginUtilities.GetDefaultTraitsMask(this);
		}

		// Token: 0x04001418 RID: 5144
		private PartyBase _party;

		// Token: 0x04001419 RID: 5145
		private Banner _banner;

		// Token: 0x0400141A RID: 5146
		private CharacterObject _troop;

		// Token: 0x0400141B RID: 5147
		private bool _hasThrownWeapon;

		// Token: 0x0400141C RID: 5148
		private bool _hasHeavyArmor;

		// Token: 0x0400141D RID: 5149
		private bool _hasShield;

		// Token: 0x0400141E RID: 5150
		private bool _hasSpear;

		// Token: 0x04001420 RID: 5152
		private readonly UniqueTroopDescriptor _descriptor;

		// Token: 0x04001421 RID: 5153
		private readonly bool _alwaysWounded;

		// Token: 0x04001422 RID: 5154
		private readonly bool _isInvincible;
	}
}

using System;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.CampaignSystem.TroopSuppliers;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace TaleWorlds.CampaignSystem.AgentOrigins
{
	// Token: 0x0200048C RID: 1164
	public class PartyGroupAgentOrigin : IAgentOriginBase
	{
		// Token: 0x06004907 RID: 18695 RVA: 0x0016FC3C File Offset: 0x0016DE3C
		internal PartyGroupAgentOrigin(PartyGroupTroopSupplier supplier, UniqueTroopDescriptor descriptor, int rank)
		{
			this._supplier = supplier;
			this._descriptor = descriptor;
			this._rank = rank;
			AgentOriginUtilities.GetDefaultTroopTraits(this.Troop, out this._hasThrownWeapon, out this._hasSpear, out this._hasShield, out this._hasHeavyArmor);
		}

		// Token: 0x17000E85 RID: 3717
		// (get) Token: 0x06004908 RID: 18696 RVA: 0x0016FC7C File Offset: 0x0016DE7C
		public PartyBase Party
		{
			get
			{
				return this._supplier.GetParty(this._descriptor);
			}
		}

		// Token: 0x17000E86 RID: 3718
		// (get) Token: 0x06004909 RID: 18697 RVA: 0x0016FC8F File Offset: 0x0016DE8F
		public IBattleCombatant BattleCombatant
		{
			get
			{
				return this.Party;
			}
		}

		// Token: 0x17000E87 RID: 3719
		// (get) Token: 0x0600490A RID: 18698 RVA: 0x0016FC97 File Offset: 0x0016DE97
		public Banner Banner
		{
			get
			{
				if (this.Party.LeaderHero == null)
				{
					return this.Party.MapFaction.Banner;
				}
				return this.Party.LeaderHero.ClanBanner;
			}
		}

		// Token: 0x17000E88 RID: 3720
		// (get) Token: 0x0600490B RID: 18699 RVA: 0x0016FCC8 File Offset: 0x0016DEC8
		public int UniqueSeed
		{
			get
			{
				return this._descriptor.UniqueSeed;
			}
		}

		// Token: 0x17000E89 RID: 3721
		// (get) Token: 0x0600490C RID: 18700 RVA: 0x0016FCE3 File Offset: 0x0016DEE3
		public CharacterObject Troop
		{
			get
			{
				return this._supplier.GetTroop(this._descriptor);
			}
		}

		// Token: 0x17000E8A RID: 3722
		// (get) Token: 0x0600490D RID: 18701 RVA: 0x0016FCF6 File Offset: 0x0016DEF6
		bool IAgentOriginBase.HasThrownWeapon
		{
			get
			{
				return this._hasThrownWeapon;
			}
		}

		// Token: 0x17000E8B RID: 3723
		// (get) Token: 0x0600490E RID: 18702 RVA: 0x0016FCFE File Offset: 0x0016DEFE
		bool IAgentOriginBase.HasHeavyArmor
		{
			get
			{
				return this._hasHeavyArmor;
			}
		}

		// Token: 0x17000E8C RID: 3724
		// (get) Token: 0x0600490F RID: 18703 RVA: 0x0016FD06 File Offset: 0x0016DF06
		bool IAgentOriginBase.HasShield
		{
			get
			{
				return this._hasShield;
			}
		}

		// Token: 0x17000E8D RID: 3725
		// (get) Token: 0x06004910 RID: 18704 RVA: 0x0016FD0E File Offset: 0x0016DF0E
		bool IAgentOriginBase.HasSpear
		{
			get
			{
				return this._hasSpear;
			}
		}

		// Token: 0x17000E8E RID: 3726
		// (get) Token: 0x06004911 RID: 18705 RVA: 0x0016FD16 File Offset: 0x0016DF16
		BasicCharacterObject IAgentOriginBase.Troop
		{
			get
			{
				return this.Troop;
			}
		}

		// Token: 0x17000E8F RID: 3727
		// (get) Token: 0x06004912 RID: 18706 RVA: 0x0016FD1E File Offset: 0x0016DF1E
		public UniqueTroopDescriptor TroopDesc
		{
			get
			{
				return this._descriptor;
			}
		}

		// Token: 0x17000E90 RID: 3728
		// (get) Token: 0x06004913 RID: 18707 RVA: 0x0016FD26 File Offset: 0x0016DF26
		public int Rank
		{
			get
			{
				return this._rank;
			}
		}

		// Token: 0x17000E91 RID: 3729
		// (get) Token: 0x06004914 RID: 18708 RVA: 0x0016FD2E File Offset: 0x0016DF2E
		public bool IsUnderPlayersCommand
		{
			get
			{
				return this.Troop == Hero.MainHero.CharacterObject || PartyBase.IsPartyUnderPlayerCommand(this.Party);
			}
		}

		// Token: 0x17000E92 RID: 3730
		// (get) Token: 0x06004915 RID: 18709 RVA: 0x0016FD4F File Offset: 0x0016DF4F
		public uint FactionColor
		{
			get
			{
				return this.Party.MapFaction.Color;
			}
		}

		// Token: 0x17000E93 RID: 3731
		// (get) Token: 0x06004916 RID: 18710 RVA: 0x0016FD61 File Offset: 0x0016DF61
		public uint FactionColor2
		{
			get
			{
				return this.Party.MapFaction.Color2;
			}
		}

		// Token: 0x17000E94 RID: 3732
		// (get) Token: 0x06004917 RID: 18711 RVA: 0x0016FD73 File Offset: 0x0016DF73
		public int Seed
		{
			get
			{
				return CharacterHelper.GetPartyMemberFaceSeed(this.Party, this.Troop, this.Rank);
			}
		}

		// Token: 0x06004918 RID: 18712 RVA: 0x0016FD8C File Offset: 0x0016DF8C
		public void SetWounded()
		{
			if (!this._isRemoved)
			{
				this._supplier.OnTroopWounded(this._descriptor);
				this._isRemoved = true;
			}
		}

		// Token: 0x06004919 RID: 18713 RVA: 0x0016FDB0 File Offset: 0x0016DFB0
		public void SetKilled()
		{
			if (!this._isRemoved)
			{
				this._supplier.OnTroopKilled(this._descriptor);
				if (this.Troop.IsHero)
				{
					KillCharacterAction.ApplyByBattle(this.Troop.HeroObject, null, true);
				}
				this._isRemoved = true;
			}
		}

		// Token: 0x0600491A RID: 18714 RVA: 0x0016FDFC File Offset: 0x0016DFFC
		public void SetRouted(bool isOrderRetreat)
		{
			if (!this._isRemoved)
			{
				this._supplier.OnTroopRouted(this._descriptor, isOrderRetreat);
				this._isRemoved = true;
			}
		}

		// Token: 0x0600491B RID: 18715 RVA: 0x0016FE1F File Offset: 0x0016E01F
		public void OnAgentRemoved(float agentHealth)
		{
			if (this.Troop.IsHero)
			{
				this.Troop.HeroObject.HitPoints = MathF.Max(1, MathF.Round(agentHealth));
			}
		}

		// Token: 0x0600491C RID: 18716 RVA: 0x0016FE4A File Offset: 0x0016E04A
		void IAgentOriginBase.OnScoreHit(BasicCharacterObject victim, BasicCharacterObject captain, int damage, bool isFatal, bool isTeamKill, WeaponComponentData attackerWeapon)
		{
			this._supplier.OnTroopScoreHit(this._descriptor, victim, damage, isFatal, isTeamKill, attackerWeapon);
		}

		// Token: 0x0600491D RID: 18717 RVA: 0x0016FE65 File Offset: 0x0016E065
		public void SetBanner(Banner banner)
		{
			throw new NotImplementedException();
		}

		// Token: 0x0600491E RID: 18718 RVA: 0x0016FE6C File Offset: 0x0016E06C
		TroopTraitsMask IAgentOriginBase.GetTraitsMask()
		{
			return AgentOriginUtilities.GetDefaultTraitsMask(this);
		}

		// Token: 0x04001423 RID: 5155
		private readonly PartyGroupTroopSupplier _supplier;

		// Token: 0x04001424 RID: 5156
		private readonly UniqueTroopDescriptor _descriptor;

		// Token: 0x04001425 RID: 5157
		private readonly int _rank;

		// Token: 0x04001426 RID: 5158
		private bool _isRemoved;

		// Token: 0x04001427 RID: 5159
		private bool _hasThrownWeapon;

		// Token: 0x04001428 RID: 5160
		private bool _hasHeavyArmor;

		// Token: 0x04001429 RID: 5161
		private bool _hasShield;

		// Token: 0x0400142A RID: 5162
		private bool _hasSpear;
	}
}

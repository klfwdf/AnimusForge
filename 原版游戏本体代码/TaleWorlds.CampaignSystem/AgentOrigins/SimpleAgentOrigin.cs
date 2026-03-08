using System;
using Helpers;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.AgentOrigins
{
	// Token: 0x0200048D RID: 1165
	public class SimpleAgentOrigin : IAgentOriginBase
	{
		// Token: 0x17000E95 RID: 3733
		// (get) Token: 0x0600491F RID: 18719 RVA: 0x0016FE74 File Offset: 0x0016E074
		public BasicCharacterObject Troop
		{
			get
			{
				return this._troop;
			}
		}

		// Token: 0x17000E96 RID: 3734
		// (get) Token: 0x06004920 RID: 18720 RVA: 0x0016FE7C File Offset: 0x0016E07C
		bool IAgentOriginBase.HasThrownWeapon
		{
			get
			{
				return this._hasThrownWeapon;
			}
		}

		// Token: 0x17000E97 RID: 3735
		// (get) Token: 0x06004921 RID: 18721 RVA: 0x0016FE84 File Offset: 0x0016E084
		bool IAgentOriginBase.HasHeavyArmor
		{
			get
			{
				return this._hasHeavyArmor;
			}
		}

		// Token: 0x17000E98 RID: 3736
		// (get) Token: 0x06004922 RID: 18722 RVA: 0x0016FE8C File Offset: 0x0016E08C
		bool IAgentOriginBase.HasShield
		{
			get
			{
				return this._hasShield;
			}
		}

		// Token: 0x17000E99 RID: 3737
		// (get) Token: 0x06004923 RID: 18723 RVA: 0x0016FE94 File Offset: 0x0016E094
		bool IAgentOriginBase.HasSpear
		{
			get
			{
				return this._hasSpear;
			}
		}

		// Token: 0x17000E9A RID: 3738
		// (get) Token: 0x06004924 RID: 18724 RVA: 0x0016FE9C File Offset: 0x0016E09C
		public bool IsUnderPlayersCommand
		{
			get
			{
				PartyBase party = this.Party;
				return party != null && (party == PartyBase.MainParty || party.Owner == Hero.MainHero || party.MapFaction.Leader == Hero.MainHero);
			}
		}

		// Token: 0x17000E9B RID: 3739
		// (get) Token: 0x06004925 RID: 18725 RVA: 0x0016FEDE File Offset: 0x0016E0DE
		public uint FactionColor
		{
			get
			{
				if (this.Party != null)
				{
					return this.Party.MapFaction.Color;
				}
				if (this._troop.IsHero)
				{
					return this._troop.HeroObject.MapFaction.Color;
				}
				return 0U;
			}
		}

		// Token: 0x17000E9C RID: 3740
		// (get) Token: 0x06004926 RID: 18726 RVA: 0x0016FF1D File Offset: 0x0016E11D
		public uint FactionColor2
		{
			get
			{
				if (this.Party != null)
				{
					return this.Party.MapFaction.Color2;
				}
				if (this._troop.IsHero)
				{
					return this._troop.HeroObject.MapFaction.Color2;
				}
				return 0U;
			}
		}

		// Token: 0x17000E9D RID: 3741
		// (get) Token: 0x06004927 RID: 18727 RVA: 0x0016FF5C File Offset: 0x0016E15C
		public int Seed
		{
			get
			{
				if (this.Party != null)
				{
					return CharacterHelper.GetPartyMemberFaceSeed(this.Party, this._troop, this.Rank);
				}
				return CharacterHelper.GetDefaultFaceSeed(this._troop, this.Rank);
			}
		}

		// Token: 0x17000E9E RID: 3742
		// (get) Token: 0x06004928 RID: 18728 RVA: 0x0016FF8F File Offset: 0x0016E18F
		public PartyBase Party
		{
			get
			{
				if (!this._troop.IsHero || this._troop.HeroObject.PartyBelongedTo == null)
				{
					return null;
				}
				return this._troop.HeroObject.PartyBelongedTo.Party;
			}
		}

		// Token: 0x17000E9F RID: 3743
		// (get) Token: 0x06004929 RID: 18729 RVA: 0x0016FFC7 File Offset: 0x0016E1C7
		public IBattleCombatant BattleCombatant
		{
			get
			{
				return this.Party;
			}
		}

		// Token: 0x17000EA0 RID: 3744
		// (get) Token: 0x0600492A RID: 18730 RVA: 0x0016FFCF File Offset: 0x0016E1CF
		public Banner Banner
		{
			get
			{
				return this._banner;
			}
		}

		// Token: 0x17000EA1 RID: 3745
		// (get) Token: 0x0600492B RID: 18731 RVA: 0x0016FFD7 File Offset: 0x0016E1D7
		// (set) Token: 0x0600492C RID: 18732 RVA: 0x0016FFDF File Offset: 0x0016E1DF
		public int Rank { get; private set; }

		// Token: 0x17000EA2 RID: 3746
		// (get) Token: 0x0600492D RID: 18733 RVA: 0x0016FFE8 File Offset: 0x0016E1E8
		public int UniqueSeed
		{
			get
			{
				return this._descriptor.UniqueSeed;
			}
		}

		// Token: 0x0600492E RID: 18734 RVA: 0x0016FFF8 File Offset: 0x0016E1F8
		public SimpleAgentOrigin(BasicCharacterObject troop, int rank = -1, Banner banner = null, UniqueTroopDescriptor descriptor = default(UniqueTroopDescriptor))
		{
			this._troop = (CharacterObject)troop;
			this._descriptor = descriptor;
			this.Rank = ((rank == -1) ? MBRandom.RandomInt(10000) : rank);
			this._banner = banner;
			AgentOriginUtilities.GetDefaultTroopTraits(this._troop, out this._hasThrownWeapon, out this._hasSpear, out this._hasShield, out this._hasHeavyArmor);
		}

		// Token: 0x0600492F RID: 18735 RVA: 0x00170060 File Offset: 0x0016E260
		public void SetWounded()
		{
		}

		// Token: 0x06004930 RID: 18736 RVA: 0x00170062 File Offset: 0x0016E262
		public void SetKilled()
		{
			if (this._troop.IsHero)
			{
				KillCharacterAction.ApplyByBattle(this._troop.HeroObject, null, true);
			}
		}

		// Token: 0x06004931 RID: 18737 RVA: 0x00170083 File Offset: 0x0016E283
		public void SetRouted(bool isOrderRetreat)
		{
		}

		// Token: 0x06004932 RID: 18738 RVA: 0x00170085 File Offset: 0x0016E285
		public void OnAgentRemoved(float agentHealth)
		{
		}

		// Token: 0x06004933 RID: 18739 RVA: 0x00170088 File Offset: 0x0016E288
		void IAgentOriginBase.OnScoreHit(BasicCharacterObject victim, BasicCharacterObject formationCaptain, int damage, bool isFatal, bool isTeamKill, WeaponComponentData attackerWeapon)
		{
			if (isTeamKill)
			{
				CharacterObject troop = this._troop;
				ExplainedNumber xpFromHit = Campaign.Current.Models.CombatXpModel.GetXpFromHit(troop, (CharacterObject)formationCaptain, (CharacterObject)victim, this.Party, damage, isFatal, CombatXpModel.MissionTypeEnum.Battle);
				if (troop.IsHero && attackerWeapon != null)
				{
					SkillObject skillForWeapon = Campaign.Current.Models.CombatXpModel.GetSkillForWeapon(attackerWeapon, false);
					troop.HeroObject.AddSkillXp(skillForWeapon, (float)xpFromHit.RoundedResultNumber);
				}
			}
		}

		// Token: 0x06004934 RID: 18740 RVA: 0x00170104 File Offset: 0x0016E304
		public void SetBanner(Banner banner)
		{
			this._banner = banner;
		}

		// Token: 0x06004935 RID: 18741 RVA: 0x0017010D File Offset: 0x0016E30D
		TroopTraitsMask IAgentOriginBase.GetTraitsMask()
		{
			return AgentOriginUtilities.GetDefaultTraitsMask(this);
		}

		// Token: 0x0400142B RID: 5163
		private CharacterObject _troop;

		// Token: 0x0400142C RID: 5164
		private bool _hasThrownWeapon;

		// Token: 0x0400142D RID: 5165
		private bool _hasHeavyArmor;

		// Token: 0x0400142E RID: 5166
		private bool _hasShield;

		// Token: 0x0400142F RID: 5167
		private bool _hasSpear;

		// Token: 0x04001430 RID: 5168
		private Banner _banner;

		// Token: 0x04001432 RID: 5170
		private UniqueTroopDescriptor _descriptor;
	}
}

using System;

namespace TaleWorlds.Core
{
	// Token: 0x0200004E RID: 78
	public class DefaultBannerEffects
	{
		// Token: 0x17000218 RID: 536
		// (get) Token: 0x06000643 RID: 1603 RVA: 0x00015B84 File Offset: 0x00013D84
		private static DefaultBannerEffects Instance
		{
			get
			{
				return Game.Current.DefaultBannerEffects;
			}
		}

		// Token: 0x17000219 RID: 537
		// (get) Token: 0x06000644 RID: 1604 RVA: 0x00015B90 File Offset: 0x00013D90
		public static BannerEffect IncreasedMeleeDamage
		{
			get
			{
				return DefaultBannerEffects.Instance._increasedMeleeDamage;
			}
		}

		// Token: 0x1700021A RID: 538
		// (get) Token: 0x06000645 RID: 1605 RVA: 0x00015B9C File Offset: 0x00013D9C
		public static BannerEffect IncreasedMeleeDamageAgainstMountedTroops
		{
			get
			{
				return DefaultBannerEffects.Instance._increasedMeleeDamageAgainstMountedTroops;
			}
		}

		// Token: 0x1700021B RID: 539
		// (get) Token: 0x06000646 RID: 1606 RVA: 0x00015BA8 File Offset: 0x00013DA8
		public static BannerEffect IncreasedRangedDamage
		{
			get
			{
				return DefaultBannerEffects.Instance._increasedRangedDamage;
			}
		}

		// Token: 0x1700021C RID: 540
		// (get) Token: 0x06000647 RID: 1607 RVA: 0x00015BB4 File Offset: 0x00013DB4
		public static BannerEffect IncreasedChargeDamage
		{
			get
			{
				return DefaultBannerEffects.Instance._increasedChargeDamage;
			}
		}

		// Token: 0x1700021D RID: 541
		// (get) Token: 0x06000648 RID: 1608 RVA: 0x00015BC0 File Offset: 0x00013DC0
		public static BannerEffect DecreasedChargeDamage
		{
			get
			{
				return DefaultBannerEffects.Instance._decreasedChargeDamage;
			}
		}

		// Token: 0x1700021E RID: 542
		// (get) Token: 0x06000649 RID: 1609 RVA: 0x00015BCC File Offset: 0x00013DCC
		public static BannerEffect DecreasedRangedAccuracyPenalty
		{
			get
			{
				return DefaultBannerEffects.Instance._decreasedRangedAccuracyPenalty;
			}
		}

		// Token: 0x1700021F RID: 543
		// (get) Token: 0x0600064A RID: 1610 RVA: 0x00015BD8 File Offset: 0x00013DD8
		public static BannerEffect DecreasedMoraleShock
		{
			get
			{
				return DefaultBannerEffects.Instance._decreasedMoraleShock;
			}
		}

		// Token: 0x17000220 RID: 544
		// (get) Token: 0x0600064B RID: 1611 RVA: 0x00015BE4 File Offset: 0x00013DE4
		public static BannerEffect DecreasedMeleeAttackDamage
		{
			get
			{
				return DefaultBannerEffects.Instance._decreasedMeleeAttackDamage;
			}
		}

		// Token: 0x17000221 RID: 545
		// (get) Token: 0x0600064C RID: 1612 RVA: 0x00015BF0 File Offset: 0x00013DF0
		public static BannerEffect DecreasedRangedAttackDamage
		{
			get
			{
				return DefaultBannerEffects.Instance._decreasedRangedAttackDamage;
			}
		}

		// Token: 0x17000222 RID: 546
		// (get) Token: 0x0600064D RID: 1613 RVA: 0x00015BFC File Offset: 0x00013DFC
		public static BannerEffect DecreasedShieldDamage
		{
			get
			{
				return DefaultBannerEffects.Instance._decreasedShieldDamage;
			}
		}

		// Token: 0x17000223 RID: 547
		// (get) Token: 0x0600064E RID: 1614 RVA: 0x00015C08 File Offset: 0x00013E08
		public static BannerEffect IncreasedTroopMovementSpeed
		{
			get
			{
				return DefaultBannerEffects.Instance._increasedTroopMovementSpeed;
			}
		}

		// Token: 0x17000224 RID: 548
		// (get) Token: 0x0600064F RID: 1615 RVA: 0x00015C14 File Offset: 0x00013E14
		public static BannerEffect IncreasedMountMovementSpeed
		{
			get
			{
				return DefaultBannerEffects.Instance._increasedMountMovementSpeed;
			}
		}

		// Token: 0x17000225 RID: 549
		// (get) Token: 0x06000650 RID: 1616 RVA: 0x00015C20 File Offset: 0x00013E20
		public static BannerEffect IncreasedMoraleShockByMeleeTroops
		{
			get
			{
				return DefaultBannerEffects.Instance._increasedMoraleShockByMeleeTroops;
			}
		}

		// Token: 0x06000651 RID: 1617 RVA: 0x00015C2C File Offset: 0x00013E2C
		public DefaultBannerEffects()
		{
			this.RegisterAll();
		}

		// Token: 0x06000652 RID: 1618 RVA: 0x00015C3C File Offset: 0x00013E3C
		private void RegisterAll()
		{
			this._increasedMeleeDamage = this.Create("IncreasedMeleeDamage");
			this._increasedMeleeDamageAgainstMountedTroops = this.Create("IncreasedMeleeDamageAgainstMountedTroops");
			this._increasedRangedDamage = this.Create("IncreasedRangedDamage");
			this._increasedChargeDamage = this.Create("IncreasedChargeDamage");
			this._decreasedChargeDamage = this.Create("DecreasedChargeDamage");
			this._decreasedRangedAccuracyPenalty = this.Create("DecreasedRangedAccuracyPenalty");
			this._decreasedMoraleShock = this.Create("DecreasedMoraleShock");
			this._decreasedMeleeAttackDamage = this.Create("DecreasedMeleeAttackDamage");
			this._decreasedRangedAttackDamage = this.Create("DecreasedRangedAttackDamage");
			this._decreasedShieldDamage = this.Create("DecreasedShieldDamage");
			this._increasedTroopMovementSpeed = this.Create("IncreasedTroopMovementSpeed");
			this._increasedMountMovementSpeed = this.Create("IncreasedMountMovementSpeed");
			this._increasedMoraleShockByMeleeTroops = this.Create("IncreasedMoraleShockByMeleeTroops");
			this.InitializeAll();
		}

		// Token: 0x06000653 RID: 1619 RVA: 0x00015D2C File Offset: 0x00013F2C
		private BannerEffect Create(string stringId)
		{
			return Game.Current.ObjectManager.RegisterPresumedObject<BannerEffect>(new BannerEffect(stringId));
		}

		// Token: 0x06000654 RID: 1620 RVA: 0x00015D44 File Offset: 0x00013F44
		private void InitializeAll()
		{
			this._increasedMeleeDamage.Initialize("{=unaWKloT}Increased Melee Damage", "{=8ZNOgT8Z}{BONUS_AMOUNT}% melee damage to troops in your formation.", 0.05f, 0.1f, 0.15f, EffectIncrementType.AddFactor);
			this._increasedMeleeDamageAgainstMountedTroops.Initialize("{=t0Qzb7CY}Increased Melee Damage Against Mounted Troops", "{=sxGmF0tC}{BONUS_AMOUNT}% melee damage by troops in your formation against cavalry.", 0.1f, 0.2f, 0.3f, EffectIncrementType.AddFactor);
			this._increasedRangedDamage.Initialize("{=Ch5NpCd0}Increased Ranged Damage", "{=labbKop6}{BONUS_AMOUNT}% ranged damage to troops in your formation.", 0.04f, 0.06f, 0.08f, EffectIncrementType.AddFactor);
			this._increasedChargeDamage.Initialize("{=O2oBC9sH}Increased Charge Damage", "{=Z2xgnrDa}{BONUS_AMOUNT}% charge damage to mounted troops in your formation.", 0.1f, 0.2f, 0.3f, EffectIncrementType.AddFactor);
			this._decreasedChargeDamage.Initialize("{=PkFT0D9a}Decreased Charge Damage", "{=Z2xgnrDa}{BONUS_AMOUNT}% charge damage to mounted troops in your formation.", -0.1f, -0.2f, -0.3f, EffectIncrementType.AddFactor);
			this._decreasedRangedAccuracyPenalty.Initialize("{=MkBPRCuF}Decreased Ranged Accuracy Penalty", "{=Gu0Wxxul}{BONUS_AMOUNT}% accuracy penalty for ranged troops in your formation.", -0.04f, -0.06f, -0.08f, EffectIncrementType.AddFactor);
			this._decreasedMoraleShock.Initialize("{=nOMT0Cw6}Decreased Morale Shock", "{=W0agPHes}{BONUS_AMOUNT}% morale penalty from casualties to troops in your formation.", -0.1f, -0.2f, -0.3f, EffectIncrementType.AddFactor);
			this._decreasedMeleeAttackDamage.Initialize("{=a3Vc59WV}Decreased Taken Melee Attack Damage", "{=ORFrCYSn}{BONUS_AMOUNT}% damage by melee attacks to troops in your formation.", -0.05f, -0.1f, -0.15f, EffectIncrementType.AddFactor);
			this._decreasedRangedAttackDamage.Initialize("{=p0JFbL7G}Decreased Taken Ranged Attack Damage", "{=W0agPHes}{BONUS_AMOUNT}% morale penalty from casualties to troops in your formation.", -0.05f, -0.1f, -0.15f, EffectIncrementType.AddFactor);
			this._decreasedShieldDamage.Initialize("{=T79exjaP}Decreased Taken Shield Damage", "{=klGEDUmw}{BONUS_AMOUNT}% damage to shields of troops in your formation.", -0.15f, -0.25f, -0.3f, EffectIncrementType.AddFactor);
			this._increasedTroopMovementSpeed.Initialize("{=PbJAOKKZ}Increased Troop Movement Speed", "{=nqWulUTP}{BONUS_AMOUNT}% movement speed to infantry in your formation.", 0.15f, 0.25f, 0.3f, EffectIncrementType.AddFactor);
			this._increasedMountMovementSpeed.Initialize("{=nMfxbc0Y}Increased Mount Movement Speed", "{=g0l7W5xQ}{BONUS_AMOUNT}% movement speed to mounts in your formation.", 0.05f, 0.08f, 0.1f, EffectIncrementType.AddFactor);
			this._increasedMoraleShockByMeleeTroops.Initialize("{=nOMT0Cw6}Increased Morale Shock", "{=!}INCREASED MORALE SHOCK BY MELEE TROOPS DESCRIPTION", 0.1f, 0.2f, 0.3f, EffectIncrementType.AddFactor);
		}

		// Token: 0x040002FB RID: 763
		private BannerEffect _increasedMeleeDamage;

		// Token: 0x040002FC RID: 764
		private BannerEffect _increasedMeleeDamageAgainstMountedTroops;

		// Token: 0x040002FD RID: 765
		private BannerEffect _increasedRangedDamage;

		// Token: 0x040002FE RID: 766
		private BannerEffect _increasedChargeDamage;

		// Token: 0x040002FF RID: 767
		private BannerEffect _decreasedChargeDamage;

		// Token: 0x04000300 RID: 768
		private BannerEffect _decreasedRangedAccuracyPenalty;

		// Token: 0x04000301 RID: 769
		private BannerEffect _decreasedMoraleShock;

		// Token: 0x04000302 RID: 770
		private BannerEffect _decreasedMeleeAttackDamage;

		// Token: 0x04000303 RID: 771
		private BannerEffect _decreasedRangedAttackDamage;

		// Token: 0x04000304 RID: 772
		private BannerEffect _decreasedShieldDamage;

		// Token: 0x04000305 RID: 773
		private BannerEffect _increasedTroopMovementSpeed;

		// Token: 0x04000306 RID: 774
		private BannerEffect _increasedMountMovementSpeed;

		// Token: 0x04000307 RID: 775
		private BannerEffect _increasedMoraleShockByMeleeTroops;
	}
}

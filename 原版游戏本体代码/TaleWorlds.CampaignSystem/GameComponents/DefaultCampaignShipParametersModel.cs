using System;
using TaleWorlds.CampaignSystem.ComponentInterfaces;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.GameComponents
{
	// Token: 0x020000FD RID: 253
	public class DefaultCampaignShipParametersModel : CampaignShipParametersModel
	{
		// Token: 0x06001686 RID: 5766 RVA: 0x00067448 File Offset: 0x00065648
		public override float GetShipSizeWeatherFactor(ShipHull shipHull)
		{
			return 0f;
		}

		// Token: 0x06001687 RID: 5767 RVA: 0x0006744F File Offset: 0x0006564F
		public override float GetDefaultCombatFactor(ShipHull shipHull)
		{
			return 0f;
		}

		// Token: 0x06001688 RID: 5768 RVA: 0x00067456 File Offset: 0x00065656
		public override float GetCampaignSpeedBonusFactor(Ship ship)
		{
			return 0f;
		}

		// Token: 0x06001689 RID: 5769 RVA: 0x0006745D File Offset: 0x0006565D
		public override float GetCrewCapacityBonusFactor(Ship ship)
		{
			return 0f;
		}

		// Token: 0x0600168A RID: 5770 RVA: 0x00067464 File Offset: 0x00065664
		public override float GetShipWeightFactor(Ship ship)
		{
			return 0f;
		}

		// Token: 0x0600168B RID: 5771 RVA: 0x0006746B File Offset: 0x0006566B
		public override float GetForwardDragFactor(Ship ship)
		{
			return 0f;
		}

		// Token: 0x0600168C RID: 5772 RVA: 0x00067472 File Offset: 0x00065672
		public override float GetCrewShieldHitPointsFactor(Ship ship)
		{
			return 0f;
		}

		// Token: 0x0600168D RID: 5773 RVA: 0x00067479 File Offset: 0x00065679
		public override int GetAdditionalAmmoBonus(Ship ship)
		{
			return 0;
		}

		// Token: 0x0600168E RID: 5774 RVA: 0x0006747C File Offset: 0x0006567C
		public override float GetMaxOarPowerFactor(Ship ship)
		{
			return 0f;
		}

		// Token: 0x0600168F RID: 5775 RVA: 0x00067483 File Offset: 0x00065683
		public override float GetMaxOarForceFactor(Ship ship)
		{
			return 0f;
		}

		// Token: 0x06001690 RID: 5776 RVA: 0x0006748A File Offset: 0x0006568A
		public override float GetSailForceFactor(Ship ship)
		{
			return 0f;
		}

		// Token: 0x06001691 RID: 5777 RVA: 0x00067491 File Offset: 0x00065691
		public override float GetCrewMeleeDamageFactor(Ship ship)
		{
			return 0f;
		}

		// Token: 0x06001692 RID: 5778 RVA: 0x00067498 File Offset: 0x00065698
		public override int GetAdditionalArcherQuivers(Ship ship)
		{
			return 0;
		}

		// Token: 0x06001693 RID: 5779 RVA: 0x0006749B File Offset: 0x0006569B
		public override int GetAdditionalThrowingWeaponStack(Ship ship)
		{
			return 0;
		}

		// Token: 0x06001694 RID: 5780 RVA: 0x0006749E File Offset: 0x0006569E
		public override float GetSailRotationSpeedFactor(Ship ship)
		{
			return 0f;
		}

		// Token: 0x06001695 RID: 5781 RVA: 0x000674A5 File Offset: 0x000656A5
		public override float GetFurlUnfurlSpeedFactor(Ship ship)
		{
			return 0f;
		}
	}
}

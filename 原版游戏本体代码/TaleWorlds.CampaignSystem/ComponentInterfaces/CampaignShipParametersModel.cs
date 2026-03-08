using System;
using TaleWorlds.CampaignSystem.Naval;
using TaleWorlds.Core;

namespace TaleWorlds.CampaignSystem.ComponentInterfaces
{
	// Token: 0x020001FF RID: 511
	public abstract class CampaignShipParametersModel : MBGameModel<CampaignShipParametersModel>
	{
		// Token: 0x06001F55 RID: 8021
		public abstract float GetShipSizeWeatherFactor(ShipHull shipHull);

		// Token: 0x06001F56 RID: 8022
		public abstract float GetDefaultCombatFactor(ShipHull shipHull);

		// Token: 0x06001F57 RID: 8023
		public abstract float GetCampaignSpeedBonusFactor(Ship ship);

		// Token: 0x06001F58 RID: 8024
		public abstract float GetCrewCapacityBonusFactor(Ship ship);

		// Token: 0x06001F59 RID: 8025
		public abstract float GetShipWeightFactor(Ship ship);

		// Token: 0x06001F5A RID: 8026
		public abstract float GetForwardDragFactor(Ship ship);

		// Token: 0x06001F5B RID: 8027
		public abstract float GetCrewShieldHitPointsFactor(Ship ship);

		// Token: 0x06001F5C RID: 8028
		public abstract int GetAdditionalAmmoBonus(Ship ship);

		// Token: 0x06001F5D RID: 8029
		public abstract float GetMaxOarPowerFactor(Ship ship);

		// Token: 0x06001F5E RID: 8030
		public abstract float GetMaxOarForceFactor(Ship ship);

		// Token: 0x06001F5F RID: 8031
		public abstract float GetSailForceFactor(Ship ship);

		// Token: 0x06001F60 RID: 8032
		public abstract float GetCrewMeleeDamageFactor(Ship ship);

		// Token: 0x06001F61 RID: 8033
		public abstract int GetAdditionalArcherQuivers(Ship ship);

		// Token: 0x06001F62 RID: 8034
		public abstract int GetAdditionalThrowingWeaponStack(Ship ship);

		// Token: 0x06001F63 RID: 8035
		public abstract float GetSailRotationSpeedFactor(Ship ship);

		// Token: 0x06001F64 RID: 8036
		public abstract float GetFurlUnfurlSpeedFactor(Ship ship);
	}
}

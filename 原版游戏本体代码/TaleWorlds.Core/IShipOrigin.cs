using System;
using System.Collections.Generic;
using TaleWorlds.Localization;

namespace TaleWorlds.Core
{
	// Token: 0x02000093 RID: 147
	public interface IShipOrigin
	{
		// Token: 0x170002F2 RID: 754
		// (get) Token: 0x060008AF RID: 2223
		ShipHull Hull { get; }

		// Token: 0x170002F3 RID: 755
		// (get) Token: 0x060008B0 RID: 2224
		TextObject Name { get; }

		// Token: 0x170002F4 RID: 756
		// (get) Token: 0x060008B1 RID: 2225
		string OriginShipId { get; }

		// Token: 0x170002F5 RID: 757
		// (get) Token: 0x060008B2 RID: 2226
		bool IsPlayerShip { get; }

		// Token: 0x170002F6 RID: 758
		// (get) Token: 0x060008B3 RID: 2227
		float HitPoints { get; }

		// Token: 0x170002F7 RID: 759
		// (get) Token: 0x060008B4 RID: 2228
		float MaxHitPoints { get; }

		// Token: 0x170002F8 RID: 760
		// (get) Token: 0x060008B5 RID: 2229
		float MaxFireHitPoints { get; }

		// Token: 0x170002F9 RID: 761
		// (get) Token: 0x060008B6 RID: 2230
		float SailHitPoints { get; }

		// Token: 0x170002FA RID: 762
		// (get) Token: 0x060008B7 RID: 2231
		float MaxSailHitPoints { get; }

		// Token: 0x170002FB RID: 763
		// (get) Token: 0x060008B8 RID: 2232
		int TotalCrewCapacity { get; }

		// Token: 0x170002FC RID: 764
		// (get) Token: 0x060008B9 RID: 2233
		int MainDeckCrewCapacity { get; }

		// Token: 0x170002FD RID: 765
		// (get) Token: 0x060008BA RID: 2234
		int SkeletalCrewCapacity { get; }

		// Token: 0x170002FE RID: 766
		// (get) Token: 0x060008BB RID: 2235
		int DefaultFormationGroupIndex { get; }

		// Token: 0x170002FF RID: 767
		// (get) Token: 0x060008BC RID: 2236
		float ForwardDragFactor { get; }

		// Token: 0x17000300 RID: 768
		// (get) Token: 0x060008BD RID: 2237
		float ShipWeightFactor { get; }

		// Token: 0x17000301 RID: 769
		// (get) Token: 0x060008BE RID: 2238
		float RudderSurfaceAreaFactor { get; }

		// Token: 0x17000302 RID: 770
		// (get) Token: 0x060008BF RID: 2239
		int RandomValue { get; }

		// Token: 0x17000303 RID: 771
		// (get) Token: 0x060008C0 RID: 2240
		string CustomSailPatternId { get; }

		// Token: 0x17000304 RID: 772
		// (get) Token: 0x060008C1 RID: 2241
		float MaxRudderForceFactor { get; }

		// Token: 0x17000305 RID: 773
		// (get) Token: 0x060008C2 RID: 2242
		float MaxOarForceFactor { get; }

		// Token: 0x17000306 RID: 774
		// (get) Token: 0x060008C3 RID: 2243
		float SailForceFactor { get; }

		// Token: 0x17000307 RID: 775
		// (get) Token: 0x060008C4 RID: 2244
		float MaxOarPowerFactor { get; }

		// Token: 0x17000308 RID: 776
		// (get) Token: 0x060008C5 RID: 2245
		float SailRotationSpeedFactor { get; }

		// Token: 0x17000309 RID: 777
		// (get) Token: 0x060008C6 RID: 2246
		float FurlUnfurlSpeedFactor { get; }

		// Token: 0x1700030A RID: 778
		// (get) Token: 0x060008C7 RID: 2247
		float CrewShieldHitPointsFactor { get; }

		// Token: 0x1700030B RID: 779
		// (get) Token: 0x060008C8 RID: 2248
		float CrewMeleeDamageFactor { get; }

		// Token: 0x1700030C RID: 780
		// (get) Token: 0x060008C9 RID: 2249
		int AdditionalArcherQuivers { get; }

		// Token: 0x1700030D RID: 781
		// (get) Token: 0x060008CA RID: 2250
		int AdditionalThrowingWeaponStack { get; }

		// Token: 0x060008CB RID: 2251
		void OnShipDamaged(float rawDamage, IShipOrigin rammingShip, out float modifiedDamage);

		// Token: 0x060008CC RID: 2252
		void OnSailDamaged(float rawDamage);

		// Token: 0x060008CD RID: 2253
		List<ShipVisualSlotInfo> GetShipVisualSlotInfos();

		// Token: 0x060008CE RID: 2254
		List<ShipSlotAndPieceName> GetShipSlotAndPieceNames();
	}
}

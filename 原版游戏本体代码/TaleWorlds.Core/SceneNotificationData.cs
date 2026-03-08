using System;
using System.Collections.Generic;
using TaleWorlds.Localization;

namespace TaleWorlds.Core
{
	// Token: 0x020000CA RID: 202
	public class SceneNotificationData
	{
		// Token: 0x170003BA RID: 954
		// (get) Token: 0x06000ADD RID: 2781 RVA: 0x0002333D File Offset: 0x0002153D
		public virtual string SceneID { get; }

		// Token: 0x170003BB RID: 955
		// (get) Token: 0x06000ADE RID: 2782 RVA: 0x00023345 File Offset: 0x00021545
		public virtual string SoundEventPath { get; }

		// Token: 0x170003BC RID: 956
		// (get) Token: 0x06000ADF RID: 2783 RVA: 0x0002334D File Offset: 0x0002154D
		public virtual TextObject TitleText { get; }

		// Token: 0x170003BD RID: 957
		// (get) Token: 0x06000AE0 RID: 2784 RVA: 0x00023355 File Offset: 0x00021555
		public virtual TextObject AffirmativeDescriptionText { get; }

		// Token: 0x170003BE RID: 958
		// (get) Token: 0x06000AE1 RID: 2785 RVA: 0x0002335D File Offset: 0x0002155D
		public virtual TextObject NegativeDescriptionText { get; }

		// Token: 0x170003BF RID: 959
		// (get) Token: 0x06000AE2 RID: 2786 RVA: 0x00023365 File Offset: 0x00021565
		public virtual TextObject AffirmativeHintText { get; }

		// Token: 0x170003C0 RID: 960
		// (get) Token: 0x06000AE3 RID: 2787 RVA: 0x0002336D File Offset: 0x0002156D
		public virtual TextObject AffirmativeHintTextExtended { get; }

		// Token: 0x170003C1 RID: 961
		// (get) Token: 0x06000AE4 RID: 2788 RVA: 0x00023375 File Offset: 0x00021575
		public virtual TextObject AffirmativeTitleText { get; }

		// Token: 0x170003C2 RID: 962
		// (get) Token: 0x06000AE5 RID: 2789 RVA: 0x0002337D File Offset: 0x0002157D
		public virtual TextObject NegativeTitleText { get; }

		// Token: 0x170003C3 RID: 963
		// (get) Token: 0x06000AE6 RID: 2790 RVA: 0x00023385 File Offset: 0x00021585
		public virtual TextObject AffirmativeText { get; }

		// Token: 0x170003C4 RID: 964
		// (get) Token: 0x06000AE7 RID: 2791 RVA: 0x0002338D File Offset: 0x0002158D
		public virtual TextObject NegativeText { get; }

		// Token: 0x170003C5 RID: 965
		// (get) Token: 0x06000AE8 RID: 2792 RVA: 0x00023395 File Offset: 0x00021595
		public virtual bool IsAffirmativeOptionShown { get; }

		// Token: 0x170003C6 RID: 966
		// (get) Token: 0x06000AE9 RID: 2793 RVA: 0x0002339D File Offset: 0x0002159D
		public virtual bool IsNegativeOptionShown { get; }

		// Token: 0x170003C7 RID: 967
		// (get) Token: 0x06000AEA RID: 2794 RVA: 0x000233A5 File Offset: 0x000215A5
		public virtual bool PauseActiveState { get; } = 1;

		// Token: 0x170003C8 RID: 968
		// (get) Token: 0x06000AEB RID: 2795 RVA: 0x000233AD File Offset: 0x000215AD
		public virtual SceneNotificationData.RelevantContextType RelevantContext { get; }

		// Token: 0x170003C9 RID: 969
		// (get) Token: 0x06000AEC RID: 2796 RVA: 0x000233B5 File Offset: 0x000215B5
		public virtual SceneNotificationData.NotificationSceneProperties SceneProperties { get; } = new SceneNotificationData.NotificationSceneProperties
		{
			InitializePhysics = false,
			DisableStaticShadows = false,
			OverriddenWaterStrength = null
		};

		// Token: 0x06000AED RID: 2797 RVA: 0x000233BD File Offset: 0x000215BD
		public virtual void OnAffirmativeAction()
		{
		}

		// Token: 0x06000AEE RID: 2798 RVA: 0x000233BF File Offset: 0x000215BF
		public virtual void OnNegativeAction()
		{
		}

		// Token: 0x06000AEF RID: 2799 RVA: 0x000233C1 File Offset: 0x000215C1
		public virtual void OnCloseAction()
		{
		}

		// Token: 0x06000AF0 RID: 2800 RVA: 0x000233C3 File Offset: 0x000215C3
		public virtual Banner[] GetBanners()
		{
			return Array.Empty<Banner>();
		}

		// Token: 0x06000AF1 RID: 2801 RVA: 0x000233CA File Offset: 0x000215CA
		public virtual SceneNotificationData.SceneNotificationCharacter[] GetSceneNotificationCharacters()
		{
			return Array.Empty<SceneNotificationData.SceneNotificationCharacter>();
		}

		// Token: 0x06000AF2 RID: 2802 RVA: 0x000233D1 File Offset: 0x000215D1
		public virtual SceneNotificationData.SceneNotificationShip[] GetShips()
		{
			return Array.Empty<SceneNotificationData.SceneNotificationShip>();
		}

		// Token: 0x02000128 RID: 296
		public readonly struct SceneNotificationCharacter
		{
			// Token: 0x06000C15 RID: 3093 RVA: 0x000264BC File Offset: 0x000246BC
			public SceneNotificationCharacter(BasicCharacterObject character, Equipment overriddenEquipment = null, BodyProperties overriddenBodyProperties = default(BodyProperties), bool useCivilianEquipment = false, uint customColor1 = 4294967295U, uint customColor2 = 4294967295U, bool useHorse = false)
			{
				this.Character = character;
				this.OverriddenEquipment = overriddenEquipment;
				this.OverriddenBodyProperties = overriddenBodyProperties;
				this.UseCivilianEquipment = useCivilianEquipment;
				this.CustomColor1 = customColor1;
				this.CustomColor2 = customColor2;
				this.UseHorse = useHorse;
			}

			// Token: 0x040007D1 RID: 2001
			public readonly BasicCharacterObject Character;

			// Token: 0x040007D2 RID: 2002
			public readonly Equipment OverriddenEquipment;

			// Token: 0x040007D3 RID: 2003
			public readonly BodyProperties OverriddenBodyProperties;

			// Token: 0x040007D4 RID: 2004
			public readonly bool UseCivilianEquipment;

			// Token: 0x040007D5 RID: 2005
			public readonly bool UseHorse;

			// Token: 0x040007D6 RID: 2006
			public readonly uint CustomColor1;

			// Token: 0x040007D7 RID: 2007
			public readonly uint CustomColor2;
		}

		// Token: 0x02000129 RID: 297
		public readonly struct SceneNotificationShip
		{
			// Token: 0x06000C16 RID: 3094 RVA: 0x000264F3 File Offset: 0x000246F3
			public SceneNotificationShip(string shipPrefabId, List<ShipVisualSlotInfo> shipUpgrades, float shipHitPointRatio, uint sailColor1, uint sailColor2, int shipSeed)
			{
				this.ShipPrefabId = shipPrefabId;
				this.ShipUpgrades = shipUpgrades;
				this.ShipHitPointRatio = shipHitPointRatio;
				this.SailColor1 = sailColor1;
				this.SailColor2 = sailColor2;
				this.ShipSeed = shipSeed;
			}

			// Token: 0x040007D8 RID: 2008
			public readonly string ShipPrefabId;

			// Token: 0x040007D9 RID: 2009
			public readonly List<ShipVisualSlotInfo> ShipUpgrades;

			// Token: 0x040007DA RID: 2010
			public readonly float ShipHitPointRatio;

			// Token: 0x040007DB RID: 2011
			public readonly uint SailColor1;

			// Token: 0x040007DC RID: 2012
			public readonly uint SailColor2;

			// Token: 0x040007DD RID: 2013
			public readonly int ShipSeed;
		}

		// Token: 0x0200012A RID: 298
		public struct NotificationSceneProperties
		{
			// Token: 0x040007DE RID: 2014
			public bool InitializePhysics;

			// Token: 0x040007DF RID: 2015
			public bool DisableStaticShadows;

			// Token: 0x040007E0 RID: 2016
			public float? OverriddenWaterStrength;
		}

		// Token: 0x0200012B RID: 299
		public enum RelevantContextType
		{
			// Token: 0x040007E2 RID: 2018
			Any,
			// Token: 0x040007E3 RID: 2019
			MPLobby,
			// Token: 0x040007E4 RID: 2020
			CustomBattle,
			// Token: 0x040007E5 RID: 2021
			Mission,
			// Token: 0x040007E6 RID: 2022
			Map
		}
	}
}

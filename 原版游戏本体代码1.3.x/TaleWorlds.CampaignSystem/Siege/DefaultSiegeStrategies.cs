using System;
using System.Collections.Generic;
using TaleWorlds.Core;
using TaleWorlds.Localization;

namespace TaleWorlds.CampaignSystem.Siege
{
	// Token: 0x020002D9 RID: 729
	public class DefaultSiegeStrategies
	{
		// Token: 0x170009AD RID: 2477
		// (get) Token: 0x060027BE RID: 10174 RVA: 0x000A59B5 File Offset: 0x000A3BB5
		private static DefaultSiegeStrategies Instance
		{
			get
			{
				return Campaign.Current.DefaultSiegeStrategies;
			}
		}

		// Token: 0x170009AE RID: 2478
		// (get) Token: 0x060027BF RID: 10175 RVA: 0x000A59C1 File Offset: 0x000A3BC1
		public static SiegeStrategy PreserveStrength
		{
			get
			{
				return DefaultSiegeStrategies.Instance._preserveStrength;
			}
		}

		// Token: 0x170009AF RID: 2479
		// (get) Token: 0x060027C0 RID: 10176 RVA: 0x000A59CD File Offset: 0x000A3BCD
		public static SiegeStrategy PrepareAgainstAssault
		{
			get
			{
				return DefaultSiegeStrategies.Instance._prepareAgainstAssault;
			}
		}

		// Token: 0x170009B0 RID: 2480
		// (get) Token: 0x060027C1 RID: 10177 RVA: 0x000A59D9 File Offset: 0x000A3BD9
		public static SiegeStrategy CounterBombardment
		{
			get
			{
				return DefaultSiegeStrategies.Instance._counterBombardment;
			}
		}

		// Token: 0x170009B1 RID: 2481
		// (get) Token: 0x060027C2 RID: 10178 RVA: 0x000A59E5 File Offset: 0x000A3BE5
		public static SiegeStrategy PrepareAssault
		{
			get
			{
				return DefaultSiegeStrategies.Instance._prepareAssault;
			}
		}

		// Token: 0x170009B2 RID: 2482
		// (get) Token: 0x060027C3 RID: 10179 RVA: 0x000A59F1 File Offset: 0x000A3BF1
		public static SiegeStrategy BreachWalls
		{
			get
			{
				return DefaultSiegeStrategies.Instance._breachWalls;
			}
		}

		// Token: 0x170009B3 RID: 2483
		// (get) Token: 0x060027C4 RID: 10180 RVA: 0x000A59FD File Offset: 0x000A3BFD
		public static SiegeStrategy WearOutDefenders
		{
			get
			{
				return DefaultSiegeStrategies.Instance._wearOutDefenders;
			}
		}

		// Token: 0x170009B4 RID: 2484
		// (get) Token: 0x060027C5 RID: 10181 RVA: 0x000A5A09 File Offset: 0x000A3C09
		public static SiegeStrategy Custom
		{
			get
			{
				return DefaultSiegeStrategies.Instance._custom;
			}
		}

		// Token: 0x170009B5 RID: 2485
		// (get) Token: 0x060027C6 RID: 10182 RVA: 0x000A5A15 File Offset: 0x000A3C15
		public static IEnumerable<SiegeStrategy> AllAttackerStrategies
		{
			get
			{
				yield return DefaultSiegeStrategies.PrepareAssault;
				yield return DefaultSiegeStrategies.BreachWalls;
				yield return DefaultSiegeStrategies.WearOutDefenders;
				yield return DefaultSiegeStrategies.PreserveStrength;
				yield return DefaultSiegeStrategies.Custom;
				yield break;
			}
		}

		// Token: 0x170009B6 RID: 2486
		// (get) Token: 0x060027C7 RID: 10183 RVA: 0x000A5A1E File Offset: 0x000A3C1E
		public static IEnumerable<SiegeStrategy> AllDefenderStrategies
		{
			get
			{
				yield return DefaultSiegeStrategies.PrepareAgainstAssault;
				yield return DefaultSiegeStrategies.CounterBombardment;
				yield return DefaultSiegeStrategies.PreserveStrength;
				yield return DefaultSiegeStrategies.Custom;
				yield break;
			}
		}

		// Token: 0x060027C8 RID: 10184 RVA: 0x000A5A27 File Offset: 0x000A3C27
		public DefaultSiegeStrategies()
		{
			this.RegisterAll();
		}

		// Token: 0x060027C9 RID: 10185 RVA: 0x000A5A38 File Offset: 0x000A3C38
		private void RegisterAll()
		{
			this._preserveStrength = this.Create("siege_strategy_preserve_strength");
			this._prepareAgainstAssault = this.Create("siege_strategy_prepare_against_assault");
			this._counterBombardment = this.Create("siege_strategy_counter_bombardment");
			this._prepareAssault = this.Create("siege_strategy_prepare_assault");
			this._breachWalls = this.Create("siege_strategy_breach_walls");
			this._wearOutDefenders = this.Create("siege_strategy_wear_out_defenders");
			this._custom = this.Create("siege_strategy_custom");
			this.InitializeAll();
		}

		// Token: 0x060027CA RID: 10186 RVA: 0x000A5AC2 File Offset: 0x000A3CC2
		private SiegeStrategy Create(string stringId)
		{
			return Game.Current.ObjectManager.RegisterPresumedObject<SiegeStrategy>(new SiegeStrategy(stringId));
		}

		// Token: 0x060027CB RID: 10187 RVA: 0x000A5ADC File Offset: 0x000A3CDC
		private void InitializeAll()
		{
			this._custom.Initialize(new TextObject("{=!}Custom", null), new TextObject("{=!}Custom strategy that can be managed entirely.", null));
			this._preserveStrength.Initialize(new TextObject("{=!}Preserve Strength", null), new TextObject("{=!}Priority is set to preserving our strength.", null));
			this._prepareAgainstAssault.Initialize(new TextObject("{=!}Prepare Against Assault", null), new TextObject("{=!}Priority is set to keep advantage when the enemies' assault starts.", null));
			this._counterBombardment.Initialize(new TextObject("{=!}Counter Bombardment", null), new TextObject("{=!}Priority is set to countering enemy bombardment.", null));
			this._prepareAssault.Initialize(new TextObject("{=!}Prepare Assault", null), new TextObject("{=!}Priority is set to assaulting the walls.", null));
			this._breachWalls.Initialize(new TextObject("{=!}Breach Walls", null), new TextObject("{=!}Priority is set to breaching the walls.", null));
			this._wearOutDefenders.Initialize(new TextObject("{=!}Wear out Defenders", null), new TextObject("{=!}Priority is set to destroying engines of the enemy.", null));
		}

		// Token: 0x04000B88 RID: 2952
		private SiegeStrategy _preserveStrength;

		// Token: 0x04000B89 RID: 2953
		private SiegeStrategy _prepareAgainstAssault;

		// Token: 0x04000B8A RID: 2954
		private SiegeStrategy _counterBombardment;

		// Token: 0x04000B8B RID: 2955
		private SiegeStrategy _prepareAssault;

		// Token: 0x04000B8C RID: 2956
		private SiegeStrategy _breachWalls;

		// Token: 0x04000B8D RID: 2957
		private SiegeStrategy _wearOutDefenders;

		// Token: 0x04000B8E RID: 2958
		private SiegeStrategy _custom;
	}
}

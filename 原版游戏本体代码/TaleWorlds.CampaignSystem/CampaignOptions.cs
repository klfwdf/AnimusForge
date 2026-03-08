using System;
using System.Collections.Generic;
using TaleWorlds.SaveSystem;

namespace TaleWorlds.CampaignSystem
{
	// Token: 0x0200005D RID: 93
	public class CampaignOptions
	{
		// Token: 0x170001E1 RID: 481
		// (get) Token: 0x06000949 RID: 2377 RVA: 0x00027E6F File Offset: 0x0002606F
		private static CampaignOptions _current
		{
			get
			{
				Campaign campaign = Campaign.Current;
				if (campaign == null)
				{
					return null;
				}
				return campaign.Options;
			}
		}

		// Token: 0x170001E2 RID: 482
		// (get) Token: 0x0600094A RID: 2378 RVA: 0x00027E81 File Offset: 0x00026081
		// (set) Token: 0x0600094B RID: 2379 RVA: 0x00027E93 File Offset: 0x00026093
		public static bool IsLifeDeathCycleDisabled
		{
			get
			{
				CampaignOptions current = CampaignOptions._current;
				return current != null && current._isLifeDeathCycleDisabled;
			}
			set
			{
				if (CampaignOptions._current != null)
				{
					CampaignOptions._current._isLifeDeathCycleDisabled = value;
				}
			}
		}

		// Token: 0x170001E3 RID: 483
		// (get) Token: 0x0600094C RID: 2380 RVA: 0x00027EA7 File Offset: 0x000260A7
		// (set) Token: 0x0600094D RID: 2381 RVA: 0x00027EB9 File Offset: 0x000260B9
		public static bool AutoAllocateClanMemberPerks
		{
			get
			{
				CampaignOptions current = CampaignOptions._current;
				return current != null && current._autoAllocateClanMemberPerks;
			}
			set
			{
				if (CampaignOptions._current != null)
				{
					CampaignOptions._current._autoAllocateClanMemberPerks = value;
				}
			}
		}

		// Token: 0x170001E4 RID: 484
		// (get) Token: 0x0600094E RID: 2382 RVA: 0x00027ECD File Offset: 0x000260CD
		// (set) Token: 0x0600094F RID: 2383 RVA: 0x00027EDF File Offset: 0x000260DF
		public static bool IsIronmanMode
		{
			get
			{
				CampaignOptions current = CampaignOptions._current;
				return current != null && current._isIronmanMode;
			}
			set
			{
				if (CampaignOptions._current != null)
				{
					CampaignOptions._current._isIronmanMode = value;
				}
			}
		}

		// Token: 0x170001E5 RID: 485
		// (get) Token: 0x06000950 RID: 2384 RVA: 0x00027EF3 File Offset: 0x000260F3
		// (set) Token: 0x06000951 RID: 2385 RVA: 0x00027F05 File Offset: 0x00026105
		public static CampaignOptions.Difficulty PlayerTroopsReceivedDamage
		{
			get
			{
				CampaignOptions current = CampaignOptions._current;
				if (current == null)
				{
					return CampaignOptions.Difficulty.Realistic;
				}
				return current._playerTroopsReceivedDamage;
			}
			set
			{
				if (CampaignOptions._current != null)
				{
					CampaignOptions._current._playerTroopsReceivedDamage = value;
				}
			}
		}

		// Token: 0x170001E6 RID: 486
		// (get) Token: 0x06000952 RID: 2386 RVA: 0x00027F19 File Offset: 0x00026119
		// (set) Token: 0x06000953 RID: 2387 RVA: 0x00027F2B File Offset: 0x0002612B
		public static CampaignOptions.Difficulty RecruitmentDifficulty
		{
			get
			{
				CampaignOptions current = CampaignOptions._current;
				if (current == null)
				{
					return CampaignOptions.Difficulty.Realistic;
				}
				return current._recruitmentDifficulty;
			}
			set
			{
				if (CampaignOptions._current != null)
				{
					CampaignOptions._current._recruitmentDifficulty = value;
				}
			}
		}

		// Token: 0x170001E7 RID: 487
		// (get) Token: 0x06000954 RID: 2388 RVA: 0x00027F3F File Offset: 0x0002613F
		// (set) Token: 0x06000955 RID: 2389 RVA: 0x00027F51 File Offset: 0x00026151
		public static CampaignOptions.Difficulty PlayerMapMovementSpeed
		{
			get
			{
				CampaignOptions current = CampaignOptions._current;
				if (current == null)
				{
					return CampaignOptions.Difficulty.Realistic;
				}
				return current._playerMapMovementSpeed;
			}
			set
			{
				if (CampaignOptions._current != null)
				{
					CampaignOptions._current._playerMapMovementSpeed = value;
				}
			}
		}

		// Token: 0x170001E8 RID: 488
		// (get) Token: 0x06000956 RID: 2390 RVA: 0x00027F65 File Offset: 0x00026165
		// (set) Token: 0x06000957 RID: 2391 RVA: 0x00027F77 File Offset: 0x00026177
		public static CampaignOptions.Difficulty StealthAndDisguiseDifficulty
		{
			get
			{
				CampaignOptions current = CampaignOptions._current;
				if (current == null)
				{
					return CampaignOptions.Difficulty.Realistic;
				}
				return current._stealthAndDisguiseDifficulty;
			}
			set
			{
				if (CampaignOptions._current != null)
				{
					CampaignOptions._current._stealthAndDisguiseDifficulty = value;
				}
			}
		}

		// Token: 0x170001E9 RID: 489
		// (get) Token: 0x06000958 RID: 2392 RVA: 0x00027F8B File Offset: 0x0002618B
		// (set) Token: 0x06000959 RID: 2393 RVA: 0x00027F9D File Offset: 0x0002619D
		public static CampaignOptions.Difficulty CombatAIDifficulty
		{
			get
			{
				CampaignOptions current = CampaignOptions._current;
				if (current == null)
				{
					return CampaignOptions.Difficulty.Realistic;
				}
				return current._combatAIDifficulty;
			}
			set
			{
				if (CampaignOptions._current != null)
				{
					CampaignOptions._current._combatAIDifficulty = value;
				}
			}
		}

		// Token: 0x170001EA RID: 490
		// (get) Token: 0x0600095A RID: 2394 RVA: 0x00027FB1 File Offset: 0x000261B1
		// (set) Token: 0x0600095B RID: 2395 RVA: 0x00027FC3 File Offset: 0x000261C3
		public static CampaignOptions.Difficulty PersuasionSuccessChance
		{
			get
			{
				CampaignOptions current = CampaignOptions._current;
				if (current == null)
				{
					return CampaignOptions.Difficulty.Realistic;
				}
				return current._persuasionSuccessChance;
			}
			set
			{
				if (CampaignOptions._current != null)
				{
					CampaignOptions._current._persuasionSuccessChance = value;
				}
			}
		}

		// Token: 0x170001EB RID: 491
		// (get) Token: 0x0600095C RID: 2396 RVA: 0x00027FD7 File Offset: 0x000261D7
		// (set) Token: 0x0600095D RID: 2397 RVA: 0x00027FE9 File Offset: 0x000261E9
		public static CampaignOptions.Difficulty ClanMemberDeathChance
		{
			get
			{
				CampaignOptions current = CampaignOptions._current;
				if (current == null)
				{
					return CampaignOptions.Difficulty.Realistic;
				}
				return current._clanMemberDeathChance;
			}
			set
			{
				if (CampaignOptions._current != null)
				{
					CampaignOptions._current._clanMemberDeathChance = value;
				}
			}
		}

		// Token: 0x170001EC RID: 492
		// (get) Token: 0x0600095E RID: 2398 RVA: 0x00027FFD File Offset: 0x000261FD
		// (set) Token: 0x0600095F RID: 2399 RVA: 0x0002800F File Offset: 0x0002620F
		public static CampaignOptions.Difficulty BattleDeath
		{
			get
			{
				CampaignOptions current = CampaignOptions._current;
				if (current == null)
				{
					return CampaignOptions.Difficulty.Realistic;
				}
				return current._battleDeath;
			}
			set
			{
				if (CampaignOptions._current != null)
				{
					CampaignOptions._current._battleDeath = value;
				}
			}
		}

		// Token: 0x06000960 RID: 2400 RVA: 0x00028024 File Offset: 0x00026224
		public CampaignOptions()
		{
			this._playerTroopsReceivedDamage = CampaignOptions.Difficulty.VeryEasy;
			this._recruitmentDifficulty = CampaignOptions.Difficulty.VeryEasy;
			this._playerMapMovementSpeed = CampaignOptions.Difficulty.VeryEasy;
			this._combatAIDifficulty = CampaignOptions.Difficulty.VeryEasy;
			this._persuasionSuccessChance = CampaignOptions.Difficulty.VeryEasy;
			this._clanMemberDeathChance = CampaignOptions.Difficulty.VeryEasy;
			this._battleDeath = CampaignOptions.Difficulty.VeryEasy;
			this._stealthAndDisguiseDifficulty = CampaignOptions.Difficulty.VeryEasy;
			this._isLifeDeathCycleDisabled = false;
			this._autoAllocateClanMemberPerks = false;
			this._isIronmanMode = false;
			this.AccelerationMode = GameAccelerationMode.Default;
		}

		// Token: 0x06000961 RID: 2401 RVA: 0x0002808B File Offset: 0x0002628B
		internal static void AutoGeneratedStaticCollectObjectsCampaignOptions(object o, List<object> collectedObjects)
		{
			((CampaignOptions)o).AutoGeneratedInstanceCollectObjects(collectedObjects);
		}

		// Token: 0x06000962 RID: 2402 RVA: 0x00028099 File Offset: 0x00026299
		protected virtual void AutoGeneratedInstanceCollectObjects(List<object> collectedObjects)
		{
		}

		// Token: 0x06000963 RID: 2403 RVA: 0x0002809B File Offset: 0x0002629B
		internal static object AutoGeneratedGetMemberValueAccelerationMode(object o)
		{
			return ((CampaignOptions)o).AccelerationMode;
		}

		// Token: 0x06000964 RID: 2404 RVA: 0x000280AD File Offset: 0x000262AD
		internal static object AutoGeneratedGetMemberValue_autoAllocateClanMemberPerks(object o)
		{
			return ((CampaignOptions)o)._autoAllocateClanMemberPerks;
		}

		// Token: 0x06000965 RID: 2405 RVA: 0x000280BF File Offset: 0x000262BF
		internal static object AutoGeneratedGetMemberValue_playerTroopsReceivedDamage(object o)
		{
			return ((CampaignOptions)o)._playerTroopsReceivedDamage;
		}

		// Token: 0x06000966 RID: 2406 RVA: 0x000280D1 File Offset: 0x000262D1
		internal static object AutoGeneratedGetMemberValue_recruitmentDifficulty(object o)
		{
			return ((CampaignOptions)o)._recruitmentDifficulty;
		}

		// Token: 0x06000967 RID: 2407 RVA: 0x000280E3 File Offset: 0x000262E3
		internal static object AutoGeneratedGetMemberValue_playerMapMovementSpeed(object o)
		{
			return ((CampaignOptions)o)._playerMapMovementSpeed;
		}

		// Token: 0x06000968 RID: 2408 RVA: 0x000280F5 File Offset: 0x000262F5
		internal static object AutoGeneratedGetMemberValue_stealthAndDisguiseDifficulty(object o)
		{
			return ((CampaignOptions)o)._stealthAndDisguiseDifficulty;
		}

		// Token: 0x06000969 RID: 2409 RVA: 0x00028107 File Offset: 0x00026307
		internal static object AutoGeneratedGetMemberValue_combatAIDifficulty(object o)
		{
			return ((CampaignOptions)o)._combatAIDifficulty;
		}

		// Token: 0x0600096A RID: 2410 RVA: 0x00028119 File Offset: 0x00026319
		internal static object AutoGeneratedGetMemberValue_isLifeDeathCycleDisabled(object o)
		{
			return ((CampaignOptions)o)._isLifeDeathCycleDisabled;
		}

		// Token: 0x0600096B RID: 2411 RVA: 0x0002812B File Offset: 0x0002632B
		internal static object AutoGeneratedGetMemberValue_persuasionSuccessChance(object o)
		{
			return ((CampaignOptions)o)._persuasionSuccessChance;
		}

		// Token: 0x0600096C RID: 2412 RVA: 0x0002813D File Offset: 0x0002633D
		internal static object AutoGeneratedGetMemberValue_clanMemberDeathChance(object o)
		{
			return ((CampaignOptions)o)._clanMemberDeathChance;
		}

		// Token: 0x0600096D RID: 2413 RVA: 0x0002814F File Offset: 0x0002634F
		internal static object AutoGeneratedGetMemberValue_isIronmanMode(object o)
		{
			return ((CampaignOptions)o)._isIronmanMode;
		}

		// Token: 0x0600096E RID: 2414 RVA: 0x00028161 File Offset: 0x00026361
		internal static object AutoGeneratedGetMemberValue_battleDeath(object o)
		{
			return ((CampaignOptions)o)._battleDeath;
		}

		// Token: 0x040002DE RID: 734
		[SaveableField(4)]
		private bool _autoAllocateClanMemberPerks;

		// Token: 0x040002DF RID: 735
		[SaveableField(5)]
		private CampaignOptions.Difficulty _playerTroopsReceivedDamage;

		// Token: 0x040002E0 RID: 736
		[SaveableField(8)]
		private CampaignOptions.Difficulty _recruitmentDifficulty;

		// Token: 0x040002E1 RID: 737
		[SaveableField(9)]
		private CampaignOptions.Difficulty _playerMapMovementSpeed;

		// Token: 0x040002E2 RID: 738
		[SaveableField(18)]
		private CampaignOptions.Difficulty _stealthAndDisguiseDifficulty;

		// Token: 0x040002E3 RID: 739
		[SaveableField(11)]
		private CampaignOptions.Difficulty _combatAIDifficulty;

		// Token: 0x040002E4 RID: 740
		[SaveableField(12)]
		private bool _isLifeDeathCycleDisabled;

		// Token: 0x040002E5 RID: 741
		[SaveableField(13)]
		private CampaignOptions.Difficulty _persuasionSuccessChance;

		// Token: 0x040002E6 RID: 742
		[SaveableField(14)]
		private CampaignOptions.Difficulty _clanMemberDeathChance;

		// Token: 0x040002E7 RID: 743
		[SaveableField(15)]
		private bool _isIronmanMode;

		// Token: 0x040002E8 RID: 744
		[SaveableField(17)]
		private CampaignOptions.Difficulty _battleDeath;

		// Token: 0x040002E9 RID: 745
		[SaveableField(19)]
		public GameAccelerationMode AccelerationMode;

		// Token: 0x02000516 RID: 1302
		public enum Difficulty : short
		{
			// Token: 0x0400159E RID: 5534
			VeryEasy,
			// Token: 0x0400159F RID: 5535
			Easy,
			// Token: 0x040015A0 RID: 5536
			Realistic
		}
	}
}

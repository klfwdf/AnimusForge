using System;

namespace TaleWorlds.Core
{
	// Token: 0x02000004 RID: 4
	public static class ActionSetCode
	{
		// Token: 0x06000003 RID: 3 RVA: 0x00002058 File Offset: 0x00000258
		public static string GenerateActionSetNameWithSuffix(Monster monster, bool isFemale, string suffix)
		{
			if (monster == null)
			{
				return "as_human" + (isFemale ? "_female" : "") + suffix;
			}
			return "as_" + (string.IsNullOrEmpty(monster.BaseMonster) ? monster.StringId : monster.BaseMonster) + (isFemale ? "_female" : "") + suffix;
		}

		// Token: 0x04000001 RID: 1
		public const string WarriorActionSetSuffix = "_warrior";

		// Token: 0x04000002 RID: 2
		public const string HideoutBanditActionSetSuffix = "_hideout_bandit";

		// Token: 0x04000003 RID: 3
		public const string ChildActionSetSuffix = "_child";

		// Token: 0x04000004 RID: 4
		public const string Villager1ActionSetSuffix = "_villager";

		// Token: 0x04000005 RID: 5
		public const string Villager2ActionSetSuffix = "_villager_2";

		// Token: 0x04000006 RID: 6
		public const string Villager3ActionSetSuffix = "_villager_3";

		// Token: 0x04000007 RID: 7
		public const string VillagerInTavernActionSetSuffix = "_villager_in_tavern";

		// Token: 0x04000008 RID: 8
		public const string WarriorInTavernActionSetSuffix = "_warrior_in_tavern";

		// Token: 0x04000009 RID: 9
		public const string VillagerInAseraiTavernActionSetSuffix = "_villager_in_aserai_tavern";

		// Token: 0x0400000A RID: 10
		public const string WarriorInAseraiTavernActionSetSuffix = "_warrior_in_aserai_tavern";

		// Token: 0x0400000B RID: 11
		public const string VillainActionSetSuffix = "_villain";

		// Token: 0x0400000C RID: 12
		public const string LordActionSetSuffix = "_lord";

		// Token: 0x0400000D RID: 13
		public const string BarmaidActionSetSuffix = "_barmaid";

		// Token: 0x0400000E RID: 14
		public const string TavernKeeperSuffix = "_tavern_keeper";

		// Token: 0x0400000F RID: 15
		public const string WeaponsmithSuffix = "_weaponsmith";

		// Token: 0x04000010 RID: 16
		public const string SellerSuffix = "_seller";

		// Token: 0x04000011 RID: 17
		public const string MusicianSuffix = "_musician";

		// Token: 0x04000012 RID: 18
		public const string GuardSuffix = "_guard";

		// Token: 0x04000013 RID: 19
		public const string UnarmedGuardSuffix = "_unarmed_guard";

		// Token: 0x04000014 RID: 20
		public const string DancerSuffix = "_dancer";

		// Token: 0x04000015 RID: 21
		public const string BeggarSuffix = "_beggar";

		// Token: 0x04000016 RID: 22
		public const string VillagerWithBackPackSuffix = "_villager_with_backpack";

		// Token: 0x04000017 RID: 23
		public const string VillagerWithStaffSuffix = "_villager_with_staff";

		// Token: 0x04000018 RID: 24
		public const string VillagerCarryOnShoulderSuffix = "_villager_carry_on_shoulder";

		// Token: 0x04000019 RID: 25
		public const string VillagerCarryAxeSuffix = "_villager_carry_axe";

		// Token: 0x0400001A RID: 26
		public const string MerchantSuffix = "_villager_merchant";

		// Token: 0x0400001B RID: 27
		public const string ArtisanSuffix = "_villager_artisan";

		// Token: 0x0400001C RID: 28
		public const string PreacherSuffix = "_villager_preacher";

		// Token: 0x0400001D RID: 29
		public const string GangLeaderSuffix = "_villager_gangleader";

		// Token: 0x0400001E RID: 30
		public const string RuralNotableSuffix = "_villager_ruralnotable";

		// Token: 0x0400001F RID: 31
		public const string GangLeaderBodyGuardSuffix = "_gangleader_bodyguard";

		// Token: 0x04000020 RID: 32
		public const string MerchantNotarySuffix = "_merchant_notary";

		// Token: 0x04000021 RID: 33
		public const string VillagerCarryRightSideSuffix = "_villager_carry_right_side";

		// Token: 0x04000022 RID: 34
		public const string VillagerCarryFrontSuffix = "_villager_carry_front";

		// Token: 0x04000023 RID: 35
		public const string VillagerCarryFront2Suffix = "_villager_carry_front_v2";

		// Token: 0x04000024 RID: 36
		public const string VillagerCarryRightHandSuffix = "_villager_carry_right_hand";

		// Token: 0x04000025 RID: 37
		public const string VillagerCarryRightArmSuffix = "_villager_carry_right_arm";

		// Token: 0x04000026 RID: 38
		public const string VillagerCarryOverHeadSuffix = "_villager_carry_over_head";

		// Token: 0x04000027 RID: 39
		public const string VillagerCarryOverHead2Suffix = "_villager_carry_over_head_v2";

		// Token: 0x04000028 RID: 40
		public const string WorkerCarryOnShoulderSuffix = "_worker_carry_wood_on_shoulder";

		// Token: 0x04000029 RID: 41
		public const string VillagerCarryBucketLeftHand = "_villager_carry_bucket_on_lefthand";

		// Token: 0x0400002A RID: 42
		public const string VillagerCarryFishBucketsLeftHand = "_villager_carry_fish_buckets";

		// Token: 0x0400002B RID: 43
		public const string PosesSuffix = "_poses";

		// Token: 0x0400002C RID: 44
		public const string FaceGenActionSetSuffix = "_facegen";

		// Token: 0x0400002D RID: 45
		public const string MapActionSetSuffix = "_map";

		// Token: 0x0400002E RID: 46
		public const string MapWithBannerActionSetSuffix = "_map_with_banner";
	}
}

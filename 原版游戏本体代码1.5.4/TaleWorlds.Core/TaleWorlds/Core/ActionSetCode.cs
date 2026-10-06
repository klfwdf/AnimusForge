namespace TaleWorlds.Core;

public static class ActionSetCode
{
	public const string WarriorActionSetSuffix = "_warrior";

	public const string HideoutBanditActionSetSuffix = "_hideout_bandit";

	public const string ChildActionSetSuffix = "_child";

	public const string Villager1ActionSetSuffix = "_villager";

	public const string Villager2ActionSetSuffix = "_villager_2";

	public const string Villager3ActionSetSuffix = "_villager_3";

	public const string VillagerInTavernActionSetSuffix = "_villager_in_tavern";

	public const string WarriorInTavernActionSetSuffix = "_warrior_in_tavern";

	public const string VillagerInAseraiTavernActionSetSuffix = "_villager_in_aserai_tavern";

	public const string WarriorInAseraiTavernActionSetSuffix = "_warrior_in_aserai_tavern";

	public const string VillainActionSetSuffix = "_villain";

	public const string LordActionSetSuffix = "_lord";

	public const string BarmaidActionSetSuffix = "_barmaid";

	public const string TavernKeeperSuffix = "_tavern_keeper";

	public const string WeaponsmithSuffix = "_weaponsmith";

	public const string SellerSuffix = "_seller";

	public const string MusicianSuffix = "_musician";

	public const string GuardSuffix = "_guard";

	public const string UnarmedGuardSuffix = "_unarmed_guard";

	public const string DancerSuffix = "_dancer";

	public const string BeggarSuffix = "_beggar";

	public const string VillagerWithBackPackSuffix = "_villager_with_backpack";

	public const string VillagerWithStaffSuffix = "_villager_with_staff";

	public const string VillagerCarryOnShoulderSuffix = "_villager_carry_on_shoulder";

	public const string VillagerCarryAxeSuffix = "_villager_carry_axe";

	public const string MerchantSuffix = "_villager_merchant";

	public const string ArtisanSuffix = "_villager_artisan";

	public const string PreacherSuffix = "_villager_preacher";

	public const string GangLeaderSuffix = "_villager_gangleader";

	public const string RuralNotableSuffix = "_villager_ruralnotable";

	public const string GangLeaderBodyGuardSuffix = "_gangleader_bodyguard";

	public const string MerchantNotarySuffix = "_merchant_notary";

	public const string VillagerCarryRightSideSuffix = "_villager_carry_right_side";

	public const string VillagerCarryFrontSuffix = "_villager_carry_front";

	public const string VillagerCarryFront2Suffix = "_villager_carry_front_v2";

	public const string VillagerCarryRightHandSuffix = "_villager_carry_right_hand";

	public const string VillagerCarryRightArmSuffix = "_villager_carry_right_arm";

	public const string VillagerCarryOverHeadSuffix = "_villager_carry_over_head";

	public const string VillagerCarryOverHead2Suffix = "_villager_carry_over_head_v2";

	public const string WorkerCarryOnShoulderSuffix = "_worker_carry_wood_on_shoulder";

	public const string VillagerCarryBucketLeftHand = "_villager_carry_bucket_on_lefthand";

	public const string VillagerCarryFishBucketsLeftHand = "_villager_carry_fish_buckets";

	public const string PosesSuffix = "_poses";

	public const string FaceGenActionSetSuffix = "_facegen";

	public const string MapActionSetSuffix = "_map";

	public const string MapWithBannerActionSetSuffix = "_map_with_banner";

	public static string GenerateActionSetNameWithSuffix(Monster monster, bool isFemale, string suffix)
	{
		if (monster == null)
		{
			return "as_human" + (isFemale ? "_female" : "") + suffix;
		}
		return "as_" + (string.IsNullOrEmpty(monster.BaseMonster) ? monster.StringId : monster.BaseMonster) + (isFemale ? "_female" : "") + suffix;
	}
}

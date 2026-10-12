using System.Text;















namespace AnimusForge.SiegeAftermathIntervention;















/// <summary>







/// Dependency-free contract for the castle captive-lord duel. Bannerlord mission







/// movement, teams, weapons and damage handling remain in the fused runtime adapter.







/// </summary>







public static class SiegeCastleLordDuelProfile







{







    public const int MinimumWeaponTier = 4;















    public const int MaximumWeaponTier = 6;















    public const float WeaponForwardDistance = 20f;















    public const float LoadoutItemSpacing = 0.75f;















    public const float AudienceBaseRadius = 12.5f;















    public const float AudienceRingSpacing = 1.5f;















    public const float AudienceSpacing = 1.25f;















    public const float AudienceCombatIsolationRefreshSeconds = 0.2f;















    public const float DuelHealthFloor = 1f;















    public const float PreparationSeconds = 1.5f;















    public const string RuntimeSource = "castle_captive_lord_duel";















    public static bool IsEligibleWeapon(







        int tier,







        bool isOneHanded,







        bool isTwoHanded,







        bool isMeleeWeapon,







        bool isMerchandise)







    {







        return tier >= MinimumWeaponTier







            && tier <= MaximumWeaponTier







            && (isOneHanded || isTwoHanded)







            && isMeleeWeapon







            && isMerchandise;







    }















    public static bool IsEligibleShield(int tier, bool isShield, bool isMerchandise)







    {







        return tier >= MinimumWeaponTier







            && tier <= MaximumWeaponTier







            && isShield







            && isMerchandise;







    }















    public static string BuildEquipmentFact(







        bool playerIsMounted,







        bool playerCarriesRangedWeapon,







        bool playerWieldsRangedWeapon)







    {







        return BuildEquipmentFact(playerIsMounted, playerCarriesRangedWeapon, playerWieldsRangedWeapon, isEnglish: false);







    }















    public static string BuildEquipmentFact(







        bool playerIsMounted,







        bool playerCarriesRangedWeapon,







        bool playerWieldsRangedWeapon,







        bool isEnglish)







    {







        if (isEnglish)







        {







            return "Player currently mounted=" + YesNo(playerIsMounted, true)







                + "; carries ranged weapon=" + YesNo(playerCarriesRangedWeapon, true)







                + "; currently wields ranged weapon=" + YesNo(playerWieldsRangedWeapon, true) + ".";







        }







        return CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_duel_profile_4acee8bba6d2") + YesNo(playerIsMounted, false)







            + CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_duel_profile_d7d434a0777e") + YesNo(playerCarriesRangedWeapon, false)







            + CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_duel_profile_3d42088f665d") + YesNo(playerWieldsRangedWeapon, false) + "。";







    }















    public static string BuildResultFact(

        string castleName,

        string playerName,

        string lordName,

        bool playerWon,

        bool playerWasMountedWhenAccepted,

        bool playerCarriedRangedWeaponWhenAccepted,

        bool playerWieldedRangedWeaponWhenAccepted,

        bool playerMountedDuringDuel,

        bool playerUsedRangedWeapon)

    {

        return BuildResultFact(castleName, playerName, lordName, playerWon, playerWasMountedWhenAccepted, playerCarriedRangedWeaponWhenAccepted, playerWieldedRangedWeaponWhenAccepted, playerMountedDuringDuel, playerUsedRangedWeapon, isEnglish: false);

    }



    public static string BuildResultFact(

        string castleName,

        string playerName,

        string lordName,

        bool playerWon,

        bool playerWasMountedWhenAccepted,

        bool playerCarriedRangedWeaponWhenAccepted,

        bool playerWieldedRangedWeaponWhenAccepted,

        bool playerMountedDuringDuel,

        bool playerUsedRangedWeapon,

        bool isEnglish)

    {

        var sb = new StringBuilder();

        if (isEnglish)

        {

            sb.Append("[CASTLE CAPTIVE LORD DUEL RESULT - COMMITTED FACT] Location=")

                .Append(string.IsNullOrWhiteSpace(castleName) ? SiegeCastleRuntimePromptProfile.DefaultCastleNameEn : castleName.Trim())

                .Append("; Player=")

                .Append(string.IsNullOrWhiteSpace(playerName) ? SiegeCastleRuntimePromptProfile.DefaultPlayerNameEn : playerName.Trim())

                .Append("; Captive Lord=")

                .Append(string.IsNullOrWhiteSpace(lordName) ? "the captive lord" : lordName.Trim())

                .Append("; Winner=")

                .Append(playerWon ? "Player" : "Captive Lord")

                .Append(". Neither side died; the duel has ended; the captive lord remains a prisoner; this fact does not automatically release, recruit, sell, or execute anyone. ")

                .Append("Player mounted when accepted=").Append(YesNo(playerWasMountedWhenAccepted, true))

                .Append("; Player carried ranged weapon when accepted=").Append(YesNo(playerCarriedRangedWeaponWhenAccepted, true))

                .Append("; Player wielded ranged weapon when accepted=").Append(YesNo(playerWieldedRangedWeaponWhenAccepted, true))

                .Append("; Player mounted during duel=").Append(YesNo(playerMountedDuringDuel, true))

                .Append("; Player used ranged weapon during duel=").Append(YesNo(playerUsedRangedWeapon, true)).Append(". ")

                .Append("Carrying or wielding ranged weapon upon acceptance only records equipment then, not actual use during the duel; violation of melee agreement is judged solely by \u0027Player used ranged weapon during duel\u0027. ");



            if (playerWon)

            {

                sb.Append("Have the lord respond to defeat based on personality, prior negotiations, agreements, and whether rules were broken; do not grant release or any other final disposition.");

            }

            else

            {

                sb.Append("The lord has won and lowered the duel weapon. Have the lord respond based on personality, prior negotiations, and agreements; the lord may demand the player keep promises, but cannot leave the scene or drop captive status.");

            }

            return sb.ToString();

        }



        sb.Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_duel_profile_ad7f6e136a4a"))

            .Append(string.IsNullOrWhiteSpace(castleName) ? SiegeCastleRuntimePromptProfile.DefaultCastleName : castleName.Trim())

            .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_duel_profile_dc9557d3f01a"))

            .Append(string.IsNullOrWhiteSpace(playerName) ? SiegeCastleRuntimePromptProfile.DefaultPlayerName : playerName.Trim())

            .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_duel_profile_1a5a86d96cad"))

            .Append(string.IsNullOrWhiteSpace(lordName) ? CastleAftermathCompatibilityTexts.Raw("captured_lord") : lordName.Trim())

            .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_duel_profile_fab0f8dc8149"))

            .Append(playerWon ? CastleAftermathCompatibilityTexts.Raw("player_fallback") : CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_duel_profile_6d02502543d3"))

            .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_duel_profile_400dfb69dbad"))

            .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_duel_profile_fbe8ab77870d")).Append(YesNo(playerWasMountedWhenAccepted, false))

            .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_duel_profile_b469d9579c10")).Append(YesNo(playerCarriedRangedWeaponWhenAccepted, false))

            .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_duel_profile_8453cf970e88")).Append(YesNo(playerWieldedRangedWeaponWhenAccepted, false))

            .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_duel_profile_6bb81e9e9cf4")).Append(YesNo(playerMountedDuringDuel, false))

            .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_duel_profile_6f9843ee23f7")).Append(YesNo(playerUsedRangedWeapon, false)).Append("。")

            .Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_duel_profile_a38a19c1f4cc"));



        if (playerWon)

        {

            sb.Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_duel_profile_eee86aef7662"));

        }

        else

        {

            sb.Append(CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_lord_duel_profile_efbeaf55f2ac"));

        }

        return sb.ToString();

    }

    private static string YesNo(bool value) => YesNo(value, false);
    private static string YesNo(bool value, bool isEnglish) => isEnglish ? (value ? "Yes" : "No") : (value ? CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_b5141d3d19e9") : CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_0c70665b6eb6"));
}

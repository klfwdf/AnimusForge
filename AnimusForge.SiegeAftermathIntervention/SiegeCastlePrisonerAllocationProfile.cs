using System;

using System.Text.RegularExpressions;



namespace AnimusForge.SiegeAftermathIntervention;



/// <summary>

/// Dependency-free count policy for staged regular-prisoner allocations.

/// Runtime code owns concrete Bannerlord troop rosters and supplies the random roll.

/// </summary>

public static class SiegeCastlePrisonerAllocationProfile

{

    private static readonly char ChineseZero = CastleAftermathCompatibilityTexts.Raw("compat_prisoner_count__zero_9489a4a6f3fb")[0];
    private static readonly char ChineseOne = CastleAftermathCompatibilityTexts.Raw("compat_prisoner_count__one_51a75f4634df")[0];
    private static readonly char ChineseTwo = CastleAftermathCompatibilityTexts.Raw("compat_prisoner_count__two_084b42f6e95e")[0];
    private static readonly char ChineseTwoAlternative = CastleAftermathCompatibilityTexts.Raw("compat_prisoner_count__two_alternative_83385d2edafe")[0];
    private static readonly char ChineseThree = CastleAftermathCompatibilityTexts.Raw("compat_prisoner_count__three_a4c3313deb18")[0];
    private static readonly char ChineseFour = CastleAftermathCompatibilityTexts.Raw("compat_prisoner_count__four_754a9d5828d5")[0];
    private static readonly char ChineseFive = CastleAftermathCompatibilityTexts.Raw("compat_prisoner_count__five_c9b87f516a38")[0];
    private static readonly char ChineseSix = CastleAftermathCompatibilityTexts.Raw("compat_prisoner_count__six_de07b5383874")[0];
    private static readonly char ChineseSeven = CastleAftermathCompatibilityTexts.Raw("compat_prisoner_count__seven_1b0542878c67")[0];
    private static readonly char ChineseEight = CastleAftermathCompatibilityTexts.Raw("compat_prisoner_count__eight_2c467d367392")[0];
    private static readonly char ChineseNine = CastleAftermathCompatibilityTexts.Raw("compat_prisoner_count__nine_df897da6e02e")[0];
    private static readonly char ChineseTen = CastleAftermathCompatibilityTexts.Raw("compat_prisoner_count__ten_fc7edd399a5c")[0];
    private static readonly char ChineseHundred = CastleAftermathCompatibilityTexts.Raw("compat_prisoner_count__hundred_089d8c4c0070")[0];

    private static readonly Regex ArabicQualifiedCountRegex = new Regex(

        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_d2f583648634"),

        RegexOptions.Compiled);



    private static readonly Regex ArabicBareCountRegex = new Regex(

        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_4d68bf4ffdc5"),

        RegexOptions.IgnoreCase | RegexOptions.Compiled);



    private static readonly Regex ChineseCountRegex = new Regex(

        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_441b239f4be7"),

        RegexOptions.Compiled);



    private static readonly string[] AllKeywords =

    {

        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_5c55a67935af"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_905dd3eeb7c6"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_ae99f297e9f5"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_55d6350750d3"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_d6822b04178d"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_851b46b8db5b"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_dd8102e74227"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_d7fef126d696"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_3c1b1002d16e"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_24528affe0e6"),

        "all of them", "the rest", "remainder", "the remaining"

    };



    private static readonly string[] ResetKeywords =

    {

        CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_ba9e1a101079"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_511e56ad7183"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_25ca6b353574"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_c56f5dfe2518"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_adb46686faec"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_ff2944c7dbc3"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_ce790496086b"),

        "start over", "reconsider", "change my mind", "reset the plan", "scratch that"

    };



    public static SiegeCastlePrisonerQuantityDecision Resolve(

        string playerText,

        int availableCount,

        double randomRoll)

    {

        int available = Math.Max(0, availableCount);

        if (available == 0)

        {

            return new SiegeCastlePrisonerQuantityDecision(0, false, false, IsPlanResetRequested(playerText), "none_available");

        }



        bool reset = IsPlanResetRequested(playerText);

        if (ContainsAny(playerText, AllKeywords))

        {

            return new SiegeCastlePrisonerQuantityDecision(available, true, true, reset, "all_available");

        }



        if (TryParseExplicitCount(playerText, out int explicitCount))

        {

            return new SiegeCastlePrisonerQuantityDecision(

                Math.Min(available, Math.Max(1, explicitCount)),

                true,

                false,

                reset,

                explicitCount > available ? "explicit_clamped" : "explicit_count");

        }



        return new SiegeCastlePrisonerQuantityDecision(available, false, true, reset, "default_all_unspecified_count");

    }



    public static bool IsPlanResetRequested(string playerText)
    {
        if (string.IsNullOrWhiteSpace(playerText)) return false;
        string lower = playerText.ToLowerInvariant();
        if (lower.Contains("start over") || lower.Contains("reset") || lower.Contains("redo") || lower.Contains("overturn"))
        {
            return true;
        }
        return ContainsAny(playerText, ResetKeywords);
    }



    public static bool RequestsAllAvailable(string playerText)
    {
        if (string.IsNullOrWhiteSpace(playerText)) return false;
        string lower = playerText.ToLowerInvariant();
        if (Regex.IsMatch(lower, @"\b(all|everyone|all of them|the rest|remainder|the remaining)\b"))
        {
            return true;
        }
        return ContainsAny(playerText, AllKeywords);
    }



    public static bool RequestsEliteTroops(string playerText)
    {
        if (string.IsNullOrWhiteSpace(playerText)) return false;
        string lower = playerText.ToLowerInvariant();
        if (lower.Contains("elite") || lower.Contains("veteran") || lower.Contains("high-tier") || lower.Contains("strongest"))
        {
            return true;
        }
        return ContainsAny(playerText, new[] { CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_0425e9c31a48"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_a5399ff0bbbb"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_d6e61888b078"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_53eb36f55901"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_64dfcc065cea"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_97f18652da00") });
    }



    public static bool RequestsLowTierTroops(string playerText)
    {
        if (string.IsNullOrWhiteSpace(playerText)) return false;
        string lower = playerText.ToLowerInvariant();
        if (lower.Contains("low-tier") || lower.Contains("low tier") || lower.Contains("recruit") || lower.Contains("weak"))
        {
            return true;
        }
        return ContainsAny(playerText, new[] { CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_463dd3e3d8a1"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_b65f8cbdeece"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_f48afb92dd46"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_788d5efc1161"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_9a30fea1d2a7"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_player_authorization_policy_7ed27f5dbaa3"), CastleAftermathCompatibilityTexts.Raw("compat_siege_castle_prisoner_allocation_profile_c97142705b80") });
    }



    public static bool TryParseExplicitCount(string playerText, out int count)

    {

        string text = playerText ?? string.Empty;

        Match arabic = ArabicQualifiedCountRegex.Match(text);

        if (!arabic.Success)

        {

            arabic = ArabicBareCountRegex.Match(text);

        }

        if (arabic.Success && int.TryParse(arabic.Groups["count"].Value, out count) && count > 0)

        {

            return true;

        }



        Match chinese = ChineseCountRegex.Match(text);

        if (chinese.Success)

        {

            count = ParseChineseNumber(chinese.Groups["count"].Value);

            return count > 0;

        }



        count = 0;

        return false;

    }



    private static int ParseChineseNumber(string value)

    {

        if (string.IsNullOrWhiteSpace(value))

        {

            return 0;

        }

        int total = 0;

        int current = 0;

        foreach (char ch in value)

        {

            int digit = ChineseDigit(ch);

            if (digit >= 0)

            {

                current = digit;

                continue;

            }

            int unit = ch == ChineseTen ? 10 : (ch == ChineseHundred ? 100 : 0);

            if (unit > 0)

            {

                total += Math.Max(1, current) * unit;

                current = 0;

            }

        }

        return total + current;

    }



    private static int ChineseDigit(char value)

    {

        if (value == ChineseZero || value == '〇') return 0;
        if (value == ChineseOne) return 1;
        if (value == ChineseTwo || value == ChineseTwoAlternative) return 2;
        if (value == ChineseThree) return 3;
        if (value == ChineseFour) return 4;
        if (value == ChineseFive) return 5;
        if (value == ChineseSix) return 6;
        if (value == ChineseSeven) return 7;
        if (value == ChineseEight) return 8;
        if (value == ChineseNine) return 9;
        return -1;

    }



    private static bool ContainsAny(string text, string[] keywords)

    {

        string source = text ?? string.Empty;

        foreach (string keyword in keywords)

        {

            if (source.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)

            {

                return true;

            }

        }

        return false;

    }

}



public sealed class SiegeCastlePrisonerQuantityDecision

{

    public SiegeCastlePrisonerQuantityDecision(

        int requestedCount,

        bool countWasExplicit,

        bool usedAllAvailable,

        bool resetPreviousPlan,

        string reasonCode)

    {

        RequestedCount = Math.Max(0, requestedCount);

        CountWasExplicit = countWasExplicit;

        UsedAllAvailable = usedAllAvailable;

        ResetPreviousPlan = resetPreviousPlan;

        ReasonCode = reasonCode ?? string.Empty;

    }



    public int RequestedCount { get; }



    public bool CountWasExplicit { get; }



    public bool UsedAllAvailable { get; }



    public bool ResetPreviousPlan { get; }



    public string ReasonCode { get; }

}


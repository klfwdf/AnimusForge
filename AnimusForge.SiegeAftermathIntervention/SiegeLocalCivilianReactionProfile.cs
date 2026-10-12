using System.Collections.Generic;
using System;



namespace AnimusForge.SiegeAftermathIntervention;



/// <summary>

/// Dependency-free policy for local civilian witness reactions when the player hurts a civilian

/// during the active GCCZ scene. AF adapters own live agent lookup, movement, and speech dispatch; combat escalation stays outside this local panic policy.

/// </summary>

public static class SiegeLocalCivilianReactionProfile

{

    public const float WitnessRadius = 24f;



    public const int MaxWitnessesPerIncident = 18;



    public const int MaxDefiantWitnessesPerIncident = 3;



    public const float LocalFleeMinDistance = 28f;



    public const float LocalFleeFallbackDistance = 36f;



    public const float LocalFleeArrivalRadius = 5f;



    public const float LocalFleeRefreshSeconds = 3.5f;



    public const float WitnessRepeatCooldownSeconds = 18f;



    public const uint SoldierWitnessFallbackMessageColor = 0xFFFFD27Fu;



    public const string WitnessFleeSource = "local_player_attack_witness_flee";



    public const string WitnessDefiantSource = "local_player_attack_witness_defiant";



    public const string NativeFleeBridgeSource = "local_player_attack_native_flee";



    public const string PlayerDownSource = "local_player_attack_down";



    public const string SoldierWitnessInquirySource = "local_player_attack_soldier_witness_inquiry";



    public static string WitnessMemoryTitle => TownPromptCatalogResources.Get(false).GetRuntimeText("LocalWitnessMemoryTitle");

    public const string WitnessMemoryTitleEn = "Local Panic";



    public static string SoldierWitnessMemoryTitle => TownPromptCatalogResources.Get(false).GetRuntimeText("LocalSoldierInquiryMemoryTitle");

    public const string SoldierWitnessMemoryTitleEn = "Soldier Inquiry";



    public static string GetWitnessMemoryTitle(bool isEnglish) => isEnglish ? WitnessMemoryTitleEn : WitnessMemoryTitle;

    public static string GetSoldierWitnessMemoryTitle(bool isEnglish) => isEnglish ? SoldierWitnessMemoryTitleEn : SoldierWitnessMemoryTitle;



    public static bool IsInsideWitnessRadiusSquared(float distanceSquared)

    {

        return distanceSquared <= WitnessRadius * WitnessRadius;

    }



    public static int CalculateMaxDefiantWitnesses(int witnessCount, int defiantEligibleCount)

    {

        if (witnessCount <= 0 || defiantEligibleCount <= 0)

        {

            return 0;

        }

        int proportionalLimit = (int)Math.Round(witnessCount * 0.18d, MidpointRounding.AwayFromZero);

        int safeLimit = Math.Max(1, proportionalLimit);

        return Math.Min(MaxDefiantWitnessesPerIncident, Math.Min(defiantEligibleCount, safeLimit));

    }



    public static bool ShouldCivilianDefy(int deterministicSeed, bool isNotable, bool carriesWeapon, bool isGuardOrSoldier)

    {

        if (isGuardOrSoldier)

        {

            return false;

        }

        int normalizedSeed = deterministicSeed == int.MinValue ? int.MaxValue : Math.Abs(deterministicSeed);

        int bucket = normalizedSeed % 12;

        if (isNotable)

        {

            return bucket < 4;

        }

        if (carriesWeapon)

        {

            return bucket < 3;

        }

        return bucket == 0;

    }



    public static bool IsLocalFleeTargetFarEnough(float distanceSquared)

    {

        return distanceSquared >= LocalFleeMinDistance * LocalFleeMinDistance;

    }



    public static bool IsLocalFleeTargetReached(float distanceSquared)

    {

        return distanceSquared <= LocalFleeArrivalRadius * LocalFleeArrivalRadius;

    }



    public static bool ShouldRefreshLocalFleeOrder(bool force, bool hasTarget, bool hasLastOrder, bool reachedTarget, float elapsedSeconds)

    {

        return force

            || !hasTarget

            || !hasLastOrder

            || (!reachedTarget && elapsedSeconds >= LocalFleeRefreshSeconds);

    }



    public static string BuildPlayerDownMessage(string targetName)

        => BuildPlayerDownMessage(targetName, false);



    public static string BuildPlayerDownMessage(string targetName, bool isEnglish)

    {

        if (isEnglish)

        {

            return "[Local conflict] Knocked down " + NormalizeTargetName(targetName, "an NPC") + "; nearby fleeing.";

        }

        return SiegeAftermathCompatibilityTexts.Raw("local_civ_knockdown_prefix") + NormalizeTargetName(targetName, SiegeAftermathCompatibilityTexts.Raw("attack_npc_fallback")) + SiegeAftermathCompatibilityTexts.Raw("local_civ_scatter_suffix");

    }



    public static string BuildPlayerDownMemoryText(string targetName)

        => BuildPlayerDownMemoryText(targetName, false);



    public static string BuildPlayerDownMemoryText(string targetName, bool isEnglish)

    {

        if (isEnglish)

        {

            return "The player knocked down " + NormalizeTargetName(targetName, "an NPC") + " in the post-siege scene; this only triggers local street panic and defiance, and must not automatically escalate to full-city massacre.";

        }

        return SiegeAftermathCompatibilityTexts.Raw("local_civ_memory_prefix") + NormalizeTargetName(targetName, SiegeAftermathCompatibilityTexts.Raw("attack_npc_fallback")) + SiegeAftermathCompatibilityTexts.Raw("local_civ_memory_scuffle_suffix");

    }



    public static string BuildWitnessMemoryText(string targetName, int fleeingCount, int defiantCount)

        => BuildWitnessMemoryText(targetName, fleeingCount, defiantCount, false);



    public static string BuildWitnessMemoryText(string targetName, int fleeingCount, int defiantCount, bool isEnglish)

    {

        int fleeing = Math.Max(0, fleeingCount);

        int defiant = Math.Max(0, defiantCount);

        if (isEnglish)

        {

            return "After the player attacked " + NormalizeTargetName(targetName, "an NPC") + ", approx " + fleeing + " civilians began fleeing and approx " + defiant + " attempted local restraint/defiance; this represents street-level conflict and does not link to massacre hostility.";

        }

        return SiegeAftermathCompatibilityTexts.Raw("local_civ_flee_prefix") + NormalizeTargetName(targetName, SiegeAftermathCompatibilityTexts.Raw("attack_npc_fallback")) + SiegeAftermathCompatibilityTexts.Raw("local_civ_flee_middle") + fleeing + SiegeAftermathCompatibilityTexts.Raw("local_civ_flee_count_prefix") + defiant + SiegeAftermathCompatibilityTexts.Raw("local_civ_flee_defy_suffix");

    }



    public static string BuildWitnessFact(string targetName, bool victimDown, bool witnessWillDefy, string settlementName)

        => BuildWitnessFact(targetName, victimDown, witnessWillDefy, settlementName, false);



    public static string BuildWitnessFact(string targetName, bool victimDown, bool witnessWillDefy, string settlementName, bool isEnglish)

    {

        string scene = string.IsNullOrWhiteSpace(settlementName)

            ? (isEnglish ? SiegeAmbientReactionProfile.DefaultSettlementNameEn : SiegeAmbientReactionProfile.DefaultSettlementName)

            : settlementName.Trim();

        string target = NormalizeTargetName(targetName, isEnglish ? "a nearby civilian" : SiegeAftermathCompatibilityTexts.Raw("local_civ_nearby_civilian"));

        if (isEnglish)

        {

            string incidentEn = victimDown ? "the player just knocked down " : "the player just attacked ";

            string roleEn = witnessWillDefy

                ? "You are one of the few bold, armed, or prominent individuals nearby, currently standing defiant or calling for restraint. You may rebuke, curse, question, or verbally threaten the player, or show fear/plead; do not claim to have actively attacked the player or soldiers. "

                : "You are a nearby witnessing civilian, currently taking shelter. You may shout for others to run, curse, grieve, or plead for mercy; do not invent unseen casualties. ";

            return "[SIEGE AFTERMATH AMBIENT LINE] Current location is " + scene + ". " + incidentEn + target + "; this is only a local street conflict, not a general massacre order. "

                + roleEn

                + "The player holds on-site military disposition authority; whether this is a conquered enemy town or an internal incident in an owned/attached town follows runtime provenance. Your curses, threats, pleas, or statements of kneeling are spoken lines only; do not execute attacks, kneeling, submission, or loyalty settlement. Speak only one on-site line of 12 to 32 characters without narration, action descriptions, or bracketed tags.";

        }



        string incident = victimDown ? SiegeAftermathCompatibilityTexts.Raw("local_civ_player_knocked") : SiegeAftermathCompatibilityTexts.Raw("local_civ_player_attacked");

        string role = witnessWillDefy

            ? SiegeAftermathCompatibilityTexts.Raw("local_civ_defy_prompt")

            : SiegeAftermathCompatibilityTexts.Raw("local_civ_flee_prompt");

        return SiegeAftermathCompatibilityTexts.Raw("local_civ_ambient_prefix") + scene + SiegeAftermathCompatibilityTexts.Raw("local_civ_ambient_body_prefix") + incident + target + SiegeAftermathCompatibilityTexts.Raw("local_civ_ambient_body_suffix")

            + role

            + SiegeAftermathCompatibilityTexts.Raw("local_civ_ambient_power")

            + SiegeAftermathCompatibilityTexts.Raw("local_civ_ambient_limit");

    }



    public static string BuildSoldierWitnessInquiryFact(string targetName, bool victimDown, string settlementName, bool soldierIsBloodthirsty = false, string soldierPersonaText = null)

        => BuildSoldierWitnessInquiryFact(targetName, victimDown, settlementName, soldierIsBloodthirsty, soldierPersonaText, false);



    public static string BuildSoldierWitnessInquiryFact(string targetName, bool victimDown, string settlementName, bool soldierIsBloodthirsty, string soldierPersonaText, bool isEnglish)

    {

        string scene = string.IsNullOrWhiteSpace(settlementName)

            ? (isEnglish ? SiegeAmbientReactionProfile.DefaultSettlementNameEn : SiegeAmbientReactionProfile.DefaultSettlementName)

            : settlementName.Trim();

        string target = NormalizeTargetName(targetName, isEnglish ? "a nearby civilian" : SiegeAftermathCompatibilityTexts.Raw("local_civ_nearby_civilian"));

        if (isEnglish)

        {

            string incidentEn = victimDown ? "the player just knocked down " : "the player just attacked ";

            string personaLineEn = string.IsNullOrWhiteSpace(soldierPersonaText) ? "" : " Your known traits: " + soldierPersonaText.Trim() + ". ";

            string responsePolicyEn = soldierIsBloodthirsty

                ? "You have bloodthirsty tendencies; you may proactively inquire whether to order a general massacre, but must frame it as an inquiry to the player. "

                : (victimDown

                    ? "A person was knocked down; ask whether to secure this street corner first; remind that any general pillage or massacre requires an explicit command. "

                    : "This was a local strike; ask whether to restrain the crowd or secure the street, without actively proposing full plunder or slaughter. ");

            return "[SIEGE AFTERMATH SOLDIER INQUIRY] Current location is " + scene + ". " + incidentEn + target + "; you witnessed this local violence within 24m. "

                + "You are an allied soldier; address the player as commander or lord. " + personaLineEn + responsePolicyEn

                + "Speak one natural short inquiry; do not declare executed actions or output bracketed tags.";

        }



        string incident = victimDown ? SiegeAftermathCompatibilityTexts.Raw("local_civ_player_knocked") : SiegeAftermathCompatibilityTexts.Raw("local_civ_player_attacked");

        string personaLine = BuildSoldierPersonaLine(soldierPersonaText);

        string responsePolicy = BuildSoldierWitnessResponsePolicy(victimDown, soldierIsBloodthirsty);

        return SiegeAftermathCompatibilityTexts.Raw("local_civ_soldier_prompt_prefix") + scene + "." + incident + target + SiegeAftermathCompatibilityTexts.Raw("local_civ_soldier_prompt_signal")

            + SiegeAftermathCompatibilityTexts.Raw("local_civ_soldier_prompt_role") + personaLine + responsePolicy

            + SiegeAftermathCompatibilityTexts.Raw("local_civ_soldier_prompt_rule");

    }



    public static string BuildSoldierWitnessMemoryText(string targetName, bool victimDown, string soldierName, bool soldierIsBloodthirsty = false)

        => BuildSoldierWitnessMemoryText(targetName, victimDown, soldierName, soldierIsBloodthirsty, false);



    public static string BuildSoldierWitnessMemoryText(string targetName, bool victimDown, string soldierName, bool soldierIsBloodthirsty, bool isEnglish)

    {

        string actor = NormalizeTargetName(soldierName, isEnglish ? "nearby allied soldier" : SiegeAftermathCompatibilityTexts.Raw("local_civ_allied_soldier_noun"));

        string incident = victimDown ? (isEnglish ? "knocking down" : SiegeAftermathCompatibilityTexts.Raw("local_civ_incident_knocked")) : (isEnglish ? "attacking" : SiegeAftermathCompatibilityTexts.Raw("local_civ_incident_attacked"));

        if (isEnglish)

        {

            string inquiryEn = soldierIsBloodthirsty

                ? "inquired with bloodthirsty tone whether to order massacre"

                : (victimDown ? "inquired whether to secure the area or wait for player orders" : "inquired whether to restrain the crowd and secure the street");

            return actor + " witnessed the player " + incident + " " + NormalizeTargetName(targetName, "an NPC") + " within 24m and " + inquiryEn + "; the inquiry itself does not execute destructive disposition.";

        }



        string inquiry = soldierIsBloodthirsty

            ? SiegeAftermathCompatibilityTexts.Raw("local_civ_soldier_inquiry_massacre")

            : (victimDown ? SiegeAftermathCompatibilityTexts.Raw("local_civ_soldier_inquiry_control_plunder") : SiegeAftermathCompatibilityTexts.Raw("local_civ_soldier_inquiry_control_crowd"));

        return actor + SiegeAftermathCompatibilityTexts.Raw("local_civ_witness_24m") + incident + " " + NormalizeTargetName(targetName, SiegeAftermathCompatibilityTexts.Raw("attack_npc_fallback")) + SiegeAftermathCompatibilityTexts.Raw("local_civ_inquiry_sep") + inquiry + SiegeAftermathCompatibilityTexts.Raw("local_civ_inquiry_suffix");

    }



    public static string BuildSoldierWitnessFallbackMessage(string soldierName, string targetName, bool victimDown, bool soldierIsBloodthirsty = false)

        => BuildSoldierWitnessFallbackMessage(soldierName, targetName, victimDown, soldierIsBloodthirsty, false);



    public static string BuildSoldierWitnessFallbackMessage(string soldierName, string targetName, bool victimDown, bool soldierIsBloodthirsty, bool isEnglish)

    {

        string actor = NormalizeTargetName(soldierName, isEnglish ? "Allied soldier" : SiegeAftermathCompatibilityTexts.Raw("local_civ_allied_soldier_noun"));

        if (isEnglish)

        {

            if (soldierIsBloodthirsty)

            {

                return "[Soldier inquiry] " + actor + ": Commander, shall we order a general massacre while chaos reigns? Say the word and we move.";

            }

            if (victimDown)

            {

                return "[Soldier inquiry] " + actor + ": Commander, someone is down. Shall I hold this area? If you want pillage or slaughter, give the word.";

            }

            return "[Soldier inquiry] " + actor + ": My lord, things are getting tense here. Shall I take men to hold the street and disperse onlookers?";

        }



        if (soldierIsBloodthirsty)

        {

            return SiegeAftermathCompatibilityTexts.Raw("local_civ_fallback_line_massacre").Replace("{actor}", actor);

        }

        if (victimDown)

        {

            return SiegeAftermathCompatibilityTexts.Raw("local_civ_fallback_line_control").Replace("{actor}", actor);

        }

        return SiegeAftermathCompatibilityTexts.Raw("local_civ_fallback_line_crowd").Replace("{actor}", actor);

    }





        private static string[] LoadCruelTerms()
    {
        var list = new List<string> { "bloodthirsty", "blood-thirsty", "cruel", "ruthless", "merciless", "sadistic", "brutal", "savage", "slaughter", "massacre", "plunder-hungry", "loot-hungry" };
        string raw = SiegeAftermathCompatibilityTexts.Raw("cruel_keywords");
        if (!string.IsNullOrEmpty(raw))
        {
            list.AddRange(raw.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries));
        }
        return list.ToArray();
    }

    public static bool ResolveSoldierWitnessBloodthirstyFromPersona(string personalityText, string backgroundText = null)

    {

        string text = ((personalityText ?? "") + " " + (backgroundText ?? "")).Trim();

        if (string.IsNullOrWhiteSpace(text))

        {

            return false;

        }

        return ContainsAny(text, LoadCruelTerms());

    }



    private static string BuildSoldierWitnessResponsePolicy(bool victimDown, bool soldierIsBloodthirsty)

    {

        if (soldierIsBloodthirsty)

        {

            return SiegeAftermathCompatibilityTexts.Raw("local_civ_cruel_instruction");

        }

        return victimDown

            ? SiegeAftermathCompatibilityTexts.Raw("local_civ_down_instruction")

            : SiegeAftermathCompatibilityTexts.Raw("local_civ_hit_instruction");

    }



    private static string BuildSoldierPersonaLine(string soldierPersonaText)

    {

        string persona = NormalizeOptionalText(soldierPersonaText);

        return string.IsNullOrWhiteSpace(persona)

            ? ""

            : SiegeAftermathCompatibilityTexts.Raw("local_civ_persona_prefix") + persona + SiegeAftermathCompatibilityTexts.Raw("local_civ_persona_suffix");

    }



    private static string NormalizeOptionalText(string text)

    {

        return (text ?? "").Replace("\r", " ").Replace("\n", " ").Trim();

    }



    private static bool ContainsAny(string text, params string[] needles)

    {

        if (string.IsNullOrWhiteSpace(text) || needles == null)

        {

            return false;

        }

        foreach (string needle in needles)

        {

            if (!string.IsNullOrWhiteSpace(needle) && text.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0)

            {

                return true;

            }

        }

        return false;

    }



    private static string NormalizeTargetName(string targetName, string fallback)

    {

        return string.IsNullOrWhiteSpace(targetName) ? fallback : targetName.Trim();

    }

}


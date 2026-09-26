using System;
using System.Collections.Generic;

namespace AnimusForge.Refactor.Adapters;

/// <summary>
/// Finite compatibility catalog for the action protocol currently owned by
/// AnimusForge. Patterns are action names, not arbitrary raw text: payloads
/// are still validated by the channel-owned executor against live state.
/// Keep this list aligned with RuleBehaviorPrompts/ActionPostprocessPrompts;
/// do not replace it with ACTION:* because that would authorize an unknown
/// tag family in the detached path.
/// </summary>
public static class LegacyActionTagCatalog
{
    public static IReadOnlyList<string> DefaultAllowedTagFamilies { get; } = Array.AsReadOnly(new[]
    {
        "ACTION:AGENDA",
        "ACTION:CASTLE_*",
        "ACTION:DIPLOMACY",
        "ACTION:DIVORCE",
        "ACTION:DUEL",
        "ACTION:DUEL_LINE_LOSE",
        "ACTION:DUEL_LINE_WIN",
        "ACTION:DUEL_STAKE_*",
        "ACTION:GIVE",
        "ACTION:GIVE_ASSET",
        "ACTION:GIVE_GOLD",
        "ACTION:GIVE_ITEM",
        "ACTION:INTIMACY_INTERNAL",
        "ACTION:ISSUE_*",
        "ACTION:JOIN_MERCENARY",
        "ACTION:JOIN_VASSAL",
        "ACTION:KING_ABDICATE_TO_PLAYER",
        "ACTION:KINGDOM_*",
        "ACTION:LET_PLAYER_GO",
        "ACTION:LORDS_HALL_BRIBE_PRICE",
        "ACTION:LOVE_DELTA",
        "ACTION:MARRIAGE_*",
        "ACTION:MEETING_TAUNT_*",
        "ACTION:MOOD",
        "ACTION:NOBLE_*",
        "ACTION:NPC_SURRENDER",
        "ACTION:OPEN_LORDS_HALL",
        "ACTION:PROPOSE",
        "ACTION:QUEST_TURN_IN",
        "ACTION:SCENE_*",
        "ACTION:SETS_*",
        "ACTION:SETTLEMENT_TRANSFER",
        "ACTION:TOWN_*",
        "ACTION:TRADE_TRUST",
        "ACTION:TROOP_INSPECTION_SLAUGHTER_PRISONERS",
        "ACTION:VASSALAGE",
        "ACTION:VOTE_DEAL",
        "ACTION:PEACE",
        "ACTION:WORLDMAP_ORDER",
        "ACTION:1",
        "ACTION:2",
        "ACTION:3",
        "ACTION:4",
        "ACTION:5",
        "ACTION:6",
        "ACTION:7",
        "ACTION:8",
        "ACTION:9",
        "ACTION:10",
        "ACTION:11",
        "ACTION:12",
        "A:H_J_P_P_C&L",
        "A:C_J_P_K",
        "A:C_J_K",
        "A:P_J_K_M",
        "A:P_J_K_V",
        "A:P_L_K",
        "AD",
        "ADP",
        "ASS",
        "GUI",
        "ATT",
        "ATP",
        "RELAY",
        "FOL",
        "STP",
        "END"
    });
}

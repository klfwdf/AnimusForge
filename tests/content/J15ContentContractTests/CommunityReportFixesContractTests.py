"""Focused contracts for the four reported CN/EN and encounter/diplomacy issues."""
import json
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]


def read(relative):
    return (ROOT / relative).read_text(encoding="utf-8-sig")


def strings(relative):
    return {
        entry.attrib["id"]: entry.attrib["text"]
        for entry in ET.parse(ROOT / relative).iter("string")
    }


# Fixed role identities are stored in compatibility language and projected only
# when creating the EN/CN display or prompt string.
conversation_owner = read("src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs")
capture_texts = read("src/AF.GameAdapter.Bannerlord/Prompt/AdapterPromptCaptureTexts.cs")
compat = json.loads(read("content/foundation/AF.Foundation.Localization/ModuleData/AdapterPromptCaptureCompatibility.json"))
capture_en = json.loads(read("content/foundation/AF.Foundation.Localization/ModuleData/AdapterPromptCapture.en.json"))
capture_zh = json.loads(read("content/foundation/AF.Foundation.Localization/ModuleData/AdapterPromptCapture.zh-CN.json"))
assert compat["Project_RosterWanderer"] == "流浪者"
assert capture_en["Project_RosterWanderer"] == "wanderer"
assert capture_zh["Project_RosterWanderer"] == "流浪者"
assert "AdapterPromptCaptureTexts.PcapProjectKnown((npc?.RoleDesc ?? \"\").Trim())" in conversation_owner
assert "GetSceneNpcListIdentityForPrompt(npc)" in conversation_owner
assert 'entry.Key.StartsWith("Project_", StringComparison.Ordinal)' in capture_texts

# An alliance remains active until an explicitly scoped break; its native
# diplomacy row reports the persistent contract instead of the rolling timer.
alliance_guard = read("src/modules/AF.Module.Diplomacy/Guards/PermanentAllianceGuard.cs")
end_postfix = strings("content/modules/AF.Module.Diplomacy/ModuleData/Languages/afdip_ui_strings.xml")
end_zh = strings("content/modules/AF.Module.Diplomacy/ModuleData/Languages/CNs/afdip_ui_strings-zh-CN.xml")
assert end_postfix["AF_DIP_UI_AllianceInfinite"] == "Indefinite"
assert end_zh["AF_DIP_UI_AllianceInfinite"] == "无限期"
assert "TryPatchAllianceDisplay(harmony)" in alliance_guard
assert 'AccessTools.Method(vmType, "UpdateDiplomacyProperties")' in alliance_guard
assert 'new TextObject("{=AF_DIP_UI_AllianceInfinite}Indefinite")' in alliance_guard
assert "endTime.SetValue(__instance" in alliance_guard and '"HasAlliance"' in alliance_guard
assert "if (IsAuthorized(kingdom1, kingdom2, out string source))" in alliance_guard
assert '"world_diplomacy_break_alliance", "world_diplomacy_declare_war"' in alliance_guard
assert '"diplomacy_break_alliance"' in alliance_guard

# Persona facts carry game-thread life-state labels plus explicit precedence,
# in the same CN/EN world-entity prompt resource already consumed by Knowledge.
persona_capture = read("src/AF.GameAdapter.Bannerlord/Prompt/PersonaGenerationFactCaptureAdapter.cs")
world_entities = read("src/modules/AF.Module.Knowledge/Entities/WorldEntityRetrievalService.cs")
world_zh = json.loads(read("content/modules/AF.Module.Knowledge/ModuleData/WorldEntityPromptTexts.json"))
world_en = json.loads(read("content/modules/AF.Module.Knowledge/ModuleData/WorldEntityPromptTexts.en.json"))
assert 'WorldEntityRetrievalService.FormatHeroRelatives(hero, 24)' in persona_capture
assert 'WorldEntityPromptTexts.Get("FamilyStatusAuthority"' in persona_capture
assert 'WorldEntityPromptTexts.Get("FamilyStatusAuthority"' in world_entities
assert "hero.IsAlive ? WorldEntityPromptTexts.Get(\"LivingSuffix\"" in world_entities
assert "WorldEntityPromptTexts.Get(\"DeceasedSuffix\"" in world_entities
assert "生死状态" in world_zh["FamilyStatusAuthority"]
assert "life or death status" in world_en["FamilyStatusAuthority"]

# Native-dialog surrender remains bound to its captured encounter and permits
# a transiently unavailable vanilla party read only during teardown.
encounter = read("src/AF.GameAdapter.Bannerlord/Encounter/LordEncounterBehavior.cs")
assert "_npcSurrenderScope.IsCurrent(PlayerEncounter.Current, _pendingNativeConversationNpcSurrenderParty" in encounter
assert "|| IsPendingNpcSurrenderEncounterPartyReplaced()" in encounter
assert "observed != null && !ReferenceEquals(observed, _pendingNativeConversationNpcSurrenderParty)" in encounter
assert "_pendingNativeConversationNpcSurrenderParty != null" in encounter
assert "TryForcePendingNativeConversationNpcSurrenderIfReady();" in encounter

print("PASS community-report contracts: CN/EN role projection, indefinite alliance display with explicit break authorization, authoritative persona life states, captured-party surrender lifecycle")

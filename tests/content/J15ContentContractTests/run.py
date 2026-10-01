from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
import uuid
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[3]
DEFAULT_RUN_ROOT = ROOT / "artifacts" / "j15-content" / ("j15-contracts-" + uuid.uuid4().hex)
J15B_BASELINE_REVISION = "f54a812757699f108371b6bfd1c37de0222d9adb"

J15A_EXPECTED = {
    "ModuleData/PreprocessPrompts.json": {
        "owner": "AF.Module.Prompt",
        "source": "content/modules/AF.Module.Prompt/ModuleData/PreprocessPrompts.json",
        "logicalName": "AnimusForge.Defaults.PreprocessPrompts.json",
        "withCulture": None,
        "sha256": "985FB377A96B7530396189EF5C23AE4AFB1F29A3C2E4400D1CB163EE98743099",
    },
    "ModuleData/RpItemIntroductionPrompts.json": {
        "owner": "AF.Module.Economy",
        "source": "content/modules/AF.Module.Economy/ModuleData/RpItemIntroductionPrompts.json",
        "logicalName": "AnimusForge.Defaults.RpItemIntroductionPrompts.json",
        "withCulture": None,
        "sha256": "EC30EB9B15030189F6F49E30BC3F3955848524CCC99F388A4784EE015606458D",
    },
    "ModuleData/GcczTownPrompt.zh-CN.json": {
        "owner": "AnimusForge.SiegeAftermathIntervention",
        "source": "content/modules/AnimusForge.SiegeAftermathIntervention/ModuleData/GcczTownPrompt.zh-CN.json",
        "logicalName": "AnimusForge.Defaults.GcczTownPrompt.zh-CN.json",
        "withCulture": "false",
        "sha256": "7537562A3B84A364432BFE870A7E3FABCE4B1AB7E6042B388A010685AA06C552",
    },
    "ModuleData/GcczTownEntryPresentation.zh-CN.json": {
        "owner": "AnimusForge.SiegeAftermathIntervention",
        "source": "content/modules/AnimusForge.SiegeAftermathIntervention/ModuleData/GcczTownEntryPresentation.zh-CN.json",
        "logicalName": "AnimusForge.Defaults.GcczTownEntryPresentation.zh-CN.json",
        "withCulture": "false",
        "sha256": "F99FDE95CF454D26F028B7B2B95EA694603710118E95EB4CEB2B8C2AF9FC63AB",
    },
    "ModuleData/GcczTownActionPresentation.zh-CN.json": {
        "owner": "AnimusForge.SiegeAftermathIntervention",
        "source": "content/modules/AnimusForge.SiegeAftermathIntervention/ModuleData/GcczTownActionPresentation.zh-CN.json",
        "logicalName": "AnimusForge.Defaults.GcczTownActionPresentation.zh-CN.json",
        "withCulture": "false",
        "sha256": "8F069A55C5784CD14539EA4B4C4D1763EA3A9D7B613B6A35B35A425EB484523F",
    },
    "ModuleData/GcczTownHiddenResidents.zh-CN.json": {
        "owner": "AnimusForge.SiegeAftermathIntervention",
        "source": "content/modules/AnimusForge.SiegeAftermathIntervention/ModuleData/GcczTownHiddenResidents.zh-CN.json",
        "logicalName": "AnimusForge.Defaults.GcczTownHiddenResidents.zh-CN.json",
        "withCulture": "false",
        "sha256": "11FC1D2FD52FC264B9DDBA70F1C7ADF311A9D8DC40CE60944E091959A015E2B0",
    },
    "ModuleData/GcczTownManual.zh-CN.json": {
        "owner": "AnimusForge.SiegeAftermathIntervention",
        "source": "content/modules/AnimusForge.SiegeAftermathIntervention/ModuleData/GcczTownManual.zh-CN.json",
        "logicalName": "AnimusForge.Defaults.GcczTownManual.zh-CN.json",
        "withCulture": "false",
        "sha256": "625F65983F53D817AAB96CFC88FE489979017B9C19FF0E575A8AE4F4273B790D",
    },
}


J15B_EXPECTED: dict[str, dict[str, str]] = {}


def add_j15b_group(owner: str, source_root: str, targets: list[str]) -> None:
    for target in targets:
        if target in J15B_EXPECTED:
            raise AssertionError(f"duplicate test target: {target}")
        J15B_EXPECTED[target] = {
            "owner": owner,
            "source": f"{source_root}/{target}",
        }


add_j15b_group("AF.Module.Prompt", "content/modules/AF.Module.Prompt", [
    "ModuleData/ActionPostprocessPrompts.json",
    "ModuleData/RuleBehaviorPrompts.json",
])
add_j15b_group("AF.Module.Social", "content/modules/AF.Module.Social", [
    "ModuleData/ProactiveNpcRequestPrompts.json",
    "GUI/Prefabs/PlayerNotorietyPopup.xml",
    "GUI/SpriteParts/af_player_notoriety/af_player_notoriety_background.png",
    "GUI/SpriteParts/af_player_notoriety/af_player_notoriety_bar_frame.png",
    "GUI/SpriteParts/af_player_notoriety/af_player_notoriety_bar_frame_overlay.png",
    "GUI/SpriteParts/af_player_notoriety/af_player_notoriety_century_patch.png",
    "GUI/SpriteParts/af_player_notoriety/af_player_notoriety_culture_patch.png",
    "GUI/SpriteParts/af_player_notoriety/af_player_notoriety_fill_culture.png",
    "GUI/SpriteParts/af_player_notoriety/af_player_notoriety_fill_mask.png",
    "GUI/SpriteParts/af_player_notoriety/af_player_notoriety_fill_world.png",
])
add_j15b_group("AF.Module.Conversation", "content/modules/AF.Module.Conversation", [
    "ModuleData/TownAmbientDialogue.json",
    "GUI/Prefabs/AnimusForgeConversationHistoryLog.xml",
    "GUI/Prefabs/AnimusForgeNativeConversationOverlay.xml",
    "GUI/Prefabs/CourierLetterInputPopup.xml",
    "GUI/Prefabs/CourierLetterReplyPopup.xml",
    "GUI/Prefabs/ShoutTextInputPopup.xml",
    "GUI/SpriteParts/af_courier/af_courier_reply_notice.png",
    "GUI/SpriteParts/af_courier/af_courier_scroll.png",
])
add_j15b_group("AF.Module.WarStats", "content/modules/AF.Module.WarStats", [
    "ModuleData/Languages/afwarstats_strings.xml",
    "ModuleData/Languages/CNs/afwarstats_strings-zh-CN.xml",
])
add_j15b_group(
    "AnimusForge.SiegeAftermathIntervention",
    "content/modules/AnimusForge.SiegeAftermathIntervention",
    [
        "ModuleData/Languages/CNs/gccz_town_manual_strings.xml",
        "ModuleData/Languages/sets_hostile_meeting_strings.xml",
        "ModuleData/Languages/CNs/sets_hostile_meeting_strings-zh-CN.xml",
    ],
)
add_j15b_group("AnimusForge.XihaiAction", "content/modules/AnimusForge.XihaiAction", [
    "ModuleData/Languages/sceneactions_strings.xml",
    "ModuleData/Languages/CNs/sceneactions_strings-zh-CN.xml",
    "ModuleData/SceneActions/battle-speech-performance.v1.json",
    "ModuleData/SceneActions/battle-speech.v1.json",
    "ModuleData/SceneActions/settings.v1.json",
    "ModuleData/SceneActions/settings.v2.json",
    "ModuleData/SceneActions/settings.v3.json",
    "ModuleData/SceneActions/settings.v4.json",
    "ModuleData/action_sets.xml",
    "ModuleData/action_types.xml",
    "ModuleData/combat_parameters.xml",
    "ModuleData/project.mbproj",
    "ModuleData/sceneactions.mbproj",
    "ModuleData/sceneactions_items.xml",
])
add_j15b_group("AF.Module.Economy", "content/modules/AF.Module.Economy", [
    "ModuleData/animusforge_scene_gold_items.xml",
    "GUI/Prefabs/PlayerRpForgePopup.xml",
    "GUI/SpriteParts/af_player_rp_forge/af_player_rp_forge_background.png",
])
add_j15b_group(
    "AF.Foundation.Localization",
    "content/foundation/AF.Foundation.Localization",
    ["ModuleData/Languages/language_data.xml", "ModuleData/Languages/CNs/language_data.xml"],
)
add_j15b_group("AF.Foundation.UI", "content/foundation/AF.Foundation.UI", [
    "GUI/Brushes/AFCourierLetterBrushes.xml",
    "GUI/Prefabs/FloatingTextLayer.xml",
])
add_j15b_group("AF.Module.UI", "content/modules/AF.Module.UI", [
    "GUI/Prefabs/AnimusForgeTerminalPopup.xml",
    "GUI/Prefabs/DevHistoryEditPopup.xml",
    "GUI/Prefabs/DevLargeSelectionPopup.xml",
])
add_j15b_group("AF.Module.Onboarding", "content/modules/AF.Module.Onboarding", [
    "GUI/Prefabs/AnimusForgeApiOnboardingPopup.xml",
])
add_j15b_group("AF.Module.WorldEvents", "content/modules/AF.Module.WorldEvents", [
    "GUI/Prefabs/AnimusForgeWorldEventInboxPopup.xml",
])
add_j15b_group("AF.Module.Weekly", "content/modules/AF.Module.Weekly", [
    "GUI/Prefabs/AnimusForgeWorldMessageTimelinePopup.xml",
    "GUI/Prefabs/DevWeeklyReportPopup.xml",
    "GUI/SpriteParts/af_weekly_chronicle/af_weekly_chronicle_clean.png",
    "GUI/SpriteParts/af_weekly_report/af_weekly_report.png",
])
add_j15b_group("PolicySystem", "content/modules/PolicySystem", [
    "GUI/Prefabs/CustomPolicyComposePopup.xml",
    "GUI/Prefabs/CustomPolicyHistoryPopup.xml",
    "GUI/Prefabs/CustomPolicyResultPopup.xml",
    "GUI/Prefabs/LocalPolicyComposePopup.xml",
    "GUI/Prefabs/LocalPolicyHistoryPopup.xml",
    "GUI/Prefabs/PolicyEffectModuleManagerPopup.xml",
])
add_j15b_group("AF.Module.Diplomacy", "content/modules/AF.Module.Diplomacy", [
    "GUI/Prefabs/WorldDiplomacyComposePopup.xml",
    "GUI/SpriteParts/af_vassalage_notifications/af_npc_tributary_vassalage.png",
    "GUI/SpriteParts/af_vassalage_notifications/af_vassalage_breach.png",
    "GUI/SpriteParts/af_vassalage_notifications/af_vassalage_contract.png",
    "GUI/SpriteParts/af_vassalage_notifications/af_vassalage_protection.png",
    "GUI/SpriteParts/af_vassalage_notifications/af_vassalage_tribute.png",
    "GUI/SpriteParts/af_world_diplomacy/af_world_diplomacy_notice_v2.png",
])

J15B_HOLD_PATHS = {
    "ModuleData/EarlyException_2026-06-28.html",
    "ModuleData/FeatureBridges.json",
    "ModuleData/UnnamedNpcProfiles.json",
    "ModuleData/UnnamedNpcProfiles/troop_hidden_hand_tier_3_kingdom_empire_w__f1274041.json",
    "ModuleData/UnnamedNpcProfiles/troop_imperial_veteran_archer_kingdom_empire_w__71cc7949.json",
    "ModuleData/UnnamedNpcProfiles/troop_townsman_empire_kingdom_empire_w__a97b2802.json",
    "ModuleData/UnnamedNpcProfiles/troop_townsman_vlandia_kingdom_vlandia__edca3492.json",
    "ModuleData/UnnamedNpcProfiles/troop_townswoman_empire_kingdom_empire_s__365dece0.json",
    "GUI/SplitShadowsOnlyAISpriteData.xml",
    "GUI/SpriteParts/Config.xml",
    "GUI/SpriteParts/af_courier/af_courier_scroll_version_a.png",
    "GUI/SpriteParts/af_player_notoriety/af_player_notoriety_background.png.tmp.png",
    "GUI/SpriteParts/af_player_notoriety/af_player_notoriety_culture_panel_patch.png",
    "GUI/SpriteParts/af_terminal/af_scroll_quill.png",
    "GUI/SpriteParts/af_world_diplomacy/af_world_diplomacy_notice.png",
    "GUI/SpriteSheets/af_vassalage_notifications/af_vassalage_notifications_1.png",
    "GUI/SpriteParts/ui_account/MODDB.png",
    "GUI/SpriteParts/ui_account/STEAM.png",
    "GUI/SpriteParts/ui_account/discord.png",
    "GUI/SpriteParts/ui_account/input.png",
    "GUI/SpriteParts/ui_account/ss_account_background.png",
    "GUI/SpriteParts/ui_achievement/bim.png",
    "GUI/SpriteParts/ui_achievement/icon_hint.png",
    "GUI/SpriteParts/ui_achievement/return.png",
    "GUI/SpriteParts/ui_achievement/split_titile.png",
    "GUI/SpriteParts/ui_achievement/ss_button_down.png",
    "GUI/SpriteParts/ui_achievement/ss_button_right.png",
    "GUI/SpriteParts/ui_achievement/ss_deepseek.png",
    "GUI/SpriteParts/ui_achievement/ss_tax.png",
    "GUI/SpriteParts/ui_achievement/ss_x2.png",
    "GUI/SpriteParts/ui_subscribe/duke.png",
    "GUI/SpriteParts/ui_subscribe/knight.png",
    "GUI/SpriteParts/ui_subscribe/lord.png",
    "GUI/SpriteParts/ui_subscribe/ss_campaigns_title.png",
    "GUI/SpriteParts/ui_subscribe/ss_center_title.png",
    "GUI/SpriteParts/ui_subscribe/ss_duke_title.png",
    "GUI/SpriteParts/ui_subscribe/ss_knight_title.png",
    "GUI/SpriteParts/ui_subscribe/ss_lord_title.png",
    "GUI/SpriteParts/ui_subscribe/ss_outline.png",
    "GUI/SpriteParts/ui_subscribe/ss_subscribe_level_background.png",
    "GUI/SpriteParts/ui_subscribe/subscribe_background_3.png",
}

J15C_EXPECTED = {
    "ModuleData/FeatureBridges.json": {
        "owner": "AF.Foundation.Runtime",
        "source": "content/foundation/AF.Foundation.Runtime/ModuleData/FeatureBridges.json",
        "sha256": "10C573B461EC148EF8478A0D4269F73A7CA106F5A8C6A50C9D37FACC4FF86896",
    },
}

J15C_PROMPT_EXPECTED = {
    "CustomPrompts/DailyMemoryCompressionWritingRequirements.json": {
        "owner": "AF.Module.Memory",
        "source": "content/modules/AF.Module.Memory/CustomPrompts/DailyMemoryCompressionWritingRequirements.json",
        "sha256": "F98FDE4FBC3B46AF28360F1878323BA4EC7933C10C2958694BAFC2A16483CDA8"
    },
    "CustomPrompts/KingdomRebellionSystemPrompt.json": {
        "owner": "AF.Module.Kingdom",
        "source": "content/modules/AF.Module.Kingdom/CustomPrompts/KingdomRebellionSystemPrompt.json",
        "sha256": "3F09ABD7AEB0BCB6A79A381D5E108754F8C851030939C13E3D5183A15AA628A3"
    },
    "CustomPrompts/MajorActionCompressionWritingRequirements.json": {
        "owner": "AF.Module.Memory",
        "source": "content/modules/AF.Module.Memory/CustomPrompts/MajorActionCompressionWritingRequirements.json",
        "sha256": "76FE731751575BDC20DA2E6D305F673E12E07F60884C85143E848D2B3C9770BB"
    },
    "CustomPrompts/MemoryOverviewCompressionWritingRequirements.json": {
        "owner": "AF.Module.Memory",
        "source": "content/modules/AF.Module.Memory/CustomPrompts/MemoryOverviewCompressionWritingRequirements.json",
        "sha256": "001E824BBE950960C46AAA8106BA4E77DE891111F602818A30CE9BAE5C92427C"
    },
    "CustomPrompts/NpcPersonaGenerationRequirements.json": {
        "owner": "AF.Module.Persona",
        "source": "content/modules/AF.Module.Persona/CustomPrompts/NpcPersonaGenerationRequirements.json",
        "sha256": "2D22EF667F1876FE5F9D034F53D4AC88E5252350ECA80DC5FD4315615DE23C46"
    },
    "CustomPrompts/PlayerCustomPromptRule.json": {
        "owner": "AF.Module.Prompt",
        "source": "content/modules/AF.Module.Prompt/CustomPrompts/PlayerCustomPromptRule.json",
        "sha256": "A1634CE335855F66C209196B2581A2A85FE26A24392F69318F7AA71FDEC38752"
    },
    "CustomPrompts/Policy/CustomPolicyEvaluatorPrompt.json": {
        "owner": "PolicySystem",
        "source": "content/modules/PolicySystem/CustomPrompts/Policy/CustomPolicyEvaluatorPrompt.json",
        "sha256": "A3E55780284E374399A5BDC341F04E0DA4B7F8BE3F7AD95DD59DFACFF2FA86C8"
    },
    "CustomPrompts/Policy/Effects/_Common.json": {
        "owner": "PolicySystem",
        "source": "content/modules/PolicySystem/CustomPrompts/Policy/Effects/_Common.json",
        "sha256": "22DEB7EBB84EEE57759C6F928ADF8C030474AF585E28F464E54DD6FC343542DD"
    },
    "CustomPrompts/Policy/Effects/armyFormationTendencyPct.json": {
        "owner": "PolicySystem",
        "source": "content/modules/PolicySystem/CustomPrompts/Policy/Effects/armyFormationTendencyPct.json",
        "sha256": "06DF8D195219863004845E526E44AE2656CF5D5AC11CF6B5D9E30452B38948FD"
    },
    "CustomPrompts/Policy/Effects/clanInfluence.json": {
        "owner": "PolicySystem",
        "source": "content/modules/PolicySystem/CustomPrompts/Policy/Effects/clanInfluence.json",
        "sha256": "4C81BD8FD757E5DD2F80DA1037806816415713DF8407EC2BBB092F2EFC70AC54"
    },
    "CustomPrompts/Policy/Effects/clanLeaderRelationOnce.json": {
        "owner": "PolicySystem",
        "source": "content/modules/PolicySystem/CustomPrompts/Policy/Effects/clanLeaderRelationOnce.json",
        "sha256": "BEA0F61625F44BE055B77653EABDC8115ED5ACBB7496852749A880A1859A021C"
    },
    "CustomPrompts/Policy/Effects/constructionPerDay.json": {
        "owner": "PolicySystem",
        "source": "content/modules/PolicySystem/CustomPrompts/Policy/Effects/constructionPerDay.json",
        "sha256": "25AB8E65C8A377DEA734BFC69F7DB67B08654643111A250B730763DF15561AE1"
    },
    "CustomPrompts/Policy/Effects/foodPerDay.json": {
        "owner": "PolicySystem",
        "source": "content/modules/PolicySystem/CustomPrompts/Policy/Effects/foodPerDay.json",
        "sha256": "E29DD6FCD5C0CB20CA6BE466DF7E848E3B4A0A14C9BCF78B12B17E69C500A3EB"
    },
    "CustomPrompts/Policy/Effects/hearthPerDay.json": {
        "owner": "PolicySystem",
        "source": "content/modules/PolicySystem/CustomPrompts/Policy/Effects/hearthPerDay.json",
        "sha256": "C5FDDA6DC400F49941B6BE29702B9E85CE19439F8D6CF310FCAB18C593426BDD"
    },
    "CustomPrompts/Policy/Effects/heroGold.json": {
        "owner": "PolicySystem",
        "source": "content/modules/PolicySystem/CustomPrompts/Policy/Effects/heroGold.json",
        "sha256": "E0D279AC34F7541F904368BAE9C397D8049A3AB80BA368C7F54113BF4C06B4B6"
    },
    "CustomPrompts/Policy/Effects/kingdomStability.json": {
        "owner": "PolicySystem",
        "source": "content/modules/PolicySystem/CustomPrompts/Policy/Effects/kingdomStability.json",
        "sha256": "CD227EF3658CC649C2B9AB5C00D2F2B8A8D5A51F3713D015750554746B6585C2"
    },
    "CustomPrompts/Policy/Effects/kingdomVillageRaidBan.json": {
        "owner": "PolicySystem",
        "source": "content/modules/PolicySystem/CustomPrompts/Policy/Effects/kingdomVillageRaidBan.json",
        "sha256": "577D9ECAD527CD254958D5329DE601B28671474D7656210381701DD91CA6CC2B"
    },
    "CustomPrompts/Policy/Effects/loyaltyPerDay.json": {
        "owner": "PolicySystem",
        "source": "content/modules/PolicySystem/CustomPrompts/Policy/Effects/loyaltyPerDay.json",
        "sha256": "48E1378098C69013E1CCFE35A63D3D2E2DBF35CB4A41CA2E88EA049A79D850DC"
    },
    "CustomPrompts/Policy/Effects/militiaPerDay.json": {
        "owner": "PolicySystem",
        "source": "content/modules/PolicySystem/CustomPrompts/Policy/Effects/militiaPerDay.json",
        "sha256": "8A3C7D56A49AFD63BCB85C9F64786FFBD6A00E406226FA6E2A89219D7FA6AF6F"
    },
    "CustomPrompts/Policy/Effects/partySizeLimit.json": {
        "owner": "PolicySystem",
        "source": "content/modules/PolicySystem/CustomPrompts/Policy/Effects/partySizeLimit.json",
        "sha256": "2CBF42B52B0D7EF271E510B55A2C744923D0F82BC794BD79635E24FBE2ABF9B0"
    },
    "CustomPrompts/Policy/Effects/prosperityPerDay.json": {
        "owner": "PolicySystem",
        "source": "content/modules/PolicySystem/CustomPrompts/Policy/Effects/prosperityPerDay.json",
        "sha256": "8D078E4C87D85B30F56904FF9526E3116C9AA61BEE843892593D08C18254E39C"
    },
    "CustomPrompts/Policy/Effects/securityPerDay.json": {
        "owner": "PolicySystem",
        "source": "content/modules/PolicySystem/CustomPrompts/Policy/Effects/securityPerDay.json",
        "sha256": "07EF750768E207510B8F2CA199AEF0B3DF3A20C8760C1A31442178EFB1DF3F52"
    },
    "CustomPrompts/Policy/Effects/soldierTroopXp.json": {
        "owner": "PolicySystem",
        "source": "content/modules/PolicySystem/CustomPrompts/Policy/Effects/soldierTroopXp.json",
        "sha256": "3B29F8B387522E947F7E7F0B4BFFFB3B7158FB806C5FADE5C479557E1A5491F2"
    },
    "CustomPrompts/Policy/Effects/taxIncomePct.json": {
        "owner": "PolicySystem",
        "source": "content/modules/PolicySystem/CustomPrompts/Policy/Effects/taxIncomePct.json",
        "sha256": "BED00201317CEF2E08261385B220D519435EC4FCFCB7314826C091ABDD0A3E0B"
    },
    "CustomPrompts/Policy/Effects/villageProductionPct.json": {
        "owner": "PolicySystem",
        "source": "content/modules/PolicySystem/CustomPrompts/Policy/Effects/villageProductionPct.json",
        "sha256": "1417C3FF9D5A269955C019B3F01268FBADF0E55451BB13CA5C8696B5E1767389"
    },
    "CustomPrompts/Policy/Effects/volunteerProductionGrowthPct.json": {
        "owner": "PolicySystem",
        "source": "content/modules/PolicySystem/CustomPrompts/Policy/Effects/volunteerProductionGrowthPct.json",
        "sha256": "949822045EF6089CC3D19470F894811089F72A9B7AC490B77F78C713BF56E2AE"
    },
    "CustomPrompts/Policy/NpcRulerPolicyPrompt.json": {
        "owner": "PolicySystem",
        "source": "content/modules/PolicySystem/CustomPrompts/Policy/NpcRulerPolicyPrompt.json",
        "sha256": "9DEECA538F12E9F5A861D1A50554A9A2814FD2FBCF1179CC59B12E0AB28237C1"
    },
    "CustomPrompts/Policy/PlayerPolicyAutoDraftPrompt.json": {
        "owner": "PolicySystem",
        "source": "content/modules/PolicySystem/CustomPrompts/Policy/PlayerPolicyAutoDraftPrompt.json",
        "sha256": "25BA42E2DB584E644CA4C9E223DF27896FD54BBD4898F55EDAB22BE5919AB90D"
    },
    "CustomPrompts/WeeklyReportWritingRequirements.json": {
        "owner": "AF.Module.Weekly",
        "source": "content/modules/AF.Module.Weekly/CustomPrompts/WeeklyReportWritingRequirements.json",
        "sha256": "8655B07FE203288E3F579A2CB6043EAF711BE263559185272DE224FF10C210DB"
    },
    "CustomPrompts/WorldDiplomacyPrompt.json": {
        "owner": "AF.Module.Diplomacy",
        "source": "content/modules/AF.Module.Diplomacy/CustomPrompts/WorldDiplomacyPrompt.json",
        "sha256": "A447D0691F322388EDF27B9075DCA624414015964BE367D8A379BA78E3EC168A"
    }
}
J15C_EXPECTED.update(J15C_PROMPT_EXPECTED)

F4D_ARCHIVED_PRIVATE_PATHS = {
    "ModuleData/EarlyException_2026-06-28.html",
    "ModuleData/UnnamedNpcProfiles.json",
    "ModuleData/UnnamedNpcProfiles/troop_hidden_hand_tier_3_kingdom_empire_w__f1274041.json",
    "ModuleData/UnnamedNpcProfiles/troop_imperial_veteran_archer_kingdom_empire_w__71cc7949.json",
    "ModuleData/UnnamedNpcProfiles/troop_townsman_empire_kingdom_empire_w__a97b2802.json",
    "ModuleData/UnnamedNpcProfiles/troop_townsman_vlandia_kingdom_vlandia__edca3492.json",
    "ModuleData/UnnamedNpcProfiles/troop_townswoman_empire_kingdom_empire_s__365dece0.json",
    "VoiceMapping.json",
}
F4U_ARCHIVED_LEGACY_GUI_PATHS = {
    path for path in J15B_HOLD_PATHS
    if path in {"GUI/SplitShadowsOnlyAISpriteData.xml", "GUI/SpriteParts/Config.xml"}
    or path.startswith(("GUI/SpriteParts/ui_account/", "GUI/SpriteParts/ui_achievement/",
                        "GUI/SpriteParts/ui_subscribe/"))
}
F4U_RETIRED_REMAINING_GUI_PATHS = {
    "GUI/SpriteParts/af_courier/af_courier_scroll_version_a.png",
    "GUI/SpriteParts/af_player_notoriety/af_player_notoriety_background.png.tmp.png",
    "GUI/SpriteParts/af_player_notoriety/af_player_notoriety_culture_panel_patch.png",
    "GUI/SpriteParts/af_terminal/af_scroll_quill.png",
    "GUI/SpriteParts/af_world_diplomacy/af_world_diplomacy_notice.png",
    "GUI/SpriteSheets/af_vassalage_notifications/af_vassalage_notifications_1.png",
}
CURRENT_HOLD_PATHS = (J15B_HOLD_PATHS - J15C_EXPECTED.keys() - F4D_ARCHIVED_PRIVATE_PATHS
                      - F4U_ARCHIVED_LEGACY_GUI_PATHS - F4U_RETIRED_REMAINING_GUI_PATHS)
F4A_EXPECTED = {
    "AssetPackages/pack0.tpac": {
        "owner": "AnimusForge.XihaiAction",
        "source": "extensions/AnimusForge.XihaiAction/AssetPackages/pack0.tpac",
        "sha256": "95A97D81A78A3096B2A5B1D91F4C17444909028390AE108BE885B629ABF05BD2",
    },
}
# SHA-256 of the committed LF text; the check normalizes CRLF so autocrlf checkouts compare equal.
F4A_DESIGN_EXPECTED = {
    "README_scene_gold_coin.txt": "CED5590F7DE76EE7990FCFAC1A75048E08EB4EB17260D0859478D0D5A2B754FF",
    "SubModule_items_patch_example.xml": "4EA952CE0F0FA0F834E61C028989979A824DAEAABCB4299C6C636F3DAF5DDB45",
    "animusforge_denar_coin.mtl": "8AF8B844CBBB56F7E6A88D87FD015ADA790BFC23E6550A7B6333A518EC62EE29",
    "animusforge_denar_coin.obj": "BA559C57E41E48A3B6D062CF3FA4F63815E3A1DCFCC2C5731B2DE5E49F2CDD6D",
    "animusforge_denar_coin.prefab.xml": "0D0BF8DD651221BF95835204CBEC399099C715C4487FF61D01C3AA32DEEAE55B",
    "animusforge_scene_gold_items.xml": "75028E1310479B368EEC743C878E12B1CB4B12292A9412C90750BCA833C68602",
}
# ada9894a four-feature integration content (DialogueUI, Illustrator, RichExecutions/Vengeance).
# SHA-256 of the checked-out source bytes, like J15A/F4A.
INTEGRATION_EXPECTED = {
    "GUI/Prefabs/AFDialogueConversation.xml": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/Prefabs/AFDialogueConversation.xml", "sha256": "676414590946FD91E0E0ECFB22DDF18FAE2FA7B626912DC97F1190E82170F696"},
    "GUI/Prefabs/AFDialogueMapConversation.xml": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/Prefabs/AFDialogueMapConversation.xml", "sha256": "30A2BA529106F15BBF49D682C2180576AD8F985F224CFF9FBF69145856648911"},
    "GUI/Prefabs/AFDialogueNativeOverlay.xml": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/Prefabs/AFDialogueNativeOverlay.xml", "sha256": "0A97DE1528CBD6A0E7BB7BDA6B37120FAEDF232AA53C99D250B7EF8032CED968"},
    "GUI/Prefabs/AFDialogueShout.xml": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/Prefabs/AFDialogueShout.xml", "sha256": "EA62F5D09727EDC43EA000583AEF9DB593C19EFA71D34551E363AE41468A6E31"},
    "GUI/Prefabs/AFSceneSessionFolio.xml": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/Prefabs/AFSceneSessionFolio.xml", "sha256": "A1DCE3AFB46ABCF10F9CED7E0CDBF387714E54615A9B77882071B4BF368B6DD7"},
    "GUI/Prefabs/AFSceneSessionScroll.xml": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/Prefabs/AFSceneSessionScroll.xml", "sha256": "6EEDF5885A9C277D333C483CAA61D5D7E06C982DDC22B1412FA13D1D171505AB"},
    "GUI/Prefabs/AFSceneWheel.xml": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/Prefabs/AFSceneWheel.xml", "sha256": "7643BACADE9D9E8521DA90C3FEF877FC7B58754C7A6AE703E2EEE15822CC6C0D"},
    "GUI/SpriteParts/afdui_3tier_console_chassis_clean.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_3tier_console_chassis_clean.png", "sha256": "65ABE47320063E6B9FFD7D13C8635C35BE4382FB0DE9F9D429016198A0B3AE54"},
    "GUI/SpriteParts/afdui_audience_docket_base.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_audience_docket_base.png", "sha256": "B18B5CDAB7AEB06D7C331D194E83E0F7655E9AA2F871E83ECB84D0C3A09AA9FD"},
    "GUI/SpriteParts/afdui_audience_docket_base_clean.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_audience_docket_base_clean.png", "sha256": "B18B5CDAB7AEB06D7C331D194E83E0F7655E9AA2F871E83ECB84D0C3A09AA9FD"},
    "GUI/SpriteParts/afdui_audience_docket_pure_clean.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_audience_docket_pure_clean.png", "sha256": "B3CC2798474DF58F1F0EF395370B4C5729930ACCFF8DB53B7B454193D2588F13"},
    "GUI/SpriteParts/afdui_aux_panel.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_aux_panel.png", "sha256": "25AB57E4EB595A672BECDF5FFB0C3D91B178F877F1CB57D9DC395793AAF20D1B"},
    "GUI/SpriteParts/afdui_btn_medallion.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_btn_medallion.png", "sha256": "7D49A849C41C911781A8B5CEA1761CE6710C18013DAE35BAB9E64EBC10D1CAC7"},
    "GUI/SpriteParts/afdui_btn_wax_seal.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_btn_wax_seal.png", "sha256": "25D883B867A66A0E9A68F8E3368AEE69F3E8DEE596BDD113539E25062FE540F7"},
    "GUI/SpriteParts/afdui_button_hover.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_button_hover.png", "sha256": "829485936B218AC289102EF69E2CB34CD10D597807CA0135892594F27D205498"},
    "GUI/SpriteParts/afdui_button_normal.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_button_normal.png", "sha256": "19258B33E38160245D39FFEA8469B229A1E9794ED6442C8F585F6575A6B50C56"},
    "GUI/SpriteParts/afdui_button_plate_hover.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_button_plate_hover.png", "sha256": "A669C0F7FB6CE4B15E7A03A2C53534563A6B4E25DC3E104C29C6945C2B087D1E"},
    "GUI/SpriteParts/afdui_button_plate_normal.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_button_plate_normal.png", "sha256": "091EB753ABFEF045AE9F39A4B59D4651A3451748985AD3601B7D8846F228A15B"},
    "GUI/SpriteParts/afdui_button_plate_pressed.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_button_plate_pressed.png", "sha256": "E6503D6C6ABD5DD1E8D9E6C50E182FD0B54FA65B67F060BF35E78A4A045AD1D3"},
    "GUI/SpriteParts/afdui_button_pressed.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_button_pressed.png", "sha256": "D2F7463D6D21D65363AA87FD7917D1433613C831CD2AFEDB40E2D45942451A2D"},
    "GUI/SpriteParts/afdui_capsule_topic.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_capsule_topic.png", "sha256": "73AB0FF47FA5906F15A64CEED88391B58FE1EEF1B41C29236A879FECAAAC0E11"},
    "GUI/SpriteParts/afdui_console_base.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_console_base.png", "sha256": "4FB5335A2666E4AC648CD2729ADB2BD3040AE94F1693F1D70ABADF1C86C44956"},
    "GUI/SpriteParts/afdui_console_base_option_01.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_console_base_option_01.png", "sha256": "E311251A70673249938614EA2020203C420F84D7E729B9B5207EA476C80E6E05"},
    "GUI/SpriteParts/afdui_console_base_option_02.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_console_base_option_02.png", "sha256": "EAD2B7ABDD3AED354B3648583CC659C23B0E07FB5EA869F57C4F7C2A4938D60D"},
    "GUI/SpriteParts/afdui_console_base_option_02_walnut.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_console_base_option_02_walnut.png", "sha256": "5DB51DC00DCDA9AB58DAC8420B506EACA143A811AFE107B7B2DC6D9C05CFF2CA"},
    "GUI/SpriteParts/afdui_console_base_option_02_walnut_original_ratio.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_console_base_option_02_walnut_original_ratio.png", "sha256": "1D58A92E7478889A8B0D3DA0BF5D3E192BB06DDA90DE0CB9BD80390609C2468D"},
    "GUI/SpriteParts/afdui_console_base_option_03.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_console_base_option_03.png", "sha256": "52260714B42AF9E6BF86B202833410E1F43E93756C227C1B3BC9949427604271"},
    "GUI/SpriteParts/afdui_dual_codex_base_clean.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_dual_codex_base_clean.png", "sha256": "E1829A8FBC5A073DD786E308D3530CB9C6E83E260C2416DF06B96ADBC41905B2"},
    "GUI/SpriteParts/afdui_icon_coin.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_icon_coin.png", "sha256": "595AA6B595405B1A94D4E3C67A974F638C8C41A2873FFF7EFCECBF500FA61489"},
    "GUI/SpriteParts/afdui_icon_search.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_icon_search.png", "sha256": "7300AB7828555D7BF3DACFAF4AB003C4BB8663FB1A6BAD42C10FA141AB9DFAAB"},
    "GUI/SpriteParts/afdui_input_panel.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_input_panel.png", "sha256": "3FFC459E39E984DEB2F30B1676BAA74F82B92872E73E5B7589054B6ED3C77B65"},
    "GUI/SpriteParts/afdui_nameplate.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_nameplate.png", "sha256": "7AD0917119540229D4D58A38664DF676E8277B8CA8A86B396540A4FCE5012717"},
    "GUI/SpriteParts/afdui_parchment_panel.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_parchment_panel.png", "sha256": "CB742A355EACDA1A16ABF3CEEDCF475BBD33F4F9FCB032CE70339B4623D10387"},
    "GUI/SpriteParts/afdui_persuasion_dot.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_persuasion_dot.png", "sha256": "3D4166FEDAC3BA0D68C1601F586749573358938215E8724080B918DA1765B0B3"},
    "GUI/SpriteParts/afdui_plaque_nameplate.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_plaque_nameplate.png", "sha256": "5CCE9004AF187E0A4A0C7E6121BD20595A4ECE1B85524C5D05DB5C754AB80097"},
    "GUI/SpriteParts/afdui_portrait_background.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_portrait_background.png", "sha256": "3628AA725561512C6426EB279E94F9208B8F46A21C64C58B80BE64661C1A3ED5"},
    "GUI/SpriteParts/afdui_portrait_frame_redesign.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_portrait_frame_redesign.png", "sha256": "EA6B52DE0C0617D7B16C96A0928315B2CEC4BFC1F4A0690526F06885ACADCDFD"},
    "GUI/SpriteParts/afdui_portrait_option_01.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_portrait_option_01.png", "sha256": "F3DDEB2E0DEE0920420A8F3F912E298A453090886906FEBE9DAA6D511D995CFB"},
    "GUI/SpriteParts/afdui_portrait_option_02.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_portrait_option_02.png", "sha256": "A2B0C3FA274F322BD0D59FB4EE813F73FB34C17BF8D7AB62183A14330092902B"},
    "GUI/SpriteParts/afdui_portrait_option_03.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_portrait_option_03.png", "sha256": "92AEB2E4EA54AA652D91E2D2CF46C701E72EA8F3739CFD78299E634B42DD8FD8"},
    "GUI/SpriteParts/afdui_radial_wheel_base.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_radial_wheel_base.png", "sha256": "2554979CFF343A206BCA6F86425AD03375A4538F540C7D31988F1F2C046C457C"},
    "GUI/SpriteParts/afdui_ribbon_tab.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_ribbon_tab.png", "sha256": "DF62A1E1EDF84D48B5B874B50D89E293A17C8589C9DF32C976ECFC5766A5BEDF"},
    "GUI/SpriteParts/afdui_row_hover.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_row_hover.png", "sha256": "7C6198C3790E26C3DF4D0C3DF717DCAB417B8E573616A82BE617140D569A819E"},
    "GUI/SpriteParts/afdui_row_normal.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_row_normal.png", "sha256": "E0F89DC98BC3CD5E36FE43AFBF3D9CE142F69BE881931E0A9733700E0DF30628"},
    "GUI/SpriteParts/afdui_row_selected.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_row_selected.png", "sha256": "1108982B112996FB4CBAC8AE9E789B8A13510217C7460C6F24E518D76B06DD14"},
    "GUI/SpriteParts/afdui_scroll_body.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_scroll_body.png", "sha256": "2868AB1C15B466D424A77C54E7C61F3ABC22370A7AD84C2BFDF29977000E2703"},
    "GUI/SpriteParts/afdui_scroll_chassis_clean.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_scroll_chassis_clean.png", "sha256": "648E32D5B55637F5C1179F0DC92DC7D17907C229920447746404BE25BC890B85"},
    "GUI/SpriteParts/afdui_scroll_handle.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_scroll_handle.png", "sha256": "432D73F60CAADDCC1EA200DB8BF87481E63D756A4EB8C2D36668A02A44D823FB"},
    "GUI/SpriteParts/afdui_scroll_left.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_scroll_left.png", "sha256": "1BD84143C8EB2ADC05597B22832B5B12E54023076D034B5C3F9DB3CF37487B48"},
    "GUI/SpriteParts/afdui_scroll_right.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_scroll_right.png", "sha256": "69AA7B1C2744EE4409233F6E4DFD3096C43A4C67514B480E6687FFE051580FCE"},
    "GUI/SpriteParts/afdui_seal_base_gold.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_seal_base_gold.png", "sha256": "C09D5F49D420FDB21AC125D0BCA9B3B12E7AE1B82B174BFEE5E71A543D08F93F"},
    "GUI/SpriteParts/afdui_seal_base_green.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_seal_base_green.png", "sha256": "37DD6E867068C099BE1DB2D3EDF7A9EBF1595DA31A38365E1084704B0CCE668B"},
    "GUI/SpriteParts/afdui_seal_base_red.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_seal_base_red.png", "sha256": "71AE677EB7101E14AE60C571FAE30F1FFF7FC8EDB7158DAC1AFCBA857ACC2668"},
    "GUI/SpriteParts/afdui_seal_exclude.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_seal_exclude.png", "sha256": "71A1E010B748305E4FEDD6550E88E8E22F88B1BAA0AB4573F8C744E6054A65E3"},
    "GUI/SpriteParts/afdui_seal_locked.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_seal_locked.png", "sha256": "49F185A6815F300A85D68DB739E254FF1D92E0A06B731F59268693D19E0FD413"},
    "GUI/SpriteParts/afdui_seal_participate.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_seal_participate.png", "sha256": "DB88AA3655287661603C61232496B96C613283D8E367C296B644E3ACB8864C5F"},
    "GUI/SpriteParts/afdui_shout_dialog.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_shout_dialog.png", "sha256": "2FBAFC69C491BE5814BAAA9FA61CFC72886EF3197F2CB5992EF540C23CC171FE"},
    "GUI/SpriteParts/afdui_shout_scroll_base.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_shout_scroll_base.png", "sha256": "4D16DA99C9BB0A38FB7BBD19EA2ACF62697D00CAB41348B616FAB200916E238D"},
    "GUI/SpriteParts/afdui_side_console_base.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_side_console_base.png", "sha256": "9DE2CD66BC2BDF3F655529B4C7DC470E471A86DC8EAB02F64A90160D4B51F6EA"},
    "GUI/SpriteParts/afdui_side_parchment_console_base.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_side_parchment_console_base.png", "sha256": "D75884BA5CC04FF0FEF9FAF759AEDA32297B51613877F021CE7EE64CCE91B8E1"},
    "GUI/SpriteParts/afdui_side_parchment_folio_clean.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_side_parchment_folio_clean.png", "sha256": "AF41DD307AEE0E7889B1872513BB5D475DC5CDEB41F925195A2D80DE69FC0A47"},
    "GUI/SpriteParts/afdui_tab_hover.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_tab_hover.png", "sha256": "2CACC0E6FE7CEF52D1F1DC54E64A6178380E9D4ADE864F854ECA680EFE4CE274"},
    "GUI/SpriteParts/afdui_tab_normal.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_tab_normal.png", "sha256": "1DF7591583C6AEDB044910C2F0A69DDCDA128AFDEFB71DE13E4A8481601C9AE4"},
    "GUI/SpriteParts/afdui_tab_pressed.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_tab_pressed.png", "sha256": "4C524F0A7A5E563F58C58BD724BC8F08240A2CA2ECAA37171835E48A32AC4D6F"},
    "GUI/SpriteParts/afdui_topic_capsule.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_topic_capsule.png", "sha256": "74B41B79E05B1727A35C40521FD925680DB91B96415E1A04200A6953026EEACB"},
    "GUI/SpriteParts/afdui_wax_seal.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_wax_seal.png", "sha256": "5E46D3EB77C6C2F8F6CDB4C8A4E3351C1F3211E9D5DD9C094E6C209E24C7FF13"},
    "GUI/SpriteParts/afdui_wheel_chassis_clean.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_wheel_chassis_clean.png", "sha256": "CB29C56F5B74340D57D05D4E36DDAAA595DF7351C20BE935EA7D9AB6B4089B05"},
    "GUI/SpriteParts/afdui_wheel_chassis_symmetric.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_wheel_chassis_symmetric.png", "sha256": "C679974D4C0EE67CFB37AF02A497D9037CAD96BD8E086A28B8CADCABCD052427"},
    "GUI/SpriteParts/afdui_wheel_chassis_symmetric_hollow.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_wheel_chassis_symmetric_hollow.png", "sha256": "CA434174D3F8657C94649F4F4797345C9598A89E5660D3C19E66875C90A324D5"},
    "GUI/SpriteParts/afdui_wheel_wedge_actions.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_wheel_wedge_actions.png", "sha256": "50B2B7F142AD30ECCBAAEAFC09311A723EE5702218FEB32B3CAEF7FA9A440FD4"},
    "GUI/SpriteParts/afdui_wheel_wedge_give.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_wheel_wedge_give.png", "sha256": "E91A14F4133237CF5FFF461D254FBA59BA0CD15B06E5613A58B5413DDE3C65BA"},
    "GUI/SpriteParts/afdui_wheel_wedge_leave.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_wheel_wedge_leave.png", "sha256": "90D698A40F216866DCF018CC769014EA2C8F676297055B920DB037454F5C8234"},
    "GUI/SpriteParts/afdui_wheel_wedge_talk.png": {"owner": "AnimusForge.DialogueUI", "source": "extensions/AnimusForge.DialogueUI/GUI/SpriteParts/afdui_wheel_wedge_talk.png", "sha256": "A4BCFC5B7145C761A5BFDC9A01B29D1380791422DCF80C913D5E93C9CE952C0D"},
    "GUI/Prefabs/ConversationIllustrationFullscreenBackdrop.xml": {"owner": "AnimusForge.Illustrator", "source": "extensions/AnimusForge.Illustrator/GUI/Prefabs/ConversationIllustrationFullscreenBackdrop.xml", "sha256": "E68BBFC3548BDB8D0B4BC04E9C493E80C9CC6F1146FECAC2A04F5A7CA778E568"},
    "GUI/Prefabs/ConversationIllustrationFullscreenOverlay.xml": {"owner": "AnimusForge.Illustrator", "source": "extensions/AnimusForge.Illustrator/GUI/Prefabs/ConversationIllustrationFullscreenOverlay.xml", "sha256": "9683ED43A827AEC32BEC356FD78FEC630E5BE0DBAEEB6DD96149A4E2C901579E"},
    "GUI/Prefabs/ConversationIllustrationOverlay.xml": {"owner": "AnimusForge.Illustrator", "source": "extensions/AnimusForge.Illustrator/GUI/Prefabs/ConversationIllustrationOverlay.xml", "sha256": "FD2BE51832D99E74A629B4CF2DA015155773723A54A790D089B62794A0A4679D"},
    "GUI/Prefabs/EncyclopediaIllustrationOverlay.xml": {"owner": "AnimusForge.Illustrator", "source": "extensions/AnimusForge.Illustrator/GUI/Prefabs/EncyclopediaIllustrationOverlay.xml", "sha256": "83A7BB907308A5C56B0E79C757DB589A07E1EAE416F0121886E87630C4307E6B"},
    "GUI/Prefabs/IllustratorGalleryPopup.xml": {"owner": "AnimusForge.Illustrator", "source": "extensions/AnimusForge.Illustrator/GUI/Prefabs/IllustratorGalleryPopup.xml", "sha256": "C2D01FF3301696EB6C3B680D930CB644F030A5143F2D47CD9A1E002554891920"},
    "GUI/Prefabs/IllustratorOffscreenStage.xml": {"owner": "AnimusForge.Illustrator", "source": "extensions/AnimusForge.Illustrator/GUI/Prefabs/IllustratorOffscreenStage.xml", "sha256": "6E12733C511A75D195EDB6B65FDA2BB4FAC9BD7D10F16229A983378CAD64C454"},
    "GUI/Prefabs/WeeklyReportIllustrationOverlay.xml": {"owner": "AnimusForge.Illustrator", "source": "extensions/AnimusForge.Illustrator/GUI/Prefabs/WeeklyReportIllustrationOverlay.xml", "sha256": "FF6AAF77572D9B0768DDD2274BB7DF0938F3B5624ACFB0D218E721EF12E42EED"},
    "AssetPackages/vengeance_pack0.tpac": {"owner": "RichExecutions", "source": "AnimusForge/AssetPackages/vengeance_pack0.tpac", "sha256": "B7CE1AB8751375BBB03046459C46702F07606CF5023F93EE894BA02C0DCA3A92"},
    "GUI/Prefabs/RichExecutionJudgement.xml": {"owner": "RichExecutions", "source": "AnimusForge/GUI/Prefabs/RichExecutionJudgement.xml", "sha256": "0E5D14CF8528FF06BD2D12E094E32261FB460B83F5C6F0C2FD655CD8DC90FEE8"},
    "GUI/Prefabs/RichExecutionSiteBuilder.xml": {"owner": "RichExecutions", "source": "AnimusForge/GUI/Prefabs/RichExecutionSiteBuilder.xml", "sha256": "5D147A30517346F9A6A094EB440FD872F6209CBCA50E1DCEC06FA0EC5A7CD849"},
    "GUI/Prefabs/rex_speech_bubble.xml": {"owner": "RichExecutions", "source": "AnimusForge/GUI/Prefabs/rex_speech_bubble.xml", "sha256": "80C0A2D433DB7E4317866FD2728B8B8752AABD83D6D5323774689FD886FEFE78"},
    "ModuleData/vengeance_items.xml": {"owner": "RichExecutions", "source": "AnimusForge/ModuleData/vengeance_items.xml", "sha256": "B7EFED97CFD42A96F2F1F450AC8193E18287599CC040DE0E5AC94C8E41D4B1DA"},
    "ModuleData/Languages/vengeance_strings-en.xml": {"owner": "RichExecutions", "source": "AnimusForge/ModuleData/Languages/vengeance_strings-en.xml", "sha256": "21E0D9CBBBF81B3260942FF751491429D3CB6BF4222E005698C551F011092501"},
    "ModuleData/Languages/CNs/vengeance_strings-zh-CN.xml": {"owner": "RichExecutions", "source": "AnimusForge/ModuleData/Languages/CNs/vengeance_strings-zh-CN.xml", "sha256": "FBE0D80B6114D5DECD239F4CA42AE83FD42187385CB3BCD9C7ABECE5F94F3386"},
}
# User-approved instant world bulletin (即时快报) content owned by AF.Module.Weekly. These sources
# post-date the J15b baseline, so they are pinned by reviewed SHA-256 of the checked-out bytes.
# Consumers: DevWeeklyReportPopup.cs loads the movie; AnimusForgeWeeklyReportMapNotification.cs
# registers the af_world_bulletin sprite category.
WORLD_BULLETIN_EXPECTED = {
    "GUI/Prefabs/WorldBulletinPanel.xml": {"owner": "AF.Module.Weekly", "source": "content/modules/AF.Module.Weekly/GUI/Prefabs/WorldBulletinPanel.xml", "sha256": "E29638ACEE4B442273D4F4112FF3664F801BA6C0C13FD60DE63FEFA1BD5A2EEA"},
    "GUI/SpriteParts/af_world_bulletin/af_world_bulletin_parchment.png": {"owner": "AF.Module.Weekly", "source": "content/modules/AF.Module.Weekly/GUI/SpriteParts/af_world_bulletin/af_world_bulletin_parchment.png", "sha256": "EDEA0C8F25950F509118FCA13682404B0FC9372F3AAFDC53DFAE9EA18A9C1EDF"},
    "GUI/SpriteParts/af_world_bulletin/af_world_bulletin_seal.png": {"owner": "AF.Module.Weekly", "source": "content/modules/AF.Module.Weekly/GUI/SpriteParts/af_world_bulletin/af_world_bulletin_seal.png", "sha256": "9EC2745C0EC13BFF5A830E63A50083D13E0A52A89E14BFBC52F3F9BAAB82A067"},
    "GUI/SpriteParts/af_world_bulletin/af_world_bulletin_rule_l.png": {"owner": "AF.Module.Weekly", "source": "content/modules/AF.Module.Weekly/GUI/SpriteParts/af_world_bulletin/af_world_bulletin_rule_l.png", "sha256": "F82E84F61B924BBFE0F550401D318BD9E7F9F35D3DC47A941EA24765ABD726E3"},
    "GUI/SpriteParts/af_world_bulletin/af_world_bulletin_rule_r.png": {"owner": "AF.Module.Weekly", "source": "content/modules/AF.Module.Weekly/GUI/SpriteParts/af_world_bulletin/af_world_bulletin_rule_r.png", "sha256": "3DBCD9AC0C13AED014FA1F672261DC2FF5CCCD0EE76B6B57C78C9C3F97F24E3D"},
}
# Native map-bar terminal entry brushes (layers resolved by ItemId in AfWarStatsMapNavigationEntry.cs).
TERMINAL_MAPBAR_EXPECTED = {
    "GUI/Brushes/AFTerminalMapBarBrushes.xml": {"owner": "AF.Module.WarStats", "source": "content/modules/AF.Module.WarStats/GUI/Brushes/AFTerminalMapBarBrushes.xml", "sha256": "3655AD1396F4D69D88257493E97BB48424511517B0FD97C35490B7C69B144AFC"},
    "GUI/SpriteParts/af_terminal/af_terminal_icon.png": {"owner": "AF.Module.WarStats", "source": "content/modules/AF.Module.WarStats/GUI/SpriteParts/af_terminal/af_terminal_icon.png", "sha256": "9E3CA854E1E88B66D270910A7D8CB0479B8FF0BEA6B4FC8C8F187997BAEAE42F"},
}
# Courier stationery presets (MCM 信纸样式): new sprite layers loaded on demand by AnimusForgeCourierUiSprites.cs.
COURIER_THEME_EXPECTED = {
    "GUI/SpriteParts/af_courier/af_courier_scroll_base.png": {"owner": "AF.Module.Conversation", "source": "content/modules/AF.Module.Conversation/GUI/SpriteParts/af_courier/af_courier_scroll_base.png", "sha256": "985537894FD584D6D96547321C50CA67273D37D1DC3372BD390B193E19332B38"},
    "GUI/SpriteParts/af_courier/af_courier_pattern_left_1.png": {"owner": "AF.Module.Conversation", "source": "content/modules/AF.Module.Conversation/GUI/SpriteParts/af_courier/af_courier_pattern_left_1.png", "sha256": "C0A3E7118280B228FB9A33273180B5663A962151842FB5D6676D1DCF700C794D"},
    "GUI/SpriteParts/af_courier/af_courier_pattern_left_2.png": {"owner": "AF.Module.Conversation", "source": "content/modules/AF.Module.Conversation/GUI/SpriteParts/af_courier/af_courier_pattern_left_2.png", "sha256": "2B40FB6EBA480F0BBCE7A8217B34908E1ED7533BD8F80DA5B0C69472ABC48AF9"},
    "GUI/SpriteParts/af_courier/af_courier_pattern_left_3.png": {"owner": "AF.Module.Conversation", "source": "content/modules/AF.Module.Conversation/GUI/SpriteParts/af_courier/af_courier_pattern_left_3.png", "sha256": "50E582D551C9B0DD9F965A97AA2AA2A01833B1A6AAA86C2B1F93DC3AF6EAABF5"},
    "GUI/SpriteParts/af_courier/af_courier_pattern_left_4.png": {"owner": "AF.Module.Conversation", "source": "content/modules/AF.Module.Conversation/GUI/SpriteParts/af_courier/af_courier_pattern_left_4.png", "sha256": "B280F3082B2241985816DC7ABD45FEFF82BBEB513EF376A788966319D74AEB53"},
    "GUI/SpriteParts/af_courier/af_courier_pattern_left_5.png": {"owner": "AF.Module.Conversation", "source": "content/modules/AF.Module.Conversation/GUI/SpriteParts/af_courier/af_courier_pattern_left_5.png", "sha256": "2A8EAE98F61E2C3BE3809599437253B34D27F50135D72749EF5F56068BEB39C2"},
    "GUI/SpriteParts/af_courier/af_courier_pattern_right_1.png": {"owner": "AF.Module.Conversation", "source": "content/modules/AF.Module.Conversation/GUI/SpriteParts/af_courier/af_courier_pattern_right_1.png", "sha256": "28F588B6848FDE861A9F9BDDFF67190382229A6B8E93006837602B274A68FF17"},
    "GUI/SpriteParts/af_courier/af_courier_pattern_right_2.png": {"owner": "AF.Module.Conversation", "source": "content/modules/AF.Module.Conversation/GUI/SpriteParts/af_courier/af_courier_pattern_right_2.png", "sha256": "94099EF5F97107CCDC6F7004D71B5025F40A3821E75C3D760C015C2067CB8878"},
    "GUI/SpriteParts/af_courier/af_courier_pattern_right_3.png": {"owner": "AF.Module.Conversation", "source": "content/modules/AF.Module.Conversation/GUI/SpriteParts/af_courier/af_courier_pattern_right_3.png", "sha256": "AE5BDB465B1399E6F8BD8EB0D75EAE27B68CCDB0702BB9226AD5CB9BA361C567"},
    "GUI/SpriteParts/af_courier/af_courier_pattern_right_4.png": {"owner": "AF.Module.Conversation", "source": "content/modules/AF.Module.Conversation/GUI/SpriteParts/af_courier/af_courier_pattern_right_4.png", "sha256": "C2EF71AF48FDAEFEED4BCC82D46A41CF842627C7AB69484D65B0EA3213EFB543"},
    "GUI/SpriteParts/af_courier/af_courier_pattern_right_5.png": {"owner": "AF.Module.Conversation", "source": "content/modules/AF.Module.Conversation/GUI/SpriteParts/af_courier/af_courier_pattern_right_5.png", "sha256": "E54999B515BF27BAC1AA9352B849508A75DAE4A9B4AFCED2D5394DAB4074DD6D"},
    "GUI/SpriteParts/af_courier/af_courier_seal_vlandia.png": {"owner": "AF.Module.Conversation", "source": "content/modules/AF.Module.Conversation/GUI/SpriteParts/af_courier/af_courier_seal_vlandia.png", "sha256": "4157A76BBD8146CA27192B8C0062FEF42993386811E3F976125A21732733456F"},
    "GUI/SpriteParts/af_courier/af_courier_seal_north_empire.png": {"owner": "AF.Module.Conversation", "source": "content/modules/AF.Module.Conversation/GUI/SpriteParts/af_courier/af_courier_seal_north_empire.png", "sha256": "B3EB217C0CF3E56F64FDC5D93EAE30853C1191CAE74D35544C63BF7CD782A31F"},
    "GUI/SpriteParts/af_courier/af_courier_seal_west_empire.png": {"owner": "AF.Module.Conversation", "source": "content/modules/AF.Module.Conversation/GUI/SpriteParts/af_courier/af_courier_seal_west_empire.png", "sha256": "E43F8902744437B06B72025470ACFCE86E7CE294CB8583B5FD8183349A0F2DDE"},
    "GUI/SpriteParts/af_courier/af_courier_seal_south_empire.png": {"owner": "AF.Module.Conversation", "source": "content/modules/AF.Module.Conversation/GUI/SpriteParts/af_courier/af_courier_seal_south_empire.png", "sha256": "D65F1B6700805B558F517148B25E334E4722BC5BA6353917400F8E5B36452570"},
    "GUI/SpriteParts/af_courier/af_courier_seal_sturgia.png": {"owner": "AF.Module.Conversation", "source": "content/modules/AF.Module.Conversation/GUI/SpriteParts/af_courier/af_courier_seal_sturgia.png", "sha256": "05F86BC3A7725A7D2142743542BAD61D95743EDD09811D9FDECDF654D783887A"},
    "GUI/SpriteParts/af_courier/af_courier_seal_battania.png": {"owner": "AF.Module.Conversation", "source": "content/modules/AF.Module.Conversation/GUI/SpriteParts/af_courier/af_courier_seal_battania.png", "sha256": "E96D59CF7F0D5D2D40B4A9341D3FA20263D5CE2B2EF6BB6F225E7D4B94658D80"},
    "GUI/SpriteParts/af_courier/af_courier_seal_khuzait.png": {"owner": "AF.Module.Conversation", "source": "content/modules/AF.Module.Conversation/GUI/SpriteParts/af_courier/af_courier_seal_khuzait.png", "sha256": "24E0C958B403B8B4A6228FF0B6E0467CE3BC8C450D2CA16E2B536D6E536891C6"},
    "GUI/SpriteParts/af_courier/af_courier_seal_aserai.png": {"owner": "AF.Module.Conversation", "source": "content/modules/AF.Module.Conversation/GUI/SpriteParts/af_courier/af_courier_seal_aserai.png", "sha256": "83D3D9E4EEDFA5717426A763486B9650D4580F788B653DECA9528FA597CFEEC5"},
    "GUI/SpriteParts/af_courier/af_courier_seal_nord.png": {"owner": "AF.Module.Conversation", "source": "content/modules/AF.Module.Conversation/GUI/SpriteParts/af_courier/af_courier_seal_nord.png", "sha256": "0DA54FC85B49C404F07A39EC6687F126CC5FA41AE4AB3261AD106F55B2FEA7B2"},
    "GUI/SpriteParts/af_courier/af_courier_button_band.png": {"owner": "AF.Module.Conversation", "source": "content/modules/AF.Module.Conversation/GUI/SpriteParts/af_courier/af_courier_button_band.png", "sha256": "C1368261A84CAB434DCEEDA27421B34019CB51CDE361078E3AA2E171BBC965A9"},
}
EXPECTED = {**J15A_EXPECTED, **J15B_EXPECTED, **J15C_EXPECTED, **F4A_EXPECTED, **INTEGRATION_EXPECTED,
            **WORLD_BULLETIN_EXPECTED, **TERMINAL_MAPBAR_EXPECTED, **COURIER_THEME_EXPECTED}
# ada9894a edited these J15b-migrated sources (SceneActions postprocess rule, Vengeance language
# index entries, DialogueUI history/terminal prefabs, Xihai action sets). They must equal the J15b
# baseline at the integration parent and the reviewed SHA-256 of the checked-out bytes now.
J15B_REVIEWED_EDIT_PARENT = "99ca85ae"
J15B_REVIEWED_EDITS = {
    "ModuleData/ActionPostprocessPrompts.json": "509EE9248F938042CCF7338AE4ED2401631224DA8DCB95D7ED26C7553D8F0C38",
    "ModuleData/RuleBehaviorPrompts.json": "3396D30D95CCB808A8C798023467FC1EB2AEF9DFAA984EAAEA2C661EBC679CCF",
    "GUI/Prefabs/AnimusForgeConversationHistoryLog.xml": "59DF887251BC46F09ED5CB4DB8116F2D3AACBD404624A76A45B4CCD364F6026C",
    "ModuleData/action_sets.xml": "DD52422AC7F6B5D0BF7049964CD64E1A79E109E62414B0235CF7F7432966DB2B",
    "ModuleData/action_types.xml": "75DBD6F57B01D544EAABC5AA0165403A3AC8080985E340B00FB18A22FBCE91AD",
    "ModuleData/Languages/language_data.xml": "C552D3CC00DB957E729AECA07034C0D297733D71D4F7501D4B4AB0737D1AED6F",
    "ModuleData/Languages/CNs/language_data.xml": "B8DBB6C8F0A6CF6EF66B70ADF79F6CC8F6B57E1887151111DFB6DFD4A0CE5C65",
    # cb045840 blob is 96BE1920...; the uncommitted CivilWar UI relocation (faction tab moved to the
    # kingdom screen via KingdomFactionTab.cs) removes the terminal Faction tab button and panel (-64 lines).
    "GUI/Prefabs/AnimusForgeTerminalPopup.xml": "2ADD0334115222197AF191C03CED890A58384FFEF0384E807E8A61C02E39778B",
    # Uncommitted: +AFWST_TerminalNavTooltip for the native map-bar terminal entry (AfWarStatsTexts.cs).
    "ModuleData/Languages/afwarstats_strings.xml": "9291A45C7419EA55A414E1C80FD494C70C67E85AB3FBF9EC3F6610A6A930B27A",
    "ModuleData/Languages/CNs/afwarstats_strings-zh-CN.xml": "A06CDB6A0D3B4D19EC4224A4C02BC437E82DD8B55DD75D08E8D0C7991D2DDE05",
    # Courier stationery presets: themed layers bound from CourierLetterThemeVM (DataSource="{Theme}"),
    # plus stacked translucent band buttons (AFCourierLetter.Band.Button) replacing the native popup buttons.
    "GUI/Prefabs/CourierLetterInputPopup.xml": "E453B37E1184C9425A5711250FD6ADD04C300686D781DD5F2CAD9B49BA7675AC",
    "GUI/Prefabs/CourierLetterReplyPopup.xml": "EEECF1C2DC8262836469C091F564642EC38C32199E8C5F5821047EF25D14EB56",
    # +AFCourierLetter.Band.Button / .Text; the band sprite is pushed in at runtime by AnimusForgeCourierUiSprites.cs.
    "GUI/Brushes/AFCourierLetterBrushes.xml": "01A6E6C173C7F912C2182A6F07E99B8BCF141A11E044929D2C556A03C86FAA55",
}
# Uncommitted worktree edits on top of cb045840 INTEGRATION sources (CRLF-normalized SHA-256).
# AFDialogueNativeOverlay.xml: toolbar gains PositionYOffset="@ToolbarOffsetY" (NativeOverlayVM.cs:58).
# vengeance_strings-*.xml: +REX_Speech_Log_Line consumed by ExecutionSpeechDirector.cs:278 EchoToMessageLog.
INTEGRATION_WORKTREE_EDITS = {
    "GUI/Prefabs/AFDialogueNativeOverlay.xml": "CE9CD971A4402753A210ECDE4552062671F7DD4D5E6BC4CA559F6AD79E76A2DF",
    "ModuleData/Languages/vengeance_strings-en.xml": "F1D609E4DE90C3CAE583E3DFC5A7EEFD0E66F30B686EE88B793E9E4DC39E0896",
    "ModuleData/Languages/CNs/vengeance_strings-zh-CN.xml": "575D4CBFB1011F72EC35EDF76420F4F51048D2F491FCAAE278FEF18137FC10DF",
}


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


TEXT_SUFFIXES = {".xml", ".json", ".txt", ".mbproj"}


def source_digests(source: Path) -> set[str]:
    """Raw SHA-256, plus CRLF- and LF-normalized forms for text, so autocrlf and LF checkouts compare equal."""
    data = source.read_bytes()
    digests = {hashlib.sha256(data).hexdigest().upper()}
    if source.suffix.lower() in TEXT_SUFFIXES:
        lf = data.replace(b"\r\n", b"\n")
        digests.add(hashlib.sha256(lf).hexdigest().upper())
        digests.add(hashlib.sha256(lf.replace(b"\n", b"\r\n")).hexdigest().upper())
    return digests


def load_module(path: Path, name: str):
    spec = importlib.util.spec_from_file_location(name, path)
    check(spec is not None and spec.loader is not None, f"cannot import {path}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def git_worktree_blob(revision: str, path: str) -> bytes:
    return subprocess.check_output(
        ["git", "cat-file", "--filters", f"--path={path}", f"{revision}:{path}"],
        cwd=ROOT,
    )


def verify_map_and_resources() -> None:
    map_path = ROOT / "content" / "content-map.json"
    payload = json.loads(map_path.read_text(encoding="utf-8"))
    check(payload.get("schemaVersion") == 1, "content map schemaVersion")
    entries = payload.get("entries")
    check(isinstance(entries, list) and len(entries) == len(EXPECTED), "content map entry count")
    by_target = {entry["target"]: entry for entry in entries}
    check(len(by_target) == len(entries), "content map target uniqueness")
    check(len({entry["source"] for entry in entries}) == len(entries), "content map source uniqueness")
    check(set(by_target) == set(EXPECTED), "content map target set")

    for target, expected in J15A_EXPECTED.items():
        entry = by_target[target]
        for field in ("owner", "source", "logicalName"):
            check(entry.get(field) == expected[field], f"{target} {field}")
        source = ROOT / expected["source"]
        check(source.is_file(), f"missing migrated source: {source}")
        digest = hashlib.sha256(source.read_bytes()).hexdigest().upper()
        check(digest == expected["sha256"], f"source hash drift: {target}")
        old = ROOT / "AnimusForge" / Path(target)
        check(not old.exists(), f"old editable source remains: {old}")

    for target, expected in J15B_EXPECTED.items():
        entry = by_target[target]
        for field in ("owner", "source"):
            check(entry.get(field) == expected[field], f"{target} {field}")
        check(not entry.get("logicalName"), f"non-embedded content must not invent a LogicalName: {target}")
        source = ROOT / expected["source"]
        check(source.is_file(), f"missing migrated source: {source}")
        baseline = git_worktree_blob(J15B_BASELINE_REVISION, f"AnimusForge/{target}")
        if target in J15B_REVIEWED_EDITS:
            # Unchanged up to the pre-integration commit, then exactly the reviewed edit.
            check(git_worktree_blob(J15B_REVIEWED_EDIT_PARENT, expected["source"]) == baseline,
                  f"source bytes drifted from J15b baseline before reviewed edit: {target}")
            check(J15B_REVIEWED_EDITS[target] in source_digests(source),
                  f"source hash drift after reviewed edit: {target}")
        else:
            check(source.read_bytes() == baseline, f"source bytes drifted from J15b baseline: {target}")
        old = ROOT / "AnimusForge" / Path(target)
        check(not old.exists(), f"old editable source remains: {old}")

    for target, expected in J15C_EXPECTED.items():
        entry = by_target[target]
        for field in ("owner", "source"):
            check(entry.get(field) == expected[field], f"{target} {field}")
        if target in J15C_PROMPT_EXPECTED:
            check(entry.get("sha256") == expected["sha256"], f"{target} baseline hash lock")
        check(not entry.get("logicalName"), f"non-embedded content must not invent a LogicalName: {target}")
        source = ROOT / expected["source"]
        check(source.is_file(), f"missing migrated source: {source}")
        check(hashlib.sha256(source.read_bytes()).hexdigest().upper() == expected["sha256"],
              f"source hash drift: {target}")
        check(not (ROOT / "AnimusForge" / target).exists(), f"old editable source remains: {target}")

    for target, expected in F4A_EXPECTED.items():
        entry = by_target[target]
        for field in ("owner", "source", "sha256"):
            check(entry.get(field) == expected[field], f"{target} {field}")
        check(not entry.get("logicalName"), f"binary content must not invent a LogicalName: {target}")
        source = ROOT / expected["source"]
        check(source.is_file() and source.stat().st_size == 1_671_304, f"authoritative TPAC missing or size drifted: {source}")
        source_bytes = source.read_bytes()
        check(hashlib.sha256(source_bytes).hexdigest().upper() == expected["sha256"], f"TPAC hash drift: {target}")
        check(source_bytes.count(b"nacisword1") == 1, f"Xihai action marker missing or duplicated: {target}")
        check(not (ROOT / "AnimusForge" / target).exists(), f"retired duplicate TPAC returned: {target}")

    for target, expected in INTEGRATION_EXPECTED.items():
        entry = by_target[target]
        for field in ("owner", "source"):
            check(entry.get(field) == expected[field], f"{target} {field}")
        check(not entry.get("logicalName"), f"non-embedded content must not invent a LogicalName: {target}")
        source = ROOT / expected["source"]
        check(source.is_file(), f"missing integration source: {source}")
        check(INTEGRATION_WORKTREE_EDITS.get(target, expected["sha256"]) in source_digests(source),
              f"source hash drift: {target}")

    for target, expected in WORLD_BULLETIN_EXPECTED.items():
        entry = by_target[target]
        for field in ("owner", "source"):
            check(entry.get(field) == expected[field], f"{target} {field}")
        check(not entry.get("logicalName"), f"non-embedded content must not invent a LogicalName: {target}")
        check(expected["source"] == f"content/modules/AF.Module.Weekly/{target}", f"world bulletin source layout: {target}")
        source = ROOT / expected["source"]
        check(source.is_file(), f"missing world bulletin source: {source}")
        check(expected["sha256"] in source_digests(source), f"source hash drift: {target}")
        check(not (ROOT / "AnimusForge" / target).exists(), f"world bulletin duplicated in legacy root: {target}")

    for target, expected in COURIER_THEME_EXPECTED.items():
        entry = by_target[target]
        for field in ("owner", "source"):
            check(entry.get(field) == expected[field], f"{target} {field}")
        check(not entry.get("logicalName"), f"non-embedded content must not invent a LogicalName: {target}")
        source = ROOT / expected["source"]
        check(source.is_file(), f"missing courier theme source: {source}")
        check(expected["sha256"] in source_digests(source), f"source hash drift: {target}")
        check(not (ROOT / "AnimusForge" / target).exists(), f"courier theme duplicated in legacy root: {target}")

    for target, expected in TERMINAL_MAPBAR_EXPECTED.items():
        entry = by_target[target]
        for field in ("owner", "source"):
            check(entry.get(field) == expected[field], f"{target} {field}")
        check(not entry.get("logicalName"), f"non-embedded content must not invent a LogicalName: {target}")
        source = ROOT / expected["source"]
        check(source.is_file(), f"missing terminal map bar source: {source}")
        check(expected["sha256"] in source_digests(source), f"source hash drift: {target}")
        if source.suffix.lower() == ".png":
            icon_loader = (ROOT / "WarStats/AfTerminalMapBarIconSprite.cs").read_text(encoding="utf-8-sig")
            check(source.name in icon_loader and 'Category = "af_terminal"' in icon_loader, "terminal map bar icon consumer drift")
            continue
        ET.parse(source)
        text = source.read_text(encoding="utf-8-sig")
        check('OverrideBrush="MapBar.Left.Icons"' in text and 'OverrideBrush="MapBar.Left.Button.Backgrounds"' in text, "terminal map bar brush overrides")
        consumer = (ROOT / "WarStats/AfWarStatsMapNavigationEntry.cs").read_text(encoding="utf-8-sig")
        check('ItemId = "af_terminal"' in consumer and text.count('BrushLayer Name="af_terminal"') == 2, "terminal map bar layer/ItemId drift")

    design_root = ROOT / "content" / "modules" / "AF.Module.Economy" / "AssetSources"
    check({path.name for path in design_root.iterdir()} == set(F4A_DESIGN_EXPECTED),
          "Economy design source set drifted")
    for name, expected_hash in F4A_DESIGN_EXPECTED.items():
        source = design_root / name
        check(source.is_file(), f"Economy design source missing: {name}")
        check(hashlib.sha256(source.read_bytes().replace(b"\r\n", b"\n")).hexdigest().upper() == expected_hash,
              f"Economy design source hash drift: {name}")
        check(not (ROOT / "AnimusForge" / "AssetSources" / name).exists(),
              f"old design source returned: {name}")
    check(not any(entry["source"].startswith("content/modules/AF.Module.Economy/AssetSources/")
                  for entry in entries), "design sources must not enter the runtime content map")

    for target in CURRENT_HOLD_PATHS:
        check((ROOT / "AnimusForge" / Path(target)).is_file(), f"HOLD path was moved or removed: {target}")
        check(target not in by_target, f"HOLD path entered content map: {target}")

    for target in F4D_ARCHIVED_PRIVATE_PATHS:
        check(not (ROOT / "AnimusForge" / Path(target)).exists(), f"retired private source returned: {target}")
        check(target not in by_target, f"retired private source entered content map: {target}")

    check(len(F4U_ARCHIVED_LEGACY_GUI_PATHS) == 27, "legacy GUI retirement set must remain exact")
    for target in F4U_ARCHIVED_LEGACY_GUI_PATHS:
        check(not (ROOT / "AnimusForge" / Path(target)).exists(), f"retired legacy GUI returned: {target}")
        check(target not in by_target, f"retired legacy GUI entered content map: {target}")

    check(len(F4U_RETIRED_REMAINING_GUI_PATHS) == 6, "remaining legacy GUI retirement set must remain exact")
    check(not F4U_RETIRED_REMAINING_GUI_PATHS - J15B_HOLD_PATHS, "unused GUI retirement must come from the J15b HOLD set")
    for target in F4U_RETIRED_REMAINING_GUI_PATHS:
        check(not (ROOT / "AnimusForge" / Path(target)).exists(), f"retired remaining GUI returned: {target}")
        check(target not in by_target, f"retired remaining GUI entered content map: {target}")

    remaining = {
        path.relative_to(ROOT / "AnimusForge").as_posix()
        for root_name in ("ModuleData", "GUI", "CustomPrompts")
        for path in (ROOT / "AnimusForge" / root_name).rglob("*")
        if path.is_file()
    }
    # ada9894a placed six mapped Vengeance sources in the legacy root; they are explicit, hashed
    # INTEGRATION_EXPECTED entries (not HOLD). The count is pinned so no new legacy file slips in.
    legacy_integration = {
        expected["source"][len("AnimusForge/"):]
        for expected in INTEGRATION_EXPECTED.values()
        if expected["source"].startswith("AnimusForge/") and expected["source"].split("/")[1] in ("ModuleData", "GUI", "CustomPrompts")
    }
    check(len(legacy_integration) == 6 and not legacy_integration & CURRENT_HOLD_PATHS,
          "legacy-root integration source set must remain exact")
    check(remaining == CURRENT_HOLD_PATHS | legacy_integration,
          "legacy resource roots must contain only explicit HOLD files or mapped integration sources")


def verify_project_resources() -> None:
    project = ET.parse(ROOT / "AnimusForge.csproj")
    resources = {}
    for item in project.getroot().iter("EmbeddedResource"):
        include = item.attrib.get("Include", "").replace("\\", "/")
        logical = item.findtext("LogicalName")
        culture = item.findtext("WithCulture")
        if logical:
            resources[logical] = {"include": include, "culture": culture}
    expected_names = {item["logicalName"] for item in J15A_EXPECTED.values()}
    check(set(resources) == expected_names, "EmbeddedResource LogicalName set must match the seven defaults")
    for expected in J15A_EXPECTED.values():
        actual = resources[expected["logicalName"]]
        check(actual["include"] == expected["source"], f"include path: {expected['logicalName']}")
        check(actual["culture"] == expected["withCulture"], f"WithCulture: {expected['logicalName']}")


def verify_script_wiring() -> None:
    deploy = (ROOT / "scripts" / "build" / "deploy_module.ps1").read_text(encoding="utf-8-sig")
    call = "Invoke-AnimusForgeContentProjection"
    check(deploy.count(call) == 1, "Stage and Deploy must share one content projection")
    stage = deploy[deploy.index('$projectStagePath = Join-Path $projectRootFull'):]
    check(stage.index("Reset-ProjectStageDirectory") < stage.index(call) <
          stage.index("Set-SingleModuleIdentity") < stage.index("Assert-AnimusForgeCleanStage") <
          stage.index("Invoke-ManagedStageDeployment"), "clean Stage must precede managed deployment")
    check("-RequireCurrentArtifacts" in stage, "deploy must require current build artifacts")
    for retired in ("Invoke-Robocopy", "/MIR", "Merge-PlayerExports",
                    "Merge-InstalledCustomPromptsIntoStaging", "Sync-PlayerExportsBackToSource"):
        check(retired not in deploy, f"retired install/data merge remains: {retired}")
    check("Get-AnimusForgeContentSourcePath" not in deploy,
          "deploy must use the validated Stage rather than direct source fallbacks")


def verify_formats_and_references() -> None:
    for target, expected in {**J15B_EXPECTED, **J15C_EXPECTED, **WORLD_BULLETIN_EXPECTED}.items():
        source = ROOT / expected["source"]
        suffix = source.suffix.lower()
        if suffix == ".json":
            json.loads(source.read_text(encoding="utf-8-sig"))
        elif suffix in {".xml", ".mbproj"}:
            ET.parse(source)
        elif suffix == ".png":
            data = source.read_bytes()
            check(data.startswith(b"\x89PNG\r\n\x1a\n") and len(data) > 24, f"invalid PNG: {target}")

    for target in (
        "ModuleData/Languages/language_data.xml",
        "ModuleData/Languages/CNs/language_data.xml",
    ):
        tree = ET.parse(ROOT / J15B_EXPECTED[target]["source"])
        for item in tree.getroot().iter("LanguageFile"):
            referenced = "ModuleData/Languages/" + item.attrib["xml_path"].replace("\\", "/")
            check(referenced in J15B_EXPECTED or referenced in INTEGRATION_EXPECTED, f"language index target not mapped: {referenced}")

    for target in ("ModuleData/project.mbproj", "ModuleData/sceneactions.mbproj"):
        tree = ET.parse(ROOT / J15B_EXPECTED[target]["source"])
        for item in tree.getroot().iter("file"):
            referenced = item.attrib["name"].replace("\\", "/")
            check(referenced in J15B_EXPECTED, f"mbproj target not mapped: {referenced}")

    submodule = ET.parse(ROOT / "AnimusForge" / "SubModule.xml")
    xml_targets = {
        f"ModuleData/{item.attrib['path']}.xml"
        for item in submodule.getroot().iter("XmlName")
    }
    check(xml_targets == {
        "ModuleData/animusforge_scene_gold_items.xml",
        "ModuleData/sceneactions_items.xml",
        "ModuleData/vengeance_items.xml",
    }, "SubModule XML targets")
    check(xml_targets <= set(J15B_EXPECTED) | set(INTEGRATION_EXPECTED), "SubModule XML targets must be mapped")

    movie_consumers = {
        "GUI/Prefabs/AnimusForgeApiOnboardingPopup.xml": "src/AF.GameAdapter.Bannerlord/UI/Onboarding/AnimusForgeApiOnboardingPopup.cs",
        "GUI/Prefabs/AnimusForgeConversationHistoryLog.xml": "src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeConversationHistoryLogPopup.cs",
        "GUI/Prefabs/AnimusForgeNativeConversationOverlay.xml": "src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeNativeConversationOverlay.cs",
        "GUI/Prefabs/AnimusForgeTerminalPopup.xml": "src/AF.GameAdapter.Bannerlord/UI/Terminal/AnimusForgeTerminalUiModels.cs",
        "GUI/Prefabs/AnimusForgeWorldEventInboxPopup.xml": "WorldEvents/WorldEventInbox.cs",
        "GUI/Prefabs/AnimusForgeWorldMessageTimelinePopup.xml": "src/AF.GameAdapter.Bannerlord/UI/WorldTimeline/WorldMessageTimelineUi.cs",
        "GUI/Prefabs/CourierLetterInputPopup.xml": "src/AF.GameAdapter.Bannerlord/UI/Courier/CourierLetterInputPopup.cs",
        "GUI/Prefabs/CourierLetterReplyPopup.xml": "src/AF.GameAdapter.Bannerlord/UI/Courier/CourierLetterReplyPopup.cs",
        "GUI/Prefabs/CustomPolicyComposePopup.xml": "PolicySystem/UI/KingdomPolicyUi.cs",
        "GUI/Prefabs/CustomPolicyHistoryPopup.xml": "PolicySystem/UI/KingdomPolicyUi.cs",
        "GUI/Prefabs/CustomPolicyResultPopup.xml": "PolicySystem/UI/KingdomPolicyUi.cs",
        "GUI/Prefabs/DevHistoryEditPopup.xml": "src/AF.GameAdapter.Bannerlord/UI/Editors/DevHistoryEditPopup.cs",
        "GUI/Prefabs/DevLargeSelectionPopup.xml": "src/AF.GameAdapter.Bannerlord/UI/Editors/DevLargeSelectionPopup.cs",
        "GUI/Prefabs/DevWeeklyReportPopup.xml": "src/AF.GameAdapter.Bannerlord/UI/Weekly/DevWeeklyReportPopup.cs",
        "GUI/Prefabs/FloatingTextLayer.xml": "src/AF.GameAdapter.Bannerlord/UI/Conversation/FloatingTextMissionView.cs",
        "GUI/Prefabs/LocalPolicyComposePopup.xml": "PolicySystem/UI/LocalPolicyUi.cs",
        "GUI/Prefabs/LocalPolicyHistoryPopup.xml": "PolicySystem/UI/LocalPolicyUi.cs",
        "GUI/Prefabs/PlayerNotorietyPopup.xml": "src/AF.GameAdapter.Bannerlord/UI/Social/PlayerNotorietyPopup.cs",
        "GUI/Prefabs/PlayerRpForgePopup.xml": "src/AF.GameAdapter.Bannerlord/UI/Economy/PlayerRpForgePopup.cs",
        "GUI/Prefabs/PolicyEffectModuleManagerPopup.xml": "PolicySystem/UI/PolicyEffectModuleManagerUi.cs",
        "GUI/Prefabs/ShoutTextInputPopup.xml": "src/AF.GameAdapter.Bannerlord/UI/Conversation/ShoutTextInputPopup.cs",
        "GUI/Prefabs/WorldDiplomacyComposePopup.xml": "src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs",
    }
    for target, consumer in movie_consumers.items():
        movie = Path(target).stem
        text = (ROOT / consumer).read_text(encoding="utf-8-sig")
        # DevWeeklyReportPopup now selects its movie via a _movieName field (default = the weekly movie).
        via_field = "LoadMovie(_movieName," in text and f'_movieName = "{movie}";' in text
        check(f'LoadMovie("{movie}"' in text or via_field, f"movie consumer drift: {movie}")

    sprite_consumers = {
        "GUI/SpriteParts/af_courier/": "src/AF.GameAdapter.Bannerlord/UI/Courier/AnimusForgeCourierUiSprites.cs",
        "GUI/SpriteParts/af_player_notoriety/": "src/AF.GameAdapter.Bannerlord/UI/Social/AnimusForgePlayerNotorietyUiSprites.cs",
        "GUI/SpriteParts/af_player_rp_forge/": "src/AF.GameAdapter.Bannerlord/UI/Economy/AnimusForgePlayerRpForgeUiSprites.cs",
        "GUI/SpriteParts/af_vassalage_notifications/": "src/modules/AF.Module.Diplomacy/Vassalage/VassalageBehavior.cs",
        "GUI/SpriteParts/af_weekly_": "src/AF.GameAdapter.Bannerlord/UI/Weekly/AnimusForgeWeeklyReportMapNotification.cs",
        "GUI/SpriteParts/af_world_diplomacy/": "src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs",
    }
    for target in (item for item in J15B_EXPECTED if item.endswith(".png")):
        matches = [consumer for prefix, consumer in sprite_consumers.items() if target.startswith(prefix)]
        check(len(matches) == 1, f"sprite owner mapping: {target}")
        text = (ROOT / matches[0]).read_text(encoding="utf-8-sig")
        check(Path(target).name in text, f"sprite consumer drift: {target}")

    bulletin_movie = (ROOT / "src/AF.GameAdapter.Bannerlord/UI/Weekly/DevWeeklyReportPopup.cs").read_text(encoding="utf-8-sig")
    check('_movieName = "WorldBulletinPanel";' in bulletin_movie, "movie consumer drift: WorldBulletinPanel")
    bulletin_sprites = (ROOT / "src/AF.GameAdapter.Bannerlord/UI/Weekly/AnimusForgeWeeklyReportMapNotification.cs").read_text(encoding="utf-8-sig")
    check('BulletinCategory = "af_world_bulletin"' in bulletin_sprites, "sprite category drift: af_world_bulletin")
    catalog = (ROOT / "src/AF.GameAdapter.Bannerlord/UI/Courier/CourierLetterThemes.cs").read_text(encoding="utf-8-sig")
    sprites = (ROOT / "src/AF.GameAdapter.Bannerlord/UI/Courier/AnimusForgeCourierUiSprites.cs").read_text(encoding="utf-8-sig")
    for target in COURIER_THEME_EXPECTED:
        stem = Path(target).stem
        if stem in ("af_courier_scroll_base", "af_courier_button_band"):
            check(f'"{stem}.png"' in sprites, f"sprite consumer drift: {target}")
        elif stem.startswith("af_courier_seal_"):
            check('"' + stem.removeprefix("af_courier_seal_") + '")' in catalog, f"seal not in theme catalog: {target}")
        else:
            check('["' + stem.removeprefix("af_courier_pattern_") + '"]' in catalog, f"pattern not in theme catalog: {target}")
    for target in (item for item in WORLD_BULLETIN_EXPECTED if item.endswith(".png")):
        check(f'("{Path(target).stem}",' in bulletin_sprites, f"sprite consumer drift: {target}")

    policy_tests = (ROOT / "tests/bridges/Policy/PolicyEffectModule.ContractTests/Program.cs").read_text(encoding="utf-8-sig")
    for file_name in ('CustomPolicyComposePopup.xml', 'LocalPolicyComposePopup.xml', 'PolicyEffectModuleManagerPopup.xml', 'CustomPolicyHistoryPopup.xml', 'LocalPolicyHistoryPopup.xml', 'AnimusForgeWorldEventInboxPopup.xml'):
        parts = J15B_EXPECTED["GUI/Prefabs/" + file_name]["source"].split("/")
        expression = "Path.Combine(repositoryRoot, " + ", ".join(json.dumps(part) for part in parts) + ")"
        check(expression in policy_tests, f"Policy test must read the mapped GUI source: {file_name}")

    brush = (ROOT / J15B_EXPECTED["GUI/Brushes/AFCourierLetterBrushes.xml"]["source"]).read_text(encoding="utf-8-sig")
    courier_prefab = (ROOT / J15B_EXPECTED["GUI/Prefabs/CourierLetterInputPopup.xml"]["source"]).read_text(encoding="utf-8-sig")
    check("AFCourierLetter." in brush and "AFCourierLetter." in courier_prefab, "courier brush linkage")
    band_owner = (ROOT / "src/AF.GameAdapter.Bannerlord/UI/Courier/AnimusForgeCourierUiSprites.cs").read_text(encoding="utf-8-sig")
    for name in ("AFCourierLetter.Band.Button", "AFCourierLetter.Band.Button.Text"):
        check(f'Name="{name}"' in brush, f"courier band brush missing: {name}")
    check('ButtonBrushName = "AFCourierLetter.Band.Button"' in band_owner, "courier band brush runtime owner drift")
    for prefab_name in ("CourierLetterInputPopup.xml", "CourierLetterReplyPopup.xml"):
        prefab = (ROOT / J15B_EXPECTED["GUI/Prefabs/" + prefab_name]["source"]).read_text(encoding="utf-8-sig")
        check(prefab.count('Brush="AFCourierLetter.Band.Button"') == 2, f"courier band buttons: {prefab_name}")
        check("Popup.Done.Button" not in prefab and "Popup.Cancel.Button" not in prefab, f"native popup buttons remain: {prefab_name}")

    generator = (ROOT / "_DeveloperPatch" / "generate_town_ambient_dialogue.ps1").read_text(encoding="utf-8-sig")
    check(J15B_EXPECTED["ModuleData/TownAmbientDialogue.json"]["source"].replace("/", "\\") in generator,
          "TownAmbient generator must write the unique source")


def verify_inventory_and_overlay() -> None:
    inventory = load_module(ROOT / "tools" / "repository_source_inventory.py", "j15_inventory")
    check(inventory.classify_path("content/content-map.json") == "content", "map inventory class")
    for expected in {**J15A_EXPECTED, **J15B_EXPECTED, **J15C_EXPECTED, **WORLD_BULLETIN_EXPECTED}.values():
        check(inventory.classify_path(expected["source"]) == "content",
              f"migrated content inventory class: {expected['source']}")
    check(inventory.classify_path(F4A_EXPECTED["AssetPackages/pack0.tpac"]["source"]) == "HOLD:asset-package-provenance",
          "TPAC projection does not clear provenance HOLD")
    check(inventory.classify_path("content/modules/AF.Module.Economy/AssetSources/animusforge_denar_coin.obj") == "design",
          "Economy AssetSources remain design-only")
    check(inventory.classify_path("content/modules/Unknown/ModuleData/file.json") is None, "unknown content owner must fail closed")
    check(inventory.classify_path("content/foundation/Unknown/GUI/file.xml") is None, "unknown foundation owner must fail closed")
    check(inventory.classify_path("content/PlayerExports/private.json") == "HOLD:user-data", "content user data hold")
    check(inventory.classify_path("content/modules/AF.Module.Knowledge/ONNX/model.json") == "HOLD:model-provenance", "content model hold")
    check(inventory.classify_path("content/modules/AF.Module.Knowledge/onnx/model.json") == "HOLD:model-provenance", "content model hold is case-insensitive")
    check(inventory.classify_path("content/modules/AF.Module.UI/AssetPackages/ui.bin") == "HOLD:asset-package-provenance", "content asset-package hold")

    overlay = load_module(ROOT / "tools" / "package_policy_system_source_overlay.py", "j15_overlay")
    files, categories = overlay.build_file_set()
    check("AnimusForge/GUI/SpriteParts/af_world_diplomacy/af_world_diplomacy_notice.png" not in files,
          "retired diplomacy notice must not enter the source overlay")
    alias_targets = {
        "CustomPrompts/Policy/CustomPolicyEvaluatorPrompt.json",
        "CustomPrompts/Policy/NpcRulerPolicyPrompt.json",
        "CustomPrompts/WorldDiplomacyPrompt.json",
        "ModuleData/PreprocessPrompts.json",
        "ModuleData/RuleBehaviorPrompts.json",
        "ModuleData/ActionPostprocessPrompts.json",
        "ModuleData/ProactiveNpcRequestPrompts.json",
        "GUI/Prefabs/CustomPolicyComposePopup.xml",
        "GUI/Prefabs/CustomPolicyHistoryPopup.xml",
        "GUI/Prefabs/CustomPolicyResultPopup.xml",
        "GUI/Prefabs/LocalPolicyComposePopup.xml",
        "GUI/Prefabs/LocalPolicyHistoryPopup.xml",
        "GUI/Prefabs/WorldDiplomacyComposePopup.xml",
        "GUI/SpriteParts/af_world_diplomacy/af_world_diplomacy_notice_v2.png",
    }
    for target in alias_targets:
        delivery = "AnimusForge/" + target
        check(delivery in files, f"overlay delivery path preserved: {delivery}")
        check(files[delivery].resolve() == (ROOT / EXPECTED[target]["source"]).resolve(),
              f"overlay source uses content map: {delivery}")
        check(categories[delivery] == "runtime_assets", f"overlay category preserved: {delivery}")


def run_command(command: list[str]) -> None:
    completed = subprocess.run(command, cwd=ROOT, text=True, encoding="utf-8", errors="replace")
    check(completed.returncode == 0, f"command failed ({completed.returncode}): {' '.join(command)}")


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--run-root", type=Path, default=DEFAULT_RUN_ROOT)
    args = parser.parse_args()
    run_root = args.run_root.resolve()
    allowed_root = (ROOT / "artifacts" / "j15-content").resolve()
    check(allowed_root in run_root.parents, "run root must remain under the authorized J15 content directory")
    check(not run_root.exists(), "run root must be new; existing fixtures are never cleared")
    run_root.mkdir(parents=True)

    verify_map_and_resources()
    verify_project_resources()
    verify_script_wiring()
    verify_formats_and_references()
    verify_inventory_and_overlay()

    # Toolchain paths default to the original dev machine; override per machine without editing the runner.
    pwsh = Path(os.environ.get("AF_J15_PWSH", r"C:\Program Files\PowerShell\7-preview\pwsh.exe"))
    run_command([
        str(pwsh), "-NoLogo", "-NoProfile", "-File",
        str(Path(__file__).with_name("ContentLayoutContractTests.ps1")),
        "-ProjectRoot", str(ROOT), "-RunRoot", str(run_root),
    ])
    run_command([
        str(pwsh), "-NoLogo", "-NoProfile", "-File",
        str(Path(__file__).with_name("PlayerExportsContractTests.ps1")),
        "-ProjectRoot", str(ROOT), "-RunRoot", str(run_root / "playerexports"),
    ])
    dotnet = Path(os.environ.get("AF_J15_DOTNET8", str(ROOT / "local" / "dotnet" / "8.0.425" / "dotnet.exe")))
    run_command([
        str(dotnet), "run", "--project", str(Path(__file__).with_name("GcczLoaderHarness.csproj")),
        "-c", "Release", "--", str(run_root / "gccz"),
    ])
    print(f"j15ContentContracts mappings={len(EXPECTED)} j15b={len(J15B_EXPECTED)} j15c={len(J15C_EXPECTED)} "
          f"holds={len(CURRENT_HOLD_PATHS)} invalidCases=8 gcczFallbackCases=4 overlayAliases=14 PASS")
    return 0


if __name__ == "__main__":
    sys.exit(main())

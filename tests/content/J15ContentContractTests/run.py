from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
from pathlib import Path
import shutil
import subprocess
import sys
import xml.etree.ElementTree as ET


ROOT = Path(__file__).resolve().parents[3]
DEFAULT_RUN_ROOT = ROOT / "artifacts" / "j15-content" / "j15-contracts"
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
    "GUI/Prefabs/AFWarStatsMapButton.xml",
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

CURRENT_HOLD_PATHS = J15B_HOLD_PATHS - J15C_EXPECTED.keys()
EXPECTED = {**J15A_EXPECTED, **J15B_EXPECTED, **J15C_EXPECTED}


def check(condition: bool, message: str) -> None:
    if not condition:
        raise AssertionError(message)


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

    for target in CURRENT_HOLD_PATHS:
        check((ROOT / "AnimusForge" / Path(target)).is_file(), f"HOLD path was moved or removed: {target}")
        check(target not in by_target, f"HOLD path entered content map: {target}")

    remaining = {
        path.relative_to(ROOT / "AnimusForge").as_posix()
        for root_name in ("ModuleData", "GUI", "CustomPrompts")
        for path in (ROOT / "AnimusForge" / root_name).rglob("*")
        if path.is_file()
    }
    check(remaining == CURRENT_HOLD_PATHS, "legacy resource roots must contain only explicit HOLD files")


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
    check(set(resources) == expected_names, "EmbeddedResource LogicalName set must remain exactly seven")
    for expected in J15A_EXPECTED.values():
        actual = resources[expected["logicalName"]]
        check(actual["include"] == expected["source"], f"include path: {expected['logicalName']}")
        check(actual["culture"] == expected["withCulture"], f"WithCulture: {expected['logicalName']}")


def verify_script_wiring() -> None:
    deploy = (ROOT / "一键编译覆盖推送" / "deploy_module.ps1").read_text(encoding="utf-8-sig")
    call = "Invoke-AnimusForgeContentProjection"
    check(deploy.count(call) == 2, "Stage and Deploy must each project content once")
    stage_start = deploy.index('if (-not [string]::IsNullOrWhiteSpace($StageOnlyOutputDir))')
    stage_end = deploy.index("$legacy13ModuleDir", stage_start)
    stage = deploy[stage_start:stage_end]
    check(stage.index("Invoke-Robocopy") < stage.index(call) < stage.index("Set-SingleModuleIdentity"),
          "Stage projection order")
    deploy_start = deploy.index("$sourceCopyArguments = @(")
    deploy_end = deploy.index("Set-SingleModuleIdentity", deploy_start)
    deploy_block = deploy[deploy_start:deploy_end]
    check(deploy_block.index("Invoke-Robocopy") < deploy_block.index(call) < deploy_block.index("Merge-InstalledCustomPromptsIntoStaging"),
          "Deploy projection must precede installed prompt merge")
    check(deploy.count("Get-AnimusForgeContentSourcePath") == 3,
          "Preprocess, Rule and Policy defaults must use content map")
    check("Merge-InstalledCustomPromptsIntoStaging -ProjectRoot $projectRootFull" in deploy,
          "Policy merge must use the resolved project root, including default invocation")
    check('Join-Path $SourceModuleDir "CustomPrompts\\Policy"' not in deploy,
          "Policy merge defaults must not use the legacy editable tree")
    check('Join-Path $sourceModuleDir "ModuleData\\RuleBehaviorPrompts.json"' not in deploy,
          "Rule source hash lookup must not use the legacy editable tree")


def verify_formats_and_references() -> None:
    for target, expected in {**J15B_EXPECTED, **J15C_EXPECTED}.items():
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
            check(referenced in J15B_EXPECTED, f"language index target not mapped: {referenced}")

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
    }, "SubModule XML targets")
    check(xml_targets <= set(J15B_EXPECTED), "SubModule XML targets must be mapped")

    movie_consumers = {
        "GUI/Prefabs/AFWarStatsMapButton.xml": "WarStats/AfWarStatsMapButtonLayer.cs",
        "GUI/Prefabs/AnimusForgeApiOnboardingPopup.xml": "AnimusForgeApiOnboardingPopup.cs",
        "GUI/Prefabs/AnimusForgeConversationHistoryLog.xml": "AnimusForgeConversationHistoryLogPopup.cs",
        "GUI/Prefabs/AnimusForgeNativeConversationOverlay.xml": "AnimusForgeNativeConversationOverlay.cs",
        "GUI/Prefabs/AnimusForgeTerminalPopup.xml": "AnimusForgeTerminalUiModels.cs",
        "GUI/Prefabs/AnimusForgeWorldEventInboxPopup.xml": "WorldEvents/WorldEventInbox.cs",
        "GUI/Prefabs/AnimusForgeWorldMessageTimelinePopup.xml": "WorldMessageTimelineUi.cs",
        "GUI/Prefabs/CourierLetterInputPopup.xml": "CourierLetterInputPopup.cs",
        "GUI/Prefabs/CourierLetterReplyPopup.xml": "CourierLetterReplyPopup.cs",
        "GUI/Prefabs/CustomPolicyComposePopup.xml": "PolicySystem/UI/KingdomPolicyUi.cs",
        "GUI/Prefabs/CustomPolicyHistoryPopup.xml": "PolicySystem/UI/KingdomPolicyUi.cs",
        "GUI/Prefabs/CustomPolicyResultPopup.xml": "PolicySystem/UI/KingdomPolicyUi.cs",
        "GUI/Prefabs/DevHistoryEditPopup.xml": "DevHistoryEditPopup.cs",
        "GUI/Prefabs/DevLargeSelectionPopup.xml": "DevLargeSelectionPopup.cs",
        "GUI/Prefabs/DevWeeklyReportPopup.xml": "DevWeeklyReportPopup.cs",
        "GUI/Prefabs/FloatingTextLayer.xml": "FloatingTextMissionView.cs",
        "GUI/Prefabs/LocalPolicyComposePopup.xml": "PolicySystem/UI/LocalPolicyUi.cs",
        "GUI/Prefabs/LocalPolicyHistoryPopup.xml": "PolicySystem/UI/LocalPolicyUi.cs",
        "GUI/Prefabs/PlayerNotorietyPopup.xml": "PlayerNotorietyPopup.cs",
        "GUI/Prefabs/PlayerRpForgePopup.xml": "PlayerRpForgePopup.cs",
        "GUI/Prefabs/PolicyEffectModuleManagerPopup.xml": "PolicySystem/UI/PolicyEffectModuleManagerUi.cs",
        "GUI/Prefabs/ShoutTextInputPopup.xml": "ShoutTextInputPopup.cs",
        "GUI/Prefabs/WorldDiplomacyComposePopup.xml": "src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs",
    }
    for target, consumer in movie_consumers.items():
        movie = Path(target).stem
        text = (ROOT / consumer).read_text(encoding="utf-8-sig")
        check(f'LoadMovie("{movie}"' in text, f"movie consumer drift: {movie}")

    sprite_consumers = {
        "GUI/SpriteParts/af_courier/": "AnimusForgeCourierUiSprites.cs",
        "GUI/SpriteParts/af_player_notoriety/": "AnimusForgePlayerNotorietyUiSprites.cs",
        "GUI/SpriteParts/af_player_rp_forge/": "AnimusForgePlayerRpForgeUiSprites.cs",
        "GUI/SpriteParts/af_vassalage_notifications/": "VassalageBehavior.cs",
        "GUI/SpriteParts/af_weekly_": "AnimusForgeWeeklyReportMapNotification.cs",
        "GUI/SpriteParts/af_world_diplomacy/": "src/modules/AF.Module.Diplomacy/World/WorldDiplomacyBehavior.cs",
    }
    for target in (item for item in J15B_EXPECTED if item.endswith(".png")):
        matches = [consumer for prefix, consumer in sprite_consumers.items() if target.startswith(prefix)]
        check(len(matches) == 1, f"sprite owner mapping: {target}")
        text = (ROOT / matches[0]).read_text(encoding="utf-8-sig")
        check(Path(target).name in text, f"sprite consumer drift: {target}")

    policy_tests = (ROOT / "tools/PolicyEffectModule.ContractTests/Program.cs").read_text(encoding="utf-8-sig")
    for file_name in ('CustomPolicyComposePopup.xml', 'LocalPolicyComposePopup.xml', 'PolicyEffectModuleManagerPopup.xml', 'CustomPolicyHistoryPopup.xml', 'LocalPolicyHistoryPopup.xml', 'AnimusForgeWorldEventInboxPopup.xml'):
        parts = J15B_EXPECTED["GUI/Prefabs/" + file_name]["source"].split("/")
        expression = "Path.Combine(repositoryRoot, " + ", ".join(json.dumps(part) for part in parts) + ")"
        check(expression in policy_tests, f"Policy test must read the mapped GUI source: {file_name}")

    brush = (ROOT / J15B_EXPECTED["GUI/Brushes/AFCourierLetterBrushes.xml"]["source"]).read_text(encoding="utf-8-sig")
    courier_prefab = (ROOT / J15B_EXPECTED["GUI/Prefabs/CourierLetterInputPopup.xml"]["source"]).read_text(encoding="utf-8-sig")
    check("AFCourierLetter." in brush and "AFCourierLetter." in courier_prefab, "courier brush linkage")

    generator = (ROOT / "_DeveloperPatch" / "generate_town_ambient_dialogue.ps1").read_text(encoding="utf-8-sig")
    check(J15B_EXPECTED["ModuleData/TownAmbientDialogue.json"]["source"].replace("/", "\\") in generator,
          "TownAmbient generator must write the unique source")


def verify_inventory_and_overlay() -> None:
    inventory = load_module(ROOT / "tools" / "repository_source_inventory.py", "j15_inventory")
    check(inventory.classify_path("content/content-map.json") == "content", "map inventory class")
    for expected in EXPECTED.values():
        check(inventory.classify_path(expected["source"]) == "content",
              f"migrated content inventory class: {expected['source']}")
    check(inventory.classify_path("content/modules/Unknown/ModuleData/file.json") is None, "unknown content owner must fail closed")
    check(inventory.classify_path("content/foundation/Unknown/GUI/file.xml") is None, "unknown foundation owner must fail closed")
    check(inventory.classify_path("content/PlayerExports/private.json") == "HOLD:user-data", "content user data hold")
    check(inventory.classify_path("content/modules/AF.Module.Knowledge/ONNX/model.json") == "HOLD:model-provenance", "content model hold")
    check(inventory.classify_path("content/modules/AF.Module.Knowledge/onnx/model.json") == "HOLD:model-provenance", "content model hold is case-insensitive")
    check(inventory.classify_path("content/modules/AF.Module.UI/AssetPackages/ui.bin") == "HOLD:asset-package-provenance", "content asset-package hold")

    overlay = load_module(ROOT / "tools" / "package_policy_system_source_overlay.py", "j15_overlay")
    files, categories = overlay.build_file_set()
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
    check(run_root == DEFAULT_RUN_ROOT.resolve(), "run root must remain the authorized J15 content directory")
    if run_root.exists():
        shutil.rmtree(run_root)
    run_root.mkdir(parents=True)

    verify_map_and_resources()
    verify_project_resources()
    verify_script_wiring()
    verify_formats_and_references()
    verify_inventory_and_overlay()

    pwsh = Path(r"C:\Program Files\PowerShell\7-preview\pwsh.exe")
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
    dotnet = ROOT / "local" / "dotnet" / "8.0.425" / "dotnet.exe"
    run_command([
        str(dotnet), "run", "--project", str(Path(__file__).with_name("GcczLoaderHarness.csproj")),
        "-c", "Release", "--", str(run_root / "gccz"),
    ])
    print(f"j15ContentContracts mappings={len(EXPECTED)} j15b={len(J15B_EXPECTED)} j15c={len(J15C_EXPECTED)} "
          f"holds={len(CURRENT_HOLD_PATHS)} invalidCases=8 gcczFallbackCases=4 overlayAliases=14 PASS")
    return 0


if __name__ == "__main__":
    sys.exit(main())

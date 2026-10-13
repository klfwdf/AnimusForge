"""Production lifecycle/readers + verbatim release normalizer; game/provider are fixtures."""
import json
import os
from pathlib import Path
import re
import subprocess
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
OUT = ROOT / "artifacts/player-captivity-opening-20261013"
OUT.mkdir(parents=True, exist_ok=True)
post = (ROOT / "src/modules/AF.Module.Conversation/Internal/Postprocess/ConversationActionPostprocessOwner.cs").read_text(encoding="utf-8-sig")
start = post.index("internal static string NormalizeEncounterReleasePostprocessTagsForScene(")
end = post.index("internal static string NormalizeHeroJoinPartyPostprocessTagsForScene(", start)
probe = OUT / "PostprocessProbe.cs"
composer = (ROOT / "src/modules/AF.Module.Prompt/Composition/ScenePromptMessageProjectionComposer.cs").read_text(encoding="utf-8-sig")
opening_methods = []
for first, following in (("internal static string BuildNpcInitiatedOpeningPersistentFactText(", "internal static string BuildRecentSceneLoreQueryText("),
                         ("internal static string BuildNpcInitiatedOpeningUserText(", "internal static string BuildNativeConversationStreamingVisibleText("),
                         ("internal static string StripAfefPrefixForPromptSection(", "internal static string BuildScopedAfefFactLinesForPrompt(")):
    opening_methods.append(composer[composer.index(first):composer.index(following, composer.index(first))])
probe.write_text("using System; using System.Collections.Generic; using System.Linq; using System.Text.RegularExpressions;\nnamespace AnimusForge; internal static class PostprocessProbe {\n" + post[start:end] + "\n" + "\n".join(opening_methods) + "\n}\n", encoding="utf-8")

# Exact production wiring: offered rules must reach both RAW requests and FINAL closures.
assert post.count("List<PostprocessRuleEntry> captivityReleaseRules = PlayerCaptivityConversation.BuildPostprocessRules(") == 2
assert post.count("NormalizeEncounterReleasePostprocessTagsForScene(content, captivityReleaseRules)") == 2
assert "text14, captivityReleaseTags, text15" in post
assert "meetingReleaseTags, captivityReleaseTags, vanillaIssueTags" in post
assert post.count("meetingReleaseRules, captivityReleaseRules, vanillaIssueRules") == 2
source = (ROOT / "src/AF.GameAdapter.Bannerlord/Encounter/PlayerCaptivityConversation.cs").read_text(encoding="utf-8-sig")
assert "EndCaptivityAction.ApplyByReleasedByChoice(Hero.MainHero);" in source
assert "RegisterAction" not in source and "SyncData" not in source
host = (ROOT / "src/AF.GameAdapter.Bannerlord/Encounter/LordEncounterBehavior.cs").read_text(encoding="utf-8-sig")
assert "CampaignEvents.HeroPrisonerTaken.AddNonSerializedListener(this, PlayerCaptivityConversation.OnPlayerTakenPrisoner);" in host
assert "PlayerCaptivityConversation.Tick();" in host
overlay = (ROOT / "src/AF.GameAdapter.Bannerlord/UI/Conversation/AnimusForgeNativeConversationOverlay.cs").read_text(encoding="utf-8-sig")
assert "NpcInitiatedOpeningRouter.HasPendingNativeOpeningForCurrentConversation()" in overlay
assert "SubmitNpcInitiatedOpeningAsync();" in overlay
admission = (ROOT / "src/modules/AF.Module.Conversation/Channels/Native/NativeAdmissionApplicationAdapter.cs").read_text(encoding="utf-8-sig")
assert "NpcInitiatedOpeningRouter.TryConsumePendingNativeOpening(admission.Hero," in admission
assert "new NativeOpeningRetry(admission)" in admission
turn = (ROOT / "src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurn.cs").read_text(encoding="utf-8-sig")
assert "shouldRecordPlayerInput = !npcInitiatedOpening;" in turn
assert "BuildNpcInitiatedOpeningUserText(npcOpeningExtraFact, npcOpeningPromptText)" in turn
assert "BuildNpcInitiatedOpeningPersistentFactText(npcOpeningExtraFact)" in turn
assert "EnsureNativeConversationPersonaReadyAsync" in turn
prompt = (ROOT / "src/modules/AF.Module.Conversation/Channels/Native/ShoutBehavior.NativeTurnPrompt.cs").read_text(encoding="utf-8-sig")
assert "npcInitiatedOpening ? npcOpeningUserText" in prompt
for edition in ("", ".en"):
    facts = json.loads((ROOT / f"content/modules/AF.Module.Encounter/ModuleData/EncounterPromptTexts{edition}.json").read_text(encoding="utf-8-sig"))
    opening = facts["PlayerCaptivityOpeningPrompt"]
    assert opening and not any(x in opening.lower() for x in ("ransom", "trade", "price", "赎金", "交易", "报价", "[action:"))
assert json.loads((ROOT / "content/modules/AF.Module.Encounter/ModuleData/EncounterPromptTexts.json").read_text(encoding="utf-8-sig")) == json.loads((ROOT / "content/modules/AF.Module.Encounter/ModuleData/EncounterPromptTexts.zh-CN.json").read_text(encoding="utf-8-sig"))
assert "PlayerCaptivityConversation.RuleId" in (ROOT / "src/AF.GameAdapter.Bannerlord/Prompt/SceneMechanismPromptCaptureAdapter.cs").read_text(encoding="utf-8-sig")
assert '"player_captivity_release"' in (ROOT / "src/modules/AF.Module.Conversation/Channels/Courier/CourierDeliveryBehavior.cs").read_text(encoding="utf-8-sig")
assert '"ACTION:RELEASE_PLAYER_CAPTIVITY"' in (ROOT / "src/modules/AF.Module.Actions/Tags/LegacyActionTagCatalog.cs").read_text(encoding="utf-8-sig")
effects = (ROOT / "src/modules/AF.Module.Conversation/Channels/Native/NativeConversationGameEffectsRuntime.cs").read_text(encoding="utf-8-sig")
assert "PlayerCaptivityConversation.ConsumeReleaseTag(targetHero, ref content);" in effects

rule_pairs = []
for edition in ("", ".en"):
    path = ROOT / f"content/modules/AF.Module.Prompt/ModuleData/RuleBehaviorPrompts{edition}.json"
    rules = json.loads(path.read_text(encoding="utf-8-sig"))["RulePrompts"]
    matches = [r for r in rules if r.get("TopicNumber") == 35]
    assert len(matches) == 1 and matches[0]["Id"] == "player_captivity_release"
    rule = matches[0]
    assert "[ACTION:" not in rule["Instruction"]
    assert rule["PostprocessRules"][0]["Tag"] == "[ACTION:RELEASE_PLAYER_CAPTIVITY]"
    rule_pairs.append(rule)
assert set(rule_pairs[0]) == set(rule_pairs[1])
mapping = json.loads((ROOT / "content/content-map.json").read_text(encoding="utf-8-sig"))["entries"]
for target in ("EncounterPromptTexts.json", "RuleBehaviorPrompts.json", "PromptComposerTexts.json"):
    entry = next(e for e in mapping if e.get("target") == "ModuleData/" + target)
    assert (ROOT / entry["source"]).is_file()
    english = Path(entry["source"].replace(".zh-CN.json", ".json")).with_suffix(".en.json")
    assert (ROOT / english).is_file(), english
assert 'AnimusForge.Prompt.Compatibility.json' in (ROOT / "AnimusForge.csproj").read_text(encoding="utf-8-sig")

ui = ROOT / "content/modules/AF.Module.UI/ModuleData/Languages"
en = {n.get("id"): n.get("text") for n in ET.parse(ui / "af_ui_strings.xml").iter("string")}
cn = {n.get("id"): n.get("text") for n in ET.parse(ui / "CNs/af_ui_strings-zh-CN.xml").iter("string")}
keys = {key for key in en if key.startswith("AF_ENC_UI_Captivity")}
assert len(keys) == 15 and keys.issubset(cn)
language_root = ROOT / "content/foundation/AF.Foundation.Localization/ModuleData/Languages"
for registration, filename in (("language_data.xml", "af_ui_strings.xml"), ("CNs/language_data.xml", "CNs/af_ui_strings-zh-CN.xml")):
    assert filename in {node.get("xml_path") for node in ET.parse(language_root / registration).iter("LanguageFile")}
for key in keys:
    assert en[key] and cn[key]
    assert re.findall(r"\{[A-Z_]+\}", en[key]) == re.findall(r"\{[A-Z_]+\}", cn[key])
for code, log_name in (("CN", "captivity-cn.log"), ("EN", "captivity-en.log")):
    env = dict(os.environ, AF_CAPTIVITY_TEST_ROOT=str(ROOT), AF_CAPTIVITY_TEST_EDITION=code)
    proc = subprocess.run(["dotnet", "run", "--project", str(HERE), "-p:CaptivityContractSource=" + str(probe)], cwd=ROOT, env=env, capture_output=True)
    (OUT / log_name).write_bytes(proc.stdout + proc.stderr)
    print((proc.stdout + proc.stderr).decode("utf-8", errors="replace"))
    if proc.returncode:
        raise SystemExit(proc.returncode)
print("PASS CN/EN topic 35, UI 15 keys, paired pipeline wiring and actual readers. LLM semantic/live-game acceptance NOT RUN.")

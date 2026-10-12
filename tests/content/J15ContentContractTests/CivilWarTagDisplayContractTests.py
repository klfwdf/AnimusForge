"""Static cross-module fact/display contract. Does not compile or run a campaign."""
from pathlib import Path
import re
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[3]
def read(path):
    return (ROOT / path).read_text(encoding="utf-8-sig")

owner = read("src/modules/AF.Module.Kingdom/CivilWar/KingdomCivilWarOwner.cs")
adapter = read("src/modules/AF.Module.Kingdom/CivilWar/CivilWarModuleAdapter.cs")
port = read("src/AF.Contracts/Internal/TeamModules/ICivilWarModulePort.cs")
economy = read("src/modules/AF.Module.Economy/Host/RewardSystemBehavior.cs")
ui = read("src/modules/AF.Module.Kingdom/CivilWar/CivilWarUiText.cs")
checks = 0
def check(condition, message):
    global checks
    checks += 1
    if not condition:
        raise AssertionError(message)

check("bool TryApplyTag(Hero speaker, string tag, out string message, out string displayMessage);" in port,
      "same-DLL port carries both channels")
check("=> TryApplyTag(speaker, tag, out message, out _);" in adapter, "old callers retain fact return")
for action, method in (("JOIN:CROWN", "TryJoinPlayer"), ("JOIN:OPPOSITION", "TryJoinPlayer"),
                       ("RECRUIT", "TryRecruitClan"), ("LEAVE:SELF", "TryLeaveClan"),
                       ("LEAVE:PLAYER", "TryLeaveClan"), ("DETONATE", "TryDetonate")):
    line = next(line for line in adapter.splitlines() if 'action.' in line and '"'+action+'"' in line)
    check(line.count("_owner." + method + "(") == 1 and "out message, out displayMessage" in line,
          action + " executes once and forwards both channels")
check("speaker != Hero.MainHero && speaker.Clan?.Leader == speaker &&" in adapter,
      "NPC self-leave authorization retained")
for method in ("TryJoinPlayer", "TryLeaveClan", "TryDetonate"):
    start = owner.index("\tinternal bool " + method + "(")
    end = owner.index("\n\tinternal bool ", owner.index("out string displayMessage)", start))
    section = owner[start:end]
    check(section.count("= Execute(") == 1, method + " has one mutation")
    check("message = result.Message;" in section and "displayMessage = CivilWarUiText.ResultNotice(result);" in section,
          method + " preserves raw result and renders structured projection")
start = owner.index("\tinternal bool TryRecruitClan(")
end = owner.index("\tinternal bool TryLeaveClan(", start)
recruit = owner[start:end]
check("displayMessage = display;" in recruit and 'message, displayText: display);' in recruit,
      "recruitment reuses its existing chronicle projection")
start = economy.index('string civilWarStatus = "";')
end = economy.index("}, RegexOptions.IgnoreCase)", start)
consumer = economy[start:end]
check(consumer.count(".TryApplyTag(") == 1, "HUD does not re-execute action to translate it")
check("giverFacts.Add(civilWarStatus);" in consumer and "receiverFacts.Add(civilWarStatus);" in consumer,
      "both fact channels use raw outcome")
check("out civilWarStatus, out string civilWarDisplay" in consumer and
      "CivilWarFaction(string.IsNullOrEmpty(civilWarDisplay) ? civilWarStatus : civilWarDisplay)" in consumer,
      "HUD uses display projection with truthful fallback")
check('if (string.IsNullOrEmpty(key)) return raw;' in ui and 'if (fallback == null) return raw;' in ui,
      "unrecognized or old receipt is not replaced with an invented result")
en = {}; zh = {}
for base in ("AF.Module.Kingdom", "AF.Module.Diplomacy"):
    for path in (ROOT / "content/modules" / base / "ModuleData/Languages").glob("**/*.xml"):
        target = zh if "CNs" in path.parts else en
        for node in ET.parse(path).getroot().iter("string"):
            target[node.get("id")] = node.get("text", "")
for suffix in ("AlreadyInCamp", "Joined", "LeftCamp", "RisingAccepted"):
    key = "AF_KINGDOM_CW_Notice_" + suffix
    check(key in en and key in zh and
          set(re.findall(r"\{\w+\}", en[key])) == set(re.findall(r"\{\w+\}", zh[key])),
          key + " has both languages with matching arguments")
for key in ("AF_KINGDOM_CW_Chronicle_RecruitedCrown", "AF_KINGDOM_CW_Chronicle_RecruitedFaction"):
    check(key in en and key in zh, key + " is registered on both sides")
print(f"PASS civil-war tag fact/display static contract: {checks} checks; runtime/compiled replay NOT RUN")

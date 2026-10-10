#!/usr/bin/env python3
"""Static projection/source contracts; deliberately does not compile or run the game."""
import json
import re
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
BASELINE = "c8b6c652ebeaf43e0f503298ec3a76f9b60505f1"
DIRECTORY = ROOT / "src/AF.GameAdapter.Bannerlord/Composition/SiegeAftermath"
HELPER = (DIRECTORY / "CastleAftermathUiText.cs").read_text(encoding="utf-8-sig")


def main():
    en = {}
    for key, fallback in re.findall(r'Text\("(AF_CASTLE_UI_[A-Z_]+)", "([^"\r\n]*)"', HELPER):
        assert key not in en or en[key] == fallback, f"Divergent fallback: {key}"
        assert not re.search(r"[\u3400-\u4dbf\u4e00-\u9fff]", fallback), key
        en[key] = fallback
    zh = {}
    for name in ("2026-10-10-castle-aftermath-ui-keys.json", "2026-10-10-castle-aftermath-dynamic-ui-keys.json"):
        table = json.loads((ROOT / "docs/handoffs" / name).read_text(encoding="utf-8-sig"))
        if "strings" in table:
            rows = table["strings"]
            assert len(rows) == 48, "Expected exactly 48 new dynamic keys"
            current = {row["id"]: row["zh"] for row in rows}
            assert len(current) == len(rows), "Duplicate dynamic key"
            for row in rows:
                assert set(row) == {"id", "en", "zh"}, "Incomplete dynamic key row"
                assert en.get(row["id"]) == row["en"], f"Delivery fallback differs from actual helper: {row['id']}"
        else:
            current = table["chinese"]
            assert len(current) == 23, "Expected exactly 23 original keys"
        assert not set(zh) & set(current), "Repeated key across handoff tables"
        zh.update(current)
    assert en.keys() == zh.keys(), f"Key mismatch: {set(en) ^ set(zh)}"
    for key in en:
        assert sorted(re.findall(r"\{[A-Z]+\}", en[key])) == sorted(re.findall(r"\{[A-Z]+\}", zh[key])), key
    for filename, enum in (("SiegeCastleRegularDispositionStagingProfile.cs", "SiegeCastleActionKind"), ("SiegeCastleLordRecruitmentBranchProfile.cs", "SiegeCastleLordRecruitmentBranch")):
        profile = (ROOT / "AnimusForge.SiegeAftermathIntervention" / filename).read_text(encoding="utf-8-sig")
        expected = set(re.findall(enum + r"\.([A-Za-z]+)\s*=>", profile))
        actual = set(re.findall(enum + r"\.([A-Za-z]+)\s*=>", HELPER))
        assert expected <= actual, f"Missing enum projection: {expected - actual}"
    original = (ROOT / "AnimusForge.SiegeAftermathIntervention/SiegeCastleActionOutcomeTextProfile.cs").read_text(encoding="utf-8-sig")
    sale_codes = set(re.findall(r'"([a-z_]+)"\s*=>\s*"', original))
    assert sale_codes <= set(re.findall(r'"([a-z_]+)"\s*=>\s*Text', HELPER)), "Missing sale reason"
    disposition = (ROOT / "AnimusForge.SiegeAftermathIntervention/SiegeCastlePrisonerDispositionProfile.cs").read_text(encoding="utf-8-sig")
    required = set(re.findall(r"case ([A-Za-z]+Reason):", disposition.split("public static string DescribeDeferredFailureReason", 1)[1].split("public static int ResolveTransferred", 1)[0]))
    assert required <= set(re.findall(r"case SiegeCastlePrisonerDispositionProfile\.([A-Za-z]+Reason):", HELPER)), "Missing deferred reason"
    assert "RemovedForReasonPrefix, StringComparison.OrdinalIgnoreCase" in HELPER
    assert "ExceptionReasonPrefix, StringComparison.OrdinalIgnoreCase" in HELPER
    assert "EconomyUiTexts.FactionIdentityStatusForDisplay(status)" in HELPER
    assert "SocialUiLocalization.RecruitmentStatusForDisplay(status)" in HELPER
    assert "catch (RegexMatchTimeoutException) { return status; }" in HELPER
    bridge = (DIRECTORY / "CastleAftermathLordRecruitmentRuntimeBridge.cs").read_text(encoding="utf-8-sig")
    for literal in re.findall(r'case "([^"]+)": return Text', HELPER):
        assert '"' + literal + '"' in bridge, f"Status lacks producer evidence: {literal}"
    notice = re.compile(r"InformationManager\.DisplayMessage\(new InformationMessage\(.*?Color\.FromUint\([^;]*?\)\)\);", re.S)
    calls = 0
    for filename in ("CastleAftermathLordRecruitmentRuntimeBridge.cs", "SiegeAiInterventionBehavior.cs"):
        path = DIRECTORY / filename
        current = path.read_text(encoding="utf-8-sig").strip()
        baseline = subprocess.check_output(["git", "show", BASELINE + ":" + path.relative_to(ROOT).as_posix()], cwd=ROOT).decode("utf-8-sig").replace("\r\n", "\n").strip()
        assert notice.sub("<notice>", current) == notice.sub("<notice>", baseline), f"Changed source outside notification: {filename}"
        direct = current.count("CastleAftermathUiText.")
        assert direct == sum(block.count("CastleAftermathUiText.") for block in notice.findall(current)), filename
        calls += direct
    assert not re.search(r"\b(SyncData|Logger|Task|OnTick)\b", HELPER)
    print(f"PASS castle projection keys={len(en)} new={len(en)-23}; notification consumers={calls}; enum/reason/status producer coverage; unchanged facts/logs/persistence outside notices")


if __name__ == "__main__":
    main()

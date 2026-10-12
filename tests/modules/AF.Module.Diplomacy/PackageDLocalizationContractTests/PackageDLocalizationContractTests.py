#!/usr/bin/env python3
"""Package D (diplomacy + civil war) localization contract.

Static, offline checks only: this reads source and resources as text. It does not
compile the mod, load Bannerlord, or establish in-game acceptance. Compile-time and
runtime behavior still require the mainline build and a live-game pass.

What this gate protects
-----------------------
1. The two English/zh-CN resource pairs stay key-identical, duplicate-free and
   placeholder-identical, and the English resource never contains Chinese.
2. Every localization key the source names literally is present in both languages.
3. Every *dynamically built* key (``"AF_KINGDOM_CW_Failure_" + code`` and friends)
   is present in both languages for every code the codebase can actually produce.
   This is the check that catches a failure/notice code added in C# without a
   matching resource entry -- the case that would otherwise fall back to raw text.
4. Each English fallback literal in C# equals the English resource value once the
   C# escapes are decoded, so the no-resource path and the localized path show the
   same sentence. Decoding is required because the resource spells a newline as
   ``&#10;`` (a real newline after XML parsing) while the source spells it ``\n``.
5. An unknown code resolves to the caller's original text rather than to a guessed
   English sentence. Hiding Chinese behind a generic string is the failure mode this
   asserts against.
"""

from __future__ import annotations

import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path


ROOT = Path(__file__).resolve().parents[4]

DIP_DIR = ROOT / "content/modules/AF.Module.Diplomacy/ModuleData/Languages"
KIN_DIR = ROOT / "content/modules/AF.Module.Kingdom/ModuleData/Languages"
DIP_EN = DIP_DIR / "afdip_ui_strings.xml"
DIP_CN = DIP_DIR / "CNs/afdip_ui_strings-zh-CN.xml"
KIN_EN = KIN_DIR / "afkingdom_ui_strings.xml"
KIN_CN = KIN_DIR / "CNs/afkingdom_ui_strings-zh-CN.xml"

DIP_SOURCE_DIRS = (
    ROOT / "src/modules/AF.Module.Diplomacy",
    ROOT / "src/AF.GameAdapter.Bannerlord/UI/Kingdom",
)
KIN_SOURCE_DIRS = (
    ROOT / "src/modules/AF.Module.Kingdom",
    ROOT / "src/AF.GameAdapter.Bannerlord/UI/Kingdom",
)

CW_UI_TEXT = ROOT / "src/modules/AF.Module.Kingdom/CivilWar/CivilWarUiText.cs"
CW_SOURCE_DIR = ROOT / "src/modules/AF.Module.Kingdom/CivilWar"

HAN = re.compile(r"[\u3400-\u4dbf\u4e00-\u9fff]")
VARIABLE = re.compile(r"\{([A-Za-z][A-Za-z0-9_]*)\}")
VALID_ID = re.compile(r"^[A-Za-z0-9_]+$")
# Any literal key named in source. A trailing underscore marks a built prefix,
# not a key, and is filtered out by the caller.
NAMED_KEY = re.compile(r'"(AF_DIP_UI_[A-Za-z0-9_]+|AF_KINGDOM_CW_[A-Za-z0-9_]+)"')
# A localization helper call with a literal id and a literal English fallback.
# Whitespace is not flattened: the call punctuation may sit on another line, but
# the fallback literal's own interior spacing is significant and must survive.
HELPER_CALL = re.compile(
    r'(?:DiplomacyUiLocalization\.(?:Text|Format|Object)|CivilWarUiText\.T)'
    r'\(\s*"([A-Za-z0-9_]+)"\s*,\s*"((?:\\.|[^"\\])*)"'
)
# ``"Code" => "fallback"`` rows of a C# switch-expression lookup table.
SWITCH_ROW = re.compile(r'"([A-Za-z0-9_]+)"\s*=>\s*"((?:\\.|[^"\\])*)"')

# C# string escapes, and the character each one produces at runtime. The resource
# side stores a newline as the entity ``&#10;``, which XML decodes to a real
# newline, so the source side must be decoded the same way before comparing.
CS_ESCAPE = re.compile(r"\\(.)")
CS_UNESCAPE = {"n": "\n", "r": "\r", "t": "\t", '"': '"', "\\": "\\", "0": "\0"}


def unescape_cs(value: str) -> str:
    """Decode the escapes of a C# string literal body into its runtime characters.

    An unrecognized escape (``\\u1234``) is left exactly as written rather than
    guessed at, so a construct this scan does not model cannot silently pass as a
    match.
    """
    return CS_ESCAPE.sub(lambda match: CS_UNESCAPE.get(match.group(1), match.group(0)), value)


# English fallback literals that disagree with the English resource, inherited from
# before this contract existed. Every entry was verified byte-identical at the
# baseline revision (git show HEAD:<path>) and is named by no resource this contract
# added, so none of them is a Package D regression; they are pre-existing drift in
# files this package does not own, and correcting another author's English wording
# is not this package's call. Three shapes:
#   * trailing padding  - the resource pads a label so the value after it is
#     separated (``"Change:  "``); the C# literal pads with one space.
#   * curly apostrophe  - the resource uses U+2019, the C# literal ASCII ``'``.
#   * stale wording     - one duplicated call site kept older phrasing than its
#     canonical sibling (KingdomStrategicProfileBehavior.cs:777), which does match.
# Removing an entry is safe once the literal is aligned; adding one requires the same
# evidence, so a new divergence fails the gate instead of joining this list.
KNOWN_FALLBACK_DRIFT = {
    "AF_DIP_UI_KingdomProfile_GenerationConfigError": "stale wording, older than its canonical sibling",
    "AF_DIP_UI_Vassalage_Terminal_CannotViewLedger": "curly apostrophe",
    "AF_DIP_UI_Vassalage_Terminal_IndependenceLabel": "trailing padding",
    "AF_DIP_UI_Vassalage_Terminal_NoSettlementGain": "curly apostrophe",
    "AF_DIP_UI_Vassalage_Terminal_NotRuler": "curly apostrophe",
    "AF_DIP_UI_Vassalage_Terminal_NotRulerShort": "curly apostrophe",
    "AF_DIP_UI_WorldDiplomacy_ActiveEventParticipants": "trailing padding",
    "AF_DIP_UI_WorldDiplomacy_ActiveEventPrefix": "trailing padding",
    "AF_DIP_UI_WorldDiplomacy_ImpactDeltaLabel": "trailing padding",
    "AF_DIP_UI_WorldDiplomacy_ImpactReasonLabel": "trailing padding",
    "AF_DIP_UI_WorldDiplomacy_IndexMetaDeclaration": "trailing padding",
    "AF_DIP_UI_WorldDiplomacy_StandingPrestigeLabel": "trailing padding",
    "AF_DIP_UI_WorldDiplomacy_StandingReputationLabel": "trailing padding",
}


def read_strings(path: Path) -> dict[str, str]:
    """Return the id -> text map of one Bannerlord language document."""
    root = ET.parse(path).getroot()
    assert root.tag == "base" and root.attrib.get("type") == "string", f"not a language document: {path}"
    result: dict[str, str] = {}
    for item in root.findall("strings/string"):
        key = item.attrib.get("id", "")
        assert VALID_ID.match(key), f"invalid localization id: {path}:{key!r}"
        assert key not in result, f"duplicate localization id: {path}:{key}"
        result[key] = item.attrib.get("text", "")
    assert result, f"no strings: {path}"
    return result


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig")


def flatten(text: str) -> str:
    """Collapse whitespace so multi-line helper calls match as single lines."""
    return re.sub(r"\s+", " ", text)


def source_files(directories) -> list[Path]:
    found: list[Path] = []
    for directory in directories:
        found.extend(sorted(directory.rglob("*.cs")))
    assert found, f"no C# sources under {directories}"
    return found


def switch_table(text: str, declaration: str) -> dict[str, str]:
    """Read one ``"Code" => "fallback"`` lookup table out of a C# source file.

    Expects raw text: the fallback values are compared against the resource, and
    some of them carry significant runs of spaces (alignment padding), which a
    whitespace-collapsing pass would destroy.
    """
    start = text.find(declaration)
    assert start >= 0, f"lookup table declaration not found: {declaration!r}"
    body = text[start:]
    terminator = body.find("_ =>")
    assert terminator >= 0, f"lookup table has no default arm: {declaration!r}"
    rows = dict(SWITCH_ROW.findall(body[:terminator]))
    assert rows, f"lookup table is empty: {declaration!r}"
    return rows


def check_pair(name: str, en: dict[str, str], zh: dict[str, str]) -> None:
    assert en.keys() == zh.keys(), (
        f"{name}: key mismatch; EN-only={sorted(en.keys() - zh.keys())} "
        f"ZH-only={sorted(zh.keys() - en.keys())}"
    )
    for key in en:
        assert not HAN.search(en[key]), f"{name}: Chinese in English resource: {key}"
        assert zh[key].strip(), f"{name}: empty translation: {key}"
        assert sorted(VARIABLE.findall(en[key])) == sorted(VARIABLE.findall(zh[key])), (
            f"{name}: placeholder mismatch: {key}"
        )
        for value in (en[key], zh[key]):
            assert "\\n" not in value, f"{name}: literal newline escape in {key}; use &#10;"


def check_named_keys(name: str, pairs, directories) -> int:
    """Every key the source writes out in full must exist in both languages.

    A key is resolved by its own prefix, not by which directory the call site
    lives in: ``AF_KINGDOM_CW_*`` strings are split across the diplomacy and
    kingdom pairs, and one shared directory (``UI/Kingdom``) feeds both. Looking
    the key up in the wrong pair would report a defined key as missing.
    """
    checked = 0
    missing: dict[str, set[str]] = {}
    for path in source_files(directories):
        for key in NAMED_KEY.findall(read_text(path)):
            if key.endswith("_"):
                continue  # a concatenation prefix, validated by the dynamic checks
            checked += 1
            if not any(key in en and key in zh for en, zh in pairs):
                missing.setdefault(key, set()).add(path.name)
    assert not missing, (
        f"{name}: {len(missing)} key(s) named in source are missing from every resource "
        "pair; a missing key silently falls back to the English literal, so a Chinese "
        "player reads English:\n"
        + "\n".join(f"  {key}  {sorted(paths)}" for key, paths in sorted(missing.items()))
    )
    assert checked, f"{name}: no literal keys found; the scan is not seeing the sources"
    return checked


def check_english_fallbacks(name: str, pairs, directories) -> int:
    """A C# fallback literal must equal the English resource it stands in for.

    Runs on raw text: HELPER_CALL already tolerates whitespace around the call
    punctuation, so a call wrapped over several lines still matches, while the
    literal's own interior spacing survives for the comparison.

    The check is strict: a new divergence fails the gate. The divergences below are
    exceptions carried over from before this contract existed, each verified to be
    byte-identical at the baseline revision and absent from every resource Package D
    added. They are listed rather than asserted so the gate stays meaningful for new
    work while the inherited state is recorded instead of hidden.
    """
    checked = 0
    known: set[str] = set()
    failures: list[str] = []
    for path in source_files(directories):
        for key, raw_fallback in HELPER_CALL.findall(read_text(path)):
            fallback = unescape_cs(raw_fallback)
            found = [en[key] for en, _zh in pairs if key in en]
            if not found:
                continue  # reported by check_named_keys / the dynamic checks
            checked += 1
            if any(value == fallback for value in found):
                continue
            if key in KNOWN_FALLBACK_DRIFT:
                known.add(key)
                continue
            failures.append(
                f"  {key}  ({path.name})\n"
                f"    C#:       {fallback!r}\n    resource: {found!r}"
            )
    assert not failures, (
        f"{name}: {len(failures)} English fallback literal(s) differ from the resource. "
        "The no-resource path would then show a different sentence than the localized "
        "path. Fix the literal, or -- only for a divergence that predates this "
        "contract -- add the key to KNOWN_FALLBACK_DRIFT with its evidence:\n"
        + "\n".join(failures)
    )
    assert checked, f"{name}: no helper calls with literal fallbacks found"
    if known:
        print(f"NOTE {name}: {len(known)} key(s) keep inherited fallback drift, "
              f"unchanged at the baseline revision: {sorted(known)}")
    return checked


def check_unknown_code_is_honest(text: str) -> None:
    """An unresolvable code must return the caller's own text, not a guessed string."""
    expectations = (
        ("FailureText", "if (fallback == null) return null;"),
        ("ResultNotice", "if (fallback == null) return raw;"),
        ("NoteDisplay", "fallback == null ? raw :"),
    )
    for label, needle in expectations:
        assert needle in text, (
            f"{label} no longer falls back to the original text when a code is unknown; "
            f"expected {needle!r}"
        )


def main() -> int:
    dip_en, dip_zh = read_strings(DIP_EN), read_strings(DIP_CN)
    kin_en, kin_zh = read_strings(KIN_EN), read_strings(KIN_CN)
    check_pair("diplomacy", dip_en, dip_zh)
    check_pair("kingdom", kin_en, kin_zh)

    pairs = ((dip_en, dip_zh), (kin_en, kin_zh))
    dip_named = check_named_keys("diplomacy", pairs, DIP_SOURCE_DIRS)
    kin_named = check_named_keys("kingdom", pairs, KIN_SOURCE_DIRS)
    dip_fallback = check_english_fallbacks("diplomacy", pairs, DIP_SOURCE_DIRS)
    kin_fallback = check_english_fallbacks("kingdom", pairs, KIN_SOURCE_DIRS)

    # The dynamic families are the ones a naive literal scan cannot see. Their code
    # sets come from two places: the lookup tables that hold the English fallbacks,
    # and the call sites that actually produce a code.
    cw_ui_raw = read_text(CW_UI_TEXT)
    check_unknown_code_is_honest(flatten(cw_ui_raw))
    tables = {
        "AF_KINGDOM_CW_Notice_": switch_table(cw_ui_raw, "string NoticeFallback("),
        "AF_KINGDOM_CW_Failure_": switch_table(cw_ui_raw, "string FailureFallback("),
        "AF_KINGDOM_CW_Note_": switch_table(cw_ui_raw, "string NoteFallback("),
    }

    produced: dict[str, set[str]] = {prefix: set() for prefix in tables}
    for path in sorted(CW_SOURCE_DIR.glob("*.cs")):
        text = flatten(read_text(path))
        produced["AF_KINGDOM_CW_Failure_"] |= set(
            re.findall(r'Refuse\(\s*ctx\s*,\s*"([A-Za-z0-9_]+)"', text)
        )
        produced["AF_KINGDOM_CW_Failure_"] |= set(
            re.findall(r'CivilWarRetryableEffectException\([^,()]+,\s*"([A-Za-z0-9_]+)"', text)
        )
        produced["AF_KINGDOM_CW_Failure_"] |= set(
            re.findall(r'CivilWarReviewRequiredException\(\s*"([A-Za-z0-9_]+)"', text)
        )
        # Note codes are the PascalCase literal in the first argument. The second
        # argument is the human sentence, which never looks like a code.
        for arguments in re.findall(r"ctx\.Note\(([^;]*?)\);", text):
            for literal in re.findall(r'"([^"\\]*)"', arguments):
                if re.fullmatch(r"[A-Z][A-Za-z0-9]{2,}", literal):
                    produced["AF_KINGDOM_CW_Note_"].add(literal)

    dynamic = 0
    for prefix, table in tables.items():
        for code, raw_fallback in table.items():
            fallback = unescape_cs(raw_fallback)
            key = prefix + code
            assert key in kin_en, f"lookup table code has no English resource entry: {key}"
            assert key in kin_zh, f"lookup table code has no Chinese resource entry: {key}"
            assert kin_en[key] == fallback, (
                f"English fallback differs from the resource for {key}\n"
                f"  C#:       {fallback!r}\n  resource: {kin_en[key]!r}"
            )
            assert kin_zh[key].strip(), f"empty translation: {key}"
            dynamic += 1
        for code in sorted(produced[prefix]):
            assert code in table, (
                f"{prefix}{code} is produced by the codebase but has no English fallback in "
                f"CivilWarUiText, so the notice would render raw text"
            )
            assert prefix + code in kin_en, f"produced code has no English resource entry: {prefix}{code}"
            assert prefix + code in kin_zh, f"produced code has no Chinese resource entry: {prefix}{code}"

    # Keys in the Kingdom resource that no literal and no dynamic family reaches.
    # Reported rather than asserted: another module may consume them.
    reachable = set(NAMED for NAMED in NAMED_KEY.findall("".join(
        read_text(p) for p in source_files(KIN_SOURCE_DIRS)
    )) if not NAMED.endswith("_"))
    for prefix, table in tables.items():
        reachable |= {prefix + code for code in table}
    for prefix in ("AF_KINGDOM_CW_FactionTag_", "AF_KINGDOM_CW_Demand_",
                   "AF_KINGDOM_CW_Source_", "AF_KINGDOM_CW_Stance_"):
        reachable |= {key for key in kin_en if key.startswith(prefix)}
    unreached = sorted(key for key in kin_en if key.startswith("AF_KINGDOM_CW_") and key not in reachable)
    if unreached:
        print(f"NOTE kingdom keys not reached by this scan ({len(unreached)}): {unreached}")

    print(
        "PASS Package D localization contract: "
        f"diplomacy pair={len(dip_en)} kingdom pair={len(kin_en)} "
        f"named keys={dip_named + kin_named} "
        f"fallbacks checked={dip_fallback + kin_fallback} "
        f"dynamic keys={dynamic} "
        f"produced codes={sum(len(codes) for codes in produced.values())}"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())

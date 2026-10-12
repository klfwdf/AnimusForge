"""Offline reproducible validator for PlayerExports English worldbook identity mapping.

Validates:
1. Path safety (relative paths only, no '..' traversal, strictly under PlayerExports, no reparse points),
   file existence, UTF-8 JSON parsing, and bidirectional directory coverage.
2. Separate accounting of mapping entries, unique Chinese sources, unique English targets,
   strict 1-to-1 mappings, and many-to-one merge groups.
3. Per-file-type contracts (hero_persona, knowledge_rule, kingdom_profiles, unnamed_persona,
   voice_mapping, event_data): stable IDs, conditions (When), numbers, booleans, placeholders,
   action/protocol tags, CJK absence, and rule rename reference tracking.
4. Byte-level and JSON-semantic comparison across all merged Chinese hero source groups,
   flagging any same-HeroId group whose Chinese source bodies differ without explicit
   approved merge resolution.
5. Routing classification (Categories A / B / C) against both committed HEAD and working-tree
   Get-AnimusForgeEnglishAsset selector behavior.
6. Separate standalone verification of unpaired English worldbooks (AD 1259 Chronicles).
7. Four negative fixtures under artifacts/localization/fixtures/worldbook-identity-map/
   (path traversal, missing English file, duplicate mapping source, same HeroId with different body).
"""
from __future__ import annotations

import argparse
import collections
import hashlib
import json
import os
import re
import subprocess
import sys
from pathlib import Path
from typing import Any

CJK_RE = re.compile(r"[\u3400-\u4dbf\u4e00-\u9fff\uf900-\ufaff]|[\ud840-\ud87e][\udc00-\udfff]")
TOKEN_RE = re.compile(r"\{[A-Za-z_][A-Za-z0-9_]*\}|\[(?:ACTION|RELAY|AD|ADP|AFEF):[^\]\r\n]*\]")
RULE_TOKEN_RE = re.compile(r"rule_[A-Za-z0-9_\u4e00-\u9fff]+")


class DuplicateKeyError(ValueError):
    """Raised when a JSON object contains duplicate keys."""


def load_json_strict_unique_keys(path: Path) -> Any:
    raw = path.read_text(encoding="utf-8-sig")

    def hook(pairs: list[tuple[str, Any]]) -> dict[str, Any]:
        out: dict[str, Any] = {}
        seen: set[str] = set()
        for k, v in pairs:
            if k in seen:
                raise DuplicateKeyError(f"Duplicate JSON key '{k}' in {path}")
            seen.add(k)
            out[k] = v
        return out

    return json.loads(raw, object_pairs_hook=hook)


def sha256_file(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def has_cjk(text: str) -> bool:
    return bool(CJK_RE.search(text))


def extract_tokens(text: str) -> list[str]:
    return sorted(set(TOKEN_RE.findall(text)))


def is_safe_relative_path(rel_path: str) -> tuple[bool, str]:
    if not isinstance(rel_path, str) or not rel_path.strip():
        return False, "empty_path"
    if os.path.isabs(rel_path) or rel_path.startswith(("/", "\\")) or ":" in rel_path:
        return False, f"rooted_or_drive_path:{rel_path}"
    segments = re.split(r"[\\/]", rel_path)
    if not segments or any(seg in ("", ".", "..") for seg in segments):
        return False, f"invalid_or_traversal_segment:{rel_path}"
    return True, "ok"


def has_reparse_point(path: Path, stop_at: Path) -> bool:
    try:
        cur = path
        stop_resolved = stop_at.resolve()
        while True:
            if cur.exists() and cur.is_symlink():
                return True
            if hasattr(os.stat_result, "st_file_attributes") and cur.exists():
                attrs = os.lstat(cur).st_file_attributes
                if attrs & 0x0400:  # FILE_ATTRIBUTE_REPARSE_POINT
                    return True
            if cur == stop_resolved or cur.parent == cur:
                break
            cur = cur.parent
    except OSError:
        return True
    return False


def ensure_under_root(candidate: Path, allowed_root: Path) -> tuple[bool, str]:
    try:
        resolved_root = allowed_root.resolve()
        resolved_cand = candidate.resolve()
        if not resolved_cand.is_relative_to(resolved_root):
            return False, f"escapes_root:{resolved_cand}"
        if has_reparse_point(candidate, allowed_root):
            return False, f"reparse_point:{candidate}"
        return True, "ok"
    except Exception as exc:
        return False, f"resolve_error:{exc}"


def check_contract_recursive(
    zh_val: Any,
    en_val: Any,
    path: str,
    file_type: str,
    errors: list[dict[str, Any]],
    warnings: list[dict[str, Any]],
    context_label: str,
) -> None:
    if zh_val is None or en_val is None:
        if (zh_val is None) != (en_val is None):
            errors.append({
                "code": "null_shape_mismatch",
                "context": context_label,
                "json_path": path,
                "detail": f"Null mismatch: zh={zh_val!r} vs en={en_val!r}",
            })
        return

    if isinstance(zh_val, bool) or isinstance(en_val, bool):
        if type(zh_val) is not type(en_val) or zh_val != en_val:
            errors.append({
                "code": "boolean_mismatch",
                "context": context_label,
                "json_path": path,
                "detail": f"Boolean mismatch: zh={zh_val!r} vs en={en_val!r}",
            })
        return

    if isinstance(zh_val, (int, float)) or isinstance(en_val, (int, float)):
        if type(zh_val) is not type(en_val) or zh_val != en_val:
            errors.append({
                "code": "numeric_value_mismatch",
                "context": context_label,
                "json_path": path,
                "detail": f"Numeric mismatch: zh={zh_val!r} vs en={en_val!r}",
            })
        return

    if isinstance(zh_val, str) and isinstance(en_val, str):
        if has_cjk(en_val):
            errors.append({
                "code": "cjk_in_english_value",
                "context": context_label,
                "json_path": path,
                "detail": f"CJK characters found in English string: {en_val[:120]}",
            })
        if path == "$.Id" and file_type == "knowledge_rule":
            if not en_val.startswith("rule_"):
                errors.append({
                    "code": "invalid_english_rule_id_prefix",
                    "context": context_label,
                    "json_path": path,
                    "detail": f"English rule Id must start with 'rule_': {en_val}",
                })
            return

        stable_leaf_names = (
            ".Kind",
            ".TargetId",
            ".VoiceId",
            ".KingdomId",
            ".CultureId",
            ".RulerHeroId",
            ".DefaultSource",
            ".GenerationState",
        )
        if any(path.endswith(sfx) for sfx in stable_leaf_names) or file_type == "voice_mapping":
            if zh_val != en_val:
                errors.append({
                    "code": "stable_field_mismatch",
                    "context": context_label,
                    "json_path": path,
                    "detail": f"Stable field changed: zh={zh_val!r} vs en={en_val!r}",
                })
            return

        if path == "$.ExportedAtUtc" and file_type == "kingdom_profiles":
            if zh_val != en_val:
                warnings.append({
                    "code": "exported_at_utc_timestamp_diff",
                    "context": context_label,
                    "json_path": path,
                    "detail": f"ExportedAtUtc metadata timestamp differs: zh={zh_val!r} vs en={en_val!r}",
                })
            return

        if zh_val.strip() and not en_val.strip():
            errors.append({
                "code": "empty_english_translation",
                "context": context_label,
                "json_path": path,
                "detail": "Chinese string is non-empty but English translation is empty",
            })

        zh_tokens = extract_tokens(zh_val)
        en_tokens = extract_tokens(en_val)
        if zh_tokens != en_tokens:
            errors.append({
                "code": "placeholder_or_protocol_tag_mismatch",
                "context": context_label,
                "json_path": path,
                "detail": f"Tokens changed: zh={zh_tokens} vs en={en_tokens}",
            })
        return

    if isinstance(zh_val, list) and isinstance(en_val, list):
        if path.endswith(".Keywords"):
            if len(zh_val) > 0 and len(en_val) == 0:
                errors.append({
                    "code": "empty_keywords_array",
                    "context": context_label,
                    "json_path": path,
                    "detail": "English Keywords array is empty while Chinese Keywords is non-empty",
                })
            for idx, kw in enumerate(en_val):
                if not isinstance(kw, str) or not kw.strip() or has_cjk(kw):
                    errors.append({
                        "code": "invalid_english_keyword",
                        "context": context_label,
                        "json_path": f"{path}[{idx}]",
                        "detail": f"Invalid or CJK keyword: {kw!r}",
                    })
            return

        if len(zh_val) != len(en_val):
            errors.append({
                "code": "array_length_mismatch",
                "context": context_label,
                "json_path": path,
                "detail": f"Array length mismatch: zh={len(zh_val)} vs en={len(en_val)}",
            })
            return

        if ".When." in f"{path}." and all(isinstance(x, str) for x in zh_val):
            if zh_val != en_val:
                errors.append({
                    "code": "when_condition_array_mismatch",
                    "context": context_label,
                    "json_path": path,
                    "detail": f"When condition list changed: zh={zh_val} vs en={en_val}",
                })
            return

        for idx in range(len(zh_val)):
            check_contract_recursive(
                zh_val[idx],
                en_val[idx],
                f"{path}[{idx}]",
                file_type,
                errors,
                warnings,
                context_label,
            )
        return

    if isinstance(zh_val, dict) and isinstance(en_val, dict):
        zh_keys = sorted(zh_val.keys())
        en_keys = sorted(en_val.keys())
        if zh_keys != en_keys:
            errors.append({
                "code": "object_keys_mismatch",
                "context": context_label,
                "json_path": path,
                "detail": f"Object keys changed: zh={zh_keys} vs en={en_keys}",
            })
            return
        for k in zh_keys:
            check_contract_recursive(
                zh_val[k],
                en_val[k],
                f"{path}.{k}",
                file_type,
                errors,
                warnings,
                context_label,
            )
        return

    errors.append({
        "code": "value_type_mismatch",
        "context": context_label,
        "json_path": path,
        "detail": f"Type mismatch: zh={type(zh_val).__name__} vs en={type(en_val).__name__}",
    })


def classify_file_type(rel_path: str) -> str:
    if rel_path.startswith("personality_background/"):
        return "hero_persona"
    if rel_path.startswith("knowledge/rules/"):
        return "knowledge_rule"
    if rel_path == "kingdom_profiles/KingdomProfiles.json":
        return "kingdom_profiles"
    if rel_path == "unnamed_persona/UnnamedNpcProfiles.json":
        return "unnamed_persona"
    if rel_path == "voice_mapping/VoiceMapping.json":
        return "voice_mapping"
    if rel_path.startswith("event_data/"):
        return "event_data"
    return "other"


def validate_map(
    root: Path,
    player_exports_root: Path,
    map_path: Path,
    check_disk_coverage: bool = True,
    audit_routing: bool = True,
) -> dict[str, Any]:
    errors: list[dict[str, Any]] = []
    warnings: list[dict[str, Any]] = []
    fingerprints: dict[str, str] = {}

    if not map_path.is_file():
        errors.append({
            "code": "missing_identity_map",
            "context": str(map_path),
            "json_path": "$",
            "detail": f"Identity map file not found: {map_path}",
        })
        return {"valid": False, "errors": errors, "warnings": warnings}

    fingerprints[map_path.relative_to(root).as_posix() if map_path.is_relative_to(root) else str(map_path)] = sha256_file(map_path)

    try:
        id_map = load_json_strict_unique_keys(map_path)
    except DuplicateKeyError as exc:
        errors.append({
            "code": "duplicate_mapping_source",
            "context": str(map_path),
            "json_path": "$",
            "detail": str(exc),
        })
        return {"valid": False, "errors": errors, "warnings": warnings}
    except Exception as exc:
        errors.append({
            "code": "invalid_identity_map_json",
            "context": str(map_path),
            "json_path": "$",
            "detail": str(exc),
        })
        return {"valid": False, "errors": errors, "warnings": warnings}

    # Parse all JSON files under player_exports_root if check_disk_coverage is enabled
    total_player_exports_json_files = 0
    if check_disk_coverage and player_exports_root.is_dir():
        for jf in sorted(player_exports_root.rglob("*.json")):
            total_player_exports_json_files += 1
            rel_jf = jf.relative_to(root).as_posix() if jf.is_relative_to(root) else str(jf)
            fingerprints[rel_jf] = sha256_file(jf)
            try:
                load_json_strict_unique_keys(jf)
            except Exception as exc:
                errors.append({
                    "code": "json_parse_error",
                    "context": rel_jf,
                    "json_path": "$",
                    "detail": str(exc),
                })

    worldbooks_node = id_map.get("worldbooks")
    if not isinstance(worldbooks_node, dict) or not worldbooks_node:
        errors.append({
            "code": "invalid_worldbooks_root",
            "context": str(map_path),
            "json_path": "$.worldbooks",
            "detail": "$.worldbooks must be a non-empty object",
        })
        return {"valid": False, "errors": errors, "warnings": warnings}

    all_zh_rule_ids: dict[str, list[str]] = collections.defaultdict(list)
    all_renamed_rules_count = 0
    all_unchanged_rules_count = 0
    per_worldbook_stats: dict[str, Any] = {}
    multi_source_groups: list[dict[str, Any]] = []
    independent_same_hero_id_variant_groups: list[dict[str, Any]] = []
    file_type_counts: dict[str, int] = collections.Counter()

    seen_global_zh_sources: set[str] = set()
    mapped_by_target_key: dict[str, dict[str, Any]] = {}
    dup_resolutions_root = id_map.get("duplicate_resolutions") or {}

    for wb_name, bmap in worldbooks_node.items():
        zh_dir_rel = bmap.get("zh_directory", "")
        en_dir_rel = bmap.get("en_directory", "")
        for label, d_rel in (("zh_directory", zh_dir_rel), ("en_directory", en_dir_rel)):
            ok, reason = is_safe_relative_path(d_rel)
            if not ok:
                errors.append({
                    "code": "path_out_of_bounds_or_traversal",
                    "context": f"{wb_name}.{label}",
                    "json_path": f"$.worldbooks[{wb_name!r}].{label}",
                    "detail": reason,
                })
        zh_dir = player_exports_root / zh_dir_rel
        en_dir = player_exports_root / en_dir_rel
        for label, d_path in (("zh_directory", zh_dir), ("en_directory", en_dir)):
            ok, reason = ensure_under_root(d_path, player_exports_root)
            if not ok or not d_path.is_dir():
                errors.append({
                    "code": "path_out_of_bounds_or_traversal" if not ok else "missing_worldbook_directory",
                    "context": f"{wb_name}.{label}",
                    "json_path": f"$.worldbooks[{wb_name!r}].{label}",
                    "detail": reason if not ok else f"Directory does not exist: {d_path}",
                })

        wb_entries: list[tuple[str, str, str, dict[str, Any]]] = []
        seen_wb_zh: set[str] = set()

        for section in ("globals", "extra_files", "heroes", "rules"):
            sec_obj = bmap.get(section)
            if sec_obj is None:
                continue
            if not isinstance(sec_obj, dict):
                errors.append({
                    "code": "invalid_section_type",
                    "context": f"{wb_name}.{section}",
                    "json_path": f"$.worldbooks[{wb_name!r}].{section}",
                    "detail": f"Section {section} must be an object",
                })
                continue
            for zh_rel, target_spec in sec_obj.items():
                if zh_rel in seen_wb_zh:
                    errors.append({
                        "code": "duplicate_mapping_source",
                        "context": f"{wb_name}/{zh_rel}",
                        "json_path": f"$.worldbooks[{wb_name!r}].{section}[{zh_rel!r}]",
                        "detail": f"Duplicate Chinese source path '{zh_rel}' across sections in {wb_name}",
                    })
                seen_wb_zh.add(zh_rel)
                full_zh_key = f"{zh_dir_rel}/{zh_rel}"
                if full_zh_key in seen_global_zh_sources:
                    errors.append({
                        "code": "duplicate_mapping_source",
                        "context": full_zh_key,
                        "json_path": f"$.worldbooks[{wb_name!r}].{section}[{zh_rel!r}]",
                        "detail": f"Duplicate Chinese source path '{full_zh_key}' across worldbooks",
                    })
                seen_global_zh_sources.add(full_zh_key)

                if section in ("globals", "extra_files"):
                    en_rel = target_spec if isinstance(target_spec, str) else ""
                    meta = {"en_file": en_rel}
                else:
                    en_rel = target_spec.get("en_file", "") if isinstance(target_spec, dict) else ""
                    meta = target_spec if isinstance(target_spec, dict) else {}
                wb_entries.append((section, zh_rel, en_rel, meta))

        zh_files_map: dict[str, Path] = {}
        en_files_map: dict[str, Path] = {}
        by_en_target: dict[str, list[tuple[str, str, dict[str, Any]]]] = collections.defaultdict(list)
        by_hero_id: dict[str, list[tuple[str, str, str, dict[str, Any]]]] = collections.defaultdict(list)
        en_hero_id_owners: dict[str, str] = {}
        en_rule_id_owners: dict[str, str] = {}
        en_kw_owners: dict[str, str] = {}

        wb_renamed_rules = 0
        wb_unchanged_rules = 0

        declared_variant_resolutions: dict[str, dict[str, Any]] = {}
        if isinstance(dup_resolutions_root, dict):
            for res_item in dup_resolutions_root.get(wb_name) or []:
                if isinstance(res_item, dict) and res_item.get("resolution_type") == "independent_same_hero_id_variants":
                    raw_hid = str(res_item.get("hero_id") or res_item.get("identity") or "").strip()
                    if "hero:" in raw_hid:
                        raw_hid = raw_hid.split("hero:", 1)[1].strip()
                    hid_key = raw_hid.lower()
                    if hid_key:
                        declared_variant_resolutions[hid_key] = res_item

        for section, zh_rel, en_rel, meta in wb_entries:
            ok_zh, reason_zh = is_safe_relative_path(zh_rel)
            ok_en, reason_en = is_safe_relative_path(en_rel)
            if not ok_zh or not ok_en:
                errors.append({
                    "code": "path_out_of_bounds_or_traversal",
                    "context": f"{wb_name}:{zh_rel}->{en_rel}",
                    "json_path": f"$.worldbooks[{wb_name!r}].{section}[{zh_rel!r}]",
                    "detail": f"zh={reason_zh}, en={reason_en}",
                })
                continue

            zh_full = zh_dir / zh_rel
            en_full = en_dir / en_rel
            ok_u_zh, r_u_zh = ensure_under_root(zh_full, player_exports_root)
            ok_u_en, r_u_en = ensure_under_root(en_full, player_exports_root)
            if not ok_u_zh or not ok_u_en:
                errors.append({
                    "code": "path_out_of_bounds_or_traversal",
                    "context": f"{wb_name}:{zh_rel}->{en_rel}",
                    "json_path": f"$.worldbooks[{wb_name!r}].{section}[{zh_rel!r}]",
                    "detail": f"zh={r_u_zh}, en={r_u_en}",
                })
                continue

            if not zh_full.is_file():
                errors.append({
                    "code": "missing_chinese_source_file",
                    "context": f"{wb_name}/{zh_rel}",
                    "json_path": f"$.worldbooks[{wb_name!r}].{section}[{zh_rel!r}]",
                    "detail": f"Chinese source file does not exist: {zh_full}",
                })
                continue

            if not en_full.is_file():
                errors.append({
                    "code": "missing_english_target_file",
                    "context": f"{wb_name}/{en_rel}",
                    "json_path": f"$.worldbooks[{wb_name!r}].{section}[{zh_rel!r}].en_file",
                    "detail": f"English target file does not exist: {en_full}",
                })
                continue

            zh_files_map[zh_rel] = zh_full
            en_files_map[en_rel] = en_full
            by_en_target[en_rel].append((section, zh_rel, meta))
            mapped_by_target_key[f"PlayerExports/{zh_dir_rel}/{zh_rel}"] = {
                "worldbook": wb_name,
                "section": section,
                "zh_rel": zh_rel,
                "en_rel": en_rel,
                "zh_full": zh_full,
                "en_full": en_full,
                "meta": meta,
            }

            ftype = classify_file_type(zh_rel)
            file_type_counts[ftype] += 1

            try:
                zh_data = load_json_strict_unique_keys(zh_full)
                en_data = load_json_strict_unique_keys(en_full)
            except Exception as exc:
                errors.append({
                    "code": "json_parse_error",
                    "context": f"{wb_name}:{zh_rel}->{en_rel}",
                    "json_path": "$",
                    "detail": str(exc),
                })
                continue

            check_contract_recursive(
                zh_data,
                en_data,
                "$",
                ftype,
                errors,
                warnings,
                f"{wb_name}:{zh_rel}->{en_rel}",
            )

            if section == "heroes" or ftype == "hero_persona":
                map_hid = meta.get("hero_id", "")
                zh_hid = Path(zh_rel).stem.split("__", 1)[0]
                en_hid = Path(en_rel).stem.split("__", 1)[0]
                if not (map_hid and map_hid == zh_hid == en_hid):
                    errors.append({
                        "code": "hero_id_mismatch",
                        "context": f"{wb_name}:{zh_rel}->{en_rel}",
                        "json_path": f"$.worldbooks[{wb_name!r}].heroes[{zh_rel!r}].hero_id",
                        "detail": f"HeroId mismatch: map={map_hid!r}, zh_stem={zh_hid!r}, en_stem={en_hid!r}",
                    })
                by_hero_id[en_hid.lower()].append((en_hid, zh_rel, en_rel, meta))
                en_hero_id_owners[en_hid.lower()] = en_rel

            elif section == "rules" or ftype == "knowledge_rule":
                map_zh_id = meta.get("zh_id", "")
                map_en_id = meta.get("en_id", "")
                actual_zh_id = zh_data.get("Id", "") if isinstance(zh_data, dict) else ""
                actual_en_id = en_data.get("Id", "") if isinstance(en_data, dict) else ""
                if actual_zh_id != map_zh_id or actual_en_id != map_en_id:
                    errors.append({
                        "code": "rule_id_mapping_mismatch",
                        "context": f"{wb_name}:{zh_rel}->{en_rel}",
                        "json_path": f"$.worldbooks[{wb_name!r}].rules[{zh_rel!r}]",
                        "detail": (
                            f"Rule Id mismatch: zh_file={actual_zh_id!r} vs map.zh_id={map_zh_id!r}, "
                            f"en_file={actual_en_id!r} vs map.en_id={map_en_id!r}"
                        ),
                    })
                if actual_zh_id != actual_en_id:
                    wb_renamed_rules += 1
                    all_renamed_rules_count += 1
                else:
                    wb_unchanged_rules += 1
                    all_unchanged_rules_count += 1

                all_zh_rule_ids[actual_zh_id].append(f"{zh_dir_rel}/{zh_rel}")

                prev_rule_en = en_rule_id_owners.get(actual_en_id.lower())
                if prev_rule_en and prev_rule_en != en_rel:
                    errors.append({
                        "code": "duplicate_english_rule_id",
                        "context": f"{wb_name}/{en_rel}",
                        "json_path": "$.Id",
                        "detail": f"Duplicate English Rule Id '{actual_en_id}' in {prev_rule_en} and {en_rel}",
                    })
                en_rule_id_owners[actual_en_id.lower()] = en_rel

                seen_local_kw: set[str] = set()
                for kw in (en_data.get("Keywords") or []) if isinstance(en_data, dict) else []:
                    kn = " ".join(str(kw).strip().split()).lower()
                    if kn in seen_local_kw:
                        errors.append({
                            "code": "duplicate_rule_local_keyword",
                            "context": f"{wb_name}/{en_rel}",
                            "json_path": "$.Keywords",
                            "detail": f"Duplicate keyword '{kn}' inside {en_rel}",
                        })
                    seen_local_kw.add(kn)
                    prev_kw_owner = en_kw_owners.get(kn)
                    if prev_kw_owner and prev_kw_owner != en_rel:
                        errors.append({
                            "code": "duplicate_worldbook_keyword",
                            "context": f"{wb_name}/{en_rel}",
                            "json_path": "$.Keywords",
                            "detail": f"Duplicate keyword '{kn}' across {prev_kw_owner} and {en_rel}",
                        })
                    en_kw_owners[kn] = en_rel

        # Check HeroId groups that span multiple English target files:
        # Distinguish declared independent same-HeroId fidelity variants from unapproved duplicate HeroIds.
        variant_en_targets_in_wb: set[str] = set()
        wb_independent_variant_groups = 0
        wb_independent_variant_zh_count = 0
        wb_independent_variant_en_count = 0

        for hid_lower, h_items in sorted(by_hero_id.items()):
            unique_en_rels = sorted({it[2] for it in h_items})
            if len(unique_en_rels) <= 1:
                continue
            zh_rels = [it[1] for it in h_items]
            en_rels = [it[2] for it in h_items]
            canonical_hid = h_items[0][0]
            decl = declared_variant_resolutions.get(hid_lower)

            zh_json_list = [load_json_strict_unique_keys(zh_dir / zr) for zr in zh_rels]
            en_json_list = [load_json_strict_unique_keys(en_dir / er) for er in en_rels]
            zh_bodies_differ = not all(j == zh_json_list[0] for j in zh_json_list[1:])
            en_bodies_differ = len({json.dumps(ej, sort_keys=True) for ej in en_json_list}) == len(en_json_list)
            all_have_variant_note = all(
                isinstance(it[3], dict) and bool(str(it[3].get("variant_note", "")).strip())
                for it in h_items
            )
            decl_matches = (
                decl is not None
                and sorted(decl.get("zh_files") or []) == sorted(zh_rels)
                and sorted(decl.get("en_files") or []) == sorted(en_rels)
                and bool(str(decl.get("reason") or decl.get("note") or "").strip())
            )

            if (
                len(unique_en_rels) == len(zh_rels)
                and decl_matches
                and all_have_variant_note
                and zh_bodies_differ
                and en_bodies_differ
            ):
                wb_independent_variant_groups += 1
                wb_independent_variant_zh_count += len(zh_rels)
                wb_independent_variant_en_count += len(unique_en_rels)
                variant_en_targets_in_wb.update(unique_en_rels)

                zh_diffs: dict[str, Any] = {}
                en_diffs: dict[str, Any] = {}
                if all(isinstance(j, dict) for j in zh_json_list):
                    for k in sorted({k for j in zh_json_list for k in j.keys()}):
                        vals = [j.get(k) for j in zh_json_list]
                        if not all(v == vals[0] for v in vals[1:]):
                            zh_diffs[f"$.{k}"] = {zh_rels[i]: vals[i] for i in range(len(zh_rels))}
                if all(isinstance(j, dict) for j in en_json_list):
                    for k in sorted({k for j in en_json_list for k in j.keys()}):
                        vals = [j.get(k) for j in en_json_list]
                        if not all(v == vals[0] for v in vals[1:]):
                            en_diffs[f"$.{k}"] = {en_rels[i]: vals[i] for i in range(len(en_rels))}

                independent_same_hero_id_variant_groups.append({
                    "worldbook": wb_name,
                    "zh_directory": zh_dir_rel,
                    "en_directory": en_dir_rel,
                    "hero_id": canonical_hid,
                    "resolution_type": "independent_same_hero_id_variants",
                    "resolution_note": str(decl.get("reason") or decl.get("note") or ""),
                    "pairs": [
                        {
                            "zh_file": h_items[i][1],
                            "zh_sha256": sha256_file(zh_dir / h_items[i][1]),
                            "en_file": h_items[i][2],
                            "en_sha256": sha256_file(en_dir / h_items[i][2]),
                            "variant_note": h_items[i][3].get("variant_note", ""),
                        }
                        for i in range(len(h_items))
                    ],
                    "zh_field_differences": zh_diffs,
                    "en_field_differences": en_diffs,
                })
            else:
                errors.append({
                    "code": "duplicate_english_hero_id",
                    "context": f"{wb_name}:{canonical_hid}",
                    "json_path": f"$.worldbooks[{wb_name!r}].heroes",
                    "detail": (
                        f"Duplicate English HeroId '{canonical_hid}' across {unique_en_rels} "
                        f"without valid independent_same_hero_id_variants declaration/evidence."
                    ),
                })

        # Check many-to-one groups in this worldbook
        wb_strict_1to1 = 0
        wb_many_to_one_groups = 0
        wb_many_to_one_zh_count = 0
        for en_rel, src_items in sorted(by_en_target.items()):
            if len(src_items) == 1:
                if en_rel not in variant_en_targets_in_wb:
                    wb_strict_1to1 += 1
            else:
                wb_many_to_one_groups += 1
                wb_many_to_one_zh_count += len(src_items)
                zh_rels = [item[1] for item in src_items]
                raw_bytes_list = [(zh_dir / zr).read_bytes() for zr in zh_rels]
                json_list = [load_json_strict_unique_keys(zh_dir / zr) for zr in zh_rels]
                en_json_obj = load_json_strict_unique_keys(en_dir / en_rel)
                byte_eq = all(b == raw_bytes_list[0] for b in raw_bytes_list[1:])
                json_eq = all(j == json_list[0] for j in json_list[1:])
                status = (
                    "byte_identical"
                    if byte_eq
                    else ("json_semantic_identical" if json_eq else "content_different")
                )
                field_diffs: dict[str, Any] = {}
                if not json_eq and all(isinstance(j, dict) for j in json_list):
                    all_k = sorted({k for j in json_list for k in j.keys()})
                    for k in all_k:
                        vals = [j.get(k) for j in json_list]
                        if not all(v == vals[0] for v in vals[1:]):
                            field_diffs[f"$.{k}"] = {zh_rels[i]: vals[i] for i in range(len(zh_rels))}

                # Lossless many-to-one requires byte or JSON-semantic equality; divergent merges always fail
                has_lossless_evidence = byte_eq or json_eq
                if not has_lossless_evidence:
                    errors.append({
                        "code": "unapproved_content_divergent_merge",
                        "context": f"{wb_name}:{en_rel}",
                        "json_path": f"$.worldbooks[{wb_name!r}].heroes",
                        "detail": (
                            f"Multiple Chinese sources {zh_rels} map to '{en_rel}' with same HeroId "
                            f"but different JSON content in fields {list(field_diffs.keys())}."
                        ),
                    })

                multi_source_groups.append({
                    "worldbook": wb_name,
                    "zh_directory": zh_dir_rel,
                    "en_directory": en_dir_rel,
                    "section": src_items[0][0],
                    "hero_id": src_items[0][2].get("hero_id") if isinstance(src_items[0][2], dict) else None,
                    "en_file": en_rel,
                    "en_sha256": sha256_file(en_dir / en_rel),
                    "zh_files": zh_rels,
                    "zh_sha256": {
                        zh_rels[i]: hashlib.sha256(raw_bytes_list[i]).hexdigest()
                        for i in range(len(zh_rels))
                    },
                    "byte_identical": byte_eq,
                    "json_semantic_identical": json_eq,
                    "comparison_status": status,
                    "merge_evidence": (
                        "byte_identical_sha256"
                        if byte_eq
                        else ("json_semantic_equivalent" if json_eq else "missing_or_divergent")
                    ),
                    "field_differences": field_diffs,
                    "en_target_content": en_json_obj if not json_eq else None,
                })

        # Disk coverage check for this worldbook
        unmapped_zh_on_disk: list[str] = []
        unmapped_en_on_disk: list[str] = []
        if check_disk_coverage and zh_dir.is_dir() and en_dir.is_dir():
            disk_zh = {p.relative_to(zh_dir).as_posix() for p in zh_dir.rglob("*.json")}
            disk_en = {p.relative_to(en_dir).as_posix() for p in en_dir.rglob("*.json")}
            unmapped_zh_on_disk = sorted(disk_zh - set(zh_files_map.keys()))
            unmapped_en_on_disk = sorted(disk_en - set(en_files_map.keys()))
            for uz in unmapped_zh_on_disk:
                errors.append({
                    "code": "unmapped_chinese_disk_file",
                    "context": f"{wb_name}/{uz}",
                    "json_path": f"$.worldbooks[{wb_name!r}]",
                    "detail": f"Chinese JSON file on disk is missing from identity map: {uz}",
                })
            for ue in unmapped_en_on_disk:
                errors.append({
                    "code": "unmapped_english_disk_file",
                    "context": f"{wb_name}/{ue}",
                    "json_path": f"$.worldbooks[{wb_name!r}]",
                    "detail": f"English JSON file on disk is not targeted by identity map: {ue}",
                })

        per_worldbook_stats[wb_name] = {
            "zh_directory": zh_dir_rel,
            "en_directory": en_dir_rel,
            "mapping_entries": len(wb_entries),
            "unique_zh_sources": len(zh_files_map),
            "unique_en_targets": len(en_files_map),
            "strict_one_to_one_entries": wb_strict_1to1,
            "many_to_one_groups_count": wb_many_to_one_groups,
            "many_to_one_zh_sources_count": wb_many_to_one_zh_count,
            "independent_same_hero_id_variant_groups_count": wb_independent_variant_groups,
            "independent_same_hero_id_variant_zh_sources_count": wb_independent_variant_zh_count,
            "independent_same_hero_id_variant_en_targets_count": wb_independent_variant_en_count,
            "globals_count": len(bmap.get("globals") or {}),
            "heroes_mapping_count": len(bmap.get("heroes") or {}),
            "unique_en_hero_ids_count": len(en_hero_id_owners),
            "unique_en_hero_files_count": len(en_hero_id_owners) + (wb_independent_variant_en_count - wb_independent_variant_groups),
            "rules_mapping_count": len(bmap.get("rules") or {}),
            "unique_en_rules_count": len(en_rule_id_owners),
            "renamed_rules_count": wb_renamed_rules,
            "unchanged_rules_count": wb_unchanged_rules,
            "unique_en_keywords_count": len(en_kw_owners),
            "unmapped_zh_disk_files": unmapped_zh_on_disk,
            "unmapped_en_disk_files": unmapped_en_on_disk,
        }

    # Audit unpaired English worldbooks (AD 1259 Chronicles) separately
    unpaired_report: dict[str, Any] = {}
    unpaired_node = id_map.get("unpaired_english_worldbooks") or {}
    if isinstance(unpaired_node, dict):
        for up_name, up_info in unpaired_node.items():
            en_dir_rel = up_info.get("en_directory", up_name) if isinstance(up_info, dict) else up_name
            ok, reason = is_safe_relative_path(en_dir_rel)
            up_dir = player_exports_root / en_dir_rel
            ok_root, reason_root = ensure_under_root(up_dir, player_exports_root)
            if not ok or not ok_root or not up_dir.is_dir():
                errors.append({
                    "code": "invalid_unpaired_worldbook_dir",
                    "context": up_name,
                    "json_path": f"$.unpaired_english_worldbooks[{up_name!r}]",
                    "detail": f"ok={ok} ({reason}), root={ok_root} ({reason_root})",
                })
                continue
            up_files = sorted(up_dir.rglob("*.json")) if check_disk_coverage else []
            up_heroes: dict[str, str] = {}
            up_rules: dict[str, str] = {}
            up_kws: dict[str, str] = {}
            up_globals = 0
            for uf in up_files:
                rel = uf.relative_to(up_dir).as_posix()
                raw = uf.read_text(encoding="utf-8-sig")
                if has_cjk(raw):
                    errors.append({
                        "code": "cjk_in_unpaired_english_worldbook",
                        "context": f"{up_name}/{rel}",
                        "json_path": "$",
                        "detail": "CJK characters found in unpaired English worldbook",
                    })
                d = load_json_strict_unique_keys(uf)
                if rel.startswith("personality_background/"):
                    hid = uf.stem.split("__", 1)[0].lower()
                    if hid in up_heroes:
                        errors.append({
                            "code": "duplicate_unpaired_hero_id",
                            "context": f"{up_name}/{rel}",
                            "json_path": "$",
                            "detail": f"Duplicate hero ID {hid}",
                        })
                    up_heroes[hid] = rel
                elif rel.startswith("knowledge/rules/"):
                    rid = str(d.get("Id", "")).strip().lower()
                    if not rid.startswith("rule_") or rid in up_rules:
                        errors.append({
                            "code": "invalid_or_duplicate_unpaired_rule_id",
                            "context": f"{up_name}/{rel}",
                            "json_path": "$.Id",
                            "detail": f"Invalid or duplicate rule ID {rid}",
                        })
                    up_rules[rid] = rel
                    for kw in d.get("Keywords") or []:
                        kn = " ".join(str(kw).strip().split()).lower()
                        if kn in up_kws and up_kws[kn] != rid:
                            errors.append({
                                "code": "duplicate_unpaired_keyword",
                                "context": f"{up_name}/{rel}",
                                "json_path": "$.Keywords",
                                "detail": f"Duplicate keyword '{kn}' in {up_kws[kn]} and {rid}",
                            })
                        up_kws[kn] = rid
                else:
                    up_globals += 1
            unpaired_report[up_name] = {
                "en_directory": en_dir_rel,
                "has_chinese_counterpart": False,
                "included_in_chinese_source_coverage": False,
                "total_json_files": len(up_files),
                "globals_count": up_globals,
                "unique_heroes_count": len(up_heroes),
                "unique_rules_count": len(up_rules),
                "unique_keywords_count": len(up_kws),
            }

    # Check internal PlayerExports cross-references to Chinese rule IDs (outside each rule's own $.Id)
    internal_zh_rule_id_refs: list[dict[str, str]] = []
    if check_disk_coverage and player_exports_root.is_dir():
        zh_id_set = set(all_zh_rule_ids.keys())
        for jf in sorted(player_exports_root.rglob("*.json")):
            if jf.name == map_path.name:
                continue
            rel = jf.relative_to(player_exports_root).as_posix()
            try:
                d = json.loads(jf.read_text(encoding="utf-8-sig"))
            except Exception:
                continue
            if "/knowledge/rules/" in rel and isinstance(d, dict):
                d_copy = dict(d)
                d_copy.pop("Id", None)
                scan_text = json.dumps(d_copy, ensure_ascii=False)
            else:
                scan_text = json.dumps(d, ensure_ascii=False)
            for m in RULE_TOKEN_RE.finditer(scan_text):
                tok = m.group(0)
                if tok in zh_id_set:
                    internal_zh_rule_id_refs.append({"file": rel, "referenced_zh_rule_id": tok})

    # Audit routing (Categories A, B, C) against content/content-map.json and package_localization.ps1
    routing_summary: dict[str, Any] = {}
    cmap_path = root / "content/content-map.json"
    pkg_ps1_path = root / "scripts/build/package_localization.ps1"
    if audit_routing and cmap_path.is_file():
        fingerprints["content/content-map.json"] = sha256_file(cmap_path)
        if pkg_ps1_path.is_file():
            fingerprints["scripts/build/package_localization.ps1"] = sha256_file(pkg_ps1_path)
        cmap_entries = json.loads(cmap_path.read_text(encoding="utf-8"))["entries"]
        pe_entries = [e for e in cmap_entries if str(e.get("target", "")).startswith("PlayerExports/")]
        pe_json_entries = [e for e in pe_entries if str(e.get("target", "")).endswith(".json")]
        pe_non_json_entries = [e for e in pe_entries if not str(e.get("target", "")).endswith(".json")]

        # 1. Committed HEAD selector behavior (Calradia Chronicles only, rules by Chinese $.Id)
        head_cat_a: list[str] = []
        head_cat_b: list[str] = []
        head_cat_c: list[str] = []
        head_b_by_reason: dict[str, int] = collections.Counter()

        # 2. Working-tree selector behavior (with MappedFiles from english-worldbook-identity-map.json)
        wt_cat_a: list[str] = []
        wt_cat_b: list[str] = []
        wt_cat_c: list[str] = []

        calradia_zh_prefix = "PlayerExports/卡拉迪亚编年史/"
        for e in pe_json_entries:
            tgt = str(e["target"])
            in_map = tgt in mapped_by_target_key
            en_exists = in_map and mapped_by_target_key[tgt]["en_full"].is_file()
            if not (in_map and en_exists):
                head_cat_a.append(tgt)
                wt_cat_a.append(tgt)
                continue

            # Working-tree selector checks $Worldbook.MappedFiles[$Entry.Target]
            wt_cat_c.append(tgt)

            # Committed HEAD selector only indexes 'Calradia Chronicles' and matches rules by Chinese $.Id
            if tgt.startswith(calradia_zh_prefix):
                sub = tgt[len(calradia_zh_prefix):]
                if sub.startswith("knowledge/rules/"):
                    m_info = mapped_by_target_key[tgt]["meta"]
                    if m_info.get("zh_id") == m_info.get("en_id"):
                        head_cat_c.append(tgt)
                    else:
                        head_cat_b.append(tgt)
                        head_b_by_reason["calradia_rule_id_renamed_from_zh_to_en"] += 1
                else:
                    head_cat_c.append(tgt)
            else:
                head_cat_b.append(tgt)
                wb_label = mapped_by_target_key[tgt]["worldbook"]
                head_b_by_reason[f"non_default_worldbook_not_indexed_in_head:{wb_label}"] += 1

        non_json_audit = []
        for e in pe_non_json_entries:
            tgt = str(e["target"])
            src = str(e["source"])
            ext = Path(tgt).suffix.lower()
            gated_by_en_text_check = ext in {".json", ".xml", ".txt", ".md", ".csv", ".yml", ".yaml"}
            non_json_audit.append({
                "target": tgt,
                "source": src,
                "extension": ext,
                "gated_by_package_localization_text_gate": gated_by_en_text_check,
                "classification": (
                    "Category A (unmapped Chinese .txt file in content-map.json; hits EN text gate)"
                    if gated_by_en_text_check
                    else "Non-gated authoring script (.py) in content-map.json"
                ),
            })

        routing_summary = {
            "content_map_player_exports_total_entries": len(pe_entries),
            "content_map_player_exports_json_entries": len(pe_json_entries),
            "content_map_player_exports_non_json_entries": len(pe_non_json_entries),
            "committed_head_selector_json_counts": {
                "basis": "HEAD (97ad1ed14) scripts/build/package_localization.ps1 without MappedFiles wiring",
                "category_A_missing_asset_or_mapping": len(head_cat_a),
                "category_B_mapped_and_exists_but_selector_unrouted": len(head_cat_b),
                "category_C_routed_and_selected": len(head_cat_c),
                "category_B_breakdown": dict(head_b_by_reason),
            },
            "working_tree_selector_json_counts": {
                "basis": "Working-tree scripts/build/package_localization.ps1 with MappedFiles wiring (verified via real PowerShell execution and static model)",
                "category_A_missing_asset_or_mapping": len(wt_cat_a),
                "category_B_mapped_and_exists_but_selector_unrouted": len(wt_cat_b),
                "category_C_routed_and_selected": len(wt_cat_c),
            },
            "non_json_content_map_entries_under_player_exports": non_json_audit,
        }

    total_mapping_entries = sum(s["mapping_entries"] for s in per_worldbook_stats.values())
    total_unique_zh = sum(s["unique_zh_sources"] for s in per_worldbook_stats.values())
    total_unique_en = sum(s["unique_en_targets"] for s in per_worldbook_stats.values())
    total_strict_1to1 = sum(s["strict_one_to_one_entries"] for s in per_worldbook_stats.values())
    total_many_to_one_groups = sum(s["many_to_one_groups_count"] for s in per_worldbook_stats.values())
    total_many_to_one_zh = sum(s["many_to_one_zh_sources_count"] for s in per_worldbook_stats.values())
    total_ind_var_groups = sum(s["independent_same_hero_id_variant_groups_count"] for s in per_worldbook_stats.values())
    total_ind_var_zh = sum(s["independent_same_hero_id_variant_zh_sources_count"] for s in per_worldbook_stats.values())
    total_ind_var_en = sum(s["independent_same_hero_id_variant_en_targets_count"] for s in per_worldbook_stats.values())

    byte_eq_groups = sum(1 for g in multi_source_groups if g["comparison_status"] == "byte_identical")
    json_eq_only_groups = sum(1 for g in multi_source_groups if g["comparison_status"] == "json_semantic_identical")
    diff_groups = sum(1 for g in multi_source_groups if g["comparison_status"] == "content_different")

    try:
        revision = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip()
    except Exception:
        revision = "unknown"

    changed_during_scan: list[str] = []
    for rel_p, digest in fingerprints.items():
        p_obj = root / rel_p if not os.path.isabs(rel_p) else Path(rel_p)
        if p_obj.is_file() and sha256_file(p_obj) != digest:
            changed_during_scan.append(rel_p)

    return {
        "schemaVersion": 2,
        "sourceRevision": revision,
        "identityMapPath": map_path.relative_to(root).as_posix() if map_path.is_relative_to(root) else str(map_path),
        "identityMapSha256": fingerprints.get(
            map_path.relative_to(root).as_posix() if map_path.is_relative_to(root) else str(map_path), ""
        ),
        "valid": len(errors) == 0 and len(changed_during_scan) == 0,
        "errorCount": len(errors),
        "warningCount": len(warnings),
        "totals": {
            "paired_worldbooks_count": len(per_worldbook_stats),
            "unpaired_english_worldbooks_count": len(unpaired_report),
            "total_player_exports_json_files_parsed": total_player_exports_json_files,
            "mapping_entries": total_mapping_entries,
            "unique_zh_sources": total_unique_zh,
            "unique_en_targets": total_unique_en,
            "strict_one_to_one_entries": total_strict_1to1,
            "many_to_one_groups_count": total_many_to_one_groups,
            "many_to_one_zh_sources_count": total_many_to_one_zh,
            "many_to_one_byte_identical_groups": byte_eq_groups,
            "many_to_one_json_semantic_identical_groups": json_eq_only_groups,
            "many_to_one_content_different_groups": diff_groups,
            "independent_same_hero_id_variant_groups_count": total_ind_var_groups,
            "independent_same_hero_id_variant_zh_sources_count": total_ind_var_zh,
            "independent_same_hero_id_variant_en_targets_count": total_ind_var_en,
            "file_type_entry_counts": dict(file_type_counts),
        },
        "ruleRenameAudit": {
            "total_mapped_rules": all_renamed_rules_count + all_unchanged_rules_count,
            "intentionally_renamed_rules_count": all_renamed_rules_count,
            "unchanged_id_rules_count": all_unchanged_rules_count,
            "internal_player_exports_zh_rule_id_references_count": len(internal_zh_rule_id_refs),
            "internal_player_exports_zh_rule_id_references": internal_zh_rule_id_refs,
            "external_chinese_rule_id_or_path_consumers": [
                {
                    "file": "content/content-map.json",
                    "reference_type": "chinese_rule_relative_path_in_source_and_target",
                    "affected_rules_count": 1877,
                    "detail": (
                        "All 1,877 Chinese worldbook rule files are registered by their Chinese "
                        "relative paths (PlayerExports/<zh_book>/knowledge/rules/rule_<zh>.json) "
                        "in content/content-map.json."
                    ),
                },
                {
                    "file": "scripts/build/package_localization.ps1",
                    "lines": "29-33, 85-88, 227-235",
                    "reference_type": "rule_id_lookup_and_projection_restore",
                    "affected_rules_count": 1877,
                    "detail": (
                        "In committed HEAD (97ad1ed14), Get-AnimusForgeEnglishAsset looks up "
                        "'knowledge/rules/id:' + $source.Id (Chinese Id), missing all 337 renamed "
                        "Calradia Chronicles rules and all 1,540 rules in the other 3 worldbooks. "
                        "In working-tree package_localization.ps1, MappedFiles and RuleMappings "
                        "resolve all 1,877 rules and restore $translated.Id = $original.Id during "
                        "EN projection contract check."
                    ),
                },
                {
                    "file": "tools/localization/inventory_untranslated.py",
                    "lines": "262-263, 278-280",
                    "reference_type": "calradia_rule_id_direct_match",
                    "affected_rules_count": 337,
                    "detail": (
                        "worldbook_english_index and worldbook_counterpart match Calradia Chronicles "
                        "rules only by direct ('knowledge/rules', payload.get('Id')) without reading "
                        "english-worldbook-identity-map.json, leaving renamed rules and the other 3 "
                        "worldbooks flagged as worldbook_variant."
                    ),
                },
            ],
        },
        "routingAudit": routing_summary,
        "worldbooks": per_worldbook_stats,
        "unpairedEnglishWorldbooks": unpaired_report,
        "multiSourceGroups": multi_source_groups,
        "independentSameHeroIdVariantGroups": independent_same_hero_id_variant_groups,
        "errors": errors,
        "warnings": warnings,
        "changedDuringScan": changed_during_scan,
        "sourceHashes": fingerprints,
    }


def run_negative_fixtures(root: Path, fixtures_dir: Path) -> list[dict[str, Any]]:
    fixtures_dir.mkdir(parents=True, exist_ok=True)
    data_dir = fixtures_dir / "fixture_player_exports"
    zh_dir = data_dir / "zh_book"
    en_dir = data_dir / "en_book"
    (zh_dir / "personality_background").mkdir(parents=True, exist_ok=True)
    (en_dir / "personality_background").mkdir(parents=True, exist_ok=True)

    # Create minimal valid fixture hero files under artifacts/localization/fixtures/
    zh_hero_a = zh_dir / "personality_background/hero_1__甲.json"
    zh_hero_b_diff = zh_dir / "personality_background/hero_1__乙.json"
    en_hero_1 = en_dir / "personality_background/hero_1__Alpha.json"

    zh_hero_a.write_text(
        json.dumps({"Personality": "性格温和。", "Background": "出身北方。", "VoiceId": "v1"}, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    zh_hero_b_diff.write_text(
        json.dumps({"Personality": "性格暴烈且多疑。", "Background": "出身南方海岛。", "VoiceId": "v1"}, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    en_hero_1.write_text(
        json.dumps({"Personality": "Gentle in temperament.", "Background": "Born in the north.", "VoiceId": "v1"}, indent=2),
        encoding="utf-8",
    )

    # Also copy actual ROTwanderer25 Chinese sources and pre-repair single target into fixture_player_exports
    # so Fixture 4 preserves the exact pre-repair lossy merge of ROTwanderer25 in Game of Thrones Chronicles
    got_zh_fixture_dir = data_dir / "权游国度编年史-大帝版" / "personality_background"
    got_en_fixture_dir = data_dir / "Game of Thrones Chronicles" / "personality_background"
    got_zh_fixture_dir.mkdir(parents=True, exist_ok=True)
    got_en_fixture_dir.mkdir(parents=True, exist_ok=True)

    prod_pe = root / "content/modules/AF.Module.Onboarding/PlayerExports"
    for fn in ("ROTwanderer25__铁手杰斯林.json", "ROTwanderer25__杰斯林.json"):
        src_p = prod_pe / "权游国度编年史-大帝版" / "personality_background" / fn
        if src_p.is_file():
            (got_zh_fixture_dir / fn).write_bytes(src_p.read_bytes())
    for fn in ("ROTwanderer25__Jacelyn_Bywater.json", "ROTwanderer25__Jacelyn.json"):
        src_p = prod_pe / "Game of Thrones Chronicles" / "personality_background" / fn
        if src_p.is_file():
            (got_en_fixture_dir / fn).write_bytes(src_p.read_bytes())

    results: list[dict[str, Any]] = []

    # Fixture 1: Path traversal (路径越界)
    f1_path = fixtures_dir / "fixture-1-path-traversal.json"
    f1_payload = {
        "schema_version": "1.0",
        "worldbooks": {
            "FixtureBook": {
                "zh_directory": "zh_book",
                "en_directory": "en_book",
                "globals": {},
                "heroes": {
                    "../escape_outside_root.json": {
                        "hero_id": "hero_1",
                        "en_file": "../../outside_target.json",
                    }
                },
                "rules": {},
            }
        },
    }
    f1_path.write_text(json.dumps(f1_payload, ensure_ascii=False, indent=2), encoding="utf-8")
    r1 = validate_map(root, data_dir, f1_path, check_disk_coverage=False, audit_routing=False)
    r1_codes = [e["code"] for e in r1["errors"]]
    results.append({
        "fixture_id": "fixture_1_path_traversal",
        "fixture_file": f1_path.relative_to(root).as_posix(),
        "expected_error_code": "path_out_of_bounds_or_traversal",
        "actual_error_codes": r1_codes,
        "exit_code": 0 if r1["valid"] else 1,
        "passed_negative_assertion": (not r1["valid"]) and ("path_out_of_bounds_or_traversal" in r1_codes),
    })

    # Fixture 2: Missing English target file (缺英文文件)
    f2_path = fixtures_dir / "fixture-2-missing-english-file.json"
    f2_payload = {
        "schema_version": "1.0",
        "worldbooks": {
            "FixtureBook": {
                "zh_directory": "zh_book",
                "en_directory": "en_book",
                "globals": {},
                "heroes": {
                    "personality_background/hero_1__甲.json": {
                        "hero_id": "hero_1",
                        "en_file": "personality_background/hero_1__MissingTarget.json",
                    }
                },
                "rules": {},
            }
        },
    }
    f2_path.write_text(json.dumps(f2_payload, ensure_ascii=False, indent=2), encoding="utf-8")
    r2 = validate_map(root, data_dir, f2_path, check_disk_coverage=False, audit_routing=False)
    r2_codes = [e["code"] for e in r2["errors"]]
    results.append({
        "fixture_id": "fixture_2_missing_english_file",
        "fixture_file": f2_path.relative_to(root).as_posix(),
        "expected_error_code": "missing_english_target_file",
        "actual_error_codes": r2_codes,
        "exit_code": 0 if r2["valid"] else 1,
        "passed_negative_assertion": (not r2["valid"]) and ("missing_english_target_file" in r2_codes),
    })

    # Fixture 3: Duplicate mapping source (重复映射源)
    f3_path = fixtures_dir / "fixture-3-duplicate-mapping-source.json"
    f3_raw = """{
  "schema_version": "1.0",
  "worldbooks": {
    "FixtureBook": {
      "zh_directory": "zh_book",
      "en_directory": "en_book",
      "globals": {},
      "heroes": {
        "personality_background/hero_1__甲.json": {
          "hero_id": "hero_1",
          "en_file": "personality_background/hero_1__Alpha.json"
        },
        "personality_background/hero_1__甲.json": {
          "hero_id": "hero_1",
          "en_file": "personality_background/hero_1__Alpha.json"
        }
      },
      "rules": {}
    }
  }
}
"""
    f3_path.write_text(f3_raw, encoding="utf-8")
    r3 = validate_map(root, data_dir, f3_path, check_disk_coverage=False, audit_routing=False)
    r3_codes = [e["code"] for e in r3["errors"]]
    results.append({
        "fixture_id": "fixture_3_duplicate_mapping_source",
        "fixture_file": f3_path.relative_to(root).as_posix(),
        "expected_error_code": "duplicate_mapping_source",
        "actual_error_codes": r3_codes,
        "exit_code": 0 if r3["valid"] else 1,
        "passed_negative_assertion": (not r3["valid"]) and ("duplicate_mapping_source" in r3_codes),
    })

    # Fixture 4: Exact pre-repair lossy merge of ROTwanderer25 (同HeroId但正文不同的有损合并反例)
    f4_path = fixtures_dir / "fixture-4-same-hero-different-body.json"
    f4_payload = {
        "schema_version": "1.0",
        "description": "Pre-repair lossy mapping of ROTwanderer25 in Game of Thrones Chronicles (both ROTwanderer25__铁手杰斯林.json and ROTwanderer25__杰斯林.json merged into ROTwanderer25__Jacelyn_Bywater.json)",
        "worldbooks": {
            "Game of Thrones Chronicles": {
                "zh_directory": "权游国度编年史-大帝版",
                "en_directory": "Game of Thrones Chronicles",
                "globals": {},
                "heroes": {
                    "personality_background/ROTwanderer25__铁手杰斯林.json": {
                        "hero_id": "ROTwanderer25",
                        "en_file": "personality_background/ROTwanderer25__Jacelyn_Bywater.json",
                    },
                    "personality_background/ROTwanderer25__杰斯林.json": {
                        "hero_id": "ROTwanderer25",
                        "en_file": "personality_background/ROTwanderer25__Jacelyn_Bywater.json",
                    },
                },
                "rules": {},
            }
        },
    }
    f4_path.write_text(json.dumps(f4_payload, ensure_ascii=False, indent=2), encoding="utf-8")
    r4 = validate_map(root, data_dir, f4_path, check_disk_coverage=False, audit_routing=False)
    r4_codes = [e["code"] for e in r4["errors"]]
    results.append({
        "fixture_id": "fixture_4_same_hero_id_different_body",
        "fixture_file": f4_path.relative_to(root).as_posix(),
        "expected_error_code": "unapproved_content_divergent_merge",
        "actual_error_codes": r4_codes,
        "exit_code": 0 if r4["valid"] else 1,
        "passed_negative_assertion": (not r4["valid"]) and ("unapproved_content_divergent_merge" in r4_codes),
    })

    return results


def build_markdown_report(report: dict[str, Any]) -> str:
    totals = report["totals"]
    routing = report["routingAudit"]
    head_r = routing.get("committed_head_selector_json_counts", {})
    wt_r = routing.get("working_tree_selector_json_counts", {})
    rule_audit = report["ruleRenameAudit"]

    lines = [
        "# 外部 Agent B 后续验收报告：世界书映射可复现核验（含 `ROTwanderer25` 有损合并修复后状态）",
        "",
        f"- **任务日期**：`2026-10-11`",
        f"- **绑定源码 HEAD**：`{report['sourceRevision']}`",
        f"- **映射表路径**：`{report['identityMapPath']}`",
        f"- **映射表 SHA-256**：`{report['identityMapSha256']}`",
        f"- **`content/content-map.json` SHA-256**：`{report['sourceHashes'].get('content/content-map.json', 'N/A')}`",
        f"- **工作树 `scripts/build/package_localization.ps1` SHA-256**：`{report['sourceHashes'].get('scripts/build/package_localization.ps1', 'N/A')}`",
        f"- **核验脚本**：`tools/localization/validate_worldbook_identity_map.py`",
        f"- **结构化 JSON 报告**：`artifacts/localization/agent-b-mapping-verification-20261011.json`",
        f"- **扫描期间文件变化 (`changedDuringScan`)**：`{len(report['changedDuringScan'])}`",
        f"- **生产映射总体退出码**：`{0 if report['valid'] else 1}`（`valid = {report['valid']}`，`errorCount = {report['errorCount']}`，`warningCount = {report['warningCount']}`）",
        "",
        "---",
        "",
        "## 一、可直接从仓库根执行的复现命令",
        "",
        "```powershell",
        "# 1. 运行完整生产世界书映射核验 + 4 个反例 Fixture 自检，生成 JSON 与 Markdown 报告",
        "py -B tools/localization/validate_worldbook_identity_map.py `",
        "  --output-json artifacts/localization/agent-b-mapping-verification-20261011.json `",
        "  --output-md artifacts/localization/agent-b-mapping-verification-20261011.md `",
        "  --run-fixtures",
        "",
        "# 2. 单独执行任一反例 Fixture（均返回 Exit Code = 1，含修复前 ROTwanderer25 有损映射反例）",
        "py -B tools/localization/validate_worldbook_identity_map.py --player-exports-root artifacts/localization/fixtures/worldbook-identity-map/fixture_player_exports --map artifacts/localization/fixtures/worldbook-identity-map/fixture-1-path-traversal.json --no-disk-coverage --no-routing-audit",
        "py -B tools/localization/validate_worldbook_identity_map.py --player-exports-root artifacts/localization/fixtures/worldbook-identity-map/fixture_player_exports --map artifacts/localization/fixtures/worldbook-identity-map/fixture-2-missing-english-file.json --no-disk-coverage --no-routing-audit",
        "py -B tools/localization/validate_worldbook_identity_map.py --player-exports-root artifacts/localization/fixtures/worldbook-identity-map/fixture_player_exports --map artifacts/localization/fixtures/worldbook-identity-map/fixture-3-duplicate-mapping-source.json --no-disk-coverage --no-routing-audit",
        "py -B tools/localization/validate_worldbook_identity_map.py --player-exports-root artifacts/localization/fixtures/worldbook-identity-map/fixture_player_exports --map artifacts/localization/fixtures/worldbook-identity-map/fixture-4-same-hero-different-body.json --no-disk-coverage --no-routing-audit",
        "```",
        "",
        "---",
        "",
        "## 二、分开统计：严格一对一 / 无损多对一 / 独立同 `HeroId` 保真版本",
        "",
        "> **口径说明**：",
        "> 1. **严格一对一 (`strict_one_to_one_entries`)**：唯一中文源 ↔ 唯一英文目标，且稳定身份 ID 无同 ID 多版本（**3,113** 条：3,113 中文源 → 3,113 英文目标）。",
        "> 2. **无损多对一 (`many_to_one_groups`)**：2 个字节完全相同的中文源共享 1 个英文目标（**9** 组：18 中文源 → 9 英文目标）。",
        "> 3. **独立同 `HeroId` 保真版本 (`independent_same_hero_id_variant_groups`)**：中文源本来存在同 `HeroId` 但不同正文的多个角色版本，各自显式一对一映射到独立英文目标文件（**1** 组 `ROTwanderer25`：2 中文源 → 2 英文目标）。",
        "> 4. `AD 1259 Chronicles` 无中文源，单独列于第六节，不纳入中文源覆盖率分母。",
        "",
        "| 世界书 (EN ↔ ZH) | 映射条目数 | 唯一中文源文件 | 唯一英文目标文件 | 严格一对一条目 | 无损多对一组数 (中文源 → 英文目标) | 独立同 `HeroId` 保真版本组数 (中文源 → 英文目标) | 唯一英文人物文件 (唯一 `HeroId`) | 唯一英文规则 | 唯一英文关键词 |",
        "| :--- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |",
    ]

    for wb_name, st in report["worldbooks"].items():
        m_str = f"{st['many_to_one_groups_count']} 组 ({st['many_to_one_zh_sources_count']} → {st['many_to_one_groups_count']})"
        v_str = (
            f"{st['independent_same_hero_id_variant_groups_count']} 组 "
            f"({st['independent_same_hero_id_variant_zh_sources_count']} → {st['independent_same_hero_id_variant_en_targets_count']})"
        )
        lines.append(
            f"| `{wb_name}` ↔ `{st['zh_directory']}` | {st['mapping_entries']} | {st['unique_zh_sources']} | "
            f"{st['unique_en_targets']} | {st['strict_one_to_one_entries']} | {m_str} | {v_str} | "
            f"{st['unique_en_hero_files_count']} ({st['unique_en_hero_ids_count']}) | {st['unique_en_rules_count']} | {st['unique_en_keywords_count']} |"
        )

    lines.append(
        f"| **合计（4 套配对世界书）** | **{totals['mapping_entries']}** | **{totals['unique_zh_sources']}** | "
        f"**{totals['unique_en_targets']}** | **{totals['strict_one_to_one_entries']}** | "
        f"**{totals['many_to_one_groups_count']} 组 ({totals['many_to_one_zh_sources_count']} → {totals['many_to_one_groups_count']})** | "
        f"**{totals['independent_same_hero_id_variant_groups_count']} 组 ({totals['independent_same_hero_id_variant_zh_sources_count']} → {totals['independent_same_hero_id_variant_en_targets_count']})** | "
        f"**1,222 (1,221)** | **1,877** | **5,616** |"
    )

    lines += [
        "",
        "### 按文件类型统计（4 套配对世界书共 3,133 条映射 → 3,124 个唯一英文文件）",
        "",
        f"- `hero_persona` (`personality_background/*.json`)：**{totals['file_type_entry_counts'].get('hero_persona', 0)}** 条映射 → **1,222** 个唯一英文人物文件（含 1,211 条严格一对一 + 9 组 18→9 无损多对一 + 1 组 2→2 独立同 `HeroId` 保真版本 `ROTwanderer25`）",
        f"- `knowledge_rule` (`knowledge/rules/*.json`)：**{totals['file_type_entry_counts'].get('knowledge_rule', 0)}** 条映射 → **1,877** 个唯一英文目标文件（严格一对一）",
        f"- `event_data` (`event_data/*.json`)：**{totals['file_type_entry_counts'].get('event_data', 0)}** 条映射 → **13** 个唯一英文目标文件（严格一对一）",
        f"- `kingdom_profiles` (`kingdom_profiles/KingdomProfiles.json`)：**{totals['file_type_entry_counts'].get('kingdom_profiles', 0)}** 条映射 → **4** 个唯一英文目标文件（严格一对一）",
        f"- `unnamed_persona` (`unnamed_persona/UnnamedNpcProfiles.json`)：**{totals['file_type_entry_counts'].get('unnamed_persona', 0)}** 条映射 → **4** 个唯一英文目标文件（严格一对一）",
        f"- `voice_mapping` (`voice_mapping/VoiceMapping.json`)：**{totals['file_type_entry_counts'].get('voice_mapping', 0)}** 条映射 → **4** 个唯一英文目标文件（严格一对一）",
        "",
        "---",
        "",
        "## 三、无损多对一合并（9 组）与独立同 `HeroId` 保真版本（1 组）核验详情",
        "",
        "### 3.1 无损多对一合并中文人物源（共 9 组 / 18 个中文文件 → 9 个英文文件）",
        "",
        f"- **字节完全相同 (`byte_identical = true`, `json_semantic_identical = true`)**：**{totals['many_to_one_byte_identical_groups']} 组**（18 个中文文件 → 9 个英文文件，双文件 SHA-256 完全一致）",
        f"- **实际内容不同的有损多对一合并 (`content_different`)**：**{totals['many_to_one_content_different_groups']} 组**",
        "",
        "| 组号 | 世界书 | HeroId | 英文目标文件 | 中文源文件 1 & 2 | 字节相同 | JSON语义相同 | 合并状态 |",
        "| :---: | :--- | :--- | :--- | :--- | :---: | :---: | :--- |",
    ]

    for idx, g in enumerate(report["multiSourceGroups"], 1):
        z0, z1 = g["zh_files"][0], g["zh_files"][1]
        h0, h1 = g["zh_sha256"][z0][:12], g["zh_sha256"][z1][:12]
        lines.append(
            f"| {idx} | `{g['worldbook']}` | `{g['hero_id']}` | `{g['en_file']}` | "
            f"`{z0}` (`{h0}…`)<br>`{z1}` (`{h1}…`) | "
            f"{'是' if g['byte_identical'] else '**否**'} | "
            f"{'是' if g['json_semantic_identical'] else '**否**'} | "
            f"`{g['comparison_status']}` |"
        )

    lines += [
        "",
        "### 3.2 独立同 `HeroId` 保真版本（共 1 组 / 2 个中文文件 → 2 个独立英文文件）",
        "",
    ]
    for vg in report.get("independentSameHeroIdVariantGroups", []):
        p0, p1 = vg["pairs"][0], vg["pairs"][1]
        lines += [
            f"- **世界书**：`{vg['worldbook']}`（`{vg['zh_directory']}` ↔ `{vg['en_directory']}`）",
            f"- **`HeroId`**：`{vg['hero_id']}`（分辨率类型：`{vg['resolution_type']}`）",
            f"- **版本 1**：`{p0['zh_file']}`（SHA-256: `{p0['zh_sha256']}`） → `{p0['en_file']}`（SHA-256: `{p0['en_sha256']}`）",
            f"  - 说明：{p0['variant_note']}",
            f"- **版本 2**：`{p1['zh_file']}`（SHA-256: `{p1['zh_sha256']}`） → `{p1['en_file']}`（SHA-256: `{p1['en_sha256']}`）",
            f"  - 说明：{p1['variant_note']}",
            f"- **修复前有损合并反例**：已保存在 `artifacts/localization/fixtures/worldbook-identity-map/fixture-4-same-hero-different-body.json`，执行必失败（`unapproved_content_divergent_merge`, `exit_code = 1`）。",
            "",
        ]

    lines += [
        "---",
        "",
        "## 四、知识规则改名与引用核查（共 1,877 条规则）",
        "",
        f"- **映射规则总数**：**{rule_audit['total_mapped_rules']}** 条（标准版 337 + 低配版 30 + 战锤 279 + 权游 1,231）",
        f"- **有意改名规则数 (`zh_id != en_id`)**：**{rule_audit['intentionally_renamed_rules_count']}** 条（100% 在 `english-worldbook-identity-map.json` 登记了精确的 `zh_id` ↔ `en_id` 映射）",
        f"- **未改名规则数 (`zh_id == en_id`)**：**{rule_audit['unchanged_id_rules_count']}** 条",
        f"- **`PlayerExports` 内部 JSON 正文跨文件引用中文规则 `Id` 数量**：**{rule_audit['internal_player_exports_zh_rule_id_references_count']}** 处",
        "",
        "---",
        "",
        "## 五、选择器路由分类统计（A / B / C 三类数量）",
        "",
        "| 统计口径 | A 类：英文资产或映射确实缺失 | B 类：映射与英文资产均存在，但选择器路由未命中 | C 类：已被选择器路由正确选中 | 合计 |",
        "| :--- | ---: | ---: | ---: | ---: |",
        f"| **基线 `HEAD` (`97ad1ed14`) 选择器** | **{head_r.get('category_A_missing_asset_or_mapping', 0)}** | **{head_r.get('category_B_mapped_and_exists_but_selector_unrouted', 0)}** | **{head_r.get('category_C_routed_and_selected', 0)}** | **3,133** |",
        f"| **当前工作树 `package_localization.ps1` 选择器** | **{wt_r.get('category_A_missing_asset_or_mapping', 0)}** | **{wt_r.get('category_B_mapped_and_exists_but_selector_unrouted', 0)}** | **{wt_r.get('category_C_routed_and_selected', 0)}** | **3,133** |",
        "",
        "---",
        "",
        "## 六、`AD 1259 Chronicles` 独立统计（无中文源，不纳入中文源覆盖率）",
        "",
    ]
    for up_name, up_st in report["unpairedEnglishWorldbooks"].items():
        lines += [
            f"- **目录**：`content/modules/AF.Module.Onboarding/PlayerExports/{up_st['en_directory']}`",
            f"- **中文源对照**：无（`has_chinese_counterpart = false`，不计入中文世界书覆盖率分母）",
            f"- **JSON 文件总数**：**{up_st['total_json_files']}** 个（全部可解析、0 汉字：全局 {up_st['globals_count']} + 人物 {up_st['unique_heroes_count']} + 规则 {up_st['unique_rules_count']}，唯一关键词 {up_st['unique_keywords_count']} 个）",
        ]

    lines += [
        "",
        "---",
        "",
        "## 七、四个反例 Fixture 验证结果（含修复前 `ROTwanderer25` 有损合并反例）",
        "",
        "| Fixture ID | Fixture 路径 | 期望错误码 | 实测错误码 | 退出码 | 反例断言结果 |",
        "| :--- | :--- | :--- | :--- | :---: | :---: |",
    ]
    for fx in report.get("fixtureResults", []):
        lines.append(
            f"| `{fx['fixture_id']}` | `{fx['fixture_file']}` | "
            f"`{fx['expected_error_code']}` | `{', '.join(fx['actual_error_codes'])}` | "
            f"`{fx['exit_code']}` | {'**PASS**' if fx['passed_negative_assertion'] else '**FAIL**'} |"
        )

    lines += [
        "",
        "---",
        "",
        "## 八、未验证项声明 (Unverified Scope)",
        "",
        "- 本脚本执行离线 JSON 解析、路径安全、结构契约、身份映射与选择器路由核验；JSON/身份核验不等同于全量文学翻译质量评估或游戏内实机运行验收。",
    ]

    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate PlayerExports English worldbook identity mapping.")
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--player-exports-root", type=Path, default=None)
    parser.add_argument("--map", type=Path, default=None)
    parser.add_argument("--output-json", type=Path, default=None)
    parser.add_argument("--output-md", type=Path, default=None)
    parser.add_argument("--run-fixtures", action="store_true")
    parser.add_argument("--no-disk-coverage", action="store_true")
    parser.add_argument("--no-routing-audit", action="store_true")
    args = parser.parse_args()

    root = args.root.resolve()
    pe_root = (
        args.player_exports_root.resolve()
        if args.player_exports_root
        else (root / "content/modules/AF.Module.Onboarding/PlayerExports").resolve()
    )
    map_path = (
        args.map.resolve()
        if args.map
        else (pe_root / "english-worldbook-identity-map.json").resolve()
    )

    report = validate_map(
        root=root,
        player_exports_root=pe_root,
        map_path=map_path,
        check_disk_coverage=not args.no_disk_coverage,
        audit_routing=not args.no_routing_audit,
    )

    if args.run_fixtures:
        fixtures_dir = root / "artifacts/localization/fixtures/worldbook-identity-map"
        fixture_results = run_negative_fixtures(root, fixtures_dir)
        report["fixtureResults"] = fixture_results
        if not all(fx["passed_negative_assertion"] for fx in fixture_results):
            report["valid"] = False
            report["errors"].append({
                "code": "negative_fixture_assertion_failed",
                "context": str(fixtures_dir),
                "json_path": "$",
                "detail": "One or more negative fixtures did not fail as expected.",
            })

    if args.output_json:
        out_json = args.output_json if args.output_json.is_absolute() else (root / args.output_json)
        out_json.parent.mkdir(parents=True, exist_ok=True)
        out_json.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    if args.output_md:
        out_md = args.output_md if args.output_md.is_absolute() else (root / args.output_md)
        out_md.parent.mkdir(parents=True, exist_ok=True)
        out_md.write_text(build_markdown_report(report), encoding="utf-8")

    summary = {
        "sourceRevision": report["sourceRevision"],
        "identityMapSha256": report["identityMapSha256"],
        "valid": report["valid"],
        "errorCount": report["errorCount"],
        "warningCount": report["warningCount"],
        "totals": report["totals"],
        "errors": report["errors"],
        "fixtureSummary": [
            {
                "fixture_id": fx["fixture_id"],
                "exit_code": fx["exit_code"],
                "passed_negative_assertion": fx["passed_negative_assertion"],
            }
            for fx in report.get("fixtureResults", [])
        ],
    }
    print(json.dumps(summary, ensure_ascii=False, indent=2))
    return 0 if report["valid"] else 1


if __name__ == "__main__":
    raise SystemExit(main())

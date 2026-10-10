"""Read-only inventory of Chinese copy without an evidenced English path.

Run with --output artifacts/localization/untranslated-only-current.json.
This is a lexical audit, not a C# semantic analysis or live-game acceptance.
Only direct UI arguments and separately traced display projections are confirmed.
"""
from __future__ import annotations

import argparse
import bisect
import collections
import hashlib
import json
import re
import subprocess
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / "tools/localization"))
from extract_localization_inventory import HAN, ROOTS, included, owner_for
from resource_catalog import registered_pairs
from build_remaining_work_packages import package_for

LEX = re.compile(
    r'(?P<comment>//[^\n]*|/\*[\s\S]*?\*/)'
    r'|(?P<string>(?:\$@|@\$|@)"(?:""|[^"])*"|\$?"(?:\\[\s\S]|[^"\\])*")'
    r"|(?P<char>'(?:\\.|[^'\\])')"
)
ESCAPE = re.compile(r'\\(?:u([0-9a-fA-F]{4})|U([0-9a-fA-F]{8})|x([0-9a-fA-F]{1,4})|(.))')
ID = re.compile(r'\{=([^}]+)\}')
SINKS = {
    "InquiryData": None, "TextInquiryData": None,
    "MultiSelectionInquiryData": None, "InquiryElement": {1},
    "InformationMessage": {0}, "TextObject": {0},
    "ShowFailurePopup": {0, 1}, "ShowRetryPrompt": {0},
    "NonBlockingErrorReport.Show": {0, 1},
    "AppendDevNpcActionField": {1}, "AppendDevField": {1},
    "AddGameMenu": {1}, "AddGameMenuOption": {2},
}
# Consumers verified in DeveloperRootEditorController:648,727-728 and
# WeeklyReportEditorController:683-702. These helpers only construct UI projections.
PROJECTIONS = {"NpcActionEditorProjection.cs", "WeeklyEditorProjection.cs"}
INTERNAL = re.compile(r'\b(?:IndexOf|StartsWith|EndsWith|Contains|Equals|Replace|Regex|Log|Trace|LogDebug|LogError|LogWarning)\b')
UI_CONTEXT = re.compile(r'Inquiry|InformationMessage|DisplayMessage|Show.*(?:Popup|Editor)|Title|Subtitle|Hint|Tooltip|Button|Notification|StatusText|DisplayName|SettingProperty|TextObject|AppendDev.*Field|MenuOption', re.I)
PROMPT_CONTEXT = re.compile(r'prompt|instruction|afef|constraint|ruletemplate|tagtemplate', re.I)
TAB_LABELS = {"全部", "AI核心", "场景互动", "角色与同伴", "决斗与部队", "世界与事件", "高级与开发"}

# Traced to TerminalUiModels selectors, courier theme dropdown, and retry/error
# analysis display boundaries. Values may also be internal; translate projection only.
REVIEWED = {
    "DuelSettings.cs": {"*手动填写*", "黑色透明", "白色透明", "粉色透明", "关闭", "每次启动", "每30分钟", "每1小时", "每6小时", "每天", "每3天", "每1星期", "CPU（默认）", "少量", "适中", "较多", "紧凑（约15天）", "标准（约21天）", "从容（约28天）", "主链路", "前处理", "后处理", "周报", "(空)"},
    "CourierLetterThemes.cs": {"瓦兰迪亚", "北帝国", "西帝国", "南帝国", "斯特吉亚", "巴旦尼亚", "库塞特", "阿塞莱", "诺德"},
    "LlmRetryPrompt.cs": {"LLM请求", "失败。详细错误已显示在左下角消息并写入日志。\n\n是否立即重试？", "AnimusForge 请求失败", "未知错误", "失败：\n\n", "重试", "放弃"},
    "NonBlockingErrorReport.cs": {"AnimusForge 操作失败"},
    "AiErrorAnalysisInquiry.cs": {"无法读取 MCM 前处理 API 配置。", "前处理 API 地址、密钥或模型名未配置完整。", "前处理 API 返回 HTTP ", "前处理 API 请求失败：", "前处理 API 已响应，但没有解析出可用的模型回复。", "未生成分析结果。", "前处理 API 分析超过 60 秒，已取消本次请求。", "AI 报错分析 - ", "AnimusForge 报错", "前处理 API 未能完成报错分析。\n\n【分析请求错误】\n", "\n\n【原报错】\n", "AI 分析失败"},
}


def decode(raw: str) -> str:
    prefix = raw[:raw.index('"')]
    value = raw[raw.index('"') + 1:-1]
    if "@" in prefix:
        return value.replace('""', '"')
    controls = {"n": "\n", "r": "\r", "t": "\t", "0": "\0"}
    return ESCAPE.sub(lambda m: chr(int(next(x for x in m.groups()[:3] if x), 16))
                      if any(m.groups()[:3]) else controls.get(m[4], m[4]), value)


def tokenize(source: str):
    matches = list(LEX.finditer(source))
    masked = list(source)
    for match in matches:
        masked[match.start():match.end()] = ["\n" if c == "\n" else " " for c in match[0]]
    code = "".join(masked)
    stack = []
    frames = {}
    starts = {m.start() for m in matches if m.lastgroup == "string"}
    for offset, char in enumerate(code):
        if offset in starts:
            frames[offset] = [(f[1], f[2], f[3]) for f in stack if f[0] == "("]
        if char in "([{":
            callee = ""
            if char == "(":
                found = re.search(r'([\w.]+)\s*$', code[max(0, offset-160):offset])
                callee = found[1] if found else ""
            stack.append([char, callee, 0, offset])
        elif char in ")]}":
            if stack and stack[-1][0] == {")": "(", "]": "[", "}": "{"}[char]:
                stack.pop()
        elif char == "," and stack and stack[-1][0] == "(":
            stack[-1][2] += 1
    return code, [(m, frames.get(m.start(), [])) for m in matches if m.lastgroup == "string"]


def module(path: Path, root: Path) -> str:
    relative = path.relative_to(root).as_posix()
    found = re.search(r'AF\.Module\.([^/]+)/', relative)
    if found:
        return found[1]
    if relative.startswith("src/AF.GameAdapter.Bannerlord/"):
        return "GameAdapter/" + relative.split("/")[2]
    return owner_for(path, root)


def json_strings(value, pointer=""):
    if isinstance(value, str):
        yield pointer, value
    elif isinstance(value, dict):
        for key, child in value.items():
            yield from json_strings(child, pointer + "/" + key.replace("~", "~0").replace("/", "~1"))
    elif isinstance(value, list):
        for i, child in enumerate(value):
            yield from json_strings(child, pointer + "/" + str(i))


def worldbook_english_index(root: Path):
    base = root / "content/modules/AF.Module.Onboarding/PlayerExports/Calradia Chronicles"
    index = {}
    for path in sorted(base.rglob("*.json")):
        relative = path.relative_to(base)
        try:
            payload = json.loads(path.read_text(encoding="utf-8-sig"))
        except (UnicodeDecodeError, json.JSONDecodeError):
            continue
        if any(HAN.search(value) for _, value in json_strings(payload)):
            continue
        if relative.parts[0] == "personality_background":
            key = (relative.parts[0], path.stem.split("__", 1)[0])
        elif relative.parts[:2] == ("knowledge", "rules"):
            key = ("knowledge/rules", payload.get("Id")) if isinstance(payload, dict) else None
        else:
            key = ("path", relative.as_posix())
        if key:
            index.setdefault(key, []).append(path)
    return index


def worldbook_counterpart(path: Path, root: Path, index):
    base = root / "content/modules/AF.Module.Onboarding/PlayerExports/卡拉迪亚编年史"
    if not path.is_relative_to(base):
        return None
    relative = path.relative_to(base)
    if relative.parts[0] == "personality_background":
        key = (relative.parts[0], path.stem.split("__", 1)[0])
    elif relative.parts[:2] == ("knowledge", "rules"):
        payload = json.loads(path.read_text(encoding="utf-8-sig"))
        key = ("knowledge/rules", payload.get("Id")) if isinstance(payload, dict) else None
    else:
        key = ("path", relative.as_posix())
    matches = index.get(key, [])
    return matches[0] if len(matches) == 1 else None


def run(root: Path):
    en_keys = {}
    pairs = registered_pairs(root)
    worldbook_index = worldbook_english_index(root)
    for en, zh in pairs:
        for node in ET.parse(en).getroot().iter("string"):
            if not HAN.search(node.get("text", "")):
                en_keys[node.get("id")] = node.get("text", "")
    terminal = root / "src/AF.GameAdapter.Bannerlord/UI/Terminal/TerminalSettingsLocalization.cs"
    terminal_source = terminal.read_text(encoding="utf-8-sig")
    terminal_ids = set(re.findall(r'"(\w+)"', terminal_source.split("EnglishTitles", 1)[0]))
    excluded = collections.Counter()
    rows, assets, problems, fingerprints = [], [], [], {}
    sources = sorted({p for name in ROOTS for p in (root / name).rglob("*")
                      if p.is_file() and included(p) and not any(part.lower() == "tests" for part in p.parts)
                      and p.suffix.lower() in {".cs", ".xml", ".json", ".xaml"}})
    for path in sources:
        relative = path.relative_to(root).as_posix()
        if "/design/" in relative or relative.startswith("AnimusForge/PlayerExports/"):
            # Not included in the unified content map/module source set.
            continue
        data = path.read_bytes()
        fingerprints[relative] = hashlib.sha256(data).hexdigest()
        try:
            source = data.decode("utf-8-sig")
        except UnicodeDecodeError:
            problems.append({"file": relative, "reason": "not_utf8"})
            continue
        if path.suffix == ".json":
            if relative == "content/content-map.json":
                continue
            try:
                payload = json.loads(source)
            except json.JSONDecodeError as exc:
                problems.append({"file": relative, "reason": str(exc)})
                continue
            chinese = [(pointer, value) for pointer, value in json_strings(payload) if HAN.search(value)]
            counterpart = worldbook_counterpart(path, root, worldbook_index)
            if counterpart:
                excluded["verified_calradia_original_files"] += 1
                continue
            if re.search(r'\.en(?:-US)?\.json$', path.name) or "/Calradia Chronicles/" in relative:
                # Two protocol templates deliberately retain Chinese tokens.
                unexpected = [(k, v) for k, v in chinese if HAN.search(v.replace("[ACTION:SCENE_ACT:动作程序]", "").replace("[RELAY:接力编号]", ""))]
                if unexpected:
                    problems.append({"file": relative, "reason": "Chinese remains in English asset", "values": len(unexpected)})
                excluded["english_asset_protocol_tokens"] += len(chinese)
                continue
            if path.name.endswith(".zh-CN.json"):
                excluded["retained_chinese_backup"] += len(chinese)
                continue
            counterparts = [path.with_name(path.stem + suffix) for suffix in (".en.json", ".en-US.json")]
            counterpart = next((p for p in counterparts if p.is_file()), None)
            if counterpart:
                try:
                    english = json.loads(counterpart.read_text(encoding="utf-8-sig"))
                    unexpected = [(p, v) for p, v in json_strings(english) if HAN.search(
                        v.replace("[ACTION:SCENE_ACT:动作程序]", "").replace("[RELAY:接力编号]", ""))]
                    if not unexpected:
                        excluded["paired_prompt_or_content"] += len(chinese)
                        continue
                except (UnicodeDecodeError, json.JSONDecodeError) as exc:
                    problems.append({"file": counterpart.relative_to(root).as_posix(), "reason": str(exc)})
            if chinese:
                if path.name == "TownAmbientDialogue.json":
                    internal_values = [(p, v) for p, v in chinese if re.search(r'/(?:Memory|ReplyHints)(?:/|$)', p)]
                    excluded["ambient_memory_or_prompt"] += len(internal_values)
                    chinese = [(p, v) for p, v in chinese if not re.search(r'/(?:Memory|ReplyHints)(?:/|$)', p)]
                    if not chinese:
                        continue
                assets.append({"file": relative, "module": module(path, root),
                               "chineseValueCount": len(chinese), "status": "no_parallel_english_file_found",
                               "kind": "worldbook_variant" if "/PlayerExports/" in relative else "content_or_config_review",
                               "examples": [{"pointer": p, "text": v[:240]} for p, v in chinese[:3]]})
            continue
        if path.suffix in {".xml", ".xaml"}:
            try:
                tree = ET.fromstring(source)
            except ET.ParseError as exc:
                problems.append({"file": relative, "reason": str(exc)})
                end = source.rfind("</Prefab>") + len("</Prefab>")
                if source[end:].strip() == "\\n":
                    # Audit the actual complete XML tree, recording the invalid
                    # trailing bytes. Never repair the product file in this task.
                    tree = ET.fromstring(source[:end])
                    problems[-1]["recovery"] = "attributes scanned after ignoring trailing literal backslash-n in memory"
                else:
                    continue
            for node in tree.iter():
                for attr, value in node.attrib.items():
                    if not HAN.search(value):
                        continue
                    key = node.get("id")
                    ids = ID.findall(value)
                    if "/Languages/" in relative and key in en_keys:
                        excluded["paired_chinese_xml"] += 1
                        continue
                    if ids and all(i in en_keys for i in ids):
                        excluded["localized_xml_reference"] += 1
                        continue
                    if "/GUI/Prefabs/" in relative and attr == "Text" and not value.startswith("@"):
                        rows.append({"file": relative, "line": None, "xmlElement": node.tag,
                                     "attribute": attr, "module": module(path, root), "text": value,
                                     "status": "confirmed_ui", "evidence": "literal prefab Text attribute"})
                    elif "/Languages/" in relative and attr == "text":
                        rows.append({"file": relative, "line": None, "module": module(path, root),
                                     "text": value, "status": "consumer_review", "evidence": "resource lacks registered English text", "id": key})
            continue
        if not HAN.search(source) and not re.search(r'\\[uUx]', source):
            continue
        code, tokens = tokenize(source)
        lines = source.splitlines()
        newlines = [m.start() for m in re.finditer("\n", source)]
        for match, frames in tokens:
            value = decode(match[0])
            if not HAN.search(value):
                continue
            left = max(code.rfind(";", 0, match.start()), code.rfind("{", 0, match.start()), code.rfind("}", 0, match.start())) + 1
            right = code.find(";", match.end())
            right = right if right >= 0 else len(code)
            statement = code[left:right]
            line = bisect.bisect_left(newlines, match.start()) + 1
            ids = ID.findall(value)
            if ids and all(i in en_keys for i in ids):
                excluded["localized_source_reference"] += 1
                continue
            nearest = frames[-1] if frames else ("", -1, 0)
            calls = [f[0] for f in frames]
            if any(re.search(r'(?:Logger|Log|Trace|Debug)\.', call) for call in calls):
                excluded["logs"] += 1
                continue
            if INTERNAL.search(nearest[0]):
                excluded["internal_matching"] += 1
                continue
            if path.name == "TerminalSettingsRegistry.cs":
                frame = next((f for f in reversed(frames) if f[0] == "TerminalSettingDef"), None)
                if frame:
                    argument_source = source[frame[2]+1:match.start()]
                    id_match = re.match(r'\s*"(\w+)"', argument_source)
                    if id_match and frame[1] in {2, 3, 4} and id_match[1] in terminal_ids:
                        part = {2: "Group", 3: "Title", 4: "Hint"}[frame[1]]
                        key = "AF_TERMINAL_UI_" + id_match[1] + "_" + part
                        if key in en_keys:
                            excluded["terminal_mapped_display"] += 1
                            continue
                    if frame[1] == 1:
                        excluded["terminal_category_routing"] += 1
                        continue
            callee, arg, _ = nearest
            name = callee.split(".")[-1]
            sink = callee if callee in SINKS else name
            confirmed = sink in SINKS and (SINKS[sink] is None or arg in SINKS[sink])
            evidence = "direct UI argument: " + callee + " argument " + str(arg)
            if name.startswith("SettingProperty"):
                confirmed, evidence = True, "MCM display attribute"
            if path.name in PROJECTIONS:
                confirmed, evidence = True, "traced display projection; consumers recorded in scanner"
            if value in REVIEWED.get(path.name, set()):
                confirmed, evidence = True, "reviewed selector or error UI display boundary; preserve internal value"
            if path.name in {"LlmRetryPrompt.cs", "AiErrorAnalysisInquiry.cs"} and value == "未知错误":
                # This fallback also appears in protocol/diagnostic helpers.
                display_context = "".join(calls)
                if not any(t in display_context for t in ("Show", "Retry", "NormalizeFullText")) and line > 415:
                    confirmed = False
            if not confirmed and (PROMPT_CONTEXT.search(statement) or PROMPT_CONTEXT.search(path.stem)):
                # LlmRetryPrompt also contains UI calls; only its direct UI arguments are confirmed.
                excluded["prompt_or_protocol"] += 1
                continue
            if not confirmed and not ("/UI/" in relative or UI_CONTEXT.search(statement)):
                excluded["non_ui_or_internal"] += 1
                continue
            rows.append({"file": relative, "line": line, "column": match.start() - (source.rfind("\n", 0, match.start()) + 1) + 1,
                         "module": module(path, root), "text": value,
                         "status": "confirmed_ui" if confirmed else "consumer_review",
                         "evidence": evidence if confirmed else "Chinese literal near UI; trace consumer before translating",
                         "sourceLine": lines[line-1].strip(), "hasEnglishResourceWithSameChineseCopy": False})
    # Identical Chinese text in a resource does not prove the caller resolves it.
    zh_values = {n.get("text") for _, zh in pairs for n in ET.parse(zh).getroot().iter("string")}
    for row in rows:
        row["hasEnglishResourceWithSameChineseCopy"] = row["text"] in zh_values
    statuses = collections.Counter(r["status"] for r in rows)
    packages = {}
    for name in ("A", "B", "C"):
        packages[name] = dict(collections.Counter(r["status"] for r in rows if package_for(r["file"]) == name))
    modules = {}
    for name in sorted({r["module"] for r in rows}):
        selected = [r for r in rows if r["module"] == name]
        modules[name] = dict(collections.Counter(r["status"] for r in selected))
    revision = subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=root, text=True).strip()
    changed = []
    for relative, digest in fingerprints.items():
        if hashlib.sha256((root / relative).read_bytes()).hexdigest() != digest:
            changed.append(relative)
    return {"schemaVersion": 1, "sourceRevision": revision, "root": str(root),
            "basis": "working tree, including untracked English assets",
            "meaning": "literal occurrences, not unique phrases or a complete semantic/UI count; confirmed is a lower bound",
            "summary": dict(statuses), "files": len({r["file"] for r in rows}),
            "registeredResourcePairs": len(pairs), "modules": modules, "packages": packages,
            "excluded": dict(excluded), "records": rows, "assetsWithoutParallelEnglish": assets,
            "problems": problems, "changedDuringScan": changed,
            "sourceHashes": fingerprints}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[2])
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()
    report = run(args.root.resolve())
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    confirmed = [r for r in report["records"] if r["status"] == "confirmed_ui"]
    review = [r for r in report["records"] if r["status"] == "consumer_review"]
    assets = report["assetsWithoutParallelEnglish"]
    md = ["# 尚无英文显示路径盘点", "", "阶段：验证。只读扫描工作树（包括未提交和未跟踪的英文资产）。", "",
          "源码 HEAD：`" + report["sourceRevision"] + "`。完整 JSON 含文件 hash、行号、原文和分类证据。", "",
          "确认 UI：**" + str(len(confirmed)) + " 处中文字符串出现位置**，涉及 " + str(len({r["file"] for r in confirmed})) + " 个文件；原文去重 " + str(len({r["text"] for r in confirmed})) + " 项。",
          "此数为直接 UI 参数、已追踪显示投影及人工复核下拉值的下限，不是完整词条数；拼接片段和重复使用按位置计数。", "",
          "待消费者审查：" + str(len(review)) + " 处；不是确认未翻译数量，不参与上面的总数。", "",
          "排除已有英文对应的中文 XML、逐文件核实对应的中文世界书、平行 Prompt、注释、日志、协议和内部匹配。", "",
          "已注册中英资源：" + str(report["registeredResourcePairs"]) + " 对。英文资产存在与运行时自动加载是两个状态，本轮不加入语言探测。", "",
          "## 分工范围", "", "| 包 | 确认 UI 位置 | 待消费者审查 |", "| --- | ---: | ---: |"]
    md.extend("| " + name + " | " + str(part.get("confirmed_ui", 0)) + " | " + str(part.get("consumer_review", 0)) + " |" for name, part in report["packages"].items())
    md += ["", "## 尚无可验证英文对应的世界书", "", "仅英文目录存在不证明全套翻译。按相对路径、人物稳定 ID 前缀、规则 JSON Id 匹配；英文规则 Id 改名且无映射时保持待核查，不猜对应。未计入 UI 总数。", "", "| 目录 | 含中文 JSON 文件 |", "| --- | ---: |"]
    counts = collections.Counter(a["file"].split("/PlayerExports/", 1)[1].split("/", 1)[0] for a in assets if a["kind"] == "worldbook_variant")
    md.extend("| " + name + " | " + str(count) + " |" for name, count in counts.most_common())
    md += ["", "## 确认 UI 逐条记录", "", "| 文件 | 行号 | 原文 | 证据 |", "| --- | ---: | --- | --- |"]
    md.extend("| `" + r["file"] + "` | " + str(r["line"]) + " | " + r["text"].replace("\n", "\\n").replace("\r", "\\r").replace("|", "\\|") + " | " + r["evidence"] + " |" for r in confirmed)
    md += ["", "## 扫描限制与未验证", "", "未构建、未部署、未实机验证。仅创建忽略的本地盘点文件，没有修改产品代码。", "",
           "不支持完整 C# 数据流；候选仍须追踪消费者。以下文件解析失败或扫描期间变化，完整结果不能宣称覆盖它们：", ""]
    md.extend("- `" + p["file"] + "`：" + p["reason"] for p in report["problems"])
    md.extend("- 扫描期间变化：`" + p + "`" for p in report["changedDuringScan"])
    args.output.with_suffix(".md").write_text("\n".join(md) + "\n", encoding="utf-8")
    print(json.dumps({k: report[k] for k in ("sourceRevision", "summary", "files", "registeredResourcePairs", "modules", "excluded", "problems", "changedDuringScan")}, ensure_ascii=False, indent=2))
    assets = report["assetsWithoutParallelEnglish"]
    print("assets_without_parallel_english_files=" + str(len(assets)))
    for name, count in collections.Counter(a["file"].split("/PlayerExports/", 1)[1].split("/", 1)[0] if "/PlayerExports/" in a["file"] else a["module"] for a in assets).most_common():
        print(name + ": " + str(count) + " files")
    return 1 if report["changedDuringScan"] else 0


if __name__ == "__main__":
    raise SystemExit(main())

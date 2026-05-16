from __future__ import annotations

import argparse
import csv
import hashlib
import json
import os
import re
import subprocess
import tempfile
import zipfile
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
from xml.etree import ElementTree as ET
from xml.sax.saxutils import escape


TEXT_ATTRS = {"text", "name", "description", "title", "value", "hint", "tooltip", "label"}
DEFAULT_TEXT_ROOTS = ("ModuleData", "GUI", "LauncherGUI", "Prefabs")
HEAVY_ROOTS = (
    "AssetPackages",
    "AssetSources",
    "Atmospheres",
    "Music",
    "NavMeshPrefabs",
    "SceneObj",
    "Sounds",
    "TileSets",
    "Videos",
)
SOURCE_PATTERN = (
    r"TextObject|GameText|GameTexts|AddDialogLine|AddPlayerLine|AddDialog|"
    r"AddRepeatable|AddGameMenu|InquiryData|InformationMessage|SetTextVariable"
)
STRING_RE = re.compile(r'@?"([^"\\]*(?:\\.[^"\\]*)*)"')
ATTR_RE = re.compile(
    r"\b(text|name|description|title|value|hint|tooltip|label)\s*=\s*\"([^\"\\]*(?:\\.[^\"\\]*)*)\"",
    re.IGNORECASE,
)
TOKEN_RE = re.compile(r"[\w']+", re.UNICODE)


def sha256(text: str) -> str:
    return hashlib.sha256(text.strip().encode("utf-8")).hexdigest()


def likely_human_text(text: str | None) -> bool:
    if not text or not text.strip():
        return False
    t = text.strip()
    if len(t) < 2:
        return False
    if re.fullmatch(r"[0-9.\-_,:; ]+", t):
        return False
    if re.fullmatch(r"[A-Za-z0-9_./\\:-]+", t) and len(t) < 4:
        return False
    if re.fullmatch(r"#[0-9a-fA-F]{6,8}", t):
        return False
    return True


def category(relative_path: str, key: str = "", field: str = "") -> str:
    s = f"{relative_path} {key} {field}".lower()
    if re.search(r"culture|cultures", s):
        return "Culture"
    if re.search(r"background|history|encyclopedia|lore|concept|kingdom|clan|hero|settlement|village|town|castle", s):
        return "Background/History/Encyclopedia"
    if re.search(r"conversation|dialog|dialogue|lordconversation|conversationcampaign", s):
        return "Dialogue"
    if re.search(r"quest|issue|storymode|mainstoryline|tutorial", s):
        return "Quest/Issue/Story"
    if re.search(r"language|languages|string|std_", s):
        return "Localization"
    if re.search(r"gui|prefab|gauntlet|brush|sprite|movie|menu", s):
        return "UI/System Text"
    return "Other Text"


def is_english_source_path(relative_path: str) -> bool:
    normalized = relative_path.replace("\\", "/")
    parts = normalized.split("/")
    if len(parts) >= 3 and parts[0] == "ModuleData" and parts[1] == "Languages":
        language = parts[2].lower()
        if language in {"en", "eng", "english", "voicedlines"}:
            return True
        filename = parts[-1].lower()
        return "_en." in filename or filename.endswith("_en.xml") or filename.endswith("_english.xml")
    return True


def node_name(tag: str) -> str:
    return tag.rsplit("}", 1)[-1]


def value_attr(elem: ET.Element | None) -> str:
    if elem is None:
        return ""
    return elem.attrib.get("value", "")


def parse_module(submodule: Path) -> dict[str, str] | None:
    try:
        root = ET.parse(submodule).getroot()
    except ET.ParseError:
        return None
    values: dict[str, str] = {}
    for child in root:
        values[node_name(child.tag)] = value_attr(child)
    module_type = values.get("ModuleType", "")
    if not module_type.startswith("Official"):
        return None
    folder = submodule.parent.name
    return {
        "Folder": folder,
        "Path": str(submodule.parent),
        "Id": values.get("Id", ""),
        "Name": values.get("Name", ""),
        "Version": values.get("Version", ""),
        "RequiredBaseVersion": values.get("RequiredBaseVersion", ""),
        "ModuleType": module_type,
        "ModuleCategory": values.get("ModuleCategory", ""),
        "DefaultModule": values.get("DefaultModule", ""),
        "Entries": 0,
    }


def csv_row(module: str, source_type: str, cat: str, relative_path: str, location: str, key: str, field: str, text: str) -> dict[str, object]:
    normalized = text.strip()
    return {
        "Module": module,
        "SourceType": source_type,
        "Category": cat,
        "RelativePath": relative_path,
        "Location": location,
        "Key": key,
        "Field": field,
        "TextLength": len(normalized),
        "WordishCount": len(TOKEN_RE.findall(normalized)),
        "Sha256": sha256(normalized),
    }


def write_docx(path: Path, title: str, module_rows: list[dict[str, object]], category_rows: list[dict[str, object]], source_rows: list[dict[str, object]], top_rows: list[dict[str, object]], csv_path: Path, game_root: Path, source_root: Path, include_heavy: bool) -> None:
    def p(text: str, style: str = "") -> str:
        style_xml = f'<w:pPr><w:pStyle w:val="{style}"/></w:pPr>' if style else ""
        return f'<w:p>{style_xml}<w:r><w:t xml:space="preserve">{escape(text)}</w:t></w:r></w:p>'

    def table(headers: list[str], rows: list[dict[str, object]], props: list[str]) -> str:
        parts = ['<w:tbl><w:tblPr><w:tblStyle w:val="TableGrid"/><w:tblW w:w="0" w:type="auto"/></w:tblPr>']
        parts.append("<w:tr>")
        for header in headers:
            parts.append(f"<w:tc><w:p><w:r><w:b/><w:t>{escape(header)}</w:t></w:r></w:p></w:tc>")
        parts.append("</w:tr>")
        for row in rows:
            parts.append("<w:tr>")
            for prop in props:
                parts.append(f'<w:tc><w:p><w:r><w:t xml:space="preserve">{escape(str(row.get(prop, "")))}</w:t></w:r></w:p></w:tc>')
            parts.append("</w:tr>")
        parts.append("</w:tbl>")
        return "".join(parts)

    body: list[str] = []
    body.append(p(title, "Title"))
    body.append(p(f"Generated: {datetime.now().astimezone().strftime('%Y-%m-%d %H:%M:%S %z')}"))
    body.append(p(f"Game root: {game_root}"))
    body.append(p(f"Decompiled source root: {source_root}"))
    body.append(p("Copyright note: this document is an audit index. It intentionally does not reproduce full original TaleWorlds or DLC text. The full manifest records source paths, keys, fields, lengths, categories, and SHA-256 hashes for traceability."))
    body.append(p("Scope", "Heading1"))
    body.append(p("Included modules are all local modules whose SubModule.xml declares ModuleType Official or OfficialOptional. Installed official DLC such as NavalDLC is included when present. Heavy resource directories included: " + str(include_heavy)))
    body.append(table(["Module", "Id", "Name", "Version", "Type", "Category", "Default", "Entries"], module_rows, ["Folder", "Id", "Name", "Version", "ModuleType", "ModuleCategory", "DefaultModule", "Entries"]))
    body.append(p("Text Categories", "Heading1"))
    body.append(table(["Category", "Entries", "Files", "Modules"], category_rows, ["Category", "Entries", "Files", "Modules"]))
    body.append(p("Source Types", "Heading1"))
    body.append(table(["Source type", "Entries", "Files", "Modules"], source_rows, ["SourceType", "Entries", "Files", "Modules"]))
    body.append(p("Largest Source Files", "Heading1"))
    body.append(p("These files contain the most discovered text entries. Use the CSV manifest for exact per-entry lookup."))
    body.append(table(["Module", "Category", "Entries", "Relative path"], top_rows, ["Module", "Category", "Entries", "RelativePath"]))
    body.append(p("Full Manifest", "Heading1"))
    body.append(p(f"CSV manifest: {csv_path}"))
    body.append(p("Columns: Module, SourceType, Category, RelativePath, Location, Key, Field, TextLength, WordishCount, Sha256. Text bodies are excluded by design; resolve rows back to the local installed game files when licensed content must be inspected locally."))
    body.append('<w:sectPr><w:pgSz w:w="11906" w:h="16838"/><w:pgMar w:top="1440" w:right="1440" w:bottom="1440" w:left="1440" w:header="708" w:footer="708" w:gutter="0"/></w:sectPr>')
    document_xml = '<?xml version="1.0" encoding="UTF-8" standalone="yes"?><w:document xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main"><w:body>' + "".join(body) + "</w:body></w:document>"
    styles_xml = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<w:styles xmlns:w="http://schemas.openxmlformats.org/wordprocessingml/2006/main">
  <w:style w:type="paragraph" w:styleId="Normal"><w:name w:val="Normal"/><w:qFormat/></w:style>
  <w:style w:type="paragraph" w:styleId="Title"><w:name w:val="Title"/><w:basedOn w:val="Normal"/><w:qFormat/><w:rPr><w:b/><w:sz w:val="34"/></w:rPr></w:style>
  <w:style w:type="paragraph" w:styleId="Heading1"><w:name w:val="heading 1"/><w:basedOn w:val="Normal"/><w:next w:val="Normal"/><w:qFormat/><w:rPr><w:b/><w:sz w:val="28"/></w:rPr></w:style>
  <w:style w:type="table" w:styleId="TableGrid"><w:name w:val="Table Grid"/><w:tblPr><w:tblBorders><w:top w:val="single" w:sz="4" w:space="0" w:color="auto"/><w:left w:val="single" w:sz="4" w:space="0" w:color="auto"/><w:bottom w:val="single" w:sz="4" w:space="0" w:color="auto"/><w:right w:val="single" w:sz="4" w:space="0" w:color="auto"/><w:insideH w:val="single" w:sz="4" w:space="0" w:color="auto"/><w:insideV w:val="single" w:sz="4" w:space="0" w:color="auto"/></w:tblBorders></w:tblPr></w:style>
</w:styles>"""
    content_types = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
  <Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/>
  <Default Extension="xml" ContentType="application/xml"/>
  <Override PartName="/word/document.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml"/>
  <Override PartName="/word/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml"/>
  <Override PartName="/docProps/core.xml" ContentType="application/vnd.openxmlformats-package.core-properties+xml"/>
  <Override PartName="/docProps/app.xml" ContentType="application/vnd.openxmlformats-officedocument.extended-properties+xml"/>
</Types>"""
    root_rels = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="word/document.xml"/>
  <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties" Target="docProps/core.xml"/>
  <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties" Target="docProps/app.xml"/>
</Relationships>"""
    doc_rels = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
  <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/>
</Relationships>"""
    now = datetime.now(timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
    core = f"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<cp:coreProperties xmlns:cp="http://schemas.openxmlformats.org/package/2006/metadata/core-properties" xmlns:dc="http://purl.org/dc/elements/1.1/" xmlns:dcterms="http://purl.org/dc/terms/" xmlns:dcmitype="http://purl.org/dc/dcmitype/" xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance">
  <dc:title>{escape(title)}</dc:title>
  <dc:creator>Codex</dc:creator>
  <cp:lastModifiedBy>Codex</cp:lastModifiedBy>
  <dcterms:created xsi:type="dcterms:W3CDTF">{now}</dcterms:created>
  <dcterms:modified xsi:type="dcterms:W3CDTF">{now}</dcterms:modified>
</cp:coreProperties>"""
    app = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?>
<Properties xmlns="http://schemas.openxmlformats.org/officeDocument/2006/extended-properties" xmlns:vt="http://schemas.openxmlformats.org/officeDocument/2006/docPropsVTypes">
  <Application>Codex Python OpenXML Exporter</Application>
</Properties>"""
    with tempfile.TemporaryDirectory() as temp_dir:
        temp = Path(temp_dir)
        (temp / "_rels").mkdir()
        (temp / "word" / "_rels").mkdir(parents=True)
        (temp / "docProps").mkdir()
        (temp / "[Content_Types].xml").write_text(content_types, encoding="utf-8")
        (temp / "_rels" / ".rels").write_text(root_rels, encoding="utf-8")
        (temp / "word" / "_rels" / "document.xml.rels").write_text(doc_rels, encoding="utf-8")
        (temp / "word" / "document.xml").write_text(document_xml, encoding="utf-8")
        (temp / "word" / "styles.xml").write_text(styles_xml, encoding="utf-8")
        (temp / "docProps" / "core.xml").write_text(core, encoding="utf-8")
        (temp / "docProps" / "app.xml").write_text(app, encoding="utf-8")
        if path.exists():
            path.unlink()
        with zipfile.ZipFile(path, "w", zipfile.ZIP_DEFLATED) as zf:
            for file in temp.rglob("*"):
                zf.write(file, file.relative_to(temp).as_posix())


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--game-root", default=r"F:\SteamLibrary\steamapps\common\Mount & Blade II Bannerlord")
    parser.add_argument("--source-root", default=r"f:\my mod\animusforge-1.3.x\原版游戏本体代码")
    parser.add_argument("--output-dir", default=r"f:\my mod\animusforge-1.3.x\docs\vanilla_text_export")
    parser.add_argument("--include-heavy-resource-directories", action="store_true")
    parser.add_argument("--english-only", action="store_true", help="Skip non-English ModuleData/Languages translation folders.")
    args = parser.parse_args()

    game_root = Path(args.game_root)
    source_root = Path(args.source_root)
    output_dir = Path(args.output_dir)
    output_dir.mkdir(parents=True, exist_ok=True)
    modules_root = game_root / "Modules"
    modules = []
    for submodule in modules_root.glob("*/SubModule.xml"):
        parsed = parse_module(submodule)
        if parsed:
            modules.append(parsed)
    module_by_folder = {m["Folder"]: m for m in modules}
    module_folders = set(module_by_folder)

    csv_path = output_dir / "vanilla_official_and_dlc_text_manifest.no_original_text.csv"
    if args.english_only:
        csv_path = output_dir / "vanilla_official_and_dlc_english_text_manifest.no_original_text.csv"
    category_counts: Counter[str] = Counter()
    source_counts: Counter[str] = Counter()
    module_counts: Counter[str] = Counter()
    file_counts_by_category: defaultdict[str, set[str]] = defaultdict(set)
    module_sets_by_category: defaultdict[str, set[str]] = defaultdict(set)
    file_counts_by_source: defaultdict[str, set[str]] = defaultdict(set)
    module_sets_by_source: defaultdict[str, set[str]] = defaultdict(set)
    top_file_counts: Counter[tuple[str, str, str]] = Counter()

    fields = ["Module", "SourceType", "Category", "RelativePath", "Location", "Key", "Field", "TextLength", "WordishCount", "Sha256"]

    def emit(writer: csv.DictWriter, row: dict[str, object]) -> None:
        writer.writerow(row)
        mod = str(row["Module"])
        cat = str(row["Category"])
        source = str(row["SourceType"])
        rel = str(row["RelativePath"])
        module_counts[mod] += 1
        category_counts[cat] += 1
        source_counts[source] += 1
        file_counts_by_category[cat].add(rel)
        module_sets_by_category[cat].add(mod)
        file_counts_by_source[source].add(rel)
        module_sets_by_source[source].add(mod)
        top_file_counts[(mod, cat, rel)] += 1

    with csv_path.open("w", encoding="utf-8-sig", newline="") as handle:
        writer = csv.DictWriter(handle, fieldnames=fields)
        writer.writeheader()
        for module in modules:
            module_path = Path(str(module["Path"]))
            scan_roots = [module_path / root for root in DEFAULT_TEXT_ROOTS if (module_path / root).exists()]
            if args.include_heavy_resource_directories:
                scan_roots.extend(module_path / root for root in HEAVY_ROOTS if (module_path / root).exists())
            files = [module_path / "SubModule.xml"]
            for root in scan_roots:
                for file in root.rglob("*"):
                    if file.is_file() and file.suffix.lower() in {".xml", ".xslt", ".xsl", ".json", ".txt"} and file.stat().st_size <= 8 * 1024 * 1024:
                        files.append(file)
            for file in files:
                if not file.exists():
                    continue
                rel = str(file.relative_to(module_path))
                if args.english_only and not is_english_source_path(rel):
                    continue
                suffix = file.suffix.lower()
                if suffix in {".xml", ".xslt", ".xsl"}:
                    try:
                        root = ET.parse(file).getroot()
                        for elem in root.iter():
                            key = ""
                            for key_name in ("id", "stringId", "name", "key"):
                                if key_name in elem.attrib and elem.attrib[key_name].strip():
                                    key = elem.attrib[key_name]
                                    break
                            for attr_name, attr_value in elem.attrib.items():
                                if attr_name.lower() not in TEXT_ATTRS or not likely_human_text(attr_value):
                                    continue
                                cat = category(rel, key, attr_name)
                                emit(writer, csv_row(str(module["Folder"]), "Module XML", cat, rel, node_name(elem.tag), key, attr_name, attr_value))
                            if elem.text and likely_human_text(elem.text):
                                cat = category(rel, key, node_name(elem.tag))
                                emit(writer, csv_row(str(module["Folder"]), "Module XML Text Node", cat, rel, node_name(elem.tag), key, "#text", elem.text))
                    except ET.ParseError:
                        with file.open("r", encoding="utf-8", errors="ignore") as reader:
                            for line_no, line in enumerate(reader, 1):
                                for match in ATTR_RE.finditer(line):
                                    field = match.group(1)
                                    text = match.group(2)
                                    if not likely_human_text(text):
                                        continue
                                    cat = category(rel, "", field)
                                    emit(writer, csv_row(str(module["Folder"]), "Module XML Attribute Fallback", cat, rel, f"line {line_no}", "", field, text))
                elif suffix in {".json", ".txt"}:
                    with file.open("r", encoding="utf-8", errors="ignore") as reader:
                        for line_no, line in enumerate(reader, 1):
                            for match in STRING_RE.finditer(line):
                                text = match.group(1)
                                if likely_human_text(text):
                                    cat = category(rel, "", "json/text")
                                    emit(writer, csv_row(str(module["Folder"]), "Module JSON/TXT String", cat, rel, f"line {line_no}", "", "string", text))

        if source_root.exists():
            rg_command = [
                "rg",
                "--json",
                "-n",
                "-S",
                "--glob",
                "*.cs",
                SOURCE_PATTERN,
                str(source_root),
            ]
            try:
                process = subprocess.Popen(rg_command, stdout=subprocess.PIPE, stderr=subprocess.DEVNULL, text=True, encoding="utf-8", errors="replace")
                assert process.stdout is not None
                for json_line in process.stdout:
                    if not json_line.strip():
                        continue
                    item = json.loads(json_line)
                    if item.get("type") != "match":
                        continue
                    full_path = Path(item["data"]["path"]["text"])
                    line = item["data"]["lines"]["text"]
                    line_no = item["data"]["line_number"]
                    try:
                        rel = str(full_path.relative_to(source_root))
                    except ValueError:
                        continue
                    module_folder = rel.split(os.sep, 1)[0].split("/", 1)[0]
                    if module_folder not in module_folders:
                        continue
                    for match in STRING_RE.finditer(line):
                        text = match.group(1)
                        if likely_human_text(text):
                            cat = category(rel, "", "csharp-string")
                            emit(writer, csv_row(module_folder, "Decompiled C# Text Reference", cat, rel, f"line {line_no}", "", "string", text))
                process.wait()
            except FileNotFoundError:
                pass

    for module in modules:
        module["Entries"] = module_counts[str(module["Folder"])]

    category_rows = [
        {
            "Category": cat,
            "Entries": count,
            "Files": len(file_counts_by_category[cat]),
            "Modules": len(module_sets_by_category[cat]),
        }
        for cat, count in sorted(category_counts.items())
    ]
    source_rows = [
        {
            "SourceType": source,
            "Entries": count,
            "Files": len(file_counts_by_source[source]),
            "Modules": len(module_sets_by_source[source]),
        }
        for source, count in sorted(source_counts.items())
    ]
    top_rows = [
        {"Module": mod, "Category": cat, "RelativePath": rel, "Entries": count}
        for (mod, cat, rel), count in top_file_counts.most_common(80)
    ]

    docx_path = output_dir / "Bannerlord_vanilla_official_dlc_text_index.docx"
    if args.english_only:
        docx_path = output_dir / "Bannerlord_vanilla_official_dlc_english_text_index.docx"
    write_docx(
        docx_path,
        "Bannerlord Vanilla + Official DLC Text Index",
        sorted(modules, key=lambda m: str(m["Folder"])),
        category_rows,
        source_rows,
        top_rows,
        csv_path,
        game_root,
        source_root,
        args.include_heavy_resource_directories,
    )
    summary_path = output_dir / "vanilla_official_and_dlc_text_summary.json"
    if args.english_only:
        summary_path = output_dir / "vanilla_official_and_dlc_english_text_summary.json"
    summary = {
        "GeneratedAt": datetime.now().astimezone().isoformat(),
        "GameRoot": str(game_root),
        "DecompiledSourceRoot": str(source_root),
        "Docx": str(docx_path),
        "CsvManifest": str(csv_path),
        "Summary": str(summary_path),
        "Modules": modules,
        "Categories": category_rows,
        "SourceTypes": source_rows,
        "TotalEntries": sum(module_counts.values()),
        "IncludeHeavyResourceDirectories": args.include_heavy_resource_directories,
        "EnglishOnly": args.english_only,
    }
    summary_path.write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps({"Docx": str(docx_path), "CsvManifest": str(csv_path), "Summary": str(summary_path), "ModuleCount": len(modules), "EntryCount": sum(module_counts.values())}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
